﻿using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using UploadWebapp.DB;
using UploadWebapp.Models.Pheno;

namespace UploadWebapp.Controllers
{
    public class PhenoController : Controller
    {
        // GET: Pheno
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult CreateUploadPhenocamZips()
        {
            try
            {
                string phenofolder = ConfigurationManager.AppSettings["phenofolder"];
                string mail = ConfigurationManager.AppSettings["CPmail"];
                string password = ConfigurationManager.AppSettings["CPpassword"];

                string logs = phenofolder + "\\logs";
                if (!System.IO.Directory.Exists(logs))
                    System.IO.Directory.CreateDirectory(logs);
                StreamWriter log = new StreamWriter(phenofolder + "\\logs\\phenocam_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + ".log", true);
                log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- " + "Start Phenocam upload");

                DateTime yesterday = DateTime.Today.Date.AddDays(-1);

                List<PhenoCamera> phenoCameras = PhenoDA.getPhenoCameras();
                string uploadedFolder = phenofolder + "\\uploaded";
                if (!System.IO.Directory.Exists(uploadedFolder))
                    System.IO.Directory.CreateDirectory(uploadedFolder);
                var directories = Directory.GetDirectories(phenofolder);

                var ziplist = new List<Zipfile>();

                //BE-Maa-F01-L01_2025_04_30_224602.jpg
                Regex rg = new Regex("[a-zA-Z]{2}-[a-zA-Z0-9]{3}-F[0-9]{2}-L[0-9]{2}_[0-9]{4}_[0-9]{2}_[0-9]{2}_[0-9]{6}.[a-zA-Z0-9]{3}");
                //BE-Maa-F01-L01_2025_04_30_224602.meta
                Regex rgm = new Regex("[a-zA-Z]{2}-[a-zA-Z0-9]{3}-F[0-9]{2}-L[0-9]{2}_[0-9]{4}_[0-9]{2}_[0-9]{2}_[0-9]{6}.meta");
                //BE-Maa-F01-L01_IR_2025_04_30_224602.jpg
                Regex rgir = new Regex("[a-zA-Z]{2}-[a-zA-Z0-9]{3}-F[0-9]{2}-L[0-9]{2}_IR_[0-9]{4}_[0-9]{2}_[0-9]{2}_[0-9]{6}.[a-zA-Z0-9]{3}");
                //BE-Maa-F01-L01_IR_2025_04_30_224602.meta
                Regex rgirm = new Regex("[a-zA-Z]{2}-[a-zA-Z0-9]{3}-F[0-9]{2}-L[0-9]{2}_IR_[0-9]{4}_[0-9]{2}_[0-9]{2}_[0-9]{6}.meta");

                //create lists of files for zips
                foreach (var dir in directories)
                {
                    DirectoryInfo directoryName = new DirectoryInfo(dir);
                    string cam = directoryName.Name;
                    if (phenoCameras.SingleOrDefault(p => p.name == cam && p.status == phenoCamStatus.process) != null)
                    {
                        foreach (var file in Directory.GetFiles(dir))
                        {
                            try
                            {
                                FileInfo fi = new FileInfo(file);
                                string filename = fi.Name;
                                string site = "";
                                string suffix = "";
                                int year = 0;
                                int month = 0;
                                int day = 0;
                                string timestamp = "";
                                string ext = "";
                                string newName = "";

                                if (rg.IsMatch(filename) || rgm.IsMatch(filename) || rgir.IsMatch(filename) || rgirm.IsMatch(filename))
                                {
                                    site = filename.Substring(0, 6);
                                    suffix = "L" + filename.Substring(12, 2) + "_F" + filename.Substring(8, 2);
                                    ext = fi.Extension;
                                    if (rgm.IsMatch(filename))
                                    {
                                        //BE-Maa-F01-L01_2025_04_30_224602.meta
                                        int.TryParse(filename.Substring(15, 4), out year);
                                        int.TryParse(filename.Substring(20, 2), out month);
                                        int.TryParse(filename.Substring(23, 2), out day);
                                        timestamp = filename.Substring(26, 6);
                                        newName = site + "_PHEN_" + year + month.ToString("00") + day.ToString("00") + timestamp + "_" + suffix + ext;
                                    }
                                    else if (rg.IsMatch(filename))
                                    {
                                        //BE-Maa-F01-L01_2025_04_30_224602.jpg
                                        int.TryParse(filename.Substring(15, 4), out year);
                                        int.TryParse(filename.Substring(20, 2), out month);
                                        int.TryParse(filename.Substring(23, 2), out day);
                                        timestamp = filename.Substring(26, 6);
                                        newName = site + "_PHEN_" + year + month.ToString("00") + day.ToString("00") + timestamp + "_" + suffix + ext;

                                        //TODO!!! copy for calculations
                                    }
                                    else if (rgirm.IsMatch(filename))
                                    {
                                        //BE-Maa-F01-L01_IR_2025_04_30_224602.meta
                                        int.TryParse(filename.Substring(18, 4), out year);
                                        int.TryParse(filename.Substring(23, 2), out month);
                                        int.TryParse(filename.Substring(26, 2), out day);
                                        timestamp = filename.Substring(29, 6);
                                        newName = site + "_PHEN_IR_" + year + month.ToString("00") + day.ToString("00") + timestamp + "_" + suffix + ext;
                                    }
                                    else
                                    {
                                        //BE-Maa-F01-L01_IR_2025_04_30_224602.jpg
                                        int.TryParse(filename.Substring(18, 4), out year);
                                        int.TryParse(filename.Substring(23, 2), out month);
                                        int.TryParse(filename.Substring(26, 2), out day);
                                        timestamp = filename.Substring(29, 6);
                                        newName = site + "_PHEN_IR_" + year + month.ToString("00") + day.ToString("00") + timestamp + "_" + suffix + ext;
                                    }

                                    DateTime groupDate = new DateTime(year, month, day);
                                    // only upload files from yesterday, to prevent uploading incomplete sets 
                                    if (groupDate <= yesterday)
                                    {
                                        fi.MoveTo(fi.Directory.FullName + "\\" + newName);

                                        if (ziplist.Where(s => s.siteCode == site && s.suffix == suffix).Where(d => d.date == groupDate).ToList().Count == 0)
                                        {
                                            Zipfile zipfile = new Zipfile();
                                            zipfile.siteCode = site;
                                            zipfile.date = groupDate;
                                            zipfile.suffix = suffix;
                                            zipfile.dayfiles = new List<string>();
                                            zipfile.dayfiles.Add(fi.Directory.FullName + "\\" + newName);
                                            ziplist.Add(zipfile);
                                        }
                                        else
                                        {
                                            Zipfile zipfile = ziplist.Where(s => s.siteCode == site).Where(d => d.date == groupDate).ToList()[0];
                                            zipfile.dayfiles.Add(fi.Directory.FullName + "\\" + newName);
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- " + "Error:" + e.Message);
                            }
                        }
                    }
                }

                // zip the files
                log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- " + "Generating ZIPs:");
                foreach (var item in ziplist)
                {
                    item.fileName = item.siteCode + "_PHEN_" + item.date.ToString("yyyyMMdd") + "_" + item.suffix + ".zip";
                    item.fullName = phenofolder + "\\ZIP\\" + item.fileName;

                    using (ZipArchive archive = ZipFile.Open(item.fullName, ZipArchiveMode.Create))
                    {
                        foreach (var fPath in item.dayfiles)
                        {
                            archive.CreateEntryFromFile(fPath, Path.GetFileName(fPath));
                            System.IO.File.Delete(fPath);
                            //TODO
                            // Move to backupfolder for the time being
                            //System.IO.File.Move(fPath, fPath.Replace("toUpload", "backup"));

                        }
                        log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- " + item.fileName);
                    }
                }

                //effekes niet om te testen
                try
                {
                    CPuploadGetToken(log, phenofolder, mail, password);
                    log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- " + "Generating hashcodes:");
                    using (SHA256 mySHA256 = SHA256.Create())
                    {
                        foreach (var item in ziplist)
                        {
                            String xsha;

                            SHA256 Sha256 = SHA256.Create();
                            using (FileStream fileStream = System.IO.File.OpenRead(item.fullName))
                            {

                                fileStream.Position = 0;
                                byte[] hashValue = mySHA256.ComputeHash(fileStream);
                                xsha = BitConverter.ToString(hashValue).Replace("-", String.Empty);
                                item.hash = xsha;
                            }

                            log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- " + item.fileName + " - " + item.hash);

                            //            //write metadata file
                            //            string text = File.ReadAllText(folder + "metaPackageXX.json");
                            //            text = text.Replace("XXX1", item.fileName);
                            //            text = text.Replace("XXX2", item.hash);
                            //            text = text.Replace("XXX3", item.date.ToString("yyyy-MM-ddT00:00:00Z"));
                            //            text = text.Replace("XXX4", item.date.ToString("yyyy-MM-ddT23:59:00Z"));
                            //            text = text.Replace("XXX5", item.siteCode);

                            //            File.WriteAllText(folder + "metaPackage.json", text);

                            //            //send metadata
                            //            log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- start send metapackage");
                            //            ProcessStartInfo procStartInfo2 = new ProcessStartInfo(folder + "curl.exe", " /C -k --cookie cookies.txt -H \"Content-Type: application/json\" -X POST -d @\"" + folder + "metaPackage.json\" https://meta.icos-cp.eu/upload");
                            //            reply = stuff(procStartInfo2,log, true);
                            //            log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- reply: " + reply);
                            //            string mpName = folder + "uploaded\\" + item.fileName.Replace(".zip", "_metaPackage.json");
                            //            if (File.Exists(mpName))
                            //                File.Delete(mpName);
                            //            File.Move(folder + "metaPackage.json",mpName);
                            //            log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- end send metapackage");

                            //            //upload file
                            //            log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- start send " + item.fileName);
                            //            ProcessStartInfo procStartInfo3 = new ProcessStartInfo(folder + "curl.exe", "-k -v --cookie cookies.txt -H \"Transfer-Encoding: chunked\" --upload-file " + item.fullName + " https://data.icos-cp.eu/objects/" + item.hash);

                            //            //log.WriteLine(procStartInfo3.Arguments.ToString());
                            //            stuff(procStartInfo3, log, false);

                            //            log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- end send " + item.fileName);

                            //            //move file
                            //            if (File.Exists(item.fullName.Replace("toUpload", "uploaded")))
                            //                File.Delete(item.fullName.Replace("toUpload", "uploaded"));
                            //            try
                            //            {
                            //                WaitForFile(item.fullName.ToString(), maxWaitInSec: 60);
                            //                var web = new HtmlWeb();
                            //                var doc = web.Load(reply.Replace("data", "meta"));
                            //                var parsedtext = doc.ParsedText;
                            //                if (parsedtext != null && parsedtext.Contains("btn-warning") && parsedtext.Contains(reply))
                            //                {
                            //                    File.Move(item.fullName, item.fullName.Replace("toUpload", "uploaded"));
                            //                    log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- send success: " + reply.Replace("data", "meta"));
                            //                }
                            //                else
                            //                {
                            //                    File.Move(item.fullName, item.fullName.Replace("toUpload", "failed"));
                            //                    log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- send failed: " + reply.Replace("data", "meta"));
                            //                }                            
                            //            }
                            //            catch (Exception e)
                            //            { }
                        }
                    }
                }
                catch (Exception objException)
                {
                    log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- " + "Error: " + objException.ToString());
                }


                log.Close();
                return View();
            }
            catch (Exception e)
            {
                return View(e.ToString());
            }
        }

        private static void CPuploadGetToken(StreamWriter curlLogger, string folder, string mail, string password)
        {
            //StreamWriter curlLogger = new StreamWriter(folder + "logs\\curltoken_" + DateTime.Now.ToString("yyyyMMddHHmm") + ".log", true);
            try
            {
                // create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // Incidentally, /c tells cmd that we want it to execute the command that follows,
                // and then exit.
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo(folder + "\\curl.exe", " /C -k --cookie-jar cookies.txt --data \"mail=" + mail + "&password=" + password + "\" https://cpauth.icos-cp.eu/password/login");
                stuff(procStartInfo, folder, curlLogger, true);
            }
            catch (Exception objException)
            {
                // Log the exception
                curlLogger.WriteLine("Error 1:: " + objException.ToString());
            }
        }

        private static string stuff(ProcessStartInfo procStartInfo, string folder, StreamWriter log, bool read)
        {
            // The following commands are needed to redirect the standard output.
            // This means that it will be redirected to the Process.StandardOutput StreamReader.
            procStartInfo.WorkingDirectory = folder;
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.RedirectStandardError = true;
            procStartInfo.UseShellExecute = false;
            // Do not create the black window.
            procStartInfo.CreateNoWindow = true;
            string reply = null;

            // Make the process and set its start information.
            using (Process proc = new Process())
            {
                proc.StartInfo = procStartInfo;

                // Start the process.
                proc.Start();

                // Attach to stdout and stderr.
                using (StreamReader std_out = proc.StandardOutput)
                {
                    using (StreamReader std_err = proc.StandardError)
                    {
                        // Display the results.
                        if (read)
                        {
                            string a1 = std_out.ReadToEnd();
                            string a2 = std_err.ReadToEnd();
                            log.WriteLine(a1);
                            log.WriteLine(a2);
                            reply = a1;
                        }

                        // Clean up.
                        std_err.Close();
                        std_out.Close();
                        proc.Close();
                    }

                    // Now we create a process, assign its ProcessStartInfo and start it
                }
            }
            return reply;
        }


        /// <summary>
        /// Blocks until the file is not locked any more.
        /// </summary>
        /// <param name="fullPath"></param>
        private static bool WaitForFile(string fullPath, int maxWaitInSec)
        {
            int numTries = 0;
            while (true)
            {
                ++numTries;
                try
                {
                    // Attempt to open the file exclusively.
                    using (FileStream fs = new FileStream(fullPath,
                        FileMode.Open, FileAccess.ReadWrite,
                        FileShare.None, 100))
                    {
                        fs.ReadByte();

                        // If we got this far the file is ready
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (numTries == 1 || numTries % 20 == 0)

                        if (numTries >= maxWaitInSec * 2)
                        {
                            throw new Exception("Max wait time reached for file : " + fullPath + ". Waited for " + maxWaitInSec + " seconds but lock not released");
                        }

                    // Wait for the lock to be released
                    System.Threading.Thread.Sleep(500);
                }


            }
            return true;
        }
    }
}