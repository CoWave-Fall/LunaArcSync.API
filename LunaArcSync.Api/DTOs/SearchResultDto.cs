namespace LunaArcSync.Api.DTOs
{
    public class SearchResultDto
    {
        public string Type { get; set; } = string.Empty; // "document" or "page"
        public Guid? DocumentId { get; set; }
        public Guid? PageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string MatchSnippet { get; set; } = string.Empty;
    }
}
