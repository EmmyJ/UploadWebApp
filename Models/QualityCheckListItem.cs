using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public DateTime imageDateTaken { get; set; }
        [DisplayFormat(DataFormatString = "{0:N}", ApplyFormatInEditMode = true)]
        public double? LAI { get; set; }
    }
}