using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���� ���� ���� �߿�
/// ������ �����͸� ������ �ִ� �͵�
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