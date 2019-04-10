using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;

namespace Citygate.Controllers
{
    public class HomeController : Controller
    {
        private SqlConnection GetCon()
        {
            string conString = "Data Source=HP;Initial Catalog=Citygate;Integrated Security=True";
            SqlConnection con = new SqlConnection(conString);

            return con;
        }

        private List<Models.Ads> GetAllAds()
        {
            List<Models.Ads> ads = new List<Models.Ads>();

            SqlConnection con = GetCon();

            con.Open();

            SqlCommand cmd = new SqlCommand("exec spPublicationDates_SelectAll", con);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                ads.Add(new Models.Ads
                {
                    AdId = (int)reader[0],
                    PublicationDate = (DateTime)reader[1]
                });
            }

            reader.Close();
            cmd.Dispose();

            foreach (Models.Ads ad in ads)
            {
                cmd = new SqlCommand("exec spAds_GetAdById " + ad.AdId, con);
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ad.AdvertiserId = (int)reader[1];
                    ad.AdHeadline = (string)reader[2];
                    ad.AdText = (string)reader[3];
                }

                reader.Close();
                cmd.Dispose();
            }

            foreach (Models.Ads ad in ads)
            {
                cmd = new SqlCommand("spAdvertiser_GetNameByID " + ad.AdvertiserId, con);
                ad.AdvertiserName = (string)cmd.ExecuteScalar();
                cmd.Dispose();
            }

