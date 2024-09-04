using Deep.Gameplay.Characters;
using Deep.Gameplay.Common;
using Deep.Gameplay.Weapons;
using Deep.Input;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Deep.Gameplay.Players
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class PlayerInputSystem : SystemBase
    {
        public const float LookSensitivity = 2f; // TODO: move to GameSettings

        private InputActions _inputActions;

        protected override void OnCreate()
        {
            RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Player, PlayerCommands>().Build());
            RequireForUpdate<NetworkTime>();
            RequireForUpdate<NetworkId>();

            _inputActions = new InputActions();
            _inputActions.Enable();
            _inputActions.Default.Enable();
        }

        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            var actions = _inputActions.Default;

            foreach (var (player, playerCommands) in SystemAPI.Query<RefRW<Player>, RefRW<PlayerCommands>>()
                .WithAll<GhostOwnerIsLocal>())
            {
                var isOnNewTick = !player.ValueRW.LastKnownCommandsTick.IsValid || tick.IsNewerThan(player.ValueRW.LastKnownCommandsTick);

                playerCommands.ValueRW = default;

                playerCommands.ValueRW.MoveInput = Vector2.ClampMagnitude(actions.Move.ReadValue<Vector2>(), 1f);

                // Look input must be accumulated on each update belonging to the same tick, because it is a delta and will be processed at a variable update
                if (!isOnNewTick)
                {
                    playerCommands.ValueRW.LookInputDelta = player.ValueRW.LastKnownCommands.LookInputDelta;
                }
                if (math.lengthsq(actions.LookConst.ReadValue<Vector2>()) > math.lengthsq(actions.LookDelta.ReadValue<Vector2>()))
                {
                    // Gamepad look with a constant stick value
                    playerCommands.ValueRW.LookInputDelta += (float2)(actions.LookConst.ReadValue<Vector2>() * LookSensitivity * deltaTime);
                }
                else
                {
                    // Mouse look with a mouse move delta value
                    playerCommands.ValueRW.LookInputDelta += (float2)(actions.LookDelta.ReadValue<Vector2>() * LookSensitivity);
                }

                // Jump
                if (actions.Jump.WasPressedThisFrame())
                {
                    playerCommands.ValueRW.JumpPressed.Set();
                }

                // Shoot
                if (actions.Shoot.WasPressedThisFrame())
                {
                    playerCommands.ValueRW.ShootPressed.Set();
                }
                if (actions.Shoot.WasReleasedThisFrame())
                {
                    playerCommands.ValueRW.ShootReleased.Set();
                }

                // Aim
                playerCommands.ValueRW.AimHeld = actions.Aim.IsPressed();

                player.ValueRW.LastKnownCommandsTick = tick;
                player.ValueRW.LastKnownCommands = playerCommands.ValueRW;
            }
        }
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(CharacterVariableUpdateSystem))]
    [UpdateAfter(typeof(BuildCharacterRotationSystem))]
    [BurstCompile]
    public partial struct PlayerVariableStepControlSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Player, PlayerCommands>().Build());
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (playerCommands, player) in SystemAPI.Query<PlayerCommands, Player>().WithAll<Simulate>())
            {
                if (SystemAPI.HasComponent<CharacterControl>(player.ControlledCharacter))
                {
                    var characterControl = SystemAPI.GetComponent<CharacterControl>(player.ControlledCharacter);

                    // Look
                    characterControl.LookYawPitchDegrees = playerCommands.LookInputDelta;

                    SystemAPI.SetComponent(player.ControlledCharacter, characterControl);
                }
            }
        }
    }

    [UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct PlayerFixedStepControlSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Player, PlayerCommands>().Build());
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (playerCommands, player, commandInterpolationDelay) in SystemAPI
                .Query<PlayerCommands, Player, CommandDataInterpolationDelay>()
                .WithAll<Simulate>())
            {
                // Character
                if (SystemAPI.HasComponent<CharacterControl>(player.ControlledCharacter))
                {
                    var characterControl = SystemAPI.GetComponent<CharacterControl>(player.ControlledCharacter);

                    var characterRotation = SystemAPI.GetComponent<LocalTransform>(player.ControlledCharacter).Rotation;

                    // Move
                    var characterForward = math.mul(characterRotation, math.forward());
                    var characterRight = math.mul(characterRotation, math.right());
                    characterControl.MoveVector = (playerCommands.MoveInput.y * characterForward) + (playerCommands.MoveInput.x * characterRight);
                    characterControl.MoveVector = MathUtilities.ClampToMaxLength(characterControl.MoveVector, 1f);

                    // Jump
                    characterControl.Jump = playerCommands.JumpPressed.IsSet;

                    SystemAPI.SetComponent(player.ControlledCharacter, characterControl);
                }

                // Weapon
                if (SystemAPI.HasComponent<ActiveWeapon>(player.ControlledCharacter))
                {
                    ActiveWeapon activeWeapon = SystemAPI.GetComponent<ActiveWeapon>(player.ControlledCharacter);
                    if (SystemAPI.HasComponent<WeaponControl>(activeWeapon.Value))
                    {
                        var weaponControl = SystemAPI.GetComponent<WeaponControl>(activeWeapon.Value);
                        var interpolationDelay = SystemAPI.GetComponent<InterpolationDelay>(activeWeapon.Value);

                        // Shoot
                        weaponControl.FirePressed = playerCommands.ShootPressed.IsSet;
                        weaponControl.FireReleased = playerCommands.ShootReleased.IsSet;

                        // Aim
                        weaponControl.AimHeld = playerCommands.AimHeld;

                        // Interp delay
                        interpolationDelay.Value = commandInterpolationDelay.Delay;

                        SystemAPI.SetComponent(activeWeapon.Value, weaponControl);
                        SystemAPI.SetComponent(activeWeapon.Value, interpolationDelay);
                    }
                }
            }
        }
    }
}
