# Tài liệu thiết kế game

> Tên game: chưa đặt.
> Tài liệu chỉ ghi những gì **đã chốt**. Phần nào chưa quyết thì để trống, chốt tới đâu ghi tới đó.

## Vòng lặp chính

Kinh tế và chiến đấu chạy **cùng lúc, theo thời gian thực**. Không tách riêng pha "ngày xây" và "đêm thủ".

1. **Giữa các đợt (chuẩn bị):** kéo thẻ bài, đặt công trình vào lưới bên trong thành.
2. **Trong trận (tự động):**
   - Công trình kinh tế (ví dụ Nông trại, Mỏ) sinh tài nguyên theo chu kỳ.
   - Công trình quân sự tự gọi quân khi hết thời gian hồi, **nhưng chỉ khi kho đủ tài nguyên cho lần gọi đó**. Thiếu thì đứng chờ.
   - Lính tự đánh. Người chơi **chỉ ngồi xem** (sau này có thể thêm phép hỗ trợ).

Tham khảo cơ chế: Backpack Hero (đồ vật phải đủ tài nguyên mới dùng được).

**Lưới thành:** mỗi công trình chiếm đúng **1 ô**. Lúc bắt đầu có **9 ô (3×3)**, giống 9 Kings.

**Mở rộng lưới:** có **thẻ mở thêm ô trống**. Người chơi tự chọn vị trí ô mới, miễn là nằm **sát cạnh** lưới đang có. Vì vậy hình dạng lưới do người chơi tự quyết.

**Nâng cấp (gộp):** gộp **2 công trình cùng loại, cùng số sao** thành 1 công trình **thêm 1 sao**. Tối đa **4 sao**. Mỗi sao tăng gì **tuỳ từng loại nhà**.

## Tài nguyên

Có 4 loại: **Dân, Lương thực, Gỗ, Vàng**.

Mỗi loại quân cần tổ hợp tài nguyên riêng cho mỗi lần gọi, nhưng **luôn cần Dân**. Ví dụ:

| Quân | Tài nguyên mỗi lần gọi |
|---|---|
| Kiếm sĩ (cận chiến) | Dân + Lương thực |
| Cung thủ | Dân + Gỗ |

**Nguyên tắc:** không có nguồn thu nào tỉ lệ với số lính chết (mỗi đợt chết hàng nghìn con, sẽ bùng nổ). Nguồn thu gắn với thời gian, vị trí trên lưới hoặc số công trình.

## Công trình tài nguyên

Mỗi tài nguyên có 3 nhà, khác nhau ở **cách sinh**: một nhà cơ bản đều đặn, một nhà mạnh nhờ ghép kề, một nhà có điều kiện. Các con số sẽ cân bằng sau.

| Tài nguyên | Nhà | Cách sinh |
|---|---|---|
| Lương thực | **Đồng lúa** | Đều đặn, rẻ. Các đồng lúa liền nhau sinh thêm một chút |
| Lương thực | **Chuồng gia súc** | Chu kỳ chậm, mỗi lần ra nhiều. Cạnh Đồng lúa thì gấp đôi |
| Lương thực | **Lều săn** | Chỉ sinh khi đặt ở ô rìa (có cạnh giáp vùng chưa mở). Càng nhiều cạnh giáp vùng hoang càng sinh nhiều |
| Gỗ | **Trại đốn gỗ** | Đều đặn, rẻ |
| Gỗ | **Xưởng cưa** | Không tự sinh gỗ. Trại đốn gỗ quanh nó sinh nhanh hơn |
| Gỗ | **Vườn ươm** | Đầu trận sinh rất ít, tăng dần theo thời gian trận |
| Vàng | **Mỏ vàng** | Chu kỳ chậm, mỗi lần ra nhiều |
| Vàng | **Chợ** | Vàng theo số công trình xung quanh |
| Vàng | **Toà thị chính** | Vàng theo số Dân đang có. Tiêu Dân để gọi quân thì thu giảm |
| Dân | **Nhà gỗ** | Rẻ, sinh ít |
| Dân | **Khu dân cư** | Đắt hơn, sinh nhiều hơn. Ồn: làm chậm nhà bên cạnh |
| Dân | **Quán rượu** | Dân sinh nhanh, mỗi lần sinh tốn một ít Lương thực |

## Công trình buff

Mỗi nhà buff có **vùng buff hình dạng riêng**: vòng quanh (8 ô), chữ thập (4 ô), chỉ ô bên dưới, cả hàng ngang. Các con số sẽ cân bằng sau.

