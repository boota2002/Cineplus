using CinePlus.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinePlus.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]    
    public class AdminController : Controller
    {
        CinePlusContext db = new CinePlusContext(); 
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Logout()
        {


            HttpContext.Session.Clear();   
            return RedirectToAction("Login", "User");
        }
    
        [HttpGet]
        public IActionResult Home()
        {
            if (HttpContext.Session.GetString("uname") == null)
            {
                return RedirectToAction("Login", "User");
            }
            return View();
      
        }
        [HttpGet]
        public IActionResult AddMovie()
        {
            ViewBag.Genre = db.Genres.ToList();    
         if (HttpContext.Session.GetString("uname") == null)
            {
                return RedirectToAction("Login", "User");
            }

            return View();
        }
        [HttpPost]
        public IActionResult AddMovie(IFormCollection f)
        {
            
            ViewBag.Genre = db.Genres.ToList();
            var movie = new Movie();


            movie.MovieName = f["moviename"];
            movie.GenreId = Convert.ToInt32(f["genname"]);
            movie.Description = f["des"];
            movie.ReleaseDate = DateOnly.Parse(f["rdate"]);
            var image = f.Files["poster"];
            if (image != null && image.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    image.CopyTo(ms);
                    byte[] imagedata = ms.ToArray();
                    movie.MoviePoster =imagedata;
                }
            }
            try
            {
                if (ModelState.IsValid)
                {
                   if(!db.Movies.Any(x => x.MovieName == movie.MovieName))
                    {
                        db.Movies.Add(movie);
                        int i = db.SaveChanges();
                        if (i == 1)
                        {
                            var res = db.Movies.Where(x=> x.MovieName == movie.MovieName).FirstOrDefault();

                            var moviecast = new MovieCast()
                            {
                                MovieId = res.MovieId,
                                Actor = f["actor"],
                                Actress = f["actress"],
                                Director = f["director"],
                                Producer = f["producer"],
                                Musician = f["musician"]
                            };
                            db.MovieCasts.Add(moviecast);
                            int i2 = db.SaveChanges();
                            if (i2 == 1)
                            {
                                ViewBag.movie = $"{movie.MovieName} is Added Successfully";
                            }
                        }
                      
                    }
                    else
                    {
                        ViewBag.movieexist = $"{movie.MovieName} is Already Exist";
                    }
                }
            }
            catch(Exception e)
            {
                ViewBag.movieexist = "somthing went wrong";
                Console.WriteLine(e.Message);
            }
            return View();
        }
    }
}
