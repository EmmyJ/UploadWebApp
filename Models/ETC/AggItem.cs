using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models.ETC
{
    public class AggItem
    {
        public List<AggPlot> plots { get; set; } 
        public string siteCode { get; set; }
        public string siteName { get; set; }
        public double? LAIaverage { get; set; }
        public double? LAIdeviation { get; set; }
        public double? clumpAverage { get; set; }
        public double? clumpDeviation { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        //public double? LAIplotDeviationAvg { get; set; }
        //public double? clumpPlotDeviationAvg { get; set; }
    }
}