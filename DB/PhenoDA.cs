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

    }
}