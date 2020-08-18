using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class OverviewModel
    {
        public bool isETCuser { get; set; }

        public List<UploadSet> uploadSets { get; set; }
        public int selectedSite { get; set; }
        public Dictionary<int,string> siteList { get; set; }
        public int selectedYear { get; set; }
        public List<int> yearList { get; set; }
    }
}