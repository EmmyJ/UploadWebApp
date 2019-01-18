using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using UploadWebapp.Models;
using UploadWebapp.usersitesservice;

namespace UploadWebapp.DB
{
    public class ImageDA
    {
        //public static UserSiteService uss = new UserSiteService();
        public static int CurrentUploadSetId
        {
            get
            {
                int id = 0;
                if (HttpContext.Current != null && HttpContext.Current.Session != null && HttpContext.Current.Session["UploadSetId"] != null)
                    int.TryParse(HttpContext.Current.Session["UploadSetId"].ToString(), out id);
                else
                {
                    DB db = new DB();
                    SqlDataReader result = db.ExecuteReader("SELECT MAX(ID) as ID FROM [uploadSet] WHERE USERID = " + UserDA.CurrentUserId);
                    result.Read();
                    id = result.GetInt32(0);
                }
                return id;
            }
            set
            {
                if (HttpContext.Current.Session != null)
                    HttpContext.Current.Session["UploadSetId"] = value;
            }
        }

        public static int GetCountUnprocessed(int uploadSetID, DB db = null)
        {
            db = new DB();

            int count = Convert.ToInt32(db.ExecuteScalar("select  COUNT(i.id) from images i left join results r on i.plotSetID = r.plotSetID left join plotSets ps on i.plotSetID = ps.ID where processed = 0 and ps.uploadSetID <= " + uploadSetID));

            return count;
        }

        public static List<string> GetUploadSetData(int uploadSetID, DB db = null)
        {
            db = new DB();
            var result = db.ExecuteReader("select r.data from results r left join plotSets ps on r.plotSetID = ps.id where ps.uploadSetID = " + uploadSetID);
            List<string> results = new List<string>();
            while (result.Read())
            {
                if (!result.IsDBNull(0))
                    results.Add(result.GetString(0));
            }
            result.Close();
            return results;

        }

        public static UploadSet GetUploadSetByID(int uploadSetID, DB db = null)
        {
            db = new DB();
            UploadSet uploadSet = new UploadSet();
            var result = db.ExecuteReader("SELECT [ID],[camSetupID] ,[siteID] ,[userID] ,[person],[uploadTime], [siteName], [qualityCheck] FROM [uploadSet] WHERE ID = " + uploadSetID);
            uploadSet = FromSetData(result).FirstOrDefault();
            if (!UserDA.CurrentUserFree)
            {
                //if (UserDA.CurrentUserICOS)
                //{
                //    //UserSiteService uss = new UserSiteService();
                //    uploadSet.siteCode = uss.GetSiteCode(uploadSet.siteID.Value);
                //}
                //else
                //{
                    result = db.ExecuteReader("SELECT [site] FROM [sites] WHERE ID =" + uploadSet.siteID);
                    result.Read();
                    uploadSet.siteCode = result.GetString(0);
                //}
            }
            result.Close();

            result = db.ExecuteReader("SELECT [ID],[camType],[camSerial],[lensType] ,[lensSerial] ,[x],[y],[a] ,[b], [maxRadius], [width], [height], [processed], [name] FROM [cameraSetup] WHERE ID = " + uploadSet.cameraSetup.ID);

            uploadSet.cameraSetup = FromSetupData(result).FirstOrDefault();
            //SELECT [ID], [uploadSetID], [plotID] FROM [plotSets] WHERE uploadSetID = 
            result = db.ExecuteReader("SELECT ps.[ID], ps.[uploadSetID], ps.[plotID], p.name, p.slope, p.slopeAspect FROM [plotSets] ps LEFT JOIN [plots]  p on ps.plotID = p.ID WHERE uploadSetID = " + uploadSetID + " ORDER BY p.name");
            uploadSet.plotSets = FromPlotSetData(result);

            foreach (PlotSet plot in uploadSet.plotSets)
            {
                result = db.ExecuteReader("SELECT [ID],[plotSetID] ,[filename] ,[path],[dngFilename] ,[dngPath] FROM [images] WHERE plotSetID = " + plot.ID);
                plot.images = FromImageData(result);

                result = db.ExecuteReader("SELECT [ID],[plotSetID] ,[processed] ,[LAI] ,[LAI_SD] ,[resultLog], [data] FROM [results] WHERE plotSetID = " + plot.ID);

                plot.resultsSet = FromResultsData(result).FirstOrDefault();
            }

            db.Dispose();

            return uploadSet;
        }

