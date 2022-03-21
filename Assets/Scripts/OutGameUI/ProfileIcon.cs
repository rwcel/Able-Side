using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProfileIcon : MonoBehaviour
{
    [SerializeField] Toggle toggle;
    public Toggle IconToggle => toggle;

    [SerializeField] Image iconImage;
    [SerializeField] GameObject lockObj;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="profileData">데이터 적용</param>
    /// <param name="isOpen">활성화 조건 만족하는지</param>
    public void SetData(ProfileData profileData, bool isOpen)
    {
        iconImage.sprite = profileData.sprite;
        lockObj.SetActive(!isOpen);
    }

    public void UpdateOpen(bool isOpen)
    {
        lockObj.SetActive(!isOpen);
    }
}
