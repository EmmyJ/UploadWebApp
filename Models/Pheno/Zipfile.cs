using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models.Pheno
{
    public class Zipfile
    {
        public string siteCode { get; set; }
        public DateTime date { get; set; }
        public List<string> dayfiles { get; set; }
        public string fullName { get; set; }
        public string fileName { get; set; }
        public string hash { get; set; }
        public string suffix { get; set; }
        public PhenoUpload upload { get; set; }
    }
}