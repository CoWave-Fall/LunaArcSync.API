using System;

namespace LunaArcSync.Api.DTOs
{
    public class DocumentDto
    {
        public Guid DocumentId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int PageCount { get; set; } // 新增：显示文档包含的页面数量
    }
}