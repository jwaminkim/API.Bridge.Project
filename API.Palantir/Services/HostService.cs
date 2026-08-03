using Serilog;

namespace API.Palantir.Services
{
    public class HostService : BackgroundService
    {
        private IConfiguration config { get; }
        public HostService(IConfiguration _config)
        {
            config = _config;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var title = config.GetValue<string>("APITitle");

            Log.Information($"{title} is starting.");
            stoppingToken.Register(() => Log.Information($"{title} is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                //Logger.LogInformation("ServiceA is doing background work.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            Log.Information($"{title} has stopped.");
        }

    }
}
