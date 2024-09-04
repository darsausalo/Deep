using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Deep.Gameplay.Players
{
    [GhostComponent]
    public struct Player : IComponentData
    {
        [GhostField]
        public Entity ControlledCharacter;

        [GhostField]
        public FixedString128Bytes Name;

        public NetworkTick LastKnownCommandsTick;
        public PlayerCommands LastKnownCommands;
    }

    public struct PlayerCommands : IInputComponentData
    {
        public float2 MoveInput;
        public float2 LookInputDelta;
        public InputEvent JumpPressed;
        public InputEvent ShootPressed;
        public InputEvent ShootReleased;
        public bool AimHeld;
    }

    public struct PlayerSpwaner : IComponentData
    {
        public Entity Player;
        public Entity Character;
        public Entity Weapon;
    }
}
