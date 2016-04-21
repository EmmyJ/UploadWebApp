using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class UploadSet
    {
        public int ID { get; set; }
        public CameraSetup cameraSetup { get; set; }
        public int? siteID { get; set; }
        public int userID { get; set; }
        public string person { get; set; }
        public DateTime uploadTime { get; set; }
        public string siteName { get; set; }
        //public double? slope { get; set; }
        //public double? slopeAspect { get; set; }

        public List<PlotSet> plotSets { get; set; }
        //public List<Image> images { get; set; }
        //public ResultsSet resultsSet { get; set; }
        public string siteCode { get; set; }
        public bool hasDataLogs;
        public string plotNames { get; set; }
    }
}