using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Subtitle/Database")]
public class SubtitleDatabase : ScriptableObject
{
    public List<SubtitleLine> subtitles;
}


[System.Serializable]
public class SubtitleLine
{
    public string id;
    [TextArea] public string english;
    [TextArea] public string mongolian;
    public float duration = 3f;
}
