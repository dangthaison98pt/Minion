# Fantasy World War — Tài liệu thiết kế

> Tài liệu mô tả **thiết kế đã chốt** và đang có trong game. Mọi con số là **số tạm**, sẽ cân bằng sau:
> số chung nằm trong `Tuning` ([GameData.cs](../Assets/_Project/Scripts/Runtime/Core/GameData.cs)), chỉ số quân / nhà nằm trong
> ScriptableObject ở `Assets/_Project/Resources/Balance/`. Danh sách đầy đủ lõi, kỹ năng quân và lệnh bot xem [README](../README.md).

## Tóm tắt

Thủ thành bằng thẻ bài, chơi đơn, roguelite. Người chơi chọn 1 trong 6 phe, kéo thẻ để xây công trình vào lưới thành,
rồi **ngồi xem** hàng nghìn lính tự đánh qua **30 đợt**. Không có PvP.

## Vòng lặp chính

Kinh tế và chiến đấu chạy **cùng lúc, theo thời gian thực**. Không tách riêng pha "ngày xây" và "đêm thủ".

1. **Chuẩn bị (giữa các đợt):** kéo thẻ từ tay đặt công trình vào lưới thành, gộp sao, mua / bán / roll ở Cửa hàng.
2. **Trong trận (tự động):**
   - Công trình kinh tế sinh tài nguyên theo chu kỳ.
   - Nhà lính tự gọi 1 tiểu đội khi hết thời gian hồi, **nhưng chỉ khi kho đủ tài nguyên cho lần gọi đó**. Thiếu thì đứng chờ (viền đỏ).
   - Lính tự đánh. Người chơi **chỉ ngồi xem**; vẫn mua / bán / roll / khoá ở Cửa hàng được (như TFT), nhưng không đặt thẻ lên lưới.
   - Tua nhanh x1 / x2 / x4.
3. **Hết đợt:** phần thưởng, đôi khi chọn lõi, đôi khi có sứ giả đổi phe thù địch, rồi sang đợt sau.

Tham khảo cơ chế: Backpack Hero (đồ vật phải đủ tài nguyên mới dùng được), 9 Kings (lưới nhỏ, kéo thẻ để xây), TFT (cửa hàng, lãi, lõi).

## Lưới thành

- Mỗi công trình chiếm đúng **1 ô**. Lúc bắt đầu có **9 ô (3×3)**.
- **Mở rộng đất:** thẻ *Expand Land* mở 1 ô trống **sát cạnh** lưới (4 hướng). Người chơi tự chọn vị trí, nên hình dạng lưới do người chơi quyết. Tối đa trong vùng **7×7**.
- **Gộp sao:** gộp **2 công trình cùng loại, cùng số sao** thành 1 công trình **thêm 1 sao**, tối đa **4 sao**. Gộp được cả bằng cách kéo thẻ từ tay lên công trình. Không cần đứng cạnh nhau. Chỉ gộp lúc chuẩn bị.
- Màu sao theo hạng: 1 sao đồng, 2 sao bạc, 3 sao vàng, 4 sao bạch kim xanh.
- **Mỗi sao tăng gì tuỳ loại nhà:** nhà tài nguyên ra nhiều hơn; nhà lính vẫn gọi 1 tiểu đội nhưng lính mạnh hơn và mỗi lần gọi đắt hơn một chút; nhà buff buff mạnh hơn. Số cụ thể nhập ở từng mốc 1★..4★ trong asset.
- **Thao tác:** kéo công trình sang ô khác để đổi chỗ hoặc gộp; thả thẻ đè lên nhà không gộp được thì **đổi** (nhà cũ về tay, giữ số sao, không bao giờ mất nhà); kéo công trình về tay thành thẻ giữ số sao.

## Tài nguyên

Có 4 tài nguyên trong trận: **Dân, Lương thực, Gỗ, Vàng**. Tài nguyên giữ nguyên giữa các đợt. Vào game có sẵn 30 mỗi loại.

Mỗi loại quân cần tổ hợp tài nguyên riêng cho mỗi lần gọi, nhưng **luôn cần Dân** (bảng Nhà lính).

