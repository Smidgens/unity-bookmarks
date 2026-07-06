// smidgens @ github

#pragma warning disable 0414

namespace Smidgenomics.Unity.Bookmarks
{
	using Newtonsoft.Json;
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.IO;
	using UnityEngine;
	using ADB = UnityEditor.AssetDatabase;

	/// <summary>
	/// Read
	/// </summary>
	internal interface IBKReader
	{
		public IEnumerable<BKAsset> Read();
	}

	/// <summary>
	/// Bookmarks profile
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	internal sealed class BKProfile
	{
		public const string FILE_EXT = "bk.json";
		public const string FILE_PATTERN = "*." + FILE_EXT;

		public bool IsInvalid => _deleted;

		public static IBKReader GetDefaultReader() => new EmptyReader();

		public void AddGUID(string guid)
		{
			if (Include.AddGUID(guid))
			{
				Save();
			}
		}

		public void AddGUIDs(IEnumerable<string> guids)
		{
			if (Include.AddGUIDs(guids))
			{
				Save();
			}
		}

		public void RemoveGUID(string guid)
		{
			if (Include.RemoveGUID(guid))
			{
				Save();
			}
		}

		public BKProfile()
		{
			_icon = new(() =>
			{
				if (_iconGUID.IsNullOrEmpty())
				{
					return null;
				}
				// TODO: allow alias
				var path = ADB.GUIDToAssetPath(_iconGUID);
				var ico = ADB.LoadAssetAtPath<Texture>(path);
				return ico ? new GUIContent(ico) : null;
			});

			_sort = new(() => _sortKey);
		}

		public static BKProfile LoadFromPath(string path)
		{
			if (!File.Exists(path)) { return null; }

			var json = File.ReadAllText(path);
			var settings = JsonConvert.DeserializeObject<BKProfile>(json);
			var root = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
			settings.FilePath = $"{root}/{path}";
			settings.ProjectPath = path;
			return settings;
		}

		/// <summary>
		/// Get list of bookmark profiles in project folder
		/// </summary>
		public static IEnumerable<string> FindProfiles()
		{
			List<string> paths = new();
			FindBookmarks("ProjectSettings/bookmarks", paths);
			FindBookmarks("UserSettings/bookmarks", paths);

			return paths.OrderByDescending(x =>
			{
				var c = x.Count(c => c == '/');
				return c;
			});
		}

		private static void FindBookmarks(string dir, List<string> paths)
		{
			var files = Helpers.ListFiles(dir, FILE_PATTERN, true);
			foreach (var uf in files)
			{
				paths.Add(uf.Replace(@"\", "/"));
			}
		}

		/// <summary>
		/// Write object to file
		/// </summary>
		public void Save()
		{
			try
			{
				var json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
				{
					Formatting = Formatting.Indented, // no idea how to get tabs to work
				});
				File.WriteAllText(ProjectPath, json);
			}
			catch(Exception e)
			{
				Debug.LogError(e.Message);
			}
		}

		/// <summary>
		/// Relative path from project folder
		/// </summary>
		public string ProjectPath { get; private set; }

		/// <summary>
		/// Absolute path to file
		/// </summary>
		public string FilePath { get; private set; }


		[JsonProperty("defaultSort")]
		private string _sortKey = "name";

		//[JsonProperty("defaultSort")]
		public BKSort DefaultSort => _sort.Value;


		/// <summary>
		/// Display title
		/// </summary>
		[JsonProperty("title")]
		public string Title { get; private set; }
		/// <summary>
		/// Filter profiles
		/// </summary>
		[JsonProperty("views")]
		public BKView[] Views { get; private set; } = new BKView[0];
		/// <summary>
		/// Assets to include
		///// </summary>
		[JsonProperty("include")]
		public BKInclude Include { get; private set; } = new BKInclude();

		/// <summary>
		/// GUID of display icon for view
		/// </summary>
		[JsonProperty("icon")]
		private string _iconGUID = null;

		public GUIContent Icon => _icon?.Value;

		private Lazy<GUIContent> _icon = null;
		private Lazy<BKSort> _sort = null;

		public IBKReader GetReader()
		{
			if(_reader == null || !_reader.IsValid)
			{
				_reader = new ReadHandle
				{
					getFn = GetAllAssets,
					dateFn = GetLastFileEdit,
					reloadFn = Reload
				};
			}
			return _reader;
		}

		private ReadHandle _reader = null;

		private DateTime _lastWrite = default;

		private bool _deleted = false;

		private void Reload()
		{
			var lastEdit = GetLastFileEdit();
			// nothing to reload
			if (lastEdit == _lastWrite) { return; }
			_lastWrite = lastEdit;
			try
			{
				var json = File.ReadAllText(FilePath);
				JsonConvert.PopulateObject(json, this);
			}
			catch
			{
				_deleted = true;
			}
		}

		private IEnumerable<BKAsset> GetAllAssets()
		{
			return Include.FindAssets();
		}

		private DateTime GetLastFileEdit()
		{
			return File.GetLastWriteTime(FilePath);
		}

		private struct EmptyReader : IBKReader
		{
			public bool IsValid => true;
			public IEnumerable<BKAsset> Read() => Enumerable.Empty<BKAsset>();
		}

		private class ReadHandle : IBKReader
		{
			public bool IsValid => true;

			public Action reloadFn = null;
			public Func<DateTime> dateFn = null;
			public Func<IEnumerable<BKAsset>> getFn = null;

			private DateTime _lastDate = default;
			private IEnumerable<BKAsset> _cachedAssets = Enumerable.Empty<BKAsset>();

			public IEnumerable<BKAsset> Read()
			{
				var latestDate = dateFn.Invoke();
				if(latestDate != _lastDate)
				{
					_lastDate = latestDate;
					reloadFn.Invoke();
					_cachedAssets = getFn.Invoke();
				}
				return _cachedAssets;
			}
		}

	}
}