using UnityEngine;

// COOP: CARREGAR E ARREMESSAR. Segurar o botão do EMPURRÃO por 0,25 s com um companheiro colado (na frente ou
// em cima) pega ele no COLO: ele vai em cima da cabeça de quem carrega. Soltar o botão ARREMESSA em arco para
// o lado em que você olha (9 blocos/s para a frente, 11 para cima): atravessa buracos e alcança moedas altas.
// Apertar o botão rápido continua sendo o empurrão de sempre (sai quando solta).
// Quem é carregado sai sozinho apertando PULAR (pula de cima da cabeça). Morreu um dos dois, solta.
public partial class Jogador
{
    public float tempoParaPegarNoColo = 0.25f;
    public Vector2 forcaDoArremesso = new Vector2(9f, 11f);

    public Jogador Carregando { get; private set; }   // quem está no meu colo
    public Jogador CarregadoPor { get; private set; } // no colo de quem eu estou
    float segurandoEmpurrao;
    bool apertouEmpurrao;

    // Chamado no AtualizarCoop no lugar do empurrão direto (veja JogadorCoop.cs).
    void AtualizarColo()
    {
        bool segurando = marujo.EmpurraoSegurando();
        if (Carregando != null)
        {
            if (Carregando.Morto || Morto) { Soltar(false); return; }
            if (!segurando) Soltar(true);
            return;
        }
        if (marujo.EmpurraoApertou())
        {
            apertouEmpurrao = true;
            segurandoEmpurrao = 0f;
        }
        if (!apertouEmpurrao) return;
        if (segurando)
        {
            segurandoEmpurrao += Time.deltaTime;
            if (segurandoEmpurrao >= tempoParaPegarNoColo)
            {
                Jogador perto = CompanheiroColado();
                if (perto != null)
                {
                    apertouEmpurrao = false;
                    Pegar(perto);
                }
            }
            return;
        }
        // soltou rápido: é o empurrão de sempre
        apertouEmpurrao = false;
        if (segurandoEmpurrao < tempoParaPegarNoColo && Time.time >= proximoEmpurrao) EmpurrarQuemEstaNaFrente();
    }

    Jogador CompanheiroColado()
    {
        float lado = visual.flipX ? -1f : 1f;
        foreach (Jogador outro in GerenciadorDoJogo.Jogadores)
        {
            if (outro == this || outro.Morto || outro.congelado || outro.CarregadoPor != null || outro.Carregando != null) continue;
            Vector2 d = outro.Corpo.position - Corpo.position;
            bool naFrente = d.x * lado > -0.3f && Mathf.Abs(d.x) < 1.1f && Mathf.Abs(d.y) < 0.7f;
            bool emCima = Mathf.Abs(d.x) < 0.6f && d.y > 0.4f && d.y < 1.2f;
            if (naFrente || emCima) return outro;
        }
        return null;
    }

    void Pegar(Jogador outro)
    {
        Carregando = outro;
        outro.CarregadoPor = this;
        outro.Corpo.linearVelocity = Vector2.zero;
        outro.Corpo.simulated = false;
        GerenciadorDoJogo.Som("pulo", 0.5f);
        escala = new Vector2(1.2f, 0.85f);
    }

    void Soltar(bool arremessar)
    {
        Jogador outro = Carregando;
        Carregando = null;
        if (outro == null) return;
        outro.CarregadoPor = null;
        outro.Corpo.simulated = true;
        if (!arremessar || outro.Morto) return;
        float lado = visual.flipX ? -1f : 1f;
        outro.Lancar(new Vector2(forcaDoArremesso.x * lado, forcaDoArremesso.y), 0.35f);
        GerenciadorDoJogo.Som("mola", 0.5f);
        escala = new Vector2(0.75f, 1.25f);
        Efeitos.Poeira(transform.parent, transform.position + Vector3.up * 0.9f, 4, 2f);
    }

    // Quem está no colo: fica em cima da cabeça de quem carrega (sem física) e sai pulando.
    // Devolve true se está no colo (aí o resto do Update/FixedUpdate não roda).
    bool NoColo()
    {
        if (CarregadoPor == null) return false;
        if (CarregadoPor == null || CarregadoPor.Morto)
        {
            CarregadoPor = null;
            Corpo.simulated = true;
            return false;
        }
        transform.position = CarregadoPor.transform.position + new Vector3(0f, 0.95f, 0f);
        if (marujo.PuloApertou())
        {
            Jogador quem = CarregadoPor;
            quem.Carregando = null;
            CarregadoPor = null;
            Corpo.simulated = true;
            Corpo.linearVelocity = new Vector2(0f, forcaDoPulo * 0.8f);
            GerenciadorDoJogo.Som("pulo");
        }
        return CarregadoPor != null;
    }
}
