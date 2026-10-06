using UnityEngine;

// '?' - bloco de moeda (dá uma moeda quando você bate a cabeça).
// 'K' - IGUALZINHO ao de moeda, mas solta um inimigo em cima de você.
public class BlocoSurpresa : MonoBehaviour
{
    public bool soltaInimigo;

    Transform visual;
    SpriteRenderer desenho;
    AnimacaoDeQuadros brilho;
    UnityEngine.Rendering.Universal.Light2D luz;
    bool usado;

    void Awake()
    {
        var filho = ConstrutorDeFase.Visual("Visual", transform, transform.position, "bloco_surpresa", 0);
        visual = filho.transform;
        desenho = filho.GetComponent<SpriteRenderer>();
        // um brilho passa pelo bloco de vez em quando (no 'K' também, senão ele se entregava)
        brilho = AnimacaoDeQuadros.Adicionar(desenho, FabricaDeSprites.Quadros("bloco_surpresa_brilho", FabricaDeSprites.QuadrosDoBrilho), 20f, 2f);
        gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;
        luz = Luzes.Ponto(transform, Luzes.Lilas, 1.6f, 0.45f); // o 'K' brilha igualzinho
    }

    void OnCollisionEnter2D(Collision2D colisao)
    {
        if (usado) return;
        var jogador = colisao.collider.GetComponent<Jogador>();
        if (jogador == null) return;

        Vector2 d = jogador.transform.position - transform.position;
        if (d.y < -0.6f && Mathf.Abs(d.x) < 0.85f) // cabeçada por baixo
            Ativar(jogador);
    }

    void Ativar(Jogador jogador)
    {
        usado = true;
        brilho.enabled = false;
        luz.enabled = false;
        desenho.sprite = FabricaDeSprites.Pegar("bloco_usado");
        StartCoroutine(Efeitos.Pulinho(visual));

        if (soltaInimigo)
        {
            GerenciadorDoJogo.Som("risada");
            var inimigo = ConstrutorDeFase.Criar<Inimigo>("InimigoSurpresa", transform.parent, transform.position + Vector3.up);
            inimigo.direcao = jogador.transform.position.x < transform.position.x ? -1 : 1;
            inimigo.velocidade = 3.5f;
        }
        else
        {
            GerenciadorDoJogo.Som("moeda");
            GerenciadorDoJogo.Instancia.GanharMoeda();
            StartCoroutine(Efeitos.MoedaSaltando(transform.parent, transform.position + Vector3.up));
        }
    }
}
