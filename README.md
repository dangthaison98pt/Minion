# Horde Lab — thủ thành bằng thẻ bài, vạn quân tự đánh

Game làm theo [Docs/GDD.md](Docs/GDD.md): chọn 1 trong 5 phe, đặt công trình vào lưới thành bằng thẻ bài, rồi ngồi xem
hàng nghìn lính tự đánh qua 30 đợt. Kinh tế và chiến đấu chạy cùng lúc: nhà tài nguyên sinh Dân / Lương thực / Gỗ / Vàng,
nhà lính tự gọi quân mỗi khi hồi xong **và** kho đủ tài nguyên.

Chiến trường 96×54 đơn vị, mặc định phóng x2 trên màn hình 1080p để lính to, dễ nhìn.
Phần mô phỏng vẫn là lõi "vạn quân" cũ: **Job System + Burst + GPU instancing**, không dùng ECS, mỗi lính là 1 struct.

## Chạy thử ngay
`Build/HordeLab.exe` — bản build sẵn (toàn màn hình không viền, Alt+Enter để ra cửa sổ).

## Mở project
> ⚠️ Đặt project ở **đường dẫn ngắn** (ví dụ `D:\Unity\HordeLab`). Windows giới hạn đường dẫn 260 ký tự;
> nếu thư mục quá sâu, Burst không tìm thấy file trong `Library/PackageCache` và build sẽ lỗi.

1. Unity Hub → **Add project from disk** → chọn thư mục `HordeLab` (Unity **6000.3.12f1**).
2. Scene `Assets/_Project/Scenes/Battle.unity` tự mở → bấm **Play**. (Bấm Play ở scene trống cũng được.)
3. Muốn ra file `.exe`: menu **Horde Lab → Build Windows (.exe)** → nằm ở `Build/HordeLab.exe`.

## Cách chơi
0. **Menu chính:** *Continue* chơi tiếp lượt đang lưu, *New run* bắt đầu lượt mới (ghi đè bản lưu), *Quit*.
   Lượt chơi **tự lưu** sau mỗi thay đổi lúc chuẩn bị và khi xong một đợt (file `run.json` trong `Application.persistentDataPath`).
   Thoát giữa trận thì lần sau quay lại lúc chuẩn bị của đợt đó.
1. **Chọn phe.** 3 trong 4 phe còn lại thành kẻ thù; mỗi đợt 1 phe trong số đó tấn công.
2. **Chuẩn bị (giữa các đợt):**
   - Kéo thẻ từ tay thả vào ô trống của lưới thành để xây (lưới bắt đầu 3×3). Hoặc bấm chọn thẻ rồi bấm vào ô.
   - Lăn chuột trên lưới để đổi cỡ ô, giữ chuột phải / giữa để kéo lưới.
   - Kéo thẻ lên nhà cùng loại, cùng sao để **gộp** (tối đa 4 sao). Kéo công trình sang ô khác để gộp hoặc đổi chỗ.
   - **Thẻ trên tay có số sao** (góc trên trái thẻ). Thẻ mua / thưởng là 1 sao; nhà kéo về tay giữ nguyên số sao,
     đặt xuống lại vẫn đủ sao. Thẻ n sao gộp được với nhà cùng loại n sao.
   - **Kéo** thẻ lên công trình **không gộp được** để **đổi**: thẻ lên ô, nhà cũ về tay đúng chỗ thẻ, giữ nguyên số sao
     (viền cam + dòng báo trước). Không bao giờ mất nhà.
     Bấm chọn thẻ rồi bấm vào nhà khác loại thì không đổi gì cả, chỉ cầm nhà đó lên.
   - Thẻ **Mở rộng đất**: thả vào ô sát cạnh lưới để mở thêm ô.
   - **Cửa hàng** (góc phải): mua thẻ bằng Tinh thể năng lượng, có nút làm mới. Nút **Lock** giữ thẻ ở ô đó qua các lần làm mới.
   - **Bán:** kéo thẻ hoặc công trình vào ô *SELL* để lấy Tinh thể (nhà n sao = 2^(n-1) nhà gộp lại nên được ngần ấy lần).
     Công trình nào cũng kéo ngược về tay được (thành thẻ giữ số sao).
   - Rê chuột vào thẻ / công trình để xem chi tiết; nhà buff sẽ tô viền vàng các ô nó tác động.
3. **Bắt đầu đợt** → chỉ ngồi xem. Góc trên ghi *Enemies left*: số địch còn lại / tổng quân cả đợt (tính cả các tiểu đội chưa kéo đến). Nhà lính viền đỏ = đã hồi xong nhưng đang chờ đủ tài nguyên.
4. Hết đợt: chọn 1 trong 3 thẻ + nhận Tinh thể. Thành thất thủ thì mất 1 trong 3 tim nhưng vẫn sang đợt sau và vẫn nhận thưởng.
5. Thắng hoặc thua cả lượt: chọn *New run* hoặc *Main menu*.

