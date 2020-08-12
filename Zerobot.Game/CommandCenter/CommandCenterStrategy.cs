using Stride.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Zerobot.Player;

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

                        while (pipeServer.IsConnected)
                        {
                            //reader.ReadLineAsync().ContinueWith((rawCommand) =>
                            //{
                            //    PlayerController.RemoteCommandQueue.Enqueue(rawCommand.Result);
                            //});

                            PlayerController.RemoteCommandQueue.Enqueue(reader.ReadLine());
                        }
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine($"[CommandCenter SERVER] Error: {e.Message}");
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
