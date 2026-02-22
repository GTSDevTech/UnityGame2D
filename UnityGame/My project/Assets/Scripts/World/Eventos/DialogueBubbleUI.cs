using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueBubbleUI : MonoBehaviour
{
    [Header("Refs UI")]
    public GameObject root;        // el panel/bocadillo (para activar/desactivar)
    public TMP_Text txtSpeaker;
    public TMP_Text txtLine;
    public Button btnNext;

    [Header("Opcional: seguir a un NPC")]
    public bool followTarget = false;
    public Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    Camera cam;
    Transform follow;

    bool waitingNext = false;

    void Awake()
    {
        cam = Camera.main;

        if (btnNext != null)
            btnNext.onClick.AddListener(ClickNext);

        Hide();
    }

    void LateUpdate()
    {
        if (!followTarget || follow == null || root == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(follow.position + worldOffset);
        root.transform.position = screenPos;
    }

    void ClickNext()
    {
        waitingNext = false;
    }

    public void ShowLine(string speaker, string line, Transform followTransform = null)
    {
        if (root != null) root.SetActive(true);

        if (txtSpeaker != null)
            txtSpeaker.text = speaker ?? "";

        if (txtLine != null)
            txtLine.text = line ?? "";

        follow = followTransform;
        followTarget = (follow != null);
        waitingNext = true;
    }

    public IEnumerator WaitForNext()
    {
        while (waitingNext)
            yield return null;
    }

    public void Hide()
    {
        waitingNext = false;
        follow = null;
        followTarget = false;

        if (root != null) root.SetActive(false);
    }
}