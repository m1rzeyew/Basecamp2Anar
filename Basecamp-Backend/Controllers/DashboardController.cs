using Basecamp_Backend.Data;
using Basecamp_Backend.Models;
using Basecamp_Backend.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Basecamp_Backend.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Account");

            var userProjects = await _context.Projects
                .Include(p => p.Members)
                .Where(p => p.Members.Any(m => m.AppUserId == userId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var viewModel = new DashboardVM
            {
                AllProjects = userProjects,
                CreatedByMe = userProjects
                    .Where(p => p.Members.Any(m => m.AppUserId == userId && m.Role == "Owner"))
                    .ToList(),
                SharedWithMe = userProjects
                    .Where(p => p.Members.Any(m => m.AppUserId == userId && m.Role != "Owner"))
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(string name, string? description)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Project name is required.";
                return RedirectToAction(nameof(Index));
            }

            var newProject = new Project
            {
                Name = name.Trim(),
                Description = description?.Trim() ?? string.Empty
            };

            _context.Projects.Add(newProject);
            await _context.SaveChangesAsync();

            _context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = newProject.Id,
                AppUserId = user.Id,
                Role = "Owner"
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Project created successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
