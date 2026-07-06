// smidgens @ github

#pragma warning disable 0414

namespace Smidgenomics.Unity.Bookmarks
{
	using UnityEngine;
	using UnityEditor;
	using System;
	using System.Linq;
	using Asset = UnityEngine.Object;
	using ADB = UnityEditor.AssetDatabase;
	using System.Collections.Generic;

	// TODO: refactor this mess - move to UIElements to minimize gui code

	internal sealed class BKWindow : EditorWindow
	{
		private enum WindowSize { XS, SM, MD, LG, XL } // responsive size type

		[MenuItem("Window/Bookmarks", false, -40)]
		public static void Open() => CreateWindow<BKWindow>(DOCK).Show();

		// dock next to
		private readonly static Type[] DOCK = { Constants.PROJECT_WIN_TYPE, };

		private const string NO_ASSETS_MSG = "Awfully empty in here...";
		private const string NO_BOOKMARKS_MSG = "No bookmarks loaded";
		private const float DRAG_THRESHOLD = 20f;
		private const float MIN_ZOOM = 1.1f;
		private const float PREVIEW_ZOOM = 1.3f;
		private static bool SHOW_FOOTER = false;

		// breakpoints for responsive size (sm,md,lg)
		private static readonly (float,float,float,float) VP_BREAKPOINTS = (300f, 600f, 1000f, 2000f);

		[Flags]
		internal enum DisplayOption { Type = 1, Size = 2, GUID = 4, All = 1|2|4, }

		// currently loaded bookmarks path
		[SerializeField] private Vector2 _browserScroll = default;
		[SerializeField] private DisplayOption _displayFlags = 0;
		[SerializeField] private int _zoom = 1;
		[SerializeField] private float _scale = MIN_ZOOM;
		[SerializeField] private BKUser _bkUser = new BKUser();

		private (Asset, string) _lastSelectedObject = default;
		private Vector2 _dragPos = default;
		private int _repaintFor = 0;
		private string _rclicked = null; // hack, fix later
		private bool _hasSelection = false;
		private string _lastProfile = null;

		// misc utility events
		private event Action _onAfterGUI = null; // called after gui then cleared

		private static Color SeparatorColor => EditorGUIUtility.isProSkin
		? Color.black * 0.8f
		: Color.black* 0.3f;

		private float ZScale => _scale;
		
		/// <summary>
		/// Viewport size
		/// </summary>
		private WindowSize WSize
		{
			get
			{
				var w = Screen.width;
				var (xs, sm, md, lg) = VP_BREAKPOINTS;
				if(w < xs) { return WindowSize.XS; }
				if(w < sm) { return WindowSize.SM; }
				if(w < md) { return WindowSize.MD; }
				if(w < lg) { return WindowSize.LG; }
				return WindowSize.XL;
			}
		}
		private bool ShowAssetPreviews => ZScale >= PREVIEW_ZOOM;
		private bool IsSmall => WSize == WindowSize.SM || WSize == WindowSize.XS;

		private void OnEnable()
		{
			_bkUser.viewChanged += OnViewChanged;
		}

		private void OnDisable()
		{
			_bkUser.viewChanged -= OnViewChanged;
		}