**Nguyên tắc:** không có nguồn thu nào tỉ lệ với số lính chết (mỗi đợt chết hàng nghìn con, sẽ bùng nổ). Nguồn thu gắn với thời gian, vị trí trên lưới hoặc số công trình.

**Tinh thể năng lượng** là tài nguyên riêng, dùng chung cho mọi phe, **chỉ dùng ngoài trận**: mua thẻ, roll Cửa hàng, mua đất. Bán thẻ / công trình cũng ra Tinh thể.

## Công trình tài nguyên (dùng chung)

Mỗi tài nguyên có 3 nhà, khác nhau ở **cách sinh**.

| Tài nguyên | Nhà | Cách sinh |
|---|---|---|
| Lương thực | **Wheat Field** (Đồng lúa) | Đều đặn, rẻ. +15% cho mỗi Đồng lúa kề bên |
| Lương thực | **Cattle Barn** (Chuồng gia súc) | Chu kỳ chậm, mỗi lần ra nhiều. Cạnh Đồng lúa thì gấp đôi |
| Lương thực | **Hunter's Lodge** (Lều săn) | Chỉ sinh ở ô rìa: ra theo số cạnh giáp vùng chưa mở |
| Gỗ | **Lumber Camp** (Trại đốn gỗ) | Đều đặn, rẻ |
| Gỗ | **Sawmill** (Xưởng cưa) | Không tự sinh gỗ. Trại đốn gỗ vòng quanh nó sinh nhanh hơn 50% |
| Gỗ | **Tree Nursery** (Vườn ươm) | Đầu trận sinh rất ít, tăng dần theo thời gian trận |
| Vàng | **Gold Mine** (Mỏ vàng) | Chu kỳ chậm, mỗi lần ra nhiều |
| Vàng | **Market** (Chợ) | **Đổi Vàng lấy Lương / Gỗ / Dân** (chọn khi đặt xuống). Thiếu Vàng thì đứng chờ. Muốn đổi loại: nhấc lên đặt lại |
| Vàng | **Town Hall** (Toà thị chính) | Vàng theo số Dân đang có. Tiêu Dân để gọi quân thì thu giảm |
| Dân | **Cabin** (Nhà gỗ) | Rẻ, sinh ít |
| Dân | **Residential Block** (Khu dân cư) | Sinh nhiều. Ồn: nhà kề bên chạy chậm hơn 20% |
| Dân | **Tavern** (Quán rượu) | Dân sinh nhanh, mỗi chu kỳ tốn 2 Lương thực |

## Công trình buff (dùng chung)

Mỗi nhà buff có **vùng buff hình dạng riêng**. Rê chuột lên nhà buff thì các ô được buff viền vàng.

| Nhóm | Nhà | Vùng buff | Tác dụng |
|---|---|---|---|
| Kinh tế | **Windmill** (Cối xay gió) | Chữ thập | Đồng lúa sinh nhiều hơn |
| Kinh tế | **Well** (Giếng nước) | Vòng quanh | Mọi nhà ra Lương thực tăng nhẹ |
| Kinh tế | **Ranger Hut** (Nhà kiểm lâm) | Chỉ ô bên dưới | Vườn ươm lớn nhanh gấp đôi |
| Kinh tế | **Mint** (Xưởng đúc tiền) | Chữ thập | Mỏ vàng chu kỳ ngắn hơn |
| Kinh tế | **Trade Road** (Đường buôn) | Cả hàng ngang | Mỗi Chợ trong hàng đổi được nhiều hơn (cùng số Vàng) |
| Kinh tế | **Plaza** (Quảng trường) | Vòng quanh | Nhà ở xung quanh sinh Dân nhanh hơn |
| Quân sự | **Forge** (Xưởng rèn) | Chỉ ô bên dưới | Lính từ nhà quân phía dưới tăng mạnh sát thương |
| Quân sự | **Training Ground** (Bãi tập) | Chữ thập | Nhà quân xung quanh gọi quân nhanh hơn |
| Quân sự | **Supply Depot** (Kho quân nhu) | Chữ thập | Mỗi lần gọi quân tốn ít tài nguyên hơn |
| Quân sự | **War Banner** (Cờ hiệu) | 2 ô mỗi bên trong hàng ngang | Nhà quân trong vùng ra lính có thêm máu |
| Quân sự | **Infirmary** (Bệnh xá) | Vòng quanh | Lính từ nhà xung quanh hồi máu chậm trong trận |