        public static List<PlotSet> GetResultsSet(int uploadSetID, DB db = null)
        {
            db = new DB();
            List<PlotSet> plotSets = new List<PlotSet>();
            //SELECT p.[ID] ,p.[uploadSetID],p.[plotname],r.[ID],r.[plotSetID],r.[processed],r.[LAI],r.[LAI_SD],r.[resultLog], r.[data] FROM [plotSets] p LEFT JOIN results r on r.[plotSetID] = p.id where p.[uploadSetID] = 
            var result = db.ExecuteReader("SELECT p.[ID] ,p.[uploadSetID],pl.name, r.[ID],r.[plotSetID],r.[processed],r.[LAI],r.[LAI_SD],r.[resultLog], r.[data], pl.ID FROM [plotSets] p LEFT JOIN results r on r.[plotSetID] = p.id left join plots pl on p.plotID = pl.ID where p.[uploadSetID] = " + uploadSetID);
            plotSets = FromPlotResultsData(result);
            db.Dispose();
            return plotSets;
        }

        public static List<UploadSet> GetUploadSetsByUserID(int userID, DB db = null)
        {
            db = new DB();
            List<UploadSet> list = new List<UploadSet>();
            //var result = db.ExecuteReader("SELECT us.[ID],[camSetupID],[siteID] ,[userID] ,[person],[uploadTime],s.site, r.LAI, r.LAI_SD, r.processed, us.slope, us.slopeAspect FROM [uploadSet] us LEFT JOIN [sites] s on s.ID = us.[siteID] LEFT JOIN [results] r on r.uploadSetID = us.ID WHERE us.userID = " + userID + " ORDER BY us.uploadTime DESC");
            //var result = db.ExecuteReader("SELECT us.[ID],[camSetupID],[siteID] ,[userID] ,[person],[uploadTime],s.site FROM [uploadSet] us LEFT JOIN [sites] s on s.ID = us.[siteID]  WHERE us.userID = " + userID + " ORDER BY us.uploadTime DESC");
            //todo: italy
            if (UserDA.CurrentUserICOS)
            {
                //var result = db.ExecuteReader("SELECT us.[ID],[camSetupID],[siteID] ,[userID] ,[person],[uploadTime],'-' as site , (SELECT count(r.[ID]) FROM [results] r left join LAI_App.dbo.plotSets ps on ps.ID = r.plotSetID where data is not null and ps.uploadSetID = us.ID) FROM [uploadSet] us  WHERE us.userID = " + userID + " ORDER BY us.uploadTime DESC");
                var result = db.ExecuteReader("SELECT us.[ID],[camSetupID],[siteID] ,[userID] ,[person],[uploadTime], '-' as site ,	(SELECT count(r.[ID]), us.[qualityCheck] 	FROM [results] r left join plotSets ps on ps.ID = r.plotSetID where data is not null and ps.uploadSetID = us.ID) as count, (SELECT p.name as [data()] FROM plotSets ps left join plots p on p.ID = ps.plotID where ps.uploadSetID = us.id ORDER BY p.name FOR xml path('')) as plots, us.siteName FROM [uploadSet] us  WHERE us.userID = " + userID + " GROUP BY us.[ID],[camSetupID],[siteID] ,[userID] ,[person],[uploadTime], us.siteName ORDER BY us.uploadTime DESC");
                list = FromUserSetData(result);

                //get sitecodes from italy
                Dictionary<int, string> sitelist = new Dictionary<int, string>();
                List<SiteData> datalist = GetUserSites(userID);
                foreach (SiteData data in datalist)
                {
                    sitelist.Add(data.ID, data.siteCode);
                }
                foreach (UploadSet us in list)
                {
                    us.siteCode = us.siteID.HasValue ? sitelist[us.siteID.Value] : "";
                }

            }
            else
            {
                var result = db.ExecuteReader("SELECT us.[ID],[camSetupID],[siteID] ,[userID] ,[person],[uploadTime],s.site ,(SELECT count(r.[ID]) 	FROM [results] r left join plotSets ps on ps.ID = r.plotSetID where data is not null and ps.uploadSetID = us.ID) as count, (SELECT p.name as [data()] FROM plotSets ps left join plots p on p.ID = ps.plotID where ps.uploadSetID = us.id ORDER BY p.name FOR xml path('')) as plots, us.siteName, us.[qualityCheck] FROM [uploadSet] us  LEFT JOIN [sites] s on s.ID = us.[siteID]  WHERE us.userID = " + userID + " GROUP BY us.[ID],[camSetupID],[siteID] ,[userID] ,[person],[uploadTime],s.site, us.siteName, us.[qualityCheck] ORDER BY us.uploadTime DESC");
                //var result = db.ExecuteReader("SELECT us.[ID],[camSetupID],[siteID] ,[userID] ,[person],[uploadTime],s.site , (SELECT count(r.[ID]) FROM [results] r left join LAI_App.dbo.plotSets ps on ps.ID = r.plotSetID where data is not null and ps.uploadSetID = us.ID) FROM [uploadSet] us  LEFT JOIN [sites] s on s.ID = us.[siteID]  WHERE us.userID = " + userID + " ORDER BY us.uploadTime DESC");
                list = FromUserSetData(result);
                db.Dispose();
            }
            return list;
        }

