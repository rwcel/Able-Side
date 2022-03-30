using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;
using DG.Tweening;


/// <summary>
/// 캐릭터 스폰, 위치 관련 함수
/// </summary>
public class GameCharacter : MonoBehaviour
{
    // **밖으로 뺄 생각도?
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

    // 기타 스크립트
    protected GameController gameController;
    protected GameUIManager _GameUIManager;

    protected Sequence moveSequence;
    protected Sequence outSequence;

    // 게임 시작 시 변할 수 있음
    protected List<ECharacter> leftSideChars = new List<ECharacter>();
    protected List<ECharacter> rightSideChars = new List<ECharacter>();

    // 임시 저장 값
    protected List<ECharacter> tmpLeftChars = new List<ECharacter>();
    protected List<ECharacter> tmpRightChars = new List<ECharacter>();

    public int ShowCharacterNum { get; private set; }       // 현재 보여주고 있는 캐릭터 수

    protected Queue<CharacterController> spawnCharacters = new Queue<CharacterController>();

    protected Vector3[] charactersPos;
    // protected CharacterController outCharacter;         // *게임 나갈 때 이동중이라면 Dequeue 되었기때문에 없어지지 않음 -> 그냥 냅두기

    // 캐릭터 열리는 점수 배열
    protected int nextOpen = 0;
    private System.IDisposable scoreDis;

    private int leftOpenCount = 0;
    private int rightOpenCount = 0;

    // 폭탄관련 변수
    private bool isSpawnBomb;

    // 피버
    private ECharacter feverCharacter;
    private bool isFever;                   // GameController의 Fever

    // 보너스 캐릭터
    protected Coroutine bonusCharacterCo;

    public System.Action OnStartBomb;

    // 풀 개수 생성 -> 중앙에서 나오는
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

        // **Bomb이 들어갈 수 있음
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

                Debug.Log($"새로운 캐릭터 : {value} - {nextSide} : {leftOpenCount} - {rightOpenCount}");

