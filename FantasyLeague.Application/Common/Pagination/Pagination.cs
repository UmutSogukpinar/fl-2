using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Application.Common.Pagination;

internal static class Pagination
{
    public static int CalculateTotalPage(this int totalCount, int pageSize)
    {
        return (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}
