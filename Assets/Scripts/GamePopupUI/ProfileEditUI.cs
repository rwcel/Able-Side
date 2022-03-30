using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;

[RequireComponent(typeof(BadWords))]
public class ProfileEditUI : PopupUI
{
    [Header("NickName")]
    [SerializeField] TextMeshProUGUI nicknameText;
    [SerializeField] Button editButton;

    [System.Serializable]
    private struct FProfileIcon
    {
        //public Button 
        //public int scoreMission;
        //public int comboMission;
    }

    [Header("Select Icon")]
    [SerializeField] Image profileIconImage;
    [SerializeField] LocalizationText nameLangText;
    [SerializeField] TextMeshProUGUI missionText;
    [SerializeField] TextMeshProUGUI descText;

    [Header("Icon List")]
    [SerializeField] GameObject iconPrefab;
    [SerializeField] ScrollRect scrollRect;                 // 열때마다 리셋
    [SerializeField] ToggleGroup toggleGroup;
    [SerializeField] Transform iconParent;

    [Header("etc")]
    [SerializeField] TextMeshProUGUI tooltipText;           // 해금 조건
    [SerializeField] Button saveButton;
    [SerializeField] Button exitButton;

    private List<FProfileIcon> iconList = new List<FProfileIcon>();

    private ProfileData curData;
    private int selectNum;          // 선택한 아이콘 번호

    private bool isDuplicateCheck;
    private bool isIconOpen;
    private bool changeNickname;
    private void UpdateSaveButton() => saveButton.interactable = (isDuplicateCheck && isIconOpen);

    private List<ProfileIcon> profileIcons = new List<ProfileIcon>();

    private BadWords badWords;

    protected override void Awake()
    {
        base.Awake();

        badWords = GetComponent<BadWords>();
    }

    protected override void Start()
    {
        base.Start();

        CreateIcons();

        AddListeners();

        profileIcons[BackEndServerManager.Instance.ProfileIcon].IconToggle.isOn = true;
        tooltipText.text = string.Format(232.Localization(), Values.Bonus_AllProfile);
    }

    protected override void UpdateData()
    {
        base.UpdateData();

        // 리셋
        scrollRect.verticalNormalizedPosition = 1f;
        isDuplicateCheck = true;

        nicknameText.text = _GameManager.NickName;
        profileIconImage.sprite = _GameManager.ProfileData.sprite;

        // *Unirx 작동 확인
        curData = _GameManager.ProfileData;


        // 계속 전체를 검사하는게 맞는지
        for (int i = 0, length = profileIcons.Count; i < length; i++)
        {
            int idx = i;
            var profileData = LevelData.Instance.ProfileDatas[idx];
            bool isOpen = _GameManager.AccumulateScore >= profileData.missionScore;
            profileIcons[idx].UpdateOpen(isOpen);
            // **데이터 열때마다 현재 스코어에 따라 isOpen을 연결해줘야함
            profileIcons[idx].IconToggle.onValueChanged.RemoveAllListeners();
            profileIcons[idx].IconToggle.onValueChanged.AddListener((value) =>
            {
                if (value)
                {
                    UpdateProfile(profileData, idx, isOpen);
                    AudioManager.Instance.PlaySFX(ESFX.Touch);
                }
            });
        }

        //missionText.text = 28.Localization() + " " + string.Format(231.Localization(), curData.missionScore);
        missionText.text = string.Format(231.Localization(), curData.missionScore);
        descText.text = curData.descNum.Localization();
    }

    private void CreateIcons()
    {
        for (int i = 0, length = LevelData.Instance.ProfileDatas.Length; i < length; i++)
        {
            int idx = i;
            var profileData = LevelData.Instance.ProfileDatas[i];
            var profileIcon = Instantiate(iconPrefab, iconParent).GetComponent<ProfileIcon>();
            bool isOpen = _GameManager.AccumulateScore >= profileData.missionScore;           // 계속 실행 필요
            profileIcon.SetData(profileData, isOpen);
            profileIcon.IconToggle.group = toggleGroup;
            profileIcon.IconToggle.onValueChanged.AddListener((value) =>
            {
                if (value)
                {
                    UpdateProfile(profileData, idx, isOpen);
                    AudioManager.Instance.PlaySFX(ESFX.Touch);
                }
            });

            profileIcons.Add(profileIcon);
        }
    }

    private void AddListeners()
    {
        editButton.onClick.AddListener(() => SystemPopupUI.Instance.OpenInputTwoButton(Values.Input_Limit_Nickname, 107, 188, DuplicateNickNameCheck, null));

        saveButton.onClick.AddListener(OnSave);
        exitButton.onClick.AddListener(OnExit);

        this.ObserveEveryValueChanged(_ => curData)
            .Subscribe(value => profileIconImage.sprite = value.sprite)
            .AddTo(this.gameObject);
    }

    private void UpdateProfile(ProfileData profileData, int idx, bool isOpen)
    {
        curData = profileData;
        selectNum = idx;
        isIconOpen = isOpen;

        nameLangText.ChangeText(profileData.nameNum);
        missionText.text = string.Format(231.Localization(), curData.missionScore);
        descText.text = curData.descNum.Localization();

        //tooltipText.gameObject.SetActive(!isOpen);
        //tooltipText.text = $"UnLock - Score : {profileData.missionScore}, Combo : {profileData.missionCombo}";

        UpdateSaveButton();
    }
 
    public void DuplicateNickNameCheck(string _text)
    {
        if(_text.Length <= 1)
        {
            SystemPopupUI.Instance.OpenNoneTouch(110);
            return;
        }

        if (!badWords.CheckFilter(_text))
        {
            SystemPopupUI.Instance.OpenNoneTouch(108);
            return;
        }

        int errCode = BackEndServerManager.Instance.DuplicateNickNameCheck(_text);

        // 중복이 아닌경우
        if (errCode == 0)
        {
            nicknameText.text = _text;
            isDuplicateCheck = true;
            changeNickname = true;
            UpdateSaveButton();
        }
        else
        {
            SystemPopupUI.Instance.OpenNoneTouch(109);
        }
    }

    private void OnSave()
    {
        if(changeNickname)
        {
            BackEndServerManager.Instance.UpdateNickname(nicknameText.text);
        }

        BackEndServerManager.Instance.Update_ProfileIcon(selectNum);

        _GamePopup.ClosePopup();
    }

    private void OnExit()
    {
        profileIcons[BackEndServerManager.Instance.ProfileIcon].IconToggle.isOn = true;

        _GamePopup.ClosePopup();
    }
}
