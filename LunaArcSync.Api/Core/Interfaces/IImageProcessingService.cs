using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Core.Interfaces
{
    public interface IImageProcessingService
    {
        /// <summary>
        /// Stitches multiple images into a single image and creates a new version for the page.
        /// </summary>
        /// <param name="pageId">The ID of the page to which the new version will belong.</param>
        /// <param name="sourceVersionIds">A list of version IDs whose images need to be stitched.</param>
        /// <returns>The newly created Version entity.</returns>
        Task<Core.Entities.Version> StitchImagesAsync(string userId, Guid pageId, List<Guid> sourceVersionIds);
    }
}