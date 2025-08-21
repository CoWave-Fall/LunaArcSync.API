using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace LunaArcSync.Api.DTOs
{
    public class UpdateDocumentDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public required string Title { get; set; }

        public List<string>? Tags { get; set; }
    }
}