# Sơ đồ UML hệ thống quản lý nghiên cứu khoa học ở đại học

## 1. Kiến trúc ba tầng

Các sơ đồ về nhân sự, đề tài, cấp kinh phí và hai báo cáo kinh phí mô tả mã hiện tại. Hội đồng, đầu ra nghiên cứu, Strategy về đầu ra và Factory Method thuộc thiết kế tích hợp. Xem [đặc tả nghiệp vụ](scope.md) và [README](../README.md).

Chương trình C# gồm ba tầng trong cùng project.

- **Presentation.** `ConsoleMenu` nhận lựa chọn và dữ liệu nhập, giữ vai trò hiện tại, gọi service và hiển thị kết quả hoặc lỗi. Menu khởi tạo kho JSON để đưa vào các service.
- **Business.** Chứa mô hình, service, các giao diện và `CurrentUserContext`. Xử lý quy tắc nghiệp vụ mà không đọc hoặc ghi console, tệp dữ liệu.
- **Data.** `InMemoryResearchRepository` hiện thực hai giao diện kho. `JsonResearchRepository` kế thừa lớp này và ghi đè `Save` để ghi `data/nckh.json`. Bộ kiểm thử dùng kho trong bộ nhớ.

```mermaid
graph TD
    subgraph Presentation ["Tầng Trình diễn (Presentation Layer)"]
        UI["ConsoleMenu<br/>(Menu điều khiển, Nhập/Xuất, Bắt lỗi biên)"]
    end

    subgraph Business ["Tầng Nghiệp vụ (Business Layer)"]
        subgraph Services ["Nghiệp vụ ứng dụng (Services)"]
            PS["ProjectService (A)"]
            PeoS["PeopleService (A)"]
            FS["FundingService (A)"]
            ES["EvaluationService (dự kiến B)"]
            OS["OutputService (dự kiến B)"]
            RS["ReportService"]
        end

        subgraph Patterns ["3 Design Patterns"]
            StatePat["State Pattern<br/>(Vòng đời đề tài: Pending/Active/Completed)"]
            StratPat["Strategy<br/>(Kinh phí đã có, đầu ra dự kiến)"]
            FactPat["Factory Method<br/>(Dự kiến cho UC07)"]
        end

        subgraph DomainModels ["Mô hình nghiệp vụ"]
            Entities["Department, Researcher, ResearchProject,<br/>FundingAllocation và các lớp liên quan"]
            PlannedEntities["EvaluationCouncil, Paper, ProjectDeliverable<br/>(Dự kiến B)"]
            CTX["CurrentUserContext<br/>(Researcher | Officer | CouncilChair)"]
        end

        subgraph RepoInterfaces ["Giao diện"]
            IPeopleRepo["IPeopleRepository"]
            IProjectRepo["IProjectRepository"]
            IResultRepo["IResultRepository<br/>(Dự kiến phân hệ B)"]
            IEvidence["ICompletionEvidence<br/>(Hợp đồng tích hợp A-B)"]
        end
    end

    subgraph Data ["Tầng Dữ liệu (Data Layer)"]
        JsonRepo["JsonResearchRepository<br/>(Nạp và lưu nckh.json)"]
        MemRepo["InMemoryResearchRepository<br/>(Phục vụ Unit Test độc lập)"]
        Snapshot["AppSnapshot và DTO<br/>(Giữ trường bổ sung ở gốc JSON)"]
    end

    UI --> Services
    UI --> CTX
    UI --> JsonRepo
    Services --> DomainModels
    Services --> Patterns
    Services --> RepoInterfaces
    MemRepo -.-> IPeopleRepo
    MemRepo -.-> IProjectRepo
    JsonRepo -->|Kế thừa| MemRepo
    ES -.-> IResultRepo
    OS -.-> IResultRepo
    JsonRepo --> Snapshot
```

---

## 2. Sơ đồ lớp nghiệp vụ

Thiết kế tổng thể có 16 kiểu nghiệp vụ cụ thể, gồm 9 kiểu trong mã hiện tại và 7 kiểu dự kiến cho nghiệm thu và đầu ra. `ProjectStatusHistory` nằm trong nhóm 9 kiểu của A. `ResearchOutput` là lớp trừu tượng dự kiến cho phần đầu ra.

