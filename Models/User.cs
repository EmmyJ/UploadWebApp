using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class User
    {
        public int ID { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string username { get; set; }
        public string pwd { get; set; }

        public bool isICOSuser { get; set; }
        public bool isFreeUser { get; set; }

        public List<Site> sites { get; set; }
        public List<CameraSetup> cameraSetups { get; set; }
    }
}