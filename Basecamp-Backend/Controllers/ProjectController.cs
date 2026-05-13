using Basecamp_Backend.Data;
using Basecamp_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Basecamp_Backend.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ProjectController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.Projects
                .Include(p => p.Tasks)
                .Include(p => p.Members)
                .ThenInclude(m => m.AppUser)
                .Include(p => p.Attachments)
                .Include(p => p.Discussions)
                .ThenInclude(d => d.AppUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            if (!User.IsInRole("Admin") && !project.Members.Any(m => m.AppUserId == userId))
            {
                return Forbid();
            }

            return View(project);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult QuickEdit([FromBody] Project model)
        {
            var project = _context.Projects.FirstOrDefault(p => p.Id == model.Id);

            if (project == null)
            {
                return Json(new { success = false, message = "Project was not found." });
            }

            project.Name = model.Name;
            project.Description = model.Description;

            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult HardDelete(int id)
        {
            var project = _context.Projects.FirstOrDefault(p => p.Id == id);

            if (project == null)
            {
                return Json(new { success = false, message = "Project was not found." });
            }

            _context.Projects.Remove(project);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageProject(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .ThenInclude(m => m.AppUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            return View(project);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSettings(int id, string Name, string Description)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            project.Name = Name;
            project.Description = Description;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Project settings updated!";

            return RedirectToAction("ManageProject", new { id = project.Id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddMember(int projectId, string email, bool isAdmin)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction("ManageProject", new { id = projectId });

            var existingMember = await _context.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.AppUserId == user.Id);

            if (existingMember != null)
            {
                return RedirectToAction("ManageProject", new { id = projectId });
            }

            var member = new ProjectMember
            {
                ProjectId = projectId,
                AppUserId = user.Id,
                Role = isAdmin ? "Admin" : "Member"
            };

            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageProject", new { id = projectId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveMember(int projectId, string userId)
        {
            var member = await _context.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.AppUserId == userId);

            if (member == null)
            {
                return Json(new { success = false, message = "Member was not found." });
            }

            if (member.Role == "Owner")
            {
                return Json(new { success = false, message = "Owner cannot be removed." });
            }

            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMemberRole(int projectId, string userId, string role)
        {
            if (role != "Admin" && role != "Member")
            {
                return Json(new { success = false, message = "Role is not valid." });
            }

            var member = await _context.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.AppUserId == userId);

            if (member == null)
            {
                return Json(new { success = false, message = "Member was not found." });
            }

            if (member.Role == "Owner")
            {
                return Json(new { success = false, message = "Owner role cannot be changed." });
            }

            member.Role = role;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddTask(int projectId, string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return BadRequest();

            var task = new ProjectTask
            {
                ProjectId = projectId,
                Title = title,
                IsCompleted = false
            };

            _context.projectTasks.Add(task);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = task.Id, title = task.Title });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleTask(int taskId)
        {
            var task = await _context.projectTasks.FindAsync(taskId);
            if (task == null) return NotFound();

            task.IsCompleted = !task.IsCompleted;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isCompleted = task.IsCompleted });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            var task = await _context.projectTasks.FindAsync(taskId);
            if (task == null) return NotFound();

            _context.projectTasks.Remove(task);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadAttachment(int projectId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file selected." });
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new Attachment
            {
                FileName = file.FileName,
                FilePath = "/uploads/" + uniqueFileName,
                FileType = Path.GetExtension(file.FileName).ToLower(),
                ProjectId = projectId
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = attachment.Id,
                fileName = attachment.FileName,
                fileType = attachment.FileType,
                openUrl = Url.Action("OpenAttachment", "Project", new { id = attachment.Id }),
                downloadUrl = Url.Action("DownloadAttachment", "Project", new { id = attachment.Id })
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteAttachment(int attachmentId)
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);

            if (attachment == null)
            {
                return Json(new { success = false, message = "Attachment was not found." });
            }

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));

            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> OpenAttachment(int id)
        {
            var attachment = await _context.Attachments.FindAsync(id);

            if (attachment == null)
            {
                return NotFound("Attachment was not found.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var canAccess = User.IsInRole("Admin") || await _context.ProjectMembers.AnyAsync(m => m.ProjectId == attachment.ProjectId && m.AppUserId == userId);

            if (!canAccess)
            {
                return Forbid();
            }

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));

            if (!System.IO.File.Exists(physicalPath))
            {
                return NotFound("File is missing from the server.");
            }

            var contentType = GetPreviewContentType(attachment.FileType);

            if (contentType == null)
            {
                return BadRequest("This file format cannot be opened in the browser.");
            }

            return PhysicalFile(physicalPath, contentType);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var attachment = await _context.Attachments.FindAsync(id);

            if (attachment == null)
            {
                return NotFound("Attachment was not found.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var canAccess = User.IsInRole("Admin") || await _context.ProjectMembers.AnyAsync(m => m.ProjectId == attachment.ProjectId && m.AppUserId == userId);

            if (!canAccess)
            {
                return Forbid();
            }

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));

            if (!System.IO.File.Exists(physicalPath))
            {
                return NotFound("File is missing from the server.");
            }

            var contentType = GetFileContentType(attachment.FileType);

            return PhysicalFile(physicalPath, contentType, attachment.FileName);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddDiscussion(int projectId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Message cannot be empty." });
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Json(new { success = false, message = "Please log in." });
            }

            var canViewProject = User.IsInRole("Admin") || await _context.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.AppUserId == user.Id);

            if (!canViewProject)
            {
                return Json(new { success = false, message = "Access denied." });
            }

            var discussion = new Discussion
            {
                Content = content,
                ProjectId = projectId,
                AppUserId = user.Id
            };

            _context.Discussions.Add(discussion);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = discussion.Id,
                content = discussion.Content,
                fullName = user.FullName,
                createdAt = discussion.CreatedAt.ToString("dd MMM, HH:mm")
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteDiscussion(int discussionId)
        {
            var discussion = await _context.Discussions.FindAsync(discussionId);

            if (discussion == null)
            {
                return Json(new { success = false, message = "Message was not found." });
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Json(new { success = false, message = "Please log in." });
            }

            _context.Discussions.Remove(discussion);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        private string? GetPreviewContentType(string fileType)
        {
            if (fileType == ".pdf") return "application/pdf";
            if (fileType == ".png") return "image/png";
            if (fileType == ".jpg") return "image/jpeg";
            if (fileType == ".jpeg") return "image/jpeg";
            if (fileType == ".gif") return "image/gif";
            if (fileType == ".txt") return "text/plain";

            return null;
        }

        private string GetFileContentType(string fileType)
        {
            if (fileType == ".pdf") return "application/pdf";
            if (fileType == ".png") return "image/png";
            if (fileType == ".jpg") return "image/jpeg";
            if (fileType == ".jpeg") return "image/jpeg";
            if (fileType == ".gif") return "image/gif";
            if (fileType == ".txt") return "text/plain";
            if (fileType == ".doc") return "application/msword";
            if (fileType == ".docx") return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            if (fileType == ".xls") return "application/vnd.ms-excel";
            if (fileType == ".xlsx") return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return "application/octet-stream";
        }
    }
}
