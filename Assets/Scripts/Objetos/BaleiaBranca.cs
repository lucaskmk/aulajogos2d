using System.Collections;
using UnityEngine;

// 'w' - a Baleia Branca (Re:Zero), o "chefe" da última fase.
// Quando você passa deste ponto ela aparece nadando no FUNDO (atrás da floresta da paralaxe),
// ruge e depois atravessa a tela três vezes. É só desviar:
//   passagem ALTA  = não pule (passa por cima da sua cabeça);
//   passagem BAIXA = pule por cima dela.
// Um "!" vermelho na beirada da tela mostra a altura em que ela vai passar.
public class BaleiaBranca : MonoBehaviour
{
    public float velocidade = 16f;
    public float tamanho = 1.3f;          // escala do desenho quando ela vem para a frente
    public float alturaAlta = 2.6f;       // centro da baleia, medido a partir do chão
    public float alturaBaixa = 0.7f;
    public float tempoNoFundo = 3f;
    public float tempoDeAviso = 1f;

    float chao;
    bool comecou;
    Transform baleia;
    SpriteRenderer desenho;
    Perigo perigo;
    SpriteRenderer aviso;

    void Awake()
    {
        chao = transform.position.y - 0.5f; // o 'w' fica na linha em que o jogador anda

        var objeto = ConstrutorDeFase.Visual("Baleia", transform, transform.position, "baleia", 8, false);
        baleia = objeto.transform;
        desenho = objeto.GetComponent<SpriteRenderer>();
        desenho.enabled = false;
        // área que mata: um pouco menor que o desenho, para ser justo
        perigo = Perigo.Adicionar(objeto, new Vector2(2.55f, 0.85f), new Vector2(-0.1f, -0.06f));
        Perigo.TornarMovel(objeto);
        perigo.ativo = false;

        aviso = ConstrutorDeFase.Visual("Aviso", transform, transform.position, "aviso", 20, false).GetComponent<SpriteRenderer>();
        aviso.enabled = false;
    }

    void Update()
    {
        if (comecou || !GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) return;
        if (jogador.x < transform.position.x) return;
        comecou = true;
        StartCoroutine(Atacar());
    }

    IEnumerator Atacar()
    {
        GerenciadorDoJogo.Som("rugido");
        CameraSeguir.Tremer(0.12f, 1f);
        GerenciadorDoJogo.Instancia.Avisar("A BALEIA BRANCA!", 2.5f);

        // 1) Nadando lá no fundo, pequena e meio transparente (parece longe).
        desenho.enabled = true;
        desenho.sortingOrder = -23; // entre o castelo e a floresta
        desenho.color = new Color(1f, 1f, 1f, 0.55f);
        desenho.flipX = true;       // olhando para a direita
        baleia.localScale = Vector3.one * 0.6f;
        for (float t = 0f; t < tempoNoFundo; t += Time.deltaTime)
        {
            if (!Ativa()) yield break;
            Camera cam = Camera.main;
            float meiaTela = cam.orthographicSize * cam.aspect;
            float x = Mathf.Lerp(cam.transform.position.x - meiaTela - 3f, cam.transform.position.x + meiaTela + 3f, t / tempoNoFundo);
            baleia.position = new Vector3(x, cam.transform.position.y + 3.2f + Mathf.Sin(t * 2f) * 0.3f, 0f);
            yield return null;
        }
        desenho.enabled = false;

        // 2) Agora ela vem para a frente: alta, baixa, alta.
        float[] alturas = { alturaAlta, alturaBaixa, alturaAlta };
        foreach (float altura in alturas)
        {
            float y = chao + altura;
            bool alta = altura > 1.5f;
            GerenciadorDoJogo.Instancia.Avisar(alta ? "NÃO PULA!" : "PULA!", tempoDeAviso + 0.5f);

            // aviso piscando na beirada direita da tela
            for (float t = 0f; t < tempoDeAviso; t += Time.deltaTime)
            {
                if (!Ativa()) yield break;
                Camera cam = Camera.main;
                aviso.enabled = (t * 8f) % 2f < 1.2f;
                aviso.transform.position = new Vector3(cam.transform.position.x + cam.orthographicSize * cam.aspect - 0.8f, y, 0f);
                yield return null;
            }
            aviso.enabled = false;

            yield return Investida(y);
            yield return new WaitForSeconds(0.9f);
        }

        if (Ativa())
        {
            GerenciadorDoJogo.Instancia.Avisar("A Baleia Branca foi embora... por enquanto.", 2.5f);
            Conquistas.Desbloquear("baleia");
        }
    }

    // Atravessa a tela da direita para a esquerda na altura y.
    IEnumerator Investida(float y)
    {
        Camera cam = Camera.main;
        float meiaTela = cam.orthographicSize * cam.aspect;
        float x = cam.transform.position.x + meiaTela + 4f;
        float fim = cam.transform.position.x - meiaTela - 5f;

        desenho.enabled = true;
        desenho.sortingOrder = 8;
        desenho.color = Color.white;
        desenho.flipX = false; // olhando para a esquerda, para onde vai
        baleia.localScale = Vector3.one * tamanho;
        perigo.ativo = true;
        GerenciadorDoJogo.Som("rugido", 0.5f);
        CameraSeguir.Tremer(0.08f, 0.5f);

        while (x > fim)
        {
            if (!Ativa()) yield break;
            x -= velocidade * Time.deltaTime;
            baleia.position = new Vector3(x, y, 0f);
            yield return null;
        }
        perigo.ativo = false;
        desenho.enabled = false;
    }

    // Se o jogador morreu ou passou de fase, a baleia some. (Na pausa ela só espera.)
    bool Ativa()
    {
        if (GerenciadorDoJogo.Pausado || GerenciadorDoJogo.JogadorVivo(out _)) return true;
        perigo.ativo = false;
        desenho.enabled = false;
        aviso.enabled = false;
        return false;
    }
}
