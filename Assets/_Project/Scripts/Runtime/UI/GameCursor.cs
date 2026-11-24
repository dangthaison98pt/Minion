using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Con trỏ chuột hình bàn tay pixel (16x16, phóng nguyên lần theo độ phân giải): chỉ ngón trỏ khi bình thường,
    /// nắm lại khi đang bấm giữ (kéo thẻ, kéo màn hình). Điểm bấm ở đầu ngón trỏ.
    /// </summary>
    public static class GameCursor
    {
        // o viền, l da sáng, s da, d da tối, c bao cổ tay, C bao cổ tay tối, g viền vàng
        static readonly string[] Point =
        {
            "......oo........",
            ".....olso.......",
            ".....olso.......",
            ".....olso.......",
            ".....olsooo.....",
            ".....olsolsoo...",
            ".....olsolsolso.",
            "..oo.olsolsolso.",
            ".olsoolsdlsdlso.",
            ".olsoolsssssssso",
            ".olsssssssssssso",
            "..osssssssssssdo",
            "..odssssssssddo.",
            "...oggggggggo...",
            "...occcccccco...",
            "...oCCCCCCCCo...",
        };
        static readonly string[] Grab =
        {
            "................",
            "................",
            "................",
            "................",
            "....oo.oo.oo....",
            "...olsolsolsoo..",
            "..oolsolsolsolso",
            ".olsdsdsdsdsdlso",
            ".olsssssssssssso",
            ".olddddsssssssso",
            "..osssddssssssdo",
            "..odssssssssddo.",
            "...oddsssssddo..",
            "...oggggggggo...",
            "...occcccccco...",
            "...oCCCCCCCCo...",
        };

        static Texture2D point, grab;
        static int builtSize = -1, current = -1;

        static Color32 ColorOf(char ch)
        {
            switch (ch)
            {
                case 'o': return new Color32(28, 20, 24, 255);
                case 'l': return new Color32(250, 214, 172, 255);
                case 's': return new Color32(226, 176, 132, 255);
                case 'd': return new Color32(178, 124, 92, 255);
                case 'c': return new Color32(132, 88, 52, 255);
                case 'C': return new Color32(92, 60, 36, 255);
                case 'g': return new Color32(236, 186, 70, 255);
                default: return new Color32(0, 0, 0, 0);
            }
        }

        static Texture2D Make(string[] rows, int scale)
        {
            int n = 16 * scale;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    px[y * n + x] = ColorOf(rows[15 - y / scale][x / scale]);   // hàng 0 của texture là đáy
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        /// <summary>Gọi mỗi frame: đổi con trỏ khi trạng thái hoặc độ phân giải đổi.</summary>
        public static void Update(bool grabbing)
        {
            if (Application.isBatchMode) return;
            int scale = Screen.height >= 2000 ? 4 : Screen.height >= 1300 ? 3 : 2;   // 32 / 48 / 64 px
            if (scale != builtSize)
            {
                if (point != null) Object.Destroy(point);
                if (grab != null) Object.Destroy(grab);
                point = Make(Point, scale); grab = Make(Grab, scale);
                builtSize = scale; current = -1;
            }
            int want = grabbing ? 1 : 0;
            if (want == current) return;
            current = want;
            if (grabbing) Cursor.SetCursor(grab, new Vector2(8 * scale, 8 * scale), CursorMode.Auto);
            else Cursor.SetCursor(point, new Vector2(6.5f * scale, 0.5f * scale), CursorMode.Auto);   // đầu ngón trỏ
        }
    }
}
