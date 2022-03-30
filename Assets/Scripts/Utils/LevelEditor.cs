#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LevelEditor : EditorWindow
{
    private enum EToolbar
    {
        Character,
        InGame,
        OutGame,
        Difficulty,
        Item,
    }

    public Vector3 FirstPos;
    public Vector3 BetweenPos;

    private static LevelEditor window;
    private static EToolbar selected;
    private string[] toolbarStrings = { "Character", "InGame", "OutGame", "Difficulty" ,"Item" };

    private LevelData levelData;

    [MenuItem("Able Games/Level Editor")]
    public static void Init()
    {
        window = (LevelEditor)GetWindow(typeof(LevelEditor), false, "Level Editor");
        window.Show();

        //levelData = LoadingManager.LoadlLevel(currentLevel, levelData);
    }

    [MenuItem("Able Games/Settings/Character Setting")]
    public static void CharacterSetting()
    {
        Init();
        selected = EToolbar.Character;
    }

    [MenuItem("Able Games/Settings/InGame Setting")]
    public static void InGameSetting()
    {
        Init();
        selected = EToolbar.InGame;
    }

    [MenuItem("Able Games/Settings/OutGame Setting")]
    public static void OutGameSetting()
    {
        Init();
        selected = EToolbar.OutGame;
    }

    [MenuItem("Able Games/Settings/Difficulty Setting")]
    public static void DIfficultySetting()
    {
        Init();
        selected = EToolbar.OutGame;
    }

    [MenuItem("Able Games/Settings/Item Setting")]
    public static void ItemSetting()
    {
        Init();
        selected = EToolbar.Item;
    }

    private void OnFocus()
    {
        // ScriptableObject ���� ����
        levelData = Resources.Load("LevelData") as LevelData;
        if (levelData == null)
            Debug.Log("Null");
    }

    private void OnGUI()
    {
        GUILayout.Space(15);
        GUILayout.Label("�� Editor ���� �� ����� �� �ʱ�ȭ �Ǵ� ��찡 ������ ��� ���� �Ŀ� ���� ������ ���ּ���.", GUILayout.Width(500));
        GUILayout.Space(5);
        if (GUILayout.Button("���� ����", GUILayout.Width(120)))
        {
            OpenLevelData();
        }

        GUILayout.Space(30);
        GUILayout.BeginHorizontal();
        selected = (EToolbar)GUILayout.Toolbar((int)selected, toolbarStrings, GUILayout.Width(360));
        GUILayout.EndHorizontal();


        if (levelData == null)
            return;

        switch (selected)
        {
            case EToolbar.Character:
                CharacterGUI();
                break;
            case EToolbar.InGame:
                InGameGUI();
                break;
            case EToolbar.OutGame:
                OutGameGUI();
                break;
            case EToolbar.Difficulty:
                DifficultyGUI();
                break;
            case EToolbar.Item:
                ItemGUI();
                break;
            default:
                break;
        }

        if (UnityEngine.GUI.changed && !EditorApplication.isPlaying)
            EditorSceneManager.MarkAllScenesDirty();
    }

    void CharacterGUI()
    {
        GUILayout.Space(20);
        GUILayout.Label("[ĳ���� ��]");

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - ���� �ִ� ĳ���� ��", GUILayout.Width(180));
        GUILayout.Space(70);
        levelData.MaxShowCharacterNum = EditorGUILayout.IntField("", levelData.MaxShowCharacterNum, GUILayout.Width(50));
        //Values.MaxShowCharacterNum = EditorGUILayout.IntField("", Values.MaxShowCharacterNum, GUILayout.Width(50));
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - ���ο� ���� ĳ���� ��", GUILayout.Width(180));
        GUILayout.Space(70);
        levelData.MaxSpawnCharacterNum = EditorGUILayout.IntField("", levelData.MaxSpawnCharacterNum, GUILayout.Width(50));
        //Values.MaxSpawnCharacterNum = EditorGUILayout.IntField("", Values.MaxSpawnCharacterNum, GUILayout.Width(50));
        GUILayout.EndHorizontal();


        GUILayout.Space(15);
        GUILayout.Label("[��ġ]");

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - �� ó�� ĳ����", GUILayout.Width(180));
        GUILayout.Space(70);
        levelData.FirstPos = EditorGUILayout.Vector3Field("", levelData.FirstPos, GUILayout.Width(200));
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - ĳ���� ����", GUILayout.Width(180));
        GUILayout.Space(70);
        levelData.BetweenPos = EditorGUILayout.Vector3Field("", levelData.BetweenPos, GUILayout.Width(200));
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - ���������� ����", GUILayout.Width(180));
        GUILayout.Space(70);
        levelData.LeftOutPos = EditorGUILayout.Vector3Field("", levelData.LeftOutPos, GUILayout.Width(200));
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - ���������� ������", GUILayout.Width(180));
        GUILayout.Space(70);
        levelData.RightOutPos = EditorGUILayout.Vector3Field("", levelData.RightOutPos, GUILayout.Width(200));
        GUILayout.EndHorizontal();
    }

    void InGameGUI()
    {
        GUILayout.Space(25);
        GUILayout.Label("[�ΰ��� ����]");

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - �⺻ �ð�", GUILayout.Width(180));
        GUILayout.Space(70);
        levelData.MaxTimeBase = EditorGUILayout.FloatField("", levelData.MaxTimeBase, GUILayout.Width(50));
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - �⺻ ����", GUILayout.Width(180));
        GUILayout.Space(70);
        levelData.BaseScore = EditorGUILayout.IntField("", levelData.BaseScore, GUILayout.Width(120));
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - ĳ���� �ر� ����", GUILayout.Width(180));
        GUILayout.Space(70);
        GUILayout.BeginVertical();
        for (int i = 0, length = levelData.OpenScores.Length; i < length; i++)
        {
            levelData.OpenScores[i] = EditorGUILayout.IntField("", levelData.OpenScores[i], GUILayout.Width(120));
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        // Score Time
        GUILayout.Space(25);
        GUILayout.BeginHorizontal();
        GUILayout.Label(" [���� & �ð� ����]", GUILayout.Width(150));
        GUILayout.Space(70);
        GUILayout.Label("���� ����", GUILayout.Width(110));
        GUILayout.Space(10);
        GUILayout.Label("�ʴ� ���� �ð�", GUILayout.Width(100));
        GUILayout.Space(15);
        GUILayout.Label("���� �� �߰� �ð�", GUILayout.Width(100));
        GUILayout.Space(20);
        GUILayout.Label("���� �� ���� �ð�", GUILayout.Width(100));

        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Space(220);
        GUILayout.BeginVertical();

        for (int i = 0, length = levelData.TimeInfos.Length; i < length; i++)
        {
            GUILayout.BeginHorizontal();
            levelData.TimeInfos[i].score = EditorGUILayout.IntField("", levelData.TimeInfos[i].score, GUILayout.Width(120));
            levelData.TimeInfos[i].decreaseTime = EditorGUILayout.FloatField("", levelData.TimeInfos[i].decreaseTime, GUILayout.Width(120));
            levelData.TimeInfos[i].successTime = EditorGUILayout.FloatField("", levelData.TimeInfos[i].successTime, GUILayout.Width(120));
            levelData.TimeInfos[i].failTime = EditorGUILayout.FloatField("", levelData.TimeInfos[i].failTime, GUILayout.Width(120));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        // Score Combo
        GUILayout.Space(25);
        GUILayout.BeginHorizontal();
        GUILayout.Label(" [���� & �޺� ����]", GUILayout.Width(150));
        GUILayout.Space(70);
        GUILayout.Label("���� �޺�", GUILayout.Width(110));
        GUILayout.Space(10);
        GUILayout.Label("������ ����", GUILayout.Width(100));
        GUILayout.Space(15);
        GUILayout.Label("���� �޼� ����", GUILayout.Width(100));

        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Space(220);
        GUILayout.BeginVertical();

        for (int i = 0, length = levelData.ScoreComboInfos.Length; i < length; i++)
        {
            GUILayout.BeginHorizontal();
            levelData.ScoreComboInfos[i].combo = EditorGUILayout.IntField("", levelData.ScoreComboInfos[i].combo, GUILayout.Width(120));
            levelData.ScoreComboInfos[i].score = EditorGUILayout.IntField("", levelData.ScoreComboInfos[i].score, GUILayout.Width(120));
            levelData.ScoreComboInfos[i].rewardScore = EditorGUILayout.IntField("", levelData.ScoreComboInfos[i].rewardScore, GUILayout.Width(120));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        // Item Combo
        GUILayout.Space(25);
        GUILayout.BeginHorizontal();
        GUILayout.Label(" [�޺� & ������ ����]", GUILayout.Width(150));
        GUILayout.Space(70);
        GUILayout.Label("�ش� �޺� �̸�", GUILayout.Width(110));
        GUILayout.Space(10);
        GUILayout.Label("�Ϲ� ���� ����", GUILayout.Width(110));
        GUILayout.Space(10);
        GUILayout.Label("�Ϲ� ���� Ȯ��", GUILayout.Width(110));
        GUILayout.Space(10);
        GUILayout.Label("������ ���� ����", GUILayout.Width(110));
        GUILayout.Space(10);
        GUILayout.Label("������ ���� Ȯ��", GUILayout.Width(110));

        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Space(220);
        GUILayout.BeginVertical();

        for (int i = 0, length = levelData.ItemComboInfos.Length; i < length; i++)
        {
            GUILayout.BeginHorizontal();
            levelData.ItemComboInfos[i].combo = EditorGUILayout.IntField("", levelData.ItemComboInfos[i].combo, GUILayout.Width(120));
            levelData.ItemComboInfos[i].normalBoxCount = EditorGUILayout.IntField("", levelData.ItemComboInfos[i].normalBoxCount, GUILayout.Width(120));
            levelData.ItemComboInfos[i].normalBoxPercent = EditorGUILayout.FloatField("", levelData.ItemComboInfos[i].normalBoxPercent, GUILayout.Width(120));
            levelData.ItemComboInfos[i].rareBoxCount = EditorGUILayout.IntField("", levelData.ItemComboInfos[i].rareBoxCount, GUILayout.Width(120));
            levelData.ItemComboInfos[i].rareBoxPercent = EditorGUILayout.FloatField("", levelData.ItemComboInfos[i].rareBoxPercent, GUILayout.Width(120));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    void OutGameGUI()
    {
        GUILayout.Space(25);
        GUILayout.Label("[Ƽ��]");

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("�ִ� ����", GUILayout.Width(120));
        GUILayout.Space(70);
        levelData.MaxTicket = EditorGUILayout.IntField("", levelData.MaxTicket, GUILayout.Width(50));
        //Values.MaxShowCharacterNum = EditorGUILayout.IntField("", Values.MaxShowCharacterNum, GUILayout.Width(50));
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("���� �ð�", GUILayout.Width(120));
        GUILayout.Space(70);
        levelData.TicketTime = EditorGUILayout.IntField("", levelData.TicketTime, GUILayout.Width(50));
        //Values.MaxShowCharacterNum = EditorGUILayout.IntField("", Values.MaxShowCharacterNum, GUILayout.Width(50));
        GUILayout.EndHorizontal();
    }

    void DifficultyGUI()
    {
        GUILayout.Space(25);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - ���� �ڼ��� (����)", GUILayout.Width(180));
        GUILayout.Space(70);
        GUILayout.Label("���� ����", GUILayout.Width(110));
        GUILayout.Space(10);
        GUILayout.Label("Ȯ�� ��� �̵� ��", GUILayout.Width(100));
        GUILayout.Space(15);
        GUILayout.Label("Ȯ��", GUILayout.Width(100));

        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Space(250);
        GUILayout.BeginVertical();

        for (int i = 0, length = levelData.JumbleInfos.Length; i < length; i++)
        {
            GUILayout.BeginHorizontal();
            levelData.JumbleInfos[i].score = EditorGUILayout.IntField("", levelData.JumbleInfos[i].score, GUILayout.Width(120));
            levelData.JumbleInfos[i].count = EditorGUILayout.IntField("", levelData.JumbleInfos[i].count, GUILayout.Width(120));
            levelData.JumbleInfos[i].percent = EditorGUILayout.IntField("", levelData.JumbleInfos[i].percent, GUILayout.Width(120));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Space(20);
        GUILayout.Label("  - ���� �ð�", GUILayout.Width(80));
        GUILayout.Space(20);
        levelData.JumblePauseTime = EditorGUILayout.FloatField("", levelData.JumblePauseTime, GUILayout.Width(50));

        GUILayout.EndHorizontal();


        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - ������", GUILayout.Width(180));
        GUILayout.Space(70);
        GUILayout.Label("���� ����", GUILayout.Width(110));
        GUILayout.Space(10);
        GUILayout.Label("Ȯ�� ��� �̵� ��", GUILayout.Width(100));
        GUILayout.Space(15);
        GUILayout.Label("Ȯ��", GUILayout.Width(100));
        GUILayout.Space(20);
        GUILayout.Label("���� �ð�", GUILayout.Width(100));

        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Space(250);
        GUILayout.BeginVertical();

        for (int i = 0, length = levelData.ReverseInfos.Length; i < length; i++)
        {
            GUILayout.BeginHorizontal();
            levelData.ReverseInfos[i].score = EditorGUILayout.IntField("", levelData.ReverseInfos[i].score, GUILayout.Width(120));
            levelData.ReverseInfos[i].count = EditorGUILayout.IntField("", levelData.ReverseInfos[i].count, GUILayout.Width(120));
            levelData.ReverseInfos[i].percent = EditorGUILayout.IntField("", levelData.ReverseInfos[i].percent, GUILayout.Width(120));
            levelData.ReverseInfos[i].time = EditorGUILayout.FloatField("", levelData.ReverseInfos[i].time, GUILayout.Width(120));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Space(20);
        GUILayout.Label("  - ���� �ð�", GUILayout.Width(80));
        GUILayout.Space(20);
        levelData.ReversePauseTime = EditorGUILayout.FloatField("", levelData.ReversePauseTime, GUILayout.Width(50));

        GUILayout.EndHorizontal();


        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - ������", GUILayout.Width(180));
        GUILayout.Space(70);
        GUILayout.Label("���� ����", GUILayout.Width(110));
        GUILayout.Space(10);
        GUILayout.Label("Ȯ�� ��� �̵� ��", GUILayout.Width(100));
        GUILayout.Space(15);
        GUILayout.Label("Ȯ��", GUILayout.Width(100));
        GUILayout.Space(20);
        GUILayout.Label("���� �ð�", GUILayout.Width(100));

        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Space(250);
        GUILayout.BeginVertical();

        FBlurInfo blurInfo;
        for (int i = 0, length = levelData.BlurInfos.Length; i < length; i++)
        {
            GUILayout.BeginHorizontal();
            blurInfo.score = EditorGUILayout.IntField("", levelData.BlurInfos[i].score, GUILayout.Width(120));
            blurInfo.count = EditorGUILayout.IntField("", levelData.BlurInfos[i].count, GUILayout.Width(120));
            blurInfo.percent = EditorGUILayout.IntField("", levelData.BlurInfos[i].percent, GUILayout.Width(120));
            blurInfo.time = EditorGUILayout.FloatField("", levelData.BlurInfos[i].time, GUILayout.Width(120));
            GUILayout.EndHorizontal();

            levelData.BlurInfos[i] = blurInfo;
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Space(20);
        GUILayout.Label("  - ���� �ð�", GUILayout.Width(80));
        GUILayout.Space(20);
        levelData.BlurPauseTime = EditorGUILayout.FloatField("", levelData.BlurPauseTime, GUILayout.Width(50));

        GUILayout.EndHorizontal();
    }

    void ItemGUI()
    {
        GUILayout.Space(25);
        GUILayout.Label("[��ź]");

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - ������ ��", GUILayout.Width(180));
        GUILayout.Space(30);
        levelData.MaxBombCount = EditorGUILayout.IntField("", levelData.MaxBombCount, GUILayout.Width(50));
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - Ʋ���� ������ ������ ��", GUILayout.Width(180));
        GUILayout.Space(30);
        levelData.DecreaseBombCount = EditorGUILayout.IntField("", levelData.DecreaseBombCount, GUILayout.Width(50));
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - �����ϴ� ĳ���� ��", GUILayout.Width(180));
        GUILayout.Space(30);
        levelData.DeleteBombCharacters = EditorGUILayout.IntField("", levelData.DeleteBombCharacters, GUILayout.Width(50));
        GUILayout.EndHorizontal();


        // Lobby Item
        GUILayout.Space(50);
        GUILayout.BeginHorizontal();
        GUILayout.Label("[�κ� ������]", GUILayout.Width(135));
        GUILayout.Space(50);
        GUILayout.Label("����", GUILayout.Width(50));
        GUILayout.Space(30);
        GUILayout.Label("����", GUILayout.Width(50));
        GUILayout.EndHorizontal();

        for (int i = 0, length = levelData.LobbyItemDatas.Length; i < length; i++)
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"  - {levelData.LobbyItemDatas[i].itemName}", GUILayout.Width(150));
            GUILayout.Space(30);
            EditorGUI.BeginChangeCheck();
            levelData.LobbyItemDatas[i].value = EditorGUILayout.IntField("", levelData.LobbyItemDatas[i].value, GUILayout.Width(50));
            GUILayout.Space(30);
            levelData.LobbyItemDatas[i].price = EditorGUILayout.IntField("", levelData.LobbyItemDatas[i].price, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
                levelData.LobbyItemDatas[i].SaveSO();
            GUILayout.EndHorizontal();
        }


        // InGame Item
        GUILayout.Space(50);
        GUILayout.BeginHorizontal();
        GUILayout.Label("  - ������ ȹ���� ���� �޺�", GUILayout.Width(180));
        GUILayout.Space(30);
        levelData.ItemComboUnit = EditorGUILayout.IntField("", levelData.ItemComboUnit, GUILayout.Width(50));
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("[�ΰ��� ������]", GUILayout.Width(135));
        GUILayout.Space(50);
        GUILayout.Label("ȿ��", GUILayout.Width(50));
        GUILayout.Space(20);
        GUILayout.Label("ȿ�� �ð�", GUILayout.Width(70));
        GUILayout.Space(10);
        GUILayout.Label("�Ϲ� Ȯ��", GUILayout.Width(70));
        GUILayout.Space(10);
        GUILayout.Label("������ Ȯ��", GUILayout.Width(70));
        GUILayout.Space(10);
        GUILayout.Label("���� Ȯ��", GUILayout.Width(70));
        GUILayout.EndHorizontal();

        for (int i = 0, length = levelData.InGameItemDatas.Length; i < length; i++)
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"  - {levelData.InGameItemDatas[i].itemName}", GUILayout.Width(150));
            GUILayout.Space(30);
            EditorGUI.BeginChangeCheck();
            levelData.InGameItemDatas[i].value = EditorGUILayout.IntField("", levelData.InGameItemDatas[i].value, GUILayout.Width(50));
            GUILayout.Space(30);
            levelData.InGameItemDatas[i].valueTime = EditorGUILayout.FloatField("", levelData.InGameItemDatas[i].valueTime, GUILayout.Width(50));
            GUILayout.Space(30);
            levelData.InGameItemDatas[i].normalPercent = EditorGUILayout.FloatField("", levelData.InGameItemDatas[i].normalPercent, GUILayout.Width(50));
            GUILayout.Space(30);
            levelData.InGameItemDatas[i].rarePercent = EditorGUILayout.FloatField("", levelData.InGameItemDatas[i].rarePercent, GUILayout.Width(50));
            GUILayout.Space(30);
            levelData.InGameItemDatas[i].startPercent = EditorGUILayout.FloatField("", levelData.InGameItemDatas[i].startPercent, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
                levelData.InGameItemDatas[i].SaveSO();
            GUILayout.EndHorizontal();
        }
    }

    private void OpenLevelData()
    {
        var asset = Resources.Load<LevelData>("LevelData");

        Selection.activeObject = asset;
    }
}

#endif