# Hệ thống Quản lý Nghiên cứu Khoa học (NCKH)

Ứng dụng Console C# quản lý toàn diện vòng đời đề tài nghiên cứu khoa học trong trường đại học: từ khâu đề xuất, phê duyệt, phân bổ kinh phí, thành lập hội đồng nghiệm thu, đánh giá kết quả, ghi nhận sản phẩm/bài báo đến hoàn thành và kết xuất báo cáo thống kê.

---

## 1. Giới thiệu dự án

* **Ngôn ngữ & Nền tảng:** C# 12 / .NET 8.0 (Console Application).
* **Mô hình kiến trúc:** 3-Tier Architecture (Presentation – Business – Data).
* **Cơ chế lưu trữ:** Local JSON Repository độc lập, tự phục hồi từ dữ liệu seed, sử dụng đường dẫn tương đối (không phụ thuộc SQL Server hay dịch vụ bên ngoài).
* **Hình thức thực hiện:** Nhóm 02 thành viên, phân chia độc lập theo phân hệ và tích hợp dữ liệu thông qua các giao diện hợp đồng (`Interface`).

---

## 2. Phân hệ chức năng & Nghiệp vụ

Hệ thống bao gồm **8 Use Case cốt lõi**, phân chia thành 2 phân hệ nghiệp vụ chính:

| Phân hệ / Use Case | Người phụ trách | Mô tả nghiệp vụ |
|---|---|---|
| **UC01**: Quản lý đơn vị & Nhà nghiên cứu | Thành viên A | Thêm/sửa thông tin khoa, phòng ban, hồ sơ nhà nghiên cứu; theo dõi lịch sử nhiệm kỳ lãnh đạo đơn vị. |
| **UC02**: Đề xuất đề tài nghiên cứu | Thành viên A | Nhà nghiên cứu gửi hồ sơ đề xuất đề tài mới với loại đề tài và khung thời gian dự kiến (trạng thái ban đầu: `Pending`). |
| **UC03**: Gán chủ nhiệm & Phê duyệt | Thành viên A | Chuyên viên quản lý khoa học gán thành viên, chỉ định đúng 1 chủ nhiệm (`Leader`) và phê duyệt đề tài chuyển sang `Active`. |
| **UC04**: Quản lý nguồn & Cấp kinh phí | Thành viên A | Khai báo các nguồn tài trợ (nội bộ, ngân sách, tài trợ ngoài); cấp kinh phí nhiều đợt cho đề tài với số tiền dương; tính tổng kinh phí. |
| **UC05**: Thành lập hội đồng nghiệm thu | Thành viên B | Chuyên viên thành lập hội đồng đánh giá cho đề tài (mỗi đề tài tối đa 1 hội đồng), chỉ định Chủ tịch (`CouncilChair`) và các ủy viên. |
| **UC06**: Chấm điểm & Nghiệm thu | Thành viên B | Chủ tịch hội đồng ghi nhận điểm đánh giá (thang điểm 0–10); Chuyên viên chỉ đóng đề tài sang `Completed` khi có điểm và đủ đầu ra. |
| **UC07**: Ghi nhận kết quả đầu ra | Thành viên B | Ghi nhận bài báo khoa học (tác giả, DOI, nhà xuất bản) hoặc sản phẩm bàn giao thực tế gắn liền với đề tài nghiên cứu. |
| **UC08**: Báo cáo & Thống kê | Thành viên B | Tổng hợp kinh phí theo đề tài, theo nguồn tài trợ; thống kê số lượng kết quả đầu ra theo từng đề tài hoặc theo phân loại sản phẩm. |

