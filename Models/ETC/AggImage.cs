using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models.ETC
{
    public class AggImage : Image
    {
        public DateTime dateTaken { get; set; }
        public string site { get; set; }
        public string plotname { get; set; }
        public string location { get; set; }
        public int group { get; set; }
        public QCstatus QCstatus { get; set; }
        //public double? LAIdeviationSquare { get; set; }
        //public double? clumpDeviationSquare { get; set; }
    }
}