### Các kiểu nghiệp vụ
1. **Dữ liệu nền**: `Department`, `Researcher`, `DepartmentHeadTerm`, `ProjectCategory`
2. **Đề tài**: `ResearchProject`, `ProjectParticipation`, `ProjectStatusHistory`
3. **Kinh phí**: `FundingSource`, `FundingAllocation`
4. **Nghiệm thu**: `EvaluationCouncil`, `CouncilMembership`
5. **Đầu ra**: `DeliverableType`, `ProjectDeliverable`, `Paper`, `PaperAuthor`, `ProjectPaperLink`

```mermaid
classDiagram
    %% Lớp cơ sở trừu tượng
    class ResearchOutput {
        <<abstract>>
        +string Id
        +string Title
        +Validate()* void
        +GetDisplaySummary()* string
    }

    %% Dữ liệu nền và nhân sự (A)
    class Department {
        +string Id
        +string Name
        +DateOnly CreatedAt
        +Rename(string name) void
    }

    class Researcher {
        +string Id
        +string DepartmentId
        +string FullName
        +string Email
        +string? AcademicTitle
        +string? Phone
        +string? Organization
        +bool IsInstructor
        +Update(...) void
    }

    class DepartmentHeadTerm {
        +string Id
        +string DepartmentId
        +string ResearcherId
        +DateOnly From
        +DateOnly? To
    }

    class ProjectCategory {
        +string Id
        +string Name
    }

    %% Đề tài và kinh phí (A)
    class ResearchProject {
        +string Id
        +string CategoryId
        +string Title
        +DateOnly StartDate
        +DateOnly? EndDate
        +ProjectStatus Status
        +IReadOnlyList~ProjectParticipation~ Members
        +IReadOnlyList~FundingAllocation~ Allocations
        +IReadOnlyList~ProjectStatusHistory~ History
        +decimal TotalFunding
        +AddMember(string researcherId, ProjectRole role) void
        +AddAllocation(FundingAllocation allocation) void
        +Approve(string actorId, DateTimeOffset at) void
        +Complete(string actorId, DateTimeOffset at) void
        ~ChangeStatus(ProjectStatus next, string actorId, DateTimeOffset at) void
    }

    class ProjectParticipation {
        +string ProjectId
        +string ResearcherId
        +ProjectRole Role
    }

    class ProjectStatusHistory {
        +string ProjectId
        +ProjectStatus FromStatus
        +ProjectStatus ToStatus
        +DateTimeOffset ChangedAt
        +string ActorId
    }

    class FundingSource {
        +string Id
        +string Name
        +string Type
    }

    class FundingAllocation {
        +string Id
        +string ProjectId
        +string SourceId
        +decimal Amount
        +DateOnly AllocatedAt
    }

    %% Nghiệm thu (dự kiến B)
    class EvaluationCouncil {
        +string Id
        +string ProjectId
        +DateOnly EvaluationDate
        +decimal? Score
        +IReadOnlyList~CouncilMembership~ Members
        +AddMember(string researcherId, CouncilRole role) void
        +RecordScore(string actorResearcherId, decimal score) void
    }

    class CouncilMembership {
        +string CouncilId
        +string ResearcherId
        +CouncilRole Role
    }

    %% Đầu ra nghiên cứu (dự kiến B)
    class DeliverableType {
        +string Id
        +string Name
    }

    class ProjectDeliverable {
        +string ProjectId
        +string TypeId
        +int Quantity
        +Validate() void
        +GetDisplaySummary() string
    }

    class Paper {
        +string? Doi
        +string Publisher
        +int PublishedYear
        +IReadOnlyList~PaperAuthor~ Authors
        +AddAuthor(string researcherId, int order, bool isCorresponding) void
        +Validate() void
        +GetDisplaySummary() string
    }

    class PaperAuthor {
        +string PaperId
        +string ResearcherId
        +int AuthorOrder
        +bool IsCorresponding
    }

    class ProjectPaperLink {
        +string ProjectId
        +string PaperId
    }

    %% Quan hệ Kế thừa (Inheritance)
    ResearchOutput <|-- ProjectDeliverable : Kế thừa (Generalization)
    ResearchOutput <|-- Paper : Kế thừa (Generalization)

    %% Liên kết qua mã và quan hệ sở hữu danh sách
    Department "1" <-- "0..*" Researcher : Trực thuộc
    Department "1" <-- "0..*" DepartmentHeadTerm : Lịch sử nhiệm kỳ
    Researcher "1" <-- "0..*" DepartmentHeadTerm : Lãnh đạo

    ProjectCategory "1" <-- "0..*" ResearchProject : Thuộc loại

    ResearchProject "1" *-- "0..*" ProjectParticipation : Danh sách thành viên
    ResearchProject "1" *-- "0..*" ProjectStatusHistory : Lịch sử trạng thái
    ResearchProject "1" <-- "0..*" FundingAllocation : Các đợt cấp tiền

    Researcher "1" <-- "0..*" ProjectParticipation : Đảm nhiệm vai trò
    FundingSource "1" <-- "0..*" FundingAllocation : Nguồn cấp tiền

    %% Quan hệ Nghiệm thu
    ResearchProject "1" <-- "0..1" EvaluationCouncil : Tối đa 1 hội đồng
    EvaluationCouncil "1" *-- "1..*" CouncilMembership : Thành viên hội đồng
    Researcher "1" <-- "0..*" CouncilMembership : Tham gia hội đồng

    %% Quan hệ Đầu ra
    ResearchProject "1" o-- "0..*" ProjectDeliverable : Gắn trực tiếp đề tài
    DeliverableType "1" <-- "0..*" ProjectDeliverable : Phân loại sản phẩm

    Paper "1" *-- "1..*" PaperAuthor : Tác giả bài báo
    Researcher "1" <-- "0..*" PaperAuthor : Là tác giả

    ResearchProject "1" <-- "0..*" ProjectPaperLink : Liên kết
    Paper "1" <-- "0..*" ProjectPaperLink : Liên kết
```

