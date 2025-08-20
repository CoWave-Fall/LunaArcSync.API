using System;
using System.Collections.Generic;

namespace LunaArcSync.Api.Core.Entities
{
    public class Tag
    {
        public Guid TagId { get; set; }
        public string Name { get; set; }

        // 多对多关系：一个 Tag 可以关联多个 Document
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}