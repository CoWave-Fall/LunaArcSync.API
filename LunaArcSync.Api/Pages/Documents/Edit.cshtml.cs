using System;
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
    public class EditModel : PageModel
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<EditModel> _logger;

        public EditModel(IDocumentRepository documentRepository, UserManager<AppUser> userManager, ILogger<EditModel> logger)
        {
            _documentRepository = documentRepository;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public Guid Id { get; set; }

            [Required]
            [StringLength(255, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            [Display(Name = "Title")]
            public string Title { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var document = await _documentRepository.GetDocumentWithPagesByIdAsync(id, userId);
            if (document == null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Id = document.DocumentId,
                Title = document.Title
            };

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
                return Unauthorized();
            }

            var updatedDocument = await _documentRepository.UpdateDocumentAsync(Input.Id, Input.Title, null, userId);

            if (updatedDocument == null)
            {
                ModelState.AddModelError(string.Empty, "Document not found or not authorized.");
                return Page();
            }

            _logger.LogInformation("Document {DocumentId} updated.", updatedDocument.DocumentId);
            return RedirectToPage("/Documents");
        }
    }
}
