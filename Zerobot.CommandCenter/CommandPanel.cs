using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
        readonly string[] args;

        public CommandPanel(string[] args, PanelMode mode = PanelMode.CONSOLE)
        {
            this.mode = mode;

            if (mode.Equals(PanelMode.GUI))
            {
                Environment.FailFast("Sorry, we still don't have support for GUI mode.");
            }

            this.args = args;
        }

        /// <summary>
        /// Starts the Command Panel interface (depends on the PanelMode)
        /// </summary>
        public void Start()
        {
            switch (mode)
            {
                case PanelMode.CONSOLE:
                    ConsoleMode(args);
                    break;

                case PanelMode.GUI:
                    GuiMode(args);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Starts the CommandPanel in console mode.
        ///  
        /// This mode expects the OPcodes (CommandToken) directly in the console and send them synchronously to the Zerobot process.
        /// </summary>
        /// <param name="args">cmd args</param>
        private void ConsoleMode(string[] args)
        {
            using (PipeStream pipeClient =
                new AnonymousPipeClientStream(PipeDirection.Out, args[0]))
            {
                using (StreamWriter writer = new StreamWriter(pipeClient))
                {
                    writer.AutoFlush = true;

                    while (true)
                    {
                        Console.Write(">> ");
                        string input = Console.ReadLine();
                        if (input.Equals("exit"))
                        {
                            break;
                        }

                        try
                        {
                            var expression = new TokenExpression(input);
                            writer.Write(expression.ToString());

                            pipeClient.WaitForPipeDrain();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Error parsing your command: {e.Message}");
                            continue;
                        }
                    }
                }
            }
        }

        private void GuiMode(string[] args)
        {
            // TODO (jv): implement
        }
    }
}
