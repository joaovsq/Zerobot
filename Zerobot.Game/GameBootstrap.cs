using Stride.Engine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Zerobot.CommandCenter;

namespace Zerobot
{
    /// <summary>
    /// The game bootstrap entry point, to avoid code duplication in different platforms
    /// </summary>
    public sealed class GameBootstrap
    {
        private GameBootstrap() { }

        public static void Run()
        {
            var commandCenterThread = new Thread(StartCommandCenter);
            commandCenterThread.Start();

            using (var game = new Game())
            {
                game.Run();
            }
        }

        static void StartCommandCenter()
        {
            var context = new CommandCenterContext(new DesktopStrategy());
            context.Start();
        }
    }
}
