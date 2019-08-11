using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using System;

/// <summary>
/// Control group of UIPage object. Often put it in the mainCanvas, it need to hold reference to all sub-pages.
/// Receive request OpenPage, ClosePage ... then open, close page in sub-pages follow pageName ...
/// </summary>
public class UIPageController : MonoBehaviour {
	#region Init
	/// <summary> The list of all sub-page. Will be cache via Editor-CacheAllUIPage() </summary>
	[Header("PageList, log")]
	[SerializeField] List<UIPage> pageList;
	/// <summary> Use for debugging, set to True, it will log message for every Request (openPage, close page ...).
	/// Better set it to false in Release build for better performance. </summary>
	[SerializeField] bool logOpenClosePageProcess = false;

	[Header("First page")]
	[SerializeField] bool openFirstPageByDefault = false;
	[SerializeField] UIPage firstPage = null;
	[SerializeField] float delayOpenFirstPage = 0.5f;

	void OnValidate() {
		Editor_CacheAllUIPageInFirstLevelChild();
		Editor_CacheUIPageInSameRoot();
	}

	void Start() {
		// wait at least 1 frame, make sure all action relate to UserData has done.
		// cause we will call pages.Init, they may do some action need userData
		Observable.TimerFrame(1).Subscribe(_ => {
			ConfigReferenceForPageListAndInitPages();
			OpenFirstPage();
		});
	}

	void ConfigReferenceForPageListAndInitPages() {
		// set reference
		for(int m = 0; m < pageList.Count; m++) {
			pageList[m].controller = this;
		}
		/// call init. Why cost one more for loop? dont do it on above for loop?
		/// This to make sure all page are hold manager reference, before Init
		/// was called.
		for(int m = 0; m < pageList.Count; m++) {
			pageList[m].Init();
		}
	}

	void OpenFirstPage() {
		if(openFirstPageByDefault) {
			if(firstPage != null) {
				Observable.Timer(TimeSpan.FromSeconds(delayOpenFirstPage)).Subscribe(_ => {
					RequestKeepCurrentAndOpenNewPage(firstPage);
				});
			}
			else {
				Log.Warning(false, this, "Request openDefault page but firstpage = null");
			}
		}
	}
	#endregion// Init


#if UNITY_EDITOR
	void CheckToCacheChildPagesAndSameRootPage() {
		if(pageList == null || (pageList != null && pageList.Count == 0)) {
			Editor_CacheAllUIPageInFirstLevelChild();
			Editor_CacheUIPageInSameRoot();
		}
	}

	/// <summary> Call via Editor script, to cache all UIPage component.
	/// Notes : currently, we cache only UIPage in first-level-child, the UIPage in deep child level
	/// wont be cached (some cases, we will need another UIPageController in deep child, let them control
	/// their child UIPage) </summary>
	public void Editor_CacheAllUIPageInFirstLevelChild() {
		pageList = new List<UIPage>();
		for(int childIndex = 0, childCount = transform.childCount; childIndex < childCount; childIndex++) {
			var child = transform.GetChild(childIndex);
			var pageComponent = child.GetComponent<UIPage>();
			if(pageComponent != null)
				pageList.Add(pageComponent);
		}
	}

	public void Editor_CacheUIPageInSameRoot() {
		var parent = transform.parent;
		if(parent == null)
			return;
		for(int n = 0, amount = parent.childCount; n < amount; n++) {
			var sameRoot = parent.GetChild(n);
			// its me, dont need to check
			if(sameRoot == transform)
				continue;
			var pageComponent = sameRoot.gameObject.GetComponent<UIPage>();
			if(pageComponent != null)
				pageList.Add(pageComponent);
		}
	}

	public void Editor_DeactiveAllPages() {
		CheckToCacheChildPagesAndSameRootPage();
		for(int pageIndex = 0; pageIndex < pageList.Count; pageIndex++) {
			pageList[pageIndex].Editor_DeactiveAllChilds();
		}
	}
#endif //UNITY_EDITOR


	#region Public
	///<summary> Store opened pages. Use this to call OnFocus, LoseFocus on each page.
	/// Show rule is : last-open-first-close </summary>
	LinkedList<UIPage> openPageList = new LinkedList<UIPage>();

	/// <summary> Keep the current open page, call LostFocus on currentPage,
	/// and open the new page with input pageName </summary>
	//public void RequestKeepCurrentAndOpenNewPage(string pageName) {
	public void RequestKeepCurrentAndOpenNewPage(UIPage page) {
		if(page == null) {
			Log.Warning(false, this, "KeepCurrentAndOpenNewPage, with empty pageName", this);
			return;
		}
		ValidatePageExistInPagelist(page);
		// call Lost focus on current top page
		if(openPageList.Count > 0) {
			var currentTopPage = openPageList.Last.Value;
			currentTopPage.OnLostFocus();
		}
		// TODO : open pageList > 0, check if the request page already showup
		else {
			if(openPageList.Contains(page)) {
				Log.Warning(false, this, "KeepCurrentAndOpenNewPage, Page [{0}] already showup", page);
				return;
			}
		}
		// let the new page on top
		page.transform.SetAsLastSibling();
		// push to stack and Show
		page.Show();
		openPageList.AddLast(page);
		// logging
		const string FORMAT = "KeepCurrentAndOpenNewPage : {0}";
		LogOpenClosePageProcessToConsole(FORMAT, page.name);
	}


