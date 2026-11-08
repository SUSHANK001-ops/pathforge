using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Helpers;
using PathForge.Models.Entities;

namespace PathForge.Controllers
{
    public class DiscussionController : Controller
    {
        private readonly PathForgeDbContext _db;

        public DiscussionController(PathForgeDbContext db) { _db = db; }

        // ── GET /Discussion/Index/courseId — Thread list ─────────────
        public async Task<IActionResult> Index(int courseId)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            var course = await _db.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var threads = await _db.DiscussionPosts
                .Include(p => p.Author)
                .Include(p => p.Replies)
                .Where(p => p.CourseId == courseId && p.ParentPostId == null)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Course   = course;
            ViewBag.UserId   = SessionHelper.GetUserId(HttpContext);
            ViewBag.UserRole = SessionHelper.GetUserRole(HttpContext);

            return View(threads);
        }

        // ── POST /Discussion/CreateThread ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateThread(int courseId, string title, string body)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            _db.DiscussionPosts.Add(new DiscussionPost
            {
                CourseId     = courseId,
                AuthorId     = SessionHelper.GetUserId(HttpContext),
                Title        = title,
                Body         = body,
                ParentPostId = null
            });
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", new { courseId });
        }

        // ── GET /Discussion/Thread/5 — View thread + replies ─────────
        public async Task<IActionResult> Thread(int id)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            var thread = await _db.DiscussionPosts
                .Include(p => p.Author)
                .Include(p => p.Course)
                .Include(p => p.Replies.OrderBy(r => r.CreatedAt))
                    .ThenInclude(r => r.Author)
                .FirstOrDefaultAsync(p => p.Id == id && p.ParentPostId == null);

            if (thread == null) return NotFound();

            ViewBag.UserId   = SessionHelper.GetUserId(HttpContext);
            ViewBag.UserRole = SessionHelper.GetUserRole(HttpContext);

            return View(thread);
        }

        // ── POST /Discussion/Reply ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int threadId, int courseId, string body)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            _db.DiscussionPosts.Add(new DiscussionPost
            {
                CourseId     = courseId,
                AuthorId     = SessionHelper.GetUserId(HttpContext),
                Title        = "Re: thread",
                Body         = body,
                ParentPostId = threadId
            });
            await _db.SaveChangesAsync();

            return RedirectToAction("Thread", new { id = threadId });
        }

        // ── POST /Discussion/Delete/5 — Admin or author can delete ───
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            var post = await _db.DiscussionPosts.FindAsync(id);
            if (post == null) return NotFound();

            int userId = SessionHelper.GetUserId(HttpContext);
            string role = SessionHelper.GetUserRole(HttpContext);

            if (post.AuthorId != userId && role != "Admin")
                return Forbid();

            int courseId = post.CourseId;
            _db.DiscussionPosts.Remove(post);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", new { courseId });
        }
    }
}
