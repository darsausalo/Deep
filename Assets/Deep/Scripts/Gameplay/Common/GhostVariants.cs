﻿using System.Collections.Generic;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Deep.Gameplay.Common
{
    public partial class DefaultVariantSystem : DefaultVariantSystemBase
    {
        protected override void RegisterDefaultVariants(Dictionary<ComponentType, Rule> defaultVariants)
        {
            defaultVariants.Add(typeof(LocalTransform), Rule.ForAll(typeof(TransformDefaultVariant)));
            defaultVariants.Add(typeof(KinematicCharacterBody), Rule.ForAll(typeof(KinematicCharacterBody_GhostVariant)));
        }
    }

    [GhostComponentVariation(typeof(KinematicCharacterBody))]
    [GhostComponent]
    public struct KinematicCharacterBody_GhostVariant
    {
        [GhostField]
        public float3 RelativeVelocity;
        [GhostField]
        public bool IsGrounded;
    }

    // Character interpolation must be Client-only, because it would prevent proper LocalToWorld updates on server otherwise
    [GhostComponentVariation(typeof(CharacterInterpolation))]
    [GhostComponent(PrefabType = GhostPrefabType.PredictedClient)]
    public struct CharacterInterpolation_GhostVariant
    {
    }

    [GhostComponentVariation(typeof(LocalTransform))]
    [GhostComponent]
    public struct LocalTransform_Character
    {
        [GhostField(Smoothing = SmoothingAction.InterpolateAndExtrapolate)]
        public float3 Position;
    }
}
