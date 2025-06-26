using CinePlus.Controllers;
using CinePlus.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace CinePlus.Tests
{
    [TestFixture]
    public class OthersControllerTests
    {
        private CinePlusContext db;

        private IOthersController iOthersController;
        [SetUp]
        public void Setup()
        {
            var mock = new Mock<IOthersController>();

            iOthersController = mock.Object;

            var options = new DbContextOptionsBuilder<CinePlusContext>()
                .UseSqlServer("Data Source=WKSBAN36SUHTR26\\SQLEXPRESS;Initial Catalog=CinePlus;Integrated Security=True;Encrypt=False").Options;
            db = new CinePlusContext(options);
        }

        [Test]
        [TestCase("Thriller", 0)]
        [TestCase("Action", 0)]
        public void Add_Genre(string genres, int output)
        {
            var genre = new Genre();
            genre.Name = genres;

            // Create a mock IFormCollection to pass as the required parameter
            var formCollectionMock = new Mock<IFormCollection>();
            formCollectionMock.Setup(f => f["GenreName"]).Returns(new StringValues(genres));

            // Pass the mock IFormCollection to the Genre method
            iOthersController.Genre(formCollectionMock.Object);

            var result = db.SaveChanges();
            Assert.That(result, Is.EqualTo(output));
        }

        [Test]
        public void Add_Movie()
        {
            var AddMovie = new Movie()
            {
                MovieName = "Test Movie",
                GenreId = 1,
                Duration = "120 min",
                Description = "This is a test movie",
                ReleaseDate = new DateOnly(2023, 10, 1),
                MoviePoster = new byte[] { 0x20, 0x20, 0x20 }, // Example byte array for poster
                MovieCasts = new List<MovieCast>()
                {
                    new MovieCast
                    {
                        Actor = "Test Actor",
                        Actress = "Test Actress",
                        Director = "Test Director",
                        Producer = "Test Producer",
                        Musician = "Test Musician"
                    }
                },
            };
            db.Movies.Add(AddMovie);
            var result = db.SaveChanges();
            Assert.That(result, Is.EqualTo(2)); // Assuming 1 row is affected

        }
        [Test]
        public void Add_Language()
        {
            var lang = new Language()
            {
                Name = "TestLanguage"
            };
            db.Languages.Add(lang);
            var result = db.SaveChanges();
            Assert.That(result, Is.EqualTo(1)); // Assuming 1 row is affected  

        }
    }
}