using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.Core.Models; // 假设 PagedResult 在这个命名空间下
using LunaArcSync.Api.DTOs; // Added
using System;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Core.Interfaces
{
    public interface IDocumentRepository
    {
        /// <summary>
        /// Asynchronously gets a paginated list of all documents for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="pageNumber">The page number to retrieve.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="sortBy">The sort order for the documents.</param>
        /// <param name="tags">A list of tags to filter the documents by.</param>
        /// <returns>A PagedResult containing the documents.</returns>
        Task<PagedResult<Document>> GetAllDocumentsAsync(string userId, int pageNumber, int pageSize, string sortBy, List<string> tags);

        /// <summary>
        /// Asynchronously gets a single document by its ID, including its associated pages, ensuring it belongs to the user.
        /// </summary>
        /// <param name="documentId">The ID of the document.</param>
        /// <param name="userId">The ID of the user to verify ownership.</param>
        /// <returns>The Document entity with its Pages collection, or null if not found or not owned by the user.</returns>
        Task<Document?> GetDocumentWithPagesByIdAsync(Guid documentId, string userId);

        /// <summary>
        /// Asynchronously creates a new document in the database.
        /// </summary>
        /// <param name="document">The document entity to create.</param>
        /// <returns>The created document entity with its ID and timestamps populated.</returns>
        Task<Document> CreateDocumentAsync(Document document);

        /// <summary>
        /// Asynchronously updates an existing document.
        /// (我们暂时在控制器中用不到，但这是一个标准的仓储方法，为未来做准备)
        /// </summary>
        /// <param name="document">The document entity with updated values.</param>
        /// <returns>The updated document entity.</returns>
        Task<Document?> UpdateDocumentAsync(Guid documentId, string title, List<string>? tags, string userId);


        /// <summary>
        /// Asynchronously deletes a document.
        /// (我们暂时在控制器中用不到，但这是一个标准的仓储方法，为未来做准备)
        /// </summary>
        /// <param name="documentId">The ID of the document to delete.</param>
        /// <param name="userId">The ID of the user to verify ownership.</param>
        /// <returns>True if deletion was successful, otherwise false.</returns>
        Task<bool> DeleteDocumentAsync(Guid documentId, string userId);


        /// <summary>
        /// 将现有页加入文档
        /// </summary>
        /// <param name="documentId"></param>
        /// <param name="pageId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<bool> AddPageToDocumentAsync(Guid documentId, Guid pageId, string userId);

        /// <summary>
        /// Gets statistics for a specific user, including total documents and total pages.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A UserStatsDto containing the counts.</returns>
        Task<UserStatsDto> GetUserStatsAsync(string userId);

        /// <param name="pageNumber">The page number to retrieve.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="sortBy">The sort order for the documents.</param>
        /// <param name="tags">A list of tags to filter the documents by.</param>
        /// <returns>A PagedResult containing all documents.</returns>
        Task<PagedResult<Document>> GetAllDocumentsForAdminAsync(int pageNumber, int pageSize, string sortBy, List<string> tags);

        /// <summary>
        /// Asynchronously gets a single document by its ID for admin view, including its associated pages and user.
        /// </summary>
        /// <param name="documentId">The ID of the document.</param>
        /// <returns>The Document entity with its Pages and User collection, or null if not found.</returns>
        Task<Document?> GetDocumentWithPagesByIdForAdminAsync(Guid documentId);

                /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of Document entities with their related data.</returns>
        Task<List<Core.Entities.Document>> GetAllUserDocumentsWithDetailsAsync(string userId);

        /// <summary>
        /// Asynchronously gets a list of all unique tag names.
        /// </summary>
        /// <returns>A list of all unique tag names.</returns>
        Task<List<string>> GetAllTagsAsync();
    }
    }