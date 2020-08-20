using System;
using System.Collections.Generic;
using Stride.Audio;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Shaders.Ast;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Graphics;
using Stride.Navigation;
using Stride.Physics;
using Stride.Rendering;
using Zerobot.Core;

namespace Zerobot.Player
{
    public class PlayerController : SyncScript
    {

        /// <summary>
        /// The maximum speed the character can run at
        /// </summary>
        [Display("Run Speed")]
        public float MaxRunSpeed { get; set; } = 10;

        /// <summary>
        /// The distance from the destination at which the character will stop moving
        /// </summary>
        public float DestinationThreshold { get; set; } = 0.2f;

        /// <summary>
        /// A number from 0 to 1 indicating how much a character should slow down when going around corners
        /// </summary>
        /// <remarks>0 is no slowdown and 1 is completely stopping (on >90 degree angles)</remarks>
        public float CornerSlowdown { get; set; } = 0.6f;

        /// <summary>
        /// Multiplied by the distance to the target and clamped to 1 and used to slow down when nearing the destination
        /// </summary>
        public float DestinationSlowdown { get; set; } = 0.4f;

        [Display("Punch Collision")]
        public RigidbodyComponent PunchCollision { get; set; }

        /// <summary>
        /// The maximum distance from which the character can perform an attack
        /// </summary>
        [Display("Attack Distance")]
        public float AttackDistance { get; set; } = 1f;

        /// <summary>
        /// Cooldown in seconds required for the character to recover from starting an attack until it can choose another action
        /// </summary>
        [Display("Attack Cooldown")]
        public float AttackCooldown { get; set; } = 0.65f;

        // The PlayerController will propagate if it is attacking to the AnimationController
        public static readonly EventKey<bool> IsAttackingEventKey = new EventKey<bool>();

        // The PlayerController will propagate its speed to the AnimationController
        public static readonly EventKey<float> RunSpeedEventKey = new EventKey<float>();

        [Display("Beep Sound")]
        public Sound BeepEffect { get; set; }

        public Prefab SignalEffect { get; set; }

        public Prefab MarkerEffect { get; set; }

        /// <summary>
        /// the main remote command Queue.
        /// </summary>
        [DataMemberIgnore]
        public static readonly Queue<string> RemoteCommandQueue = new Queue<string>();

        private static readonly CommandInterpreter commandInterpreter = new CommandInterpreter();

        private SoundInstance beepSoundInstance;
        private bool signalOn = false;
        private Entity signalInstance;

        private bool markerOn = false;
        private static readonly List<ZerobotMarker> markerTrails = new List<ZerobotMarker>();
        private ZerobotMarker markerActiveTrail = new ZerobotMarker();

        // Allow some inertia to the movement
        private Vector3 moveDirection = Vector3.Zero;
        private bool isRunning = false;

        // Character Component
        private CharacterComponent character;
        private Entity modelChildEntity;
        private float yawOrientation;
        private float lastYawOrientation;

        private Entity attackEntity = null;
        private float attackCooldown = 0f;

        // Pathfinding Component
        private NavigationComponent navigation;
        private readonly List<Vector3> pathToDestination = new List<Vector3>();
        private int waypointIndex;
        private Vector3 moveDestination;

        private bool ReachedDestination => waypointIndex >= pathToDestination.Count;
        private Vector3 CurrentWaypoint => waypointIndex < pathToDestination.Count ? pathToDestination[waypointIndex] : Vector3.Zero;

        private readonly EventReceiver<ClickResult> moveDestinationEvent =
            new EventReceiver<ClickResult>(PlayerInput.MoveDestinationEventKey);

