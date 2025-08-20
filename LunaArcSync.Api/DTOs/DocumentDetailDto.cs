using System;
using System.Collections.Generic;

namespace LunaArcSync.Api.DTOs
{
    public class DocumentDetailDto
    {
        public Guid DocumentId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 详情页将包含其所有的页面信息
        public List<PageDto> Pages { get; set; } = new List<PageDto>();
        public List<string> Tags { get; set; } = new List<string>();
        public string? OwnerEmail { get; set; } // Added for admin view
    }
}