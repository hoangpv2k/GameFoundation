﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace custom.find.reference
{
	public class CustomTreeUI
	{
		internal Drawer drawer;
		private Vector2 position;
		private TreeItem rootItem;
		internal Rect visibleRect;
		public float itemPaddingRight = 0f;

		public CustomTreeUI(Drawer drawer)
		{
			this.drawer = drawer;
		}

		public void Reset(params string[] root)
		{
			position = Vector2.zero;

			rootItem = new TreeItem
			{
				tree = this,
				id = "$root",
				height = 0,
				depth = -1,
				_isOpen = true,
				highlight = false,
				childCount = root.Length
			};

			rootItem.RefreshChildren(root);
			rootItem.DeepOpen();
		}

		public void Draw(Rect rect)
		{
			if (rect.width > 0)
			{
				visibleRect = rect;
			}
			
			var contentRect = new Rect(0f, 0f, 1f, rootItem.childrenHeight);
			bool noScroll = contentRect.height < visibleRect.height;
			if (noScroll)
			{
				position = Vector2.zero;
			}

			var minY = (int) position.y;
			var maxY = (int) (position.y + visibleRect.height);
			contentRect.x -= CustomSetting.TreeIndent;

			TreeItem.DrawCall = 0;
			TreeItem.DrawRender = 0;
			position = UnityEngine.GUI.BeginScrollView(visibleRect, position, contentRect);
			{
				var r = new Rect(0, 0, rect.width - (noScroll ? 4f : 18f)-itemPaddingRight, 16f);
				var index = 0;
				rootItem.Draw(ref index, ref r, minY, maxY);
			}

			UnityEngine.GUI.EndScrollView();
		}

		public void DrawLayout()
		{
			EventType evtType = Event.current.type;
			Rect r = GUILayoutUtility.GetRect(1f, Screen.width, 16f, Screen.height);

			if (evtType != EventType.Layout)
			{
				visibleRect = r;
			}
			
			Draw(visibleRect);
		}

		public bool NoScroll()
		{
			return rootItem.childrenHeight < visibleRect.height;
		}

		// ------------------------ DELEGATE --------------

		public class Drawer
		{
			public virtual int GetHeight(string id)
			{
				return 16;
			}

			public virtual int GetChildCount(string id)
			{
				return 0;
			}

			public virtual string[] GetChildren(string id)
			{
				return null;
			}

			public virtual void Draw(Rect r, TreeItem item)
			{
				UnityEngine.GUI.Label(r, item.id);
			}
		}

		public class GroupDrawer : Drawer
		{
			public Action<Rect, string, int> drawGroup;
			public Action<Rect, string> drawItem;
			private Dictionary<string, List<string>> groupDict;
			internal CustomTreeUI tree;

			public GroupDrawer(Action<Rect, string, int> drawGroup, Action<Rect, string> drawItem)
			{
				this.drawItem = drawItem;
				this.drawGroup = drawGroup;
			}

			// ----------------- TREE WRAPPER ------------------
			public bool TreeNoScroll()
			{
				return tree.NoScroll();
			}

			public bool hideGroupIfPossible;
			
			
			public void Reset<T>(List<T> items, Func<T, string> idFunc, Func<T, string> groupFunc,
				Action<List<string>> customGroupSort = null)
			{
				groupDict = new Dictionary<string, List<string>>();

				for (var i = 0; i < items.Count; i++)
				{
					List<string> list;

					string groupName = groupFunc(items[i]);
					if (groupName == null) continue; // do not exclude groupName string.Empty
					
					string itemId = idFunc(items[i]);
					if (string.IsNullOrEmpty(itemId)) continue; // ignore items without id
					
					if (!groupDict.TryGetValue(groupName, out list))
					{
						list = new List<string>();
						groupDict.Add(groupName, list);
					}
					
					list.Add(itemId);
				}

				if (tree == null)
				{
					tree = new CustomTreeUI(this);
				}
				
				List<string> groups = groupDict.Keys.ToList();

				if (hideGroupIfPossible && groups.Count == 1) //single group : Flat list
				{
					var v = groupDict[groups[0]];
					tree.Reset(v.ToArray());
					groupDict.Clear();
				} else { //multiple groups
					if (customGroupSort != null)
					{
						customGroupSort(groups);
					}
					else
					{
						groups.Sort();
					}

					tree.Reset(groups.ToArray());
				}
			}

			public void Draw(Rect r)
			{
				if (tree != null) tree.Draw(r);
			}

			public bool hasChildren
			{
				get {
					return tree != null && tree.rootItem.childCount > 0;
				}
			}

			public bool hasValidTree
			{
				get { return groupDict != null && tree != null; }
			}
			
			public void DrawLayout()
			{
				if (tree != null) tree.DrawLayout();
			}

			// ----------------- DRAWER WRAPPER ------------------

			public override int GetChildCount(string id)
			{
				if (string.IsNullOrEmpty(id)) return 0;
				
				List<string> group;
				if (groupDict.TryGetValue(id, out group))
				{
					return group.Count;
				}

				return 0;
			}

			public override string[] GetChildren(string id)
			{
				List<string> group;
				if (groupDict.TryGetValue(id, out group))
				{
					return group.ToArray();
				}

				return null;
			}

			public override void Draw(Rect r, TreeItem item)
			{
				List<string> group;
				if (groupDict.TryGetValue(item.id, out group))
				{
					drawGroup(r, item.id, item.childCount);
					return;
				}

				drawItem(r, item.id);
			}
		}
		
		// ------------------------ TreeItem2 --------------

		public class TreeItem
		{
			public static int DrawCall;
			public static int DrawRender;

			internal bool _isOpen;

			public int childCount;
			public List<TreeItem> children;
			public int childrenHeight;
			public int depth; // item depth

			public int height;
			public bool highlight;

			public string id; // item id

			internal TreeItem parent;
			//static Color COLOR	= new Color(0f, 0f, 0f, 0.05f);

			internal CustomTreeUI tree;

			public bool IsOpen
			{
				get { return _isOpen; }
				set
				{
					if (_isOpen == value || childCount == 0)
					{
						return;
					}

					_isOpen = value;

					if (_isOpen)
					{
						if (children == null)
						{
							RefreshChildren(tree.drawer.GetChildren(id));
						}

						//Update height for all parents
						TreeItem p = parent;
						while (p != null)
						{
							p.childrenHeight += childrenHeight;
							p = p.parent;
						}
					}
					else
					{
						//Update height for all parents
						TreeItem p = parent;
						while (p != null)
						{
							p.childrenHeight -= childrenHeight;
							p = p.parent;
						}
					}
				}
			}

			internal void DeepOpen()
			{
				IsOpen = true;
				if (children == null)
				{
					return;
				}

				for (var i = 0; i < children.Count; i++)
				{
					children[i].DeepOpen();
				}
			}

			internal void Draw(ref int index, ref Rect rect, int minY, int maxY)
			{
				DrawCall++;

				// if (DrawCall < 10)
				// {
				// 	Debug.Log(index + ":" + rect + ":" + minY + ":" + maxY + ":" + height + ":" + childrenHeight);
				// }

				//var skipDraw = (rect.y >= maxY) || (height <=0);
				float min = rect.y;
				float max = rect.y + height;
				bool interMin = min >= minY && min <= maxY;
				bool interMax = max >= minY && max <= maxY;

				if (height > 0 && (interMin || interMax))
				{
					DrawRender++;
					rect.height = height;

					if (index % 2 == 1 && CustomSetting.AlternateRowColor)
					{
						Color o = UnityEngine.GUI.color;
						UnityEngine.GUI.color = CustomSetting.RowColor;
						// GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
						UnityEngine.GUI.DrawTexture(new Rect(rect.x - CustomSetting.TreeIndent, rect.y, rect.width, rect.height),
							EditorGUIUtility.whiteTexture);
						UnityEngine.GUI.color = o;
					}

					float x = (depth + 1) * 16f;
					tree.drawer.Draw(new Rect(x, rect.y, rect.width - x, rect.height), this);

					if (childCount > 0)
					{
						IsOpen = UnityEngine.GUI.Toggle(new Rect(rect.x + x - 16f, rect.y, 16f, 16f), IsOpen, string.Empty,
							EditorStyles.foldout);
					}

					index++;
					rect.y += height;
				}
				else
				{
					rect.y += height;
				}

				if (_isOpen && rect.y <= maxY) //draw children
				{
					for (var i = 0; i < children.Count; i++)
					{
						children[i].Draw(ref index, ref rect, minY, maxY);
						if (rect.y > maxY)
						{
							break;
						}
					}
				}
			}

			internal void RefreshChildren(string[] childrenIDs)
			{
				childCount = childrenIDs.Length;
				childrenHeight = 0;
				children = new List<TreeItem>();

				for (var i = 0; i < childCount; i++)
				{
					string itemId = childrenIDs[i];

					var item = new TreeItem
					{
						tree = tree,
						parent = this,

						id = itemId,
						depth = depth + 1,
						highlight = false,

						height = tree.drawer.GetHeight(itemId),
						childCount = tree.drawer.GetChildCount(itemId)
					};

					childrenHeight += item.height;
					children.Add(item);
				}
			}
		}
	}
}