        /// <summary>
        /// Called when the script is first initialized
        /// </summary>
        public override void Start()
        {
            base.Start();

            navigation = Entity.Get<NavigationComponent>();
            character = Entity.Get<CharacterComponent>();
            if (character == null) throw new ArgumentException("Please add a CharacterComponent to the entity containing PlayerController!");
            if (PunchCollision == null) throw new ArgumentException("Please add a RigidbodyComponent as a PunchCollision to the entity containing PlayerController!");

            modelChildEntity = Entity.GetChild(0);
            moveDestination = Entity.Transform.WorldMatrix.TranslationVector;
            PunchCollision.Enabled = false;

            beepSoundInstance = BeepEffect?.CreateInstance();
            beepSoundInstance.Stop();

            commandInterpreter.moveHandler = RemoteMove;
            commandInterpreter.moveCurrentHandler = RemoteMoveCurrentDirection;
            commandInterpreter.canMoveHandler = RemoteCanMove;
            commandInterpreter.turnHandler = RemoteTurn;
            commandInterpreter.haltHandler = HaltMovement;
            commandInterpreter.beepHandler = PlayBeep;
            commandInterpreter.signalHandler = Signal;

            commandInterpreter.markerHandler = MarkerUpDown;
        }

        /// <summary>
        /// Called on every frame update
        /// </summary>
        public override void Update()
        {
            Attack();

            commandInterpreter.NextPendantAction();
            if (!RemoteCommandQueue.IsNullOrEmpty())
            {
                string nextCommand = RemoteCommandQueue.Dequeue();
                commandInterpreter.Execute(nextCommand);
            }

            if (markerActiveTrail.Trail != null && isRunning)
            {
                UpdateMarker();
            }

            Move(MaxRunSpeed);
        }

        /// <summary>
        /// Executes the player attack
        /// </summary>
        private void Attack()
        {
            var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            attackCooldown = (attackCooldown > 0) ? attackCooldown - dt : 0f;

            PunchCollision.Enabled = (attackCooldown > 0);

            if (attackEntity == null)
                return;

            var directionToCharacter = attackEntity.Transform.WorldMatrix.TranslationVector -
                                       modelChildEntity.Transform.WorldMatrix.TranslationVector;
            directionToCharacter.Y = 0;

            var currentDistance = directionToCharacter.Length();
            if (currentDistance <= AttackDistance)
            {
                // Attack!
                HaltMovement();

                attackEntity = null;
                attackCooldown = AttackCooldown;
                PunchCollision.Enabled = true;
                IsAttackingEventKey.Broadcast(true);
            }
            else
            {
                directionToCharacter.Normalize();
                UpdateDestination(attackEntity.Transform.WorldMatrix.TranslationVector);
            }
        }

        private void HaltMovement()
        {
            isRunning = false;
            moveDirection = Vector3.Zero;
            character.SetVelocity(Vector3.Zero);
            moveDestination = modelChildEntity.Transform.WorldMatrix.TranslationVector;
        }

        private void UpdateDestination(Vector3 destination)
        {
            Vector3 delta = moveDestination - destination;
            if (delta.Length() > 0.01f) // Only recalculate path when the target position is different
            {
                pathToDestination.Clear();
                if (navigation.TryFindPath(destination, pathToDestination))
                {
                    // Skip the points that are too close to the player
                    waypointIndex = 0;
                    while (!ReachedDestination && (CurrentWaypoint - Entity.Transform.WorldMatrix.TranslationVector).Length() < 0.25f)
                    {
                        waypointIndex++;
                    }

                    if (!ReachedDestination)
                    {
                        isRunning = true;
                        moveDestination = destination;
                    }
                }
                else
                {
                    // Could not find a path to the target location
                    pathToDestination.Clear();
                    HaltMovement();
                }
            }
        }

