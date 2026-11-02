using Unity.Netcode;

namespace Ecos
{
    // Attach to a spawned NetworkObject only when implementing the host/client slice.
    // This component does not claim to implement transport/lobby or player replication.
    public sealed class SessionState : NetworkBehaviour
    {
        public readonly NetworkVariable<int> Seed = new NetworkVariable<int>(71319, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<float> WorldTime = new NetworkVariable<float>(.3f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public void SetClock(float hour)
        {
            if (IsServer)
                WorldTime.Value = hour;
        }
    }
}
