using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Pine
{
    internal sealed class MountLifetime : MonoBehaviour
    { internal Scope Scope; private void OnDestroy() => Scope?.Dispose(); }
}
