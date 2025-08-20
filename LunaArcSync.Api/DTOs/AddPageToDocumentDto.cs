using System;
using System.ComponentModel.DataAnnotations;

namespace LunaArcSync.Api.DTOs
{
    public class AddPageToDocumentDto
    {
        [Required]
        public Guid PageId { get; set; }
    }
}