        private void UpdateMoveTowardsDestination(float speed)
        {
            if (!ReachedDestination)
            {
                var direction = CurrentWaypoint - Entity.Transform.WorldMatrix.TranslationVector;

                // Get distance towards next point and normalize the direction at the same time
                var length = direction.Length();
                direction /= length;

                bool advance = false;

                // Check to see if an intermediate point was passed by projecting the position along the path
                if (pathToDestination.Count > 0 && waypointIndex > 0 && waypointIndex != pathToDestination.Count - 1)
                {
                    Vector3 pointNormal = CurrentWaypoint - pathToDestination[waypointIndex - 1];
                    pointNormal.Normalize();
                    float current = Vector3.Dot(Entity.Transform.WorldMatrix.TranslationVector, pointNormal);
                    float target = Vector3.Dot(CurrentWaypoint, pointNormal);
                    if (current > target)
                    {
                        advance = true;
                    }
                }
                else
                {
                    if (length < DestinationThreshold)
                    {
                        advance = true;
                    }
                }

                if (advance)
                {
                    waypointIndex++;
                    if (ReachedDestination)
                    {
                        HaltMovement();
                        return;
                    }
                }

                // Calculate speed based on distance from final destination
                float moveSpeed = (moveDestination - Entity.Transform.WorldMatrix.TranslationVector).Length() * DestinationSlowdown;
                if (moveSpeed > 1.0f)
                    moveSpeed = 1.0f;

                // Slow down around corners
                float cornerSpeedMultiply = Math.Max(0.0f, Vector3.Dot(direction, moveDirection)) * CornerSlowdown + (1.0f - CornerSlowdown);

                // Allow a very simple inertia to the character to make animation transitions more fluid
                moveDirection = moveDirection * 0.85f + direction * moveSpeed * cornerSpeedMultiply * 0.15f;

                character.SetVelocity(moveDirection * speed);

                // Broadcast speed as per cent of the max speed
                RunSpeedEventKey.Broadcast(moveDirection.Length());

                // Character orientation
                if (moveDirection.Length() > 0.001)
                {
                    lastYawOrientation = yawOrientation;
                    yawOrientation = MathUtil.RadiansToDegrees((float)Math.Atan2(-moveDirection.Z, moveDirection.X) + MathUtil.PiOverTwo);
                }
                modelChildEntity.Transform.Rotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(yawOrientation), 0, 0);
            }
            else
            {
                HaltMovement();
            }
        }

        private void Move(float speed)
        {
            if (attackCooldown > 0)
                return;

            ClickResult clickResult;
            if (moveDestinationEvent.TryReceive(out clickResult) && clickResult.Type != ClickType.Empty)
            {
                if (clickResult.Type == ClickType.Ground)
                {
                    attackEntity = null;
                    UpdateDestination(clickResult.WorldPosition);
                }

                if (clickResult.Type == ClickType.LootCrate)
                {
                    attackEntity = clickResult.ClickedEntity;
                    Attack();
                }
            }

            if (!isRunning)
            {
                RunSpeedEventKey.Broadcast(0);
                return;
            }

            UpdateMoveTowardsDestination(speed);
        }

        /// <summary>
        /// Move action triggered by the remote controller
        /// The final destination will be: currentPos + direction
        /// </summary>
        /// <param name="direction"> the move direction </param>
        private void RemoteMove(Vector3 direction)
        {
            if (attackCooldown > 0)
                return;

            var currentPos = modelChildEntity.Transform.WorldMatrix.TranslationVector;
            UpdateDestination(currentPos + direction);

            if (!isRunning)
            {
                RunSpeedEventKey.Broadcast(0);
                return;
            }

            UpdateMoveTowardsDestination(MaxRunSpeed);
        }

        /// <summary>
        /// Moves the remote controlled character using its current direction with the given lenght
        /// </summary>
        /// <param name="lenght"></param>
        private void RemoteMoveCurrentDirection(float lenght)
        {
            float radians = modelChildEntity.Transform.Rotation.YawPitchRoll.X;
            var rotation = Quaternion.RotationYawPitchRoll(radians, 0, 0);

            var direction = Vector3.UnitZ * lenght;
            rotation.Rotate(ref direction);

            RemoteMove(direction);
        }

        /// <summary>
        /// If the remoted controlled player can move or not
        /// </summary>
        private bool RemoteCanMove()
        {
            return CurrentWaypoint == Vector3.Zero ?
                true : modelChildEntity.Transform.WorldMatrix.TranslationVector == CurrentWaypoint;
        }

