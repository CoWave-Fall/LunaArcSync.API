namespace LunaArcSync.Api.DTOs
{
    public class PageDto
    {
        public Guid PageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}