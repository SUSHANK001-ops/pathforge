# PHASE 2 — AUTHENTICATION & USER MANAGEMENT
### PathForge · Week 2 of 5

> **Prerequisite:** Phase 1 complete. Database exists. CSS loads correctly.
> Every class name in views must use the `pf-` prefix from `site.css`.

---

## WHAT YOU BUILD THIS WEEK

- Register page (new user sign-up with role selection)
- Login page (email + password, session creation)
- Logout action
- Profile page (view + edit personal details)
- Role-based access helper (blocks wrong-role users)
- Password hashing with BCrypt

---

## STEP 1 — VIEW MODELS

These carry data between the form and the controller.
Create each file inside `Models/ViewModels/`.

**`Models/ViewModels/LoginViewModel.cs`**
```csharp
using System.ComponentModel.DataAnnotations;

namespace PathForge.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
```

**`Models/ViewModels/RegisterViewModel.cs`**
```csharp
using System.ComponentModel.DataAnnotations;

namespace PathForge.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        // Role: "Student" or "Educator" (Admin created manually)
        [Required(ErrorMessage = "Please select a role")]
        public string Role { get; set; } = "Student";

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
```

**`Models/ViewModels/ProfileViewModel.cs`**
```csharp
using System.ComponentModel.DataAnnotations;

namespace PathForge.Models.ViewModels
{
    public class ProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        // Email shown but not editable
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Bio { get; set; }

        // Optional password change — blank means no change
        [MinLength(8, ErrorMessage = "New password must be at least 8 characters")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string? ConfirmNewPassword { get; set; }
    }
}
```

---

## STEP 2 — AUTH CONTROLLER

**`Controllers/AuthController.cs`**
```csharp
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
            HttpContext.Session.SetString("UserId",   user.Id.ToString());
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserEmail",user.Email);

            // Redirect by role
            if (user.Role == "Admin")
                return RedirectToAction("Index", "Admin");

            return RedirectToAction("Index", "Home");
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

            var vm = new ProfileViewModel
            {
                Id       = user.Id,
                FullName = user.FullName,
                Email    = user.Email,
                Role     = user.Role,
                Bio      = user.Bio
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
```

---

## STEP 3 — ROLE ACCESS HELPER

Create a static helper that every controller can call to block unauthorized users.
Put it in a new folder `Helpers/`.

**`Helpers/SessionHelper.cs`**
```csharp
namespace PathForge.Helpers
{
    public static class SessionHelper
    {
        // Returns true and sets a TempData message if the user should be blocked
        public static bool RequireLogin(HttpContext ctx, TempDataDictionary tempData)
        {
            if (string.IsNullOrEmpty(ctx.Session.GetString("UserId")))
            {
                tempData["Error"] = "Please log in to access this page";
                return true; // blocked
            }
            return false;
        }

        public static bool RequireRole(
            HttpContext ctx,
            TempDataDictionary tempData,
            params string[] roles)
        {
            if (RequireLogin(ctx, tempData)) return true;

            string userRole = ctx.Session.GetString("UserRole") ?? "";
            if (!roles.Contains(userRole))
            {
                tempData["Error"] = "You do not have permission to access this page";
                return true; // blocked
            }
            return false;
        }

        public static int GetUserId(HttpContext ctx)
            => int.Parse(ctx.Session.GetString("UserId") ?? "0");

        public static string GetUserRole(HttpContext ctx)
            => ctx.Session.GetString("UserRole") ?? "";
    }
}
```

**How to use this in any controller:**
```csharp
// Block non-logged-in users
if (SessionHelper.RequireLogin(HttpContext, TempData))
    return RedirectToAction("Login", "Auth");

// Block anyone who is not an Educator or Admin
if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
    return RedirectToAction("Index", "Home");
```

---

## STEP 4 — REGISTER VIEW

