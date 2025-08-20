using Microsoft.AspNetCore.Http;
using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.DTOs;
using System;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Core.Interfaces
{
    public interface IPageRepository
    {
        // --- Page Management (Multi-User & Paginated) ---

        /// <summary>
        /// Gets a paginated list of pages for a specific user.
        /// </summary>
        Task<PagedResultDto<Page>> GetAllPagesAsync(string userId, int pageNumber, int pageSize);

        /// <summary>
        /// Gets a single page with its version details, ensuring it belongs to the specific user.
        /// </summary>
        Task<Page?> GetPageWithVersionsByIdAsync(Guid id, string userId);

        /// <summary>
        /// Gets a single page by its ID, ensuring it belongs to the specific user (lightweight version).
        /// </summary>
        Task<Page?> GetPageByIdAsync(Guid id, string userId);

        /// <summary>
        /// Creates a new page and its initial version. The userId is expected to be set on the newPage entity.
        /// </summary>
        Task<Page> CreatePageAsync(Page newPage, IFormFile file);

        /// <summary>
        /// Updates a page's metadata, ensuring it belongs to the specific user.
        /// </summary>
        Task<Page?> UpdatePageAsync(Guid id, string newTitle, string userId);

        /// <summary>
        /// Deletes a page and all its associated data, ensuring it belongs to the specific user.
        /// </summary>
        Task<bool> DeletePageAsync(Guid id, string userId);

        // --- Search ---

        /// <summary>
        /// Searches for pages for a specific user based on a query.
        /// </summary>
        Task<PagedResultDto<Page>> SearchPagesAsync(string query, string userId, int pageNumber, int pageSize);


        // --- Version Management (Context provided by Controller) ---

        /// <summary>
        /// Creates a new version for an existing page.
        /// </summary>
        Task<Core.Entities.Version> CreateNewVersionAsync(Page page, Core.Entities.Version newVersion, IFormFile file);

        /// <summary>
        /// Sets a specific version as the current one for a page.
        /// </summary>
        Task<bool> SetCurrentVersionAsync(Guid pageId, Guid versionId);

        /// <summary>
        /// Checks if a version exists.
        /// </summary>
        Task<bool> VersionExistsAsync(Guid versionId);

        Task<Core.Entities.Version?> GetVersionByIdAsync(Guid versionId);


    }
}