### Các quy tắc nghiệp vụ cốt lõi (Business Rules)
* **R1 (Thời gian):** Tên đề tài không được để trống; ngày kết thúc phải sau hoặc bằng ngày bắt đầu (`EndDate >= StartDate`).
* **R2 (Chủ nhiệm):** Một đề tài có thể có nhiều thành viên nhưng bắt buộc phải có **đúng 1 Chủ nhiệm** (`Leader`) mới được phép phê duyệt.
* **R3 (Vòng đời):** Vòng đời đề tài tuân thủ nghiêm ngặt theo thứ tự một chiều: `Pending` $\rightarrow$ `Active` $\rightarrow$ `Completed`.
* **R4 (Kinh phí):** Số tiền phân bổ mỗi đợt phải lớn hơn 0 (`amount > 0`), sử dụng kiểu số thực chính xác `decimal`; mã phân bổ không được trùng lặp.
* **R5 (Nhân sự):** Email nhà nghiên cứu phải có định dạng hợp lệ và là duy nhất trong toàn hệ thống.
* **R6 (Hội đồng):** Mỗi hội đồng chỉ có duy nhất 1 Chủ tịch (`CouncilChair`); một nhà nghiên cứu không được nhận 2 vai trò trong cùng một hội đồng.
* **R7 (Điểm số):** Điểm nghiệm thu hợp lệ phải nằm trong thang điểm $[0, 10]$; chỉ tài khoản Chủ tịch của hội đồng đó mới có quyền nhập điểm.
* **R8 (Giới hạn đánh giá):** Mỗi đề tài nghiên cứu chỉ có tối đa một hội đồng nghiệm thu kết quả.
* **R9 (Bài báo):** Thứ tự tác giả trong bài báo không được trùng lặp; mỗi bài báo có đúng một tác giả liên hệ (`Corresponding Author`); mã DOI là duy nhất nếu có.
* **R10 (Điều kiện hoàn thành):** Đề tài chỉ được chuyển sang trạng thái `Completed` khi đã có điểm đánh giá từ hội đồng và sở hữu ít nhất một kết quả đầu ra (bài báo hoặc sản phẩm bàn giao).

---

## 3. Kiến trúc hệ thống & Thiết kế hướng đối tượng

Ứng dụng tuân thủ mô hình **3 tầng (3-Tier)** kết hợp nguyên lý **Inversion of Control**: Tầng giao diện và dịch vụ chỉ giao tiếp thông qua giao diện trừu tượng (`Interface`), không phụ thuộc trực tiếp vào tầng lưu trữ tệp.

```text
NckhOop/
├── Presentation/                 # TẦNG TRÌNH DIỄN (Giao diện dòng lệnh)
│   └── ConsoleMenu.cs            # Điều hướng menu, phân vai người dùng, bắt ngoại lệ
├── Business/                     # TẦNG NGHIỆP VỤ (Domain Models, Services & Interfaces)
│   ├── Models/                   # 16 Thực thể nghiệp vụ, State, Lớp trừu tượng
│   │   ├── Common.cs             # Enums (UserRole, ProjectStatus, ProjectRole), Guard, Context
│   │   ├── People.cs             # Department, Researcher, DepartmentHeadTerm
│   │   ├── Projects.cs           # ResearchProject, ProjectParticipation, ProjectStatusHistory
│   │   ├── ProjectStates.cs      # IProjectState, PendingState, ActiveState, CompletedState
│   │   └── Funding.cs            # FundingSource, FundingAllocation
│   ├── Interfaces/               # Hợp đồng giao tiếp giữa các phân hệ
│   │   ├── Repositories.cs       # IPeopleRepository, IProjectRepository, ICompletionEvidence
│   │   └── IReportStrategy.cs    # IReportStrategy, ReportRow
│   └── Services/                 # Dịch vụ xử lý nghiệp vụ theo Use Case
│       ├── Access.cs             # Kiểm tra quyền hạn theo vai trò người dùng
│       ├── PeopleService.cs      # Xử lý UC01
│       ├── ProjectService.cs     # Xử lý UC02, UC03, Điều kiện hoàn thành
│       ├── FundingService.cs     # Xử lý UC04
│       ├── FundingByProjectStrategy.cs # Chiến lược báo cáo kinh phí theo đề tài
│       └── FundingBySourceStrategy.cs  # Chiến lược báo cáo kinh phí theo nguồn
├── Data/                         # TẦNG DỮ LIỆU (Lưu trữ JSON & Bộ nhớ tạm)
│   ├── JsonResearchRepository.cs # Xử lý đọc/ghi nckh.json an toàn
│   ├── InMemoryResearchRepository.cs # Repository in-memory phục vụ kiểm thử
│   ├── seed.json                 # Dữ liệu mẫu khởi tạo ban đầu
│   └── appsettings.json          # Cấu hình đường dẫn dữ liệu tương đối
└── docs/                         # Tài liệu thiết kế kỹ thuật
```

