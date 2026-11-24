using UnityEngine;

namespace HordeLab
{
    /// <summary>Icon chỉ số 9x9 cho tooltip (vẽ phần màu, viền đen tự thêm), để người chơi nhìn lướt thay vì đọc chữ.</summary>
    public static partial class PixelArt
    {
        public const int StatDamage = 0, StatRange = 1, StatSpeed = 2, StatTime = 3, StatSplash = 4, StatBonus = 5, StatReroll = 6, StatLock = 7, StatCount = 8;

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

        /// <summary>Biểu tượng 5 phe (15x15, viền tự thêm), dùng thay cho tên phe trên giao diện. Thứ tự = Faction.*</summary>
        public static Texture2D[] FactionEmblems() => new[]
        {
            // Người: khiên xanh, chữ thập trắng, viền vàng
            OutlinedIcon("EmblemHuman", new[]
            {
                "...............", ".aaaaaaaaaaaaa.", ".abbbbbcbbbbba.", ".abbbbbcbbbbba.", ".abbbbbcbbbbba.",
                ".accccccccccca.", ".abbbbbcbbbbba.", ".abbbbbcbbbbba.", "..abbbbcbbbba..", "...abbbcbbba...",
                "....abbcbba....", ".....abcba.....", "......aba......", ".......a.......", "...............",
            }, Gold, Rgb(56, 100, 200), Rgb(240, 240, 246), default),
            // Tiên: chiếc lá
            OutlinedIcon("EmblemElf", new[]
            {
                "...............", "..........aaa..", "........aabbba.", "......aabbbcba.", ".....abbbbcbba.",
                "....abbbbcbbba.", "...abbbbcbbbba.", "...abbbcbbbba..", "..abbbcbbbba...", "..abbcbbbba....",
                "..abcbbbaa.....", "..acbaaa.......", "..dc...........", ".d.............", "...............",
            }, Rgb(54, 116, 50), Rgb(96, 178, 72), Rgb(176, 226, 124), Rgb(120, 80, 46)),
            // Người lùn: búa
            OutlinedIcon("EmblemDwarf", new[]
            {
                "...............", "...............", "..bbbbbbbbbbb..", "..baaaaaaaaaa..", "..baaaaaaaaaa..",
                "..baaaaaaaaaa..", "..baaaaaaaaaa..", "......dcc......", "......dcc......", "......dcc......",
                "......dcc......", "......dcc......", "......dcc......", "......dcc......", "...............",
            }, Rgb(150, 158, 176), Rgb(218, 224, 234), Rgb(170, 116, 62), Rgb(112, 72, 42)),
            // Người hoá thú: dấu chân
            OutlinedIcon("EmblemBeast", new[]
            {
                "...............", "....aa...aa....", "...aaaa.aaaa...", "...aaaa.aaaa...", "....aa...aa....",
                ".aa.........aa.", ".aaa.......aaa.", ".aa...aaa...aa.", ".....aaaaa.....", "....aaaaaaa....",
                "...aaabaaaaa...", "...aabbaaaaa...", "...aaaaaaaaa...", "....aaaaaaa....", "...............",
            }, Rgb(150, 98, 58), Rgb(206, 156, 102), default, default),
            // The Void: con mắt
            OutlinedIcon("EmblemVoid", new[]
            {
                "...............", "...............", "...............", "...............", "....bbbbbbb....",
                "..aaaacccaaaa..", ".aaaaccdbcaaaa.", ".aaaacdddcaaaa.", ".aaaaccdccaaaa.", "..aaaacccaaaa..",
                "....aaaaaaa....", "...............", "...............", "...............", "...............",
            }, Rgb(116, 62, 168), Rgb(240, 90, 240), Rgb(140, 240, 255), Rgb(40, 22, 66)),
        };

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
            // đổi lại (roll): mũi tên vòng
            OutlinedIcon("Reroll", new[]
            {
                "...............", ".........b.....", ".....aaaabb....", "....aaaaabbb...", "...aaa...bb....",
                "..aaa..........", "..aa.......aa..", "..aa.......aa..", "..aa.......aa..", "..aaa.....aaa..",
                "...aaa...aaa...", "....aaaaaaa....", ".....aaaaa.....", "...............", "...............",
            }, Rgb(255, 214, 70), Rgb(255, 236, 140), default, default),
            // khoá ô cửa hàng: ổ khoá
            OutlinedIcon("Lock", new[]
            {
                "...............", "...............", ".....aaaaa.....", "....a.....a....", "....a.....a....",
                "....a.....a....", "...bbbbbbbbb...", "...bbbbbbbbb...", "...bbbbcbbbb...", "...bbbbcbbbb...",
                "...bbbbbbbbb...", "...bbbbbbbbb...", "...............", "...............", "...............",
            }, Rgb(196, 204, 218), Gold, Rgb(60, 44, 30), default),
        };
    }
}
