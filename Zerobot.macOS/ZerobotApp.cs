using Stride.Engine;

namespace Zerobot
{
    class ZerobotApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
