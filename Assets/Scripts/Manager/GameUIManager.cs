using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUIManager : Singleton<GameUIManager>
{
    [SerializeField] OutGameUI outGameUI;
    [SerializeField] InGameUI inGameUI;

    private GameObject outGameObj;
    private GameObject inGameObj;

    protected override void AwakeInstance()
    {
    }

    protected override void DestroyInstance() { }

    private void Start()
    {
        InitSet();

        AddActions();

        outGameUI.OnStart();
        inGameUI.OnStart();
    }

    private void InitSet()
    {
        outGameObj = outGameUI.gameObject;
        inGameObj = inGameUI.gameObject;

        // 반응 안함
        outGameObj.SetActive(true);
        inGameObj.SetActive(false);
    }

    private void AddActions()
    {
        GameManager.Instance.OnGameStart += (value) => outGameObj.SetActive(!value);
        GameManager.Instance.OnGameStart += (value) => inGameObj.SetActive(value);

        GameManager.Instance.IsGameStart = false;
    }


    #region OutGame

    public void MoveDock(EDock dock) => outGameUI.MoveDock(dock);

    public void AddSerialPost(FPostInfo postInfo) => outGameUI.SerialPost(postInfo);

    public EDock GetCurrentDock => outGameUI.CurDock;

    #endregion

    #region InGame

    public void InGameOpenImage(ESide side, int arrayNum) => inGameUI.OpenImage(side, arrayNum);

    public void InGameSideImg(List<Sprite> leftSprites, List<Sprite> rightSprites) => inGameUI.SetSideImage(leftSprites, rightSprites);

    public void InGameMaxTime(float time) => inGameUI.SetMaxTime(time);

    // *옵저버 패턴?
    public void InGameBlur(bool isBlur) => inGameUI.Blur(isBlur);
    // public void InGameReverse(bool isReverse) => inGameUI.ReverseButton(isReverse);

    public void InGameBonusCharImg(bool isBonus, ESide side, int arrayNum) => inGameUI.BonusCharImg(isBonus, side, arrayNum);

    public void InGameFeverBonus(bool isBonus) => inGameUI.FeverBonus(isBonus);

    #endregion
}
