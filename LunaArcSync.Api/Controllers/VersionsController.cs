using Microsoft.AspNetCore.Authorization; // 引入 Ocr DTOs
using Microsoft.AspNetCore.Mvc;
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.DTOs;
using LunaArcSync.Api.DTOs.Ocr;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json; // 引入 System.Text.Json
using System.Threading.Tasks;

namespace LunaArcSync.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/documents/{documentId}/versions")] // 嵌套路由，非常清晰
    public class VersionsController(
                IDocumentRepository documentRepository,
                Infrastructure.Data.AppDbContext context,
                ILogger<JobsController> logger) : ControllerBase
    {
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly Infrastructure.Data.AppDbContext _context = context;
        private readonly ILogger<JobsController> _logger = logger;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VersionDto>>> GetVersions(Guid documentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();


            var document = await _documentRepository.GetDocumentWithVersionsByIdAsync(documentId, userId);
            if (document == null)
            {
                return NotFound("Document not found.");
            }

            var versionDtos = document.Versions.Select(v =>
            {
                OcrResultDto? ocrResult = null;
                if (!string.IsNullOrEmpty(v.OcrData))
                {
                    try
                    {
                        ocrResult = JsonSerializer.Deserialize<OcrResultDto>(v.OcrData);
                    }
                    catch (JsonException ex)
                    {
                        // 如果反序列化失败，记录一个错误，但不要让整个请求失败
                        _logger.LogError(ex, "Failed to deserialize OcrData for version {VersionId}", v.VersionId);
                    }
                }

                return new VersionDto
                {
                    VersionId = v.VersionId,
                    VersionNumber = v.VersionNumber,
                    Message = v.Message,
                    CreatedAt = v.CreatedAt,
                    OcrResult = ocrResult // 赋值
                };
            });

            return Ok(versionDtos);
        }

        [HttpPost]
        public async Task<ActionResult<VersionDto>> CreateVersion(Guid documentId, [FromForm] CreateVersionDto createVersionDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();


            var document = await _documentRepository.GetDocumentWithVersionsByIdAsync(documentId, userId);
            if (document == null)
            {
                return NotFound("Document not found.");
            }

            // 1. 创建新的 Version 实体
            var newVersion = new Core.Entities.Version
            {
                DocumentId = documentId,
                VersionNumber = document.Versions.Any() ? document.Versions.Max(v => v.VersionNumber) + 1 : 1,
                Message = createVersionDto.Message
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 2. 在 Repository 中创建版本并保存文件
                var createdVersion = await _documentRepository.CreateNewVersionAsync(document, newVersion, createVersionDto.File);

                // 3. 将新创建的版本设置为当前版本
                await _documentRepository.SetCurrentVersionAsync(documentId, createdVersion.VersionId);

                await transaction.CommitAsync();

                // 4. 映射到 DTO 并返回
                var versionDto = new VersionDto
                {
                    VersionId = createdVersion.VersionId,
                    VersionNumber = createdVersion.VersionNumber,
                    Message = createdVersion.Message,
                    CreatedAt = createdVersion.CreatedAt
                };

                return CreatedAtAction(nameof(GetVersions), new { documentId = documentId }, versionDto);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "An internal error occurred while creating the new version.");
            }

        }
    }
}