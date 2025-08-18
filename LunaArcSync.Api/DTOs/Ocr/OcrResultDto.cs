using System.Collections.Generic;

namespace LunaArcSync.Api.DTOs.Ocr
{
    public class OcrResultDto
    {
        public List<TextLineDto> Lines { get; set; } = new();
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
    }
}