                _GameUIManager.InGameOpenImage(nextSide, (value - 1) / 2);
            })
            .AddTo(this.gameObject);

        gameController.OnBomb += () => 
        {
            isSpawnBomb = true; 
        };

        gameController.OnItemBomb += () =>
        {
            // **폭탄이 있을 때 폭탄 사용 시까지 고려해서 그냥 랜덤 실행
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
        leftOpenCount = 0;              // **둘다 열릴때 왼쪽 계산을 안함
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
    /// 데이터 초기화
    /// </summary>
    private void ClearData()
    {
        while (spawnCharacters.Count > 0)
        {
            GameObject go = spawnCharacters.Dequeue().gameObject;
            if(go.TryGetComponent(out BombController bombController))
            {
                Debug.Log($"Bomb은 Enqueue하지 않음 : {go.name}");
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
    /// 랜덤 정렬 된 캐릭터를 
    /// Left, Right에 랜덤으로 넣어놓기 (0~5번까지만)
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
    /// order에 따라 기본 pos 다르게 하기
    /// 이후 스폰되는 캐릭터는 무조건 맨 뒤에 생성됨
    /// </summary>
    public void SpawnCharacters(int order = -1)
    {
        // 아이템등 스폰 확인
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
        {   // 피버
            return feverCharacter;
        }
        else if(bonusCharacterCount == 0)
        {   // 보너스 없는 경우
            return randomCharacters[Random.Range(0, ShowCharacterNum)];
        }

        // 확률 더하기
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
    /// 점수 향상에 따른 캐릭터 추가
    /// UI Open + 중앙 리스트에 추가
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
    /// 열어야할 캐릭터 방향 확인
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
    /// 랜덤으로 리스트 섞기
    /// *Sprite -> Enum값으로 변경 == 랜덤으로 숫자섞기
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

        // Reverse면 side 반대 적용
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

        // 실드 사용 가능한지
        if(!bCorrect && gameController.UseShield())
        {
            bCorrect = true;
            side = ReverseSide(side);

            AudioManager.Instance.PlaySFX(ESFX.Shield);
            PoolingManager.Instance.Dequeue(Values.Key_ShieldEffect, Vector3.zero, Quaternion.identity, true);
        }

        // 보너스 캐릭터 조사 필요
        gameController.IsBonus = bonusCharacters[charType];

        gameController.OnCorrect?.Invoke(bCorrect);
        if (!bCorrect)
        {
            spawnCharacters.First().PlayFail();
            return;
        }

        // 파티클 재생
        PoolingManager.Instance.Dequeue(Values.Key_CorrectEffect, Values.FrontParticlePos, Quaternion.identity);

        // Move
        MoveCharacters(side);

        // Spawn
        SpawnCharacters();
    }

    /// <summary>
    /// 맨 아래 캐릭터 왼쪽/오른쪽 이동
    /// 나머지 아래로 이동
    /// </summary>
    void MoveCharacters(ESide side)
    {
        CharacterController character = spawnCharacters.Dequeue();
        character.OutMove(side == ESide.Left ? Values.LeftOutPos : Values.RightOutPos, outTime, outEase);

        // *그 전에 맞출 경우 중간까지밖에 내려오지 않음 -> tr-between말고 절대적인 값 필요
        int arrayNum = 0;
        foreach (var spawnCharacter in spawnCharacters)
        {
            spawnCharacter.DownMove(charactersPos[arrayNum++], moveTime, moveEase);
        }
    }

    /// <summary>
    /// 맞는지 확인
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

    // *이미지 없음
    public void SpawnBomb(int order = -1)
    {
        int layer = Values.MaxSpawnCharacterNum - order;
        if (order == -1)
        {
            order = Values.MaxSpawnCharacterNum - 1;
            layer = 0;
        }

        Vector3 spawnPos = charactersPos[order];

        // 풀링
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

            // 폭탄 중간에 게임 종료한 경우
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
    /// 랜덤으로 보너스 캐릭터 설정. 현재 적용중인 캐릭터는 적용 안되게
    /// </summary>
    public IEnumerator CoBonusCharacter(InGameItemData item)
    {
        WaitForSeconds waitTime = new WaitForSeconds(item.valueTime);

        if(isFever)
        {
            // 피버 상태에서는 특정 캐릭터만 활성화 되어야함 + 모든 캐릭터 이펙트 활성화
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
            {   // 현재 있는 캐릭터 들 중에서 걸려야함
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
            feverCharacter = leftSideChars[0];          // 0번캐릭터 고정 지정하기
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

            // 게임 캐릭터들 통일 시키기
            while (spawnCharacters.Count > 0)
            {
                GameObject go = spawnCharacters.Dequeue().gameObject;
                PoolingManager.Instance.Enqueue(go);
            }

            for (int i = 0, length = Values.MaxSpawnCharacterNum; i < length; i++)
            {
                SpawnCharacters(i);
            }

            // Debug.Log("선택됨");
        }
        else
        {
            // 캐릭터 이미지 되돌리기

            tmpLeftChars = leftSideChars.ToList();
            tmpRightChars = rightSideChars.ToList();

            _GameUIManager.InGameSideImg(GetCharacterSprites(tmpLeftChars), GetCharacterSprites(tmpRightChars));
        }

        //if (isFever)
        //{
        //    // 캐릭터 이미지 하나로 통일
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

        //    // 게임 캐릭터들 통일 시키기
        //    while (spawnCharacters.Count > 0)
        //    {
        //        GameObject go = spawnCharacters.Dequeue().gameObject;
        //        PoolingManager.Instance.Enqueue(go);
        //    }

        //    for (int i = 0, length = Values.MaxSpawnCharacterNum; i < length; i++)
        //    {
        //        SpawnCharacters(i);
        //    }

        //    Debug.Log("선택됨");
        //}
        //else
        //{
        //    // 캐릭터 이미지 되돌리기

        //    leftSideChars = tmpLeftCharacters;
        //    rightSideChars = tmpRightCharacters;

        //    _GameUIManager.InGameSideImg(GetCharacterSprites(ESide.Left), GetCharacterSprites(ESide.Right));
        //}
    }

    /// <summary>
    /// 캐릭터 뒤섞기
    /// *SetCharacters와 다른점은 이미 있는 캐릭터에서 랜덤을 돌려야 한다는 것
    /// 번호 내에서 랜덤을 돌려야 함
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

        // 순서대로 넣어주기
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
