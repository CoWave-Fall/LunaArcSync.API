using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LunaArcSync.Api.Core.Entities
{
    public class Page
    {
        [Key] // 声明为主键
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // 声明为自增
        public Guid PageId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public Guid CurrentVersionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // 导航属性
        public ICollection<Version> Versions { get; set; } = new List<Version>();

        [Required]
        public string UserId { get; set; } = string.Empty;

        // 外键，关联到包含此页面的文档
        public Guid? DocumentId { get; set; } // 可以为空，表示页面尚未被分配到任何文档

        // 导航属性
        public virtual Document? Document { get; set; }

        // 导航属性，表示此文档属于哪个用户
        public AppUser? User { get; set; }
        // --- 结束添加 ---

        /// <summary>
        /// 用于排序的序号，数字越小越靠前。
        /// </summary>
        public int Order { get; set; } = 0;
    }
}