		private void OnGUI()
		{
			CheckWindowTab(); // why tf do I need this, are callbacks being ignored?

			_hasSelection = false;
			var wrect = new Rect(0f, 0f, Screen.width, Screen.height); ;

			DrawToolbar();

			// bookmarks profile is loaded
			EditorGUILayout.BeginHorizontal();
			{
				// browser tab
				EditorGUILayout.BeginVertical();
				ScrollColumn(ref _browserScroll, DrawBrowser);
				DrawBrowserFooter();
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndHorizontal();

			if (_onAfterGUI != null)
			{
				try { _onAfterGUI.Invoke(); }
				catch(Exception e) { Debug.LogError(e.Message); }
				_onAfterGUI = null;
				Repaint();
			}
			if (NeedsRepaint()) { Repaint(); }
		}

		private void OnSelectionChange() => Repaint();

		private void OnLostFocus() => Repaint();

		private bool NeedsRepaint()
		{
			if (_repaintFor > 0) // this is kinda dumb, should remove later
			{
				_repaintFor--;
				return true;
			}

			// this doesn't really work outside coroutine
			if (ShowAssetPreviews && AssetPreview.IsLoadingAssetPreviews()) { return true; }

			return false;
		}

		private void OnViewChanged()
		{
			RefreshTab();
			Repaint();
		}

		private void RefreshTab()
		{
			// apparently re-using the old instance makes it bug out, ffs
			// unity must be caching this internally and assuming you'll only set it in OnEnable
			// future fix:
			// 1) make BKUser provide this value
			// 2) maybe setting title content to null and back is enough to force a refresh
			titleContent = new GUIContent(_bkUser.GetDisplayTitle(), _bkUser.GetDisplayIcon());
			Repaint();
		}

		private IEnumerable<(GUIContent, string)> GetProfileOptions()
		{
			return BKProfile.FindProfiles()
			.Select(path =>
			{
				var label =
				path.Replace("Settings", "") // clean me up, jesus christ...
				.Replace("/bookmarks", "")
				.Replace(".bk.json", "")
				.ToLower();
				label = label.ToLower().SnakeToPascal().ToSentence();
				return (new GUIContent(label), path);
			});
		}

		private void Repaint(int forFrames) => Mathf.Clamp(forFrames, 1, 100);

		private void CheckWindowTab()
		{
			var cprofile = _bkUser?.ProfilePath;

			if(_lastProfile != cprofile)
			{
				_lastProfile = cprofile;
				OnViewChanged();
			}
		}


		// reload -> fix potential issues
		private void Reload()
		{
			_bkUser.Reload();
			RefreshTab();
		}

		private void DrawBrowserFooter()
		{
			if (!_bkUser.HasProfile) { return; }
			if (!SHOW_FOOTER) { return; }
			if (!_hasSelection) { return; }

			var selection = Selection.activeObject;
			var (lastObject, lastObjectPath) = _lastSelectedObject;

			if (selection != lastObject)
			{
				_lastSelectedObject = default;
			}
			if(selection != null)
			{
				var cpath = ADB.GetAssetPath(selection);
				_lastSelectedObject = (selection, cpath);
				lastObjectPath = cpath;
			}

			const float SEP_H = 1f;

			EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorStyles.toolbar.fixedHeight + SEP_H));
			GUILayout.Label("");
			EditorGUILayout.EndHorizontal();

			var footerRect = GUILayoutUtility.GetLastRect();

			// draw toolbar bg
			EditorGUI.DrawRect(footerRect.SliceTop(SEP_H), SeparatorColor);
			GUI.Box(footerRect, GUIContent.none, EditorStyles.toolbar);

			var contentRect = footerRect;
			contentRect.width -= 4f; // l/r padding
			contentRect.height = EditorGUIUtility.singleLineHeight - 2f;
			contentRect.center = footerRect.center;

			if (selection && !lastObjectPath.IsNullOrEmpty())
			{
				contentRect.SliceRight(4f);
				WStyle.Label(contentRect, lastObjectPath.Substring(7), 0.8f, TextAnchor.MiddleLeft);
			}
		}

		private GenericMenu GetAssetContextMenu(BKAsset a)
		{
			var m = new GenericMenu();
			m.AddItem(new GUIContent("Copy/GUID"), false, () => GUIUtility.systemCopyBuffer = a.GUID);
			m.AddItem(new GUIContent("Copy/Path"), false, () => GUIUtility.systemCopyBuffer = a.Path);
			m.AddItem(new GUIContent("Copy/Type"), false, () => GUIUtility.systemCopyBuffer = a.MainType.Name);
			m.AddSeparator("");
			m.AddItem(new GUIContent("Remove"), false, () =>
			{
				_bkUser.RemoveGUID(a.GUID);
			});
			return m;
		}

		private void DrawBrowser()
		{
			if (!_bkUser.HasProfile)
			{
				GUILayout.Box(NO_BOOKMARKS_MSG);
				return;
			}

			var hasErrors = false;
			try
			{
				DrawAssetList();
				HandleDroppedOnWindow();
			}
			catch { hasErrors = true; }

			if (hasErrors)
			{
				Debug.Log("<color=yellow>Exception occurred while drawing bookmarks list: reloading</color>");
				Reload();
			}
		}

