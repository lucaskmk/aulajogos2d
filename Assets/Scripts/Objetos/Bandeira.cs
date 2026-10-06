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
