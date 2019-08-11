using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Config all transition for UIElement in this component
/// </summary>
public class UIElementTransition : MonoBehaviour {
	#region Init, config
	[SerializeField] TransitionConfig[] showTransitions;
	[SerializeField] TransitionConfig[] hideTranstions;
	///<summary> The default transform of element, save some info at begin</summary>
	LocalTransformInfo _defaultTrans = new LocalTransformInfo();

	void Awake() {
		_defaultTrans.CacheInfo(transform);
		CacheDurationTime();
	}

	bool _isCachedDurationTime = false;
	///<summary> The longest show transition</summary>
	float _showDuration = 0;
	///<summary> The longest hide transition</summary>
	float _hideDuration = 0;

	void CacheDurationTime() {
		/// if already cached, then do nothing
		/// Make sure this func only run 1time
		if(_isCachedDurationTime) return;
		// cache show duration
		for(int n = 0; n < showTransitions.Length; n++) {
			var trans = showTransitions[n];
			if(trans.duration > _showDuration) _showDuration = trans.duration;
		}
		// cache hide duration
		for(int n = 0; n < hideTranstions.Length; n++) {
			var trans = hideTranstions[n];
			if(trans.duration > _hideDuration) _hideDuration = trans.duration;
		}
	}


	CanvasGroup _cachedCanvasGroup;
	CanvasGroup CachedCanvasGroup {
		get {
			if(_cachedCanvasGroup == null) _cachedCanvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
			return _cachedCanvasGroup;
		}
	}
	#endregion//Init, config

	#region Editor
	public void Editor_MakeHideSameTranstionsWithShow() {
		hideTranstions = new TransitionConfig[showTransitions.Length];
		for(int n = 0; n < hideTranstions.Length; n++) {
			var showCfg = showTransitions[n];
			hideTranstions[n] = TransitionConfig.Clone(showCfg);
		}
	}

	public void Editor_MakeHideRevertTranstionsWithShow() {
		hideTranstions = new TransitionConfig[showTransitions.Length];
		for(int n = 0; n < hideTranstions.Length; n++) {
			var showCfg = showTransitions[n];
			hideTranstions[n] = TransitionConfig.CloneAndRevertBeginEnd(showCfg);
		}
	}

	public void Editor_TestShow() {
//		Show();
	}
	#endregion//Editor

	#region Public
	public void Show() {
		// alway active gameobject then run the animatino
		gameObject.SetActive(true);
		Play(showTransitions);
	}

	public void Hide() {
		// alway active gameobject then run the animatino
		gameObject.SetActive(true);
		Play(hideTranstions);
	}

	public float GetShowDuration() {
		CacheDurationTime();
		return _showDuration;
	}

	public float GetHideDuration() {
		CacheDurationTime();
		return _hideDuration;
	}

	public bool IsEmptyHideTransition() {
		return hideTranstions.Length == 0;
	}
	#endregion//Public

	#region Private
	void Play(TransitionConfig[] configs) {
		for(int n = 0; n < configs.Length; n++) {
			var cfg = configs[n];
			switch(cfg.transitionType) {
				case UITransitionType.MoveX:
					MoveX(cfg, _defaultTrans);
					break;

				case UITransitionType.MoveY:
					MoveY(cfg, _defaultTrans);
					break;

				case UITransitionType.RotateZ:
					RotateZ(cfg, _defaultTrans);
					break;

				case UITransitionType.Zoom:
					Zoom(cfg, _defaultTrans);
					break;

				case UITransitionType.Fade:
					Fade(cfg, _defaultTrans);
					break;

				default:
					// reach here, mean not implement for the TransitionType
					Log.Warning(false, "Not implement transition type : {0}", cfg.transitionType);
					break;
			}
		}
	}

	void MoveX(TransitionConfig cfg, LocalTransformInfo defaultTransform) {
		var beginPoint = defaultTransform.position;
		beginPoint.x += cfg.begin;

		var endPoint = defaultTransform.position;
		endPoint.x += cfg.end;

		transform.localPosition = beginPoint;
		LeanTween.moveLocal(gameObject, endPoint, cfg.duration).setEase(cfg.easing);
	}

	void MoveY(TransitionConfig cfg, LocalTransformInfo defaultTransform) {
		var beginPoint = defaultTransform.position;
		beginPoint.y += cfg.begin;

		var endPoint = defaultTransform.position;
		endPoint.y += cfg.end;

		transform.localPosition = beginPoint;
		LeanTween.moveLocal(gameObject, endPoint, cfg.duration).setEase(cfg.easing);
	}

	void RotateZ(TransitionConfig cfg, LocalTransformInfo defaultTransform) {
		var beginAngle = defaultTransform.eulerRotation;
		beginAngle.z += cfg.begin;

		var endAngle = defaultTransform.eulerRotation;
		endAngle.z += cfg.end;

		transform.localEulerAngles = beginAngle;
		LeanTween.rotateLocal(gameObject, endAngle, cfg.duration).setEase(cfg.easing);
	}

	void Zoom(TransitionConfig cfg, LocalTransformInfo defaultTransform) {
		var beginScale = defaultTransform.scale;
		beginScale.x = cfg.begin;
		beginScale.y = cfg.begin;

		var endScale = defaultTransform.scale;
		endScale.x = cfg.end;
		endScale.y = cfg.end;

		transform.localScale = beginScale;
		LeanTween.scale(gameObject, endScale, cfg.duration).setEase(cfg.easing);
	}

	void Fade(TransitionConfig cfg, LocalTransformInfo defaultTransform) {
		var fadeFrom = cfg.begin;
		var fadeTo = cfg.end;

		CachedCanvasGroup.alpha = fadeFrom;
		LeanTween.alphaCanvas(CachedCanvasGroup, fadeTo, cfg.duration).setEase(cfg.easing);
	}
	#endregion// Private


	#region Classes
	[System.Serializable]
	public class TransitionConfig {
		public UITransitionType transitionType;
		public float begin;
		public float end;
		public float duration;
		public LeanTweenType easing = LeanTweenType.easeOutQuad;

		public static TransitionConfig Clone(TransitionConfig source) {
			if(source == null) {
				throw new NullReferenceException();
			}
			var clone = new TransitionConfig();
			clone.transitionType = source.transitionType;
			clone.begin = source.begin;
			clone.end = source.end;
			clone.duration = source.duration;
			clone.easing = source.easing;
			return clone;
		}

		public static TransitionConfig CloneAndRevertBeginEnd(TransitionConfig source) {
			if(source == null) {
				throw new NullReferenceException();
			}
			var clone = new TransitionConfig();
			clone.transitionType = source.transitionType;
			clone.begin = source.end;
			clone.end = source.begin;
			clone.duration = source.duration;
			clone.easing = source.easing;
			return clone;
		}
	}

	/// Store some info of transform for using later
	class LocalTransformInfo {
		public Vector3 position;
		public Vector3 eulerRotation;
		public Vector3 scale;

		public void CacheInfo(Transform trans) {
			position = trans.localPosition;
			eulerRotation = trans.localEulerAngles;
			scale = trans.localScale;
		}
	}
	#endregion//Classes
}