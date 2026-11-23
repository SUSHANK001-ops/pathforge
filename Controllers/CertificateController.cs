using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Helpers;

namespace PathForge.Controllers
{
    /// <summary>
    /// Shows a student's earned certificates and a single printable certificate.
    /// </summary>
    public class CertificateController : Controller
    {
        private readonly PathForgeDbContext _db;
        public CertificateController(PathForgeDbContext db) { _db = db; }

        // ── GET /Certificate — the current student's certificates ───
        public async Task<IActionResult> Index()
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            int userId = SessionHelper.GetUserId(HttpContext);

            var certs = await _db.Certificates
                .Include(c => c.Course).ThenInclude(co => co.Educator)
                .Where(c => c.StudentId == userId)
                .OrderByDescending(c => c.IssuedAt)
                .ToListAsync();

            return View(certs);
        }

        // ── GET /Certificate/View/5 — printable single certificate ──
        public async Task<IActionResult> View(int id)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            int userId  = SessionHelper.GetUserId(HttpContext);
            string role = SessionHelper.GetUserRole(HttpContext);

            var cert = await _db.Certificates
                .Include(c => c.Course).ThenInclude(co => co.Educator)
                .Include(c => c.Student)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cert == null) return NotFound();

            // Only the owner or an admin may view it.
            if (cert.StudentId != userId && role != "Admin") return Forbid();

            return View(cert);
        }
    }
}
