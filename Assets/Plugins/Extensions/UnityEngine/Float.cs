using UnityEngine;

namespace Plugins.Extensions
{
    public static class Float
    {
        public static float Distance(float a, float b) => Mathf.Abs(a - b);
    }
}