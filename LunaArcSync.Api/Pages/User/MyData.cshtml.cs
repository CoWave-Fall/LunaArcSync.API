using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace LunaArcSync.Api.Pages.User
{
    public class MyDataModel : PageModel
    {
        private readonly ILogger<MyDataModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public MyDataModel(ILogger<MyDataModel> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostImportAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return Page();
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/data/import/my");
                
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);
                request.Content = content;

                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Data imported successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Import failed: {response.StatusCode} - {errorContent}";
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error during data import.");
                TempData["Error"] = "An error occurred during import: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}
