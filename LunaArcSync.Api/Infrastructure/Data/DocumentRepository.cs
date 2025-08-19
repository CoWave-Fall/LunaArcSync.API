using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Infrastructure.Data
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<DocumentRepository> _logger;

        public DocumentRepository(
            AppDbContext context,
            IFileStorageService fileStorageService,
            ILogger<DocumentRepository> logger)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        #region Document Management (Multi-User & Paginated)

        public async Task<PagedResultDto<Document>> GetAllDocumentsAsync(string userId, int pageNumber, int pageSize)
        {
            var query = _context.Documents.Where(d => d.UserId == userId);
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedResultDto<Document>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<Document?> GetDocumentWithVersionsByIdAsync(Guid id, string userId)
        {
            return await _context.Documents
                .Where(d => d.DocumentId == id && d.UserId == userId)
                .Include(d => d.Versions.OrderByDescending(v => v.VersionNumber))
                .FirstOrDefaultAsync();
        }

        public async Task<Document?> GetDocumentByIdAsync(Guid id, string userId)
        {
            return await _context.Documents
                .FirstOrDefaultAsync(d => d.DocumentId == id && d.UserId == userId);
        }

        public async Task<Document> CreateDocumentAsync(Document newDocument, IFormFile file)
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
                newDocument.Versions.Add(initialVersion);
                await _context.Documents.AddAsync(newDocument);
                await _context.SaveChangesAsync();

                var finalFileName = await _fileStorageService.SaveFileAsync(file, newDocument, initialVersion);
                initialVersion.ImagePath = finalFileName;
                newDocument.CurrentVersionId = initialVersion.VersionId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return newDocument;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create document.");
                throw;
            }
        }

        public async Task<Document?> UpdateDocumentAsync(Guid id, string newTitle, string userId)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.DocumentId == id && d.UserId == userId);
            if (document == null) return null;
            document.Title = newTitle;
            document.UpdatedAt = DateTime.UtcNow;
            _context.Documents.Update(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task<bool> DeleteDocumentAsync(Guid id, string userId)
        {
            var documentToDelete = await _context.Documents
                .Where(d => d.DocumentId == id && d.UserId == userId)
                .Include(d => d.Versions)
                .FirstOrDefaultAsync();
            if (documentToDelete == null) return false;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var version in documentToDelete.Versions)
                {
                    _fileStorageService.DeleteFile(version.ImagePath);
                }
                _context.Documents.Remove(documentToDelete);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "An error occurred while deleting document {DocumentId}", id);
                return false;
            }
        }

        #endregion

        #region Search

        public async Task<PagedResultDto<Document>> SearchDocumentsAsync(string query, string userId, int pageNumber, int pageSize)
        {
            var normalizedQuery = new string(query.Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (string.IsNullOrEmpty(normalizedQuery))
            {
                return new PagedResultDto<Document>(new List<Document>(), 0, pageNumber, pageSize);
            }
            var searchQuery = _context.Versions
                .Where(v => v.Document!.UserId == userId &&
                            v.OcrDataNormalized != null &&
                            v.OcrDataNormalized.Contains(normalizedQuery))
                .Select(v => v.Document!)
                .Distinct();
            var totalCount = await searchQuery.CountAsync();
            var items = await searchQuery
                .OrderByDescending(d => d.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedResultDto<Document>(items, totalCount, pageNumber, pageSize);
        }

        #endregion

        #region Version Management

        public async Task<Core.Entities.Version> CreateNewVersionAsync(Document document, Core.Entities.Version newVersion, IFormFile file)
        {
            var finalFileName = await _fileStorageService.SaveFileAsync(file, document, newVersion);
            newVersion.ImagePath = finalFileName;
            _context.Versions.Add(newVersion);
            await _context.SaveChangesAsync();
            return newVersion;
        }

        public async Task<bool> SetCurrentVersionAsync(Guid documentId, Guid versionId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null) return false;
            document.CurrentVersionId = versionId;
            document.UpdatedAt = DateTime.UtcNow;
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
    }
}