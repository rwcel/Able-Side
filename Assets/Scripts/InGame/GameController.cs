using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

public class GameController : MonoBehaviour
{
    [HideInInspector] public int Combo = 0;
    [HideInInspector] public int Score = 0;
    [HideInInspector] public float ExtraScore = 0;                    // 추가 점수 (%)
    [HideInInspector] public float Time;
    [HideInInspector] public int MaxCombo = 0;
    [HideInInspector] public int Bomb = 0;
    [HideInInspector] public int BombCharacter = 0;             // 폭탄 제거 캐릭터 수
    [HideInInspector] public bool IsFever;
    [HideInInspector] public bool IsStartFever;
    [HideInInspector] public bool IsInvincible;                         // 무적. but, combo는 리셋되게
    [HideInInspector] public bool IsBonus;
    [HideInInspector] public bool IsBomb;                           // **폭탄때는 1명씩터져야해서 어쩔 수 없이 생성

    [HideInInspector] public bool IsReverse;                            // Obstacle
    public bool CanShield => IsShield || shieldCount > 0;
    [HideInInspector] public bool IsShield;                               // 패시브 실드
    private List<int> itemShields = new List<int>();                // 먼저 얻은 실드가 먼저 사용되어야함
    private int shieldCount = 0;                                        // CanShield용 기록 변수

    [HideInInspector] public int AddDia = 0;

    public System.Action<bool> OnCorrect;
    public System.Action OnBomb;                                    // 기본 폭탄
    public System.Action OnItemBomb;                                // 폭탄 게이지 Full로 
    public System.Action OnEndBomb;                                 // 폭탄 종료 시 아이템 감소 시켜주는 역할
    public System.Action OnItemObstacle;                            // 아이템으로 방해요소 컷
    public System.Action<InGameItemData> OnBonus;           // 보너스 캐릭터
    public System.Action<FeverData> OnFever;

    public Queue<InGameItemData> Items = new Queue<InGameItemData>();          // Item
    [HideInInspector] public int Fever;
    [HideInInspector] public int UseFeverCount;

    [HideInInspector] public List<InGameItemData> itemGachas = new List<InGameItemData>();

    // 시간 관련
    private float resultTime;           // 능력 등에 의해 완전히 Fix된 최종 time
    private FTimeInfo timeInfo;
    private int nextTimeInfoNum = 0;
    private float calcTime = 0.05f;

    private Coroutine itemShieldCo;
    private Coroutine itemTimeCo;
    private WaitForSeconds timeDelay = new WaitForSeconds(0.05f);

    // 콤보 점수
    private FScoreComboInfo scoreComboInfo;
    private int nextComboInfoNum = 0;
    private int nextMaxComboNum = 0;

    // 아이템 콤보
    private FItemComboInfo itemComboInfo;
    private int nextItemInfoNum = 0;

    private System.IDisposable scoreDis;
    private System.IDisposable comboDis;
    private System.IDisposable itemDis;

    GameManager _GameManager;
    AudioManager _AudioManager;

    // 로그 기록용
    int itemAddCount;
    int normalCount;
    int rareCount;
    Dictionary<EInGameItem, int> normalItems;
    Dictionary<EInGameItem, int> rareItems;

    public void OnStart()
    {
        _GameManager = GameManager.Instance;
        _AudioManager = AudioManager.Instance;

        SetGachas();

        AddListeners();

        ClearData();
    }

    private int itemCapacity;

    void SetGachas()
    {
        itemCapacity = LevelData.Instance.InGameItemDatas.Length;
        normalItems = new Dictionary<EInGameItem, int>(itemCapacity);
        rareItems = new Dictionary<EInGameItem, int>(itemCapacity);

        foreach (var item in LevelData.Instance.InGameItemDatas)
        {
            itemGachas.Add(item);

            normalItems.Add(item.type, 0);
            rareItems.Add(item.type, 0);
        }
    }

