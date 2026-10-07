using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 'a' / 'b' - BLOCOS DO RITMO: blocos mágicos que se revezam no compasso de um relógio da fase.
//   'a' (azul, da Rem, com uma nota ♪) e 'b' (rosa, da Ram, com duas notas ♫) NUNCA estão sólidos juntos:
//   enquanto um time está aceso (sólido), o outro é só um "fantasma" (o contorno tracejado, sem colisão).
//   A cada 1,4 s eles trocam. Nos últimos 0,35 s antes da troca, os que vão sumir PISCAM e tremem,
//   e o relógio faz "tic... tic... TAC" (dá para jogar no ritmo, como num metrônomo).
// 'A' - O PEGADINHA: igualzinho ao 'a' (pisca e acende junto com ele)... mas nunca fica sólido de verdade. :)
//
// Todos os blocos do ritmo da fase olham para o MESMO relógio (um campo static), então ficam sempre
// sincronizados, não importa onde estão. O relógio volta para zero quando a fase é montada (a fase é
// recriada a cada morte): o ritmo começa SEMPRE igual, com os 'a' acesos. Dá para decorar.
//
// Justiça: se o Subaru estiver DENTRO de um bloco na hora em que ele deveria acender, o bloco espera
// ele sair (senão ele ficaria preso dentro da parede, ou seria cuspido para algum lado).
//
// Blocos iguais encostados NA MESMA LINHA viram um objeto só, com um colisor só (quem junta é o
// MontarTodos, chamado pelo ConstrutorDeFase): assim o Subaru não "engancha" nas emendas.
public class BlocoDoRitmo : MonoBehaviour
{
    public const float TempoDaBatida = 1.4f; // de quanto em quanto tempo os blocos trocam
    public const float TempoDoAviso = 0.35f; // quanto tempo antes da troca eles começam a piscar

    // O Subaru é uma caixa de 0.72 x 0.96 (o colisor 0.62 x 0.86 + a borda arredondada de 0.05),
    // com o centro 0.02 abaixo da posição dele. Usado para saber se ele está dentro de um bloco.
    static readonly Vector2 MeioCorpoDoJogador = new Vector2(0.36f, 0.48f);
    const float DesvioDoCorpo = -0.02f;

    // Cores (para luz e partículas). Os desenhos estão no fim do arquivo.
    static readonly Color32 AzulClaro = new Color32(170, 220, 255, 255);
    static readonly Color32 Azul = new Color32(80, 150, 240, 255);
    static readonly Color32 AzulEscuro = new Color32(36, 76, 168, 255);
    static readonly Color32 RosaClaro = new Color32(255, 205, 225, 255);
    static readonly Color32 Rosa = new Color32(240, 118, 165, 255);
    static readonly Color32 RosaEscuro = new Color32(166, 56, 104, 255);

    // ------------------------------------------------------------------ o relógio (de TODOS os blocos)

    static float relogio;              // segundos desde que a fase foi montada (para quando pausa)
    static int quadroDoRelogio = -1;   // em que quadro o relógio já andou (anda uma vez só por quadro)
    static int quadroDaMontagem = -1;  // em que quadro a última fase com blocos do ritmo foi montada
    static bool alguemPerto;           // algum bloco do ritmo está perto do Subaru? (para o tic-tac)
    static bool alguemPertoAntes;

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Zerar()
    {
        relogio = 0f;
        quadroDoRelogio = -1;
        quadroDaMontagem = -1;
        alguemPerto = false;
        alguemPertoAntes = false;
    }

    // Quantas trocas já aconteceram. Par = vez dos 'a'; ímpar = vez dos 'b'.
    public static int Batida => Mathf.FloorToInt(relogio / TempoDaBatida);
    public static bool VezDoA => Batida % 2 == 0;
    public static float TempoParaTrocar => TempoDaBatida - Mathf.Repeat(relogio, TempoDaBatida);

    // Chamado por quem acabou de ser criado (bloco ou esmagador do ritmo): fase nova = relógio do zero.
    // Toda a fase é montada no MESMO quadro, então só o primeiro objeto de cada montagem zera o relógio.
    public static void ComecarSeFaseNova()
    {
        if (Time.frameCount == quadroDaMontagem) return;
        quadroDaMontagem = Time.frameCount;
        relogio = 0f;
        quadroDoRelogio = Time.frameCount; // o relógio não anda no quadro da montagem
    }

