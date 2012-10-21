using System.Text;

namespace Simple.Data.Extensions
{
    public static class DynamicStringExtensions
    {
        public static string ToSnakeCase(this string source)
        {
            var builder = new StringBuilder();
            var length = source.Length;
            for (int index = 0; index < length; index++)
            {
                char c = source[index];
                if (char.IsUpper(c))
                {
                    if (builder.Length > 0) builder.Append('_');
                }
                builder.Append(char.ToLower(c));
            }

            return builder.ToString();
        }
    }
}