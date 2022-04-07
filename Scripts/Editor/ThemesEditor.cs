#if UNITY_EDITOR
using Helper.Extensions;
using Helper.Extensions.Unity;
using Helper.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Themes))]
public class ThemesEditor : Editor {

	#region Serialized Props
	SerializedProperty all;
	SerializedProperty levelImgAlpha;
	#endregion

	private int previewWidth = 64;
	protected bool autoSaveOnChange;
	private LevelGroups levelGroup;
	private int swapL, swapR;

	private GameObject tt;

	private Themes trg { get { return target as Themes; } }

	private void OnEnable() {
		all = serializedObject.FindProperty("all");
		levelImgAlpha = serializedObject.FindProperty("levelImgAlpha");
	}

	public override void OnInspectorGUI() {
		serializedObject.Update();

        #region Top Panel
        Rect myRect = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
        GUI.Box(myRect, "Drag & drop assets here!");
        if(myRect.Contains(Event.current.mousePosition)) {
            if(Event.current.type == EventType.DragUpdated) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            } else if(Event.current.type == EventType.DragExited) {
                var option = EditorUtility.DisplayDialogComplex("Choose the method", "What do you want trg.to do?", "Update Only Dragged Items", "Cancel", "Add");
                if(option != 1) {
                    LoadDraggedImages(DragAndDrop.paths, option == 2); //add if option is 2, overwrite otherwise
                    Debug.Log(DragAndDrop.objectReferences.Length + " assets added!");
                }
                Event.current.Use();
            }
        }
        GUILayout.Space(10);

        tt = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Theme Editor"), tt, typeof(GameObject), true);

