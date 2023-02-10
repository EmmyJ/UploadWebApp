using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace UploadWebapp.Controllers
{
    public class DashboardController : Controller
    {
        //
        // GET: /Dashboard/

        public ActionResult Index()
        {
            return View();
        }

        public CookieContainer LoginCarbonPortal(string user, string password)
        {
            string baseurl = "https://cpauth.icos-cp.eu/password/login";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(baseurl);

            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            string login = string.Format("go=&mail={0}&password={1}", user, password);
            byte[] postbuf = Encoding.ASCII.GetBytes(login);
            req.ContentLength = postbuf.Length;
            Stream rs = req.GetRequestStream();
            rs.Write(postbuf, 0, postbuf.Length);
            rs.Close();

            CookieContainer cookie = req.CookieContainer = new CookieContainer();

            WebResponse resp = req.GetResponse();
            resp.Close();
            return cookie;
        }

        public string getPidId(CookieContainer cookie)
        {
            string baseurl = "https://meta.icos-cp.eu/sparql";
            String q = "prefix%20cpmeta%3A%20%3Chttp%3A%2F%2Fmeta.icos-cp.eu%2Fontologies%2Fcpmeta%2F%3E%0Aprefix%20prov%3A%20%3Chttp%3A%2F%2Fwww.w3.org%2Fns%2Fprov%23%3E%0Aselect%20%3Fdobj%0Awhere%20%7B%0A%20%20%20%20%3Fdobj%20cpmeta%3AhasObjectSpec%20%3Chttp%3A%2F%2Fmeta.icos-cp.eu%2Fresources%2Fcpmeta%2FetcNrtMeteo%3E%20%3B%0A%20%20%20%20%20%20%20%20cpmeta%3AwasAcquiredBy%2Fprov%3AwasAssociatedWith%20%3Chttp%3A%2F%2Fmeta.icos-cp.eu%2Fresources%2Fstations%2FES_BE-Bra%3E%20%3B%0A%20%20%20%20%20%20%20%20cpmeta%3AhasSizeInBytes%20%5B%5D%20.%0A%20%20%20%20FILTER%20NOT%20EXISTS%20%7B%5B%5D%20cpmeta%3AisNextVersionOf%20%3Fdobj%7D%0A%7D";
            String url = String.Format("{0}?query={1}", baseurl, q);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            //req.CookieContainer = cookie;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            
            string query = string.Format("go=&query={0}", q);
            byte[] postbuf = Encoding.ASCII.GetBytes(query);
            req.ContentLength = postbuf.Length;
            Stream rs = req.GetRequestStream();
            rs.Write(postbuf, 0, postbuf.Length);
            rs.Close();
                        
            WebResponse resp = req.GetResponse();
            System.IO.Stream s = resp.GetResponseStream();
            System.IO.StreamReader reader = new System.IO.StreamReader(s, Encoding.UTF8);
            string _s = reader.ReadToEnd();

            reader.Close();
            s.Close();
            resp.Close();

            var details = JObject.Parse(_s);

            string dobj = details["results"]["bindings"][0]["dobj"]["value"].ToString();

            return dobj;
        }

        public string usePidId(string pidUrl, CookieContainer cookie) {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(pidUrl);
            req.CookieContainer = cookie;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";


            WebResponse resp = req.GetResponse();
            System.IO.Stream s = resp.GetResponseStream();

            return null;
        }

        public async Task<ActionResult> DownloadDataFromCarbonPortal()
        {
            CookieContainer cookie;
            cookie = LoginCarbonPortal("downloader@uantwerpen.be", "tUHPDUhZ4FetsGr");
            string pidUrl = getPidId(cookie);
            pidUrl = pidUrl.Replace("https://meta.icos-cp.eu/objects/", "https://data.icos-cp.eu/csv/");
            usePidId(pidUrl, cookie);


            //var url = "https://cpauth.icos-cp.eu/password/login";
            //string postData = HttpUtility.UrlEncode("mail") + "=" + HttpUtility.UrlEncode("downloader@uantwerpen.be") + "&"
            //    + HttpUtility.UrlEncode("password") + "=" + HttpUtility.UrlEncode("tUHPDUhZ4FetsGr");

            //HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            //myHttpWebRequest.Method = "POST";

            //byte[] data = Encoding.ASCII.GetBytes(postData);

            //myHttpWebRequest.ContentType = "application/x-www-form-urlencoded";
            //myHttpWebRequest.ContentLength = data.Length;

            //Stream requestStream = myHttpWebRequest.GetRequestStream();
            //requestStream.Write(data, 0, data.Length);
            //requestStream.Close();

            //HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();


            string URL = "https://cpauth.icos-cp.eu/password/login";
            System.Net.WebRequest webRequest = System.Net.WebRequest.Create(URL);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            Stream reqStream = webRequest.GetRequestStream();
            string postData = "mail=downloader@uantwerpen.be&password=tUHPDUhZ4FetsGr";
            byte[] postArray = Encoding.ASCII.GetBytes(postData);
            reqStream.Write(postArray, 0, postArray.Length);
            reqStream.Close();
            StreamReader sr = new StreamReader(webRequest.GetResponse().GetResponseStream());
            string Result = sr.ReadToEnd();


            return null;
        }

        public ActionResult SensorData(string station, string type, int days)
        {
            try
            {
                string filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_" + station + "_METEOSENS_NRT.csv");
                StreamReader sr = new StreamReader(filePath);
                string fulltext = sr.ReadToEnd().ToString(); //read full file text
                sr.Close();

                string[] rows = fulltext.Split('\n'); //split full file text into rows
                string[] headers = rows[0].Split(',');
                List<string> headersFinal = new List<string>();
                //Dictionary<int, int> colnrs = new Dictionary<int, int>();        
                int[] colnrs = new int[headers.Count()]; ;
                List<string> sba = new List<string>();
                string[] values;
                int hf = 0;
                Dictionary<string, int> groupedCols = new Dictionary<string, int>() {
                    {"TS_", 3},
                    {"TA_", 3},
                    {"SWC_", 4}
                    //,{"RH_1_;RH_3_", 5}
                };

                //group same sensors per plot
                for (int a = 0; a < headers.Count(); a++)
                {
                    string header = headers[a];
                    bool match = false;
                    foreach (var groupedCol in groupedCols)
                    {
                        if (match == false && header.Substring(0, groupedCol.Value) == groupedCol.Key && headersFinal.Count > 0)
                        {
                            int? colI = headersFinal.FindIndex(x => x.StartsWith("," + header.Substring(0, groupedCol.Value + 1), StringComparison.OrdinalIgnoreCase));

                            if (colI < 0)
                            {
                                headersFinal.Add("," + header);
                                colnrs[a] = hf;
                                hf++;
                            }
                            else
                            {
                                headersFinal[colI.Value] = headersFinal[colI.Value] + "," + header;
                                colnrs[a] = colI.Value;
                            }

                            match = true;
                        }
                    }
                    if (!match)
                    {
                        headersFinal.Add("," + header);
                        colnrs[a] = hf;
                        hf++;
                    }
                }

                //create stringbuilder for each sensor and add the headerline
                for (int h = 2; h < headersFinal.Count(); h++)
                {
                    sba.Add("TIMESTAMP" + headersFinal[h]);// + "\n");
                }

                //only use data for last x days
                int lines = (days * 48) + 1;
                lines = lines < rows.Count() ? lines : rows.Count();

                for (int i = rows.Count() - lines; i < rows.Count() - 1; i++)
                {
                    rows[i].Replace(",,", ", ,"); //to not lose empty columns
                    values = rows[i].Split(',');
                    int prevCol = -1;
                    int colNr = 0;

                    for (int v = 2; v < values.Count(); v++)
                    {
                        //add each value to the corresponding stringbuilders
                        colNr = colnrs[v];
                        if (colNr == prevCol)
                        {
                            sba[colNr - 2] += "," + values[v];
                        }
                        else
                        {
                            sba[colNr - 2] += "\n" + values[0] + "," + values[v];
                        }
                        prevCol = colNr;
                    }
                }

                for (int s = 0; s < sba.Count(); s++)
                {
                    string varname = (headersFinal[s + 2].Substring(1));
                    bool match = false;
                    foreach (var item in groupedCols)
                    {
                        if (!match && varname.StartsWith(item.Key))
                        {
                            varname = varname.Substring(0, item.Value + 1);
                            match = true;
                        }
                    }

                    string outfilePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "MeteoSens/" + station + "-MeteoSens-" + varname + ".csv");
                    StreamWriter sw = new StreamWriter(outfilePath);
                    sw.Write(sba[s]);
                    sw.Close();
                    headersFinal[s + 2] = varname;
                }

                ViewBag.title = station + " - " + type;
                ViewBag.station = station;

                return View(headersFinal.Where((v, i) => i > 1).ToArray());
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e);
                return View();
            }
        }

        class TAmodel
        {
            public DateTime TimeStamp { get; set; }
            public double? TA { get; set; }
            public double? TAavg { get; set; }
        }

        sealed class TAMap : ClassMap<TAmodel>
        {
            public TAMap()
            {
                Map(m => m.TimeStamp).Name("TIMESTAMP");
                Map(m => m.TA).Name("TA");
                Map(m => m.TAavg).Name("TA_avg");
            }
        }

        public ActionResult generateCPdataBra()
        {
            string filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "BE-Bra_TAwMean.csv");
            var fileReader = new StreamReader(filePath);
            var csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture);

            csvReader.Context.RegisterClassMap<TAMap>();
            csvReader.Context.TypeConverterOptionsCache.GetOptions<int?>().NullValues.Add("null");

            var list = csvReader.GetRecords<TAmodel>().ToList();
            DateTime max = list.Max(x => x.TimeStamp);
            var last7days = list.Where(m => m.TimeStamp > max.AddDays(-7));

            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "BE-Bra_TAwMean_7d.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                var options = new TypeConverterOptions { Formats = new[] { "yyyy-MM-ddT00:00:00Z" } };
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.WriteRecords(last7days);
            }

            var avgs = list.GroupBy(m => new { m.TimeStamp.Date })
                .Select(g => new TAmodel
                {
                    TimeStamp = g.Key.Date,
                    TA = g.Average(m => m.TA),
                    TAavg = g.Average(m => m.TAavg)
                }).ToList();

            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "BE-Bra_TAwMean_daily.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                var options = new TypeConverterOptions { Formats = new[] { "yyyy-MM-ddT00:00:00Z" } };
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.WriteRecords(avgs);
            }

            fileReader.Close();
            return null;
        }

        public ActionResult CPdataBra()
        {
            return View();
        }
    }
}
