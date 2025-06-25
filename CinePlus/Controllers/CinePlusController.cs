using BotDetect;
using BotDetect.C5;
using CinePlus.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
//using QRCoder;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows;

namespace CinePlus.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class CinePlusController : Controller
    {
        CinePlusContext db = new CinePlusContext();
        public IActionResult Index()
        {
            return View();
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
        //Home Method
        public IActionResult Home()
        {
            var movies = db.Movies.ToList();
            return View(movies);
        }

        //Register Page
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.SecurityQuestions = GetSecurityQuestions();

            var captchaCode = GenerateRandomCode(5);
            HttpContext.Session.SetString("CaptchaCode", captchaCode);
            ViewBag.CaptchaCode = captchaCode;

            return View();
        } 
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
                        Pass = model.Pass, // Use the correct property for password (string)
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

                return RedirectToAction("Login", "CinePlus");
            }

            // Regenerate captcha on failure
            var newCaptchaCode = GenerateRandomCode(5);
            HttpContext.Session.SetString("CaptchaCode", newCaptchaCode);
            ViewBag.CaptchaCode = newCaptchaCode;

            return View(model);
        }

        //Login
        [HttpGet]
        public IActionResult Login()
        {
            GenerateCaptcha(); //for generating first captcha
            return View();
        }
        [HttpPost]
        public IActionResult Login(User u, string captchaText, IFormCollection form)
        {
            try
            {
                if (HttpContext.Session.GetString("CaptchaCode") == null || HttpContext.Session.GetString("CaptchaCode") != captchaText)
                {
                    ViewBag.err = "Incorrect CAPTCHA. Please try again.";
                    ModelState.Clear();
                    GenerateCaptcha(); // it is for regenarting captcha again if entered captcha is wrong 
                    return View(u); // Passing the user object back to retain form data
                }
                var admin_detail = db.Admins.FirstOrDefault();
                if (form["username"] == admin_detail.Username && form["pass"] == admin_detail.Password)
                {
                    return RedirectToAction("Home", "Admin");
                }
                byte[] pass = Encoding.UTF8.GetBytes(form["pass"].ToString());
                var res = db.Users.FirstOrDefault(user => user.Username == u.Username && user.Pass.SequenceEqual(pass));
                if (res != null)
                {
                    
                    ViewBag.err = "Login successful.";

                    HttpContext.Session.SetString("Name", res.FullName);
                    HttpContext.Session.SetString("Username", res.Username);
                    HttpContext.Session.SetString("Password", form["pass"].ToString());
                    HttpContext.Session.SetInt32("UserId", res.UserId);
                    ModelState.Clear();
                    return RedirectToAction("Movies", "CinePlus");
                }
                else
                {
                    ViewBag.err = "Invalid username or password.";
                    ModelState.Clear();
                    GenerateCaptcha(); // Regenerate CAPTCHA on invalid credentials
                    return View(u); // Passing the user object back to retain form data
                }
            }
            catch (Exception ex)
            {
                ViewBag.err = "An error occurred: " + ex.Message;
                GenerateCaptcha(); // Regenerate CAPTCHA on error
                return View(u);
            }
        }
        //Foregt Password
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
                res.Pass = Encoding.UTF8.GetBytes(newPassword);
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
        //Update Profile
        [HttpGet]
        public IActionResult UpdateProfile()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login");
            }
            int UserId = Convert.ToInt32(HttpContext.Session.GetInt32("UserId"));
            ViewBag.UserId = UserId;
            ViewBag.hidden = false;
            var details = db.Users.Where(d => d.UserId == UserId).FirstOrDefault();
            if (details == null)
            {
                TempData["ErrorMessage"] = "User profile not found.Please Register First.";
                return RedirectToAction("Register", "CinePlus");
            }
            return View(details);
        }
        [HttpGet]
        public IActionResult EditProfile(string _username)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login");
            }
            var details = db.Users.Where(d => d.Username == _username).FirstOrDefault();
            if (details == null)
            {
                TempData["ErrorMessage"] = "User profile not found for editing.";
                return RedirectToAction("UpdateProfile");
            }
            return View(details);
        }
        [HttpPost]
        public async Task<IActionResult> EditProfile(string _username, IFormCollection form, IFormFile Pic)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login");
            }
            var details = db.Users.Where(d => d.Username == _username).FirstOrDefault();
            try
            {
                if (details == null)
                {
                    TempData["ErrorMessage"] = "User profile not found.";
                    return View();
                }
                if (ModelState.IsValid)
                {
                    details.FullName = form["FullName"];
                    if (int.TryParse(form["Age"], out int age))
                    {
                        details.Age = age;
                    }
                    else
                    {
                        ModelState.AddModelError("Age", "Invalid Age format.");
                    }
                    details.Address = form["Address"];
                    details.MobileNo = form["MobileNo"];
                    details.Email = form["Email"];
                    if (Pic != null && Pic.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await Pic.CopyToAsync(memoryStream);
                            details.ProfilePic = memoryStream.ToArray();
                        }
                    }
                    db.Users.Update(details);
                    await db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction("UpdateProfile");
                }
                return View(details);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred while updating the profile: " + ex.Message;
                return View(details);
            }
        }
        //This for listing all the movies
        [HttpGet]
        public IActionResult Movies()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login");
            }
            else
            {
                var city_movieid = db.Theaters
                                   .Where(t => t.CityId == 1)
                                   .Select(t => t.Movieid).ToList();
                var movie = db.Movies.ToList();
                ViewBag.genre = db.Genres.ToList();
                ViewBag.language = db.Languages.ToList();
                return View(movie);
            }
        }
        //This is for Filter the Movie
        [HttpPost]
        public IActionResult Movies(IFormCollection f)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login");
            }
            var movies = db.Movies.AsQueryable();
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
            return View(movies.ToList());
        }
        //Movie Detail
        public IActionResult MovieDetail(string id, string name, string duration, string desc, string release)
        {
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
            int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserId"));
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
            HttpContext.Session.SetInt32("MovieId",mid);
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
            int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserId"));

            //To check that the user has already rated the movie

            var rate_table = (from t in db.Reviews where t.Uid == userid && t.MovieId == movieid select t).ToList();
            ViewBag.count = rate_table.Count;
            if (rate_table.Count() > 0)
            {
                return RedirectToAction("ReviewMessage", new { id = ViewBag.count });
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
        //List of  theater
        [HttpGet]
        public IActionResult Theatre(int? cityId, DateTime? selectedDate)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login");
            }
            using var db = new CinePlusContext();

            int movie_id = Convert.ToInt32(HttpContext.Session.GetInt32("MovieId"));
            var cities = db.Cities.ToList();
            ViewBag.Cities = cities;
            ViewBag.SelectedDate = selectedDate ?? DateTime.Today;

            List<Theater> theaters = new();

            if (cityId.HasValue)
            {
                theaters = db.Theaters
                    .Where(t => t.CityId == cityId && t.Movieid == movie_id)
                    .ToList();

                ViewBag.CityId = cityId;
            }

            var showTimes = db.ShowTimes.Where(t => t.MovieId == movie_id).ToList();
            var movies = db.Movies.ToList();

            var theaters2 = db.Theaters.ToList();
            ViewBag.ShowTimes = showTimes;
            ViewBag.Movies = movies;

            return View(theaters);
        }

        //Booking 
        [HttpGet]
        public async Task<IActionResult> Book(int showId, int theaterId, string date)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login");
            }
            using var db = new CinePlusContext();

            var theater = await db.Theaters.FindAsync(theaterId);
            if (theater == null)
            {
                ModelState.AddModelError("", "Theater not found.");
                return View();
            }

            var bookedSeats = await db.Tickets
                .Include(t => t.Seat)
                .Where(t => t.ShowId == showId && t.Seat.TheaterId == theaterId)
                .Select(t => t.Seat.SeatNumber)
                .ToListAsync();

            var availableSeats = new List<string>();
            for (int i = 1; i <= theater.NoOfSeats; i++)
            {
                var seatNumber = $"Seat{i}";
                availableSeats.Add(seatNumber);
            }

            ViewBag.ShowId = showId;
            ViewBag.TheaterId = theaterId;
            ViewBag.Date = date;
            ViewBag.AvailableSeats = availableSeats.ToArray();
            ViewBag.BookedSeats = bookedSeats.ToArray();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Book(int showId, int theaterId, string date, string[] selectedSeats)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login");
            }
            using var db = new CinePlusContext();
            using var transaction = await db.Database.BeginTransactionAsync();

            if (selectedSeats == null || selectedSeats.Length == 0)
            {
                ModelState.AddModelError("", "Please select at least one seat.");
                return await Book(showId, theaterId, date);
            }

            var _username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(_username))
            {
                ModelState.AddModelError("", "User is not logged in.");
                return await Book(showId, theaterId, date);
            }

            var show = await db.ShowTimes.FindAsync(showId);
            if (show == null)
            {
                ModelState.AddModelError("", "Show not found.");
                return await Book(showId, theaterId, date);
            }
            int movieId = (int)show.MovieId;

            var alreadyBookedSeats = new List<string>();

            foreach (var seatNumber in selectedSeats)
            {
                var seat = await db.Seats.FirstOrDefaultAsync(s => s.SeatNumber == seatNumber && s.TheaterId == theaterId);
                if (seat == null)
                {
                    seat = new Seat { SeatNumber = seatNumber, TheaterId = theaterId };
                    db.Seats.Add(seat);
                    await db.SaveChangesAsync();
                }

                var isSeatBooked = await db.Tickets.AnyAsync(t => t.ShowId == showId && t.SeatId == seat.SeatId);
                if (isSeatBooked)
                {
                    alreadyBookedSeats.Add(seatNumber);
                    continue;
                }

                var userId = HttpContext.Session.GetInt32("UserId");
                var ticket = new Ticket
                {
                    UserId = userId,
                    ShowId = showId,
                    MovieId = movieId,
                    SeatId = seat.SeatId,
                    TicketDate = Convert.ToDateTime(date)
                };

                db.Tickets.Add(ticket);
            }

            if (alreadyBookedSeats.Any())
            {
                ModelState.AddModelError("", $"Seats {string.Join(", ", alreadyBookedSeats)} are already booked.");
                await transaction.RollbackAsync();
                return await Book(showId, theaterId, date);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Message"] = $"Seats {string.Join(", ", selectedSeats)} successfully booked!";
            return RedirectToAction("Payment", "CinePlus", new { showId, theaterId, date });
        }

        private string GenerateTransactionId()
        {
            return "TRN" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
        }

        [HttpGet]
        public IActionResult Payment(int showId, int theaterId, string date)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login");
            }
            var ticketPrice = db.Theaters.Where(t => t.Tid == theaterId).FirstOrDefault();

            var Seat = db.Seats.Where(s => s.TheaterId == theaterId).FirstOrDefault();

            var ticketId = db.Tickets.Where(t => t.ShowId == showId).FirstOrDefault();

            var Seat_Qty = db.Seats.Where(s => s.TheaterId == theaterId).ToList().Count;

            var model = new Allpayments
            {
                Payment = new Payment
                {
                    SeatId = Seat.SeatId,
                    MovieId = (int)ticketId.MovieId,
                    TheaterId = theaterId,
                    Ticketid = ticketId.Ticketid,
                    ShowId = showId,
                    TotalAmount = (decimal)(ticketPrice.Price * Seat_Qty),
                    PaymentDate = Convert.ToDateTime(date)
                },
                SelectedPaymentType = "UPI"  // set as default payment
            };

            ViewBag.SeatId = model.Payment.SeatId;
            ViewBag.MovieId = model.Payment.MovieId;
            ViewBag.TheaterId = theaterId;
            ViewBag.Ticketid = ticketId.Ticketid;
            ViewBag.ShowId = showId;

            ViewBag.Timings = db.ShowTimes.Where(s => s.ShowId == showId).Select(s => s.Timings).FirstOrDefault();

            ViewBag.TotalAmount = model.Payment.TotalAmount;



            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Payment(Allpayments model)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login");
            }
            try
            {
                using var db = new CinePlusContext();

                // Validate seat exists
                var seat = await db.Seats.FindAsync(model.Payment.SeatId);
                if (seat == null)
                {
                    ModelState.AddModelError("", "Selected seat does not exist.");
                    return View(model);
                }

                // Validate show exists
                var show = await db.ShowTimes.FindAsync(model.Payment.ShowId);
                if (show == null)
                {
                    ModelState.AddModelError("", "Selected show does not exist.");
                    return View(model);
                }

                using var transaction = await db.Database.BeginTransactionAsync();

                try
                {
                    // Create and save payment with initial "Ongoing" status
                    var payment = new Payment
                    {
                        SeatId = model.Payment.SeatId,
                        MovieId = model.Payment.MovieId,
                        TheaterId = model.Payment.TheaterId,
                        Ticketid = model.Payment.Ticketid,
                        ShowId = model.Payment.ShowId,
                        Status = "Ongoing",
                        PaymentType = model.SelectedPaymentType,
                        TotalAmount = model.Payment.TotalAmount,
                        PaymentDate = DateTime.Now // Use current time rather than model value
                    };

                    await db.Payments.AddAsync(payment);
                    await db.SaveChangesAsync(); // Get generated Pid

                    // Process payment based on type
                    string transactionId = GenerateTransactionId();
                    bool paymentProcessed = false;

                    switch (model.SelectedPaymentType)
                    {
                        case "UPI":
                            paymentProcessed = await ProcessUpiPayment(db, model.Upi, payment.Pid, transactionId);
                            break;

                        case "Card":
                            paymentProcessed = await ProcessCardPayment(db, model.Card, payment.Pid, transactionId);
                            break;

                        default:
                            ModelState.AddModelError("", "Invalid payment method selected.");
                            break;
                    }

                    if (paymentProcessed)
                    {
                        // Update payment status to Success
                        payment.Status = "Success";
                        db.Payments.Update(payment);
                        await db.SaveChangesAsync();

                        // Create booking record
                        var booking = new Booking
                        {
                            Pid = payment.Pid,
                            MovieId = payment.MovieId,
                            BookingDate = DateOnly.FromDateTime(DateTime.Now),
                            ShowId = payment.ShowId,
                            Status = "Confirmed",
                            Tid = payment.TheaterId,
                            UserId = HttpContext.Session.GetInt32("UserId"), // Implement this method
                            SeatNumbers = seat.SeatNumber,
                            TicketId = payment.Ticketid,
                            ShowTime = show.Timings
                        };

                        await db.Bookings.AddAsync(booking);
                        await db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        TempData["TransactionId"] = transactionId;
                        return RedirectToAction("PaymentConfirmation", new
                        {
                            status = "success",
                            paymentId = payment.Pid
                        });
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"Payment processing failed: {ex.Message}");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");
                return View(model);
            }
        }

        private async Task<bool> ProcessUpiPayment(CinePlusContext db, Upi upiModel, int pid, string transactionId)
        {
            if (string.IsNullOrWhiteSpace(upiModel?.UpiId))
            {
                ModelState.AddModelError("Upi.UpiId", "Please enter a valid UPI ID");
                return false;
            }

            var upiDetails = new Upi
            {
                Pid = pid,
                UpiId = upiModel.UpiId.Trim(),
                TransactionId = transactionId,
                PaymentTimestamp = DateTime.Now
            };

            await db.Upis.AddAsync(upiDetails);
            return true;
        }

        private async Task<bool> ProcessCardPayment(CinePlusContext db, Card cardModel, int pid, string transactionId)
        {
            if (cardModel == null ||
                string.IsNullOrWhiteSpace(cardModel.CardNumberMasked) ||
                string.IsNullOrWhiteSpace(cardModel.CardHolderName) ||
                string.IsNullOrWhiteSpace(cardModel.ExpiryMonth) ||
                string.IsNullOrWhiteSpace(cardModel.ExpiryYear) ||
                string.IsNullOrWhiteSpace(cardModel.CardCvv))
            {
                ModelState.AddModelError("", "Please fill all card details");
                return false;
            }

            var cardDetails = new Card
            {
                Pid = pid,
                CardNumberMasked = cardModel.CardNumberMasked.Trim(),
                CardHolderName = cardModel.CardHolderName.Trim(),
                ExpiryMonth = cardModel.ExpiryMonth.Trim(),
                ExpiryYear = cardModel.ExpiryYear.Trim(),
                CardCvv = cardModel.CardCvv.Trim(),
                CardType = cardModel.CardType ?? "Credit",
                TransactionId = transactionId,
                PaymentTimestamp = DateTime.Now
            };

            await db.Cards.AddAsync(cardDetails);
            return true;
        }

        [HttpGet]
        public IActionResult PaymentConfirmation(string status, int _paymentid)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login");
            }
            ViewBag.Status = status;
            ViewBag.TransactionId = TempData["TransactionId"];
            return View();
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
            Font font = new Font(FontFamily.GenericSerif, 24, FontStyle.Bold);
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
        public IActionResult LogOut()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

    }
}
