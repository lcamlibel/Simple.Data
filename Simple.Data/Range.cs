using System;

namespace Simple.Data
{
    public static class Range
    {
        public static Range<T> To<T>(this T start, T end)
            where T : IComparable<T>
        {
            return new Range<T>(start, end);
        }

        public static IRange To(this string start, string end)
        {
            DateTime startDate, endDate;
            if (DateTime.TryParse(start, out startDate) && DateTime.TryParse(end, out endDate))
            {
                return new Range<DateTime>(startDate, endDate);
            }

            return new Range<string>(start, end);
        }
    }
}