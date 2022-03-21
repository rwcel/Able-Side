using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Character", menuName = "ScriptableObject/Character")]
public class CharacterData : ScriptableObject
{
    //public 
    public ECharacter type;
    public GameObject prefab;
    public Sprite headSprite;
}