	/// <summary> Close the top open page (currentPage) and open the new page (with pageName) </summary>
	public void RequestCloseCurrentAndOpenNewPage(UIPage page) {
		if(page == null) {
			Log.Warning(false, this, "CloseCurrentAndOpenNewPage, with empty pageName", this);
			return;
		}
		ValidatePageExistInPagelist(page);

		// close current page, popup it out of the openPageList too
		UIPage topPage = null;
		if(openPageList.Count > 0) {
			topPage = openPageList.Last.Value;
			topPage.Hide();
			openPageList.RemoveLast();
		}
		else {
			Log.Warning(false, this, "OpenPageStack is empty but request CloseCurrentAndOpenNewPage");
		}
		// let the new page on top render, also add it to last in openPageStack
		page.transform.SetAsLastSibling();
		openPageList.AddLast(page);
		// if currentPage = null, then just simple show new page
		if(topPage == null) {
			page.Show();
		}
		// else, wait a little for currentPage to Hide, then show newPage
		else {
			Observable.Timer(TimeSpan.FromSeconds(topPage.HideDuration)).Subscribe(_ => page.Show()).AddTo(_destroyDispose);
		}
		// logging
		const string FORMAT = "CloseCurrentAndOpenNewPage : {0}";
		LogOpenClosePageProcessToConsole(FORMAT, page.name);
	}


	/// <summary> Close the top page, call OnFocus on previous page (if any) </summary>
	public void RequestCloseCurrentPageOnly(UIPage page) {
		if(page == null) {
			Log.Warning(false, this, "CloseCurrentPageOnly, with null page", this);
			return;
		}
		ValidatePageExistInPagelist(page);
		if(openPageList.Count == 0) {
			Log.Warning(false, this, "CloseCurrentPageOnly, but openStack is empty");
			return;
		}

		var topPage = openPageList.Last.Value;
		if(topPage != page) {
			Log.Warning(false, this, "CloseCurrentPageOnly [{0}], but it not the current topPage", page.name);
			return;
		}
		// close the top page, pop it out of stack
		topPage.Hide();
		openPageList.RemoveLast();
		// call OnFocus for preivous page
		if(openPageList.Count > 0) {
			var previousPage = openPageList.Last.Value;
			var currentPageHideTime = topPage.HideDuration;
			Observable.Timer(TimeSpan.FromSeconds(currentPageHideTime)).Subscribe(_ => previousPage.OnFocus()).AddTo(_destroyDispose);
		}
		// logging
		const string FORMAT = "RequestCloseCurrentPageOnly : {0}";
		LogOpenClosePageProcessToConsole(FORMAT, page.name);
	}


	/// <summary> Close the page event if it not on top </summary>
	public void RequestForceClosePage(UIPage page) {
		if(page == null) {
			Log.Warning(false, this, "ForceLosePage, with null page", this);
			return;
		}
		ValidatePageExistInPagelist(page);

		if(openPageList.Count == 0) {
			Log.Warning(false, this, "ForceLosePage, but openStack is empty");
			return;
		}
		// if the request close page also the top page, then this is the same case with RequestCloseCurrentPageOnly
		if(page == openPageList.Last.Value) {
			page.Hide();
			openPageList.RemoveLast();
			// empty ? then quit
			if(openPageList.Count == 0) {
				return;
			}
			// call Focus for previous page
			var previousPage = openPageList.Last.Value;
			var currentPageHideTime = page.HideDuration;
			Observable.Timer(TimeSpan.FromSeconds(currentPageHideTime)).Subscribe(_ => previousPage.OnFocus()).AddTo(_destroyDispose);
			return;
		}
		// requestClosePage in the middle of stack, then close it, no OnFocus will be call on any page
		if(openPageList.Contains(page)) {
			openPageList.Remove(page);
			page.Hide();
		}
		else {
			Log.Warning(false, this, "RequestForceLosePage [{0}], but not contain in OpenPageList", page);
			return;
		}
		// logging
		const string FORMAT = "RequestForceLosePage : {0}";
		LogOpenClosePageProcessToConsole(FORMAT, page.name);
	}
	#endregion//Public


	#region Private
	bool IsExistInPageList(UIPage page) {
		for(int i = 0; i < pageList.Count; i++) {
			if(pageList[i] == page)
				return true;
		}
		return false;
	}

	void ValidatePageExistInPagelist(UIPage page) {
		if(!IsExistInPageList(page)) {
			Log.Info(this, $"CloseCurrentAndOpenNewPage, page [{page.name}] not exist in pageList. Its ok, but the Init() on that page wont be invoked");
		}
	}

	void LogOpenClosePageProcessToConsole(string format, string para) {
		if(logOpenClosePageProcess) {
			Log.Info(this, format, para);
		}
	}

	CompositeDisposable _destroyDispose = new CompositeDisposable();
	void OnDestroy() {
		_destroyDispose.Clear();
	}
	#endregion//Private
}