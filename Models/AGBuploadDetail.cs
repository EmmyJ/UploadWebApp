using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class AGBuploadDetail
    {
        public int ID { get; set; }
        public int fileUploadPlotID { get; set; }
        public string plotname { get; set; }
        public int speciesID { get; set; }
        public int treeNr { get; set; }
        public double DBH { get; set; }
    }
}