| Phím | Tác dụng |
|---|---|
| Esc | Bỏ chọn thẻ, hoặc mở / đóng menu tạm dừng (*Save & quit to menu*, *Abandon run*) |
| Space | Tạm dừng (trong trận) |
| Tab | Tua nhanh x1 → x2 → x4 |
| Lăn chuột trên chiến trường / `+` `-` | Phóng to thêm tới 5 nấc (luôn pixel-perfect) |
| WASD / phím mũi tên / giữ chuột giữa | Di chuyển camera |
| Lăn chuột / giữ chuột phải trên lưới thành | Đổi cỡ ô / kéo lưới |

## Đã làm theo GDD
- **Vòng lặp chính:** chuẩn bị bằng thẻ → trận tự động, kinh tế + chiến đấu thời gian thực, nhà lính chờ khi thiếu tài nguyên.
- **Lưới thành:** 3×3, mỗi nhà 1 ô, thẻ mở ô sát cạnh (hình dạng lưới do người chơi quyết), gộp 2 nhà cùng loại cùng sao, tối đa 4 sao.
- **4 tài nguyên** và đủ **12 nhà tài nguyên** với cách sinh riêng (kề nhau, ô rìa, tăng dần theo thời gian trận, theo số Dân...).
- **11 nhà buff** với 4 hình vùng buff (chữ thập, vòng quanh, ô bên dưới, cả hàng), cộng dồn có trần, lên sao buff mạnh hơn.
- **5 phe × 5 nhà lính = 25 loại quân**, mỗi loại có cơ chế đúng vai trò: xung phong, hồi máu, đạn nổ lan, né đòn, làm chậm,
  vừa chạy vừa bắn, hất văng, đánh vòng ra sau lưng, tăng sức mạnh bầy, nổ khi chết, đi xuyên hàng, sinh quân con, sóng âm.
- **Đặc trưng phe** (bảng "Các phe"), kể cả đợt trăng tròn của Người hoá thú và tiểu đội 4×4 của Tiên.
- **Thắng / thua:** máu thành theo đợt, 3 tim, 30 đợt, đợt 6/12/18/24/30 đánh dấu là đợt trùm.
- **Kẻ địch:** 3 phe thù địch, mỗi đợt bốc 1 phe, sự kiện thêm / bớt phe (luôn 2–4), báo sau khi đánh xong đợt, thấy trước phe sắp tấn công.
- **Phần thưởng:** chọn 1 trong 3 thẻ + Tinh thể năng lượng; cửa hàng có roll. Chỉ ra thẻ phe mình và thẻ dùng chung.

## Những chỗ GDD chưa chốt → đang dùng số tạm
Tất cả nằm trong class `Tuning` và bảng dữ liệu ở [`GameData.cs`](Assets/_Project/Scripts/Runtime/Core/GameData.cs), đổi một chỗ là xong.

| Mục | Đang tạm dùng |
|---|---|
| "Sát cạnh" | 4 hướng (`DiagonalAdjacency = false`) |
| Giới hạn thẻ trên tay | 10 (chỉ chặn việc mua thêm; thẻ thưởng luôn nhận được) |
| Gộp thẳng từ tay | Có (`MergeFromHand`) |
| Gộp phải đứng cạnh nhau | Không |
| Gộp lúc nào | Chỉ giữa các đợt |
| Mỗi sao tăng gì | Nhà tài nguyên: sản lượng ×1/2/4/8. Nhà lính: vẫn 1 tiểu đội mỗi lần gọi, máu + sát thương lính ×1/1.4/1.8/2.2, chi phí mỗi lần gọi ×1/1.5/2/2.5. Nhà buff: độ mạnh ×1/1.5/2/2.5, cộng dồn tối đa +150% (Kho quân nhu giảm chi phí tối đa 60%) |
| Bán thẻ / công trình | 1 Tinh thể × số nhà 1 sao đã gộp vào |
| Giới hạn quân ta | 4.000 lính trên sân; quá mức thì nhà lính tạm ngừng gọi |
| Tinh thể thưởng mỗi đợt | `3 + đợt / 3` |
| Cửa hàng | 4 thẻ, giá 3–5, roll giá 1, roll thay mọi ô không bị khoá, tự làm mới miễn phí sau mỗi đợt |
| Trùm | Chưa thiết kế → tạm là đợt đông hơn 60% và mạnh hơn 25% |
| Phép hỗ trợ trong trận | Chưa có |
| Công thức ghép khác loại | Chưa có |
| Xác suất sự kiện đổi phe thù địch | 25% sau mỗi đợt |
| Bộ thẻ khởi đầu | 2 nhà lính đầu của phe + 2 Nhà gỗ + Đồng lúa + Trại đốn gỗ + Mỏ vàng; kho 30 mỗi loại |
| Tài nguyên giữa các đợt | Giữ nguyên, không reset |
| Máu thành | 300, mỗi đòn địch đánh trúng tường thành trừ 1 |
| Quy mô đợt địch | `5 + 2.2·đợt + 0.16·đợt²` tiểu đội, mạnh thêm 4% mỗi đợt |
| Mọi chỉ số quân, chi phí, chu kỳ nhà | Số khởi điểm, chưa cân bằng |

