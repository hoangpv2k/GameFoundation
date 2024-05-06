﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using custom.find.reference;
using GUI = custom.find.reference.GUI;

public class CustomDeleteButton
{
	public string warningMessage;
	public string confirmMessage;
	public GUIContent deleteLabel;
	public bool hasConfirm;
	
	public bool Draw(Action onConfirmDelete)
	{
		GUILayout.BeginHorizontal();
		{
			EditorGUILayout.HelpBox(warningMessage, MessageType.Warning);
			GUILayout.BeginVertical();
			{
				GUILayout.Space(2f);
				hasConfirm = GUILayout.Toggle(hasConfirm, confirmMessage);
				EditorGUI.BeginDisabledGroup(!hasConfirm);
				{
					GUI.BackgroundColor(() =>
					{
						if (GUILayout.Button(deleteLabel, EditorStyles.miniButton))
						{
							hasConfirm = false;
							onConfirmDelete();
							GUIUtility.ExitGUI();
						}
					}, GUI.darkRed, 0.8f);
				}
				EditorGUI.EndDisabledGroup();
			}
			GUILayout.EndVertical();
		}
		GUILayout.EndHorizontal();
		return false;
	}
}
