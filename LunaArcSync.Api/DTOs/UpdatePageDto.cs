using System.ComponentModel.DataAnnotations;

namespace LunaArcSync.Api.DTOs
{
    public class UpdatePageDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
    }
}