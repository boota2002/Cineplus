using CinePlus.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CinePlus.Controllers
{
    public class UserController : Controller
    {

        private List<SelectListItem> GetSecurityQuestions()
        {
            return new List<SelectListItem>
        {
            new SelectListItem { Value = "What is your favorite movie?", Text = "What is your favorite movie?" },
            new SelectListItem { Value = "Who is your favorite actor?", Text = "Who is your favorite actor?" },
            new SelectListItem { Value = "What is the name of the first movie you watched?", Text = "What is the name of the first movie you watched?" }
        };
        }
        // generating the RandomCode
        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        // user home page get method
        [HttpGet]
        public IActionResult Index()
        {
            using var db = new CinePlusContext();
            var movies = db.Movies.Take(5).ToList();
            return View(movies);
        }


       // user Login page get method
        [HttpGet]
        public IActionResult Login()
        {


            return View();
        }
        // user Login page post method
        [HttpPost]
        public IActionResult Login(IFormCollection f)
        {
            
            if (f["uname"] == "admin" && f["pwd"] == "123")
            {
                HttpContext.Session.SetString("uname", f["uname"]);
                return RedirectToAction("Home", "Admin");
            }
            else if (f["uname"] == "user" && f["pwd"] == "123")
            {
                HttpContext.Session.SetString("uname", f["uname"]);
                return RedirectToAction("Home", "User");
            }
            else
            {
                ViewBag.Error = "Invalid username or password";
                return View();
            }


        }
        // user register page get method
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.SecurityQuestions = GetSecurityQuestions();

            var captchaCode = GenerateRandomCode(5);
            HttpContext.Session.SetString("CaptchaCode", captchaCode);
            ViewBag.CaptchaCode = captchaCode;

            return View();
        }
        // user register page post method   
        [HttpPost]
        public IActionResult Register(User model)
        {
            ViewBag.SecurityQuestions = GetSecurityQuestions();

            var sessionCaptcha = HttpContext.Session.GetString("CaptchaCode") ?? "";
            if (!string.Equals(model.Captcha, sessionCaptcha, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Captcha", "Invalid captcha code. Please try again.");
            }

            if (ModelState.IsValid)
            {
                using (var db = new CinePlusContext())
                {
                    var user = new User
                    {
                        Username = model.Username,
                        Pass = model.Pass,
                        FullName = model.FullName,
                        Age = model.Age,
                        Email = model.Email,
                        Gender = model.Gender,
                        MobileNo = model.MobileNo,
                        Address = model.Address,
                        CreatedAt = DateTime.Now,
                        SecurityQuestion = model.SecurityQuestion,
                        SecurityAnswer = model.SecurityAnswer,
                        ProfilePic = model.ProfilePic
                    };

                    db.Users.Add(user);
                    db.SaveChanges();
                }

                return RedirectToAction("Index", "Home");
            }

            // Regenerate captcha on failure
            var newCaptchaCode = GenerateRandomCode(5);
            HttpContext.Session.SetString("CaptchaCode", newCaptchaCode);
            ViewBag.CaptchaCode = newCaptchaCode;

            return View(model);
        }
        public IActionResult Movies()
        {
            return View();
        }
    }
}
