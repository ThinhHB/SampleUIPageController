using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(UIPageController))]
public class UIPageControllerEditor : Editor {
	public override void OnInspectorGUI() {
		base.OnInspectorGUI();
		var script = target as UIPageController;
		GUILayout.Space(10f);

		if(GUILayout.Button("Cache pages in childrens")) {
			script.Editor_CacheAllUIPageInFirstLevelChild();
			script.Editor_CacheUIPageInSameRoot();
			MakeSceneDirty();
			MakeTargetDirty();
		}

		if(GUILayout.Button("Deactive all page")) {
			script.Editor_DeactiveAllPages();
			MakeSceneDirty();
			MakeTargetDirty();
		}
	}

	/// make it dirty for saving
	void MakeTargetDirty() {
		EditorUtility.SetDirty((target as MonoBehaviour).gameObject);
	}

	/// make scene dirty so it can be save
	void MakeSceneDirty() {
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}
}