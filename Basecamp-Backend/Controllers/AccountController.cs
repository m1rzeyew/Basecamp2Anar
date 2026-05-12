using Basecamp_Backend.Data;
using Basecamp_Backend.Models;
using Basecamp_Backend.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json.Serialization;

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
            if (!ModelState.IsValid) return View();

            AppUser user = new AppUser()
            {
                FullName = registerVM.FullName,
                Email = registerVM.Email,
                UserName = registerVM.Username,
            };

            var result = await _userManager.CreateAsync(user, registerVM.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    return View();
                }
            }
            //await _userManager.AddToRoleAsync(user, UserRole.Member.ToString());

            return RedirectToAction(nameof(Login));
        }
       
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM loginVM, string ReturnUrl = null)
        {
            if (!ModelState.IsValid) return View(loginVM);

            AppUser? user = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == loginVM.UsernameOrEmail || u.Email == loginVM.UsernameOrEmail);

            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Username, Email or Password is incorrect.");
                return View(loginVM);
            }


            var result = await _signInManager.PasswordSignInAsync(user, loginVM.Password,false,false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Username, Email or Password is incorrect.");
                return View(loginVM);
            }
 
            return RedirectToAction(nameof(DashboardController.Index), "dashboard");
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
                ModelState.AddModelError(string.Empty,"Wrong User name please try again");
                return View();
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, deleteAccountVM.Password);
            if (!passwordValid)
            {
                ModelState.AddModelError(string.Empty, "Please try again..");
                return View();
            }

            await _userManager.DeleteAsync(user);
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(DashboardController.Index), "dashboard");
        }

    }
}
