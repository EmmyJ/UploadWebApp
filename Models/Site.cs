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
        public string title { get; set; }
    }
}
