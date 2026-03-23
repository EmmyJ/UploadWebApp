using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models.Pheno
{
    public class PhenoReportItem
    {
        public string cameraName { get; set; }
        public string fileName { get; set; }
        public int? expFileCount { get; set; }
        public int? actFileCount { get; set; }
        public phenoUploadStatus? status { get; set; }
        public string hash { get; set; }
    }
}