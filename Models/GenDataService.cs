using Hangfire;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace TT.Models
{
    public class GenDataService
    {
        private readonly DBContext _context;

        public GenDataService()
        {
            _context = new DBContext();
        }

        [AutomaticRetry(Attempts = 0)]
        public void NoRetryJob()
        {
            throw new Exception("Thử lỗi");
        }
        public void GenDMDaCap(int cap = 8, int DmCap = 5000)
        {
            _context.Database.CommandTimeout = 0;
            _context.Database.ExecuteSqlCommand("EXEC sp_TaoDMDaCap @cap, @DmCap",
                new SqlParameter("@cap",cap),
                new SqlParameter("@DmCap",DmCap));
        }

        public void GenTinTuc(int sotintuc = 4000000)
        {
            _context.Database.CommandTimeout = 0;
            _context.Database.ExecuteSqlCommand("EXEC sp_InsertFakeTinTuc @sotintuc",
                new SqlParameter("@sotintuc", sotintuc));
        }

        public void GanDMTheoLo(int start, int end)
        {
            _context.Database.CommandTimeout = 0;
            _context.Database.ExecuteSqlCommand("EXEC sp_GanRandomDMTinTuc_SetBased @StartId, @EndId",
                new SqlParameter("@StartId", start),
                new SqlParameter("@EndId", end));
        }
    }


}