using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class CreateSiteModel
    {
        public Site site { get; set; }
        public List<Ecosystem> ecosystemList { get; set; }
        public List<Country> countryList { get; set; }
    }
}