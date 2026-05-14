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
        private static readonly Dictionary<string, string[]> AllowedUploadTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = ["image/png"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".pdf"] = ["application/pdf"],
            [".txt"] = ["text/plain", "application/octet-stream"]
        };

        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public ProjectController(AppDbContext context, UserManager<AppUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, int? selectedThreadId = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            if (!await IsProjectMemberAsync(id, userId)) return Forbid();

            var project = await _context.Projects
                .Include(p => p.Tasks)
                .Include(p => p.Members).ThenInclude(m => m.AppUser)
                .Include(p => p.Attachments).ThenInclude(a => a.UploadedByUser)
                .Include(p => p.Discussions).ThenInclude(d => d.AppUser)
                .Include(p => p.Threads).ThenInclude(t => t.CreatedByUser)
                .Include(p => p.Threads).ThenInclude(t => t.Messages).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            ViewBag.CurrentUserId = userId;
            ViewBag.CanManageProject = CanManage(project, userId);
            ViewBag.SelectedThreadId = selectedThreadId;

            return View(project);
        }

        [HttpGet]
        public async Task<IActionResult> ManageProject(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var project = await _context.Projects
                .Include(p => p.Members).ThenInclude(m => m.AppUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();
            if (!CanManage(project, userId)) return Forbid();

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickEdit([FromBody] Project model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(new { success = false, message = "Login required." });

            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (project == null) return NotFound(new { success = false, message = "Project not found." });
            if (!CanManage(project, userId)) return Forbid();

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return BadRequest(new { success = false, message = "Project name is required." });
            }

            project.Name = model.Name.Trim();
            project.Description = model.Description?.Trim() ?? string.Empty;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDelete(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(new { success = false, message = "Login required." });

            var project = await _context.Projects
                .Include(p => p.Members)
                .Include(p => p.Attachments)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound(new { success = false, message = "Project not found." });
            if (!CanManage(project, userId)) return Forbid();

            DeleteAttachmentFiles(project.Attachments);
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(int id, string name, string? description)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();
            if (!CanManage(project, userId)) return Forbid();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Project name is required.";
                return RedirectToAction(nameof(ManageProject), new { id });
            }

            project.Name = name.Trim();
            project.Description = description?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Project settings updated.";
            return RedirectToAction(nameof(ManageProject), new { id = project.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int projectId, string email, bool isAdmin = false, string? role = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();
            if (!CanManage(project, userId)) return Forbid();

            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null)
            {
                TempData["Error"] = "No user was found for that email.";
                return RedirectToAction(nameof(ManageProject), new { id = projectId });
            }

            if (project.Members.Any(m => m.AppUserId == user.Id))
            {
                TempData["Error"] = "That user is already a project member.";
                return RedirectToAction(nameof(ManageProject), new { id = projectId });
            }

            var projectRole = role == "Admin" || isAdmin ? "Admin" : "Member";

            _context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = projectId,
                AppUserId = user.Id,
                Role = projectRole
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ManageProject), new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int projectId, string userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized(new { success = false });

            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound(new { success = false });
            if (!CanManage(project, currentUserId)) return Forbid();

            var member = project.Members.FirstOrDefault(m => m.AppUserId == userId);
            if (member == null) return NotFound(new { success = false });
            if (member.Role == "Owner") return BadRequest(new { success = false, message = "Project owner cannot be removed." });

            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMemberRole(int projectId, string userId, string role)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized(new { success = false });

            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound(new { success = false });
            if (!CanManage(project, currentUserId)) return Forbid();

            if (role != "Admin" && role != "Member")
            {
                return BadRequest(new { success = false, message = "Invalid role." });
            }

            var member = project.Members.FirstOrDefault(m => m.AppUserId == userId);
            if (member == null) return NotFound(new { success = false });
            if (member.Role == "Owner") return BadRequest(new { success = false, message = "Owner role cannot be changed." });

            member.Role = role;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ToggleMemberRole(int projectId, string userId, bool makeAdmin)
        {
            return UpdateMemberRole(projectId, userId, makeAdmin ? "Admin" : "Member");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(int projectId, string title)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(new { success = false });
            if (!await IsProjectMemberAsync(projectId, userId)) return Forbid();

            if (string.IsNullOrWhiteSpace(title)) return BadRequest(new { success = false, message = "Task title is required." });

            var task = new ProjectTask
            {
                ProjectId = projectId,
                Title = title.Trim(),
                IsCompleted = false
            };

            _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = task.Id, title = task.Title });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTask(int taskId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(new { success = false });

            var task = await _context.ProjectTasks
                .Include(t => t.Project).ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return NotFound(new { success = false });
            if (!CanAccess(task.Project, userId)) return Forbid();

            task.IsCompleted = !task.IsCompleted;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isCompleted = task.IsCompleted });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(new { success = false });

            var task = await _context.ProjectTasks
                .Include(t => t.Project).ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return NotFound(new { success = false });
            if (!CanAccess(task.Project, userId)) return Forbid();

            _context.ProjectTasks.Remove(task);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(int projectId, IFormFile file)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(new { success = false, message = "Login required." });
            if (!await IsProjectMemberAsync(projectId, userId)) return Forbid();

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Choose a file to upload." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!IsAllowedUpload(extension, file.ContentType))
            {
                return BadRequest(new { success = false, message = "Allowed formats are PNG, JPG/JPEG, PDF, and TXT." });
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

            await using (var stream = new FileStream(physicalPath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new Attachment
            {
                FileName = Path.GetFileName(file.FileName),
                FilePath = $"/uploads/{uniqueFileName}",
                FileType = extension.TrimStart('.'),
                ContentType = file.ContentType,
                ProjectId = projectId,
                UploadedByUserId = userId
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);

            return Json(new
            {
                success = true,
                id = attachment.Id,
                fileName = attachment.FileName,
                filePath = attachment.FilePath,
                fileType = attachment.FileType,
                uploader = user?.FullName ?? "Unknown",
                createdAt = attachment.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int attachmentId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(new { success = false });

            var attachment = await _context.Attachments
                .Include(a => a.Project).ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(a => a.Id == attachmentId);

            if (attachment == null) return NotFound(new { success = false });

            var canDelete = CanManage(attachment.Project, userId) || attachment.UploadedByUserId == userId;
            if (!canDelete) return Forbid();

            DeleteAttachmentFiles([attachment]);
            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDiscussion(int projectId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(new { success = false, message = "Login required." });
            if (!await IsProjectMemberAsync(projectId, user.Id)) return Forbid();

            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(new { success = false, message = "Message content is required." });
            }

            var discussion = new Discussion
            {
                Content = content.Trim(),
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDiscussion(int discussionId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(new { success = false });

            var discussion = await _context.Discussions
                .Include(d => d.Project).ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(d => d.Id == discussionId);

            if (discussion == null) return NotFound(new { success = false });

            var canDelete = discussion.AppUserId == userId || CanManage(discussion.Project, userId);
            if (!canDelete) return Forbid();

            _context.Discussions.Remove(discussion);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateThread(int projectId, string title)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();
            if (!CanManage(project, userId)) return Forbid();

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Thread title is required.";
                return RedirectToAction(nameof(Details), new { id = projectId });
            }

            var thread = new ProjectThread
            {
                ProjectId = projectId,
                Title = title.Trim(),
                CreatedByUserId = userId,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ProjectThreads.Add(thread);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = projectId, selectedThreadId = thread.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditThread(int threadId, string title)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var thread = await _context.ProjectThreads
                .Include(t => t.Project).ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(t => t.Id == threadId);

            if (thread == null) return NotFound();
            if (!CanManage(thread.Project, userId)) return Forbid();

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Thread title is required.";
                return RedirectToAction(nameof(Details), new { id = thread.ProjectId, selectedThreadId = thread.Id });
            }

            thread.Title = title.Trim();
            thread.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = thread.ProjectId, selectedThreadId = thread.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteThread(int threadId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var thread = await _context.ProjectThreads
                .Include(t => t.Project).ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(t => t.Id == threadId);

            if (thread == null) return NotFound();
            if (!CanManage(thread.Project, userId)) return Forbid();

            var projectId = thread.ProjectId;
            _context.ProjectThreads.Remove(thread);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddThreadMessage(int projectThreadId, string content)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var thread = await _context.ProjectThreads
                .Include(t => t.Project).ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(t => t.Id == projectThreadId);

            if (thread == null) return NotFound();
            if (!CanAccess(thread.Project, userId)) return Forbid();

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Message content is required.";
                return RedirectToAction(nameof(Details), new { id = thread.ProjectId, selectedThreadId = thread.Id });
            }

            _context.ThreadMessages.Add(new ThreadMessage
            {
                ProjectThreadId = thread.Id,
                UserId = userId,
                Content = content.Trim(),
                UpdatedAt = DateTime.UtcNow
            });

            thread.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = thread.ProjectId, selectedThreadId = thread.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditThreadMessage(int messageId, string content)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var message = await _context.ThreadMessages
                .Include(m => m.ProjectThread).ThenInclude(t => t.Project).ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null) return NotFound();
            if (message.UserId != userId && !CanManage(message.ProjectThread.Project, userId)) return Forbid();

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Message content is required.";
                return RedirectToAction(nameof(Details), new { id = message.ProjectThread.ProjectId, selectedThreadId = message.ProjectThreadId });
            }

            message.Content = content.Trim();
            message.UpdatedAt = DateTime.UtcNow;
            message.ProjectThread.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = message.ProjectThread.ProjectId, selectedThreadId = message.ProjectThreadId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteThreadMessage(int messageId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var message = await _context.ThreadMessages
                .Include(m => m.ProjectThread).ThenInclude(t => t.Project).ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null) return NotFound();
            if (message.UserId != userId && !CanManage(message.ProjectThread.Project, userId)) return Forbid();

            var projectId = message.ProjectThread.ProjectId;
            var threadId = message.ProjectThreadId;
            _context.ThreadMessages.Remove(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = projectId, selectedThreadId = threadId });
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private async Task<bool> IsProjectMemberAsync(int projectId, string userId)
        {
            return await _context.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.AppUserId == userId);
        }

        private static bool CanAccess(Project project, string userId)
        {
            return project.Members.Any(m => m.AppUserId == userId);
        }

        private static bool CanManage(Project project, string userId)
        {
            return project.Members.Any(m =>
                m.AppUserId == userId &&
                (m.Role == "Owner" || m.Role == "Admin"));
        }

        private static bool IsAllowedUpload(string extension, string contentType)
        {
            if (!AllowedUploadTypes.TryGetValue(extension, out var allowedContentTypes))
            {
                return false;
            }

            var normalizedContentType = (contentType ?? string.Empty).Split(';')[0].Trim();
            return allowedContentTypes.Contains(normalizedContentType, StringComparer.OrdinalIgnoreCase);
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