---

## 3. Mẫu thiết kế

- **Thành viên A.** State cho vòng đời đề tài và hai Strategy tổng hợp kinh phí.
- **Thành viên B.** Factory Method và Strategy về đầu ra trong thiết kế tích hợp.

### 3.1. State cho vòng đời đề tài
- `ResearchProject.StateFor` chọn State theo trạng thái hiện tại. `PendingState` duyệt đề tài đang chờ có đúng một chủ nhiệm, `ActiveState` hoàn thành đề tài đang thực hiện, còn `CompletedState` từ chối cả hai thao tác.
- Khi thêm trạng thái, cần bổ sung lớp State, cập nhật `ProjectStatus`, `StateFor` và quy tắc phục hồi lịch sử từ JSON.

```mermaid
classDiagram
    class ResearchProject {
        +string Id
        +ProjectStatus Status
        +Approve(string actorId, DateTimeOffset at) void
        +Complete(string actorId, DateTimeOffset at) void
        ~ChangeStatus(ProjectStatus next, string actorId, DateTimeOffset at) void
        -StateFor(ProjectStatus status)$ IProjectState
    }

    class IProjectState {
        <<interface>>
        +Approve(ResearchProject project, string actorId, DateTimeOffset at)* void
        +Complete(ResearchProject project, string actorId, DateTimeOffset at)* void
    }

    class PendingState {
        +Approve(ResearchProject project, string actorId, DateTimeOffset at) void
        +Complete(ResearchProject project, string actorId, DateTimeOffset at) void
    }

    class ActiveState {
        +Approve(ResearchProject project, string actorId, DateTimeOffset at) void
        +Complete(ResearchProject project, string actorId, DateTimeOffset at) void
    }

    class CompletedState {
        +Approve(ResearchProject project, string actorId, DateTimeOffset at) void
        +Complete(ResearchProject project, string actorId, DateTimeOffset at) void
    }

    ResearchProject ..> IProjectState : Ủy quyền theo trạng thái hiện tại
    IProjectState <|.. PendingState : Hiện thực
    IProjectState <|.. ActiveState : Hiện thực
    IProjectState <|.. CompletedState : Hiện thực
```

### 3.2. Strategy cho báo cáo
- Hai báo cáo kinh phí dùng chung `IReportStrategy` và kiểu dòng `ReportRow`. `ReportService.Generate` kiểm quyền chuyên viên rồi gọi `BuildRows()` của Strategy được menu chọn.
- **Đã cài đặt ở phần A**
  - `FundingByProjectStrategy`: Tổng hợp kinh phí theo từng đề tài.
  - `FundingBySourceStrategy`: Nhóm các đợt cấp theo nguồn và cộng tiền cho từng nhóm. Nguồn chưa cấp kinh phí không xuất hiện trong bảng.
- **Dự kiến ở phần B**
  - `OutputByProjectStrategy`: Thống kê số lượng bài báo, sản phẩm theo từng đề tài.
  - `OutputByTypeStrategy`: Thống kê sản phẩm nghiên cứu theo chủng loại (Mở rộng).

