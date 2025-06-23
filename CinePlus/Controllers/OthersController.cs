using CinePlus.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinePlus.Controllers
{
    public class OthersController : Controller
    {
        CinePlusContext db = new CinePlusContext();
        // genre page with get method
        [HttpGet]
        public IActionResult Genre()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Genre(IFormCollection f)
        {
            
            try
            {
                if (ModelState.IsValid)
                {
                    var genrename = new Genre()
                    {
                        Name = f["genre"]
                    };
                    if(!db.Genres.Any(x=>x.Name.ToLower() == genrename.Name.ToLower()))
                    {
                        db.Genres.Add(genrename);
                        int i = db.SaveChanges();
                        if (i == 1)
                        {
                            ViewBag.genre = $"{genrename.Name} Added Successfully";
                        }
                    }
                    else
                    {
                        ViewBag.genreexist = $"{genrename.Name} is Already Exist";
                    }
                }

            }
            catch(Exception e)
            {
                ViewBag.genreexist = "Somthing Went Wrong";
                Console.WriteLine(e.Message);
            }

                return View();
        }

        [HttpGet]
        public IActionResult Theater()
        {
            ViewBag.Cities = db.Cities.ToList();
            return View();
        }
        [HttpPost]
        public IActionResult Theater(IFormCollection f)
        {
           var cityName = f["cityname"];
           var cityid = Convert.ToInt32(cityName);
            var theater = new Theater()
            {
               Name= f["tname"],
               CityId = cityid,
               Price= Convert.ToDecimal(f["price"]),
               NoOfSeats = Convert.ToInt32(f["seats"])    
           };
            try
            {
                if (ModelState.IsValid)
                {
                    if(!db.Theaters.Any(x=>x.Name.ToLower() == theater.Name.ToLower()))
                    {
                        db.Theaters.Add(theater);
                        int i = db.SaveChanges();
                        if (i == 1)
                        {
                            ViewBag.theater = $"{theater.Name} Added Successfully";
                        }
                       
                    }
                    else
                    {
                        ViewBag.theaterexist = $"{theater.Name} is Already Exist";
                    }
                }
                else
                {
                    ViewBag.theaterexist = $"{theater.Name} is Already Exist";
                }
            }
            catch(Exception e)
            {
                ViewBag.theaterexist = $"Something went wrong";
                Console.WriteLine(e.Message);   
            }
            ViewBag.Cities = db.Cities.ToList();
            return View();
        }

        [HttpGet]
        public IActionResult City()
        {
            return View();
        }
        [HttpPost]
        public IActionResult City(IFormCollection f)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var city = new City()
                    {
                        CityName = f["city"]
                    };
                    if (!db.Cities.Any(x => x.CityName.ToLower() == city.CityName.ToLower()))
                    {
                        db.Cities.Add(city);
                        int i = db.SaveChanges();
                        if (i == 1)
                        {
                            ViewBag.city = $"{city.CityName} Added Successfully";
                        }
                    }
                    else
                    {
                        ViewBag.cityexist = $"{city.CityName} is Already Exist";
                    }
                    
                }

            }
            catch (Exception e)
            {
                ViewBag.cityexist = "Somthing Went Wrong";
                Console.WriteLine(e.Message);
            }
            return View();
        }


        public IActionResult Languages()
        {
            return View();
        }

       
       

    }
}
