using System;

namespace LunaArcSync.Api.DTOs
{
    public class DocumentDto
    {
        public Guid DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int PageCount { get; set; } // 新增：显示文档包含的页面数量
        public List<string> Tags { get; set; } = new List<string>(); // Added for document tags
        public string? OwnerEmail { get; set; } // Added for admin view
    }
}