**Quy tắc buff:**

1. Nhà buff phải đáng một ô: đặt đúng chỗ thì mạnh hơn việc đặt thêm một nhà sản xuất.
2. Vùng hẹp thì buff mạnh, vùng rộng thì buff nhẹ.
3. Lên sao thì buff mạnh hơn.
4. Nhiều nhà cùng buff một ô thì cộng dồn, có trần.

## Các phe

Người chơi chọn 1 phe khi bắt đầu lượt chơi. Mỗi phe có: đặc trưng, **5 nhà lính**, **1 nhà kinh tế riêng**, **1 boss** và **1 lõi bí mật**.

| Phe | Điểm mạnh | Điểm yếu |
|---|---|---|
| Người (Human) | Nhà ở sinh Dân nhanh hơn 25% | Thể chất yếu: quân ít máu hơn 10% |
| Tiên (Elf) | Lính tinh nhuệ: sát thương +50%, tầm bắn xa hơn 15% | Mỗi lần gọi chỉ ra **16 lính (4×4)** thay vì 25 (5×5) |
| Người lùn (Dwarf) | Mỏ vàng đào nhanh hơn 30% | Quân đi chậm hơn 20%, nhà lính gọi quân chậm hơn 20% |
| Người hoá thú (Beastfolk) | Nhà ra Lương thực nhanh hơn 30%. Quân **dưới 25% máu** đánh và chạy nhanh hơn 30% | Nhà ở sinh Dân chậm hơn 25% |
| The Void | Quân mạnh hơn 30% (máu và sát thương) | Mỗi lần gọi tốn **gấp đôi Dân** |
| Orc | Quân thêm 20% máu, **không quân nào tốn Vàng** | Quân đánh xa có tầm ngắn hơn 25% |

Trăng tròn của Người hoá thú **đã bỏ**, thay bằng cuồng nộ khi máu thấp.

### Nhà kinh tế riêng của phe

Ra 1 loại tài nguyên như nhà thường, **cộng thêm buff cho nhà lính** quanh nó. Trong bộ thẻ khởi đầu, nhà này thay 1 nhà thường cùng loại tài nguyên.

| Phe | Nhà | Ra | Vùng buff | Buff nhà lính |
|---|---|---|---|---|
| Người | **Cathedral** | Dân | 4 ô chéo | Lính +10% máu |
| Tiên | **Elder Tree** | Gỗ | Vòng quanh | Lính hồi 1 máu / giây |
| Người lùn | **Mithril Mine** | Vàng | Ô trên và dưới | Lính +25% sát thương |
| Người hoá thú | **Totem Pole** | Lương thực | Chữ thập | Lính chạy nhanh hơn 15% |
| The Void | **Void Spire** | Dân | 4 ô chéo | Mỗi lần gọi rẻ hơn 10% |
| Orc | **Scrap Forge** | Gỗ | Chữ thập | Gọi quân nhanh hơn 15% |

## Nhà lính

Nhà lính **khác nhau theo từng phe**. Mỗi phe có **5 nhà lính**, mỗi nhà gọi một loại quân. Phe nào cũng có quân **đánh xa**.
Mỗi lần gọi quân **luôn tốn Dân**. Phe nghiêng về tài nguyên: Người dùng đủ loại, Tiên Gỗ, Người lùn Vàng, Người hoá thú Lương thực, The Void Dân, Orc không dùng Vàng.

Chi phí dưới đây là mỗi lần gọi ở 1 sao, **trước** khi nhân đặc trưng phe (The Void còn nhân đôi Dân). Mỗi quân còn có kỹ năng riêng (đòn lan, xung phong, hồi máu, né…) và 1 lõi chuyên quân, xem README.

