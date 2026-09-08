using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication_07._09._2026.Models;

namespace WebApplication_07._09._2026.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            HttpContext.Session.SetString("UserName", "ќлег");
            HttpContext.Session.SetInt32("UserAge", 25);

            string name = HttpContext.Session.GetString("UserName");
            int? age = HttpContext.Session.GetInt32("UserAge"); // ? - это говорить о том что может быть нон

            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "WoW";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult CalculateAge(UserModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CurrentYear = DateTime.Now.Year;
                return View("Index", model);
            }
            int age = DateTime.Now.Year - model.BirthYear;

            var result = new AgeResultModel
            {
                UserName = model.Name,
                Age = age,
            };
            HttpContext.Session.SetString("UserName", model.Name);
            HttpContext.Session.SetInt32("UserName", age);
            return View("AgeResult", result);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
