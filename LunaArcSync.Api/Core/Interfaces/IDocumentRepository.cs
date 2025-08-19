using Microsoft.AspNetCore.Http;
using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.DTOs;
using System;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Core.Interfaces
{
    public interface IDocumentRepository
    {
        // --- Document Management (Multi-User & Paginated) ---

        /// <summary>
        /// Gets a paginated list of documents for a specific user.
        /// </summary>
        Task<PagedResultDto<Document>> GetAllDocumentsAsync(string userId, int pageNumber, int pageSize);

        /// <summary>
        /// Gets a single document with its version details, ensuring it belongs to the specific user.
        /// </summary>
        Task<Document?> GetDocumentWithVersionsByIdAsync(Guid id, string userId);

        /// <summary>
        /// Gets a single document by its ID, ensuring it belongs to the specific user (lightweight version).
        /// </summary>
        Task<Document?> GetDocumentByIdAsync(Guid id, string userId);

        /// <summary>
        /// Creates a new document and its initial version. The userId is expected to be set on the newDocument entity.
        /// </summary>
        Task<Document> CreateDocumentAsync(Document newDocument, IFormFile file);

        /// <summary>
        /// Updates a document's metadata, ensuring it belongs to the specific user.
        /// </summary>
        Task<Document?> UpdateDocumentAsync(Guid id, string newTitle, string userId);

        /// <summary>
        /// Deletes a document and all its associated data, ensuring it belongs to the specific user.
        /// </summary>
        Task<bool> DeleteDocumentAsync(Guid id, string userId);

        // --- Search ---

        /// <summary>
        /// Searches for documents for a specific user based on a query.
        /// </summary>
        Task<PagedResultDto<Document>> SearchDocumentsAsync(string query, string userId, int pageNumber, int pageSize);


        // --- Version Management (Context provided by Controller) ---

        /// <summary>
        /// Creates a new version for an existing document.
        /// </summary>
        Task<Core.Entities.Version> CreateNewVersionAsync(Document document, Core.Entities.Version newVersion, IFormFile file);

        /// <summary>
        /// Sets a specific version as the current one for a document.
        /// </summary>
        Task<bool> SetCurrentVersionAsync(Guid documentId, Guid versionId);

        /// <summary>
        /// Checks if a version exists.
        /// </summary>
        Task<bool> VersionExistsAsync(Guid versionId);

        Task<Core.Entities.Version?> GetVersionByIdAsync(Guid versionId);


    }
}