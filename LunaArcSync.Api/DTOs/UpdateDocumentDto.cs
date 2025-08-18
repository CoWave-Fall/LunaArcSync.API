using System.ComponentModel.DataAnnotations;

namespace LunaArcSync.Api.DTOs
{
    public class UpdateDocumentDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
    }
}