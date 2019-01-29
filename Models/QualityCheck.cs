using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public enum QCstatus
    {
        created = 0,
        pass = 1 ,
        fail = 2
    }

    public class QualityCheck
    {
        public int ID { get; set; }
        public int imageID { get; set; }
        public bool setupObjects { get; set; }
        public string setupObjectsComments { get; set; }
        public bool noForeignObjects { get; set; }
        public string foreignObjectsComments { get; set; }
        public bool noRaindropsDirt { get; set; }
        public string raindropsDirtComments { get; set; }
        public bool noLensRing { get; set; }
        public bool lighting { get; set; }
        public string lightingComments { get; set; }
        public bool noOverexposure { get; set; }
        public string overexposureComments { get; set; }
        public string otherComments { get; set; }
        public QCstatus status { get; set; }

        public double? LAI { get; set; }
        public double? LAIe { get; set; }
        public double? threshold { get; set; }
        public double? clumping { get; set; }
    }
}