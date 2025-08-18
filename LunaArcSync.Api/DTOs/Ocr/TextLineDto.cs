using System.Collections.Generic;

namespace LunaArcSync.Api.DTOs.Ocr
{
    public class TextLineDto
    {
        public List<WordDto> Words { get; set; } = new();
        public string Text => string.Join(" ", Words.Select(w => w.Text));
        public BoundingBoxDto Bbox { get; set; } = new();
    }
}