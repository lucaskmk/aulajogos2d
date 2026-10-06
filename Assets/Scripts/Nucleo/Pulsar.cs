using UnityEngine;

// A transparência sobe e desce devagar (raios de luz, brilhos).
public class Pulsar : MonoBehaviour
{
    public float fase;
    public float velocidade = 0.8f;
    SpriteRenderer desenho;
    float alfa;

    void Start()
    {
        desenho = GetComponent<SpriteRenderer>();
        alfa = desenho.color.a;
    }

    void Update()
    {
        Color c = desenho.color;
        c.a = alfa * (0.55f + 0.45f * Mathf.Sin(Time.time * velocidade + fase));
        desenho.color = c;
    }
}
