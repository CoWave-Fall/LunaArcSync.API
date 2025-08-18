using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LunaArcSync.Api.DTOs
{
    public class CreateVersionDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        [MaxLength(500)]
        public string? Message { get; set; }
    }
}