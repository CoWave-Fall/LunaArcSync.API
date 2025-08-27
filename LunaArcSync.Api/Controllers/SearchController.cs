using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LunaArcSync.Api.Core.Constants;

namespace LunaArcSync.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IPageRepository _pageRepository;
        private readonly UserManager<AppUser> _userManager;

        public SearchController(
            IDocumentRepository documentRepository,
            IPageRepository pageRepository,
            UserManager<AppUser> userManager)
        {
            _documentRepository = documentRepository;
            _pageRepository = pageRepository;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<List<SearchResultDto>>> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var currentUser = await _userManager.FindByIdAsync(userId);
            var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, UserRoles.Admin);

            var documentResults = await _documentRepository.SearchDocumentsAsync(query, userId, isAdmin);
            var pageResults = await _pageRepository.SearchPagesAsync(query, userId, isAdmin);

            var combinedResults = new List<SearchResultDto>();
            combinedResults.AddRange(documentResults);
            combinedResults.AddRange(pageResults);

            // Optional: Sort results, e.g., by type then by title
            combinedResults = combinedResults.OrderBy(r => r.Type).ThenBy(r => r.Title).ToList();

            return Ok(combinedResults);
        }
    }
}
