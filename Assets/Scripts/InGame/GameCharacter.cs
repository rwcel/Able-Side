using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;
using DG.Tweening;


/// <summary>
/// ĳ���� ����, ��ġ ���� �Լ�
/// </summary>
public class GameCharacter : MonoBehaviour
{
    // **������ �� ������?
    [SerializeField] List<CharacterData> characterDatas = new List<CharacterData>();
    public List<CharacterData> CharacterDatas => characterDatas;

    public Dictionary<ECharacter, CharacterData> CharacterDictionary = new Dictionary<ECharacter, CharacterData>();

    protected List<ECharacter> characterTypes = new List<ECharacter>();
    protected List<ECharacter> randomCharacters = new List<ECharacter>();
    //protected List<bool> charactersBonus = new List<bool>();
    protected Dictionary<ECharacter, bool> bonusCharacters = new Dictionary<ECharacter, bool>();
    protected int bonusCharacterCount = 0;

    [Header("Tween")]
    [SerializeField] Ease moveEase;
    [SerializeField] float moveTime;
    [SerializeField] Ease outEase;
    [SerializeField] float outTime;

    // ��Ÿ ��ũ��Ʈ
    protected GameController gameController;
    protected GameUIManager _GameUIManager;

    protected Sequence moveSequence;
    protected Sequence outSequence;

    // ���� ���� �� ���� �� ����
    protected List<ECharacter> leftSideChars = new List<ECharacter>();
    protected List<ECharacter> rightSideChars = new List<ECharacter>();

    // �ӽ� ���� ��
    protected List<ECharacter> tmpLeftChars = new List<ECharacter>();
    protected List<ECharacter> tmpRightChars = new List<ECharacter>();

    public int ShowCharacterNum { get; private set; }       // ���� �����ְ� �ִ� ĳ���� ��

    protected Queue<CharacterController> spawnCharacters = new Queue<CharacterController>();

    protected Vector3[] charactersPos;
    // protected CharacterController outCharacter;         // *���� ���� �� �̵����̶�� Dequeue �Ǿ��⶧���� �������� ���� -> �׳� ���α�

    // ĳ���� ������ ���� �迭
    protected int nextOpen = 0;
    private System.IDisposable scoreDis;

    private int leftOpenCount = 0;
    private int rightOpenCount = 0;

    // ��ź���� ����
    private bool isSpawnBomb;

    // �ǹ�
    private ECharacter feverCharacter;
    private bool isFever;                   // GameController�� Fever

    // ���ʽ� ĳ����
    protected Coroutine bonusCharacterCo;

    public System.Action OnStartBomb;

    // Ǯ ���� ���� -> �߾ӿ��� ������
    public void OnStart()
    {
        AddCaching();

        AddActions();
    }

    private void AddCaching()
    {
        gameController = GameManager.Instance.GameController;
        _GameUIManager = GameUIManager.Instance;

        foreach (var characterData in characterDatas)
        {
            CharacterDictionary.Add(characterData.type, characterData);
        }

        // **Bomb�� �� �� ����
        //foreach (ECharacter type in System.Enum.GetValues(typeof(ECharacter)))
        //{
        //    characterTypes.Add(type);
        //}
        for (int i = 0, type = (int)ECharacter.Bomb; i < type; i++)
        {

            characterTypes.Add((ECharacter)i);
        }

        int length = Values.MaxSpawnCharacterNum;
        charactersPos = new Vector3[length];
        for (int i = 0; i < length; i++)
        {
            charactersPos[i] = Values.FirstPos + (Values.BetweenPos * i);
        }
    }

