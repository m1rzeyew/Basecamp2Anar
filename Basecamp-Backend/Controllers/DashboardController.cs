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

            var projects = _context.Projects.Include(p => p.Members).AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                projects = projects.Where(p => p.Members.Any(m => m.AppUserId == userId));
            }

            var userProjects = await projects.ToListAsync();

            var viewModel = new DashboardVM
            {
                AllProjects = userProjects,
                CreatedByMe = userProjects.Where(p => p.Members.Any(m => m.AppUserId == userId && m.Role == "Owner")).ToList(),
                SharedWithMe = userProjects.Where(p => p.Members.Any(m => m.AppUserId == userId && m.Role != "Owner")).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProject(string Name, string Description)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(Name))
            {
                TempData["Error"] = "Project name is required!";
                return RedirectToAction(nameof(Index));
            }

            var newProject = new Project
            {
                Name = Name,
                Description = Description
            };

            _context.Projects.Add(newProject);
            await _context.SaveChangesAsync();

            var projectMember = new ProjectMember
            {
                ProjectId = newProject.Id,
                AppUserId = userId,
                Role = "Owner"
            };

            _context.ProjectMembers.Add(projectMember);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Project created successfully!";

            return RedirectToAction(nameof(Index));
        }
    }
}
