using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pine
{
    internal sealed class PineSize : MonoBehaviour
    {
        [SerializeField]
        internal float ExactWidth = float.NaN;

        [SerializeField]
        internal float ExactHeight = float.NaN;
    }
}
