using Microsoft.AspNetCore.Mvc;

namespace CinePlus.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]    
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Logout()
        {


            HttpContext.Session.Clear();   
            return RedirectToAction("Login", "Admin");
        }
        [HttpGet]
        public IActionResult Login()
        {
           

            return View();
        }
        [HttpPost]
        public IActionResult Login(IFormCollection f)
        {
            if (f["uname"]=="admin"  && f["pwd"] == "123")
            {
                HttpContext.Session.SetString("uname", f["uname"]);
                return RedirectToAction("Home", "Admin");
            }
            else
            {
                ViewBag.Error = "Invalid username or password";
                return View();
            }

         
        }
        [HttpGet]
        public IActionResult Home()
        {
            if (HttpContext.Session.GetString("uname") == null)
            {
                return RedirectToAction("Login", "Admin");
            }
            return View();
      
        }  }
}
