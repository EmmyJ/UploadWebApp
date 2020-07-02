using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace UploadWebapp.Models
{
    public class Site
    {
        public int ID { get; set; }
        public string siteCode { get; set; }
        public string name { get; set; }

        public string countryCode { get; set; }
        public int ecosystemID { get; set; }
        public double lattitude { get; set; }
        public double longitude { get; set; }
        public bool labelled { get; set; }
        public DateTime? labelDate{ get; set; }

        public string title { get; set; }
    }
}
