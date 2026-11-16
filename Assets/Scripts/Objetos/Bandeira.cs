using UnityEngine;

// 'G' - a bandeira de chegada. Encostou, passou de fase.
public class Bandeira : MonoBehaviour
{
    public bool podeTocar = true;

    void Awake()
    {
        var visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = FabricaDeSprites.Pegar("bandeira");
        visual.sortingOrder = 1;
        Sombra.Adicionar(visual);
        AnimacaoDeQuadros.Adicionar(visual, FabricaDeSprites.Quadros("bandeira", FabricaDeSprites.QuadrosDaBandeira), 6f); // tremulando

        Luzes.Ponto(transform, Luzes.Verde, 2.5f, 0.45f, Vector3.up * 2.2f); // todas brilham igual (até a falsa)
        var area = gameObject.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(0.5f, 3f);
        area.offset = new Vector2(0f, 1.5f);
    }

    // 'c' - o BAÚ do tesouro, o fim da última fase: funciona igual à bandeira, só muda o desenho.
    public Bandeira VirarBau()
    {
        Destroy(GetComponent<AnimacaoDeQuadros>());
        GetComponent<SpriteRenderer>().sprite = FabricaDeSprites.Pegar("mapa_bau");
        transform.localScale = Vector3.one * 2f;
        var luz = GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>(); // (só existe nas fases escuras)
        if (luz != null) luz.color = Luzes.Dourada;
        return this;
    }

    // 'O' - a BANDEIRA SECRETA (saída secreta): preta de pirata, num lugar difícil. Passa de fase E libera a
    // fase bônus desta fase (Fase.saidaSecreta), que aparece no mapa com um "?" lilás do lado do ponto.
    bool secreta;
    public Bandeira VirarSecreta()
    {
        secreta = true;
        var desenho = GetComponent<SpriteRenderer>();
        desenho.color = new Color(0.3f, 0.26f, 0.38f);
        var luz = GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
        if (luz != null) luz.color = new Color(0.8f, 0.55f, 1f);
        return this;
    }

    void OnTriggerEnter2D(Collider2D outro) => Testar(outro);
    void OnTriggerStay2D(Collider2D outro) => Testar(outro);

    void Testar(Collider2D outro)
    {
        if (!podeTocar) return;
        var jogador = outro.GetComponent<Jogador>();
        if (jogador == null || jogador.Morto) return;

        podeTocar = false;
        if (secreta) GerenciadorDoJogo.Instancia.SaidaSecreta();
        else GerenciadorDoJogo.Instancia.FaseConcluida();
    }
}
