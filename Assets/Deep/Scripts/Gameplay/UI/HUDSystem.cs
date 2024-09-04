using Unity.Entities;
using UnityEngine;

namespace Deep.Gameplay.UI
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class HUDSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // TODO: handle UI + HUD
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
