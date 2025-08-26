using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace LunaArcSync.Api.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly IDocumentRepository _documentRepository;
        private readonly AppDbContext _context;

        public IndexModel(
            ILogger<IndexModel> logger,
            UserManager<AppUser> userManager,
            IDocumentRepository documentRepository,
            AppDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _documentRepository = documentRepository;
            _context = context;
        }

        public int UserCount { get; set; }
        public int DocumentCount { get; set; }
        public int PageCount { get; set; }
        public string? ApiPort { get; set; }

        public async Task OnGetAsync()
        {
            UserCount = await _userManager.Users.CountAsync();
            var pagedResult = await _documentRepository.GetAllDocumentsForAdminAsync(1, 1, "updated_at_desc", new System.Collections.Generic.List<string>());
            DocumentCount = pagedResult.TotalCount;
            PageCount = await _context.Pages.CountAsync();
            ApiPort = HttpContext.Connection.LocalPort.ToString();
        }
    }
}
