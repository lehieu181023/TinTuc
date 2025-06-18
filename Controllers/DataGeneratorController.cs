using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using TT.Models;

namespace TT.Controllers
{
    public class DataGeneratorController : Controller
    {
        private readonly DBContext _context;
        private static readonly Dictionary<string, GenerationProgress> _progressTracker = new Dictionary<string, GenerationProgress>();

        public DataGeneratorController()
        {
            _context = new DBContext();
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> GenerateCategories(int count, string sessionId)
        {
            try
            {
                var progress = new GenerationProgress
                {
                    SessionId = sessionId,
                    TotalItems = count,
                    ProcessedItems = 0,
                    IsCompleted = false,
                    StartTime = DateTime.Now,
                    Message = "Bắt đầu tạo danh mục..."
                };

                _progressTracker[sessionId] = progress;

                // Chạy tạo dữ liệu trong background
                Task.Run(() => GenerateCategoriesAsync(count, sessionId));

                return Json(new { success = true, sessionId = sessionId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GenerateNews(int count, string sessionId)
        {
            try
            {
                var progress = new GenerationProgress
                {
                    SessionId = sessionId,
                    TotalItems = count,
                    ProcessedItems = 0,
                    IsCompleted = false,
                    StartTime = DateTime.Now,
                    Message = "Bắt đầu tạo tin tức..."
                };

                _progressTracker[sessionId] = progress;

                // Chạy tạo dữ liệu trong background
                Task.Run(() => GenerateNewsAsync(count, sessionId));

                return Json(new { success = true, sessionId = sessionId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetProgress(string sessionId)
        {
            if (_progressTracker.ContainsKey(sessionId))
            {
                var progress = _progressTracker[sessionId];
                var elapsed = DateTime.Now - progress.StartTime;
                var percentage = progress.TotalItems > 0 ? (double)progress.ProcessedItems / progress.TotalItems * 100 : 0;

                // Tính toán thời gian còn lại
                var estimatedTimeRemaining = TimeSpan.Zero;
                if (progress.ProcessedItems > 0 && percentage < 100)
                {
                    var avgTimePerItem = elapsed.TotalSeconds / progress.ProcessedItems;
                    var remainingItems = progress.TotalItems - progress.ProcessedItems;
                    estimatedTimeRemaining = TimeSpan.FromSeconds(avgTimePerItem * remainingItems);
                }

                return Json(new
                {
                    percentage = Math.Round(percentage, 2),
                    processedItems = progress.ProcessedItems,
                    totalItems = progress.TotalItems,
                    isCompleted = progress.IsCompleted,
                    message = progress.Message,
                    elapsedTime = elapsed.ToString(@"hh\:mm\:ss"),
                    estimatedTimeRemaining = estimatedTimeRemaining.ToString(@"hh\:mm\:ss"),
                    itemsPerSecond = progress.ProcessedItems > 0 ? Math.Round(progress.ProcessedItems / elapsed.TotalSeconds, 2) : 0
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { percentage = 0, message = "Không tìm thấy tiến trình" }, JsonRequestBehavior.AllowGet);
        }

        private async Task GenerateCategoriesAsync(int count, string sessionId)
        {
            const int batchSize = 1000; // Xử lý theo batch để tối ưu
            var progress = _progressTracker[sessionId];

            try
            {
                using (var context = new DBContext())
                {
                    // Tắt AutoDetectChanges để tăng hiệu suất
                    context.Configuration.AutoDetectChangesEnabled = false;
                    context.Configuration.ValidateOnSaveEnabled = false;

                    var categories = new List<DMTinTuc>();
                    var random = new Random();

                    // Tạo danh mục cấp 1 trước
                    var level1Count = Math.Min(count / 10, 1000); // Tối đa 1000 danh mục cấp 1
                    progress.Message = "Đang tạo danh mục cấp 1...";

                    for (int i = 0; i < level1Count; i++)
                    {
                        var category = new DMTinTuc
                        {
                            Ten = $"Danh mục cấp 1 - {i + 1:D6}",
                            MoTa = $"Mô tả danh mục cấp 1 số {i + 1}",
                            Cap = 1,
                            CreateDate = DateTime.Now.AddDays(-random.Next(365)),
                            Status = true
                        };
                        categories.Add(category);

                        if (categories.Count >= batchSize)
                        {
                            context.DMTinTuc.AddRange(categories);
                            await context.SaveChangesAsync();

                            progress.ProcessedItems += categories.Count;
                            categories.Clear();
                        }
                    }

                    // Lưu batch cuối cùng của cấp 1
                    if (categories.Any())
                    {
                        context.DMTinTuc.AddRange(categories);
                        await context.SaveChangesAsync();
                        progress.ProcessedItems += categories.Count;
                        categories.Clear();
                    }

                    // Lấy danh sách ID danh mục cấp 1 vừa tạo
                    var level1Ids = await context.DMTinTuc
                        .Where(x => x.Cap == 1)
                        .OrderByDescending(x => x.Id)
                        .Take(level1Count)
                        .Select(x => new { x.Id, x.Ten })
                        .ToListAsync();

                    // Tạo danh mục con
                    var remainingCount = count - progress.ProcessedItems;
                    progress.Message = "Đang tạo danh mục con...";

                    for (int i = 0; i < remainingCount; i++)
                    {
                        var parentIndex = random.Next(level1Ids.Count);
                        var parent = level1Ids[parentIndex];

                        var category = new DMTinTuc
                        {
                            Ten = $"Danh mục con - {parent.Ten} - {i + 1:D6}",
                            MoTa = $"Mô tả danh mục con số {i + 1}",
                            ParentId = parent.Id,
                            Cap = 2,
                            IdCap1 = parent.Id,
                            CreateDate = DateTime.Now.AddDays(-random.Next(365)),
                            Status = true
                        };
                        categories.Add(category);

                        if (categories.Count >= batchSize)
                        {
                            context.DMTinTuc.AddRange(categories);
                            await context.SaveChangesAsync();

                            progress.ProcessedItems += categories.Count;
                            categories.Clear();
                        }
                    }

                    // Lưu batch cuối cùng
                    if (categories.Any())
                    {
                        context.DMTinTuc.AddRange(categories);
                        await context.SaveChangesAsync();
                        progress.ProcessedItems += categories.Count;
                    }

                    progress.IsCompleted = true;
                    progress.Message = $"Hoàn thành! Đã tạo {progress.ProcessedItems} danh mục.";
                }
            }
            catch (Exception ex)
            {
                progress.Message = $"Lỗi: {ex.Message}";
                progress.IsCompleted = true;
            }
        }

        private async Task GenerateNewsAsync(int count, string sessionId)
        {
            const int batchSize = 5000; // Batch lớn hơn cho tin tức
            var progress = _progressTracker[sessionId];

            try
            {
                using (var context = new DBContext())
                {
                    // Tắt AutoDetectChanges để tăng hiệu suất
                    context.Configuration.AutoDetectChangesEnabled = false;
                    context.Configuration.ValidateOnSaveEnabled = false;

                    // Lấy danh sách ID danh mục có sẵn
                    progress.Message = "Đang tải danh sách danh mục...";
                    var categoryIds = await context.DMTinTuc
                        .Select(x => x.Id)
                        .ToListAsync();

                    if (!categoryIds.Any())
                    {
                        progress.Message = "Không có danh mục nào để gán cho tin tức!";
                        progress.IsCompleted = true;
                        return;
                    }

                    var random = new Random();
                    var newsList = new List<TinTuc>();
                    var newsCategories = new List<string>(); // Để bulk insert vào bảng nhiều-nhiều

                    progress.Message = "Đang tạo tin tức...";

                    for (int i = 0; i < count; i++)
                    {
                        var news = new TinTuc
                        {
                            TieuDe = $"Tin tức số {i + 1:D8} - {GenerateRandomTitle()}",
                            TrichNgan = GenerateRandomSummary(),
                            ChiTiet = GenerateRandomContent(),
                            CreateDate = DateTime.Now.AddDays(-random.Next(365)),
                            Status = 1
                        };

                        newsList.Add(news);

                        if (newsList.Count >= batchSize)
                        {
                            // Bulk insert tin tức
                            context.TinTuc.AddRange(newsList);
                            await context.SaveChangesAsync();

                            // Lấy ID của các tin tức vừa insert
                            var insertedNewsIds = newsList.Select(n => n.Id).ToList();

                            // Tạo quan hệ tin tức - danh mục
                            await CreateNewsCategories(context, insertedNewsIds, categoryIds, random);

                            progress.ProcessedItems += newsList.Count;
                            newsList.Clear();
                        }
                    }

                    // Xử lý batch cuối cùng
                    if (newsList.Any())
                    {
                        context.TinTuc.AddRange(newsList);
                        await context.SaveChangesAsync();

                        var insertedNewsIds = newsList.Select(n => n.Id).ToList();
                        await CreateNewsCategories(context, insertedNewsIds, categoryIds, random);

                        progress.ProcessedItems += newsList.Count;
                    }

                    progress.IsCompleted = true;
                    progress.Message = $"Hoàn thành! Đã tạo {progress.ProcessedItems} tin tức.";
                }
            }
            catch (Exception ex)
            {
                progress.Message = $"Lỗi: {ex.Message}";
                progress.IsCompleted = true;
            }
        }

        private async Task CreateNewsCategories(DBContext context, List<int> newsIds, List<int> categoryIds, Random random)
        {
            var sql = "INSERT INTO TinTuc_DM (IdTinTuc, IdDMTinTuc) VALUES ";
            var values = new List<string>();

            foreach (var newsId in newsIds)
            {
                // Mỗi tin tức có 1-3 danh mục ngẫu nhiên
                var categoryCount = random.Next(1, 4);
                var selectedCategories = categoryIds.OrderBy(x => random.Next()).Take(categoryCount).ToList();

                foreach (var categoryId in selectedCategories)
                {
                    values.Add($"({newsId}, {categoryId})");
                }
            }

            if (values.Any())
            {
                var fullSql = sql + string.Join(", ", values);
                await context.Database.ExecuteSqlCommandAsync(fullSql);
            }
        }

        private string GenerateRandomTitle()
        {
            var titles = new[]
            {
                "Công nghệ mới thay đổi cuộc sống",
                "Kinh tế phát triển bền vững",
                "Giáo dục đổi mới sáng tạo",
                "Y tế nâng cao chất lượng",
                "Văn hóa truyền thống hiện đại",
                "Thể thao Việt Nam tỏa sáng",
                "Du lịch phát triển mạnh mẽ",
                "Nông nghiệp ứng dụng công nghệ cao"
            };
            var random = new Random();
            return titles[random.Next(titles.Length)];
        }

        private string GenerateRandomSummary()
        {
            return "Đây là tóm tắt ngắn gọn về nội dung tin tức, cung cấp thông tin tổng quan và hấp dẫn người đọc tiếp tục theo dõi nội dung chi tiết.";
        }

        private string GenerateRandomContent()
        {
            return @"Nội dung chi tiết của tin tức bao gồm các thông tin đầy đủ, chính xác và cập nhật về sự kiện. 
                    Bài viết được biên soạn kỹ lưỡng với các nguồn tin đáng tin cậy, mang lại giá trị thông tin cao cho người đọc.
                    Chúng tôi cam kết cung cấp những thông tin chất lượng, khách quan và có tính thời sự cao.
                    Mọi thông tin đều được kiểm chứng và xác thực trước khi đăng tải để đảm bảo độ tin cậy.";
        }

        [HttpPost]
        public JsonResult ClearProgress(string sessionId)
        {
            if (_progressTracker.ContainsKey(sessionId))
            {
                _progressTracker.Remove(sessionId);
            }
            return Json(new { success = true });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    
}