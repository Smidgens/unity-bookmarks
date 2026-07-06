// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Serialization;

	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	internal sealed class BKInclude
	{
		public bool AddGUID(string guid)
		{
			if (guid.IsNullOrEmpty()) { return false; }
			return _guids.Add(guid);
		}

		public bool AddGUIDs(IEnumerable<string> guids)
		{
			var changed = false;
			foreach(var g in guids)
			{
				changed |= AddGUID(g);
			}
			return changed;
		}
		public bool RemoveGUID(string guid)
		{
			return _guids.Remove(guid);
		}

		/// <summary>
		/// Find all asset guids
		/// </summary>
		/// <returns></returns>
		public BKAsset[] FindAssets()
		{
			HashSet<BKAsset> assets = new();

			foreach(var guid in _guids)
			{
				BKAsset a = guid;
				if(a == null) { continue; }
				assets.Add(a);
			}
			foreach(var q in _find)
			{
				foreach(var guid in q.GetGUIDs())
				{
					assets.Add(guid);
				}
			}
			return assets.ToArray();
		}

		/// <summary>
		/// Explicit asset GUIDs to include in bookmarks
		/// </summary>
		[JsonProperty("guids")]
		private HashSet<string> _guids = new();

		/// <summary>
		/// Asset find queries
		/// </summary>
		[JsonProperty("find")]
		private BKQuery[] _find = Array.Empty<BKQuery>();
	}
}