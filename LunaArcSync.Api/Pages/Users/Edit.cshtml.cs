using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using LunaArcSync.Api.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace LunaArcSync.Api.Pages.Users
{
    public class EditModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<EditModel> _logger;

        public EditModel(UserManager<AppUser> userManager, ILogger<EditModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string Id { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Id = user.Id,
                Email = user.Email
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(Input.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }
            }

            // Update email if changed (optional, for now we keep it readonly)
            // if (user.Email != Input.Email)
            // {
            //     var setEmailResult = await _userManager.SetEmailAsync(user, Input.Email);
            //     if (!setEmailResult.Succeeded)
            //     {
            //         foreach (var error in setEmailResult.Errors)
            //         {
            //             ModelState.AddModelError(string.Empty, error.Description);
            //         }
            //         return Page();
            //     }
            // }

            _logger.LogInformation("User {UserId} updated.", user.Id);
            return RedirectToPage("/Users");
        }
    }
}
