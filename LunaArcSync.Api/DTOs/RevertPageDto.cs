using System;
using System.ComponentModel.DataAnnotations;

namespace LunaArcSync.Api.DTOs
{
    public class RevertPageDto
    {
        [Required]
        public Guid TargetVersionId { get; set; }
    }
}