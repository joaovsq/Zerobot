using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;

namespace Zerobot.CommandCenter
{

    class DesktopStrategy : ICommandCenterStrategy
    {
        /// the active IPC command panel process
        private readonly Process ipcProcess = new Process();

        public DesktopStrategy()
        {
            ipcProcess.StartInfo.FileName = "Zerobot.CommandCenter.exe";
            ipcProcess.StartInfo.Arguments = "";
            ipcProcess.StartInfo.RedirectStandardOutput = true;
            ipcProcess.StartInfo.RedirectStandardError = true;
            ipcProcess.Start();
        }

        public void Run()
        {
            using (AnonymousPipeServerStream pipeServer =
                new AnonymousPipeServerStream(PipeDirection.Out,
                HandleInheritability.Inheritable))
            {

            }
        }
    }

    /// <summary>
    /// Implements the CommandCenter strategy for mobile devices.
    /// 
    /// This strategy requires the use of the local network and ip discovery.
    /// </summary>
    class MobileStrategy : ICommandCenterStrategy
    {
        public void Run()
        {
            // TODO: This implementation may require some xamarin components, and maybe we should move this to another package.
            throw new NotImplementedException();
        }

    }
}
