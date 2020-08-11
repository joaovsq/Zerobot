using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Zerobot.CommandCenter
{
    public enum PanelMode
    {
        CONSOLE,
        GUI
    }

    /// <summary>
    /// Represents the command panel responsible for controlling the Zerobot actions.
    /// 
    /// The CONSOLE mode allows real time scripting and the GUI mode allows real time Visual Scripting.
    /// </summary>
    public class CommandPanel
    {
        private readonly PanelMode mode;

        
        public CommandPanel(PanelMode mode = PanelMode.CONSOLE)
        {
            this.mode = mode;
        }

        /// <summary>
        /// Starts the Command Panel interface (depends on the PanelMode)
        /// </summary>
        public void Start()
        {
           
        }
    }
}
