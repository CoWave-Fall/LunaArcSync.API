using LunaArcSync.Api.Core.Entities;
using System;

namespace LunaArcSync.Api.DTOs
{
    public class JobDto
    {
        public Guid JobId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid AssociatedDocumentId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}