using BBRAPIModules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BBRModules
{
    [Module("A library to simplify paginating strings, lists, and parameters with a specifiable page size.", "1.0.0")]
    public class PaginatorLib : BattleBitModule
    {
        public int PageNum { get; set; } = 0;
        public List<object> Objects { get; set; }
        public int PageSize { get; set; } = 10;

        public PaginatorLib()
        {
            Objects = new();
        }

        public PaginatorLib(params object[] objects)
        {
            Objects = new(objects);
        }

        public PaginatorLib(IEnumerable<object> objects)
        {
            Objects = new(objects);
        }

        public PaginatorLib AddObject(object obj)
        {
            Objects.Add(obj);
            return this;
        }

        public PaginatorLib AddObjects(params object[] objects)
        {
            foreach (object obj in objects)
            {
                Objects.Add(objects);
            }

            return this;
        }

        public PaginatorLib AddObjects(IEnumerable<object> objects)
        {
            foreach (object obj in objects)
            {
                Objects.Add(objects);
            }

            return this;
        }

        public PaginatorLib Insert(object obj, int index)
        {
            Objects.Insert(index, obj);
            return this;
        }

        public PaginatorLib RemoveObject(object obj)
        {
            Objects.Remove(obj);
            return this;
        }

        public PaginatorLib RemoveObjectAt(int index)
        {
            Objects.RemoveAt(index);
            return this;
        }

        public int GetPageSize() => PageSize;

        public PaginatorLib SetPageSize(int pageSize)
        {
            PageSize = pageSize;
            return this;
        }

        public int CountPages() => (int) Math.Ceiling((double) Objects.Count / PageSize);

        public List<object> GetPage(int pageNum)
        {
            if (pageNum > CountPages())
                return new();
            return Objects.Take(new Range((pageNum * PageSize) - PageSize, pageNum * PageSize)).ToList()!;
        }

        public List<string> GetPageAsStrings(int pageNum)
        {
            if (pageNum > CountPages())
                return new();
            return Objects.Take(new Range((pageNum * PageSize) - PageSize, pageNum * PageSize)).Select(o => o.ToString()).ToList()!;
        }

        public int GetNextPageNumber()
        {
            if (PageNum + 1 > CountPages())
                PageNum = 0;

            PageNum++;
            return PageNum;
        }

        public bool HasNextPage() => !(PageNum + 1 > CountPages());

        public void ResetPageNumber()
        {
            PageNum = 0;
        }
    }
}