**`Views/Auth/Register.cshtml`**
```html
@model PathForge.Models.ViewModels.RegisterViewModel
@{
    ViewData["Title"] = "Create Account";
    Layout = "_Layout";
}

<section class="pf-section-light" style="min-height:100vh; display:flex; align-items:center; justify-content:center; padding:3rem 1rem;">

  <div class="pf-form-panel">

    {{!-- Panel header --}}
    <div class="pf-form-panel__header">
      <span class="pf-label">PathForge</span>
      <h1 class="pf-section-header" style="margin-top:0.5rem;">Create your account</h1>
      <p class="pf-body" style="margin-top:0.5rem; font-size:0.85rem; opacity:0.7;">
        Start your learning path today.
      </p>
    </div>

    {{!-- Validation summary --}}
    @if (!ViewData.ModelState.IsValid)
    {
      <div class="pf-alert pf-alert--error" style="margin-bottom:1.25rem;">
        <asp-validation-summary asp-validation-summary="All"></asp-validation-summary>
      </div>
    }

    <form asp-action="Register" asp-controller="Auth" method="post">
      @Html.AntiForgeryToken()

      {{!-- Full Name --}}
      <div class="pf-form-group">
        <label asp-for="FullName" class="pf-form-label">Full Name</label>
        <input asp-for="FullName" class="pf-input" placeholder="Jane Doe" />
        <span asp-validation-for="FullName" class="pf-field-error"></span>
      </div>

      {{!-- Email --}}
      <div class="pf-form-group">
        <label asp-for="Email" class="pf-form-label">Email Address</label>
        <input asp-for="Email" class="pf-input" type="email" placeholder="you@example.com" />
        <span asp-validation-for="Email" class="pf-field-error"></span>
      </div>

      {{!-- Role --}}
      <div class="pf-form-group">
        <label asp-for="Role" class="pf-form-label">I am a</label>
        <select asp-for="Role" class="pf-input pf-select">
          <option value="Student">Student</option>
          <option value="Educator">Educator</option>
        </select>
        <span asp-validation-for="Role" class="pf-field-error"></span>
      </div>

      {{!-- Password --}}
      <div class="pf-form-group">
        <label asp-for="Password" class="pf-form-label">Password</label>
        <input asp-for="Password" class="pf-input" type="password" placeholder="Min 8 characters" />
        <span asp-validation-for="Password" class="pf-field-error"></span>
      </div>

      {{!-- Confirm Password --}}
      <div class="pf-form-group">
        <label asp-for="ConfirmPassword" class="pf-form-label">Confirm Password</label>
        <input asp-for="ConfirmPassword" class="pf-input" type="password" placeholder="Repeat password" />
        <span asp-validation-for="ConfirmPassword" class="pf-field-error"></span>
      </div>

      <button type="submit" class="pf-btn" style="width:100%; margin-top:0.5rem;">
        Create Account
      </button>
    </form>

    <p class="pf-body" style="margin-top:1.25rem; font-size:0.8rem; text-align:center;">
      Already registered? <a asp-action="Login" asp-controller="Auth">Log In</a>
    </p>

  </div>

</section>

@section Scripts {
  <partial name="_ValidationScriptsPartial" />
}
```

---

## STEP 5 — LOGIN VIEW

**`Views/Auth/Login.cshtml`**
```html
@model PathForge.Models.ViewModels.LoginViewModel
@{
    ViewData["Title"] = "Log In";
    Layout = "_Layout";
}

<section class="pf-section-light" style="min-height:100vh; display:flex; align-items:center; justify-content:center; padding:3rem 1rem;">

  <div class="pf-form-panel">

    <div class="pf-form-panel__header">
      <span class="pf-label">PathForge</span>
      <h1 class="pf-section-header" style="margin-top:0.5rem;">Welcome back</h1>
      <p class="pf-body" style="margin-top:0.5rem; font-size:0.85rem; opacity:0.7;">
        Log in to continue your path.
      </p>
    </div>

    {{!-- TempData success from registration --}}
    @if (TempData["Success"] != null)
    {
      <div class="pf-alert pf-alert--success" style="margin-bottom:1rem;">
        @TempData["Success"]
      </div>
    }

    @if (!ViewData.ModelState.IsValid)
    {
      <div class="pf-alert pf-alert--error" style="margin-bottom:1rem;">
        <asp-validation-summary asp-validation-summary="All"></asp-validation-summary>
      </div>
    }

    <form asp-action="Login" asp-controller="Auth" method="post">
      @Html.AntiForgeryToken()

      <div class="pf-form-group">
        <label asp-for="Email" class="pf-form-label">Email Address</label>
        <input asp-for="Email" class="pf-input" type="email" placeholder="you@example.com" />
        <span asp-validation-for="Email" class="pf-field-error"></span>
      </div>

      <div class="pf-form-group">
        <label asp-for="Password" class="pf-form-label">Password</label>
        <input asp-for="Password" class="pf-input" type="password" placeholder="••••••••" />
        <span asp-validation-for="Password" class="pf-field-error"></span>
      </div>

      <button type="submit" class="pf-btn" style="width:100%; margin-top:0.5rem;">
        Log In
      </button>
    </form>

    <p class="pf-body" style="margin-top:1.25rem; font-size:0.8rem; text-align:center;">
      Don't have an account? <a asp-action="Register" asp-controller="Auth">Register</a>
    </p>

  </div>

</section>

@section Scripts {
  <partial name="_ValidationScriptsPartial" />
}
```

