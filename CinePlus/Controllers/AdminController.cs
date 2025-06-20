using Microsoft.AspNetCore.Mvc;

namespace CinePlus.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
