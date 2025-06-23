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



            try
            {
                movie.Duration = f["duration"];
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
                                return RedirectToAction("AddMovieToTheater");  
                            }
                        }
                      
                    }
                    else
                    {
                        ViewBag.movieexist = $"{movie.MovieName} is Already Exist";
                    }
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
        public IActionResult AddMovieToTheater()
        {
            var movieid = Convert.ToInt32(HttpContext.Session.GetInt32("movieid"));

            ViewBag.moviename = HttpContext.Session.GetString("moviename");
            ViewBag.Languages = db.Languages.ToList();
            return View();
        }
        [HttpPost]
        public IActionResult AddMovieToTheater(IFormCollection f)
        {

            if (ModelState.IsValid)
            {
                try
                {

                    var city = f["cityname"].ToString();
                    var t1 = f["tname1"].ToString();    
                    var t2 = f["tname2"].ToString();
                    var t3 = f["tname3"].ToString();
                    var t4 = f["tname4"].ToString();    
                    var list = new List<string>();
                    list.Add(t1);
                    list.Add(t2);
                    list.Add(t3);
                    list.Add(t4);   
                    // adding city records
                    var cityid = new City();
                    var isCityExist = db.Cities.Any(x => x.CityName.ToLower() == city.ToLower());
                    if (!isCityExist)
                    {
                        var c = new City()
                        {
                            CityName = city
                        };
                        db.Cities.Add(c);
                        db.SaveChanges();
                        cityid = db.Cities.Where(x => x.CityName.ToLower() == city.ToLower()).FirstOrDefault();
                    }
                    else
                    {
                        cityid = db.Cities.Where(x => x.CityName.ToLower() == city.ToLower()).FirstOrDefault();
                    }
                    //var t = new Theater()
                    //{
                    //    Name = f["tname1"],
                    //    Price = Convert.ToDecimal(f["mprice"]),
                    //    NoOfSeats = Convert.ToInt32(f["seats"]),
                    //    CityId = cityid.CityId
                    //};
                    var lang = f["languages"];
                    var s1 = f["show"];
                    var MovieId = Convert.ToInt32(HttpContext.Session.GetInt32("movieid"));
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
                                    MovieId = Convert.ToInt32(HttpContext.Session.GetInt32("movieid")),
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

                                    MovieId = Convert.ToInt32(HttpContext.Session.GetInt32("movieid")),
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
            var movieid = Convert.ToInt32(HttpContext.Session.GetInt32("movieid"));

            ViewBag.moviename = HttpContext.Session.GetString("moviename");
            ViewBag.Languages = db.Languages.ToList();
            return View();
        }


        [HttpGet]
        public IActionResult DeleteMovie()
        {
            return View();

        }
        [HttpPost]
        public IActionResult DeleteMovie(IFormCollection f)
        {
            var moviename = f["moviename"].ToString();
            var cityname = f["cityname"].ToString();    
            var theatername = f["theatername"].ToString();
            try
            {
                var res = db.Movies.Where(x => x.MovieName.ToLower() == moviename.ToLower()).FirstOrDefault();
                var city = db.Cities.Where(x=> x.CityName.ToLower() == cityname.ToLower()).FirstOrDefault();
                var theater_record = db.Theaters.Where(x => x.Name.ToLower() == theatername.ToLower()).FirstOrDefault();
                if(res == null)
                {
                    ViewBag.ViewBag.deletefail = $"{moviename} Is Not Found";
                    return View();
                }
                else
                {
                 
                    var showtimes = db.ShowTimes.Where(x=> x.MovieId ==res.MovieId && x.Theaterid == theater_record.Tid).ToList();
                    
                    foreach(var show in showtimes)
                    {
                        db.ShowTimes.Remove(show);
                        
                    }
                   

                    var theaters = db.Theaters.Where(x=> x.Movieid ==  res.MovieId && x.CityId == city.CityId && x.Name.ToLower() == theatername.ToLower()).ToList();
                    foreach(var  theater in theaters)
                    {
                        db.Theaters.Remove(theater);
                        
                    }
                    db.SaveChanges();

                    ViewBag.deletesucess = $"{moviename} Is Successfully Deleted From {theatername}";
                }
            }
            catch (Exception e)
            {
                ViewBag.deletefail= $"Something Went Wrong";
                Console.WriteLine(e.InnerException);
            }
            return View();

        }



    }
}
