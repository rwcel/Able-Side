using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// **µÚ³¡
[System.Serializable]
public struct FFeverInfo
{
    public EFever type;
    public float bonusScore;
    public float applyTime;
    // public Sprite sprite;

    public FFeverInfo(EFever type, float bonusScore, float applyTime)
    {
        this.type = type;
        this.bonusScore = bonusScore;
        this.applyTime = applyTime;
    }
}

[CreateAssetMenu(fileName = "Fever", menuName = "ScriptableObject/Fever")]
public class FeverData : ScriptableObject
{
    public EFever type;
    public float bonusScore;
    public float applyTime;
    public Sprite sprite;
}
