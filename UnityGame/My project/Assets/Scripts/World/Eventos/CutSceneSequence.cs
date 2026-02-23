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

        public Transform speakerTransform; // opcional: seguir NPC
        public Sprite portrait;            // foto por línea
    }

    public Line[] lines;
}