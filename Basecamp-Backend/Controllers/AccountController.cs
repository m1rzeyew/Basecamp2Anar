using Basecamp_Backend.Data;
using Basecamp_Backend.Models;
using Basecamp_Backend.ViewModels.Account;
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

        public AccountController(AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            if (!ModelState.IsValid) return View(registerVM);

            await CreateRoleIfMissing("Admin");
            await CreateRoleIfMissing("Member");

            var usersCount = await _userManager.Users.CountAsync();

            var usersCount = await _userManager.Users.CountAsync();

            AppUser user = new AppUser()
            {
                FullName = registerVM.FullName,
                Email = registerVM.Email,
                UserName = registerVM.Username
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

            var role = usersCount == 0 ? "Admin" : "Member";
            await _userManager.AddToRoleAsync(user, role);

            return RedirectToAction(nameof(Login));
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM loginVM, string? ReturnUrl = null)
        {
            if (!ModelState.IsValid) return View(loginVM);

            AppUser? user = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == loginVM.UsernameOrEmail || u.Email == loginVM.UsernameOrEmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Username, Email or Password is incorrect.");
                return View(loginVM);
            }

            await PrepareUserRole(user);

            var result = await _signInManager.PasswordSignInAsync(user, loginVM.Password, false, false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Username, Email or Password is incorrect.");
                return View(loginVM);
            }

            return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
        }

        public async Task<IActionResult> UserPage()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(user);
        }

        [HttpGet]
        public IActionResult DeleteAccount()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAccount(DeleteAccountVM deleteAccountVM)
        {
            var user = await _userManager.FindByNameAsync(deleteAccountVM.UserName);
            if (user == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (user.Id != userId)
            {
                ModelState.AddModelError(string.Empty, "Wrong User name please try again");
                return View(deleteAccountVM);
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, deleteAccountVM.Password);

            if (!passwordValid)
            {
                ModelState.AddModelError(string.Empty, "Please try again..");
                return View(deleteAccountVM);
            }

            await _userManager.DeleteAsync(user);
            await _signInManager.SignOutAsync();

            return RedirectToAction(nameof(Login));
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
        }

        private async Task PrepareUserRole(AppUser user)
        {
            await CreateRoleIfMissing("Admin");
            await CreateRoleIfMissing("Member");

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var userRoles = await _userManager.GetRolesAsync(user);

            if (!adminUsers.Any())
            {
                if (userRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, userRoles);
                }

                await _userManager.AddToRoleAsync(user, "Admin");
                return;
            }

            if (!userRoles.Any())
            {
                await _userManager.AddToRoleAsync(user, "Member");
            }
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
