using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Popup
{
    public PopupUI ui;
    public System.Action closeAction;

    public Popup(PopupUI ui, System.Action closeAction)
    {
        this.ui = ui;
        this.closeAction = closeAction;
    }
}

public class GamePopup : Singleton<GamePopup>
{
    protected List<PopupUI> popups;          // 딕셔너리?

    public Stack<Popup> popupStack = new Stack<Popup>();


    protected override void AwakeInstance()
    {
        popups = new List<PopupUI>();
        foreach (Transform child in transform)
        {
            popups.Add(child.GetComponent<PopupUI>());
            child.gameObject.SetActive(false);
        }
    }

    protected override void DestroyInstance() { }

    public void OpenPopup(EGamePopup type, Action openAction = null, Action closeAction = null)
    {
        popupStack.Push(new Popup(popups[(int)type], closeAction));
        popups[(int)type].gameObject.SetActive(true);

        // Debug.Log(popupStack.Count);

        openAction?.Invoke();           // 굳이 여기서 필요한가?
    }

    public void ClosePopupNotAction()
    {
        var offPopup = popupStack.Pop();

        offPopup.ui.PlayCloseAnim(null);
    }

    /// <summary>
    /// BG 클릭 시, 꺼지게하기
    /// </summary>
    public void ClosePopup()
    {
        var offPopup = popupStack.Pop();

        //offPopup.obj.SetActive(false);
        offPopup.ui.PlayCloseAnim(offPopup.closeAction);
        // offPopup.closeAction?.Invoke();
    }

    public void AllClosePopup(Action closeAction)
    {
        while(popupStack.Count > 0)
        {
            ClosePopup();
        }

        closeAction?.Invoke();
    }
}
