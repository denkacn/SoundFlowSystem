using Sirenix.OdinInspector;
using SoundFlowSystem.Data;
using UnityEngine;

namespace SoundFlowSystem.Libraries
{
    [CreateAssetMenu(fileName = "SoundsCollection", menuName = "Tools/SoundFlowSystem/SoundsCollection", order = 1)]
    public class SoundsCollection : ScriptableObject
    {
        [Searchable] [SerializeField] private SoundData[] _soundsData;

        public SoundData[] Get() => _soundsData;

        [SerializeField] private string _pathToSave;
        
        [Button]
        private void GenerateConstant()
        {
            SoundsCollectionConstantGenerator.GenerateClassFile(_pathToSave, _soundsData);
        }
    }
}