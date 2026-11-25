using System;
using OrostrideToolkit.Core;
using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Vòng lặp chính của game, chạy qua UpdateManager (Orostride Toolkit) thay cho Update / LateUpdate riêng của từng MonoBehaviour.
    /// Gom vào 1 chỗ để giữ đúng thứ tự mà mô phỏng cần (UpdateManager không đảm bảo thứ tự giữa các object đăng ký):
    ///   Update:     Game.Tick (sửa dữ liệu khi chưa có job) → Battle.Tick (xếp lịch job mô phỏng)
    ///   LateUpdate: Battle.LateTick (chờ job xong, đẩy lên GPU, vẽ) → GameUi.LateTick (đọc số liệu, vẽ giao diện)
    /// Mỗi bước tự bắt lỗi và ghi log: UpdateManager gặp lỗi sẽ gỡ object khỏi danh sách (game đứng hình vĩnh viễn),
    /// còn ở đây lỗi chỉ bỏ qua bước đó trong frame này, giống Update của Unity.
    /// Object mới cần Update thì cài IUpdatable / ILateUpdatable rồi UpdateManager.Register(this) — chỉ đưa vào đây khi cần đúng thứ tự.
    /// </summary>
    public sealed class GameLoop : IUpdatable, ILateUpdatable
    {
        readonly Game game;
        readonly Battle battle;
        readonly GameUi ui;

        public GameLoop(Game game, Battle battle, GameUi ui)
        {
            this.game = game; this.battle = battle; this.ui = ui;
        }

        /// <summary>Công cụ kiểm tra: đo rác bộ nhớ (byte) từng bước tạo ra: 0 Game, 1 Battle, 2 Battle vẽ, 3 giao diện.</summary>
        public static bool measureAlloc;
        public static readonly long[] allocBytes = new long[4];
        static long mark;
        static void Begin() { if (measureAlloc) mark = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong(); }
        static void End(int step) { if (measureAlloc) { long d = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() - mark; if (d > 0) allocBytes[step] += d; } }

        public void OnUpdate(float dt)
        {
            Begin(); if (game != null && game.isActiveAndEnabled) try { game.Tick(); } catch (Exception e) { Debug.LogException(e, game); } End(0);
            Begin(); if (battle != null && battle.isActiveAndEnabled) try { battle.Tick(); } catch (Exception e) { Debug.LogException(e, battle); } End(1);
        }

        public void OnLateUpdate(float dt)
        {
            Begin(); if (battle != null && battle.isActiveAndEnabled) try { battle.LateTick(); } catch (Exception e) { Debug.LogException(e, battle); } End(2);
            Begin(); if (ui != null && ui.isActiveAndEnabled) try { ui.LateTick(); } catch (Exception e) { Debug.LogException(e, ui); } End(3);
        }
    }
}