### Người
| Nhà | Quân | Vai trò | Mỗi lần gọi |
|---|---|---|---|
| Barracks | Shield Swordsman | Tuyến đầu (chém quét) | Dân 5, Lương 5 |
| Archery Range | Archer | Tầm xa | Dân 5, Gỗ 5 |
| Stable | Cavalry | Cơ động (xung phong) | Dân 6, Lương 6, Vàng 4 |
| Monastery | Monk | Hỗ trợ (hồi máu) | Dân 5, Vàng 5 |
| Siege Workshop | Catapult | Diện rộng | Dân 8, Gỗ 14, Vàng 12 |

### Tiên
| Nhà | Quân | Vai trò | Mỗi lần gọi |
|---|---|---|---|
| Sacred Grove | Treant | Tuyến đầu (rất trâu, đập lan) | Dân 6, Gỗ 12 |
| Bow Academy | Forest Archer | Tầm xa (xa nhất game) | Dân 5, Gỗ 6 |
| Blade Court | Blade Dancer | Xung kích (né đòn) | Dân 5, Lương 6 |
| Moon Temple | Druid | Phép lan, làm chậm địch | Dân 5, Vàng 6 |
| Deer Glade | Deer Rider | Cơ động (bắn khi chạy) | Dân 6, Lương 5, Gỗ 5 |

### Người lùn
| Nhà | Quân | Vai trò | Mỗi lần gọi |
|---|---|---|---|
| Stone Hall | Shield Warrior | Tuyến đầu (rất trâu) | Dân 6, Lương 5, Vàng 3 |
| Crossbow Works | Heavy Crossbow | Tầm xa (sát thương cao, nạp chậm) | Dân 5, Gỗ 5, Vàng 4 |
| War Forge | Axeman | Xung kích (bổ quét) | Dân 5, Vàng 5 |
| Cannon Foundry | Cannon | Diện rộng | Dân 8, Vàng 24 |
| Goat Pen | Goat Rider | Cơ động (chậm hơn kỵ binh, trâu hơn) | Dân 6, Lương 6, Vàng 5 |

### Người hoá thú
| Nhà | Quân | Vai trò | Mỗi lần gọi |
|---|---|---|---|
| Bear Den | Bearfolk | Tuyến đầu (vả quét, hất văng) | Dân 6, Lương 12 |
| Wolf Den | Wolffolk | Xung kích (đánh dồn dập) | Dân 5, Lương 6 |
| Leopard Run | Leopardfolk | Cơ động (vòng ra sau lưng) | Dân 5, Lương 7 |
| Ancestor Stone | Tribal Shaman | Hỗ trợ (tăng sức mạnh bầy) | Dân 5, Vàng 6 |
| Eagle Nest | Eaglefolk | Tầm xa (ném lao từ trên cao) | Dân 5, Lương 5, Gỗ 5 |

### The Void
| Nhà | Quân | Vai trò | Mỗi lần gọi |
|---|---|---|---|
| Rift | Aberration | Tuyến đầu (nổ khi chết) | Dân 8 |
| Tear | Shade | Xung kích (xuyên hàng) | Dân 5, Vàng 5 |
| Eye Tower | Gazer | Tầm xa (làm chậm) | Dân 5, Gỗ 5 |
| Void Altar | Summoner | Hỗ trợ (sinh quân con Voidling) | Dân 8, Vàng 6 |
| Howling Pit | Screamer | Diện rộng (sóng âm) | Dân 9 |

### Orc
| Nhà | Quân | Vai trò | Mỗi lần gọi |
|---|---|---|---|
| War Camp | Orc Grunt | Tuyến đầu (bổ quét) | Dân 5, Lương 6 |
| Spear Pit | Spear Thrower | Tầm xa (giáo nặng) | Dân 5, Gỗ 5 |
| Boar Pen | Boar Rider | Cơ động (xung phong, hất văng) | Dân 6, Lương 9 |
| War Drum | War Drummer | Hỗ trợ (quân gần đánh nhanh hơn) | Dân 5, Lương 3, Gỗ 4 |
| Troll Cave | Troll | Quân to (tự hồi máu, đập lan) | Dân 8, Lương 14 |

