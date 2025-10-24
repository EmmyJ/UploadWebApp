using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace UploadWebapp.Models.Pheno
{
    public enum phenoCamStatus
    {
        ignore = 0,
        process = 1
    }

    public class PhenoCamera
    {
        public int ID { get; set; }
        public string name { get; set; }
        public phenoCamStatus status { get; set; }
        public DateTime? lastDate { get; set; }
    }
}