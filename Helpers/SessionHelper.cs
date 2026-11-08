using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace PathForge.Helpers
{
    public static class SessionHelper
    {
        // Returns true and sets a TempData message if the user should be blocked
        public static bool RequireLogin(HttpContext ctx, ITempDataDictionary tempData)
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
            ITempDataDictionary tempData,
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
            => int.TryParse(ctx.Session.GetString("UserId"), out int id) ? id : 0;

        public static string GetUserRole(HttpContext ctx)
            => ctx.Session.GetString("UserRole") ?? "";
    }
}
