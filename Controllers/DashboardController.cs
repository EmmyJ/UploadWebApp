using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
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
                        if(match == false && header.Substring(0,groupedCol.Value) == groupedCol.Key && headersFinal.Count > 0 )
                        {
                            int? colI = headersFinal.FindIndex(x => x.StartsWith("," + header.Substring(0, groupedCol.Value+1), StringComparison.OrdinalIgnoreCase));

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
                    if(!match)
                    {
                        headersFinal.Add("," + header);
                        colnrs[a] = hf;
                        hf++;
                    }


                    //if (header.Substring(0, 3) == "TS_" && headersFinal.Count > 0)
                    //{
                    //    //int? TSi = List<string>.FindIndex(headersFinal, s => s.StartsWith(header.Substring(0, 4), StringComparison.OrdinalIgnoreCase));
                    //    int? TSi = headersFinal.FindIndex(x => x.StartsWith("," + header.Substring(0, 4), StringComparison.OrdinalIgnoreCase));
                    //    if (TSi < 0)
                    //    {
                    //        headersFinal.Add("," + header);
                    //        colnrs[a] = hf;
                    //        hf++;
                    //    } 
                    //    else     
                    //    {
                    //        headersFinal[TSi.Value] = headersFinal[TSi.Value] + "," + header ;
                    //        colnrs[a] = TSi.Value;
                    //    }    
                    //}
                    //else if (header.Substring(0, 3) == "TA_" && headersFinal.Count > 0)
                    //{
                    //    //int? TSi = List<string>.FindIndex(headersFinal, s => s.StartsWith(header.Substring(0, 4), StringComparison.OrdinalIgnoreCase));
                    //    int? TAi = headersFinal.FindIndex(x => x.StartsWith("," + header.Substring(0, 4), StringComparison.OrdinalIgnoreCase));
                    //    if (TAi < 0)
                    //    {
                    //        headersFinal.Add("," + header);
                    //        colnrs[a] = hf;
                    //        hf++;
                    //    }
                    //    else
                    //    {
                    //        headersFinal[TAi.Value] = headersFinal[TAi.Value] + "," + header;
                    //        colnrs[a] = TAi.Value;
                    //    }
                    //}
                    //else if (header.Substring(0, 4) == "SWC_" && headersFinal.Count > 0) 
                    //{
                    //    int? SWCi = headersFinal.FindIndex(x => x.StartsWith("," + header.Substring(0, 5), StringComparison.OrdinalIgnoreCase));
                    //    if (SWCi < 0)
                    //    {
                    //        headersFinal.Add("," + header);
                    //        colnrs[a] = hf;
                    //        hf++;
                    //    }
                    //    else
                    //    {
                    //        headersFinal[SWCi.Value] = headersFinal[SWCi.Value] + "," + header;
                    //        colnrs[a] = SWCi.Value;
                    //    }
                    //}
                    //else
                    //{
                    //    headersFinal.Add("," + header);
                    //    colnrs[a]= hf;
                    //    hf++;
                    //}
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
                    //if (varname.StartsWith("TS_"))
                    //{
                    //    varname = varname.Substring(0, 4);
                    //}
                    //else if (varname.StartsWith("TA_"))
                    //{
                    //    varname = varname.Substring(0, 4);
                    //}
                    //else if (varname.StartsWith("SWC_"))
                    //{
                    //    varname = varname.Substring(0, 5);
                    //}
                    bool match = false;
                    foreach (var item in groupedCols)
                    { 
                        if (!match && varname.StartsWith(item.Key))
                        {
                            varname = varname.Substring(0,item.Value + 1);
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
                var options = new TypeConverterOptions { Formats = new[] { "dd-MM-yyyyT00:00:00Z" } };
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
                var options = new TypeConverterOptions { Formats = new[] { "dd-MM-yyyyT00:00:00Z" } };
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
