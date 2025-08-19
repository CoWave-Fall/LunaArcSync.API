using LunaArcSync.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LunaArcSync.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AboutController : ControllerBase
    {
        [AllowAnonymous] // <-- 关键！允许匿名访问
        [HttpGet]
        public ActionResult<AboutDto> GetAboutInfo()
        {
            var aboutInfo = new AboutDto();
            // 在未来，这些值可以从 appsettings.json 或程序集信息中动态读取
            return Ok(aboutInfo);
        }
    }
}