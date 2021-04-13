using System;
using System.Text.RegularExpressions;

namespace LogGrokCore.Search
{
    public struct SearchPattern : IEquatable<SearchPattern>
    {
        public SearchPattern(string searchText, in bool isCaseSensitive, in bool useRegex)
        {
            Pattern = searchText;
            IsCaseSensitive = isCaseSensitive;
            UseRegex = useRegex;
        }

        public string Pattern { get; }
        public bool IsCaseSensitive { get; }
        public bool UseRegex { get; }

        public bool IsEmpty => string.IsNullOrEmpty(Pattern);
        
        public SearchPattern Clone()
        {
            return new(Pattern, IsCaseSensitive, UseRegex);
        }

        public Regex GetRegex(RegexOptions regexAdditionalOptions)
        {
            var regexOptions = IsCaseSensitive ?  RegexOptions.None : RegexOptions.None | RegexOptions.IgnoreCase;
            var pattern = UseRegex ? Pattern : Regex.Escape(Pattern);
            return new Regex(pattern, regexOptions | regexAdditionalOptions);
        }

        public bool Equals(SearchPattern other)
        {
            return Pattern == other.Pattern && IsCaseSensitive == other.IsCaseSensitive && UseRegex == other.UseRegex;
        }

        public override bool Equals(object? obj)
        {
            return obj is SearchPattern other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Pattern, IsCaseSensitive, UseRegex);
        }

        public override string ToString() => 
            IsEmpty ? "{Empty}" : $"{Pattern}, useRegex: {UseRegex}, caseSensitive: {IsCaseSensitive}";
    }
}