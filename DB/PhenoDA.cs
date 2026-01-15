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
            var result = db.ExecuteReader("SELECT [ID], [name], [status], [lastDate] FROM [dbo].[phenoCameras]");

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
    }
}