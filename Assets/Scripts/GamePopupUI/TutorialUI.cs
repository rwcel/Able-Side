using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialUI : PopupUI
{
    [SerializeField] Transform tutorialParent;

    private int idx = 0;
    private List<GameObject> tutorialObjs;

    // Awake 단계에서 다 false하기?
    protected override void Awake()
    {
        base.Awake();

        tutorialObjs = new List<GameObject>(tutorialParent.childCount);
        foreach (Transform child in tutorialParent)
        {
            tutorialObjs.Add(child.gameObject);
            child.gameObject.SetActive(false);
        }
    }

    protected override void UpdateData()
    {
        base.UpdateData();

        if (idx > 0)
        {
            tutorialObjs[idx].SetActive(false);
        }
        idx = -1;
        NextTutorial();
    }

    public void NextTutorial()
    {
        if(idx < tutorialParent.childCount - 1)
        {
            if(idx >= 0)
            {
                tutorialObjs[idx].SetActive(false);
            }
            tutorialObjs[++idx].SetActive(true);
        }
        else
        {
            _GamePopup.AllClosePopup(null);
            GameUIManager.Instance.InGamePauseReady(false);
        }
    }
}
