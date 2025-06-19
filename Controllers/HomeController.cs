
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using TT.Models;
namespace TT.Controllers
{
    public class HomeController : Controller
    {
        private readonly DBContext _context;

        public HomeController()
        {
            _context = new DBContext();
        }

        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> ListdataTinTuc(string keysearch = "", int IdDm = 0, int page = 1, int pagesize = 5)
        {
            List<TinTuc> list = null;
            var total = 0;

            try
            {
                var listdata = _context.TinTuc
                    .Include(x => x.DMTinTuc)
                    .AsNoTracking()
                    .AsQueryable();

                if (IdDm > 0)
                {
                    var dm = _context.DMTinTuc.FirstOrDefault(x => x.Id == IdDm);
                    if (dm != null)
                    {
                        listdata = listdata.Where(t => t.DMTinTuc.Any(d => d.Id == dm.Id || d.IdCap1 == dm.Id || d.IdCap2 == dm.Id || d.IdCap3 == dm.Id || d.IdCap4 == dm.Id || d.IdCap5 == dm.Id || d.IdCap6 == dm.Id || d.IdCap7 == dm.Id));
                    }
                }

                if (!string.IsNullOrWhiteSpace(keysearch))
                {
                    var keyword = keysearch.Trim().ToLowerInvariant();
                    listdata = listdata.Where(x => x.TieuDe.ToLower().Contains(keyword));
                }

                total = await listdata.CountAsync();

                list = await listdata
                    .OrderByDescending(x => x.Id)
                    .Skip((page - 1) * pagesize)
                    .Take(pagesize)
                    .ToListAsync();

                foreach (var item in list)
                {
                    item.TenDm = string.Join(", ", item.DMTinTuc.Select(d => d.Ten));
                }

                ViewBag.curentpage = page;
                ViewBag.curentpagesize = pagesize;
                ViewBag.total = total;
                ViewBag.totalPage = Math.Ceiling((double)total / pagesize);
                ViewBag.stt = (page - 1) * pagesize;
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }

            return PartialView(list);
        }
        public async Task<ActionResult> ListDm(string keysearch,int indexdm = 0)
        {
            var DMTT = _context.DMTinTuc.AsQueryable().AsNoTracking();
            List<DMTinTuc> listdata = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(keysearch))
                {
                    var keyword = keysearch.Trim().ToLowerInvariant();
                    DMTT = DMTT.Where(x => x.Ten.ToLower().Contains(keyword));
                }
                else
                {
                    DMTT = DMTT.Where(x => x.Cap == 1);
                }
                listdata = await DMTT.OrderBy(x => x.Cap).Skip(indexdm).Take(50).ToListAsync();

                foreach (var item in listdata)
                {
                    // Kiểm tra xem có danh mục con không (bất kỳ cấp con nào trỏ về Id hiện tại)
                    item.CountChild = _context.DMTinTuc.Any(dm =>
                        dm.IdCap1 == item.Id ||
                        dm.IdCap2 == item.Id ||
                        dm.IdCap3 == item.Id ||
                        dm.IdCap4 == item.Id ||
                        dm.IdCap5 == item.Id ||
                        dm.IdCap6 == item.Id ||
                        dm.IdCap7 == item.Id
                    ) ? 1 : 0;
                    var query= _context.TinTuc
                    .Include(x => x.DMTinTuc)
                    .AsNoTracking()
                    .Where(t => t.DMTinTuc.Any(d => d.Id == item.Id || d.IdCap1 == item.Id || d.IdCap2 == item.Id || d.IdCap3 == item.Id || d.IdCap4 == item.Id || d.IdCap5 == item.Id || d.IdCap6 == item.Id || d.IdCap7 == item.Id));

                    item.SoLuongTinTuc = await query.CountAsync();
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
            return PartialView(listdata);
        }

        public async Task<ActionResult> ListDmChild(int IdDm = 0)
        {
            // 1. Lấy danh sách danh mục con
            var listdm = _context.DMTinTuc
                .AsNoTracking()
                .Where(x => x.ParentId == IdDm)
                .ToList();

            // 2. Với mỗi item, đếm xem có con cháu hay không
            foreach (var item in listdm)
            {
                int id = item.Id;

                // Đếm các bản ghi nào có item.Id là tổ tiên trong 7 cấp
                bool coConChau = _context.DMTinTuc.Any(dm =>
                    dm.IdCap1 == id || dm.IdCap2 == id || dm.IdCap3 == id ||
                    dm.IdCap4 == id || dm.IdCap5 == id || dm.IdCap6 == id || dm.IdCap7 == id
                );

                item.CountChild = coConChau ? 1 : 0;

                var query = _context.TinTuc
                    .Include(x => x.DMTinTuc)
                    .AsNoTracking()
                    .Where(t => t.DMTinTuc.Any(d => d.Id == item.Id || d.IdCap1 == item.Id || d.IdCap2 == item.Id || d.IdCap3 == item.Id || d.IdCap4 == item.Id || d.IdCap5 == item.Id || d.IdCap6 == item.Id || d.IdCap7 == item.Id));

                item.SoLuongTinTuc = await query.CountAsync();
            }

            return PartialView("~/Views/Home/ListDm.cshtml", listdm); // ✔ dùng đúng listdm
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
        //public List<DmtinTuc> GetAllChil(List<DmtinTuc> list, int index = 0)
        //{
        //    if (index >= list.Count)
        //    {
        //        return list;
        //    }

        //    var children = _context.DmtinTuc
        //        .AsNoTracking()
        //        .Where(x => x.ParentId == list[index].Id)
        //        .ToList();

        //    list.InsertRange(index + 1, children);

        //    return GetAllChil(list, index + 1);
        //}
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
