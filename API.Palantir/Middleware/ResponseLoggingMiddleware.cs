using Serilog;
using System.Diagnostics;

namespace API.Palantir.Middleware
{
    public class ResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;        

        public ResponseLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 응답 body 캡처를 위해 스트림 교체
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            var sw = Stopwatch.StartNew();
            try
            {
                await _next(context);
                sw.Stop();

                // 응답 body 읽기
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                string responseBodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                string traceId = context.Items["TraceId"]?.ToString() ?? "unknown";

                Log.Information($"RESPONSE : " +
                            $"elapsed={sw.ElapsedMilliseconds}ms " +
                            $"status={context.Response.StatusCode} ");                            
                            //$"body={responseBodyText}");
            }
            catch (Exception ex)
            {
                //sw.Stop();
                string traceId = context.Items["TraceId"]?.ToString() ?? "unknown";
                Log.Error($"RESPONSE_FAIL " +
                             $"elapsed={sw.ElapsedMilliseconds}ms " +
                             $"message={ex.Message}");
                throw;
            }
            finally
            {
                // 원래 스트림으로 복원
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }
        }
    }
}