    private void AddActions()
    {
        GameManager.Instance.OnGameStart += (value) =>
        {
            if (value == true)
            {
                InitSet();
            }
            else
            {
                ClearData();
            }
        };

        this.ObserveEveryValueChanged(_ => ShowCharacterNum)
            .Skip(System.TimeSpan.Zero)
            .Subscribe(value =>
            {
                if (value == -1)
                    return;

                var nextSide = GetNextSide(value - 1);
                if(value == 2)
                {
                    ++leftOpenCount;    ++rightOpenCount;
                }
                else
                {
                    _ = nextSide == ESide.Left ? (++leftOpenCount) : (++rightOpenCount);
                }

                Debug.Log($"���ο� ĳ���� : {value} - {nextSide} : {leftOpenCount} - {rightOpenCount}");

                _GameUIManager.InGameOpenImage(nextSide, (value - 1) / 2);
            })
            .AddTo(this.gameObject);

        gameController.OnBomb += () => 
        {
            isSpawnBomb = true; 
        };

        gameController.OnItemBomb += () =>
        {
            // **��ź�� ���� �� ��ź ��� �ñ��� ����ؼ� �׳� ���� ����
            //var charType = spawnCharacters.First().Type;
            //PoolingManager.Instance.Enqueue(spawnCharacters.First().gameObject);
            //SpawnBomb(0);

            StartCoroutine(CoBomb(Random.Range(0f,1f) < 0.5f ? ESide.Left : ESide.Right));
        };

        gameController.OnBonus += value => bonusCharacterCo = StartCoroutine(CoBonusCharacter(value));

        gameController.ObserveEveryValueChanged(_ => gameController.IsFever)
            .Skip(System.TimeSpan.Zero)
            .Subscribe(value => SetFeverCharacter(value))
            .AddTo(this.gameObject);
    }

    private void InitSet()
    {
        ShowCharacterNum = 2;
        leftOpenCount = 0;              // **�Ѵ� ������ ���� ����� ����
        rightOpenCount = 0;

        SetCharacters();

        for (int i = 0, length = Values.MaxSpawnCharacterNum; i < length; i++)
        {
            SpawnCharacters(i);
        }

        scoreDis = gameController.ObserveEveryValueChanged(_ => gameController.Score)
            .Subscribe(value => CheckAddCharacter(value))
            .AddTo(this.gameObject);
    }

    /// <summary>
    /// ������ �ʱ�ȭ
    /// </summary>
    private void ClearData()
    {
        while (spawnCharacters.Count > 0)
        {
            GameObject go = spawnCharacters.Dequeue().gameObject;
            if(go.TryGetComponent(out BombController bombController))
            {
                Debug.Log($"Bomb�� Enqueue���� ���� : {go.name}");
            }    
            else
            {
                PoolingManager.Instance.Enqueue(go);
            }
        }

        nextOpen = 0;

        if(bonusCharacterCo != null)
            StopCoroutine(bonusCharacterCo);
        bonusCharacters.Clear();
        bonusCharacterCount = 0;

        ShowCharacterNum = -1;
    }

    /// <summary>
    /// ���� ���� �� ĳ���͸� 
    /// Left, Right�� �������� �־���� (0~5��������)
    /// </summary>
    public void SetCharacters()
    {
        bonusCharacters.Clear();
        randomCharacters = RandomCharList();

        leftSideChars.Clear();
        rightSideChars.Clear();

        var rand = Random.Range(0f, 1f) >= 0.5f ? 1 : 0;
        leftSideChars.Add(randomCharacters[rand]);
        rightSideChars.Add(randomCharacters[1 - rand]);

        rand = Random.Range(0f, 1f) >= 0.5f ? 1 : 0;
        leftSideChars.Add(randomCharacters[rand + 2]);
        rightSideChars.Add(randomCharacters[1 - rand + 2]);

        rand = Random.Range(0f, 1f) >= 0.5f ? 1 : 0;
        leftSideChars.Add(randomCharacters[rand + 4]);
        rightSideChars.Add(randomCharacters[1 - rand + 4]);


        tmpLeftChars = leftSideChars.ToList();
        tmpRightChars = rightSideChars.ToList();

        _GameUIManager.InGameSideImg(GetCharacterSprites(leftSideChars), GetCharacterSprites(rightSideChars));

        //foreach (var item in leftSideChars)
        //{
        //    Debug.Log($"Left : {item}");
        //}
        //foreach (var item in rightSideChars)
        //{
        //    Debug.Log($"Right : {item}");
        //}
    }

