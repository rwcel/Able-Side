using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuideUI : PopupUI
{
    [System.Serializable]
    private struct FGuide
    {   
        // LangNum
        public int titleNum;
        public int contentsNum;
        public Sprite image;
    }

    [Header("데이터")]
    [SerializeField] List<FGuide> guides;
    [SerializeField] GameObject dotObj;
    [SerializeField] Image guideImage;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI contentsText;

    [Header("버튼")]
    [SerializeField] Button leftButton;
    [SerializeField] Button rightButton;
    [SerializeField] Button okButton;

    private List<Image> dotImages;
    private int curNum;

    protected override void Awake()
    {
        base.Awake();

        curNum = 0;

        // Dot 생성
        dotImages = new List<Image>(guides.Count);

        var baseDotImg = dotObj.GetComponent<Image>();
        baseDotImg.color = Color.gray;
        dotImages.Add(baseDotImg);

        var parent = dotObj.transform.parent;
        for (int i = 1, length = guides.Count; i < length; i++)     // 1개 위에서 처리
        {
            var dotImage = Instantiate(dotObj, parent).GetComponent<Image>();
            dotImage.color = Color.gray;
            dotImages.Add(dotImage);
        }
    }

    protected override void Start()
    {
        base.Start();

        leftButton.onClick.AddListener(() => ShowGuide(-1));
        rightButton.onClick.AddListener(() => ShowGuide(1));
        okButton.onClick.AddListener(_GamePopup.ClosePopup);
    }

    protected override void UpdateData()
    {
        base.UpdateData();

        // 1페이지 보여주게 하기
        ShowGuide(0);
    }

    private void ShowGuide(int addNum)
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);
        dotImages[curNum].color = Color.gray;

        curNum += addNum;
        if(curNum >= guides.Count)
        {
            curNum = 0;
        }
        else if(curNum < 0)
        {
            curNum = guides.Count - 1;
        }

        if(addNum == 0)
        {
            curNum = 0;
        }

        dotImages[curNum].color = Color.white;
        nameText.text = guides[curNum].titleNum.Localization();
        contentsText.text = guides[curNum].contentsNum.Localization();
        guideImage.sprite = guides[curNum].image;
        guideImage.SetNativeSize();
        var guideRectTr = guideImage.transform as RectTransform;
        guideRectTr.sizeDelta = new Vector2(guideRectTr.sizeDelta.x, guideRectTr.sizeDelta.y) * 2f;
    }
}
