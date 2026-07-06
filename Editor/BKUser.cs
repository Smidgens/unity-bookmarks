// smidgens @ github

#pragma warning disable 0414

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using UnityEngine;
	using UnityEditor;
	using System.Linq;
	using System.Collections.Generic;

	/// <summary>
	/// Wrapper around bookmarks fetching, because tracking it in the UI is a cock
	/// </summary>
	[Serializable]
	internal sealed class BKUser
	{
		private const string DEF_TITLE = "Bookmarks";
		private const string DEF_FILE = "ProjectSettings/bookmarks/default.bk.json";

		public event Action viewChanged = null;

		public void AddGUID(string guid)
		{
			Profile?.AddGUID(guid);
		}

		public void AddGUIDs(string[] guids)
		{
			Profile?.AddGUIDs(guids);
		}

		public void RemoveGUID(string guid)
		{
			Profile?.RemoveGUID(guid);
		}

		public bool HasViews => ViewCount > 0;
		public bool HasProfile => Profile != null && !Profile.IsInvalid;
		public string ProfilePath => _filePath;

		private BKProfile Profile => GetProfile();

		// TODO: cache this
		public IEnumerable<BKAsset> Assets => GetAssets();

		public BKView CurrentView => GetCurrentView();

		public IEnumerable<BKView> Views => ViewCount > 0 ? Profile.Views : Enumerable.Empty<BKView>();

		public string View
		{
			get => _viewKey;
			set => SetView(value);
		}

		public int ViewCount => Profile?.Views?.Length ?? 0;

		public void Load(string path)
		{
			if(path == _filePath) { return; } // same file, do nothing
			LoadPath(path);
		}

		public void Reload()
		{
			if(_filePath == null) { return; } // why reload, nothing is set
			LoadPath(_filePath); // load from scratch
		}

		public void Edit()
		{
			if(_filePath == null) { return;  }
			Helpers.OpenProjectFile(_filePath);
		}

		public string GetDisplayTitle()
		{
			// view -> profile -> default
			var title = CurrentView?.Label ?? Profile?.GetTitleOrDefault();
			return !title.IsNullOrEmpty() ? title : DEF_TITLE;
		}
		public Texture GetDisplayIcon()
		{
			var icon = CurrentView?.Icon ?? Profile?.Icon ?? DEFAULT_ICON.Value;
			return icon.image;
		}

		private static readonly Lazy<GUIContent> DEFAULT_ICON = new(() =>
		{
			var path = AssetDatabase.GUIDToAssetPath(Constants.ICON_GUID_BOOKMARK);
			return new GUIContent(AssetDatabase.LoadAssetAtPath<Texture>(path));
		});

		[SerializeField] private string _filePath = DEF_FILE; // path to bookmarks file relative to project folder
		[SerializeField] private string _viewKey = null; // view key, if it exists

		private BKProfile GetProfile()
		{
			// safety check - loading profile might have failed
			var p = _getFn.Invoke(this);
			if(p != null && p.IsInvalid)
			{
				Debug.LogError($"Loading bookmarks file failed: '{p.ProjectPath}'");
				LoadPath(null);
				return null;
			}
			return p;
		}

		private BKProfile _profile = null; // currently loaded profile
		private Func<BKUser, BKProfile> _getFn = InitBK;

		private IEnumerable<BKAsset> GetAssets()
		{
			// NOTE: this is messy af but at least it isn't in the UI anymore
			if(Profile == null) { return Enumerable.Empty<BKAsset>(); }

			var reader = Profile.GetReader();
			var defaultSort = Profile.DefaultSort;
			var currentView = GetCurrentView();

			var r = reader.Read()
			.Where(a => a && currentView != null ? currentView.Contains(a) : true);

			var sort = currentView?.Sort ?? defaultSort;

			var sortBy = sort != null ? sort.SortBy : BKSortBy.Name;
			var desc = sort != null ? sort.Descending : false;

			if (sortBy == BKSortBy.Name)
			{
				if (desc) { r = r.OrderByDescending(x => x.Name); }
				else { r = r.OrderBy(x => x.Name); }
			}
			else if (sortBy == BKSortBy.Name)
			{
				if (desc) { r = r.OrderByDescending(x => x.MainType.Name); }
				else { r = r.OrderBy(x => x.MainType.Name); }
			}
			return r;
		}

		private BKView GetCurrentView()
		{
			var i = IndexOfView(_viewKey);
			return i == -1 ? null : GetView(i);
		}

		private void SetView(string key)
		{
			_viewKey = null;
			if(IndexOfView(key) > -1)
			{
				_viewKey = key; // finally, use key
			}
			viewChanged?.Invoke();
		}

		private BKView GetView(int i) => Profile.Views[i];

		private int IndexOfView(string key)
		{
			for(var i = 0; i < ViewCount; i++)
			{
				var v = GetView(i);
				if(v.Key == key) return i;
			}
			return -1;
		}

		private void LoadPath(string path)
		{
			// reset state
			ResetState();
			_filePath = path;
			viewChanged?.Invoke();
		}

		private void ResetState()
		{
			_viewKey = null;
			_filePath = null;
			_profile = null;
			// reset lazy loading
			_getFn = InitBK;
		}


		// note: getters are static so they can be used to init getter field

		// plain getter, uses loaded value
		private static BKProfile GetBK(BKUser h) => h._profile;

		// init getter, loads profile
		private static BKProfile InitBK(BKUser h) // init getter
		{
			h._profile = BKProfile.LoadFromPath(h._filePath);
			if(h._profile == null) { h._filePath = null; }
			h._getFn = GetBK; // uses loaded value


			h.viewChanged?.Invoke();

			return h._profile;
		}
	}
}