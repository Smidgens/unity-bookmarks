// smidgens @ github

#pragma warning disable 0414

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using Newtonsoft.Json;
	using System.Text.RegularExpressions;
	using System;
	using System.Linq;
	using UnityEngine;
	using ADB = UnityEditor.AssetDatabase;
	using UnityEditor.Experimental.GraphView;

	/// <summary>
	/// Filters out assets covered by bookmarks profile
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]	
	internal sealed class BKView
	{
		/// <summary>
		/// Optional display icon
		/// </summary>
		public GUIContent Icon => _icon.Value;

		//[JsonProperty("sort")]
		//public BKSort Sort { get; private set; } = BKSort.Default;

		/// <summary>
		/// Filter ID, used to persist settings
		/// </summary>
		public string Key => _id ?? Label;
		/// <summary>
		/// Display name
		/// </summary>
		[JsonProperty("label")]
		public string Label { get; private set; }

		/// <summary>
		/// ID of view to use
		/// </summary>
		[JsonProperty("id")]
		private string _id = null;
		
		/// <summary>
		/// GUID of display icon for view
		/// </summary>
		[JsonProperty("icon")]
		private string _iconGUID = null;

		/// <summary>
		/// Base types to include
		/// </summary>
		[JsonProperty("types")]
		private string[] _typePaths = new string[0];

		/// <summary>
		/// Path wildcards
		/// </summary>
		[JsonProperty("paths")]
		private string[] _pathPatterns = new string[0];

		// lazily loaded icon asset
		private Lazy<GUIContent> _icon = null;
		private Lazy<Type[]> _types = null;
		private Lazy<Regex[]> _paths = null;

		[JsonProperty("sort")]
		private string _sortKey = "name";

		//[JsonProperty("defaultSort")]
		public BKSort Sort => _sort.Value;

		private Lazy<BKSort> _sort = null;

		public BKView()
		{
			_icon = new(() =>
			{
				if (string.IsNullOrEmpty(_iconGUID)) { return null; }
				var p = ADB.GUIDToAssetPath(_iconGUID);
				if (p == null) { return null; }
				var img = ADB.LoadAssetAtPath<Texture2D>(p);
				return new GUIContent(img);
			});

			_types = new(() =>
			{
				return
				_typePaths.Select(tp => Type.GetType(tp))
				.Where(t => t != null)
				.ToArray();
			});

			_paths = new(() =>
			{
				return
				_pathPatterns.Select(pattern => Wildcard.New(pattern))
				.Where(p => p!= null)
				.ToArray();
			});

			_sort = new(() => _sortKey);
		}

		public bool Contains(BKAsset asset)
		{
			if (!MatchPath(asset.Path)) { return false; }
			if (!MatchType(asset.MainType)) { return false; }
			return true;
		}

		private bool MatchPath(string path)
		{
			if(_paths.Value.Length == 0) { return true; }
			return _paths.Value.FirstOrDefault(r => r.IsMatch(path)) != null;
		}

		private bool MatchType(Type assetType)
		{
			if(_types.Value.Length == 0) { return true; }
			return _types.Value.FirstOrDefault(t => t.IsAssignableFrom(assetType)) != null;
		}

	}
}