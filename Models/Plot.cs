using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class Plot
    {
        public int ID { get; set; }
        public int siteID { get; set; }
        public string name { get; set; }
        public double? slope { get; set; }
        public double? slopeAspect { get; set; }
        public DateTime insertDate { get; set; }
        public int insertUser { get; set; }
        public bool active { get; set; }
        public DateTime? deleteDate { get; set; }
        public int? deleteUser { get; set; }
    }
}