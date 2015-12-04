using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class CameraSetup
    {
        public int ID { get; set; }
        public string cameraType { get; set; }
        public string cameraSerial { get; set; }
        public string lensType { get; set; }
        public string lensSerial { get; set; }
        public int lensX { get; set; }
        public int lensY { get; set; }
        public double lensA { get; set; }
        public double lensB { get; set; }

        public string title { get; set; }
    }
}