using Deep.Gameplay.Players;
using Unity.Entities;
using UnityEngine;

namespace Deep.Authoring
{
    public class PlayerSpwanerAuthoring : MonoBehaviour
    {
        public GameObject Player;
        public GameObject Character;
        public GameObject Weapon;

        public class Baker : Baker<PlayerSpwanerAuthoring>
        {
            public override void Bake(PlayerSpwanerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PlayerSpwaner
                {
                    Player = GetEntity(authoring.Player, TransformUsageFlags.Dynamic),
                    Character = GetEntity(authoring.Character, TransformUsageFlags.Dynamic),
                    Weapon = GetEntity(authoring.Weapon, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