        /// <summary>
        /// Turns the character using a X float in degrees
        /// </summary>
        private void RemoteTurn(float degrees)
        {
            modelChildEntity.Transform.Rotation *= Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(degrees), 0, 0);
        }

        /// <summary>
        /// Plays a beep sound
        /// </summary>
        private void PlayBeep()
        {
            beepSoundInstance.Play();
        }

        /// <summary>
        /// Turns the signal on or off. If the signal is already ON we don't do anything.
        /// </summary>
        private void Signal(bool turnOn)
        {
            if (turnOn && !signalOn)
            {
                var instances = this.SpawnPrefab(SignalEffect, modelChildEntity, modelChildEntity.Transform.LocalMatrix, Vector3.Zero);
                if (instances != null && instances.Count > 0)
                {
                    signalInstance = instances[0];
                }
                signalOn = true;
            }
            else if (!turnOn && signalOn)
            {
                Game.RemoveEntity(signalInstance);
                signalOn = false;
            }
        }

        /// <summary>
        /// Starts or stop the marker depending on the down parameter. 
        /// if down is true = start
        /// </summary>
        private void MarkerUpDown(bool down)
        {

            // if this is a switch from deactivated to activated.
            if (!markerOn && down)
            {
                List<Entity> startEffect = this.SpawnPrefab(MarkerEffect, null, Entity.Transform.WorldMatrix, Vector3.Zero);

                var marker = new ZerobotMarker()
                {
                    StartEffectPrefabInstance = startEffect,
                    Trail = new List<Entity>()
                };

                markerTrails.Add(marker);
                markerActiveTrail = marker;
            }
            else if (markerOn && !down)
            {
                List<Entity> endEffect = this.SpawnPrefab(MarkerEffect, null, Entity.Transform.WorldMatrix, Vector3.Zero);
                markerActiveTrail.EndEffectPrefabInstance = endEffect;

                // replace with a empty reference
                markerActiveTrail = new ZerobotMarker();
            }

            markerOn = down;
        }

        /// <summary>
        /// Updates the active Marker to leave a trail
        /// </summary>
        private void UpdateMarker()
        {
            // If the direction is the same (a straight line for example) we want to update the last existing mesh
            // but, if the direction changes, we need a new entity

            int trailSize = markerActiveTrail.Trail.Count;

            Vector3 currentPos = Entity.Transform.WorldMatrix.TranslationVector;
            float orientation = MathUtil.RadiansToDegrees((float)Math.Atan2(-moveDirection.Z, moveDirection.X) + MathUtil.PiOverTwo);

            // TODO: optimize this, maybe we should use a orientation interval ? To spawn less entities
            if (trailSize <= 0 || orientation != lastYawOrientation)
            {
                var entity = new Entity();
                var model = new Model();
                var material = Content.Load<Material>("Materials/BodyGray");
                model.Materials.Add(material);
                entity.GetOrCreate<ModelComponent>().Model = model;
                entity = this.SpawnModel(entity, Entity.Transform.WorldMatrix);

                var diff = new Vector3(0.1f, 0f, 0.1f);
                //if (pathToDestination.Count > 0 && waypointIndex > 0)
                //{
                //    diff = currentPos - pathToDestination[waypointIndex - 1];
                //}

                var newMesh = this.GenerateLineMesh(diff, Vector3.Zero);
                model.Meshes.Add(newMesh);

                markerActiveTrail.Trail.Add(entity);
            }
            else if (trailSize > 0 && orientation == lastYawOrientation)
            {
                Entity lastEnt = markerActiveTrail.Trail[trailSize - 1];
                Model model = lastEnt.GetOrCreate<ModelComponent>().Model;
                Mesh mesh = model.Meshes[0];

                Vector3 startDiff = currentPos - lastEnt.Transform.WorldMatrix.TranslationVector;

                var verts = new VertexPositionTexture[3];
                verts[0].Position = startDiff;
                verts[1].Position = Vector3.Zero;
                mesh.Draw.VertexBuffers[0].Buffer.SetData(Game.GraphicsContext.CommandList, verts);
            }

            lastYawOrientation = orientation;
        }

    }
}
