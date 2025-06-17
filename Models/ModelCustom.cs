
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TT.Models
{
    public partial class  DMTinTuc
    {
        public string TenDmCha;
        public int CountChild = 0;
    }

    public partial class TinTuc
    {
        [NotMapped]
        public string TenDm { get; set; }

        [NotMapped]
        public List<int> lstIdDm { get; set; }  = null;
    }
    public class TinTucDto
    {
        public int Id { get; set; }
        public string TieuDe { get; set; }
        public string TrichNgan { get; set; }
        public string ChiTiet { get; set; }
        public DateTime CreateDate { get; set; }
        public int Status { get; set; }
        public string TenDm { get; set; } // thêm mới
    }


}