Các nhà buff quân sự dùng chung cho mọi phe. Mỗi phe có ít nhất 1 quân đánh lan rẻ từ sớm để dọn quân con.
Quân ta trên sân tối đa **4.000 lính**; quá mức thì nhà lính tạm ngừng gọi.

## Thắng và thua

- Thành có **máu** (300) trong mỗi đợt; mỗi đòn địch đánh trúng tường trừ máu. Máu thành về 0 là **thua đợt đó**.
- Người chơi có **3 trái tim** ở góc màn hình. Mỗi lần thua một đợt mất 1 tim. **Thua 3 lần** là thua cả lượt chơi.
- Thua một đợt (còn tim) thì **sang đợt sau**, không đánh lại, và **vẫn nhận phần thưởng** (trừ thưởng thêm khi thắng và nhà lính phe địch).
- Máu thành **hồi đầy** sau mỗi đợt.
- **Thắng** cả lượt chơi khi vượt qua **30 đợt**.
- **Không có PvP.**

## Độ khó

Chọn ở màn chọn phe, cố định suốt lượt chơi. Chỉ đổi **số quân địch** và **máu boss**; máu và sát thương quân thường như nhau ở mọi mức.

| Mức | Số quân địch | Máu boss | Tinh thể khởi đầu thêm |
|---|---|---|---|
| Easy | 70% | 70% | +5 |
| Normal | 85% | 85% | +2 |
| Hard | 100% | 100% | 0 |

## Kẻ địch

- Kẻ địch là **các phe khác** (cùng 5 phe còn lại, dùng chính quân và đặc trưng của phe đó).
- Trước khi vào lượt chơi, có **3 phe thù địch**. Phe của người chơi **không bao giờ** là phe thù địch.
- **Mỗi đợt chỉ có 1 phe** tấn công, **bốc ngẫu nhiên** trong danh sách thù địch. Người chơi thấy trước phe sắp tấn công và quân của nó.
- Số phe thù địch luôn **ít nhất 2, nhiều nhất 4**.
- Quy mô đợt tăng dần theo số đợt; quân địch mạnh thêm một chút mỗi đợt.

### Sứ giả (sự kiện đổi phe thù địch)

Sau mỗi đợt có xác suất (tạm 25%) **sứ giả** đến, và **người chơi tự chọn phe**:

- **Tuyên chiến:** chọn 1 trong 2 phe trung lập để thành thù địch. Có thể đổi bộ phe hiện ra (tốn 1 Tinh thể).
- **Làm hoà:** chọn 1 phe đang thù địch để thôi thù địch.

Sự kiện hiện ra **khi đánh xong một đợt**, sau màn phần thưởng.

### Trùm

Đợt **6, 12, 18, 24, 30** là đợt trùm: quân đông hơn và có **1 boss khổng lồ của phe đang tấn công** vào trận sau vài giây.

| Phe | Boss | Chiêu chính |
|---|---|---|
| Người | **The Golden Emperor** | Hào quang + chiếu chỉ; chết thì hồi sinh 1 lần với nửa máu |
| Tiên | **The World Tree** | Mọi Treant phe nó hồi máu; cú đấm tạo sóng đẩy lùi |
| Người lùn | **Thor** | Nhát búa phóng sét lan; quân phe nó gần đó đánh nhanh hơn |
| Người hoá thú | **The Berserker** | Hút máu; lần đầu dưới 25% máu hoá **Fenrir** (sói khổng lồ) |
| The Void | **The Broodmother** | Cắn độc; định kỳ đẻ nhện con |
| Orc | **The Warboss** | War Cry: quân phe nó đánh nhanh + hồi máu, địch gần hoảng sợ |

## Phần thưởng sau mỗi đợt

