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
    }
}