using LunaArcSync.Api.DTOs.Ocr;

namespace LunaArcSync.Api.DTOs
{
    public class VersionDto
    {
        public Guid VersionId { get; set; }
        public int VersionNumber { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public OcrResultDto? OcrResult { get; set; } // <-- 添加这一行
    }
}