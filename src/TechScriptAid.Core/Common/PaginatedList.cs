using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.Core.Common
{
    public class PaginatedList<T>
    {
        public List<T> Items { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages { get; }

        public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = count;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            Items = items;
        }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;


        public static PaginatedList<T> Create(
            IEnumerable<T> source, int pageNumber, int pageSize)
        {
            var enumerable = source as List<T> ?? source.ToList();
            var count = enumerable.Count;
            var items = enumerable.Skip((pageNumber - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToList();

            return new PaginatedList<T>(items, count, pageNumber, pageSize);
        }
    }

    public class PaginationParams
    {
        private const int MaxPageSize = 50;
        private int _pageSize = 10;

        public int PageNumber { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public string OrderBy { get; set; }
        public bool OrderDescending { get; set; } = true;
    }

    public class PaginatedResponse<T>
    {
        public List<T> Data { get; set; }
        public PaginationMetadata Pagination { get; set; }
    }

    public class PaginationMetadata
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}
