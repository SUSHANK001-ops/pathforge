using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Models.Entities;
using PathForge.Models.ViewModels;

namespace PathForge.Controllers
{
    public class AuthController : Controller
    {
        private readonly PathForgeDbContext _db;

        public AuthController(PathForgeDbContext db)
        {
            _db = db;
        }

        // ── GET /Auth/Register ──────────────────────────────────────
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("UserId") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // ── POST /Auth/Register ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Block duplicate emails
            bool emailTaken = await _db.Users.AnyAsync(u => u.Email == model.Email);
            if (emailTaken)
            {
                ModelState.AddModelError("Email", "An account with this email already exists");
                return View(model);
            }

            // Only allow Student or Educator from the registration form
            if (model.Role != "Student" && model.Role != "Educator")
                model.Role = "Student";

            var user = new User
            {
                FullName     = model.FullName,
                Email        = model.Email,
                Role         = model.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Account created. Please log in.";
            return RedirectToAction("Login");
        }

        // ── GET /Auth/Login ─────────────────────────────────────────
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserId") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // ── POST /Auth/Login ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Incorrect email or password");
                return View(model);
            }

            // Write session values
            HttpContext.Session.SetString("UserId",    user.Id.ToString());
            HttpContext.Session.SetString("UserName",  user.FullName);
            HttpContext.Session.SetString("UserRole",  user.Role);
            HttpContext.Session.SetString("UserEmail", user.Email);

            // Redirect by role
            if (user.Role == "Admin")
                return RedirectToAction("Index", "Admin");

            return RedirectToAction("Dashboard", "Home");
        }

        // ── GET /Auth/Logout ────────────────────────────────────────
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ── GET /Auth/Profile ───────────────────────────────────────
        public async Task<IActionResult> Profile()
        {
            int? userId = GetSessionUserId();
            if (userId == null) return RedirectToAction("Login");

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");

            // ── Stats for the right panel ────────────────────────────
            var enrolments = await _db.CourseEnrolments
                .Where(e => e.StudentId == userId)
                .Select(e => e.CourseId)
                .ToListAsync();

            int completed = 0;
            foreach (var cid in enrolments)
            {
                var total = await _db.Modules.CountAsync(m => m.CourseId == cid);
                var done  = await _db.StudentProgress
                    .CountAsync(p => p.StudentId == userId && p.ModuleId != 0 &&
                                    _db.Modules.Any(m => m.Id == p.ModuleId && m.CourseId == cid)
                                    && p.Status == "Completed");
                if (total > 0 && done == total) completed++;
            }

            var vm = new ProfileViewModel
            {
                Id                 = user.Id,
                FullName           = user.FullName,
                Email              = user.Email,
                Role               = user.Role,
                Bio                = user.Bio,
                JoinedAt           = user.CreatedAt,
                CoursesEnrolled    = enrolments.Count,
                CoursesCompleted   = completed,
                CertificatesEarned = await _db.Certificates.CountAsync(c => c.StudentId == userId),
                QuizzesPassed      = await _db.QuizAttempts.CountAsync(a => a.StudentId == userId && a.Passed),
                AssignmentsGraded  = await _db.AssignmentSubmissions
                                        .CountAsync(s => s.StudentId == userId && s.Status == "Graded")
            };
            return View(vm);
        }

        // ── POST /Auth/Profile ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            int? userId = GetSessionUserId();
            if (userId == null) return RedirectToAction("Login");

            // Clear password fields from validation if not changing password
            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                ModelState.Remove("NewPassword");
                ModelState.Remove("ConfirmNewPassword");
            }

            if (!ModelState.IsValid)
                return View(model);

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");

            user.FullName = model.FullName;
            user.Bio      = model.Bio;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            await _db.SaveChangesAsync();

            // Update session name in case it changed
            HttpContext.Session.SetString("UserName", user.FullName);

            TempData["Success"] = "Profile updated successfully";
            return RedirectToAction("Profile");
        }

        // ── HELPER — get int user ID from session ──────────────────
        private int? GetSessionUserId()
        {
            string? val = HttpContext.Session.GetString("UserId");
            return int.TryParse(val, out int id) ? id : null;
        }
    }
}
