using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class FileUpload
    {
        public int ID { get; set; }
        public int siteID { get; set; }
        public int userID  { get; set; }
        public DateTime uploadDate { get; set; }
        public string uploader { get; set; }
        public string filename { get; set; }
        public string filepath { get; set; }
        public string type { get; set; }
        public double result { get; set; }

        public List<FileUploadPlot> fileUploadPlots { get; set; }
    }
}