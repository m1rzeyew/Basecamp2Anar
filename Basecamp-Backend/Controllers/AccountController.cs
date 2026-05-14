using Basecamp_Backend.Data;
using Basecamp_Backend.Models;
using Basecamp_Backend.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Basecamp_Backend.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;

        public AccountController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            if (!ModelState.IsValid) return View(registerVM);

            await EnsureRoleAsync("Admin");
            await EnsureRoleAsync("Member");

            var user = new AppUser
            {
                FullName = registerVM.FullName.Trim(),
                Email = registerVM.Email.Trim(),
                UserName = registerVM.Username.Trim()
            };

            var result = await _userManager.CreateAsync(user, registerVM.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(registerVM);
            }

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var role = adminUsers.Any() ? "Member" : "Admin";
            await _userManager.AddToRoleAsync(user, role);

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM loginVM, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(loginVM);

            var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.UserName == loginVM.UsernameOrEmail || u.Email == loginVM.UsernameOrEmail);

            if (user is null || user.IsDeleted)
            {
                ModelState.AddModelError(string.Empty, "Username, email, or password is incorrect.");
                return View(loginVM);
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginVM.Password, false, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Username, email, or password is incorrect.");
                return View(loginVM);
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UserPage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            return View(user);
        }

        [Authorize]
        [HttpGet]
        public IActionResult DeleteAccount()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(DeleteAccountVM deleteAccountVM)
        {
            if (!ModelState.IsValid) return View(deleteAccountVM);

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.Id != currentUserId)
            {
                return Challenge();
            }

            if (!string.Equals(user.UserName, deleteAccountVM.UserName, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Username does not match the signed-in account.");
                return View(deleteAccountVM);
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, deleteAccountVM.Password);
            if (!passwordValid)
            {
                ModelState.AddModelError(string.Empty, "Password is incorrect.");
                return View(deleteAccountVM);
            }

            if (await IsLastAdminAsync(user))
            {
                ModelState.AddModelError(string.Empty, "You cannot delete the last global Admin account.");
                return View(deleteAccountVM);
            }

            await DeleteProjectsOwnedByUserAsync(user.Id);

            user.IsDeleted = true;
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(deleteAccountVM);
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        private async Task EnsureRoleAsync(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        private async Task<bool> IsLastAdminAsync(AppUser user)
        {
            if (!await _userManager.IsInRoleAsync(user, "Admin")) return false;

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            return adminUsers.Count <= 1;
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