---

## STEP 6 — PROFILE VIEW

**`Views/Auth/Profile.cshtml`**
```html
@model PathForge.Models.ViewModels.ProfileViewModel
@{
    ViewData["Title"] = "My Profile";
    Layout = "_Layout";
}

<div class="pf-page" style="padding-top:3rem; padding-bottom:3rem;">

  {{!-- Page header --}}
  <div style="border-bottom:var(--border-outer); padding-bottom:1.5rem; margin-bottom:2.5rem;">
    <span class="pf-label">Account</span>
    <h1 class="pf-section-header" style="margin-top:0.4rem;">My Profile</h1>
    <p class="pf-body" style="opacity:0.6; margin-top:0.35rem;">
      @Model.Email &nbsp;·&nbsp;
      <span class="pf-status pf-status--live">@Model.Role</span>
    </p>
  </div>

  @if (TempData["Success"] != null)
  {
    <div class="pf-alert pf-alert--success" style="margin-bottom:1.5rem;">
      @TempData["Success"]
    </div>
  }

  <div style="display:grid; grid-template-columns:1fr 1fr; gap:3rem; align-items:start;">

    {{!-- Edit form --}}
    <div>
      <h2 class="pf-card-title" style="margin-bottom:1.25rem;">Edit Details</h2>

      <form asp-action="Profile" asp-controller="Auth" method="post">
        @Html.AntiForgeryToken()
        <input type="hidden" asp-for="Id" />

        <div class="pf-form-group">
          <label asp-for="FullName" class="pf-form-label">Full Name</label>
          <input asp-for="FullName" class="pf-input" />
          <span asp-validation-for="FullName" class="pf-field-error"></span>
        </div>

        <div class="pf-form-group">
          <label class="pf-form-label">Email Address (cannot change)</label>
          <input type="text" class="pf-input" value="@Model.Email" disabled
                 style="opacity:0.5; cursor:not-allowed;" />
        </div>

        <div class="pf-form-group">
          <label asp-for="Bio" class="pf-form-label">Bio</label>
          <textarea asp-for="Bio" class="pf-input pf-textarea"
                    placeholder="Tell others about yourself..."></textarea>
        </div>

        <h3 class="pf-card-title" style="margin:1.5rem 0 1rem; font-size:1.1rem;">
          Change Password
        </h3>
        <p class="pf-body" style="font-size:0.8rem; opacity:0.6; margin-bottom:1rem;">
          Leave blank to keep your current password.
        </p>

        <div class="pf-form-group">
          <label asp-for="NewPassword" class="pf-form-label">New Password</label>
          <input asp-for="NewPassword" class="pf-input" type="password"
                 placeholder="Min 8 characters" />
          <span asp-validation-for="NewPassword" class="pf-field-error"></span>
        </div>

        <div class="pf-form-group">
          <label asp-for="ConfirmNewPassword" class="pf-form-label">Confirm New Password</label>
          <input asp-for="ConfirmNewPassword" class="pf-input" type="password" />
          <span asp-validation-for="ConfirmNewPassword" class="pf-field-error"></span>
        </div>

        <div style="display:flex; gap:0.75rem; margin-top:1.5rem;">
          <button type="submit" class="pf-btn">Save Changes</button>
          <a href="/" class="pf-btn pf-btn--ghost">Cancel</a>
        </div>
      </form>
    </div>

    {{!-- Info panel (fin-grid cell style) --}}
    <div class="pf-stat-grid" style="align-self:start;">
      <div class="pf-stat-cell">
        <span class="pf-stat-cell__label">Role</span>
        <span class="pf-stat-cell__number" style="font-size:1.5rem;">@Model.Role</span>
      </div>
      <div class="pf-stat-cell">
        <span class="pf-stat-cell__label">Member Since</span>
        <span class="pf-stat-cell__number" style="font-size:1.25rem;">
          @* Pull from controller if needed *@
          2026
        </span>
      </div>
    </div>

  </div>

</div>

@section Scripts {
  <partial name="_ValidationScriptsPartial" />
}
```

