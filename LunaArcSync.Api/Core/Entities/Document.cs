using System;
using System.Collections.Generic;
using LunaArcSync.Api.Core.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LunaArcSync.Api.Core.Entities
{
    public class Document
    {
        public Guid DocumentId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 外键，关联到创建此文档的用户
        public string UserId { get; set; }
        public virtual AppUser User { get; set; }

        // 导航属性，表示这个文档包含多个页面
        public virtual ICollection<Page> Pages { get; set; } = new List<Page>();
        public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}