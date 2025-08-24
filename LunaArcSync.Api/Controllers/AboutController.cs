using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LunaArcSync.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AboutController : ControllerBase
    {
        private readonly IApplicationStatusService _statusService;

        public AboutController(IApplicationStatusService statusService)
        {
            _statusService = statusService;
        }

        [AllowAnonymous] // <-- 关键！允许匿名访问
        [HttpGet]
        public ActionResult<AboutDto> GetAboutInfo()
        {
            if (!_statusService.IsAppReady)
            {
                var unavailableDto = new
                {
                    Status = "Initializing",
                    Message = _statusService.GetReason()
                };
                return StatusCode((int)HttpStatusCode.ServiceUnavailable, unavailableDto);
            }

            var aboutInfo = new AboutDto();
            return Ok(aboutInfo);
        }
    }
}