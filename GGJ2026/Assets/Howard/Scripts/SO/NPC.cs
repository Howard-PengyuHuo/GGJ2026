using UnityEngine;

[CreateAssetMenu(fileName = "NPC", menuName = "Scriptable Objects/NPC")]
public class NPC : ScriptableObject
{
    public string npcName;

    public Sprite portraitOffSpeak;
    public Sprite portraitOnSpeak;

    public string npcDescription;
}
