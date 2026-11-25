# Hệ thống quản lý nghiên cứu khoa học ở đại học

Bài tập lớn môn Lập trình hướng đối tượng, thực hiện theo nhóm hai sinh viên. Ứng dụng dòng lệnh dùng C# và .NET 8 để mô phỏng quy trình quản lý đề tài nghiên cứu khoa học ở trường đại học.

## 1. Phạm vi

Phiên bản hiện tại có chức năng quản lý đơn vị và nhà nghiên cứu (UC01), đề xuất đề tài (UC02), gán thành viên và phê duyệt (UC03), quản lý nguồn và phân bổ kinh phí (UC04). Phân hệ B bổ sung lập hội đồng nghiệm thu (UC05), ghi điểm và hoàn thành đề tài (UC06), ghi bài báo và sản phẩm bàn giao (UC07), báo cáo và tra cứu (UC08). Menu có hai báo cáo kinh phí (theo đề tài, theo nguồn) và hai báo cáo đầu ra (theo đề tài, theo loại). Việc đổi vai trò trong menu dùng để minh họa phân quyền.

Phạm vi thiết kế của nhóm gồm tám ca sử dụng. Luồng đầy đủ: nhà nghiên cứu → đề tài → chủ nhiệm → duyệt → kinh phí → hội đồng → điểm → đầu ra → hoàn thành → báo cáo. Xem [đặc tả nghiệp vụ](docs/scope.md) và [sơ đồ UML](docs/uml.md).

## 2. Tổ chức mã nguồn

| Thư mục | Trách nhiệm |
|---|---|
| `Presentation/` | Menu, nhập liệu và hiển thị lỗi. |
| `Business/` | Thực thể, quy tắc nghiệp vụ, kiểm tra vai trò và hợp đồng lưu trữ. |
| `Data/` | Kho dữ liệu trong bộ nhớ, kho JSON và dữ liệu mẫu. |
| `QLNCKH_OOP_FINAL.Tests/` | Bộ kiểm thử nghiệp vụ bằng kho dữ liệu trong bộ nhớ. |

`ResearchProject` dùng State để xử lý chuyển trạng thái. Báo cáo kinh phí dùng hai cài đặt của `IReportStrategy`, theo đề tài và theo nguồn. `ICompletionEvidence` là hợp đồng để phân hệ nghiệm thu cung cấp điều kiện hoàn thành đề tài, do `ResultCompletionEvidence` (phân hệ B) cài đặt trên dữ liệu thật. UC07 dùng Factory Method (`ResearchOutputCreator`, `PaperCreator`, `DeliverableCreator`) để tạo `Paper` hoặc `ProjectDeliverable`, cùng kế thừa `ResearchOutput`. Báo cáo đầu ra dùng thêm `OutputByProjectStrategy` và `OutputByTypeStrategy`.

## 3. Cài đặt và chạy

Cài .NET 8 SDK. Mở PowerShell ở thư mục chứa `QLNCKH_OOP_FINAL.csproj` và chạy các lệnh sau.

```powershell
dotnet build .\QLNCKH_OOP_FINAL.csproj
dotnet run --project .\QLNCKH_OOP_FINAL.csproj
```

Ứng dụng đọc `config/appsettings.json` và tính đường dẫn tương đối từ thư mục chứa tệp chạy. Nếu chưa có `data/nckh.json`, ứng dụng sao chép `Data/seed.json` để khởi tạo dữ liệu làm việc.

### Đóng gói

Lệnh sau tạo bản chạy cho Windows x64. Máy chạy cần có .NET 8 Runtime x64.

```powershell
dotnet publish .\QLNCKH_OOP_FINAL.csproj -c Release -r win-x64 --self-contained false -o .\dist
```

Sao chép cả thư mục `dist` để giữ tệp thực thi, các tệp .NET, cấu hình và seed. Mở `QLNCKH_OOP_FINAL.exe` trong bản sao có quyền ghi.

## 4. Kiểm thử

```powershell
dotnet run --project .\QLNCKH_OOP_FINAL.Tests\QLNCKH_OOP_FINAL.Tests.csproj
```

Bộ kiểm thử có 26 ca phần A và 21 ca phần B (B-T11 đến B-T20 cùng các ca kiểm quyền, Factory Method, Strategy, tra cứu). Mỗi ca dùng kho trong bộ nhớ. Chương trình in kết quả từng ca và trả mã thoát khác 0 nếu có ca thất bại. Ca hoàn thành đề tài dùng nguồn bằng chứng giả lập để kiểm tra `ICompletionEvidence`.

## 5. Kịch bản trình diễn UC01–UC04

Dùng một bản chạy mới để ứng dụng khởi tạo từ seed. Nếu chạy lại trên cùng dữ liệu, chọn mã đề tài, nguồn và phân bổ chưa được sử dụng. Nhập ngày theo dạng `yyyy-MM-dd`.

1. Ở vai trò Chuyên viên, chọn **10** và nhập `D01` để xem lịch sử lãnh đạo đơn vị.
2. Chọn **2**, rồi chọn **2** và nhập `R001` để vào vai Nhà nghiên cứu. Chọn **5**, tạo đề tài `P003` loại `C01`, tên *Nghiên cứu phân loại văn bản*, bắt đầu `2026-01-01` và kết thúc `2026-12-31`. Đề tài mới ở trạng thái `Pending`.
3. Chọn **2**, rồi chọn **1** để về vai Chuyên viên. Chọn **7** và nhập `P003` để thử duyệt trước khi gán chủ nhiệm. Sau khi chương trình từ chối, chọn **6**, nhập `P003`, `R001` và chọn **1** để gán chủ nhiệm. Sau đó dùng mục **7** để duyệt lại `P003` và chuyển sang `Active`.
4. Chọn **8**, thêm nguồn `F03` với tên *Quỹ hỗ trợ nghiên cứu* và loại *Tài trợ ngoài*. Dùng mục **9** hai lần để cấp kinh phí cho `P003`.
   - Đợt `A004` từ `F01`, số tiền `100000000`, ngày `2026-02-01`.
   - Đợt `A005` từ `F03`, số tiền `150000000`, ngày `2026-06-01`.
5. Chọn **11** để xem tổng kinh phí của `P003` là 250.000.000 đồng và chọn **12** để xem tổng theo nguồn. Thoát bằng **0**, mở lại ứng dụng rồi chọn **1** để đối chiếu trạng thái và tổng kinh phí đã lưu.

## 6. Phân công

- **Thành viên A — Nguyễn Khánh Toàn.** UC01–UC04, State và hai cách tổng hợp kinh phí theo Strategy.
- **Thành viên B — NGUYỄN VIỆT HOÀNG.** UC05–UC08, gồm hội đồng, chấm điểm và nghiệm thu, đầu ra nghiên cứu và báo cáo kết quả.

## 7. Về việc sử dụng AI

- Thành viên A dùng các công cụ AI để thảo luận sâu hơn về thiết kế, giải thích những điểm còn vướng và rà lại sơ đồ UML cho khớp với mã nguồn.
- AI hỗ trợ viết một số đoạn mã có thể tự động hóa, như xử lý dữ liệu, nhập liệu và chuẩn bị ca kiểm thử.
- Thành viên A tự viết các lớp, phương thức nghiệp vụ chính và phần cài đặt hướng đối tượng cốt lõi. Với các phần này, AI chủ yếu hỗ trợ đọc lại mã, nhận xét thiết kế và gợi ý cách sửa khi phát hiện vấn đề.
