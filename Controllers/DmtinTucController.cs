
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using TT.Models;



namespace Core.Areas.Hieu.Controllers
{
    public class DMTinTucController : Controller
    {
        private readonly DBContext _context;

        public DMTinTucController()
        {
            _context = new DBContext();
        }

        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> Listdata(String keysearch = "", int? idsearch = 0, int page = 1, int pagesize = 10)
        {
            List<DMTinTuc> list = null;
            var total = 0;
            try
            {
                var listdata = _context.DMTinTuc.AsNoTracking().AsQueryable();
                
                if (idsearch != null && idsearch > 0)
                {
                    listdata = listdata.Where(x => x.ParentId == idsearch);
                }

                if (!string.IsNullOrWhiteSpace(keysearch) && (idsearch == null || idsearch <= 0))
                {
                    var keyword = keysearch.Trim().ToLowerInvariant();
                    listdata = listdata.Where(x => x.Ten.ToLower().Contains(keyword));
                }

                total = await  listdata.CountAsync();
                list = listdata
                    .OrderByDescending(x => x.Id)
                    .Skip((page - 1) * pagesize)
                    .Take(pagesize)
                    .ToList();
                foreach (var item in list)
                {
                    item.TenDmCha = _context.DMTinTuc.FirstOrDefault(x => x.Id == item.ParentId)?.Ten;
                }
                
                ViewBag.curentpage = page;
                ViewBag.curentpagesize = pagesize;
                ViewBag.total = total;
                ViewBag.totalPage = Math.Ceiling(((double)total / pagesize));
                ViewBag.stt = (page - 1) * pagesize;
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi" + ex }, JsonRequestBehavior.AllowGet);
            }

            return PartialView(list);
        }


        public JsonResult GetDmSelect(string searchTerm, int page = 1, int pageSize = 20)
        {
            var list = _context.DMTinTuc.AsNoTracking().AsQueryable();
            list = list.Where(d => d.Ten.Contains(searchTerm))
                .OrderBy(d => d.Ten);

            var total = list.Count();
            var items = list
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new {
                    id = d.Id,
                    text = d.Ten
                })
                .ToList();

            bool hasMore = (page * pageSize) < total;

            return Json(new
            {
                items = items,
                hasMore = hasMore
            }, JsonRequestBehavior.AllowGet);
        }


        public ActionResult Create()
        {
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Ten,MoTa,ParentId,Status")] DMTinTuc dmtinTuc)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                           .Select(x => new {
                                               Field = x.Key,
                                               Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                                           });

                    return Json(new { success = false, message = "Dữ liệu không hợp lệ", details = errors });
                }
                dmtinTuc.CreateDate = DateTime.Now;

                if (dmtinTuc?.ParentId > 0)
                {
                    var DMcha = _context.DMTinTuc.AsNoTracking().FirstOrDefault(x => x.Id == dmtinTuc.ParentId);
                    if (DMcha != null)
                    {
                        int capCha = DMcha.Cap;
                        dmtinTuc.Cap = capCha + 1;

                        dmtinTuc.IdCap1 = DMcha.IdCap1;
                        dmtinTuc.IdCap2 = DMcha.IdCap2;
                        dmtinTuc.IdCap3 = DMcha.IdCap3;
                        dmtinTuc.IdCap4 = DMcha.IdCap4;
                        dmtinTuc.IdCap5 = DMcha.IdCap5;
                        dmtinTuc.IdCap6 = DMcha.IdCap6;
                        dmtinTuc.IdCap7 = DMcha.IdCap7;

                        switch (capCha)
                        {
                            case 1: dmtinTuc.IdCap1 = DMcha.Id; break;
                            case 2: dmtinTuc.IdCap2 = DMcha.Id; break;
                            case 3: dmtinTuc.IdCap3 = DMcha.Id; break;
                            case 4: dmtinTuc.IdCap4 = DMcha.Id; break;
                            case 5: dmtinTuc.IdCap5 = DMcha.Id; break;
                            case 6: dmtinTuc.IdCap6 = DMcha.Id; break;
                            case 7: dmtinTuc.IdCap7 = DMcha.Id; break;
                        }
                    }
                }
                else
                {
                    dmtinTuc.Cap = 1; // Nếu không có ParentId, đặt cấp là 1
                }

                _context.DMTinTuc.Add(dmtinTuc);
                await _context.SaveChangesAsync();
                CacheHelper.RefreshDanhMucTinTuc(_context);
                return Json(new { success = true, message = "Thêm mới thành công" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Thêm mới thất bại, lỗi: " + ex }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var dmtinTuc = _context.DMTinTuc.Find(id);
            dmtinTuc.TenDmCha = _context.DMTinTuc.FirstOrDefault(x => x.Id == dmtinTuc.ParentId)?.Ten;
            if (dmtinTuc == null)
            {
                return HttpNotFound();
            }
            return PartialView(dmtinTuc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, [Bind(Include = "Id,Ten,MoTa,ParentId,Cap,CreateDate,Status")] DMTinTuc dmtinTuc)
        {
            if (id != dmtinTuc.Id)
            {
                return Json(new { success = false, message = "Id không tồn tại" }, JsonRequestBehavior.AllowGet);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var CapCha = _context.DMTinTuc.AsNoTracking().FirstOrDefault(x => x.Id == dmtinTuc.ParentId)?.Cap ?? 0;
                    if (CapCha >= 0)
                    {
                        dmtinTuc.Cap = CapCha + 1;
                    }
                    dmtinTuc.CreateDate = DateTime.Now;
                    if (dmtinTuc?.ParentId > 0)
                    {
                        var DMcha = _context.DMTinTuc.AsNoTracking().FirstOrDefault(x => x.Id == dmtinTuc.ParentId);
                        if (DMcha != null)
                        {
                            int capCha = DMcha.Cap;
                            dmtinTuc.Cap = capCha + 1;

                            dmtinTuc.IdCap1 = DMcha.IdCap1;
                            dmtinTuc.IdCap2 = DMcha.IdCap2;
                            dmtinTuc.IdCap3 = DMcha.IdCap3;
                            dmtinTuc.IdCap4 = DMcha.IdCap4;
                            dmtinTuc.IdCap5 = DMcha.IdCap5;
                            dmtinTuc.IdCap6 = DMcha.IdCap6;
                            dmtinTuc.IdCap7 = DMcha.IdCap7;

                            switch (capCha)
                            {
                                case 1: dmtinTuc.IdCap1 = DMcha.Id; break;
                                case 2: dmtinTuc.IdCap2 = DMcha.Id; break;
                                case 3: dmtinTuc.IdCap3 = DMcha.Id; break;
                                case 4: dmtinTuc.IdCap4 = DMcha.Id; break;
                                case 5: dmtinTuc.IdCap5 = DMcha.Id; break;
                                case 6: dmtinTuc.IdCap6 = DMcha.Id; break;
                                case 7: dmtinTuc.IdCap7 = DMcha.Id; break;
                            }
                        }
                    }
                    _context.Entry(dmtinTuc).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    CacheHelper.RefreshDanhMucTinTuc(_context);
                    return Json(new { success = true, message = "Sửa thành công" }, JsonRequestBehavior.AllowGet);
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Json(new { success = false, message = "Thêm mới thất bại" }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            var dmtinTuc = await _context.DMTinTuc.FindAsync(id);
            if (dmtinTuc != null)
            {
                _context.DMTinTuc.Remove(dmtinTuc);
            }
            else
            {
                return Json(new { success = false, message = "Danh mục không tồn tại" }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                await _context.SaveChangesAsync();
                CacheHelper.RefreshDanhMucTinTuc(_context);
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