    /// <summary>
    /// order�� ���� �⺻ pos �ٸ��� �ϱ�
    /// ���� �����Ǵ� ĳ���ʹ� ������ �� �ڿ� ������
    /// </summary>
    public void SpawnCharacters(int order = -1)
    {
        // �����۵� ���� Ȯ��
        if (isSpawnBomb)
        {
            isSpawnBomb = false;
            SpawnBomb();
            return;
        }

        int layer = Values.MaxSpawnCharacterNum - order;
        if (order == -1)
        {
            order = Values.MaxSpawnCharacterNum - 1;
            layer = 0;
        }

        Vector3 spawnPos = charactersPos[order];            //  Values.FirstPos + (Values.BetweenPos * order);
        CharacterController newCharacter =
                    PoolingManager.Instance.Dequeue(CalcSpawnCharacter().ToString(), spawnPos, Quaternion.identity)
                    .GetComponent<CharacterController>();

        newCharacter.SetSortingGroup(layer);
        spawnCharacters.Enqueue(newCharacter);
    }


    private int sum;

    private ECharacter CalcSpawnCharacter()
    {
        if (isFever)
        {   // �ǹ�
            return feverCharacter;
        }
        else if(bonusCharacterCount == 0)
        {   // ���ʽ� ���� ���
            return randomCharacters[Random.Range(0, ShowCharacterNum)];
        }

        // Ȯ�� ���ϱ�
        sum = 0;
        int[] sums = new int[ShowCharacterNum];
        float addValue;

        for (int i = 0; i < ShowCharacterNum; i++)
        {
            addValue = (bonusCharacters[randomCharacters[i]] == true) ? 
                            (Values.BaseSum * Values.BonusCharacter_value) : Values.BaseSum;

            sum += (int)addValue;
            sums[i] += (int)addValue;
        }

        int rand = Random.Range(1, sum);
        int idx = 0;
        while (idx < ShowCharacterNum)
        {
            if (rand < sums[idx])
            {
                return randomCharacters[idx];
            }
            rand -= sums[idx++];
        }

        return randomCharacters[0];
    }

    /// <summary>
    /// ���� ��� ���� ĳ���� �߰�
    /// UI Open + �߾� ����Ʈ�� �߰�
    /// </summary>
    public void CheckAddCharacter(int value)
    {
        if (value < Values.OpenScores[nextOpen])
            return;

        ++ShowCharacterNum;
        if (++nextOpen >= Values.OpenScores.Length)
        {
            scoreDis.Dispose();
        }
    }

    /// <summary>
    /// ������� ĳ���� ���� Ȯ��
    /// </summary>
    private ESide GetNextSide(int num)
    {
        ESide result;
        int arrayNum = num / 2;
        // Debug.Log($"{num} / {arrayNum} / {leftSideChars[arrayNum]} or {rightSideChars[arrayNum]} -> {randomCharacters[num]}");

        result = leftSideChars[arrayNum] == randomCharacters[num] ? ESide.Left : ESide.Right;

        return result;
    }

    /// <summary>
    /// �������� ����Ʈ ����
    /// *Sprite -> Enum������ ���� == �������� ���ڼ���
    /// </summary>
    private List<ECharacter> RandomCharList()
    {
        List<ECharacter> result = new List<ECharacter>();
        List<ECharacter> tmpList = characterTypes.ToList();
        int rand = -1;

        int length = Values.MaxShowCharacterNum;
        for (int i = 0; i < length; i++)
        {
            rand = Random.Range(0, tmpList.Count);
            result.Add(tmpList[rand]);
            bonusCharacters.Add(tmpList[rand], false);

            tmpList.RemoveAt(rand);
        }

        // Debug.Log($"Set : {result[0].name} / {result[1].name} / {result[2].name} / {result[3].name} / {result[4].name} / {result[5].name} / ");

        return result;
    }

