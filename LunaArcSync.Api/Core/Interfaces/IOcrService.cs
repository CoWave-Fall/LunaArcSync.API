using System;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Core.Interfaces
{
    public interface IOcrService
    {
        /// <summary>
        /// Performs OCR on a specific version of a page.
        /// </summary>
        /// <param name="versionId">The ID of the version to process.</param>
        /// <returns>The recognized text.</returns>
        Task<string> PerformOcrAsync(Guid versionId);
    }
}