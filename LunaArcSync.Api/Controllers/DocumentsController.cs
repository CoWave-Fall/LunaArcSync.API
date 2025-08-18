using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.DTOs;
using LunaArcSync.Api.DTOs.Ocr;
using System;
using System.Linq;
using System.Security.Claims; // 引入 Claims
using System.Text.Json;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Controllers
{
    [Authorize] // 确保所有接口都需要认证
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            IDocumentRepository documentRepository,
            ILogger<DocumentsController> logger)
        {
            _documentRepository = documentRepository;
            _logger = logger;
        }

        #region Core Document CRUD

        [HttpGet]
        public async Task<ActionResult<PagedResultDto<DocumentDto>>> GetDocuments([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize > 100) pageSize = 100;

            var pagedDocuments = await _documentRepository.GetAllDocumentsAsync(userId, pageNumber, pageSize);
            var documentDtos = pagedDocuments.Items.Select(d => new DocumentDto
            {
                DocumentId = d.DocumentId,
                Title = d.Title,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList();

            return Ok(new PagedResultDto<DocumentDto>(documentDtos, pagedDocuments.TotalCount, pagedDocuments.PageNumber, pagedDocuments.PageSize));
        }

        [HttpGet("{id}", Name = "GetDocumentById")]
        public async Task<ActionResult<DocumentDetailDto>> GetDocumentById(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var document = await _documentRepository.GetDocumentWithVersionsByIdAsync(id, userId);
            if (document == null) return NotFound();

            var currentVersionEntity = document.Versions.FirstOrDefault(v => v.VersionId == document.CurrentVersionId);
            var documentDetailDto = new DocumentDetailDto
            {
                DocumentId = document.DocumentId,
                Title = document.Title,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt,
                TotalVersions = document.Versions.Count,
                CurrentVersion = currentVersionEntity == null ? null : new VersionDto
                {
                    VersionId = currentVersionEntity.VersionId,
                    VersionNumber = currentVersionEntity.VersionNumber,
                    Message = currentVersionEntity.Message,
                    CreatedAt = currentVersionEntity.CreatedAt,
                    OcrResult = DeserializeOcrData(currentVersionEntity.OcrData, currentVersionEntity.VersionId)
                }
            };
            return Ok(documentDetailDto);
        }

        [HttpPost]
        public async Task<ActionResult<DocumentDto>> CreateDocument([FromForm] CreateDocumentDto createDocumentDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var newDocument = new Core.Entities.Document
            {
                Title = createDocumentDto.Title,
                UserId = userId // 关联当前用户
            };

            var createdDocument = await _documentRepository.CreateDocumentAsync(newDocument, createDocumentDto.File);
            var documentDto = new DocumentDto // 返回更简洁的 DTO
            {
                DocumentId = createdDocument.DocumentId,
                Title = createdDocument.Title,
                CreatedAt = createdDocument.CreatedAt,
                UpdatedAt = createdDocument.UpdatedAt
            };

            return CreatedAtAction(nameof(GetDocumentById), new { id = documentDto.DocumentId }, documentDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentDto updateDocumentDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updatedDocument = await _documentRepository.UpdateDocumentAsync(id, updateDocumentDto.Title, userId);
            if (updatedDocument == null) return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var success = await _documentRepository.DeleteDocumentAsync(id, userId);
            if (!success) return NotFound();

            return NoContent();
        }

        #endregion

        #region Search & Versioning

        [HttpGet("search")]
        public async Task<ActionResult<PagedResultDto<DocumentDto>>> Search([FromQuery] string q, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            if (string.IsNullOrWhiteSpace(q)) return BadRequest("Search query cannot be empty.");

            var pagedDocuments = await _documentRepository.SearchDocumentsAsync(q, userId, pageNumber, pageSize);
            var documentDtos = pagedDocuments.Items.Select(d => new DocumentDto
            {
                DocumentId = d.DocumentId,
                Title = d.Title,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList();

            return Ok(new PagedResultDto<DocumentDto>(documentDtos, pagedDocuments.TotalCount, pagedDocuments.PageNumber, pagedDocuments.PageSize));
        }

        [HttpPost("{id}/revert")]
        public async Task<IActionResult> RevertDocument(Guid id, [FromBody] RevertDocumentDto revertDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 验证文档属于当前用户
            var document = await _documentRepository.GetDocumentByIdAsync(id, userId);
            if (document == null) return NotFound();

            // 验证目标版本存在且属于该文档 (Repository 中没有这个方法，所以在 Controller 中验证)
            var versionExists = await _documentRepository.VersionExistsAsync(revertDto.TargetVersionId);
            if (!versionExists) return BadRequest("Target version not found.");

            var success = await _documentRepository.SetCurrentVersionAsync(id, revertDto.TargetVersionId);
            if (!success) return StatusCode(500, "An unexpected error occurred.");

            return NoContent();
        }

        #endregion

        #region Helper Methods

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private OcrResultDto? DeserializeOcrData(string? ocrJson, Guid versionId)
        {
            if (string.IsNullOrEmpty(ocrJson)) return null;
            try
            {
                return JsonSerializer.Deserialize<OcrResultDto>(ocrJson);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize OcrData for version {VersionId}", versionId);
                return null;
            }
        }

        #endregion
    }
}