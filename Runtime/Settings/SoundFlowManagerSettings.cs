using SoundFlowSystem.Libraries;
using SoundFlowSystem.Network;
using UnityEngine;

namespace SoundFlowSystem.Settings
{
    public class SoundFlowManagerSettings : MonoBehaviour
    {
        public SoundsCollection[] SoundsCollections;
        public AudioSource BaseAudioSource;
        public BaseNetworkAudioSynchronizer NetworkSynchronizer;
    }
}