        public static List<SiteData> GetUserSites(int userID)
        {
            DB db = new DB();
            SqlDataReader rd;
            List<SiteData> sites = new List<SiteData>();            

            SiteData s;
            rd = db.ExecuteReader("SELECT DISTINCT s.[ID], s.[site], s.[NAME] FROM[sites] s LEFT JOIN[usersites] us on us.idsito = s.ID WHERE us.iduser = " + userID + " ORDER BY s.name;");
            while (rd.Read())
            {
                s = new SiteData();
                s.ID = rd.GetInt32(0);
                s.siteCode = rd.IsDBNull(1) ? "" : rd.GetString(1);
                s.name = rd.GetString(2);
                s.title = string.Format("{0} ({1})", s.name, s.siteCode);

                sites.Add(s);
            }

            rd.Close();

            return sites;
        }

        public static List<CameraSetup> GetCameraSetupsForUser(int userID, DB db = null)
        {
            db = new DB();
            //todo:italy nu alle setups van user
            //var result = db.ExecuteReader("SELECT DISTINCT c.[ID],[camType],[camSerial],[lensType] ,[lensSerial] ,[x],[y],[a] ,[b] FROM [cameraSetup] c  LEFT JOIN uploadSet u on u.camSetupID = c.ID  LEFT JOIN usersites s on s.idsito = u.siteID  where u.userID = " + userID);
            var result = db.ExecuteReader("SELECT DISTINCT c.[ID],[camType],[camSerial],[lensType] ,[lensSerial] ,[x],[y],[a] ,[b], [maxRadius], [width], [height], [processed], [name]  FROM [cameraSetup] c where deleted = 0 and c.userID = " + userID);
            //LEFT JOIN uploadSet u on u.camSetupID = c.ID  where u.userID = " + userID);
            List<CameraSetup> cameraSetups = FromSetupData(result);
            db.Dispose();
            return cameraSetups;
        }

        public static CameraSetup GetCameraSetupByID(int cameraSetupID, DB db = null)
        {
            db = new DB();
            var result = db.ExecuteReader("SELECT [ID],[camType],[camSerial],[lensType] ,[lensSerial] ,[x],[y],[a] ,[b], [maxRadius], [width], [height], [processed], [name]  FROM [cameraSetup] WHERE ID = " + cameraSetupID);
            CameraSetup cameraSetup = FromSetupData(result).FirstOrDefault();
            db.Dispose();
            return cameraSetup;
        }

        public static bool DisableCameraSetup(int cameraSetupID, DB db = null)
        {
            db = new DB();
            db.ExecuteScalar("UPDATE [cameraSetup] set [deleted] = 1 WHERE ID = " + cameraSetupID);
            db.Dispose();
            return true;
        }

        public static Plot GetPlotByName(string plotName, int SiteID, DB db = null)
        {
            db = new DB();
            var result = db.ExecuteReader("SELECT [ID],[siteID],[name],[slope],[slopeAspect] FROM [plots] WHERE active = 1 AND name = '" + plotName + "' AND siteID = " + SiteID);
            Plot plot = FromPlotData(result).FirstOrDefault();
            db.Dispose();
            return plot;
        }

        public static List<Plot> GetPlotListForSite(int SiteID, DB db = null)
        {
            db = new DB();
            var result = db.ExecuteReader("SELECT [ID],[siteID],[name],[slope],[slopeAspect] FROM [plots] WHERE active = 1 AND  siteID = " + SiteID);
            List<Plot> plots = FromPlotData(result);
            db.Dispose();
            return plots;
        }

