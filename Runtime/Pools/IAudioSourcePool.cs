using UnityEngine;

namespace SoundFlowSystem.Pools
{
    public interface IAudioSourcePool : System.IDisposable
    {
        string Id { get; }
        AudioSource Get();
        void Release(AudioSource source);
        AudioSource CreateAudioSource();
    }
}