    void AddListeners()
    {
        OnCorrect += value => {
            if (value == true)
                Correct();
            else
                InCorrect();
        };

        _GameManager.OnGameStart += (value) =>
        {
            if (value == true)
                InitSet();
            else
                ClearData();
        };

        // *아이템에 의해 추가시간이면 더해줄 변수 필요
        // OnBomb += () => { AddTime(BombCharacter); };

        _GameManager.ObserveEveryValueChanged(_ => _GameManager.CanInput)
            .Subscribe(value => calcTime = value ? 0.05f : 0f)
            .AddTo(this.gameObject);
    }

    private void InitSet()
    {
        timeInfo = Values.TimeInfos[nextTimeInfoNum++];
        scoreComboInfo = Values.ScoreComboInfos[nextComboInfoNum++];
        itemComboInfo = Values.ItemComboInfos[nextItemInfoNum];
        nextMaxComboNum++;

        // 나갈때 저장하기 때문에 시작 시 0으로 초기화
        AddDia = 0;

        // 매판 추가해줘야?
        scoreDis = this.ObserveEveryValueChanged(_ => Score)
                            .Where(value => value >= Values.TimeInfos[nextTimeInfoNum].score)
                            .Subscribe(value => CheckTimeInfo(value))
                            .AddTo(gameObject);

        comboDis = this.ObserveEveryValueChanged(_ => Combo)
                            .Where(value => nextComboInfoNum < Values.ScoreComboInfos.Length
                                && value >= Values.ScoreComboInfos[nextComboInfoNum].combo)
                            .Subscribe(value => CheckCombo(value))
                            .AddTo(gameObject);

        itemDis = this.ObserveEveryValueChanged(_ => Combo)
                    .Where(value => (value != 0) && (value % LevelData.Instance.ItemComboUnit == 0))
                    .Subscribe(value => CheckAddItem(value))
                    .AddTo(gameObject);

        // 효과등에 따른 시간 추가?
        resultTime = Time;
        GameUIManager.Instance.InGameMaxTime(resultTime);
        Debug.Log($"{resultTime} / {Time}");

        // 로그 기록 데이터
        itemAddCount = 0;
        normalCount = 0;
        rareCount = 0;

        normalItems.Clear();
        rareItems.Clear();
        foreach (var item in LevelData.Instance.InGameItemDatas)
        {
            normalItems.Add(item.type, 0);
            rareItems.Add(item.type, 0);
        }
    }

    private void AddTime(float value)
    {
        // Debug.Log("AddTime : " + value);
        Time = Mathf.Clamp(Time + value, 0, resultTime);
    }

    /// <summary>
    /// *Ready 이후에
    /// </summary>
    public void StartTime()
    {
        StartCoroutine(nameof(CoCalcTime));

        if (IsStartFever)
        {
            StartCoroutine(nameof(CoApplyFever));
        }
    }

    /// <summary>
    /// 점수, 콤보 계산
    /// </summary>
    public void Correct()
    {
        if (++Combo >= MaxCombo)
        {
            MaxCombo = Combo;
            // Debug.Log("Max Combo : " + MaxCombo);
        }
        _AudioManager.PlayComboSFX(Combo);

        float addScore = scoreComboInfo.score;
        // 보너스 캐릭터 계산 필요
        if(IsBonus)
        {
            addScore *= Values.BonusCharacter_value;
        }
        if(IsFever)
        {
            if(IsStartFever)
            {
                addScore *= Values.StartFeverData.bonusScore;
            }
            else
            {
                addScore *= Values.FeverDatas[Fever].bonusScore;
            }
        }

        Score += (int)addScore;

        if (++Bomb > Values.MaxBombCount)
        {
            Bomb = 0;
            _AudioManager.PlaySFX(ESFX.SpawnBomb);

            OnBomb?.Invoke();
        }

        //if (!IsFever && Combo >= LevelData.Instance.FeverComboNum)
        //{
        //    IsFever = true;
        //}

        AddTime(IsBomb ? Values.BombAddTime : timeInfo.successTime);
    }

    public void InCorrect()
    {
        Combo = 0;

        _AudioManager.PlayComboSFX(Combo);

        Bomb = Mathf.Clamp(Bomb - Values.DecreaseBombCount, 0, Values.MaxBombCount);

        nextItemInfoNum = 0;
        itemComboInfo = Values.ItemComboInfos[nextItemInfoNum];

        nextComboInfoNum = 0;
        scoreComboInfo = Values.ScoreComboInfos[nextComboInfoNum++];

        if (IsInvincible)
            return;

        AddTime(-timeInfo.failTime);
    }

