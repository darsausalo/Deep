using Deep.Authoring.Physics;
using Deep.Gameplay.Common;
using Deep.Gameplay.Weapons;
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Deep.Authoring.Weapons
{
    public class WeaponAuthoring : MonoBehaviour
    {
        public enum WeaponKind { Raycast, Missle };

        public WeaponKind Kind;

        [Header("References")]
        public GameObject VisualPrefab1P;
        public GameObject ProjectilePrefab;
        public GameObject ShotOrigin;

        [Header("Firing")]
        public bool Automatic = false;
        public float Rate = 1f;
        public float Range = 1000f;
        public float Spread = 0f;
        public int ProjectilesCount = 1;
        public float ProjectileSpeed = 300f;
        public PhysicsCategoryTags HitCollisionFilter = PhysicsCategoryTags.Everything;

        public WeaponFeedbackAuthoring Feedback = WeaponFeedbackAuthoring.GetDefault();

        public class Baker : Baker<WeaponAuthoring>
        {
            public override void Bake(WeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                if (authoring.Kind == WeaponKind.Missle)
                    AddComponent<MissleWeapon>(entity);
                else
                    AddComponent<RaycastWeapon>(entity);

                AddComponent(entity, new Weapon
                {
                    VisualPrefab1P = GetEntity(authoring.VisualPrefab1P, TransformUsageFlags.Dynamic),
                    ShotOrigin = GetEntity(authoring.ShotOrigin, TransformUsageFlags.Dynamic),
                    Range = authoring.Range,
                    Spread = math.radians(authoring.Spread),
                    ProjectilesCount = authoring.ProjectilesCount,
                    HitCollisionFilter = new CollisionFilter
                    {
                        BelongsTo = CollisionFilter.Default.BelongsTo,
                        CollidesWith = authoring.HitCollisionFilter.Value
                    },
                    Random = Unity.Mathematics.Random.CreateFromIndex(0)
                });
                AddComponent(entity, new WeaponFireState
                {
                    Automatic = authoring.Automatic,
                    Rate = authoring.Rate,
                    ShotTimer = authoring.Rate > 0f ? 1f / authoring.Rate : 0f
                });
                AddComponent(entity, new WeaponFeedback
                {
                    BobHAmount = authoring.Feedback.BobHAmount,
                    BobVAmount = authoring.Feedback.BobVAmount,
                    BobFrequency = authoring.Feedback.BobFrequency,
                    BobSharpness = authoring.Feedback.BobSharpness,
                    BobAimRatio = authoring.Feedback.BobAimRatio,

                    AimFOVRatio = authoring.Feedback.AimFOVRatio,
                    AimFOVSharpness = authoring.Feedback.AimFOVSharpness,
                    AimingLookSensitivityMultiplier = authoring.Feedback.AimingLookSensitivityMultiplier,

                    RecoilFOVKick = authoring.Feedback.RecoilFOVKick,
                    RecoilMaxFOVKick = authoring.Feedback.RecoilMaxFOVKick,
                    RecoilFOVKickSharpness = authoring.Feedback.RecoilFOVKickSharpness,
                    RecoilFOVKickRestitutionSharpness = authoring.Feedback.RecoilFOVKickRestitutionSharpness
                });
                AddComponent(entity, new WeaponProjectile
                {
                    Prefab = GetEntity(authoring.ProjectilePrefab, TransformUsageFlags.Dynamic),
                    Speed = authoring.ProjectileSpeed
                });
                AddComponent<WeaponControl>(entity);
                AddComponent<WeaponOwner>(entity);
                AddComponent<WeaponSimulationShotOrigin>(entity);
                AddComponent<InterpolationDelay>(entity);
                AddBuffer<ProjectileShot>(entity);
                AddBuffer<WeaponShotIgnoredEntity>(entity);
            }
        }

        [Serializable]
        public struct WeaponFeedbackAuthoring
        {
            [Header("Bobbing")]
            public float BobHAmount;
            public float BobVAmount;
            public float BobFrequency;
            public float BobSharpness;
            public float BobAimRatio;

            [Header("Aiming")]
            public float AimFOVRatio;
            public float AimFOVSharpness;
            public float AimingLookSensitivityMultiplier;

            [Header("FoV Kick")]
            public float RecoilFOVKick;
            public float RecoilMaxFOVKick;
            public float RecoilFOVKickSharpness;
            public float RecoilFOVKickRestitutionSharpness;

            public static WeaponFeedbackAuthoring GetDefault()
            {
                return new WeaponFeedbackAuthoring
                {
                    BobHAmount = 0.08f,
                    BobVAmount = 0.06f,
                    BobFrequency = 10f,
                    BobSharpness = 10f,
                    BobAimRatio = 0.25f,

                    AimFOVRatio = 0.5f,
                    AimFOVSharpness = 10f,
                    AimingLookSensitivityMultiplier = 0.7f,

                    RecoilFOVKick = 10f,
                    RecoilMaxFOVKick = 10f,
                    RecoilFOVKickSharpness = 150f,
                    RecoilFOVKickRestitutionSharpness = 15f,
                };
            }
        }
    }
}