```mermaid
classDiagram
    class ReportRow {
        +string Key
        +string Label
        +decimal Value
        +string Unit
        +string? Detail
    }

    class IReportStrategy {
        <<interface>>
        +string Title
        +BuildRows()* IReadOnlyList~ReportRow~
    }

    class FundingByProjectStrategy {
        +string Title
        +BuildRows() IReadOnlyList~ReportRow~
    }

    class FundingBySourceStrategy {
        +string Title
        +BuildRows() IReadOnlyList~ReportRow~
    }

    class OutputByProjectStrategy {
        <<planned>>
        -IProjectRepository _projects
        -IResultRepository _results
        +string Title
        +BuildRows() IReadOnlyList~ReportRow~
    }

    class OutputByTypeStrategy {
        <<planned>>
        -IResultRepository _results
        +string Title
        +BuildRows() IReadOnlyList~ReportRow~
    }

    class ReportService {
        +Generate(CurrentUserContext user, IReportStrategy strategy) IReadOnlyList~ReportRow~
    }

    ReportService ..> IReportStrategy : Gọi chiến lược nhận từ menu
    IReportStrategy ..> ReportRow : Trả về dữ liệu
    IReportStrategy <|.. FundingByProjectStrategy : Hiện thực (A)
    IReportStrategy <|.. FundingBySourceStrategy : Hiện thực (A)
    IReportStrategy <|.. OutputByProjectStrategy : Dự kiến (B)
    IReportStrategy <|.. OutputByTypeStrategy : Mở rộng dự kiến (B)
```

### 3.3. Factory Method cho UC07 — thiết kế tích hợp của B

`ResearchOutputCreator` khai báo `CreateOutput(id, title)`. Hai lớp con ghi đè phương thức để tạo `Paper` hoặc `ProjectDeliverable`. Đầu ra kiểm tra dữ liệu của chính nó. Các điều kiện cần tra kho, như DOI trùng hoặc mã loại sản phẩm không tồn tại, thuộc trách nhiệm của service.

```mermaid
classDiagram
    class ResearchOutput {
        <<abstract>>
        +string Id
        +string Title
        +Validate()* void
        +GetDisplaySummary()* string
    }

    class Paper {
        +string? Doi
        +string Publisher
        +int PublishedYear
        +Validate() void
        +GetDisplaySummary() string
    }

    class ProjectDeliverable {
        +string ProjectId
        +string TypeId
        +int Quantity
        +Validate() void
        +GetDisplaySummary() string
    }

    class ResearchOutputCreator {
        <<abstract>>
        +CreateOutput(string id, string title)* ResearchOutput
    }

    class PaperCreator {
        +string? Doi
        +string Publisher
        +int PublishedYear
        +CreateOutput(string id, string title) ResearchOutput
    }

    class DeliverableCreator {
        +string ProjectId
        +string TypeId
        +int Quantity
        +CreateOutput(string id, string title) ResearchOutput
    }

    ResearchOutput <|-- Paper
    ResearchOutput <|-- ProjectDeliverable
    ResearchOutputCreator <|-- PaperCreator
    ResearchOutputCreator <|-- DeliverableCreator
    PaperCreator ..> Paper : Khởi tạo (Creates)
    DeliverableCreator ..> ProjectDeliverable : Khởi tạo (Creates)
```

---

## 4. Service và giao diện kho dữ liệu

Service nhận giao diện thay vì tạo kho cụ thể. Sơ đồ ghi các phương thức chính. `IResultRepository`, `EvaluationService` và `OutputService` là phần dự kiến tích hợp. `ProjectService.Complete` nhận `ICompletionEvidence` từ bên gọi. Bộ kiểm thử A dùng một đối tượng giả lập giao diện này.

