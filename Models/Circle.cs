using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class Circle
    {
        public int circleNr { get; set; }
        public List<Point> points { get; set; }
    }
}