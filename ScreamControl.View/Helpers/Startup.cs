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
        private static Assembly curAssembly = Assembly.GetEntryAssembly();
        private const string STARTUP_ADDED = "mp_Added";
        private const string STARTUP_READDED = "mp_Readded";

        public static void SetAutostart(bool debugMode, bool showPopup)
        {
            try
            {
                //if (!IsStartupItem(curAssembly))
                //{
                //    RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\StartupFolder", true);

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

                    if (showPopup)
                    {
                        InfoPopupView infoPopup = new InfoPopupView();
                        infoPopup.Owner = Application.Current.MainWindow;
                        infoPopup.textBlock.Text = Application.Current.FindResource(STARTUP_ADDED).ToString();
                        infoPopup.Show();
                    }

                    Trace.TraceInformation("App added to autostart");
                }
            }
            catch(Exception e)
            {
                Trace.TraceError("Something happened at Autostart set: {0}", e);
            }

            CheckAutostartEnabled(debugMode, showPopup);
         }

        public static void CheckAutostartEnabled(bool debugMode, bool showPopup)
        {
            try
            {
                //string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                //string lnkPath = startUpFolderPath + "\\" +
                //    curAssembly.GetName().Name + ".lnk";
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\StartupFolder", true);
                if (key != null)
                {
                    byte[] newValue = (byte[])key.GetValue(curAssembly.GetName().Name + ".lnk");
                    if (newValue != null)
                    {
                        if (newValue[0] != 2)
                        {
                            newValue[0] = 2;
                            key.SetValue(curAssembly.GetName().Name + ".lnk", newValue);

                            if (showPopup)
                            {
                                InfoPopupView infoPopup = new InfoPopupView();
                                infoPopup.Owner = Application.Current.MainWindow;
                                infoPopup.textBlock.Text = Application.Current.FindResource(STARTUP_READDED).ToString();
                                infoPopup.Show();
                            }

                            Trace.TraceInformation("Autostart Reenabled");
                            return;
                        }
                    }
                }
                Trace.TraceWarning("No registry key found");
            }
            catch(Exception e)
            {
                Trace.TraceError("Something happened at Autostart enabling: {0}", e);
            }
        }

        private static bool IsStartupItem(Assembly curAssembly)
        {
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\StartupFolder", true);

            if (rkApp.GetValue(curAssembly.GetName().Name) != null)
            {
                if ((string)rkApp.GetValue(curAssembly.GetName().Name) == curAssembly.Location)
                    return true;
            }
            return false;
        }
    }
}
