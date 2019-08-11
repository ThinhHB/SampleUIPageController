using UnityEngine;
using System.Collections;

public static class GameObjectUtils {
	public static T GetOrAddComponent<T> (this GameObject obj) where T : Component {
		var result = obj.GetComponent<T>();
		if (result == null) result = obj.AddComponent<T>();
		return result;
	}
}

