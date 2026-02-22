using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Cutscene/Cutscene Sequence")]
public class CutsceneSequence : ScriptableObject
{
    [Serializable]
    public class Line
    {
        public string speakerName;
        [TextArea(2, 4)]
        public string text;
        public Transform speakerTransform; // opcional: para que el bocadillo siga al NPC
    }

    public Line[] lines;
}