		private void DrawAssetList()
		{
			var i = 0;
			foreach(var a in _bkUser.Assets)
			{
				// REFACTOR: remove try catch, track down the potential null error on the asset side and fix it
				if (!a) { continue; } // might have been deleted

				_hasSelection |= a.IsSelected();

				var height = EditorGUIUtility.singleLineHeight * ZScale;
				var pos = EditorGUILayout.GetControlRect(false, GUILayout.Height(height));
				DrawAssetListItem(a, pos);
				i++;
			}
			if(i == 0) { GUILayout.Label(NO_ASSETS_MSG); }
		}

		private void DrawAssetListItem(BKAsset a, Rect pos)
		{
			var area = pos;

			var rclicked = HasClick(area, 1, 1);

			if (_rclicked == a.GUID)
			{
				_rclicked = null;
				GetAssetContextMenu(a).ShowAsContext();
			}

			if (a.IsSelected() || rclicked) { EditorGUI.DrawRect(area, Constants.SELECT_COLOR); }

			if (rclicked)
			{
				Repaint(10);
				_rclicked = a.GUID;
				a.Select();
				Event.current.Use();
			}

			var icon = GetMiniAssetIcon(a);

			var icoRect = pos.SliceLeft(pos.height);
			icoRect.Resize(-1f);
			pos.SliceLeft(2f);

			GUI.DrawTexture(icoRect, icon); // draw icon

			pos.SliceLeft(1f * ZScale);

			WStyle.Label(pos, a.Name, 0.8f * ZScale, TextAnchor.MiddleLeft);

			if (!IsSmall)
			{
				if (_displayFlags.HasFlag(DisplayOption.Size))
				{
					var sizeRect = pos.SliceRight(40f);
					var fsize = a.Size;
					var slabel = fsize > 0 ? Helpers.GetFileSizeLabel(fsize) : "-";
					WStyle.Label(sizeRect, slabel, 0.7f, TextAnchor.MiddleRight);
				}

				if (_displayFlags.HasFlag(DisplayOption.Type))
				{
					WStyle.Label(pos.SliceRight(70f), a.MainType.Name, 0.7f, TextAnchor.MiddleRight);
				}

				if (_displayFlags.HasFlag(DisplayOption.GUID))
				{
					WStyle.Label(pos.SliceRight(180f), a.GUID, 0.7f, TextAnchor.MiddleRight);
				}
			}

			if (HasClick(area, 0, 2))
			{
				EditorApplication.delayCall += () => a.Open();
				Event.current.Use();
			}

			if(Event.current.type == EventType.DragUpdated)
			{
				var dragDist = Vector2.Distance(_dragPos, Event.current.mousePosition);
				if(dragDist >= DRAG_THRESHOLD)
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					Event.current.Use();
				}
			}

			var isDrag =
			Event.current != null
			&& area.Contains(Event.current.mousePosition)
			&& Event.current.button == 0
			&& Event.current.type == EventType.MouseDrag;

			if (isDrag)
			{
				_dragPos = Event.current.mousePosition;
				DragAndDrop.PrepareStartDrag();
				DragAndDrop.StartDrag("Dragging");
				DragAndDrop.objectReferences = new Asset[] { a.Asset };
				DragAndDrop.visualMode = DragAndDropVisualMode.None;
				Event.current.Use();
			}

