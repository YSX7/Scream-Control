using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IWshRuntimeLibrary;
using Shell32;
using System.Windows;
using System.Reflection;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace ScreamControl.View
{
    static class Startup
    {
        static Assembly curAssembly = Assembly.GetEntryAssembly();

        public static void SetAutostart(bool debugMode = false)
        {
            try
            {
                //if (!IsStartupItem(curAssembly))
                //{
                //    RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                //    key.SetValue(curAssembly.GetName().Name, curAssembly.Location);

                //    Trace.TraceInformation("Autostart Setted");
                //}
                //CheckAutostartEnabled(curAssembly.GetName().Name);

                string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string lnkPath = startUpFolderPath + "\\" +
                    curAssembly.GetName().Name + ".lnk";

                if (debugMode) Trace.TraceInformation("Startup assembly name: {0}", curAssembly.GetName().Name);

                if (!System.IO.File.Exists(lnkPath))
                {
                    WshShell wshShell = new WshShell();
                    IWshRuntimeLibrary.IWshShortcut shortcut;

                    // Create the shortcut
                    shortcut =
                      (IWshRuntimeLibrary.IWshShortcut)wshShell.CreateShortcut(lnkPath);
                    shortcut.TargetPath = curAssembly.Location;
                    shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    shortcut.Description = "Scream Control Launch";
                    shortcut.Save();
                }
            }
            catch(Exception e)
            {
                Trace.TraceError("Something happened at Autostart set: {0}", e);
            }

            CheckAutostartEnabled(debugMode);
         }

        public static void CheckAutostartEnabled(bool debugMode)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\StartupFolder", true);
                if (key != null)
                {
                    byte[] newValue = (byte[])key.GetValue(curAssembly.GetName().Name + ".lnk");
                    if (newValue[0] != 2)
                    {
                        newValue[0] = 2;
                        key.SetValue(curAssembly.GetName().Name + ".lnk", newValue);
                        Trace.TraceInformation("Autostart Enabled");
                    }
                }
                else
                {
                    Trace.TraceWarning("No registry key found");
                }
            }
            catch(Exception e)
            {
                Trace.TraceError("Something happened at Autostart enabling: {0}", e);
            }
        }

        //private static bool IsStartupItem(Assembly curAssembly)
        //{
        //    //RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        //    //if (rkApp.GetValue(curAssembly.GetName().Name) != null)
        //    //{
        //    //    if ((string)rkApp.GetValue(curAssembly.GetName().Name) == curAssembly.Location)
        //    //        return true;
        //    //}
        //    return false;
        //}
    }
}