    public void DetectCharacter(ESide side)
    {
        //Debug.Log($"{side} {spawnCharacters.First().sprite}");

        // Reverse�� side �ݴ� ����
        if(gameController.IsReverse)
        {
            side = ReverseSide(side);
        }

        var charType = spawnCharacters.First().Type;

        if (charType == ECharacter.Bomb)
        {
            StartCoroutine(CoBomb(side));
            return;
        }

        bool bCorrect = CheckCorrectSide(side, charType);

        // �ǵ� ��� ��������
        if(!bCorrect && gameController.UseShield())
        {
            bCorrect = true;
            side = ReverseSide(side);

            AudioManager.Instance.PlaySFX(ESFX.Shield);
            PoolingManager.Instance.Dequeue(Values.Key_ShieldEffect, Vector3.zero, Quaternion.identity, true);
        }

        // ���ʽ� ĳ���� ���� �ʿ�
        gameController.IsBonus = bonusCharacters[charType];

        gameController.OnCorrect?.Invoke(bCorrect);
        if (!bCorrect)
        {
            spawnCharacters.First().PlayFail();
            return;
        }

        // ��ƼŬ ���
        PoolingManager.Instance.Dequeue(Values.Key_CorrectEffect, Values.FrontParticlePos, Quaternion.identity);

        // Move
        MoveCharacters(side);

        // Spawn
        SpawnCharacters();
    }

    /// <summary>
    /// �� �Ʒ� ĳ���� ����/������ �̵�
    /// ������ �Ʒ��� �̵�
    /// </summary>
    void MoveCharacters(ESide side)
    {
        CharacterController character = spawnCharacters.Dequeue();
        character.OutMove(side == ESide.Left ? Values.LeftOutPos : Values.RightOutPos, outTime, outEase);

        // *�� ���� ���� ��� �߰������ۿ� �������� ���� -> tr-between���� �������� �� �ʿ�
        int arrayNum = 0;
        foreach (var spawnCharacter in spawnCharacters)
        {
            spawnCharacter.DownMove(charactersPos[arrayNum++], moveTime, moveEase);
        }
    }

    /// <summary>
    /// �´��� Ȯ��
    /// </summary>
    private bool CheckCorrectSide(ESide side, ECharacter type)
    {
        //Debug.Log($"{side} + {type}");

        if(side == ESide.Left)
        {
            if(tmpLeftChars.Contains(type))
            {
                // Move();
                return true;
            }
        }
        else
        {
            if(tmpRightChars.Contains(type))
            {
                return true;
            }
        }

        return false;
    }

    private ESide ReverseSide(ESide side)
    {
        return side == ESide.Left ? ESide.Right : ESide.Left;
    }

    #region Bomb

    // *�̹��� ����
    public void SpawnBomb(int order = -1)
    {
        int layer = Values.MaxSpawnCharacterNum - order;
        if (order == -1)
        {
            order = Values.MaxSpawnCharacterNum - 1;
            layer = 0;
        }

        Vector3 spawnPos = charactersPos[order];

        // Ǯ��
        var newBomb =
            PoolingManager.Instance.Dequeue(ECharacter.Bomb.ToString(), spawnPos, Quaternion.identity)
            .GetComponent<Bomb>();

        CharacterController bombController = newBomb.GetController();

        bombController.SetSortingGroup(layer);
        spawnCharacters.Enqueue(bombController);
    }

    IEnumerator CoBomb(ESide side)
    {
        --GameManager.Instance.InputValue;

        int count = 0;
        gameController.IsBomb = true;
        OnStartBomb?.Invoke(); 

        while(count < gameController.BombCharacter)
        {
            // Debug.Log(spawnCharacters.First().Type);

            // ��ź �߰��� ���� ������ ���
            if (spawnCharacters.Count <= 0)
                break;

            gameController.OnCorrect?.Invoke(true);

            // spawnCharacters.First().Type;
            PoolingManager.Instance.Dequeue(Values.Key_CorrectEffect, 
                        Values.FrontParticlePos + Vector3.up * count * 0.4f, Quaternion.identity);

            MoveCharacters(side);

            SpawnCharacters();

            yield return Values.BombDelay;
            count++;
        }

        gameController.OnEndBomb?.Invoke();
        gameController.OnEndBomb = null;
        gameController.IsBomb = false;

        ++GameManager.Instance.InputValue;
    }

    #endregion

