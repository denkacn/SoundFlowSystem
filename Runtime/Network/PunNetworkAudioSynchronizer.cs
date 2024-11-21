using UnityEngine;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;

namespace SoundFlowSystem.Network
{
    public class PunNetworkAudioSynchronizer : MonoBehaviourPun, INetworkAudioSynchronizer
    {
        private ISoundFlowManager _soundFlowManager;

        public void Init(ISoundFlowManager soundFlowManager)
        {
            _soundFlowManager = soundFlowManager;
        }
        
        public void PlayNetwork(string soundKey, Vector3 inPosition)
        {
            photonView.RPC(nameof(SoundFlowSendPlay_RPC), RpcTarget.All, soundKey, inPosition);
        }
        
        [PunRPC]
        public void SoundFlowSendPlay_RPC(string soundKey, Vector3 inPosition)
        {
            if (PhotonNetwork.IsMasterClient) return;
            
            _soundFlowManager.PlayInPosition(soundKey, inPosition);
        }
    }
}
#endif