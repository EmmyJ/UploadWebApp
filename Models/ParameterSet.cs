﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace UploadWebapp.Models
{
    public class ParameterSet
    {
        public int ID { get; set; }
        [Required]
        [DisplayName("Name")]
        public string name { get; set; }
        public CameraSetup cameraSetup { get; set; }
        [DisplayName("Lens X")]
        public int? lensX { get; set; }
        [DisplayName("Lens Y")]
        public int? lensY { get; set; }
        [DisplayName("Lens A")]
        public double? lensA { get; set; }
        [DisplayName("Lens B")]
        public double? lensB { get; set; }
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer number")]
        [DisplayName("Maximum Radius")]
        public int? maxRadius { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer number")]
        [DisplayName("Width")]
        public int? width { get; set; }
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer number")]
        [DisplayName("Height")]
        public int? height { get; set; }
        [DisplayName("Processed")]
        public bool processed { get; set; }

        //[Required]
        [DisplayName("Center Calibration File")]
        public string pathCenter { get; set; }
        //[Required]
        [DisplayName("Lens Projection File")]
        public string pathProj { get; set; }
        //public HttpPostedFileBase centerFile { get; set ;asp}
        //public HttpPostedFileBase projFile { get; set; }

        public string title { get; set; }
        public string lensAstr { get; set; }
        public string lensBstr { get; set; }
        public string lensXstr { get; set; }
        public string lensYstr { get; set; }
    }
}