using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class ExportETCmodel
    {
        public Image image { get; set; }
        public string cameraSetupName { get; set; }
        public QualityCheck qc { get; set; }
    }
}