```mermaid
classDiagram
    %% Repositories
    class IPeopleRepository {
        <<interface>>
        +FindDepartment(string id) Department?
        +FindResearcher(string id) Researcher?
        +IReadOnlyCollection~Researcher~ Researchers
        +IReadOnlyCollection~DepartmentHeadTerm~ HeadTerms
        +Save(Department department) void
        +Save(Researcher researcher) void
    }

    class IProjectRepository {
        <<interface>>
        +FindCategory(string id) ProjectCategory?
        +FindProject(string id) ResearchProject?
        +FindSource(string id) FundingSource?
        +IReadOnlyCollection~ResearchProject~ Projects
        +Save(ResearchProject project) void
        +Save(FundingSource source) void
    }

    class IResultRepository {
        <<interface>>
        +FindCouncil(string id) EvaluationCouncil
        +FindCouncilByProject(string projectId) EvaluationCouncil
        +FindPaper(string id) Paper
        +FindDeliverablesByProject(string projectId) IReadOnlyCollection~ProjectDeliverable~
        +Save(EvaluationCouncil council) void
        +Save(Paper paper) void
        +Save(ProjectDeliverable deliverable) void
    }

    class ICompletionEvidence {
        <<interface>>
        +HasScoredCouncil(string projectId) bool
        +HasResearchOutput(string projectId) bool
    }

    %% Services
    class PeopleService {
        +SaveDepartment(CurrentUserContext user, string id, string name, DateOnly createdAt) Department
        +SaveResearcher(CurrentUserContext user, string id, string departmentId, string fullName, string email, ...) Researcher
        +GetHeadHistory(CurrentUserContext user, string departmentId) IReadOnlyList~DepartmentHeadTerm~
    }

    class ProjectService {
        +ProposeProject(CurrentUserContext user, string id, string categoryId, string title, DateOnly startDate, DateOnly? endDate) ResearchProject
        +AssignMember(CurrentUserContext user, string projectId, string researcherId, ProjectRole role) void
        +Approve(CurrentUserContext user, string projectId) void
        +Complete(CurrentUserContext user, string projectId, ICompletionEvidence evidence) void
    }

    class FundingService {
        +SaveSource(CurrentUserContext user, string id, string name, string type) FundingSource
        +Allocate(CurrentUserContext user, string id, string projectId, string sourceId, decimal amount, DateOnly allocatedAt) FundingAllocation
        +TotalForProject(string projectId) decimal
    }

    class EvaluationService {
        -IProjectRepository _projects
        -IPeopleRepository _people
        -IResultRepository _results
        +CreateCouncil(CurrentUserContext user, string councilId, string projectId, DateOnly date) void
        +AssignMember(CurrentUserContext user, string councilId, string researcherId, CouncilRole role) void
        +RecordScore(CurrentUserContext user, string councilId, decimal score) void
    }

    class OutputService {
        -IProjectRepository _projects
        -IResultRepository _results
        +AddPaper(CurrentUserContext user, string projectId, PaperCreator creator) void
        +AddDeliverable(CurrentUserContext user, string projectId, DeliverableCreator creator) void
    }

    class ReportService {
        +Generate(CurrentUserContext user, IReportStrategy strategy) IReadOnlyList~ReportRow~
    }

    class InMemoryResearchRepository {
        +Save(Department value) void
        +Save(Researcher value) void
        +Save(ResearchProject value) void
        +Save(FundingSource value) void
    }

    class JsonResearchRepository {
        -string _dataPath
        -Load(AppSnapshot snapshot) void
        -Persist() void
    }

    %% Relationships
    PeopleService --> IPeopleRepository
    ProjectService --> IPeopleRepository
    ProjectService --> IProjectRepository
    ProjectService ..> ICompletionEvidence : Kiểm tra bằng chứng
    FundingService --> IProjectRepository
    EvaluationService ..> IProjectRepository : Dự kiến
    EvaluationService ..> IPeopleRepository : Dự kiến
    EvaluationService ..> IResultRepository : Dự kiến
    OutputService ..> IResultRepository : Dự kiến
    OutputService ..> IProjectRepository : Dự kiến
    ReportService ..> IReportStrategy

    InMemoryResearchRepository ..|> IPeopleRepository
    InMemoryResearchRepository ..|> IProjectRepository
    InMemoryResearchRepository <|-- JsonResearchRepository
```

---

## 5. Sơ đồ tuần tự

Bốn sơ đồ minh họa các luồng tiêu biểu. Hai sơ đồ đầu mô tả thao tác đang chạy của A. Hai sơ đồ sau mô tả cách B dự kiến tích hợp.

### 5.1. UC02–UC03 — Đề xuất, gán chủ nhiệm và duyệt
Thể hiện sự phối hợp giữa `ProjectService`, `ResearchProject`, `PendingState` và lưu vết vào `ProjectStatusHistory`.

