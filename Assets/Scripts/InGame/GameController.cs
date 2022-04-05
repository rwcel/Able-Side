using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

public class GameController : MonoBehaviour
{
    [HideInInspector] public int Combo = 0;
    [HideInInspector] public int Score = 0;
    [HideInInspector] public float ExtraScore = 0;                    // �߰� ���� (%)
    [HideInInspector] public float Time;
    [HideInInspector] public int MaxCombo = 0;
    [HideInInspector] public int Bomb = 0;
    [HideInInspector] public int BombCharacter = 0;             // ��ź ���� ĳ���� ��
    [HideInInspector] public bool IsFever;
    [HideInInspector] public bool IsStartFever;
    [HideInInspector] public bool IsInvincible;                         // ����. but, combo�� ���µǰ�
    [HideInInspector] public bool IsBonus;
    [HideInInspector] public bool IsBomb;                           // **��ź���� 1���������ؼ� ��¿ �� ���� ����

    [HideInInspector] public bool IsReverse;                            // Obstacle
    public bool CanShield => IsShield || shieldCount > 0;
    [HideInInspector] public bool IsShield;                               // �нú� �ǵ�
    private List<int> itemShields = new List<int>();                // ���� ���� �ǵ尡 ���� ���Ǿ����
    private int shieldCount = 0;                                        // CanShield�� ��� ����

    [HideInInspector] public int AddDia = 0;

    public System.Action<bool> OnCorrect;
    public System.Action OnBomb;                                    // �⺻ ��ź
    public System.Action OnItemBomb;                                // ��ź ������ Full�� 
    public System.Action OnEndBomb;                                 // ��ź ���� �� ������ ���� �����ִ� ����
    public System.Action OnItemObstacle;                            // ���������� ���ؿ�� ��
    public System.Action<InGameItemData> OnBonus;           // ���ʽ� ĳ����
    public System.Action<FeverData> OnFever;

    public Queue<InGameItemData> Items = new Queue<InGameItemData>();          // Item
    [HideInInspector] public int Fever;
    [HideInInspector] public int UseFeverCount;

    [HideInInspector] public List<InGameItemData> itemGachas = new List<InGameItemData>();

    // �ð� ����
    private float resultTime;           // �ɷ� � ���� ������ Fix�� ���� time
    private FTimeInfo timeInfo;
    private int nextTimeInfoNum = 0;
    private float calcTime = 0.05f;

    private Coroutine itemShieldCo;
    private Coroutine itemTimeCo;
    private WaitForSeconds timeDelay = new WaitForSeconds(0.05f);

    // �޺� ����
    private FScoreComboInfo scoreComboInfo;
    private int nextComboInfoNum = 0;
    private int nextMaxComboNum = 0;

    // ������ �޺�
    private FItemComboInfo itemComboInfo;
    private int nextItemInfoNum = 0;

    private System.IDisposable scoreDis;
    private System.IDisposable comboDis;
    private System.IDisposable itemDis;

    GameManager _GameManager;
    AudioManager _AudioManager;

    // �α� ��Ͽ�
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

        // *�����ۿ� ���� �߰��ð��̸� ������ ���� �ʿ�
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

        // ������ �����ϱ� ������ ���� �� 0���� �ʱ�ȭ
        AddDia = 0;

        // ���� �߰������?
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

        // ȿ��� ���� �ð� �߰�?
        resultTime = Time;
        GameUIManager.Instance.InGameMaxTime(resultTime);
        Debug.Log($"{resultTime} / {Time}");

        // �α� ��� ������
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
    /// *Ready ���Ŀ�
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
    /// ����, �޺� ���
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
        // ���ʽ� ĳ���� ��� �ʿ�
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

        // �ǵ尡 Ȯ���� �����
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

        // *���� �� �������� ���� �α� 
        while (Time > 0)
        {
            yield return timeDelay;
            Time -= calcTime * timeInfo.decreaseTime * (IsFever ? 0 : 1);

            //if(Time <= Values.WarningTime && !IsFever)
            //{
            //    // ����? -> ���ο��� �������ϰ�� return �ϰ� �ؾ��ϰ� �ȿ��� Sound���
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
        // ������ ������ ��� �ʿ�
        if (Items.Count >= 3 || inGameItem == null)
            return false;

        //Debug.Log(inGameItem.name);

        switch (inGameItem.type)
        {
            case EInGameItem.AddDia1:
            case EInGameItem.AddDia3:
            case EInGameItem.AddDia5:
            case EInGameItem.AddDia9:
                // �ΰ��� �� ��ȭ �����ٰ� ���߿� �����ֱ�
                AddDia += inGameItem.value;
                _AudioManager.PlaySFX(ESFX.GetDia);
                break;
            case EInGameItem.SuperFever:
                // �ǹ��϶� �ǹ� ���� ���ϰ�
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
                // �̹� ������
                break;
            case EInGameItem.AddBombPower:
                // ��ȸ�� ����
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
                // ���ӽð� ���ϴ� ���� �ƴ϶� ���ν���
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
                // ���� ĭ���� ��
                break;
        }
    }

    IEnumerator CoItemShield(InGameItemData item)
    {
        shieldCount += item.value;
        itemShields.Add(item.value);
        yield return new WaitForSeconds(item.valueTime);

        // *������ ������� �� �ڷ�ƾ ����Ǵ� ���� : itemShieldCo�� �������� ���� -> StopAllCoroutine
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

        // *�ΰ��� �߿��� ������ �ʱ�
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

        // ������ ���� ���
        //Debug.Log($"Fever {Fever}�� ���! : {item.itemName}");
        IsFever = true;

        _AudioManager.PlaySFX(ESFX.Fever);

        yield return applyTime;

        Debug.Log($"�ǹ� ���� �� : {Fever}");

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
    /// �̾��ϱ�
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
    /// ���� ���� �� ������ �ʱ�ȭ
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
        // OnItemObstacle = null;           // **Player, Clean�� ���ư�

        StopAllCoroutines();
        //if(itemTimeCo != null)
        //    StopCoroutine(itemTimeCo);
        //if(itemShieldCo != null)
        //    StopCoroutine(itemShieldCo);

        if(normalCount != 0 || rareCount != 0)
        {
            BackEndServerManager.Instance.InGameItemLog(itemAddCount, normalCount, normalItems, rareCount, rareItems);
        }

        // �ǹ� ���� ����ÿ��� �������ֱ�
        BackEndServerManager.Instance.Update_Fever(Fever);
    }
}
