using System.Collections.Generic;
using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Con trỏ chuột hình bàn tay vẽ mịn (không phải pixel): dựng từ các khối bo tròn (khoảng cách có dấu / SDF),
    /// mép khử răng cưa, đổ bóng sáng-tối theo đèn từ trên-trái, viền tối, bóng đổ mềm, bao cổ tay da viền vàng.
    /// Vẽ đúng cỡ con trỏ (khoảng 48px ở 1080p, to dần theo độ phân giải). Bình thường: chỉ ngón trỏ; bấm giữ: nắm tay.
    /// </summary>
    public static class GameCursor
    {
        delegate float Sdf(float x, float y);
        struct Shape { public Sdf f; public Color light, mid, dark; }

        static readonly Color SkinL = C(250, 214, 182), SkinM = C(232, 176, 138), SkinD = C(178, 118, 88);
        static readonly Color LeaL = C(150, 100, 62), LeaM = C(118, 76, 44), LeaD = C(78, 48, 28);
        static readonly Color GoldL = C(255, 230, 140), GoldM = C(232, 182, 70), GoldD = C(164, 112, 34);
        static readonly Color Outline = C(46, 26, 22);
        static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);

        static Texture2D point, grab;
        static int builtSize = -1, current = -1;

        // ---- hình học ----
        static float Capsule(float px, float py, float ax, float ay, float bx, float by, float r)
        {
            float abx = bx - ax, aby = by - ay, apx = px - ax, apy = py - ay;
            float t = Mathf.Clamp01((apx * abx + apy * aby) / (abx * abx + aby * aby + 1e-9f));
            float dx = apx - abx * t, dy = apy - aby * t;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        static float RoundBox(float px, float py, float cx, float cy, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(px - cx) - hx + r, qy = Mathf.Abs(py - cy) - hy + r;
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        static Shape Skin(Sdf f) => new Shape { f = f, light = SkinL, mid = SkinM, dark = SkinD };

        /// <summary>Các khối theo thứ tự vẽ (sau → trước). Toạ độ 0..1, y hướng xuống.</summary>
        static List<Shape> Shapes(bool fist)
        {
            var s = new List<Shape>();
            if (!fist)
            {
                s.Add(Skin((x, y) => RoundBox(x, y, 0.55f, 0.66f, 0.24f, 0.15f, 0.11f)));          // lòng bàn tay
                s.Add(Skin((x, y) => Capsule(x, y, 0.78f, 0.52f, 0.78f, 0.60f, 0.058f)));          // út
                s.Add(Skin((x, y) => Capsule(x, y, 0.655f, 0.47f, 0.655f, 0.57f, 0.066f)));        // áp út
                s.Add(Skin((x, y) => Capsule(x, y, 0.52f, 0.44f, 0.52f, 0.55f, 0.07f)));           // giữa
                s.Add(Skin((x, y) => Capsule(x, y, 0.33f, 0.70f, 0.16f, 0.53f, 0.075f)));          // ngón cái
                s.Add(Skin((x, y) => Capsule(x, y, 0.37f, 0.10f, 0.37f, 0.58f, 0.078f)));          // ngón trỏ
            }
            else
            {
                s.Add(Skin((x, y) => RoundBox(x, y, 0.55f, 0.60f, 0.26f, 0.20f, 0.13f)));          // nắm tay
                float[] xs = { 0.36f, 0.50f, 0.64f, 0.77f };
                for (int i = 0; i < xs.Length; i++)
                {
                    float cx = xs[i], r = 0.072f - 0.004f * i;
                    s.Add(Skin((x, y) => Capsule(x, y, cx, 0.40f, cx, 0.50f, r)));                  // đốt ngón
                }
                s.Add(Skin((x, y) => Capsule(x, y, 0.24f, 0.66f, 0.50f, 0.60f, 0.075f)));          // ngón cái vắt ngang
            }
            // bao cổ tay da + viền vàng, vẽ đè lên cổ tay
            s.Add(new Shape { f = (x, y) => RoundBox(x, y, 0.56f, 0.89f, 0.215f, 0.065f, 0.03f), light = LeaL, mid = LeaM, dark = LeaD });
            s.Add(new Shape { f = (x, y) => RoundBox(x, y, 0.56f, 0.83f, 0.225f, 0.022f, 0.015f), light = GoldL, mid = GoldM, dark = GoldD });
            return s;
        }

        static Texture2D Render(bool fist, int n)
        {
            var shapes = Shapes(fist);
            float px = 1f / n, ow = 0.035f;                 // cỡ 1 pixel, độ dày viền
            const float lx = -0.55f, ly = -0.83f;           // đèn từ trên-trái
            var pixels = new Color[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = (i + 0.5f) / n, y = (j + 0.5f) / n;
                    // bóng đổ mềm phía dưới-phải
                    float du = float.MaxValue;
                    foreach (var sh in shapes) du = Mathf.Min(du, sh.f(x - 0.035f, y - 0.045f));
                    float a = 0.35f * Mathf.Clamp01(0.5f - (du - ow) / (px * 4f));
                    Color c = new Color(0f, 0f, 0f, 0f);
                    foreach (var sh in shapes)
                    {
                        float d = sh.f(x, y);
                        float ao = Mathf.Clamp01(0.5f - (d - ow) / px), af = Mathf.Clamp01(0.5f - d / px);
                        if (ao > 0f) { c = Color.Lerp(c, Outline, ao); a += (1f - a) * ao; }
                        if (af <= 0f) continue;
                        // khối: pháp tuyến hướng về đèn thì sáng, gần mép đậm hơn
                        const float e = 1e-3f;
                        float gx = (sh.f(x + e, y) - sh.f(x - e, y)) / (2f * e), gy = (sh.f(x, y + e) - sh.f(x, y - e)) / (2f * e);
                        float gl = Mathf.Sqrt(gx * gx + gy * gy) + 1e-9f;
                        float ndl = -(gx * lx + gy * ly) / gl;
                        float depth = Mathf.Min(1f, -d / 0.05f);
                        float lit = 0.5f + 0.5f * ndl * (1f - depth);
                        Color col = lit > 0.5f ? Color.Lerp(sh.mid, sh.light, (lit - 0.5f) * 2f) : Color.Lerp(sh.dark, sh.mid, lit * 2f);
                        c = Color.Lerp(c, col, af); a += (1f - a) * af;
                    }
                    c.a = a;
                    pixels[(n - 1 - j) * n + i] = c;          // hàng 0 của texture là đáy
                }
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels(pixels);
            t.Apply();
            return t;
        }

        // ---- Ảnh do hoạ sĩ vẽ: Resources/Art/Cursor/Point.png (bắt buộc), Grab.png (không bắt buộc) ----
        const string PointFile = "Art/Cursor/Point", GrabFile = "Art/Cursor/Grab";
        static Vector2 pointHot, grabHot;

        /// <summary>Co ảnh về cỡ <paramref name="size"/> (cạnh dài), lọc mượt. Ảnh phải đọc được (CursorImport đặt sẵn).</summary>
        static Texture2D Resize(Texture2D src, int size)
        {
            float k = (float)size / Mathf.Max(src.width, src.height);
            int w = Mathf.Max(1, Mathf.RoundToInt(src.width * k)), h = Mathf.Max(1, Mathf.RoundToInt(src.height * k));
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = src.GetPixelBilinear((x + 0.5f) / w, (y + 0.5f) / h);
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        /// <summary>Điểm bấm = pixel đặc cao nhất của hình (đầu ngón trỏ). Toạ độ con trỏ tính từ góc trên-trái.</summary>
        static Vector2 TopHotspot(Texture2D t)
        {
            var px = t.GetPixels32();
            for (int y = t.height - 1; y >= 0; y--)
            {
                int first = -1, last = -1;
                for (int x = 0; x < t.width; x++) if (px[y * t.width + x].a > 128) { if (first < 0) first = x; last = x; }
                if (first >= 0) return new Vector2((first + last) * 0.5f, t.height - 1 - y);
            }
            return Vector2.zero;
        }

        static bool LoadArt(int size)
        {
            var srcPoint = Resources.Load<Texture2D>(PointFile);
            if (srcPoint == null || !srcPoint.isReadable) return false;
            var srcGrab = Resources.Load<Texture2D>(GrabFile);
            point = Resize(srcPoint, size);
            grab = srcGrab != null && srcGrab.isReadable ? Resize(srcGrab, size) : null;
            pointHot = TopHotspot(point);
            grabHot = grab != null ? new Vector2(grab.width * 0.5f, grab.height * 0.5f) : pointHot;
            return true;
        }

        /// <summary>Gọi mỗi frame: đổi con trỏ khi trạng thái hoặc độ phân giải đổi.</summary>
        public static void Update(bool grabbing)
        {
            if (Application.isBatchMode) return;
            int size = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 1080f * 48f / 4f) * 4, 32, 128);   // ~48px ở 1080p
            if (size != builtSize)
            {
                if (point != null) Object.Destroy(point);
                if (grab != null) Object.Destroy(grab);
                if (!LoadArt(size))
                {
                    // chưa có ảnh: dùng hình vẽ bằng code
                    point = Render(false, size); grab = Render(true, size);
                    pointHot = new Vector2(0.37f * size, 0.03f * size); grabHot = new Vector2(0.55f * size, 0.55f * size);
                }
                builtSize = size; current = -1;
            }
            int want = grabbing && grab != null ? 1 : 0;
            if (want == current) return;
            current = want;
            if (want == 1) Cursor.SetCursor(grab, grabHot, CursorMode.Auto);
            else Cursor.SetCursor(point, pointHot, CursorMode.Auto);
        }
    }
}
