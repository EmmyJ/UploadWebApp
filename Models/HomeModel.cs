using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace UploadWebapp.Models
{
    public class HomeModel
    {
        public User user { get; set; }
        public int selectedSite { get; set; }

        public UploadSet uploadSet { get; set; }

        //public ImageViewModel imageViewModel { get; set; }
        //public List<Image> images { get; set; }
    }
}
