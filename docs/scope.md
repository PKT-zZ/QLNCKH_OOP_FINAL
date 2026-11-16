# Phạm vi Hệ thống & Đặc tả Use Case — Hệ thống Quản lý NCKH

## 1. Mô tả bài toán

Hệ thống Quản lý Nghiên cứu Khoa học (NCKH) là ứng dụng phần mềm hỗ trợ các quy trình hành chính liên quan đến đề tài nghiên cứu khoa học tại trường đại học. Hệ thống giải quyết bài toán quản lý toàn bộ vòng đời của một đề tài: từ khâu đề xuất, phê duyệt, cấp kinh phí, nghiệm thu kết quả đến ghi nhận sản phẩm khoa học và kết xuất báo cáo thống kê.

Phạm vi bài OOP tập trung vào việc áp dụng các nguyên lý lập trình hướng đối tượng và mẫu thiết kế vào nghiệp vụ trên. Hệ thống mô phỏng quy trình chứ không phải hệ thống hành chính thực tế của trường.

---

## 2. Tác nhân hệ thống (Actors)

Hệ thống có **3 tác nhân** tương tác qua giao diện console. Việc chọn vai trò là mô phỏng để demo phân quyền, không phải cơ chế đăng nhập thật.

| Tác nhân | Ký hiệu trong code | Vai trò trong hệ thống |
|---|---|---|
| **Nhà nghiên cứu** | `UserRole.Researcher` | Người đề xuất đề tài, khai báo kết quả nghiên cứu (bài báo, sản phẩm). Chỉ được thao tác với đề tài có tên mình trong danh sách. |
| **Chuyên viên quản lý KH** | `UserRole.Officer` | Người điều phối hành chính: gán chủ nhiệm, phê duyệt đề tài, cấp phát kinh phí, lập hội đồng và nghiệm thu kết thúc đề tài. |
| **Chủ tịch hội đồng** | `UserRole.CouncilChair` | Nhà nghiên cứu được chỉ định chủ trì hội đồng nghiệm thu. Chỉ Chủ tịch của hội đồng đó mới được nhập điểm đánh giá. |

---

## 3. Phạm vi 8 Use Case

### Phân hệ A — Thành viên A phụ trách

#### UC01: Quản lý đơn vị và nhân sự
- **Tác nhân:** Chuyên viên.
- **Mô tả:** Thêm mới hoặc cập nhật thông tin Khoa/Phòng ban (`Department`) và hồ sơ nhà nghiên cứu (`Researcher`). Xem lịch sử nhiệm kỳ Trưởng khoa.
- **Dữ liệu vào:** Mã, tên, ngày thành lập (đơn vị); Mã, mã đơn vị, họ tên, email (nhà nghiên cứu).
- **Điều kiện hợp lệ:** Email phải đúng định dạng và là duy nhất trong hệ thống; đơn vị phải tồn tại khi tạo nhà nghiên cứu.
- **Hàm điều khiển:** `PeopleService.SaveDepartment()`, `PeopleService.SaveResearcher()`, `PeopleService.GetHeadHistory()`.

#### UC02: Đề xuất đề tài nghiên cứu
- **Tác nhân:** Nhà nghiên cứu.
- **Mô tả:** Nhà nghiên cứu khởi tạo hồ sơ đề tài mới, chọn loại đề tài và khai báo khung thời gian thực hiện.
- **Dữ liệu vào:** Mã đề tài, loại đề tài (C01/C02), tên đề tài, ngày bắt đầu, ngày kết thúc dự kiến.
- **Điều kiện hợp lệ:** Tên không rỗng; ngày kết thúc (nếu có) không trước ngày bắt đầu; mã đề tài chưa tồn tại trong hệ thống; người đề xuất phải có hồ sơ nhà nghiên cứu hợp lệ.
- **Dữ liệu ra:** Đề tài ở trạng thái `Pending`.
- **Hàm điều khiển:** `ProjectService.ProposeProject()`.

#### UC03: Gán chủ nhiệm và phê duyệt đề tài
- **Tác nhân:** Chuyên viên.
- **Mô tả:** Gán từng thành viên (vai trò Chủ nhiệm hoặc Thành viên) vào đề tài đang ở trạng thái Pending, sau đó phê duyệt để kích hoạt đề tài.
- **Dữ liệu vào (gán):** Mã đề tài, mã nhà nghiên cứu, vai trò (Leader/Member).
- **Dữ liệu vào (duyệt):** Mã đề tài.
- **Điều kiện hợp lệ:** Nhà nghiên cứu phải tồn tại; mỗi người chỉ được gán một vai trò trong đề tài; đề tài phải có **đúng 1 Chủ nhiệm** trước khi duyệt; chỉ được duyệt đề tài đang ở `Pending`.
- **Dữ liệu ra:** Đề tài chuyển sang trạng thái `Active`; lịch sử chuyển trạng thái được ghi nhận.
- **Hàm điều khiển:** `ProjectService.AssignMember()`, `ProjectService.Approve()`.

