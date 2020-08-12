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
            var panel = new CommandPanel();
            panel.Start();

            var center = new DesktopStrategy();

            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