        public static List<string> GetPlotNamesForUploadSet(int uploadSetID, DB db = null)
        {
            db = new DB();
            var result = db.ExecuteReader("SELECT  p.name from plotSets ps left join plots p on p.ID = ps.plotID where ps.uploadSetID = " + uploadSetID);
            List<string> plotnames = new List<string>();
            while (result.Read())
            {
                plotnames.Add(result.GetString(0));
            }
            return plotnames;
        }

        public static List<Species> GetSpeciesList(DB db = null)
        {
            db = new DB();
            var result = db.ExecuteReader("SELECT * FROM [species]");
            List<Species> species = FromSpeciesData(result);
            db.Dispose();
            return species;
        }
        public static List<Ecosystem> GetEcosystemList(DB db = null)
        {
            db = new DB();
            var result = db.ExecuteReader("SELECT * FROM [ecosystems]");
            List<Ecosystem> ecosystems = FromEcosystemsData(result);
            db.Dispose();
            return ecosystems;
        }

        public static List<Country> GetCountryList(DB db = null)
        {
            db = new DB();
            var result = db.ExecuteReader("SELECT * FROM [countries]");
            List<Country> countries = FromCountryData(result);
            db.Dispose();
            return countries;
        }

        public static Site GetSiteByCode(string siteCode, DB db = null)
        {
            db = new DB();
            Site site;
            //if (UserDA.CurrentUserICOS)
            //{
            //    //UserSiteService uss = new UserSiteService();
            //    SiteData data = uss.GetSiteByCode(siteCode);
            //    site = UserDA.SiteDataToSite(data);
            //}
            //else
            //{
                var result = db.ExecuteReader("SELECT [ID], [site], [NAME] FROM [sites] WHERE site = '" + siteCode + "'");
                site = result.HasRows ? UserDA.FromSiteData(result).FirstOrDefault() : null;
                db.Dispose();
            //}
            return site;
        }

        public static List<UploadSet> FromUserSetData(SqlDataReader data)
        {
            var result = new List<UploadSet>();
            while (data.Read())
            {
                UploadSet s = new UploadSet();
                s.ID = data.GetInt32(0);
                s.cameraSetup = new CameraSetup();
                s.cameraSetup.ID = data.GetInt32(1);
                s.siteID = data.GetInt32(2);
                s.userID = data.GetInt32(3);
                s.person = data.GetString(4);
                s.uploadTime = data.GetDateTime(5);
                s.siteCode = data.IsDBNull(6) ? null : data.GetString(6);
                s.hasDataLogs = data.GetInt32(7) > 0;
                s.plotNames = data.IsDBNull(8) ? "" : data.GetString(8);
                s.siteName = data.IsDBNull(9) ? "" : data.GetString(9);
                s.qualityCheck = (data.GetBoolean(10));
                //s.resultsSet = new ResultsSet();
                //s.resultsSet.LAI = data.IsDBNull(7) ? (double?)null : data.GetDouble(7);
                //s.resultsSet.LAI_SD = data.IsDBNull(8) ? (double?)null : data.GetDouble(8);
                //s.resultsSet.processed = data.IsDBNull(9) ? false : data.GetBoolean(9);
                //s.slope = data.IsDBNull(7) ? (double?)null : data.GetDouble(7);
                //s.slopeAspect = data.IsDBNull(8) ? (double?)null : data.GetDouble(8);

                result.Add(s);
            }
            data.Close();
            return result;
        }

        public static List<UploadSet> FromSetData(SqlDataReader data)
        {
            var result = new List<UploadSet>();
            while (data.Read())
            {
                UploadSet s = new UploadSet();
                s.ID = data.GetInt32(0);
                s.cameraSetup = new CameraSetup();
                s.cameraSetup.ID = data.GetInt32(1);
                s.siteID = data.GetInt32(2);
                s.userID = data.GetInt32(3);
                s.person = data.GetString(4);
                s.uploadTime = data.GetDateTime(5);
                s.siteName = data.GetString(6);
                s.qualityCheck = data.GetBoolean(7);
                //s.slope = data.IsDBNull(6) ? (double?)null : data.GetDouble(6);
                //s.slopeAspect = data.IsDBNull(7) ? (double?)null : data.GetDouble(7);

                result.Add(s);
            }
            data.Close();
            return result;
        }

