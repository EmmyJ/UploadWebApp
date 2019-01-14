using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using UploadWebapp.Models;
using UploadWebapp.usersitesservice;

namespace UploadWebapp.DB
{
    public class UserDA
    {
        public static int CurrentUserId
        {
            get
            {
                int id = 0;
                if (HttpContext.Current != null && HttpContext.Current.Session != null && HttpContext.Current.Session["UserId"] != null)
                    int.TryParse(HttpContext.Current.Session["UserId"].ToString(), out id);

                return id;
            }
            set
            {
                if (HttpContext.Current.Session != null)
                    HttpContext.Current.Session["UserId"] = value;
            }
        }

        public static string CurrentUserName
        {
            get
            {
                string username = "";
                if (HttpContext.Current != null && HttpContext.Current.Session != null && HttpContext.Current.Session["username"] != null)
                {
                    username = HttpContext.Current.Session["username"].ToString();
                }
                return username;
            }
            set
            {
                if (HttpContext.Current.Session != null)
                    HttpContext.Current.Session["username"] = value;
            }
        }

        public static bool CurrentUserICOS
        {
            get
            {
                bool id = false;
                if (HttpContext.Current != null && HttpContext.Current.Session != null && HttpContext.Current.Session["ICOSuser"] != null)
                    bool.TryParse(HttpContext.Current.Session["ICOSuser"].ToString(), out id);

                return id;
            }
            set
            {
                if (HttpContext.Current.Session != null)
                    HttpContext.Current.Session["ICOSuser"] = value;
            }
        }

        public static bool CurrentUserFree
        {
            get
            {
                bool free = false;
                if (HttpContext.Current != null && HttpContext.Current.Session != null && HttpContext.Current.Session["FreeUser"] != null)
                    bool.TryParse(HttpContext.Current.Session["FreeUser"].ToString(), out free);

                return free;
            }
            set
            {
                if (HttpContext.Current.Session != null)
                    HttpContext.Current.Session["FreeUser"] = value;
            }
        }

        public static User CurrentUser
        {
            get
            {
                User user = new User();

                user.ID = int.Parse(HttpContext.Current.Session["UserId"].ToString());
                user.username = HttpContext.Current.Session["username"].ToString();
                user.isICOSuser = bool.Parse(HttpContext.Current.Session["ICOSuser"].ToString());
                user.isFreeUser = bool.Parse(HttpContext.Current.Session["FreeUser"].ToString());

                return user;
            }
        }

        public static User GetByUsername(string username, DB db = null)
        {
            db = new DB();

            var result = db.ExecuteReader("SELECT [ID] ,[NAME] ,[EMAIL] ,[USERNAME], [PWD], [freeUser] FROM [utenti] WHERE USERNAME='" + username + "'");
            User user = result.HasRows ? FromUserData(result).FirstOrDefault() : null;
            db.Dispose();
            return user;
        }

        public static User CheckExist(string username, string email, DB db = null)
        {
            db = new DB();

            var result = db.ExecuteReader("SELECT [ID] ,[NAME] ,[EMAIL] ,[USERNAME], [PWD], [freeUser] FROM [utenti] WHERE LOWER(USERNAME)= LOWER('" + username + "') OR LOWER(EMAIL) = LOWER('" + email + "')");
            User user = result.HasRows ? FromUserData(result).FirstOrDefault() : null;
            db.Dispose();
            return user;
        }

        public static bool CreateFreeUser(RegisterModel register, DB db = null)
        {
            db = new DB();
            int id;

            id = Convert.ToInt32(db.ExecuteScalar("INSERT INTO [utenti] ([NAME], [EMAIL], [USERNAME], [PWD], [freeUser]) VALUES (@NAME, @EMAIL, @USERNAME, @PWD, @freeUser);SELECT IDENT_CURRENT('[utenti]');"
                , new SqlParameter("NAME", register.UserName)
                , new SqlParameter("EMAIL", register.Email)
                , new SqlParameter("USERNAME", register.UserName)
                , new SqlParameter("PWD", register.Password)
                , new SqlParameter("freeUser", true)));
            db.Dispose();

            return true;
        }

        //public static User GetByUserID(int ID, DB db = null)
        //{
        //    db = new DB();

        //    var result = db.ExecuteReader("SELECT [ID] ,[NAME] ,[EMAIL] ,[USERNAME], [PWD] FROM [utenti] WHERE ID=" + ID);
        //    User user = result.HasRows ? FromUserData(result).FirstOrDefault() : null;
        //    db.Dispose();
        //    return user;
        //}

        public static List<Site> GetSiteListForUser(int userID, DB db = null)
        {
            List<Site> sites = new List<Site>();
            //if (CurrentUserICOS)
            //{
            //    UserSiteService uss = new UserSiteService();
            //    List<SiteData> datalist = uss.GetUserSites(userID).ToList();
            //    foreach (SiteData data in datalist)
            //    {
            //        Site s = SiteDataToSite(data);
            //        sites.Add(s);
            //    }
            //}
            //else
            //{
                db = new DB();
                var result = db.ExecuteReader("SELECT DISTINCT s.[ID] ,s.[site] ,s.[NAME] FROM [sites] s LEFT JOIN [usersites] us on us.idsito = s.ID WHERE us.iduser =" + userID);
                sites = result.HasRows ? FromSiteData(result) : null;
                db.Dispose();
            //}
            return sites.OrderBy(s => s.siteCode).ToList();
        }

        public static Site SiteDataToSite(SiteData data)
        {
            Site s = new Site();
            s.ID = data.ID;
            s.name = data.name;
            s.siteCode = data.siteCode;
            s.title = data.title;

            return s;
        }

        public static Site GetSiteByID(int siteID, DB db = null)
        {
            Site site = new Site();
            //if (CurrentUserICOS)
            //{
            //    UserSiteService uss = new UserSiteService();
            //    site.ID = siteID;
            //    site.siteCode = uss.GetSiteCode(siteID);
            //}
            //else
            //{
                db = new DB();
                var result = db.ExecuteReader("SELECT [ID], [site], [NAME] FROM [sites] WHERE ID = " + siteID);
                site = result.HasRows ? FromSiteData(result).FirstOrDefault() : null;
                db.Dispose();
            //}
            return site;
        }

        public static List<User> FromUserData(SqlDataReader data)
        {
            var result = new List<User>();
            while (data.Read())
            {
                User u = new User();
                u.ID = data.GetInt32(0);
                u.name = data.GetString(1);
                u.email = data.GetString(2);
                u.username = data.GetString(3);
                u.pwd = data.GetString(4);
                u.isFreeUser = data.IsDBNull(5) ? false : data.GetBoolean(5);

                result.Add(u);
            }
            data.Close();
            return result;
        }

        public static List<Site> FromSiteData(SqlDataReader data)
        {
            var result = new List<Site>();
            while (data.Read())
            {
                Site s = new Site();
                s.ID = data.GetInt32(0);
                s.siteCode = data.IsDBNull(1) ? "" : data.GetString(1);
                s.name = data.GetString(2);
                s.title = string.Format("{0} ({1})", s.name, s.siteCode);

                result.Add(s);
            }
            data.Close();
            return result;
        }
    }
}