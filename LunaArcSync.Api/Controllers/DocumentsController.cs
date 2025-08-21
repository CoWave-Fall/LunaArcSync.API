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
using LunaArcSync.Api.Core.Entities; // Assuming AppUser is in this namespace
using LunaArcSync.Api.Core.Constants; // Assuming UserRoles is in this namespace
using LunaArcSync.Api.Core.Models; // Assuming PagedResultDto is in this namespace

namespace LunaArcSync.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        // 我们需要一个新的仓储接口 IDocumentRepository，您需要在后端自行创建它和它的实现
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<DocumentsController> _logger;
        private readonly UserManager<AppUser> _userManager; // Declare UserManager

        public DocumentsController(
            IDocumentRepository documentRepository,
            ILogger<DocumentsController> logger,
            UserManager<AppUser> userManager) 
        {
            _documentRepository = documentRepository;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultDto<DocumentDto>>> GetDocuments([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var currentUser = await _userManager.FindByIdAsync(userId);
            var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, UserRoles.Admin);

            PagedResult<Core.Entities.Document> pagedDocs;

            if (isAdmin)
            {
                // Admin can see all documents
                pagedDocs = await _documentRepository.GetAllDocumentsForAdminAsync(pageNumber, pageSize);
            }
            else
            {
                // Regular user can only see their own documents
                pagedDocs = await _documentRepository.GetAllDocumentsAsync(userId, pageNumber, pageSize);
            }

            var docDtos = pagedDocs.Items.Select(d => new DocumentDto
            {
                DocumentId = d.DocumentId,
                Title = d.Title,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                PageCount = d.Pages.Count, // 从实体中获取页面数量
                OwnerEmail = isAdmin ? d.User?.Email : null, // Populate OwnerEmail for admin
                Tags = d.Tags.Select(t => t.Name).ToList() // ADDED: Map Tags
            }).ToList();

            return Ok(new PagedResultDto<DocumentDto>(docDtos, pagedDocs.TotalCount, pageNumber, pageSize));
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
                // Admin can see any document
                document = await _documentRepository.GetDocumentWithPagesByIdForAdminAsync(id);
            }
            else
            {
                // Regular user can only see their own document
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
                    .OrderBy(p => p.Order) // Sort by Order ascending
                    .ThenByDescending(p => p.CreatedAt) // Then by CreatedAt descending
                    .Select(p => new PageDto
                    {
                        PageId = p.PageId,
                        Title = p.Title,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        Order = p.Order // Map the Order property
                    }).ToList(),
                OwnerEmail = isAdmin ? document.User?.Email : null // Populate OwnerEmail for admin
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

            // 通常 PUT 成功后返回 204 No Content
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
                // 返回一个通用的 Bad Request，因为失败的原因可能有很多种（文档找不到、页面找不到、页面已被关联等）
                // 详细原因已在后端日志中记录
                return BadRequest("Failed to add page to document. It may not exist, may not belong to you, or may already be in another document.");
            }

            // 成功后返回 No Content
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

            // Serialize to JSON
            var jsonString = System.Text.Json.JsonSerializer.Serialize(userDocuments, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var fileName = $"user_data_{userId}.json";

            return File(System.Text.Encoding.UTF8.GetBytes(jsonString), "application/json", fileName);
        }
    }
}