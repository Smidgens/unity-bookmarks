// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks
{
	using UnityEngine;
	using System.IO;
	using ADB = UnityEditor.AssetDatabase;
	using System;
	using System.Reflection;
	using System.Collections.Generic;

	/// <summary>
	/// IO helper
	/// </summary>
	internal static class Helpers
	{
		public delegate Dictionary<string, float> LabelsGetter();

		/// <summary>
		/// Path to folder root
		/// </summary>
		public static readonly string ProjectFolder =
		Application.dataPath.Substring(0, Application.dataPath.Length - 7); // trim "Assets"

		public static string GetAbsolutePath(string relativePath)
		{
			return Path.Combine(ProjectFolder, relativePath).Replace(@"\", "/");
		}

		public static void OpenProjectFile(string relativePath)
		{
			var path = GetAbsolutePath(relativePath);

			if (!File.Exists(path))
			{
				Debug.LogError($"cannot open file | File doesn't exist: '{path}'");
				return;
			}
			System.Diagnostics.Process.Start(path);
		}

		public static string[] ListFiles(string dir, string pattern, bool recursive = false)
		{
			if (!Directory.Exists(dir)) { return new string[0]; }

			if (!recursive)
			{
				return Directory.GetFiles(dir, pattern);
			}
			return Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);
		}

		public static Dictionary<string,float> GetAssetLabels()
		{
			return GET_ALL_LABELS_FN.Value.Invoke();
		}

		private static readonly string[] FSIZE_LABEL = { "B", "KB", "MB", "GB", "TB" };

		public static string GetFileSizeLabel(long size)
		{
			int order = 0;
			while (size >= 1024 && order < FSIZE_LABEL.Length - 1)
			{
				order++;
				size = size / 1024;
			}
			var unit = FSIZE_LABEL[order];
			return $"{size} {unit}";
		}


		private const BindingFlags INTERNAL_FN = BindingFlags.Static | BindingFlags.NonPublic;

		private static readonly Lazy<LabelsGetter> GET_ALL_LABELS_FN = new(() =>
		{
			var m = typeof(ADB).GetMethod("GetAllLabels", INTERNAL_FN);
			return (LabelsGetter)m.CreateDelegate(typeof(LabelsGetter), null);
		});

	}
}