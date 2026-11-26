# Phạm vi và đặc tả ca sử dụng

## 1. Mô tả bài toán

Bài tập lớn mô phỏng quy trình quản lý đề tài nghiên cứu khoa học tại trường đại học. Phạm vi thiết kế gồm đề xuất, phê duyệt, cấp kinh phí, nghiệm thu, ghi nhận đầu ra và tổng hợp báo cáo. Menu cung cấp đủ tám ca sử dụng; chuyên viên có thể theo dõi đề tài từ lúc đề xuất đến lúc nghiệm thu. Xem [README](../README.md) để chạy chương trình và [UML](uml.md) để đối chiếu mô hình.

---

## 2. Tác nhân

Thiết kế gồm ba tác nhân. Menu chọn vai trò để minh họa cách service kiểm tra quyền. Xác thực tài khoản nằm ngoài phạm vi bài tập lớn.

| Tác nhân | Ký hiệu trong code | Vai trò trong hệ thống |
|---|---|---|
| **Nhà nghiên cứu** | `UserRole.Researcher` | Đề xuất đề tài bằng mã nhà nghiên cứu đã có. Ghi nhận đầu ra cho đề tài đang thực hiện mà mình tham gia. |
| **Chuyên viên quản lý khoa học** | `UserRole.Officer` | Quản lý hồ sơ đơn vị và nhà nghiên cứu, gán thành viên, duyệt đề tài, cấp kinh phí và xem báo cáo kinh phí. Lập hội đồng, xác nhận nghiệm thu, xem báo cáo đầu ra và tra cứu đề tài. |
| **Chủ tịch hội đồng** | `UserRole.CouncilChair` | Ghi điểm cho hội đồng mình chủ trì khi đề tài đang thực hiện. |

---

## 3. Tám ca sử dụng

### Phân hệ A - Nguyễn Khánh Toàn

#### UC01: Quản lý đơn vị và nhân sự
- **Tác nhân:** Chuyên viên.
- **Mô tả:** Thêm đơn vị hoặc sửa tên đơn vị đã có, lưu hồ sơ nhà nghiên cứu và tra cứu lịch sử lãnh đạo từ dữ liệu đã nạp.
- **Dữ liệu vào:** Mã, tên, ngày thành lập (đơn vị); Mã, mã đơn vị, họ tên, email (nhà nghiên cứu).
- **Điều kiện hợp lệ:** Mã đơn vị và mã nhà nghiên cứu được bỏ khoảng trắng hai đầu trước khi tra hồ sơ. Đơn vị tham chiếu phải tồn tại. Email phải có dấu `@` ở giữa chuỗi và duy nhất giữa các hồ sơ, so sánh không phân biệt hoa thường.
- **Khi cập nhật:** Giữ nguyên ngày thành lập của đơn vị đã có. Menu giữ học hàm hoặc học vị, số điện thoại, tổ chức và cờ giảng viên khi sửa hồ sơ cơ bản. Caller gọi trực tiếp `SaveResearcher` cần truyền lại các trường tùy chọn muốn giữ.
- **Hàm điều khiển:** `PeopleService.SaveDepartment()`, `PeopleService.SaveResearcher()`, `PeopleService.GetHeadHistory()`.

#### UC02: Đề xuất đề tài nghiên cứu
- **Tác nhân:** Nhà nghiên cứu.
- **Mô tả:** Nhà nghiên cứu khởi tạo hồ sơ đề tài mới, chọn loại đề tài và khai báo khung thời gian thực hiện.
- **Dữ liệu vào:** Mã đề tài, loại đề tài (C01/C02), tên đề tài, ngày bắt đầu, ngày kết thúc dự kiến.
- **Điều kiện hợp lệ:** Người đề xuất và loại đề tài phải có trong hệ thống. Tên đề tài không được rỗng. Ngày kết thúc có thể bỏ trống hoặc phải từ ngày bắt đầu trở đi. Mã đề tài được bỏ khoảng trắng ở hai đầu rồi kiểm tra trùng, không phân biệt hoa thường.
- **Dữ liệu ra:** Đề tài ở trạng thái `Pending`.
- **Hàm điều khiển:** `ProjectService.ProposeProject()`.

