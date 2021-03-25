using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class UploadSetQualityChecksModel
    {
        public int uploadSetID { get; set; }
        public int siteID { get; set; }
        public int year { get; set; }
        public string campaignID { get; set; }
        public List<QualityCheckListItem> qualityChecks { get; set; }
        
    }
}