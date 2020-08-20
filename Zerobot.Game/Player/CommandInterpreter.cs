using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Zerobot.CommandCenter;

namespace Zerobot.Player
{
    sealed class CommandInterpreter
    {

        public delegate void Move(Vector3 direction);
        public delegate void MoveCurrentDirection(float lenght);
        public delegate void Turn(float degrees);
        public delegate bool CanMove();
        public delegate void Halt();
        public delegate void Beep();
        public delegate void Signal(bool on);
        public delegate void Marker(bool down);

        public Move moveHandler;
        public MoveCurrentDirection moveCurrentHandler;
        public Turn turnHandler;
        public CanMove canMoveHandler;
        public Halt haltHandler;
        public Beep beepHandler;
        public Signal signalHandler;
        public Marker markerHandler;

        // temp command queue to hold commands until the End token is received
        private static readonly Queue<TokenExpression> commandQueue = new Queue<TokenExpression>();

        // a pendant movement is hold in this queue when the CanMove() delegate returns false
        private static readonly Queue<TokenExpression> pendantActions = new Queue<TokenExpression>();

        /// <summary>
        /// Executes the next pendant action
        /// </summary>
        public void NextPendantAction()
        {
            if (!canMoveHandler() || pendantActions.IsNullOrEmpty())
            {
                return;
            }

            var expression = pendantActions.Dequeue();
            ExecuteExpression(expression);
        }

        /// <summary>
        /// Enqueue a list of commands and execute them when a token End is received
        /// </summary>
        public void Execute(string rawCommand)
        {
            try
            {
                var expression = new TokenExpression(rawCommand);

                if (expression.Token.Equals(CommandToken.End))
                {
                    while (!commandQueue.IsNullOrEmpty())
                    {
                        var queuedExpression = commandQueue.Dequeue();
                        ExecuteExpression(queuedExpression);
                    }
                    commandQueue.Clear();
                }
                else
                {
                    commandQueue.Enqueue(expression);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Executes the token expression action
        /// </summary>
        /// <param name="expression">a token expression</param>
        private void ExecuteExpression(TokenExpression expression)
        {
            if (!canMoveHandler())
            {
                pendantActions.Enqueue(expression);
                return;
            }

            switch (expression.Token)
            {
                case CommandToken.Move:

                    MoveCommand(expression.Operands);
                    break;

                case CommandToken.Turn:
                    RotateCommand(expression.Operands);
                    break;

                case CommandToken.Stop:
                    haltHandler();
                    break;

                case CommandToken.Beep:
                    beepHandler();
                    break;

                case CommandToken.Signal:
                    bool isOn = expression.Operands[0].Equals("on");
                    bool isOff = expression.Operands[0].Equals("off");
                    bool turnOn = isOn && isOff == false;
                    if (!isOn && !isOff) break;

                    signalHandler(turnOn);
                    break;

                case CommandToken.Marker:
                    bool isDown = expression.Operands[0].Equals("down");
                    bool isUp = expression.Operands[0].Equals("up");
                    bool goDown = isDown && isUp == false;
                    if (!isDown && !isUp) break;

                    markerHandler(goDown);
                    break;

                default:
                    break;
            }
        }

        private void MoveCommand(List<string> operands)
        {
            string direction = operands[0];
            float lenght = 1f;

            // TODO: optimize this thing, catch branches are terrible
            try
            {
                lenght = float.Parse(operands[1]);
            }
            catch (Exception) { }

            var movement = Vector3.Zero;
            if (direction.Equals("up"))
            {
                moveHandler(new Vector3(lenght, 0f, 0f));
            }
            else if (direction.Equals("down"))
            {
                moveHandler(new Vector3(lenght * (-1), 0f, 0f));
            }
            else if (direction.Equals("left"))
            {
                moveHandler(new Vector3(0f, 0f, lenght * (-1)));
            }
            else if (direction.Equals("right"))
            {
                moveHandler(new Vector3(0f, 0f, lenght));
            }
            else
            {
                // if is not a direction, then can only be a leght, for example: move 10 
                // in this case we must use the current direction callback
                lenght = float.Parse(direction);
                moveCurrentHandler(lenght);
            }
        }

        private void RotateCommand(List<string> operands)
        {
            if (operands.Count < 2)
            {
                throw new ArgumentException("A Turn expression must have a direction (right or left) and a value in degrees. For example: turn left 90");
            }

            string direction = operands[0];
            bool isRight = direction.Equals("right");
            bool isLeft = direction.Equals("left");
            if (!isRight && !isLeft)
            {
                throw new ArgumentException("The turn direction must be right or left");
            }

            float degrees;
            try
            {
                degrees = float.Parse(operands[1]);
            }
            catch (Exception)
            {
                throw new ArgumentException("the turn degrees must be a float");
            }

            if (isRight)
            {
                turnHandler(-degrees);
            }
            else if (isLeft)
            {
                turnHandler(degrees);
            }

        }

    }
}
