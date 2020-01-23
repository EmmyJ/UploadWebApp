using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class EditQualityCheckModel
    {
        public QualityCheck qualityCheck { get; set; }

        public QualityCheck previousQualityCheck { get; set; }

        public CameraSetup cameraSetup { get; set; }

        public Image image { get; set; }

        public int uploadSetID { get; set; }
    }
}