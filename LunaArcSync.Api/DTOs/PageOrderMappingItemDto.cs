using System;
using System.ComponentModel.DataAnnotations;

namespace LunaArcSync.Api.DTOs
{
    public class PageOrderMappingItemDto
    {
        [Required]
        public Guid PageId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Order must be a positive integer.")]
        public int Order { get; set; }
    }
}