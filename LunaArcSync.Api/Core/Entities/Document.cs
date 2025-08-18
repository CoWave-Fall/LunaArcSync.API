using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LunaArcSync.Api.Core.Entities
{
    public class Document
    {
        [Key] // 声明为主键
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // 声明为自增
        public Guid DocumentId { get; set; }

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

        // 导航属性，表示此文档属于哪个用户
        public AppUser? User { get; set; }
        // --- 结束添加 ---

        
    }
}