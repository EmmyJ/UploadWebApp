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
        //public string dngFilename { get; set; }
        //public string dngPath { get; set; }

        public double? LAI { get; set; }
        public double? LAIe { get; set; }
        public double? threshold { get; set; }
        public double? clumping { get; set; }
        public double? overexposure { get; set; }

        public string binPath { get; set; }
        public string jpgPath { get; set; }
        public string gapfraction { get; set; }
        public string histogram { get; set; }
        public string exif { get; set; }
        public string stats { get; set; }

        public int ISO { get; set; }

        public int fNumber { get; set; }

        public float exposureTimeVal { get; set; }

        public string exposureTimeStr { get; set; }

    }
}