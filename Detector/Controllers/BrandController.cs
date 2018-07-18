using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Detector.ViewModels;
using Detector.Models;
using System.IO;
using System.Diagnostics;
using System.Xml;

namespace Detector.Controllers
{
    public class BrandController : Controller
    {
        ApplicationDbContext _context = new ApplicationDbContext();

        public ViewResult New()
        {
            return View("BrandForm");
        }

        [HttpPost]
        public ActionResult Save(Brand brand)
        {
            if(!ModelState.IsValid)
            {
                var viewModel = new BrandFormViewModel
                {
                    brand = brand
                };
                return View("BrandForm", viewModel);
            }

            // work out if there is another in the dbcontext with same name
            var brandWithSameName = _context.brands.Where(b => b.Name == brand.Name).ToList();
            if(brandWithSameName.Count > 0)
            {
                return RedirectToAction("New");
            }

            if(brand.id == 0)
                _context.brands.Add(brand);
            else
            {
                var brandInDb = _context.brands.Single(b => b.id == brand.id);

                brandInDb.Name = brand.Name;
                brandInDb.trainingImages = brand.trainingImages;
            }
 
            _context.SaveChanges();
            SaveTrainingImages(brand);

            // Set up test and train subdirectories
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/data", brand.Name)));
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/data/data", brand.Name)));
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/data/training", brand.Name)));
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented/test/", brand.Name)));
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented/train/", brand.Name)));

            // Copy the model starting point
            string SourcePath =Server.MapPath("~/tf_model/faster_rcnn_inception_v2_coco_2017_11_08");
            string DestPath = Server.MapPath(String.Format("~/Storage/{0}/data/faster_rcnn_inception_v2_coco_2017_11_08", brand.Name));
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/data/faster_rcnn_inception_v2_coco_2017_11_08", brand.Name)));
            Copy(SourcePath, DestPath);

            //Copy the model config
            SourcePath = Server.MapPath("~/tf_model/training/faster_rcnn_inception_v2_coco.config");
            DestPath = Server.MapPath(String.Format("~/Storage/{0}/data/training/faster_rcnn_inception_v2_coco.config", brand.Name));
            System.IO.File.Copy(SourcePath, DestPath);

            return RedirectToAction("Index", "Home");
        }

        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        private void SaveTrainingImages(Brand brand)
        {
            // Save training images
            foreach (var image in brand.trainingImages)
            {
                // create dir 
                DirectoryInfo outDir = Directory.CreateDirectory(Server.MapPath("~/Storage/" + brand.Name + "/training-images/"));

                string filename = image.FileName;
                int lastSlash = filename.LastIndexOf("\\");
                string trailingPath = filename.Substring(lastSlash + 1);
                string fullPath = outDir.FullName + "\\" + trailingPath;
                image.SaveAs(fullPath);
            }
        }

        // Not using this anymore. Trying to find a more elegant solution to preventing brands with same name from being created with validation
        [AllowAnonymous]
        public JsonResult IsNameExists([Bind(Prefix = "brand.Name")] string Name, [Bind(Prefix = "brand.id")] int id)
        {
            // if we are not editing

            // if any brand has the same name, but a different id
            var result = Json(_context.brands.Where(b => b.Name == Name && b.id != id), JsonRequestBehavior.AllowGet);
            //var result = Json(!_context.brands.Any(x => x.Name == Name), JsonRequestBehavior.AllowGet);
            return result;
        }

        private void run_python_cmd(string cmd, string args, string workingDir = "")
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "C:\\Users\\jonny\\Miniconda3\\pythonw.exe";
            start.Arguments = string.Format("{0} {1}", cmd, args);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;

