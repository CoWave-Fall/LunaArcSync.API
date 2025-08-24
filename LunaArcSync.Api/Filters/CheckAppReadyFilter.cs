using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using LunaArcSync.Api.Core.Interfaces;
using System.Net;

namespace LunaArcSync.Api.Filters
{
    public class CheckAppReadyFilter : IActionFilter
    {
        private readonly IApplicationStatusService _statusService;

        public CheckAppReadyFilter(IApplicationStatusService statusService)
        {
            _statusService = statusService;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // The AboutController has its own logic, so we skip the filter for it.
            if (context.Controller.GetType().Name == "AboutController")
            {
                return;
            }

            if (!_statusService.IsAppReady)
            {
                context.Result = new ObjectResult(new { code = "ServiceUnavailable", message = _statusService.GetReason() })
                {
                    StatusCode = (int)HttpStatusCode.ServiceUnavailable
                };
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Do nothing
        }
    }
}
