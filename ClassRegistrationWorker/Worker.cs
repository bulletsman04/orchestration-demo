namespace ClassRegistrationWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("ClassRegistrationWorker running at: {time}", DateTimeOffset.Now);
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}