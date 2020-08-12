using System;


namespace Zerobot.CommandCenter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("CommandCenter starting, please give your commands, the power is yours...");
            Console.WriteLine("");
            
            var panel = new CommandPanel(args);
            panel.Start();
            
            Console.WriteLine("CommandCenter is finished. Press any key to close it.");
            Console.ReadLine();
        }
    }
}
