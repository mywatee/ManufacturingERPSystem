using System;
using System.IO;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using OpenXmlPackaging = DocumentFormat.OpenXml.Packaging;
using OpenXmlWord = DocumentFormat.OpenXml.Wordprocessing;
using ManufacturingERP.Core;
using ManufacturingERP.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ManufacturingERP.Services;

public class DocumentSeeder
{
    private readonly IDocumentManagementService _documentService;
    private readonly IAuthService _auth;
    private readonly INotificationService _notification;

    public DocumentSeeder(
        IDocumentManagementService documentService,
        IAuthService auth,
        INotificationService notification)
    {
        _documentService = documentService;
        _auth = auth;
        _notification = notification;
    }

    public static void Register(IServiceCollection services)
    {
        services.AddSingleton<DocumentSeeder>();
    }

    public async Task SeedAsync(int userId)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ManufacturingERP_SeedDocs");
        Directory.CreateDirectory(tempDir);

        try
        {
            var samples = new[]
            {
                ("SOP San Xuat.docx", "Quy trình sản xuất", "SOP",
                    """
                    QUY TRÌNH SẢN XUẤT SẢN PHẨM

                    1. PHẠM VI ÁP DỤNG
                    Quy trình này áp dụng cho tất cả các công đoạn sản xuất sản phẩm tại nhà máy.

                    2. MỤC ĐÍCH
                    Đảm bảo chất lượng sản phẩm đồng đều và đúng tiêu chuẩn kỹ thuật.

                    3. NỘI DUNG

                    3.1. Chuẩn bị nguyên vật liệu
                    - Kiểm tra chất lượng đầu vào theo tiêu chuẩn ISO 9001:2015
                    - Cân đong định lượng theo công thức sản xuất
                    - Ghi nhận lot number và hạn sử dụng

                    3.2. Gia công
                    - Cài đặt thông số máy theo đúng quy định
                    - Nhiệt độ gia công: 150-180 độ C
                    - Áp suất: 4-6 bar
                    - Tốc độ: 1200-1500 vòng/phút

                    3.3. Kiểm tra chất lượng
                    - Lấy mẫu ngẫu nhiên mỗi 30 phút
                    - Kiểm tra kích thước: tolerance +/- 0.5mm
                    - Kiểm tra độ bền kéo: tối thiểu 200 N/mm2
                    - Ghi nhận kết quả vào biểu mẫu QC-001

                    3.4. Đóng gói
                    - Sử dụng bao bì đạt tiêu chuẩn
                    - Dán nhãn đầy đủ thông tin: mã sản phẩm, ngày sản xuất, hạn dùng
                    - Số lượng mỗi thùng: 50 sản phẩm

                    4. THÔNG SỐ KỸ THUẬT
                    - Độ dày: 2.0mm ± 0.1mm
                    - Độ cứng: HRC 45-50
                    - Độ nhám bề mặt: Ra 0.8μm
                    - Sai số kích thước: không vượt quá 0.5%

                    5. KIỂM SOÁT THAY ĐỔI
                    Mọi thay đổi quy trình phải được phê duyệt bởi trưởng bộ phận sản xuất và quản lý chất lượng.
                    """),
                ("Bao Cao Chat Luong.docx", "Báo cáo chất lượng tháng 5/2026", "Báo cáo",
                    """
                    BÁO CÁO CHẤT LƯỢNG THÁNG 5/2026

                    I. TỔNG QUAN
                    - Tổng số lô sản xuất: 156 lô
                    - Số lô đạt yêu cầu: 148 lô (94.8%)
                    - Số lô không đạt: 8 lô (5.2%)
                    - Số lô phải tái chế: 3 lô (1.9%)
                    - Số lô hủy bỏ: 1 lô (0.6%)

                    II. PHÂN TÍCH LỖI
                    1. Lỗi kích thước (45% tổng số lỗi)
                       - Nguyên nhân: Dao cắt mòn, cần thay sau mỗi 2000 sản phẩm
                       - Biện pháp: Tăng tần suất kiểm tra dao lên 2 lần/ca

                    2. Lỗi bề mặt (30% tổng số lỗi)
                       - Nguyên nhân: Nhiệt độ gia công không ổn định
                       - Biện pháp: Bảo trì hệ thống gia nhiệt định kỳ

                    3. Lỗi độ cứng (15% tổng số lỗi)
                       - Nguyên nhân: Thời gian xử lý nhiệt chưa đủ
                       - Biện pháp: Điều chỉnh thời gian ram thêm 30 phút

                    4. Lỗi khác (10% tổng số lỗi)

                    III. KẾT LUẬN
                    Chất lượng sản phẩm tháng 5 đạt 94.8%, cao hơn mục tiêu 93% đề ra.
                    Cần tập trung cải thiện vấn đề dao cắt mòn và ổn định nhiệt độ gia công.

                    IV. KIẾN NGHỊ
                    - Mua thêm bộ đo kích thước laser
                    - Đào tạo lại công nhân vận hành máy
                    - Tăng cường kiểm tra giám sát ca đêm
                    """),
                ("Quy Dinh An Toan Lao Dong.docx", "Quy định an toàn lao động", "Quy định",
                    """
                    QUY ĐỊNH AN TOÀN LAO ĐỘNG

                    CHƯƠNG I: QUY ĐỊNH CHUNG
                    Điều 1: Tất cả nhân viên phải tuân thủ nghiêm ngặt các quy định an toàn lao động.
                    Điều 2: Trang bị bảo hộ lao động bắt buộc khi vào khu vực sản xuất.

                    CHƯƠNG II: TRANG BỊ BẢO HỘ
                    Điều 3: Khu vực sản xuất: mũ bảo hộ, giày bảo hộ, kính bảo hộ, găng tay.
                    Điều 4: Khu vực hóa chất: thêm khẩu trang chống độc, áo bảo hộ chống hóa chất.
                    Điều 5: Khu vực hàn cắt: thêm mặt nạ hàn, tạp dề da, găng tay chịu nhiệt.

                    CHƯƠNG III: QUY ĐỊNH VẬN HÀNH MÁY
                    Điều 6: Chỉ người được đào tạo và có giấy phép mới được vận hành máy móc.
                    Điều 7: Kiểm tra máy trước khi khởi động: dầu nhớt, điện áp, hệ thống an toàn.
                    Điều 8: Báo cáo ngay khi phát hiện máy có dấu hiệu bất thường.
                    Điều 9: Tắt máy và khóa nguồn trước khi vệ sinh hoặc sửa chữa.

                    CHƯƠNG IV: XỬ LÝ SỰ CỐ
                    Điều 10: Khi xảy ra tai nạn:
                    - Sơ cứu tại chỗ nếu nhẹ
                    - Gọi cấp cứu 115 nếu nghiêm trọng
                    - Báo cáo quản lý ca ngay lập tức
                    - Lập biên bản tai nạn theo mẫu AT-01

                    Điều 11: Khi xảy ra hỏa hoạn:
                    - Báo động, sơ tán khỏi khu vực
                    - Sử dụng bình chữa cháy gần nhất
                    - Gọi cứu hỏa 114 nếu lớn hơn tầm kiểm soát

                    CHƯƠNG V: XỬ PHẠT
                    Điều 12: Không tuân thủ quy định an toàn: cảnh cáo lần 1, phạt 500,000đ lần 2.
                    Điều 13: Vi phạm gây tai nạn: tùy mức độ có thể bị đình chỉ công tác hoặc sa thải.

                    CHƯƠNG VI: ĐIỀU KHOẢN THI HÀNH
                    Quy định này có hiệu lực từ ngày 01/01/2026.
                    """),
                ("Huong Dan Van Hanh May CNC.docx", "Hướng dẫn vận hành máy CNC", "Hướng dẫn",
                    """
                    HƯỚNG DẪN VẬN HÀNH MÁY CNC

                    1. THÔNG SỐ MÁY
                    - Model: CNC-5000X
                    - Số series: CNC-2025-0882
                    - Công suất: 15kW
                    - Tốc độ trục chính: 0-12000 RPM
                    - Hành trình X/Y/Z: 800/500/500 mm
                    - Độ chính xác: ±0.005mm

                    2. CHUẨN BỊ TRƯỚC KHI VẬN HÀNH
                    a) Kiểm tra dầu bôi trơn: đảm bảo mức dầu trên vạch MIN
                    b) Kiểm tra dung dịch làm mát: đầy bình chứa
                    c) Kiểm tra dao cụ: gá đúng, siết chặt
                    d) Kiểm tra phôi: đã được định vị và kẹp chặt
                    e) Đóng cửa bảo vệ trước khi chạy máy

                    3. CÁC BƯỚC VẬN HÀNH
                    Bước 1: Bật nguồn chính và chờ máy khởi động (khoảng 2 phút)
                    Bước 2: Thực hiện Reference Return (Home) cho tất cả các trục
                    Bước 3: Gá phôi lên bàn máy, căn chỉnh và kẹp chặt
                    Bước 4: Gá dao vào ổ dao, nhập thông số dao vào bảng Offset
                    Bước 5: Nạp chương trình gia công qua USB hoặc cổng Ethernet
                    Bước 6: Mô phỏng chương trình (Dry Run) để kiểm tra
                    Bước 7: Thiết lập điểm Zero phôi (Work Offset)
                    Bước 8: Chạy chương trình gia công thực tế

                    4. THEO DÕI TRONG KHI GIA CÔNG
                    - Kiểm tra tiếng ồn bất thường mỗi 15 phút
                    - Kiểm tra độ rung của trục chính
                    - Kiểm tra nhiệt độ dung dịch làm mát < 40°C
                    - Kiểm tra phoi có bám vào dao không

                    5. KẾT THÚC CA LÀM VIỆC
                    - Rút dao về vị trí an toàn
                    - Tắt trục chính và bơm dung dịch làm mát
                    - Vệ sinh máy, hút phoi
                    - Tra dầu bôi trơn đường trượt
                    - Tắt nguồn chính

                    6. BẢO TRÌ ĐỊNH KỲ
                    - Hàng ngày: Vệ sinh máy, kiểm tra dầu
                    - Hàng tuần: Kiểm tra lọc dầu, lọc khí
                    - Hàng tháng: Kiểm tra độ chính xác, thay dầu
                    - Hàng quý: Bảo trì toàn bộ hệ thống
                    - Hàng năm: Đại tu, thay thế các bộ phận mòn
                    """),
                ("Ke Hoach San Xuat Thang 6.docx", "Kế hoạch sản xuất tháng 6/2026", "Kế hoạch",
                    """
                    KẾ HOẠCH SẢN XUẤT THÁNG 6/2026

                    I. MỤC TIÊU SẢN XUẤT
                    - Tổng sản lượng: 50,000 sản phẩm
                    - Doanh thu mục tiêu: 25 tỷ đồng
                    - Tỷ lệ đạt chất lượng: ≥ 95%
                    - Tỷ lệ giao hàng đúng hạn: ≥ 98%

                    II. PHÂN BỔ SẢN LƯỢNG THEO SẢN PHẨM
                    Mã SP001 - Sản phẩm A: 20,000 sản phẩm (40%)
                    Mã SP002 - Sản phẩm B: 15,000 sản phẩm (30%)
                    Mã SP003 - Sản phẩm C: 10,000 sản phẩm (20%)
                    Mã SP004 - Sản phẩm D: 5,000 sản phẩm (10%)

                    III. NGUYÊN VẬT LIỆU CẦN NHẬP
                    - Thép tấm HRC 3mm: 50 tấn
                    - Nhôm định hình 6061: 20 tấn
                    - Dầu bôi trơn: 500 lít
                    - Dung dịch làm mát: 1000 lít
                    - Vật tư đóng gói: 50,000 bộ

                    IV. LỊCH SẢN XUẤT THEO TUẦN
                    Tuần 1 (01-07/06): Sản phẩm A - 5000 sp
                    Tuần 2 (08-14/06): Sản phẩm A - 5000 sp, Sản phẩm B - 3000 sp
                    Tuần 3 (15-21/06): Sản phẩm B - 7000 sp, Sản phẩm C - 3000 sp
                    Tuần 4 (22-28/06): Sản phẩm C - 7000 sp, Sản phẩm D - 3000 sp
                    Tuần 5 (29-30/06): Sản phẩm D - 2000 sp

                    V. NHÂN SỰ
                    - Ca 1 (06:00-14:00): 30 công nhân
                    - Ca 2 (14:00-22:00): 25 công nhân
                    - Ca 3 (22:00-06:00): 15 công nhân (sản xuất sản phẩm D)

                    VI. BẢO TRÌ
                    - Tuần 1: Bảo trì máy CNC-01, CNC-02
                    - Tuần 2: Bảo trì dây chuyền sơn
                    - Tuần 3: Bảo trì máy ép nhựa
                    - Tuần 4: Bảo trì toàn bộ hệ thống
                    """),
                ("Bao Cao Ton Kho Thang 5.docx", "Báo cáo tồn kho tháng 5/2026", "Báo cáo",
                    """
                    BÁO CÁO TỒN KHO THÁNG 5/2026

                    I. TỔNG QUAN
                    Tổng giá trị tồn kho: 8.5 tỷ đồng
                    Tổng số mặt hàng: 245 items
                    Số mặt hàng tồn dưới mức an toàn: 12 items
                    Số mặt hàng tồn trên mức tối đa: 5 items

                    II. NGUYÊN VẬT LIỆU TỒN KHO
                    1. Thép tấm HRC 3mm: 25 tấn (tồn tối thiểu 20 tấn) - An toàn
                    2. Nhôm 6061: 8 tấn (tồn tối thiểu 10 tấn) - Cần nhập thêm
                    3. Đồng thau: 3 tấn (tồn tối thiểu 5 tấn) - Sắp hết
                    4. Nhựa ABS: 12 tấn (tồn tối thiểu 8 tấn) - An toàn
                    5. Sơn tĩnh điện: 500 kg (tồn tối thiểu 300 kg) - An toàn
                    6. Dầu bôi trơn: 50 lít (tồn tối thiểu 100 lít) - Cảnh báo

                    III. THÀNH PHẨM TỒN KHO
                    1. Sản phẩm A: 3,000 sp (tồn tối đa 5,000 sp)
                    2. Sản phẩm B: 1,500 sp (tồn tối đa 4,000 sp)
                    3. Sản phẩm C: 4,500 sp (tồn tối đa 3,000 sp) - Quá nhiều
                    4. Sản phẩm D: 800 sp (tồn tối đa 2,000 sp)

                    IV. KIẾN NGHỊ
                    - Nhập khẩn 5 tấn Nhôm 6061 và 3 tấn Đồng thau
                    - Mua thêm 100 lít dầu bôi trơn
                    - Tạm dừng sản xuất sản phẩm C, đẩy mạnh bán hàng tồn
                    - Kiểm kê kho cuối tháng vào ngày 31/05/2026

                    V. LỊCH SỬ GIAO DỊCH NỔI BẬT
                    - 02/05: Nhập kho 10 tấn thép HRC - Đơn hàng PO-2026-0512
                    - 10/05: Xuất kho 5 tấn thép cho lệnh sản xuất WO-2026-0381
                    - 15/05: Nhập kho 500 kg sơn tĩnh điện - Đơn hàng PO-2026-0533
                    - 22/05: Xuất kho 2000 sản phẩm A cho đơn hàng SO-2026-0291
                    """),
            };

            foreach (var (fileName, description, category, content) in samples)
            {
                var filePath = Path.Combine(tempDir, fileName);
                CreateDocx(filePath, content);
                try
                {
                    await _documentService.UploadDocumentAsync(
                        filePath, fileName,
                        description: description,
                        category: category,
                        uploadedByUserId: userId);
                }
                catch (Exception ex)
                {
                    _notification.ShowWarning($"Không thể upload {fileName}: {ex.Message}");
                }
            }

            _notification.ShowSuccess($"Đã tạo {samples.Length} tài liệu mẫu thành công!");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    private static void CreateDocx(string filePath, string content)
    {
        using var package = OpenXmlPackaging.WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
        var mainPart = package.AddMainDocumentPart();
        mainPart.Document = new OpenXmlWord.Document();
        var body = mainPart.Document.AppendChild(new OpenXmlWord.Body());

        var paragraphs = content.Split('\n', StringSplitOptions.None);
        foreach (var para in paragraphs)
        {
            var trimmed = para.TrimEnd('\r');
            var paragraph = body.AppendChild(new OpenXmlWord.Paragraph());

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                paragraph.AppendChild(new OpenXmlWord.ParagraphProperties(
                    new OpenXmlWord.SpacingBetweenLines { After = "120" }));
                continue;
            }

            var isHeader = trimmed == trimmed.ToUpper() && trimmed.Length > 3
                && (trimmed.StartsWith("CHƯƠNG") || trimmed.StartsWith("QUY TRÌNH")
                    || trimmed.StartsWith("BÁO CÁO") || trimmed.StartsWith("HƯỚNG DẪN")
                    || trimmed.StartsWith("KẾ HOẠCH") || trimmed.StartsWith("QUY ĐỊNH")
                    || char.IsDigit(trimmed[0]) && trimmed.Contains('.') && trimmed.Length < 80);

            var isSection = trimmed.Length < 80 && (trimmed.EndsWith(':')
                || (trimmed.StartsWith("I.") || trimmed.StartsWith("II.") || trimmed.StartsWith("III.")
                    || trimmed.StartsWith("IV.") || trimmed.StartsWith("V.") || trimmed.StartsWith("VI.")
                    || trimmed.StartsWith("VII."))
                || (trimmed.StartsWith("1.") || trimmed.StartsWith("2.") || trimmed.StartsWith("3.")
                    || trimmed.StartsWith("4.") || trimmed.StartsWith("5.") || trimmed.StartsWith("6.")));

            var runProps = new OpenXmlWord.RunProperties();
            if (isHeader)
            {
                runProps.Append(new OpenXmlWord.Bold());
                runProps.Append(new OpenXmlWord.FontSize { Val = "28" });
                runProps.Append(new OpenXmlWord.Color { Val = "1E3A5F" });
            }
            else if (isSection)
            {
                runProps.Append(new OpenXmlWord.Bold());
                runProps.Append(new OpenXmlWord.FontSize { Val = "24" });
                runProps.Append(new OpenXmlWord.Color { Val = "2C5282" });
            }
            else
            {
                runProps.Append(new OpenXmlWord.FontSize { Val = "22" });
            }

            var run = paragraph.AppendChild(new OpenXmlWord.Run());
            run.AppendChild(runProps);
            run.AppendChild(new OpenXmlWord.Text(trimmed) { Space = SpaceProcessingModeValues.Preserve });

            paragraph.AppendChild(new OpenXmlWord.ParagraphProperties(
                new OpenXmlWord.SpacingBetweenLines { After = isHeader ? "200" : isSection ? "160" : "80" },
                new OpenXmlWord.Indentation { Left = isSection ? "200" : "0" }));
        }
    }
}
