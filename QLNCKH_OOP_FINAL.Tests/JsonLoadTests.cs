using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

// Mỗi ca mở ứng dụng thật trong thư mục tạm, với một tệp JSON độc lập.
internal static class JsonLoadTests
{
    public static void Run()
    {
        var cases = new (string Name, bool Valid, Action<JsonObject> Prepare)[]
        {
            ("Seed tích hợp", true, _ => { }),
            ("Thiếu toàn bộ khóa", false, s => s.Clear()),
            ("Thiếu riêng danh mục nguồn", false, s => s.Remove("sources")),
            ("Mảng có giá trị null", false, s => s["projects"] = null),
            ("Đủ khóa, các mảng rỗng", true, s =>
            {
                foreach (var key in s.Select(item => item.Key).ToArray()) s[key] = new JsonArray();
            }),
            ("Dữ liệu A cũ trước nghiệm thu", true, s =>
            {
                foreach (var key in new[] { "deliverableTypes", "councils", "papers", "paperLinks", "deliverables" })
                    s.Remove(key);
            }),
            ("Completed thiếu điểm", false, s => CompleteSecondProject(s, null)),
            ("Completed thiếu đầu ra", false, s =>
            {
                CompleteSecondProject(s, 0m);
                s["papers"] = new JsonArray();
                s["paperLinks"] = new JsonArray();
                s["deliverables"] = new JsonArray();
            }),
            ("Completed có điểm 0 và đầu ra", true, s => CompleteSecondProject(s, 0m)),
            ("Pending có hội đồng", false, s => MakeSecondProjectPending(s, "councils")),
            ("Pending có liên kết bài báo", false, s => MakeSecondProjectPending(s, "paperLinks")),
            ("Pending có sản phẩm", false, s => MakeSecondProjectPending(s, "deliverables")),
            ("Có bài báo thiếu liên kết dù bài khác đã liên kết", false, s =>
            {
                var paper = s["papers"]![0]!.DeepClone();
                paper["id"] = "PA-CHUA-LIEN-KET";
                paper["doi"] = null;
                foreach (var author in paper["authors"]!.AsArray())
                    author!["paperId"] = "PA-CHUA-LIEN-KET";
                s["papers"]!.AsArray().Add(paper);
            }),
            ("Tràn tổng của một đề tài", false, s => Allocations(s, 0)[0]!["amount"] = decimal.MaxValue),
            ("Tràn tổng giữa hai đề tài", false, s => Allocations(s, 1)[0]!["amount"] = decimal.MaxValue),
            ("Tổng đúng bằng giới hạn decimal", true, s =>
                Allocations(s, 1)[0]!["amount"] = decimal.MaxValue - 130_000_000m)
        };

        var failures = 0;
        var directory = Directory.CreateTempSubdirectory("nckh-json-");
        try
        {
            var source = AppContext.BaseDirectory;
            foreach (var suffix in new[] { ".dll", ".deps.json", ".runtimeconfig.json" })
            {
                var name = "QLNCKH_OOP_FINAL" + suffix;
                File.Copy(Path.Combine(source, name), Path.Combine(directory.FullName, name));
            }
            var config = directory.CreateSubdirectory("config");
            File.Copy(Path.Combine(source, "config", "appsettings.json"), Path.Combine(config.FullName, "appsettings.json"));
            var data = Path.Combine(directory.CreateSubdirectory("data").FullName, "nckh.json");
            var seed = File.ReadAllText(Path.Combine(source, "Data", "seed.json"));
            foreach (var (name, valid, prepare) in cases)
            {
                try
                {
                    var snapshot = JsonNode.Parse(seed)!.AsObject();
                    prepare(snapshot);
                    File.WriteAllText(data, snapshot.ToJsonString());
                    var original = File.ReadAllBytes(data);
                    var start = new ProcessStartInfo("dotnet")
                    {
                        WorkingDirectory = directory.FullName, UseShellExecute = false,
                        RedirectStandardInput = true, RedirectStandardError = true, RedirectStandardOutput = true,
                        StandardErrorEncoding = Encoding.UTF8
                    };
                    start.ArgumentList.Add("QLNCKH_OOP_FINAL.dll");
                    using var app = Process.Start(start)!;
                    var stdout = app.StandardOutput.ReadToEndAsync();
                    var stderr = app.StandardError.ReadToEndAsync();
                    app.StandardInput.Close(); // EOF thoát menu sau khi nạp thành công.
                    if (!app.WaitForExit(15_000))
                    {
                        app.Kill(entireProcessTree: true);
                        app.WaitForExit();
                        throw new TimeoutException("Ứng dụng không kết thúc sau khi mở tệp.");
                    }
                    var error = stderr.GetAwaiter().GetResult();
                    stdout.GetAwaiter().GetResult();
                    if (app.ExitCode != (valid ? 0 : 1) || (!valid && !error.Contains("Data/seed.json")) ||
                        !File.ReadAllBytes(data).SequenceEqual(original))
                        throw new InvalidOperationException($"Mã thoát {app.ExitCode}. {error}");
                    Console.WriteLine($"PASS JSON: {name}");
                }
                catch (Exception ex)
                {
                    failures++;
                    Console.WriteLine($"FAIL JSON: {name} - {ex.Message}");
                }
            }
        }
        finally
        {
            directory.Delete(recursive: true); // Chỉ dọn thư mục tạm vừa tạo cho lần chạy này.
        }
        Console.WriteLine($"Kết quả JSON: {cases.Length - failures}/{cases.Length} ca đạt.");
        if (failures > 0) Environment.ExitCode = 1;
    }

    private static JsonArray Allocations(JsonObject data, int project) =>
        data["projects"]![project]!["allocations"]!.AsArray();

    // Giữ riêng từng loại kết quả của P002 để mỗi ca kiểm đúng một nhánh nạp.
    private static void MakeSecondProjectPending(JsonObject data, string keptResult)
    {
        var project = data["projects"]![1]!;
        project["status"] = "Pending";
        project["history"] = new JsonArray();
        foreach (var key in new[] { "councils", "paperLinks", "deliverables" })
        {
            if (key != keptResult) data[key] = new JsonArray();
        }
        if (keptResult != "paperLinks") data["papers"] = new JsonArray();
    }

    private static void CompleteSecondProject(JsonObject data, decimal? score)
    {
        var project = data["projects"]![1]!;
        project["status"] = "Completed";
        project["history"]!.AsArray().Add(new JsonObject
        {
            ["projectId"] = "P002", ["fromStatus"] = "Active", ["toStatus"] = "Completed",
            ["changedAt"] = "2026-11-25T12:00:00+07:00", ["actorId"] = "OFFICER"
        });
        data["councils"]![0]!["score"] = score;
    }
}
