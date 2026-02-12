using UnityEngine;

public class StarTwinkle : MonoBehaviour
{
   public float speed = 1.2f;
    public float minA = 0.35f;
    public float maxA = 1.0f;

    [HideInInspector] public float globalAlpha = 1f;

    private SpriteRenderer sr;
    private float seed;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        seed = Random.value * 10f;
    }

    private void Update()
    {
        if (sr == null) return;

        if (globalAlpha <= 0.001f)
        {
            var c0 = sr.color;
            c0.a = 0f;
            sr.color = c0;
            return;
        }

        float t = (Mathf.Sin((Time.time + seed) * speed) + 1f) * 0.5f;
        float twinkleA = Mathf.Lerp(minA, maxA, t);

        var c = sr.color;
        c.a = twinkleA * globalAlpha;
        sr.color = c;
    }
}