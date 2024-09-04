using Deep.Gameplay.Weapons;
using Unity.Entities;
using UnityEngine;

namespace Deep.Authoring.Weapons
{
    public class WeaponVisual1PAuthoring : MonoBehaviour
    {
        public GameObject ShotOrigin;

        public class Baker : Baker<WeaponVisual1PAuthoring>
        {
            public override void Bake(WeaponVisual1PAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new WeaponVisual1P
                {
                    ShotOrigin = GetEntity(authoring.ShotOrigin, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
