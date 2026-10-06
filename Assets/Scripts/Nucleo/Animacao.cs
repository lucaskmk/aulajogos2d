using UnityEngine;

// Animações simples de enfeite, feitas por fórmula (sem Animator):
//  - Piscar:   aumenta e diminui (brilhinhos do céu);
//  - Balancar: inclina para um lado e para o outro (grama e flores no vento);
//  - Flutuar:  vai e volta devagar na horizontal (nuvens).
// A "fase" vem da posição do objeto, então cada um mexe num ritmo diferente
// (e a nuvem assassina flutua IGUALZINHO às outras).
public class Animacao : MonoBehaviour
{
    public enum Tipo { Piscar, Balancar, Flutuar }

    public Tipo tipo;
    public float velocidade = 1f;
    public float intensidade = 1f;

    Vector3 origem;
    float fase;

    public static Animacao Adicionar(GameObject objeto, Tipo tipo, float velocidade, float intensidade)
    {
        var animacao = objeto.AddComponent<Animacao>();
        animacao.tipo = tipo;
        animacao.velocidade = velocidade;
        animacao.intensidade = intensidade;
        return animacao;
    }

    void Start()
    {
        origem = transform.localPosition;
        fase = transform.position.x * 1.7f + transform.position.y * 0.9f;
    }

    void Update()
    {
        float onda = Mathf.Sin(Time.time * velocidade + fase);
        switch (tipo)
        {
            case Tipo.Piscar:
                transform.localScale = Vector3.one * Mathf.Lerp(1f - intensidade, 1f, (onda + 1f) / 2f);
                break;
            case Tipo.Balancar:
                transform.localRotation = Quaternion.Euler(0f, 0f, onda * intensidade);
                break;
            case Tipo.Flutuar:
                transform.localPosition = origem + Vector3.right * onda * intensidade;
                break;
        }
    }
}
