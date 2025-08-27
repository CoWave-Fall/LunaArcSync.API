using LunaArcSync.Api.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.DTOs;

namespace LunaArcSync.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly IDataExportImportService _dataService;
        private readonly UserManager<AppUser> _userManager;

        public DataController(IDataExportImportService dataService, UserManager<AppUser> userManager)
        {
            _dataService = dataService;
            _userManager = userManager;
        }

        [HttpGet("export")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportAllData()
        {
            var stream = await _dataService.ExportDataAsync(null, true);
            return File(stream, "application/zip", "LunaArcSync_AllData_Export.zip");
        }

        [HttpGet("export/my")]
        [Authorize]
        public async Task<IActionResult> ExportMyData()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }
            var stream = await _dataService.ExportDataAsync(userId, false);
            return File(stream, "application/zip", $"LunaArcSync_UserData_{userId}_Export.zip");
        }

        [HttpGet("export/user/{targetUserId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportSpecificUserData(string targetUserId)
        {
            if (string.IsNullOrEmpty(targetUserId))
            {
                return BadRequest("Target user ID must be provided.");
            }
            var requestingUserId = _userManager.GetUserId(User);
            var stream = await _dataService.ExportDataAsync(requestingUserId, true, targetUserId);
            return File(stream, "application/zip", $"LunaArcSync_UserData_{targetUserId}_Export.zip");
        }

        [HttpPost("import")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportAllData([FromForm] ImportFileDto importFileDto)
        {
            if (importFileDto.File == null || importFileDto.File.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var importingUserId = _userManager.GetUserId(User);
            if (importingUserId == null)
            {
                return Unauthorized();
            }

            using (var stream = importFileDto.File.OpenReadStream())
            {
                await _dataService.ImportDataAsync(stream, importingUserId, true);
            }

            return Ok("Data imported successfully.");
        }

        [HttpPost("import/my")]
        [Authorize]
        public async Task<IActionResult> ImportMyData([FromForm] ImportFileDto importFileDto)
        {
            if (importFileDto.File == null || importFileDto.File.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var importingUserId = _userManager.GetUserId(User);
            if (importingUserId == null)
            {
                return Unauthorized();
            }

            using (var stream = importFileDto.File.OpenReadStream())
            {
                await _dataService.ImportDataAsync(stream, importingUserId, false);
            }

            return Ok("Your data imported successfully.");
        }
    }
}
