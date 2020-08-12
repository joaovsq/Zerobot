using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Zerobot.CommandCenter
{
    /// <summary>
    /// 
    /// </summary>
    public class CommandCenterAdmin
    {
        private ICommandCenterStrategy currentStrategy;

        public CommandCenterAdmin()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                currentStrategy = new DesktopStrategy();
            }
            else
            {
                Environment.FailFast("Zerobot only supports Desktop platforms... wait for it....");
            }

        }

        public void StartCommandCenter()
        {
            var context = new CommandCenterContext(currentStrategy);
            context.Start();
        }
    }
}
