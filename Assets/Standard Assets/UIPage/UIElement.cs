using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(UIElementTransition))]
public class UIElement : MonoBehaviour {
	#region Config
	UIElementTransition _cacheTranstion = null;
	UIElementTransition CacheTransition {
		get {
			if(_cacheTranstion == null)
				_cacheTranstion = GetComponent<UIElementTransition>();
			return _cacheTranstion;
		}
	}
	#endregion// Config


	#region Public
	public void Show() {
		CacheTransition.Show();
		OnShowEvent.Invoke();
		// invoke finished event
		var showDuration = CacheTransition.GetShowDuration();
		if(showDuration > 0f) {
			LeanTween.delayedCall(showDuration, _ => OnShowFinishedEvent.Invoke());
		}
		else {
			OnShowFinishedEvent.Invoke();
		}
	}

	public void Hide() {
		CacheTransition.Hide();
		OnHideEvent.Invoke();
		// check to deactive gameobject
		if(CacheTransition.IsEmptyHideTransition())
			gameObject.SetActive(false);
		// invoke finished event
		var hideDuration = CacheTransition.GetHideDuration();
		if(hideDuration > 0f) {
			LeanTween.delayedCall(hideDuration, _ => OnHideFinishedEvent.Invoke());
		}
		else {
			OnHideFinishedEvent.Invoke();
		}
	}

	public float GetShowDuration() {
		return CacheTransition.GetShowDuration();
	}

	public float GetHideDuration() {
		return CacheTransition.GetHideDuration();
	}
	#endregion//Public


	#region Events
	[Header("Event on begin transition")]
	public UnityEvent OnShowEvent;
	/// Use this event to enable all Render, animation relate to this UI element
	public UnityEvent OnHideEvent;
	[Header("Event on finished transition")]
	public UnityEvent OnShowFinishedEvent;
	public UnityEvent OnHideFinishedEvent;
	#endregion// Events
}