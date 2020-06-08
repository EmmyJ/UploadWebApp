using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class Submission
    {
        public int ID { get; set; }
        public int uploadSetID { get; set; }
        public string filename { get; set; }
        public int userID { get; set; }
        public string userName { get; set; }
        public DateTime submissionDate { get; set; }
    }
}