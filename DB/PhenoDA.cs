using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;
using UploadWebapp.Models;
using UploadWebapp.Models.Pheno;

namespace UploadWebapp.DB
{
    public class PhenoDA
    {
        public static List<PhenoCamera> getPhenoCameras(DB db = null)
        {
            db = new DB();
            var result = db.ExecuteReader("SELECT [ID], [name], [status], [lastDate] FROM [dbo].[phenoCameras] WHERE [status] = 1");

            List<PhenoCamera> phenocams = FromPhenoCamerasData(result);
            db.Dispose();
            return phenocams;
        }

        public static void saveLastDates(List<PhenoCamera> phenoCameras, DB db = null)
        {
            db = new DB();

            foreach (PhenoCamera cam in phenoCameras)
            {
                if (cam.newDate != null && (cam.lastDate == null || cam.newDate > cam.lastDate))
                {
                    db.ExecuteScalar("UPDATE [LAI_App].[dbo].[phenoCameras] SET [lastDate] = @lastDate WHERE ID = @ID",
                    new SqlParameter("lastDate", cam.newDate),
                    new SqlParameter("ID", cam.ID));
                }
            }
            db.Dispose();
        }

        public static List<PhenoCamera> FromPhenoCamerasData(SqlDataReader data)
        {
            var result = new List<PhenoCamera>();
            while (data.Read())
            {
                PhenoCamera phenocam = new PhenoCamera();
                phenocam.ID = data.GetInt32(0);
                phenocam.name = data.GetString(1);
                phenocam.status = (phenoCamStatus)data.GetByte(2);
                phenocam.lastDate = data.IsDBNull(3) ? (DateTime?)null : data.GetDateTime(3);

                result.Add(phenocam);
            }
            data.Close();
            return result;
        }

        public static int insertPhenoUpload(PhenoUpload upload, DB db = null)
        {
            db = new DB();
            int id;

            id = Convert.ToInt32(db.ExecuteScalar("INSERT INTO [dbo].[phenoUploads] ([phenoCameraID], [name] ,[dateTime],[status],[dateProblem]) VALUES (@phenoCameraID, @name ,@dateTime,@status,@dateProblem);SELECT IDENT_CURRENT('[dbo].[phenoUploads]');",
                        new SqlParameter("phenoCameraID", upload.phenoCameraID),
                        new SqlParameter("name", upload.name),
                        new SqlParameter("dateTime", DateTime.Now),
                        new SqlParameter("status", upload.status),
                        new SqlParameter("dateProblem", upload.dateProblem) ));
            db.Dispose();
            return id;
        }

        public static void savePhenoUploadHash(PhenoUpload upload, DB db = null)
        {
            db = new DB();
            db.ExecuteScalar("UPDATE [dbo].[phenoUploads] set [hash] = @hash, [dateTime] = @dateTime, [status] = @status WHERE ID = @ID",
                        new SqlParameter("hash", upload.hash),
                        new SqlParameter("dateTime", DateTime.Now),
                        new SqlParameter("status", upload.status),
                        new SqlParameter("ID", upload.ID));
            db.Dispose();
        }

        public static void savePhenoUploadStatus(int ID, int status, DB db = null) {
            db = new DB();
            db.ExecuteScalar("UPDATE [dbo].[phenoUploads] set [dateTime] = @dateTime, [status] = @status WHERE ID = @ID",
                        new SqlParameter("dateTime", DateTime.Now),
                        new SqlParameter("status", status),
                        new SqlParameter("ID", ID));
            db.Dispose();
        }

        public static void savePhenoUploadStatusAndCount(int ID, int status, int count, DB db = null)
        {
            db = new DB();
            db.ExecuteScalar("UPDATE [dbo].[phenoUploads] set [dateTime] = @dateTime, [status] = @status, [fileCount] = @fileCount WHERE ID = @ID",
                        new SqlParameter("dateTime", DateTime.Now),
                        new SqlParameter("status", status),
                        new SqlParameter("fileCount", count),
                        new SqlParameter("ID", ID));
            db.Dispose();
        }

        public static List<PhenoReportItem> getDailyPhenoReportItems(DB db = null)
        {
            db = new DB();

            var result = db.ExecuteReader("select c.name, u.name, c.expFileCount, isnull(u.fileCount,0) fileCount, u.status, u.hash from [LAI_App].[dbo].[phenoCameras] c LEFT OUTER JOIN [LAI_App].[dbo].[phenoUploads] u on u.phenoCameraID = c.ID where c.status = 1 and ( cast(u.dateTime as date) = cast(getdate() as date) or u.dateTime is null) order by c.name");

            List<PhenoReportItem> list = FromPhenoReportData(result);
            db.Dispose();

            return list;
        }

        public static List<PhenoReportItem> FromPhenoReportData(SqlDataReader data)
        {
            var result = new List<PhenoReportItem>();
            while (data.Read())
            {
                PhenoReportItem item = new PhenoReportItem();
                item.cameraName = data.GetString(0);
                item.fileName = data.IsDBNull(1) ? "" : data.GetString(1);
                item.expFileCount = data.IsDBNull(2) ? 0 : data.GetInt16(2);
                item.actFileCount = data.IsDBNull(3) ? 0 : data.GetInt16(3);
                item.status = data.IsDBNull(4) ? (phenoUploadStatus?)null: (phenoUploadStatus?)data.GetByte(4);
                item.hash = data.IsDBNull(5) ? "" : data.GetString(5);

                result.Add(item);
            }
            data.Close();
            return result;
        }
    }
}