using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class OutGameUI : MonoBehaviour
{
    private struct FDockInfo
    {
        public Button button;
        //public GameObject spotlight;
        //public GameObject textObj;
        public Image iconImage;
        public List<GameObject> onoffObjs;
        public LayoutElement layoutElement;
    }

    //[SerializeField] ShopUI shopUI;
    //[SerializeField] RankUI rankUI;
    //[SerializeField] HomeUI homeUI;
    //[SerializeField] MailUI mailUI;
    //[SerializeField] u optionUI;

    [SerializeField] DockUI[] dockUIs;

    [SerializeField] GameObject[] dockObjs;
    [SerializeField] float selectFlexble = 2f;
    [SerializeField] Color disselectColor;

    private FDockInfo[] dockInfos;

    private Animator anim;

    private int selected;
    public EDock CurDock => (EDock)selected;

    private static readonly int[] _Anim_Dock =
    {
        Animator.StringToHash(EDock.Shop.ToString()),
        Animator.StringToHash(EDock.Rank.ToString()),
        Animator.StringToHash(EDock.Home.ToString()),
        Animator.StringToHash(EDock.Mail.ToString()),
        Animator.StringToHash(EDock.Option.ToString())
    };

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void OnStart()
    {
        selected = 0;
        dockInfos = new FDockInfo[dockObjs.Length];

        for (int i = 0, length = dockInfos.Length; i < length; i++)
        {
            int num = i;
            dockInfos[i].onoffObjs = new List<GameObject>();
            dockInfos[i].button = dockObjs[i].GetComponent<Button>();
            dockInfos[i].onoffObjs.Add(dockObjs[i].GetComponentInChildren<TextMeshProUGUI>().gameObject);
            dockInfos[i].onoffObjs.Add(dockObjs[i].transform.GetChild(1).gameObject);
            dockInfos[i].iconImage = dockObjs[i].transform.GetChild(0).GetComponentInChildren<Image>();   // *Hierarchy 변경되면 수정되어야함
            dockInfos[i].layoutElement = dockObjs[i].GetComponent<LayoutElement>();
            //dockInfos[i].textObj = dockObjs[i].GetComponentInChildren<TextMeshProUGUI>().gameObject;
            //dockInfos[i].spotlight = dockObjs[i].transform.GetChild(1).gameObject;                                      // *Hierarchy 변경되면 수정되어야함

            // 초기화 작업
            dockInfos[i].button.onClick.AddListener(() => OnClickButton(num));
            dockInfos[i].iconImage.color = disselectColor;
            dockInfos[i].layoutElement.flexibleWidth = 1;
            foreach (var onoffObj in dockInfos[i].onoffObjs)
            {
                onoffObj.SetActive(false);
            }
        }

        OnClickButton((int)EDock.Home);

        //shopUI.OnStart();
        //rankUI.OnStart();
        //homeUI.OnStart();
        //mailUI.OnStart();
        //optionUI.OnStart();
        foreach (var dockUI in dockUIs)
        {
            dockUI.OnStart();
        }
    }

    public void MoveDock(EDock dock)
    {
        GamePopup.Instance.AllClosePopup(() => OnClickButton((int)dock));
    }

    public void OnClickButton(int num)
    {
        AudioManager.Instance.PlaySFX(ESFX.Dock);

        dockInfos[selected].layoutElement.flexibleWidth = 1;
        dockInfos[selected].iconImage.color = disselectColor;
        foreach (var onoffObj in dockInfos[selected].onoffObjs)
        {
            onoffObj.SetActive(false);
        }

        selected = num;

        dockInfos[selected].layoutElement.flexibleWidth = selectFlexble;
        dockInfos[selected].iconImage.color = Color.white;
        foreach (var onoffObj in dockInfos[selected].onoffObjs)
        {
            onoffObj.SetActive(true);
        }

        dockUIs[num].UpdateDatas();

        anim.SetTrigger(_Anim_Dock[num]);
    }


    // Mail
    public void SerialPost(FPostInfo postInfo)
    {
        var mailUI = dockUIs[(int)EDock.Mail] as MailUI;
        mailUI.SerialPost(postInfo);
    }
}
