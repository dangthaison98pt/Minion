using Unity.Mathematics;
using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Khung hiển thị của game (pixel màn hình). Màn rộng từ 16:9 trở lên: cả màn hình. Màn hẹp / cao hơn 16:9
    /// (màn dọc, 16:10, 4:3): một khung 16:9 nằm giữa, trên dưới để viền tối → bố cục y hệt màn ngang, không bị hở giữa.
    /// </summary>
    public static class ScreenFrame
    {
        public static int W => Screen.width;
        public static int H => Mathf.Min(Screen.height, Mathf.RoundToInt(Screen.width * 9f / 16f));
        /// <summary>Mép dưới của khung (pixel từ mép dưới màn hình).</summary>
        public static int Y0 => (Screen.height - H) / 2;
        public static int Top => Y0 + H;
        /// <summary>Toạ độ màn hình → toạ độ trong khung (gốc dưới-trái của khung).</summary>
        public static Vector2 ToFrame(Vector2 p) => new Vector2(p.x, p.y - Y0);
    }

    /// <summary>
    /// Camera pixel-perfect: vẽ chiến trường vào 1 RenderTexture độ phân giải thấp (1 pixel nghệ thuật = 1 texel),
    /// rồi phóng to NGUYÊN LẦN (x1, x2, x3...) lên màn hình với lọc Point → mọi thứ, kể cả mũi tên xoay
    /// và hạt lửa, đều ra chất pixel.
    /// </summary>
    public class PixelView
    {
        public Camera Cam { get; private set; }
        public RenderTexture Target { get; private set; }
        public int Scale = 1;
        public bool BlockMouse;    // chuột đang ở trên bảng giao diện: lăn chuột / kéo là của giao diện, không phải của camera
        public int MinScale = 1;   // mức phóng vừa khít cả chiến trường vào màn hình
        public float2 Center;
        /// <summary>Chiều cao (pixel màn hình) của thanh giao diện phía trên: chiến trường chỉ vẽ trong phần còn lại bên dưới.</summary>
        public float TopInsetPx;
        /// <summary>
        /// Hệ số phóng thật lên màn hình = mức zoom nguyên × hệ số vừa khít. Ở 1080p vừa đúng x2 (hệ số 1 → pixel đều);
        /// cửa sổ cỡ lẻ (1600x900...) thì phóng lẻ cho chiến trường lấp đầy thay vì tụt xuống x1 trống 40% màn hình.
        /// </summary>
        public float Eff => Scale * fitK;
        public Vector3 CamPos => Cam.transform.position;
        float fitK = 1f;

        Camera screenCam;
        int lastW, lastH, lastScale, lastInset;
        bool dragging;
        Vector3 dragStart;
        float2 dragCenter;

        public void Create(Transform parent)
        {
            Cam = new GameObject("Pixel Camera").AddComponent<Camera>();
            Cam.transform.SetParent(parent, false);
            Cam.orthographic = true;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.07f, 0.06f, 0.08f);
            Cam.nearClipPlane = 0.1f;
            Cam.farClipPlane = 300f;
            Cam.allowMSAA = false;
            Cam.allowHDR = false;
            Cam.useOcclusionCulling = false;
            Cam.depth = 0;

            // Camera này không vẽ gì, chỉ để màn hình có 1 camera; ảnh thật được dán lên bằng OnGUI.
            screenCam = new GameObject("Screen Camera").AddComponent<Camera>();
            screenCam.transform.SetParent(parent, false);
            screenCam.cullingMask = 0;
            screenCam.clearFlags = CameraClearFlags.SolidColor; // ảnh chiến trường chỉ phủ phần bản đồ → phần còn lại xoá thành nền tối
            screenCam.backgroundColor = new Color(0.06f, 0.05f, 0.07f);
            screenCam.depth = 10;
            screenCam.allowMSAA = false;
            screenCam.allowHDR = false;

            ComputeFit();
            Scale = MinScale;
            Center = 0f;
            EnsureTarget();
            ApplyCamera();
        }

        /// <summary>Mức phóng lớn nhất mà cả chiến trường vẫn lọt vùng hiển thị (dưới thanh giao diện).</summary>
        void ComputeFit()
        {
            float fit = math.min(ScreenFrame.W / (float)Cfg.GroundW, ViewHeight / (float)Cfg.GroundH);
            MinScale = math.max(1, (int)math.floor(fit + 1e-4f));
            fitK = fit / MinScale;                               // >= 1 khi có dư; < 1 khi cửa sổ quá nhỏ (thu nhỏ cho vừa)
        }
        float ViewHeight => math.max(1f, ScreenFrame.H - TopInsetPx);

        void EnsureTarget()
        {
            int inset = Mathf.RoundToInt(TopInsetPx);
            if (Target != null && lastW == ScreenFrame.W && lastH == ScreenFrame.H && lastScale == Scale && lastInset == inset) return;
            if (lastW != ScreenFrame.W || lastH != ScreenFrame.H || lastInset != inset)
            {
                ComputeFit();                                // đổi cỡ cửa sổ / thanh giao diện: tính lại mức vừa khít
                Scale = math.clamp(Scale, MinScale, MinScale + 5);
            }
            lastW = ScreenFrame.W;
            lastH = ScreenFrame.H;
            lastScale = Scale;
            lastInset = inset;

            int w = Mathf.CeilToInt(ScreenFrame.W / Eff);
            int h = Mathf.CeilToInt(ViewHeight / Eff);
            w += w & 1; // kích thước chẵn → mép pixel trùng lưới 1/PPU
            h += h & 1;

            if (Target != null)
            {
                Cam.targetTexture = null;
                Target.Release();
                Object.Destroy(Target);
            }
            Target = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32)
            {
                name = "PixelTarget",
                filterMode = FilterMode.Point
            };
            Target.Create();
            Cam.targetTexture = Target;
            Cam.orthographicSize = h / (2f * Cfg.PPU);
        }

        public void SetScale(int scale, Vector2 anchorScreen)
        {
            scale = math.clamp(scale, MinScale, MinScale + 5);   // không thu nhỏ quá mức vừa khít chiến trường
            if (scale == Scale) return;
            float2 anchorWorld = ScreenToWorld(anchorScreen);
            anchorScreen = ScreenFrame.ToFrame(anchorScreen);
            Scale = scale;
            EnsureTarget();
            // giữ điểm dưới chuột đứng yên khi zoom
            Center = anchorWorld - new float2(anchorScreen.x / Eff - Target.width * 0.5f,
                                              anchorScreen.y / Eff - Target.height * 0.5f) / Cfg.PPU;
        }

        public void Update()
        {
            float wheel = BlockMouse ? 0f : Input.mouseScrollDelta.y;
            if (wheel > 0.01f) SetScale(Scale + 1, Input.mousePosition);
            else if (wheel < -0.01f) SetScale(Scale - 1, Input.mousePosition);
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)) SetScale(Scale + 1, new Vector2(Screen.width, Screen.height) * 0.5f);
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) SetScale(Scale - 1, new Vector2(Screen.width, Screen.height) * 0.5f);
            EnsureTarget();

            float2 move = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1f;
            Center += move * (80f / Eff) * Time.unscaledDeltaTime;

            if (Input.GetMouseButtonDown(2) && !BlockMouse) { dragging = true; dragStart = Input.mousePosition; dragCenter = Center; }
            if (Input.GetMouseButtonUp(2)) dragging = false;
            if (dragging)
            {
                Vector3 d = Input.mousePosition - dragStart;
                Center = dragCenter - new float2(d.x, d.y) / (Eff * Cfg.PPU);
            }

            ApplyCamera();
        }

        void ApplyCamera()
        {
            // không cho camera trượt ra ngoài chiến trường: ở mức vừa khít thì đứng yên, phóng to mới kéo được
            float2 half = new float2(Target.width, Target.height) / (2f * Cfg.PPU);
            float2 lim = math.max(0f, new float2(Cfg.HalfW, Cfg.HalfH) - half);
            Center = math.clamp(Center, -lim, lim);
            // màn siêu rộng (bản đồ hẹp hơn màn): bám mép trái để phần trong thành luôn nằm dưới bảng bên trái
            if (half.x > Cfg.HalfW) Center.x = half.x - Cfg.HalfW;
            // màn cao (4:3, 5:4): bám ngay dưới thanh trên cùng, phần dư dồn xuống dưới (chỗ cửa hàng)
            if (half.y > Cfg.HalfH) Center.y = Cfg.HalfH - half.y;
            float2 snapped = math.round(Center * Cfg.PPU) / Cfg.PPU; // camera bám lưới pixel → hình không bị rung
            Cam.transform.position = new Vector3(snapped.x, snapped.y, -100f);
        }

        public float2 ScreenToWorld(Vector2 screen)
        {
            screen = ScreenFrame.ToFrame(screen);
            Vector3 cp = Cam.transform.position;
            float px = screen.x / Eff, py = screen.y / Eff;
            return new float2(cp.x, cp.y) + new float2(px - Target.width * 0.5f, py - Target.height * 0.5f) / Cfg.PPU;
        }

        public float2 MouseWorld => ScreenToWorld(Input.mousePosition);

        /// <summary>Dán ảnh độ phân giải thấp lên màn hình, căn theo mép dưới-trái.</summary>
        public void DrawToScreen()
        {
            if (Target == null) return;
            float w = Target.width * Eff, h = Target.height * Eff;
            GUI.DrawTexture(new Rect(0f, Screen.height - h, w, h), Target, ScaleMode.StretchToFill, false);
        }

        public void Dispose()
        {
            if (Target == null) return;
            if (Cam != null) Cam.targetTexture = null;
            Target.Release();
            Object.Destroy(Target);
            Target = null;
        }
    }
}
