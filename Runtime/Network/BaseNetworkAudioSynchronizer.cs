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
        
        public virtual void Detach(ISoundFlowManager soundFlowManager)
        {
            if (ReferenceEquals(_soundFlowManager, soundFlowManager)) _soundFlowManager = null;
        }

        public abstract void PlayNetwork(string soundKey, Vector3 inPosition);
    }
}
