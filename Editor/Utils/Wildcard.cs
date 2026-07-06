// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks
{
	using System.Text.RegularExpressions;

	/// <summary>
	/// Regex for wildcard pattern
	/// </summary>
	internal class Wildcard : Regex
	{
		public static Regex New(string pattern)
		{
			try
			{
				return new Wildcard(pattern);
			}
			catch
			{
				// ignored
			}

			return null;
		}

		public Wildcard(string pattern) : base(WildcardToRegex(pattern)) { }

		public Wildcard(string pattern, RegexOptions options)
		 : base(WildcardToRegex(pattern), options) { }

		public static bool IsMatch(in string input, in string pattern)
		{
			return Regex.IsMatch(input, WildcardToRegex(pattern));
		}
		public static string WildcardToRegex(string pattern)
		{
			return "^" + Regex.Escape(pattern).
			 Replace("\\*", ".*").
			 Replace("\\?", ".") + "$";
		}
	}

}