using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class UploadSet
    {
        public int ID { get; set; }

        public CameraSetup cameraSetup { get; set; }

        [Display(Name = "Site")]
        public int? siteID { get; set; }

        public int userID { get; set; }

        [Display(Name = "Person")]
        public string person { get; set; }

        [Display(Name = "Upload Time")]
        public DateTime uploadTime { get; set; }

        public string siteName { get; set; }

        public double? slope { get; set; }

        public double? slopeAspect { get; set; }


        public List<PlotSet> plotSets { get; set; }

        public string siteCode { get; set; }

        public bool hasDataLogs;

        [Display(Name = "Plots")]
        public string plotNames { get; set; }

        public bool qualityCheck { get; set; }

        public string userName { get; set; }

        public int QCcreated { get; set; }
        public int QCpass { get; set; }
        public int QCfail { get; set; }
        [Display(Name = "Image Date")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime dateTaken { get; set; }
        [Display(Name = "Year")]
        public int yearTaken { get; set; }

        public Submission lastSubmission { get; set; }

        public string campaign { get; set; }
        public string dateStr { get; set; }
    }

    public class UploadSetBasic
    {
        public int ID { get; set; }
        public int? siteID { get; set; }

        public int userID { get; set; }
        public string siteCode { get; set; }
        public int? yearTaken { get; set; }
    }
}