### Các mẫu thiết kế áp dụng (Design Patterns)
1. **State Pattern (Quản lý vòng đời đề tài):**
   * Interface `IProjectState` định nghĩa các hành vi `Approve()` và `Complete()`.
   * Các lớp cụ thể: `PendingState`, `ActiveState`, `CompletedState`.
   * Lớp ngữ cảnh `ResearchProject` ủy quyền toàn bộ việc chuyển đổi trạng thái cho State hiện tại, loại bỏ các khối lệnh `if-else` kiểm tra trạng thái rải rác.
2. **Strategy Pattern (Báo cáo thống kê đa chiều):**
   * Interface `IReportStrategy` định nghĩa phương thức `BuildRows()` trả về danh sách `ReportRow`.
   * Tùy biến linh hoạt giữa các thuật toán báo cáo: `FundingByProjectStrategy` (theo đề tài), `FundingBySourceStrategy` (theo nguồn tài trợ) mà không cần thay đổi giao diện hay sửa đổi mã nguồn các lớp cũ (tuân thủ nguyên lý Open/Closed).
3. **Factory Method Pattern (Khởi tạo kết quả nghiên cứu):**
   * Lớp trừu tượng `ResearchOutputCreator` định nghĩa phương thức khởi tạo đa hình `CreateOutput()`.
   * Các lớp con `PaperCreator` và `DeliverableCreator` chịu trách nhiệm tạo và kiểm tra tính hợp lệ cho từng loại kết quả tương ứng (`Paper` hoặc `ProjectDeliverable`).

### Kỹ thuật bảo toàn dữ liệu nhóm (`AppSnapshot.OtherModules`)
Kho lưu trữ `JsonResearchRepository` được thiết kế có thuộc tính `OtherModules` lưu giữ nguyên vẹn các khối dữ liệu JSON chưa thuộc phân hệ mình xử lý. Điều này đảm bảo khi hai thành viên ghi dữ liệu vào cùng một file `data/nckh.json`, dữ liệu của người này sẽ không bao giờ bị ghi đè hay làm mất dữ liệu của người kia.

---

## 4. Hướng dẫn Cài đặt & Chạy ứng dụng

### Yêu cầu hệ thống
* Hệ điều hành: Windows / Linux / macOS.
* Môi trường: Đã cài đặt **.NET 8.0 SDK** (kiểm tra bằng lệnh: `dotnet --version`).

### 1. Build chương trình
Từ thư mục chứa solution `NckhOop/`:
```powershell
dotnet build NckhOop.sln -m:1
```

### 2. Khởi chạy ứng dụng Console
```powershell
dotnet run --project .\NckhOop\NckhOop.csproj
```
> *Ghi chú về dữ liệu:* Lần đầu khởi chạy, chương trình tự động tạo tệp `data/nckh.json` cạnh tệp thực thi từ `seed.json`. Để reset toàn bộ dữ liệu về trạng thái mẫu ban đầu, chỉ cần tắt chương trình và xóa tệp `data/nckh.json`.

### 3. Chạy bộ kiểm thử tự động (Unit Tests)
```powershell
dotnet run --project .\NckhOop.Tests\NckhOop.Tests.csproj
```
Dự án test là chương trình độc lập, tự động thực thi toàn bộ các ca kiểm tra nghiệp vụ và báo cáo kết quả `PASS` / `FAIL` trực quan trên màn hình console.

### 4. Đóng gói bản phát hành (Publish)
```powershell
dotnet publish .\NckhOop\NckhOop.csproj -c Release -o .\dist
```
Thư mục `dist/` sẽ chứa tệp thực thi độc lập (`NckhOop.exe` trên Windows) đi kèm các thư mục cấu hình và dữ liệu với đường dẫn tương đối, có thể sao chép sang máy khác chạy ngay.

