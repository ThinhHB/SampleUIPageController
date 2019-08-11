using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(UIPage))]
public class UIPageEditor : Editor {
	public override void OnInspectorGUI() {
		var script = target as UIPage;
		GUIStyle centerStyle = new GUIStyle();
		centerStyle.alignment = TextAnchor.MiddleCenter;
		GUILayout.Label("------ Editor ------", centerStyle);

		if(GUILayout.Button("Cache child elements")) {
			script.Editor_CacheChildElements();
			MakeTargetDirty();
		}

		if(GUILayout.Button("Reorder elements base on element's index")) {
			script.Editor_ReorderChildElement();
			MakeTargetDirty();
		}

		if(GUILayout.Button("Clear childs")) {
			script.Editor_ClearChilds();
			MakeSceneDirty();
		}

		if(GUILayout.Button("Active all childs")) {
			script.Editor_ActiveAllChilds();
			MakeSceneDirty();
		}

		if(GUILayout.Button("Deactive all childs")) {
			script.Editor_DeactiveAllChilds();
			MakeSceneDirty();
		}

		if(GUILayout.Button("Make me on top")) {
			script.Editor_MakeMeOnTop();
			MakeSceneDirty();
		}

		GUILayout.Space(5);
		GUILayout.Label("------ Use in Play mode ------", centerStyle);
		if(GUILayout.Button("Open me and keep others")) {
			if(EditorApplication.isPlaying) {
				script.Editor_OpenMeAndKeepOthers();
			}
			else {
				Log.Warning(false, "This OpenMe function only work in Play mode");
			}
		}

		if(GUILayout.Button("Close me")) {
			if(EditorApplication.isPlaying) {
				script.Editor_CloseMe();
			}
			else {
				Log.Warning(false, "This CloseMe function only work in Play mode");
			}
		}

		GUILayout.Label("------ Finished Editor ------", centerStyle);
		GUILayout.Space(10);
		base.OnInspectorGUI();
	}

	/// make it dirty for saving
	void MakeTargetDirty() {
		EditorUtility.SetDirty(target);
	}

	/// make scene dirty so it can be save
	void MakeSceneDirty() {
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}
}