        public static List<CameraSetup> FromSetupData(SqlDataReader data)
        {
            var result = new List<CameraSetup>();
            while (data.Read())
            {
                CameraSetup s = new CameraSetup();
                s.ID = data.GetInt32(0);
                s.cameraType = data.GetString(1);
                s.cameraSerial = data.GetString(2);
                s.lensType = data.GetString(3);
                s.lensSerial = data.GetString(4);
                s.lensX = data.IsDBNull(5) ? (int?)null : data.GetInt32(5);
                s.lensY = data.IsDBNull(6) ? (int?)null : data.GetInt32(6);
                s.lensA = data.IsDBNull(7) ? (double?)null : data.GetDouble(7);
                s.lensB = data.IsDBNull(8) ? (double?)null : data.GetDouble(8);
                s.maxRadius =  data.IsDBNull(9) ? (int?)null : data.GetInt32(9);
                s.width = data.IsDBNull(10) ? (int?)null : data.GetInt32(10);
                s.height = data.IsDBNull(11) ? (int?)null : data.GetInt32(11);
                s.processed = data.GetBoolean(12);
                s.name = data.IsDBNull(13) ? "" : data.GetString(13);
                if(s.name != "")
                    s.title = string.Format("{2}: {0} + {1}", s.cameraType, s.lensType, s.name);
                else
                    s.title = string.Format("{0} + {1}", s.cameraType, s.lensType);

                result.Add(s);
            }
            data.Close();
            return result;
        }

        public static List<ResultsSet> FromResultsData(SqlDataReader data)
        {
            var result = new List<ResultsSet>();

            while (data.Read())
            {
                ResultsSet set = new ResultsSet();
                set.ID = data.GetInt32(0);
                set.plotSetID = data.GetInt32(1);
                set.processed = data.GetBoolean(2);
                set.LAI = data.IsDBNull(3) ? (double?)null : data.GetDouble(3);
                set.LAI_SD = data.IsDBNull(4) ? (double?)null : data.GetDouble(4);
                set.resultLog = data.IsDBNull(5) ? null : data.GetString(5);
                set.data = data.IsDBNull(6) ? null : data.GetString(6);

                result.Add(set);
            }
            data.Close();
            return result;
        }

        public static List<Image> FromImageData(SqlDataReader data)
        {
            var result = new List<Image>();
            while (data.Read())
            {
                Image image = new Image();
                image.ID = data.GetInt32(0);
                image.filename = data.GetString(2);
                image.path = data.GetString(3);
                image.dngFilename = data.IsDBNull(4) ? (string)null : data.GetString(4);
                image.dngPath = data.IsDBNull(5) ? (string)null : data.GetString(5);
                result.Add(image);
            }
            data.Close();
            return result;
        }

        public static List<Species> FromSpeciesData(SqlDataReader data)
        {
            var result = new List<Species>();
            while (data.Read())
            {
                Species item = new Species();
                item.ID = data.GetInt32(0);
                item.name = data.GetString(1);
                result.Add(item);
            }
            data.Close();
            return result;
        }
        public static List<Ecosystem> FromEcosystemsData(SqlDataReader data)
        {
            var result = new List<Ecosystem>();
            while (data.Read())
            {
                Ecosystem item = new Ecosystem();
                item.ID = data.GetInt32(0);
                item.name = data.GetString(1);
                result.Add(item);
            }
            data.Close();
            return result;
        }
        public static List<Country> FromCountryData(SqlDataReader data)
        {
            var result = new List<Country>();
            while (data.Read())
            {
                Country item = new Country();
                item.ID = data.GetInt32(0);
                item.code = data.GetString(1);
                item.name = data.GetString(2);
                result.Add(item);
            }
            data.Close();
            return result;
        }
        public static List<Plot> FromPlotData(SqlDataReader data)
        {
            var result = new List<Plot>();
            while (data.Read())
            {
                Plot plot = new Plot();
                plot.ID = data.GetInt32(0);
                plot.siteID = data.GetInt32(1);
                plot.name = data.GetString(2);
                plot.slope = data.IsDBNull(3) ? (double?)null : data.GetDouble(3);
                plot.slopeAspect = data.IsDBNull(4) ? (double?)null : data.GetDouble(4);
                result.Add(plot);
            }
            data.Close();
            return result;
        }

