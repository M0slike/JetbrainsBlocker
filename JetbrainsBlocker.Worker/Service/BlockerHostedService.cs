using JetbrainsBlocker.Worker.Options;
using Microsoft.Extensions.Options;

namespace JetbrainsBlocker.Worker.Service;

internal sealed class BlockerHostedService : BackgroundService
{
    private readonly BlockerService _blockerService;
    private readonly ILogger<BlockerHostedService> _logger;
    private readonly IOptionsMonitor<ServiceOptions> _optionsMonitor;

    public BlockerHostedService(ILogger<BlockerHostedService> logger, IOptionsMonitor<ServiceOptions> optionsMonitor,
        BlockerService blockerService)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _blockerService = blockerService;

        _optionsMonitor.OnChange(OnConfigReload);
        _logger.LogInformation("Service is running with {Timeout} seconds timeout between calls",
            _optionsMonitor.CurrentValue.Timeout.TotalSeconds);
    }

    private async void OnConfigReload(ServiceOptions options)
    {
        _logger.LogInformation("Configuration is changed, updating firewall rules");
        await _blockerService.BlockAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _blockerService.BlockAsync(stoppingToken);
                await Task.Delay(_optionsMonitor.CurrentValue.Timeout, stoppingToken);
            }
            catch (TaskCanceledException e)
            {
                // expected on service stopping, swallowing
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Fatal error: {ErrorMessage}", e.Message);
            }
        }
    }
}