    // Chamado no Update de todo mundo que segue o ritmo; só o primeiro de cada quadro faz o relógio andar.
    // (na pausa o Time.deltaTime é 0, então o ritmo congela junto com o jogo)
    public static void AvancarRelogio()
    {
        if (Time.frameCount == quadroDoRelogio) return;
        quadroDoRelogio = Time.frameCount;

        float antes = relogio;
        relogio += Time.deltaTime;

        // O "perto" vem do quadro anterior (cada bloco avisa no próprio Update, depois daqui).
        alguemPertoAntes = alguemPerto;
        alguemPerto = false;
        if (!alguemPertoAntes) return;

        // Metrônomo: "tic" 0,35 s antes da troca, "tic" de novo na metade disso, e "TAC" na troca.
        if (Cruzou(antes, relogio, TempoDoAviso)) GerenciadorDoJogo.Som("bloco", 0.18f);
        else if (Cruzou(antes, relogio, TempoDoAviso / 2f)) GerenciadorDoJogo.Som("bloco", 0.18f);
        else if (Cruzou(antes, relogio, 0f)) GerenciadorDoJogo.Som("bloco", 0.32f);
    }

    // O relógio passou, neste quadro, pelo instante "falta 'antecedencia' segundos para trocar"?
    static bool Cruzou(float antes, float agora, float antecedencia)
    {
        return Mathf.FloorToInt((antes + antecedencia) / TempoDaBatida)
            != Mathf.FloorToInt((agora + antecedencia) / TempoDaBatida);
    }

    // Os blocos do ritmo e os esmagadores do ritmo só fazem barulho e poeira perto do Subaru.
    public static bool PertoDoJogador(Vector3 posicao, float distancia = 13f)
    {
        if (!GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) return false;
        return Mathf.Abs(jogador.x - posicao.x) < distancia && Mathf.Abs(jogador.y - posicao.y) < 9f;
    }

    public static void AvisarQueEstaPerto() => alguemPerto = true;

    // ------------------------------------------------------------------ montagem

    // Chamado pelo ConstrutorDeFase com todas as células 'a', 'b' e 'A' da fase.
    // Junta as letras iguais encostadas na mesma linha (da esquerda para a direita) num bloco só.
    public static void MontarTodos(Transform raiz, Dictionary<Vector2Int, char> celulas, int alturaDaFase)
    {
        foreach (KeyValuePair<Vector2Int, char> celula in celulas)
        {
            Vector2Int inicio = celula.Key;
            char letra = celula.Value;
            if (celulas.TryGetValue(inicio + Vector2Int.left, out char vizinha) && vizinha == letra) continue; // não é o primeiro
            int blocos = 1;
            while (celulas.TryGetValue(new Vector2Int(inicio.x + blocos, inicio.y), out char seguinte) && seguinte == letra)
                blocos++;
            // linha 0 do texto = topo da fase (igual ao ConstrutorDeFase)
            var centro = new Vector3(inicio.x + (blocos - 1) / 2f, alturaDaFase - 1 - inicio.y, 0f);
            ConstrutorDeFase.Criar<BlocoDoRitmo>("BlocoDoRitmo " + letra, raiz, centro).Montar(letra, blocos);
        }
    }

    char time = 'a';      // de que time ele é: 'a' ou 'b'
    bool falso;           // 'A': parece um 'a', mas nunca fica sólido
    int largura = 1;
    bool aceso;           // está sólido (ou, no caso do falso, PARECE sólido)?
    bool riu;             // o falso já riu de você nesta tentativa?

    BoxCollider2D colisor;
    Transform visual;
    readonly List<SpriteRenderer> desenhos = new List<SpriteRenderer>();
    Sprite spriteAceso, spriteFantasma;
    Color corClara, corEscura;
    Light2D luz;          // só existe nas fases escuras (de dia o Luzes.Ponto devolve null)
    float pulinho;        // 1 -> 0: o "pop" quando acende
    bool montado;

    void Awake() => ComecarSeFaseNova();

