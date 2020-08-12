using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Mathematics;

namespace Zerobot.Player
{
    class CommandInterpreter
    {

        delegate void Move(float speed, Vector3 direction);
        delegate void Stop();
        delegate void Marker(bool down);

        public void Execute(string rawCommand)
        {
            
        }
    }
}
