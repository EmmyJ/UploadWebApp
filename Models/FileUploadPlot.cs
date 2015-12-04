using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class FileUploadPlot
    {
        public int ID { get; set; }
        public int plotID { get; set; }
        public int fileUploadID { get; set; }
        public double? result { get; set; }

        public List<AGBuploadDetail> AGBdetails { get; set; }
    }
}