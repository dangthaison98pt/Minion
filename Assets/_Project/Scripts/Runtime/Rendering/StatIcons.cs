using UnityEngine;

namespace HordeLab
{
    /// <summary>Icon chỉ số 9x9 cho tooltip (vẽ phần màu, viền đen tự thêm), để người chơi nhìn lướt thay vì đọc chữ.</summary>
    public static partial class PixelArt
    {
        public const int StatDamage = 0, StatRange = 1, StatSpeed = 2, StatTime = 3, StatSplash = 4, StatBonus = 5, StatCount = 6;

        /// <summary>Vẽ từ chuỗi: '.' trống, a/b/c/d = 4 màu. Chừa 1 pixel mép rồi tự thêm viền.</summary>
        static Texture2D OutlinedIcon(string name, string[] rows, Color32 a, Color32 b, Color32 c, Color32 d)
        {
            var cv = new Canvas(rows[0].Length, rows.Length);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char ch = rows[y][x];
                    if (ch == '.') continue;
                    cv.Set(x, y, ch == 'a' ? a : ch == 'b' ? b : ch == 'c' ? c : d);
                }
            AddOutline(cv);
            return cv.ToTexture(name);
        }

        public static Texture2D[] StatIcons() => new[]
        {
            // sát thương: kiếm chéo
            OutlinedIcon("Damage", new[]
            {
                ".........",
                "......aa.",
                ".....aaa.",
                "....aaa..",
                ".b.aaa...",
                "..bba....",
                "..cb.....",
                ".c..b....",
                ".........",
            }, Rgb(220, 226, 236), Gold, Rgb(140, 92, 52), default),
            // tầm đánh: bia
            OutlinedIcon("Range", new[]
            {
                ".........",
                "...aaa...",
                "..abbba..",
                ".abaaaba.",
                ".abacaba.",
                ".abaaaba.",
                "..abbba..",
                "...aaa...",
                ".........",
            }, Rgb(214, 52, 44), Rgb(244, 240, 230), Rgb(255, 214, 70), default),
            // tốc độ: giày + vệt gió
            OutlinedIcon("Speed", new[]
            {
                ".........",
                "....aaa..",
                "....aaa..",
                "d...aaa..",
                ".d..aaab.",
                "dd.aaaaa.",
                "...ccccc.",
                ".........",
                ".........",
            }, Rgb(150, 98, 56), Rgb(190, 136, 84), Rgb(70, 50, 40), Rgb(170, 210, 255)),
            // thời gian: đồng hồ
            OutlinedIcon("Time", new[]
            {
                ".........",
                "...aaa...",
                "..abbba..",
                ".abbcbba.",
                ".abbcbba.",
                ".abbbcba.",
                "..abbba..",
                "...aaa...",
                ".........",
            }, Rgb(196, 156, 82), Rgb(246, 242, 226), Rgb(60, 48, 52), default),
            // đánh lan: vụ nổ
            OutlinedIcon("Splash", new[]
            {
                ".........",
                "....a....",
                ".a..a..a.",
                "..abba...",
                ".abccba..",
                "..abbaa..",
                ".a..a..a.",
                "....a....",
                ".........",
            }, Rgb(250, 126, 32), Rgb(255, 210, 72), Rgb(255, 248, 214), default),
            // bonus: mũi tên lên
            OutlinedIcon("Bonus", new[]
            {
                ".........",
                "....a....",
                "...aaa...",
                "..aaaaa..",
                ".aaabaaa.",
                "...aba...",
                "...aba...",
                "...aaa...",
                ".........",
            }, Rgb(120, 210, 90), Rgb(190, 250, 160), default, default),
        };
    }
}
