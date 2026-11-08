using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Helpers;
using PathForge.Models.Entities;

namespace PathForge.Controllers
{
    public class QuizController : Controller
    {
        private readonly PathForgeDbContext _db;

        public QuizController(PathForgeDbContext db) { _db = db; }

        // ── GET /Quiz/Create?courseId=5 ──────────────────────────────
        public IActionResult Create(int courseId)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");
            ViewBag.CourseId = courseId;
            return View();
        }

        // ── POST /Quiz/Create ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int courseId, string title,
            string? description, int timeLimitMinutes, int passMarkPercent)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var quiz = new Quiz
            {
                CourseId         = courseId,
                Title            = title,
                Description      = description,
                TimeLimitMinutes = timeLimitMinutes,
                PassMarkPercent  = passMarkPercent
            };
            _db.Quizzes.Add(quiz);
            await _db.SaveChangesAsync();

            return RedirectToAction("EditQuestions", new { id = quiz.Id });
        }

        // ── GET /Quiz/EditQuestions/5 — Add questions ────────────────
        public async Task<IActionResult> EditQuestions(int id)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var quiz = await _db.Quizzes
                .Include(q => q.Questions.OrderBy(qq => qq.DisplayOrder))
                    .ThenInclude(qq => qq.Options)
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound();
            return View(quiz);
        }

        // ── POST /Quiz/AddQuestion ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(int quizId, string questionText,
            int marks, string[] optionTexts, int correctIndex)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            int order = await _db.QuizQuestions
                .Where(q => q.QuizId == quizId)
                .CountAsync() + 1;

            var question = new QuizQuestion
            {
                QuizId       = quizId,
                QuestionText = questionText,
                Marks        = marks,
                DisplayOrder = order
            };
            _db.QuizQuestions.Add(question);
            await _db.SaveChangesAsync();

            for (int i = 0; i < optionTexts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(optionTexts[i]))
                {
                    _db.QuizOptions.Add(new QuizOption
                    {
                        QuestionId = question.Id,
                        OptionText = optionTexts[i],
                        IsCorrect  = (i == correctIndex)
                    });
                }
            }
            await _db.SaveChangesAsync();

            return RedirectToAction("EditQuestions", new { id = quizId });
        }

        // ── GET /Quiz/Take/5 — Student takes the quiz ────────────────
        public async Task<IActionResult> Take(int id)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Student"))
                return RedirectToAction("Login", "Auth");

            var quiz = await _db.Quizzes
                .Include(q => q.Questions.OrderBy(qq => qq.DisplayOrder))
                    .ThenInclude(qq => qq.Options)
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound();

            // Check if already attempted
            int userId = SessionHelper.GetUserId(HttpContext);
            ViewBag.Attempted = await _db.QuizAttempts
                .AnyAsync(a => a.QuizId == id && a.StudentId == userId);

            return View(quiz);
        }

        // ── POST /Quiz/SubmitQuiz — Auto-score and save attempt ──────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuiz(int quizId, IFormCollection form)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Student"))
                return RedirectToAction("Login", "Auth");

            int userId = SessionHelper.GetUserId(HttpContext);

            var quiz = await _db.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(qq => qq.Options)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            int totalQuestions = quiz.Questions.Count;
            int correctAnswers = 0;

            foreach (var question in quiz.Questions)
            {
                string key = $"q_{question.Id}";
                if (form.ContainsKey(key))
                {
                    if (int.TryParse(form[key], out int selectedOptionId))
                    {
                        var selectedOption = question.Options
                            .FirstOrDefault(o => o.Id == selectedOptionId);
                        if (selectedOption?.IsCorrect == true)
                            correctAnswers++;
                    }
                }
            }

            int scorePercent = totalQuestions > 0
                ? (int)Math.Round((double)correctAnswers / totalQuestions * 100)
                : 0;

            _db.QuizAttempts.Add(new QuizAttempt
            {
                QuizId         = quizId,
                StudentId      = userId,
                CorrectAnswers = correctAnswers,
                TotalQuestions = totalQuestions,
                ScorePercent   = scorePercent,
                Passed         = scorePercent >= quiz.PassMarkPercent
            });
            await _db.SaveChangesAsync();

            return RedirectToAction("Result", new { quizId, studentId = userId });
        }

        // ── GET /Quiz/Result — Show score ────────────────────────────
        public async Task<IActionResult> Result(int quizId, int studentId)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            var attempt = await _db.QuizAttempts
                .Include(a => a.Quiz)
                .OrderByDescending(a => a.AttemptedAt)
                .FirstOrDefaultAsync(a => a.QuizId == quizId && a.StudentId == studentId);

            if (attempt == null) return NotFound();
            return View(attempt);
        }
    }
}
