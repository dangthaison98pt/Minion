# Horde Lab — 10.000 lính pixel đánh nhau trong Unity

Prototype minh hoạ cách làm game "vạn quân" kiểu auto-battler: hơn 10 nghìn lính, hàng nghìn mũi tên,
lửa, máu, xác nằm la liệt — chạy mượt nhờ **Job System + Burst + GPU instancing**, không dùng ECS.

## Chạy thử ngay
`Build/HordeLab.exe` — bản build sẵn (toàn màn hình không viền, Alt+Enter để ra cửa sổ).

## Mở project
> ⚠️ Đặt project ở **đường dẫn ngắn** (ví dụ `D:\Unity\HordeLab`). Windows giới hạn đường dẫn 260 ký tự;
> nếu thư mục quá sâu, Burst không tìm thấy file trong `Library/PackageCache` và build sẽ lỗi.

1. Unity Hub → **Add project from disk** → chọn thư mục `HordeLab` (Unity **6000.3.12f1**).
2. Scene `Assets/_Project/Scenes/Battle.unity` tự mở → bấm **Play**.
   (Bấm Play ở scene trống cũng được, `Battle` tự tạo.)
3. Muốn ra file `.exe`: menu **Horde Lab → Build Windows (.exe)** → nằm ở `Build/HordeLab.exe`.

## Hiệu năng đo được (Intel UHD 730 — GPU tích hợp, 1920x1080)
- ~9.000 lính + ~800 mũi tên + ~2.700 hạt: **60 FPS ổn định**, mô phỏng (Jobs + Burst) **~0,7 ms/frame**, GPU 2–5 ms.
- Game giới hạn FPS bằng `Application.targetFrameRate` thay vì VSync (`Battle.targetFps`): GPU Intel hạ xung khi
  dùng VSync nên hay tụt về 40 FPS dù còn dư sức.

## Điều khiển
| Phím | Tác dụng |
|---|---|
| Chuột trái | Mưa tên (400 mũi tên từ tường thành) |
| Chuột phải | Hoả ngục (thiêu xác sống, để lại vết cháy) |
| Lăn chuột / `+` `-` | Phóng to x1 … x6 (luôn pixel-perfect) |
| WASD / giữ chuột giữa | Di chuyển camera |
| Space | Tạm dừng |
| R | Chơi lại |

Xác sống kéo đến **nhanh dần**. Mỗi con lọt qua tường thành trừ 1 máu thành; hết máu là thua.

## Cấu trúc thư mục
```
Assets/
└─ _Project/                     ← toàn bộ nội dung của game (asset bên thứ ba để ngoài thư mục này)
   ├─ Scenes/Battle.unity
   ├─ Resources/Shaders/         ← nạp bằng Resources.Load("Shaders/...") nên phải nằm trong Resources
   └─ Scripts/
      ├─ Runtime/                ← assembly HordeLab.Runtime (Burst, Mathematics)
      │  ├─ Core/                  hằng số, struct dữ liệu
      │  ├─ Simulation/            Burst Job
      │  ├─ Rendering/             camera pixel-perfect, sprite vẽ bằng code
      │  ├─ Gameplay/              vòng lặp trận đánh, kỹ năng, HUD
      │  └─ Tools/                 công cụ test
      └─ Editor/                 ← assembly HordeLab.Editor (chỉ có trong Editor)
Docs/Screenshots/                ← ảnh minh hoạ, không nằm trong Assets
Build/                           ← bản build (.gitignore)
```

## Bản đồ code
| File | Nội dung |
|---|---|
| `Runtime/Core/Config.cs` | Hằng số, struct `Unit` / `Arrow` / `Particle` — mỗi lính chỉ là 1 struct |
| `Runtime/Simulation/SimJobs.cs` | Toàn bộ mô phỏng dạng Burst Job: lưới không gian, AI, đòn đánh, mũi tên, hạt, cái chết |
| `Runtime/Gameplay/Battle.cs` | Cấp phát mảng, xếp lịch Job mỗi frame, kỹ năng, upload lên GPU, HUD |
| `Runtime/Rendering/PixelView.cs` | Camera pixel-perfect: vẽ vào RenderTexture độ phân giải thấp rồi phóng nguyên lần |
| `Runtime/Rendering/PixelArt.cs` | "Vẽ" toàn bộ sprite bằng code (lính 10x10, xác, máu, mặt đất, tường, nghĩa địa) |
| `Resources/Shaders/*.shader` | Shader instancing đọc `StructuredBuffer`, tạo quad từ `SV_VertexID` |
| `Runtime/Tools/AutoShot.cs` | Công cụ test: chạy exe với `-autoshot <thư mục>` để tự chụp màn hình + ghi số liệu |
| `Editor/HordeBuild.cs` | Menu **Horde Lab**: tạo lại scene, build file `.exe` |

## Các mẹo chính (đúng như lý thuyết)
- **Không GameObject cho lính.** `NativeArray<Unit>` + `IJobParallelFor` + `[BurstCompile]`.
- **Lưới không gian 2x2** (counting sort) → tìm địch / đẩy nhau chỉ xét 9 ô quanh mình.
- **Chia việc theo frame:** mỗi lính chỉ quét tìm mục tiêu xa 1 lần / 8 frame.
- **1 lệnh vẽ cho mọi lính:** `Graphics.RenderPrimitives` + shader đọc vị trí từ buffer. Animation chọn khung trong atlas.
- **Mũi tên không có vật lý:** bay theo parabol `h = 4·arc·t·(1-t)`, chỉ kiểm tra trúng ai lúc chạm đất.
- **Xác & máu in vào texture mặt đất** (`Stamp.shader`) rồi xoá lính khỏi mảng → xác chết không tốn gì.
- **Pixel-perfect:** mọi thứ bám lưới 1/10 đơn vị, render vào RenderTexture nhỏ, phóng to bằng lọc Point.

## Thay hình của bạn
Thay các hàm `Build*` trong `PixelArt.cs` bằng `Texture2D` của bạn, giữ đúng bố cục atlas:
- Lính: ô 10x10, 4 cột (khung hình) x 8 hàng (`loại * 2 + trạng thái`, trạng thái 0 = đi, 1 = đánh), quay mặt sang phải.
- Dấu in: ô 20x20, 8 cột x 4 hàng (xem `StampCell` trong `Config.cs`).
