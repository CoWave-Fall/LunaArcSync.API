using System.IO;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Core.Interfaces
{
    public interface IDataExportImportService
    {
        /// <summary>
        /// Exports user data (or all data for admin) as a ZIP stream.
        /// </summary>
        /// <param name="userId">The ID of the user requesting the export. Null for admin export.</param>
        /// <param name="isAdminExport">True if the export is for admin (all data), false for user (own data).</param>
        /// <param name="targetUserId">Optional: The ID of the specific user whose data to export (only applicable for admin export).</param>
        /// <returns>A Stream containing the ZIP archive.</returns>
        Task<Stream> ExportDataAsync(string? userId, bool isAdminExport, string? targetUserId = null);

        /// <summary>
        /// Imports data from a ZIP stream for a specific user (or as admin).
        /// </summary>
        /// <param name="zipFileStream">The stream of the ZIP archive to import.</param>
        /// <param name="importingUserId">The ID of the user performing the import.</param>
        /// <param name="isAdminImport">True if the import is performed by admin, false for user.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task ImportDataAsync(Stream zipFileStream, string importingUserId, bool isAdminImport);
    }
}
