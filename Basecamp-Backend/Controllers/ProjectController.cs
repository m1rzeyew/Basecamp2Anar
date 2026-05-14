using Basecamp_Backend.Data;
using Basecamp_Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Basecamp_Backend.Controllers
{
    public class ProjectController : Controller
    {
        private readonly AppDbContext _context; // Sizin DbContext-iniz
        private readonly UserManager<AppUser> _userManager;

        public ProjectController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Tasks)
                .Include(p => p.Members)
                .ThenInclude(m => m.AppUser)
                .Include(p => p.Attachments)
                .Include(p => p.Discussions)       
                .ThenInclude(d => d.AppUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();
            return View(project);
        }

        [HttpPost] // Metodun POST olduğunu bildirmək vacibdir
        public IActionResult QuickEdit([FromBody] Project model) // [FromBody] əlavə edildi
        {
            var project = _context.Projects.FirstOrDefault(p => p.Id == model.Id);

            if (project == null)
            {
                return Json(new { success = false, message = "Layihə tapılmadı" });
            }

            project.Name = model.Name;

            project.Description = model.Description;

            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult HardDelete(int id)
        {
            var project = _context.Projects.FirstOrDefault(p => p.Id == id);

            if (project == null)
            {
                return Json(new { success = false, message = "Layihə tapılmadı" });
            }

            // Hard Delete əməliyyatı: Bazadan tamamilə silinir
            _context.Projects.Remove(project);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> ManageProject(int id)
        {
            var project = await _context.Projects
        .Include(p => p.Members) // Üzvləri yüklə
            .ThenInclude(m => m.AppUser) // Üzvlərin istifadəçi məlumatlarını yüklə
        .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            return View(project);
        }
        // 3. General Settings: Ad və Təsviri yeniləmək
        [HttpPost]
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

        // 2. Üzv əlavə etmək (E-mail vasitəsilə)
        [HttpPost]
        public async Task<IActionResult> AddMember(int projectId, string email, bool isAdmin)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction("ManageProject", new { id = projectId });

            var role = isAdmin ? "Admin" : "Member";

            var member = new ProjectMember
            {
                ProjectId = projectId,
                AppUserId = user.Id,
                Role = role
            };

            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageProject", new { id = projectId });
        }
        // 5. Member Management: Üzvü layihədən çıxarmaq
        [HttpPost]
        public async Task<IActionResult> RemoveMember(int projectId, int userId)
        {
            // Layihə üzvünü silmə məntiqi
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleMemberRole(int projectId, string userId, bool makeAdmin)
        {
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.AppUserId == userId);

            if (member == null) return Json(new { success = false });

            //member.IsAdmin = makeAdmin; // Modelindəki sütun adına uyğunlaşdır
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }


        // 1. Yeni tapşırıq əlavə etmək
        [HttpPost]
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

            // Yeni yaranan task-ın ID-sini geri qaytarırıq ki, JS-də istifadə edək
            return Json(new { success = true, id = task.Id, title = task.Title });
        }

        // 2. Tapşırığın statusunu dəyişmək (Tamamlandı/Tamamlanmadı)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleTask(int taskId)
        {
            var task = await _context.projectTasks.FindAsync(taskId);
            if (task == null) return NotFound();

            task.IsCompleted = !task.IsCompleted;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isCompleted = task.IsCompleted });
        }

        // 3. Tapşırığı silmək
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            var task = await _context.projectTasks.FindAsync(taskId);
            if (task == null) return NotFound();

            _context.projectTasks.Remove(task);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Fayl yükləmək
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadAttachment(int projectId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Fayl seçilməyib" });

            // Faylı wwwroot/uploads/ qovluğuna saxla
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Unikal fayl adı (eyni adlı fayllar üst-üstə yazılmasın)
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new Attachment
            {
                FileName = file.FileName,               // Orijinal ad (göstərmək üçün)
                FilePath = "/uploads/" + uniqueFileName, // DB-də saxlanan path
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
                filePath = attachment.FilePath,
                fileType = attachment.FileType
            });
        }

        // Fayl silmək
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteAttachment(int attachmentId)
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);
            if (attachment == null)
                return Json(new { success = false });

            // Fiziki faylı da sil
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
                System.IO.File.Delete(physicalPath);

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Yeni mesaj göndərmək
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddDiscussion(int projectId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Json(new { success = false });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "Giriş edilməyib" });

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

        // Mesaj silmək
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteDiscussion(int discussionId)
        {
            var discussion = await _context.Discussions.FindAsync(discussionId);
            if (discussion == null)
                return Json(new { success = false });

            var user = await _userManager.GetUserAsync(User);

            // Yalnız öz mesajını silə bilər
            if (discussion.AppUserId != user?.Id)
                return Json(new { success = false, message = "İcazə yoxdur" });

            _context.Discussions.Remove(discussion);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
