using Stride.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;

namespace Zerobot.CommandCenter
{

    public class DesktopStrategy : ICommandCenterStrategy
    {
        /// the active IPC command panel process
        private readonly Process ipcProcess = new Process();
        private readonly Logger log = GlobalLogger.GetLogger(typeof(DesktopStrategy).Name);


        public DesktopStrategy()
        {
            log.Debug("Starting the CommandCenter Desktop strategy...");

            string fileName = "Zerobot.CommandCenter";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileName += ".exe";
            }

            ipcProcess.StartInfo.FileName = fileName;
        }

        public void Run()
        {
            log.Info("Running DesktopStrategy...");

            using (AnonymousPipeServerStream pipeServer =
                new AnonymousPipeServerStream(PipeDirection.In,
                HandleInheritability.Inheritable))
            {
                ipcProcess.StartInfo.Arguments = pipeServer.GetClientHandleAsString();
                ipcProcess.Start();

                pipeServer.DisposeLocalCopyOfClientHandle();
                try
                {
                    using (StreamReader reader = new StreamReader(pipeServer))
                    {
                        var rawCommand = reader.ReadLine();
                        Console.WriteLine($"Raw command: {rawCommand}");
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("[SERVER] Error: {0}", e.Message);
                }
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
