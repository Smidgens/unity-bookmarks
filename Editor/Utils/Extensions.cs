// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks
{
	using ADB = UnityEditor.AssetDatabase;
	using UnityEditor;
	using System.Threading;
	using System.Text;
	using UnityEngine;

	/// <summary>
	/// Bookmark extensions
	/// </summary>
	internal static class Bookmarks_
	{
		public static string SnakeToPascal(this string str)
		{
			string titleCase = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(str.Replace('_', ' '));
			return titleCase.Replace(" ", string.Empty);
		}

		public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);
		
		// filthy way to format display title for window (for now)
		public static string GetTitleOrDefault(this BKProfile profile)
		{
			var title = profile?.Title;
			if (!title.IsNullOrEmpty()) { return profile.Title; }
			var path = profile.FilePath;
			if (path.IsNullOrEmpty()) { return null; }

			// slice off file extension
			int i = path.Length - BKProfile.FILE_EXT.Length; 

			// find start of file name
			for (; i >= 0 && !(path[i] == '/' || path[i] == '\\'); i--) { }
			var fname = path.Substring(i + 1, path.Length - i - 9); // 9 = .bk.json
			// ideally optimize/simplify this crap (string builder or all-in-one regex)
			return fname.ToLower().SnakeToPascal().ToSentence();
		}
		
		public static string ToSentence(this string s)
		{
			var sb = new StringBuilder();
			for(var i = 0; i < s.Length; i++)
			{
				var c = s[i];
				if(i > 0)
				{
					if (char.IsLower(s[i - 1]) && char.IsUpper(s[i]))
					{
						sb.Append(" ");
					}
				}
				sb.Append(c);
			}
			return sb.ToString();
		}

		public static void Open(this BKAsset a)
		{
			if (a == null) { return; }
			// check if invalid here?
			ADB.OpenAsset(a.Asset);
		}

		public static bool IsSelected(this BKAsset a)
		{
			if (a == null) { return false; }
			// which is cheaper?
			// Selection.assetGUIDs.Contains(a.GUID) vs Selection.Contains(a)
			return Selection.Contains(a.Asset);
		}

		public static void Select(this BKAsset a)
		{
			if( a == null) { return; }
			// can we check selection without loading asset?
			if (Selection.activeObject != a.Asset) { Selection.activeObject = a.Asset; }
		}
		
		public static Rect SliceBottom(this ref Rect r, in float h)
		{
			var r2 = r;
			r2.height = h;
			r.height -= h;
			r2.y += r.height;
			return r2;
		}
		
		public static Rect SliceTop(this ref Rect r, in float h)
		{
			var r2 = r;
			r2.height = h;
			r.height -= h;
			r.position += new Vector2(0f, h);
			return r2;
		}
		
		public static Rect SliceRight(this ref Rect r, in float w)
		{
			var r2 = r;
			r2.width = w;
			r.width -= w;
			r2.x += r.width;
			return r2;
		}
		
		public static Rect SliceLeft(this ref Rect r, in float w)
		{
			var r2 = r;
			r2.width = w;
			r.width -= w;
			r.x += w;
			return r2;
		}


		public static void Resize(this ref Rect r, in float s) => r.Resize(s, s, s, s);

		public static void Resize(this ref Rect r, float lr, in float tb) => r.Resize(lr, lr, tb, tb);

		public static void Resize
		(
			this ref Rect rect,
			in float l,
			in float r,
			in float t,
			in float b
		)
		{
			var c = rect.center;
			rect.width += l + r;
			rect.height += t + b;
			rect.center = c;
		}

	}
}