    /// <summary>
    /// �������� ���ʽ� ĳ���� ����. ���� �������� ĳ���ʹ� ���� �ȵǰ�
    /// </summary>
    public IEnumerator CoBonusCharacter(InGameItemData item)
    {
        WaitForSeconds waitTime = new WaitForSeconds(item.valueTime);

        if(isFever)
        {
            // �ǹ� ���¿����� Ư�� ĳ���͸� Ȱ��ȭ �Ǿ���� + ��� ĳ���� ����Ʈ Ȱ��ȭ
            bonusCharacters[0] = true;
            bonusCharacterCount++;
            _GameUIManager.InGameFeverBonus(true);
            yield return waitTime;
            bonusCharacters[0] = false;
            bonusCharacterCount--;
            _GameUIManager.InGameFeverBonus(false);
        }
        else
        {
            int rand;
            ESide side = 0;
            int arrayNum = 0, count = 0;
            ECharacter bonusCharacter = ECharacter.Bomb;
            while (count < 10)
            {   // ���� �ִ� ĳ���� �� �߿��� �ɷ�����
                rand = Random.Range(0, leftOpenCount + rightOpenCount);
                if (rand < leftOpenCount)
                {
                    side = ESide.Left;
                    arrayNum = rand;

                    bonusCharacter = leftSideChars[arrayNum];
                }
                else
                {
                    arrayNum = rand - leftOpenCount;
                    side = ESide.Right;

                    bonusCharacter = rightSideChars[arrayNum];
                }

                if (!bonusCharacters[bonusCharacter])
                {
                    break;
                }

                count++;
            }

            bonusCharacters[bonusCharacter] = true;
            bonusCharacterCount++;
            _GameUIManager.InGameBonusCharImg(true, side, arrayNum);
            yield return waitTime;
            bonusCharacters[bonusCharacter] = false;
            bonusCharacterCount--;
            _GameUIManager.InGameBonusCharImg(false, side, arrayNum);
        }
    }

    protected void SetFeverCharacter(bool isFever)
    {
        this.isFever = isFever;

        if (isFever)
        {
            feverCharacter = leftSideChars[0];          // 0��ĳ���� ���� �����ϱ�
            //var rand = Random.Range(0, leftOpenCount + rightOpenCount);
            //if (rand < leftOpenCount)
            //{
            //    feverCharacter = leftSideChars[rand];
            //}
            //else
            //{
            //    feverCharacter = rightSideChars[rand - leftOpenCount];
            //}

            tmpLeftChars.Clear();
            tmpRightChars.Clear();

            tmpLeftChars.Add(feverCharacter);
            tmpRightChars.Add(feverCharacter);

            tmpLeftChars.Add(feverCharacter);
            tmpRightChars.Add(feverCharacter);

            tmpLeftChars.Add(feverCharacter);
            tmpRightChars.Add(feverCharacter);

            _GameUIManager.InGameSideImg(GetCharacterSprites(tmpLeftChars), GetCharacterSprites(tmpRightChars));

            // ���� ĳ���͵� ���� ��Ű��
            while (spawnCharacters.Count > 0)
            {
                GameObject go = spawnCharacters.Dequeue().gameObject;
                PoolingManager.Instance.Enqueue(go);
            }

            for (int i = 0, length = Values.MaxSpawnCharacterNum; i < length; i++)
            {
                SpawnCharacters(i);
            }

            // Debug.Log("���õ�");
        }
        else
        {
            // ĳ���� �̹��� �ǵ�����

            tmpLeftChars = leftSideChars.ToList();
            tmpRightChars = rightSideChars.ToList();

            _GameUIManager.InGameSideImg(GetCharacterSprites(tmpLeftChars), GetCharacterSprites(tmpRightChars));
        }

        //if (isFever)
        //{
        //    // ĳ���� �̹��� �ϳ��� ����
        //    tmpLeftCharacters = leftSideChars.ToList();
        //    tmpRightCharacters = rightSideChars.ToList();

        //    var rand = Random.Range(0, leftOpenCount + rightOpenCount);
        //    if (rand < leftOpenCount)
        //    {
        //        feverCharacter = leftSideChars[rand];
        //    }
        //    else
        //    {
        //        feverCharacter = rightSideChars[rand - leftOpenCount];
        //    }

        //    leftSideChars.Clear();
        //    rightSideChars.Clear();

        //    leftSideChars.Add(feverCharacter);
        //    rightSideChars.Add(feverCharacter);

        //    leftSideChars.Add(feverCharacter);
        //    rightSideChars.Add(feverCharacter);

        //    leftSideChars.Add(feverCharacter);
        //    rightSideChars.Add(feverCharacter);

        //    _GameUIManager.InGameSideImg(GetCharacterSprites(ESide.Left), GetCharacterSprites(ESide.Right));

        //    // ���� ĳ���͵� ���� ��Ű��
        //    while (spawnCharacters.Count > 0)
        //    {
        //        GameObject go = spawnCharacters.Dequeue().gameObject;
        //        PoolingManager.Instance.Enqueue(go);
        //    }

        //    for (int i = 0, length = Values.MaxSpawnCharacterNum; i < length; i++)
        //    {
        //        SpawnCharacters(i);
        //    }

        //    Debug.Log("���õ�");
        //}
        //else
        //{
        //    // ĳ���� �̹��� �ǵ�����

        //    leftSideChars = tmpLeftCharacters;
        //    rightSideChars = tmpRightCharacters;

        //    _GameUIManager.InGameSideImg(GetCharacterSprites(ESide.Left), GetCharacterSprites(ESide.Right));
        //}
    }

