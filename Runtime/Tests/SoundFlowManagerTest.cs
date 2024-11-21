using Sirenix.OdinInspector;
using SoundFlowSystem.Managers;
using SoundFlowSystem.Settings;
using UnityEngine;

namespace SoundFlowSystem.Tests
{
    public class SoundFlowManagerTest : MonoBehaviour
    {
        [SerializeField] private SoundFlowManagerSettings _settings;

        private ISoundFlowManager _soundFlowManager;
        
        private void Start()
        {
            _soundFlowManager = new SoundFlowManager(_settings);
        }

        [Button]
        private void TestPlaySound()
        {
            _soundFlowManager.Play("test_sfx_play");
        }
        
        [Button]
        private void TestPlaySoundInPosition()
        {
            _soundFlowManager.PlayInPosition("test_sfx_play", new Vector3(10, 10, 10));
        }
    }
}