        EditorGUILayout.BeginHorizontal();
        autoSaveOnChange = EditorGUILayout.Toggle(new GUIContent("Auto Save", "Auto Save On Swap"), autoSaveOnChange);
        if(GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("SaveActive"), "Save all!"),
        new GUILayoutOption[] { GUILayout.Width(30), GUILayout.Height(EditorGUIUtility.singleLineHeight) })) {
            Save();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        previewWidth = EditorGUILayout.IntSlider(new GUIContent("Preview Size"), previewWidth, 64, 256);
        EditorGUI.BeginDisabledGroup(true); //trg.all == null || trg.all.Count == 0
        if(GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("preAudioLoopOff"), "Shuffle"),
            new GUILayoutOption[] { GUILayout.Width(30), GUILayout.Height(EditorGUIUtility.singleLineHeight) })) {
            trg.all = trg.all.Shuffle();
            Save();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        EditorGUI.BeginChangeCheck();
        trg.levelImgAlpha = EditorGUILayout.IntSlider(new GUIContent("Level Image Alpha"), trg.levelImgAlpha, 0, 255);
        if(EditorGUI.EndChangeCheck()) {
            for(int i = 0; i < trg.all.Count; i++) {
                trg.all[i].levelImgColor = new Color(trg.all[i].levelImgColor.r, trg.all[i].levelImgColor.g, trg.all[i].levelImgColor.b,
                    ((float)levelImgAlpha.intValue) / 255);
            }
        }

        GUILayout.BeginHorizontal();
        levelGroup = (LevelGroups)EditorGUILayout.ObjectField(new GUIContent("Level Groups"), levelGroup, typeof(LevelGroups), true);
        EditorGUI.BeginDisabledGroup(levelGroup == null);
        if(GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("d_playLoopOff"), "Randomize Group Amounts"), EditorStyles.miniButtonLeft, GUILayout.Width(30))) {
            //trg.RandomizeGroupAmount(levelGroup);
        }
        if(GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("d_P4_CheckOutRemote"), "Set Group's Parent Themes Indexes"), EditorStyles.miniButtonMid, GUILayout.Width(30))) {
            trg.SetGroupThemeIndexes(levelGroup);
            EditorUtils.Save(levelGroup);
        }
        if(GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("d_P4_CheckOutRemote"), "Create From Groups"), EditorStyles.miniButtonRight, GUILayout.Width(30))) {
            trg.CreateFromLevelGroups(levelGroup);
            EditorUtils.Save(trg);
        }
        EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();
        GUILayout.Space(10); 
        #endregion

        #region Swap
        EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField(new GUIContent("Swap", "Swap given levels (uses index)"), GUILayout.Width(EditorGUIUtility.labelWidth));
		swapL = EditorGUILayout.IntField(swapL);
		swapR = EditorGUILayout.IntField(swapR);
		EditorGUI.BeginDisabledGroup(swapL == swapR);
		if(GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("preAudioLoopOff"), "Swap"), GUILayout.Width(30))) {
            Swap(swapL, swapR);
            Save();
		}
		EditorGUI.EndDisabledGroup();
		EditorGUILayout.EndHorizontal();
		GUILayout.Space(10);
		#endregion

		EditorUtils.Paging(all, ref trg.showBy, ref trg.currentPage, ref trg.from, ref trg.to, 25, 40);

		GUILayout.Space(16);
		if(trg.all.IsNotNullAndEmpty()) DrawImages();

		if(GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("Toolbar Plus"), "Add Word"), 
				new GUILayoutOption[] { GUILayout.Width(25), GUILayout.Height(EditorGUIUtility.singleLineHeight) })) {
			var index = all.arraySize;
			all.InsertArrayElementAtIndex(index);
		}

		serializedObject.ApplyModifiedProperties();
	}

	private void LoadDraggedImages(string[] paths, bool add = false) {
		var draggedOnes = paths.Select(x => AssetDatabase.LoadAssetAtPath<Sprite>(x)).ToList();
		if(add) {
			trg.all = trg.all ?? new List<Theme>();
			trg.all.AddRange(draggedOnes.Select(x => new Theme(x, 0, Utility.GetRandomColor())));
		} else {
            for(int i = 0; i < trg.all.Count; i++) {
                var sp = draggedOnes.GetIfContains(i);
                if(sp) {
                    trg.all[i].sprite = sp;
                }
            }
            //trg.all = draggedOnes.Select(x => new Theme(x, 0, Utility.GetRandomColor())).ToList();
		}
		Save();
	}

	public void DrawImages() {
		float labelWidth = 35.0f;
		float colorWidth = 90.0f;
		float amountWidth = 40.0f;
		float btnWidth = 20.0f;
		float textureWidth = previewWidth;
		float spriteWidth = EditorGUIUtility.currentViewWidth - labelWidth - amountWidth - textureWidth - colorWidth * 3.75f - 60.0f;

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Index", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
		EditorGUILayout.LabelField("Amount", EditorStyles.boldLabel, GUILayout.Width(amountWidth));
		EditorGUILayout.LabelField("Buttons Color", EditorStyles.boldLabel, GUILayout.Width(colorWidth));
		EditorGUILayout.LabelField("Level Img", EditorStyles.boldLabel, GUILayout.Width(colorWidth));
		EditorGUILayout.LabelField(new GUIContent("Lbl", "New Label Color"), EditorStyles.boldLabel, GUILayout.Width(colorWidth / 1.5f));
		EditorGUILayout.LabelField(new GUIContent("E1","Light Ray Color1"), EditorStyles.boldLabel, GUILayout.Width(colorWidth / 2));
		EditorGUILayout.LabelField(new GUIContent("E2", "Light Ray Color2"), EditorStyles.boldLabel, GUILayout.Width(colorWidth / 2));
		EditorGUILayout.LabelField("Sprite", EditorStyles.boldLabel, GUILayout.Width(spriteWidth));
		EditorGUILayout.EndHorizontal();

		for(int i = trg.from; i < trg.to; i++) {
			var rect = EditorGUILayout.GetControlRect();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(labelWidth));
			EditorGUILayout.PropertyField(all.GetArrayElementAtIndex(i).FindPropertyRelative("groupAmount"), GUIContent.none, GUILayout.Width(amountWidth));
			
            EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(all.GetArrayElementAtIndex(i).FindPropertyRelative("buttonsColor"), GUIContent.none, GUILayout.Width(colorWidth));
            if(EditorGUI.EndChangeCheck()) {
                SetTheme(i);
            }
            
            EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(all.GetArrayElementAtIndex(i).FindPropertyRelative("levelImgColor"), GUIContent.none, GUILayout.Width(colorWidth));
			if(EditorGUI.EndChangeCheck()) {
				var color = all.GetArrayElementAtIndex(i).FindPropertyRelative("levelImgColor").colorValue;
				all.GetArrayElementAtIndex(i).FindPropertyRelative("levelImgColor").colorValue = color.Transpare(levelImgAlpha.intValue / 255f);
                SetTheme(i);
			}
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(all.GetArrayElementAtIndex(i).FindPropertyRelative("labelColor"), GUIContent.none, GUILayout.Width(colorWidth / 1.5f));
            if(EditorGUI.EndChangeCheck()) {
                SetTheme(i);
            }

            EditorGUILayout.PropertyField(all.GetArrayElementAtIndex(i).FindPropertyRelative("effectColor1"), GUIContent.none, GUILayout.Width(colorWidth / 2f));
			EditorGUILayout.PropertyField(all.GetArrayElementAtIndex(i).FindPropertyRelative("effectColor2"), GUIContent.none, GUILayout.Width(colorWidth / 2f));
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(all.GetArrayElementAtIndex(i).FindPropertyRelative("sprite"), GUIContent.none, GUILayout.Width(spriteWidth));
			if(EditorGUI.EndChangeCheck()) {
				int ndo = 1;
                for(int j = 0; j < trg.all.Count; j++) {
                    if(j != i && trg.all[j].sprite && trg.all[j].sprite.name == 
						((Sprite)all.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue).name) {
						ndo++;
                    }
                    if(ndo > 1) {
						trg.all[i] = null;
						Debug.LogError("Same object is already on the list!");
						break;
					}
                }
			}
			if(trg.all[i].sprite) {
				var drawRect = new Rect(EditorGUIUtility.currentViewWidth - textureWidth - 20.0f, rect.y, textureWidth, textureWidth);
				EditorGUI.DrawPreviewTexture(drawRect, trg.all[i].sprite.texture);
				if(drawRect.Contains(Event.current.mousePosition)) {
					if(Event.current.type == EventType.MouseDown) {
						DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
						DragAndDrop.PrepareStartDrag();
						DragAndDrop.objectReferences = new Object[] { trg.all[i].sprite };
						DragAndDrop.StartDrag(trg.all[i].sprite.name);
						Event.current.Use();
					} else if(Event.current.type == EventType.DragUpdated) {
						Event.current.Use();
					} else if(Event.current.type == EventType.DragExited) {
						if(DragAndDrop.objectReferences.Length > 0) {
							int toSwap = trg.all.IndexOf(trg.all.FirstOrDefault(x => x.sprite == (Sprite)DragAndDrop.objectReferences[0]));
							if(toSwap > 0) {
								DragAndDrop.AcceptDrag();
                                Swap(i, toSwap);
								if(autoSaveOnChange) Save();
							}
						}
						Event.current.Use();
					}
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space(labelWidth);
			EditorGUILayout.PropertyField(all.GetArrayElementAtIndex(i).FindPropertyRelative("effectType"), GUIContent.none);
			if(GUILayout.Button(new GUIContent("Set", "SET"), //EditorGUIUtility.FindTexture("preAudioLoopOff")
			new GUILayoutOption[] { GUILayout.Height(EditorGUIUtility.singleLineHeight) })) {
                SetTheme(i);
			}
			if(GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("d_winbtn_mac_close_h"), ""), 
				EditorStyles.boldLabel, GUILayout.Width(btnWidth))) {
				all.DeleteArrayElementAtIndex(i);
			}
			EditorGUILayout.Space(textureWidth);
			EditorGUILayout.EndHorizontal();
		}
        if(trg.all.Count > 0) {
			EditorGUILayout.LabelField("Total: " + trg.all.Sum(x => x.groupAmount).ToString());
        }
	}

    private void SetTheme(int i) {
        if(tt.IsNotNull()) {
            tt.GetComponent<ThemeTester>().SetTheme(i);
            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
        }
    }

	private void Save() {
		EditorUtility.SetDirty(target);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Repaint();
	}

    private void Swap(int rhs, int lhs) {
        trg.all.Swap(rhs, lhs);
        var i1 = trg.all[lhs].groupAmount;
        trg.all[lhs].groupAmount = trg.all[rhs].groupAmount;
        trg.all[rhs].groupAmount = i1;
        Debug.Log("Successfully swapped!");
    }
}
#endif
