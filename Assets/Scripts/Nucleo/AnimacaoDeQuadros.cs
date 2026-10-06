using UnityEngine;

// Troca o desenho de um SpriteRenderer em sequência, como um "flipbook".
// Usado na bandeira tremulando e no brilho que passa pelos blocos '?'.
//  - quadrosPorSegundo: velocidade da animação;
//  - pausa: segundos parado no desenho "normal" entre uma passada e outra (0 = sem parar).
// A "fase" vem da posição, então objetos iguais não ficam sincronizados.
// (E o bloco 'K' anima IGUALZINHO ao '?', e a bandeira falsa igualzinha à de verdade.)
public class AnimacaoDeQuadros : MonoBehaviour
{
    public Sprite[] quadros;
    public Sprite normal;
    public float quadrosPorSegundo = 8f;
    public float pausa;

    SpriteRenderer desenho;
    float fase;

    public static AnimacaoDeQuadros Adicionar(SpriteRenderer desenho, Sprite[] quadros, float quadrosPorSegundo, float pausa = 0f)
    {
        var animacao = desenho.gameObject.AddComponent<AnimacaoDeQuadros>();
        animacao.desenho = desenho;
        animacao.quadros = quadros;
        animacao.normal = desenho.sprite;
        animacao.quadrosPorSegundo = quadrosPorSegundo;
        animacao.pausa = pausa;
        animacao.fase = Mathf.Repeat(desenho.transform.position.x * 0.37f, 1f) * (quadros.Length / quadrosPorSegundo + pausa);
        return animacao;
    }

    void Update()
    {
        float duracao = quadros.Length / quadrosPorSegundo;
        float t = Mathf.Repeat(Time.time + fase, duracao + pausa);
        desenho.sprite = t < duracao ? quadros[Mathf.Min(quadros.Length - 1, (int)(t * quadrosPorSegundo))] : normal;
    }
}
