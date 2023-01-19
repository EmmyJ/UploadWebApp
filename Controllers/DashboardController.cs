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

                //group same sensors per plot
                for (int a = 0; a < headers.Count(); a++)
                {
                    string header = headers[a];
                    if (header.Substring(0, 3) == "TS_" && headersFinal.Count > 0)
                    {
                        //int? TSi = List<string>.FindIndex(headersFinal, s => s.StartsWith(header.Substring(0, 4), StringComparison.OrdinalIgnoreCase));
                        int? TSi = headersFinal.FindIndex(x => x.StartsWith("," + header.Substring(0, 4), StringComparison.OrdinalIgnoreCase));
                        if (TSi < 0)
                        {
                            headersFinal.Add("," + header);
                            colnrs[a] = hf;
                            hf++;
                        } 
                        else     
                        {
                            headersFinal[TSi.Value] = headersFinal[TSi.Value] + "," + header ;
                            colnrs[a] = TSi.Value;
                        }    
                    }
                    else if (header.Substring(0, 3) == "TA_" && headersFinal.Count > 0)
                    {
                        //int? TSi = List<string>.FindIndex(headersFinal, s => s.StartsWith(header.Substring(0, 4), StringComparison.OrdinalIgnoreCase));
                        int? TAi = headersFinal.FindIndex(x => x.StartsWith("," + header.Substring(0, 4), StringComparison.OrdinalIgnoreCase));
                        if (TAi < 0)
                        {
                            headersFinal.Add("," + header);
                            colnrs[a] = hf;
                            hf++;
                        }
                        else
                        {
                            headersFinal[TAi.Value] = headersFinal[TAi.Value] + "," + header;
                            colnrs[a] = TAi.Value;
                        }
                    }
                    else if (header.Substring(0, 4) == "SWC_" && headersFinal.Count > 0) 
                    {
                        int? SWCi = headersFinal.FindIndex(x => x.StartsWith("," + header.Substring(0, 5), StringComparison.OrdinalIgnoreCase));
                        if (SWCi < 0)
                        {
                            headersFinal.Add("," + header);
                            colnrs[a] = hf;
                            hf++;
                        }
                        else
                        {
                            headersFinal[SWCi.Value] = headersFinal[SWCi.Value] + "," + header;
                            colnrs[a] = SWCi.Value;
                        }
                    }
                    else
                    {
                        headersFinal.Add("," + header);
                        colnrs[a]= hf;
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
                        if(colNr == prevCol)
                        {
                            sba[colNr - 2] += "," + values[v];
                        }
                        else { 
                            sba[colNr - 2] += "\n" + values[0] + "," + values[v];
                        }
                        prevCol = colNr;
                    }
                }

                for (int s = 0; s < sba.Count(); s++)
                {
                    string varname = (headersFinal[s + 2].Substring(1));
                    if (varname.StartsWith("TS_"))
                    {
                        varname = varname.Substring(0, 4);
                    }
                    else if (varname.StartsWith("TA_"))
                    {
                        varname = varname.Substring(0, 4);
                    }
                    else if (varname.StartsWith("SWC_"))
                    {
                        varname = varname.Substring(0, 5);
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
