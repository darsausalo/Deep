using Deep.Gameplay.Players;
using Unity.Entities;
using UnityEngine;

namespace Deep.Authoring.Players
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public GameObject ControlledCharacter;

        public class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Player { ControlledCharacter = GetEntity(authoring.ControlledCharacter, TransformUsageFlags.Dynamic) });
                AddComponent<PlayerCommands>(entity);
            }
        }
    }
}