    public bool UseShield()
    {
        //Debug.Log($"Shield Count : {itemShields} , {Shield}");

        for (int i = 0, length = itemShields.Count; i < length; i++)
        {
            if (itemShields[i] > 0)
            {
                itemShields[i]--;
                shieldCount--;

                // Debug.Log($"Shield Count : {itemShields[i]}");
                return true;
            }
        }

        // 실드가 확률로 변경됨
        if(IsShield)
        {
            return Random.Range(0, 100) < Values.Shield_Value;
        }

        return false;
    }

    void CheckCombo(int value)
    {
        //Debug.Log($"{value} >= {Values.ScoreComboInfos[nextComboInfoNum].combo}");

        if (nextComboInfoNum >= Values.ScoreComboInfos.Length)
        {
            return;
        }
        else
        {
            scoreComboInfo = Values.ScoreComboInfos[nextComboInfoNum++];
        }

        if (nextMaxComboNum < Values.ScoreComboInfos.Length - 1 
            && MaxCombo >= Values.ScoreComboInfos[nextMaxComboNum].combo)
        {
            Score += scoreComboInfo.rewardScore;
            nextMaxComboNum++;

            // Debug.Log("Add Score : " + scoreComboInfo.rewardScore);
        }
    }

    void CheckTimeInfo(float value)
    {
        if (++nextTimeInfoNum >= Values.TimeInfos.Length)
        {
            scoreDis.Dispose();
        }
        else
        {
            timeInfo = Values.TimeInfos[nextTimeInfoNum];
        }
    }

    IEnumerator CoCalcTime()
    {
        // Debug.Log($"{Time}");

        // *조건 더 많아지면 따로 두기 
        while (Time > 0)
        {
            yield return timeDelay;
            Time -= calcTime * timeInfo.decreaseTime * (IsFever ? 0 : 1);

            //if(Time <= Values.WarningTime && !IsFever)
            //{
            //    // 연출? -> 내부에서 진행중일경우 return 하게 해야하고 안에서 Sound재생
            //}

            //Debug.Log($"{delay} : {calcTime} {calcTime * timeInfo.decreaseTime}");
        }

        _AudioManager.PlaySFX(ESFX.Result);
        _GameManager.GameOver((int)(Score * ExtraScore), MaxCombo);

        GamePopup.Instance.OpenPopup(EGamePopup.Result, 
            () => 
            {
                _AudioManager.PauseBGM(true);
            }, 
            () =>
            {
                _GameManager.EndGame();
                _AudioManager.PauseBGM(false);
            });
    }

    #region Item

    public void CheckAddItem(int value)
    {
        if (Items.Count >= 3)
            return;

        if (nextItemInfoNum >= Values.ItemComboInfos.Length)
            return;

        itemAddCount++;

        InGameItemData data;

        for (int i = 0, length = itemComboInfo.normalBoxCount; i < length; i++)
        {
            if (Random.Range(0, 100) < itemComboInfo.normalBoxPercent)
            {
                //AddItem(NormalGacha());
                data = NormalGacha();
                AddItem(data);

                normalCount++;
                normalItems[data.type]++;
            }
        }

        for (int i = 0, length = itemComboInfo.rareBoxCount; i < length; i++)
        {
            if (Random.Range(0, 100) < itemComboInfo.rareBoxPercent)
            {
                //AddItem(RareGacha());

                data = RareGacha();
                AddItem(data);

                rareCount++;
                rareItems[data.type]++;
            }
        }

        if (value >= Values.ItemComboInfos[nextItemInfoNum].combo)
        {
            nextItemInfoNum++;
        }
    }

    public InGameItemData NormalGacha()
    {
        float rand = Random.Range(0f, 100f);
        //Debug.Log($"normal : {rand}");
        int idx = 0;
        while(idx < itemGachas.Count)
        {
            if(itemGachas[idx].normalPercent > 0)
            {
                if (rand < itemGachas[idx].normalPercent)
                {
                    return itemGachas[idx];
                }
            }
            rand -= itemGachas[idx++].normalPercent;
        }

        return null;
    }