Ngoài GDD, có thêm vài thứ cho dễ chơi: kéo công trình để **đổi chỗ**, thả thẻ đè lên nhà để **đổi về tay**, thẻ trên tay **giữ số sao**, **tua nhanh** x2/x4, **bán** thẻ / công trình,
**khoá ô** cửa hàng, **tự lưu** lượt chơi.
Tầm bắn và tốc độ đi của mọi quân nhân 0,6 (`RangeScale`, `SpeedScale`) cho vừa chiến trường 96 × 54. Còn một lưới an toàn:
nếu địch đã đến hết mà 150 giây sau trận vẫn chưa ngã ngũ thì tính là địch rút (phòng hai bên kẹt nhau mãi).

## Cấu trúc thư mục
```
Assets/
└─ _Project/                     ← toàn bộ nội dung của game (asset bên thứ ba để ngoài thư mục này)
   ├─ Scenes/Battle.unity
   ├─ Resources/Shaders/         ← nạp bằng Resources.Load("Shaders/...") nên phải nằm trong Resources
   └─ Scripts/
      ├─ Runtime/                ← assembly HordeLab.Runtime (Burst, Mathematics)
      │  ├─ Core/                  hằng số, struct dữ liệu, bảng công trình / quân / phe, số tạm
      │  ├─ Simulation/            Burst Job
      │  ├─ Rendering/             camera pixel-perfect, sprite vẽ bằng code
      │  ├─ Gameplay/              lượt chơi, lưới thành, chiến trường
      │  ├─ UI/                    giao diện uGUI + TextMesh Pro
      │  └─ Tools/                 bot chơi thử, đo hiệu năng
      └─ Editor/                 ← assembly HordeLab.Editor (chỉ có trong Editor)
Assets/TextMesh Pro/              ← tài nguyên mặc định của TextMesh Pro (Unity tạo, đừng sửa)
Docs/GDD.md                      ← tài liệu thiết kế
Docs/Screenshots/                ← ảnh chụp màn hình + atlas sprite xuất ra PNG
Build/                           ← bản build (.gitignore)
```

## Bản đồ code
| File | Nội dung |
|---|---|
| `Runtime/Core/Config.cs` | Hằng số, mã phe / loại quân, struct `Unit` / `Arrow` / `Particle` — mỗi lính chỉ là 1 struct |
| `Runtime/Core/GameData.cs` | `Tuning` (mọi số tạm), bảng 5 phe, 26 loại quân, 49 lá thẻ / công trình |
| `Runtime/Gameplay/Game.cs` | Lượt chơi: menu, lưu / nạp (`SaveData`), pha, tim, đợt, phe thù địch, thẻ trên tay, cửa hàng (khoá ô), bán, phần thưởng, quân địch kéo đến, sandbox nền menu |
| `Runtime/Gameplay/City.cs` | Lưới thành: đặt / gộp / mở ô, tính buff, sinh tài nguyên, nhà lính gọi quân |
| `Runtime/UI/GameUi.cs` | Giao diện uGUI: menu chính, chọn phe, HUD, lưới thành, tay bài, cửa hàng, ô bán, phần thưởng, tooltip, kéo thả |
| `Runtime/UI/UiKit.cs` | Hàm dựng giao diện bằng code (panel, nút, chữ TextMesh Pro, khung gỗ pixel) |
| `Runtime/Gameplay/Battle.cs` | Chiến trường: cấp phát mảng, xếp lịch Job, upload lên GPU. Không biết gì về thẻ bài |
| `Runtime/Simulation/SimJobs.cs` | Toàn bộ mô phỏng dạng Burst Job: lưới không gian, AI từng loại quân, đòn đánh, đạn, hạt, cái chết |
| `Runtime/Rendering/PixelView.cs` | Camera pixel-perfect: vẽ vào RenderTexture độ phân giải thấp rồi phóng nguyên lần |
| `Runtime/Rendering/PixelArt.cs` | "Vẽ" toàn bộ sprite bằng code (26 loại quân, xác, máu, đạn, mặt đất, thành, biểu tượng). `DrawUnit` tự thêm viền đen |
| `Runtime/Rendering/PixelArtTiles.cs` | Tranh ô công trình nhìn từ trên xuống (32×32 / ô, ghép từ nhà, cây, hàng rào...) và khung giao diện gỗ kiểu pixel |
| `Resources/Shaders/*.shader` | Shader instancing đọc `StructuredBuffer`, tạo quad từ `SV_VertexID` |
| `Runtime/Tools/AutoShot.cs` | Bot chơi thử + chụp màn hình: `-autoshot <thư mục> [-faction 0..4] [-waves N] [-stress 20000] [-savetest]` |
| `Runtime/Tools/Benchmark.cs` | Đo chi phí từng job, seed + dt cố định: `-batchmode -bench <file.txt> [-stress 20000]` |
| `Editor/HordeBuild.cs` | Menu **Horde Lab**: tạo lại scene, xuất atlas sprite ra PNG, build file `.exe` |

