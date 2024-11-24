using UnityEngine;

namespace SoundFlowSystem.Pools
{
    public interface IAudioSourcePool
    {
        string Id { get; }
        AudioSource Get();
        AudioSource CreateAudioSource();
    }
}