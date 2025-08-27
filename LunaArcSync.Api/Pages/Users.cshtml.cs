using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LunaArcSync.Api.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LunaArcSync.Api.Pages
{
    public class UsersModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public UsersModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public List<AppUser> Users { get; set; } = new List<AppUser>();

        public async Task OnGetAsync()
        {
            Users = await _userManager.Users.ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
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

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return RedirectToPage();
        }
    }
}