            if (!string.IsNullOrEmpty(workingDir))
            {
                start.WorkingDirectory = workingDir;
            }
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.Write(result);
                }
                using (StreamReader reader = process.StandardError)
                {
                    string result = reader.ReadToEnd();
                    Console.Write(result);
                }
                process.WaitForExit();
            }
        }

        private void run_python_cmd_async(string cmd, string args, string brandName, string workingDir = "")
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "C:\\Users\\jonny\\Miniconda3\\pythonw.exe";
            start.Arguments = string.Format("{0} {1}", cmd, args);
            start.CreateNoWindow = true;

            if (!string.IsNullOrEmpty(workingDir))
            {
                start.WorkingDirectory = workingDir;
            }

            Process process = new Process();
            process.EnableRaisingEvents = true;
            process.StartInfo = start;
            process.Exited += (sender, e) => p_Exited(sender, e, brandName); ;
            process.Start();
        }

        void p_Exited(object sender, System.EventArgs e, string brandName)
        {
            Console.Write("exited");
            ExportGraph(brandName);
        }

        public ActionResult Train(int id, string name)
        {
            // Steps to take before training
            // Augment images
            // Convert xml to csv
            // Generate TFRecords

            // Set up a folder for this brands augmented images
            string dir = String.Format("~/Storage/{0}/training-images-augmented", name);
            DirectoryInfo augDir = Directory.CreateDirectory(Server.MapPath(dir));

          

            GenerateImgAugmentations(name);
            ConvertXMLtoCSV(name);
            GenerateTFRecords(name);
            CreateLabelPbTextFile(name);
            StartTraining(name);
           
            // Lame


           return RedirectToAction("Index", "Home");
        }

        private void ExportGraph(string brandName)
        {
            string cmd = Server.MapPath("~/tf_model/research/object_detection/export_inference_graph.py");
            string workingDir = Server.MapPath(String.Format("~/Storage/{0}/data", brandName));
            string args = String.Format("--input_type image_tensor --pipeline_config_path training/faster_rcnn_inception_v2_coco.config --trained_checkpoint_prefix training/model.ckpt-10 --output_directory graph", brandName);
            run_python_cmd(cmd, args, workingDir);
        }

        private void StartTraining(string brandName)
        {
            string cmd = Server.MapPath("~/tf_model/research/object_detection/train.py");
            string args = "--logtostderr --train_dir=training/ --pipeline_config_path=training/faster_rcnn_inception_v2_coco.config";
            string workingDir = Server.MapPath(String.Format("~/Storage/{0}/data", brandName ));
            run_python_cmd_async(cmd, args, brandName, workingDir);
        }

        private void CreateLabelPbTextFile(string brandName)
        {
            string[] lines = { "item {", "id: 1", String.Format("name: '{0}'", brandName), "}" };
            System.IO.File.WriteAllLines(Server.MapPath(String.Format("~/Storage/{0}/data/data/object-detection.pbtxt", brandName)), lines);
        }
        
        private void GenerateTFRecords(string brandName)
        {
            string cmd = Server.MapPath("~/python/generate-tfrecord.py");
            string args = String.Format("--csv_input={0} --output_path={1} --brandName={2} --imagePath={3}", Server.MapPath(String.Format("~/Storage/{0}/data/data/test_labels.csv", brandName)), Server.MapPath(String.Format("~/Storage/{0}/data/data/test.record", brandName)), brandName, Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented", brandName)));
            run_python_cmd(cmd, args);
            args = String.Format("--csv_input={0} --output_path={1} --brandName={2} --imagePath={3}", Server.MapPath(String.Format("~/Storage/{0}/data/data/train_labels.csv", brandName)), Server.MapPath(String.Format("~/Storage/{0}/data/data/train.record", brandName)), brandName, Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented", brandName)));
            run_python_cmd(cmd, args);
        }

        private void ConvertXMLtoCSV(string brandName)
        {
            string cmd = Server.MapPath("~/python/xml-to-csv.py");
            string csvOutPath = Server.MapPath(String.Format("~/Storage/{0}/data/data", brandName));
            string annotationPath = Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented", brandName));
            string args = String.Format("--annotationPath={0} --outputPath={1}", annotationPath, csvOutPath);

            run_python_cmd(cmd, args);

            // Get all xml files
            //string[] test = System.IO.Directory.GetFiles(Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented/test", brandName)), "*.xml");
            //string[] train = System.IO.Directory.GetFiles(Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented/train", brandName)), "*.xml");

            // foreach test file, find all the object tags

        }

        private void GenerateImgAugmentations(string brandName)
        {
            // Assign the in and out path for the augmentation script
            string augOutPath = Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented/", brandName));
            string inPath = Server.MapPath(String.Format("~/Storage/{0}/training-images/", brandName));
           
            // Call imgaug
            string args = augOutPath + " " + inPath + " " + brandName;
            string cmd = Server.MapPath("~/imgaug/generate_augs.py");
            run_python_cmd(cmd, args);
        }

        public ActionResult Delete(int id)
        {
            var brand = _context.brands.SingleOrDefault(c => c.id == id);
            _context.brands.Remove(brand);
            _context.SaveChanges();

            // also remove stored data for that brand
            string path = Server.MapPath(String.Format("~/Storage/{0}/", brand.Name));
            Directory.Delete(path, true);

            return RedirectToAction("Index", "Home");
        }

        public ActionResult Edit(int id)
        {
            var brand = _context.brands.SingleOrDefault(c => c.id == id);

            if (brand == null)
                return HttpNotFound();

            var viewModel = new BrandFormViewModel
            {
                brand = brand
            };
            return View("BrandForm", viewModel);
        }

     
    }
}