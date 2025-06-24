using CinePlus.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows;

namespace CinePlus.Controllers
{
    public class UserController : Controller
    {
        CinePlusContext db;
        public UserController(CinePlusContext database)
        {
            db = database;
        }

        private List<SelectListItem> GetSecurityQuestions()
        {
            return new List<SelectListItem>
        {
            new SelectListItem { Value = "What is your favorite movie?", Text = "What is your favorite movie?" },
            new SelectListItem { Value = "Who is your favorite actor?", Text = "Who is your favorite actor?" },
            new SelectListItem { Value = "What is the name of the first movie you watched?", Text = "What is the name of the first movie you watched?" }
        };
        }

        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public IActionResult Home()
        {
            var movies = db.Movies.ToList();
            return View(movies);
        }
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
                return RedirectToAction("Login", "User");
            }

            // Regenerate captcha on failure
            var newCaptchaCode = GenerateRandomCode(5);
            HttpContext.Session.SetString("CaptchaCode", newCaptchaCode);
            ViewBag.CaptchaCode = newCaptchaCode;

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(User user)
        {
            try
            {
                // Convert input password string to byte array
                var inputPassBytes = user.Pass; // <-- You need to get the password string from the form

                // Query for user by username only (fetch stored pass)
                var dbUser = db.Users.FirstOrDefault(t => t.Username == user.Username);

                if (dbUser == null)
                {
                    ViewBag.err = "Invalid Username or Password";
                }
                else
                {
                    // Compare byte arrays for password equality
                    bool passwordMatch = dbUser.Pass.SequenceEqual(inputPassBytes);

                    if (!passwordMatch)
                    {
                        ViewBag.err = "Invalid Username or Password";
                    }
                    else
                    {
                        HttpContext.Session.SetInt32("uid", dbUser.UserId);
                        return RedirectToAction("Movies");
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.err = "Something Went Wrong";
            }

            return View();
        }
        //Forget Password
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            ViewBag.SecurityQuestions = GetSecurityQuestions();
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(User s, IFormCollection f)
        {
            ViewBag.SecurityQuestions = GetSecurityQuestions();
            ViewBag.update = 0;
            try
            {
                var res = (from t in db.Users where t.Email == s.Email select t).FirstOrDefault();
                if (res == null)
                {
                    ViewBag.err = "No user found with that email.";
                    return View();
                }
                if (res.SecurityQuestion != s.SecurityQuestion || res.SecurityAnswer != s.SecurityAnswer)
                {
                    ViewBag.err = "Security question or answer is incorrect.";
                    return View();
                }
                var newPassword = f["Pass"];
                var confirmPassword = f["confirmpass"];

                if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                {
                    ViewBag.err = "Password fields cannot be empty.";
                    return View();
                }

                if (newPassword != confirmPassword)
                {
                    ViewBag.err = "Password and Confirm Password do not match.";
                    return View();
                }
                res.Pass = res.Pass = Encoding.UTF8.GetBytes(newPassword);
                db.Users.Update(res);
                ViewBag.update = db.SaveChanges();
                return View();
            }
            catch (Exception e)
            {
                ViewBag.err = "An error occurred: " + e.Message;
                return View();
            }
        }

        //This for listing all the movies
        [HttpGet]
        public IActionResult Movies()
        {
            if (HttpContext.Session.GetInt32("uid") == null)
            {
                return RedirectToAction("Login");
            }
            else
            {
                HttpContext.Session.SetInt32("CityId", 1);
                var city_movieid = db.Theaters
                                   .Where(t => t.CityId == 1)
                                   .Select(t => t.MovieId).ToList();
                var movie = db.Movies.Where(m => city_movieid.Contains(m.MovieId)).ToList();

                int cityid = Convert.ToInt32(HttpContext.Session.GetInt32("CityId"));
                ViewBag.cityname = (from t in db.Cities where t.CityId == cityid select t.CityName).FirstOrDefault();
                ViewBag.genre = db.Genres.ToList();
                ViewBag.language = db.Languages.ToList();
                ViewBag.cities = db.Cities.ToList();
                return View(movie);
            }
        }
        //This is for Filter the Movie
        [HttpPost]
        public IActionResult Movies(IFormCollection f)
        {
            // Set CityId in session, default is 1
            int cityId = 1;
            if (f["City"] != "All")
            {
                cityId = Convert.ToInt32(f["City"]);
            }
            HttpContext.Session.SetInt32("CityId", cityId);
            ViewBag.cityname = (from c in db.Cities where c.CityId == cityId select c.CityName).FirstOrDefault();

            var movies = db.Movies.AsQueryable();

            // Filter by City
            if (f["City"] != "All")
            {
                var city_movieIds = db.Theaters
                                     .Where(t => t.CityId == cityId)
                                     .Select(t => t.MovieId)
                                     .Distinct()
                                     .ToList();
                movies = movies.Where(m => city_movieIds.Contains(m.MovieId));
            }

            // Filter by Genre
            if (f["option"] != "All")
            {
                int genreId = Convert.ToInt32(f["option"]);
                movies = movies.Where(m => m.GenreId == genreId);
            }

            // Filter by Language
            if (f["Lang"] != "All")
            {
                int langId = Convert.ToInt32(f["Lang"]);

                // Join with MovieLanguages for filtering languages
                var movieIdsWithLang = db.MovieLanguages
                                        .Where(ml => ml.LanguageId == langId)
                                        .Select(ml => ml.MovieId)
                                        .Distinct()
                                        .ToList();
                movies = movies.Where(m => movieIdsWithLang.Contains(m.MovieId));
            }
            // Filter movies by both GenreId and LanguageId
            else if (f["option"] != "All" && f["Lang"] != "All")
            {
                var searchTerm = Convert.ToInt32(f["Option"]);
                var langsearch = Convert.ToInt32(f["Lang"]);
                movies = (from m in db.Movies
                          join ml in db.MovieLanguages on m.MovieId equals ml.MovieId
                          where m.GenreId == searchTerm && ml.LanguageId == langsearch
                          select m).Distinct();
            }

            // Filter by Search Text (movie name contains)
            string searchText = f["SearchText"];
            if (!string.IsNullOrEmpty(searchText))
            {
                int cityid = 1;
                HttpContext.Session.SetInt32("CityId", cityid);
                movies = (from t in db.Movies where t.MovieName.Contains(searchText) select t);
            }

            // Pass the filters data for dropdowns again
            ViewBag.genre = db.Genres.ToList();
            ViewBag.language = db.Languages.ToList();
            ViewBag.cities = db.Cities.ToList();

            return View(movies.ToList());
        }

        [HttpGet]
        public IActionResult MovieDetail(string id,string name,string duration,string desc,string release)
        {
            if (HttpContext.Session.GetInt32("uid") == null)
            {
                return RedirectToAction("Login");
            }
            //To get the Movie detail
            var res = (from t in db.Movies
                       where t.MovieId == Convert.ToInt32(id)
                       select t).FirstOrDefault();
            //To get the language code
            var lan = (from t in db.MovieLanguages
                       where t.MovieId == Convert.ToInt32(id)
                       select t.LanguageId).ToList();
            //To get the language name
            var language_name = (from t in db.Languages
                                 where lan.Contains(t.LanguageId)
                                 select t.Name).ToList();
            //To get the genre detail
            var genre = (from t in db.Genres
                         where t.GenreId == res.GenreId
                         select t.Name).FirstOrDefault();
            //To get the cast details
            var cast = (from t in db.MovieCasts
                        where t.MovieId == Convert.ToInt32(id)
                        select t).ToList();
            //To get the recommended movie
            var related = (from t in db.Movies
                           where t.GenreId == res.GenreId && t.MovieId != res.MovieId
                           select t).ToList();
            //To get the user detail
            int userid = Convert.ToInt32(HttpContext.Session.GetInt32("uid"));
            var username = (from t in db.Users
                            where t.UserId == userid
                            select t.Username).FirstOrDefault();
            //To get the Review
            int mid = Convert.ToInt32(id);
            var review_table = (from t in db.Reviews where t.MovieId == mid select t).ToList();

            ViewBag.username = username;
            ViewBag.review = review_table;
            ViewBag.related = related;
            ViewBag.genre = genre;
            ViewBag.language = language_name;
            ViewBag.Cast = cast;
            ViewBag.image = res.MoviePoster;
            ViewBag.id = id;
            ViewBag.name = name;
            ViewBag.duration = duration;
            ViewBag.desc = desc;
            ViewBag.release = release;
            return View();
        }
        [HttpPost]
        public IActionResult MovieDetail(IFormCollection f)
        {
            int movieid = Convert.ToInt32(f["movieid"]);
            int rating = Convert.ToInt32(f["Rating"]);
            string comment = f["Comment"];
            int like = Convert.ToInt32(f["Radio"]);
            int userid = Convert.ToInt32(HttpContext.Session.GetInt32("uid"));

            //To check that the user has already rated the movie

            var rate_table = (from t in db.Reviews where t.Uid == userid && t.MovieId == movieid select t).ToList();
            ViewBag.count = rate_table.Count;
            if(rate_table.Count() > 0)
            {
                return RedirectToAction("ReviewMessage", new {id = ViewBag.count});  
            }
            else
            {
                Review review = new Review() { MovieId = movieid, Rating = rating, CommentText = comment, Like = like, Uid = userid };
                db.Reviews.Add(review);
                db.SaveChanges();
                return RedirectToAction("ReviewMessage", new { id = ViewBag.count });
            }      
        }
        //To check if the user already reviewed or not
        public IActionResult ReviewMessage(int id)
        {
            ViewBag.count = id;
            return View();
        }

        //To list the theater
        public IActionResult TheaterList(int id)
        {
            ViewBag.id = id;
            return View();
        }
        public IActionResult LogOut()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

    }
}
