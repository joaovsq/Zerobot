using System;
using System.Collections.Generic;
using System.Text;

namespace Zerobot.CommandCenter
{

    /// <summary>
    /// Controls the Zerobot.CommandCenter integration.
    /// 
    /// If we are running on a Desktop platform we use IPC.
    /// If the CommandCenter is running on other platforms (android, ios, etc) then we have to use the network
    /// </summary>
    interface ICommandCenterStrategy
    {
        void Run();
    }

    /// <summary>
    /// 
    /// </summary>
    class CommandCenterContext
    {
        private readonly ICommandCenterStrategy strategy;

        public CommandCenterContext(ICommandCenterStrategy strategy)
        {
            this.strategy = strategy;
        }

        public void Start()
        {
            strategy.Run();
        }
    }


}
