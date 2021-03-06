using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Physics;
using Stride.Rendering;

namespace Zerobot.Core
{
    public static class Utils
    {

        public static Mesh GenerateLineMesh(this ScriptComponent script, Vector3 positionA, Vector3 positionB)
        {
            var pointA = new Vector3(positionA.X, 0f, positionA.Z);
            var pointB = new Vector3(positionB.X, 0f, positionB.Z);

            var vertices = new VertexPositionTexture[3];
            vertices[0].Position = pointA;
            vertices[1].Position = pointB;
            var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(script.GraphicsDevice, vertices,
                                                                 GraphicsResourceUsage.Dynamic);
            int[] indices = { 0, 1 };
            var indexBuffer = Stride.Graphics.Buffer.Index.New(script.GraphicsDevice, indices);
            var mesh = new Mesh
            {
                //Draw = GeometricPrimitive.Cylinder.New(GraphicsDevice).ToMeshDraw()
                Draw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.LineList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
                    VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer,
                                  VertexPositionTexture.Layout, vertexBuffer.ElementCount) },
                }
            };

            return mesh;
        }

        public static Entity SpawnModel(this ScriptComponent script, Entity source, Matrix localMatrix)
        {
            if (source == null)
                return null;

            var sourceClone = source.Clone();

            var entityMatrix = source.Transform.LocalMatrix * localMatrix;
            entityMatrix.Decompose(out sourceClone.Transform.Scale, out sourceClone.Transform.Rotation, out sourceClone.Transform.Position);

            script.SceneSystem.SceneInstance.RootScene.Entities.Add(sourceClone);

            return sourceClone;
        }

        public static List<Entity> SpawnPrefab(this ScriptComponent script, Prefab source, Entity attachEntity, Matrix localMatrix, Vector3 forceImpulse)
        {
            if (source == null)
                return null;

            var spawnedEntities = source.Instantiate();

            foreach (var prefabEntity in spawnedEntities)
            {
                prefabEntity.Transform.UpdateLocalMatrix();
                var entityMatrix = prefabEntity.Transform.LocalMatrix * localMatrix;
                entityMatrix.Decompose(out prefabEntity.Transform.Scale, out prefabEntity.Transform.Rotation, out prefabEntity.Transform.Position);

                if (attachEntity != null)
                {
                    attachEntity.AddChild(prefabEntity);
                }
                else
                {
                    script.SceneSystem.SceneInstance.RootScene.Entities.Add(prefabEntity);
                }

                var physComp = prefabEntity.Get<RigidbodyComponent>();
                if (physComp != null)
                {
                    physComp.ApplyImpulse(forceImpulse);
                }
            }

            return spawnedEntities;
        }

        public static void SpawnPrefabInstance(this ScriptComponent script, Prefab source, Entity attachEntity, float timeout, Matrix localMatrix)
        {
            if (source == null)
                return;

            Func<Task> spawnTask = async () =>
            {
                // Clone
                var spawnedEntities = source.Instantiate();

                foreach (var prefabEntity in spawnedEntities)
                {
                    prefabEntity.Transform.UpdateLocalMatrix();
                    var entityMatrix = prefabEntity.Transform.LocalMatrix * localMatrix;
                    entityMatrix.Decompose(out prefabEntity.Transform.Scale, out prefabEntity.Transform.Rotation, out prefabEntity.Transform.Position);

                    if (attachEntity != null)
                    {
                        attachEntity.AddChild(prefabEntity);
                    }
                    else
                    {
                        script.SceneSystem.SceneInstance.RootScene.Entities.Add(prefabEntity);
                    }
                }

                // Countdown
                var secondsCountdown = timeout;
                while (secondsCountdown > 0f)
                {
                    await script.Script.NextFrame();
                    secondsCountdown -= (float)script.Game.UpdateTime.Elapsed.TotalSeconds;
                }

                // Remove
                foreach (var clonedEntity in spawnedEntities)
                {
                    if (attachEntity != null)
                    {
                        attachEntity.RemoveChild(clonedEntity);
                    }
                    else
                    {
                        script.SceneSystem.SceneInstance.RootScene.Entities.Remove(clonedEntity);
                    }
                }

                // Cleanup
                spawnedEntities.Clear();
            };

            script.Script.AddTask(spawnTask);
        }

        /// <summary>
        /// Removes a prefab from the scene
        /// </summary>
        public static void RemovePrefab(this IGame game, Prefab prefab)
        {
            foreach (var entity in prefab.Entities)
            {
                game.RemoveEntity(entity);
            }
        }

        /// <summary>
        /// Removes an entity, together with its children, from the Game's scene graph
        /// </summary>
        /// <param name="game">The game instance containing the entity</param>
        /// <param name="entity">Entity to remove</param>
        public static void RemoveEntity(this IGame game, Entity entity)
        {
            var parent = entity.GetParent();
            if (parent != null)
            {
                parent.RemoveChild(entity);
                return;
            }

            ((Game)game).SceneSystem.SceneInstance.RootScene.Entities.Remove(entity);
        }

        public static async Task WaitTime(this IGame game, TimeSpan time)
        {
            var g = (Game)game;
            var goal = game.UpdateTime.Total + time;
            while (game.UpdateTime.Total < goal)
            {
                await g.Script.NextFrame();
            }
        }

        public static Vector3 LogicDirectionToWorldDirection(Vector2 logicDirection, CameraComponent camera, Vector3 upVector)
        {
            var inverseView = Matrix.Invert(camera.ViewMatrix);

            var forward = Vector3.Cross(upVector, inverseView.Right);
            forward.Normalize();

            var right = Vector3.Cross(forward, upVector);
            var worldDirection = forward * logicDirection.Y + right * logicDirection.X;
            worldDirection.Normalize();
            return worldDirection;
        }

        public static bool ScreenPositionToWorldPositionRaycast(Vector2 screenPos, CameraComponent camera, Simulation simulation, out ClickResult clickResult)
        {
            Matrix invViewProj = Matrix.Invert(camera.ViewProjectionMatrix);

            Vector3 sPos;
            sPos.X = screenPos.X * 2f - 1f;
            sPos.Y = 1f - screenPos.Y * 2f;

            sPos.Z = 0f;
            var vectorNear = Vector3.Transform(sPos, invViewProj);
            vectorNear /= vectorNear.W;

            sPos.Z = 1f;
            var vectorFar = Vector3.Transform(sPos, invViewProj);
            vectorFar /= vectorFar.W;

            clickResult.ClickedEntity = null;
            clickResult.WorldPosition = Vector3.Zero;
            clickResult.Type = ClickType.Empty;
            clickResult.HitResult = new HitResult();

            var minDistance = float.PositiveInfinity;

            var result = new FastList<HitResult>();
            simulation.RaycastPenetrating(vectorNear.XYZ(), vectorFar.XYZ(), result);
            foreach (var hitResult in result)
            {
                ClickType type = ClickType.Empty;

                var staticBody = hitResult.Collider as StaticColliderComponent;
                if (staticBody != null)
                {
                    if (staticBody.CollisionGroup == CollisionFilterGroups.CustomFilter1)
                        type = ClickType.Ground;

                    if (staticBody.CollisionGroup == CollisionFilterGroups.CustomFilter2)
                        type = ClickType.LootCrate;

                    if (type != ClickType.Empty)
                    {
                        var distance = (vectorNear.XYZ() - hitResult.Point).LengthSquared();
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            clickResult.Type = type;
                            clickResult.HitResult = hitResult;
                            clickResult.WorldPosition = hitResult.Point;
                            clickResult.ClickedEntity = hitResult.Collider.Entity;
                        }
                    }
                }
            }

            return (clickResult.Type != ClickType.Empty);
        }
    }
}