    public InGameItemData RareGacha()
    {
        float rand = Random.Range(0f, 100f);
        //Debug.Log($"rare : {rand}");
        int idx = 0;
        while (idx < itemGachas.Count)
        {
            if (itemGachas[idx].rarePercent > 0)
            {
                if (rand < itemGachas[idx].rarePercent)
                {
                    return itemGachas[idx];
                }
            }
            rand -= itemGachas[idx++].rarePercent;
        }

        return null;
    }

    public InGameItemData StartGacha()
    {
        int rand = Random.Range(1, 1001);
        int idx = 0;
        while (idx < itemGachas.Count)
        {
            if (itemGachas[idx].startPercent > 0)
            {
                if (rand < itemGachas[idx].startPercent * 10)
                {
                    return itemGachas[idx];
                }
            }

            rand -= (int)itemGachas[idx++].startPercent * 10;
        }

        return null;
    }

    public bool AddItem(InGameItemData inGameItem)
    {
        // 여러번 들어오는 경우 필요
        if (Items.Count >= 3 || inGameItem == null)
            return false;

        //Debug.Log(inGameItem.name);

        switch (inGameItem.type)
        {
            case EInGameItem.AddDia1:
            case EInGameItem.AddDia3:
            case EInGameItem.AddDia5:
            case EInGameItem.AddDia9:
                // 인게임 중 재화 모으다가 나중에 전해주기
                AddDia += inGameItem.value;
                _AudioManager.PlaySFX(ESFX.GetDia);
                break;
            case EInGameItem.SuperFever:
                // 피버일때 피버 충전 못하게
                if (IsFever)
                    return false;

                AddFever();
                _AudioManager.PlaySFX(ESFX.GetItem);
                break;
            case EInGameItem.AddBombPower:
            case EInGameItem.BonusCharacter:
            case EInGameItem.BombFullGauge:
            case EInGameItem.TimeCharge:
            case EInGameItem.PreventInterrupt:
            case EInGameItem.PreventInCorrect:
                Items.Enqueue(inGameItem);
                _AudioManager.PlaySFX(ESFX.GetItem);
                break;
            default:
                Debug.LogError("No InGameItem!");
                break;
        }

        return true;
    }

    public void UseItem()
    {
        if (Items.Count <= 0)
            return;

        var item = Items.Dequeue();
        // Debug.Log($"Use Item : {item.type}");

        switch (item.type)
        { 
            case EInGameItem.AddDia1:
            case EInGameItem.AddDia3:
            case EInGameItem.AddDia5:
            case EInGameItem.AddDia9:
                // 이미 제공함
                break;
            case EInGameItem.AddBombPower:
                // 일회성 제공
                BombCharacter += item.value;
                OnEndBomb += () => BombCharacter -= item.value;
                _AudioManager.PlaySFX(ESFX.UseItem);
                break;
            case EInGameItem.BonusCharacter:
                OnBonus?.Invoke(item);
                _AudioManager.PlaySFX(ESFX.UseItem);
                break;
            case EInGameItem.BombFullGauge:
                Bomb = 0;
                OnItemBomb?.Invoke();
                _AudioManager.PlaySFX(ESFX.SpawnBomb);
                break;
            case EInGameItem.TimeCharge:
                // 지속시간 더하는 것이 아니라 새로시작
                if(itemTimeCo != null)
                    StopCoroutine(itemTimeCo);
                itemTimeCo = StartCoroutine(CoItemInvincible(item));
                _AudioManager.PlaySFX(ESFX.UseItem);
                break;
            case EInGameItem.PreventInterrupt:
                OnItemObstacle?.Invoke();
                _AudioManager.PlaySFX(ESFX.Clean);
                break;
            case EInGameItem.PreventInCorrect:
                itemShieldCo = StartCoroutine(CoItemShield(item));
                _AudioManager.PlaySFX(ESFX.UseItem);
                break;
            case EInGameItem.SuperFever:
                // 왼쪽 칸으로 들어감
                break;
        }
    }

