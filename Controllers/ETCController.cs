using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Web;
using System.Web.Mvc;
using UploadWebapp.DB;
using UploadWebapp.Models;
using UploadWebapp.Models.ETC;
using Site = UploadWebapp.Models.Site;

namespace UploadWebapp.Controllers
{
    public class ETCController : Controller
    {
        //
        // GET: /ETC/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult FillImagePlotLocations()
        {
            ETCDA.FillImagePlotLocations();

            return View();
        }

        //public ActionResult fillCampaign()
        //{
        //    ETCDA.fillCampaign();

        //    return View();
        //}

        public ActionResult fillImageDateTaken() {
            ETCDA.fillImageDateTaken();
            return View();
        }

        public ActionResult AggregateLAI(string year, bool download = true)
        {
            List<AggImage> images;
            if (String.IsNullOrEmpty(year))
                year = (DateTime.Now.Year - 1).ToString();

            ViewBag.Year = year;
            images = ETCDA.getAggregationImages(year);
            images = images.OrderBy(i => i.site).ThenBy(j => j.dateTaken).ThenBy(k => k.plotname).ThenBy(l => l.location).ThenByDescending(m => m.ID).ToList();
            List<String> siteNames = images.Select(i => i.site).Distinct().ToList();
            List<Site> sites = new List<Site>();
            foreach (string s in siteNames)
            {
                Site site = ImageDA.GetSiteByCode(s);
                sites.Add(site);
            }
            List<AggItem> list = new List<AggItem>();

            AggItem item = null;
            AggPlot plot = null;

            //images onderverdelen in groepen per site/date/plot
            foreach (AggImage image in images)
            {
                //only images from labelled sites
                Site siteData = sites.Where(s => s.siteCode == image.site).SingleOrDefault();
                if (siteData.labelled && image.dateTaken >= siteData.labelDate)
                {
                    if (item == null || item.siteCode != image.site || (image.dateTaken.Subtract(item.startDate).Days > 15))
                    {
                        item = new AggItem();
                        item.siteCode = image.site;
                        item.siteName = siteData.name;
                        item.startDate = image.dateTaken;
                        item.endDate = image.dateTaken;
                        item.plots = new List<AggPlot>();
                        list.Add(item);

                        plot = new AggPlot();
                        plot.plotname = image.plotname;
                        plot.images = new List<AggImage>();
                        item.plots.Add(plot);
                        plot.images.Add(image);
                    }
                    else
                    {
                        if (image.dateTaken > item.endDate)
                            item.endDate = image.dateTaken;

                        if (plot.plotname == image.plotname)
                            plot.images.Add(image);
                        else
                        {
                            plot = item.plots.Find(p => p.plotname == image.plotname);
                            if (plot == null)
                            {
                                plot = new AggPlot();
                                plot.plotname = image.plotname;
                                plot.images = new List<AggImage>();
                                item.plots.Add(plot);
                            }
                            plot.images.Add(image);
                        }
                    }
                }
            }


            foreach (AggItem aggitem in list)
            {
                foreach (AggPlot aggPlot in aggitem.plots)
                {
                    //disable duplicate images
                    AggImage previous = null;
                    aggPlot.images.OrderBy(i => i.location).ThenByDescending(i => i.ID);
                    foreach (AggImage im in aggPlot.images)
                    {
                        if (previous != null && previous.location == im.location)
                        {
                            im.QCstatus = Models.QCstatus.fail;
                        }
                        if (im.QCstatus == Models.QCstatus.pass)
                            previous = im;
                    }

                    //get plot counts and averages
                    aggPlot.totalNum = aggPlot.images.Select(i => i.location).Distinct().Count();
                    aggPlot.passNum = aggPlot.images.Where(i => i.QCstatus == Models.QCstatus.pass).Count();
                    if (aggPlot.plotname.Substring(0, 2) == "CP")
                        aggPlot.enough = aggPlot.passNum > 0 && (double)aggPlot.passNum / (double)aggPlot.totalNum > 0.7;
                    else
                        aggPlot.enough = aggPlot.passNum >= 4;



                    if (aggPlot.enough)
                    {
                        aggPlot.LAIaverage = aggPlot.images.Where(i => i.QCstatus == Models.QCstatus.pass).Average(i => i.LAI);
                        aggPlot.clumpAverage = aggPlot.images.Where(i => i.QCstatus == Models.QCstatus.pass).Average(i => i.clumping);
                    }

                }
                aggitem.LAIaverage = aggitem.plots.Where(p => p.enough).Average(p => p.LAIaverage);

                aggitem.clumpAverage = aggitem.plots.Where(p => p.enough).Average(p => p.clumpAverage);

                foreach (AggPlot pl in aggitem.plots)
                {
                    if (pl.enough)
                    {
                        pl.LAIdeviationSquare = Math.Pow(pl.LAIaverage.Value - aggitem.LAIaverage.Value, 2);
                        pl.clumpDeviationSquare = Math.Pow(pl.clumpAverage.Value - aggitem.clumpAverage.Value, 2);
                    }
                }

                int nr = aggitem.plots.Where(p => p.enough).Count();
                bool CP = aggitem.plots[0].plotname.Substring(0, 2) == "CP";
                if ((CP && nr >= 2 && nr <= 5) || (!CP && nr > 0 && nr <= 20))
                {
                    aggitem.LAIdeviation = Math.Sqrt(aggitem.plots.Where(p => p.enough).Average(p => p.LAIdeviationSquare.Value));
                    aggitem.clumpDeviation = Math.Sqrt(aggitem.plots.Where(p => p.enough).Average(p => p.clumpDeviationSquare.Value));
                }
                aggitem.LAIaverage = aggitem.LAIaverage.HasValue ? Math.Round(aggitem.LAIaverage.Value, 3) : (double?)null;
                aggitem.clumpAverage = aggitem.clumpAverage.HasValue ? Math.Round(aggitem.clumpAverage.Value, 3) : (double?)null;
                aggitem.LAIdeviation = aggitem.LAIdeviation.HasValue ? Math.Round(aggitem.LAIdeviation.Value, 3) : (double?)null;
                aggitem.clumpDeviation = aggitem.clumpDeviation.HasValue ? Math.Round(aggitem.clumpDeviation.Value, 3) : (double?)null;
            }


            if (list.Where(l => l.plots.Count() > 0).Count() > 0)
            {
                List<string> data = new List<string>();
                data.Add("SITE_ID,SITE_NAME,SUBMISSION_CONTACT_NAME,SUBMISSION_CONTACT_EMAIL,SUBMISSION_DATE,LAI,LAI_TYPE,LAI_CANOPY_TYPE,LAI_STATISTIC,LAI_STATISTIC_TYPE,LAI_STATISTIC_NUMBER,LAI_CLUMP,LAI_METHOD,LAI_APPROACH,LAI_DATE,LAI_DATE_START,LAI_DATE_END,LAI_DATE_UNC,LAI_COMMENT");

                foreach (AggItem si in list)
                {
                    if (si.plots.Count > 0 && si.LAIaverage != null)
                    {
                        string s;
                        s = si.siteCode;
                        s += "," + si.siteName;
                        s += "," + "ETC Antwerp";
                        s += "," + "info@icos-etc.eu";
                        s += "," + DateTime.Today.ToString("yyyyMMdd");
                        s += "," + si.LAIaverage.ToString().Replace(",", ".");
                        s += ",PAI";
                        s += ",Overstory";
                        s += ",MEAN";
                        s += ",SPATIAL";                        
                        s += "," + si.plots.Where(p => p.enough).Count().ToString();
                        s += "," + si.clumpAverage.ToString().Replace(",", ".");
                        s += ",Hemispherical photo";
                        if(si.plots[0].plotname.Substring(0, 2) == "CP")
                            s += ",Up to 13 DHP pictures were taken in each continuous plots (CP). LAI was calculated for each DHP passing the quality check and then averaged per plot if at least 70% of DHP was available. Values reported are the average and SD across the CPs used (number reported in the STATISTIC_NUMBER variable). For more info visit www.icos-etc.eu.";
                        else
                            s += ",Up to 7 DHP pictures were taken in each sparse plots (SP). LAI was calculated for each DHP passing the quality check and then averaged per plot if at least 70% of DHP was available. Values reported are the average and SD across the SPs used (number reported in the STATISTIC_NUMBER variable). For more info visit www.icos-etc.eu.";
                        if (si.startDate == si.endDate)
                        {
                            s += "," + si.startDate.ToString("yyyyMMdd");
                            s += ",,";
                        }
                        else
                        {
                            s += ",";
                            s += "," + si.startDate.ToString("yyyyMMdd");
                            s += "," + si.endDate.ToString("yyyyMMdd");
                        }
                        s += ",0";
                        s += ",";

                        data.Add(s);
                        s = "";
                        s = si.siteCode;
                        s += "," + si.siteName;
                        s += "," + "ETC Antwerp";
                        s += "," + "info@icos-etc.eu";
                        s += "," + DateTime.Today.ToString("yyyyMMdd");
                        s += "," + si.LAIdeviation.ToString().Replace(",", ".");
                        s += ",PAI";
                        s += ",Overstory";
                        s += ",Standard Deviation";
                        s += ",SPATIAL";
                        s += "," + si.plots.Where(p => p.enough).Count().ToString();
                        s += "," + si.clumpDeviation.ToString().Replace(",", ".");
                        s += ",Hemispherical photo";
                        if (si.plots[0].plotname.Substring(0, 2) == "CP")
                            s += ",Up to 13 DHP pictures were taken in each continuous plots (CP). LAI was calculated for each DHP passing the quality check and then averaged per plot if at least 70% of DHP was available. Values reported are the average and SD across the CPs used (number reported in the STATISTIC_NUMBER variable). For more info visit www.icos-etc.eu.";
                        else
                            s += ",Up to 7 DHP pictures were taken in each sparse plots (SP). LAI was calculated for each DHP passing the quality check and then averaged per plot if at least 70% of DHP was available. Values reported are the average and SD across the SPs used (number reported in the STATISTIC_NUMBER variable). For more info visit www.icos-etc.eu.";
                        if (si.startDate == si.endDate)
                        {
                            s += "," + si.startDate.ToString("yyyyMMdd");
                            s += ",,";
                        }
                        else
                        {
                            s += ",";
                            s += "," + si.startDate.ToString("yyyyMMdd");
                            s += "," + si.endDate.ToString("yyyyMMdd");
                        }
                        s += ",0";
                        s += ",";

                        data.Add(s);
                    }
                }
                string fileName = String.Format("BADM-DHP-LAI_{0}.csv",DateTime.Today.ToString("yyyyMMdd"));
                string fileContent = String.Join("\n", data);
                var byteArray = System.Text.ASCIIEncoding.Unicode.GetBytes(fileContent);
                
                if(download)
                    return File(byteArray, "text/csv", fileName);
            }

            return View(list);
        }

    }
}
