using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace Citygate.Models
{
    public class Ads
    {
        public int AdId { get; set; }
        public int AdvertiserId { get; set; }
        public string AdHeadline { get; set; }
        public string AdText { get; set; }
        public string AdvertiserName { get; set; }
        public DateTime PublicationDate { get; set; }

    }
}