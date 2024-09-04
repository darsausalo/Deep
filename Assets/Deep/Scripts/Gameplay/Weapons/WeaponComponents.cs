using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;

namespace Deep.Gameplay.Weapons
{
    [GhostComponent]
    public struct Weapon : IComponentData
    {
        public Entity VisualPrefab1P;
        public Entity Visual1P;

        public Entity ShotOrigin;

        public float Range;
        public float Spread;
        public int ProjectilesCount;

        public CollisionFilter HitCollisionFilter;

        [GhostField]
        public Random Random;
        [GhostField]
        public uint RemoteShotsCount;
        public uint LastRemoteShotsCount;
    }

    public struct WeaponVisual1P : IComponentData
    {
        public Entity ShotOrigin;
    }

    public struct WeaponSimulationShotOrigin : IComponentData
    {
        public Entity Value;
    }

    public struct RaycastWeapon : IComponentData { }
    public struct MissleWeapon : IComponentData { }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToNonOwner)]
    public struct WeaponControl : IComponentData
    {
        public bool FirePressed;
        public bool FireReleased;
        public bool AimHeld;
    }

    public struct WeaponOwner : IComponentData
    {
        public Entity Owner;
    }

    [GhostComponent]
    public struct WeaponFireState : IComponentData
    {
        public bool Automatic;
        public float Rate;

        [GhostField]
        public float ShotTimer;
        [GhostField]
        public bool IsFiring;
        public uint ShotsToFire;
    }

    public struct WeaponFeedback : IComponentData
    {
        public float BobHAmount;
        public float BobVAmount;
        public float BobFrequency;
        public float BobSharpness;
        public float BobAimRatio;

        public float AimFOVRatio;
        public float AimFOVSharpness;
        public float AimingLookSensitivityMultiplier;

        public float RecoilFOVKick;
        public float RecoilMaxFOVKick;
        public float RecoilFOVKickSharpness;
        public float RecoilFOVKickRestitutionSharpness;

        public int ShotFeedbackRequests;
    }

    [GhostComponent]
    public struct ActiveWeapon : IComponentData
    {
        [GhostField]
        public Entity Value;
        public Entity Previous;
    }

    [GhostComponent]
    public struct WeaponProjectile : IComponentData
    {
        public Entity Prefab;

        public float Speed;
    }

    public struct ProjectileShot : IBufferElementData
    {
        public ProjectileShotData Data;
    }

    public struct ProjectileShotData : IComponentData
    {
        public Entity OriginEntity;
        public float3 SimulationOrigin;
        public float3 SimulationDirection;
        public float3 Up;
        public float HitDistance;
    }

    public struct WeaponShotIgnoredEntity : IBufferElementData
    {
        public Entity Value;
    }

    public struct CharacterLinkedWeapon : IComponentData
    {
        public Entity Value;
    }

    public struct CharacterWeaponFeedback : IComponentData
    {
        public float3 WeaponLocalPosBob;
        public float3 WeaponLocalPosRecoil;

        public float TargetRecoilFOVKick;
        public float CurrentRecoilFOVKick;
    }
}