            con.Close();
            return ads;
        }

        private List<Models.Ads> GetActiveAds()
        {

            List<Models.PublicationDate> ActiveAds = GetActiveAdsID();

            List<Models.Ads> ads = CreateAdModel(ActiveAds);

            ConnectAdvertiserName(ads);

            return ads;
        }

        private void ConnectAdvertiserName(List<Models.Ads> ads)
        {
            SqlConnection con = GetCon();
            SqlCommand cmd;

            con.Open();

            foreach (Models.Ads ad in ads)
            {
                cmd = new SqlCommand("spAdvertiser_GetNameByID " + ad.AdvertiserId, con);
                ad.AdvertiserName = (string)cmd.ExecuteScalar();
                cmd.Dispose();
            }

            con.Close();
        }

        private List<Models.Ads> CreateAdModel(List<Models.PublicationDate> ActiveAds)
        {
            List<Models.Ads> ads = new List<Models.Ads>();

            SqlConnection con = GetCon();
            SqlCommand cmd;
            SqlDataReader reader;

            con.Open();

            foreach (Models.PublicationDate ad in ActiveAds)
            {
                cmd = new SqlCommand("exec spAds_GetAdById " + ad.AdId, con);
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ads.Add(new Models.Ads
                    {
                        AdId = (int)reader[0],
                        AdvertiserId = (int)reader[1],
                        AdHeadline = (string)reader[2],
                        AdText = (string)reader[3],
                        PublicationDate = ad.Date
                    });
                }

                reader.Close();
                cmd.Dispose();
            }

            con.Close();

            return ads;
        }

        private List<Models.PublicationDate> GetActiveAdsID()
        {
            List<Models.PublicationDate> ActiveAds = new List<Models.PublicationDate>();

            SqlConnection con = GetCon();

            con.Open();

            SqlCommand cmd = new SqlCommand("exec spPublicationDates_GetActiveAds '" + DateTime.Today.ToShortDateString() + "' ", con);
            SqlDataReader reader = cmd.ExecuteReader();


            while (reader.Read())
            {
                DateTime publicationDay = (DateTime)reader[1];
                if (publicationDay.AddDays(1).Date < DateTime.Today.Date)
                {
                    RemoveAdData((int)reader[0], publicationDay);
                }
                else
                {
                    ActiveAds.Add(new Models.PublicationDate() { AdId = (int)reader[0], Date = publicationDay } );
                }
            }

            con.Close();

            return ActiveAds;
        }

        public ActionResult Index()
        {
            List<Models.Ads> ads = GetActiveAds();

            return View(ads);
        }

        private List<Models.Advertisers> GetAdvertisers()
        {
            List<Models.Advertisers> adver = new List<Models.Advertisers>();

            SqlConnection con = GetCon();
            con.Open();

            SqlCommand cmd = new SqlCommand("spAdvertisers_GetAll", con);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                adver.Add(new Models.Advertisers
                {
                    AdvertiserId = (int)reader[0],
                    AdvName = (string)reader[1]
                });
            }

            cmd.Dispose();
            con.Close();

            return adver;
        }

        public ActionResult Add()
        {
            List<Models.Advertisers> adver = GetAdvertisers();

            ViewBag.Roles = new SelectList(adver, "AdvertiserId", "AdvName");

            Models.AddModel ad = new Models.AddModel();
            ad.numberOfDates = 1;

            return View(ad);
        }

        public ActionResult AddAd(Models.AddModel model, string submit)
        {
            List<Models.Advertisers> adver = GetAdvertisers();
            ViewBag.Roles = new SelectList(adver, "AdvertiserId", "AdvName");

            if (submit == "AddAd")
            {
                if (ModelState.IsValid)
                {
                    CreateAd(model.AdvertiserId, model.headline, model.text, model.date, model.numberOfDates);

                    List<Models.Ads> ads = GetActiveAds();
                    return View("Index", ads);
                }
                
                return View("Add", model);
            }
            else if (submit == "RemoveDate")
            {
                model.numberOfDates--;
                return View("Add", model);
            }
            else
            {
                model.numberOfDates++;
                return View("Add", model);
            }
        }

        //väljer man ingen Date tas annonsen bort från databasen
        private void CreateAd(int AdvertiserId, string headline, string text, DateTime[] date, int numberOfDates)
        {
            SqlConnection con = GetCon();

            Random random = new Random();
            int AdId = random.Next();

            con.Open();

            SqlCommand cmd = new SqlCommand("exec spAds_whereID @ID = " + AdId, con);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.HasRows)
            {
                cmd.Dispose();
                reader.Close();

                AdId = random.Next();

                cmd = new SqlCommand("exec spAds_whereID @ID = " + AdId, con);
                reader = cmd.ExecuteReader();
            }

            cmd.Dispose();
            reader.Close();

            cmd = new SqlCommand("exec spAds_CreateNew @Adid = " + AdId + ", @advertiserId = " + AdvertiserId +
                ", @AdHeadline = '" + headline + "', @adText = '" + text + "'", con);
            cmd.ExecuteNonQuery();
            cmd.Dispose();


            for(int x = 0; x < numberOfDates; x++)
            {
                cmd = new SqlCommand("exec spPublicationDate_CreateNew @AdId = " + AdId +
                ", @Date = '" + date[x].ToString() + "'", con);
                cmd.ExecuteNonQuery();

                cmd.Dispose();
            }

            con.Close();

        }

        public ActionResult RemoveAd(int removeId, DateTime date)
        {
            RemoveAdData(removeId, date);

            List<Models.Ads> ads = GetActiveAds();
            
            return View("Index", ads);
        }

        private void RemoveAdData(int removeId, DateTime date)
        {
            SqlConnection con = GetCon();
            con.Open();

            string s = date.ToShortDateString();

            SqlCommand cmd = new SqlCommand("exec spPublicationDates_RemoveAtIdAndDate " +
                removeId + ", '" + date.ToShortDateString() + "'", con);
            cmd.ExecuteNonQuery();

            cmd.Dispose();

            cmd = new SqlCommand("exec spPublicationDates_SelectAllWithId " + removeId, con);
            SqlDataReader reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                cmd.Dispose();
                cmd = new SqlCommand("spAds_RemoveAtID " + removeId, con);
                cmd.ExecuteNonQuery();
            }

            reader.Close();
            cmd.Dispose();

            con.Close();
        }

        public ActionResult AllAds()
        {
            List<Models.Ads> ads = GetAllAds();

            return View(ads);
        }
    }
}