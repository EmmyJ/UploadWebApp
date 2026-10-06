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

    public enum phenoCamType
    {
        unknown = 0,
        NetCamSC5 = 1,
        NetCamLive2 = 2
    }

    public class PhenoCamera
    {
        public int ID { get; set; }
        public string name { get; set; }
        public string oldName { get; set; }
        public phenoCamStatus status { get; set; }
        public int expFileCount { get; set; }
        public DateTime? lastDate { get; set; }
        public DateTime? newDate { get; set; }
        public phenoCamType type { get; set; }
        public string serial { get; set; }
    }
}