        public static List<PlotSet> FromPlotSetData(SqlDataReader data)
        {
            var result = new List<PlotSet>();
            while (data.Read())
            {
                PlotSet plotSet = new PlotSet();
                plotSet.ID = data.GetInt32(0);
                plotSet.uploadSetID = data.GetInt32(1);
                plotSet.plotID = data.IsDBNull(2) ? (int?)null : data.GetInt32(2);

                Plot plot = new Plot();
                plot.ID = plotSet.plotID.Value;
                plot.name = data.IsDBNull(3) ? null : data.GetString(3);
                plot.slope = data.IsDBNull(4) ? (double?)null : data.GetDouble(4);
                plot.slopeAspect = data.IsDBNull(5) ? (double?)null : data.GetDouble(5);

                plotSet.plot = plot;

                result.Add(plotSet);
            }
            data.Close();
            return result;
        }
        public static List<PlotSet> FromPlotResultsData(SqlDataReader data)
        {
            var result = new List<PlotSet>();
            while (data.Read())
            {
                PlotSet plotSet = new PlotSet();
                plotSet.ID = data.GetInt32(0);
                plotSet.uploadSetID = data.GetInt32(1);
                plotSet.plotname = data.GetString(2);

                ResultsSet set = new ResultsSet();
                set.ID = data.GetInt32(3);
                set.plotSetID = data.GetInt32(4);
                set.processed = data.GetBoolean(5);
                set.LAI = data.IsDBNull(6) ? (double?)null : data.GetDouble(6);
                set.LAI_SD = data.IsDBNull(7) ? (double?)null : data.GetDouble(7);
                set.resultLog = data.IsDBNull(8) ? null : data.GetString(8);
                set.data = data.IsDBNull(9) ? null : data.GetString(9);
                plotSet.resultsSet = set;

                plotSet.plotID = data.GetInt32(10);

                result.Add(plotSet);
            }
            data.Close();
            return result;
        }

        public static FileUpload SaveFileUpload(FileUpload fileUpload, DB db = null)
        {
            db = new DB();
            int id;

            id = Convert.ToInt32(db.ExecuteScalar("INSERT INTO [fileUploads] ([siteID], [userID], [uploadDate], [operator], [filename], [path], [type]) VALUES (@siteID, @userID, @uploadDate, @operator, @filename, @path, @type);SELECT IDENT_CURRENT('[fileUploads]');"
                , new SqlParameter("siteID", fileUpload.siteID)
                , new SqlParameter("userID", fileUpload.userID)
                , new SqlParameter("uploadDate", fileUpload.uploadDate)
                , new SqlParameter("operator", fileUpload.uploader)
                , new SqlParameter("filename", fileUpload.filename)
                , new SqlParameter("path", fileUpload.filepath)
                , new SqlParameter("type", fileUpload.type)
                ));

            fileUpload.ID = id;

            foreach (FileUploadPlot fileUploadPlot in fileUpload.fileUploadPlots)
            {
                fileUploadPlot.fileUploadID = fileUpload.ID;
                id = Convert.ToInt32(db.ExecuteScalar("INSERT INTO [fileUploadPlots] ([plotID], [fileUploadID]) VALUES (@plotID, @fileUploadID);SELECT IDENT_CURRENT('[fileUploadPlots]');",
                    new SqlParameter("plotID", fileUploadPlot.plotID),
                    new SqlParameter("fileUploadID", fileUploadPlot.fileUploadID)
                    ));
                fileUploadPlot.ID = id;

                foreach (AGBuploadDetail AGBdetail in fileUploadPlot.AGBdetails)
                {
                    AGBdetail.fileUploadPlotID = fileUploadPlot.ID;
                    id = Convert.ToInt32(db.ExecuteScalar("INSERT INTO [AGBuploadDetail] ([fileUploadPlotID], [treeNr], [DBH], [speciesID]) VALUES (@fileUploadPlotID, @treeNr, @DBH, @speciesID);SELECT IDENT_CURRENT('[AGBuploadDetail]');",
                        new SqlParameter("fileUploadPlotID", AGBdetail.fileUploadPlotID),
                        new SqlParameter("treeNr", AGBdetail.treeNr),
                        new SqlParameter("DBH", AGBdetail.DBH),
                        new SqlParameter("speciesID", AGBdetail.speciesID)
                        ));
                    AGBdetail.ID = id;
                }
            }
            db.Dispose();
            return fileUpload;
        }

