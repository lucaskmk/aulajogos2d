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
        AnimacaoDeQuadros.Adicionar(visual, FabricaDeSprites.Quadros("bandeira", FabricaDeSprites.QuadrosDaBandeira), 6f); // tremulando

        Luzes.Ponto(transform, Luzes.Verde, 2.5f, 0.45f, Vector3.up * 2.2f); // todas brilham igual (até a falsa)
        var area = gameObject.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(0.5f, 3f);
        area.offset = new Vector2(0f, 1.5f);
    }

    void OnTriggerEnter2D(Collider2D outro)
    {
        var jogador = outro.GetComponent<Jogador>();
        if (jogador == null || jogador.Morto) return;

        // volta para o último checkpoint (nas fases compridas, voltar para o começo de tudo seria maldade demais)
        Vector3? save = GerenciadorDoJogo.Instancia.PontoDeSave;
        Vector3 destino = save ?? GerenciadorDoJogo.Instancia.PosicaoInicial;
        jogador.Teletransportar(destino);
        GerenciadorDoJogo.Instancia.LevarTripulacao(jogador, destino); // coop: volta todo mundo junto
        GerenciadorDoJogo.Som("risada");
        GerenciadorDoJogo.Instancia.Avisar(save.HasValue ? "VOLTA PRO CHECKPOINT! :)" : "VOLTA PRO COMEÇO! :)", 2.5f);
    }
}