| Nhóm | Nhà | Vùng buff | Tác dụng |
|---|---|---|---|
| Kinh tế | **Cối xay gió** | Chữ thập | Đồng lúa sinh nhiều hơn |
| Kinh tế | **Giếng nước** | Vòng quanh | Mọi nhà ra Lương thực tăng nhẹ |
| Kinh tế | **Nhà kiểm lâm** | Chỉ ô bên dưới | Vườn ươm lớn nhanh gấp đôi |
| Kinh tế | **Xưởng đúc tiền** | Chữ thập | Mỏ vàng chu kỳ ngắn hơn |
| Kinh tế | **Đường buôn** | Cả hàng ngang | Mỗi Chợ trong hàng thu thêm vàng |
| Kinh tế | **Quảng trường** | Vòng quanh | Nhà ở xung quanh sinh Dân nhanh hơn |
| Quân sự | **Xưởng rèn** | Chỉ ô bên dưới | Lính từ nhà quân phía dưới tăng mạnh sát thương |
| Quân sự | **Bãi tập** | Chữ thập | Nhà quân xung quanh gọi quân nhanh hơn |
| Quân sự | **Kho quân nhu** | Chữ thập | Mỗi lần gọi quân tốn ít tài nguyên hơn |
| Quân sự | **Cờ hiệu** | Cả hàng ngang | Nhà quân trong hàng ra lính có thêm máu |
| Quân sự | **Bệnh xá** | Vòng quanh | Lính từ nhà xung quanh hồi máu chậm trong trận |

Xưởng cưa (ở bảng trên) cũng là nhà buff: buff Trại đốn gỗ xung quanh.

**Quy tắc buff:**

1. Nhà buff phải đáng một ô: đặt đúng chỗ thì mạnh hơn việc đặt thêm một nhà sản xuất.
2. Vùng hẹp thì buff mạnh, vùng rộng thì buff nhẹ.
3. Lên sao thì buff mạnh hơn hoặc vùng buff rộng hơn (tuỳ nhà).
4. Nhiều nhà cùng buff một ô thì cộng dồn, có trần.

## Nhà lính

Nhà lính **khác nhau theo từng phe**. Mỗi phe có **5 nhà lính**, mỗi nhà gọi một loại quân. Phe nào cũng có quân **đánh xa**.

Mỗi lần gọi quân **luôn tốn Dân**. Mỗi phe thiên về một tài nguyên: Người dùng đủ loại, Tiên thiên về Gỗ, Người lùn thiên về Vàng, Người hoá thú thiên về Lương thực, The Void chủ yếu tiêu Dân. Các con số sẽ cân bằng sau.

### Người
| Nhà | Quân | Vai trò | Mỗi lần gọi |
|---|---|---|---|
| Doanh trại | Kiếm sĩ khiên | Tuyến đầu | Dân + Lương thực |
| Trường bắn | Cung thủ | Tầm xa | Dân + Gỗ |
| Chuồng ngựa | Kỵ binh | Cơ động (xung phong) | Dân + Lương thực + Vàng |
| Tu viện | Tu sĩ | Hỗ trợ (hồi máu) | Dân + Vàng |
| Xưởng công thành | Máy bắn đá | Diện rộng | Dân + Gỗ + Vàng |

### Tiên
| Nhà | Quân | Vai trò | Mỗi lần gọi |
|---|---|---|---|
| Rừng thiêng | Người cây | Tuyến đầu (rất trâu, chậm) | Dân + nhiều Gỗ |
| Học viện cung | Cung thủ rừng | Tầm xa (xa nhất game) | Dân + Gỗ |
| Sân kiếm vũ | Vũ kiếm sư | Xung kích (né đòn) | Dân + Lương thực |
| Đền trăng | Pháp sư tự nhiên | Hỗ trợ (làm chậm địch) | Dân + Vàng |
| Bãi hươu | Kỵ sĩ hươu | Cơ động (bắn khi chạy) | Dân + Lương thực + Gỗ |

### Người lùn
| Nhà | Quân | Vai trò | Mỗi lần gọi |
|---|---|---|---|
| Sảnh đá | Chiến binh khiên | Tuyến đầu (rất trâu) | Dân + Lương thực + Vàng |
| Xưởng nỏ | Lính nỏ nặng | Tầm xa (sát thương cao, nạp chậm) | Dân + Gỗ + Vàng |
| Lò rèn chiến | Lính rìu | Xung kích | Dân + Vàng |
| Xưởng pháo | Pháo | Diện rộng | Dân + nhiều Vàng |
| Chuồng dê núi | Kỵ sĩ dê núi | Cơ động (chậm hơn kỵ binh nhưng trâu) | Dân + Lương thực + Vàng |

### Người hoá thú
| Nhà | Quân | Vai trò | Mỗi lần gọi |
|---|---|---|---|
| Hang gấu | Người gấu | Tuyến đầu (hất văng) | Dân + nhiều Lương thực |
| Hang sói | Người sói | Xung kích (đánh dồn dập) | Dân + Lương thực |
| Bãi báo | Người báo | Cơ động (vòng ra sau lưng) | Dân + Lương thực |
| Đá tổ tiên | Pháp sư bộ lạc | Hỗ trợ (tăng sức mạnh bầy) | Dân + Vàng |
| Tổ chim ưng | Người chim ưng | Tầm xa (ném lao từ trên cao) | Dân + Lương thực + Gỗ |

