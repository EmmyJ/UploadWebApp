using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models.ETC
{
    public class AggPlot
    {
        public List<AggImage> images { get; set; }
        public string plotname { get; set; }
        public int totalNum { get; set; }
        public int passNum { get; set; }
        public bool enough { get; set; }
        public double? LAIaverage { get; set; }
        //public double? LAIdeviation { get; set; }
        public double? LAIdeviationSquare { get; set; }
        public double? clumpAverage { get; set; }
        //public double? clumpDeviation { get; set; }
        public double? clumpDeviationSquare { get; set; }
    }
}