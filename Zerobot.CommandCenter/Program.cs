using System;


namespace Zerobot.CommandCenter
{
    class Program
    {
        static void Main(string[] args)
        {
            var panel = new CommandPanel();
            panel.Start();

            Console.WriteLine("CommandCenter is finished. Press any key to close it.");
            Console.ReadLine();
        }
    }
}
