using Deep.Gameplay.Characters;
using Deep.Gameplay.Common;
using Deep.Gameplay.Players;
using Deep.Gameplay.Weapons;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Deep.Gameplay.GameManagement
{
    public struct GoInGameRequest : IRpcCommand
    {
    }

    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GoInGameClientSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>()
                .Build();
            state.RequireForUpdate(query);

            // TODO: use common TickRate setup
            var tickRate = new ClientServerTickRate();
            tickRate.ResolveDefaults();
            tickRate.SimulationTickRate = 60;
            tickRate.NetworkTickRate = 60;
            tickRate.MaxSimulationStepsPerFrame = 4;
            state.EntityManager.CreateSingleton(tickRate);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            using var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>()
                .WithEntityAccess())
            {
                commandBuffer.AddComponent<NetworkStreamInGame>(entity);

                var request = commandBuffer.CreateEntity();
                commandBuffer.AddComponent<GoInGameRequest>(request);
                commandBuffer.AddComponent(request, new SendRpcCommandRequest { TargetConnection = entity });
            }

            commandBuffer.Playback(state.EntityManager);
        }
    }

    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [CreateAfter(typeof(NetworkStreamReceiveSystem))]
    public partial struct GoInGameServerSystem : ISystem
    {
        private ComponentLookup<NetworkId> _networkIdLookup;
        private NativeList<float3> _usedPositions;
        private Unity.Mathematics.Random _random;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerSpwaner>();
            state.RequireForUpdate<SpawnPointElement>();

            var query = SystemAPI.QueryBuilder()
                .WithAll<GoInGameRequest>()
                .WithAll<ReceiveRpcCommandRequest>()
                .Build();
            state.RequireForUpdate(query);

            _networkIdLookup = state.GetComponentLookup<NetworkId>(true);

            _usedPositions = new NativeList<float3>(Allocator.Persistent);
            var currentTime = DateTime.UtcNow;
            var seed = currentTime.Minute + currentTime.Second + currentTime.Millisecond + 1;
            _random = new Unity.Mathematics.Random((uint)seed);

            // TODO: use common TickRate setup
            var tickRate = new ClientServerTickRate();
            tickRate.ResolveDefaults();
            tickRate.SimulationTickRate = 60;
            tickRate.NetworkTickRate = 60;
            tickRate.MaxSimulationStepsPerFrame = 4;
            state.EntityManager.CreateSingleton(tickRate);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _usedPositions.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var worldName = state.WorldUnmanaged.Name;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            _networkIdLookup.Update(ref state);

            var spawnPoints = SystemAPI.GetSingletonBuffer<SpawnPointElement>();
            var playerSpawner = SystemAPI.GetSingleton<PlayerSpwaner>();

            foreach (var (commandRequest, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>()
                .WithAll<GoInGameRequest>()
                .WithEntityAccess())
            {
                var connEntity = commandRequest.ValueRO.SourceConnection;

                ecb.AddComponent<NetworkStreamInGame>(connEntity);

                var networkId = _networkIdLookup[connEntity];

                Debug.Log($"'{worldName}' setting connection '{networkId.Value}' to go in game");

                var spawnPointsArray = spawnPoints.ToNativeArray(Allocator.TempJob);
                state.Dependency = new GetPositionJob
                {
                    SpawnPoints = spawnPointsArray,
                    UsedPositions = _usedPositions,
                    Random = _random
                }.Schedule(state.Dependency);
                state.Dependency.Complete();
                spawnPointsArray.Dispose(state.Dependency);

                // spawn player
                var playerEntity = ecb.Instantiate(playerSpawner.Player);
                ecb.SetComponent(playerEntity, new GhostOwner { NetworkId = networkId.Value });

                // spawn player character
                var spawnPosition = _usedPositions[_usedPositions.Length - 1];
                var characterEntity = CharacterUtilities.SpawnCharacter(ref ecb, playerSpawner.Character, spawnPosition, networkId.Value);
                ecb.SetComponent(playerEntity, new Player { ControlledCharacter = characterEntity });

                // spawn weapon (TODO: remove after inventory implementation)
                if (playerSpawner.Weapon != Entity.Null)
                {
                    var weaponEntity = ecb.Instantiate(playerSpawner.Weapon);
                    ecb.SetComponent(weaponEntity, new GhostOwner { NetworkId = networkId.Value });
                    ecb.SetComponent(characterEntity, new ActiveWeapon { Value = weaponEntity });
                }

                // link player and character to connection
                ecb.AppendToBuffer(connEntity, new LinkedEntityGroup { Value = playerEntity });
                ecb.AppendToBuffer(connEntity, new LinkedEntityGroup { Value = characterEntity });

                // assign ghost connection position
                ecb.AddComponent<GhostConnectionPosition>(connEntity);
                ecb.SetComponent(connEntity, new CommandTarget { targetEntity = characterEntity });

                // cleanup
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);

            state.Dependency = new UpdatePositionJob
            {
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true)
            }.ScheduleParallel(state.Dependency);

            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct GetPositionJob : IJob
        {
            public NativeArray<SpawnPointElement> SpawnPoints;
            public NativeList<float3> UsedPositions;
            public Unity.Mathematics.Random Random;

            [BurstCompile]
            public void Execute()
            {
                var availablePositions = CreateAvailablePositions();

                if (availablePositions.Length == 0)
                {
                    UsedPositions.Clear();
                    availablePositions.Dispose();
                    availablePositions = CreateAvailablePositions();
                }

                var randomIndex = Random.NextInt(0, availablePositions.Length);
                var position = availablePositions[randomIndex];
                UsedPositions.Add(position);

                availablePositions.Dispose();
            }

            private NativeList<float3> CreateAvailablePositions()
            {
                var availablePositions = new NativeList<float3>(Allocator.TempJob);

                foreach (var position in SpawnPoints)
                {
                    if (!UsedPositions.Contains(position.Value))
                    {
                        availablePositions.Add(position.Value);
                    }
                }

                return availablePositions;
            }
        }

        [BurstCompile]
        private partial struct UpdatePositionJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalTransform> TransformLookup;

            public void Execute(ref GhostConnectionPosition connectionPosition, in CommandTarget target)
            {
                if (!TransformLookup.HasComponent(target.targetEntity))
                    return;
                connectionPosition = new GhostConnectionPosition
                {
                    Position = TransformLookup[target.targetEntity].Position
                };
            }
        }
    }
}
