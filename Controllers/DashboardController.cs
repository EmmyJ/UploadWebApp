using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
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

        class MeteoNRTmodel
        {
            [Format("yyyy-MM-ddTHH:mm:ssZ")]
            public DateTime TIMESTAMP { get; set; }
            //public DateTime TIMESTAMP_END { get; set; }
            public string monthYear { get; set; }
            public double? TA { get; set; }
            public double? P { get; set; }
            public double? WTD { get; set; }
            public double? TA_1 { get; set; }
            public double? TA_8 { get; set; }
            public double? TS_2 { get; set; }
        }

        sealed class MeteoNRTMap : ClassMap<MeteoNRTmodel>
        {
            public MeteoNRTMap()
            {
                Map(m => m.TIMESTAMP).Name("TIMESTAMP").TypeConverterOption.Format("yyyy-MM-ddTHH:mm:ssZ");
                //Map(m => m.TIMESTAMP_END).Name("TIMESTAMP_END").TypeConverterOption.Format("yyyyMMddHHmm");
                Map(m => m.TA).Name("TA");
                Map(m => m.TA_1).Name("TA_1");
                Map(m => m.TA_8).Name("TA_8");
                Map(m => m.TS_2).Name("TS_2");
                Map(m => m.P).Name("P");
                Map(m => m.WTD).Name("WTD");
            }
        }

        class MaaMeteoNRTmodel
        {
            [Format("yyyy-MM-ddTHH:mm:ssZ")]
            public DateTime TIMESTAMP { get; set; }
            //public DateTime TIMESTAMP_END { get; set; }
            public string monthYear { get; set; }
            public double? P { get; set; }
        }

        sealed class MaaMeteoNRTMap : ClassMap<MaaMeteoNRTmodel>
        {
            public MaaMeteoNRTMap()
            {
                Map(m => m.TIMESTAMP).Name("TIMESTAMP").TypeConverterOption.Format("yyyy-MM-ddTHH:mm:ssZ");
                Map(m => m.P).Name("P");
            }
        }

        class FluxesNRTmodel
        {
            [Format("yyyy-MM-ddTHH:mm:ssZ")]
            public DateTime TIMESTAMP { get; set; }
            //public DateTime TIMESTAMP_END { get; set; }
            public double? NEE { get; set; }
            public double? NEEpos { get; set; }
            public double? NEEneg { get; set; }
        }

        sealed class FluxesNRTMap : ClassMap<FluxesNRTmodel>
        {
            public FluxesNRTMap()
            {
                Map(m => m.TIMESTAMP).Name("TIMESTAMP").TypeConverterOption.Format("yyyy-MM-ddTHH:mm:ssZ");
                Map(m => m.NEE).Name("NEE");
            }
        }

        class OTCNRTmodel {
            [Format("yyyy-MM-ddTHH:mm:ssZ")]
            public DateTime TIMESTAMP { get; set; }
            //public DateTime TIMESTAMP_END { get; set; }
            public double? pCO2 { get; set; }
            public double? pCO2atm { get; set; }
        }

        sealed class OTCNRTMap : ClassMap<OTCNRTmodel>
        {
            public OTCNRTMap()
            {
                Map(m => m.TIMESTAMP).Name("TIMESTAMP").TypeConverterOption.Format("yyyy-MM-ddTHH:mm:ssZ");
                //Map(m => m.TIMESTAMP_END).Name("TIMESTAMP_END").TypeConverterOption.Format("yyyyMMddHHmm");
                Map(m => m.pCO2).Name("pCO2 [uatm]");
                Map(m => m.pCO2atm).Name("pCO2 in atmosphere [uatm]");
            }
        }

        public ActionResult generateBraInfoData()
        {
            //meteo
            string filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT.csv");
            List<MeteoNRTmodel> NRT = new List<MeteoNRTmodel>();
            using (var fileReader = new StreamReader(filePath))
            using (var csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture))
            {
                csvReader.Context.RegisterClassMap<MeteoNRTMap>();
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");
                
                NRT = csvReader.GetRecords<MeteoNRTmodel>().ToList();
            }
            var options = new TypeConverterOptions { Formats = new[] { "yyyy-MM-ddTHH:mm:ssZ" } };

            //meteo L2
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_L2.csv");
            List<MeteoNRTmodel> MeteoL2 = new List<MeteoNRTmodel>();
            using (var fileReader = new StreamReader(filePath))
            using (var csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture))
            {
                csvReader.Context.RegisterClassMap<MeteoNRTMap>();
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");
                MeteoL2 = csvReader.GetRecords<MeteoNRTmodel>().ToList();
            }
            DateTime lastYear = DateTime.Today.AddYears(-1);
            var NRTy = MeteoL2.Where(y => y.TIMESTAMP > lastYear).ToList();
            NRT = NRTy.Concat(NRT).ToList();

            DateTime max = NRT.Max(x => x.TIMESTAMP);
            var NRT7 = NRT.Where(n => n.TIMESTAMP > max.AddDays(-7)).ToList();
            var NRTdaily = NRT.GroupBy(m => new { m.TIMESTAMP.Date })
                .Select(g => new MeteoNRTmodel
                {
                    TIMESTAMP = g.Key.Date,
                    TA = g.Average(m => m.TA),
                    P = g.Sum(m => m.P),
                    WTD = g.Average(m => m.WTD)
                }).ToList();
            var NRTmonthly = NRT.GroupBy(m => new { m.TIMESTAMP.Date.Month, m.TIMESTAMP.Date.Year })
                .Select(g => new MeteoNRTmodel
                {
                    monthYear = string.Format("{1}/{0}", g.Key.Month, g.Key.Year),
                    TA = g.Average(m => m.TA),
                    P = g.Sum(m => m.P),
                    WTD = g.Average(m => m.WTD)
                }).ToList();

            //read historical averages
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_AVG.csv");
            List<AveragesModel> AVG = new List<AveragesModel>();
            using (var fileReader = new StreamReader(filePath))
            using (var csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture))
            {
                csvReader.Context.RegisterClassMap<AveragesMap>();
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");

                AVG = csvReader.GetRecords<AveragesModel>().ToList();
            }

            //var TAdaily = (from n in NRTdaily select new { n.TIMESTAMP, n.TA }).ToList();
            var TAdaily = (from n in NRTdaily join a in AVG on n.TIMESTAMP.DayOfYear equals a.dayOfYear select new { n.TIMESTAMP, n.TA, a.TA_AVG }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_TA_daily.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(TAdaily);
            }

            var TA7 = (from n in NRT7 select new { n.TIMESTAMP, n.TA }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_TA_7days.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(TA7);
            }

            var T7mix = (from n in NRT7 select new { n.TIMESTAMP, n.TA_1, n.TA_8, n.TS_2 }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_Tmix_7days.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(T7mix);
            }

            var Pdaily = (from n in NRTdaily select new { n.TIMESTAMP, n.P }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_P_daily.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(Pdaily);
            }
            var Pmonthly = (from n in NRTmonthly select new { n.monthYear, n.P }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_P_monthly.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(Pmonthly);
            }

            var P7 = (from n in NRT7 select new { n.TIMESTAMP, n.P }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_P_7days.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(P7);
            }

            var WTDdaily = (from n in NRTdaily select new { n.TIMESTAMP, n.WTD }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_WTD_daily.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(WTDdaily);
            }

            var WTD7 = (from n in NRT7 select new { n.TIMESTAMP, n.WTD }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_WTD_7days.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(WTD7);
            }

            //Fluxes
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_FLUXES_NRT.csv");
            List<FluxesNRTmodel> FLUXES = new List<FluxesNRTmodel>();
            using (var fileReader = new StreamReader(filePath))
            using (var csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture))
            {
                csvReader.Context.RegisterClassMap<FluxesNRTMap>();
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");
                FLUXES = csvReader.GetRecords<FluxesNRTmodel>().ToList();
            }

            //FluxesL2
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_FLUXES_L2.csv");
            List<FluxesNRTmodel> FLUXESL2 = new List<FluxesNRTmodel>();
            using (var fileReader = new StreamReader(filePath))
            using (var csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture))
            {
                csvReader.Context.RegisterClassMap<FluxesNRTMap>();
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");
                FLUXESL2 = csvReader.GetRecords<FluxesNRTmodel>().ToList();
            }
            lastYear = DateTime.Today.AddYears(-1);
            var FLUXESy = FLUXESL2.Where(y => y.TIMESTAMP > lastYear).ToList();
            FLUXES = FLUXESy.Concat(FLUXES).ToList();

            max = FLUXES.Max(x => x.TIMESTAMP);
            var FLUXES7 = FLUXES.Where(n => n.TIMESTAMP > max.AddDays(-7)).ToList();
            FLUXES7 = FLUXES7.Where(n => n.TIMESTAMP > max.AddDays(-7))
                .Select(g => new FluxesNRTmodel
                {
                    TIMESTAMP = g.TIMESTAMP,
                    NEE = g.NEE ?? null,
                    NEEneg = g.NEE <= 0 ? g.NEE : null,
                    NEEpos = g.NEE > 0 ? g.NEE : null
                })
                .ToList();
            var FLUXESdaily = FLUXES.GroupBy(m => new { m.TIMESTAMP.Date })
                .Select(g => new FluxesNRTmodel
                {
                    TIMESTAMP = g.Key.Date,
                    NEE = g.Sum(m => m.NEE),
                    NEEneg = g.Sum(m => m.NEE) <= 0 ? g.Sum(m => m.NEE) : null,
                    NEEpos = g.Sum(m => m.NEE) > 0 ? g.Sum(m => m.NEE) : null
                }).ToList();

            var NEEdaily = (from n in FLUXESdaily select new { n.TIMESTAMP, n.NEEneg, n.NEEpos }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_FLUXES_NRT_NEE_daily.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(NEEdaily);
            }

            var NEE7 = (from n in FLUXES7 select new { n.TIMESTAMP, n.NEEneg, n.NEEpos }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_FLUXES_NRT_NEE_7days.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(NEE7);
            }

            //OTC NRT
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSOTC_BE-FOS_NRT.csv");
            List<OTCNRTmodel> OTCNRT = new List<OTCNRTmodel>();
            using (var fileReader = new StreamReader(filePath))
            using (var csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture))
            {
                csvReader.Context.RegisterClassMap<OTCNRTMap>();
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");
                OTCNRT = csvReader.GetRecords<OTCNRTmodel>().ToList();
            }
            max = OTCNRT.Max(x => x.TIMESTAMP);
            var OTCNRT7 = OTCNRT.Where(n => n.TIMESTAMP > max.AddDays(-7)).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSOTC_BE-FOS_NRT_7days.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(OTCNRT7);
            }
            return null;
        }

        public ActionResult generateMaaInfoData()
        {
            //meteo
            string filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Maa_METEO_NRT.csv");
            List<MaaMeteoNRTmodel> NRT = new List<MaaMeteoNRTmodel>();
            using (var fileReader = new StreamReader(filePath))
            using (var csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture))
            {
                csvReader.Context.RegisterClassMap<MaaMeteoNRTMap>();
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");

                NRT = csvReader.GetRecords<MaaMeteoNRTmodel>().ToList();
            }
            var options = new TypeConverterOptions { Formats = new[] { "yyyy-MM-ddTHH:mm:ssZ" } };

            //meteo L2
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Maa_METEO_L2.csv");
            List<MaaMeteoNRTmodel> MeteoL2 = new List<MaaMeteoNRTmodel>();
            using (var fileReader = new StreamReader(filePath))
            using (var csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture))
            {
                csvReader.Context.RegisterClassMap<MaaMeteoNRTMap>();
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");
                MeteoL2 = csvReader.GetRecords<MaaMeteoNRTmodel>().ToList();
            }
            DateTime lastYear = DateTime.Today.AddYears(-1);
            var NRTy = MeteoL2.Where(y => y.TIMESTAMP > lastYear).ToList();
            NRT = NRTy.Concat(NRT).ToList();

            DateTime max = NRT.Max(x => x.TIMESTAMP);
            var NRT7 = NRT.Where(n => n.TIMESTAMP > max.AddDays(-7)).ToList();
            var NRTdaily = NRT.GroupBy(m => new { m.TIMESTAMP.Date })
                .Select(g => new MaaMeteoNRTmodel
                {
                    TIMESTAMP = g.Key.Date,
                    P = g.Sum(m => m.P)
                }).ToList();
            var NRTmonthly = NRT.GroupBy(m => new { m.TIMESTAMP.Date.Month, m.TIMESTAMP.Date.Year })
                .Select(g => new MaaMeteoNRTmodel
                {
                    monthYear = string.Format("{1}/{0}", g.Key.Month, g.Key.Year),
                    P = g.Sum(m => m.P)
                }).ToList();   

            var Pdaily = (from n in NRTdaily select new { n.TIMESTAMP, n.P }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Maa_METEO_NRT_P_daily.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(Pdaily);
            }
            var Pmonthly = (from n in NRTmonthly select new { n.monthYear, n.P }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Maa_METEO_NRT_P_monthly.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(Pmonthly);
            }

            var P7 = (from n in NRT7 select new { n.TIMESTAMP, n.P }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Maa_METEO_NRT_P_7days.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(P7);
            }            

            //Fluxes
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Maa_FLUXES_NRT.csv");
            List<FluxesNRTmodel> FLUXES = new List<FluxesNRTmodel>();
            using (var fileReader = new StreamReader(filePath))
            using (var csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture))
            {
                csvReader.Context.RegisterClassMap<FluxesNRTMap>();
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");
                FLUXES = csvReader.GetRecords<FluxesNRTmodel>().ToList();
            }

            //FluxesL2
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Maa_FLUXES_L2.csv");
            List<FluxesNRTmodel> FLUXESL2 = new List<FluxesNRTmodel>();
            using (var fileReader = new StreamReader(filePath))
            using (var csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture))
            {
                csvReader.Context.RegisterClassMap<FluxesNRTMap>();
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");
                FLUXESL2 = csvReader.GetRecords<FluxesNRTmodel>().ToList();
            }
            lastYear = DateTime.Today.AddYears(-1);
            var FLUXESy = FLUXESL2.Where(y => y.TIMESTAMP > lastYear).ToList();
            FLUXES = FLUXESy.Concat(FLUXES).ToList();

            max = FLUXES.Max(x => x.TIMESTAMP);
            var FLUXES7 = FLUXES.Where(n => n.TIMESTAMP > max.AddDays(-7)).ToList();
            FLUXES7 = FLUXES7.Where(n => n.TIMESTAMP > max.AddDays(-7))
                .Select(g => new FluxesNRTmodel
                {
                    TIMESTAMP = g.TIMESTAMP,
                    NEE = g.NEE ?? null,
                    NEEneg = g.NEE <= 0 ? g.NEE : null,
                    NEEpos = g.NEE > 0 ? g.NEE : null
                })
                .ToList();
            var FLUXESdaily = FLUXES.GroupBy(m => new { m.TIMESTAMP.Date })
                .Select(g => new FluxesNRTmodel
                {
                    TIMESTAMP = g.Key.Date,
                    NEE = g.Sum(m => m.NEE),
                    NEEneg = g.Sum(m => m.NEE) <= 0 ? g.Sum(m => m.NEE) : null,
                    NEEpos = g.Sum(m => m.NEE) > 0 ? g.Sum(m => m.NEE) : null
                }).ToList();

            var NEEdaily = (from n in FLUXESdaily select new { n.TIMESTAMP, n.NEEneg, n.NEEpos }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Maa_FLUXES_NRT_NEE_daily.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(NEEdaily);
            }

            var NEE7 = (from n in FLUXES7 select new { n.TIMESTAMP, n.NEEneg, n.NEEpos }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Maa_FLUXES_NRT_NEE_7days.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(NEE7);
            }            
            return null;
        }

        public ActionResult generateChartHtmls()
        {
            generateChartHtml("Dorinne", "Fig_Dorinne.csv", "ESDorinneData.html");
            generateChartHtml("Vielsalm", "Fig_Vielsalm.csv", "ESVielsalmData.html");
            generateChartHtml("Lonz&eacute;e", "Fig_Lonzee.csv", "ESLonzeeData.html");

            return null;
        }

        public ActionResult generateChartHtml(string name, string fileIn, string fileOut) {
            string filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), fileIn);
            string outPath = System.IO.Path.Combine("C:/xampp8/htdocs/ICOS2023clean/", fileOut);

            StreamReader sr = new StreamReader(filePath);
            StreamWriter sw = new StreamWriter(outPath);

            String line = sr.ReadLine();
            String prevLine = "";
            String html = "";
            int i = 0;
            while(line != null)
            {
                try
                {
                    if (line.StartsWith("Fig_"))
                    {
                        if (i >= 3)
                        {
                            i = 0;
                            html += "</div>\n</div>\n<div class='row clearfix'><div class='col-md-12'>\n";
                        }
                        var a = line.Split(',');
                        string url = "https://www.gembloux.ulg.ac.be/icos/img/" + a[0];
                        string alt = a[1];
                        html += "<div class='col-md-4'>\n<a href='" + url + "' target='_blank'><img alt='" + alt + "' title='" + alt + "' src='" + url + "'></a>\n</div>\n";
                        i++;
                    }
                    else if (line.ToLower() == "last week,")
                    {
                        html += "<div class='bg-grad-white-bottomleft section'>            <div class='container'>                <div class='row clearfix'>                    <div class='col-md-12'>                        <h1 class='minimal-h1'>" + name + " Data</h1>			<h2 class='text-additional'>" + line.Split(',')[0] + "</h2>";
                    }
                    else if (line.ToLower() == "last year," || line.ToLower() == "pluriannual,")
                    {
                        html += "</div>\n</div>\n</div>\n</div>\n<div class='bg-grad-white-bottomleft section'>            <div class='container'>                <div class='row clearfix'>                    <div class='col-md-12'>                        <h2 class='text-additional'>" + line.Split(',')[0] + "</h2>";

                    }
                    else 
                    {
                        i = 0;
                        if(prevLine.StartsWith("Fig_"))
                            html += "</div>\n</div>\n<div class='row clearfix'><div class='col-md-12'>\n";
                        html += "<h3 class='text-main'>" + line.Split(',')[0] + "</h3>";
                    }
                }
                catch (Exception ex) { }
                prevLine = line;
                line = sr.ReadLine();                
            }
            sr.Close();
            html += "</div>\n</div>\n</div>\n</div>\n";
            sw.Write(html);
            sw.Close();

            return null;
        }

        public ActionResult generateHistoricalAverages()
        {
            //meteo
            string filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "FLX_BE-Bra_FLUXNET2015_FULLSET_DD_1996-2020_beta-3_TA_F.csv");
            string outPath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_AVG.csv");
            using (var fileReader = new StreamReader(filePath))
            using (var csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture))                
            using (var writer = new StreamWriter(outPath))
            using (var csvWriter = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csvReader.Context.RegisterClassMap<HistoricalMap>();
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
                csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");
                var options = new TypeConverterOptions { Formats = new[] { "yyyyMMdd" } };
                var data = csvReader.GetRecords<HistoricalModel>().ToList();

                csvWriter.WriteHeader<AveragesModel>();
                var avgs = new List<AveragesModel>();

                for (var i = 0; i < 366; i++)
                {
                    var day = data[i];
                    var avg = data.Where(d => d.TIMESTAMP.Month == data[i].TIMESTAMP.Month && d.TIMESTAMP.Day == data[i].TIMESTAMP.Day).Select(t => t.TA_F).Average();
                    AveragesModel m = new AveragesModel();
                    m.dayOfYear = day.TIMESTAMP.DayOfYear;
                    m.TA_AVG = avg;

                    csvWriter.NextRecord();
                    csvWriter.WriteRecord<AveragesModel>(m);
                }
            }
            return null;
        }

        sealed class HistoricalMap : ClassMap<HistoricalModel>
        {
            public HistoricalMap()
            {
                Map(m => m.TIMESTAMP).Name("TIMESTAMP").TypeConverterOption.Format("yyyyMMdd");
                Map(m => m.TA_F).Name("TA_F");
                Map(m => m.TA_AVG).Name("TA_AVG");
            }

        }

        class HistoricalModel
        {
            [Format("yyyyMMdd")]
            public DateTime TIMESTAMP { get; set; }
            public int? dayOfYear { get; set; }
            public double? TA_F { get; set; }
            public double? TA_AVG { get; set; }
        }

        sealed class AveragesMap : ClassMap<AveragesModel>
        {
            public AveragesMap()
            {
                Map(m => m.dayOfYear).Name("dayOfYear");
                Map(m => m.TA_AVG).Name("TA_AVG");

            }
        }

        class AveragesModel
        {
            public int? dayOfYear { get; set; }
            public double? TA_AVG { get; set; }
        }
    }
    }
