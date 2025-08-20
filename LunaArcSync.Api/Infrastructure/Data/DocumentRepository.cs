using LunaArcSync.Api.Controllers;
using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.Core.Models;
using LunaArcSync.Api.Infrastructure.Data; // 确保引用了 AppDbContext 的命名空间
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // Added for List<string> in UpdateDocumentAsync
using System.Linq;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Infrastructure.Data
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentRepository(AppDbContext context)
        {
            _context = context;
            // _logger = logger; // This was commented out, keep it that way or inject ILogger
        }

        public async Task<PagedResult<Document>> GetAllDocumentsAsync(string userId, int pageNumber, int pageSize)
        {
            var query = _context.Documents
                .Where(d => d.UserId == userId)
                .Include(d => d.Pages) // 包含关联的 Pages 以便计算 PageCount
                .OrderByDescending(d => d.UpdatedAt); // 按更新时间降序排序

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Document>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<Document?> GetDocumentWithPagesByIdAsync(Guid documentId, string userId)
        {
            return await _context.Documents
                .Where(d => d.DocumentId == documentId && d.UserId == userId)
                .Include(d => d.Tags) // **关键修复：添加这一行来加载关联的 Tags**
                .Include(d => d.Pages.OrderBy(p => p.Order).ThenByDescending(p => p.CreatedAt))
                .FirstOrDefaultAsync();
        }

        public async Task<Document> CreateDocumentAsync(Document document)
        {
            document.DocumentId = Guid.NewGuid();
            document.CreatedAt = DateTime.UtcNow;
            document.UpdatedAt = DateTime.UtcNow;

            await _context.Documents.AddAsync(document);
            await _context.SaveChangesAsync();
            return document;
        }

        // --- 以下是为未来准备的标准方法实现 ---

        public async Task<Document> UpdateDocumentAsync(Document document)
        {
            document.UpdatedAt = DateTime.UtcNow;
            _context.Documents.Update(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task<bool> DeleteDocumentAsync(Guid documentId, string userId)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.UserId == userId);

            if (document == null)
            {
                return false;
            }

            // 注意：删除文档时，需要考虑如何处理其包含的页面。
            // 默认情况下 (取决于您的 EF Core 配置), 可能会级联删除或将 Pages 的 DocumentId 设为 null。
            // 确保这个行为符合您的业务逻辑。
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Document> UpdateDocumentAsync(Guid documentId, string title, List<string>? tags, string userId)
        {
            var document = await _context.Documents
                .Include(d => d.Tags)
                .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.UserId == userId);

            if (document == null)
            {
                return null;
            }

            document.Title = title;
            document.UpdatedAt = DateTime.UtcNow;

            if (tags != null)
            {
                // --- START: 全新的、更健壮的标签更新逻辑 ---

                // 规范化传入的标签名称（去重、去空、去首尾空格）
                var desiredTagNames = tags.Where(t => !string.IsNullOrWhiteSpace(t))
                                          .Select(t => t.Trim())
                                          .Distinct()
                                          .ToList();

                // 1. 从数据库中一次性查询出所有已存在的标签
                var existingTags = await _context.Tags
                    .Where(t => desiredTagNames.Contains(t.Name))
                    .ToListAsync();

                // 2. 找出哪些标签是新的，需要被创建
                var existingTagNames = existingTags.Select(t => t.Name).ToHashSet();
                var newTagNames = desiredTagNames.Where(name => !existingTagNames.Contains(name));

                // 3. 为新标签创建实体对象
                var newTags = newTagNames.Select(name => new Tag { Name = name }).ToList();

                // 4. 将找到的旧标签和创建的新标签合并
                var finalTags = existingTags.Concat(newTags).ToList();

                // 5. 直接将文档的标签集合赋值为最终的集合
                //    这是最能清晰地向 EF Core 表达意图的方式
                document.Tags = finalTags;

                // --- END: 全新的、更健壮的标签更新逻辑 ---
            }

            await _context.SaveChangesAsync();

            return document;
        }

        public async Task<bool> AddPageToDocumentAsync(Guid documentId, Guid pageId, string userId)
        {
            // 1. 查找文档，并确保它属于当前用户
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.UserId == userId);

            if (document == null)

            {
                _logger.LogWarning("AddPageToDocument: Document with ID {DocumentId} not found for user {UserId}.", documentId, userId);
                return false; // 文档不存在或不属于该用户
            }

            // 2. 查找页面，并确保它也属于当前用户
            var page = await _context.Pages
                .FirstOrDefaultAsync(p => p.PageId == pageId && p.UserId == userId);

            if (page == null)
            {
                _logger.LogWarning("AddPageToDocument: Page with ID {PageId} not found for user {UserId}.", pageId, userId);
                return false; // 页面不存在或不属于该用户
            }

            // 3. 检查页面是否已经属于另一个文档
            if (page.DocumentId != null)
            {
                _logger.LogWarning("AddPageToDocument: Page {PageId} is already associated with Document {ExistingDocumentId}.", pageId, page.DocumentId);
                // 根据业务逻辑，您可以决定是返回错误，还是允许页面移动
                // 目前我们假设一个页面只能属于一个文档，如果它已经有归属，则操作失败
                return false;
            }

            // 4. 建立关联
            page.DocumentId = document.DocumentId;
            document.UpdatedAt = DateTime.UtcNow; // 更新文档的修改时间

            // Set the order for the new page
            // Find the maximum order for the document's existing pages
            var maxOrder = await _context.Pages
                                        .Where(p => p.DocumentId == document.DocumentId)
                                        .MaxAsync(p => (int?)p.Order) ?? 0; // Use (int?) to handle empty sequence, default to 0

            page.Order = maxOrder + 1; // Assign the next order number

            // 5. 保存更改
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully added Page {PageId} to Document {DocumentId} for user {UserId}.", pageId, documentId, userId);
            return true;
        }

        public async Task<LunaArcSync.Api.DTOs.UserStatsDto> GetUserStatsAsync(string userId)
        {
            var totalDocuments = await _context.Documents
                                            .Where(d => d.UserId == userId)
                                            .CountAsync();

            var totalPages = await _context.Pages
                                        .Where(p => p.UserId == userId)
                                        .CountAsync();

            return new LunaArcSync.Api.DTOs.UserStatsDto
            {
                TotalDocuments = totalDocuments,
                TotalPages = totalPages
            };
        }

        public async Task<PagedResult<Document>> GetAllDocumentsForAdminAsync(int pageNumber, int pageSize)
        {
            var query = _context.Documents
                .Include(d => d.Pages) // Include Pages for PageCount
                .Include(d => d.User) // Include User for OwnerEmail
                .OrderByDescending(d => d.UpdatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Document>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<Document?> GetDocumentWithPagesByIdForAdminAsync(Guid documentId)
        {
            return await _context.Documents
                .Where(d => d.DocumentId == documentId)
                .Include(d => d.Tags)
                .Include(d => d.Pages.OrderBy(p => p.Order).ThenByDescending(p => p.CreatedAt))
                .Include(d => d.User) // Include User for OwnerEmail
                .FirstOrDefaultAsync();
        }

    }
}