using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.Core.Models;
using LunaArcSync.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Infrastructure.Data
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DocumentRepository> _logger;

        public DocumentRepository(AppDbContext context, ILogger<DocumentRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<Document>> GetAllDocumentsAsync(string userId, int pageNumber, int pageSize, string sortBy, List<string> tags)
        {
            var query = _context.Documents
                .AsNoTracking()
                .Where(d => d.UserId == userId);

            query = ApplyFiltering(query, tags);
            query = ApplySorting(query, sortBy);

            query = query.Include(d => d.Pages).Include(d => d.Tags);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Document>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PagedResult<Document>> GetAllDocumentsForAdminAsync(int pageNumber, int pageSize, string sortBy, List<string> tags)
        {
            var query = _context.Documents.AsNoTracking();

            query = ApplyFiltering(query, tags);
            query = ApplySorting(query, sortBy);

            query = query.Include(d => d.Pages).Include(d => d.User).Include(d => d.Tags);

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
                .Include(d => d.Tags)
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

        public async Task<bool> DeleteDocumentAsync(Guid documentId, string userId)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.UserId == userId);

            if (document == null)
            {
                return false;
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Document?> UpdateDocumentAsync(Guid documentId, string title, List<string>? tags, string userId)
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
                var desiredTagNames = tags.Where(t => !string.IsNullOrWhiteSpace(t))
                                          .Select(t => t.Trim())
                                          .Distinct()
                                          .ToList();

                var existingTags = await _context.Tags
                    .Where(t => desiredTagNames.Contains(t.Name))
                    .ToListAsync();

                var existingTagNames = existingTags.Select(t => t.Name).ToHashSet();
                var newTagNames = desiredTagNames.Where(name => !existingTagNames.Contains(name));

                var newTags = newTagNames.Select(name => new Tag { Name = name }).ToList();

                var finalTags = existingTags.Concat(newTags).ToList();

                document.Tags = finalTags;
            }

            await _context.SaveChangesAsync();

            return document;
        }

        public async Task<bool> AddPageToDocumentAsync(Guid documentId, Guid pageId, string userId)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.UserId == userId);

            if (document == null)
            {
                _logger.LogWarning("AddPageToDocument: Document with ID {DocumentId} not found for user {UserId}.", documentId, userId);
                return false;
            }

            var page = await _context.Pages
                .FirstOrDefaultAsync(p => p.PageId == pageId && p.UserId == userId);

            if (page == null)
            {
                _logger.LogWarning("AddPageToDocument: Page with ID {PageId} not found for user {UserId}.", pageId, userId);
                return false;
            }

            if (page.DocumentId != null)
            {
                _logger.LogWarning("AddPageToDocument: Page {PageId} is already associated with Document {ExistingDocumentId}.", pageId, page.DocumentId);
                return false;
            }

            page.DocumentId = document.DocumentId;
            document.UpdatedAt = DateTime.UtcNow;

            var maxOrder = await _context.Pages
                                        .Where(p => p.DocumentId == document.DocumentId)
                                        .MaxAsync(p => (int?)p.Order) ?? 0;

            page.Order = maxOrder + 1;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully added Page {PageId} to Document {DocumentId} for user {UserId}.", pageId, documentId, userId);
            return true;
        }

        public async Task<UserStatsDto> GetUserStatsAsync(string userId)
        {
            var totalDocuments = await _context.Documents
                                            .Where(d => d.UserId == userId)
                                            .CountAsync();

            var totalPages = await _context.Pages
                                        .Where(p => p.UserId == userId)
                                        .CountAsync();

            return new UserStatsDto
            {
                TotalDocuments = totalDocuments,
                TotalPages = totalPages
            };
        }

        public async Task<Document?> GetDocumentWithPagesByIdForAdminAsync(Guid documentId)
        {
            return await _context.Documents
                .Where(d => d.DocumentId == documentId)
                .Include(d => d.Tags)
                .Include(d => d.Pages.OrderBy(p => p.Order).ThenByDescending(p => p.CreatedAt))
                .Include(d => d.User)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Document>> GetAllUserDocumentsWithDetailsAsync(string userId)
        {
            return await _context.Documents
                                .Where(d => d.UserId == userId)
                                .Include(d => d.Pages)
                                .Include(d => d.Tags)
                                .Include(d => d.User)
                                .OrderByDescending(d => d.UpdatedAt)
                                .ToListAsync();
        }

        public async Task<List<string>> GetAllTagsAsync()
        {
            return await _context.Tags
                .AsNoTracking()
                .Select(t => t.Name)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }

        public async Task<List<SearchResultDto>> SearchDocumentsAsync(string query, string userId, bool isAdmin)
        {
            var normalizedQuery = query.Trim().ToLower();
            if (string.IsNullOrEmpty(normalizedQuery))
            {
                return new List<SearchResultDto>();
            }

            IQueryable<Document> documentsQuery = _context.Documents
                .Include(d => d.User)
                .Include(d => d.Pages);

            if (!isAdmin)
            {
                documentsQuery = documentsQuery.Where(d => d.UserId == userId);
            }

            var results = await documentsQuery
                .Where(d => d.Title.ToLower().Contains(normalizedQuery))
                .Select(d => new SearchResultDto
                {
                    Type = "document",
                    DocumentId = d.DocumentId,
                    PageId = null,
                    Title = d.Title,
                    MatchSnippet = d.Title // For document title match, snippet is the title itself
                })
                .ToListAsync();

            return results;
        }

        private IQueryable<Document> ApplySorting(IQueryable<Document> query, string sortBy)
        {
            return sortBy?.ToLower() switch
            {
                "date_asc" => query.OrderBy(d => d.UpdatedAt),
                "title_asc" => query.OrderBy(d => d.Title),
                "title_desc" => query.OrderByDescending(d => d.Title),
                _ => query.OrderByDescending(d => d.UpdatedAt), // Default case
            };
        }

        private IQueryable<Document> ApplyFiltering(IQueryable<Document> query, List<string> tags)
        {
            if (tags != null && tags.Any())
            {
                foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    query = query.Where(d => d.Tags.Any(t => t.Name == tag));
                }
            }
            return query;
        }
    }
}