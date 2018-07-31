using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Detector.Models
{
    public class BrandJob
    {
        public int id { get; set; }
        public int brandId { get; set; }
        public int jobId { get; set; }
        public string cmd { get; set; }
    }
}