        public static CameraSetup SaveCameraSetup(CameraSetup cameraSetup, DB db = null)
        {
            db = new DB();
            int id;
            //INSERT INTO [cameraSetup] ([userID], [camType], [camSerial], [lensType], [lensSerial], [maxRadius], [pathCenter], [pathProj], [processed], [width], [height])
            id = Convert.ToInt32(db.ExecuteScalar("INSERT INTO [cameraSetup] ([userID], [camType], [camSerial], [lensType], [lensSerial], [maxRadius], [pathCenter], [pathProj], [processed], [width], [height], [a], [b], [x], [y], [name]) VALUES (@userID, @camType, @camSerial, @lensType, @lensSerial, @maxRadius, @pathCenter, @pathProj, @processed, @width, @height, @a, @b, @x, @y, @name);SELECT IDENT_CURRENT('[cameraSetup]');"
                   , new SqlParameter("userID", cameraSetup.userID)
                   , new SqlParameter("camType", cameraSetup.cameraType)
                   , new SqlParameter("camSerial", cameraSetup.cameraSerial)
                   , new SqlParameter("lensType", cameraSetup.lensType)
                   , new SqlParameter("lensSerial", cameraSetup.lensSerial)                   
                   , new SqlParameter("maxRadius", cameraSetup.maxRadius)
                   , new SqlParameter("pathCenter", cameraSetup.pathCenter)
                   , new SqlParameter("pathProj", cameraSetup.pathProj)
                   , new SqlParameter("processed", false)
                   , new SqlParameter("width", cameraSetup.width)
                   , new SqlParameter("height", cameraSetup.height)
                   , new SqlParameter("a", cameraSetup.lensA)
                   , new SqlParameter("b", cameraSetup.lensB)
                   , new SqlParameter("x", cameraSetup.lensX)
                   , new SqlParameter("y", cameraSetup.lensY)
                   , new SqlParameter("name", cameraSetup.name)
                   ));

            cameraSetup.ID = id;

            db.Dispose();
            return cameraSetup;
        }

        public static UploadSet SaveUploadSet(UploadSet uploadset, DB db = null)
        {
            db = new DB();
            int id;
            if (uploadset.cameraSetup.ID == null || uploadset.cameraSetup.ID == 0)
            {
                id = Convert.ToInt32(db.ExecuteScalar("INSERT INTO [cameraSetup] ([camType],[camSerial],[lensType] ,[lensSerial] ,[x] ,[y] ,[a] ,[b], [maxRadius] )VALUES (@camType ,@camSerial ,@lensType ,@lensSerial,@x ,@y ,@a,@b,@maxRadius);SELECT IDENT_CURRENT('[cameraSetup]');"
                   , new SqlParameter("camType", uploadset.cameraSetup.cameraType)
                   , new SqlParameter("camSerial", uploadset.cameraSetup.cameraSerial)
                   , new SqlParameter("lensType", uploadset.cameraSetup.lensType)
                   , new SqlParameter("lensSerial", uploadset.cameraSetup.lensSerial)
                   , new SqlParameter("x", uploadset.cameraSetup.lensX)
                   , new SqlParameter("y", uploadset.cameraSetup.lensY)
                   , new SqlParameter("a", uploadset.cameraSetup.lensA)
                   , new SqlParameter("b", uploadset.cameraSetup.lensB)
                   , new SqlParameter("maxRadius", uploadset.cameraSetup.maxRadius)));

                uploadset.cameraSetup.ID = id;
            }

            id = Convert.ToInt32(
                db.ExecuteScalar("INSERT INTO [uploadSet] ([camSetupID],[siteID],[userID],[person],[uploadTime],[siteName]) VALUES (@camSetupID ,@siteID,@userID,@person,@uploadTime, @siteName);SELECT IDENT_CURRENT('[uploadSet]');",
                new SqlParameter("camSetupID", uploadset.cameraSetup.ID),
                new SqlParameter("siteID", uploadset.siteID.HasValue ? uploadset.siteID.Value : 0),
                new SqlParameter("userID", uploadset.userID.ToString()),
                new SqlParameter("person", uploadset.person),
                new SqlParameter("uploadTime", uploadset.uploadTime),
                new SqlParameter("siteName", uploadset.siteName)));
            //new SqlParameter("slope", uploadset.slope),
            //new SqlParameter("slopeAspect", uploadset.slopeAspect)));

            uploadset.ID = id;

            foreach (PlotSet plotset in uploadset.plotSets)
            {
                plotset.uploadSetID = uploadset.ID;
                id = Convert.ToInt32(db.ExecuteScalar("INSERT INTO [plotSets]  ([uploadSetID] ,[plotID]) VALUES (@uploadSetID ,@plotID);SELECT IDENT_CURRENT('[plotSets]');",
                new SqlParameter("uploadSetID", plotset.uploadSetID),
                new SqlParameter("plotID", plotset.plotID)));

                plotset.ID = id;

                db.ExecuteScalar("INSERT INTO [results] ([plotSetID] ,[processed]) VALUES(@plotSetID ,@processed )",
                new SqlParameter("plotSetID", plotset.ID),
                new SqlParameter("processed", false));

                foreach (Image image in plotset.images)
                {
                    id = Convert.ToInt32(db.ExecuteScalar("INSERT INTO [images] ([plotSetID],[filename],[path])  VALUES (@plotSetID ,@filename ,@path);SELECT IDENT_CURRENT('[images]');",//, @dngFilename, @dngPath
                        //,[dngFilename],[dngPath]
                    new SqlParameter("plotSetID", plotset.ID),
                    new SqlParameter("filename", image.filename),
                    new SqlParameter("path", image.path)
                    //,
                    //new SqlParameter("dngFilename", image.dngFilename),
                    //new SqlParameter("dngPath", image.dngPath)
                    ));

                    image.ID = id;
                }
            }

            db.Dispose();
            return uploadset;
        }

