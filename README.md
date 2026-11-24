# Horde Lab — thủ thành bằng thẻ bài, vạn quân tự đánh

Game làm theo [Docs/GDD.md](Docs/GDD.md): chọn 1 trong 5 phe, đặt công trình vào lưới thành bằng thẻ bài, rồi ngồi xem
hàng nghìn lính tự đánh qua 30 đợt. Kinh tế và chiến đấu chạy cùng lúc: nhà tài nguyên sinh Dân / Lương thực / Gỗ / Vàng,
nhà lính tự gọi quân mỗi khi hồi xong **và** kho đủ tài nguyên.

Chiến trường 96×50,4 đơn vị (960×504 pixel), mặc định phóng x2: ở 1080p vừa khít vùng dưới thanh trên cùng, không chỗ nào bị giao diện che.
**Toàn màn hình** luôn chạy đúng cỡ gốc của màn hình đang chứa game (không kéo giãn ảnh nên chữ không mờ).
**Chế độ cửa sổ** chọn cỡ cố định 16:9 trong **Settings → Resolution** (1280×720, 1600×900, 1920×1080, 2560×1440, 3840×2160 — chỉ tới
cỡ vừa màn, chừa chỗ thanh tiêu đề); cửa sổ **không kéo giãn được**.
Màn hẹp hơn 16:9 (màn dọc, 16:10, 4:3): cả game nằm trong một khung 16:9 giữa màn, trên dưới để viền tối (`ScreenFrame`),
bố cục giống hệt màn ngang.
1080p và 4K phóng đúng x2 / x4 (sắc nét nhất); các cỡ còn lại phóng lẻ (720p x1,33, 900p x1,67, 1440p x2,67).
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
   - Màu sao theo hạng: 1 sao **đồng**, 2 sao **bạc**, 3 sao **vàng**, 4 sao **bạch kim xanh** (trên lưới, thẻ và tooltip).
   - **Thẻ trên tay có số sao** (góc trên trái thẻ). Thẻ mua / thưởng là 1 sao; nhà kéo về tay giữ nguyên số sao,
     đặt xuống lại vẫn đủ sao. Thẻ n sao gộp được với nhà cùng loại n sao.
   - **Kéo** thẻ lên công trình **không gộp được** để **đổi**: thẻ lên ô, nhà cũ về tay đúng chỗ thẻ, giữ nguyên số sao
     (viền cam + dòng báo trước). Không bao giờ mất nhà.
     Bấm chọn thẻ rồi bấm vào nhà khác loại thì không đổi gì cả, chỉ cầm nhà đó lên.
   - Thẻ **Mở rộng đất**: thả vào ô sát cạnh lưới để mở thêm ô, tối đa trong vùng **7×7**. Không ra khi roll cửa hàng;
     mua bằng nút **Buy Land** (luôn có, mua bao nhiêu cũng được) hoặc nhận ở phần thưởng.
   - **Cửa hàng** (kiểu TFT): bấm vào thẻ để mua bằng Tinh thể năng lượng; cột trái có nút **mua đất** và **đổi lại** (chỉ icon + giá);
     ổ khoá ở góc thẻ giữ thẻ qua các lần đổi. Tab **SHOP** trên cửa hàng để thu gọn / mở.
   - **Bán:** kéo thẻ hoặc công trình vào ô *SELL* để lấy Tinh thể (nhà n sao = 2^(n-1) nhà gộp lại nên được ngần ấy lần).
     Công trình nào cũng kéo ngược về tay được (thành thẻ giữ số sao).
   - Rê chuột vào thẻ / công trình để xem chi tiết; nhà buff sẽ tô viền vàng các ô nó tác động.
3. **Bắt đầu đợt** → lính tự đánh. Như TFT, **trong trận vẫn mua / bán / roll / khoá** được ở cửa hàng và bán thẻ trên tay
   (chỉ đặt thẻ lên lưới là phải đợi chuẩn bị). Cửa hàng và bảng **STATS** (quân ta, địch còn, đã diệt, đã mất, thời gian)
   có nút thu gọn / mở. Phe hiển thị bằng **biểu tượng** (rê chuột để xem tên + điểm mạnh / yếu).
   Góc trên ghi *Enemies left*: số địch còn lại / tổng quân cả đợt (tính cả các tiểu đội chưa kéo đến). Nhà lính viền đỏ = đã hồi xong nhưng đang chờ đủ tài nguyên.
