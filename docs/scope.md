# Phạm vi và đặc tả ca sử dụng

## 1. Mô tả bài toán

Bài tập lớn mô phỏng quy trình quản lý đề tài nghiên cứu khoa học tại trường đại học. Phạm vi thiết kế gồm đề xuất, phê duyệt, cấp kinh phí, nghiệm thu, ghi nhận đầu ra và tổng hợp báo cáo. Menu hiện có UC01–UC04 và hai báo cáo kinh phí thuộc UC08. Các luồng hội đồng, nghiệm thu và đầu ra ở mục 3 là thiết kế tích hợp. Xem [README](../README.md) để chạy chương trình và [UML](uml.md) để đối chiếu mô hình.

---

## 2. Tác nhân

Thiết kế gồm ba tác nhân. Menu chọn vai trò để minh họa cách service kiểm tra quyền. Xác thực tài khoản nằm ngoài phạm vi bài tập lớn.

| Tác nhân | Ký hiệu trong code | Vai trò trong hệ thống |
|---|---|---|
| **Nhà nghiên cứu** | `UserRole.Researcher` | Đề xuất đề tài bằng mã nhà nghiên cứu đã có. Khi tích hợp UC07, chỉ ghi nhận đầu ra cho đề tài mình tham gia. |
| **Chuyên viên quản lý khoa học** | `UserRole.Officer` | Quản lý hồ sơ đơn vị và nhà nghiên cứu, gán thành viên, duyệt đề tài, cấp kinh phí và xem báo cáo kinh phí. Phụ trách lập hội đồng và xác nhận nghiệm thu trong thiết kế tích hợp. |
| **Chủ tịch hội đồng** | `UserRole.CouncilChair` | Ghi điểm cho hội đồng mình chủ trì trong thiết kế UC06. Menu hiện cho phép chọn vai trò để minh họa phân quyền. |

---

## 3. Tám ca sử dụng

### Phân hệ A — Nguyễn Khánh Toàn

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
- **Điều kiện hợp lệ:** Mã nguồn mới và mã đợt cấp được bỏ khoảng trắng hai đầu trước khi tra trùng, không phân biệt hoa thường. Mã đợt cấp phải duy nhất trên tất cả đề tài. Đề tài và nguồn phải tồn tại. Số tiền dùng `decimal`, phải dương và không làm tổng kinh phí toàn hệ thống vượt phạm vi của kiểu số này.
- **Dữ liệu ra:** Chương trình lưu khoản cấp và hiển thị tổng kinh phí tính từ các đợt của đề tài.
- **Hàm điều khiển:** `FundingService.SaveSource()`, `FundingService.Allocate()`, `FundingService.TotalForProject()`.

---

### Phân hệ B — NGUYỄN VIỆT HOÀNG

UC05–UC07 và phần đầu ra của UC08 mô tả luồng dự kiến. Mã hiện tại đã có `ProjectService.Complete` và `ICompletionEvidence` để thử điều kiện hoàn thành với bằng chứng giả lập. Menu chưa nối luồng này với dữ liệu hội đồng và đầu ra thật.

#### UC05: Thành lập hội đồng nghiệm thu
- **Tác nhân:** Chuyên viên.
- **Mô tả:** Thành lập một hội đồng đánh giá kết quả nghiên cứu cho đề tài đang `Active`, gán Chủ tịch và các ủy viên hội đồng.
- **Điều kiện hợp lệ:** Mỗi đề tài chỉ có tối đa 1 hội đồng; hội đồng có đúng 1 Chủ tịch; không được gán 1 người 2 vai trò trong cùng hội đồng.
- **Hàm điều khiển:** `EvaluationService.CreateCouncil()`, `EvaluationService.AssignMember()`.

#### UC06: Chấm điểm và nghiệm thu hoàn thành đề tài
- **Tác nhân:** Chủ tịch hội đồng (ghi điểm), Chuyên viên (xác nhận hoàn thành).
- **Mô tả:** Chủ tịch hội đồng nhập điểm đánh giá. Sau khi có điểm và ít nhất một kết quả đầu ra, Chuyên viên xác nhận nghiệm thu và chuyển đề tài sang `Completed`.
- **Điều kiện hợp lệ:** Chỉ Chủ tịch của hội đồng đó được ghi điểm trong khoảng `[0, 10]`. Chuyên viên chỉ hoàn thành đề tài đang `Active` khi đã có điểm hội đồng và ít nhất một đầu ra.
- **Hàm điều khiển:** `EvaluationService.RecordScore()`, `ProjectService.Complete(user, projectId, ICompletionEvidence)`.

#### UC07: Ghi nhận kết quả đầu ra nghiên cứu
- **Tác nhân:** Nhà nghiên cứu.
- **Mô tả:** Nhà nghiên cứu ghi nhận bài báo khoa học (kèm danh sách tác giả, DOI, nhà xuất bản) hoặc sản phẩm bàn giao thực tế, liên kết với đề tài.
- **Điều kiện hợp lệ:** Người ghi nhận phải tham gia đề tài. Bài báo có ít nhất một tác giả và đúng một tác giả liên hệ. Thứ tự tác giả không trùng. DOI phải duy nhất nếu có. Sản phẩm có loại hợp lệ và số lượng lớn hơn 0.
- **Hàm điều khiển:** `OutputService.AddPaper()`, `OutputService.AddDeliverable()`.

#### UC08: Báo cáo tổng hợp và tra cứu
- **Tác nhân:** Chuyên viên.
- **Đã có:** Menu 11 và 12 tổng hợp kinh phí theo đề tài và theo nguồn. Menu 1 hiển thị danh sách đề tài và trạng thái hiện tại.
- **Dự kiến tích hợp:** Lọc danh sách theo trạng thái và thống kê kết quả đầu ra theo đề tài. Thống kê theo loại sản phẩm là hướng mở rộng.
- **Dữ liệu ra:** Bảng báo cáo dạng dòng `ReportRow(Key, Label, Value, Unit, Detail)`. Có thể bỏ trống `Detail`.
- **Hàm điều khiển:** `ReportService.Generate(CurrentUserContext user, IReportStrategy strategy)` kiểm quyền trước khi gọi `strategy.BuildRows()`.

---

## 4. Ngoài phạm vi

- Giao diện đồ họa (GUI) hoặc giao diện web.
- Cơ chế xác thực người dùng thực tế (tài khoản, mật khẩu, JWT...). Việc "đăng nhập" trong hệ thống chỉ là chọn vai trò để demo phân quyền.
- Gửi email thông báo tự động.
- Kết nối hệ quản trị cơ sở dữ liệu quan hệ (MySQL, SQL Server, PostgreSQL...). Dữ liệu được lưu dưới dạng JSON cục bộ.
- Quản lý nhiều học kỳ, nhiều trường hoặc cấp vùng miền.
- Quy trình thanh toán, giải ngân hay chứng từ tài chính chi tiết.
- Chức năng upload/đính kèm file báo cáo hay tài liệu dạng PDF.
- CRUD đầy đủ cho mọi danh mục. Loại đề tài hiện lấy từ seed. Loại sản phẩm là danh mục dự kiến khi tích hợp đầu ra.
