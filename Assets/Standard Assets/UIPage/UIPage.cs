#pragma warning disable 0649
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(GraphicRaycaster))]
public class UIPage : MonoBehaviour {
	#region Init
	[Header("---Config all element in This page here")]
	[SerializeField] List<UIElementConfig> elementConfig;

	[Header("---Config the order for Show/Hide")]
	[SerializeField] ShowHideOrderType showOrder;
	[SerializeField] ShowHideOrderType hideOrder;

	[Header("--- Some default config")]
	[SerializeField] bool deactivePageOnHideFinished = true;
	[SerializeField] bool hidePageOnInit = true;

	//---- fields
	/// Will use it to active/deactive interactable on page
	CanvasGroup _cachedCanvasGroup;
	CanvasGroup CachedCanvasGroup {
		get {
			if(_cachedCanvasGroup == null)
				_cachedCanvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
			return _cachedCanvasGroup;
		}
	}

	void OnValidate() {
		Log.Warning(elementConfig != null, this, "Not config any element");
		ValidateElementList();
	}

	void ValidateElementList() {
		if(elementConfig == null) {
			return;
		}
		for(int n = 0; n < elementConfig.Count; n++) {
			var element = elementConfig[n];
			if(element == null) {
				Log.Warning(false, this, "Element at index [{0}] is null !!");
				continue;
			}
			if(element.element == null) {
				Log.Warning(false, this, "Element at index [{0}] has null reference to UIElement !!");
				continue;
			}
		}
	}


#if UNITY_EDITOR
	void Editor_CacheComponents() {
		Editor_CacheChildElements();
		Editor_ReorderChildElement();
	}

	public void Editor_CacheChildElements() {
		Editor_ClearChilds();
		// find all UIElement in children.
		var elements = GetComponentsInChildren<UIElement>(true);
		if(elements.Length == 0) {
			Log.Info(this, "Not found any UIElement");
			return;
		}
		// if element arrays is empty, them create new one
		if(elementConfig == null || (elementConfig != null && elementConfig.Count == 0)) {
			elementConfig = new List<UIElementConfig>(elements.Length);
			FillElementConfigListWithElementArray(elementConfig, elements);
		}
		// element array not empty, so we should keep the old element, just add new element
		else {
			UpdateConfigListWithNewElementFromElementArray(elementConfig, elements);
		}
	}

	public void Editor_ReorderChildElement() {
		if(elementConfig == null ||
		   (elementConfig != null && elementConfig.Count == 0)) {
			Log.Info(this, "Must cache element first");
			return;
		}
		// simple sort
		for(int m = 0; m < elementConfig.Count - 1; m++) {
			for(int n = m + 1; n < elementConfig.Count; n++) {
				if(elementConfig[m].order > elementConfig[n].order) {
					var temp = elementConfig[m];
					elementConfig[m] = elementConfig[n];
					elementConfig[n] = temp;
				}
			}
		}
	}

	public void Editor_ActiveAllChilds() {
		// dont call EditCacheChildElements, it may reset the configs (delayShow,hide ...)
		var elements = GetComponentsInChildren<UIElement>(true);
		for(int n = 0; n < elements.Length; n++) {
			elements[n].gameObject.SetActive(true);
		}
	}

	public void Editor_DeactiveAllChilds() {
		// dont call EditCacheChildElements, it may reset the configs (delayShow,hide ...)
		var elements = GetComponentsInChildren<UIElement>(true);
		for(int n = 0; n < elements.Length; n++) {
			elements[n].gameObject.SetActive(false);
		}
	}

	public void Editor_ClearChilds() {
		if(elementConfig != null)
			elementConfig.Clear();
	}

	public void Editor_MakeMeOnTop() {
		transform.SetAsLastSibling();
	}

	public void Editor_OpenMeAndKeepOthers() {
		if(controller != null) {
			controller.RequestKeepCurrentAndOpenNewPage(this);
		}
	}

	public void Editor_CloseMe() {
		if(controller != null) {
			controller.RequestCloseCurrentPageOnly(this);
		}
	}

	void FillElementConfigListWithElementArray(List<UIElementConfig> list, UIElement[] array) {
		for(int m = 0; m < array.Length; m++) {
			var cfg = CreateConfig(array[m]);
			list.Add(cfg);
		}
	}

