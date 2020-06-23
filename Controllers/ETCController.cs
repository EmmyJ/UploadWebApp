using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Web;
using System.Web.Mvc;
using UploadWebapp.DB;
using UploadWebapp.Models.ETC;

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

        public ActionResult AggregateLAI(string year)
        {
            //TODO use year
            List<AggImage> images = ETCDA.getAggregationImages("2019");

            images = images.OrderBy(i => i.site).ThenBy(j => j.dateTaken).ThenBy(k => k.plotname).ThenBy(l => l.location).ThenByDescending(m => m.ID).ToList();

            List<AggItem> list = new List<AggItem>();            

            AggItem item = null;
            AggPlot plot = null;

            //images onderverdelen in groepen per site/date/plot
            foreach (AggImage image in images)
            {
                if (item == null || item.site != image.site || (image.dateTaken.Subtract(item.startDate).Days > 15))
                {
                    item = new AggItem();
                    item.site = image.site;
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

            
            foreach(AggItem aggitem in list)
            {
                
                foreach (AggPlot aggPlot in aggitem.plots)
                {
                    //disable duplicate images
                    AggImage previous = null;
                    aggPlot.images.OrderBy(i => i.location).ThenByDescending(i => i.ID);
                    foreach(AggImage im in aggPlot.images)
                    {
                        if(previous!= null && previous.location == im.location)
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
                aggitem.LAIaverage =  aggitem.plots.Where(p => p.enough).Average(p => p.LAIaverage);
                
                aggitem.clumpAverage = aggitem.plots.Where(p => p.enough).Average(p => p.clumpAverage);

                foreach(AggPlot pl in aggitem.plots)
                {
                    if (pl.enough)
                    {
                        pl.LAIdeviationSquare = Math.Pow(pl.LAIaverage.Value - aggitem.LAIaverage.Value,2);
                        pl.clumpDeviationSquare = Math.Pow(pl.clumpAverage.Value - aggitem.clumpAverage.Value, 2);
                    }
                }
                if (aggitem.plots.Where(p => p.enough).Count() > 0)
                {
                    aggitem.LAIdeviation = Math.Sqrt(aggitem.plots.Where(p => p.enough).Average(p => p.LAIdeviationSquare.Value));
                    aggitem.clumpDeviation = Math.Sqrt(aggitem.plots.Where(p => p.enough).Average(p => p.clumpDeviationSquare.Value));
                }
                aggitem.LAIaverage = aggitem.LAIaverage.HasValue ? Math.Round(aggitem.LAIaverage.Value, 3) : (double?)null;
                aggitem.clumpAverage = aggitem.clumpAverage.HasValue ? Math.Round(aggitem.clumpAverage.Value, 3) : (double?)null;
                aggitem.LAIdeviation = aggitem.LAIdeviation.HasValue ? Math.Round(aggitem.LAIdeviation.Value, 3) : (double?)null;
                aggitem.clumpDeviation = aggitem.clumpDeviation.HasValue ? Math.Round(aggitem.clumpDeviation.Value, 3) : (double?)null;
            }

            return View(list);
        }

    }
}
