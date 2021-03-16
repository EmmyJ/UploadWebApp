using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Web;
using UploadWebapp.Models;
using UploadWebapp.Models.ETC;
using Image = UploadWebapp.Models.Image;
using System.Text.RegularExpressions;

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

        public static void rerunUploadSet(int setId, DB db = null)
        {
            db = new DB();
            db.ExecuteScalar("update results set processed = 'false' where plotSetID in (select ID from plotSets where uploadSetID = " + setId + ")");
            db.Dispose();
        }

        public static void FillImagePlotLocations(DB db = null)
        {
            db = new DB();
            string pattern = "^[a-zA-Z]{2}-[a-zA-Z0-9]{3}_DHP_[a-zA-Z0-9]{2}_[SC]{1}P[0-9]{2}_L[0-9]{2}_[0-9]{8}.[a-zA-Z0-9]{3}$";
            Regex rg = new Regex(pattern);

            var result = db.ExecuteReader("SELECT[ID],[plotSetID],[filename],[path], exif FROM[images]");
            List<Image> images = ImageDA.FromImageData(result);

            foreach (Image i in images)
            {
                if (rg.IsMatch(i.filename))
                {
                    int location = 0;
                    int.TryParse(i.filename.Substring(20, 2), out location);

                    if (location != 0)
                    {
                        result = db.ExecuteReader("select pl.ID from plotLocations pl left join plots p on pl.plotID = p.ID left join plotSets ps on ps.plotID = p.ID where pl.active =1 and ps.ID = " + i.plotSetID + " and pl.location = " + location);

                        while (result.Read())
                        {
                            i.plotLocationID = result.GetInt32(0);
                            db.ExecuteScalar("update images set plotLocationID = " + i.plotLocationID + " where ID = " + i.ID);
                        }
                        result.Close();
                    }
                }
            }

            db.Dispose();
        }
    }
}