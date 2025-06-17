using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;

namespace TT.Models
{
    public class EnumDmTT
    {
        public enum TrangThaiTinTuc
        {
            DangChoDuyet = 0,
            DaDuyet = 1,
            KhongDuyet = 2,
            DaDang = 3,
        }

        public static string GetName(int value)
        {
            switch (value)
            {
                case (int)TrangThaiTinTuc.DangChoDuyet:
                    return "Đang chờ duyệt";
                case (int)TrangThaiTinTuc.DaDuyet:
                    return "Đã duyệt";
                case (int)TrangThaiTinTuc.KhongDuyet:
                    return "Không duyệt";
                case (int)TrangThaiTinTuc.DaDang:
                    return "Đã đăng";
                default:
                    return "Không xác định";
            }
        }


        public static SelectList GetSelectList()
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = ((int)TrangThaiTinTuc.DangChoDuyet).ToString(), Text = GetName((int)TrangThaiTinTuc.DangChoDuyet) },
                new SelectListItem { Value = ((int)TrangThaiTinTuc.DaDuyet).ToString(), Text = GetName((int)TrangThaiTinTuc.DaDuyet) },
                new SelectListItem { Value = ((int)TrangThaiTinTuc.KhongDuyet).ToString(), Text = GetName((int)TrangThaiTinTuc.KhongDuyet) },
                new SelectListItem { Value = ((int)TrangThaiTinTuc.DaDang).ToString(), Text = GetName((int)TrangThaiTinTuc.DaDang) }
            };
            return new SelectList(items, "Value", "Text");
        }

    }
}