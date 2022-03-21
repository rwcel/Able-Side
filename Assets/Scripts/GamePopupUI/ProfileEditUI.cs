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
    [SerializeField] TMP_InputField nicknameInput;
    [SerializeField] TextMeshProUGUI currentNickName;
    [SerializeField] Button duplicateButton;

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

    protected override void UpdateData()
    {
        base.UpdateData();

        // 리셋
        scrollRect.verticalNormalizedPosition = 1f;
        nicknameInput.text = "";
        isDuplicateCheck = true;

        currentNickName.text = _GameManager.NickName;
        profileIconImage.sprite = _GameManager.ProfileData.sprite;

        // *Unirx 작동 확인
        curData = _GameManager.ProfileData;

        // 계속 전체를 검사하는게 맞는지
        for (int i = 0, length = profileIcons.Count; i < length; i++)
        {
            profileIcons[i].UpdateOpen(_GameManager.AccumulateScore >= LevelData.Instance.ProfileDatas[i].missionScore);
        }

        missionText.text = string.Format(55.Localization(), curData.missionScore);      // 언어 변경 최초 적용 필요
    }

    private void Start()
    {
        CreateIcons();

        AddListeners();

        profileIcons[BackEndServerManager.Instance.ProfileIcon].IconToggle.isOn = true;
        tooltipText.text = string.Format(56.Localization(), Values.Bonus_AllProfile);
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
        duplicateButton.onClick.AddListener(DuplicateNickNameCheck);

        saveButton.onClick.AddListener(OnSave);
        exitButton.onClick.AddListener(OnExit);

        nicknameInput.onValueChanged.AddListener(NickNameInput);

        this.ObserveEveryValueChanged(_ => curData)
            .Subscribe(value => profileIconImage.sprite = value.sprite)
            .AddTo(this.gameObject);

        nicknameInput.characterLimit = 12;
    }

    private void UpdateProfile(ProfileData profileData, int idx, bool isOpen)
    {
        curData = profileData;
        selectNum = idx;
        isIconOpen = isOpen;

        nameLangText.ChangeText(profileData.nameNum);
        missionText.text = string.Format(55.Localization(), profileData.missionScore);

        //tooltipText.gameObject.SetActive(!isOpen);
        //tooltipText.text = $"UnLock - Score : {profileData.missionScore}, Combo : {profileData.missionCombo}";

        UpdateSaveButton();
    }

    private void NickNameInput(string text)
    {
        // 텍스트 확인
        if(text != null)
        {
            isDuplicateCheck = false;
            UpdateSaveButton();
        }
    }
 
    public void DuplicateNickNameCheck()
    {
        string tmp = nicknameInput.text;
        if (!badWords.CheckFilter(tmp))
            return;

        int errCode = BackEndServerManager.Instance.DuplicateNickNameCheck(tmp);

        // 중복이 아닌경우
        if (errCode == 0)
        {
            currentNickName.text = tmp;
            isDuplicateCheck = true;
            changeNickname = true;
            UpdateSaveButton();
        }
    }

    private void OnSave()
    {
        if(changeNickname)
        {
            BackEndServerManager.Instance.UpdateNickname(nicknameInput.text);
        }

        BackEndServerManager.Instance.Update_ProfileIcon(selectNum);

        _GamePopup.ClosePopup();
    }

    /// <summary>
    /// Reset
    /// **토글이 Acive False 가 되면 isOn이 제대로 적용되지 않음
    /// </summary>
    private void OnExit()
    {
        profileIcons[BackEndServerManager.Instance.ProfileIcon].IconToggle.isOn = true;

        _GamePopup.ClosePopup();
    }
}
