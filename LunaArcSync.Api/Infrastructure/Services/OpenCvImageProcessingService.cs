using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Infrastructure.Services
{
    public class OpenCvImageProcessingService : IImageProcessingService
    {
        private readonly ILogger<OpenCvImageProcessingService> _logger;
        private readonly AppDbContext _context;
        private readonly IDocumentRepository _documentRepository;
        private readonly string _storageRootPath;

        public OpenCvImageProcessingService(
            ILogger<OpenCvImageProcessingService> logger,
            AppDbContext context,
            IDocumentRepository documentRepository,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _context = context;
            _documentRepository = documentRepository;
            _storageRootPath = Path.Combine(environment.ContentRootPath, "FileStorage");
        }

        // --- 核心修正：确保这个方法签名与 IImageProcessingService.cs 中的定义完全一致 ---
        public async Task<Core.Entities.Version> StitchImagesAsync(string userId, Guid documentId, List<Guid> sourceVersionIds)
        {
            if (sourceVersionIds == null || sourceVersionIds.Count < 2)
            {
                throw new ArgumentException("At least two source images are required for stitching.");
            }

            _logger.LogInformation("Starting stitching process for user {UserId}, document {DocumentId}", userId, documentId);

            var sourceVersions = _context.Versions
                .Where(v => sourceVersionIds.Contains(v.VersionId))
                .ToList();

            if (sourceVersions.Count != sourceVersionIds.Count)
            {
                throw new FileNotFoundException("One or more source versions could not be found.");
            }

            var imagePaths = sourceVersions.Select(v => Path.Combine(_storageRootPath, v.ImagePath)).ToList();
            var images = imagePaths.Select(path => Cv2.ImRead(path, ImreadModes.Color)).ToList();

            try
            {
                using var stitcher = Stitcher.Create(Stitcher.Mode.Scans);
                using var pano = new Mat();
                var status = stitcher.Stitch(images, pano);

                if (status != Stitcher.Status.OK)
                {
                    _logger.LogError("OpenCV stitching failed for document {DocumentId} with status: {Status}", documentId, status);
                    throw new InvalidOperationException($"Could not stitch images. Status: {status}");
                }

                _logger.LogInformation("Stitching successful. Creating new version.");

                // --- 修正部分：使用传入的 userId 来获取文档，并确保只有一个 'document' 变量 ---
                var document = await _documentRepository.GetDocumentWithVersionsByIdAsync(documentId, userId);
                if (document == null)
                {
                    throw new KeyNotFoundException($"Document with ID {documentId} not found for user {userId}.");
                }

                var newVersion = new Core.Entities.Version
                {
                    DocumentId = documentId,
                    VersionNumber = document.Versions.Any() ? document.Versions.Max(v => v.VersionNumber) + 1 : 1,
                    Message = $"Stitched from {sourceVersionIds.Count} images."
                };

                // (后续逻辑与之前提供的最终版一致，确保ID生成和文件保存的健壮性)
                _context.Versions.Add(newVersion);
                await _context.SaveChangesAsync();

                var fileExtension = Path.GetExtension(imagePaths.First());
                var newFileName = $"{document.DocumentId}_{newVersion.VersionId}{fileExtension}";
                var newFilePath = Path.Combine(_storageRootPath, newFileName);

                pano.ImWrite(newFilePath);
                newVersion.ImagePath = newFileName;

                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Versions.Update(newVersion);
                    await _context.SaveChangesAsync();
                    await _documentRepository.SetCurrentVersionAsync(documentId, newVersion.VersionId);
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to commit transaction for new stitched version {VersionId}", newVersion.VersionId);
                    if (System.IO.File.Exists(newFilePath)) System.IO.File.Delete(newFilePath);
                    throw;
                }

                _logger.LogInformation("New version {VersionId} created successfully from stitching.", newVersion.VersionId);
                return newVersion;

            }
            finally
            {
                foreach (var image in images)
                {
                    image.Dispose();
                }
            }
        }
    }
}