```mermaid
sequenceDiagram
    actor NNC as Nhà nghiên cứu (Researcher)
    actor CV as Chuyên viên (Officer)
    participant UI as ConsoleMenu
    participant PS as ProjectService
    participant People as IPeopleRepository
    participant Repo as IProjectRepository
    participant P as ResearchProject
    participant State as PendingState

    %% UC02: Đề xuất
    NNC->>UI: 1. Nhập thông tin đề tài (Menu 5)
    UI->>PS: ProposeProject(user, id, catId, title, start, end)
    PS->>PS: Kiểm quyền Researcher
    PS->>People: FindResearcher(user.ResearcherId)
    PS->>Repo: FindCategory(catId)
    PS->>PS: Chuẩn hóa mã đề tài
    PS->>Repo: FindProject(normalizedId)
    PS->>P: new ResearchProject(normalizedId, catId, title, start, end)
    PS->>Repo: Save(project)
    UI-->>NNC: Tạo thành công, trạng thái Pending

    %% UC03: Gán chủ nhiệm
    CV->>UI: 2. Gán chủ nhiệm đề tài (Menu 6)
    UI->>PS: AssignMember(user, id, resId, ProjectRole.Leader)
    PS->>PS: Kiểm quyền (Role == Officer)
    PS->>People: FindResearcher(resId)
    PS->>Repo: FindProject(id)
    PS->>P: AddMember(resId, ProjectRole.Leader)
    P->>P: Kiểm tra Pending, thành viên trùng và chủ nhiệm
    PS->>Repo: Save(project)
    UI-->>CV: Đã gán chủ nhiệm

    %% UC03: Phê duyệt
    CV->>UI: 3. Phê duyệt đề tài (Menu 7)
    UI->>PS: Approve(user, id)
    PS->>PS: Kiểm quyền Officer
    PS->>Repo: FindProject(id)
    PS->>P: Approve("OFFICER", timestamp)
    P->>P: StateFor(Status)
    P->>State: Approve(project, "OFFICER", timestamp)
    State->>State: Kiểm tra Pending và đúng một chủ nhiệm
    State->>P: ChangeStatus(Active, "OFFICER", timestamp)
    P->>P: Ghi nhận vào ProjectStatusHistory
    PS->>Repo: Save(project)
    UI-->>CV: Đề tài đã chuyển sang Active
```

### 5.2. UC04 — Khai báo nguồn và phân bổ kinh phí

Sơ đồ theo luồng thành công. Service từ chối mã trùng và kiểm tra tổng kinh phí trước khi thêm khoản cấp vào đề tài.

```mermaid
sequenceDiagram
    actor CV as Chuyên viên (Officer)
    participant UI as ConsoleMenu
    participant FS as FundingService
    participant Repo as IProjectRepository
    participant P as ResearchProject

    %% Khai báo nguồn
    CV->>UI: 1. Thêm nguồn tài trợ (Menu 8)
    UI->>FS: SaveSource(user, id, name, type)
    FS->>FS: Kiểm quyền Officer
    FS->>FS: Chuẩn hóa mã nguồn
    FS->>Repo: FindSource(normalizedId)
    FS->>Repo: Save(new FundingSource(normalizedId, name, type))
    UI-->>CV: Khai báo nguồn thành công

    %% Phân bổ kinh phí
    CV->>UI: 2. Phân bổ kinh phí (Menu 9)
    UI->>FS: Allocate(user, allocId, projId, sourceId, amount, date)
    FS->>FS: Kiểm quyền Officer
    FS->>Repo: FindProject(projId)
    FS->>Repo: FindSource(sourceId)
    FS->>FS: Chuẩn hóa mã cấp
    FS->>Repo: Đọc Projects
    FS->>FS: Kiểm tra mã đợt cấp trên toàn hệ thống
    FS->>FS: Tạo FundingAllocation, kiểm tra số tiền dương
    FS->>Repo: Đọc Projects để tính tổng
    FS->>FS: Kiểm tra tổng mới còn trong phạm vi decimal
    FS->>P: AddAllocation(allocation)
    P->>P: Kiểm tra mã đề tài và mã khoản cấp trong đề tài
    FS->>Repo: Save(project)
    UI->>FS: TotalForProject(projId)
    FS->>Repo: FindProject(projId)
    FS->>P: Đọc TotalFunding
    UI-->>CV: Phân bổ thành công, hiển thị tổng kinh phí cập nhật
```

### 5.3. UC05–UC06 — Hội đồng và nghiệm thu dự kiến tích hợp

