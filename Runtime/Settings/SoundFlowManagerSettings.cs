using System.Collections.Generic;
using SoundFlowSystem.Libraries;
using SoundFlowSystem.Network;
using SoundFlowSystem.Rules.Checkers;
using UnityEngine;

namespace SoundFlowSystem.Settings
{
    public class SoundFlowManagerSettings : MonoBehaviour
    {
        public SoundsCollection[] SoundsCollections;
        public AudioSource BaseAudioSource;
        public BaseNetworkAudioSynchronizer NetworkSynchronizer;
        public List<IPlayConditionChecker> ConditionCheckers;

        public void AddConditionChecker(IPlayConditionChecker checker)
        {
            ConditionCheckers.Add(checker);
        }
    }
}