4. Hết đợt: chọn 1 trong 3 thẻ + nhận 5 Tinh thể (+1 nếu giữ được thành), cộng **lãi** +1 cho mỗi 10 Tinh thể đang giữ (tối đa +5, như TFT) nên để dành cũng có lợi. Thành thất thủ thì mất 1 trong 3 tim nhưng vẫn sang đợt sau và vẫn nhận thưởng.
   Xong **đợt 1** và **mỗi đợt trùm** (6, 12, 18, 24) thì được **chọn 1 trong 3 lõi** (giống Augment của TFT): thưởng vĩnh viễn cho cả lượt.
   Như TFT, **mỗi ô lõi có 1 lượt đổi** (nút *Reroll*, miễn phí): đổi ô nào ra lõi khác ở ô đó, 2 ô kia giữ nguyên.
   Lõi đang có ghi ở góc dưới bảng bên trái, rê chuột vào để xem chi tiết.
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

## Lõi (Augment)
Chọn 1 trong 3 lõi ngẫu nhiên (không trùng lõi đã có) sau các đợt `Tuning.AugmentWaves` = 1, 6, 12, 18, 24 (đợt 1 + mỗi đợt trùm; đợt 30 là đợt cuối), tổng cộng 5 lõi. Danh sách + số liệu ở
class `Aug` và `GameData.BuildAugments()` trong [`GameData.cs`](Assets/_Project/Scripts/Runtime/Core/GameData.cs); hiệu ứng tìm `Aug.<tên>` trong `City.cs` / `Game.cs`.

| Nhóm | Lõi | Tác dụng (số tạm) |
|---|---|---|
| Kinh tế | Bountiful Harvest / Timber Boom / Gold Rush / Baby Boom | Nhà ra Lương thực / Gỗ / Vàng / Dân sinh thêm 35% |
| Kinh tế | Stockpile | Đầu mỗi đợt +50 mỗi loại tài nguyên |
| Kinh tế | Treasury | +15 Tinh thể ngay |
| Kinh tế | Rich Get Richer | +5 Tinh thể ngay, trần lãi +3 (tối đa +8) |
| Quân sự | Sharpened Steel / Iron Skin | Quân ta +20% sát thương / +25% máu |
| Quân sự | Drill Sergeant | Nhà lính gọi quân nhanh hơn 20% |
| Quân sự | Supply Lines | Mỗi lần gọi quân rẻ hơn 20% |
| Quân sự | Field Medics | Quân ta hồi 1,5 máu / giây |
| Quân sự | Stone Walls | Máu thành +150 (450) |
| Quân sự | Scouts | Đợt địch ít hơn 15% quân |
| Tiện ích | Lucky Dice | Lần roll đầu mỗi lần chuẩn bị miễn phí |
| Tiện ích | Bargain Hunter | Thẻ trong cửa hàng rẻ hơn 1 (tối thiểu 1) |
| Tiện ích | Land Grant | Nhận ngay 2 thẻ Mở rộng đất |
| Tiện ích | Veteran Recruits | Nhận ngay 1 nhà lính 2 sao ngẫu nhiên của phe mình |
| Tiện ích | Master Architect | Nhà buff mạnh hơn 40% |
| Huyền thoại | Dragon's Pact | Mỗi đợt có 1 con rồng khổng lồ (loại quân `UnitType.Dragon`) đi bộ chặn đường, đẩy lính dạt ra, phun lửa lan; vảy giảm 80% sát thương từ tên / nỏ / lao; chết thì 30 giây sau quay lại |
| Huyền thoại | Meteor Shower | Mỗi 6 giây 1 thiên thạch (đạn `Proj.Meteor`: đầu đá cháy + đuôi lửa, kéo vệt lửa / khói) rơi chéo từ trời vào 1 tên địch ngẫu nhiên; nổ lửa lan + vết cháy |
| Huyền thoại | Ballista Towers | Tường thành bắn mũi nỏ to (đạn `Proj.BigBolt`, đuôi lông đỏ) vào tên địch gần tường nhất (tầm 40), khoảng 1 phát / giây |
| Huyền thoại | Mercenary Camp | Mỗi 20 giây 1 đội 4×4 lính thuê ngẫu nhiên của phe khác tới giúp |
| Huyền thoại | Last Stand | Mỗi đợt, lần đầu thành sắp vỡ thì hồi lại 50% máu |

