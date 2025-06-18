
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TT.Models
{
    public class GenerationProgress
    {
        public string SessionId { get; set; }
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public string Message { get; set; }
    }
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
