using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Profile Icon", menuName = "ScriptableObject/Profile Icon")]
public class ProfileData : ScriptableObject
{
    public Sprite sprite;
    public int missionScore = 0;
    public int missionCombo = 0;

    public int nameNum;
    public int descNum;
}