	void UpdateConfigListWithNewElementFromElementArray(List<UIElementConfig> list, UIElement[] array) {
		for(int n = 0; n < array.Length; n++) {
			var element = array[n];
			// check duplicate
			bool duplicate = false;
			for(int m = 0, amount = list.Count; m < amount; m++) {
				var check = list[m];
				if(check.element == element)
					duplicate = true;
			}
			// not duplicate, then add to list
			if(!duplicate)
				list.Add(CreateConfig(element));
		}
	}

	UIElementConfig CreateConfig(UIElement element) {
		var cfg = new UIElementConfig();
		cfg.element = element;
		// let default time
		cfg.delayShowNextElement = cfg.element.GetShowDuration();
		cfg.delayHideNextElement = cfg.element.GetHideDuration();
		return cfg;
	}
#endif//UNITY_EDITOR
	#endregion//Init

	#region Public
	///<summary> Will be set from UIPageManager</summary>
	[HideInInspector] public UIPageController controller;

	/// <summary>Will be invoke from controller, do some init tasks.
	/// Why dont we do on Awake() ? We often deactive page, then the Awake
	/// will not be call, PageManager will call this, even if the Page is deactivated</summary>
	public void Init() {
		OnInitPageHandler.Invoke();
		// at begin of the scene, we should deactive all pages
		if(hidePageOnInit)
			HideWithoutAnimation();
	}

	///<summary> Call from UIPageManager, when returning from other page</summary>
	public void OnFocus() {
		// default is active interactable when Focused
		SetActiveInteractable(true);
		OnFocusHandler.Invoke();
	}

	///<summary> Call from UIPageManager, when other page show up and cover it</summary>
	public void OnLostFocus() {
		// default is deacive interactable when Lost Focused
		SetActiveInteractable(false);
		OnLostFocusHandler.Invoke();
	}

	/// <summary> Let PageManager call this, because it need to handle
	/// the openPageStack. Use UI_RequestOpenSelf instead </summary>
	public void Show(float delay = 0) {
		// gameobject must be actived to start a coroutine
		gameObject.SetActive(true);

		// deactive all child first, at begin the show, all element will be deactive
		// , the active() task will be done on each UIElement.SHow()
		SetActiveAllChild(false);
		StartCoroutine(IE_ShowElements(delay));
		// RULE : page is un-interactable untils finishing all transition
		SetActiveInteractable(false);
	}

	/// <summary> Let PageManager call this, because it need to handle
	/// the openPageStack. Use UI_RequestCloseSelf instead </summary>
	public void Hide(float delay = 0) {
		// gameobject must be actived to start a corountine
		gameObject.SetActive(true);
		// RULE : page is un-interactable untils finishing all transition
		SetActiveInteractable(false);
		// for Hide, all child must actived at begin of Hide()
		SetActiveAllChild(true);
		StartCoroutine(IE_HideElements(delay));
		// RULE : page is un-interactable untils finishing all transition
		SetActiveInteractable(false);
	}

	/// <summary> Simple active all element withoud FadeIn aimation </summary>
	public void ShowWithoutAnimation() {
		SetActiveAllChild(true);
		SetActiveInteractable(true);
	}

	/// <summary> Simple deactive all element withoud FadeIn aimation </summary>
	public void HideWithoutAnimation() {
		SetActiveAllChild(false);
		SetActiveInteractable(false);
	}
	#endregion// Public

	#region Events
	[Header("--- InitPage, by PageManager, call once, even if page disabled")]
	///<summary> Will be call from PageManager, only once, at pageManager.Awake</summary>
	public UnityEvent OnInitPageHandler;

	[Header("--- Focus/LostFocus, by PageManager, when transition between pages")]
	///<summary> Will be call when returning from another page</summary>
	public UnityEvent OnFocusHandler;
	///<summary> Will be call when another page open and cover this page</summary>
	public UnityEvent OnLostFocusHandler;


	[Header("--- Event at Transition start")]
	///<summary> Invoke right before transition elements. Use this to disable Touch on page or something</summary>
	public UnityEvent OnShowStart;
	///<summary> Invoke right before transition elements. Use this to disable Touch on page or something</summary>
	public UnityEvent OnHideStart;

