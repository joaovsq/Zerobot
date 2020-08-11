using System;


namespace Zerobot.CommandCenter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            while (true)
            {
                var input = Console.ReadLine();

                if (input.Equals("exit"))
                {
                    break;
                }
            }
        }
    }
}