			if (!isDrag && HasClick(area, 0, 1, true))
			{
				a.Select();
				Event.current.Use();
			}
		}

		private void HandleDroppedOnWindow()
		{
			// allow drop if assets are being dragged
			var valid = DragAndDrop.paths.Length > 0;

			// show visual indicator of 
			if(Event.current.type == EventType.DragUpdated)
			{
				DragAndDrop.visualMode = valid
				? DragAndDropVisualMode.Copy
				: DragAndDropVisualMode.Rejected;
			}

			var canceled = Event.current.keyCode == KeyCode.Escape;

			if (Event.current.type == EventType.DragPerform && valid)
			{
				// NOTE: this might not be safe this way
				var guids = DragAndDrop.paths.Select(p => ADB.AssetPathToGUID(p)).ToArray();
				_onAfterGUI += () =>
				{
					_bkUser.AddGUIDs(guids);
				};
				Event.current.Use();
			}
		}

		private void DrawToolbar()
		{
			// TODO: bless this mess...to hell
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

			IconButton(WIcon.FLOPPY, ShowBookmarksContext, WIcon.ICON_COLOR * WIcon.DEF_OPACITY, true);

			if (_bkUser.HasViews)
			{
				//var filtering = CurrentView != null;
				var filtering = _bkUser.View != null;

				var filterIcon = filtering ? WIcon.FILTER : WIcon.FILTER_UNCHECKED;
				var c = WIcon.ICON_COLOR * (filtering ? 1f : WIcon.DEF_OPACITY);
				IconButton(filterIcon, ShowViewContext, c, true);
			}

			GUILayout.FlexibleSpace();

			if (!IsSmall)
			{
				IconButton(WIcon.SORT, ShowColumnPicker, WIcon.ICON_COLOR, true);
			}
			EditorGUILayout.EndHorizontal();
		}

		private void ShowViewContext()
		{
			var m = new GenericMenu();
			var activeView = _bkUser.CurrentView;
			m.AddItem(new GUIContent("All"), activeView == null, () =>
			{
				_bkUser.View = null;
			});
			var i = 0;

			foreach(var view in _bkUser.Views)
			{
				if(i == 0) { m.AddSeparator(""); }
				var v = view;
				var key = v.Key;

				m.AddItem(new GUIContent(view.Label), activeView == view, () =>
				{
					_bkUser.View = key;
				});
				i++;
			}
			m.ShowAsContext();
		}

		private void ShowBookmarksContext()
		{
			var options = GetProfileOptions();

			var m = new GenericMenu();

			if (_bkUser.HasProfile)
			{
				m.AddItem(new GUIContent("Edit", "Open in default editor"), false, () =>
				{
					_bkUser.Edit();
				});
				m.AddSeparator("");
			}

			var value = _bkUser.ProfilePath;

			foreach (var (olabel, ovalue) in options)
			{
				var active = ovalue == value;
				var val = ovalue;

				if (active)
				{
					m.AddDisabledItem(olabel, active);
				}
				else
				{
					m.AddItem(olabel, ovalue == value, () =>
					{
						_bkUser.Load(val);
						//EditorApplication.delayCall += () => selectFn.Invoke(val);
					});
				}
			}
			m.AddSeparator("");
			m.AddItem(new GUIContent("Reload"), false, Reload);
			m.ShowAsContext();
		}

		private void ShowColumnPicker()
		{
			var m = new GenericMenu();
			var options = Enum.GetNames(typeof(DisplayOption));
			var hasNone = _displayFlags == 0;

			m.AddItem(new GUIContent("Nothing"), hasNone, () =>
			{
				_displayFlags = 0;
			});
			m.AddItem(new GUIContent("Everything"), _displayFlags == DisplayOption.All, () =>
			{
				_displayFlags = DisplayOption.All;
			});

			m.AddSeparator("");

			for (var i = 0; i < 3; i++)
			{
				var value = (DisplayOption)Mathf.Pow(2, i);
				var o = options[i];
				var active = _displayFlags.HasFlag(value);
				m.AddItem(new GUIContent(o.ToSentence()), active, () =>
				{
					_displayFlags = active
					? _displayFlags & ~value
					: _displayFlags | value;
				});
			}
			m.ShowAsContext();
		}

		// =============================================
		// ============= STATIC HELPERS ================
		// =============================================
		// TODO: "Maybe" move some of these somewhere else

		private static class WIcon
		{
			public const float DEF_OPACITY = 0.8f;
			public static Color ICON_COLOR => _skinColor.Value;
			public static GUIContent FILTER => _filter.Value;
			public static GUIContent FILTER_UNCHECKED => _filter_unchecked.Value;
			public static GUIContent SORT => _sort.Value;
			public static GUIContent MISSING_ASSET => _missingAsset.Value;
			public static GUIContent BLENDER => _blender.Value;
			public static GUIContent FLOPPY => _save.Value;

			// move these guids somewhere?
			private static readonly Lazy<GUIContent> _filter_unchecked = Load(Constants.ICON_GUID_FILTER_OFF);
			private static readonly Lazy<GUIContent> _sort = Load(Constants.ICON_GUID_SORT);
			private static readonly Lazy<GUIContent> _filter = Load(Constants.ICON_GUID_FILTER);
			private static readonly Lazy<GUIContent> _save = Load(Constants.ICON_GUID_SAVE);
			private static readonly Lazy<GUIContent> _blender = Load(Constants.ICON_GUID_BLENDER);
			private static readonly Lazy<GUIContent> _missingAsset = Load(Constants.ICON_GUID_MISSING);

			private static readonly Lazy<Color> _skinColor = new(() =>
			{
				return (EditorGUIUtility.isProSkin ? Color.white : Color.black);
			});


			private static Lazy<GUIContent> Load(string guid)
			{
				return new(() =>
				{
					var ico = ADB.LoadAssetAtPath<Texture>(ADB.GUIDToAssetPath(guid));
					return new GUIContent(ico);
				});
			}
		}

		private static class WStyle
		{
			public static void Label(Rect pos, string txt, float scale, TextAnchor anchor)
			{
				DefaultLabel.alignment = anchor;
				DefaultLabel.fontSize = (int)(EditorStyles.label.fontSize * scale);
				EditorGUI.LabelField(pos, txt, DefaultLabel);
			}

			private static GUIStyle DefaultLabel => _label.Value;
			public static GUIStyle IconButton => _icoButtonStyle.Value;
			public static GUIStyle IconPopup => icoPopupStyle.Value;

			private static Lazy<GUIStyle> _icoButtonStyle = new(() =>
			{
				var s = new GUIStyle(EditorStyles.iconButton);
				s.alignment = TextAnchor.MiddleCenter;
				s.imagePosition = ImagePosition.ImageOnly;
				s.fixedWidth += 4;
				s.padding = new RectOffset(2, 2, 0, 0);
				s.margin = new RectOffset(3, 3, 2, 2);
				return s;
			});

			private static Lazy<GUIStyle> icoPopupStyle = new(() =>
			{
				var s = new GUIStyle(EditorStyles.toolbarPopup);
				s.fixedWidth = s.fixedHeight * 2f;
				return s;
			});

			private static readonly Lazy<GUIStyle> _label = new(() =>
			{
				var s = new GUIStyle(EditorStyles.label);
				s.alignment = TextAnchor.MiddleLeft;
				return s;
			});
		}

		private static void ScrollColumn(ref Vector2 scroll, Action fn)
		{
			GUILayout.BeginVertical();
			scroll = EditorGUILayout.BeginScrollView(scroll, false, false);
			fn.Invoke();
			EditorGUILayout.EndScrollView();
			GUILayout.EndVertical();
		}

		private Texture GetMiniAssetIcon(BKAsset a)
		{
			if(ShowAssetPreviews)
			{
				if (a.AssetPreview) { return a.AssetPreview; }
			}
			// TODO: move this to extension?
			var path = a.Path;
			if (path.EndsWith("end")) { return WIcon.BLENDER.image; }
			var cachedIcon = ADB.GetCachedIcon(path);

			// 
			if (!cachedIcon) { a.Reload(); cachedIcon = ADB.GetCachedIcon(path); }

			return cachedIcon ?? WIcon.MISSING_ASSET.image;
		}


		private static bool HasClick(Rect pos, int button, int count, bool up = false)
		{
			return
			Event.current.isMouse
			&& Event.current.button == button
			&& Event.current.type == (up ? EventType.MouseUp : EventType.MouseDown)
			&& Event.current.clickCount == count
			&& pos.Contains(Event.current.mousePosition);
		}

		private static void IconButton(GUIContent icon, Action fn, Color c, bool popup = false)
		{
			var s = popup ? WStyle.IconPopup : WStyle.IconButton;

			var tc = GUI.contentColor;
			GUI.contentColor = c;
			if (GUILayout.Button(icon, s))
			{
				fn.Invoke();
			}
			GUI.contentColor = tc;
		}

	}
}