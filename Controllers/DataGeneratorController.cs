using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Infrastructure;
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
            const int batchSize = 100;
            var progress = _progressTracker[sessionId];

            try
            {
                using (var context = new DBContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    context.Configuration.ValidateOnSaveEnabled = false;

                    var categories = new List<DMTinTuc>();
                    var random = new Random();

                    // Phân bổ số lượng
                    int level1Count = (int)(count * 0.1);                // Cấp 1: 10%
                    int remainingCount = count - level1Count;            // Còn lại: 90%
                    int perLevel = remainingCount / 7;                   // Cấp 2 đến 8
                    int distributed = 0;

                    var levelCategories = new Dictionary<int, List<DMTinTuc>>();

                    for (int level = 1; level <= 8; level++)
                    {
                        int itemsToCreate = level == 1 ? level1Count : perLevel;
                        if (level == 8) // Dồn phần dư vào cấp 8
                        {
                            itemsToCreate = count - progress.ProcessedItems;
                        }

                        progress.Message = $"Đang tạo danh mục cấp {level} ({itemsToCreate} danh mục)...";
                        var currentLevelList = new List<DMTinTuc>();

                        for (int i = 0; i < itemsToCreate; i++)
                        {
                            DMTinTuc parent = null;
                            int? parentId = null;
                            int? idCap1 = null, idCap2 = null, idCap3 = null, idCap4 = null, idCap5 = null, idCap6 = null, idCap7 = null;

                            if (level > 1)
                            {
                                var parentList = levelCategories[level - 1];
                                parent = parentList[random.Next(parentList.Count)];
                                parentId = parent.Id;

                                // Sao chép IdCap từ cha
                                idCap1 = parent.IdCap1;
                                idCap2 = parent.IdCap2;
                                idCap3 = parent.IdCap3;
                                idCap4 = parent.IdCap4;
                                idCap5 = parent.IdCap5;
                                idCap6 = parent.IdCap6;
                                idCap7 = parent.IdCap7;

                                // Gán IdCapN tương ứng cấp hiện tại
                                switch (level - 1)
                                {
                                    case 1: idCap1 = parent.Id; break;
                                    case 2: idCap2 = parent.Id; break;
                                    case 3: idCap3 = parent.Id; break;
                                    case 4: idCap4 = parent.Id; break;
                                    case 5: idCap5 = parent.Id; break;
                                    case 6: idCap6 = parent.Id; break;
                                    case 7: idCap7 = parent.Id; break;
                                }
                            }

                            var category = new DMTinTuc
                            {
                                Ten = $"Danh mục cấp {level} - {i + 1:D6}",
                                MoTa = $"Mô tả danh mục cấp {level} số {i + 1}",
                                Cap = level,
                                ParentId = parentId,
                                IdCap1 = idCap1,
                                IdCap2 = idCap2,
                                IdCap3 = idCap3,
                                IdCap4 = idCap4,
                                IdCap5 = idCap5,
                                IdCap6 = idCap6,
                                IdCap7 = idCap7,
                                CreateDate = DateTime.Now.AddDays(-random.Next(365)),
                                Status = true
                            };

                            categories.Add(category);
                            currentLevelList.Add(category);

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
                            categories.Clear();
                        }

                        levelCategories[level] = currentLevelList;
                    }

                    progress.IsCompleted = true;
                    progress.Message = $"✅ Hoàn thành! Đã tạo {progress.ProcessedItems} danh mục từ cấp 1 đến 8.";
                }
            }
            catch (DbUpdateException ex)
            {
                progress.Message = $"❌ Lỗi DB: {ex.InnerException?.InnerException?.Message}";
                progress.IsCompleted = true;
            }
            catch (Exception ex)
            {
                progress.Message = $"❌ Lỗi: {ex.Message}";
                progress.IsCompleted = true;
            }
        }

        private async Task GenerateNewsAsync(int count, string sessionId)
        {
            const int batchSize = 5000;
            var progress = _progressTracker[sessionId];

            try
            {
                using (var context = new DBContext())
                using (var connection = new SqlConnection(context.Database.Connection.ConnectionString))
                {
                    await connection.OpenAsync();

                    context.Configuration.AutoDetectChangesEnabled = false;
                    context.Configuration.ValidateOnSaveEnabled = false;

                    var categoryIds = await context.DMTinTuc.Select(x => x.Id).ToListAsync();
                    if (!categoryIds.Any())
                    {
                        progress.Message = "Không có danh mục nào để gán!";
                        progress.IsCompleted = true;
                        return;
                    }

                    var random = new Random();
                    var newsList = new List<TinTuc>();
                    var dtTinTucDm = new DataTable();
                    dtTinTucDm.Columns.Add("IdTinTuc", typeof(int));
                    dtTinTucDm.Columns.Add("IdDMTinTuc", typeof(int));

                    progress.Message = "Đang tạo tin tức...";

                    for (int i = 0; i < count; i++)
                    {
                        var news = new TinTuc
                        {
                            TieuDe = $"Tin {i + 1:D8} - {GenerateRandomTitle()}",
                            TrichNgan = GenerateRandomSummary(),
                            ChiTiet = GenerateRandomContent(),
                            CreateDate = DateTime.Now.AddDays(-random.Next(365)),
                            Status = 1
                        };
                        newsList.Add(news);

                        if (newsList.Count >= batchSize)
                        {
                            context.TinTuc.AddRange(newsList);
                            await context.SaveChangesAsync();

                            foreach (var newsItem in newsList)
                            {
                                var dmCount = random.Next(1, 4);
                                var selected = categoryIds.OrderBy(_ => random.Next()).Take(dmCount).ToList();

                                foreach (var dmId in selected)
                                {
                                    dtTinTucDm.Rows.Add(newsItem.Id, dmId);
                                }
                            }

                            await BulkInsertNewsCategories(connection, dtTinTucDm);
                            await BulkUpdateCategoryCountAsync(connection, dtTinTucDm);

                            progress.ProcessedItems += newsList.Count;
                            newsList.Clear();
                            dtTinTucDm.Rows.Clear();
                        }
                    }

                    if (newsList.Any())
                    {
                        context.TinTuc.AddRange(newsList);
                        await context.SaveChangesAsync();

                        foreach (var newsItem in newsList)
                        {
                            var dmCount = random.Next(1, 4);
                            var selected = categoryIds.OrderBy(_ => random.Next()).Take(dmCount).ToList();

                            foreach (var dmId in selected)
                            {
                                dtTinTucDm.Rows.Add(newsItem.Id, dmId);
                            }
                        }

                        await BulkInsertNewsCategories(connection, dtTinTucDm);
                        await BulkUpdateCategoryCountAsync(connection, dtTinTucDm);

                        progress.ProcessedItems += newsList.Count;
                    }

                    progress.IsCompleted = true;
                    progress.Message = $"Hoàn thành! Đã tạo {progress.ProcessedItems} tin tức.";
                }
            }
            catch (DbUpdateException ex)
            {
                progress.Message = $"❌ Lỗi DB: {ex.InnerException?.InnerException?.Message}";
                progress.IsCompleted = true;
            }
            catch (Exception ex)
            {
                progress.Message = $"Lỗi: {ex.Message}";
                progress.IsCompleted = true;
            }
        }
        private async Task BulkInsertNewsCategories(SqlConnection connection, DataTable dtTinTucDm)
        {
            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                bulkCopy.DestinationTableName = "TinTuc_DM";
                bulkCopy.ColumnMappings.Add("IdTinTuc", "IdTinTuc");
                bulkCopy.ColumnMappings.Add("IdDMTinTuc", "IdDMTinTuc");

                await bulkCopy.WriteToServerAsync(dtTinTucDm);
            }
        }
        private async Task BulkUpdateCategoryCountAsync(SqlConnection connection, DataTable dtTinTucDm)
        {
            // Tạo bảng tạm #TmpDM
            await new SqlCommand(@"IF OBJECT_ID('tempdb..#TmpDM') IS NOT NULL DROP TABLE #TmpDM;
                                        CREATE TABLE #TmpDM (Id INT);
                                    ", connection).ExecuteNonQueryAsync();

            // Lọc ra các IdDMTinTuc duy nhất
            var tempTable = new DataTable();
            tempTable.Columns.Add("Id", typeof(int));

            var uniqueIds = dtTinTucDm.AsEnumerable()
                                      .Select(r => r.Field<int>("IdDMTinTuc"))
                                      .Distinct();

            foreach (var id in uniqueIds)
            {
                tempTable.Rows.Add(id);
            }
            // Bulk copy vào #TmpDM
            using (var bulk = new SqlBulkCopy(connection))
            {
                bulk.DestinationTableName = "#TmpDM";
                bulk.ColumnMappings.Add("Id", "Id");
                await bulk.WriteToServerAsync(tempTable);
            }

        }
        //private async Task GenerateNewsAsync(int count, string sessionId)
        //{
        //    const int batchSize = 5000; // Batch lớn hơn cho tin tức
        //    var progress = _progressTracker[sessionId];

        //    try
        //    {
        //        using (var context = new DBContext())
        //        {
        //            // Tắt AutoDetectChanges để tăng hiệu suất
        //            context.Configuration.AutoDetectChangesEnabled = false;
        //            context.Configuration.ValidateOnSaveEnabled = false;

        //            // Lấy danh sách ID danh mục có sẵn
        //            progress.Message = "Đang tải danh sách danh mục...";
        //            var categoryIds = await context.DMTinTuc
        //                .Select(x => x.Id)
        //                .ToListAsync();

        //            if (!categoryIds.Any())
        //            {
        //                progress.Message = "Không có danh mục nào để gán cho tin tức!";
        //                progress.IsCompleted = true;
        //                return;
        //            }

        //            var random = new Random();
        //            var newsList = new List<TinTuc>();
        //            var newsCategories = new List<string>(); // Để bulk insert vào bảng nhiều-nhiều

        //            progress.Message = "Đang tạo tin tức...";

        //            for (int i = 0; i < count; i++)
        //            {
        //                var news = new TinTuc
        //                {
        //                    TieuDe = $"Tin tức số {i + 1:D8} - {GenerateRandomTitle()}",
        //                    TrichNgan = GenerateRandomSummary(),
        //                    ChiTiet = GenerateRandomContent(),
        //                    CreateDate = DateTime.Now.AddDays(-random.Next(365)),
        //                    Status = 1
        //                };

        //                newsList.Add(news);

        //                if (newsList.Count >= batchSize)
        //                {
        //                    // Bulk insert tin tức
        //                    context.TinTuc.AddRange(newsList);
        //                    await context.SaveChangesAsync();

        //                    // Lấy ID của các tin tức vừa insert
        //                    var insertedNewsIds = newsList.Select(n => n.Id).ToList();

        //                    // Tạo quan hệ tin tức - danh mục
        //                    await CreateNewsCategories(context, insertedNewsIds, categoryIds, random);

        //                    progress.ProcessedItems += newsList.Count;
        //                    newsList.Clear();
        //                }
        //            }

        //            // Xử lý batch cuối cùng
        //            if (newsList.Any())
        //            {
        //                context.TinTuc.AddRange(newsList);
        //                await context.SaveChangesAsync();

        //                var insertedNewsIds = newsList.Select(n => n.Id).ToList();
        //                await CreateNewsCategories(context, insertedNewsIds, categoryIds, random);

        //                progress.ProcessedItems += newsList.Count;
        //            }

        //            progress.IsCompleted = true;
        //            progress.Message = $"Hoàn thành! Đã tạo {progress.ProcessedItems} tin tức.";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        progress.Message = $"Lỗi: {ex.Message}";
        //        progress.IsCompleted = true;
        //    }
        //}

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
                var dmList = context.DMTinTuc.Where(x => categoryIds.Contains(x.Id)).OrderByDescending(x => x.Cap).ToList();
                for (var i = 0; i < dmList.Count; i++)
                {
                    if (i > 0 && (dmList[i].IdCap1 == dmList[i - 1].IdCap1 || dmList[i].Id == dmList[i - 1].IdCap1))
                    {

                    }
                    else
                    {
                        context.Database.ExecuteSqlCommand("exec TangSoLuongTinTucInDm @id", new SqlParameter("@id", dmList[i].Id));
                    }
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