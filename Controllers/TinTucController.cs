using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using TT.Models;

namespace TT.Controllers
{
    public class TinTucController : Controller
    {
        private readonly DBContext _context;

        public TinTucController()
        {
            _context = new DBContext();
        }

        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> Listdata(string keysearch = "", string lisdm = "", int page = 1, int pagesize = 10,int tongbanghi = 1)
        {
            List<TinTuc> list = null;
            var total = 0;

            try
            {
                var listdata = _context.TinTuc
                    .Include(x => x.DMTinTuc)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(lisdm))
                {
                    var listdmid = lisdm.Split(',').Select(int.Parse).ToList();
                    foreach (var id in listdmid)
                    {
                        var dm = _context.DMTinTuc.FirstOrDefault(x => x.Id == id);
                        if (dm != null)
                        {
                            listdata = listdata.Where(t => t.DMTinTuc.Any(d => d.Id == dm.Id || d.IdCap1 == dm.Id || d.IdCap2 == dm.Id || d.IdCap3 == dm.Id || d.IdCap4 == dm.Id || d.IdCap5 == dm.Id || d.IdCap6 == dm.Id || d.IdCap7 == dm.Id));
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(keysearch))
                {
                    string keywordd = ConvertToFullTextSearch(keysearch);
                    string sqlCount = $@"
                                            SELECT COUNT(DISTINCT TT.Id)
                                            FROM TinTuc TT
                                            INNER JOIN TinTuc_DM TDM ON TT.Id = TDM.IdTinTuc
                                            INNER JOIN DMTinTuc DM ON TDM.IdDMTinTuc = DM.Id
                                            WHERE CONTAINS((TT.TieuDe, TT.TrichNgan, TT.ChiTiet), '{keywordd}')
                                        ";
                    total = _context.Database.SqlQuery<int>(sqlCount).FirstOrDefault();
                    string sql = $@"
                                    SELECT 
                                        TT.Id,
                                        TT.TieuDe,
                                        TT.TrichNgan,
                                        TT.ChiTiet,
                                        TT.CreateDate,
                                        TT.Status,
                                        STRING_AGG(DM.Ten, ',') AS TenDm
                                    FROM TinTuc TT
                                    INNER JOIN TinTuc_DM TDM ON TT.Id = TDM.IdTinTuc
                                    INNER JOIN DMTinTuc DM ON TDM.IdDMTinTuc = DM.Id
                                    WHERE CONTAINS((TT.TieuDe, TT.TrichNgan, TT.ChiTiet), '{keywordd}')
                                    GROUP BY TT.Id, TT.TieuDe, TT.TrichNgan, TT.ChiTiet, TT.CreateDate, TT.Status
                                    ORDER BY TT.Id
                                    OFFSET {page-1} ROWS
                                    FETCH NEXT {pagesize} ROWS ONLY;
                                ";
                    var data = _context.Database.SqlQuery<TinTucDto>(sql).ToList();

                    list = data.Select(dto => new TinTuc
                    {
                        Id = dto.Id,
                        TieuDe = dto.TieuDe,
                        TrichNgan = dto.TrichNgan,
                        ChiTiet = dto.ChiTiet,
                        CreateDate = dto.CreateDate,
                        Status = dto.Status,
                        TenDm = dto.TenDm
                    }).ToList();

                    ViewBag.curentpage = page;
                    ViewBag.curentpagesize = pagesize;
                    ViewBag.total = total;
                    ViewBag.totalPage = Math.Ceiling((double)total / pagesize);
                    return PartialView(list);
                    
                }
                total = await listdata.CountAsync();

                list = await listdata
                    .OrderByDescending(x => x.Id)
                    .Skip((page - 1) * pagesize)
                    .Take(pagesize)
                    .ToListAsync();

                // Lấy tên danh mục để gán vào TinTuc
                foreach (var item in list)
                {
                    item.TenDm = string.Join(", ", item.DMTinTuc.Select(d => d.Ten));
                }

                ViewBag.curentpage = page;
                ViewBag.curentpagesize = pagesize;
                ViewBag.total = total;
                ViewBag.totalPage = Math.Ceiling((double)total / pagesize);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }

            return PartialView(list);
        }
        public string ConvertToFullTextSearch(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput)) return "";

            var words = rawInput
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .ToArray();

