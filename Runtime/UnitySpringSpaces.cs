using System;
using UnityEngine;

namespace Pine
{
    internal static partial class Core
    {
        private static bool _springSpacesRegistered;

        static partial void ConfigureSpringSpaces()
        {
            if (_springSpacesRegistered)
                return;
            SpringSpaces.Registered[typeof(Vector2)] = UnitySpringSpaces.Vector2;
            SpringSpaces.Registered[typeof(Vector3)] = UnitySpringSpaces.Vector3;
            SpringSpaces.Registered[typeof(Vector4)] = UnitySpringSpaces.Vector4;
            SpringSpaces.Registered[typeof(Color)] = UnitySpringSpaces.Color;
            SpringSpaces.Registered[typeof(Rect)] = UnitySpringSpaces.Rect;
            SpringSpaces.Registered[typeof(Quaternion)] = UnitySpringSpaces.Quaternion;
            SpringSpaces.Registered[typeof(Pose)] = UnitySpringSpaces.Pose;
            _springSpacesRegistered = true;
        }
    }

    /// <summary>Built-in spring mappings for Unity vectors, colors, rectangles, quaternions and poses.</summary>
    public static class UnitySpringSpaces
    {
        /// <summary>Built-in fixed-lane mapping for Vector2.</summary>
        public static readonly SpringSpace<Vector2> Vector2 = new(
            v => new double[] { v.x, v.y },
            a => new Vector2((float)a[0], (float)a[1])
        );

        /// <summary>Built-in fixed-lane mapping for Vector3.</summary>
        public static readonly SpringSpace<Vector3> Vector3 = new(
            v => new double[] { v.x, v.y, v.z },
            a => new Vector3((float)a[0], (float)a[1], (float)a[2])
        );

        /// <summary>Built-in fixed-lane mapping for Vector4.</summary>
        public static readonly SpringSpace<Vector4> Vector4 = new(
            v => new double[] { v.x, v.y, v.z, v.w },
            a => new Vector4((float)a[0], (float)a[1], (float)a[2], (float)a[3])
        );

        /// <summary>Built-in fixed-lane mapping for Color.</summary>
        public static readonly SpringSpace<Color> Color = new(
            v => new double[] { v.r, v.g, v.b, v.a },
            a => new Color(
                Mathf.Clamp01((float)a[0]),
                Mathf.Clamp01((float)a[1]),
                Mathf.Clamp01((float)a[2]),
                Mathf.Clamp01((float)a[3])
            )
        );

        /// <summary>Built-in fixed-lane mapping for Rect.</summary>
        public static readonly SpringSpace<Rect> Rect = new(
            v => new double[] { v.xMin, v.yMin, v.xMax, v.yMax },
            a => UnityEngine.Rect.MinMaxRect((float)a[0], (float)a[1], (float)a[2], (float)a[3])
        );

        /// <summary>Built-in fixed-lane mapping for Quaternion.</summary>
        public static readonly SpringSpace<Quaternion> Quaternion = new(
            v => PackQuaternion(v),
            a => UnpackQuaternion(a, 0)
        );

        /// <summary>Built-in fixed-lane mapping for Pose.</summary>
        public static readonly SpringSpace<Pose> Pose = new(
            v =>
            {
                double[] q = PackQuaternion(v.rotation);
                return new double[]
                {
                    v.position.x,
                    v.position.y,
                    v.position.z,
                    q[0],
                    q[1],
                    q[2],
                    q[3],
                };
            },
            a => new Pose(
                new Vector3((float)a[0], (float)a[1], (float)a[2]),
                UnpackQuaternion(a, 3)
            )
        );

        private static double[] PackQuaternion(Quaternion value)
        {
            value = value.normalized;
            double sign = value.w < 0 ? -1 : 1;
            return new[] { value.x * sign, value.y * sign, value.z * sign, value.w * sign };
        }

        private static Quaternion UnpackQuaternion(double[] a, int offset)
        {
            Quaternion value = new(
                (float)a[offset],
                (float)a[offset + 1],
                (float)a[offset + 2],
                (float)a[offset + 3]
            );
            double length =
                value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w;
            return length < 1e-12 ? UnityEngine.Quaternion.identity : value.normalized;
        }
    }
}
