using System;
using System.ComponentModel.DataAnnotations;

namespace LunaArcSync.Api.DTOs
{
    public class RevertDocumentDto
    {
        [Required]
        public Guid TargetVersionId { get; set; }
    }
}