using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace LunaArcSync.Api.Pages
{
    public class DocumentsModel : PageModel
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly UserManager<AppUser> _userManager;

        public DocumentsModel(IDocumentRepository documentRepository, UserManager<AppUser> userManager)
        {
            _documentRepository = documentRepository;
            _userManager = userManager;
        }

        public IEnumerable<Document> Documents { get; set; } = new List<Document>();

        public async Task OnGetAsync()
        {
            var pagedResult = await _documentRepository.GetAllDocumentsForAdminAsync(1, 20, "updated_at_desc", new List<string>());
            Documents = pagedResult.Items;
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _documentRepository.DeleteDocumentAsync(id, userId);
            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Failed to delete document or document not found.");
            }

            return RedirectToPage();
        }
    }
}
