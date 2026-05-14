using Basecamp_Backend.Models;
<<<<<<< HEAD
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
=======
using Basecamp_Backend.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
>>>>>>> main

namespace Basecamp_Backend.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("/admin/users")]
        public async Task<IActionResult> Users()
        {
            await CreateRoleIfMissing("Admin");
            await CreateRoleIfMissing("Member");

<<<<<<< HEAD
            var users = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync();
            var adminIds = new List<string>();

            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    adminIds.Add(user.Id);
                }
            }

            ViewBag.AdminIds = adminIds;

            return View(users);
=======
            var users = _userManager.Users.OrderBy(u => u.UserName).ToList();
            var model = new List<AdminUserVM>();

            foreach (var user in users)
            {
                model.Add(new AdminUserVM
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    UserName = user.UserName,
                    Email = user.Email,
                    IsAdmin = await _userManager.IsInRoleAsync(user, "Admin")
                });
            }

            return View(model);
>>>>>>> main
        }

        [HttpPost("/admin/update-role/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(string id, string role)
        {
            if (role != "Admin" && role != "Member")
            {
                return RedirectToAction(nameof(Users));
            }

            await CreateRoleIfMissing("Admin");
            await CreateRoleIfMissing("Member");

            var user = await _userManager.FindByIdAsync(id);

<<<<<<< HEAD
            if (user == null)
=======
            if (user is null)
>>>>>>> main
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, roles);
            }

            await _userManager.AddToRoleAsync(user, role);

            return RedirectToAction(nameof(Users));
        }

        private async Task CreateRoleIfMissing(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
