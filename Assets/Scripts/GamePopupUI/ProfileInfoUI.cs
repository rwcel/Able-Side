using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UniRx;

public class ProfileInfoUI : PopupUI
{
    [Header("Images")]
    [SerializeField] Image iconImage;

    [Header("Texts")]
    [SerializeField] TextMeshProUGUI nicknameText;
    [SerializeField] TextMeshProUGUI bestScoreText;
    [SerializeField] TextMeshProUGUI comboText;
    [SerializeField] TextMeshProUGUI accuScoreText;
    [SerializeField] TextMeshProUGUI rankText;

    [Header("Buttons")]
    [SerializeField] Button editButton;
    [SerializeField] Button rankButton;
    [SerializeField] Button exitButton;


    protected override void Start()
    {
        base.Start();

        AddListeners();

        AddObserve();
    }

    protected override void UpdateData()
    {
        base.UpdateData();

        // 열때마다 알려주기?
        iconImage.sprite = _GameManager.ProfileData.sprite;
        nicknameText.text = _GameManager.NickName;
        bestScoreText.text = _GameManager.BestScore.CommaThousands();
        comboText.text = _GameManager.BestMaxCombo.ToString();
        accuScoreText.text = _GameManager.AccumulateScore.ToString();
        rankText.text = _GameManager.Rank.Ordinalnumber();
    }

    void AddListeners()
    {
        editButton.onClick.AddListener(() => _GamePopup.OpenPopup(EGamePopup.ProfileEdit));
        rankButton.onClick.AddListener(() =>GameUIManager.Instance.MoveDock(EDock.Rank));
        exitButton.onClick.AddListener(_GamePopup.ClosePopup);
    }

    /// <summary>
    /// Edit에서 변하는 값만 observe 하기
    /// - iconImage, nickname
    /// </summary>
    void AddObserve()
    {
        this.ObserveEveryValueChanged(_ => _GameManager.ProfileData.sprite)
            .Subscribe(value => iconImage.sprite = value)
            .AddTo(this.gameObject);

        this.ObserveEveryValueChanged(_ => _GameManager.NickName)
            .Subscribe(value => nicknameText.text = value)
            .AddTo(this.gameObject);
    }
}
