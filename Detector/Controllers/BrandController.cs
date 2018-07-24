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
using Hangfire;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using Ionic.Zip;

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

            if (brand.id == 0)
            {
                var id = User.Identity.GetUserId();
                brand.userId = id;
                _context.brands.Add(brand);
            }
            else
            {
                var brandInDb = _context.brands.Single(b => b.id == brand.id);
                brandInDb.Name = brand.Name;
                brandInDb.trainingImages = brand.trainingImages;
            }
 
            _context.SaveChanges();
            SaveTrainingImages(brand);

            // Set up test and train subdirectories
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/data", brand.id)));
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/data/data", brand.id)));
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/data/graph", brand.id)));
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/data/training", brand.id)));
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/data/trainingoutput", brand.id)));
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented/test/", brand.id)));
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented/train/", brand.id)));
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented/debug/", brand.id)));


            // Copy the model starting point
            string SourcePath =Server.MapPath("~/tf_model/faster_rcnn_inception_v2_coco_2017_11_08");
            string DestPath = Server.MapPath(String.Format("~/Storage/{0}/data/faster_rcnn_inception_v2_coco_2017_11_08", brand.id));
            Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/data/faster_rcnn_inception_v2_coco_2017_11_08", brand.id)));
            Copy(SourcePath, DestPath);

            //Copy the model config
            SourcePath = Server.MapPath("~/tf_model/training/faster_rcnn_inception_v2_coco.config");
            DestPath = Server.MapPath(String.Format("~/Storage/{0}/data/training/faster_rcnn_inception_v2_coco.config", brand.id));
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

        public ActionResult DownloadGraph(int id)
        {
            // get the location of the graph for this id
            string path = Server.MapPath(String.Format("~/Storage/{0}/data/graph/", id));

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(path);
                zip.Save(path + "/graph.zip");
                return File(path + "/graph.zip", "application/zip", "graph.zip");
            }
        }

        private void SaveTrainingImages(Brand brand)
        {
            // Save training images
            foreach (var image in brand.trainingImages)
            {
                // create dir 
                DirectoryInfo outDir = Directory.CreateDirectory(Server.MapPath(String.Format("~/Storage/{0}/training-images/", brand.id)));

                string filename = image.FileName;
                int lastSlash = filename.LastIndexOf("\\");
                string trailingPath = filename.Substring(lastSlash + 1);
                string fullPath = outDir.FullName + "\\" + trailingPath;
                image.SaveAs(fullPath);
            }
        }

        private void killProcessesForBrand(int brandId)
        {
            string workingDir = String.Format("/Storage/{0}/data", brandId);
            var processes = Process.GetProcessesByName("pythonw");
            foreach ( var proc in processes) 
            {
               
                if(proc.StartInfo.WorkingDirectory.ToString().Contains(workingDir))
                {
                    proc.Kill();
                    
                }
            }
        }

        private void run_python_cmd(string cmd, string args, int brandId, string workingDir = "")
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "C:\\Users\\jonny\\Miniconda3\\pythonw.exe";
            start.Arguments = string.Format("{0} {1}", cmd, args);
            
            //start.UseShellExecute = false;
            //start.RedirectStandardOutput = true;
            //start.RedirectStandardError = true;

            if (!string.IsNullOrEmpty(workingDir))
            {
                start.WorkingDirectory = workingDir;
            }
            using (Process process = Process.Start(start))
            {

                AddBrandProcess(brandId, process.Id);
                //process.StartInfo.Verb = "test";
                //using (StreamReader reader = process.StandardOutput)
                //{
                //    string result = reader.ReadToEnd();
                //    Console.Write(result);
                //}
                //using (StreamReader reader = process.StandardError)
                //{
                //    string result = reader.ReadToEnd();
                //    Console.Write(result);
                //}

                process.WaitForExit();
            }
        }

        private void AddBrandProcess(int brandId, int processId)
        {
           // BrandProcess brandProcess = new BrandProcess();
            //brandProcess
        }

        public JsonResult Status()
        {
            // work out what status is for each brand
            string userId = User.Identity.GetUserId();
            List<Brand> brands = _context.brands.Where(b => b.userId == userId).ToList();
            List<string> statuses = new List<string>();

            for(int i = 0; i < brands.Count; i++)
            {
                // get dir for brand
                string baseDir = Server.MapPath(String.Format("~/Storage/{0}/", brands[i].id));
                string augDir = baseDir + "training-images-augmented\\test";
                string tfRecordDir = baseDir + "data\\data\\train.record";
                string trainDir = baseDir + "data\\trainingoutput";
                string graphDir = baseDir + "data\\graph";

                string status = "trained";
                bool graphExists = false;

                bool tfRecordFilesExist = System.IO.File.Exists(tfRecordDir);


                if(Directory.Exists(augDir))
                {
                    // if aug dir is empty and we don't have tfrecords yet, we are untrained
                    if (IsDirectoryEmpty(augDir) && !tfRecordFilesExist)
                    {
                        status = "untrained";
                    }

                    // if aug dir is not empty but we don't have tfrecords yet, we are generating augs
                    if (!IsDirectoryEmpty(augDir) && !tfRecordFilesExist)
                    {
                        status = "generating-augs";
                    }
                }

                if(System.IO.File.Exists(tfRecordDir))
                {
                    // if tfrecord file exists but graph does not, we are training
                    if(tfRecordFilesExist && IsDirectoryEmpty(graphDir) )
                    {
                        status = "training";
                    }
                    
                }

                statuses.Add(brands[i].id + " " + status);
                
            }

            return Json(statuses, JsonRequestBehavior.AllowGet);
        }

        public bool IsDirectoryEmpty(string path)
        {
            IEnumerable<string> items = Directory.EnumerateFileSystemEntries(path);
            using (IEnumerator<string> en = items.GetEnumerator())
            {
                return !en.MoveNext();
            }
        }

        private void CleanForBrand(int id)
        {
            ClearDir(String.Format(Server.MapPath("~/Storage/{0}/data/trainingoutput"), id));
            ClearDir(String.Format(Server.MapPath("~/Storage/{0}/training-images-augmented/test"), id));
            ClearDir(String.Format(Server.MapPath("~/Storage/{0}/training-images-augmented/train"), id));
            ClearDir(String.Format(Server.MapPath("~/Storage/{0}/training-images-augmented"), id), false);
            ClearDir(String.Format(Server.MapPath("~/Storage/{0}/data/data"), id));
            ClearDir(String.Format(Server.MapPath("~/Storage/{0}/data/graph"), id));
        }

        public ActionResult Train(int id, string name)
        {
            StopJobsForBrand(id);
            CleanForBrand(id);

            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromSeconds(1);   
            CreateLabelPbTextFile(name, id);
      
            var job1 = GenerateImgAugmentations(name, id);
            var job2 = ConvertXMLtoCSV(id, job1 );
            var job3 = GenerateTFRecords(id, name, job2);
            var job4 = StartTraining(id, job3);
            var job5 = ExportGraph(id, job4);
            AddBrandJob(job1, id);
           
            AddBrandJob(job2, id);
            AddBrandJob(job3, id);
            AddBrandJob(job4, id);
            AddBrandJob(job5, id);

            return RedirectToAction("Index", "Home");
        }

        private void StopJobsForBrand(int brandId)
        {
            killProcessesForBrand(brandId);

            // get all jobs for a brand
            List<BrandJob> jobs = _context.brandJobs.Where(j => j.brandId == brandId).ToList();

            // destroy their background jobs and remove them from brandjob table
            foreach (BrandJob j in jobs)
            {
                BackgroundJob.Delete(j.jobId);
                
                _context.brandJobs.Remove(j);
            }

            _context.SaveChanges();
        }

        private void AddBrandJob(string jobId, int brandId)
        {
            var brandJob = new BrandJob();
            brandJob.brandId = brandId;
            brandJob.jobId = jobId;
            _context.brandJobs.Add(brandJob);
            _context.SaveChanges();
        }

        private void ClearDir(string directory, bool recursive = true)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(directory);

            if (Directory.Exists(directory))
            {
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                if (recursive)
                {
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }
            }

            
        }
      
        private void CreateLabelPbTextFile(string brandName, int id)
        {
            string[] lines = { "item {", "id: 1", String.Format("name: '{0}'", brandName), "}" };
            System.IO.File.WriteAllLines(Server.MapPath(String.Format("~/Storage/{0}/data/data/object-detection.pbtxt", id)), lines);
        }
        
        private string GenerateImgAugmentations(string name, int id)
        {
            string outPath = Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented/", id));
            string inPath = Server.MapPath(String.Format("~/Storage/{0}/training-images/", id));
            string cmd = Server.MapPath("~/imgaug/generate_augs.py");
            string args = outPath + " " + inPath + " " + name;
            string workingDir = Server.MapPath(String.Format("~/Storage/{0}/", id));
            var jobId = BackgroundJob.Enqueue(() => ImgAugJob(cmd, args, id, workingDir, JobCancellationToken.Null));            
            return jobId;
        }

        private string ConvertXMLtoCSV(int id, string lastJob)
        {
            string cmd = Server.MapPath("~/python/xml-to-csv.py");
            string outPath = Server.MapPath(String.Format("~/Storage/{0}/data/data", id));
            string inPath = Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented", id));
            string args = String.Format("--annotationPath={0} --outputPath={1}", inPath, outPath);
            string workingDir = Server.MapPath(String.Format("~/Storage/{0}/", id));
            var jobId = BackgroundJob.ContinueWith(lastJob, () => XmlToCsvJob(cmd, args, id, workingDir,JobCancellationToken.Null));
            return jobId;
        }

        private string GenerateTFRecords(int id, string brandName, string lastJob)
        {
            string cmd = Server.MapPath("~/python/generate-tfrecord.py");
            string args = String.Format("--csv_input={0} --output_path={1} --brandName={2} --imagePath={3}", Server.MapPath(String.Format("~/Storage/{0}/data/data/test_labels.csv", id)), Server.MapPath(String.Format("~/Storage/{0}/data/data/test.record", id)), brandName, Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented", id)));
            string workingDir = Server.MapPath(String.Format("~/Storage/{0}/", id));
            var jobId1 = BackgroundJob.ContinueWith(lastJob, () => GenerateTFRecordsJob(cmd, args, id, workingDir, JobCancellationToken.Null));
            lastJob = jobId1;
            args = String.Format("--csv_input={0} --output_path={1} --brandName={2} --imagePath={3}", Server.MapPath(String.Format("~/Storage/{0}/data/data/train_labels.csv", id)), Server.MapPath(String.Format("~/Storage/{0}/data/data/train.record", id)), brandName, Server.MapPath(String.Format("~/Storage/{0}/training-images-augmented", id)));
            var jobId2 = BackgroundJob.ContinueWith(lastJob, () => GenerateTFRecordsJob(cmd, args, id, workingDir, JobCancellationToken.Null));
            return jobId2;
        }

        private string StartTraining(int id, string lastJob)
        {
            string cmd = Server.MapPath("~/tf_model/research/object_detection/train.py");
            string args = "--logtostderr --train_dir=trainingoutput/ --pipeline_config_path=training/faster_rcnn_inception_v2_coco.config";
            string workingDir = Server.MapPath(String.Format("~/Storage/{0}/data", id));
            //run_python_cmd_async(cmd, args, brandName, workingDir);
            var jobId = BackgroundJob.ContinueWith(lastJob, () => TrainJob(cmd, args, id, workingDir, JobCancellationToken.Null));
            return jobId;
        }

        private string ExportGraph(int id, string lastJob)
        {
            int numSteps = 10;
            string cmd = Server.MapPath("~/tf_model/research/object_detection/export_inference_graph.py");
            string workingDir = Server.MapPath(String.Format("~/Storage/{0}/data", id));
            string args = String.Format("--input_type image_tensor --pipeline_config_path training/faster_rcnn_inception_v2_coco.config --trained_checkpoint_prefix trainingoutput/model.ckpt-{0} --output_directory graph", numSteps.ToString());
            var jobId = BackgroundJob.ContinueWith(lastJob, () => ExportGraphJob(cmd, args, id, workingDir, JobCancellationToken.Null));
            return jobId;
        }

        // hangfire jobs
        public void ImgAugJob(string cmdPath, string args, int brandId, string workingDir, IJobCancellationToken ct)
        {        
            run_python_cmd(cmdPath, args, brandId, workingDir);
        }
        public void XmlToCsvJob(string cmdPath, string args, int brandId, string workingDir, IJobCancellationToken ct)
        {
            run_python_cmd(cmdPath, args, brandId, workingDir);
        }
        public void GenerateTFRecordsJob(string cmdPath, string args, int brandId, string workingDir, IJobCancellationToken ct)
        {
            run_python_cmd(cmdPath, args, brandId, workingDir);
        }
        public void TrainJob(string cmdPath, string args, int brandId, string workingDir, IJobCancellationToken ct)
        {
            run_python_cmd(cmdPath, args, brandId, workingDir);
        }
        public void ExportGraphJob(string cmdPath, string args, int brandId, string workingDir, IJobCancellationToken ct)
        {
            run_python_cmd(cmdPath, args, brandId, workingDir);
        }

        public ActionResult Delete(int id)
        {
            // stop all jobs relating to this brand before we delete
            StopJobsForBrand(id);
            
            var brand = _context.brands.SingleOrDefault(c => c.id == id);
            _context.brands.Remove(brand);
            _context.SaveChanges();

            // also remove stored data for that brand
            string path = Server.MapPath(String.Format("~/Storage/{0}/", id));
            ClearDir(path, true);
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