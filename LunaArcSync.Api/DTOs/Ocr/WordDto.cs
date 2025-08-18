namespace LunaArcSync.Api.DTOs.Ocr
{
    public class WordDto
    {
        public string Text { get; set; } = string.Empty;
        public BoundingBoxDto Bbox { get; set; } = new();
        public float Confidence { get; set; }
    }
}