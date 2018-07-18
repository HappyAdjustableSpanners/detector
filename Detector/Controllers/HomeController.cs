using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Detector.Models;
using Detector.ViewModels;

namespace Detector.Controllers
{
    public class HomeController : Controller
    {
        // GET: DetectObject

        public ApplicationDbContext _context = new ApplicationDbContext();
        public ViewResult Index()
        {
            var brands = _context.brands.ToList();

            var viewModel = new HomeViewModel
            {
                brands = brands
            };

            return View(viewModel);
        }

    }
}