    IEnumerator CoItemShield(InGameItemData item)
    {
        shieldCount += item.value;
        itemShields.Add(item.value);
        yield return new WaitForSeconds(item.valueTime);

        // *게임이 종료됐을 때 코루틴 실행되는 문제 : itemShieldCo가 여러개기 때문 -> StopAllCoroutine
        //if (itemShields.Count <= 0)
        //    yield return;

        shieldCount -= itemShields[0];
        itemShields.RemoveAt(0);
    }

    IEnumerator CoItemInvincible(InGameItemData item)
    {
        Time = resultTime;
        IsInvincible = true;
        StopCoroutine(nameof(CoCalcTime));
        yield return new WaitForSeconds(item.valueTime);
        IsInvincible = false;
        StartCoroutine(nameof(CoCalcTime));
    }
    #endregion

    #region Fever

    public void UseFever()
    {
        if (IsFever || Fever <= 0)
            return;

        StartCoroutine(nameof(CoApplyFever));
    }

    public void AddFever(int count = 1)
    {
        if (Fever == Values.MaxFever)
            return;

        Fever = (Fever + count) > Values.MaxFever ? Values.MaxFever : Fever + count;

        // *인게임 중에는 보내지 않기
        // BackEndServerManager.Instance.Update_Fever(Fever);
    }

    IEnumerator CoApplyFever()
    {
        WaitForSeconds applyTime;
        if (IsStartFever)
        {
            applyTime = new WaitForSeconds(Values.StartFeverData.applyTime);
            OnFever?.Invoke(Values.StartFeverData);
        }
        else
        {
            applyTime = new WaitForSeconds(Values.FeverDatas[Fever].applyTime);
            OnFever?.Invoke(Values.FeverDatas[Fever]);
        }

        // 개수에 따라 사용
        //Debug.Log($"Fever {Fever}개 사용! : {item.itemName}");
        IsFever = true;

        _AudioManager.PlaySFX(ESFX.Fever);

        yield return applyTime;

        Debug.Log($"피버 적용 끝 : {Fever}");

        if(IsStartFever)
        {
            IsStartFever = false;
        }
        else
        {
            UseFeverCount += Fever;
            Fever = 0;
            BackEndServerManager.Instance.Update_Fever(Fever);
        }

        IsFever = false;
    }

    #endregion

    /// <summary>
    /// 이어하기
    /// </summary>
    public void Revive()
    {
        ++_GameManager.InputValue;
        _AudioManager.PauseBGM(false);

        Time = resultTime;

        StopCoroutine(nameof(CoCalcTime));
        StartCoroutine(nameof(CoCalcTime));
    }

    /// <summary>
    /// 게임 종료 시 데이터 초기화
    /// </summary>
    private void ClearData()
    {
        StopCoroutine(nameof(CoCalcTime));

        Combo = 0;
        Score = 0;
        ExtraScore = 1;
        calcTime = 0.05f;
        Time = Values.MaxTimeBase;
        Bomb = 0;
        BombCharacter = Values.DeleteBombCharacters;
        MaxCombo = 0;
        UseFeverCount = 0;
        shieldCount = 0;

        nextTimeInfoNum = 0;
        nextComboInfoNum = 0;
        nextMaxComboNum = 0;

        if (scoreDis != null)
            scoreDis.Dispose();
        if (comboDis != null)
            comboDis.Dispose();
        if (itemDis != null)
            itemDis.Dispose();

        IsFever = false;
        IsStartFever = false;
        IsReverse = false;
        IsShield = false;
        IsBomb = false;
        IsInvincible = false;

        Items.Clear();
        itemShields.Clear();

        OnEndBomb = null;
        // OnItemObstacle = null;           // **Player, Clean이 날아감

        StopAllCoroutines();
        //if(itemTimeCo != null)
        //    StopCoroutine(itemTimeCo);
        //if(itemShieldCo != null)
        //    StopCoroutine(itemShieldCo);

        if(normalCount != 0 || rareCount != 0)
        {
            BackEndServerManager.Instance.InGameItemLog(itemAddCount, normalCount, normalItems, rareCount, rareItems);
        }

        // 피버 게임 종료시에만 갱신해주기
        BackEndServerManager.Instance.Update_Fever(Fever);
    }
}
