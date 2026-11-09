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

    void OnTriggerEnter2D(Collider2D outro) => Testar(outro);
    void OnTriggerStay2D(Collider2D outro) => Testar(outro);

    void Testar(Collider2D outro)
    {
        if (!podeTocar) return;
        var jogador = outro.GetComponent<Jogador>();
        if (jogador == null || jogador.Morto) return;

        podeTocar = false;
        GerenciadorDoJogo.Instancia.FaseConcluida();
    }
}
