using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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

        /*public ActionResult retryFails(int folder =1)
        {
            try
            {
                string phenofolder = "";
                switch (folder)
                {
                    case 0:
                        phenofolder = ConfigurationManager.AppSettings["phenoSFTPfolder"];
                        break;
                    case 1:
                        phenofolder = ConfigurationManager.AppSettings["phenoFTPfolder"];
                        break;
                    default:
                        phenofolder = ConfigurationManager.AppSettings["phenoSFTPfolder"];
                        break;
                }

                string mail = ConfigurationManager.AppSettings["CPmail"];
                string password = ConfigurationManager.AppSettings["CPpassword"];
                string logs = phenofolder + "\\logs";
                if (!System.IO.Directory.Exists(logs))
                    System.IO.Directory.CreateDirectory(logs);
                StreamWriter log = new StreamWriter(phenofolder + "\\logs\\retry_phenocam_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + ".log", true);
                log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- " + "Start Retry Phenocam upload");

                var directory = new DirectoryInfo(phenofolder + "\\failed");
                var upDir = new DirectoryInfo(phenofolder + "\\uploaded");
                CPuploadGetToken(log, phenofolder, mail, password);

                foreach (var file in Directory.GetFiles(directory.FullName,"*202609*"))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        string name = fi.Name;
                        string metaUrl = Directory.GetFiles(upDir.FullName, "*" + name.Substring(0, 28) + ".json").SingleOrDefault();

                        PhenoUpload upload = PhenoDA.GetPhenoUpload(name);

                        if (upload != null) {
                            //send metadata
                            log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- start send metapackage " + name);
                            ProcessStartInfo procStartInfo2 = new ProcessStartInfo(phenofolder + "\\curl.exe", " /C -k --cookie cookies.txt -H \"Content-Type: application/json\" -X POST -d @\"" + metaUrl + "\" https://meta.icos-cp.eu/upload");
                            string reply = stuff(procStartInfo2, phenofolder, log, true);
                            log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- reply: " + reply);

                            log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- end send metapackage");

                            upload.status = phenoUploadStatus.metaSent;
                            PhenoDA.savePhenoUploadStatus(upload.ID, (int)upload.status);

                            //upload file
                            log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- start send " + fi.Name);
                            ProcessStartInfo procStartInfo3 = new ProcessStartInfo(phenofolder + "\\curl.exe", "-k -v --cookie cookies.txt -H \"Transfer-Encoding: chunked\" --upload-file " + fi.FullName + " https://data.icos-cp.eu/objects/" + upload.hash);

                            //log.WriteLine(procStartInfo3.Arguments.ToString());
                            stuff(procStartInfo3, phenofolder, log, false);

                            log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " -- end send " + fi.Name);
                        }
                    }
                    catch (Exception e) { log.Close(); }
                }
                log.Close();
            }

            catch (Exception e) { }

            return View();

        }*/

        // ---------------------------------------------------------------------------
        // Phenocam daily ZIP creation + ICOS upload
        //
        // Required usings:
        //   System, System.Collections.Generic, System.Configuration, System.Diagnostics,
        //   System.Globalization, System.IO, System.IO.Compression, System.Linq, System.Net,
        //   System.Security.Cryptography, System.Text.RegularExpressions, System.Web.Mvc,
        //   HtmlAgilityPack
        //
        // Targets C# 6 / .NET Framework (no out-vars, no ValueTuples).
        // Assumes these members already exist in the controller / project:
        //   RunProcess, WaitForFile        -> PhenocamHelpers.cs
        //   LoginToIcos, HasCookie         -> PhenocamAuth.cs (replaces CPuploadGetToken)
        //   PhenoDA, PhenoCamera, PhenoUpload, Zipfile, phenoCamStatus, phenoUploadStatus
        // ---------------------------------------------------------------------------

        #region Constants and parsing

        private const string IcosMetaUploadUrl = "https://meta.icos-cp.eu/upload";
        private const string IcosDataUploadUrl = "https://data.icos-cp.eu/objects/";

        // Only files captured inside this time-of-day window (from the file name, both ends inclusive) go into a ZIP.
        private static readonly TimeSpan UploadWindowStart = new TimeSpan(6, 31, 0);
        private static readonly TimeSpan UploadWindowEnd = new TimeSpan(20, 29, 0);

        /// <summary>
        /// One regex for every supported camera file name (replaces the previous eight).
        ///
        ///   New format: BE-Maa-C01_2025_04_30_224602.jpg   /  BE-Maa-C01_IR_2025_04_30_224602.meta
        ///   Old format: BE-Maa-L01-F01_2025_04_30_224602.jpg / BE-Maa-L01-F01_IR_2025_04_30_224602.meta
        ///
        /// Named groups: site, old | new (camera part), ir, year, month, day, time, ext.
        /// The pattern is anchored; only the jpg/jpeg extension is case-insensitive.
        /// </summary>
        private static readonly Regex PhenoFileRegex = new Regex(
            @"^(?<site>[A-Za-z]{2}-[A-Za-z0-9]{3})-(?:(?<old>L[0-9]{2}-F[0-9]{2})|(?<new>C[0-9]{2}))" +
            @"(?<ir>_IR)?_(?<year>[0-9]{4})_(?<month>[0-9]{2})_(?<day>[0-9]{2})_(?<time>[0-9]{6})" +
            @"\.(?<ext>(?i:jpe?g)|meta)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>The parsed pieces of a phenocam file name.</summary>
        private sealed class PhenoFileName
        {
            public string Site;        // e.g. "BE-Maa"
            public string CameraId;    // e.g. "BE-Maa-C01" or "BE-Maa-L01-F01" (used as calc sub-folder)
            public string Suffix;      // e.g. "C01"; empty for old-format names until they are renamed
            public bool IsOld;         // old camera naming (L01-F01)
            public bool IsIr;          // infrared image/metadata
            public bool IsMeta;        // .meta sidecar instead of an image
            public string Time;        // HHmmss
            public TimeSpan TimeOfDay; // the same time as a TimeSpan, for the window check
            public string Extension;   // including the dot, original casing
            public DateTime Date;      // capture day
        }

        /// <summary>
        /// Parses a file name. Returns false when it is not a phenocam file.
        /// An impossible calendar date (e.g. month 13) throws; callers log that per file.
        /// </summary>
        private static bool TryParseName(string fileName, out PhenoFileName parsed)
        {
            parsed = null;
            Match m = PhenoFileRegex.Match(fileName);
            if (!m.Success) return false;

            string ext = m.Groups["ext"].Value;
            bool isOld = m.Groups["old"].Success;
            string site = m.Groups["site"].Value;

            parsed = new PhenoFileName
            {
                Site = site,
                IsOld = isOld,
                Suffix = m.Groups["new"].Value,
                CameraId = site + "-" + (isOld ? m.Groups["old"].Value : m.Groups["new"].Value),
                IsIr = m.Groups["ir"].Success,
                IsMeta = ext == "meta",
                Time = m.Groups["time"].Value,
                TimeOfDay = new TimeSpan(int.Parse(m.Groups["time"].Value.Substring(0, 2)),
                                         int.Parse(m.Groups["time"].Value.Substring(2, 2)),
                                         int.Parse(m.Groups["time"].Value.Substring(4, 2))),
                Extension = "." + ext,
                Date = new DateTime(int.Parse(m.Groups["year"].Value),
                                    int.Parse(m.Groups["month"].Value),
                                    int.Parse(m.Groups["day"].Value))
            };
            return true;
        }

        /// <summary>Builds the ICOS archive name, e.g. BE-Maa_PHEN_IR_20250430224602_C01.jpg</summary>
        private static string BuildArchiveName(PhenoFileName p)
        {
            return p.Site + "_PHEN_" + (p.IsIr ? "IR_" : "") +
                   p.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + p.Time +
                   "_" + p.Suffix + p.Extension;
        }

        private static void Log(StreamWriter log, string message)
        {
            log.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " -- " + message);
        }

        /// <summary>Moves a file, replacing the destination if it already exists.</summary>
        private static void MoveOverwrite(string source, string destination)
        {
            System.IO.File.Delete(destination); // no-op when the file does not exist
            System.IO.File.Move(source, destination);
        }

        #endregion

        #region Action

        /// <summary>
        /// Collects yesterday's (and older) phenocam files, zips them per site/camera/day,
        /// registers each upload in the database and sends metadata + ZIP to ICOS.
        /// </summary>
        /// <param name="folder">1 = FTP folder (default), anything else = SFTP folder.</param>
        public ActionResult CreateUploadPhenocamZips(int folder = 1)
        {
            try
            {
                // .NET Framework may default to SSL3/TLS 1.0, which ICOS rejects ("Could not create SSL/TLS secure channel").
                // This is process-wide and only affects .NET HTTPS calls (HtmlWeb); curl.exe does its own TLS.
                // Better long-term: set <httpRuntime targetFramework="4.7.2" /> in web.config and remove this line.
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                string phenofolder = ConfigurationManager.AppSettings[folder == 1 ? "phenoFTPfolder" : "phenoSFTPfolder"];
                string calcfolder = ConfigurationManager.AppSettings["calcfolder"];
                string mail = ConfigurationManager.AppSettings["CPmail"];
                string password = ConfigurationManager.AppSettings["CPpassword"];

                // Make sure every working folder exists (CreateDirectory is a no-op if it already does).
                foreach (string sub in new[] { "logs", "backup", "ZIP", "uploaded", "failed", "excluded" })
                    Directory.CreateDirectory(Path.Combine(phenofolder, sub));

                string logFile = Path.Combine(phenofolder, "logs",
                    "phenocam_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm", CultureInfo.InvariantCulture) + ".log");

                // using => the log is flushed/closed even when something throws; AutoFlush keeps it current.
                using (var log = new StreamWriter(logFile, true) { AutoFlush = true })
                {
                    Log(log, "Start Phenocam upload");

                    // Only complete days are processed, so a still-running day is never zipped half-way.
                    DateTime yesterday = DateTime.Today.AddDays(-1);

                    List<PhenoCamera> cameras = PhenoDA.getPhenoCameras();

                    // 1. Rename / back up / copy to calc folder / group per ZIP
                    Dictionary<string, Zipfile> zips = CollectDailyFiles(phenofolder, calcfolder, cameras, yesterday, log);
                    PhenoDA.saveLastDates(cameras);

                    // 2. Create the ZIP files (one failing ZIP no longer stops the others)
                    Log(log, "Generating ZIPs:");
                    var created = new List<Zipfile>();
                    foreach (Zipfile item in zips.Values.OrderBy(z => z.date).ThenBy(z => z.siteCode))
                    {
                        try
                        {
                            CreateZip(item, Path.Combine(phenofolder, "ZIP"));
                            Log(log, item.fileName);
                            created.Add(item);
                        }
                        catch (Exception e)
                        {
                            Log(log, "Error creating " + item.fileName + ": " + e.Message);
                        }
                    }

                    // 3. Upload (skip the token request entirely when there is nothing to send)
                    if (created.Count > 0)
                    {
                        try
                        {
                            LoginToIcos(log, phenofolder, mail, password);
                            string metaTemplate = System.IO.File.ReadAllText(Path.Combine(phenofolder, "metaPackageXX.json"));

                            Log(log, "Generating hashcodes:");
                            using (SHA256 sha = SHA256.Create()) // one instance, reused for every file
                            {
                                foreach (Zipfile item in created)
                                {
                                    try
                                    {
                                        UploadZip(item, phenofolder, metaTemplate, sha, log);
                                    }
                                    catch (Exception e)
                                    {
                                        Log(log, "Send failed: " + item.fileName + " " + e.Message);
                                        item.upload.status = phenoUploadStatus.sentFailed;
                                        PhenoDA.savePhenoUploadStatus(item.upload.ID, (int)item.upload.status);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log(log, "Error: " + e);
                        }
                    }
                }
                return View();
            }
            catch (Exception e)
            {
                return View(e.ToString());
            }
        }

        #endregion

        #region Step 1: collect files

        /// <summary>
        /// Walks every camera folder that is flagged "process" and, per file:
        ///   1. backs it up,
        ///   2. copies non-IR images to the calc folder (phenocam calculations),
        ///   3. renames old-format names to the current camera name,
        ///   4. renames to the ICOS archive name and groups it into a ZIP definition,
        ///      but only when the day is complete (date &lt;= yesterday) and the capture time is inside
        ///      the upload window (files outside it are moved to \excluded).
        /// Errors are logged per file so one bad file never blocks the rest.
        /// </summary>
        /// <returns>ZIP definitions keyed by site|suffix|date.</returns>
        private Dictionary<string, Zipfile> CollectDailyFiles(
            string phenofolder, string calcfolder, List<PhenoCamera> cameras, DateTime yesterday, StreamWriter log)
        {
            var zips = new Dictionary<string, Zipfile>();
            string backupFolder = Path.Combine(phenofolder, "backup");
            string excludedFolder = Path.Combine(phenofolder, "excluded");
            var activeCameras = cameras.Where(c => c.status == phenoCamStatus.process).ToList();

            foreach (string dir in Directory.GetDirectories(phenofolder))
            {
                string dirName = new DirectoryInfo(dir).Name;

                // Resolve the camera once per folder (it was looked up up to ~10x per file before).
                PhenoCamera camera = activeCameras.SingleOrDefault(c => c.name == dirName || c.oldName == dirName);
                if (camera == null) continue; // logs, backup, ZIP, ... or inactive cameras

                foreach (string file in Directory.GetFiles(dir))
                {
                    try
                    {
                        var fi = new FileInfo(file);

                        PhenoFileName parsed;
                        if (!TryParseName(fi.Name, out parsed)) continue; // not a phenocam file

                        // Safety copy before anything is renamed or moved.
                        //fi.CopyTo(Path.Combine(backupFolder, fi.Name), true); 
                        //also copy with year/month folders
                        CopyToCalcFolder(fi, parsed, backupFolder);

                        // Calc-folder copy happens BEFORE renaming and only for visible-light images.
                        if (!parsed.IsMeta && !parsed.IsIr && parsed.Date <= yesterday)
                            CopyToCalcFolder(fi, parsed, calcfolder);

                        // Old camera naming -> current naming (done regardless of date).
                        if (parsed.IsOld &&  parsed.Date <= yesterday)
                        {
                            fi.MoveTo(Path.Combine(dir, fi.Name.Replace(camera.oldName, camera.name)));
                            if (!TryParseName(fi.Name, out parsed) || parsed.IsOld) continue; // unexpected name
                        }

                        // Only complete days are zipped.
                        if (parsed.Date > yesterday) continue;

                        // Outside the 06:31-20:29 window: keep it out of the ZIP. It is moved aside (not deleted) so it
                        // is not picked up again on every run; it was already backed up above.
                        if (parsed.TimeOfDay < UploadWindowStart || parsed.TimeOfDay > UploadWindowEnd)
                        {
                            MoveOverwrite(fi.FullName, Path.Combine(excludedFolder, fi.Name));
                            continue;
                        }

                        string target = Path.Combine(dir, BuildArchiveName(parsed));
                        System.IO.File.Delete(target);
                        fi.MoveTo(target);

                        string key = parsed.Site + "|" + parsed.Suffix + "|" +
                                     parsed.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                        Zipfile zip;
                        if (!zips.TryGetValue(key, out zip)) // O(1) instead of scanning a list 2-3x per file
                        {
                            zip = new Zipfile
                            {
                                siteCode = parsed.Site,
                                suffix = parsed.Suffix,
                                date = parsed.Date,
                                dayfiles = new List<string>(),
                                upload = new PhenoUpload
                                {
                                    phenoCameraID = camera.ID,
                                    dateProblem = parsed.Date < camera.lastDate, // older than what was already sent
                                    cameraType = camera.type
                                }
                            };
                            zips.Add(key, zip);
                        }
                        zip.dayfiles.Add(target);

                        // Track the newest day seen for this camera.
                        if (camera.newDate == null || parsed.Date > camera.newDate)
                            camera.newDate = parsed.Date;
                    }
                    catch (Exception e)
                    {
                        Log(log, "Error:" + e.Message);
                    }
                }
            }
            return zips;
        }

        /// <summary>Copies an image to calcfolder\{camera}\{yyyy}\{MM}\, overwriting an existing copy.</summary>
        private static void CopyToCalcFolder(FileInfo fi, PhenoFileName parsed, string calcfolder)
        {
            string targetDir = Path.Combine(calcfolder, parsed.CameraId,
                parsed.Date.Year.ToString(CultureInfo.InvariantCulture),
                parsed.Date.Month.ToString("00", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(targetDir); // creates the whole chain in one call
            fi.CopyTo(Path.Combine(targetDir, fi.Name), true);
        }

        #endregion

        #region Step 2: zip

        /// <summary>
        /// Registers the upload in the database, writes the ZIP and only then removes the source files,
        /// so a failure while zipping no longer loses images.
        /// </summary>
        private static void CreateZip(Zipfile item, string zipFolder)
        {
            item.fileName = item.siteCode + "_PHEN_" + item.date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) +
                            "_" + item.suffix + ".zip";
            item.fullName = Path.Combine(zipFolder, item.fileName);
            item.upload.name = item.fileName;
            item.upload.status = phenoUploadStatus.start;
            item.upload.ID = PhenoDA.insertPhenoUpload(item.upload);

            System.IO.File.Delete(item.fullName); // replace a leftover ZIP from an earlier run

            using (ZipArchive archive = ZipFile.Open(item.fullName, ZipArchiveMode.Create))
            {
                foreach (string path in item.dayfiles)
                    archive.CreateEntryFromFile(path, Path.GetFileName(path));
            }
            foreach (string path in item.dayfiles)
                System.IO.File.Delete(path);

            item.upload.status = phenoUploadStatus.zipCreated;
            PhenoDA.savePhenoUploadStatusAndCount(item.upload.ID, (int)item.upload.status, item.dayfiles.Count);
        }

        #endregion

        #region Step 3: upload to ICOS

        /// <summary>
        /// Full upload of one ZIP: SHA-256 -> metadata package -> send metadata -> send ZIP -> verify.
        /// The database status is updated after every stage so a crash shows exactly where it stopped.
        /// On success the ZIP moves to \uploaded, on a failed verification to \failed.
        /// </summary>
        private void UploadZip(Zipfile item, string phenofolder, string metaTemplate, SHA256 sha, StreamWriter log)
        {
            string zipFolder = Path.Combine(phenofolder, "ZIP");
            string curl = Path.Combine(phenofolder, "curl.exe");

            // --- hash ---
            item.hash = ComputeSha256(item.fullName, sha);
            item.upload.hash = item.hash;
            item.upload.status = phenoUploadStatus.hash;
            PhenoDA.savePhenoUploadHash(item.upload);
            Log(log, item.fileName + " - " + item.hash);

            // --- metadata package ---
            string metaFileName = Path.Combine(zipFolder,
                "metaPackage_" + Path.GetFileNameWithoutExtension(item.fileName) + ".json");
            System.IO.File.WriteAllText(metaFileName, BuildMetaPackage(metaTemplate, item));
            item.upload.status = phenoUploadStatus.metaCreated;
            PhenoDA.savePhenoUploadStatus(item.upload.ID, (int)item.upload.status);

            // --- send metadata; ICOS answers with the URL of the data object ---
            Log(log, "start send metapackage");
            // NOTE: "/C" is kept from the original call; it looks like a leftover from cmd.exe usage.
            var metaProc = new ProcessStartInfo(curl,
                " /C -k --cookie cookies.txt -H \"Content-Type: application/json\" -X POST -d @\"" +
                metaFileName + "\" " + IcosMetaUploadUrl);
            string reply = RunProcess(metaProc, phenofolder, log, true);
            Log(log, "reply: " + reply);

            MoveOverwrite(metaFileName, Path.Combine(phenofolder, "uploaded", Path.GetFileName(metaFileName)));
            Log(log, "end send metapackage");
            item.upload.status = phenoUploadStatus.metaSent;
            PhenoDA.savePhenoUploadStatus(item.upload.ID, (int)item.upload.status);

            // --- send the ZIP itself (chunked) ---
            Log(log, "start send " + item.fileName);
            var zipProc = new ProcessStartInfo(curl,
                "-k -v --cookie cookies.txt -H \"Transfer-Encoding: chunked\" --upload-file \"" +
                item.fullName + "\" " + IcosDataUploadUrl + item.hash);
            string sent = RunProcess(zipProc, phenofolder, log, true);
            Log(log, "end send " + item.fileName);

            item.upload.status = phenoUploadStatus.startSent;
            PhenoDA.savePhenoUploadStatus(item.upload.ID, (int)item.upload.status);

            // --- verify: the metadata landing page must show the object ---
            string metaUrl = reply.Replace("data", "meta"); // computed once (was 6x)
            //WaitForFile(item.fullName, maxWaitInSec: 180);   // wait until curl released the file
            //string page = new HtmlWeb().Load(metaUrl).ParsedText;
            //bool ok = page != null && page.Contains("btn-warning") && page.Contains(metaUrl);
            bool ok = false;
            string toBeSearched = "https://data.icos-cp.eu/objects/";
            int ix = reply.IndexOf(toBeSearched);
            if (ix != -1)
            {
                string code = reply.Substring(ix + toBeSearched.Length);
                ok = sent.Contains(code);
            }

            string destFolder = ok ? "uploaded" : "failed";
            MoveOverwrite(item.fullName, Path.Combine(phenofolder, destFolder, item.fileName));

            Log(log, (ok ? "send success: " : "send failed: ") + metaUrl);
            item.upload.status = ok ? phenoUploadStatus.sentSucces : phenoUploadStatus.sentFailed;
            PhenoDA.savePhenoUploadStatus(item.upload.ID, (int)item.upload.status);
        }

        /// <summary>Streams the file through the (reused) SHA256 instance; returns upper-case hex.</summary>
        private static string ComputeSha256(string path, SHA256 sha)
        {
            using (FileStream stream = System.IO.File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        /// <summary>
        /// Fills the metadata template. The covered period is the capture day expressed in UTC:
        /// 23:00Z the day before until 23:00Z (22:00Z for GL-ZaF, which uses a different offset).
        /// Formatting is culture-invariant so ':' and digits can never be localised.
        /// </summary>
        private static string BuildMetaPackage(string template, Zipfile item)
        {
            // TODO: derive the UTC offset per site instead of special-casing GL-ZaF.
            string hour = item.siteCode.Contains("GL-ZaF") ? "22" : "23";
            Func<DateTime, string> utc = d =>
                d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T" + hour + ":00:00Z";

            // TODO: add camera serial as XXX6 (item.upload.cameraType is not the serial).
            return template
                .Replace("XXX1", item.fileName)
                .Replace("XXX2", item.hash)
                .Replace("XXX3", utc(item.date.AddDays(-1)))
                .Replace("XXX4", utc(item.date))
                .Replace("XXX5", item.siteCode);
        }

        #endregion


        // Extra usings needed: System.Diagnostics, System.IO, System.Threading, System.Threading.Tasks
        // Uses the Log(StreamWriter, string) helper from PhenocamUpload.cs.

        /// <summary>
        /// Runs an external program (here: curl.exe), waits for it to finish and optionally returns stdout.
        /// </summary>
        /// <param name="startInfo">Program and arguments. Redirection/window settings are applied here.</param>
        /// <param name="workingFolder">Working directory (curl reads cookies.txt from here).</param>
        /// <param name="log">Log writer for output and problems.</param>
        /// <param name="captureOutput">
        /// true  = log stdout/stderr and return stdout (needed for the metadata POST, whose reply is the object URL).
        /// false = output is still drained (so the process can never block on a full pipe) but not returned;
        ///         stderr is logged only when the exit code is non-zero (curl -v is very chatty).
        /// </param>
        /// <param name="timeoutSeconds">
        /// Maximum run time. On timeout the process is killed and a <see cref="TimeoutException"/> is thrown.
        /// The default is generous because large ZIP uploads can take a while; tune it to your slowest upload.
        /// </param>
        /// <param name="throwOnNonZeroExit">
        /// When true a non-zero exit code throws. Left off by default because the metadata call passes a stray
        /// "/C" argument that curl may treat as a second (invalid) URL and report as an error even though the POST worked.
        /// Remove that argument, then switch this on.
        /// </param>
        /// <param name="standardInput">
        /// Optional text written to the process's stdin (then closed). Use it for secrets so they never show up
        /// on the command line, where every user on the machine can read them. Keep it ASCII (e.g. URL-encoded):
        /// the console code page decides how stdin is encoded.
        /// </param>
        /// <returns>stdout when <paramref name="captureOutput"/> is true; otherwise null.</returns>
        private static string RunProcess(ProcessStartInfo startInfo, string workingFolder, StreamWriter log,
            bool captureOutput, int timeoutSeconds = 1800, bool throwOnNonZeroExit = false, string standardInput = null)
        {
            startInfo.WorkingDirectory = workingFolder;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = standardInput != null;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            using (var proc = new Process { StartInfo = startInfo })
            {
                proc.Start();

                // Read both pipes WHILE the process runs. If nobody reads them, the OS pipe buffer (a few KB)
                // fills up, curl blocks on its next write and the upload stalls until the timeout.
                Task<string> stdOutTask = proc.StandardOutput.ReadToEndAsync();
                Task<string> stdErrTask = proc.StandardError.ReadToEndAsync();

                if (standardInput != null)
                {
                    proc.StandardInput.Write(standardInput);
                    proc.StandardInput.Close(); // curl waits for end-of-input before sending
                }

                if (!proc.WaitForExit(timeoutSeconds * 1000))
                {
                    try { proc.Kill(); }
                    catch (InvalidOperationException) { /* it exited between the check and the kill */ }
                    proc.WaitForExit();
                    throw new TimeoutException("Process did not finish within " + timeoutSeconds + "s and was killed: " +
                                               Path.GetFileName(startInfo.FileName));
                }

                // Exit has happened; the pipes are closed so these return immediately.
                string stdOut = stdOutTask.Result;
                string stdErr = stdErrTask.Result;
                int exitCode = proc.ExitCode;

                if (captureOutput)
                {
                    log.WriteLine(stdOut);
                    log.WriteLine(stdErr);
                }

                if (exitCode != 0)
                {
                    string message = Path.GetFileName(startInfo.FileName) + " exited with code " + exitCode;
                    if (!captureOutput) log.WriteLine(stdErr); // otherwise already logged above
                    Log(log, message);
                    if (throwOnNonZeroExit) throw new InvalidOperationException(message);
                }

                return captureOutput ? stdOut : null;
            }
        }

        private const string IcosLoginUrl = "https://cpauth.icos-cp.eu/password/login";

        /// <summary>
        /// Logs in to the ICOS Carbon Portal with curl and stores the session cookie in
        /// <c>cookies.txt</c> (in <paramref name="folder"/>), which the later metadata/data uploads send along.
        /// Throws when the login fails, so the caller never starts uploads that are guaranteed to be rejected.
        /// </summary>
        private static void LoginToIcos(StreamWriter log, string folder, string mail, string password)
        {
            string cookieFile = Path.Combine(folder, "cookies.txt");

            // A leftover cookie file from an earlier run must not make a failed login look successful.
            System.IO.File.Delete(cookieFile);

            // URL-encode both values: a password containing & + % = or similar would otherwise corrupt the form body.
            // The result is plain ASCII, so the stdin encoding is not an issue.
            string body = "mail=" + Uri.EscapeDataString(mail) + "&password=" + Uri.EscapeDataString(password);

            // -sS     silent (no progress meter) but still print errors
            // --fail  HTTP status >= 400 gives a non-zero exit code
            // @-      read the form body from stdin => the password is not visible in the process list
            // No "/C" (curl would treat it as a second URL) and no "-k" (keeps TLS certificate validation on).
            var startInfo = new ProcessStartInfo(Path.Combine(folder, "curl.exe"),
                "-sS --fail --cookie-jar cookies.txt --data @- " + IcosLoginUrl);

            RunProcess(startInfo, folder, log, captureOutput: false, throwOnNonZeroExit: true, standardInput: body);

            if (!HasCookie(cookieFile))
                throw new InvalidOperationException("ICOS login returned no session cookie (check CPmail/CPpassword).");

            Log(log, "ICOS login OK");
        }

        /// <summary>
        /// True when a curl cookie jar (Netscape format) contains at least one cookie. Comment lines start with '#',
        /// but curl writes HttpOnly cookies as "#HttpOnly_domain...", so those count as cookies too.
        /// </summary>
        private static bool HasCookie(string cookieFile)
        {
            return System.IO.File.Exists(cookieFile) &&
                   System.IO.File.ReadLines(cookieFile).Any(line =>
                       line.Length > 0 &&
                       (line[0] != '#' || line.StartsWith("#HttpOnly_", StringComparison.Ordinal)));
        }


        public ActionResult dailyPhenoReport(DateTime? datetime)
        {
            string date = "";
            if (datetime.HasValue)
                date = datetime.Value.ToString("yyyyMMdd");
            else
                date = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
            date = "%" + date + "%";

            List<PhenoReportItem> list = PhenoDA.getDailyPhenoReportItems(date);

            return View(list);
        }
    }
}