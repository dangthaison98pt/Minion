using UnityEngine;

namespace HordeLab
{
    /// <summary>Icon game "Fantasy World War": vẽ bằng code như mọi hình khác (32x32, phóng nguyên lần ra các cỡ).</summary>
    public static partial class PixelArt
    {
        public const int IconPx = 32;

        /// <summary>
        /// Huy hiệu: kiếm bạc và rìu chiến bắt chéo sau một tấm khiên đỏ viền vàng có vương miện,
        /// trên nền ô vuông bo góc xanh đêm, viền vàng, vài tàn lửa bay.
        /// </summary>
        public static Canvas DrawAppIcon()
        {
            var bg = new Canvas(IconPx, IconPx);
            Color32 top = Rgb(30, 34, 70), bottom = Rgb(66, 34, 52), rim = Rgb(214, 164, 64), rimD = Rgb(130, 90, 36);
            for (int y = 0; y < IconPx; y++)
                for (int x = 0; x < IconPx; x++)
                {
                    // bo góc bán kính 3
                    int cx = x < 3 ? 3 - x : x > IconPx - 4 ? x - (IconPx - 4) : 0, cy = y < 3 ? 3 - y : y > IconPx - 4 ? y - (IconPx - 4) : 0;
                    if (cx * cx + cy * cy > 9) continue;
                    bool edge = x == 0 || y == 0 || x == IconPx - 1 || y == IconPx - 1 || cx * cx + cy * cy > 4;
                    float t = y / (float)(IconPx - 1);
                    var c = Color32.Lerp(top, bottom, t);
                    if (((x * 3 + y * 5) % 11) == 0) c = Color32.Lerp(c, Rgb(90, 70, 110), 0.25f);   // chút hạt cho nền đỡ phẳng
                    bg.Set(x, y, edge ? (y < IconPx / 2 ? rim : rimD) : c);
                }
            // quầng sáng cam sau huy hiệu
            for (int y = 4; y < 28; y++)
                for (int x = 4; x < 28; x++)
                {
                    float dx = (x + 0.5f - 16f) / 11f, dy = (y + 0.5f - 16f) / 11f, d = dx * dx + dy * dy;
                    if (d < 1f) bg.Set(x, y, Color32.Lerp(bg.Get(x, y), Rgb(200, 96, 40), (1f - d) * 0.45f));
                }

            // huy hiệu vẽ riêng để có viền tối bao quanh
            var e = new Canvas(IconPx, IconPx);
            Color32 steel = Rgb(232, 238, 248), steelD = Rgb(140, 152, 176), wood = Rgb(126, 84, 48), woodD = Rgb(84, 54, 30);
            Color32 gold = Rgb(236, 186, 64), goldL = Rgb(255, 230, 130), goldD = Rgb(168, 116, 34), red = Rgb(186, 36, 46), redL = Rgb(226, 70, 70), redD = Rgb(120, 20, 32), blue = Rgb(80, 160, 255);
            // rìu: cán từ dưới phải lên trên trái, lưỡi trăng khuyết ở trên trái
            Line(e, 26, 27, 8, 9, wood); Line(e, 27, 27, 9, 9, woodD);
            FillPoly(e, V(9, 9, 4, 4, 3, 9, 5, 13, 9, 12), 0, (x, y) => x <= 4 ? steel : x <= 6 ? Rgb(196, 206, 222) : steelD);
            e.Set(8, 7, steelD); e.Set(7, 6, steelD);
            // kiếm: lưỡi từ dưới trái lên trên phải, chuôi ở dưới trái
            Line(e, 8, 23, 25, 6, steel); Line(e, 9, 23, 26, 6, steelD); e.Set(26, 5, steel); e.Set(27, 4, steel);
            Line(e, 5, 21, 10, 26, gold); Line(e, 6, 21, 10, 25, goldL);                    // chắn tay
            Line(e, 7, 24, 4, 27, wood); e.Set(3, 28, goldL); e.Set(4, 28, gold);            // chuôi + núm
            // khiên
            FillPoly(e, V(10, 9, 22, 9, 22, 18, 16, 25, 10, 18), 0, (x, y) => y <= 10 ? goldL : gold);
            FillPoly(e, V(11, 11, 21, 11, 21, 18, 16, 23, 11, 18), 0, (x, y) => x <= 12 ? redL : x >= 20 || y >= 20 ? redD : red);
            Line(e, 16, 11, 16, 22, redD);                                                   // đường chia giữa khiên
            // vương miện vàng
            Box(e, 13, 15, 19, 17, gold); Line(e, 13, 17, 19, 17, goldD);
            e.Set(13, 13, goldL); e.Set(13, 14, gold); e.Set(16, 12, goldL); e.Set(16, 13, goldL); e.Set(16, 14, gold); e.Set(19, 13, goldL); e.Set(19, 14, gold);
            e.Set(14, 14, gold); e.Set(18, 14, gold); e.Set(16, 16, blue); e.Set(14, 16, Rgb(230, 60, 70)); e.Set(18, 16, Rgb(230, 60, 70));
            AddOutline(e);
            bg.Blit(e, 0, 0);
            // tàn lửa
            Color32 ember = Rgb(255, 200, 90), ember2 = Rgb(255, 130, 50);
            bg.Set(5, 5, ember); bg.Set(27, 8, ember2); bg.Set(26, 24, ember); bg.Set(23, 27, ember2); bg.Set(4, 17, ember2);
            return bg;
        }

        /// <summary>Icon phóng nguyên lần lên cỡ <paramref name="size"/> (nhỏ hơn 32 thì lấy mẫu điểm).</summary>
        public static Texture2D AppIconTexture(int size)
        {
            var c = DrawAppIcon();
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = c.Px[(y * IconPx / size) * IconPx + x * IconPx / size];
            t.SetPixels32(px);
            t.Apply();
            return t;
        }
    }
}
