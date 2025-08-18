namespace LunaArcSync.Api.DTOs
{
    public class DocumentDto
    {
        public Guid DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}