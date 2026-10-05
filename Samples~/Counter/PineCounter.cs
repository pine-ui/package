using Pine;
using UnityEngine;
using UI = Pine.Pine;

namespace Pine.Samples
{
    public sealed class PineCounter : MonoBehaviour
    {
        private MountHandle _mount;

        private void OnEnable() => _mount = UI.Mount(PineExample.Build);

        private void OnDisable()
        {
            _mount?.Dispose();
            _mount = null;
        }
    }
}