1. **Chọn 1 trong 3 thẻ** (không trùng nhau). Bể thẻ: Mở rộng đất + nhà dùng chung + (**nếu thắng**) nhà lính và nhà kinh tế riêng **của phe vừa bị đánh bại**.
2. **Tinh thể năng lượng:** 5 mỗi đợt, giữ được thành thêm +1, cộng **lãi** +1 cho mỗi 10 Tinh thể đang giữ (tối đa +5, như TFT).

Đây là **cách duy nhất** để có nhà của phe khác. Cửa hàng chỉ bán thẻ phe mình và thẻ dùng chung.

## Cửa hàng và tay bài

- **Cửa hàng kiểu TFT:** 5 ô, giá 3–4 Tinh thể (nhà quân sự đắt hơn), **roll** 1 Tinh thể, **khoá** ô để giữ qua các lần roll, tự làm mới miễn phí sau mỗi đợt. Thẻ Mở rộng đất không ra khi roll, mua riêng bằng nút **Buy Land** (5 Tinh thể).
- **Tay bài tối đa 10 thẻ.** Tay đầy thì không mua thêm được; thẻ thưởng vẫn luôn nhận được.
- **Bán:** kéo thẻ hoặc công trình vào ô *Sell* để lấy Tinh thể (nhà nhiều sao được nhiều hơn theo số nhà đã gộp).
- **Bộ thẻ khởi đầu:** 2 nhà lính đầu tiên của phe + 2 Cabin, Wheat Field, Lumber Camp, Gold Mine (1 lá cùng loại tài nguyên được thay bằng nhà kinh tế riêng của phe). Vào game có sẵn 5 Tinh thể.

## Lõi (Augment)

Giống Augment của TFT: thưởng vĩnh viễn cho cả lượt chơi.

- Chọn **1 trong 3 lõi** sau đợt **1, 6, 12, 18, 24** → tổng cộng 5 lõi mỗi lượt. Mỗi ô lõi có **1 lượt đổi** miễn phí.
- **3 bậc:** **Silver** (cộng tài nguyên, tiện ích nhỏ), **Gold** (lõi mạnh + lõi chuyên quân), **Prismatic** (lõi huyền thoại). Mỗi lần chọn quay ra 1 bậc, càng về sau càng dễ ra bậc cao.
- **Lõi chuyên quân:** mỗi loại quân có 1 lõi đổi cách nó đánh; chỉ ra khi đang có nhà lính của quân đó. Một số lõi là **kỹ năng trưởng đạo** (con giữa đội hình, viền vàng, +50% máu).
- **Lõi huyền thoại** ví dụ: Rồng chặn đường, mưa thiên thạch, nỏ trên tường thành, trại lính thuê, Last Stand.
- **Lõi bí mật:** mỗi phe 1 lõi, gọi **boss của phe mình** đánh cho người chơi. Không ra theo kiểu thường; đạt điều kiện ẩn thì lần chọn lõi kế tiếp chắc chắn có.

| Phe | Lõi bí mật | Điều kiện ẩn |
|---|---|---|
| Người | The Golden Emperor | Giữ thành không mất máu 5 đợt liên tiếp |
| Tiên | The World Tree | Có 4 nhà Gỗ trên lưới |
| Người lùn | Thor | Có 4 nhà Vàng trên lưới |
| Người hoá thú | The Berserker | Thắng tổng cộng 9 đợt |
| The Void | The Broodmother | Diệt tổng cộng 1.500 quân địch |
| Orc | The Warboss | Thắng 5 đợt liên tiếp |

## Lưu và menu

- Menu chính: *Continue* (chơi tiếp bản lưu), *New run*, *Settings*, *Quit*.
- Lượt chơi **tự lưu** sau mỗi thay đổi lúc chuẩn bị và khi xong một đợt. Thoát giữa trận thì lần sau quay lại lúc chuẩn bị của đợt đó.

## Chưa chốt

- Phép hỗ trợ trong trận (để sau).
- Công thức ghép **khác loại** (ví dụ Doanh trại cạnh Xưởng rèn thành Doanh trại Hiệp sĩ): chưa có.
- Toàn bộ con số: chỉ số quân, chi phí, chu kỳ nhà, quy mô đợt, giá cửa hàng, xác suất bậc lõi.
