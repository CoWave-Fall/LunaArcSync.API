using LunaArcSync.Api.Core.Interfaces;

namespace LunaArcSync.Api.Infrastructure.Services
{
    public class ApplicationStatusService : IApplicationStatusService
    {
        private volatile bool _isReady = false;
        private string _reason = "Application is starting up and initializing cache...";

        public bool IsAppReady => _isReady;

        public string GetReason() => _reason;

        public void SetReady(string reason = "")
        {
            _reason = reason;
            _isReady = true;
        }
    }
}