    public void Montar(char letra, int blocos)
    {
        falso = letra == 'A';
        time = letra == 'b' ? 'b' : 'a';
        largura = Mathf.Max(1, blocos);

        spriteAceso = FabricaDeSprites.Pegar(time == 'a' ? "bloco_ritmo_a" : "bloco_ritmo_b");
        spriteFantasma = FabricaDeSprites.Pegar(time == 'a' ? "bloco_ritmo_a_fantasma" : "bloco_ritmo_b_fantasma");
        corClara = time == 'a' ? AzulClaro : RosaClaro;
        corEscura = time == 'a' ? AzulEscuro : RosaEscuro;

        // O falso não tem colisor nenhum: você atravessa ele SEMPRE.
        if (!falso)
        {
            colisor = gameObject.AddComponent<BoxCollider2D>();
            colisor.size = new Vector2(largura, 1f);
        }

        // Os desenhos ficam num filho, para dar o "pop" e tremer sem mexer no colisor.
        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);
        for (int i = 0; i < largura; i++)
        {
            float x = i - (largura - 1) / 2f;
            var pedaco = ConstrutorDeFase.Visual("Pedaco", visual, transform.position + Vector3.right * x, "bloco_ritmo_a", 1);
            desenhos.Add(pedaco.GetComponent<SpriteRenderer>());
        }

        luz = Luzes.Ponto(transform, time == 'a' ? (Color)Azul : (Color)Rosa, 1.5f + largura * 0.6f, 0.6f);

