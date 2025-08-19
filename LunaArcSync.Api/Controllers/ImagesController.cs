using LunaArcSync.Api.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Security.Claims; // 引入 Claims

namespace LunaArcSync.Api.Controllers
{
    [ApiController]
    [Route("api/images")]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IFileStorageService _fileStorageService;

        public ImagesController(IDocumentRepository documentRepository, IFileStorageService fileStorageService)
        {
            _documentRepository = documentRepository;
            _fileStorageService = fileStorageService;
        }

        [HttpGet("{versionId}")]
        public async Task<IActionResult> GetImageByVersionId(Guid versionId)
        {
            var version = await _documentRepository.GetVersionByIdAsync(versionId);

            if (version == null || string.IsNullOrEmpty(version.ImagePath))
            {
                return NotFound("Image or version data not found.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // 安全检查：确保该版本所属的文档属于当前登录的用户
            var document = await _documentRepository.GetDocumentByIdAsync(version.DocumentId, userId);
            if (document == null)
            {
                return Forbid(); // 如果文档不属于该用户，则明确拒绝访问
            }

            // 注意：您代码中的 ImagePath 似乎就是物理文件名，如果不是，请用 PhysicalFileName
            var fileBytes = await _fileStorageService.ReadFileBytesAsync(version.ImagePath);
            if (fileBytes == null)
            {
                return NotFound("Physical file not found.");
            }

            var mimeType = GetMimeType(version.ImagePath);

            return File(fileBytes, mimeType);
        }

        private string GetMimeType(string fileName)
        {
            var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream",
            };
        }
    }
}