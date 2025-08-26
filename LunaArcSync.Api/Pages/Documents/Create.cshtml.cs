using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace LunaArcSync.Api.Pages.Documents
{
    public class CreateModel : PageModel
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(IDocumentRepository documentRepository, UserManager<AppUser> userManager, ILogger<CreateModel> logger)
        {
            _documentRepository = documentRepository;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(255, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            [Display(Name = "Title")]
            public string Title { get; set; }
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return Page();
            }

            var document = new Document
            {
                Title = Input.Title,
                UserId = userId,
                CreatedAt = System.DateTime.UtcNow,
                UpdatedAt = System.DateTime.UtcNow
            };

            await _documentRepository.CreateDocumentAsync(document);

            _logger.LogInformation("Document '{Title}' created.", document.Title);
            return RedirectToPage("/Documents");
        }
    }
}
