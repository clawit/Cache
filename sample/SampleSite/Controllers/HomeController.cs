using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SampleSite.DataProvider;
using SampleSite.Models;

namespace SampleSite.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            bool hasStock = DataSample.HasStock(12345);
            var result = DataSample.Calc(1, 2);

            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
