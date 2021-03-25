using ImageMagick;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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
                ViewBag.ETCuser = UserDA.CurrentUserETC;
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
                if (!user.isFreeUser)
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
                string fileContent = String.Join("\n", dataList);//.Replace('.', ',');
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

        public ActionResult DownloadUploadSetQualityChecks(int setID)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                List<string> dataList = ImageDA.GetUploadSetQualityChecksData(setID);


                if (dataList.Count > 1)
                {
                    string fileName = "DHP_QualityCheck_" + dataList[0] + ".csv";
                    dataList.RemoveAt(0);
                    string fileContent = String.Join("\n", dataList);
                    // Get the bytes for the dynamic string content
                    var byteArray = Encoding.ASCII.GetBytes(fileContent);

                    return File(byteArray, "text/csv", fileName);
                }
                else
                    return RedirectToAction("Overview");
            }
            else
                return RedirectToAction("Login", "Account");
        }

        public ActionResult DownloadDataForETC(int setID)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                List<ExportETCmodel> list = ImageDA.GetDataForETC(setID);
                string siteName = "";

                if (list.Count > 0)
                {
                    List<string> data = new List<string>();
                    data.Add("Filename,Site,Method,Camera Setup,Plot Name,Plot Location,Date,QC,QC_Motivation,QC_Comment,LAI,LAIe,Threshold_RC,Clumping_LX,Overexposure Value");

                    for (int i = 0; i < list.Count; i++)
                    {
                        string s;
                        ExportETCmodel m = list[i];
                        //TODO: REMOVE!!!
                        //m.siteName = "FA-Lso";
                        s = m.image.filename;
                        s += "," + m.siteName;
                        s += ",DHP";
                        s += "," + m.cameraSetupName;//.Substring(1);
                        s += "," + m.plotName;
                        s += "," + m.plotLocation;
                        s += "," + m.dateString;

                        string comment = "";
                        if (!m.qc.setupObjects || !string.IsNullOrEmpty(m.qc.setupObjectsComments))
                            comment = "Setup Objects: " + m.qc.setupObjectsComments + "| ";
                        if (!m.qc.noForeignObjects || !string.IsNullOrEmpty(m.qc.foreignObjectsComments))
                            comment += "Foreign Objects: " + m.qc.foreignObjectsComments + "| ";
                        if (!m.qc.noRaindropsDirt || !string.IsNullOrEmpty(m.qc.raindropsDirtComments))
                            comment += "Raindrops/Dirt: " + m.qc.raindropsDirtComments + "| ";
                        if (!m.qc.noLensRing)
                            comment += "Lens Ring | ";
                        if (!m.qc.lighting || !string.IsNullOrEmpty(m.qc.lightingComments))
                            comment += "Lighting Conditions: " + m.qc.lightingComments + "| ";
                        if (!m.qc.noOverexposure || !string.IsNullOrEmpty(m.qc.overexposureComments))
                            comment += "Overexposure: " + m.qc.overexposureComments + "| ";
                        if (!m.qc.settings || !string.IsNullOrEmpty(m.qc.settingsComments))
                            comment += "Settings: " + m.qc.settingsComments.Replace(',', '.') + "| ";
                        if (!string.IsNullOrEmpty(m.qc.otherComments))
                            comment += "Other: " + m.qc.otherComments;
                        comment = comment.Trim();
                        comment = comment.Trim(new Char[] { '|' });

                        if (m.qc.status == QCstatus.pass)
                        {
                            s += ",0,," + comment;
                            s += "," + Math.Round(m.image.LAI.Value, 2).ToString().Replace(',', '.');
                            s += "," + Math.Round(m.image.LAIe.Value, 2).ToString().Replace(',', '.');
                            s += "," + Math.Round(m.image.threshold.Value, 3).ToString().Replace(',', '.');
                            s += "," + Math.Round(m.image.clumping.Value, 3).ToString().Replace(',', '.');
                            s += "," + Math.Round(m.image.overexposure.Value, 5).ToString().Replace(',', '.');
                        }
                        else if (m.qc.status == QCstatus.created)
                        {
                            s += ",1";
                            s += ", Multiple";
                            s += ", Not checked yet";
                            s += ",,,,,";
                        }
                        else
                        {
                            string motivation = "";


                            if (!m.qc.setupObjects)
                            {
                                motivation = "Setup";
                            }
                            if (!m.qc.noForeignObjects)
                            {
                                if (motivation == "")
                                    motivation = "Foreign";
                                else
                                    motivation = "Multiple";
                            }
                            if (!m.qc.noRaindropsDirt)
                            {
                                if (motivation == "")
                                    motivation = "Rain";
                                else
                                    motivation = "Multiple";
                            }
                            if (!m.qc.noLensRing)
                            {
                                if (motivation == "")
                                    motivation = "Ring";
                                else
                                    motivation = "Multiple";
                            }
                            if (!m.qc.lighting)
                            {
                                if (motivation == "")
                                    motivation = "Light";
                                else
                                    motivation = "Multiple";
                            }
                            if (!m.qc.noOverexposure)
                            {
                                if (motivation == "")
                                    motivation = "Overexp";
                                else
                                    motivation = "Multiple";
                            }
                            if (!m.qc.settings)
                            {
                                if (motivation == "")
                                    motivation = "Settings";
                                else
                                    motivation = "Multiple";
                            }
                            if (m.qc.otherComments != "")
                            {
                                if (motivation == "")
                                    motivation = "Other";
                                else
                                    motivation = "Multiple";
                            }

                            s += ",1";
                            s += "," + motivation;
                            s += "," + comment;
                            s += ",,,,,";
                        }

                        data.Add(s);
                        siteName = m.siteName;
                    }

                    DateTime now = DateTime.Now;
                    string fileName = String.Format("{0}_DHP-LAI_{1}.csv", siteName, now.ToString("yyyyMMddHHmmss"));
                    string fileContent = String.Join("\n", data);
                    var byteArray = Encoding.ASCII.GetBytes(fileContent);

                    //send to ETC
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings["ITurl"].ToString() + siteName + "/" + fileName);
                    request.Method = WebRequestMethods.Ftp.UploadFile;

                    request.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["ITuser"].ToString(), ConfigurationManager.AppSettings["ITpass"].ToString());

                    byte[] bytes = byteArray;
                    try
                    {
                        // Write the bytes into the request stream.
                        request.ContentLength = bytes.Length;
                        using (Stream request_stream = request.GetRequestStream())
                        {

                            request_stream.Write(bytes, 0, bytes.Length);
                            request_stream.Close();

                        }
                    }
                    catch (Exception)
                    //folder doesn't exist, so create folder and try again
                    {
                        request = (FtpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings["ITurl"].ToString() + siteName + "/");
                        request.Method = WebRequestMethods.Ftp.MakeDirectory;
                        request.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["ITuser"].ToString(), ConfigurationManager.AppSettings["ITpass"].ToString());
                        using (var resp = (FtpWebResponse)request.GetResponse())
                        {
                            if(resp.StatusCode == FtpStatusCode.PathnameCreated)
                            {
                                request = (FtpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings["ITurl"].ToString() + siteName + "/" + fileName);
                                request.Method = WebRequestMethods.Ftp.UploadFile;

                                request.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["ITuser"].ToString(), ConfigurationManager.AppSettings["ITpass"].ToString());

                                request.ContentLength = bytes.Length;
                                using (Stream request_stream = request.GetRequestStream())
                                {
                                    request_stream.Write(bytes, 0, bytes.Length);
                                    request_stream.Close();
                                }
                            }
                        }
                    }
                    Submission submission = new Submission();
                    submission.uploadSetID = setID;
                    submission.filename = fileName;
                    submission.userID = UserDA.CurrentUserId;
                    submission.submissionDate = now;

                    ImageDA.InsertSubmission(submission);

                    return File(byteArray, "text/csv", fileName);
                }

                return null;
            }
            else
                return RedirectToAction("Login", "Account");
        }

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

        public ActionResult rerunUploadSet(int setID)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0 && UserDA.CurrentUserETC)
            {
                ETCDA.rerunUploadSet(setID);
                return RedirectToAction("SetDetails", new { setID = setID });
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
                        plot.plotLocations = ImageDA.GetLocationListForPlot(plot.ID);
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
                    Plot plot = null;
                    //Save file content goes here
                    bool newplotset = false;
                    if (file != null && file.ContentLength > 0)
                    {
                        fName = file.FileName;
                        if (!UserDA.CurrentUserFree)
                        {
                            plotname = fName.Substring(14, 4);
                            plot = plotList.Find(p => p.name == plotname);
                            plotID = plot.ID;
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
                        if (!UserDA.CurrentUserFree)
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
                        int location = 0;
                        int.TryParse(file.FileName.Substring(20, 2), out location);
                        if (plot != null && plot.plotLocations != null && plot.plotLocations.Count != 0 && location != 0)
                        {
                            if (plot.plotLocations.Where(l => l.location == location).Count() > 1)
                            {
                                image.plotLocationID = plot.plotLocations.Where(l => l.location == location).OrderByDescending(m => m.insertDate).First().ID;
                            }
                            else
                                image.plotLocationID = plot.plotLocations.Where(l => l.location == location).Select(m => m.ID).SingleOrDefault();
                        }
                    }

                }
                if (UserDA.CurrentUserFree)
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
            //UserSiteService uss = new UserSiteService();
            //ViewBag.Message = "Your contact page." + uss.HelloWorld();
            return View();
        }

        public ActionResult Overview()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                OverviewModel model = new OverviewModel();
                model.uploadSets = ImageDA.GetUploadSetsByUserID(UserDA.CurrentUserId);
                model.isETCuser = UserDA.CurrentUserETC;
                model.siteList = new Dictionary<int, string>();
                model.yearList = new List<int>();
                foreach(UploadSet u in model.uploadSets.OrderBy(u => u.siteCode))
                {
                    if (!model.siteList.ContainsKey(u.siteID.Value))
                        model.siteList.Add(u.siteID.Value, u.siteCode);
                }
                model.selectedSite = string.IsNullOrEmpty(Request.Params["selectedSite"]) ? 0 : int.Parse(Request.Params["selectedSite"]);
                if (model.selectedSite != 0)
                {
                    model.uploadSets = model.uploadSets.Where(u => u.siteID == model.selectedSite).ToList();
                }
                foreach(UploadSet u in model.uploadSets.OrderBy(u => u.yearTaken))
                {
                    if (!model.yearList.Contains(u.yearTaken))
                        model.yearList.Add(u.yearTaken);
                }
                model.selectedYear = string.IsNullOrEmpty(Request.Params["selectedYear"]) ? 0 : int.Parse(Request.Params["selectedYear"]);
                if (model.selectedYear != 0)
                {
                    model.uploadSets = model.uploadSets.Where(u => u.yearTaken == model.selectedYear).OrderByDescending(v => v.dateTaken).ThenByDescending(v => v.uploadTime).ToList();
                }
                return View(model);
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
                CreateCamerSetupModel model = new CreateCamerSetupModel();
                User user = UserDA.CurrentUser;
                if (!user.isFreeUser)
                    user.sites = UserDA.GetSiteListForUser(UserDA.CurrentUserId);
                model.user = user;
                return View(model);
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


                    if (pathCenter != null)
                    {
                        var path = string.Format("{0}/{1}", pathString, "centercalibration_" + DateTime.Today.Date.ToString("yyyyMMdd") + ".csv");
                        pathCenter.SaveAs(path);
                        cameraSetup.pathCenter = path;
                        cameraSetup.lensX = 0;
                        cameraSetup.lensY = 0;
                    }
                    else
                    {
                        cameraSetup.pathCenter = "no";
                        try
                        {
                            cameraSetup.lensX = int.Parse(cameraSetup.lensXstr);
                            cameraSetup.lensY = int.Parse(cameraSetup.lensYstr);
                        }
                        catch (Exception)
                        {
                            return View(cameraSetup);
                        }
                    }

                    if (pathProj != null)
                    {
                        var path = string.Format("{0}/{1}", pathString, "lenscalibration_" + DateTime.Today.Date.ToString("yyyyMMdd") + ".csv");
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

        class laiData
        {
            public string filename { get; set; }
            public double? LAI { get; set; }
            public double? LAIe { get; set; }
            public double? threshold { get; set; }
            public double? clumping { get; set; }
            public double? overexposure { get; set; }
        }

        public ActionResult GenerateQualityChecksForUploadSet(int setID)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0 && UserDA.CurrentUserETC)
            {
                UploadSet uploadSet = ImageDA.GetUploadSetByID(setID);
                List<QualityCheck> qualityChecks = new List<QualityCheck>();

                foreach (PlotSet plotSet in uploadSet.plotSets)
                {
                    List<Image> images = plotSet.images;
                    List<laiData> lais = new List<laiData>();

                    if (plotSet.resultsSet.processed && !string.IsNullOrEmpty(plotSet.resultsSet.data) && plotSet.images[0].LAI == null)
                    {
                        string dataString = plotSet.resultsSet.data;
                        string[] split = plotSet.resultsSet.data.Split('\n');
                        for (int i = 1; i < split.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(split[i]))
                            {
                                string[] split2 = split[i].Split(',');
                                laiData lai = new laiData();
                                lai.filename = !string.IsNullOrEmpty(split2[0]) ? split2[0] : "";
                                lai.LAI = !string.IsNullOrEmpty(split2[1]) ? double.Parse(split2[1], CultureInfo.InvariantCulture) : (double?)null;
                                lai.LAIe = !string.IsNullOrEmpty(split2[2]) ? double.Parse(split2[2], CultureInfo.InvariantCulture) : (double?)null;
                                lai.threshold = !string.IsNullOrEmpty(split2[3]) ? double.Parse(split2[3], CultureInfo.InvariantCulture) : (double?)null;
                                lai.clumping = !string.IsNullOrEmpty(split2[4]) ? double.Parse(split2[4], CultureInfo.InvariantCulture) : (double?)null;
                                if (split2.Length > 5)
                                    lai.overexposure = !string.IsNullOrEmpty(split2[5]) ? double.Parse(split2[5], CultureInfo.InvariantCulture) : (double?)null;
                                else
                                    lai.overexposure = (double?)null;

                                lais.Add(lai);
                            }
                        }
                    }

                    foreach (Image image in images)
                    {
                        QualityCheck qc = new QualityCheck();
                        qc.imageID = image.ID;
                        qc.status = QCstatus.created;
                        qc.dateModified = DateTime.Now;
                        qc.userID = UserDA.CurrentUserId;
                        qc.setupObjects = false;
                        qc.noForeignObjects = false;
                        qc.noRaindropsDirt = false;
                        qc.noLensRing = false;
                        qc.lighting = false;
                        qc.noOverexposure = false;
                        qc.image = image;

                        var res = lais.Where(l => l.filename == image.filename);
                        if (res.Any())
                        {
                            laiData lai = res.First();
                            qc.image.LAI = lai.LAI;
                            qc.image.LAIe = lai.LAIe;
                            qc.image.threshold = lai.threshold;
                            qc.image.clumping = lai.clumping;
                            qc.image.overexposure = lai.overexposure;
                        }
                        //qc.ID = 
                        ImageDA.insertQualityCheck(qc);
                        //qualityChecks.Add(qc); 
                    }
                }
                ImageDA.setUploadSetQualityCheck(setID);

                return RedirectToAction("UploadSetQualityChecks", new { setID = setID });
            }
            else
                return RedirectToAction("Login", "Account");
        }

        public ActionResult FillImageData()
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0 && UserDA.CurrentUserETC)
            {
                List<UploadSet> us = ImageDA.getUploadSetList();
                foreach (UploadSet uploadSet in us)
                {
                    FillImageData(uploadSet);
                }

                return View();
            }
            else
                return RedirectToAction("Login", "Account");
        }

        public ActionResult FillImageDataUS(int id)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0 && UserDA.CurrentUserETC)
            {
                UploadSet us = ImageDA.GetUploadSetByID(id);
                FillImageData(us);
                return View();
            }
            else
                return RedirectToAction("Login", "Account");
        }

        private static void FillImageData(UploadSet uploadSet)
        {
            foreach (PlotSet plotSet in uploadSet.plotSets)
            {
                List<Image> images = plotSet.images;
                List<laiData> lais = new List<laiData>();

                if (plotSet.resultsSet.processed)
                {
                    string dataString = plotSet.resultsSet.data;
                    string[] split = plotSet.resultsSet.data.Split('\n');
                    for (int i = 1; i < split.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(split[i]))
                        {
                            string[] split2 = split[i].Split(',');
                            laiData lai = new laiData();
                            lai.filename = !string.IsNullOrEmpty(split2[0]) ? split2[0] : "";
                            lai.LAI = !string.IsNullOrEmpty(split2[1]) ? double.Parse(split2[1], CultureInfo.InvariantCulture) : (double?)null;
                            lai.LAIe = !string.IsNullOrEmpty(split2[2]) ? double.Parse(split2[2], CultureInfo.InvariantCulture) : (double?)null;
                            lai.threshold = !string.IsNullOrEmpty(split2[3]) ? double.Parse(split2[3], CultureInfo.InvariantCulture) : (double?)null;
                            lai.clumping = !string.IsNullOrEmpty(split2[4]) ? double.Parse(split2[4], CultureInfo.InvariantCulture) : (double?)null;
                            if (split2.Length > 5)
                                lai.overexposure = !string.IsNullOrEmpty(split2[5]) ? double.Parse(split2[5], CultureInfo.InvariantCulture) : (double?)null;
                            else
                                lai.overexposure = (double?)null;

                            lais.Add(lai);
                        }
                    }
                }

                foreach (Image image in images)
                {
                    var res = lais.Where(l => l.filename == image.filename);
                    if (res.Any())
                    {
                        laiData lai = res.First();
                        image.LAI = lai.LAI;
                        image.LAIe = lai.LAIe;
                        image.threshold = lai.threshold;
                        image.clumping = lai.clumping;
                        image.overexposure = lai.overexposure;
                    }
                    ImageDA.saveImageData(image);
                }
            }
        }

        public ActionResult UploadSetQualityChecks(int setID)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                UploadSetQualityChecksModel model = new UploadSetQualityChecksModel();
                model.uploadSetID = setID;
                model = ImageDA.getCampaignAndSite(model);
                model.qualityChecks = ImageDA.getUploadSetQualityChecks(setID);
                model.year = int.Parse(model.qualityChecks[0].filename.Substring(23, 4));

                return View(model);
            }
            else
                return RedirectToAction("Login", "Account");
        }

        public ActionResult removeCampaign(int setID)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                ImageDA.removeCampaign(setID);
                return null;
            }

            else
                return RedirectToAction("Login", "Account");
        }
        public  ActionResult getNewCampaignCode(int setID, int siteID, int year) {
            string campaignCode = ImageDA.getNewCampaignCode(siteID, year);
            ImageDA.saveCampaign(setID, campaignCode);
            return Content(campaignCode);
        }

        public ActionResult attachCampaignToPrevious(int setID, int siteID, string dateTaken, string currentCampaign)
        {
            string campaignCode = ImageDA.attachCampaignToPrevious(setID, siteID, dateTaken, currentCampaign);
            return Content(campaignCode);
        }

        public ActionResult EditQualityCheck(int checkID, int setID)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                EditQualityCheckModel model = ImageDA.getQualityCheck(checkID, setID);
                Dictionary<string, string> exifs = new Dictionary<string, string>();

                if (model != null)
                {
                    model.previousQualityCheck = ImageDA.getPreviousQualityCheck(model.image);
                    model.uploadSetID = setID;

                    if (string.IsNullOrEmpty(model.image.exif) || model.image.exif == "todo")
                    {
                        //retrieve exif info
                        FileStream fileStream = new FileStream(model.image.path, FileMode.Open);
                        string exifstr = "key, value\n";

                        using (var image = new MagickImage(fileStream))
                        {
                            string fnumber = image.GetAttribute("exif:FNumber");
                            fnumber =  string.IsNullOrEmpty(fnumber) ? image.GetAttribute("dng:f.number") : fnumber;
                            exifstr += "EXIF FNumber, " + fnumber + "\n";
                            string exposure = image.GetAttribute("exif:ExposureTime");
                            exposure = string.IsNullOrEmpty(exposure) ? image.GetAttribute("dng:exposure.time") : exposure;
                            exifstr += "EXIF ExposureTime, " + exposure + "\n";
                            string ISO = image.GetAttribute("exif:ISOSpeedRatings");
                            ISO = string.IsNullOrEmpty(ISO) ? image.GetAttribute("dng:iso.setting") : ISO;
                            exifstr += "EXIF ISOSpeedRatings, " + ISO + "\n";
                        }

                        model.image.exif = exifstr;
                        fileStream.Close();
                        ImageDA.saveImageExif(model.image);
                    }

                    //read exif values
                    if (model.image.exif != null)
                    {

                        string[] e = model.image.exif.Split('\n');
                        for (int i = 1; i < e.Length - 1; i++)
                        {
                            string[] ei = e[i].Split(',');
                            exifs.Add(ei[0].Trim(), ei[1].Trim());
                        }

                        model.image.fNumberStr = exifs["EXIF FNumber"];
                        model.image.ISO = string.IsNullOrEmpty(exifs["EXIF ISOSpeedRatings"]) ? (float?)null : float.Parse(exifs["EXIF ISOSpeedRatings"].Replace('.', ','));
                        model.image.exposureTimeStr = exifs["EXIF ExposureTime"];

                        if (!string.IsNullOrEmpty(model.image.exposureTimeStr))
                        {
                            if (model.image.exposureTimeStr.Contains("/"))
                            {
                                string[] et = model.image.exposureTimeStr.Split('/');
                                float teller = float.Parse(et[0].Replace('.', ','));
                                float noemer = float.Parse(et[1].Replace('.', ','));
                                model.image.exposureTimeVal = teller / noemer;
                                model.image.exposureTimeStr = string.Format("{0}/{1}", teller.ToString(), noemer.ToString());
                            }
                            else
                                model.image.exposureTimeVal = float.Parse(model.image.exposureTimeStr.Replace('.', ','));
                        }
                        else
                            model.image.exposureTimeVal = null;

                        if (!string.IsNullOrEmpty(model.image.fNumberStr))
                        {
                            if (model.image.fNumberStr.Contains("/"))
                            {
                                string[] et = model.image.fNumberStr.Split('/');
                                float teller = float.Parse(et[0].Replace('.', ','));
                                float noemer = float.Parse(et[1].Replace('.', ','));
                                model.image.fNumber = teller / noemer;
                                model.image.fNumberStr = string.Format("{0}/{1}", teller.ToString(), noemer.ToString());
                            }
                            else { 
                                model.image.fNumber = float.Parse(model.image.fNumberStr.Replace('.', ','));
                                model.image.fNumberStr = model.image.fNumber.ToString();
                            }

                        }
                        else
                            model.image.fNumber = null;

                        //check if values are OK
                        if (model.qualityCheck.status == QCstatus.created)
                        {
                            if (model.image.ISO == null || model.image.fNumber == null || model.image.exposureTimeVal == null)
                            {
                                model.qualityCheck.settings = false;
                                model.qualityCheck.settingsComments += " EXIF info incomplete.";
                            }
                            else
                            {
                                if (model.image.ISO < 200 || model.image.ISO > 1000)
                                {
                                    model.qualityCheck.settings = false;
                                    model.qualityCheck.settingsComments += string.Format(" ISO: {0} not between 200 and 1000.", model.image.ISO);
                                }
                                if (model.image.fNumber != 8)
                                {
                                    model.qualityCheck.settings = false;
                                    model.qualityCheck.settingsComments += string.Format(" F-value: {0} <> 8.", model.image.fNumber.ToString().Replace(',', '.'));
                                }
                                if (model.image.exposureTimeVal > ((float.Parse("1") / (float.Parse("30")))))
                                {
                                    model.qualityCheck.settings = false;
                                    model.qualityCheck.settingsComments += string.Format(" Shutterspeed: {0} > 1/30", model.image.exposureTimeStr);
                                }
                            }
                            if (!model.qualityCheck.settings)
                                model.qualityCheck.status = QCstatus.fail;
                        }

                    }

                    return View(model);
                }
                else
                    return RedirectToAction("UploadSetQualityChecks", new { setID = setID });
            }
            else
                return RedirectToAction("Login", "Account");
        }


        public ActionResult Imagify(string url)
        {
            return File(@url, "image/jpeg");
        }

        [HttpPost]
        public ActionResult EditQualityCheck(EditQualityCheckModel qc)
        {
            if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
            {
                if (qc.qualityCheck.setupObjects && qc.qualityCheck.noForeignObjects && qc.qualityCheck.noRaindropsDirt && qc.qualityCheck.noLensRing && qc.qualityCheck.noOverexposure && qc.qualityCheck.lighting && qc.qualityCheck.settings && String.IsNullOrEmpty(qc.qualityCheck.otherComments))
                    qc.qualityCheck.status = QCstatus.pass;
                else
                    qc.qualityCheck.status = QCstatus.fail;

                ImageDA.SaveQualityCheck(qc.qualityCheck);

                return RedirectToAction("EditQualityCheck", new { checkID = qc.qualityCheck.ID + 1, setID = qc.uploadSetID });
            }
            else
                return RedirectToAction("Login", "Account");
        }

        public class ProcessImage
        {
            public string filename { get; set; }
            public string path { get; set; }
            public string siteCode { get; set; }
            public string plotName { get; set; }
            public string location { get; set; }
            public DateTime date { get; set; }
            public string cameraSetupName { get; set; }

            public int siteID { get; set; }
            public int plotSetID { get; set; }
        }

        public class PlotsPerSite
        {
            public int siteID { get; set; }
            public List<Plot> plotsList { get; set; }
        }

        public ActionResult ProcessImages()
        {
            try
            {
                List<SiteData> sites = ImageDA.GetUserSites(1);
                List<PlotsPerSite> plotsPerSite = new List<PlotsPerSite>();
                List<CameraSetup> cameraSetups = ImageDA.GetCameraSetupsForUser(1);
                List<UploadSet> uploadSets = new List<UploadSet>();
                UploadSet us = new UploadSet();
                DirectoryInfo d = new DirectoryInfo(ConfigurationManager.AppSettings["ProcessFolder"].ToString());
                DirectoryInfo ds = new DirectoryInfo(ConfigurationManager.AppSettings["UploadFolder"].ToString());
                FileInfo[] Files = d.GetFiles();
                List<String> res = new List<String>();
                List<String> erStr = new List<String>();

                bool error = false;
                string erMes = "";
                string pattern = "^[a-zA-Z]{2}-[a-zA-Z0-9]{3}_DHP_[a-zA-Z0-9]{2}_[SC]{1}P[0-9]{2}_L[0-9]{2}_[0-9]{8}.[a-zA-Z0-9]{3}$";
                Regex rg = new Regex(pattern);

                //create images
                List<ProcessImage> procImages = new List<ProcessImage>();
                foreach (FileInfo file in Files)
                {
                    //res.Add(string.Concat("{0}: {1}", image.filename, erMes));
                    if (rg.IsMatch(file.Name) && file.Name != "cpd.exe" && file.Name != "cpdcaller.ps1" && file.Name != "processcaller.ps1")
                    {
                        ProcessImage procIm = new ProcessImage();
                        procIm.filename = file.Name;
                        procIm.path = file.Directory.ToString();
                        procIm.siteCode = file.Name.Substring(0, 6);
                        procIm.siteID = sites.FirstOrDefault(s => s.siteCode == procIm.siteCode).ID;
                        procIm.plotName = file.Name.Substring(14, 4);
                        procIm.location = file.Name.Substring(20, 2);
                        procIm.date = DateTime.ParseExact(file.Name.Substring(23, 8), "yyyyMMdd", CultureInfo.InvariantCulture);
                        procIm.cameraSetupName = file.Name.Substring(11, 2);

                        procImages.Add(procIm);
                    }
                    //else
                    //{
                    //    if (file.Name != "cpd.exe" && file.Name != "cpdcaller.ps1" && file.Name != "processcaller.ps1") { 
                    //        erStr.Add(string.Format("{0}: {1}", file.Name, "Wrong filename format."));
                    //        try
                    //        {
                    //            System.IO.File.Move(System.IO.Path.Combine(file.Directory.ToString(), file.Name), System.IO.Path.Combine(file.Directory.ToString() + "/fail", file.Name));
                    //        }
                    //        catch
                    //        {
                    //            System.IO.File.Delete(System.IO.Path.Combine(file.Directory.ToString(), file.Name));
                    //        }
                    //    }
                    //}
                }
                //sort images
                procImages = procImages.OrderBy(p => p.siteCode).ThenBy(p => p.date).ThenBy(p => p.plotName).ToList();

                //catch plotlist for each used site 
                foreach (int s in procImages.Select(p => p.siteID).Distinct())
                {
                    PlotsPerSite pps = new PlotsPerSite();
                    pps.siteID = s;
                    pps.plotsList = ImageDA.GetPlotListForSite(s);
                    foreach (Plot p in pps.plotsList)
                    {
                        p.plotLocations = ImageDA.GetLocationListForPlot(p.ID);
                    }
                    plotsPerSite.Add(pps);
                }

                //process images
                foreach (ProcessImage procIm in procImages)
                {
                    error = false;
                    erMes = "";

                    //check if camerasetup exists
                    CameraSetup camSetup = cameraSetups.FirstOrDefault(c => c.siteID == procIm.siteID && c.name == procIm.cameraSetupName);
                    if (camSetup == null)
                    {
                        error = true;
                        erMes = string.Format("Camerasetup {0} for site {1} is missing.", procIm.cameraSetupName, procIm.siteCode);
                    }

                    else
                    {
                        //check if new uploadset is needed and create if so
                        if (uploadSets.Count == 0 || uploadSets.Last().siteCode != procIm.siteCode || uploadSets.Last().cameraSetup.name != procIm.cameraSetupName) // || uploadSets.Last().dateTaken != procIm.date)
                        {
                            us = new UploadSet();
                            us.siteCode = procIm.siteCode;
                            us.siteID = procIm.siteID;
                            us.siteName = procIm.siteCode;
                            us.userID = 1;
                            us.dateTaken = procIm.date;
                            us.cameraSetup = camSetup;
                            us.plotSets = new List<PlotSet>();
                            us.uploadTime = DateTime.Now;
                            us.person = procIm.siteCode + "_" + procIm.date.ToString("yyyyMMdd");

                            uploadSets.Add(us);
                        }
                        us = uploadSets.Last();
                        PlotSet ps = us.plotSets.Find(s => s.plotname == procIm.plotName); // TODO verschillende dates
                        if (ps == null)
                        {
                            ps = new PlotSet();
                            ps.uploadSetID = us.ID;
                            ps.plotname = procIm.plotName;
                            ps.images = new List<Image>();

                            Plot plot = plotsPerSite.Find(p => p.siteID == us.siteID).plotsList.Find(p => p.name == procIm.plotName);
                            if (plot == null)
                            {
                                plot = new Plot();
                                plot.insertDate = DateTime.Now;
                                plot.insertUser = 1;
                                plot.name = procIm.plotName;
                                plot.siteID = us.siteID.Value;
                                plot.slope = 0;
                                plot.slopeAspect = 0;
                                //TODO: slope & slopaspect
                                plot.ID = ImageDA.SavePlot(plot, null);
                            }
                            ps.plot = plot;
                            ps.plotID = plot.ID;

                            us.plotSets.Add(ps);
                        }

                        //if (!error)
                        {
                            Image image = new Image();
                            image.filename = procIm.filename;
                            string pathString = ds.ToString() + procIm.siteCode + "/LAI";
                            try
                            {

                                if (!System.IO.Directory.Exists(pathString))
                                    System.IO.Directory.CreateDirectory(pathString);

                                System.IO.File.Delete(System.IO.Path.Combine(pathString, procIm.filename));
                                System.IO.File.Move(System.IO.Path.Combine(procIm.path, procIm.filename), System.IO.Path.Combine(pathString, procIm.filename));
                            }
                            catch
                            {
                                System.IO.File.Delete(System.IO.Path.Combine(procIm.path, procIm.filename));
                            }
                            image.path = System.IO.Path.Combine(pathString, procIm.filename);
                            int location = 0;
                            int.TryParse(procIm.location, out location);
                            if (ps.plot.plotLocations != null && ps.plot.plotLocations.Count != 0 && location != 0)
                            {
                                if (ps.plot.plotLocations.Where(l => l.location == location).Count() > 1)
                                {
                                    image.plotLocationID = ps.plot.plotLocations.Where(l => l.location == location).OrderByDescending(m => m.insertDate).First().ID;  
                                }
                                else
                                    image.plotLocationID = ps.plot.plotLocations.Where(l => l.location == location).Select(m => m.ID).SingleOrDefault();
                            }
                            ps.images.Add(image);
                            res.Add(string.Format("{0}: {1}", image.filename, "succes"));
                        }
                    }
                    //else
                    //if(error)
                    //{
                    //    Image image = new Image();
                    //    image.filename = procIm.filename;
                    //    try
                    //    {
                    //        System.IO.File.Move(System.IO.Path.Combine(procIm.path, procIm.filename), System.IO.Path.Combine(procIm.path + "/fail", procIm.filename));
                    //    }
                    //    catch {
                    //        System.IO.File.Delete(System.IO.Path.Combine(procIm.path, procIm.filename));
                    //    }
                    //    image.path = System.IO.Path.Combine(procIm.path + "/fail", procIm.filename);

                    //    erStr.Add(string.Format("{0}: {1}", image.filename, erMes));
                    //}
                }

                foreach (UploadSet u in uploadSets)
                {
                    ImageDA.SaveUploadSet(u);
                }

                //if (erStr.Count > 0)
                //{
                //    res.Add("");
                //    res.Add("Following images were not processed:");
                //    res.Add("");

                //    string errorlog = string.Format("Time: {0}", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                //    errorlog += Environment.NewLine;
                //    errorlog += "-----------------------------------------------------------";
                //    errorlog += Environment.NewLine;

                //    foreach (string e in erStr)
                //    {
                //        res.Add(e);

                //        errorlog += e;
                //        errorlog += Environment.NewLine;
                //    }

                //    string path = System.IO.Path.Combine(ConfigurationManager.AppSettings["ProcessFolder"].ToString() + "/fail", "errorlog.txt");
                //    using (StreamWriter writer = new StreamWriter(path, true))
                //    {
                //        writer.WriteLine(errorlog);
                //        writer.Close();
                //    }
                //}

                return View(res);
            }
            catch (Exception e)
            {
                return View(e.ToString());
            }
        }

        //public ActionResult ProcessList()
        //{
        //    if (UserDA.CurrentUserId != null && UserDA.CurrentUserId != 0)
        //    {
        //        try
        //    {
        //        List<SiteData> sites = ImageDA.GetUserSites(1);
        //        List<PlotsPerSite> plotsPerSite = new List<PlotsPerSite>();
        //        List<CameraSetup> cameraSetups = ImageDA.GetCameraSetupsForUser(1);
        //        List<UploadSet> uploadSets = new List<UploadSet>();
        //        UploadSet us = new UploadSet();

        //        List<String> res = new List<String>();
        //        List<String> erStr = new List<String>();

        //        bool error = false;
        //        string erMes = "";
        //        string pattern = "^[a-zA-Z]{2}-[a-zA-Z0-9]{3}_DHP_[a-zA-Z0-9]{2}_[SC]{1}P[0-9]{2}_L[0-9]{2}_[0-9]{8}.[a-zA-Z0-9]{3}$";
        //        Regex rg = new Regex(pattern);

        //        List<Image> filelist = ImageDA.getFileList();

        //        //create images
        //        List<ProcessImage> procImages = new List<ProcessImage>();
        //        foreach (Image file in filelist)
        //        {
        //            //res.Add(string.Concat("{0}: {1}", image.filename, erMes));
        //            if (rg.IsMatch(file.filename) && file.filename != "cpd.exe" && file.filename != "cpdcaller.ps1" && file.filename != "processcaller.ps1")
        //            {
        //                ProcessImage procIm = new ProcessImage();
        //                procIm.filename = file.filename;
        //                procIm.path = file.path;
        //                procIm.siteCode = file.filename.Substring(0, 6);
        //                procIm.siteID = sites.FirstOrDefault(s => s.siteCode == procIm.siteCode).ID;
        //                procIm.plotName = file.filename.Substring(14, 4);
        //                procIm.date = DateTime.ParseExact(file.filename.Substring(23, 8), "yyyyMMdd", CultureInfo.InvariantCulture);
        //                procIm.cameraSetupName = file.filename.Substring(11, 2);

        //                procImages.Add(procIm);
        //            }
        //        }
        //        //sort images
        //        procImages = procImages.OrderBy(p => p.siteCode).ThenBy(p => p.date).ThenBy(p => p.plotName).ToList();

        //        //catch plotlist for each used site 
        //        foreach (int s in procImages.Select(p => p.siteID).Distinct())
        //        {
        //            PlotsPerSite pps = new PlotsPerSite();
        //            pps.siteID = s;
        //            pps.plotsList = ImageDA.GetPlotListForSite(s);
        //            plotsPerSite.Add(pps);
        //        }

        //        //process images
        //        foreach (ProcessImage procIm in procImages)
        //        {
        //            error = false;
        //            erMes = "";

        //            //check if camerasetup exists
        //            CameraSetup camSetup = cameraSetups.FirstOrDefault(c => c.siteID == procIm.siteID && c.name == procIm.cameraSetupName);
        //            if (camSetup == null)
        //            {
        //                error = true;
        //                erMes = string.Format("Camerasetup {0} for site {1} is missing.", procIm.cameraSetupName, procIm.siteCode);
        //            }

        //            else
        //            {
        //                //check if new uploadset is needed and create if so
        //                if (uploadSets.Count == 0 || uploadSets.Last().siteCode != procIm.siteCode || uploadSets.Last().cameraSetup.name != procIm.cameraSetupName) // || uploadSets.Last().dateTaken != procIm.date)
        //                {
        //                    us = new UploadSet();
        //                    us.siteCode = procIm.siteCode;
        //                    us.siteID = procIm.siteID;
        //                    us.siteName = procIm.siteCode;
        //                    us.userID = 1;
        //                    us.dateTaken = procIm.date;
        //                    us.cameraSetup = camSetup;
        //                    us.plotSets = new List<PlotSet>();
        //                    us.uploadTime = DateTime.Now;
        //                    us.person = procIm.siteCode + ConfigurationManager.AppSettings["mess"].ToString();

        //                    uploadSets.Add(us);
        //                }
        //                us = uploadSets.Last();
        //                PlotSet ps = us.plotSets.Find(s => s.plotname == procIm.plotName); 
        //                if (ps == null)
        //                {
        //                    ps = new PlotSet();
        //                    ps.uploadSetID = us.ID;
        //                    ps.plotname = procIm.plotName;
        //                    ps.images = new List<Image>();

        //                    Plot plot = plotsPerSite.Find(p => p.siteID == us.siteID).plotsList.Find(p => p.name == procIm.plotName);
        //                    if (plot == null)
        //                    {
        //                        plot = new Plot();
        //                        plot.insertDate = DateTime.Now;
        //                        plot.insertUser = 1;
        //                        plot.name = procIm.plotName;
        //                        plot.siteID = us.siteID.Value;
        //                        plot.slope = 0;
        //                        plot.slopeAspect = 0;
        //                        plot.ID = ImageDA.SavePlot(plot, null);
        //                    }
        //                    ps.plot = plot;
        //                    ps.plotID = plot.ID;

        //                    us.plotSets.Add(ps);
        //                }

        //                //if (!error)
        //                {
        //                    Image image = new Image();
        //                    image.filename = procIm.filename;
        //                    image.path = procIm.path;
        //                    ps.images.Add(image);
        //                    res.Add(string.Format("{0}: {1}", image.filename, "succes"));
        //                }
        //            }
        //        }

        //        foreach (UploadSet u in uploadSets)
        //        {
        //            ImageDA.SaveUploadSet(u);
        //        }

        //        return View(res);
        //    }
        //    catch (Exception e)
        //    {
        //        return View(e.ToString());
        //    }
        //    }
        //    else
        //        return RedirectToAction("Login", "Account");
        //}
    }
}