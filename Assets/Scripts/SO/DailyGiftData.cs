using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 일일 제공 변수 중에
/// 고정된 데이터를 가지고 있는 것들
/// Max : FreeCount, AdCount, AdDelay
/// ChargeValue
/// </summary>
[CreateAssetMenu(fileName = "DailyGift Item", menuName = "ScriptableObject/DailyGift Item")]
public class DailyGiftData : ScriptableObject
{
    public EDailyGift type;
    public int freeCount;
    public int adCount;
    public int adDelay;
    public int chargeValue;
}