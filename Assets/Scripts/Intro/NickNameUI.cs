using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(BadWords))]
public class NickNameUI : MonoBehaviour
{
    [SerializeField] TMP_InputField nicknameInput;
    [SerializeField] Button okButton;

    private BadWords badWords;

    private bool isDuplicateCheck = false;
    private string tmpNickName = "";

    SystemPopupUI _SystemPopupUI;

    private void Awake()
    {
        badWords = GetComponent<BadWords>();
    }

    private void Start()
    {
        _SystemPopupUI = SystemPopupUI.Instance;
        
        nicknameInput.onValueChanged.AddListener(TextFieldChange);
        okButton.onClick.AddListener(UpdateNickName);

        nicknameInput.characterLimit = 12;
    }

    public void TextFieldChange(string nickname)
    {
        if (tmpNickName == nickname && nickname != "")
        {
            isDuplicateCheck = true;
        }
        else
        {
            isDuplicateCheck = false;
        }
    }

    public void UpdateNickName()
    {
        if(!isDuplicateCheck)
        {
            if (!DuplicateNickNameCheck())
            {
                Debug.LogWarning("실패");
                return;
            }
        }

        BackEndServerManager.Instance.UpdateNickname(tmpNickName);
        Debug.Log("회원가입 성공");

        BackEndServerManager.Instance.OnBackendAuthorized();
    }

    public bool DuplicateNickNameCheck()
    {
        string tmp = nicknameInput.text;

        if (tmp.Length <= 1)
        {
            _SystemPopupUI.OpenNoneTouch(77);
            return false;
        }
        

        // 금칙어 확인
        if (!badWords.CheckFilter(tmp))
        {
            _SystemPopupUI.OpenNoneTouch(75);
            return false;
        }

        isDuplicateCheck = false;
        int errCode = BackEndServerManager.Instance.DuplicateNickNameCheck(tmp);

        // 중복이 아닌경우
        if (errCode == 0)
        {
            tmpNickName = tmp;
            isDuplicateCheck = true;

            return true;
        }

        _SystemPopupUI.OpenNoneTouch(76);
        return false;
    }

}
