using Basecamp_Backend.Data;
using Basecamp_Backend.Models;
using Basecamp_Backend.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Basecamp_Backend.Controllers
{
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
