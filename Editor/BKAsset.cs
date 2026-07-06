// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using Newtonsoft.Json;
	using System;
	using UnityEngine;
	using ADB = UnityEditor.AssetDatabase;
	using Asset = UnityEngine.Object;
	using AP = UnityEditor.AssetPreview;

	/// <summary>
	/// Wrapper for managing asset in bookmarks UI
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	internal sealed class BKAsset : IEquatable<BKAsset>
	{
		public Texture AssetPreview => _assetPreview.Value;
		public string Name => _asset?.name ?? NameFromPath(_assetPath.Value);
		public long Size => _fileSize.Value;
		public string Path => _assetPath.Value;
		public string GUID => _guid;
		public Asset Asset => _getAssetFn.Invoke();
		public Type MainType => _assetType.Value;
		/// <summary>
		/// Asset is invalid - could be invalid GUID
		/// </summary>
		public bool IsInvalid =>  CheckInvalid();

		/// <summary>
		/// Reload cached information like asset paths
		/// (useful to deal with for renames or moves)
		/// </summary>
		public void Reload()
		{
			_assetPath = new(() => ADB.GUIDToAssetPath(_guid));
		}

		public static implicit operator BKAsset(string guid)
		{
			guid = ParseGUID(guid); // guid might be alias
			if (guid.IsNullOrEmpty()) { return null; }
			var bka = new BKAsset();
			bka._guid = guid;
			bka._assetPath = new(() => ADB.GUIDToAssetPath(bka._guid));
			bka._assetType = new(() => ADB.GetMainAssetTypeAtPath(bka.Path));
			bka._fileSize = new(() => GetFileSize(bka.Path));

			bka._assetPreview = new(() =>
			{
				return AP.GetAssetPreview(bka.Asset);
			});

			bka._getAssetFn = bka.LoadAsset;
			return bka;
		}

		// convert to the only safe constant: guid
		public static implicit operator string(BKAsset a) => a.GUID;
		// truthy value depends on asset existing
		public static implicit operator bool(BKAsset a) => !(a == null || a.GUID.IsNullOrEmpty() || a.IsInvalid);

		private static string ParseGUID(string guidOrAlias)
		{
			// TODO: allow mapping of guids to names somewhere for reuse
			return guidOrAlias;
		}

		private BKAsset() { }

		private string _guid = null;

		private Lazy<Texture> _assetPreview = null;
		private Lazy<string> _assetPath = null; // might need some safety checks for this
		private Lazy<Type> _assetType = null;
		private Lazy<long> _fileSize = null;

		// lazy loading for asset
		private Func<Asset> _getAssetFn = null; // lazy load
		private Asset _asset = null; // lazily loaded asset
		private bool _loaded = false; // used to check if asset is null

		private bool CheckInvalid()
		{
			return
			_guid.IsNullOrEmpty()
			|| (_loaded && !_asset) // a load attempt was made
			|| _assetPath == null // 
			|| (_assetPath.IsValueCreated && _assetPath.Value == null);
		}

		private Asset LoadAsset()
		{
			_asset = ADB.LoadMainAssetAtPath(_assetPath.Value);
			_loaded = true;
			_getAssetFn = GetAsset;
			return _asset;
		}

		private Asset GetAsset() => _asset;

		private static string NameFromPath(string path)
		{
			if(path.Length == 0) { return null; }
			var period = path.Length;
			var slash = -1;

			var i = path.Length - 1;

			while(i >= 0)
			{
				var c = path[i];
				if(c == '/')
				{
					slash = i;
					break;
				}
				if(c == '.') { period = i; } // extension
				i--;
			}
			var si = slash + 1;
			return path.Substring(si, period - si);
		}

		public override int GetHashCode() => _guid.GetHashCode();

		bool IEquatable<BKAsset>.Equals(BKAsset other) => _guid == other._guid;

		private static long GetFileSize(string path)
		{
			try { return new System.IO.FileInfo(path).Length; }
			catch { }
			return 0;
		}
	}
}