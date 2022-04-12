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
        var gameManager = GameManager.Instance;

        gameManager.OnGameStart += (value) => 
        {
            outGameObj.SetActive(!value);
            inGameObj.SetActive(value);
        };

        gameManager.IsGameStart = false;
    }


    #region OutGame

    public void MoveDock(EDock dock) => outGameUI.MoveDock(dock);

    public void AddSerialPost(FPostInfo postInfo) => outGameUI.SerialPost(postInfo);

    public EDock GetCurrentDock => outGameUI.CurDock;

    #endregion

    #region InGame

    public void InGameSlotOpenImage(ESide side, int arrayNum) => inGameUI.SlotUI.OpenImage(side, arrayNum);

    public void InGameSlotSideImg(List<Sprite> leftSprites, List<Sprite> rightSprites) => inGameUI.SlotUI.SetSideImage(leftSprites, rightSprites);

    public void InGameMaxTime(float time) => inGameUI.SetMaxTime(time);

    // *옵저버 패턴?
    public void InGameSlotBlur(bool isBlur) => inGameUI.SlotUI.Blur(isBlur);
    // public void InGameReverse(bool isReverse) => inGameUI.ReverseButton(isReverse);

    public void InGameSlotSetBonusChar(bool isBonus, ESide side, int arrayNum) => inGameUI.SlotUI.SetBonusChar(isBonus, side, arrayNum);

    public void InGameSlotMoveBonusChar(ESide side, int arrayNum) => inGameUI.SlotUI.MoveBonusChar(side, arrayNum);

    public void InGameSlotFeverBonus(bool isBonus) => inGameUI.SlotUI.FeverBonus(isBonus);

    public void InGamePauseReady(bool isPause) => inGameUI.PauseReadyAnim(isPause);

    #endregion
}
