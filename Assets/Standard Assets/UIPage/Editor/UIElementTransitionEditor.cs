using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UIElementTransition))]
public class UIElementTransitionEditor : Editor {
	public override void OnInspectorGUI() {
		base.OnInspectorGUI();
		var script = target as UIElementTransition;

		if(GUILayout.Button("Make Hide same with Show")) {
			script.Editor_MakeHideSameTranstionsWithShow();
			MakeTargetDirty();
		}

		if(GUILayout.Button("Make Hide Revert with Show")) {
			script.Editor_MakeHideRevertTranstionsWithShow();
			MakeTargetDirty();
		}

		if(GUILayout.Button("Test Show")) {
			script.Editor_TestShow();
			MakeTargetDirty();
		}
	}

	/// make it dirty for saving
	void MakeTargetDirty() {
		EditorUtility.SetDirty(target);
	}
}