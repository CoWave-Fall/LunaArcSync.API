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

        Task<List<Page>> GetUnassignedPagesAsync(string userId);

        /// <summary>
        /// Updates the order of multiple pages within a document.
        /// </summary>
        /// <param name="documentId">The ID of the document.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="pageOrders">A dictionary where key is PageId and value is the new Order.</param>
        /// <returns>True if successful, false otherwise.</returns>
        Task<bool> UpdatePageOrdersAsync(Guid documentId, string userId, Dictionary<Guid, int> pageOrders);

        /// <summary>
        /// Gets all pages for a specific document, ensuring they belong to the user.
        /// </summary>
        /// <param name="documentId">The ID of the document.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of pages belonging to the document and user.</returns>
        Task<List<Page>> GetPagesByDocumentIdAsync(Guid documentId, string userId);
    }
}