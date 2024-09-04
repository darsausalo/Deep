using Deep.Gameplay.Cameras;
using Deep.Gameplay.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace Deep.Gameplay.Characters
{

    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct ClientCharacterSetupSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<NetworkId>()
                .WithAll<NetworkStreamInGame>()
                .Build();
            state.RequireForUpdate(query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            // TODO: ISSUE - sometines doesn't work disable rendering for 3P (need wait for scene load)

            // Initialize local-owned characters
            foreach (var (visuals, entity) in SystemAPI
                .Query<CharacterVisuals>()
                .WithAll<GhostOwnerIsLocal>()
                .WithNone<CharacterClientCleanup>()
                .WithEntityAccess())
            {
                // TODO: use GameSettings (singleton?) for FOV
                ecb.AddComponent(visuals.Visuals1P, new MainEntityCamera
                {
                    BaseFoV = 80f,
                    CurrentFoV = 80f
                });

                CommonUtilities.DisableRenderingInHierarchy(state.EntityManager, ecb, visuals.Visuals3P, SystemAPI.GetBufferLookup<Child>());
                // TODO: enable crosshair

                ecb.AddComponent<CharacterClientCleanup>(entity);
            }

            // Initialize remote characters
            foreach (var (visuals, entity) in SystemAPI
                .Query<CharacterVisuals>()
                .WithNone<GhostOwnerIsLocal>()
                .WithNone<CharacterClientCleanup>()
                .WithEntityAccess())
            {
                CommonUtilities.DisableRenderingInHierarchy(state.EntityManager, ecb, visuals.Visuals1P, SystemAPI.GetBufferLookup<Child>());

                ecb.AddComponent<CharacterClientCleanup>(entity);
            }

            // Handle destroyed characters
            foreach (var (characterCleanup, entity) in SystemAPI
                .Query<CharacterClientCleanup>()
                .WithNone<CharacterComponent>()
                .WithEntityAccess())
            {
                // TODO: spawn DeathVFX?
                ecb.RemoveComponent<CharacterClientCleanup>(entity);
            }
        }
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct BuildCharacterRotationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<LocalTransform, CharacterComponent>().Build());
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new BuildCharacterRotationJob().Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct BuildCharacterRotationJob : IJobEntity
        {
            void Execute(ref LocalTransform localTransform, in CharacterComponent characterComponent)
            {
                localTransform.Rotation = CharacterUtilities.ComputeRotationFromYAngleAndUp(characterComponent.CharacterYDegrees, math.up());
            }
        }
    }

    [UpdateInGroup(typeof(KinematicCharacterPhysicsUpdateGroup))]
    [BurstCompile]
    public partial struct CharacterPhysicsUpdateSystem : ISystem
    {
        private EntityQuery _characterQuery;
        private CharacterUpdateContext _context;
        private KinematicCharacterUpdateContext _baseContext;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _characterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder()
                .WithAll<
                    CharacterComponent,
                    CharacterControl>()
                .Build(ref state);

            _context = new CharacterUpdateContext();
            _context.OnSystemCreate(ref state);
            _baseContext = new KinematicCharacterUpdateContext();
            _baseContext.OnSystemCreate(ref state);

            state.RequireForUpdate(_characterQuery);
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<NetworkTime>())
                return;

            _context.OnSystemUpdate(ref state);
            _baseContext.OnSystemUpdate(ref state, SystemAPI.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());

            state.Dependency = new CharacterPhysicsUpdateJob
            {
                Context = _context,
                BaseContext = _baseContext,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct CharacterPhysicsUpdateJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public CharacterUpdateContext Context;
            public KinematicCharacterUpdateContext BaseContext;

            void Execute(CharacterAspect characterAspect)
            {
                characterAspect.PhysicsUpdate(ref Context, ref BaseContext);
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BaseContext.EnsureCreationOfTmpCollections();
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            { }
        }
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedFixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct CharacterVariableUpdateSystem : ISystem
    {
        private EntityQuery _characterQuery;
        private CharacterUpdateContext _context;
        private KinematicCharacterUpdateContext _baseContext;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _characterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder()
                .WithAll<
                    CharacterComponent,
                    CharacterControl>()
                .Build(ref state);

            _context = new CharacterUpdateContext();
            _context.OnSystemCreate(ref state);
            _baseContext = new KinematicCharacterUpdateContext();
            _baseContext.OnSystemCreate(ref state);

            state.RequireForUpdate(_characterQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _context.OnSystemUpdate(ref state);
            _baseContext.OnSystemUpdate(ref state, SystemAPI.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());

            state.Dependency = new CharacterVariableUpdateJob
            {
                Context = _context,
                BaseContext = _baseContext,
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new CharacterViewJob
            {
                CharacterLookup = SystemAPI.GetComponentLookup<CharacterComponent>(true),
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct CharacterVariableUpdateJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public CharacterUpdateContext Context;
            public KinematicCharacterUpdateContext BaseContext;

            void Execute(CharacterAspect characterAspect)
            {
                characterAspect.VariableUpdate(ref Context, ref BaseContext);
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BaseContext.EnsureCreationOfTmpCollections();
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            { }
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct CharacterViewJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<CharacterComponent> CharacterLookup;

            void Execute(ref LocalTransform localTransform, in CharacterView characterView)
            {
                if (CharacterLookup.TryGetComponent(characterView.Character, out CharacterComponent character))
                {
                    localTransform.Rotation = character.ViewLocalRotation;
                }
            }
        }
    }
}
