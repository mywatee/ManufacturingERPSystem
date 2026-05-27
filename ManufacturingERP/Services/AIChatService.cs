using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ManufacturingERP.Core;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public class AIChatService : IAIChatService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly IProductionService _production;
    private readonly IWarehouseService _warehouse;
    private readonly IHRService _hr;
    private readonly IFinanceService _finance;
    private readonly IQualityControlService _qc;
    private readonly IAuthService _auth;
    private readonly IPermissionService _permissions;
    private readonly IAuditLogService _auditLog;
    private readonly IRagService _ragService;
    private readonly IMasterDataService _masterData;
    private readonly IPartnerService _partner;
    private readonly IActivityService _activity;

    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash";
    private const string ApiUrl = BaseUrl + ":generateContent";
    private const string StreamUrl = BaseUrl + ":streamGenerateContent?alt=sse";
    private const int MaxMessageLength = 1000;
    private const int RateLimitSeconds = 5;
    private const int MaxHistoryCount = 20;
    private const int MaxRetries = 3;

    private string? _lastTopic;
    private static readonly Dictionary<string, (DateTime CachedAt, string Data)> _prefetchCache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private static readonly Regex EntityCodeRegex = new(@"[A-Z]{2,6}-?\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string SystemPrompt = """
        Bạn là chuyên gia trợ lý AI của hệ thống Manufacturing ERP, được thiết kế để hỗ trợ quản lý sản xuất toàn diện. 
        Bạn có phong cách trả lời chuyên nghiệp, chi tiết và dễ hiểu.
        
        === KIẾN THỨC NỀN TẢNG ===
         Bạn am hiểu về quy trình sản xuất, quản lý kho, nhân sự, tài chính, định mức vật tư (BOM), quy trình gia công (Routing) và kiểm soát chất lượng trong môi trường nhà máy sản xuất.
         
         === DỮ LIỆU CÓ THỂ TRUY CẬP ===
         Bạn có hai nguồn dữ liệu:
         1. CƠ SỞ DỮ LIỆU (qua công cụ): 
            - Sản xuất: lệnh sx, sản phẩm, BOM/định mức, Routing/quy trình, tiến độ, biểu đồ
            - Kho: tồn kho, vật tư, cảnh báo, giao dịch, đối tác/khách hàng/nhà cung cấp
            - Nhân sự: nhân viên, chấm công, lương, ca làm, lịch làm việc, tổng hợp chấm công
            - Tài chính: giao dịch, hóa đơn, dòng tiền, chi phí sản xuất
            - Chất lượng: QC, thống kê lỗi, xu hướng
            - Khác: lịch sử hoạt động, nhật ký hệ thống
         2. TÀI LIỆU THAM KHẢO (RAG): Nội dung từ tài liệu đã upload (SOP, hợp đồng, báo cáo, quy trình...)
        
         === CÁCH TRẢ LỜI ===
         Áp dụng các nguyên tắc sau, TÙY BIẾN theo ngữ cảnh:
         
         1. VỚI CÂU HỎI THỐNG KÊ:
            - Đưa ra con số tổng quan trước, sau đó phân tích chi tiết
            - VD: "Hiện có 12 lệnh sản xuất đang chạy. Trong đó: 5 lệnh khẩn cấp, 7 lệnh thường."
         
         2. VỚI CÂU HỎI DANH SÁCH:
            - Tổng hợp thành bảng/bullet points dễ đọc
            - VD: "Danh sách 10 vật tư gần đây: | Mã | Tên | Tồn kho |"
         
         3. VỚI CÂU HỎI ĐỊNH MỨC (BOM):
            - Dùng công cụ get_bom_details để tra cứu nguyên liệu cấu thành sản phẩm
            - VD: "Thành phẩm 'Kiếm' cần: Thép (5 kg), Gỗ (1 kg), Da (0.5 kg)"
         
         4. VỚI CÂU HỎI QUY TRÌNH (ROUTING):
            - Dùng công cụ get_routing_details để tra cứu các bước gia công
            - VD: "Quy trình sản xuất Kiếm: Bước 1 - Cắt thép, Bước 2 - Rèn, Bước 3 - Mài"
         
         5. VỚI CÂU HỎI SO SÁNH / XU HƯỚNG:
            - Đưa ra nhận xét, so sánh tăng/giảm
            - VD: "Sản lượng tháng này tăng 15% so với tháng trước..."
         
         6. VỚI CÂU HỎI VẤN ĐỀ / CẢNH BÁO:
            - Ưu tiên thông báo vấn đề trước, sau đó đề xuất giải pháp
            - VD: "Có 3 vật tư sắp hết hàng: Xi măng (còn 5 tấn), Thép (còn 2 tấn)... Cần nhập thêm."
         
         7. KHI KHÔNG ĐỦ DỮ LIỆU:
            - Hỏi lại người dùng một cách thông minh để làm rõ
            - VD: "Bạn muốn xem theo kho nào? Hay xem tất cả?"
        
        === GIỚI HẠN ===
        - Bạn CHỈ trả lời các câu hỏi liên quan đến hệ thống ERP, sản xuất, kho bãi, nhân sự, tài chính, chất lượng.
        - NẾU câu hỏi KHÔNG liên quan (thời sự, thể thao, giải trí, AI khác, coding...), hãy lịch sự từ chối.
        - VD: "Tôi chỉ hỗ trợ các vấn đề về quản lý sản xuất. Bạn có câu hỏi nào về hệ thống không?"
        
        === QUY TẮC QUAN TRỌNG ===
        - KHI DỮ LIỆU ĐÃ CÓ TRONG NGỮ CẢNH: dùng ngay, KHÔNG gọi công cụ
        - KHI CẦN DỮ LIỆU MỚI: gọi công cụ phù hợp
        - KHÔNG BAO GIỜ trả về JSON/thông tin kỹ thuật thô
        - KHÔNG BAO GIỜ nói "theo dữ liệu được cung cấp" - hãy trả lời tự nhiên
        - Luôn kết thúc bằng câu hỏi gợi mở nếu có thể: "Bạn cần xem thêm gì không?"
        """;

    private static readonly Dictionary<string, string> FunctionModuleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["get_work_orders"] = SystemModules.Production,
        ["get_work_order_detail"] = SystemModules.Production,
        ["get_products"] = SystemModules.Production,
        ["get_production_stats"] = SystemModules.Production,
        ["check_material_availability"] = SystemModules.Production,
        ["get_inventory"] = SystemModules.Warehouse,
        ["get_stock_alerts"] = SystemModules.Warehouse,
        ["get_warehouses"] = SystemModules.Warehouse,
        ["get_materials"] = SystemModules.Warehouse,
        ["get_stock_transactions"] = SystemModules.Warehouse,
        ["get_employees"] = SystemModules.HumanResources,
        ["get_attendance"] = SystemModules.HumanResources,
        ["get_payroll"] = SystemModules.HumanResources,
        ["get_top_performers"] = SystemModules.HumanResources,
        ["get_shifts"] = SystemModules.HumanResources,
        ["get_transactions"] = SystemModules.Finance,
        ["get_invoices"] = SystemModules.Finance,
        ["get_cashflow"] = SystemModules.Finance,
        ["get_production_costs"] = SystemModules.Finance,
        ["get_qc_records"] = SystemModules.QualityControl,
        ["get_qc_statistics"] = SystemModules.QualityControl,
        ["get_pending_inspections"] = SystemModules.QualityControl,
        ["get_defect_trend"] = SystemModules.QualityControl,
        ["get_bom_details"] = SystemModules.Production,
        ["get_routing_details"] = SystemModules.Production,
        ["get_production_progress"] = SystemModules.Production,
        ["get_production_chart"] = SystemModules.Production,
        ["get_attendance_summary"] = SystemModules.HumanResources,
        ["get_schedules"] = SystemModules.HumanResources,
        ["get_partners"] = SystemModules.MasterData,
        ["get_activities"] = SystemModules.SystemAdmin,
    };

    private DateTime _lastMessageTime = DateTime.MinValue;
    private readonly object _rateLimitLock = new();

    public AIChatService(
        IProductionService production,
        IWarehouseService warehouse,
        IHRService hr,
        IFinanceService finance,
        IQualityControlService qc,
        IAuthService auth,
        IPermissionService permissions,
        IAuditLogService auditLog,
        IRagService ragService,
        IMasterDataService masterData,
        IPartnerService partner,
        IActivityService activity)
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };
        _apiKey = LoadApiKey();
        if (!string.IsNullOrWhiteSpace(_apiKey))
            _httpClient.DefaultRequestHeaders.Add("X-Goog-Api-Key", _apiKey);
        _production = production;
        _warehouse = warehouse;
        _hr = hr;
        _finance = finance;
        _qc = qc;
        _auth = auth;
        _permissions = permissions;
        _auditLog = auditLog;
        _ragService = ragService;
        _masterData = masterData;
        _partner = partner;
        _activity = activity;
    }

    private static string LoadApiKey()
    {
        var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (File.Exists(jsonPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
                var key = doc.RootElement.TryGetProperty("GeminiApiKey", out var prop) ? prop.GetString() : null;
                if (!string.IsNullOrWhiteSpace(key)) return key;
            }
            catch { }
        }
        return Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
    }

    private string? CheckPreconditions(string message)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return "Chưa cấu hình API key.";
        if (_auth.CurrentUser == null)
            return "Bạn cần đăng nhập để sử dụng AI Chat.";
        if (message.Length > MaxMessageLength)
            return $"Tin nhắn quá dài. Tối đa {MaxMessageLength} ký tự.";
        lock (_rateLimitLock)
        {
            var elapsed = DateTime.Now - _lastMessageTime;
            if (elapsed.TotalSeconds < RateLimitSeconds)
            {
                var waitSeconds = (int)Math.Ceiling(RateLimitSeconds - elapsed.TotalSeconds);
                return $"Vui lòng đợi {waitSeconds} giây trước khi gửi tin nhắn tiếp theo.";
            }
            _lastMessageTime = DateTime.Now;
        }
        return null;
    }

    private async Task AuditLogAsync(string message, string? response)
    {
        try
        {
            var user = _auth.CurrentUser;
            var preview = response != null && response.Length > 200 ? response[..200] + "..." : response;
            await _auditLog.LogActionAsync("AI_CHAT",
                $"User {user?.Username} hỏi: {message} | AI: {preview}", "AIChat");
        }
        catch { }
    }

    // ============ Non-streaming (kept for compatibility) ============

    public async Task<string> SendMessageAsync(string message, List<ChatMessage> history)
    {
        var err = CheckPreconditions(message);
        if (err != null) return err;

        try
        {
            var contents = BuildContents(history, message);
            var json = await CallGeminiAsync(ApiUrl, contents);
            using var doc = JsonDocument.Parse(json);
            var result = await ProcessResponseAsync(doc.RootElement, contents, _auth.CurrentUser!.UserId);

            _ = AuditLogAsync(message, result);

            return result ?? "(Không có phản hồi)";
        }
        catch (TaskCanceledException) { return "(Hệ thống AI không phản hồi kịp, vui lòng thử lại)"; }
        catch (HttpRequestException ex) { return $"(Lỗi kết nối: {ex.Message})"; }
        catch (Exception ex) { return $"({ex.Message})"; }
    }

    // ============ Streaming ============

    public async IAsyncEnumerable<string> StreamSendMessageAsync(string message, List<ChatMessage> history,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var err = CheckPreconditions(message);
        if (err != null)
        {
            yield return err;
            yield break;
        }

        var userId = _auth.CurrentUser!.UserId;
        var contents = BuildContents(history, message);
        var textBuilder = new StringBuilder();

        // Try local intent detection: query DB first, provide as context
        var localData = await TryPrefetchAsync(message);
        if (localData != null)
        {
            contents.Insert(contents.Count - 1, new JsonObject
            {
                ["role"] = "model",
                ["parts"] = new JsonArray { new JsonObject { ["text"] = $"Dữ liệu tham khảo:\n{localData}" } }
            });
        }

        // RAG search: find relevant document chunks
        try
        {
            var ragResults = await _ragService.SearchAsync(message, topK: 2, minScore: 0.45);
            if (ragResults.Count > 0)
            {
                var ragContext = new StringBuilder();
                ragContext.AppendLine("Tài liệu tham khảo (RAG):");
                foreach (var r in ragResults)
                {
                    ragContext.AppendLine($"- Từ '{r.FileName}': {r.Content[..Math.Min(r.Content.Length, 400)]}");
                }

                var ragText = ragContext.ToString();
                if (ragText.Length <= 2000)
                {
                    contents.Insert(contents.Count - 1, new JsonObject
                    {
                        ["role"] = "model",
                        ["parts"] = new JsonArray { new JsonObject { ["text"] = ragText } }
                    });
                }
            }
        }
        catch { }

        // Luôn cho AI dùng tools, prefetch data chỉ là context tham khảo
        await foreach (var token in StreamCallGeminiAsync(contents, userId, skipTools: false, cancellationToken))
        {
            textBuilder.Append(token);
            yield return token;
        }

        _ = AuditLogAsync(message, textBuilder.ToString());
    }

    private static string MakeCacheKey(string msg)
    {
        // Dùng 30 ký tự đầu làm key cache
        return msg.Length <= 30 ? msg : msg[..30];
    }

    private async Task<string?> TryPrefetchAsync(string message)
    {
        var msg = message.ToLowerInvariant();

        // 1. Kiểm tra cache
        var cacheKey = MakeCacheKey(msg);
        lock (_prefetchCache)
        {
            if (_prefetchCache.TryGetValue(cacheKey, out var cached) && DateTime.Now - cached.CachedAt < CacheDuration)
                return cached.Data;
        }

        try
        {
            // 2. Entity extraction — phát hiện mã số
            var codeMatch = EntityCodeRegex.Match(message);
            var hasCode = codeMatch.Success;
            var code = hasCode ? codeMatch.Value : null;

            // Nếu có mã + lệnh sản xuất → gọi detail
            if (hasCode && (msg.Contains("lệnh") || msg.Contains("order")))
            {
                using var argsDoc = JsonDocument.Parse($"{{\"code\":\"{code}\"}}");
                var detail = await GetWorkOrderDetailAsync(argsDoc.RootElement);
                if (!detail.TryGetPropertyValue("error", out var _))
                {
                    var result = detail.ToJsonString();
                    CacheResult(cacheKey, result);
                    return result;
                }
            }

            // Nếu có mã nhân viên + lương → gọi payroll riêng
            if (hasCode && code.StartsWith("NV", StringComparison.OrdinalIgnoreCase) && msg.Contains("lương"))
            {
                using var argsDoc = JsonDocument.Parse($"{{\"month\":{DateTime.Now.Month},\"year\":{DateTime.Now.Year},\"employeeCode\":\"{code}\"}}");
                var payroll = await GetPayrollAsync(argsDoc.RootElement);
                if (!payroll.TryGetPropertyValue("error", out var _))
                {
                    var result = payroll.ToJsonString();
                    CacheResult(cacheKey, result);
                    return result;
                }
            }

            // 3. Intent detection keywords
            string? resultData = null;

            // ======= SẢN XUẤT =======
            if (msg.Contains("lệnh sản xuất") || msg.Contains("work order"))
            {
                var data = await GetWorkOrdersAsync(null);
                resultData = SummarizeJsonArray(data, "work_orders", ["code", "status", "product"]);
            }
            else if (msg.Contains("chi tiết lệnh") || msg.Contains("xem lệnh"))
                resultData = (await GetWorkOrderDetailAsync(null)).ToJsonString();
            else if (msg.Contains("thống kê sản xuất") || msg.Contains("sản xuất thống kê") || msg.Contains("tổng quan sản xuất"))
                resultData = (await GetProductionStatsAsync()).ToJsonString();
            else if (msg.Contains("chi phí sản xuất") || msg.Contains("production cost"))
                resultData = (await GetProductionCostsAsync(null)).ToJsonString();
            else if (msg.Contains("sản phẩm") || msg.Contains("product") && !msg.Contains("vật tư"))
                resultData = SummarizeJsonArray(await GetProductsAsync(), "products", ["code", "name", "status"]);
            else if (msg.Contains("tiến độ") || msg.Contains("progress") || msg.Contains("đang sản xuất"))
                resultData = SummarizeJsonArray(await GetProductionProgressAsync(null), "production_progress", ["stage", "product", "qty"]);
            else if (msg.Contains("biểu đồ") || msg.Contains("chart") || msg.Contains("đồ thị"))
                resultData = (await GetProductionChartAsync(null)).ToJsonString();

            // ======= ĐỊNH MỨC (BOM) =======
            else if (msg.Contains("định mức") || msg.Contains("bom") || msg.Contains("nguyên liệu làm") || msg.Contains("nguyên liệu để") || msg.Contains("cấu thành") || msg.Contains("thành phần") && msg.Contains("vật tư"))
            {
                if (code != null)
                {
                    using var bomArgs = JsonDocument.Parse($"{{\"parentCode\":\"{code}\"}}");
                    resultData = (await GetBomDetailsAsync(bomArgs.RootElement)).ToJsonString();
                }
                else
                {
                    // Thử tìm tên vật tư trong câu hỏi
                    var products = await _production.GetProductsAsync();
                    var productName = products.FirstOrDefault(p =>
                        msg.Contains(p.MaterialName, StringComparison.OrdinalIgnoreCase));
                    if (productName != null)
                    {
                        using var bomArgs = JsonDocument.Parse($"{{\"parentCode\":\"{productName.MaterialName}\"}}");
                        resultData = (await GetBomDetailsAsync(bomArgs.RootElement)).ToJsonString();
                    }
                    // Nếu vẫn không tìm được → để AI tự gọi hàm get_bom_details
                }
            }

            // ======= QUY TRÌNH SẢN XUẤT (Routing) =======
            else if (msg.Contains("quy trình") || msg.Contains("routing") || msg.Contains("bước sản xuất") || msg.Contains("gia công"))
            {
                if (code != null)
                {
                    using var routingArgs = JsonDocument.Parse($"{{\"parentCode\":\"{code}\"}}");
                    resultData = (await GetRoutingDetailsAsync(routingArgs.RootElement)).ToJsonString();
                }
                else
                {
                    var products = await _production.GetProductsAsync();
                    var productName = products.FirstOrDefault(p =>
                        msg.Contains(p.MaterialName, StringComparison.OrdinalIgnoreCase));
                    if (productName != null)
                    {
                        using var routingArgs = JsonDocument.Parse($"{{\"parentCode\":\"{productName.MaterialName}\"}}");
                        resultData = (await GetRoutingDetailsAsync(routingArgs.RootElement)).ToJsonString();
                    }
                }
            }

            // ======= KHO BÃI =======
            else if (msg.Contains("tồn kho") || msg.Contains("inventory"))
            {
                var data = await GetInventoryAsync(null);
                resultData = SummarizeJsonArray(data, "inventory", ["material", "stock", "warehouse"]);
            }
            else if (msg.Contains("vật tư") || msg.Contains("material") || msg.Contains("nguyên liệu"))
                resultData = SummarizeJsonArray(await GetMaterialsAsync(), "materials", ["code", "name", "unit", "category"]);
            else if (msg.Contains("đối tác") || msg.Contains("partner") || msg.Contains("khách hàng") || msg.Contains("nhà cung cấp") || msg.Contains("supplier") || msg.Contains("customer"))
                resultData = SummarizeJsonArray(await GetPartnersAsync(), "partners", ["code", "name", "type", "status"]);
            else if (msg.Contains("cảnh báo") || msg.Contains("tồn thấp") || msg.Contains("sắp hết"))
                resultData = SummarizeJsonArray(await GetStockAlertsAsync(), "stock_alerts", null);
            else if ((msg.Contains("kho") || msg.Contains("warehouse")) && !msg.Contains("tồn kho") && !msg.Contains("tồn thấp"))
                resultData = SummarizeJsonArray(await GetWarehousesAsync(), "warehouses", ["code", "name", "type"]);
            else if ((msg.Contains("nhập xuất") || msg.Contains("xuất nhập") || msg.Contains("giao dịch kho") || msg.Contains("lịch sử kho") || msg.Contains("stock transaction")) && !msg.Contains("tài chính"))
                resultData = SummarizeJsonArray(await GetStockTransactionsAsync(null), "transactions", ["material", "type", "qty", "date"]);

            // ======= NHÂN SỰ =======
            else if (msg.Contains("nhân viên") || msg.Contains("employee"))
            {
                var perms = await _permissions.GetUserPermissionsAsync(_auth.CurrentUser!.UserId);
                resultData = SummarizeJsonArray(await GetEmployeesAsync(perms), "employees", ["name", "position"]);
            }
            else if (msg.Contains("chấm công") || msg.Contains("attendance"))
                resultData = (await GetAttendanceAsync(null)).ToJsonString();
            else if (msg.Contains("lương") || msg.Contains("payroll") || msg.Contains("bảng lương"))
                resultData = SummarizeJsonArray(await GetPayrollAsync(null), "payroll", ["employee", "salary"]);
            else if (msg.Contains("ca làm") || msg.Contains("shift") || msg.Contains("ca làm việc"))
                resultData = SummarizeJsonArray(await GetShiftsAsync(), "shifts", ["code", "name", "start", "end"]);
            else if (msg.Contains("tổng hợp chấm công") || msg.Contains("chấm công tháng") || msg.Contains("attendance summary"))
                resultData = SummarizeJsonArray(await GetAttendanceSummaryAsync(null), "attendance_summary", ["employee", "workdays", "late"]);
            else if (msg.Contains("lịch làm") || msg.Contains("schedule") || msg.Contains("lịch việc"))
                resultData = SummarizeJsonArray(await GetSchedulesAsync(null), "schedules", ["employee", "shift", "date"]);
            else if (msg.Contains("năng suất") || msg.Contains("top performer") || msg.Contains("nhân viên xuất sắc"))
                resultData = (await GetTopPerformersAsync(null)).ToJsonString();

            // ======= TÀI CHÍNH =======
            else if ((msg.Contains("hóa đơn") || msg.Contains("invoice")) && !msg.Contains("tiền"))
                resultData = SummarizeJsonArray(await GetInvoicesAsync(null), "invoices", ["code", "customer", "total"]);
            else if (msg.Contains("giao dịch") || msg.Contains("transaction") || msg.Contains("thu chi"))
                resultData = SummarizeJsonArray(await GetTransactionsAsync(), "transactions", ["type", "amount", "date"]);
            else if (msg.Contains("dòng tiền") || msg.Contains("cashflow") || msg.Contains("vào ra"))
                resultData = (await GetCashflowAsync(null)).ToJsonString();

            // ======= CHẤT LƯỢNG =======
            else if (msg.Contains("chất lượng") || msg.Contains("quality") || msg.Contains("qc"))
                resultData = SummarizeJsonArray(await GetQcRecordsAsync(null), "qc_records", ["status", "result"]);
            else if (msg.Contains("thống kê chất lượng") || msg.Contains("qc stat") || msg.Contains("tỉ lệ đạt"))
                resultData = (await GetQcStatisticsAsync(null)).ToJsonString();
            else if (msg.Contains("chờ kiểm tra") || msg.Contains("pending inspection") || msg.Contains("cần kiểm tra"))
                resultData = SummarizeJsonArray(await GetPendingInspectionsAsync(), "pending_inspections", ["code", "status"]);
            else if (msg.Contains("xu hướng lỗi") || msg.Contains("defect trend") || msg.Contains("lỗi chất lượng"))
                resultData = (await GetDefectTrendAsync(null)).ToJsonString();

            // ======= KHÁC =======
            else if (msg.Contains("hoạt động") || msg.Contains("activity") || msg.Contains("lịch sử") || msg.Contains("nhật ký"))
                resultData = SummarizeJsonArray(await GetActivitiesAsync(null), "activities", ["type", "content", "time"]);

            // 4. Follow-up: nếu không khớp keyword, thử dùng chủ đề cũ
            if (resultData == null && _lastTopic != null)
            {
                // Chỉ dùng follow-up nếu câu hỏi ngắn (<= 5 từ) hoặc chứa từ gợi ý
                var wordCount = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                var isFollowUp = wordCount <= 5
                    || msg.Contains("nó") || msg.Contains("đó") || msg.Contains("ấy")
                    || msg.Contains("thêm") || msg.Contains("tiếp") || msg.Contains("nữa")
                    || msg.Contains("còn") || msg.Contains("khác");

                if (isFollowUp)
                    return _lastTopic;
            }

            if (resultData != null)
            {
                CacheResult(cacheKey, resultData);
            }
            return resultData;
        }
        catch { }
        return null;
    }

    private void CacheResult(string cacheKey, string data)
    {
        _lastTopic = data;
        lock (_prefetchCache)
        {
            _prefetchCache[cacheKey] = (DateTime.Now, data);
        }
    }

    private static string SummarizeJsonArray(JsonObject obj, string arrayKey, string[]? fields)
    {
        if (!obj.TryGetPropertyValue(arrayKey, out var arr) || arr == null)
            return obj.ToJsonString();

        var items = arr.AsArray();
        var count = items.Count;
        var top = items.Take(10).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"[DỮ LIỆU] {arrayKey}: tổng số {count} bản ghi.");

        // Phân tích categorical fields và numeric fields
        try
        {
            var categoricalCounts = new Dictionary<string, Dictionary<string, int>>();
            var numericFields = new Dictionary<string, List<double>>();

            foreach (var item in items)
            {
                if (item is not JsonObject jo) continue;

                foreach (var kv in jo)
                {
                    var key = kv.Key;
                    var val = kv.Value;

                    // Categorical: đếm nhóm
                    if (val is JsonValue jv && jv.TryGetValue(out string? s) && !string.IsNullOrWhiteSpace(s))
                    {
                        categoricalCounts.TryAdd(key, new Dictionary<string, int>());
                        var dict = categoricalCounts[key];
                        dict.TryGetValue(s, out var c);
                        dict[s] = c + 1;
                    }
                    // Numeric: thu thập
                    else if (val is JsonValue jv2 && (jv2.TryGetValue(out int i) || jv2.TryGetValue(out double d)))
                    {
                        numericFields.TryAdd(key, new List<double>());
                        numericFields[key].Add(jv2.TryGetValue(out int ii) ? ii : (jv2.TryGetValue(out double dd) ? dd : 0));
                    }
                }
            }

            // Chỉ hiển thị categorical có <= 20 nhóm (tránh field nhiều giá trị như name, code)
            foreach (var (field, groups) in categoricalCounts)
            {
                if (groups.Count <= 1 || groups.Count > 20) continue;
                if (IsSkippableField(field)) continue;
                var breakdown = string.Join(", ", groups.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}: {kv.Value}"));
                sb.AppendLine($"  Phân bổ {field}: {breakdown}");
            }

            // Thống kê numeric
            foreach (var (field, vals) in numericFields)
            {
                if (vals.Count < 2) continue;
                if (IsSkippableField(field)) continue;
                var min = vals.Min();
                var max = vals.Max();
                var avg = vals.Average();
                sb.AppendLine($"  {field}: từ {min:F1} đến {max:F1}, trung bình {avg:F1}");
            }

            static bool IsSkippableField(string f)
            {
                var lower = f.ToLowerInvariant();
                return lower is "id" or "code" or "name" or "fullname" or "description"
                    or "address" or "phone" or "email" or "image" or "note";
            }
        }
        catch { }

        // Danh sách top records
        if (top.Count > 0 && count > 1)
        {
            sb.AppendLine($"  {top.Count} bản ghi đầu:");
            var header = fields != null ? string.Join(" | ", fields) : "value";
            sb.AppendLine($"    {header}");
            sb.AppendLine($"    {new string('-', header.Length)}");
            foreach (var item in top)
            {
                if (fields != null)
                {
                    var parts = new List<string>();
                    foreach (var f in fields)
                    {
                        if (item is JsonObject jo && jo.TryGetPropertyValue(f, out var val))
                            parts.Add(TruncateValue(val?.ToString() ?? "", 25));
                    }
                    sb.AppendLine($"    {string.Join(" | ", parts)}");
                }
                else
                {
                    sb.AppendLine($"    {item}");
                }
            }
        }

        return sb.ToString();
    }

    private static string TruncateValue(string s, int max)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
        return s[..(max - 3)] + "...";
    }

    private async IAsyncEnumerable<string> StreamCallGeminiAsync(JsonArray contents, int userId,
        bool skipTools = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Phase 1: Send request and read SSE stream
        string? funcName = null;
        string? funcArgsRaw = null;

        {
            var contentsJson = contents.ToJsonString();
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, StreamUrl)
            {
                Content = new StringContent(
                    skipTools
                        ? $$"""{"system_instruction":{"parts":[{"text":{{JsonSerializer.Serialize(SystemPrompt)}}}]},"contents":{{contentsJson}}}"""
                        : $$"""{"system_instruction":{"parts":[{"text":{{JsonSerializer.Serialize(SystemPrompt)}}}]},"contents":{{contentsJson}},"tools":{{GetToolsJson()}}}""",
                    Encoding.UTF8, "application/json")
            };

            HttpResponseMessage response = null!;
            for (var attempt = 0; attempt <= MaxRetries; attempt++)
            {
                response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if ((int)response.StatusCode != 429 || attempt == MaxRetries)
                    break;
                var waitMs = (int)Math.Pow(2, attempt) * 1000;
                await Task.Delay(waitMs, cancellationToken);
            }
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;
                if (!line.StartsWith("data: ")) continue;
                if (line == "data: [DONE]") break;

                var data = line[6..];
                using var doc = JsonDocument.Parse(data);

                var (text, fn, fargs) = ExtractResponsePart(doc.RootElement);

                if (text != null)
                {
                    yield return text;
                }
                else if (fn != null)
                {
                    funcName = fn;
                    funcArgsRaw = fargs.HasValue ? fargs.Value.GetRawText() : null;
                    break; // Close this connection before handling function call
                }
            }
        } // SSE connection disposed here

        // Phase 2: Handle function call with a new connection
        if (funcName != null)
        {
            JsonElement? funcArgs = null;
            using var argsDoc = funcArgsRaw != null ? JsonDocument.Parse(funcArgsRaw) : null;
            if (argsDoc != null)
                funcArgs = argsDoc.RootElement;

            var funcResult = await ExecuteFunctionAsync(funcName, funcArgs, userId);

            contents.Add(new JsonObject
            {
                ["role"] = "model",
                ["parts"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["functionCall"] = new JsonObject
                        {
                            ["name"] = funcName,
                            ["args"] = funcArgs != null
                                ? JsonNode.Parse(funcArgs.Value.GetRawText())
                                : new JsonObject()
                        }
                    }
                }
            });

            contents.Add(new JsonObject
            {
                ["role"] = "function",
                ["parts"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["functionResponse"] = new JsonObject
                        {
                            ["name"] = funcName,
                            ["response"] = new JsonObject
                            {
                                ["name"] = funcName,
                                ["content"] = funcResult
                            }
                        }
                    }
                }
            });

            await foreach (var token in StreamCallGeminiAsync(contents, userId, false, cancellationToken))
                yield return token;
        }
    }

    private static string GetToolsJson()
    {
        return GetTools().ToJsonString();
    }

    // ============ Helpers ============

    private static JsonArray BuildContents(List<ChatMessage> history, string currentMessage)
    {
        var contents = new JsonArray();
        foreach (var msg in history)
        {
            contents.Add(new JsonObject
            {
                ["role"] = msg.IsUser ? "user" : "model",
                ["parts"] = new JsonArray { new JsonObject { ["text"] = msg.Content } }
            });
        }
        contents.Add(new JsonObject
        {
            ["role"] = "user",
            ["parts"] = new JsonArray { new JsonObject { ["text"] = currentMessage } }
        });
        return contents;
    }

    private async Task<string> CallGeminiAsync(string url, JsonArray contents)
    {
        var contentsJson = contents.ToJsonString();
        var json = $$"""{"system_instruction":{"parts":[{"text":{{JsonSerializer.Serialize(SystemPrompt)}}}]},"contents":{{contentsJson}},"tools":{{GetToolsJson()}}}""";
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, httpContent);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return $"{{\"error\":\"API error {(int)response.StatusCode}\",\"detail\":{JsonSerializer.Serialize(responseJson)}}}";

        return responseJson;
    }

    private async Task<string> ProcessResponseAsync(JsonElement root, JsonArray contents, int userId)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return "(Không có phản hồi từ AI)";

        var candidate = candidates[0];

        if (candidate.TryGetProperty("finishReason", out var reason) && reason.GetString() == "SAFETY")
            return "(Phản hồi bị chặn do vi phạm an toàn)";

        var content = candidate.GetProperty("content");
        var parts = content.GetProperty("parts");

        if (parts.GetArrayLength() == 0)
            return "(Không có nội dung phản hồi)";

        var part = parts[0];

        if (part.TryGetProperty("functionCall", out var functionCall))
        {
            var funcName = functionCall.GetProperty("name").GetString()!;
            var funcArgs = functionCall.TryGetProperty("args", out var args) ? args : default;

            var result = await ExecuteFunctionAsync(funcName, funcArgs, userId);

            contents.Add(new JsonObject
            {
                ["role"] = "model",
                ["parts"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["functionCall"] = new JsonObject
                        {
                            ["name"] = funcName,
                            ["args"] = functionCall.TryGetProperty("args", out var existingArgs)
                                ? JsonNode.Parse(existingArgs.GetRawText())
                                : new JsonObject()
                        }
                    }
                }
            });

            contents.Add(new JsonObject
            {
                ["role"] = "function",
                ["parts"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["functionResponse"] = new JsonObject
                        {
                            ["name"] = funcName,
                            ["response"] = new JsonObject
                            {
                                ["name"] = funcName,
                                ["content"] = result
                            }
                        }
                    }
                }
            });

            var finalJson = await CallGeminiAsync(ApiUrl, contents);
            using var finalDoc = JsonDocument.Parse(finalJson);
            return await ProcessResponseAsync(finalDoc.RootElement, contents, userId);
        }

        if (part.TryGetProperty("text", out var textProp))
            return textProp.GetString() ?? "(trống)";

        return "(Định dạng phản hồi không xác định)";
    }

    private static (string? Text, string? FunctionName, JsonElement? Args) ExtractResponsePart(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return (null, null, null);

        var candidate = candidates[0];

        if (candidate.TryGetProperty("finishReason", out var reason) && reason.GetString() == "SAFETY")
            return (null, null, null);

        if (!candidate.TryGetProperty("content", out var content))
            return (null, null, null);

        if (!content.TryGetProperty("parts", out var parts) || parts.GetArrayLength() == 0)
            return (null, null, null);

        var part = parts[0];

        if (part.TryGetProperty("text", out var text))
            return (text.GetString(), null, null);

        if (part.TryGetProperty("functionCall", out var funcCall))
        {
            var funcName = funcCall.GetProperty("name").GetString();
            funcCall.TryGetProperty("args", out var args);
            return (null, funcName, args);
        }

        return (null, null, null);
    }

    private static JsonArray GetTools()
    {
        var funcs = new JsonArray();

        void AddFunc(string name, string desc)
        {
            funcs.Add(new JsonObject { ["name"] = name, ["description"] = desc });
        }
        void AddFuncWithParams(string name, string desc, JsonObject paramsObj)
        {
            funcs.Add(new JsonObject
            {
                ["name"] = name,
                ["description"] = desc,
                ["parameters"] = paramsObj
            });
        }

        JsonObject Param(string type, string desc) => new()
        {
            ["type"] = type,
            ["description"] = desc
        };
        JsonObject MakeParams(JsonObject props, params string[] required)
        {
            var r = new JsonArray();
            foreach (var p in required) r.Add(p);
            return new JsonObject
            {
                ["type"] = "OBJECT",
                ["properties"] = props,
                ["required"] = r
            };
        }

        AddFuncWithParams("get_work_orders", "Lấy danh sách lệnh sản xuất, lọc theo trạng thái (Planned, Running, Paused, Completed, Cancelled)",
            MakeParams(new JsonObject { ["status"] = Param("STRING", "Trạng thái lọc (để trống lấy tất cả)") }));
        AddFuncWithParams("get_work_order_detail", "Lấy chi tiết một lệnh sản xuất theo mã",
            MakeParams(new JsonObject { ["code"] = Param("STRING", "Mã lệnh sản xuất (vd: MSX-001)") }, "code"));
        AddFunc("get_products", "Lấy danh sách sản phẩm (thành phẩm)");
        AddFuncWithParams("get_bom_details", "Tra cứu định mức nguyên vật liệu (BOM) của một thành phẩm. Trả về danh sách nguyên liệu cần để sản xuất sản phẩm đó. Có thể nhập mã hoặc tên sản phẩm.",
            MakeParams(new JsonObject { ["parentCode"] = Param("STRING", "Mã hoặc tên thành phẩm (vd: SP001 hoặc Kiếm)") }, "parentCode"));
        AddFuncWithParams("get_routing_details", "Tra cứu quy trình sản xuất (Routing) của một thành phẩm. Trả về danh sách các bước gia công theo thứ tự. Có thể nhập mã hoặc tên sản phẩm.",
            MakeParams(new JsonObject { ["parentCode"] = Param("STRING", "Mã hoặc tên thành phẩm (vd: SP001 hoặc Kiếm)") }, "parentCode"));
        AddFunc("get_production_stats", "Lấy thống kê tổng quan sản xuất");
        AddFuncWithParams("get_production_progress", "Lấy tiến độ sản xuất gần đây",
            MakeParams(new JsonObject { ["count"] = Param("NUMBER", "Số lượng (mặc định 20)") }));
        AddFuncWithParams("get_production_chart", "Lấy dữ liệu biểu đồ sản xuất",
            MakeParams(new JsonObject { ["days"] = Param("NUMBER", "Số ngày (mặc định 30)") }));
        AddFuncWithParams("check_material_availability", "Kiểm tra khả năng cung ứng nguyên vật liệu",
            MakeParams(new JsonObject { ["workOrderId"] = Param("NUMBER", "ID lệnh sản xuất") }, "workOrderId"));
        AddFuncWithParams("get_inventory", "Lấy danh sách tồn kho",
            MakeParams(new JsonObject { ["warehouseName"] = Param("STRING", "Tên kho (để trống lấy tất cả)") }));
        AddFunc("get_stock_alerts", "Lấy danh sách cảnh báo tồn kho thấp");
        AddFunc("get_warehouses", "Lấy danh sách các kho");
        AddFunc("get_materials", "Lấy danh sách nguyên vật liệu");
        AddFunc("get_partners", "Lấy danh sách đối tác (khách hàng, nhà cung cấp)");
        AddFuncWithParams("get_stock_transactions", "Lấy lịch sử giao dịch nhập/xuất kho",
            MakeParams(new JsonObject
            {
                ["warehouseName"] = Param("STRING", "Tên kho"),
                ["limit"] = Param("NUMBER", "Số lượng (mặc định 20)")
            }));
        AddFunc("get_employees", "Lấy danh sách nhân viên");
        AddFuncWithParams("get_attendance", "Lấy chấm công theo ngày",
            MakeParams(new JsonObject { ["date"] = Param("STRING", "Ngày (yyyy-MM-dd)") }));
        AddFuncWithParams("get_payroll", "Lấy bảng lương theo tháng, có thể lọc theo nhân viên",
            MakeParams(new JsonObject
            {
                ["month"] = Param("NUMBER", "Tháng (1-12)"),
                ["year"] = Param("NUMBER", "Năm"),
                ["employeeCode"] = Param("STRING", "Mã nhân viên (để trống lấy tất cả)")
            }, "month", "year"));
        AddFuncWithParams("get_top_performers", "Lấy nhân viên năng suất cao nhất tháng",
            MakeParams(new JsonObject
            {
                ["month"] = Param("NUMBER", "Tháng"),
                ["year"] = Param("NUMBER", "Năm")
            }, "month", "year"));
        AddFunc("get_shifts", "Lấy danh sách ca làm việc");
        AddFuncWithParams("get_attendance_summary", "Lấy tổng hợp chấm công theo tháng",
            MakeParams(new JsonObject
            {
                ["month"] = Param("NUMBER", "Tháng (1-12)"),
                ["year"] = Param("NUMBER", "Năm")
            }, "month", "year"));
        AddFuncWithParams("get_schedules", "Lấy lịch làm việc theo khoảng ngày",
            MakeParams(new JsonObject
            {
                ["startDate"] = Param("STRING", "Ngày bắt đầu (yyyy-MM-dd)"),
                ["endDate"] = Param("STRING", "Ngày kết thúc (yyyy-MM-dd)")
            }));
        AddFunc("get_transactions", "Lấy danh sách giao dịch tài chính");
        AddFuncWithParams("get_invoices", "Lấy danh sách hóa đơn",
            MakeParams(new JsonObject { ["type"] = Param("STRING", "AR (bán) hoặc AP (mua)") }));
        AddFuncWithParams("get_cashflow", "Lấy dòng tiền vào/ra theo tháng",
            MakeParams(new JsonObject
            {
                ["month"] = Param("NUMBER", "Tháng"),
                ["year"] = Param("NUMBER", "Năm")
            }, "month", "year"));
        AddFuncWithParams("get_production_costs", "Tính chi phí sản xuất",
            MakeParams(new JsonObject
            {
                ["startDate"] = Param("STRING", "Ngày bắt đầu"),
                ["endDate"] = Param("STRING", "Ngày kết thúc")
            }));
        AddFuncWithParams("get_qc_records", "Lấy kiểm tra chất lượng gần đây",
            MakeParams(new JsonObject { ["count"] = Param("NUMBER", "Số lượng (mặc định 20)") }));
        AddFuncWithParams("get_qc_statistics", "Lấy thống kê chất lượng",
            MakeParams(new JsonObject
            {
                ["startDate"] = Param("STRING", "Ngày bắt đầu"),
                ["endDate"] = Param("STRING", "Ngày kết thúc")
            }));
        AddFunc("get_pending_inspections", "Lấy lệnh chờ kiểm tra chất lượng");
        AddFuncWithParams("get_defect_trend", "Lấy xu hướng lỗi chất lượng",
            MakeParams(new JsonObject
            {
                ["period"] = Param("STRING", "daily, weekly, monthly"),
                ["count"] = Param("NUMBER", "Số điểm dữ liệu")
            }));
        AddFuncWithParams("get_activities", "Lấy lịch sử hoạt động gần đây",
            MakeParams(new JsonObject { ["count"] = Param("NUMBER", "Số lượng (mặc định 20)") }));

        return new JsonArray { new JsonObject { ["functionDeclarations"] = funcs } };
    }

    // ============ Authorization & function execution ============

    private async Task<JsonObject> ExecuteFunctionAsync(string name, JsonElement? args, int userId)
    {
        try
        {
            if (!FunctionModuleMap.TryGetValue(name, out var moduleKey))
                return new JsonObject { ["error"] = $"Không tìm thấy công cụ: {name}" };

            var userPerms = await _permissions.GetUserPermissionsAsync(userId);
            if (!userPerms.TryGetValue(moduleKey, out var perm) || !perm.CanView)
            {
                var moduleName = SystemModules.All.FirstOrDefault(m => m.Key == moduleKey).DisplayName;
                return new JsonObject { ["error"] = $"Bạn không có quyền xem module '{moduleName}'." };
            }

            return name switch
            {
                "get_work_orders" => await GetWorkOrdersAsync(args),
                "get_work_order_detail" => await GetWorkOrderDetailAsync(args),
                "get_products" => await GetProductsAsync(),
                "get_bom_details" => await GetBomDetailsAsync(args),
                "get_routing_details" => await GetRoutingDetailsAsync(args),
                "get_production_stats" => await GetProductionStatsAsync(),
                "get_production_progress" => await GetProductionProgressAsync(args),
                "get_production_chart" => await GetProductionChartAsync(args),
                "check_material_availability" => await CheckMaterialAvailabilityAsync(args),
                "get_inventory" => await GetInventoryAsync(args),
                "get_stock_alerts" => await GetStockAlertsAsync(),
                "get_warehouses" => await GetWarehousesAsync(),
                "get_materials" => await GetMaterialsAsync(),
                "get_partners" => await GetPartnersAsync(),
                "get_stock_transactions" => await GetStockTransactionsAsync(args),
                "get_employees" => await GetEmployeesAsync(userPerms),
                "get_attendance" => await GetAttendanceAsync(args),
                "get_payroll" => await GetPayrollAsync(args),
                "get_top_performers" => await GetTopPerformersAsync(args),
                "get_shifts" => await GetShiftsAsync(),
                "get_attendance_summary" => await GetAttendanceSummaryAsync(args),
                "get_schedules" => await GetSchedulesAsync(args),
                "get_transactions" => await GetTransactionsAsync(),
                "get_invoices" => await GetInvoicesAsync(args),
                "get_cashflow" => await GetCashflowAsync(args),
                "get_production_costs" => await GetProductionCostsAsync(args),
                "get_qc_records" => await GetQcRecordsAsync(args),
                "get_qc_statistics" => await GetQcStatisticsAsync(args),
                "get_pending_inspections" => await GetPendingInspectionsAsync(),
                "get_defect_trend" => await GetDefectTrendAsync(args),
                "get_activities" => await GetActivitiesAsync(args),
                _ => new JsonObject { ["error"] = $"Không tìm thấy công cụ: {name}" }
            };
        }
        catch (Exception ex)
        {
            return new JsonObject { ["error"] = $"Lỗi: {ex.Message}" };
        }
    }

    private static string? GetArgString(JsonElement? args, string key)
    {
        if (args == null) return null;
        if (args.Value.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static int? GetArgInt(JsonElement? args, string key)
    {
        if (args == null) return null;
        if (args.Value.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.Number)
            return prop.GetInt32();
        return null;
    }

    private static JsonObject ToJsonObj(string key, object data)
    {
        return new JsonObject
        {
            [key] = JsonNode.Parse(JsonSerializer.Serialize(data, JsonOptions))!
        };
    }

    // ========== PRODUCTION ==========

    private async Task<JsonObject> GetWorkOrdersAsync(JsonElement? args)
    {
        var status = GetArgString(args, "status");
        var now = DateTime.Now;
        var start = now.AddYears(-1);
        var orders = await _production.GetFilteredWorkOrdersAsync(start, now, null);
        if (!string.IsNullOrWhiteSpace(status))
            orders = orders.Where(o => o.Status == status).ToList();
        return ToJsonObj("work_orders", orders);
    }

    private async Task<JsonObject> GetWorkOrderDetailAsync(JsonElement? args)
    {
        var code = GetArgString(args, "code");
        if (string.IsNullOrWhiteSpace(code))
            return new JsonObject { ["error"] = "Vui lòng cung cấp mã lệnh sản xuất" };
        var order = await _production.GetWorkOrderByCodeAsync(code);
        if (order == null)
            return new JsonObject { ["error"] = $"Không tìm thấy lệnh: {code}" };
        return ToJsonObj("work_order", order);
    }

    private async Task<JsonObject> GetProductsAsync() => ToJsonObj("products", await _production.GetProductsAsync());

    private async Task<JsonObject> GetBomDetailsAsync(JsonElement? args)
    {
        var code = GetArgString(args, "parentCode");
        if (string.IsNullOrWhiteSpace(code))
            return new JsonObject { ["error"] = "Vui lòng cung cấp mã hoặc tên thành phẩm" };

        var bom = await _masterData.GetBomByParentCodeAsync(code);
        if (bom.Count > 0)
            return ToJsonObj("bom_details", bom);

        // Thử tìm theo tên vật tư
        var materials = await _warehouse.GetAllMaterialsAsync();
        var match = materials.FirstOrDefault(m =>
            m.MaterialName.Contains(code, StringComparison.OrdinalIgnoreCase)
            || m.MaterialCode.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            bom = await _masterData.GetBomByParentCodeAsync(match.MaterialCode);
            if (bom.Count > 0)
                return ToJsonObj("bom_details", bom);
            return new JsonObject { ["error"] = $"Vật tư '{match.MaterialName}' ({match.MaterialCode}) không có định mức BOM" };
        }

        return new JsonObject { ["error"] = $"Không tìm thấy vật tư nào khớp với '{code}'" };
    }

    private async Task<JsonObject> GetRoutingDetailsAsync(JsonElement? args)
    {
        var code = GetArgString(args, "parentCode");
        if (string.IsNullOrWhiteSpace(code))
            return new JsonObject { ["error"] = "Vui lòng cung cấp mã hoặc tên thành phẩm" };

        var routing = await _masterData.GetRoutingByParentCodeAsync(code);
        if (routing.Count > 0)
            return ToJsonObj("routing_details", routing);

        var materials = await _warehouse.GetAllMaterialsAsync();
        var match = materials.FirstOrDefault(m =>
            m.MaterialName.Contains(code, StringComparison.OrdinalIgnoreCase)
            || m.MaterialCode.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            routing = await _masterData.GetRoutingByParentCodeAsync(match.MaterialCode);
            if (routing.Count > 0)
                return ToJsonObj("routing_details", routing);
            return new JsonObject { ["error"] = $"Vật tư '{match.MaterialName}' ({match.MaterialCode}) không có quy trình sản xuất" };
        }

        return new JsonObject { ["error"] = $"Không tìm thấy vật tư nào khớp với '{code}'" };
    }

    private async Task<JsonObject> GetProductionStatsAsync()
    {
        var stats = await _production.GetDashboardStatsAsync();
        return new JsonObject { ["stats"] = new JsonObject { ["material_alerts"] = stats.MaterialAlerts, ["today_productivity"] = stats.TodayProductivity, ["monthly_revenue"] = stats.MonthlyRevenue } };
    }

    private async Task<JsonObject> GetProductionProgressAsync(JsonElement? args)
    {
        var count = GetArgInt(args, "count") ?? 20;
        return ToJsonObj("production_progress", await _production.GetRecentProgressAsync(count));
    }

    private async Task<JsonObject> GetProductionChartAsync(JsonElement? args)
    {
        var days = GetArgInt(args, "days") ?? 30;
        var (prod, defects, labels, plans) = await _production.GetProductionChartDataAsync(days);
        return new JsonObject { ["production_chart"] = new JsonObject { ["labels"] = JsonNode.Parse(JsonSerializer.Serialize(labels))!, ["production"] = JsonNode.Parse(JsonSerializer.Serialize(prod))!, ["defects"] = JsonNode.Parse(JsonSerializer.Serialize(defects))!, ["plans"] = JsonNode.Parse(JsonSerializer.Serialize(plans))! } };
    }

    private async Task<JsonObject> CheckMaterialAvailabilityAsync(JsonElement? args)
    {
        var id = GetArgInt(args, "workOrderId");
        if (id == null) return new JsonObject { ["error"] = "Vui lòng cung cấp ID lệnh sản xuất" };
        return ToJsonObj("material_availability", await _production.CheckMaterialAvailabilityAsync(id.Value));
    }

    // ========== WAREHOUSE ==========

    private async Task<JsonObject> GetInventoryAsync(JsonElement? args)
    {
        var name = GetArgString(args, "warehouseName") ?? "Tất cả";
        return ToJsonObj("inventory", await _warehouse.GetInventoryAsync(name));
    }

    private async Task<JsonObject> GetStockAlertsAsync() => ToJsonObj("stock_alerts", await _warehouse.GetStockAlertsAsync());
    private async Task<JsonObject> GetWarehousesAsync() => ToJsonObj("warehouses", await _warehouse.GetWarehousesAsync());
    private async Task<JsonObject> GetMaterialsAsync() => ToJsonObj("materials", await _warehouse.GetAllMaterialsAsync());
    private async Task<JsonObject> GetPartnersAsync() => ToJsonObj("partners", await _partner.GetAllAsync());

    private async Task<JsonObject> GetStockTransactionsAsync(JsonElement? args)
    {
        var name = GetArgString(args, "warehouseName") ?? "Tất cả";
        var limit = GetArgInt(args, "limit") ?? 20;
        return ToJsonObj("transactions", await _warehouse.GetTransactionsAsync(name, limit));
    }

    // ========== HR ==========

    private async Task<JsonObject> GetEmployeesAsync(Dictionary<string, RolePermissionDto> userPerms)
    {
        var employees = await _hr.GetEmployeesAsync();
        var canSeeSensitive = userPerms.GetValueOrDefault(SystemModules.HumanResources) is { CanEdit: true };
        var masked = employees.Select(e => new
        {
            e.EmployeeId, e.EmployeeCode, e.FullName, e.Position, e.Department,
            Phone = canSeeSensitive ? e.Phone : MaskContent(e.Phone),
            Email = canSeeSensitive ? e.Email : MaskContent(e.Email),
            e.Status
        });
        return new JsonObject { ["employees"] = JsonNode.Parse(JsonSerializer.Serialize(masked, JsonOptions))! };
    }

    private async Task<JsonObject> GetAttendanceAsync(JsonElement? args)
    {
        var dateStr = GetArgString(args, "date");
        var date = !string.IsNullOrWhiteSpace(dateStr) && DateTime.TryParse(dateStr, out var p) ? p : DateTime.Today;
        return new JsonObject
        {
            ["date"] = date.ToString("yyyy-MM-dd"),
            ["attendance"] = JsonNode.Parse(JsonSerializer.Serialize(await _hr.GetDailyAttendanceAsync(date), JsonOptions))!
        };
    }

    private async Task<JsonObject> GetPayrollAsync(JsonElement? args)
    {
        var month = GetArgInt(args, "month") ?? DateTime.Now.Month;
        var year = GetArgInt(args, "year") ?? DateTime.Now.Year;
        var employeeCode = GetArgString(args, "employeeCode");
        var data = await _hr.CalculateMonthlyPayrollAsync(month, year);
        if (!string.IsNullOrWhiteSpace(employeeCode))
            data = data.Where(p => p.EmployeeCode.Equals(employeeCode, StringComparison.OrdinalIgnoreCase)).ToList();
        return new JsonObject { ["payroll"] = JsonNode.Parse(JsonSerializer.Serialize(data, JsonOptions))!, ["month"] = month, ["year"] = year };
    }

    private async Task<JsonObject> GetTopPerformersAsync(JsonElement? args)
    {
        var month = GetArgInt(args, "month") ?? DateTime.Now.Month;
        var year = GetArgInt(args, "year") ?? DateTime.Now.Year;
        var data = await _hr.GetTopPerformersAsync(month, year);
        return new JsonObject { ["top_performers"] = JsonNode.Parse(JsonSerializer.Serialize(data, JsonOptions))!, ["month"] = month, ["year"] = year };
    }

    private async Task<JsonObject> GetShiftsAsync() => ToJsonObj("shifts", await _hr.GetShiftsAsync());

    private async Task<JsonObject> GetAttendanceSummaryAsync(JsonElement? args)
    {
        var month = GetArgInt(args, "month") ?? DateTime.Now.Month;
        var year = GetArgInt(args, "year") ?? DateTime.Now.Year;
        return ToJsonObj("attendance_summary", await _hr.GetAttendanceSummariesAsync(month, year));
    }

    private async Task<JsonObject> GetSchedulesAsync(JsonElement? args)
    {
        var startStr = GetArgString(args, "startDate");
        var endStr = GetArgString(args, "endDate");
        var start = !string.IsNullOrWhiteSpace(startStr) && DateTime.TryParse(startStr, out var sp) ? sp : DateTime.Today;
        var end = !string.IsNullOrWhiteSpace(endStr) && DateTime.TryParse(endStr, out var ep) ? ep : start.AddDays(7);
        return ToJsonObj("schedules", await _hr.GetSchedulesAsync(start, end));
    }

    // ========== FINANCE ==========

    private async Task<JsonObject> GetTransactionsAsync() => ToJsonObj("transactions", await _finance.GetTransactionsAsync());

    private async Task<JsonObject> GetInvoicesAsync(JsonElement? args)
    {
        var type = GetArgString(args, "type") ?? "";
        return ToJsonObj("invoices", await _finance.GetInvoicesAsync(type));
    }

    private async Task<JsonObject> GetCashflowAsync(JsonElement? args)
    {
        var m = GetArgInt(args, "month") ?? DateTime.Now.Month;
        var y = GetArgInt(args, "year") ?? DateTime.Now.Year;
        var cf = await _finance.GetMonthlyCashFlowAsync(m, y);
        return new JsonObject { ["cashflow"] = new JsonObject { ["month"] = m, ["year"] = y, ["inflow"] = cf.Inflow, ["outflow"] = cf.Outflow, ["net"] = cf.Inflow - cf.Outflow } };
    }

    private async Task<JsonObject> GetProductionCostsAsync(JsonElement? args)
    {
        DateTime? s = null, e = null;
        var ss = GetArgString(args, "startDate");
        var es = GetArgString(args, "endDate");
        if (!string.IsNullOrWhiteSpace(ss) && DateTime.TryParse(ss, out var sp)) s = sp;
        if (!string.IsNullOrWhiteSpace(es) && DateTime.TryParse(es, out var ep)) e = ep;
        return ToJsonObj("production_costs", await _finance.CalculateProductionCostsAsync(s, e));
    }

    // ========== QUALITY ==========

    private async Task<JsonObject> GetQcRecordsAsync(JsonElement? args)
    {
        var count = GetArgInt(args, "count") ?? 20;
        return ToJsonObj("qc_records", await _qc.GetRecentRecordsAsync(count));
    }

    private async Task<JsonObject> GetQcStatisticsAsync(JsonElement? args)
    {
        var start = DateTime.Today.AddMonths(-1);
        var end = DateTime.Today;
        var ss = GetArgString(args, "startDate");
        var es = GetArgString(args, "endDate");
        if (!string.IsNullOrWhiteSpace(ss) && DateTime.TryParse(ss, out var sp)) start = sp;
        if (!string.IsNullOrWhiteSpace(es) && DateTime.TryParse(es, out var ep)) end = ep;
        var s = await _qc.GetStatisticsAsync(start, end);
        return new JsonObject
        {
            ["qc_statistics"] = new JsonObject
            {
                ["total"] = s.Total, ["passed"] = s.Passed, ["failed"] = s.Failed,
                ["pass_rate"] = s.Total > 0 ? Math.Round((double)s.Passed / s.Total * 100, 1) : 0,
                ["defect_stats"] = JsonNode.Parse(JsonSerializer.Serialize(s.DefectStats, JsonOptions))!,
                ["start_date"] = start.ToString("yyyy-MM-dd"), ["end_date"] = end.ToString("yyyy-MM-dd")
            }
        };
    }

    private async Task<JsonObject> GetPendingInspectionsAsync() => ToJsonObj("pending_inspections", await _qc.GetPendingInspectionOrdersAsync());

    private async Task<JsonObject> GetDefectTrendAsync(JsonElement? args)
    {
        var period = GetArgString(args, "period") ?? "daily";
        var count = GetArgInt(args, "count") ?? 30;
        var start = period switch { "monthly" => DateTime.Today.AddMonths(-count), "weekly" => DateTime.Today.AddDays(-7 * count), _ => DateTime.Today.AddDays(-count) };
        return ToJsonObj("defect_trend", await _qc.GetDefectTrendAsync(period, count, start));
    }

    // ========== OTHER ==========

    private async Task<JsonObject> GetActivitiesAsync(JsonElement? args)
    {
        var count = GetArgInt(args, "count") ?? 20;
        return ToJsonObj("activities", await _activity.GetRecentActivitiesAsync(count));
    }

    private static string MaskContent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "***";
        return value.Length <= 3 ? "***" : value[..2] + "***" + value[^1..];
    }
}
