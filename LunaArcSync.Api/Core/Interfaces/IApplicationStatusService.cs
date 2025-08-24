namespace LunaArcSync.Api.Core.Interfaces
{
    public interface IApplicationStatusService
    {
        bool IsAppReady { get; }
        void SetReady(string reason = "");
        string GetReason();
    }
}
