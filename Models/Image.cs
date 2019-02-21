using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class Image
    {
        public int ID { get; set; }
        public string filename { get; set; }
        public string path { get; set; }
        public string dngFilename { get; set; }
        public string dngPath { get; set; }

        public double? LAI { get; set; }
        public double? LAIe { get; set; }
        public double? threshold { get; set; }
        public double? clumping { get; set; }
        public double? overexposure { get; set; }
    }
}