#### UC04: Khai báo nguồn và phân bổ kinh phí
- **Tác nhân:** Chuyên viên.
- **Mô tả:** Khai báo các nguồn kinh phí (quỹ trường, ngân sách nhà nước, tài trợ ngoài). Cấp tiền từng đợt cho đề tài, có thể cấp nhiều lần từ cùng một nguồn hoặc từ nhiều nguồn khác nhau.
- **Dữ liệu vào (nguồn):** Mã nguồn, tên nguồn, loại nguồn.
- **Dữ liệu vào (phân bổ):** Mã đợt, mã đề tài, mã nguồn, số tiền, ngày cấp.
- **Điều kiện hợp lệ:** Số tiền mỗi đợt phải lớn hơn 0; mã phân bổ duy nhất trên toàn hệ thống; đề tài và nguồn kinh phí phải tồn tại.
- **Dữ liệu ra:** Tổng kinh phí của đề tài được cập nhật tích lũy theo từng đợt.
- **Hàm điều khiển:** `FundingService.SaveSource()`, `FundingService.Allocate()`, `FundingService.TotalForProject()`.

---

### Phân hệ B — Thành viên B phụ trách

#### UC05: Thành lập hội đồng nghiệm thu
- **Tác nhân:** Chuyên viên.
- **Mô tả:** Thành lập một hội đồng đánh giá kết quả nghiên cứu cho đề tài đang `Active`, gán Chủ tịch và các ủy viên hội đồng.
- **Điều kiện hợp lệ:** Mỗi đề tài chỉ có tối đa 1 hội đồng; hội đồng có đúng 1 Chủ tịch; không được gán 1 người 2 vai trò trong cùng hội đồng.
- **Hàm điều khiển:** `EvaluationService.CreateCouncil()`, `EvaluationService.AssignMember()`.

#### UC06: Chấm điểm và nghiệm thu hoàn thành đề tài
- **Tác nhân:** Chủ tịch hội đồng (ghi điểm), Chuyên viên (xác nhận hoàn thành).
- **Mô tả:** Chủ tịch hội đồng nhập điểm đánh giá. Sau khi có điểm và ít nhất một kết quả đầu ra, Chuyên viên xác nhận nghiệm thu và chuyển đề tài sang `Completed`.
- **Điều kiện hợp lệ:** Điểm trong khoảng `[0, 10]`; chỉ Chủ tịch của chính hội đồng đó mới được ghi điểm; đề tài cần có điểm hội đồng **và** ít nhất 1 đầu ra trước khi hoàn thành.
- **Hàm điều khiển:** `EvaluationService.RecordScore()`, `ProjectService.Complete(user, projectId, ICompletionEvidence)`.

#### UC07: Ghi nhận kết quả đầu ra nghiên cứu
- **Tác nhân:** Nhà nghiên cứu.
- **Mô tả:** Nhà nghiên cứu ghi nhận bài báo khoa học (kèm danh sách tác giả, DOI, nhà xuất bản) hoặc sản phẩm bàn giao thực tế, liên kết với đề tài.
- **Điều kiện hợp lệ:** Bài báo cần có ít nhất 1 tác giả; thứ tự tác giả không trùng; đúng 1 tác giả liên hệ; DOI là duy nhất nếu được nhập; số lượng sản phẩm bàn giao phải lớn hơn 0.
- **Hàm điều khiển:** `OutputService.AddPaper()`, `OutputService.AddDeliverable()`.

#### UC08: Báo cáo tổng hợp và tra cứu
- **Tác nhân:** Chuyên viên.
- **Mô tả:** Tra cứu danh sách đề tài theo trạng thái. Xem báo cáo tổng hợp kinh phí theo đề tài hoặc theo nguồn tài trợ; thống kê kết quả đầu ra theo đề tài.
- **Dữ liệu ra:** Bảng báo cáo dạng dòng `ReportRow(Key, Label, Value, Unit, Detail)`.
- **Hàm điều khiển:** `ReportService.Generate(IReportStrategy)`.

---

## 4. Ngoài phạm vi (Out of Scope)

Các tính năng sau đây được xác định **không nằm trong phạm vi** của đồ án này:

- Giao diện đồ họa (GUI) hoặc giao diện web.
- Cơ chế xác thực người dùng thực tế (tài khoản, mật khẩu, JWT...). Việc "đăng nhập" trong hệ thống chỉ là chọn vai trò để demo phân quyền.
- Gửi email thông báo tự động.
- Kết nối hệ quản trị cơ sở dữ liệu quan hệ (MySQL, SQL Server, PostgreSQL...). Dữ liệu được lưu dưới dạng JSON cục bộ.
- Quản lý nhiều học kỳ, nhiều trường hoặc cấp vùng miền.
- Quy trình thanh toán, giải ngân hay chứng từ tài chính chi tiết.
- Chức năng upload/đính kèm file báo cáo hay tài liệu dạng PDF.
- CRUD đầy đủ cho mọi danh mục (loại đề tài, loại sản phẩm được định nghĩa sẵn trong seed).
