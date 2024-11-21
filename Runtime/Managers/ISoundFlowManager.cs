using System;
using UnityEngine;

namespace SoundFlowSystem.Managers
{
    public interface ISoundFlowManager
    {
        void Play(string soundKey, AudioSource audioSource = null, Action onFinished = null);
        void PlayInPosition(string soundKey, Vector3 position, AudioSource audioSource = null, Action onFinished = null);
        void PlayNetwork(string soundKey);
        void PlayNetworkInPosition(string soundKey, Vector3 position);
    }
}