### The Void
| Nhà | Quân | Vai trò | Mỗi lần gọi |
|---|---|---|---|
| Khe nứt | Kẻ dị dạng | Tuyến đầu (nổ khi chết) | Nhiều Dân |
| Vết rách | Bóng hư | Xung kích (xuyên hàng) | Dân + Vàng |
| Tháp mắt | Con mắt | Tầm xa (làm chậm) | Dân + Gỗ |
| Bàn thờ hư không | Kẻ triệu hồi | Hỗ trợ (sinh quân con) | Nhiều Dân + Vàng |
| Hố gào thét | Kẻ gào thét | Diện rộng (sóng âm) | Nhiều Dân |

Các nhà buff quân sự (Xưởng rèn, Bãi tập, Kho quân nhu, Cờ hiệu, Bệnh xá) dùng chung cho mọi phe.

## Thắng và thua

- Thành có **máu** trong mỗi đợt. Máu thành về 0 là **thua đợt đó**.
- Người chơi có **3 trái tim**, hiển thị ở góc màn hình. Mỗi lần thua một đợt mất 1 tim. **Thua 3 lần** là thua cả lượt chơi.
- Thua một đợt (còn tim) thì **sang đợt sau**, không đánh lại, và **vẫn nhận đủ phần thưởng** của đợt đó.
- Máu thành **hồi đầy** sau mỗi đợt.
- **Thắng** cả lượt chơi khi vượt qua **30 đợt**.
- Có **5 đợt trùm**: đợt **6, 12, 18, 24, 30**. Thiết kế trùm làm sau.
- **Không có PvP.**

## Kẻ địch

- Trước khi vào lượt chơi, có **3 phe thù địch** với người chơi.
- **Mỗi đợt chỉ có 1 phe** tấn công.
- **Sự kiện ngẫu nhiên** có thể làm thay đổi danh sách phe thù địch:
  - Một phe **thôi thù địch** (rút khỏi danh sách).
  - Một phe **mới trở thành thù địch** (thêm vào danh sách).
- Số phe thù địch luôn **ít nhất 2, nhiều nhất 4**.
- Phe của chính người chơi **không bao giờ** là phe thù địch. Sau này sẽ thêm nhiều phe hơn.
- Phe tấn công mỗi đợt được **bốc ngẫu nhiên** trong danh sách thù địch.
- Sự kiện thay đổi phe thù địch được **thông báo khi đánh xong một đợt**.

Người chơi **không lấy được thẻ của phe khác** (khác 9 Kings). Bộ thẻ luôn chỉ gồm thẻ của phe mình và thẻ dùng chung.

## Phần thưởng sau mỗi đợt

1. **Chọn 1 trong 3 thẻ** ngẫu nhiên.
2. Nhận **Tinh thể năng lượng** để mua trong Cửa hàng. Cửa hàng có thể **làm mới (roll)**.

**Tinh thể năng lượng** là tài nguyên dùng chung cho mọi phe, **chỉ dùng ngoài trận**: roll Cửa hàng và mua công trình. Không dùng trong trận (tách riêng với 4 tài nguyên trong trận).

## Chưa chốt

"Sát cạnh" tính 4 hướng hay cả đường chéo, giới hạn thẻ trên tay, có gộp thẳng từ tay lên công trình không, lượng Tinh thể năng lượng thưởng mỗi đợt, Cửa hàng bán mấy thẻ, giá roll, có giữ thẻ khi roll không, chi tiết mỗi sao của từng nhà, gộp có bắt buộc 2 nhà đứng cạnh nhau không, gộp lúc nào (giữa các đợt hay cả trong trận), công thức ghép khác loại, trùm (làm sau), phép hỗ trợ trong trận (để sau), các con số.

## Các phe

Người chơi chọn một phe khi bắt đầu game.

Đặc trưng dưới đây là **bản tạm**, sẽ chỉnh lại sau. Các con số là số khởi điểm.

| Phe | Điểm mạnh | Điểm yếu |
|---|---|---|
| Người | Nhà ở sinh Dân nhanh hơn 25% | Thể chất yếu: quân ít máu hơn 10% |
| Tiên | Lính tinh nhuệ: chỉ số mỗi lính cao hơn khoảng 50%, tầm bắn xa hơn | Mỗi lần gọi chỉ ra 16 lính (4×4) thay vì 25 (5×5) |
| Người lùn | Mỏ vàng đào nhanh hơn 30% | Quân đi chậm hơn 20%, nhà lính gọi quân chậm hơn 20% |
| Người hoá thú | Nhà ra Lương thực nhanh hơn 30%. Đợt trăng tròn (ví dụ cứ 3 đợt có 1 đợt, thấy trước khi xem trước đợt địch): quân mạnh hơn hẳn | Nhà ở sinh Dân chậm hơn 25% |
| The Void | Quân mạnh hơn khoảng 30% | Mỗi lần gọi tốn gấp đôi Dân |

