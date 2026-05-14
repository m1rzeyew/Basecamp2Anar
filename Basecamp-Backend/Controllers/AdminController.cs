using Basecamp_Backend.Data;
using Basecamp_Backend.Models;
using Basecamp_Backend.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Basecamp_Backend.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;

        public AdminController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _environment = environment;
        }

        [HttpGet("/Admin/Dashboard")]
        public IActionResult Dashboard()
        {
            return RedirectToAction(nameof(Users));
        }

        [HttpGet("/Admin/Users")]
        public async Task<IActionResult> Users()
        {
            await CreateRoleIfMissing("Admin");
            await CreateRoleIfMissing("Member");

            var users = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync();
            var model = new List<AdminUserVM>();

            foreach (var user in users)
            {
                model.Add(new AdminUserVM
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    IsAdmin = await _userManager.IsInRoleAsync(user, "Admin")
                });
            }

            return View(model);
        }

        [HttpPost("/Admin/Users/MakeAdmin/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            await CreateRoleIfMissing("Admin");

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost("/Admin/Users/RemoveAdmin/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction(nameof(Users));
            }

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            if (adminUsers.Count <= 1)
            {
                TempData["Error"] = "The last global Admin cannot be demoted.";
                return RedirectToAction(nameof(Users));
            }

            await _userManager.RemoveFromRoleAsync(user, "Admin");
            await CreateRoleIfMissing("Member");
            if (!await _userManager.IsInRoleAsync(user, "Member"))
            {
                await _userManager.AddToRoleAsync(user, "Member");
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost("/Admin/Users/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Global Admins cannot be deleted from this action. Demote them first, but never demote the last Admin.";
                return RedirectToAction(nameof(Users));
            }

            await DeleteProjectsOwnedByUserAsync(user.Id);

            user.IsDeleted = true;
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost("/admin/update-role/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(string id, string role)
        {
            return role == "Admin"
                ? await MakeAdmin(id)
                : await RemoveAdmin(id);
        }

        private async Task CreateRoleIfMissing(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        private async Task DeleteProjectsOwnedByUserAsync(string userId)
        {
            var ownedProjects = await _context.Projects
                .Include(p => p.Attachments)
                .Include(p => p.Members)
                .Where(p => p.Members.Any(m => m.AppUserId == userId && m.Role == "Owner"))
                .ToListAsync();

            foreach (var project in ownedProjects)
            {
                DeleteAttachmentFiles(project.Attachments);
            }

            _context.Projects.RemoveRange(ownedProjects);
            await _context.SaveChangesAsync();
        }

        private void DeleteAttachmentFiles(IEnumerable<Attachment> attachments)
        {
            var uploadRoot = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "uploads"));

            foreach (var attachment in attachments)
            {
                var relativePath = attachment.FilePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, relativePath));

                if (fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
        }
    }
}
