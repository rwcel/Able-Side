using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SystemPopupUI : Singleton<SystemPopupUI>
{
    [SerializeField] OneButtonUI oneButtonUI; 
    [SerializeField] TwoButtonUI twoButtonUI;
    [SerializeField] NoneTouchUI noneTouchUI;

    [SerializeField] InputFieldTwoButtonUI inputTwoButtonUI;
    [SerializeField] AdvertiseUI advertiseUI;


    protected override void AwakeInstance()
    {
        var obj = FindObjectsOfType<SystemPopupUI>();
        if (obj.Length == 1)
            DontDestroyOnLoad(gameObject);
        else
        {
            Destroy(gameObject);
        }
    }

    protected override void DestroyInstance() { }

    public void OpenOneButton(string _titleText, string _contentsText, string _buttonText, System.Action _buttonAction = null)
        => oneButtonUI.SetData(_titleText, _contentsText, _buttonText, _buttonAction);

    public void OpenOneButton(int _titleNum, int _contentsNum, int _buttonNum, System.Action _buttonAction = null)
    => oneButtonUI.SetData(_titleNum, _contentsNum, _buttonNum, _buttonAction);


    public void OpenTwoButton(string _titleText, string _contentsText, string _leftButtonText, string _rightButtonText,
                                        System.Action _leftButtonAction = null, System.Action _rightButtonAction = null)
    => twoButtonUI.SetData(_titleText, _contentsText, _leftButtonText, _rightButtonText, _leftButtonAction, _rightButtonAction);

    public void OpenTwoButton(int _titleNum, int _contentsNum, int _leftButtonNum = 0, int _rightButtonNum = 1,
                                        System.Action _leftButtonAction = null, System.Action _rightButtonAction = null)
    => twoButtonUI.SetData(_titleNum, _contentsNum, _leftButtonNum, _rightButtonNum, _leftButtonAction, _rightButtonAction);

    public void OpenTwoButton(int _titleNum, string _contentsText, int _leftButtonNum = 0, int _rightButtonNum = 1,
                                    System.Action _leftButtonAction = null, System.Action _rightButtonAction = null)
    => twoButtonUI.SetData(_titleNum, _contentsText, _leftButtonNum, _rightButtonNum, _leftButtonAction, _rightButtonAction);

    public void OpenNoneTouch(string _contentsText)
    => noneTouchUI.SetData(_contentsText);

    public void OpenNoneTouch(int _contentsNum)
    => noneTouchUI.SetData(_contentsNum);

    public void OpenInputTwoButton(int _charLimit, int _titleNum, int _contentsNum,
                                        System.Action<string> _leftButtonAction = null, System.Action<string> _rightButtonAction = null)
    => inputTwoButtonUI.SetData(_charLimit, _titleNum, _contentsNum,  _leftButtonAction, _rightButtonAction);

    public void OpenAdvertise(EDailyGift _type, int _titleNum, System.Action _adRewardAction = null)
    => advertiseUI.SetData(_type, _titleNum, _adRewardAction);
}
