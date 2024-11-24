using SoundFlowSystem.Managers;
using UnityEngine;

namespace SoundFlowSystem.Network
{
    public abstract class BaseNetworkAudioSynchronizer : MonoBehaviour
    {
        protected ISoundFlowManager _soundFlowManager;

        public virtual void Init(ISoundFlowManager soundFlowManager)
        {
            _soundFlowManager = soundFlowManager;
        }
        
        public abstract void PlayNetwork(string soundKey, Vector3 inPosition);
    }
}