	[Header("--- Event at Transition finished")]
	///<summary> Invoked when all element in this page was finished Show up</summary>
	public UnityEvent OnShowFinshed;
	///<summary> Invoked when all element in this page was finished Hide out</summary>
	public UnityEvent OnHideFinished;
	#endregion// Events

	#region UI Request
	public void UI_CloseSelfAndOpenPage(UIPage nextPage) {
		if(controller == null) {
			Log.Warning(false, this, "UI_CloseSelfAndOpenPage, Missing controller");
			return;
		}
		controller.RequestCloseCurrentAndOpenNewPage(nextPage);
	}

	public void UI_KeepSelfAndOpenPage(UIPage nextPage) {
		if(controller == null) {
			Log.Warning(false, this, "UI_KeepSelfAndOpenPage, Missing controller");
			return;
		}
		controller.RequestKeepCurrentAndOpenNewPage(nextPage);
	}

	public void UI_CloseSelfOnly() {
		if(controller == null) {
			Log.Warning(false, this, "UI_CloseSelfOnly, Missing controller");
			return;
		}
		controller.RequestCloseCurrentPageOnly(this);
	}

	public void UI_ForceCloseSelf() {
		if(controller == null) {
			Log.Warning(false, this, "UI_CloseSelfOnly, Missing controller");
			return;
		}
		controller.RequestForceClosePage(this);
	}

	public void UI_DeactiveInteractable() {
		SetActiveInteractable(false);
	}
	#endregion//UI Request

	#region Private
	void SetActiveAllChild(bool active) {
		for(int m = 0; m < elementConfig.Count; m++) {
			elementConfig[m].element.gameObject.SetActive(active);
		}
	}

	IEnumerator IE_ShowElements(float delay) {
		OnShowStart.Invoke();
		if(delay > 0)
			yield return new WaitForSeconds(delay);
		UIElement lastElement = null;

		// if no element in the list, then nothing to do
		if(elementConfig.Count > 0) {
			switch(showOrder) {
				case ShowHideOrderType.Normal:
					// loop, exclude last element
					for(int m = 0, amount = elementConfig.Count - 1; m < amount; m++) {
						var child = elementConfig[m];
						child.element.Show();
						if(child.delayShowNextElement > 0f)
							yield return new WaitForSeconds(child.delayShowNextElement);
					}
					// last element, dont use delayShowNext, just save lastElement
					lastElement = elementConfig[elementConfig.Count - 1].element;
					break;

				case ShowHideOrderType.Revert:
					// loop, exclude last element
					for(int m = elementConfig.Count - 1; m > 0; m--) {
						var child = elementConfig[m];
						child.element.Show();
						if(child.delayShowNextElement > 0f)
							yield return new WaitForSeconds(child.delayShowNextElement);
					}
					// last element, dont use delayShowNext, just save lastElement
					lastElement = elementConfig[0].element;
					break;
			}

			// wait for last element Show, then invoke OnShowFinished
			lastElement.Show();
			yield return new WaitForSeconds(lastElement.GetShowDuration());
		}
		else {
			Log.Info(this, "No element in the page ");
		}
		OnShowFinshed.Invoke();

		// RULE : page is un-interactable untils finishing all transitions
		SetActiveInteractable(true);
	}

	IEnumerator IE_HideElements(float delay) {
		OnHideStart.Invoke();
		if(delay > 0)
			yield return new WaitForSeconds(delay);
		UIElement lastElement = null;

		// if no element in the list, then nothing to do
		if(elementConfig.Count > 0) {
			switch(hideOrder) {
				case ShowHideOrderType.Normal:
					// loop, exclude last element
					for(int m = 0, amount = elementConfig.Count - 1; m < amount; m++) {
						var child = elementConfig[m];
						child.element.Hide();
						if(child.delayHideNextElement > 0f)
							yield return new WaitForSeconds(child.delayHideNextElement);
					}
					// last element, dont use delayHideNext, just save lastElement
					lastElement = elementConfig[elementConfig.Count - 1].element;
					break;

				case ShowHideOrderType.Revert:
					// loop, exclude last element
					for(int m = elementConfig.Count - 1; m > 0; m--) {
						var child = elementConfig[m];
						// Revert Hide() order, delay first, Hide later
						child.element.Hide();
						if(child.delayHideNextElement > 0f)
							yield return new WaitForSeconds(child.delayHideNextElement);
					}
					// last element, dont use delayHideNext, just save lastElement
					lastElement = elementConfig[0].element;
					break;
			}

			// wait for last element Hide, then invoke OnHideFinished
			lastElement.Hide();
			yield return new WaitForSeconds(lastElement.GetHideDuration());
		}
		else {
			Log.Info(this, "No element in the page ");
		}
		OnHideFinished.Invoke();
		CheckingDeactivePageOnHideFinished();
	}

