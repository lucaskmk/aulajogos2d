using UnityEngine;

// 'U' - cano (2 blocos de largura, desce até o chão). De tempos em tempos sai dele
// uma MÃO DE SOMBRA da Bruxa. Encostou, morreu.
// Dá para subir no cano e pular por cima... mas só enquanto a mão está lá dentro. Timing!
public class Cano : MonoBehaviour
{
    enum Estado { Escondida, Subindo, Fora, Descendo }

    public float tempoEscondida = 1.6f;
    public float tempoSubindo = 0.35f;
    public float tempoFora = 1.1f;
    public float tempoDescendo = 0.35f;

    Estado estado = Estado.Escondida;
    float timer;
    Transform mao;
    Perigo perigo;
    float bocaDoCano, yEscondida, yFora;

    // Chamado pelo ConstrutorDeFase. A posição do objeto é o bloco de cima à esquerda do cano.
    public void Montar(int altura)
    {
        Vector3 topo = transform.position + Vector3.right * 0.5f; // centro dos 2 blocos
        ConstrutorDeFase.Visual("Topo", transform, topo, "cano_topo", 2);
        for (int i = 1; i < altura; i++)
            ConstrutorDeFase.Visual("Corpo", transform, topo + Vector3.down * i, "cano_corpo", 2);

        var colisor = gameObject.AddComponent<BoxCollider2D>(); // dá pra ficar em cima
        colisor.size = new Vector2(2f, altura);
        colisor.offset = new Vector2(0.5f, -(altura - 1) / 2f);

        // A mão (16x24 pixels = 1 x 1,5 blocos) fica atrás do cano e do chão quando escondida.
        bocaDoCano = topo.y + 0.5f;
        yEscondida = bocaDoCano - 1.25f;
        yFora = bocaDoCano + 0.55f;
        var objetoMao = ConstrutorDeFase.Visual("Mao", transform, new Vector3(topo.x, yEscondida, 0f), "mao_sombra", -1);
        mao = objetoMao.transform;
        Luzes.Ponto(mao, Luzes.Lilas, 1.6f, 0.6f);
        perigo = Perigo.Adicionar(objetoMao, new Vector2(0.6f, 1.3f), Vector2.zero);
        Perigo.TornarMovel(objetoMao);
        perigo.ativo = false;

        // canos diferentes não ficam sincronizados
        timer = tempoEscondida * Mathf.Repeat(transform.position.x * 0.37f, 1f);
    }

    void Update()
    {
        if (mao == null) return;
        timer -= Time.deltaTime;

        switch (estado)
        {
            case Estado.Escondida:
                if (timer <= 0f)
                {
                    estado = Estado.Subindo;
                    timer = tempoSubindo;
                    if (GerenciadorDoJogo.JogadorVivo(out Vector2 jogador) && Mathf.Abs(jogador.x - mao.position.x) < 10f)
                        GerenciadorDoJogo.Som("armadilha", 0.5f);
                }
                break;

            case Estado.Subindo:
                Posicionar(Mathf.SmoothStep(0f, 1f, 1f - timer / tempoSubindo));
                if (timer <= 0f)
                {
                    estado = Estado.Fora;
                    timer = tempoFora;
                }
                break;

            case Estado.Fora:
                Posicionar(1f);
                mao.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 12f) * 8f); // mão se mexendo
                if (timer <= 0f)
                {
                    estado = Estado.Descendo;
                    timer = tempoDescendo;
                }
                break;

            case Estado.Descendo:
                mao.localRotation = Quaternion.identity;
                Posicionar(Mathf.SmoothStep(0f, 1f, timer / tempoDescendo));
                if (timer <= 0f)
                {
                    estado = Estado.Escondida;
                    timer = tempoEscondida;
                }
                break;
        }
    }

    // 0 = toda dentro do cano, 1 = toda pra fora. Só mata quando os dedos já passaram da boca do cano.
    void Posicionar(float quanto)
    {
        Vector3 p = mao.position;
        p.y = Mathf.Lerp(yEscondida, yFora, Mathf.Clamp01(quanto));
        mao.position = p;
        perigo.ativo = p.y + 0.6f > bocaDoCano + 0.1f;
    }
}
