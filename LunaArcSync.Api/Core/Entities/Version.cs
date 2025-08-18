using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LunaArcSync.Api.Core.Entities
{
    public class Version
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid VersionId { get; set; }

        public int VersionNumber { get; set; }

        [MaxLength(500)]
        public string? Message { get; set; } // 变更说明，可以为空

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        public string? OcrData { get; set; } // 将存储序列化后的 OcrResultDto JSON
        public string? OcrDataNormalized { get; set; } // 新增：用于搜索

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 外键
        public Guid DocumentId { get; set; }
        // 导航属性
        public Document? Document { get; set; }

    }
}