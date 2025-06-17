using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using TT.Models;

public static class CacheHelper
{
    private static readonly ObjectCache _cache = MemoryCache.Default;
    private static readonly string CacheKey_DMTinTuc = "DMTinTuc";

    private static readonly object _lock = new object(); // Đảm bảo thread-safe

    public static List<DMTinTuc> GetDanhMucTinTuc(DBContext context)
    {
        var cached = _cache[CacheKey_DMTinTuc] as List<DMTinTuc>;
        if (cached != null)
            return cached;

        lock (_lock)
        {
            // Double-check sau khi lock
            cached = _cache[CacheKey_DMTinTuc] as List<DMTinTuc>;
            if (cached != null)
                return cached;

            var data = context.DMTinTuc.AsNoTracking().ToList();
            _cache.Set(CacheKey_DMTinTuc, data, new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.MaxValue
            });

            return data;
        }
    }

    public static void RefreshDanhMucTinTuc(DBContext context)
    {
        // Load mới danh mục
        var newData = context.DMTinTuc.AsNoTracking().ToList();

        // Ghi đè luôn cache cũ bằng cache mới (an toàn)
        _cache.Set(CacheKey_DMTinTuc, newData, new CacheItemPolicy
        {
            AbsoluteExpiration = DateTimeOffset.MaxValue
        });
    }
}
