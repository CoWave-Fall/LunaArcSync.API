using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LunaArcSync.Api.Core.Constants;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite; // ADDED

namespace LunaArcSync.Api.Controllers
{
    [Authorize(Roles = UserRoles.Admin)] // Only Admin users can access this controller
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly ILogger<DataController> _logger;
        private readonly string _databasePath;

        public DataController(ILogger<DataController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _databasePath = configuration.GetConnectionString("DefaultConnection")?.Replace("Data Source=", "")
                            ?? throw new InvalidOperationException("Database connection string not configured.");
        }

        [HttpGet("export")]
        public IActionResult ExportDatabase()
        {
            // Create a temporary file for the backup
            var tempFilePath = Path.GetTempFileName();
            try
            {
                // Open a connection to the source database
                var connectionString = $"Data Source={_databasePath}";
                using (var sourceConnection = new SqliteConnection(connectionString))
                {
                    sourceConnection.Open();

                    // Open a connection to the destination (backup) database
                    using (var destinationConnection = new SqliteConnection($"Data Source={tempFilePath}"))
                    {
                        destinationConnection.Open();

                        // Perform the online backup
                        sourceConnection.BackupDatabase(destinationConnection);
                    }
                }

                // Read the backup file bytes
                var fileBytes = System.IO.File.ReadAllBytes(tempFilePath);
                var fileName = Path.GetFileName(_databasePath); // Use original file name for download

                _logger.LogInformation("Database exported successfully to temporary file {TempFilePath}", tempFilePath);
                return File(fileBytes, "application/x-sqlite3", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting database: {Message}", ex.Message);
                return StatusCode(500, "An error occurred during database export. It might be locked or corrupted.");
            }
            finally
            {
                // Clean up the temporary file
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportDatabase(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            if (!file.FileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".sqlite3", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid file type. Only .db, .sqlite, or .sqlite3 files are allowed.");
            }

            _logger.LogWarning("Attempting to import database. This will overwrite the existing database at {DatabasePath}. Application restart may be required.", _databasePath);

            try
            {
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Copy the uploaded file to the database path
                using (var stream = new FileStream(_databasePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("Database imported successfully to {DatabasePath}. Application restart is recommended.", _databasePath);
                return Ok("Database imported successfully. Please restart the application for changes to take full effect.");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error importing database: {Message}", ex.Message);
                return StatusCode(500, "Error importing database. It might be locked or in use. Please ensure the application is not actively using the database during import.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error importing database: {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred during import.");
            }
        }
    }
}