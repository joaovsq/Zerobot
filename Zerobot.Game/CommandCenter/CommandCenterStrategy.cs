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

            ipcProcess.StartInfo.FileName = "Zerobot.CommandCenter.exe";
            ipcProcess.StartInfo.Arguments = "";
            ipcProcess.StartInfo.RedirectStandardOutput = true;
            ipcProcess.StartInfo.RedirectStandardError = true;
        }

        public void Run()
        {
            log.Info("Running DesktopStrategy...");

            using (AnonymousPipeServerStream pipeServer =
                new AnonymousPipeServerStream(PipeDirection.Out,
                HandleInheritability.Inheritable))
            {
                Console.WriteLine("[SERVER] Current TransmissionMode: {0}.",
                    pipeServer.TransmissionMode);

                ipcProcess.StartInfo.Arguments = pipeServer.GetClientHandleAsString();
                ipcProcess.Start();

                pipeServer.DisposeLocalCopyOfClientHandle();

                try
                {
                    // Read user input and send that to the client process.
                    using (StreamWriter sw = new StreamWriter(pipeServer))
                    {
                        sw.AutoFlush = true;
                        // Send a 'sync message' and wait for client to receive it.
                        sw.WriteLine("SYNC");
                        pipeServer.WaitForPipeDrain();
                        
                        // Send the console input to the client process.
                        Console.Write("[SERVER] Enter text: ");
                        sw.WriteLine(Console.ReadLine());
                    }
                }
                // Catch the IOException that is raised if the pipe is broken
                // or disconnected.
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
