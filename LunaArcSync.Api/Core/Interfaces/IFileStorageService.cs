using Microsoft.AspNetCore.Http;
using LunaArcSync.Api.Core.Entities; // 引入实体

namespace LunaArcSync.Api.Core.Interfaces
{
    public interface IFileStorageService
    {
        // 修改方法签名
        Task<string> SaveFileAsync(IFormFile file, Document document, Core.Entities.Version version);
        void DeleteFile(string fileName); // <-- 添加这个同步方法
    }
}