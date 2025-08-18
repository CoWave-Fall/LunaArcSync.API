using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LunaArcSync.Api.Core.Entities; // 引入实体
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.DTOs;
using LunaArcSync.Api.Infrastructure.Data;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/jobs")]
    public class JobsController : ControllerBase
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILogger<JobsController> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDocumentRepository _documentRepository;
        private readonly AppDbContext _context;

        public JobsController(
            IBackgroundTaskQueue taskQueue,
            ILogger<JobsController> logger,
            IServiceProvider serviceProvider,
            IDocumentRepository documentRepository,
            AppDbContext context)
        {
            _taskQueue = taskQueue;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _documentRepository = documentRepository;
            _context = context;
        }

        [HttpGet("{jobId}")]
        public async Task<ActionResult<JobDto>> GetJobStatus(Guid jobId)
        {
            var job = await _context.Jobs.FindAsync(jobId);

            if (job == null)
            {
                return NotFound(new { message = $"No job found with ID: {jobId}" });
            }

            var jobDto = new JobDto
            {
                JobId = job.JobId,
                Type = job.Type.ToString(),
                Status = job.Status.ToString(),
                AssociatedDocumentId = job.AssociatedDocumentId,
                SubmittedAt = job.SubmittedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                ErrorMessage = job.ErrorMessage
            };

            return Ok(jobDto);
        }

        [HttpPost("ocr/{versionId}")]
        public async Task<IActionResult> RequestOcr(Guid versionId)
        {
            var version = await _context.Versions.FindAsync(versionId);
            if (version == null)
            {
                return NotFound(new { message = $"No version found with ID: {versionId}" });
            }

            // 1. 创建 Job 实体并存入数据库
            var newJob = new Job
            {
                JobId = Guid.NewGuid(),
                Type = JobType.Ocr,
                Status = JobStatus.Queued,
                AssociatedDocumentId = version.DocumentId
            };
            await _context.Jobs.AddAsync(newJob);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created and queued OCR job {JobId} for version ID: {VersionId}", newJob.JobId, versionId);

            // 2. 将 Job 推入后台队列处理
            _taskQueue.QueueBackgroundWorkItem(async token =>
            {
                // 后台任务必须创建自己的依赖注入作用域
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var ocrService = scope.ServiceProvider.GetRequiredService<IOcrService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobsController>>();
                    Job? job = null; // 在 try/catch 外部声明

                    try
                    {
                        job = await dbContext.Jobs.FindAsync(newJob.JobId);
                        if (job != null)
                        {
                            job.Status = JobStatus.Processing;
                            job.StartedAt = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync(token);
                            logger.LogInformation("Job {JobId} status updated to Processing.", job.JobId);
                        }

                        // 执行核心任务
                        await ocrService.PerformOcrAsync(versionId);

                        job = await dbContext.Jobs.FindAsync(newJob.JobId);
                        if (job != null)
                        {
                            job.Status = JobStatus.Completed;
                            job.CompletedAt = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync(token);
                            logger.LogInformation("Job {JobId} status updated to Completed.", job.JobId);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Job {JobId} failed during execution.", newJob.JobId);
                        job = await dbContext.Jobs.FindAsync(newJob.JobId);
                        if (job != null)
                        {
                            job.Status = JobStatus.Failed;
                            job.CompletedAt = DateTime.UtcNow;
                            job.ErrorMessage = ex.Message;
                            await dbContext.SaveChangesAsync(token);
                        }
                    }
                }
            });

            // 3. 立即返回 202 Accepted，并附带 JobId
            return Accepted(new { jobId = newJob.JobId, message = "OCR job has been queued." });
        }

        [HttpPost("stitch/document/{documentId}")]
        public async Task<IActionResult> RequestStitch(Guid documentId, [FromBody] StitchJobDto stitchJobDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // <--- 获取 userId
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var documentExists = await _documentRepository.GetDocumentByIdAsync(documentId, userId);
            if (documentExists == null)
            {
                return NotFound(new { message = $"No document found with ID: {documentId}" });
            }

            foreach (var versionId in stitchJobDto.SourceVersionIds)
            {
                if (!await _documentRepository.VersionExistsAsync(versionId))
                {
                    return NotFound(new { message = $"No version found with ID: {versionId}" });
                }
            }

            var newJob = new Job
            {
                JobId = Guid.NewGuid(),
                Type = JobType.Stitch,
                Status = JobStatus.Queued,
                AssociatedDocumentId = documentId
            };
            await _context.Jobs.AddAsync(newJob);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created and queued Stitch job {JobId} for document ID: {DocumentId}", newJob.JobId, documentId);

            _taskQueue.QueueBackgroundWorkItem(async token =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var imageService = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobsController>>();
                    Job? job = null;

                    try
                    {
                        job = await dbContext.Jobs.FindAsync(newJob.JobId);
                        if (job != null)
                        {
                            job.Status = JobStatus.Processing;
                            job.StartedAt = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync(token);
                            logger.LogInformation("Job {JobId} status updated to Processing.", job.JobId);
                        }

                        await imageService.StitchImagesAsync(userId, documentId, stitchJobDto.SourceVersionIds);

                        job = await dbContext.Jobs.FindAsync(newJob.JobId);
                        if (job != null)
                        {
                            job.Status = JobStatus.Completed;
                            job.CompletedAt = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync(token);
                            logger.LogInformation("Job {JobId} status updated to Completed.", job.JobId);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Job {JobId} failed during execution.", newJob.JobId);
                        job = await dbContext.Jobs.FindAsync(newJob.JobId);
                        if (job != null)
                        {
                            job.Status = JobStatus.Failed;
                            job.CompletedAt = DateTime.UtcNow;
                            job.ErrorMessage = ex.Message;
                            await dbContext.SaveChangesAsync(token);
                        }
                    }
                }
            });

            return Accepted(new { jobId = newJob.JobId, message = "Stitch job has been queued." });
        }
    }
}