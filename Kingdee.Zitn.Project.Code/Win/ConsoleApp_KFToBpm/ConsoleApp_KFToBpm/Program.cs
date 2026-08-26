using System.Text;
using System.Text.Json;

// ============ 共 108 个单号 ============
string[] sequenceNos = new[]
{
    "KFPDJ-000000066", "KFPDJ-000000071", "KFPDJ-000000075", "KFPDJ-000000077",
    "KFPDJ-000000078", "KFPDJ-000000080", "KFPDJ-000000083", "KFPDJ-000000087",
    "KFPDJ-000000088", "KFPDJ-000000089", "KFPDJ-000000090", "KFPDJ-000000091",
    "KFPDJ-000000092", "KFPDJ-000000096", "KFPDJ-000000097", "KFPDJ-000000099",
    "KFPDJ-000000103", "KFPDJ-000000104", "KFPDJ-000000105", "KFPDJ-000000109",
    "KFPDJ-000000111", "KFPDJ-000000113", "KFPDJ-000000116", "KFPDJ-000000120",
    "KFPDJ-000000121", "KFPDJ-000000122", "KFPDJ-000000123", "KFPDJ-000000124",
    "KFPDJ-000000127", "KFPDJ-000000128", "KFPDJ-000000130", "KFPDJ-000000133",
    "KFPDJ-000000137", "KFPDJ-000000138", "KFPDJ-000000141", "KFPDJ-000000142",
    "KFPDJ-000000145", "KFPDJ-000000146", "KFPDJ-000000148", "KFPDJ-000000150",
    "KFPDJ-000000155", "KFPDJ-000000156", "KFPDJ-000000157", "KFPDJ-000000165",
    "KFPDJ-000000166", "KFPDJ-000000167", "KFPDJ-000000168", "KFPDJ-000000169",
    "KFPDJ-000000171", "KFPDJ-000000173", "KFPDJ-000000174", "KFPDJ-000000178",
    "KFPDJ-000000179", "KFPDJ-000000180", "KFPDJ-000000182", "KFPDJ-000000186",
    "KFPDJ-000000187", "KFPDJ-000000188", "KFPDJ-000000189", "KFPDJ-000000190",
    "KFPDJ-000000191", "KFPDJ-000000192", "KFPDJ-000000193", "KFPDJ-000000194",
    "KFPDJ-000000195", "KFPDJ-000000196", "KFPDJ-000000198", "KFPDJ-000000200",
    "KFPDJ-000000202", "KFPDJ-000000203", "KFPDJ-000000205", "KFPDJ-000000207",
    "KFPDJ-000000208", "KFPDJ-000000209", "KFPDJ-000000211", "KFPDJ-000000213",
    "KFPDJ-000000214", "KFPDJ-000000215", "KFPDJ-000000218", "KFPDJ-000000219",
    "KFPDJ-000000221", "KFPDJ-000000223", "KFPDJ-000000225", "KFPDJ-000000226",
    "KFPDJ-000000227", "KFPDJ-000000228", "KFPDJ-000000229", "KFPDJ-000000232",
    "KFPDJ-000000233", "KFPDJ-000000235", "KFPDJ-000000236", "KFPDJ-000000238",
    "KFPDJ-000000239", "KFPDJ-000000240", "KFPDJ-000000242", "KFPDJ-000000244",
    "KFPDJ-000000249", "KFPDJ-000000252", "KFPDJ-000000257", "KFPDJ-000000259",
    "KFPDJ-000000261", "KFPDJ-000000262", "KFPKS-000000001", "KFPKS-000000005",
    "KFPKS-000000006", "KFPKS-000000008", "KFPKS-000000009", "KFPKS-000000010",
};

const string ApiUrl = "http://10.0.32.10:8769/api/public/aftersale/noticeSend";

using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30)
};

int successCount = 0;
int failCount = 0;

foreach (string sequenceNo in sequenceNos)
{
    if (string.IsNullOrWhiteSpace(sequenceNo))
        continue;

    var (code, msg, ok) = await CallApiAsync(httpClient, ApiUrl, sequenceNo);

    string status = ok ? "成功" : "失败";
    if (ok) successCount++;
    else failCount++;

    Console.WriteLine($"单号 {sequenceNo} 调用{status}，返回 code: {code}, msg: {msg}");
}

Console.WriteLine();
Console.WriteLine($"执行完成：共 {sequenceNos.Length} 个单号，成功 {successCount} 个，失败 {failCount} 个");


static async Task<(string code, string msg, bool ok)> CallApiAsync(
    HttpClient httpClient,
    string url,
    string sequenceNo)
{
    try
    {
        var payload = new { sequenceNo };
        string jsonBody = JsonSerializer.Serialize(payload);

        using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        using var response = await httpClient.PostAsync(url, content);

        string responseText = await response.Content.ReadAsStringAsync();

        var (code, msg) = ParseCodeMsg(responseText);

        // 响应体里没有 code 字段时，退回显示 HTTP 状态码
        if (string.IsNullOrEmpty(code))
            code = $"HTTP {(int)response.StatusCode}";

        bool ok = response.IsSuccessStatusCode && IsSuccess(code, msg);

        return (code, msg, ok);
    }
    catch (Exception ex)
    {
        return ("异常", ex.Message, false);
    }
}

static (string code, string msg) ParseCodeMsg(string json)
{
    try
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string code = GetField(root, "code", "errcode", "Code", "status");
        string msg = GetField(root, "msg", "message", "errmsg", "Msg", "Message");

        // 两个字段都取不到时，直接展示原始响应，方便排查
        if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(msg))
            return ("", json);

        return (code, msg);
    }
    catch
    {
        return ("", json);
    }
}

static string GetField(JsonElement root, params string[] names)
{
    foreach (string name in names)
    {
        if (root.TryGetProperty(name, out var el))
        {
            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString() ?? "",
                JsonValueKind.Number => el.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => el.GetRawText()
            };
        }
    }
    return "";
}

static bool IsSuccess(string code, string msg)
{
    if (code == "200" || code == "0")
        return true;

    if (!string.IsNullOrEmpty(msg)
        && (msg.Contains("成功") || msg.Contains("success", StringComparison.OrdinalIgnoreCase)))
        return true;

    return false;
}