Chủ tịch hội đồng ghi điểm. Chuyên viên gọi `ProjectService.Complete` với bằng chứng lấy từ dữ liệu nghiệm thu và đầu ra. Nhánh hoàn thành minh họa đề tài đang `Active`.

```mermaid
sequenceDiagram
    actor CV as Chuyên viên (Officer)
    actor CT as Chủ tịch HĐ (CouncilChair)
    participant UI as ConsoleMenu
    participant ES as EvaluationService
    participant PS as ProjectService
    participant Repo as IProjectRepository
    participant Evidence as ICompletionEvidence
    participant State as ActiveState
    participant P as ResearchProject
    participant C as EvaluationCouncil

    %% UC05: Lập hội đồng
    CV->>UI: 1. Lập hội đồng nghiệm thu (Menu B)
    UI->>ES: CreateCouncil(user, councilId, projId, date)
    ES->>ES: Kiểm tra đề tài Active và chưa có hội đồng
    ES->>C: new EvaluationCouncil(councilId, projId, date)
    UI-->>CV: Tạo hội đồng thành công

    %% UC06: Ghi điểm nghiệm thu
    CT->>UI: 2. Nhập điểm đánh giá (Menu B)
    UI->>ES: RecordScore(user, councilId, score)
    ES->>ES: Kiểm tra đúng Chủ tịch hội đồng và điểm từ 0 đến 10
    ES->>C: RecordScore(user.ResearcherId, score)
    UI-->>CT: Đã lưu điểm nghiệm thu

    %% UC06: Hoàn thành đề tài
    CV->>UI: 3. Đóng và hoàn thành đề tài (Menu B)
    UI->>PS: Complete(user, projId, Evidence)
    PS->>PS: Kiểm quyền Officer
    PS->>Repo: FindProject(projId)
    PS->>Evidence: HasScoredCouncil(projId)
    opt Đã có điểm hội đồng
        PS->>Evidence: HasResearchOutput(projId)
    end
    alt Đủ điều kiện điểm và đầu ra
        PS->>P: Complete("OFFICER", timestamp)
        P->>P: StateFor(Status)
        P->>State: Complete(project, "OFFICER", timestamp)
        State->>P: ChangeStatus(Completed, "OFFICER", timestamp)
        P->>P: Ghi nhận ProjectStatusHistory
        PS->>Repo: Save(project)
        UI-->>CV: Đề tài chính thức chuyển trạng thái Completed
    else Thiếu điểm hoặc thiếu đầu ra
        PS-->>UI: Ném ngoại lệ InvalidOperationException
        UI-->>CV: Báo lỗi: Chưa đủ điều kiện nghiệm thu/đầu ra
    end
```

### 5.4. UC07 — Ghi nhận đầu ra qua Factory Method dự kiến

Factory tạo đối tượng theo loại đầu ra. Service kiểm tra tham chiếu và các ràng buộc cần tra kho trước khi lưu.

```mermaid
sequenceDiagram
    actor NNC as Nhà nghiên cứu (Researcher)
    participant UI as ConsoleMenu
    participant OS as OutputService
    participant Factory as PaperCreator / DeliverableCreator
    participant Out as ResearchOutput (Paper / ProjectDeliverable)
    participant Repo as IResultRepository

    NNC->>UI: 1. Chọn loại đầu ra (Bài báo hoặc Sản phẩm)
    alt Chọn tạo Bài báo
        UI->>OS: AddPaper(user, projId, paperCreator)
        OS->>OS: Kiểm quyền và người dùng tham gia đề tài
        OS->>Repo: Tra DOI nếu có
        OS->>Factory: CreateOutput(paperId, title)
        Factory->>Out: new Paper(...)
        OS->>Out: Validate() (danh sách tác giả)
        OS->>Repo: Lưu bài báo và liên kết đề tài
    else Chọn tạo Sản phẩm bàn giao
        UI->>OS: AddDeliverable(user, projId, deliverableCreator)
        OS->>OS: Kiểm quyền và người dùng tham gia đề tài
        OS->>Repo: Tra loại sản phẩm
        OS->>Factory: CreateOutput(deliverableId, title)
        Factory->>Out: new ProjectDeliverable(...)
        OS->>Out: Validate() (số lượng dương)
        OS->>Repo: Save(deliverable)
    end
    UI-->>NNC: Đã ghi nhận đầu ra và liên kết đề tài thành công
```
