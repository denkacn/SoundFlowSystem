using System;
using SoundFlowSystem.Data;
using UnityEngine;

namespace SoundFlowSystem.Managers
{
    public interface ISoundFlowManager
    {
        PlayProcessData Play(string soundKey, AudioSource audioSource = null, Action onFinished = null);
        PlayProcessData PlayInPosition(string soundKey, Vector3 position, AudioSource audioSource = null, Action onFinished = null);
        void Stop(string playProcessId);
        AudioSource Create();
        void PlayNetwork(string soundKey);
        void PlayNetworkInPosition(string soundKey, Vector3 position);
    }
}