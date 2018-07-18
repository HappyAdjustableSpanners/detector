using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Web;

namespace Detector.common
{
    public class TrainingImageAttribute : RequiredAttribute 
    {
        public override bool IsValid(object value)
        {
            var files = value as HttpPostedFileBase[];
            if (files == null)
            {
                return false;
            }

         

            foreach(var file in files)
            {
                if(file == null)
                {
                    return false;
                }

                if (file.ContentLength > 1 * 1024 * 1024)
                {
                    return false;
                }

                try
                {
                    using (var img = Image.FromStream(file.InputStream))
                    {
                        return img.RawFormat.Equals(ImageFormat.Jpeg);
                    }
                }
                catch {

                }
            }
            return false;
        }
    }
}