using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.DTOs;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.Core.Constants;
using LunaArcSync.Api.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using LunaArcSync.Api.BackgroundTasks;
using System.Collections.Generic;

namespace LunaArcSync.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<DocumentsController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMemoryCache _cache;

        public DocumentsController(
            IDocumentRepository documentRepository,
            ILogger<DocumentsController> logger,
            UserManager<AppUser> userManager,
            IMemoryCache cache)
        {
            _documentRepository = documentRepository;
            _logger = logger;
            _userManager = userManager;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultDto<DocumentDto>>> GetDocuments(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "date_desc", 
            [FromQuery] string? tags = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var tagList = string.IsNullOrEmpty(tags) 
                ? new List<string>() 
                : tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            var currentUser = await _userManager.FindByIdAsync(userId);
            var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, UserRoles.Admin);

            PagedResult<Core.Entities.Document> pagedDocs;

            if (isAdmin)
            {
                pagedDocs = await _documentRepository.GetAllDocumentsForAdminAsync(pageNumber, pageSize, sortBy, tagList);
            }
            else
            {
                pagedDocs = await _documentRepository.GetAllDocumentsAsync(userId, pageNumber, pageSize, sortBy, tagList);
            }

            var docDtos = pagedDocs.Items.Select(d => new DocumentDto
            {
                DocumentId = d.DocumentId,
                Title = d.Title,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                PageCount = d.Pages.Count,
                OwnerEmail = isAdmin ? d.User?.Email : null,
                Tags = d.Tags.Select(t => t.Name).ToList()
            }).ToList();

            return Ok(new PagedResultDto<DocumentDto>(docDtos, pagedDocs.TotalCount, pageNumber, pageSize));
        }

        [HttpGet("tags")]
        public async Task<ActionResult<List<string>>> GetAllTags()
        {
            var cacheKey = CacheWarmingService.GetTagsCacheKey();
            if (!_cache.TryGetValue(cacheKey, out List<string>? tags) || tags == null)
            {
                _logger.LogInformation("Tags cache miss. Fetching from repository.");
                tags = await _documentRepository.GetAllTagsAsync();
                _cache.Set(cacheKey, tags, TimeSpan.FromDays(1));
            }
            else
            {
                _logger.LogInformation("Tags cache hit.");
            }
            return Ok(tags);
        }

        [HttpGet("stats")]
        public async Task<ActionResult<UserStatsDto>> GetUserStats()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var stats = await _documentRepository.GetUserStatsAsync(userId);

            return Ok(stats);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentDetailDto>> GetDocumentById(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var currentUser = await _userManager.FindByIdAsync(userId);
            var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, UserRoles.Admin);

            Core.Entities.Document? document;

            if (isAdmin)
            {
                document = await _documentRepository.GetDocumentWithPagesByIdForAdminAsync(id);
            }
            else
            {
                document = await _documentRepository.GetDocumentWithPagesByIdAsync(id, userId);
            }

            if (document == null) return NotFound();

            var documentDetailDto = new DocumentDetailDto
            {
                DocumentId = document.DocumentId,
                Title = document.Title,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt,
                Tags = document.Tags.Select(t => t.Name).ToList(),
                Pages = document.Pages
                    .OrderBy(p => p.Order)
                    .ThenByDescending(p => p.CreatedAt)
                    .Select(p => new PageDto
                    {
                        PageId = p.PageId,
                        Title = p.Title,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        Order = p.Order
                    }).ToList(),
                OwnerEmail = isAdmin ? document.User?.Email : null
            };

            return Ok(documentDetailDto);
        }

        [HttpPost]
        public async Task<ActionResult<DocumentDto>> CreateDocument([FromBody] CreateDocumentDto createDocumentDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var newDocument = new Core.Entities.Document
            {
                Title = createDocumentDto.Title,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdDocument = await _documentRepository.CreateDocumentAsync(newDocument);

            _cache.Remove(CacheWarmingService.GetTagsCacheKey());

            var documentDto = new DocumentDto
            {
                DocumentId = createdDocument.DocumentId,
                Title = createdDocument.Title,
                CreatedAt = createdDocument.CreatedAt,
                UpdatedAt = createdDocument.UpdatedAt,
                PageCount = 0
            };

            return CreatedAtAction(nameof(GetDocumentById), new { id = documentDto.DocumentId }, documentDto);
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentDto updateDocumentDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var updatedDocument = await _documentRepository.UpdateDocumentAsync(id, updateDocumentDto.Title, updateDocumentDto.Tags, userId);

            if (updatedDocument == null)
            {
                return NotFound();
            }

            _cache.Remove(CacheWarmingService.GetTagsCacheKey());

            return NoContent();
        }

        [HttpPost("{documentId}/pages")]
        public async Task<IActionResult> AddPageToDocument(Guid documentId, [FromBody] AddPageToDocumentDto addPageDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var success = await _documentRepository.AddPageToDocumentAsync(documentId, addPageDto.PageId, userId);

            if (!success)
            {
                return BadRequest("Failed to add page to document. It may not exist, may not belong to you, or may already be in another document.");
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var success = await _documentRepository.DeleteDocumentAsync(id, userId);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpGet("my-data-export")]
        public async Task<IActionResult> ExportMyData()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userDocuments = await _documentRepository.GetAllUserDocumentsWithDetailsAsync(userId);

            var jsonString = System.Text.Json.JsonSerializer.Serialize(userDocuments, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var fileName = $"user_data_{userId}.json";

            return File(System.Text.Encoding.UTF8.GetBytes(jsonString), "application/json", fileName);
        }
    }
}
