using Unity.Multiplayer.Playmode;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Scripting;

namespace Deep.Gameplay.GameManagment
{
    [Preserve]
    public class GameBootstrap : ClientServerBootstrap
    {
        private const string DedicatedTag = "Dedicated";
        private const string ClientTag = "Client";

        public override bool Initialize(string defaultWorldName)
        {
#if UNITY_EDITOR
            AutoConnectPort = 7979;
            if (CurrentPlayer.Tag == DedicatedTag)
            {
                CreateServerWorld("ServerWorld");
                Debug.Log("Created dedicated server world");
            }
            else if (CurrentPlayer.Tag == ClientTag)
            {
                CreateClientWorld("ClientWorld");
                Debug.Log("Created client world");
            }
            else
            {
                CreateDefaultClientServerWorlds();
                Debug.Log($"Created default host worlds for player '{CurrentPlayer.Tag}'");
            }
            return true;
#else
            return base.Initialize(defaultWorldName);
#endif
        }
    }
}
