using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Zerobot.CommandCenter
{
    /// <summary>
    /// Controls the Zerobot.CommandCenter integration.
    /// 
    /// If we are running on a Desktop platform we use IPC
    /// </summary>
    public class CommandCenterAdmin
    {
        /// the active IPC command panel process
        private readonly Process ipcProcess = new Process();

        public CommandCenterAdmin()
        {
            // TODO: implement and verify the GUI panel mode. For now we just assume that it is in CONSOLE mode.
            // TODO: mobile implementation

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ipcProcess.StartInfo.FileName = "Zerobot.CommandCenter.exe";
                ipcProcess.StartInfo.Arguments = "";
                ipcProcess.StartInfo.RedirectStandardOutput = true;
                ipcProcess.StartInfo.RedirectStandardError = true;
                ipcProcess.Start();

                ipcProcess.WaitForExit();
            }
            else
            {
                Environment.FailFast("Zerobot only supports Desktop platforms... wait for it....");
            }
        }


        void Integrate()
        {



        }
    }
}
