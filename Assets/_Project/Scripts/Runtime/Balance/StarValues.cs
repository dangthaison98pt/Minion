using UnityEngine;

namespace HordeLab
{
    /// <summary>4 hệ số theo sao (1..4 sao) cho Inspector: 1 dòng gồm 4 ô.</summary>
    [System.Serializable]
    public struct StarValues
    {
        [Tooltip("1 sao")] public float star1;
        [Tooltip("2 sao")] public float star2;
        [Tooltip("3 sao")] public float star3;
        [Tooltip("4 sao")] public float star4;

        public StarValues(float a, float b, float c, float d) { star1 = a; star2 = b; star3 = c; star4 = d; }
        public StarValues(float[] v) : this(v[0], v[1], v[2], v[3]) { }

        public bool IsEmpty => star1 == 0f && star2 == 0f && star3 == 0f && star4 == 0f;
        public void CopyTo(float[] dst) { dst[0] = star1; dst[1] = star2; dst[2] = star3; dst[3] = star4; }
    }
}
