using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LunaArcSync.Api.Core.Entities; // 引入实体
using LunaArcSync.Api.Core.Interfaces;
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
            // ... (构造函数不变)
            _logger = logger;
            string relativePath = configuration.GetValue<string>("Storage:LocalPath") ?? "FileStorage";
            _storageRootPath = Path.Combine(environment.ContentRootPath, relativePath);
            _logger.LogInformation("File storage root path is set to: {path}", _storageRootPath);
            if (!Directory.Exists(_storageRootPath))
            {
                Directory.CreateDirectory(_storageRootPath);
            }
        }

        // 实现新的接口方法
        public async Task<string> SaveFileAsync(IFormFile file, Document document, Core.Entities.Version version)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty", nameof(file));
            }

            // 1. 构建新的、有意义的文件名
            var fileExtension = Path.GetExtension(file.FileName);
            var newFileName = $"{document.DocumentId}_{version.VersionId}{fileExtension}";

            var filePath = Path.Combine(_storageRootPath, newFileName);
            _logger.LogInformation("Saving file to absolute path: {filePath}", filePath);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 2. 返回构建好的文件名，以便存入数据库
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
                // 在生产环境中，这里可能需要更复杂的错误处理，但现在记录日志即可
            }
        }
    }
}