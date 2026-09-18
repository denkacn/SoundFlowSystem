using UnityEngine;

namespace SoundFlowSystem.Managers
{
    // Scene ownership guarantees cleanup on unload, without detached asynchronous waits.
    [AddComponentMenu("")]
    public sealed class SoundFlowRunner : MonoBehaviour
    {
        private SoundFlowManager _manager;
        internal void Initialize(SoundFlowManager manager) => _manager = manager;
        private void Update() => _manager?.Tick();
        private void OnDestroy()
        {
            var manager = _manager;
            _manager = null;
            manager?.Dispose();
        }
    }
}
