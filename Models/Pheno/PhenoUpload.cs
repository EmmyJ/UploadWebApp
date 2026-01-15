using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models.Pheno
{
    public enum phenoUploadStatus { 
        start = 0,
        zipCreated = 1,
        hash = 2,
        metaCreated = 3,
        metaSent = 4,
        startSent = 5,
        sentSucces = 6,
        sentFailed = 7
    }
    public class PhenoUpload
    {
        public int ID { get; set; }
        public int phenoCameraID { get; set; }
        public string name { get; set; }
        public string hash { get; set; }
        public phenoUploadStatus status { get; set; }
        public bool dateProblem { get; set; }
    }
}