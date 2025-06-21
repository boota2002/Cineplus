using CinePlus.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinePlus.Controllers
{
    public class UserController : Controller
    {
        CinePlusContext pc = new CinePlusContext();

        public IActionResult Home()
        {
            return View();
        }
        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }
        //This for listing all the movies
        [HttpGet]
        public IActionResult Movies()
        {
            var movie = pc.Movies.ToList();
            ViewBag.genre = pc.Genres.ToList();
            ViewBag.language = pc.Languages.ToList();
            return View(movie);
        }
        //This is for Filter the Movie
        [HttpPost]
        public IActionResult Movies(IFormCollection f)
        {
            if (f["option"] == "All" && f["Lang"] == "All")
            {
                return RedirectToAction("Movies");
            }
            else if (f["option"] != "All" && f["Lang"] != "All")
            {
                var searchTerm = Convert.ToInt32(f["Option"]);
                var langsearch = Convert.ToInt32(f["Lang"]);
                ViewBag.genre = pc.Genres.ToList();
                ViewBag.language = pc.Languages.ToList();

                // LINQ: Filter movies by both GenreId and LanguageId
                var movies = (from m in pc.Movies
                              join ml in pc.MovieLanguages on m.MovieId equals ml.MovieId
                              where m.GenreId == searchTerm && ml.LanguageId == langsearch
                              select m).Distinct().ToList();

                return View(movies);
            }
            else if (f["option"] != "All")
            {
                var searchTerm = Convert.ToInt32(f["Option"]);
                ViewBag.t = searchTerm;
                ViewBag.genre = pc.Genres.ToList();
                ViewBag.language = pc.Languages.ToList();
                var movies = pc.Movies.Where(t => t.GenreId == searchTerm).ToList();
                return View(movies);
            }
            else if (f["Lang"] != "All")
            {
                var langsearch = Convert.ToInt32(f["Lang"]);
                ViewBag.genre = pc.Genres.ToList();
                ViewBag.language = pc.Languages.ToList();
                var movieIds = pc.MovieLanguages
                    .Where(t => t.LanguageId == langsearch)
                    .Select(t => t.MovieId)
                    .ToList();
                var movies = pc.Movies.Where(m => movieIds.Contains(m.MovieId)).ToList();
                return View(movies);
            }
            else
            {
                return RedirectToAction("Movies");
            }
        }
        [HttpGet]
        //id=@movie.MovieId&name=@movie.MovieName&duration=@movie.Duration&desc=@movie.Description&release=@movie.ReleaseDate&image
        public IActionResult MovieDetail(string id,string name,string duration,string desc,string release)
        {
            ViewBag.id = id;
            ViewBag.name = name;
            ViewBag.duration = duration;
            ViewBag.desc = desc;
            ViewBag.release = release;
            return View();
        }
        public IActionResult LogOut()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "CinePlus");
        }

    }
}
