using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class ResultsSet
    {
        public int ID { get; set; }
        public int plotSetID { get; set; }
        public bool processed { get; set; }
        public double? LAI { get; set; }
        public double? LAI_SD { get; set; }
        public string resultLog { get; set; }
        public string data { get; set; }
    }
}