            if (words.Length == 1)
            {
                return $"\"{words[0]}\"";
            }

            return string.Join(" AND ", words.Select(w => $"\"{w}\""));
        }

        //public List<DMTinTuc> GetAllChil(List<DMTinTuc> rootList)
        //{
        //    var all = CacheHelper.GetDanhMucTinTuc(_context);

        //    var result = new List<DMTinTuc>(rootList);

        //    var queue = new Queue<DMTinTuc>(rootList);

        //    while (queue.Count > 0)
        //    {
        //        var current = queue.Dequeue();

        //        var children = all.Where(x => x.ParentId == current.Id).ToList();

        //        foreach (var child in children)
        //        {
        //            result.Add(child);
        //            queue.Enqueue(child);
        //        }
        //    }

        //    return result;
        //}


        public ActionResult Create()
        {
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include ="Id,TieuDe,TrichNgan,ChiTiet,CreateDate,Status")] TinTuc tinTuc,List<int> lstIdDM)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (!ModelState.IsValid)
                        return Json(new { success = false, message = "Lỗi dữ liệu nhập" });
                    var dmList = new List<DMTinTuc>();
                    if (lstIdDM != null && lstIdDM.Any())
                    {
                        dmList = await _context.DMTinTuc
                                                   .Where(dm => lstIdDM.Contains(dm.Id))
                                                   .ToListAsync();
                        foreach (var dm in dmList)
                        {
                            tinTuc.DMTinTuc.Add(dm);
                        }
                    }
                    tinTuc.CreateDate = DateTime.Now;
                    _context.TinTuc.Add(tinTuc);
                    await _context.SaveChangesAsync();
                    transaction.Commit();
                    return Json(new { success = true, message = "Thêm mới thành công" }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Thêm mới thất bại, lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }


        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var tinTuc = await _context.TinTuc
                                        .Include(t => t.DMTinTuc)
                                        .FirstOrDefaultAsync(t => t.Id == id);
            if (tinTuc == null)
            {
                return HttpNotFound();
            }
            tinTuc.lstIdDm = tinTuc.DMTinTuc.Select(d => d.Id).ToList();
            return PartialView(tinTuc);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, [Bind(Include = "Id,TieuDe,TrichNgan,ChiTiet,CreateDate,Status")] TinTuc tinTuc, List<int> lstIdDM)
        {
            if (id != tinTuc.Id)
                return Json(new { success = false, message = "Id không tồn tại" }, JsonRequestBehavior.AllowGet);

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" }, JsonRequestBehavior.AllowGet);
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    tinTuc.CreateDate = DateTime.Now;
                    var existing = await _context.TinTuc
                                                 .Include(t => t.DMTinTuc)
                                                 .FirstOrDefaultAsync(t => t.Id == id);

                    if (existing == null)
                        return Json(new { success = false, message = "Tin tức không tồn tại" }, JsonRequestBehavior.AllowGet);

                    _context.Entry(existing).CurrentValues.SetValues(tinTuc);

                    if (lstIdDM == null)
                    {
                        lstIdDM = new List<int>();
                    }

                    var currentIds = existing.DMTinTuc.Select(d => d.Id).ToList();

                    var toRemove = existing.DMTinTuc.Where(d => !lstIdDM.Contains(d.Id)).ToList();
                    foreach (var dm in toRemove)
                    {
                        existing.DMTinTuc.Remove(dm);


                    }
                    var toAddIds = lstIdDM.Except(currentIds).ToList();
                    if (toAddIds.Any())
                    {
                        var toAdd = await _context.DMTinTuc
                                                  .Where(d => toAddIds.Contains(d.Id))
                                                  .ToListAsync();
                        foreach (var dm in toAdd)
                        {
                            existing.DMTinTuc.Add(dm);
                            
                        }

                    }
                    await _context.SaveChangesAsync();
                    transaction.Commit();
                    return Json(new { success = true, message = "Sửa thành công" }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Sửa thất bại, lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }


        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            var tinTuc = await _context.TinTuc.FindAsync(id);
            if (tinTuc != null)
            {
                _context.TinTuc.Remove(tinTuc);
            }
            else
            {
                return Json(new { success = false, message = "Danh mục không tồn tại" }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa thành công" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { success = false, message = "lỗi:" + e }, JsonRequestBehavior.AllowGet);
            }

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
