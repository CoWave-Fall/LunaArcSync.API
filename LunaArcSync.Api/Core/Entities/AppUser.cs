using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace LunaArcSync.Api.Core.Entities
{
    public class AppUser : IdentityUser
    {
        // 你可以在这里添加自定义属性，例如：
        // public string? Nickname { get; set; }

        // 添加一个导航属性，表示一个用户可以拥有多个文档
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}