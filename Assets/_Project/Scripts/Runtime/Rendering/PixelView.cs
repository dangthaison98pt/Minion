using Unity.Mathematics;
using UnityEngine;

namespace HordeLab
{
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

        Camera screenCam;
        int lastW, lastH, lastScale;
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
            screenCam.clearFlags = CameraClearFlags.Nothing;   // ảnh độ phân giải thấp luôn phủ kín màn hình → khỏi xoá
            screenCam.backgroundColor = Color.black;
            screenCam.depth = 10;
            screenCam.allowMSAA = false;
            screenCam.allowHDR = false;

            Scale = MinScale = math.max(1, (int)math.floor(math.min(Screen.width / (float)Cfg.GroundW, Screen.height / (float)Cfg.GroundH)));
            Center = 0f;
            EnsureTarget();
            ApplyCamera();
        }

        void EnsureTarget()
        {
            if (Target != null && lastW == Screen.width && lastH == Screen.height && lastScale == Scale) return;
            lastW = Screen.width;
            lastH = Screen.height;
            lastScale = Scale;

            int w = Mathf.CeilToInt(Screen.width / (float)Scale);
            int h = Mathf.CeilToInt(Screen.height / (float)Scale);
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
            Scale = scale;
            EnsureTarget();
            // giữ điểm dưới chuột đứng yên khi zoom
            Center = anchorWorld - new float2(anchorScreen.x / Scale - Target.width * 0.5f,
                                              anchorScreen.y / Scale - Target.height * 0.5f) / Cfg.PPU;
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
            Center += move * (80f / Scale) * Time.unscaledDeltaTime;

            if (Input.GetMouseButtonDown(2) && !BlockMouse) { dragging = true; dragStart = Input.mousePosition; dragCenter = Center; }
            if (Input.GetMouseButtonUp(2)) dragging = false;
            if (dragging)
            {
                Vector3 d = Input.mousePosition - dragStart;
                Center = dragCenter - new float2(d.x, d.y) / (Scale * Cfg.PPU);
            }

            ApplyCamera();
        }

        void ApplyCamera()
        {
            // không cho camera trượt ra ngoài chiến trường: ở mức vừa khít thì đứng yên, phóng to mới kéo được
            float2 lim = math.max(0f, new float2(Cfg.HalfW, Cfg.HalfH) - new float2(Target.width, Target.height) / (2f * Cfg.PPU));
            Center = math.clamp(Center, -lim, lim);
            float2 snapped = math.round(Center * Cfg.PPU) / Cfg.PPU; // camera bám lưới pixel → hình không bị rung
            Cam.transform.position = new Vector3(snapped.x, snapped.y, -100f);
        }

        public float2 ScreenToWorld(Vector2 screen)
        {
            Vector3 cp = Cam.transform.position;
            float px = screen.x / Scale, py = screen.y / Scale;
            return new float2(cp.x, cp.y) + new float2(px - Target.width * 0.5f, py - Target.height * 0.5f) / Cfg.PPU;
        }

        public float2 MouseWorld => ScreenToWorld(Input.mousePosition);

        /// <summary>Dán ảnh độ phân giải thấp lên màn hình, căn theo mép dưới-trái.</summary>
        public void DrawToScreen()
        {
            if (Target == null) return;
            float w = Target.width * Scale, h = Target.height * Scale;
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
