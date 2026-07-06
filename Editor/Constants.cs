// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using UnityEngine;

	internal static class Constants
	{
		// type to dock bookmarks window next to
		public static readonly Type PROJECT_WIN_TYPE =
		Type.GetType("UnityEditor.ProjectBrowser, UnityEditor.CoreModule");

		public const string BKWIN_TITLE = "Bookmarks";

		public const string
		ICON_GUID_FILTER = "ea6406ccac24f36498d942421e24a2e4",
		ICON_GUID_FILTER_OFF = "7c7a8dc519803f648806eedd76184ee9",
		ICON_GUID_SORT = "df4723edece84364db77e80984c4e199",
		ICON_GUID_SAVE = "f71578ff255cd6d409eebb86d75abed0",
		ICON_GUID_BLENDER = "94a0ed9242490bb449553ef8ee4555ba",
		ICON_GUID_MISSING = "1e5aae4eb6b424c4a929ede16fefbc04",
		ICON_GUID_BOOKMARK = "87c90638610bc394683a9d172e419689";

		public static readonly Color SELECT_COLOR = new Color(0.4288596f, 0.7415035f, 0.9811321f) * 0.7f;

	}
}