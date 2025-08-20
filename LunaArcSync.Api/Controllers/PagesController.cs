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
                UpdatedAt = d.UpdatedAt,
                Order = d.Order // Map the Order property
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
                UpdatedAt = d.UpdatedAt,
                Order = d.Order // Map the Order property
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

        #region Page Ordering

        [HttpPost("/api/documents/{documentId}/pages/reorder/set")]
        public async Task<IActionResult> SetPageOrder(Guid documentId, [FromBody] PageReorderSetDto reorderDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Validation: "同page异号" (Same page, different order number)
            var duplicatePageIds = reorderDto.PageOrders
                .GroupBy(p => p.PageId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicatePageIds.Any())
            {
                return BadRequest($"Duplicate PageIds found in the request: {string.Join(", ", duplicatePageIds)}");
            }

            // Validation: "异page同号" (Different page, same order number)
            var duplicateOrders = reorderDto.PageOrders
                .GroupBy(p => p.Order)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateOrders.Any())
            {
                return BadRequest($"Duplicate Order numbers found in the request: {string.Join(", ", duplicateOrders)}");
            }

            // Convert list to dictionary for easier lookup in repository
            var pageOrdersDict = reorderDto.PageOrders.ToDictionary(p => p.PageId, p => p.Order);

            var success = await _pageRepository.UpdatePageOrdersAsync(documentId, userId, pageOrdersDict);

            if (!success)
            {
                return BadRequest("Failed to update page orders. Ensure all pages belong to the specified document and user.");
            }

            return NoContent();
        }

        [HttpPost("/api/documents/{documentId}/pages/reorder/insert")]
        public async Task<IActionResult> InsertPageOrder(Guid documentId, [FromBody] PageReorderInsertDto insertDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Get all pages for the document, ordered by current order
            var documentPages = await _pageRepository.GetPagesByDocumentIdAsync(documentId, userId); // Need to add this method to IPageRepository

            if (documentPages == null || !documentPages.Any())
            {
                return NotFound("Document not found or has no pages.");
            }

            var targetPage = documentPages.FirstOrDefault(p => p.PageId == insertDto.PageId);
            if (targetPage == null)
            {
                return NotFound($"Page with ID {insertDto.PageId} not found in document {documentId}.");
            }

            // Ensure NewOrder is within valid range (1 to maxOrder + 1)
            var maxOrder = documentPages.Max(p => p.Order);
            if (insertDto.NewOrder < 1 || insertDto.NewOrder > maxOrder + 1)
            {
                return BadRequest($"NewOrder must be between 1 and {maxOrder + 1}.");
            }

            // Create a dictionary to hold the new orders
            var newPageOrders = new Dictionary<Guid, int>();
            var currentOrder = 1;

            // Iterate through existing pages and assign new orders
            foreach (var page in documentPages.OrderBy(p => p.Order))
            {
                if (currentOrder == insertDto.NewOrder)
                {
                    // Insert the target page at the desired position
                    newPageOrders[targetPage.PageId] = currentOrder;
                    currentOrder++;
                }

                if (page.PageId != targetPage.PageId) // Skip the target page if it's already handled
                {
                    newPageOrders[page.PageId] = currentOrder;
                    currentOrder++;
                }
            }

            // If the target page was inserted at the very end
            if (!newPageOrders.ContainsKey(targetPage.PageId))
            {
                newPageOrders[targetPage.PageId] = currentOrder;
            }


            var success = await _pageRepository.UpdatePageOrdersAsync(documentId, userId, newPageOrders);

            if (!success)
            {
                return BadRequest("Failed to update page orders during insertion.");
            }

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


        [HttpGet("unassigned")]
        public async Task<ActionResult<List<PageDto>>> GetUnassignedPages()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // 我们需要在 IPageRepository 中添加一个新方法
            var unassignedPages = await _pageRepository.GetUnassignedPagesAsync(userId);

            var pageDtos = unassignedPages.Select(p => new PageDto
            {
                PageId = p.PageId,
                Title = p.Title,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();

            return Ok(pageDtos);
        }

    }

}

    