#### UC03: Gán chủ nhiệm và phê duyệt đề tài
- **Tác nhân:** Chuyên viên.
- **Mô tả:** Gán từng thành viên (vai trò Chủ nhiệm hoặc Thành viên) vào đề tài đang ở trạng thái Pending, sau đó phê duyệt để kích hoạt đề tài.
- **Dữ liệu vào (gán):** Mã đề tài, mã nhà nghiên cứu, vai trò (Leader/Member).
- **Dữ liệu vào (duyệt):** Mã đề tài.
- **Điều kiện hợp lệ:** Chỉ gán người và duyệt khi đề tài ở `Pending`. Nhà nghiên cứu phải tồn tại và chỉ được gán một lần trong đề tài. Đề tài có tối đa một chủ nhiệm trong lúc lập danh sách và phải có đúng một chủ nhiệm để được duyệt.
- **Dữ liệu ra:** Đề tài chuyển sang trạng thái `Active`; lịch sử chuyển trạng thái được ghi nhận.
- **Hàm điều khiển:** `ProjectService.AssignMember()`, `ProjectService.Approve()`.

#### UC04: Khai báo nguồn và phân bổ kinh phí
- **Tác nhân:** Chuyên viên.
- **Mô tả:** Khai báo các nguồn kinh phí (quỹ trường, ngân sách nhà nước, tài trợ ngoài). Cấp tiền từng đợt cho đề tài, có thể cấp nhiều lần từ cùng một nguồn hoặc từ nhiều nguồn khác nhau.
- **Dữ liệu vào (nguồn):** Mã nguồn, tên nguồn, loại nguồn.
- **Dữ liệu vào (phân bổ):** Mã đợt, mã đề tài, mã nguồn, số tiền, ngày cấp.
- **Điều kiện hợp lệ:** Mã nguồn mới và mã đợt cấp được bỏ khoảng trắng hai đầu trước khi tra trùng, không phân biệt hoa thường. Mã đợt cấp phải duy nhất trên tất cả đề tài. Đề tài và nguồn phải tồn tại. Menu 9 nhận số đồng nguyên dương, chỉ gồm các chữ số (ví dụ `1000000`). Số tiền lưu bằng `decimal`, phải dương và không làm tổng kinh phí toàn hệ thống vượt phạm vi của kiểu số này.
- **Mốc khóa kinh phí.** Chuyên viên có thể cấp thêm kinh phí khi đề tài ở `Pending` hoặc `Active`. Sau khi Chuyên viên xác nhận hoàn thành và đề tài chuyển sang `Completed`, chương trình từ chối khoản cấp mới và giữ nguyên các khoản đã lưu.
- **Dữ liệu ra:** Chương trình lưu khoản cấp và hiển thị tổng kinh phí tính từ các đợt của đề tài.
- **Hàm điều khiển:** `FundingService.SaveSource()`, `FundingService.Allocate()`, `FundingService.TotalForProject()`.

---

### Phân hệ B - NGUYỄN VIỆT HOÀNG

Ở menu 16, `ProjectService.Complete` nhận `ResultCompletionEvidence` để kiểm tra điểm và đầu ra của đúng đề tài trong kho đang dùng.

#### UC05: Thành lập hội đồng nghiệm thu
- **Tác nhân:** Chuyên viên.
- **Mô tả:** Thành lập một hội đồng đánh giá kết quả nghiên cứu cho đề tài đang `Active`, gán Chủ tịch và các ủy viên hội đồng.
- **Điều kiện hợp lệ:** Mỗi đề tài chỉ có tối đa 1 hội đồng; hội đồng mới có thể chưa có thành viên, chỉ được gán tối đa một Chủ tịch và phải có Chủ tịch trước khi ghi điểm; không được gán 1 người 2 vai trò trong cùng hội đồng.
- Danh sách hội đồng được khóa ngay sau lần ghi điểm đầu tiên, kể cả điểm `0`. Chủ tịch vẫn có thể sửa điểm khi đề tài còn ở trạng thái `Active`.
- **Hàm điều khiển:** `EvaluationService.CreateCouncil()`, `EvaluationService.AssignMember()`.

#### UC06: Chấm điểm và nghiệm thu hoàn thành đề tài
- **Tác nhân:** Chủ tịch hội đồng (ghi điểm), Chuyên viên (xác nhận hoàn thành).
- **Mô tả:** Chủ tịch hội đồng nhập điểm đánh giá. Sau khi có điểm và ít nhất một kết quả đầu ra, Chuyên viên xác nhận nghiệm thu và chuyển đề tài sang `Completed`.
- **Điều kiện hợp lệ:** Chỉ Chủ tịch của hội đồng đó được ghi điểm trong khoảng `[0, 10]`. Chuyên viên chỉ hoàn thành đề tài đang `Active` khi đã có điểm hội đồng và ít nhất một đầu ra.
- **Hàm điều khiển:** `EvaluationService.RecordScore()`, `ProjectService.Complete(user, projectId, ICompletionEvidence)`.

