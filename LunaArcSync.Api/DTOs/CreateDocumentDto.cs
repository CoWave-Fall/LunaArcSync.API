using System.ComponentModel.DataAnnotations;

namespace LunaArcSync.Api.DTOs
{
    public class CreateDocumentDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public required string Title { get; set; }
    }
}