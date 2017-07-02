using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;
using Octokit;
using System.Reflection;

namespace ScreamControl
{
    public static class CheckUpdates
    {
        public static async Task<bool> Check(ExtendedVersion currentVersion, bool isUpdaterUpdated, bool isStealthMode, bool isDebugMode = false)
        {
            if (isUpdaterUpdated)
            {
                Trace.TraceInformation("Update updater");
                ZipArchive za = ZipFile.OpenRead("temp.zip");
                var updaterFiles = za.Entries.Where(element => element.Name.ToLower().Contains("updater"));
                foreach (ZipArchiveEntry file in updaterFiles)
                {
                    if (file.Name == "")
                    {// Assuming Empty for Directory
                        Directory.CreateDirectory(Path.GetDirectoryName(file.FullName));
                        continue;
                    }
                    file.ExtractToFile(file.FullName, true);
                }
                za.Dispose();
                File.Delete("temp.zip");
            }
            else
                try
                {
                    Trace.TraceInformation("Updates check");
                    var silentArgument = isStealthMode ? " s" : "";
                    var updatedUpdaterArgument = isUpdaterUpdated ? " u" : "";
                    var debugArgument = isDebugMode ? " d" : "";

                    var client = new GitHubClient(new ProductHeaderValue("Scream-Control"));
                    var getReleaseTask = client.Repository.Release.GetLatest("YSXrus", "Scream-Control");
                    if(!getReleaseTask.Wait(new TimeSpan(0, 0, 15)))
                    {
                        Trace.TraceInformation("Timeout getting updates.");
                        return true;
                    }
                    var latest = getReleaseTask.Result;
                    var gitVersion = new ExtendedVersion(latest.TagName);
                    bool updateAvailable = gitVersion > currentVersion;
                    Trace.TraceInformation("Updates available: {0}. Current: {1}. Git latest: {2}", updateAvailable.ToString(), currentVersion, gitVersion);
                    string appType = ((AssemblyTitleAttribute)Assembly.GetEntryAssembly().GetCustomAttribute(typeof(AssemblyTitleAttribute))).Title.Split(' ')[1];
                    string updateUrl = latest.Assets.First(element => element.Name.ToLower().Contains("update." + appType.ToLower())).BrowserDownloadUrl;
                    if ((updateAvailable || isDebugMode) && File.Exists("Updater.exe"))
                    {
                        Trace.TraceInformation("Go for updates");
                        string commandLine = updateUrl + " " + System.AppDomain.CurrentDomain.FriendlyName + silentArgument + updatedUpdaterArgument + debugArgument;
                        if (isDebugMode) Trace.TraceInformation("Command line: " + commandLine);
                        System.Diagnostics.Process.Start("Updater.exe", commandLine);
                        return false;
                    }
                }
                catch (Octokit.NotFoundException ex)
                {
                    Trace.TraceWarning("No updates found: {0}", ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    Trace.TraceWarning("No updates found: {0}", ex);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Something happend when checking: {0}", ex);
                    //no updates
                }
            return true;
        }
    }
}
