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
| `QLNCKH_OOP_FINAL.Tests/` | Kiểm thử nghiệp vụ trong bộ nhớ và kiểm thử mở tệp qua ứng dụng thật. |

`ResearchProject` dùng State để xử lý chuyển trạng thái. Báo cáo kinh phí dùng hai cài đặt của `IReportStrategy`, theo đề tài và theo nguồn. `ICompletionEvidence` là hợp đồng để phân hệ nghiệm thu cung cấp điều kiện hoàn thành đề tài, do `ResultCompletionEvidence` (phân hệ B) cài đặt trên dữ liệu thật. UC07 dùng Factory Method (`ResearchOutputCreator`, `PaperCreator`, `DeliverableCreator`) để tạo `Paper` hoặc `ProjectDeliverable`, cùng kế thừa `ResearchOutput`. Báo cáo đầu ra dùng thêm `OutputByProjectStrategy` và `OutputByTypeStrategy`.

## 3. Cài đặt và chạy

Cài **.NET 8 SDK** để build và chạy kiểm thử. Dùng `dotnet --list-sdks` để kiểm tra có bản `8.0.x`. Tại thư mục chứa `QLNCKH_OOP_FINAL.csproj`, chạy hai lệnh sau.

```powershell
dotnet build .\QLNCKH_OOP_FINAL.csproj
dotnet run --project .\QLNCKH_OOP_FINAL.csproj
```

Ứng dụng mở ở vai **Chuyên viên**. Để xem nhanh dữ liệu mẫu, chọn `21`, nhập `P002` rồi nhấn Enter ở ô trạng thái. Với seed ban đầu, P002 hiện `Active`, kinh phí 120.000.000 đồng, điểm hội đồng `chưa có`, bài báo `PA01` và sản phẩm `SP01`. Chọn `0` để thoát. Mục 4 hướng dẫn chạy bộ test, mục 5 có kịch bản thực hành cả hai phân hệ.

Ứng dụng đọc `config/appsettings.json` cạnh tệp chạy. Ở lần chạy đầu, ứng dụng tạo tệp `data/nckh.json` từ `Data/seed.json`. Với `dotnet run` theo lệnh trên, tệp làm việc nằm dưới `bin\Debug\net8.0`. Mở lại cùng bản chạy sẽ đọc tiếp dữ liệu đã lưu.

### Đóng gói

Lệnh sau tạo bản chạy cho Windows x64. Máy chạy cần có .NET 8 Runtime x64.

```powershell
dotnet publish .\QLNCKH_OOP_FINAL.csproj -c Release -r win-x64 --self-contained false -o .\dist
.\dist\QLNCKH_OOP_FINAL.exe
```

Bản nộp dùng `Data/seed.json` của bản tích hợp A–B. Sao chép cả thư mục `dist` sang một thư mục mới có quyền ghi, giữ các tệp thực thi, tệp .NET, `config/appsettings.json` và `Data/seed.json`. Khi đóng gói, chỉ kèm dữ liệu mẫu và để ứng dụng tự tạo `data/nckh.json` khi giảng viên mở `QLNCKH_OOP_FINAL.exe` lần đầu. Giữ tệp dữ liệu làm việc hiện có ở thư mục cũ.

## 4. Kiểm thử

```powershell
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding
dotnet run --project .\QLNCKH_OOP_FINAL.Tests\QLNCKH_OOP_FINAL.Tests.csproj
"Mã thoát test: $LASTEXITCODE"
```

Bộ kiểm thử tự chuẩn bị dữ liệu và chạy đủ ba nhóm bằng lệnh trên. Khi tất cả đều đạt, console in `Kết quả: 27/27 ca đạt.`, `Kết quả B: 24/24 ca đạt.`, `Kết quả JSON: 16/16 ca đạt.` và mã thoát `0`. Hai nhóm A và B dùng kho trong bộ nhớ. Nhóm JSON mở ứng dụng thật trong thư mục tạm. Ca thất bại hiện `FAIL` và trả mã thoát khác `0`.

## 5. Kịch bản trình diễn hai phân hệ

Dùng thư mục chạy mới để thực hiện đúng các mã trong kịch bản, kể cả khi đã demo trước đó. Trên Windows x64, mở PowerShell tại thư mục repo và chạy các lệnh sau.

```powershell
$demo = Join-Path $env:TEMP ('oop-demo-' + [guid]::NewGuid().ToString('N'))
dotnet publish .\QLNCKH_OOP_FINAL.csproj -c Release -r win-x64 --self-contained false -o $demo
if ($LASTEXITCODE -eq 0) { & (Join-Path $demo 'QLNCKH_OOP_FINAL.exe') }
```

Ở các bước cần thoát rồi mở lại, dùng `& (Join-Path $demo 'QLNCKH_OOP_FINAL.exe')` trong cùng cửa sổ PowerShell để đọc tiếp dữ liệu của lượt demo. Nhập ngày theo dạng `yyyy-MM-dd`.