	void CheckingDeactivePageOnHideFinished() {
		if(deactivePageOnHideFinished)
			gameObject.SetActive(false);
	}

	///<summary> active/deactive the interactable and blockRaycast on CanvasGroup</summary>
	void SetActiveInteractable(bool active) {
		/// UPDATE : we have to disable both interactable and blocksRaycast on CanvasGroup to fully disable input
		/// on this Canvas, if only interactable be disable, object use EventTrigger component still receive 
		/// pointerDown/Up ... event
		CachedCanvasGroup.interactable = active;
		CachedCanvasGroup.blocksRaycasts = active;
	}
	#endregion // Private

	#region Private - cache show,hide duration
	///<summary> To mark that we alreay dy count the show/hide duration</summary>
	bool _cacheShowHideDuration = false;
	///<summary> the time from start open page to finished open all element</summary>
	float _showDuration;
	///<summary> the time from start close page to finished hide all element</summary>
	float _hideDuration;

	/// <summary> The duration for all UIElement to finished their Showing animation</summary>
	public float ShowDuration {
		get {
			CacheShowHideDuration();
			return _showDuration;
		}
	}

	/// <summary> The duration for all UIElement to finished their Hiding animation</summary>
	public float HideDuration {
		get {
			CacheShowHideDuration();
			return _hideDuration;
		}
	}

	void CacheShowHideDuration() {
		if(_cacheShowHideDuration)
			return;
		_showDuration = 0f;
		_hideDuration = 0f;
		// calculate Show duration first
		switch(showOrder) {
			case ShowHideOrderType.Normal:
				// count all the "Delay.." time, ignore the last element
				for(int n = 0, amount = elementConfig.Count - 1; n < amount; n++) {
					_showDuration += elementConfig[n].delayShowNextElement;
				}
				// plus the show duration of the last element
				_showDuration += elementConfig[elementConfig.Count - 1].element.GetShowDuration();
				break;

			case ShowHideOrderType.Revert:
				// count all the "Delay.." time, ignore the first element
				for(int n = elementConfig.Count - 1; n > 0; n--) {
					_showDuration += elementConfig[n].delayShowNextElement;
				}
				// plus the show duration of the first element
				_showDuration += elementConfig[0].element.GetShowDuration();
				break;
		}
		// Hide duration
		switch(hideOrder) {
			case ShowHideOrderType.Normal:
				// count all the "Delay.." time, ignore the last element
				for(int n = 0, amount = elementConfig.Count - 1; n < amount; n++) {
					_hideDuration += elementConfig[n].delayHideNextElement;
				}
				// plus the show duration of the last element
				_hideDuration += elementConfig[elementConfig.Count - 1].element.GetHideDuration();
				break;

			case ShowHideOrderType.Revert:
				// count all the "Delay.." time, ignore the first element
				for(int n = elementConfig.Count - 1; n > 0; n--) {
					_hideDuration += elementConfig[n].delayHideNextElement;
				}
				// plus the show duration of the first element
				_hideDuration += elementConfig[0].element.GetHideDuration();
				break;
		}
		// turn on this flag, next time call this func wont be call
		_cacheShowHideDuration = true;
	}
	#endregion//Private - cache show,hide duration

	#region Classes
	[System.Serializable]
	class UIElementConfig {
		public UIElement element;
		[Tooltip("Delay time to show next lement")]
		public float delayShowNextElement;
		[Tooltip("Delay time to hide next lement")]
		public float delayHideNextElement;

#if UNITY_EDITOR
		[Tooltip("The order of this element, use in editor only")]
		public int order;
#endif
	}

	enum ShowHideOrderType {
		Normal,
		Revert,
	}
	#endregion //Classes
}