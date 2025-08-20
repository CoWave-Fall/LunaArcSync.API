using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.DTOs;
using System;
using System.Collections.Generic; // Added for Dictionary
using System.Linq;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Infrastructure.Data
{
    public class PageRepository : IPageRepository
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<PageRepository> _logger;

        public PageRepository(AppDbContext context, IFileStorageService fileStorageService, ILogger<PageRepository> logger)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        #region Page Management (Multi-User & Paginated)

        public async Task<PagedResultDto<Page>> GetAllPagesAsync(string userId, int pageNumber, int pageSize)
        {
            var query = _context.Pages.Where(d => d.UserId == userId);
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedResultDto<Page>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<Page?> GetPageWithVersionsByIdAsync(Guid id, string userId)
        {
            return await _context.Pages
                .Where(d => d.PageId == id && d.UserId == userId)
                .Include(d => d.Versions.OrderByDescending(v => v.VersionNumber))
                .FirstOrDefaultAsync();
        }

        public async Task<Page?> GetPageByIdAsync(Guid id, string userId)
        {
            return await _context.Pages
                .FirstOrDefaultAsync(d => d.PageId == id && d.UserId == userId);
        }

        public async Task<Page> CreatePageAsync(Page newPage, IFormFile file)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var initialVersion = new Core.Entities.Version
                {
                    VersionNumber = 1,
                    Message = "Initial upload",
                    ImagePath = string.Empty
                };
                newPage.Versions.Add(initialVersion);
                await _context.Pages.AddAsync(newPage);
                await _context.SaveChangesAsync();

                var finalFileName = await _fileStorageService.SaveFileAsync(file, newPage, initialVersion);
                initialVersion.ImagePath = finalFileName;
                newPage.CurrentVersionId = initialVersion.VersionId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return newPage;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create page.");
                throw;
            }
        }

        public async Task<Page?> UpdatePageAsync(Guid id, string newTitle, string userId)
        {
            var page = await _context.Pages
                .FirstOrDefaultAsync(d => d.PageId == id && d.UserId == userId);
            if (page == null) return null;
            page.Title = newTitle;
            page.UpdatedAt = DateTime.UtcNow;
            _context.Pages.Update(page);
            await _context.SaveChangesAsync();
            return page;
        }

        public async Task<bool> DeletePageAsync(Guid id, string userId)
        {
            var pageToDelete = await _context.Pages
                .Where(d => d.PageId == id && d.UserId == userId)
                .Include(d => d.Versions)
                .FirstOrDefaultAsync();
            if (pageToDelete == null) return false;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var version in pageToDelete.Versions)
                {
                    _fileStorageService.DeleteFile(version.ImagePath);
                }
                _context.Pages.Remove(pageToDelete);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "An error occurred while deleting page {PageId}", id);
                return false;
            }
        }

        #endregion

        #region Search

        public async Task<PagedResultDto<Page>> SearchPagesAsync(string query, string userId, int pageNumber, int pageSize)
        {
            var normalizedQuery = new string(query.Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (string.IsNullOrEmpty(normalizedQuery))
            {
                return new PagedResultDto<Page>(new List<Page>(), 0, pageNumber, pageSize);
            }
            var searchQuery = _context.Versions
                .Where(v => v.Page!.UserId == userId &&
                            v.OcrDataNormalized != null &&
                            v.OcrDataNormalized.Contains(normalizedQuery))
                .Select(v => v.Page!)
                .Distinct();
            var totalCount = await searchQuery.CountAsync();
            var items = await searchQuery
                .OrderByDescending(d => d.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedResultDto<Page>(items, totalCount, pageNumber, pageSize);
        }

        #endregion

        #region Version Management

        public async Task<Core.Entities.Version> CreateNewVersionAsync(Page page, Core.Entities.Version newVersion, IFormFile file)
        {
            var finalFileName = await _fileStorageService.SaveFileAsync(file, page, newVersion);
            newVersion.ImagePath = finalFileName;
            _context.Versions.Add(newVersion);
            await _context.SaveChangesAsync();
            return newVersion;
        }

        public async Task<bool> SetCurrentVersionAsync(Guid pageId, Guid versionId)
        {
            var page = await _context.Pages.FindAsync(pageId);
            if (page == null) return false;
            page.CurrentVersionId = versionId;
            page.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> VersionExistsAsync(Guid versionId)
        {
            return await _context.Versions.AnyAsync(v => v.VersionId == versionId);
        }

        // +++ 这是我们添加并修正的方法 +++
        public async Task<Core.Entities.Version?> GetVersionByIdAsync(Guid versionId)
        {
            // 使用正确的 DbSet 名称 'Versions'
            return await _context.Versions
                .FirstOrDefaultAsync(v => v.VersionId == versionId);
        }

        #endregion


        public async Task<List<Page>> GetUnassignedPagesAsync(string userId)
        {
            return await _context.Pages
                .Where(p => p.UserId == userId && p.DocumentId == null)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdatePageOrdersAsync(Guid documentId, string userId, Dictionary<Guid, int> pageOrders)
        {
            // 1. Verify the document belongs to the user
            var document = await _context.Documents
                                        .Where(d => d.DocumentId == documentId && d.UserId == userId)
                                        .FirstOrDefaultAsync();
            if (document == null)
            {
                _logger.LogWarning("UpdatePageOrders: Document {DocumentId} not found or does not belong to user {UserId}.", documentId, userId);
                return false;
            }

            // 2. Get all pages for this document that belong to the user
            //    and whose IDs are in the provided pageOrders dictionary.
            var pagesToUpdate = await _context.Pages
                                            .Where(p => p.DocumentId == documentId && p.UserId == userId && pageOrders.Keys.Contains(p.PageId))
                                            .ToListAsync();

            // 3. Check if all provided pageIds actually exist and belong to this document/user
            if (pagesToUpdate.Count != pageOrders.Count)
            {
                _logger.LogWarning("UpdatePageOrders: Mismatch between provided page IDs and actual pages found for Document {DocumentId}.", documentId);
                return false; // Some pageIds were invalid or didn't belong to the document/user
            }

            // 4. Update the order for each page
            foreach (var page in pagesToUpdate)
            {
                page.Order = pageOrders[page.PageId];
                page.UpdatedAt = DateTime.UtcNow; // Update timestamp
            }

            // 5. Save changes
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated page orders for Document {DocumentId}.", documentId);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "UpdatePageOrders: Concurrency error when updating page orders for Document {DocumentId}.", documentId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdatePageOrders: An error occurred while updating page orders for Document {DocumentId}.", documentId);
                return false;
            }
        }

        public async Task<List<Page>> GetPagesByDocumentIdAsync(Guid documentId, string userId)
        {
            return await _context.Pages
                                .Where(p => p.DocumentId == documentId && p.UserId == userId)
                                .OrderBy(p => p.Order) // Order by the new Order property
                                .ThenByDescending(p => p.CreatedAt) // Then by CreatedAt for tie-breaking
                                .ToListAsync();
        }

    }
}