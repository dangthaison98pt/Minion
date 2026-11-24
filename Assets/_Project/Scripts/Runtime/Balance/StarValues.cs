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

        public float this[int i] => i == 0 ? star1 : i == 1 ? star2 : i == 2 ? star3 : star4;
        /// <summary>Làm tròn cho dễ đọc: số nhỏ giữ 2 chữ số thập phân, số lớn giữ 1.</summary>
        public static float Nice(float v) => Mathf.Abs(v) < 1f ? Mathf.Round(v * 100f) / 100f : Mathf.Round(v * 10f) / 10f;
        public static StarValues Scaled(float baseValue, float[] mul) => new StarValues(Nice(baseValue * mul[0]), Nice(baseValue * mul[1]), Nice(baseValue * mul[2]), Nice(baseValue * mul[3]));
        public bool IsEmpty => star1 == 0f && star2 == 0f && star3 == 0f && star4 == 0f;
        public void CopyTo(float[] dst) { dst[0] = star1; dst[1] = star2; dst[2] = star3; dst[3] = star4; }
    }
}
