using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mihari
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string[] keywords)
        {
            return keywords.Any(_ => source.Contains(_));
        }

        public static bool WildcardMatch(this string source, string pattern, bool ignoreCase = true)
        {
            pattern = Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".");

            return source.RegexMatch(pattern);
        }

        public static bool RegexMatch(this string source, string pattern, bool ignoreCase = true)
        {
            var r = ignoreCase
                ? new Regex(pattern, RegexOptions.IgnoreCase)
                : new Regex(pattern);

            return r.IsMatch(source);
        }
    }
}
