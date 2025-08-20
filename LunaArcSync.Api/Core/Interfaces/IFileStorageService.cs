using Microsoft.AspNetCore.Http;
using LunaArcSync.Api.Core.Entities; // 引入实体
using System.Threading.Tasks; // 引入 Tasks

namespace LunaArcSync.Api.Core.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves a file to the configured storage and returns its unique name.
        /// </summary>
        Task<string> SaveFileAsync(IFormFile file, Page page, Core.Entities.Version version);

        /// <summary>
        /// Deletes a file from the storage.
        /// </summary>
        void DeleteFile(string fileName);

        // +++ 添加这个新方法的定义 +++
        /// <summary>
        /// Reads a file from the storage and returns its content as a byte array.
        /// </summary>
        /// <param name="fileName">The unique name of the file to read.</param>
        /// <returns>A byte array of the file content, or null if the file does not exist.</returns>
        Task<byte[]?> ReadFileBytesAsync(string fileName);
    }
}