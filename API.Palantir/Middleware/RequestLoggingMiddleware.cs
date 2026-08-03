using Serilog;
using System.Text;
using Serilog;

namespace API.Palantir.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;        

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // TraceId 생성
            string traceId = Guid.NewGuid().ToString("N")[..8];
            context.Items["TraceId"] = traceId;

            // 요청 body 읽기 위해 스트림 교체
            context.Request.EnableBuffering();
            string requestBody = string.Empty;
            using (var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0; // 다시 읽을 수 있게 리셋
            }

            Log.Information($"REQUEST : " +
                        $"method={context.Request.Method} " +
                        $"path={context.Request.Path} " +
                        $"ip={context.Connection.RemoteIpAddress}");
                        //$"body={requestBody}");

            await _next(context);
        }
    }
}