---

## STEP 7 — CLIENT-SIDE FORM VALIDATION (site.js)

Put this inside `wwwroot/js/site.js`.
This validates the form before it is even sent to the server.

```javascript
/* PathForge — site.js
   Client-side form validation and general UI behaviour */

document.addEventListener('DOMContentLoaded', function () {

  // ── 1. Real-time field validation ──────────────────────────
  // On every pf-input blur, check it is not empty
  document.querySelectorAll('.pf-input[required]').forEach(function (input) {
    input.addEventListener('blur', function () {
      validateField(input);
    });
    input.addEventListener('input', function () {
      clearError(input);
    });
  });

  // ── 2. Pre-submit validation ────────────────────────────────
  document.querySelectorAll('form').forEach(function (form) {
    form.addEventListener('submit', function (e) {
      let valid = true;

      form.querySelectorAll('.pf-input[required]').forEach(function (input) {
        if (!validateField(input)) valid = false;
      });

      // Password match check
      const pw  = form.querySelector('input[name="Password"]');
      const cpw = form.querySelector('input[name="ConfirmPassword"]');
      if (pw && cpw && pw.value !== cpw.value) {
        showError(cpw, 'Passwords do not match');
        valid = false;
      }

      // Email format
      const email = form.querySelector('input[type="email"]');
      if (email && email.value && !isValidEmail(email.value)) {
        showError(email, 'Enter a valid email address');
        valid = false;
      }

      if (!valid) e.preventDefault();
    });
  });

  // ── 3. Reading progress bar ─────────────────────────────────
  const strip = document.getElementById('readProgress');
  if (strip) {
    window.addEventListener('scroll', function () {
      const scrolled  = window.scrollY;
      const docHeight = document.documentElement.scrollHeight - window.innerHeight;
      const pct       = docHeight > 0 ? (scrolled / docHeight) * 100 : 0;
      strip.style.width = pct + '%';
    });
  }

  // ── Helpers ─────────────────────────────────────────────────
  function validateField(input) {
    if (!input.value.trim()) {
      const label = input.previousElementSibling
                    ? input.previousElementSibling.textContent.trim()
                    : 'This field';
      showError(input, label + ' is required');
      return false;
    }
    clearError(input);
    return true;
  }

  function showError(input, message) {
    clearError(input);
    input.style.borderColor = 'var(--red)';
    const err = document.createElement('span');
    err.className = 'pf-field-error js-error';
    err.textContent = message;
    input.insertAdjacentElement('afterend', err);
  }

  function clearError(input) {
    input.style.borderColor = '';
    const next = input.nextElementSibling;
    if (next && next.classList.contains('js-error')) next.remove();
  }

  function isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }

});
```

---

## STEP 8 — HOME CONTROLLER (update with session check)

Update the existing `Controllers/HomeController.cs` to pass session info to the view.

```csharp
using Microsoft.AspNetCore.Mvc;
using PathForge.Data;
using PathForge.Helpers;

namespace PathForge.Controllers
{
    public class HomeController : Controller
    {
        private readonly PathForgeDbContext _db;

        public HomeController(PathForgeDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Pass session data to the view via ViewBag so the layout can use it
            ViewBag.IsLoggedIn = !string.IsNullOrEmpty(
                HttpContext.Session.GetString("UserId"));
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole") ?? "";
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
```

