using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using LunaArcSync.Api.Core.Constants;

namespace LunaArcSync.Api.Infrastructure.Services
{
    public class DataExportImportService : IDataExportImportService
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<DataExportImportService> _logger;

        public DataExportImportService(
            AppDbContext context,
            IFileStorageService fileStorageService,
            UserManager<AppUser> userManager,
            ILogger<DataExportImportService> logger)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
            _logger = logger;
        }

        private class ExportDataDto
        {
            public string? ExportedByUserId { get; set; }
            public DateTime ExportedAt { get; set; }
            public List<ExportDocumentDto> Documents { get; set; } = new List<ExportDocumentDto>();
        }

        private class ExportDocumentDto
        {
            public Guid DocumentId { get; set; }
            public string Title { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public string UserId { get; set; } = string.Empty;
            public List<string> Tags { get; set; } = new List<string>(); // Assuming tags are just strings
            public List<ExportPageDto> Pages { get; set; } = new List<ExportPageDto>();
        }

        private class ExportPageDto
        {
            public Guid PageId { get; set; }
            public string Title { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public Guid CurrentVersionId { get; set; }
            public int Order { get; set; }
            public List<ExportVersionDto> Versions { get; set; } = new List<ExportVersionDto>();
        }

        private class ExportVersionDto
        {
            public Guid VersionId { get; set; }
            public int VersionNumber { get; set; }
            public string Message { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public string ImagePath { get; set; } = string.Empty; // Relative path within the ZIP
            public string? OcrData { get; set; } // OCR data might be large, consider if needed
        }

        public async Task<Stream> ExportDataAsync(string? userId, bool isAdminExport, string? targetUserId = null)
        {
            var exportData = new ExportDataDto
            {
                ExportedByUserId = userId,
                ExportedAt = DateTime.UtcNow
            };

            IQueryable<Document> documentsQuery;

            if (isAdminExport)
            {
                if (!string.IsNullOrEmpty(targetUserId))
                {
                    // Admin exporting a specific user's data
                    documentsQuery = _context.Documents
                        .Where(d => d.UserId == targetUserId)
                        .Include(d => d.Pages)
                            .ThenInclude(p => p.Versions)
                        .Include(d => d.Tags);
                }
                else
                {
                    // Admin exporting all data
                    documentsQuery = _context.Documents
                        .Include(d => d.Pages)
                            .ThenInclude(p => p.Versions)
                        .Include(d => d.Tags);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(userId))
                {
                    throw new ArgumentNullException(nameof(userId), "User ID must be provided for non-admin export.");
                }
                // Regular user exporting their own data
                documentsQuery = _context.Documents
                    .Where(d => d.UserId == userId)
                    .Include(d => d.Pages)
                        .ThenInclude(p => p.Versions)
                    .Include(d => d.Tags);
            }

            var documents = await documentsQuery.ToListAsync();

            foreach (var doc in documents)
            {
                var exportDoc = new ExportDocumentDto
                {
                    DocumentId = doc.DocumentId,
                    Title = doc.Title,
                    CreatedAt = doc.CreatedAt,
                    UpdatedAt = doc.UpdatedAt,
                    UserId = doc.UserId,
                    Tags = doc.Tags.Select(t => t.Name).ToList(),
                    Pages = doc.Pages.Select(p => new ExportPageDto
                    {
                        PageId = p.PageId,
                        Title = p.Title,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        CurrentVersionId = p.CurrentVersionId,
                        Order = p.Order,
                        Versions = p.Versions.Select(v => new ExportVersionDto
                        {
                            VersionId = v.VersionId,
                            VersionNumber = v.VersionNumber,
                            Message = v.Message,
                            CreatedAt = v.CreatedAt,
                            ImagePath = v.ImagePath, // Original path
                            OcrData = v.OcrData
                        }).ToList()
                    }).ToList()
                };
                exportData.Documents.Add(exportDoc);
            }

            // Create a temporary directory for the export files
            var tempDir = Path.Combine(Path.GetTempPath(), "LunaArcSyncExport_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var imagesDir = Path.Combine(tempDir, "images");
            Directory.CreateDirectory(imagesDir);

            // Write data.json
            var dataJsonPath = Path.Combine(tempDir, "data.json");
            await System.IO.File.WriteAllTextAsync(dataJsonPath, JsonConvert.SerializeObject(exportData, Formatting.Indented));

            // Copy image files
            foreach (var doc in exportData.Documents)
            {
                foreach (var page in doc.Pages)
                {
                    foreach (var version in page.Versions)
                    {
                        if (!string.IsNullOrEmpty(version.ImagePath))
                        {
                            var sourcePath = _fileStorageService.GetFilePath(version.ImagePath);
                            if (System.IO.File.Exists(sourcePath))
                            {
                                // Create subdirectories for images based on document/page/version IDs
                                var imageRelativePath = Path.Combine(doc.DocumentId.ToString(), page.PageId.ToString(), version.VersionId.ToString() + Path.GetExtension(version.ImagePath));
                                var destinationPath = Path.Combine(imagesDir, imageRelativePath);
                                var directoryName = Path.GetDirectoryName(destinationPath);
                                if (directoryName != null) {
                                    Directory.CreateDirectory(directoryName);
                                }
                                System.IO.File.Copy(sourcePath, destinationPath, true);

                                // Update ImagePath in DTO to be relative to the 'images' folder in the ZIP
                                version.ImagePath = Path.Combine("images", imageRelativePath);
                            }
                            else
                            {
                                _logger.LogWarning("Image file not found for version {VersionId}: {ImagePath}", version.VersionId, sourcePath);
                                version.ImagePath = string.Empty; // Clear path if file not found
                            }
                        }
                    }
                }
            }

            // Create ZIP archive
            var zipFilePath = Path.Combine(Path.GetTempPath(), "LunaArcSyncExport_" + Guid.NewGuid().ToString() + ".zip");
            ZipFile.CreateFromDirectory(tempDir, zipFilePath);

            // Clean up temporary directory
            Directory.Delete(tempDir, true);

            // Return the ZIP file stream
            var stream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
            return stream;
        }

        public async Task ImportDataAsync(Stream zipFileStream, string importingUserId, bool isAdminImport)
        {
            var tempImportDir = Path.Combine(Path.GetTempPath(), "LunaArcSyncImport_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempImportDir);

            try
            {
                // Extract the ZIP file
                using (var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(tempImportDir, true);
                }

                var dataJsonPath = Path.Combine(tempImportDir, "data.json");
                if (!System.IO.File.Exists(dataJsonPath))
                {
                    throw new FileNotFoundException("data.json not found in the import archive.");
                }

                var jsonData = await System.IO.File.ReadAllTextAsync(dataJsonPath);
                var importData = JsonConvert.DeserializeObject<ExportDataDto>(jsonData);

                if (importData == null || !importData.Documents.Any())
                {
                    _logger.LogWarning("No documents found in the import data.");
                    return; // Nothing to import
                }

                // Maps old GUIDs to new GUIDs to maintain relationships
                var documentIdMap = new Dictionary<Guid, Guid>();
                var pageIdMap = new Dictionary<Guid, Guid>();
                var versionIdMap = new Dictionary<Guid, Guid>();

                foreach (var exportDoc in importData.Documents)
                {
                    var newDocId = Guid.NewGuid();
                    documentIdMap[exportDoc.DocumentId] = newDocId;

                    var newDocument = new Document
                    {
                        DocumentId = newDocId,
                        Title = exportDoc.Title,
                        CreatedAt = DateTime.UtcNow, // Set new creation time
                        UpdatedAt = DateTime.UtcNow, // Set new update time
                        UserId = importingUserId // Assign to the importing user
                    };

                    // Handle Tags (assuming tags are unique by name and can be reused)
                    foreach (var tagName in exportDoc.Tags)
                    {
                        var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                        if (existingTag == null)
                        {
                            existingTag = new Tag { Name = tagName };
                            _context.Tags.Add(existingTag);
                        }
                        newDocument.Tags.Add(existingTag);
                    }

                    _context.Documents.Add(newDocument);

                    foreach (var exportPage in exportDoc.Pages)
                    {
                        var newPageId = Guid.NewGuid();
                        pageIdMap[exportPage.PageId] = newPageId;

                        var newPage = new Page
                        {
                            PageId = newPageId,
                            Title = exportPage.Title,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            UserId = importingUserId, // Assign to the importing user
                            DocumentId = newDocId, // Link to the new document
                            Order = exportPage.Order
                        };

                        _context.Pages.Add(newPage);

                        Guid newCurrentVersionId = Guid.Empty;

                        foreach (var exportVersion in exportPage.Versions)
                        {
                            var newVersionId = Guid.NewGuid();
                            versionIdMap[exportVersion.VersionId] = newVersionId;

                            var newVersion = new Core.Entities.Version
                            {
                                VersionId = newVersionId,
                                VersionNumber = exportVersion.VersionNumber,
                                Message = exportVersion.Message,
                                CreatedAt = DateTime.UtcNow,
                                OcrData = exportVersion.OcrData,
                                PageId = newPageId // Link to the new page
                            };

                            // Handle image file import
                            if (!string.IsNullOrEmpty(exportVersion.ImagePath))
                            {
                                var sourceImagePath = Path.Combine(tempImportDir, exportVersion.ImagePath);
                                if (System.IO.File.Exists(sourceImagePath))
                                {
                                    // Save the image to the application's file storage
                                    var savedImagePath = await _fileStorageService.SaveFileFromPathAsync(sourceImagePath, newPage, newVersion);
                                    newVersion.ImagePath = savedImagePath;
                                }
                                else
                                {
                                    _logger.LogWarning("Image file not found in import archive for version {VersionId}: {ImagePath}", exportVersion.VersionId, sourceImagePath);
                                }
                            }

                            _context.Versions.Add(newVersion);

                            if (exportPage.CurrentVersionId == exportVersion.VersionId)
                            {
                                newCurrentVersionId = newVersionId;
                            }
                        }
                        newPage.CurrentVersionId = newCurrentVersionId;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Data import completed successfully for user {UserId}.", importingUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data import for user {UserId}.", importingUserId);
                throw; // Re-throw to indicate failure
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempImportDir))
                {
                    Directory.Delete(tempImportDir, true);
                }
            }
        }
    }
}
