using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LunaArcSync.Api.DTOs
{
    public class PageReorderSetDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one page mapping is required.")]
        public List<PageOrderMappingItemDto> PageOrders { get; set; } = new List<PageOrderMappingItemDto>();
    }
}