using API.Palantir.Middleware;
using API.Palantir.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Newtonsoft.Json;
using Serilog;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((_, config) =>
{
    config.WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration);
});


builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
    options.Limits.MaxRequestBodySize = 2000000000;
    options.Limits.MaxRequestBufferSize = 2000000000;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2000000000;
});

//설정 파일 추가
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("Configurations/database.json", optional: true, reloadOnChange: true)
    .AddJsonFile("Configurations/logger.json", optional: true, reloadOnChange: true)
    .AddJsonFile("Configurations/palantir.json", optional: true, reloadOnChange: true);

var hostyn = builder.Configuration.GetValue<bool>("HostService");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PlantAsset Palantir API",
        Version = "v1",
        Description = "API documentation for SWP3D P&ID"
    });
});
builder.Services.AddHealthChecks();

//HttpClient(커넥션 제한 설정) : 공통
builder.Services.AddHttpClient("palantir", client =>
{
    client.Timeout = TimeSpan.FromMinutes(30);
    client.DefaultRequestHeaders.ExpectContinue = false;
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    MaxConnectionsPerServer = 10,//특정서버로 동시 연결 개수 제한
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),//커넥션 사용시간(오래된 연결 교체)
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)//유휴 상태 커넥션 유지 시간
});

//HttpClient(커넥션 제한 설정) : 서비스 각각
//builder.Services.AddHttpClient<PalantirAuthService>()
//.ConfigureHttpClient(client =>
//{
//    client.Timeout = TimeSpan.FromMinutes(5);
//    client.DefaultRequestHeaders.ExpectContinue = false;
//})
//.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
//{
//    MaxConnectionsPerServer = 50,
//    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
//    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
//});

//builder.Services.AddHttpClient<PalantirSessionService>()
//.ConfigureHttpClient(client =>
//{
//    client.Timeout = TimeSpan.FromMinutes(5);
//    client.DefaultRequestHeaders.ExpectContinue = false;
//})
//.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
//{
//    MaxConnectionsPerServer = 50,
//    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
//    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
//});

//DI 등록
builder.Services.AddScoped<PalantirAuthService>();
builder.Services.AddScoped<PalantirSessionService>();
builder.Services.AddScoped<PalantirService>();
builder.Services.AddScoped<ExceptionMiddleware>();

if (hostyn)
{
    builder.Services.AddWindowsService();
    builder.Services.AddHostedService<HostService>();

    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        //서버가 요청 받는쪽 설정
        serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);  // http 연결 유지 시간
        serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30); // 요청 헤더 수신 시간
    });
}
else
{    
    //iis는 서버에서 직접 설정
    builder.Services.Configure<IISServerOptions>(options =>
    {
        options.AllowSynchronousIO = true;
        options.MaxRequestBodySize = 2000000000;
    });
}

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PlantAsset Palantir API v1");
        c.RoutePrefix = "apidoc"; // Swagger UI를 루트 경로에서 제공
    });
}
app.UseMiddleware<ExceptionMiddleware>();
//app.UseMiddleware<RequestLoggingMiddleware>();
//app.UseMiddleware<ResponseLoggingMiddleware>();

// HealthChecks 상태 확인용 엔드포인트
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            }),
            totalDuration = report.TotalDuration.TotalSeconds
        });
        await context.Response.WriteAsync(result);
    }
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