        public static int InsertPlot(Plot plot, DB db = null)
        {
            db = new DB();
            int id;

            id = Convert.ToInt32(db.ExecuteScalar("INSERT INTO [plots] ([siteID], [name], [insertDate], [insertUser], [active]) VALUES(@siteID, @name, @insertDate, @insertUser, @active);SELECT IDENT_CURRENT('[plots]');",
                    new SqlParameter("siteID", plot.siteID),
                    new SqlParameter("name", plot.name),
                    new SqlParameter("insertDate", DateTime.Now),
                    new SqlParameter("insertUser", UserDA.CurrentUserId),
                    new SqlParameter("active", true)));
            return id;
        }

        public static int SavePlot(Plot plot, int? siteID, DB db = null)
        {
            db = new DB();
            int id;
            Plot oPlot = null;
            if (siteID.HasValue)
                oPlot = GetPlotByName(plot.name, siteID.Value);
            if (oPlot == null || (oPlot != null && (oPlot.slope != plot.slope || oPlot.slopeAspect != plot.slopeAspect)))
            {
                id = Convert.ToInt32(db.ExecuteScalar("INSERT INTO [plots] ([siteID], [name], [slope], [slopeAspect], [insertDate], [insertUser], [active]) VALUES(@siteID, @name, @slope, @slopeAspect, @insertDate, @insertUser, @active);SELECT IDENT_CURRENT('[plots]');",
                    new SqlParameter("siteID", siteID.HasValue ? siteID.Value : 0),
                    new SqlParameter("name", plot.name),
                    new SqlParameter("slope", plot.slope),
                    new SqlParameter("slopeAspect", plot.slopeAspect),
                    new SqlParameter("insertDate", DateTime.Now),
                    new SqlParameter("insertUser", UserDA.CurrentUserId),
                    new SqlParameter("active", true)));

                if (oPlot != null && (oPlot.slope != plot.slope || oPlot.slopeAspect != plot.slopeAspect))
                {
                    db.ExecuteScalar("UPDATE [plots] SET [active] = 0, [deleteDate] = @deleteDate, [deleteUser] = @deleteUser WHERE ID = @ID",
                        new SqlParameter("deleteDate", DateTime.Now),
                        new SqlParameter("deleteUser", UserDA.CurrentUserId),
                        new SqlParameter("ID", oPlot.ID));
                }
            }
            else
            {
                id = oPlot.ID;
            }

            db.Dispose();
            return id;
        }

        public static int insertQualityCheck(QualityCheck qc, DB db = null)
        {
            db = new DB();
            int id;

            id = Convert.ToInt32(db.ExecuteScalar("INSERT INTO [dbo].[qualityCheck] ([imageID], [status], [LAI], [LAIe], [threshold], [clumping]) VALUES (@imageID, @status , @LAI , @LAIe, @threshold , @clumping);SELECT IDENT_CURRENT('[qualityCheck]');",
                    new SqlParameter("imageID", qc.imageID),
                    new SqlParameter("status", qc.status),
                    new SqlParameter("LAI", qc.LAI),
                    new SqlParameter("LAIe", qc.LAIe),
                    new SqlParameter("threshold", qc.threshold),
                    new SqlParameter("clumping", qc.clumping)));

            return id;
        }

        public static void setUploadSetQualityCheck(int setId, DB db = null)
        {
            db = new DB();
            db.ExecuteScalar("UPDATE [dbo].[uploadSet] SET [qualityCheck] = 1 WHERE ID = " +  setId);
        }
    }
}