using LunaArcSync.Api.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LunaArcSync.Api.BackgroundTasks
{
    public class CacheWarmingService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CacheWarmingService> _logger;
        private readonly IApplicationStatusService _statusService;

        public CacheWarmingService(IServiceProvider serviceProvider, ILogger<CacheWarmingService> logger, IApplicationStatusService statusService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _statusService = statusService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cache Warming Service is starting.");

            // Don't block the startup process
            _ = Task.Run(async () =>
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                        var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

                        _logger.LogInformation("Warming up the tags cache...");
                        var tags = await documentRepository.GetAllTagsAsync();
                        memoryCache.Set(GetTagsCacheKey(), tags, TimeSpan.FromDays(1));
                        _logger.LogInformation("Tags cache warmed up with {TagCount} tags.", tags.Count);
                    }

                    _statusService.SetReady();
                    _logger.LogInformation("Cache Warming Service has completed. Application is now ready.");
                }
                catch (Exception ex)
                {
                    var reason = "A critical error occurred during cache warming. The application might be in an unstable state.";
                    _logger.LogCritical(ex, reason);
                    _statusService.SetReady(reason); // Set as ready but with a critical reason
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cache Warming Service is stopping.");
            return Task.CompletedTask;
        }

        public static string GetTagsCacheKey() => "AllKnownTags";
    }
}
