using Microsoft.AspNetCore.Mvc;

namespace CinePlus.Controllers
{
    public class UserController : Controller
    {
        public IActionResult LogOut()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "CinePlus");
        }
    }
}