---

## STEP 9 — HOME PAGE VIEW (public landing page)

**`Views/Home/Index.cshtml`**
```html
@{
    ViewData["Title"] = "PathForge — Forge Your Path to Mastery";
    Layout = "_Layout";
}

{{!-- ── HERO SECTION ──────────────────────────────────────── --}}
<section class="pf-page" style="padding-top:5rem; padding-bottom:4rem;">
  <div>
    {{!-- Eyebrow label — Space Grotesk uppercase red --}}
    <span class="pf-label">Web-Based Learning System</span>

    {{!-- Three-line Shrikhand hero (FRAME.md hero-title-stack) --}}
    <div style="margin-top:1rem; margin-bottom:1.5rem;">
      <div class="pf-hero-title">Learning,</div>
      <div class="pf-hero-title-red">mapped</div>
      <div class="pf-hero-title" style="transform:rotate(2deg); display:inline-block;">
        one step at a time.
      </div>
    </div>

    {{!-- Baskerville tagline --}}
    <p class="pf-body" style="max-width:520px; margin-bottom:2rem;">
      Join courses, track your progress on a living skill map, and learn
      alongside educators who share resources, feedback, and discussion in real time.
    </p>

    {{!-- CTAs --}}
    <div style="display:flex; gap:0.75rem; flex-wrap:wrap;">
      @if (ViewBag.IsLoggedIn == true)
      {
        <a href="/Course" class="pf-btn">My Courses</a>
        <a href="/Progress" class="pf-btn pf-btn--ghost">View Progress</a>
      }
      else
      {
        <a href="/Auth/Register" class="pf-btn">Start Learning</a>
        <a href="/Course" class="pf-btn pf-btn--ghost">Browse Courses</a>
      }
    </div>
  </div>
</section>

{{!-- ── STATS STRIP — fin-grid style ─────────────────────── --}}
<section class="pf-section-light">
  <div class="pf-page" style="padding-top:2.5rem; padding-bottom:2.5rem;">
    <div class="pf-stat-grid">
      <div class="pf-stat-cell">
        <span class="pf-stat-cell__number">3</span>
        <span class="pf-stat-cell__label">User Roles</span>
      </div>
      <div class="pf-stat-cell">
        <span class="pf-stat-cell__number">5</span>
        <span class="pf-stat-cell__label">Core Modules</span>
      </div>
      <div class="pf-stat-cell">
        <span class="pf-stat-cell__number">∞</span>
        <span class="pf-stat-cell__label">Learning Paths</span>
      </div>
    </div>
  </div>
</section>

{{!-- ── TECH STACK LABELS ────────────────────────────────── --}}
<section class="pf-page" style="padding-top:2rem; padding-bottom:2rem;">
  <div style="display:flex; gap:1rem; flex-wrap:wrap; border-bottom:var(--border-inner); padding-bottom:1.5rem;">
    @foreach (var tech in new[]{"ASP.NET Core","SQL Server","HTML5 / CSS3","JavaScript","MVC Pattern"})
    {
      <span class="pf-status pf-status--draft" style="opacity:1; font-size:0.6rem; padding:0.2rem 0.6rem;">
        @tech
      </span>
    }
  </div>
</section>
```

---

## WEEK 2 DONE — CHECKLIST

Before moving to Phase 3, verify all of these work:

- [ ] `/Auth/Register` — form shows, validation fires before submit, duplicate email blocked
- [ ] `/Auth/Login` — wrong password shows error, correct login sets session
- [ ] `/Auth/Logout` — session cleared, redirected to login
- [ ] `/Auth/Profile` — shows user info, saves name/bio, password change works
- [ ] Navbar shows correct links based on login state and role
- [ ] No user can access `/Admin` without Admin role

---

## WHAT PHASE 3 COVERS

Phase 3 builds Course Management: educator course CRUD, student browsing and enrolment,
module creation, and material file uploads.
