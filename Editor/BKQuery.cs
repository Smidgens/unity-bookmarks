// smidgens @ github

#pragma warning disable 0414

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Serialization;
	using ADB = UnityEditor.AssetDatabase;

	/// <summary>
	/// Query used to filter assets in DB
	/// https://docs.unity3d.com/ScriptReference/AssetDatabase.FindAssets.html
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	internal sealed class BKQuery
	{
		/// <summary>
		/// Find all asset guids targeted by query
		/// </summary>
		/// <returns></returns>
		public string[] GetGUIDs() => _folders.Length > 0 ? ADB.FindAssets(_filter, _folders) : Array.Empty<string>();

		/// <summary>
		/// Asset filter
		/// </summary>
		[JsonProperty("filter")]
		private string _filter = "";

		/// <summary>
		/// Folders to include in search
		/// </summary>
		[JsonProperty("folders")]
		private string[] _folders = Array.Empty<string>();
	}
}