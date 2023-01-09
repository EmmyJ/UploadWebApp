using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
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

        public ActionResult BeBraMeteoSens()
        {
            try {
                string filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEOSENS_NRT.csv");
                StreamReader sr = new StreamReader(filePath);
                string fulltext = sr.ReadToEnd().ToString(); //read full file text
                sr.Close();

                string[] rows = fulltext.Split('\n'); //split full file text into rows
                string[] headers = rows[0].Split(',');
                List<string> sba = new List<string>();
                string[] values;

                //create stringbuilder for each sensor and add the headerline
                for (int h = 2; h < headers.Count(); h++)
                {
                    sba.Add("TIMESTAMP," + headers[h] + "\n");
                }

                for (int i = rows.Count() - 241; i < rows.Count() - 1; i++)
                {
                    rows[i].Replace(",,", ", ,"); //to not lose empty columns
                    values = rows[i].Split(',');

                    for (int v = 2; v < values.Count(); v++)
                    {
                        sba[v - 2] += values[0] + "," + values[v] + "\n";
                    }

                }

                for (int s = 0; s < sba.Count(); s++)
                {
                    string outfilePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "MeteoSens/BE-Bra-MeteoSens-" + headers[s + 2] + ".csv");
                    StreamWriter sw = new StreamWriter(outfilePath);
                    sw.Write(sba[s]);
                    sw.Close();
                }

                return View(headers.Where((v, i) => i > 1).ToArray());
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e);
                return View();
            }
        }           

    }
}
