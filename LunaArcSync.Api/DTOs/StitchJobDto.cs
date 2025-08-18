using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LunaArcSync.Api.DTOs
{
    public class StitchJobDto
    {
        [Required]
        [MinLength(2)]
        public List<Guid> SourceVersionIds { get; set; } = new List<Guid>();
    }
}