Sức mạnh rồng, thiên thạch, nỏ và lính thuê nhân theo `Tuning.EnemyPower(đợt)` để không bị tụt hậu về cuối game.

### Lõi chuyên quân (25 lõi, nhóm Specialist màu xanh lá)
Mỗi loại quân có 1 lõi đổi cách nó đánh. Chỉ xuất hiện khi đang có **nhà lính của quân đó** trên lưới; ô đầu tiên của
màn chọn lõi nghiêng hẳn về lõi chuyên quân (70%), 2 ô sau 25%. Mã lõi = `Aug.UnitBase + loại quân`, số liệu ở class `Aug`.

- **Trưởng đạo:** chỉ khi có lõi 👑 (kỹ năng trưởng đạo) của quân đó, mỗi đạo gọi ra mới có 1 trưởng đạo (con giữa đội hình):
  **viền vàng**, **+50% máu**. Lõi 👑 chỉ trưởng đạo dùng,
  nên số lần kích hoạt tính theo số đạo, không theo số lính. Trưởng đạo chết thì đạo đó mất kỹ năng.
- **Khống chế** (choáng / trói / sợ hãi) không cộng dồn; hết khống chế thì **miễn khống chế 2 giây** (`CC.Immune`).
- Bỏng không chồng; Phân bào chỉ 1 tầng; Hồi sinh mỗi lính 1 lần, tối đa 10 lần / trưởng đạo.

| Phe | Quân | Lõi | Tác dụng (số tạm) |
|---|---|---|---|
| Người | Kiếm sĩ khiên | Shield Wall | Kháng 40% tên / nỏ / lao |
| Người | Cung thủ | 👑 Arrow Volley | Mỗi 8 giây bắn 1 loạt 7 mũi toả quạt |
| Người | Kỵ binh | Breakthrough | Xung phong đầy đà thì xuyên hàng, đòn đâm quét quanh mục tiêu |
| Người | Tu sĩ | 👑 Resurrection | Lính ta chết gần trưởng đạo: 25% đứng dậy với nửa máu |
| Người | Máy bắn đá | Fire Shot | Đá gây bỏng 4 giây |
| Tiên | Người cây | Take Root | Đứng yên 3 giây: không bị hất, hồi 1 máu/giây, đòn đánh làm chậm |
| Tiên | Cung thủ rừng | Piercing Arrows | Tên xuyên, trúng thêm 1 con phía sau (tổng tối đa 2 con, phát xuyên 70% sát thương) |
| Tiên | Vũ kiếm sư | Blade Dance | Né được thì phản 1 nhát quét |
| Tiên | Pháp sư tự nhiên | 👑 Entangling Vines | Mỗi 6 giây phép kế tiếp trói chân 1,5 giây |
| Tiên | Kỵ sĩ hươu | Running Fire | Chạy vòng thì bắn nhanh gấp đôi (tốc bắn gốc đã giảm 1,5 → 2,2 giây) |
| Người lùn | Chiến binh khiên | Spiked Shields | Phản 30% sát thương cận chiến |
| Người lùn | Lính nỏ nặng | Armor Piercing | x2 sát thương lên quân to / khổng lồ |
| Người lùn | Lính rìu | Berserk | Càng mất máu đánh càng mạnh; dưới 30% máu đánh nhanh gấp đôi |
| Người lùn | Pháo | Cluster Shot | Đạn nổ xong văng 3 tiếng nổ nhỏ |
| Người lùn | Kỵ sĩ dê núi | 👑 Mountain Ram | Mỗi 8 giây húc choáng địch quanh mục tiêu 1 giây |
| Hoá thú | Người gấu | 👑 Terrifying Roar | Mỗi 12 giây gầm: địch quanh mình sợ hãi bỏ chạy 1 giây |
| Hoá thú | Người sói | Pack Hunters | +8% sát thương mỗi sói đứng gần, tối đa +40% |
| Hoá thú | Người báo | Predator | Ưu tiên quân đánh xa; đánh sau lưng x3 sát thương |
| Hoá thú | Pháp sư bộ lạc | Blood Moon | Đợt trăng tròn vùng buff còn hồi máu |
| Hoá thú | Người chim ưng | 👑 Dive Bomb | Mỗi 15 giây phát lao kế tiếp x3 sát thương, nổ lan |
| Void | Kẻ dị dạng | Mitosis | Chết thì tách 2 Voidling |
| Void | Bóng hư | Soul Drain | Hút 20% sát thương thành máu |
| Void | Con mắt | 👑 Paralyzing Gaze | Mỗi 7 giây phát kế tiếp làm tê liệt 1 giây |
| Void | Kẻ triệu hồi | 👑 Void Portal | Mỗi 20 giây gọi 1 Kẻ dị dạng thay vì Voidling (tối đa 3 / đợt) |
| Void | Kẻ gào thét | Resonance | Sóng âm nảy sang 1 cụm địch khác (60% sát thương) |

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
| Tinh thể thưởng mỗi đợt | Cố định 5 như TFT, giữ được thành thêm +1, thua không có (`CrystalReward`, `WinBonus`); vào game có sẵn 5 |
| Lãi Tinh thể | Hết mỗi đợt: +1 cho mỗi 10 Tinh thể đang giữ (tính trước khi cộng thưởng), tối đa +5 như TFT (`InterestStep`, `InterestMax`) |
| Cửa hàng | 5 thẻ, giá 3–4, roll giá 1, roll thay mọi ô không bị khoá, tự làm mới miễn phí sau mỗi đợt; Mở rộng đất bán riêng (nút Buy Land, giá 5) |
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
**khoá ô** cửa hàng, **tự lưu** lượt chơi, **lõi** kiểu TFT.
Tầm bắn và tốc độ đi của mọi quân nhân 0,6 (`RangeScale`, `SpeedScale`) cho vừa chiến trường 96 × 54. Còn một lưới an toàn:
nếu địch đã đến hết mà 150 giây sau trận vẫn chưa ngã ngũ thì tính là địch rút (phòng hai bên kẹt nhau mãi).

