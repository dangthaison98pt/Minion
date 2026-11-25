using UnityEngine;

namespace OrostrideToolkit.Core
{
    public static class VectorExtensions
    {
        #region Vector2
        public static Vector2 WithX(this Vector2 v, float x) => new Vector2(x, v.y);
        public static Vector2 WithY(this Vector2 v, float y) => new Vector2(v.x, y);

        public static Vector2 DirectionTo(this Vector2 a, Vector2 b) => (b - a).normalized;

        public static float SqrDistanceTo(this Vector2 a, Vector2 b) => (a - b).sqrMagnitude;

        public static Vector2 ClampMag(this Vector2 v, float max) => Vector2.ClampMagnitude(v, max);

        public static bool IsZero(this Vector2 v, float epsilon = 0.0001f) => v.sqrMagnitude < (epsilon * epsilon);

        public static Vector2 Abs(this Vector2 v) => new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));

        public static Vector3 ToVector3(this Vector2 v, float z = 0f) => new Vector3(v.x, v.y, z);
        #endregion

        #region Vector3
        public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
        public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

        public static Vector3 WithXY(this Vector3 v, float x, float y) => new Vector3(x, y, v.z);
        public static Vector3 WithXZ(this Vector3 v, float x, float z) => new Vector3(x, v.y, z);

        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0, v.z);

        public static Vector3 DirectionTo(this Vector3 a, Vector3 b) => (b - a).normalized;

        public static float SqrDistanceTo(this Vector3 a, Vector3 b) => (a - b).sqrMagnitude;

        public static Vector3 ClampMag(this Vector3 v, float max) => Vector3.ClampMagnitude(v, max);

        public static bool IsZero(this Vector3 v, float epsilon = 0.0001f) => v.sqrMagnitude < (epsilon * epsilon);

        public static Vector3 Abs(this Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        #endregion

        #region Conversion & Rounding
        public static Vector2Int ToInt(this Vector2 v) => new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
        public static Vector3Int ToInt(this Vector3 v) => new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));

        public static Vector2Int FloorToInt(this Vector2 v) => new Vector2Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
        public static Vector3Int FloorToInt(this Vector3 v) => new Vector3Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y), Mathf.FloorToInt(v.z));
        #endregion

        #region VectorInt Utilities
        public static Vector2Int WithX(this Vector2Int v, int x) => new Vector2Int(x, v.y);
        public static Vector2Int WithY(this Vector2Int v, int y) => new Vector2Int(v.x, y);

        public static Vector3Int WithX(this Vector3Int v, int x) => new Vector3Int(x, v.y, v.z);
        public static Vector3Int WithZ(this Vector3Int v, int z) => new Vector3Int(v.x, v.y, z);
        #endregion
    }
}