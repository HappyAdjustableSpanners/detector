using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Detector.Controllers
{
    public class TestController : Controller
    {
        // GET: Test
        public string Date()
        {
            return  DateTime.Now.ToString("HH:mm:ss dd/MM/yy");
        }
    }
}