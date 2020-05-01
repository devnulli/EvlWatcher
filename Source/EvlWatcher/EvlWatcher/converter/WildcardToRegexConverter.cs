using System.Text.RegularExpressions;

namespace EvlWatcher.Converter
{
    class WildcardToRegexConverter
    {
        public static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$";
        }
    }
}
