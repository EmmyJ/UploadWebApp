using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class CreateCamerSetupModel
    {
        public CameraSetup cameraSetup { get; set; }
        public User user { get; set; }
        public int? selectedSite { get; set; }
    }
}