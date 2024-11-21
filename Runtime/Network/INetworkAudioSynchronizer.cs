using SoundFlowSystem.Managers;
using UnityEngine;

namespace SoundFlowSystem.Network
{
    public interface INetworkAudioSynchronizer
    {
        void Init(ISoundFlowManager soundFlowManager);
        void PlayNetwork(string soundKey, Vector3 inPosition);
    }
}