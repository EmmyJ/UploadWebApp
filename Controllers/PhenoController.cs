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
        //   System.Globalization, System.Threading, System.Threading.Tasks, System.IO, System.IO.Compression, System.Linq,
        //   System.Security.Cryptography, System.Text.RegularExpressions, System.Web.Mvc
        //
        // Targets C# 6 / .NET Framework (no out-vars, no ValueTuples).
        // Assumes these members already exist in the controller / project:
        //   RunProcess, LoginToIcos, HasCookie are defined further down in this file
        //   PhenoDA, PhenoCamera, PhenoUpload, Zipfile, phenoCamStatus, phenoUploadStatus
        // ---------------------------------------------------------------------------

        #region Constants and parsing

        private const string IcosMetaUploadUrl = "https://meta.icos-cp.eu/upload";
        private const string IcosDataUploadUrl = "https://data.icos-cp.eu/objects/";

        // Only files captured inside this time-of-day window (from the file name, both ends inclusive) go into a ZIP.
        private static readonly TimeSpan UploadWindowStart = new TimeSpan(6, 45, 0);
        private static readonly TimeSpan UploadWindowEnd = new TimeSpan(20, 15, 0);

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

        /// <summary>
        /// Files that an earlier run already renamed to the archive name (BE-Maa_PHEN_20250430224602_C01.jpg) but never
        /// got into a ZIP because that run stopped in between. Without this they would sit in the camera folder forever.
        /// </summary>
        private static readonly Regex ArchivedFileRegex = new Regex(
            @"^(?<site>[A-Za-z]{2}-[A-Za-z0-9]{3})_PHEN_(?<ir>IR_)?(?<year>[0-9]{4})(?<month>[0-9]{2})(?<day>[0-9]{2})" +
            @"(?<time>[0-9]{6})_(?<new>C[0-9]{2})\.(?<ext>(?i:jpe?g)|meta)$",
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
            public bool IsArchived;    // already carries the archive name (left over from an interrupted run)
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
            bool archived = false;
            Match m = PhenoFileRegex.Match(fileName);
            if (!m.Success)
            {
                m = ArchivedFileRegex.Match(fileName);
                if (!m.Success) return false;
                archived = true;
            }

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
                IsArchived = archived,
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
            // Two overlapping runs work on the same files at the same time: source files vanish ("Could not find file")
            // and targets are locked ("being used by another process"). The mutex lets only one run proceed;
            // a second request (scheduler retry, browser refresh, long-running first run) is skipped.
            // No "Global\\" prefix: all IIS worker processes share one session, and Global\ needs extra privileges.
            using (var runLock = new Mutex(false, "PhenocamUpload_" + folder))
            {
                bool acquired;
                try { acquired = runLock.WaitOne(0); }
                catch (AbandonedMutexException) { acquired = true; } // a previous run died while holding it; we own it now

                if (!acquired)
                    return View("Another phenocam upload run is still in progress; this request was skipped.");

                try { return RunPhenocamUpload(folder); }
                finally { runLock.ReleaseMutex(); }
            }
        }

        /// <summary>The actual work of <see cref="CreateUploadPhenocamZips"/>, guarded against overlapping runs.</summary>
        private ActionResult RunPhenocamUpload(int folder)
        {
            try
            {
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
                    var runTimer = Stopwatch.StartNew(); // shows how long a run takes, e.g. versus the trigger interval

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
                            int totalEntries = CreateZip(item, Path.Combine(phenofolder, "ZIP"));
                            Log(log, item.fileName + ": " + item.dayfiles.Count + " files added, " + totalEntries + " in ZIP");
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

                    Log(log, "Finished in " + runTimer.Elapsed.ToString(@"hh\:mm\:ss"));
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
        ///      the upload window (UploadWindowStart-UploadWindowEnd) (files outside it are moved to \excluded).
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

                // One bad camera (e.g. two cameras sharing a name/oldName, or an unreadable folder) is logged and
                // skipped; the other cameras are still processed.
                try
                {

                    // Resolve the camera once per folder (it was looked up up to ~10x per file before).
                    PhenoCamera camera = activeCameras.SingleOrDefault(c => c.name == dirName || c.oldName == dirName);
                    if (camera == null) continue; // logs, backup, ZIP, ... or inactive cameras

                    int found = 0, tooRecent = 0, excluded = 0, added = 0, resumed = 0, errors = 0;

                    foreach (string file in Directory.GetFiles(dir))
                    {
                        try
                        {
                            var fi = new FileInfo(file);

                            PhenoFileName parsed;
                            if (!TryParseName(fi.Name, out parsed)) continue; // not a phenocam file

                            found++;

                            // A leftover with the archive name was already backed up/copied/renamed by an earlier run.
                            if (parsed.IsArchived)
                            {
                                resumed++;
                            }
                            else
                            {
                                // Safety copy (camera\year\month layout) before anything is renamed or moved.
                                // Skipped when the backup already holds a file of the same size, so reruns over a
                                // backlog or today's incomplete day don't copy everything again.
                                CopyToDatedFolder(fi, parsed, backupFolder, skipIfSameSize: true);

                                // Calc-folder copy happens BEFORE renaming and only for visible-light images.
                                if (!parsed.IsMeta && !parsed.IsIr && parsed.Date <= yesterday)
                                    CopyToDatedFolder(fi, parsed, calcfolder, skipIfSameSize: false);

                                // Old camera naming -> current naming (done regardless of date).
                                if (parsed.IsOld && parsed.Date <= yesterday)
                                {
                                    fi.MoveTo(Path.Combine(dir, fi.Name.Replace(camera.oldName, camera.name)));
                                    if (!TryParseName(fi.Name, out parsed) || parsed.IsOld) continue; // unexpected name
                                }
                            }

                            // Only complete days are zipped.
                            if (parsed.Date > yesterday) { tooRecent++; continue; }

                            // Outside the upload window (UploadWindowStart-UploadWindowEnd): keep it out of the ZIP. It is moved aside (not deleted) so it
                            // is not picked up again on every run; it was already backed up above.
                            if (parsed.TimeOfDay < UploadWindowStart || parsed.TimeOfDay > UploadWindowEnd)
                            {
                                MoveOverwrite(fi.FullName, Path.Combine(excludedFolder, fi.Name));
                                excluded++;
                                continue;
                            }

                            string target = fi.FullName;
                            if (!parsed.IsArchived)
                            {
                                target = Path.Combine(dir, BuildArchiveName(parsed));
                                System.IO.File.Delete(target);
                                fi.MoveTo(target);
                            }

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
                            added++;

                            // Track the newest day seen for this camera.
                            if (camera.newDate == null || parsed.Date > camera.newDate)
                                camera.newDate = parsed.Date;
                        }
                        catch (Exception e)
                        {
                            errors++;
                            Log(log, "Error on " + Path.GetFileName(file) + ": " + e.Message); // the file name was missing before
                        }
                    }

                    // One line per camera: found = added + too recent + outside window + errors (+ skipped unexpected names).
                    Log(log, dirName + ": " + found + " files found, " + added + " added to ZIPs (" + resumed +
                             " left over from an earlier run), " + tooRecent + " too recent, " + excluded +
                             " outside " + UploadWindowStart + "-" + UploadWindowEnd + ", " + errors + " errors");
                }
                catch (Exception e)
                {
                    Log(log, "Error in camera folder " + dirName + ": " + e.Message);
                }
            }
            return zips;
        }

        /// <summary>
        /// Copies a file to root\{camera}\{yyyy}\{MM}\ (used for both the backup and the calc folder).
        /// </summary>
        /// <param name="skipIfSameSize">
        /// true  = do nothing when the target already exists with the same length (one cheap stat call instead of
        ///         a copy over the network share). A file that was rewritten with exactly the same size would not be
        ///         re-copied; compare LastWriteTimeUtc as well if that can happen.
        /// false = always copy, overwriting an existing file.
        /// </param>
        private static void CopyToDatedFolder(FileInfo fi, PhenoFileName parsed, string root, bool skipIfSameSize)
        {
            string targetDir = Path.Combine(root, parsed.CameraId,
                parsed.Date.Year.ToString(CultureInfo.InvariantCulture),
                parsed.Date.Month.ToString("00", CultureInfo.InvariantCulture));
            string targetFile = Path.Combine(targetDir, fi.Name);

            if (skipIfSameSize)
            {
                var existing = new FileInfo(targetFile);
                if (existing.Exists && existing.Length == fi.Length) return; // already there; the folder exists too
            }

            Directory.CreateDirectory(targetDir); // creates the whole chain in one call
            fi.CopyTo(targetFile, true);
        }

        #endregion

        #region Step 2: zip

        /// <summary>
        /// Registers the upload in the database, writes the ZIP and only then removes the source files,
        /// so a failure while zipping no longer loses images.
        /// </summary>
        private static int CreateZip(Zipfile item, string zipFolder)
        {
            item.fileName = item.siteCode + "_PHEN_" + item.date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) +
                            "_" + item.suffix + ".zip";
            item.fullName = Path.Combine(zipFolder, item.fileName);
            item.upload.name = item.fileName;
            item.upload.status = phenoUploadStatus.start;
            item.upload.ID = PhenoDA.insertPhenoUpload(item.upload);

            // A ZIP with this name may still be in the ZIP folder (never confirmed as uploaded, or late files arriving
            // for a day that was already zipped). Deleting it, as the old code did, silently threw its images away and left
            // a ZIP with only the newest files. So add to it instead.
            bool append = System.IO.File.Exists(item.fullName);

            using (ZipArchive archive = ZipFile.Open(item.fullName, append ? ZipArchiveMode.Update : ZipArchiveMode.Create))
            {
                foreach (string path in item.dayfiles)
                {
                    string entryName = Path.GetFileName(path);
                    if (append)
                    {
                        ZipArchiveEntry existing = archive.GetEntry(entryName);
                        if (existing != null) existing.Delete(); // newer copy wins
                    }
                    archive.CreateEntryFromFile(path, entryName);
                }
            }

            // Verify before deleting anything: every file must really be inside the finished ZIP.
            int totalEntries;
            using (ZipArchive check = ZipFile.OpenRead(item.fullName))
            {
                foreach (string path in item.dayfiles)
                    if (check.GetEntry(Path.GetFileName(path)) == null)
                        throw new InvalidOperationException("Missing in " + item.fileName + ": " + Path.GetFileName(path));
                totalEntries = check.Entries.Count;
            }

            foreach (string path in item.dayfiles)
                System.IO.File.Delete(path);

            item.upload.status = phenoUploadStatus.zipCreated;
            PhenoDA.savePhenoUploadStatusAndCount(item.upload.ID, (int)item.upload.status, totalEntries);
            return totalEntries;
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

            // --- verify: the object id from the metadata reply must show up in the upload response ---
            string metaUrl = reply.Replace("data", "meta"); // computed once (was 6x)
            bool ok = false;
            string toBeSearched = "https://data.icos-cp.eu/objects/";
            int ix = reply.IndexOf(toBeSearched, StringComparison.Ordinal);
            if (ix != -1)
            {
                // Trim: curl output usually ends with a newline, which would make the Contains check fail.
                string code = reply.Substring(ix + toBeSearched.Length).Trim();
                ok = code.Length > 0 && sent.Contains(code); // an empty code would match anything
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

        // Extra usings needed: System.Diagnostics, System.IO, System.Threading.Tasks
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


        // ---------------------------------------------------------------------------
        // Daily report by mail (same table as the dailyPhenoReport page), built with MimeKit and sent with MailKit.
        //
        // ---------------------------------------------------------------------------

        private const string IcosObjectPageUrl = "https://meta.icos-cp.eu/objects/";

        /// <summary>
        /// Mails the daily phenocam report for the given day (default: yesterday), laid out like dailyPhenoReport:
        /// Camera | Filename | Expected # Files | Actual # Files | Link, with rows in red when no files arrived or
        /// fewer than 90% of the expected number. Call it from the scheduler after the upload run, e.g.
        /// /sendDailyPhenoReport?datetime=2026-10-29
        /// </summary>
        public ActionResult sendDailyPhenoReport(DateTime? datetime)
        {
            try
            {
                DateTime date = (datetime ?? DateTime.Now.AddDays(-1)).Date;
                List<PhenoReportItem> list = PhenoDA.getDailyPhenoReportItems(
                    "%" + date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "%");

                string to = ConfigurationManager.AppSettings["phenoReportTo"];
                string from = ConfigurationManager.AppSettings["smtpFrom"];
                if (string.IsNullOrWhiteSpace(to))
                    throw new InvalidOperationException("AppSetting 'phenoReportTo' is not configured.");
                if (string.IsNullOrWhiteSpace(from))
                    throw new InvalidOperationException("AppSetting 'smtpFrom' is not configured.");

                // Absolute link to the web version of the same report.
                string reportUrl = Url.Action("dailyPhenoReport", null,
                    new { datetime = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) }, Request.Url.Scheme);

                int problems = list.Count(IsBelowExpected);
                string subject = "Daily Phenocam Report: " + date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) +
                                 (problems > 0 ? " - " + problems + " below expected" : "");

                SendHtmlMail(from, to, subject, BuildDailyReportHtml(list, date, reportUrl), BuildDailyReportText(list, date, reportUrl));

                return Content("Daily phenocam report for " + date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) +
                               " sent to " + to);
            }
            catch (Exception e)
            {
                // 500 so a scheduler or monitor notices that the mail did not go out.
                return new HttpStatusCodeResult(500, e.Message);
            }
        }

        /// <summary>
        /// Builds a MimeKit message (HTML body with a plain-text alternative) and sends it through MailKit,
        /// using the phenoSmtp* appSettings. Server certificates are validated normally.
        /// </summary>
        private static void SendHtmlMail(string from, string to, string subject, string html, string text)
        {
            string host = ConfigurationManager.AppSettings["smtpHost"];
            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("AppSetting 'smtpHost' is not configured.");

            int port;
            if (!int.TryParse(ConfigurationManager.AppSettings["smtpPort"], out port)) port = 25;

            string securityText = ConfigurationManager.AppSettings["smtpSecurity"];
            MailKit.Security.SecureSocketOptions security = string.IsNullOrWhiteSpace(securityText)
                ? MailKit.Security.SecureSocketOptions.Auto
                : (MailKit.Security.SecureSocketOptions)Enum.Parse(typeof(MailKit.Security.SecureSocketOptions), securityText, true);

            var message = new MimeKit.MimeMessage();
            message.From.Add(MimeKit.MailboxAddress.Parse(from));
            foreach (string address in to.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                message.To.Add(MimeKit.MailboxAddress.Parse(address.Trim()));
            message.Subject = subject;
            message.Body = new MimeKit.BodyBuilder { HtmlBody = html, TextBody = text }.ToMessageBody();

            using (var smtp = new MailKit.Net.Smtp.SmtpClient())
            {
                smtp.Connect(host, port, security);

                string user = ConfigurationManager.AppSettings["smtpUser"];
                if (!string.IsNullOrEmpty(user))
                    smtp.Authenticate(user, ConfigurationManager.AppSettings["smtpPassword"]);

                smtp.Send(message);
                smtp.Disconnect(true);
            }
        }

        /// <summary>Plain-text version of the report for mail clients that don't show HTML.</summary>
        private static string BuildDailyReportText(IEnumerable<PhenoReportItem> items, DateTime date, string reportUrl)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Daily Phenocam Report: " + date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(reportUrl)) sb.AppendLine(reportUrl);
            sb.AppendLine();

            int rows = 0;
            foreach (PhenoReportItem item in items)
            {
                rows++;
                sb.AppendLine((IsBelowExpected(item) ? "[!] " : "    ") + item.cameraName + " | " + item.fileName +
                              " | expected " + Convert.ToString(item.expFileCount, CultureInfo.InvariantCulture) +
                              " | actual " + Convert.ToString(item.actFileCount, CultureInfo.InvariantCulture) +
                              (string.IsNullOrEmpty(item.hash) ? "" : " | " + IcosObjectPageUrl + item.hash));
            }
            if (rows == 0) sb.AppendLine("No uploads found for this date.");
            else sb.AppendLine().AppendLine("[!] = no files, or fewer than 90% of the expected number.");
            return sb.ToString();
        }

        /// <summary>
        /// Same rule as the web report: red when no file count is known or it is below 90% of the expected count.
        /// </summary>
        private static bool IsBelowExpected(PhenoReportItem item)
        {
            return item.actFileCount == null || item.actFileCount < item.expFileCount * 0.9;
        }

        /// <summary>
        /// Builds the e-mail body. Mail clients ignore style sheets and classes, so everything is inline-styled,
        /// and every value is HTML-encoded.
        /// </summary>
        private static string BuildDailyReportHtml(IEnumerable<PhenoReportItem> items, DateTime date, string reportUrl)
        {
            Func<object, string> enc = o => System.Web.HttpUtility.HtmlEncode(Convert.ToString(o, CultureInfo.InvariantCulture));

            const string th = "text-align:left;padding:4px 8px;border-bottom:2px solid #999;";
            const string td = "padding:4px 8px;border-bottom:1px solid #ddd;";

            var sb = new System.Text.StringBuilder();
            sb.Append("<html><body style=\"font-family:Arial,Helvetica,sans-serif;font-size:14px;\">");
            sb.Append("<h2>Daily Phenocam Report: " + date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) + "</h2>");
            if (!string.IsNullOrEmpty(reportUrl))
                sb.Append("<p><a href=\"" + enc(reportUrl) + "\">Open this report in the browser</a></p>");

            sb.Append("<table style=\"border-collapse:collapse;width:100%;max-width:1170px;\">");
            sb.Append("<tr><th style=\"" + th + "\">Camera</th><th style=\"" + th + "\">Filename</th>" +
                      "<th style=\"" + th + "\">Expected # Files</th><th style=\"" + th + "\">Actual # Files</th>" +
                      "<th style=\"" + th + "\">Link</th></tr>");

            int rows = 0;
            foreach (PhenoReportItem item in items)
            {
                rows++;
                sb.Append(IsBelowExpected(item) ? "<tr style=\"color:red;\">" : "<tr>");
                sb.Append("<td style=\"" + td + "\">" + enc(item.cameraName) + "</td>");
                sb.Append("<td style=\"" + td + "\">" + enc(item.fileName) + "</td>");
                sb.Append("<td style=\"" + td + "\">" + enc(item.expFileCount) + "</td>");
                sb.Append("<td style=\"" + td + "\">" + enc(item.actFileCount) + "</td>");
                sb.Append("<td style=\"" + td + "\">");
                if (!string.IsNullOrEmpty(item.hash))
                    sb.Append("<a href=\"" + IcosObjectPageUrl + enc(item.hash) + "\">View on Carbon Portal</a>");
                sb.Append("</td></tr>");
            }
            sb.Append("</table>");

            if (rows == 0)
                sb.Append("<p>No uploads found for this date.</p>");

            sb.Append("</body></html>");
            return sb.ToString();
        }


    }

}