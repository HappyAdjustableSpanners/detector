using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using Detector.common;
using System.Web.Mvc;

namespace Detector.Models
{
    public class Brand
    {
        public int id { get; set; }

        [Required]
        [StringLength(50)]
        //[Remote("IsNameExists", "Brand", AdditionalFields="id", ErrorMessage = "A brand already exists with this name")]
        public string Name { get; set; }

        [TrainingImage(ErrorMessage = "Please ensure all images are in jpg format, and smaller than 1MB")]
        public IEnumerable<HttpPostedFileBase> trainingImages { get; set; }
    }
}