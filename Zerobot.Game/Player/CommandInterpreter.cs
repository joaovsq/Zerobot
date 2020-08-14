using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Mathematics;
using Zerobot.CommandCenter;

namespace Zerobot.Player
{
    class CommandInterpreter
    {

        public delegate void Move(Vector3 direction);
        public delegate void Halt();
        public delegate void Beep();
        public delegate void Signal(bool on);
        public delegate void Marker(bool down);

        public Move moveHandler;
        public Halt haltHandler;
        public Beep beepHandler;
        public Signal signalHandler;
        public Marker markerHandler;

        public void Execute(string rawCommand)
        {
            try
            {
                var expression = new TokenExpression(rawCommand);

                switch (expression.Token)
                {
                    case CommandToken.Move:
                        // TODO: optimize this thing, catch branches are terrible
                        try
                        {
                            moveHandler(GetMoveDirectionVector(expression.Operands[0], float.Parse(expression.Operands[1])));
                        }
                        catch (Exception)
                        {
                            moveHandler(GetMoveDirectionVector(expression.Operands[0]));
                        }
                        break;

                    case CommandToken.Stop:
                        haltHandler();
                        break;

                    case CommandToken.Beep:
                        beepHandler();
                        break;

                    case CommandToken.Signal:
                        bool isOn = expression.Operands[0].Equals("on");
                        signalHandler(isOn);
                        break;

                    case CommandToken.Marker:
                        bool isDown = expression.Operands[0].Equals("down");
                        markerHandler(isDown);
                        break;

                    default:
                        break;
                }

            }
            catch (Exception)
            {
            }
        }

        private Vector3 GetMoveDirectionVector(string direction, float lenght = 1f)
        {

            if (direction.Equals("up"))
            {
                return new Vector3(lenght, 0f, 0f);
            }
            else if (direction.Equals("down"))
            {
                return new Vector3(lenght * (-1), 0f, 0f);
            }
            else if (direction.Equals("left"))
            {
                return new Vector3(0f, 0f, lenght * (-1));
            }
            else if (direction.Equals("right"))
            {
                return new Vector3(0f, 0f, lenght);
            }

            return new Vector3();
        }

    }
}
