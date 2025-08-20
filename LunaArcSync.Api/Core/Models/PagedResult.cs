using System.Collections.Generic;

namespace LunaArcSync.Api.Core.Models // 注意这个新的命名空间
{
    public class PagedResult<T>
    {
        public List<T> Items { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages => (int)System.Math.Ceiling(TotalCount / (double)PageSize);

        public PagedResult(List<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}