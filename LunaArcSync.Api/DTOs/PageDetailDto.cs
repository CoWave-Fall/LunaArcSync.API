namespace LunaArcSync.Api.DTOs
{
    public class PageDetailDto
    {
        public Guid PageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public VersionDto? CurrentVersion { get; set; } // 包含当前版本的详细信息
        public int TotalVersions { get; set; }
    }
}