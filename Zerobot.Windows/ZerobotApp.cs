using Stride.Engine;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Zerobot.CommandCenter;

namespace Zerobot
{
    class ZerobotApp
    {
        static void Main(string[] args)
        {
            var context = new CommandCenterContext(new DesktopStrategy());
            context.Start();

            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
