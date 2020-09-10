using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Web;
using UploadWebapp.Models;
using UploadWebapp.Models.ETC;

namespace UploadWebapp.DB
{
    public class ETCDA
    {
        public static List<AggImage> getAggregationImages(string year, DB db = null)
        {
            db = new DB();
            var result = db.ExecuteReader("select i.ID, i.filename, i.LAI, i.clumping, q.status from images i join qualityCheck q on q.imageID = i.ID where q.status <> 0 and filename like '%_" + year + "%' and not i.LAI is null");

            List<AggImage> images = FromAggImageData(result);
            return images;
        }

        public static List<AggImage> FromAggImageData(SqlDataReader data)
        {
            var result = new List<AggImage>();
            while (data.Read())
            {
                AggImage image = new AggImage();
                image.ID = data.GetInt32(0);
                image.filename = data.GetString(1);
                image.LAI = data.IsDBNull(2) ? (double?) null : data.GetDouble(2);
                image.clumping = data.IsDBNull(3) ? (double?)null : data.GetDouble(3);
                image.QCstatus = (QCstatus)data.GetByte(4);

                image.site = image.filename.Substring(0, 6);
                image.plotname = image.filename.Substring(14, 4);
                image.location = image.filename.Substring(19, 3);
                image.dateTaken = DateTime.ParseExact(image.filename.Substring(23, 8), "yyyyMMdd", CultureInfo.InvariantCulture);

                result.Add(image);
            }
            data.Close();
            return result;
        }
    }
}