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
    public class PagesController : ControllerBase
    {
        private readonly IPageRepository _pageRepository;
        private readonly ILogger<PagesController> _logger;

        public PagesController(
            IPageRepository pageRepository,
            ILogger<PagesController> logger)
        {
            _pageRepository = pageRepository;
            _logger = logger;
        }

        #region Core Page CRUD

        [HttpGet]
        public async Task<ActionResult<PagedResultDto<PageDto>>> GetPages([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize > 100) pageSize = 100;

            var pagedPages = await _pageRepository.GetAllPagesAsync(userId, pageNumber, pageSize);
            var pageDtos = pagedPages.Items.Select(d => new PageDto
            {
                PageId = d.PageId,
                Title = d.Title,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList();

            return Ok(new PagedResultDto<PageDto>(pageDtos, pagedPages.TotalCount, pagedPages.PageNumber, pagedPages.PageSize));
        }

        [HttpGet("{id}", Name = "GetPageById")]
        public async Task<ActionResult<PageDetailDto>> GetPageById(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var page = await _pageRepository.GetPageWithVersionsByIdAsync(id, userId);
            if (page == null) return NotFound();

            var currentVersionEntity = page.Versions.FirstOrDefault(v => v.VersionId == page.CurrentVersionId);
            var pageDetailDto = new PageDetailDto
            {
                PageId = page.PageId,
                Title = page.Title,
                CreatedAt = page.CreatedAt,
                UpdatedAt = page.UpdatedAt,
                TotalVersions = page.Versions.Count,
                CurrentVersion = currentVersionEntity == null ? null : new VersionDto
                {
                    VersionId = currentVersionEntity.VersionId,
                    VersionNumber = currentVersionEntity.VersionNumber,
                    Message = currentVersionEntity.Message,
                    CreatedAt = currentVersionEntity.CreatedAt,
                    OcrResult = DeserializeOcrData(currentVersionEntity.OcrData, currentVersionEntity.VersionId)
                }
            };
            return Ok(pageDetailDto);
        }

        [HttpPost]
        public async Task<ActionResult<PageDto>> CreatePage([FromForm] CreatePageDto createPageDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var newPage = new Core.Entities.Page
            {
                Title = createPageDto.Title,
                UserId = userId // 关联当前用户
            };

            var createdPage = await _pageRepository.CreatePageAsync(newPage, createPageDto.File);
            var pageDto = new PageDto // 返回更简洁的 DTO
            {
                PageId = createdPage.PageId,
                Title = createdPage.Title,
                CreatedAt = createdPage.CreatedAt,
                UpdatedAt = createdPage.UpdatedAt
            };

            return CreatedAtAction(nameof(GetPageById), new { id = pageDto.PageId }, pageDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePage(Guid id, [FromBody] UpdatePageDto updatePageDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updatedPage = await _pageRepository.UpdatePageAsync(id, updatePageDto.Title, userId);
            if (updatedPage == null) return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePage(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var success = await _pageRepository.DeletePageAsync(id, userId);
            if (!success) return NotFound();

            return NoContent();
        }

        #endregion

        #region Search & Versioning

        [HttpGet("search")]
        public async Task<ActionResult<PagedResultDto<PageDto>>> Search([FromQuery] string q, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            if (string.IsNullOrWhiteSpace(q)) return BadRequest("Search query cannot be empty.");

            var pagedPages = await _pageRepository.SearchPagesAsync(q, userId, pageNumber, pageSize);
            var pageDtos = pagedPages.Items.Select(d => new PageDto
            {
                PageId = d.PageId,
                Title = d.Title,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList();

            return Ok(new PagedResultDto<PageDto>(pageDtos, pagedPages.TotalCount, pagedPages.PageNumber, pagedPages.PageSize));
        }

        [HttpPost("{id}/revert")]
        public async Task<IActionResult> RevertPage(Guid id, [FromBody] RevertPageDto revertDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 验证文档属于当前用户
            var page = await _pageRepository.GetPageByIdAsync(id, userId);
            if (page == null) return NotFound();

            // 验证目标版本存在且属于该文档 (Repository 中没有这个方法，所以在 Controller 中验证)
            var versionExists = await _pageRepository.VersionExistsAsync(revertDto.TargetVersionId);
            if (!versionExists) return BadRequest("Target version not found.");

            var success = await _pageRepository.SetCurrentVersionAsync(id, revertDto.TargetVersionId);
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