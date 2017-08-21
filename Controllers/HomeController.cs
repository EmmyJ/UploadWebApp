using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Mvc;
//using UploadWebapp.dashbservice;
using UploadWebapp.DB;
using UploadWebapp.Models;
using UploadWebapp.usersitesservice;


namespace UploadWebapp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                return View();

            }
            else
                return RedirectToAction("Login", "Account");
        }

        public ActionResult PictureUpload()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";
                //UploadWebapp.Models.User user = UserDA.GetByUserID(UserDA.CurrentUserId);
                User user = UserDA.CurrentUser;
                user.sites = UserDA.GetSiteListForUser(UserDA.CurrentUserId);
                user.cameraSetups = ImageDA.GetCameraSetupsForUser(UserDA.CurrentUserId);

                HomeModel homeModel = new HomeModel();
                homeModel.user = user;
                //homeModel.images = new List<Image>();
                //homeModel.imageViewModel = new ImageViewModel();

                return View(homeModel);
            }
            else
                return RedirectToAction("Login", "Account");
        }

        public ActionResult PictureUploadMinCopy()
        {
            return View();
        }

        public ActionResult PictureUploadCopy()
        {
            //if (UserDA.CurrentUserId == null || UserDA.CurrentUserId == 0)
            //{
            //    Session["UserId"] = 127;
            //    Session["ICOSuser"] = true;
            //    Session["username"] = "database";
            //}
            ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";
            //UploadWebapp.Models.User user = UserDA.GetByUserID(UserDA.CurrentUserId);
            User user = new User();
            user.name = "database";
            user.sites = UserDA.GetSiteListForUser(127);
            user.cameraSetups = ImageDA.GetCameraSetupsForUser(127);

            HomeModel homeModel = new HomeModel();
            homeModel.user = user;
            //homeModel.images = new List<Image>();
            //homeModel.imageViewModel = new ImageViewModel();

            return View(homeModel);
            //}
            //else
            //    return RedirectToAction("Login", "Account");
        }

        public ActionResult PictureUploadResult()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {

                UploadSet uploadset = ImageDA.GetUploadSetByID(ImageDA.CurrentUploadSetId);
                return View(uploadset);
            }
            else
                return RedirectToAction("Login", "Account");
        }

        public ActionResult SetDownload(int setID)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                List<string> dataList = ImageDA.GetUploadSetData(setID);
                string fileContent = String.Join("\n", dataList).Replace('.', ',');
                if (!string.IsNullOrEmpty(fileContent))
                {
                    // Get the bytes for the dynamic string content
                    var byteArray = Encoding.ASCII.GetBytes(fileContent);

                    string fileName = "setData.csv";

                    return File(byteArray, "text/csv", fileName);
                }
                else
                    return RedirectToAction("Overview");

            }
            else
                return RedirectToAction("Login", "Account");
        }
        //protected internal virtual FileStreamResult File(Stream fileStream, string contentType, string fileDownloadName);

        public ActionResult SetDetails(int setID)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {

                UploadSet uploadset = ImageDA.GetUploadSetByID(setID);
                //uploadset.resultsSet = ImageDA.GetResultsSet(uploadset.ID);
                return View("", uploadset);
            }
            else
                return RedirectToAction("Login", "Account");
        }

        static EventWaitHandle _waitOneHandle = new AutoResetEvent(false);
        static void WaitOne()
        {
            System.Threading.Thread.Sleep(1000);
            _waitOneHandle.Set();
        }

        //[HttpPost]
        //public ActionResult SaveUploadedPhoto()
        //{
        //    return Json(new { Message = "Succes!" });
        //}

        [HttpPost]
        public ActionResult SaveUploadedFile()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                foreach (string fileID in Request.Files)
                {
                    HttpPostedFileBase file = Request.Files[fileID];
                    string siteCode = file.FileName.Substring(0, 6);
                    Site site = ImageDA.GetSiteByCode(siteCode);
                    if (site != null)
                    {
                        string pathString = ConfigurationManager.AppSettings["UploadFolder"].ToString() + site.siteCode + "/AGB";
                        bool isExists = System.IO.Directory.Exists(pathString);
                        if (!isExists)
                            System.IO.Directory.CreateDirectory(pathString);
                        var path = string.Format("{0}/{1}", pathString, file.FileName);
                        file.SaveAs(path);

                        FileUpload fileUpload = new Models.FileUpload();
                        fileUpload.siteID = site.ID;
                        fileUpload.userID = UserDA.CurrentUserId;
                        fileUpload.uploadDate = DateTime.Now;
                        fileUpload.filename = file.FileName;
                        fileUpload.filepath = path;
                        fileUpload.type = fileUpload.filename.Substring(16, 3);

                        StreamReader reader = new StreamReader(path);
                        int rownr = 0;
                        List<Plot> siteplots = ImageDA.GetPlotListForSite(site.ID);
                        List<Species> speciesList = ImageDA.GetSpeciesList();

                        //Read all the lines in the datafile and put them in a list
                        List<AGBuploadDetail> detailList = new List<AGBuploadDetail>();
                        while (!reader.EndOfStream)
                        {
                            rownr++;
                            var line = reader.ReadLine();
                            var values = line.Split(';');
                            if (rownr == 3)
                                fileUpload.uploader = values[1];

                            if (rownr > 4)
                            {
                                AGBuploadDetail detail = new AGBuploadDetail();

                                detail.plotname = values[0];
                                detail.treeNr = int.Parse(values[1]);
                                detail.DBH = double.Parse(values[2]);
                                detail.speciesID = speciesList.Find(s => s.name == values[3]).ID;
                                detailList.Add(detail);
                            }
                        }

                        //Determine the different plots in the list and add the items for this plot
                        List<string> plotnameList = detailList.Select(l => l.plotname).Distinct().ToList();
                        fileUpload.fileUploadPlots = new List<FileUploadPlot>();
                        foreach (string plotname in plotnameList)
                        {
                            int plotID;
                            if (siteplots.Exists(p => p.name == plotname))
                                plotID = siteplots.Find(p => p.name == plotname).ID;
                            //if plot is not in the database, create it first
                            else
                            {
                                Plot plot = new Plot();
                                plot.name = plotname;
                                plot.siteID = site.ID;
                                plotID = ImageDA.InsertPlot(plot);
                            }

                            FileUploadPlot uploadPlot = new FileUploadPlot();
                            uploadPlot.plotID = plotID;
                            uploadPlot.AGBdetails = detailList.Where(d => d.plotname == plotname).ToList();

                            fileUpload.fileUploadPlots.Add(uploadPlot);

                        }

                        ImageDA.SaveFileUpload(fileUpload);
                    }




                    //string previousplot = "";
                    //int previousplotID = 0;
                    //while (!reader.EndOfStream)
                    //{
                    //    rownr++;

                    //    var line = reader.ReadLine();
                    //    var values = line.Split(';');

                    //    if (rownr == 3)
                    //        fileUpload.uploader = values[1];
                    //    if (rownr > 4)
                    //    {
                    //        string plotname = values[0];
                    //        int plotID = siteplots.Find(p => p.name == plotname).ID;
                    //        if (plotID != 0)
                    //        {
                    //            int uploadplotID = plotlist.Find(p => p.plotID == plotID).ID;
                    //            if (uploadplotID != 0)
                    //            {
                    //                AGBuploadDetail detail = new AGBuploadDetail();
                    //                detail.treeNr = int.Parse(values[1]);
                    //                detail.DBH = double.Parse(values[2]);
                    //                detail.speciesID = speciesList.Find(s => s.name == values[3]).ID;

                    //                plotlist.Find(p => p.plotID == plotID).AGBdetails.Add(detail);
                    //            }
                    //            else
                    //            {
                    //                FileUploadPlot uploadPlot = new FileUploadPlot();
                    //                uploadPlot.plotID = plotID;
                    //                uploadPlot.AGBdetails = new List<AGBuploadDetail>();

                    //            }
                    //        }
                    //        else
                    //        { 
                    //            //create new plot in db
                    //        }
                    //    }
                    //}

                }
                return Json(new { Message = "Succes!" });
            }

            else
                return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        public ActionResult CreateSite(CreateSiteModel model)
        {
            return null;
        }

        [HttpPost]
        public ActionResult SaveUploadedPhoto()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {

                bool isSavedSuccessfully = true;
                string fName = "";
                UploadSet uploadSet = new UploadSet();
                uploadSet.siteID = Request.Params["siteID"] == "undefined" ? (int?)null : int.Parse(Request.Params["siteID"]);
                uploadSet.person = Request.Params["person"];
                uploadSet.siteName = Request.Params["siteName"] == "" ? null : Request.Params["siteName"];
                uploadSet.cameraSetup = new CameraSetup();
                uploadSet.cameraSetup.ID = Request.Params["cameraSetupID"] == "" ? 0 : int.Parse(Request.Params["cameraSetupID"]);
                uploadSet.cameraSetup.cameraType = Request.Params["cameraType"];
                uploadSet.cameraSetup.cameraSerial = Request.Params["cameraSerial"];
                uploadSet.cameraSetup.lensType = Request.Params["lensType"];
                uploadSet.cameraSetup.lensSerial = Request.Params["lensSerial"];
                uploadSet.cameraSetup.lensX = int.Parse(Request.Params["lensX"]);
                uploadSet.cameraSetup.lensY = int.Parse(Request.Params["lensY"]);
                uploadSet.cameraSetup.lensA = double.Parse(Request.Params["lensA"]);
                uploadSet.cameraSetup.lensB = double.Parse(Request.Params["lensB"]);
                uploadSet.cameraSetup.maxRadius = int.Parse(Request.Params["maxRadius"]);     
                uploadSet.uploadTime = DateTime.Now;
                uploadSet.userID = UserDA.CurrentUserId;
                uploadSet.plotSets = new List<PlotSet>();
                List<Plot> plotList = new List<Plot>();
                Site site = null;
                PlotSet plotset = null;
                if (!UserDA.CurrentUserFree)
                {
                    string plots = Request.Params["plots"];
                    string[] plotsArray = plots.Split(';');
                    for (int i = 0; i < plotsArray.Count() - 1; i++)
                    {
                        string[] ar = plotsArray[i].Split('_');
                        Plot plot = new Plot();
                        plot.name = ar[0];
                        plot.slope = double.Parse(ar[1]);
                        plot.slopeAspect = double.Parse(ar[2]);

                        plot.ID = ImageDA.SavePlot(plot, uploadSet.siteID.Value);
                        plotList.Add(plot);
                    }

                    site = UserDA.GetSiteByID(uploadSet.siteID.Value);
                }
                else
                {
                    Plot plot = new Plot();
                    plot.name = Request.Params["plotNames"];
                    plot.slope = double.Parse(Request.Params["slope"]);
                    plot.slopeAspect = double.Parse(Request.Params["slopeAspect"]);
                    plot.ID = ImageDA.SavePlot(plot, null);
                    plotList.Add(plot);
                    plotset = new PlotSet();
                    plotset.uploadSetID = uploadSet.ID;
                    plotset.plotID = plot.ID;
                    plotset.images = new List<Image>();
                }

                //try
                //{
                foreach (string fileName in Request.Files)
                {
                    HttpPostedFileBase file = Request.Files[fileName];
                    string plotname = "";
                    int plotID = 0;
                    //Save file content goes here
                    bool newplotset = false;
                    if (file != null && file.ContentLength > 0)
                    {
                        fName = file.FileName;
                        if (!UserDA.CurrentUserFree)
                        {
                            plotname = fName.Substring(16, 1);
                            plotID = plotList.Find(p => p.name == plotname).ID;
                            plotset = uploadSet.plotSets.Find(p => p.plotID == plotID);
                            if (plotset == null)
                            {
                                plotset = new PlotSet();
                                plotset.uploadSetID = uploadSet.ID;
                                plotset.plotID = plotID;
                                plotset.images = new List<Image>();
                                newplotset = true;
                            }
                        }

                        string pathString;
                        if(!UserDA.CurrentUserFree)
                            pathString = ConfigurationManager.AppSettings["UploadFolder"].ToString() + site.siteCode + "/LAI"; 
                        else
                            pathString = ConfigurationManager.AppSettings["UploadFolder"].ToString() + uploadSet.userID + "/" + uploadSet.siteName + "/LAI"; 

                        var fileName1 = Path.GetFileName(file.FileName);

                        bool isExists = System.IO.Directory.Exists(pathString);
                        if (!isExists)
                            System.IO.Directory.CreateDirectory(pathString);

                        var path = string.Format("{0}/{1}", pathString, file.FileName);
                        file.SaveAs(path);

                        Image image = new Image();
                        image.filename = file.FileName;
                        image.path = path;
                        plotset.images.Add(image);
                        if (newplotset)
                            uploadSet.plotSets.Add(plotset);
                    }

                }
                if(UserDA.CurrentUserFree)
                    uploadSet.plotSets.Add(plotset);
                uploadSet = ImageDA.SaveUploadSet(uploadSet);
                ImageDA.CurrentUploadSetId = uploadSet.ID;

                if (isSavedSuccessfully)
                {

                    return Json(new { Message = "Succes!" });
                }
                else
                {
                    return Json(new { Message = "Error in saving file" });
                }
            }
            else
                return RedirectToAction("Login", "Account");
        }
        static EventWaitHandle _waitHandle = new AutoResetEvent(false);
        static void Waiter()
        {
            List<PlotSet> plotsets = new List<PlotSet>();
            bool succes = false;
            var startTime = DateTime.UtcNow;
            do
            {
                plotsets = ImageDA.GetResultsSet(rsetid);
                //succes = rset.processed;
                succes = !plotsets.Exists(p => p.resultsSet.processed == false);
                System.Threading.Thread.Sleep(1000);
            }
            while (succes == false && DateTime.UtcNow - startTime < TimeSpan.FromMinutes(3));
            _waitHandle.Set();
        }
        public static Thread T;
        static int rsetid;

        [HttpGet]
        public ActionResult GetETA(int setID)
        {
            int count = ImageDA.GetCountUnprocessed(setID);
            int time = Convert.ToInt32(ConfigurationManager.AppSettings["averageTime"].ToString());
            DateTime eta = DateTime.Now.AddSeconds((double)(count * time));

            return Json(eta.ToLocalTime().ToString("HH:mm"), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetResultsSet(int setid)
        {
            //rsetid = setid;
            //new Thread(Waiter).Start();
            //_waitHandle.WaitOne();
            List<PlotSet> plotsets = ImageDA.GetResultsSet(setid);
            return Json(plotsets, JsonRequestBehavior.AllowGet);
            //ManualResetEvent syncEvent = new ManualResetEvent(false);
            //if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            //{
            //    ResultsSet rset = new ResultsSet();
            //    T = new Thread((ThreadStart)delegate
            //    {
            //        bool succes = false;
            //        var startTime = DateTime.UtcNow;
            //        do
            //        {
            //            rset = ImageDA.GetResultsSet(setid);
            //            succes = rset.processed;
            //            //System.Threading.Thread.Sleep(1000);
            //            _waitHandle.WaitOne();
            //        }
            //        while (succes == false && DateTime.UtcNow - startTime < TimeSpan.FromMinutes(1));
            //        syncEvent.Set();
            //    });
            //    T.Start();
            //    syncEvent.WaitOne();
            //    return Json(rset, JsonRequestBehavior.AllowGet);
            //}
            //else
            //    return RedirectToAction("Login", "Account");

            //if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            //{
            //    ResultsSet rset = new ResultsSet();

            //    bool succes = false;
            //    var startTime = DateTime.UtcNow;
            //    do
            //    {
            //        rset = ImageDA.GetResultsSet(setid);
            //        succes = rset.processed;
            //        System.Threading.Thread.Sleep(1000);
            //    }
            //    while (succes == false && DateTime.UtcNow - startTime < TimeSpan.FromMinutes(1));

            //    return Json(rset, JsonRequestBehavior.AllowGet);
            //}
            //else
            //    return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public ActionResult GetCameraSetup(int setupid)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                CameraSetup cset = ImageDA.GetCameraSetupByID(setupid);

                return Json(cset, JsonRequestBehavior.AllowGet);
            }
            else
                return RedirectToAction("Login", "Account");
        }
        [HttpGet]
        public ActionResult GetPlotData(int siteID, string plotname)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                Plot plot = ImageDA.GetPlotByName(plotname, siteID);

                return Json(plot, JsonRequestBehavior.AllowGet);
            }
            else
                return RedirectToAction("Login", "Account");
        }

        public ActionResult About()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                ViewBag.Message = "Your app description page.";

                return View();
            }
            else
                return RedirectToAction("Login", "Account");
        }

        public ActionResult Contact()
        {
            UserSiteService uss = new UserSiteService();
            ViewBag.Message = "Your contact page." + uss.HelloWorld();
            return View();
        }

        public ActionResult Overview()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                return View(ImageDA.GetUploadSetsByUserID(UserDA.CurrentUserId));
            }
            else
                return RedirectToAction("Login", "Account");
        }

        public ActionResult FileUpload()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                return View();
            }
            else
                return RedirectToAction("Login", "Account");
        }

        public ActionResult Manage()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                User user = UserDA.CurrentUser;
                user.sites = UserDA.GetSiteListForUser(UserDA.CurrentUserId);
                return View(user);
            }
            else
                return RedirectToAction("Login", "Account");
        }
        public ActionResult AddSite()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                CreateSiteModel model = new CreateSiteModel();
                model.site = new Site();
                model.ecosystemList = ImageDA.GetEcosystemList();
                model.countryList = ImageDA.GetCountryList();
                return View(model);
            }
            else
                return RedirectToAction("Login", "Account");
        }

        public ActionResult CameraSetups()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                return View(ImageDA.GetCameraSetupsForUser(UserDA.CurrentUserId));
            }
            else
                return RedirectToAction("Login", "Account");
        }

        public ActionResult CreateCameraSetup()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                return View();
            }
            else
                return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        public ActionResult CreateCameraSetup(CameraSetup cameraSetup, HttpPostedFileBase pathCenter, HttpPostedFileBase pathProj)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                if (ModelState.IsValid)
                {
                    cameraSetup.userID = UserDA.CurrentUserId;
                    string pathString = ConfigurationManager.AppSettings["UploadFolder"].ToString() + "CameraSetups/" + cameraSetup.userID;
                    bool isExists = System.IO.Directory.Exists(pathString);
                    if (!isExists)
                        System.IO.Directory.CreateDirectory(pathString);
                    var path = string.Format("{0}/{1}", pathString, "centercalibration_" + DateTime.Today.Date.ToString("yyyyMMdd") + ".cvs");
                    pathCenter.SaveAs(path);
                    cameraSetup.pathCenter = path;

                    if (pathProj != null)
                    {
                        path = string.Format("{0}/{1}", pathString, "lenscalibration_" + DateTime.Today.Date.ToString("yyyyMMdd") + ".cvs");
                        pathProj.SaveAs(path);
                        cameraSetup.pathProj = path;
                        cameraSetup.lensA = 0;
                        cameraSetup.lensB = 0;
                    }
                    else
                    {
                        cameraSetup.pathProj = "no";
                        try
                        {
                            cameraSetup.lensA = double.Parse(cameraSetup.lensAstr);
                            cameraSetup.lensB = double.Parse(cameraSetup.lensBstr);
                        }
                        catch (Exception)
                        {
                            return View(cameraSetup);
                        }                        
                    }

                    cameraSetup = ImageDA.SaveCameraSetup(cameraSetup);

                    return RedirectToAction("CameraSetups");
                }
                else
                    return View(cameraSetup);
            }
            else
                return RedirectToAction("Login", "Account");
        }

        
        public ActionResult DeleteCameraSetup(int cameraSetupID)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                ImageDA.DisableCameraSetup(cameraSetupID);
                return View("CameraSetups", ImageDA.GetCameraSetupsForUser(UserDA.CurrentUserId));
            }
            else
                return RedirectToAction("Login", "Account");           
        }
    }
}