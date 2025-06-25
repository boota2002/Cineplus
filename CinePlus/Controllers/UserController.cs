using CinePlus.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

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

        //Login
        [HttpGet]
        public IActionResult Login()
        {
            GenerateCaptcha(); //for generating first captcha
            return View();
        }
        [HttpPost]
        public IActionResult Login(Admin a, string captchaText, IFormCollection form)
        {
            //try
                
            //{
                if (HttpContext.Session.GetString("CaptchaCode") == null || HttpContext.Session.GetString("CaptchaCode") != captchaText)
                {
                    ViewBag.err = "Incorrect CAPTCHA. Please try again.";
                    ModelState.Clear();
                    GenerateCaptcha(); // it is for regenarting captcha again if entered captcha is wrong 
                    return View(a); // Passing the user object back to retain form data
                }
                CinePlusContext db = new CinePlusContext();

                //byte[] pass = Encoding.UTF8.GetBytes(form["pass"].ToString());
                var admin_detail = db.Admins.FirstOrDefault(user => user.Username == a.Username && user.Password.SequenceEqual(a.Password));
                //var  = db.Admins.FirstOrDefault();
                //if (a.Username == admin_detail.Username && a.Password == admin_detail.Password)
                //{
                //    return RedirectToAction("Home", "Admin");
                //}

                if (admin_detail != null)
                {
                    ViewBag.err = "Login successful.";

                    //HttpContext.Session.SetString("Name", res.FullName);
                    //HttpContext.Session.SetString("Username", res.Username);
                    //HttpContext.Session.SetString("Password", form["pass"].ToString());
                    //HttpContext.Session.SetInt32("UserId", res.UserId);
                    //ModelState.Clear();
                    return RedirectToAction("Home", "Admin");
                }
                else
                {
                    ViewBag.err = "Invalid username or password.";
                    ModelState.Clear();
                    GenerateCaptcha(); // Regenerate CAPTCHA on invalid credentials
                    return View(a); // Passing the user object back to retain form data
                }
            //}
            //catch (Exception ex)
            //{
            //    ViewBag.err = "An error occurred: " + ex.Message;
            //    GenerateCaptcha(); // Regenerate CAPTCHA on error
            //    return View(a);
            //}
        }

        private void GenerateCaptcha()
        {
            // Generating a random string for the CAPTCHA
            string captchaCode = GenerateRandomString(6); // You can adjust the length
            HttpContext.Session.SetString("CaptchaCode", captchaCode);

            // Create the CAPTCHA image
            byte[] captchaImage = CreateCaptchaImage(captchaCode);
            ViewBag.CaptchaImage = "data:image/png;base64," + Convert.ToBase64String(captchaImage);
        }
        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private byte[] CreateCaptchaImage(string text)
        {
            var image = new Bitmap(150, 50);
            var graphics = Graphics.FromImage(image);

            graphics.FillRectangle(Brushes.White, 0, 0, image.Width, image.Height);
            System.Drawing.Font font = new System.Drawing.Font(FontFamily.GenericSerif, 24, FontStyle.Bold);
            graphics.DrawString(text, font, Brushes.Black, 10, 10);

            // Adding some noise or lines for better security
            var random = new Random();
            for (int i = 0; i < 10; i++)
            {
                graphics.DrawLine(new Pen(Color.Gray, 1),
                random.Next(image.Width), random.Next(image.Height),
                random.Next(image.Width), random.Next(image.Height));
            }

            var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();


        }






        // user Login page get method
        //[HttpGet]
        //public IActionResult Login()
        //{


        //    return View();
        //}
        //// user Login page post method
        //[HttpPost]
        //public IActionResult Login(IFormCollection f)
        //{
        //    CinePlusContext db = new CinePlusContext();
        //    var username = f["uname"];
        //    var password = Encoding.UTF8.GetBytes(f["pwd"]);    
        //    var admin = db.Admins.Where(x => x.Username == username && x.Password.ToString()==password.ToString()).FirstOrDefault();


        //    if (admin!=null)
        //    {
        //        HttpContext.Session.SetString("uname", f["uname"]);
        //        return RedirectToAction("Home", "Admin");
        //    }
        //    else if (f["uname"] == "user" && f["pwd"] == "123")
        //    {
        //        HttpContext.Session.SetString("uname", f["uname"]);
        //        return RedirectToAction("Home", "User");
        //    }
        //    else
        //    {
        //        ViewBag.Error = "Invalid username or password";
        //        return View();
        //    }

        //}
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