## Thêm / sửa nội dung
- **Chỉnh số:** `Tuning` và các hàm `BuildUnits` / `BuildBuildings` trong `GameData.cs`.
- **Thêm cơ chế cho quân:** thêm trường vào `UnitStats` (Config.cs) rồi dùng trong `ThinkMoveJob` / `ResolveJob` (SimJobs.cs).
- **Thêm nhà buff:** thêm mã vào `B`, 1 dòng trong `BuildBuildings`, 1 `case` trong `City.Recompute`.
- **Thay hình:** thay các hàm `Draw*` trong `PixelArt.cs`. Atlas lính: ô 16×16, 4 cột (khung hình) × 52 hàng
  (`loại * 2 + trạng thái`, 0 = đi, 1 = đánh), quay mặt sang phải; lính bộ vẽ 10×10 ở giữa ô.
  Không tự gọi `AddOutline`: `DrawUnit` tự thêm viền (lính bộ được chừa sẵn 1 pixel mỗi bên). Quân to 16×16 phải
  **chừa trống hàng / cột sát mép** cho viền, không thì chỗ đó bị hở viền.
- **Thay tranh công trình:** mỗi công trình là 1 `case` trong `DrawTile` (PixelArtTiles.cs), vẽ trên ô 32×32.
  Menu **Horde Lab → Xuất atlas sprite ra PNG** để xem nhanh (`Docs/Screenshots/atlas_*.png`).

## Kiểm tra tự động
```
Build\HordeLab.exe -autoshot ketqua -faction 0 -waves 5
```
Bot chọn phe, đặt thẻ, mua đồ, đánh 5 đợt, chụp màn hình từng pha và ghi kết quả từng đợt vào `ketqua\stats.txt`.
Thêm `-batchmode` để chạy ngầm, nhanh hết mức (không chụp hình). Bot mặc định không ghi file lưu, để khỏi đè lượt chơi thật;
thêm `-savetest` để thử cả phần lưu / nạp (ghi vào `run_autotest.json`) và đổi nhà / thẻ có sao (`SWAP TEST` trong `stats.txt`).

```
Build\HordeLab.exe -batchmode -bench ketqua.txt -stress 20000
```
Đo riêng phần mô phỏng: seed và dt cố định, 3 lượt × 1200 frame, ghi thời gian từng job.

> Chỉ số "mô phỏng" trên màn hình đo từ lúc xếp lịch job đến lúc `Complete()`, tức gồm cả thời gian chờ luồng worker,
> nên thường cao hơn số của `-bench`. Muốn so sánh trước / sau khi tối ưu thì dùng `-bench`.

## Các mẹo hiệu năng
- **Không GameObject cho lính.** `NativeArray<Unit>` + `IJobParallelFor` + `[BurstCompile]`.
- **Lưới không gian 2×2** (counting sort) → tìm địch / đẩy nhau / vùng hồi máu chỉ xét 9 ô quanh mình.
- **Chia việc theo frame:** mỗi lính chỉ quét tìm mục tiêu xa 1 lần / 8 frame.
- **1 lệnh vẽ cho mọi lính:** `Graphics.RenderPrimitives` + shader đọc vị trí từ buffer. Animation chọn khung trong atlas.
- **Đạn không có vật lý:** bay theo parabol `h = 4·arc·t·(1-t)`, chỉ kiểm tra trúng ai lúc chạm đất.
- **Xác & máu in vào texture mặt đất** (`Stamp.shader`) rồi xoá lính khỏi mảng → xác chết không tốn gì.
- **Pixel-perfect:** mọi thứ bám lưới 1/10 đơn vị, render vào RenderTexture nhỏ, phóng to bằng lọc Point.
