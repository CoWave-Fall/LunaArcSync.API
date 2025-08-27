using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

namespace LunaArcSync.Api.Pages
{
    public class SettingsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public SettingsModel(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        [BindProperty]
        public string ServerName { get; set; } = string.Empty;

        [BindProperty]
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
        public int HttpsPort { get; set; }

        [BindProperty]
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
        public int HttpPort { get; set; }

        public void OnGet()
        {
            ServerName = _configuration["ServerName"] ?? "Default Server Name";
            LoadLaunchSettings();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Update appsettings.json
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var appSettingsJson = System.IO.File.ReadAllText(appSettingsPath);
            dynamic? appSettingsObj = JsonConvert.DeserializeObject(appSettingsJson);
            if (appSettingsObj != null)
            {
                appSettingsObj["ServerName"] = ServerName;
                System.IO.File.WriteAllText(appSettingsPath, JsonConvert.SerializeObject(appSettingsObj, Formatting.Indented));
            }

            // Update launchSettings.json
            SaveLaunchSettings();

            TempData["Message"] = "Settings saved successfully! Application restart required for port changes to take effect.";
            return RedirectToPage();
        }

        private void LoadLaunchSettings()
        {
            var launchSettingsPath = Path.Combine(_env.ContentRootPath, "Properties", "launchSettings.json");
            if (!System.IO.File.Exists(launchSettingsPath))
            {
                // Handle case where launchSettings.json doesn't exist
                HttpsPort = 0;
                HttpPort = 0;
                return;
            }

            var json = System.IO.File.ReadAllText(launchSettingsPath);
            dynamic? jsonObj = JsonConvert.DeserializeObject(json);

            var httpsProfile = jsonObj?.profiles?.https;
            if (httpsProfile != null)
            {
                string? applicationUrl = httpsProfile.applicationUrl;
                if (applicationUrl != null)
                {
                    var urls = applicationUrl.Split(';');
                    foreach (var url in urls)
                    {
                        if (url.StartsWith("https://"))
                        {
                            var match = new Regex(@":(\d+)").Match(url);
                            if (match.Success && int.TryParse(match.Groups[1].Value, out int port))
                            {
                                HttpsPort = port;
                            }
                        }
                        else if (url.StartsWith("http://"))
                        {
                            var match = new Regex(@":(\d+)").Match(url);
                            if (match.Success && int.TryParse(match.Groups[1].Value, out int port))
                            {
                                HttpPort = port;
                            }
                        }
                    }
                }
            }
        }

        private void SaveLaunchSettings()
        {
            var launchSettingsPath = Path.Combine(_env.ContentRootPath, "Properties", "launchSettings.json");
            var json = System.IO.File.ReadAllText(launchSettingsPath);
            dynamic? jsonObj = JsonConvert.DeserializeObject(json);

            if (jsonObj != null)
            {
                var httpsProfile = jsonObj?.profiles?.https;
                if (httpsProfile != null)
                {
                    string? currentApplicationUrl = httpsProfile.applicationUrl;
                    List<string> urls;

                    if (currentApplicationUrl != null)
                    {
                        urls = currentApplicationUrl.Split(';').ToList();
                    }
                    else
                    {
                        urls = new List<string>();
                    }

                    // Update HTTPS port
                    var httpsUrlIndex = urls.FindIndex(url => url.StartsWith("https://"));
                    if (httpsUrlIndex != -1)
                    {
                        urls[httpsUrlIndex] = Regex.Replace(urls[httpsUrlIndex], @":\d+", ":" + HttpsPort);
                    } else {
                        urls.Add($"https://localhost:{HttpsPort}");
                    }

                    // Update HTTP port
                    var httpUrlIndex = urls.FindIndex(url => url.StartsWith("http://"));
                    if (httpUrlIndex != -1)
                    {
                        urls[httpUrlIndex] = Regex.Replace(urls[httpUrlIndex], @":\d+", ":" + HttpPort);
                    } else {
                        urls.Add($"http://localhost:{HttpPort}");
                    }

                    httpsProfile.applicationUrl = string.Join(";", urls);
                }
            }

            System.IO.File.WriteAllText(launchSettingsPath, JsonConvert.SerializeObject(jsonObj, Formatting.Indented));
        }
    }
}
