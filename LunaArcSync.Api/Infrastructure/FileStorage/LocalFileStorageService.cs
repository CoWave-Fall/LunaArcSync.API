using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http; // 引入 Http
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LunaArcSync.Api.Core.Entities; // 引入实体
using LunaArcSync.Api.Core.Interfaces;
using System; // 引入 System
using System.IO;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Infrastructure.FileStorage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _storageRootPath;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            string relativePath = configuration.GetValue<string>("Storage:LocalPath") ?? "FileStorage";
            _storageRootPath = Path.Combine(environment.ContentRootPath, relativePath);
            _logger.LogInformation("File storage root path is set to: {path}", _storageRootPath);
            if (!Directory.Exists(_storageRootPath))
            {
                Directory.CreateDirectory(_storageRootPath);
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file, Page page, Core.Entities.Version version)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty", nameof(file));
            }

            var fileExtension = Path.GetExtension(file.FileName);
            var newFileName = $"{page.PageId}_{version.VersionId}{fileExtension}";

            var filePath = Path.Combine(_storageRootPath, newFileName);
            _logger.LogInformation("Saving file to absolute path: {filePath}", filePath);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return newFileName;
        }

        public void DeleteFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            var filePath = Path.Combine(_storageRootPath, fileName);
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Successfully deleted file: {FilePath}", filePath);
                }
                else
                {
                    _logger.LogWarning("Attempted to delete a file that does not exist: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting file: {FilePath}", filePath);
            }
        }

        // +++ 添加这个新方法的完整实现 +++
        public async Task<byte[]?> ReadFileBytesAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                _logger.LogWarning("ReadFileBytesAsync called with empty file name.");
                return null;
            }

            try
            {
                var filePath = Path.Combine(_storageRootPath, fileName);
                _logger.LogInformation("Attempting to read file from: {FilePath}", filePath);

                if (File.Exists(filePath))
                {
                    return await File.ReadAllBytesAsync(filePath);
                }

                _logger.LogWarning("File not found at path: {FilePath}", filePath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while reading file: {FileName}", fileName);
                return null;
            }
        }
    }
}