using UnityEngine;

// 'W' - parece a bandeira de chegada... mas te manda de volta para o COMEÇO da fase.
// Não conta como morte. Só como raiva.
public class BandeiraVolta : MonoBehaviour
{
    void Awake()
    {
        var visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = FabricaDeSprites.Pegar("bandeira");
        visual.sortingOrder = 1;
        Sombra.Adicionar(visual);

        var area = gameObject.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(0.5f, 3f);
        area.offset = new Vector2(0f, 1.5f);
    }

    void OnTriggerEnter2D(Collider2D outro)
    {
        var jogador = outro.GetComponent<Jogador>();
        if (jogador == null || jogador.Morto) return;

        jogador.Teletransportar(GerenciadorDoJogo.Instancia.PosicaoInicial);
        GerenciadorDoJogo.Som("risada");
        GerenciadorDoJogo.Instancia.Avisar("VOLTA PRO COMEÇO! :)", 2.5f);
    }
}
