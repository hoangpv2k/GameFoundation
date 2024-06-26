﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace custom.find.reference
{
	public class AssetType
	{
		// ------------------------------- STATIC -----------------------------

		internal static readonly AssetType[] FILTERS =
		{
			new AssetType("Scene", ".unity"),
			new AssetType("Prefab", ".prefab"),
			new AssetType("Model", ".3df", ".3dm", ".3dmf", ".3dv", ".3dx", ".c5d", ".lwo", ".lws", ".ma", ".mb",
				".mesh", ".vrl", ".wrl", ".wrz", ".fbx", ".dae", ".3ds", ".dxf", ".obj", ".skp", ".max", ".blend"),
			new AssetType("Material", ".mat", ".cubemap", ".physicsmaterial"),
			new AssetType("Texture", ".ai", ".apng", ".png", ".bmp", ".cdr", ".dib", ".eps", ".exif", ".ico", ".icon",
				".j", ".j2c", ".j2k", ".jas", ".jiff", ".jng", ".jp2", ".jpc", ".jpe", ".jpeg", ".jpf", ".jpg", "jpw",
				"jpx", "jtf", ".mac", ".omf", ".qif", ".qti", "qtif", ".tex", ".tfw", ".tga", ".tif", ".tiff", ".wmf",
				".psd", ".exr", ".rendertexture"),
			new AssetType("Video", ".asf", ".asx", ".avi", ".dat", ".divx", ".dvx", ".mlv", ".m2l", ".m2t", ".m2ts",
				".m2v", ".m4e", ".m4v", "mjp", ".mov", ".movie", ".mp21", ".mp4", ".mpe", ".mpeg", ".mpg", ".mpv2",
				".ogm", ".qt", ".rm", ".rmvb", ".wmv", ".xvid", ".flv"),
			new AssetType("Audio", ".mp3", ".wav", ".ogg", ".aif", ".aiff", ".mod", ".it", ".s3m", ".xm"),
			new AssetType("Script", ".cs", ".js", ".boo", ".h"),
			new AssetType("Text", ".txt", ".json", ".xml", ".bytes", ".sql"),
			new AssetType("Shader", ".shader", ".cginc"),
			new AssetType("Animation", ".anim", ".controller", ".overridecontroller", ".mask"),
			new AssetType("Unity Asset", ".asset", ".guiskin", ".flare", ".fontsettings", ".prefs"),
			new AssetType("Others") //
		};

		private static CustomIgnore _ignore;
		public HashSet<string> extension;
		public string name;

		public AssetType(string name, params string[] exts)
		{
			this.name = name;
			extension = new HashSet<string>();
			for (var i = 0; i < exts.Length; i++)
			{
				extension.Add(exts[i]);
			}
		}

		private static CustomIgnore ignore
		{
			get
			{
				if (_ignore == null)
				{
					_ignore = new CustomIgnore();
				}

				return _ignore;
			}
		}

		public static int GetIndex(string ext)
		{
			for (var i = 0; i < FILTERS.Length - 1; i++)
			{
				if (FILTERS[i].extension.Contains(ext))
				{
					return i;
				}
			}

			return FILTERS.Length - 1; //Others
		}

		public static bool DrawSearchFilter()
		{
			int n = FILTERS.Length;
			var nCols = 4;
			int nRows = Mathf.CeilToInt(n / (float) nCols);
			var result = false;

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				if (GUILayout.Button("All", EditorStyles.toolbarButton) && !CustomSetting.IsIncludeAllType())
				{
					CustomSetting.IncludeAllType();
					result = true;
				}

				if (GUILayout.Button("None", EditorStyles.toolbarButton) && CustomSetting.GetExcludeType() != -1)
				{
					CustomSetting.ExcludeAllType();
					result = true;
				}
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			for (var i = 0; i < nCols; i++)
			{
				GUILayout.BeginVertical();
				for (var j = 0; j < nRows; j++)
				{
					int idx = i * nCols + j;
					if (idx >= n)
					{
						break;
					}

					bool s = !CustomSetting.IsTypeExcluded(idx);
					bool s1 = GUILayout.Toggle(s, FILTERS[idx].name);
					if (s1 != s)
					{
						result = true;
						CustomSetting.ToggleTypeExclude(idx);
					}
				}

				GUILayout.EndVertical();
				if ((i + 1) * nCols >= n)
				{
					break;
				}
			}

			GUILayout.EndHorizontal();
			
			return result;
		}

		public static void SetDirtyIgnore()
		{
			ignore.SetDirty();
		}

		public static bool DrawIgnoreFolder()
		{
			var change = false;
			ignore.Draw();
			
			


			// CustomHelper.GuiLine();
			// List<string> lst = CustomSetting.IgnoreFolder.ToList();
			// bool change = false;
			// pos = EditorGUILayout.BeginScrollView(pos);
			// for(int i =0; i < lst.Count; i++)
			// {
			// 	GUILayout.BeginHorizontal();
			// 	{
			// 		if(GUILayout.Button("X", GUILayout.Width(30)))
			// 		 {
			// 			 change = true;
			// 			 CustomSetting.RemoveIgnore(lst[i]);
			// 		 }
			// 		 GUILayout.Label(lst[i]);
			// 	}GUILayout.EndHorizontal();
			// }
			// EditorGUILayout.EndScrollView();
			return change;
		}
		
		private class CustomIgnore
		{
			public readonly CustomTreeUI.GroupDrawer groupIgnore;
			private bool dirty;
			private Dictionary<string, CustomRef> refs;

			public CustomIgnore()
			{
				groupIgnore = new CustomTreeUI.GroupDrawer(DrawGroup, DrawItem);
				groupIgnore.hideGroupIfPossible = false;
				
				ApplyFilter();
			}

			private void DrawItem(Rect r, string guid)
			{
				CustomRef rf;
				if (!refs.TryGetValue(guid, out rf))
				{
					return;
				}

				if (rf.depth == 1) //mode != Mode.Dependency && 
				{
					Color c = UnityEngine.GUI.color;
					UnityEngine.GUI.color = Color.blue;
					UnityEngine.GUI.DrawTexture(new Rect(r.x - 4f, r.y + 2f, 2f, 2f), EditorGUIUtility.whiteTexture);
					UnityEngine.GUI.color = c;
				}

				rf.asset.Draw(r, false,
					true,
					false, false, false, false, null);

				Rect drawR = r;
				drawR.x = drawR.x + drawR.width - 50f; // (groupDrawer.TreeNoScroll() ? 60f : 70f) ;
				drawR.width = 30;
				drawR.y += 1;
				drawR.height -= 2;

				if (UnityEngine.GUI.Button(drawR, "X", EditorStyles.miniButton))
				{
					CustomSetting.RemoveIgnore(rf.asset.assetPath);
				}
			}

			private void DrawGroup(Rect r, string id, int childCound)
			{
				UnityEngine.GUI.Label(r, id, EditorStyles.boldLabel);
				if (childCound <= 1)
				{
					return;
				}

				Rect drawR = r;
				drawR.x = drawR.x + drawR.width - 50f; // (groupDrawer.TreeNoScroll() ? 60f : 70f) ;
				drawR.width = 30;
				drawR.y += 1;
				drawR.height -= 2;
			}

			public void SetDirty()
			{
				dirty = true;
			}
			//private float sizeRatio {
			//    get{
			//        if(CustomWindow.window != null)
			//            return CustomWindow.window.sizeRatio;
			//        return .3f;
			//    }
			//}

			public void Draw()
			{
				if (dirty)
				{
					ApplyFilter();
				}

				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(4f);
					var drops = GUI.DropZone("Drag & Drop folders here to exclude", 100, 95);
					if (drops != null && drops.Length > 0)
					{
						for (var i = 0; i < drops.Length; i++)
						{
							string path = AssetDatabase.GetAssetPath(drops[i]);
							if (path.Equals(CustomCache.DEFAULT_CACHE_PATH))
							{
								continue;
							}

							CustomSetting.AddIgnore(path);
						}
					}
				
					groupIgnore.DrawLayout();
				}
				GUILayout.EndHorizontal();
			}


			private void ApplyFilter()
			{
				dirty = false;
				refs = new Dictionary<string, CustomRef>();

				//foreach (KeyValuePair<string, List<string>> item in CustomSetting.IgnoreFiltered)
				foreach (string item2 in CustomSetting.s.listIgnore)
				{
					string guid = AssetDatabase.AssetPathToGUID(item2);
					if (string.IsNullOrEmpty(guid))
					{
						continue;
					}

					CustomAsset asset = CustomCache.Api.Get(guid, true);
					var r = new CustomRef(0, 0, asset, null, "Ignore");
					refs.Add(guid, r);
				}

				groupIgnore.Reset
				(
					refs.Values.ToList(),
					rf => rf.asset != null ? rf.asset.guid : "",
					GetGroup,
					SortGroup
				);
			}

			private string GetGroup(CustomRef rf)
			{
				return "Ignore";
			}

			private void SortGroup(List<string> groups) { }
		}
	}
}