1. Ở vai trò Chuyên viên, chọn **10** và nhập `D01` để xem lịch sử lãnh đạo đơn vị.
2. Chọn **2**, rồi chọn **2** và nhập `R001` để vào vai Nhà nghiên cứu. Chọn **5**, tạo đề tài `P003` loại `C01`, tên *Nghiên cứu phân loại văn bản*, bắt đầu `2026-01-01` và kết thúc `2026-12-31`. Đề tài mới ở trạng thái `Pending`.
3. Chọn **2**, rồi chọn **1** để về vai Chuyên viên. Chọn **7** và nhập `P003` để thử duyệt trước khi gán chủ nhiệm. Sau khi chương trình từ chối, chọn **6**, nhập `P003`, `R001` và chọn **1** để gán chủ nhiệm. Sau đó dùng mục **7** để duyệt lại `P003` và chuyển sang `Active`.
4. Chọn **8**, thêm nguồn `F03` với tên *Quỹ hỗ trợ nghiên cứu* và loại *Tài trợ ngoài*. Dùng mục **9** hai lần để cấp kinh phí cho `P003`.
   - Đợt `A004` từ `F01`, số tiền `100000000`, ngày `2026-02-01`.
   - Đợt `A005` từ `F03`, số tiền `150000000`, ngày `2026-06-01`.
5. Chọn **11** để xem tổng kinh phí của `P003` là 250.000.000 đồng và chọn **12** để xem tổng theo nguồn. Thoát bằng **0**, mở lại ứng dụng rồi chọn **1** để đối chiếu trạng thái và **11** để kiểm tra lại tổng kinh phí đã lưu.

6. Trên cùng đề tài P003 đang `Active`, chọn **13** và nhập `HD03`, `P003`, `2026-11-20`. Chọn **14** và nhập `HD03`, `R002`, `1` để gán Chủ tịch. Đổi vai bằng **2 → 3 → R002**, rồi chọn **15** và nhập `HD03`, `8.5`.
7. Đổi vai bằng **2 → 2 → R001**. Chọn **17**, nhập `PA03`, *Kết quả thử nghiệm phân loại văn bản*, `P003`, nhấn Enter để bỏ qua DOI, nhập *Tạp chí Khoa học*, `2026`. Nhập `1` ở ô số tác giả, rồi nhập `R001`, `1` (thứ tự tác giả) và `y` (tác giả liên hệ).
8. Chọn **18**, nhập `SP03`, *Bộ dữ liệu thử*, `P003`, `DT02`, `2`. Đổi về Chuyên viên bằng **2 → 1**, rồi chọn **16** và nhập `P003`. Thoát bằng **0**.
9. Mở lại, dùng **21** lọc `P003` và `Completed`. Đối chiếu 8,5 điểm, 250.000.000 đồng, bài báo PA03 và sản phẩm SP03. Menu **19** đếm hai bản ghi đầu ra cho P003. Menu **20** tổng hợp trên toàn hệ thống, trong đó SP03 đóng góp hai đơn vị cho loại DT02.

Chỉ mở một phiên khi ghi vào cùng tệp dữ liệu. Khi khởi động, kho kiểm tra sáu danh sách lõi, quan hệ tham chiếu, bằng chứng của đề tài đã hoàn thành và giới hạn tổng kinh phí. Ứng dụng dừng trước khi vào menu nếu một bài báo thiếu liên kết đề tài hoặc đề tài `Pending` đã có hội đồng hay đầu ra. Hãy giữ tệp lỗi để đối chiếu và khôi phục bản sao lưu hợp lệ. JSON cũ chỉ có phần A vẫn có thể mở nếu dữ liệu lõi hợp lệ. UC07 cần danh mục loại sản phẩm, vì vậy demo đầy đủ dùng seed A–B trong một thư mục chạy mới.

## 6. Phân công

- **Thành viên A - Nguyễn Khánh Toàn.** UC01–UC04, State và hai cách tổng hợp kinh phí theo Strategy.
- **Thành viên B - NGUYỄN VIỆT HOÀNG.** UC05–UC08, gồm hội đồng, chấm điểm và nghiệm thu, đầu ra nghiên cứu và báo cáo kết quả.

## 7. Về việc sử dụng AI

- Thành viên A dùng các công cụ AI để thảo luận sâu hơn về thiết kế, giải thích những điểm còn vướng và rà lại sơ đồ UML cho khớp với mã nguồn.
- AI hỗ trợ viết một số đoạn mã có thể tự động hóa, như xử lý dữ liệu, nhập liệu và chuẩn bị ca kiểm thử.
- Thành viên A tự viết các lớp, phương thức nghiệp vụ chính và phần cài đặt hướng đối tượng cốt lõi. Với các phần này, AI hỗ trợ rà soát, đề xuất và chỉnh sửa một số chỗ khi tích hợp, trong đó có điều kiện khóa danh sách hội đồng, cách tính tổng sản phẩm và kiểm tra dữ liệu khi nạp JSON.
