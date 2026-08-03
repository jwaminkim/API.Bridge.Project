using API.Palantir.Enums;
using API.Palantir.Middleware;
using API.Palantir.Models.Palantir;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace API.Palantir.Services
{
    public class PalantirSessionService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly string _env;

        //public PalantirSessionService(HttpClient http, IConfiguration config)
        public PalantirSessionService(IHttpClientFactory factory, IConfiguration config)        
        {
            //_http = http;
            _http = factory.CreateClient("palantir");
            _config = config;
            _env = _config["Palantir:Env"] ?? "Prod";
        }

        public async Task<string> CreateSessionAsync(string token, PalantirNoteType type)
        {
            try
            {
                string key = type == PalantirNoteType.Discipline
                ? $"Palantir:{_env}:NoteDiscipline:CreateSessionUrl"
                : $"Palantir:{_env}:NoteCompare:CreateSessionUrl";

                using (var request = new HttpRequestMessage(HttpMethod.Post, _config[key]))
                {
                    //var request = new HttpRequestMessage(HttpMethod.Post, _config[key]);
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    string agentVersions = type == PalantirNoteType.Discipline ? $"Palantir:AgentVersion:Discipline" : $"Palantir:AgentVersion:Compare";
                    var payload = new
                    {
                        agentVersion = _config[agentVersions] ?? "5.0"
                    };

                    request.Content = new StringContent(
                        JsonConvert.SerializeObject(payload),
                        Encoding.UTF8,
                        "application/json"
                    );

                    //Log.Information($"[CreateSessionAsync] token : {token}");
                    //Log.Information($"[CreateSessionAsync] create session notecompare request url : {_config[key]}");
                    //Log.Information($"[CreateSessionAsync] create session start");

                    var sw = Stopwatch.StartNew();
                    var response = await _http.SendAsync(request);

                    sw.Stop();
                    //Log.Information($"[CreateSessionAsync] create session time : {sw.ElapsedMilliseconds}ms");
                    //log주석//Log.Information($"[CreateSessionAsync] response status : {response.StatusCode}");

                    response.EnsureSuccessStatusCode();

                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(
                        await response.Content.ReadAsStringAsync()
                    )!;

                    //Log.Information($"[CreateSessionAsync] create session end");
                    //Log.Information($"[CreateSessionAsync] Session Id Response: {json.rid}");

                    string sessionId = Convert.ToString(json.rid);
                    if (string.IsNullOrWhiteSpace(sessionId))
                    {
                        Log.Warning($"[CreateSessionAsync] sessionId is null. Retry...");
                        throw new RateLimitException($"[CreateSessionAsync] sessionId is null. Retry...");
                    }
                    return json.rid;
                }
            }
            catch (TaskCanceledException ex)
            {
                Log.Error($"[CreateSessionAsync] timeout or cancelled", ex);
                throw;
            }
            catch (HttpRequestException ex)
            {
                Log.Error($"[CreateSessionAsync] http error", ex);
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"[CreateSessionAsync] unknown error", ex);
                throw;
            }            
        }

        public async Task<List<TResponse>> BlockingContinueSessionAsync<TRequest, TResponse>(
            string token,
            string sessionId,
            List<TRequest> requests,
            PalantirNoteType type)
        {
            try
            {
                //log주석//Log.Information($"[BlockingContinueSessionAsync] token : {token}");
                //log주석//Log.Information($"[BlockingContinueSessionAsync] sessionId : {sessionId}");
                //log주석//Log.Information($"[BlockingContinueSessionAsync] requests data : {JsonConvert.SerializeObject(requests)}");

                var responseResults = new List<TResponse>();
                string key = type == PalantirNoteType.Discipline
                    ? $"Palantir:{_env}:NoteDiscipline:BlockingContinueUrl"
                    : $"Palantir:{_env}:NoteCompare:BlockingContinueUrl";

                string url = _config[key]!
                    .Replace("{{sessionRid}}", sessionId);

                //JSON 파일처럼 객체로 전달
                var payload = new
                {
                    userInput = new
                    {
                        text = JsonConvert.SerializeObject(requests)
                    }
                };

                //URL 로그 추가
                //Log.Information($"[BlockingContinueSessionAsync] BlockingContinue URL: {url}");                
                //var request = new HttpRequestMessage(HttpMethod.Post, url);
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    request.Content = new StringContent(
                        JsonConvert.SerializeObject(payload),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var sw = Stopwatch.StartNew();
                    
                    //Log.Information($"[BlockingContinueSessionAsync] blockingcontinue session start");

                    var response = await _http.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    sw.Stop();
                    //log주석//Log.Information($"[BlockingContinueSessionAsync] blockingcontinue session time : {sw.ElapsedMilliseconds}ms");
                    //log주석//Log.Information($"[BlockingContinueSessionAsync] response status : {response.StatusCode}");

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    //RAW 로그 출력
                    //Log.Information($"[BlockingContinueSessionAsync] jsonResponse Raw Response: {jsonResponse}");
                    //limit error 감지
                    if (jsonResponse.Contains("RetryAttemptsExceeded") || jsonResponse.Contains("RateLimitsExceeded"))
                    {
                        Log.Warning($"[BlockingContinueSessionAsync] Rate Limit 감지 sessionId={sessionId}");
                        throw new RateLimitException($"[BlockingContinueSessionAsync] Rate limit exceeded. sessionId={sessionId}");
                    }

                    var agentResponse = JsonConvert.DeserializeObject<PalantirAgentResponse>(jsonResponse);
                    if (string.IsNullOrWhiteSpace(agentResponse?.AgentMarkdownResponse))
                    {
                        throw new Exception("[BlockingContinueSessionAsync] agentMarkdownResponse is null.");
                    }

                    var resultJson = agentResponse.AgentMarkdownResponse
                        .Replace("```json", "")
                        .Replace("```", "")
                        .Trim();

                    var responseItems = JsonConvert.DeserializeObject<List<TResponse>>(resultJson) ?? new List<TResponse>();
                    responseResults.AddRange(responseItems);
                }

                return responseResults;
            }
            catch (RateLimitException)
            {                
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Log.Error($"[BlockingContinueSessionAsync] timeout or cancelled", ex);
                throw;
            }
            catch (HttpRequestException ex)
            {
                Log.Error($"[BlockingContinueSessionAsync] http error", ex);
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"[BlockingContinueSessionAsync] unknown error", ex);
                throw;
            }            
        }
    }
}
