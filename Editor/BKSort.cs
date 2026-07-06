// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using Newtonsoft.Json;

	internal enum BKSortBy { Name, Type }

	/// <summary>
	/// Serialization helper - validate default sort mode
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	internal sealed class BKSort
	{
		public static readonly BKSort Default = new BKSort();
		public delegate int CompareFunction(BKAsset a, BKAsset b);

		public CompareFunction CompareFn => GetSortFn(); // todo: cache this
		public BKSortBy SortBy { get; private set; } = DEFAULT_SORT;
		public bool Descending { get; private set; } = false;

		private const BKSortBy DEFAULT_SORT = BKSortBy.Name;

		private BKSort() { }

		public static implicit operator string(BKSort s)
		{
			var m = s.Descending ? "-" : "";
			return $"{m}{s.SortBy.ToString().ToLower()}";
		}

		public static implicit operator BKSort(string s) => new BKSort(s);

		public BKSort(string sort)
		{
			var (type, desc) = ParseSort(sort);
			SortBy = type;
			Descending = desc;
		}

		private const string S_NAME = "name";
		private const string S_TYPE = "type";

		private static (BKSortBy,bool) ParseSort(string sort)
		{
			if(sort.Length < 2) { return (DEFAULT_SORT, false); }
			var desc = sort[0] == '-';
			var type = DEFAULT_SORT;
			var sortKey = sort.AsSpan(desc ? 1 : 0);
			if (Compare(sortKey, S_NAME)) { type = BKSortBy.Name; }
			else if (Compare(sortKey, S_TYPE)) { type = BKSortBy.Type; }
			return (type, desc);
		}

		private static bool Compare(in ReadOnlySpan<char> s, string other)
		{
			// TODO: compare span better
			if(s.Length != other.Length) { return false; }
			var i = 0;
			foreach(var c in s)
			{
				if(c != other[i]) { return false; }
				i++;
			}
			return true;
		}

		private CompareFunction GetSortFn()
		{
			if(SortBy == BKSortBy.Name) { return SortByName; }
			if(SortBy == BKSortBy.Type) { return SortByType; }
			return SortByName;
		}

		private int SortByName(BKAsset a, BKAsset b)
		{
			SwapIf(ref a, ref b, Descending);
			return a.Name.CompareTo(b);
		}

		private int SortByType(BKAsset a, BKAsset b)
		{
			SwapIf(ref a, ref b, Descending);
			return a.MainType.Name.CompareTo(b.MainType.Name);
		}

		private static void SwapIf<T>(ref T a, ref T b, bool condition)
		{
			if (!condition)
			{
				return;
			}
			(a, b) = (b, a);
		}
	}
}