using Microsoft.AspNetCore.Http;

namespace LunaArcSync.Api.DTOs
{
    public class ImportFileDto
    {
        public IFormFile File { get; set; }
    }
}
