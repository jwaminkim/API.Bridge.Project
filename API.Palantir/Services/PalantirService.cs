using API.Palantir.Enums;
using API.Palantir.Helpers;
using API.Palantir.Middleware;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Concurrent;

namespace API.Palantir.Services
{
    public class PalantirService
    {
        private readonly IConfiguration _config;
        private readonly PalantirSessionService _session;

        public PalantirService(PalantirSessionService session, IConfiguration config)
        {
            _session = session;
            _config = config;
        }

        public async Task<List<TResponse>> PalantirProcessAndReturnAsync<TRequest, TResponse>(
            string token,
            List<TRequest> requests,
            PalantirNoteType type)
        {
            try
            {
                //log주석//
                string traceId = Guid.NewGuid().ToString("N")[..8];
                Log.Information($"[{traceId}] request data : {JsonConvert.SerializeObject(requests)}");
                //log주석//Log.Information($"request start");

                var results = new ConcurrentBag<TResponse>();
                int maxTextLength = _config.GetValue<int>("Palantir:MaxTextLength");
                var chunks = NoteChunkHelper.MaxTextLengthSplit(
                    requests,
                    maxTextLength
                );

                //log주석//
                Log.Information($"request count : {requests.Count}");
                //log주석//
                Log.Information($"chunks count : {chunks.Count}");

                int maxParallel = _config.GetValue<int>("Palantir:MaxParallel", 5);

                //log주석//Log.Information($"maxParallel : {maxParallel}");
                //log주석//Log.Information($"maxTextLength : {maxTextLength}"); 

                var semaphore = new SemaphoreSlim(maxParallel);

                var tasks = chunks.Select(async chunk =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        int maxRetry = _config.GetValue<int>("Palantir:MaxRetry");
                        int rateLimitDelaySeconds = _config.GetValue<int>("Palantir:RateLimitDelaySeconds");
                        for (int attempt = 1; attempt <= maxRetry; attempt++)
                        {
                            try
                            {
                                //log주석//
                                Log.Information($"create session start (attempt {attempt})");

                                //chunk별 세션 생성
                                string sessionId = await _session.CreateSessionAsync(token, type);

                                //log주석//
                                Log.Information($"create session end (attempt {attempt})");
                                //log주석//
                                Log.Information($"blocking contine session /start chunk size : {chunk.Count} (attempt {attempt})");

                                var response = await _session.BlockingContinueSessionAsync<TRequest, TResponse>(
                                            token,
                                            sessionId,
                                            chunk,
                                            type);

                                //log주석//
                                Log.Information($"blocking contine session /end chunk size : {chunk.Count} (attempt {attempt})");

                                foreach (var r in response)
                                {
                                    results.Add(r);
                                }
                                break;
                            }
                            catch (RateLimitException ex) when (attempt < maxRetry)
                            {
                                //Rate Limit은 더 오래 대기
                                Log.Warning($"[Palantir Retry] attempt/maxRetry={attempt}/{maxRetry}, ChunkSize={chunk.Count}, {rateLimitDelaySeconds}초 대기 후 재시도...");
                                await Task.Delay(TimeSpan.FromSeconds(rateLimitDelaySeconds)).ConfigureAwait(false);
                            }
                            catch (Exception ex) 
                            {
                                Log.Error($"[Palantir Attempt Error] attempt={attempt}/{maxRetry}, ChunkSize={chunk.Count}, error={ex.Message}.");
                                break;
                            }
                        }                        
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex,
                            $"[Palantir Chunk Error] Type={type}, ChunkSize={chunk.Count}",
                            type,
                            chunk.Count);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
                Log.Information($"[{traceId}][Final] results : {JsonConvert.SerializeObject(results.ToList())}");
                return results.ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Palantir Process Error] Type={type}");

                throw;
            }
        }
    }
}