#### UC07: Ghi nhận kết quả đầu ra nghiên cứu
- **Tác nhân:** Nhà nghiên cứu.
- **Mô tả:** Nhà nghiên cứu ghi nhận bài báo khoa học (kèm danh sách tác giả, DOI, nhà xuất bản) hoặc sản phẩm bàn giao thực tế, liên kết với đề tài.
- **Điều kiện chung.** Người ghi nhận phải có hồ sơ và tham gia đề tài đang `Active`.
- **Bài báo.** Người ghi phải có tên trong danh sách tác giả. Mỗi tác giả phải có hồ sơ, chỉ xuất hiện một lần và có số thứ tự nguyên dương, không trùng. Bài báo cần ít nhất một tác giả, trong đó có đúng một tác giả liên hệ. Năm xuất bản nằm trong khoảng 1900–2100. DOI phải duy nhất nếu có.
- **Nhập tác giả liên hệ ở menu 17.** Nhập `y` nếu là tác giả liên hệ hoặc `n` nếu không, chấp nhận cả chữ hoa. Nếu nhập giá trị khác, chương trình hỏi lại cùng câu hỏi.
- **Sản phẩm bàn giao.** Loại sản phẩm phải tồn tại và số lượng phải là số nguyên dương.
- **Hàm điều khiển:** `OutputService.AddPaper()`, `OutputService.AddDeliverable()`.
- **Lưu và mở lại.** Bài báo mới và liên kết với đề tài được ghi cùng một lần vào JSON. Nếu ghi thất bại, phiên dừng và tệp chính giữ dữ liệu của lần lưu thành công gần nhất. Khi mở tệp, kho yêu cầu mỗi bài báo có ít nhất một liên kết đề tài hợp lệ. Kho từ chối hội đồng, liên kết bài báo hoặc sản phẩm gắn với đề tài `Pending`, đồng thời giữ dữ liệu lịch sử của đề tài `Completed` khi hợp lệ.

#### UC08: Báo cáo tổng hợp và tra cứu
- **Tác nhân:** Chuyên viên.
- **Danh sách và báo cáo.** Mọi vai trò có thể dùng menu 1 để xem mã, tên, trạng thái và số lượng thành viên của đề tài. Chuyên viên xem kinh phí theo đề tài ở menu 11 và theo nguồn ở menu 12.
- **Báo cáo ở menu 11, 12, 19 và 20.** `ReportService.Generate` kiểm quyền rồi gọi `IReportStrategy.BuildRows` để tạo các dòng `ReportRow`. Menu 19 đếm bản ghi đầu ra của từng đề tài. Menu 20 đếm bài báo và cộng số lượng sản phẩm theo loại trên toàn hệ thống.
- **Tra cứu ở menu 21.** `ProjectLookupService.Search` kiểm quyền rồi lọc theo mã, trạng thái hoặc cả hai. Mỗi `ProjectSummary` chứa mã, tên, trạng thái, tổng kinh phí, điểm hội đồng và danh sách đầu ra.

---

## 4. Ngoài phạm vi

- Giao diện đồ họa (GUI) hoặc giao diện web.
- Cơ chế xác thực người dùng thực tế (tài khoản, mật khẩu, JWT...). Việc "đăng nhập" trong hệ thống chỉ là chọn vai trò để demo phân quyền.
- Gửi email thông báo tự động.
- Kết nối hệ quản trị cơ sở dữ liệu quan hệ (MySQL, SQL Server, PostgreSQL...). Dữ liệu được lưu dưới dạng JSON cục bộ.
- Quản lý nhiều học kỳ, nhiều trường hoặc cấp vùng miền.
- Quy trình thanh toán, giải ngân hay chứng từ tài chính chi tiết.
- Chức năng upload/đính kèm file báo cáo hay tài liệu dạng PDF.
- CRUD đầy đủ cho mọi danh mục. Loại đề tài hiện lấy từ seed. Loại sản phẩm được nạp từ seed tích hợp.
