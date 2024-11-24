using UnityEngine;

namespace SoundFlowSystem.Data
{
    public class PlayProcessData
    {
        public string Id;
        public SoundData SoundData;
        public AudioSource AudioSource;

        public PlayProcessData(string id, SoundData soundData, AudioSource audioSource)
        {
            Id = id;
            SoundData = soundData;
            AudioSource = audioSource;
        }
    }
}