---

## 5. Kịch bản chạy thử nghiệm (Demo Walkthrough)

Dưới đây là kịch bản chạy thử mẫu để trải nghiệm đầy đủ các tính năng và cơ chế kiểm soát lỗi của hệ thống:

```text
================================================================================
KỊCH BẢN DEMO: QUẢN LÝ ĐỀ TÀI, PHÊ DUYỆT VÀ PHÂN BỔ KINH PHÍ (UC01 - UC04)
================================================================================

1. [Khởi động]: Mở ứng dụng, hệ thống mặc định vào vai trò "Chuyên viên" (Officer).

2. [Đổi vai trò]:
   - Bấm phím "2" -> Chọn "2" để đổi sang vai trò Nhà nghiên cứu "R001".

3. [UC02 - Đề xuất đề tài]:
   - Bấm phím "5": Nhập mã đề tài "P001", loại "C01", 
     tên "Nghiên cứu ứng dụng Học máy trong Chẩn đoán Y tế",
     ngày bắt đầu "2026-01-01", ngày kết thúc "2026-12-31".
   - Kết quả: Đề tài P001 được tạo thành công ở trạng thái Pending.

4. [UC03 - Thử nghiệm bẫy lỗi & Phê duyệt]:
   - Bấm phím "2" -> Chọn "1" để quay lại vai trò Chuyên viên.
   - Thử nghiệm bẫy lỗi: Bấm "7" duyệt ngay P001.
     -> Hệ thống từ chối: "Lỗi: Đề tài cần đúng một chủ nhiệm trước khi duyệt."
   - Bấm phím "6": Gán thành viên cho "P001", mã nhà nghiên cứu "R001", vai trò "1" (Chủ nhiệm).
   - Thử nghiệm bẫy lỗi: Gán tiếp "R002" làm Chủ nhiệm thứ hai.
     -> Hệ thống từ chối: "Lỗi: Đề tài đã có chủ nhiệm."
   - Bấm phím "7": Duyệt đề tài "P001".
     -> Hệ thống thông báo: "Đã duyệt đề tài." (Trạng thái chuyển sang Active).

5. [UC04 - Phân bổ kinh phí]:
   - Bấm phím "8": Thêm nguồn kinh phí "F02", tên "Quỹ Phát triển KH&CN Quốc gia", loại "Tài trợ ngoài".
   - Bấm phím "9": Cấp đợt 1 mã "A001" cho "P001", nguồn "F01", số tiền "100000000", ngày "2026-02-01".
   - Bấm phím "9": Cấp đợt 2 mã "A002" cho "P001", nguồn "F02", số tiền "150000000", ngày "2026-06-01".
   - Kết quả: Tổng kinh phí của đề tài được cập nhật chính xác: 250,000,000 đ.

6. [Kiểm tra dữ liệu]:
   - Bấm phím "1": Xem danh sách đề tài hiện tại kèm trạng thái và tổng kinh phí đã phân bổ.
   - Thoát chương trình (phím "0") và mở lại: Dữ liệu vẫn được lưu trữ nguyên vẹn trong tệp data/nckh.json.
```

---

## 6. Quy chuẩn Quản lý Mã nguồn (Git Workflow)

* **Nhánh `main`:** Lưu trữ phiên bản ổn định đã tích hợp và vượt qua toàn bộ các ca kiểm thử.
* **Nhánh `feature/projects-funding`:** Phân hệ quản lý đề tài, kinh phí và các mẫu thiết kế của Thành viên A.
* **Nhánh `feature/evaluation-outputs`:** Phân hệ quản lý nghiệm thu, kết quả đầu ra và báo cáo của Thành viên B.
* **Quy ước Commit:** Áp dụng chuẩn Conventional Commits (`feat:`, `test:`, `refactor:`, `docs:`, `fix:`) với thông điệp rõ ràng, minh chứng cho từng bước phát triển của dự án.
