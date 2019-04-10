using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace Citygate.Models
{
    public class AddModel
    {
        [Required(ErrorMessage = "Dont Forget Who You Are")]
        public int AdvertiserId { get; set; }

        public string headline { get; set; }
        public string text { get; set; }
        public int numberOfDates { get; set; }

        public DateTime[] date { get; set; }
    }
}