    /// <summary>
    /// ĳ���� �ڼ���
    /// *SetCharacters�� �ٸ����� �̹� �ִ� ĳ���Ϳ��� ������ ������ �Ѵٴ� ��
    /// ��ȣ ������ ������ ������ ��
    /// </summary>
    public void Jumble()
    {
        List<ECharacter> result = new List<ECharacter>();
        List<ECharacter> tmpList = randomCharacters.ToList();
        int rand = -1;

        //foreach (var item in tmpList)
        //{
        //    Debug.Log("Tmp Item : " + item.ToString());
        //}

        for (int i = 0, length = ShowCharacterNum; i < length; i++)
        {
            rand = Random.Range(0, length - i);
            result.Add(tmpList[rand]);
            tmpList.RemoveAt(rand);
            //Debug.Log(tmpList[rand].ToString());
        }
        foreach (var item in tmpList)
        {
            result.Add(item);
        }
        randomCharacters = result;

        // ������� �־��ֱ�
        var leftCount = leftOpenCount;
        var rightCount = rightOpenCount;

        Debug.Log($"{leftCount} / {rightCount}");

        var tmpLeftChars = new List<ECharacter>();
        var tmpRightChars = new List<ECharacter>();

        for (int i = 0; i < ShowCharacterNum; i++)
        {
            if(i%2 == 0)
            {
                if (leftCount > 0)
                {
                    --leftCount;
                    tmpLeftChars.Add(randomCharacters[i]);
                }
                else
                {
                    --rightCount;
                    tmpRightChars.Add(randomCharacters[i]);
                }
            }
            else if(i%2 == 1)
            {
                if(rightCount > 0)
                {
                    --rightCount;
                    tmpRightChars.Add(randomCharacters[i]);
                }
                else
                {
                    --leftCount;
                    tmpLeftChars.Add(randomCharacters[i]);
                }
            }
        }

        int sideLength = (int)(Values.MaxShowCharacterNum * 0.5f);

        for (int i = leftOpenCount; i < sideLength; i++)
        {
            tmpLeftChars.Add(leftSideChars[i]);
        }
        for (int i = rightOpenCount; i < sideLength; i++)
        {
            tmpRightChars.Add(rightSideChars[i]);
        }

        leftSideChars = tmpLeftChars;
        rightSideChars = tmpRightChars;

        this.tmpLeftChars = leftSideChars.ToList();
        this.tmpRightChars = rightSideChars.ToList();

        _GameUIManager.InGameSideImg(GetCharacterSprites(leftSideChars), GetCharacterSprites(rightSideChars));
    }

    protected List<Sprite> GetCharacterSprites(List<ECharacter> characters)
    {
        var result = new List<Sprite>();

        foreach (var character in characters)
        {
            result.Add(CharacterDictionary[character].headSprite);
        }

        //if(side == ESide.Left)
        //{
        //    foreach (var character in leftSideChars)
        //    {
        //        result.Add(CharacterDictionary[character].headSprite);
        //    }
        //}
        //else
        //{
        //    foreach (var character in rightSideChars)
        //    {
        //        result.Add(CharacterDictionary[character].headSprite);
        //    }
        //}

        return result;
    }
}
