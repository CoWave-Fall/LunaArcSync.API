using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using LunaArcSync.Api.Core.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace LunaArcSync.Api.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        public string ServerName { get; set; }

        public LoginModel(SignInManager<AppUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [BindProperty]
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [BindProperty]
        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync(string returnUrl = null)
        {

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var _configuration = configurationBuilder.Build();


            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;

            string ServerName  = _configuration["ServerName"] ?? "Default Server Name";

        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            _logger.LogInformation("OnPostAsync called.");
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Email, Password, RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    _logger.LogWarning("Login failed for user {Email}. IsLockedOut: {IsLockedOut}, IsNotAllowed: {IsNotAllowed}, RequiresTwoFactor: {RequiresTwoFactor}", Email, result.IsLockedOut, result.IsNotAllowed, result.RequiresTwoFactor);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors);
            _logger.LogWarning("ModelState is invalid. Errors: {Errors}", string.Join(", ", errors.Select(e => e.ErrorMessage)));
            return Page();
        }
    }
}