using UnityEngine;

namespace OrostrideToolkit.Core
{
    public static class TransformExtensions
    {
        #region Position (World Space)
        public static Transform SetX(this Transform t, float x)
        {
            var p = t.position; p.x = x; t.position = p;
            return t;
        }

        public static Transform SetY(this Transform t, float y)
        {
            var p = t.position; p.y = y; t.position = p;
            return t;
        }

        public static Transform SetZ(this Transform t, float z)
        {
            var p = t.position; p.z = z; t.position = p;
            return t;
        }

        public static Transform SetXY(this Transform t, float x, float y)
        {
            var p = t.position; p.x = x; p.y = y; t.position = p;
            return t;
        }

        public static Transform SetXZ(this Transform t, float x, float z)
        {
            var p = t.position; p.x = x; p.z = z; t.position = p;
            return t;
        }
        #endregion

        #region Local Position
        public static Transform SetLocalX(this Transform t, float x)
        {
            var p = t.localPosition; p.x = x; t.localPosition = p;
            return t;
        }

        public static Transform SetLocalY(this Transform t, float y)
        {
            var p = t.localPosition; p.y = y; t.localPosition = p;
            return t;
        }

        public static Transform SetLocalZ(this Transform t, float z)
        {
            var p = t.localPosition; p.z = z; t.localPosition = p;
            return t;
        }

        public static Transform SetLocalXY(this Transform t, float x, float y)
        {
            var p = t.localPosition; p.x = x; p.y = y; t.localPosition = p;
            return t;
        }
        #endregion

        #region Rotation (Euler Angles)
        public static Transform SetEulerX(this Transform t, float x)
        {
            var e = t.eulerAngles; e.x = x; t.eulerAngles = e;
            return t;
        }

        public static Transform SetEulerY(this Transform t, float y)
        {
            var e = t.eulerAngles; e.y = y; t.eulerAngles = e;
            return t;
        }

        public static Transform SetEulerZ(this Transform t, float z)
        {
            var e = t.eulerAngles; e.z = z; t.eulerAngles = e;
            return t;
        }

        public static Transform SetLocalEulerX(this Transform t, float x)
        {
            var e = t.localEulerAngles; e.x = x; t.localEulerAngles = e;
            return t;
        }

        public static Transform SetLocalEulerY(this Transform t, float y)
        {
            var e = t.localEulerAngles; e.y = y; t.localEulerAngles = e;
            return t;
        }

        public static Transform SetLocalEulerZ(this Transform t, float z)
        {
            var e = t.localEulerAngles; e.z = z; t.localEulerAngles = e;
            return t;
        }
        #endregion

        #region Scale
        public static Transform SetScaleX(this Transform t, float x)
        {
            var s = t.localScale; s.x = x; t.localScale = s;
            return t;
        }

        public static Transform SetScaleY(this Transform t, float y)
        {
            var s = t.localScale; s.y = y; t.localScale = s;
            return t;
        }

        public static Transform SetScaleZ(this Transform t, float z)
        {
            var s = t.localScale; s.z = z; t.localScale = s;
            return t;
        }

        public static Transform SetScaleXY(this Transform t, float x, float y)
        {
            var s = t.localScale; s.x = x; s.y = y; t.localScale = s;
            return t;
        }

        public static Transform SetScaleUniform(this Transform t, float scale)
        {
            t.localScale = new Vector3(scale, scale, scale);
            return t;
        }
        #endregion

        #region Hierarchy & Helpers
        public static void DestroyChildren(this Transform t)
        {
            foreach (Transform child in t)
            {
                Object.Destroy(child.gameObject);
            }
        }

        public static Transform ResetLocal(this Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            return t;
        }
        #endregion

        #region RectTransform (UI Support)
        public static RectTransform SetAnchoredX(this RectTransform rt, float x)
        {
            var p = rt.anchoredPosition; p.x = x; rt.anchoredPosition = p;
            return rt;
        }

        public static RectTransform SetAnchoredY(this RectTransform rt, float y)
        {
            var p = rt.anchoredPosition; p.y = y; rt.anchoredPosition = p;
            return rt;
        }

        public static RectTransform SetSizeWidth(this RectTransform rt, float width)
        {
            var s = rt.sizeDelta; s.x = width; rt.sizeDelta = s;
            return rt;
        }

        public static RectTransform SetSizeHeight(this RectTransform rt, float height)
        {
            var s = rt.sizeDelta; s.y = height; rt.sizeDelta = s;
            return rt;
        }
        #endregion
    }
}