using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class QualityCheckListItem
    {
        public int ID { get; set; }
        public string filename { get; set; }
        public QCstatus status { get; set; }
        public int uploadSetID { get; set; }
        public string userName { get; set; }
        public DateTime dateModified { get; set; }
    }
}