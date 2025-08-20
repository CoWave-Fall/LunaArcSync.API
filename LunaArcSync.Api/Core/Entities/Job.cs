using System;
using System.ComponentModel.DataAnnotations;

namespace LunaArcSync.Api.Core.Entities
{
    public enum JobStatus
    {
        Queued,
        Processing,
        Completed,
        Failed
    }

    public enum JobType
    {
        Ocr,
        Stitch
    }

    public class Job
    {
        [Key]
        public Guid JobId { get; set; }

        public JobType Type { get; set; }
        public JobStatus Status { get; set; }

        public Guid AssociatedPageId { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public string? ErrorMessage { get; set; }
    }
}