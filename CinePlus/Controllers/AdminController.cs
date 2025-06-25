using CinePlus.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.EntityFrameworkCore;


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



            try
            {
                movie.Duration = f["duration"];
                movie.MovieName = f["MovieName"];
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
                        movie.MoviePoster = imagedata;
                    }
                }
                if (ModelState.IsValid)
                {
                   if(!db.Movies.Any(x => x.MovieName == movie.MovieName))
                    {
                        db.Movies.Add(movie);
                        int i = db.SaveChanges();
                        if (i == 1)
                        {
                            var res = db.Movies.Where(x=> x.MovieName == movie.MovieName).FirstOrDefault();
                            HttpContext.Session.SetInt32("movieid", res.MovieId);
                            HttpContext.Session.SetString("moviename", res.MovieName);
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
                else
                {
                    ViewBag.validErr = "Please Fill The All The Fields";
                }
            }
            catch (Exception e)
            {
                ViewBag.movieexist = "somthing went wrong";
                Console.WriteLine(e.Message);
            }
            return View();
        }

        [HttpGet]
        public IActionResult AddMovieToTheater(int? cityId)
        {
            var movieid = Convert.ToInt32(HttpContext.Session.GetInt32("movieid"));

            ViewBag.moviename = HttpContext.Session.GetString("moviename");
            ViewBag.Languages = db.Languages.ToList();
            ViewBag.Cities = db.Cities.ToList();
           

            if (cityId.HasValue)
            {
                ViewBag.Theaternames = db.Theaternames.Where(x => x.CityId == cityId.Value).ToList();
                var res = db.Cities.Where(x => x.CityId == cityId.Value).FirstOrDefault();
                ViewBag.SelectedCity=cityId.Value;
                ViewBag.SelectedCity2 = res;
                
                HttpContext.Session.SetInt32("cityid", cityId.Value); 
               
                
               
            }
            else
            {
                ViewBag.Theaternames = new List<Theatername>();
            }

            return View();
        }
        [HttpPost]
        public IActionResult AddMovieToTheater(IFormCollection f)
        {
            var selectedcityid = Convert.ToInt32(HttpContext.Session.GetInt32("cityid"));   


            var city = db.Cities.Where(x => x.CityId == selectedcityid).Select(x => x.CityName).FirstOrDefault();
            var cityid = db.Cities.Where(x => x.CityName.ToLower() == city.ToLower()).FirstOrDefault();
            if (ModelState.IsValid)
            {
               
                try
                {
                    
                    
                    var list = f["theaternames"];
                    var moviename = f["mname"].ToString();
                   
                    var lang = f["languages"];
                    var s1 = f["show"];
                    var MovieId = db.Movies.Where(x => x.MovieName.ToLower() == moviename.ToLower()).Select(x => x.MovieId).FirstOrDefault();
                    var theaterid = new Theater();
                    // add theater records 
                    foreach (var i in list)
                    {
                        if (i != null)
                        {
                            var t = new Theater()
                            {
                                Name = i,
                                Price = Convert.ToDecimal(f["mprice"]),
                                NoOfSeats = Convert.ToInt32(f["seats"]),
                                CityId = cityid.CityId,
                                Movieid = MovieId
                            };

                            var isTheaterExist = db.Theaters.Any(x => x.Name.ToLower() == t.Name.ToLower() && x.CityId == cityid.CityId && x.Movieid == MovieId);
                            if (isTheaterExist)
                            {
                                db.Theaters.Update(t);
                                db.SaveChanges();
                                theaterid = db.Theaters.Where(x => x.Name.ToLower() == t.Name.ToLower() && x.CityId == cityid.CityId && x.Movieid == MovieId).FirstOrDefault();
                            }
                            else
                            {
                                db.Theaters.Add(t);

                                db.SaveChanges();
                                theaterid = db.Theaters.Where(x => x.Name.ToLower() == t.Name.ToLower() && x.CityId == cityid.CityId && x.Movieid == MovieId).FirstOrDefault();
                            }


                            // adding records to the languages
                           
                            foreach (var l in lang)
                            {
                                var ml = new MovieLanguage()
                                {
                                    MovieId = MovieId,
                                    LanguageId = Convert.ToInt32(l)
                                };
                                db.MovieLanguages.Add(ml);
                            }
                            db.SaveChanges();
                            // adding records to showtime
                           

                            foreach (var s in s1)
                            {
                                var showtime = new ShowTime()
                                {

                                    MovieId = MovieId,
                                    Theaterid = theaterid.Tid,
                                    Timings = s
                                };
                                db.ShowTimes.Add(showtime);

                            }
                            db.SaveChanges();
                            ViewBag.success = "Movie Added Successfully to the Theater";
                        }
                    }
                }
                catch (Exception e)
                {
                    ViewBag.errr = "somthing went wrong";
                    Console.WriteLine(e);
                }
            }
           
            ViewBag.Languages = db.Languages.ToList();
            ViewBag.Cities = db.Cities.ToList();
            ViewBag.Theaternames = db.Theaternames.Where(x => x.CityId == cityid.CityId).ToList();
            return View();
        }


        [HttpGet]
        public IActionResult DeleteMovie(int? movieId, int? cityId)
        {
            ViewBag.movies = db.Movies.ToList();
            ViewBag.Cities = db.Cities.ToList();
            if(movieId.HasValue)
            {
                ViewBag.SelectedMovieid = movieId.Value;
                HttpContext.Session.SetInt32("mid", movieId.Value);
               
            }
            if(cityId.HasValue)
            {
                ViewBag.SelectedCityid = cityId.Value;
                ViewBag.Theaternames = db.Theaternames.Where(x => x.CityId == cityId.Value).ToList();
                HttpContext.Session.SetInt32("cid", cityId.Value);
            }
            else
            {
             ViewBag.Theaternames = new List<Theatername>();
            }
                return View();

        }
        [HttpPost]
        public IActionResult DeleteMovie(IFormCollection f)
        {
           
            try
            {
               
                if (ModelState.IsValid)
                {
                    var mid = Convert.ToInt32(HttpContext.Session.GetInt32("mid"));
                    var cid = Convert.ToInt32(HttpContext.Session.GetInt32("cid"));
                    var tid = f["tname"].ToString();
                    if (mid == 0 || cid == 0 || tid == null)
                    {
                        ViewBag.ViewBag.deletefail = $"Movie Is Not Found";
                        return View();
                    }
                    else
                    {
                        var tt = db.Theaters.Where(x => x.Name == tid && x.CityId == cid && x.Movieid == mid).FirstOrDefault();
                        if (tt != null)
                        {

                            var showtimes = db.ShowTimes.Where(x => x.MovieId == mid && x.Theaterid == tt.Tid).ToList();
                            if (showtimes != null)
                            {

                                foreach (var show in showtimes)
                                {
                                    db.ShowTimes.Remove(show);

                                }
                                var theaters = db.Theaters.Where(x => x.Movieid == mid && x.CityId == cid && x.Name.ToLower() == tid.ToLower()).ToList();
                                foreach (var theater in theaters)
                                {
                                    db.Theaters.Remove(theater);

                                }
                                db.SaveChanges();

                                ViewBag.deletesucess = $"Movie Is Successfully Deleted ";
                            }
                            else
                            {
                                ViewBag.ViewBag.deletefail = $"Movie Is Not Found";
                                return View();

                            }
                        }
                        else
                        {
                            ViewBag.ViewBag.deletefail = $"Movie Is Not Found";
                            return View();
                        }
                    }
                }
            }
            catch (Exception e)
            {
               ViewBag.deletefail= $"Values Can't Be Empty";
                Console.WriteLine(e.InnerException);
            }
            ViewBag.movies = db.Movies.ToList();
            ViewBag.Cities = db.Cities.ToList();
            ViewBag.Theaternames = new List<Theatername>();
            return View();

        }


        [HttpGet]
        public IActionResult UpdateMovie(int? cityId)
        {
            ViewBag.Cities = db.Cities.ToList();


            if (cityId.HasValue)
            {
                ViewBag.Theaternames = db.Theaternames.Where(x => x.CityId == cityId.Value).ToList();
                var res = db.Cities.Where(x => x.CityId == cityId.Value).FirstOrDefault();
                ViewBag.SelectedCity = cityId.Value;
                ViewBag.SelectedCity2 = res;

                HttpContext.Session.SetInt32("cityid", cityId.Value);

            }
            else
            {
                ViewBag.Theaternames = new List<Theatername>();
            }

            return View();
        }

        [HttpPost]
        public IActionResult UpdateMovie(IFormCollection f)
        {

            var selectedcityid = Convert.ToInt32(HttpContext.Session.GetInt32("cityid"));

                var moviename = f["mname"].ToString();
                var cityname = f["cityname"].ToString();
                var theatername = f["tname"].ToString();
                var price = Convert.ToDecimal(f["mprice"]);
                ViewBag.Cities = db.Cities.ToList();
            ViewBag.Theaternames = new List<Theatername>();
            var shows = f["show"];
            try
                {
                    var res = db.Movies.Where(x => x.MovieName.ToLower() == moviename.ToLower()).FirstOrDefault();
                if (res == null )
                {

                    ViewBag.updatefail = $"{moviename} Is Not Found";
                    return View();
                }
                var city = db.Cities.Where(x => x.CityId==selectedcityid).FirstOrDefault();
                 
                    var theater_record = db.Theaters.Where(x => x.Name.ToLower() == theatername.ToLower()).FirstOrDefault();
                    var theaters = db.Theaters.Where(x => x.Movieid == res.MovieId && x.CityId == city.CityId && x.Name.ToLower() == theatername.ToLower()).FirstOrDefault();
                 if (theaters==null)
                    {
                        
                    ViewBag.updatefail = $"{moviename} Is Not Found";
                        return View();
                    }
                    else
                    {

                    theaters.Price = price;

                    var showtimes = db.ShowTimes.Where(x => x.MovieId == res.MovieId && x.Theaterid == theaters.Tid).ToList();

                        foreach (var show in showtimes)
                        {
                            db.ShowTimes.Remove(show);

                        }

                        db.SaveChanges();

                    foreach (var s in shows)
                    {
                        var showtime = new ShowTime()
                        {

                            MovieId = res.MovieId,
                            Theaterid = theaters.Tid,
                            Timings = s
                        };
                        db.ShowTimes.Add(showtime);

                    }
                    db.SaveChanges();

                   
                    var image = f.Files["poster"];
                    if (image != null && image.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            image.CopyTo(ms);
                            byte[] imagedata = ms.ToArray();
                            res.MoviePoster = imagedata;
                        }
                    }
                    db.SaveChanges();
                    ViewBag.updatesucess = $"{moviename} Is Successfully Updated From {theatername}";
                    }
                }
                catch (Exception e)
                {
                    ViewBag.updatefail = $"Something Went Wrong";
                    Console.WriteLine(e.InnerException);
                }
           

            return View();

        }




    }
}
