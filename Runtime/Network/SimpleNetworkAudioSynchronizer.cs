using SoundFlowSystem.Managers;
using UnityEngine;

namespace SoundFlowSystem.Network
{
    public class SimpleNetworkAudioSynchronizer : INetworkAudioSynchronizer
    {
        public void Init(ISoundFlowManager soundFlowManager){}

        public void PlayNetwork(string soundKey, Vector3 inPosition){}
    }
}