## Cấu trúc thư mục
```
Assets/
└─ _Project/                     ← toàn bộ nội dung của game (asset bên thứ ba để ngoài thư mục này)
   ├─ Scenes/Battle.unity
   ├─ Resources/Art/             ← PNG từng quân / công trình để hoạ sĩ sửa (mục "Sửa art")
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
| `Runtime/Core/GameData.cs` | `Tuning` (mọi số tạm), bảng 5 phe, 27 loại quân (kể cả Rồng), 49 lá thẻ / công trình, 24 lõi |
| `Runtime/Gameplay/Game.cs` | Lượt chơi: menu, lưu / nạp (`SaveData`), pha, tim, đợt, phe thù địch, thẻ trên tay, cửa hàng (khoá ô), bán, phần thưởng, quân địch kéo đến, sandbox nền menu |
| `Runtime/Gameplay/City.cs` | Lưới thành: đặt / gộp / mở ô, tính buff, sinh tài nguyên, nhà lính gọi quân |
| `Runtime/UI/GameUi.cs` | Giao diện uGUI: menu chính, chọn phe, HUD, lưới thành, tay bài, cửa hàng, ô bán, phần thưởng, tooltip, kéo thả |
| `Runtime/UI/UiKit.cs` | Hàm dựng giao diện bằng code (panel, nút, chữ TextMesh Pro, khung gỗ pixel) |
| `Runtime/Gameplay/Battle.cs` | Chiến trường: cấp phát mảng, xếp lịch Job, upload lên GPU. Không biết gì về thẻ bài |
| `Runtime/Simulation/SimJobs.cs` | Toàn bộ mô phỏng dạng Burst Job: lưới không gian, AI từng loại quân, đòn đánh, đạn, hạt, cái chết |
| `Runtime/Rendering/PixelView.cs` | Camera pixel-perfect: vẽ vào RenderTexture độ phân giải thấp rồi phóng nguyên lần |
| `Runtime/Rendering/PixelArt.cs` | "Vẽ" toàn bộ sprite bằng code (27 loại quân kể cả Rồng, xác, máu, đạn, mặt đất, thành, biểu tượng). `DrawUnit` tự thêm viền đen |
| `Runtime/Rendering/PixelArtTiles.cs` | Tranh ô công trình nhìn từ trên xuống (32×32 / ô, ghép từ nhà, cây, hàng rào...) và khung giao diện gỗ kiểu pixel |
| `Resources/Shaders/*.shader` | Shader instancing đọc `StructuredBuffer`, tạo quad từ `SV_VertexID` |
| `Runtime/Tools/AutoShot.cs` | Bot chơi thử + chụp màn hình: `-autoshot <thư mục> [-faction 0..4] [-waves N] [-stress 20000] [-savetest]` |
| `Runtime/Tools/Benchmark.cs` | Đo chi phí từng job, seed + dt cố định: `-batchmode -bench <file.txt> [-stress 20000]` |
| `Editor/HordeBuild.cs` | Menu **Horde Lab**: tạo lại scene, xuất atlas sprite ra PNG, build file `.exe` |
| `Editor/ArtTools.cs` | Menu **Xuất art ra PNG để chỉnh sửa** + tự cài đặt import cho PNG trong `Resources/Art` |
| `Runtime/Rendering/ArtFiles.cs` | Đọc PNG từng quân / công trình (có file thì dùng file, thiếu thì vẽ bằng code) |

## Thêm / sửa nội dung
- **Chỉnh số:** `Tuning` và các hàm `BuildUnits` / `BuildBuildings` trong `GameData.cs`.
- **Thêm cơ chế cho quân:** thêm trường vào `UnitStats` (Config.cs) rồi dùng trong `ThinkMoveJob` / `ResolveJob` (SimJobs.cs).
- **Thêm nhà buff:** thêm mã vào `B`, 1 dòng trong `BuildBuildings`, 1 `case` trong `City.Recompute`.
- **Thay hình:** sửa file PNG trong `Assets/_Project/Resources/Art/` (xem mục **Sửa art** bên dưới). Không có file thì game
  dùng hình vẽ bằng code: các hàm `Draw*` trong `PixelArt.cs`. Atlas lính: ô 16×16, 4 cột (khung hình) × 60 hàng
  (`loại * 2 + trạng thái`, 0 = đi, 1 = đánh), quay mặt sang phải; lính bộ vẽ 10×10 ở giữa ô.
  Quân khổng lồ (`UFlag.Giant`, hiện chỉ có Rồng, phải là loại cuối bảng) vẽ 32×32, mỗi khung chiếm 2×2 ô, 8 hàng atlas;
  shader nhận cờ 8 trong `_Units.w` để vẽ quad to gấp đôi.
  Không tự gọi `AddOutline`: `DrawUnit` tự thêm viền (lính bộ được chừa sẵn 1 pixel mỗi bên). Quân to 16×16 phải
  **chừa trống hàng / cột sát mép** cho viền, không thì chỗ đó bị hở viền.
- **Thay tranh công trình:** sửa `Art/Buildings/<Tên>.png`; không có file thì dùng `case` trong `DrawTile` (PixelArtTiles.cs), ô 32×32.
  Menu **Horde Lab → Xuất atlas sprite ra PNG** để xem nhanh (`Docs/Screenshots/atlas_*.png`).

## Sửa art
Mỗi loại quân và mỗi công trình là **1 file PNG** trong `Assets/_Project/Resources/Art/`. Có file thì game dùng nguyên file đó,
**không tự thêm viền** (bạn tự vẽ viền); xoá file thì game quay về hình vẽ bằng code.

| Thư mục | File | Kích thước | Bố cục |
|---|---|---|---|
| `Art/Units/` | `<Tên quân>.png`, ví dụ `ShieldSwordsman.png`, `Dragon.png` | 64×32 (Rồng 128×64) | 4 cột = 4 khung hình, hàng trên = đi, hàng dưới = đánh. Khung 16×16 (Rồng 32×32), quay mặt sang **phải** (quay trái game tự lật) |
| `Art/Buildings/` | `<Tên nhà>.png`, `_Empty.png` (ô trống), `_Unopened.png` (ô chưa mở) | 32×32 | 1 ô |

- **Tên file** = tên trong game bỏ dấu cách và ký tự lạ ("Heavy Crossbow" → `HeavyCrossbow.png`). Tên quân / nhà xem ở `GameData.cs`.
- **Lính bộ** (không phải quân to) chỉ được vẽ trong **12×12 ở giữa** mỗi khung 16×16 (chừa 2 pixel mỗi bên); phần ngoài bị cắt.
  Quân to (kỵ binh, máy bắn đá, Người cây, gấu, rồng...) dùng trọn khung.
- **Khung đánh:** khung 0–1 thường là lấy đà, khung 2–3 là ra đòn (lúc đó đạn / đòn đánh xuất hiện).
- Sai kích thước hoặc không đọc được thì game báo cảnh báo trong Console và dùng hình vẽ bằng code.
- PNG trong `Art/` tự được cài đúng kiểu pixel art khi import (lọc Point, không nén, bật Read/Write) — không cần chỉnh tay.
- Menu **Horde Lab → Xuất art ra PNG để chỉnh sửa** ghi hình vẽ bằng code cho những quân / nhà **chưa có file**
  (file đã có không bao giờ bị ghi đè). Muốn lấy lại bản gốc của 1 file: xoá file đó rồi chạy lại menu.
- Tranh nhà lính có vẽ sẵn lính gác: sửa quân thì nhớ sửa cả tranh nhà, hoặc xoá file nhà rồi xuất lại để lấy lính gác mới.
- Sửa xong bấm **Play** là thấy. Build `.exe` thì file trong `Resources/` tự được đóng gói.

## Kiểm tra tự động
```
Build\HordeLab.exe -autoshot ketqua -faction 0 -waves 5
```
Bot chọn phe, đặt thẻ, mua đồ, đánh 5 đợt, chụp màn hình từng pha và ghi kết quả từng đợt vào `ketqua\stats.txt`.
Thêm `-batchmode` để chạy ngầm, nhanh hết mức (không chụp hình). Bot mặc định không ghi file lưu, để khỏi đè lượt chơi thật;
thêm `-savetest` để thử cả phần lưu / nạp (ghi vào `run_autotest.json`), đổi nhà / thẻ có sao (`SWAP TEST`) và lõi (`AUG TEST`) trong `stats.txt`.
`-unitaugs`: bot có đủ 5 lõi chuyên quân của phe mình; `-allaugs`: bật đủ 25 lõi chuyên quân và đầu mỗi đợt thả 1 đạo của
mọi loại quân, `stats.txt` ghi số lần kỹ năng trưởng đạo / khống chế / hồi sinh / phân bào / tên xuyên / phản đòn.
Thêm cả `-legendary` thì bot có đủ 5 lõi huyền thoại từ đợt 1; `stats.txt` ghi rồng / thiên thạch / nỏ / lính thuê mỗi đợt.

```
Build\HordeLab.exe -batchmode -bench ketqua.txt -stress 20000
```
Đo riêng phần mô phỏng: seed và dt cố định, 3 lượt × 1200 frame, ghi thời gian từng job.

> Chỉ số "mô phỏng" trên màn hình đo từ lúc xếp lịch job đến lúc `Complete()`, tức gồm cả thời gian chờ luồng worker,
> nên thường cao hơn số của `-bench`. Muốn so sánh trước / sau khi tối ưu thì dùng `-bench`.

## Các mẹo hiệu năng
- **Không GameObject cho lính.** `NativeArray<Unit>` + `IJobParallelFor` + `[BurstCompile]`.
- **Vị trí lính = chỗ chân chạm đất.** Shader nâng hình lên `liftPx` pixel (Battle tự đo từ hàng pixel thấp nhất của sprite, kể cả PNG tự vẽ),
  nên đẩy nhau, đánh nhau và thứ tự che nhau đều tính ở chân.
- **Bán kính thân theo cỡ quân** (`Cfg.RadiusSmall / RadiusBig / RadiusGiant` = 0,425 / 0,7 / 1,4): hai lính đẩy nhau khi chân gần hơn tổng bán kính,
  quân to đẩy quân nhỏ mạnh hơn bị đẩy lại; cận chiến chém tới mép thân, đạn trúng trong thân.
- **Lưới không gian 2×2** (counting sort) → tìm địch / đẩy nhau / vùng hồi máu chỉ xét 9 ô quanh mình.
- **Chia việc theo frame:** mỗi lính chỉ quét tìm mục tiêu xa 1 lần / 8 frame.
- **1 lệnh vẽ cho mọi lính:** `Graphics.RenderPrimitives` + shader đọc vị trí từ buffer. Animation chọn khung trong atlas.
- **Đạn không có vật lý:** bay theo parabol `h = 4·arc·t·(1-t)`, chỉ kiểm tra trúng ai lúc chạm đất.
- **Xác & máu in vào texture mặt đất** (`Stamp.shader`) rồi xoá lính khỏi mảng → xác chết không tốn gì.
- **Pixel-perfect:** mọi thứ bám lưới 1/10 đơn vị, render vào RenderTexture nhỏ, phóng to bằng lọc Point.