        // Começa já no estado certo (no começo da fase, os 'a' estão acesos), sem "pop" nem barulho.
        aceso = DeveEstarAceso();
        if (colisor != null) colisor.enabled = aceso;
        montado = true;
        Desenhar();
    }

    bool DeveEstarAceso() => (time == 'a') == VezDoA;

    void Update()
    {
        if (!montado) return;
        AvancarRelogio();

        bool perto = PertoDoJogador(transform.position, 13f + largura / 2f);
        if (perto) AvisarQueEstaPerto();

        bool deve = DeveEstarAceso();
        if (deve && !aceso)
        {
            // Hora de acender... a não ser que o Subaru esteja dentro (aí espera ele sair).
            if (falso || !JogadorDentro()) Acender(perto);
        }
        else if (!deve && aceso)
        {
            Apagar(perto);
        }

        // O pegadinha: o Subaru "pisou" nele bem quando ele parecia sólido. Puck ri.
        if (falso && aceso && !riu && JogadorDentro())
        {
            riu = true;
            GerenciadorDoJogo.Som("risada");
            Conquistas.Desbloquear("fora_do_ritmo");
        }

        if (!GerenciadorDoJogo.Pausado) Desenhar(); // na pausa o desenho congela (nada de tremer)
    }

    void Acender(bool perto)
    {
        aceso = true;
        if (colisor != null) colisor.enabled = true;
        pulinho = 1f;
        if (perto) Faiscas(false);
    }

    void Apagar(bool perto)
    {
        aceso = false;
        if (colisor != null) colisor.enabled = false;
        if (perto) Faiscas(true);
    }

    // A caixa do Subaru encosta (por dentro, não só na borda) na caixa deste bloco?
    bool JogadorDentro()
    {
        if (!GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) return false;
        jogador.y += DesvioDoCorpo;
        const float folga = 0.04f; // em pé EM CIMA do bloco não conta como "dentro"
        float distanciaX = Mathf.Abs(jogador.x - transform.position.x);
        float distanciaY = Mathf.Abs(jogador.y - transform.position.y);
        return distanciaX < largura / 2f + MeioCorpoDoJogador.x - folga
            && distanciaY < 0.5f + MeioCorpoDoJogador.y - folga;
    }

    // Pedacinhos de luz saindo dos blocos: sobem quando acende, caem quando apaga.
    void Faiscas(bool apagando)
    {
        for (int i = 0; i < largura; i++)
        {
            Vector3 centro = transform.position + Vector3.right * (i - (largura - 1) / 2f);
            for (int j = 0; j < 2; j++)
            {
                Vector3 lugar = centro + new Vector3(Random.Range(-0.45f, 0.45f), apagando ? Random.Range(-0.4f, 0.4f) : 0.5f, 0f);
                var velocidade = apagando
                    ? new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(-2.5f, -0.5f))
                    : new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(0.8f, 2f));
                Particula.Criar(transform.parent, lugar, velocidade, Random.Range(0.25f, 0.4f), Random.Range(0.35f, 0.6f),
                    apagando ? corEscura : corClara);
            }
        }
    }

    void Desenhar()
    {
        float falta = TempoParaTrocar;
        bool vaiTrocar = falta < TempoDoAviso;
        bool deve = DeveEstarAceso();

        Sprite sprite;
        float alfa = 1f;
        Vector3 tremor = Vector3.zero;
        if (aceso)
        {
            // Vai sumir: pisca (cada vez mais rápido) e treme.
            bool apagadoNoPisca = vaiTrocar && Mathf.Repeat(falta, falta > TempoDoAviso / 2f ? 0.12f : 0.07f) < (falta > TempoDoAviso / 2f ? 0.06f : 0.035f);
            sprite = apagadoNoPisca ? spriteFantasma : spriteAceso;
            if (vaiTrocar) tremor = (Vector3)(Random.insideUnitCircle * 0.035f);
        }
        else
        {
            sprite = spriteFantasma;
            if (deve)
                alfa = 0.45f + 0.35f * Mathf.Sin(Time.time * 25f);    // esperando o Subaru sair de dentro
            else if (vaiTrocar)
                alfa = 1f;                                               // já já acende: o contorno fica forte
            else
                alfa = 0.55f + 0.1f * Mathf.Sin(Time.time * 4f + transform.position.x); // fantasma "respirando"
        }

        // "Pop" quando acende: cresce um pouco e volta.
        pulinho = Mathf.MoveTowards(pulinho, 0f, 7f * Time.deltaTime);
        visual.localScale = new Vector3(1f + 0.06f * pulinho / largura, 1f + 0.16f * pulinho, 1f);
        visual.localPosition = tremor;

        var cor = new Color(1f, 1f, 1f, alfa);
        foreach (SpriteRenderer desenho in desenhos)
        {
            desenho.sprite = sprite;
            desenho.color = cor;
        }

        // Luz: forte quando aceso, fraquinha quando fantasma. (0.33 = 0.6 x a força das luzinhas)
        if (luz != null) luz.intensity = aceso ? 0.33f + 0.15f * pulinho : 0.07f;
    }

    // ------------------------------------------------------------------ desenhos (pixel art em texto)
    // Letras destes desenhos: k = contorno, W = nota (branca), L = claro, B = cor do bloco, D = escuro.
    // As cores de verdade vêm de cada time (azul da Rem, rosa da Ram).

    // Rem: azul, com uma colcheia (♪).
    static readonly string[] ArteBlocoA =
    {
        ".kkkkkkkkkkkkkk.",
        "kWLLLLLLLLLLLLLk",
        "kLBBBBBBBBBBBBDk",
        "kLBBBBBBWWBBBBDk",
        "kLBBBBBBWLWBBBDk",
        "kLBBBBBBWBLWBBDk",
        "kLBBBBBBWBBLWBDk",
        "kLBBBBBBWBBBWBDk",
        "kLBBBBBBWBBBDBDk",
        "kLBBBBBBWBBBBBDk",
        "kLBBBBWWWBBBBBDk",
        "kLBBBWWWWDBBBBDk",
        "kLBBBWWWWDBBBBDk",
        "kLBBBBDDDBBBBBDk",
        "kLDDDDDDDDDDDDDk",
        ".kkkkkkkkkkkkkk.",
    };

    // Ram: rosa, com duas semicolcheias ligadas (♫). (desenho diferente: dá para jogar sem distinguir as cores)
    static readonly string[] ArteBlocoB =
    {
        ".kkkkkkkkkkkkkk.",
        "kWLLLLLLLLLLLLLk",
        "kLBBBBBBBBBBBBDk",
        "kLBBBBWWWWWWBBDk",
        "kLBBBBWWWWWWBBDk",
        "kLBBBBWDDDDWBBDk",
        "kLBBBBWBBBBWBBDk",
        "kLBBBBWBBBBWBBDk",
        "kLBBBBWBBBBWBBDk",
        "kLBBBBWBBBBWBBDk",
        "kLBBWWWBBWWWBBDk",
        "kLBWWWWDWWWWDBDk",
        "kLBWWWWDWWWWDBDk",
        "kLBBDDDBBDDDBBDk",
        "kLDDDDDDDDDDDDDk",
        ".kkkkkkkkkkkkkk.",
    };

    // Pinta um desenho com as cores de um time. Fantasma: só o contorno TRACEJADO (na cor clara),
    // um fundo bem transparente e a nota apagadinha (para ainda dar para ver onde o bloco vai aparecer).
    static Sprite Pintar(string[] arte, Color32 claro, Color32 meio, Color32 escuro, bool fantasma)
    {
        Color32 contorno = FabricaDeSprites.Cor('k');
        Color32 branco = FabricaDeSprites.Cor('w');
        int altura = arte.Length;
        return FabricaDeSprites.Procedural(16, altura, FabricaDeSprites.Centro, (x, y) =>
        {
            char c = arte[altura - 1 - y][x];
            if (c == '.') return FabricaDeSprites.Transparente;
            if (!fantasma)
            {
                switch (c)
                {
                    case 'k': return contorno;
                    case 'W': return Color32.Lerp(branco, claro, 0.15f);
                    case 'L': return claro;
                    case 'D': return escuro;
                    default: return meio;
                }
            }
            if (c == 'k') // tracejado: 3 pixels sim, 2 não, andando em volta do bloco
                return (x + y) % 5 < 3 ? claro : FabricaDeSprites.Transparente;
            if (c == 'W') return new Color32(claro.r, claro.g, claro.b, 110);
            return new Color32(meio.r, meio.g, meio.b, 40);
        });
    }

    // Ícone do mapa do mundo: um 'a' e um 'b' em escadinha (meio aceso, meio fantasma).
    static Sprite Icone()
    {
        Color32 contorno = FabricaDeSprites.Cor('k');
        return FabricaDeSprites.Procedural(16, 16, FabricaDeSprites.Centro, (x, y) =>
        {
            // bloco azul (aceso) embaixo à esquerda, de 9 x 9; bloco rosa (fantasma) em cima à direita
            bool noAzul = x <= 8 && y <= 8;
            bool noRosa = x >= 7 && y >= 7;
            if (noAzul)
            {
                bool borda = x == 0 || x == 8 || y == 0 || y == 8;
                if (borda) return (x == 0 || x == 8) && (y == 0 || y == 8) ? FabricaDeSprites.Transparente : contorno;
                if (x == 4 && y >= 3 && y <= 6) return Cor255(255, 255, 255);            // haste da nota
                if ((x == 2 || x == 3) && (y == 2 || y == 3)) return Cor255(255, 255, 255); // cabeça da nota
                if (x == 5 && y == 6) return Cor255(255, 255, 255);                      // bandeirinha
                if (x == 1 || y == 7) return AzulClaro;
                if (x == 7 || y == 1) return AzulEscuro;
                return Azul;
            }
            if (noRosa)
            {
                bool borda = x == 7 || x == 15 || y == 7 || y == 15;
                if (borda) return (x + y) % 4 < 2 ? RosaClaro : FabricaDeSprites.Transparente;
                return new Color32(Rosa.r, Rosa.g, Rosa.b, 70);
            }
            return FabricaDeSprites.Transparente;
        });
    }

    static Color32 Cor255(byte r, byte g, byte b) => new Color32(r, g, b, 255);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("bloco_ritmo", Icone);
        FabricaDeSprites.Registrar("bloco_ritmo_a", () => Pintar(ArteBlocoA, AzulClaro, Azul, AzulEscuro, false));
        FabricaDeSprites.Registrar("bloco_ritmo_a_fantasma", () => Pintar(ArteBlocoA, AzulClaro, Azul, AzulEscuro, true));
        FabricaDeSprites.Registrar("bloco_ritmo_b", () => Pintar(ArteBlocoB, RosaClaro, Rosa, RosaEscuro, false));
        FabricaDeSprites.Registrar("bloco_ritmo_b_fantasma", () => Pintar(ArteBlocoB, RosaClaro, Rosa, RosaEscuro, true));
    }
}
