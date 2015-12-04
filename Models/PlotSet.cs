using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class PlotSet
    {
        public int ID { get; set; }
        public int uploadSetID { get; set; }
        //TODO: remove
        public string plotname { get; set; }
        public int? plotID { get; set; }

        public List<Image> images { get; set; }
        public ResultsSet resultsSet { get; set; }
        public Plot plot { get; set; }
    }
}