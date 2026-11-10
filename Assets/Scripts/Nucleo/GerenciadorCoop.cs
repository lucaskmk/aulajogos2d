using System.Collections.Generic;
using UnityEngine;

// O COOP dentro do GerenciadorDoJogo (a outra metade do GerenciadorDoJogo.cs): os Grumetes da fase, quem
// "puxa" a câmera, quem ficou para trás e os títulos da tripulação no fim da fase.
// Quem são os jogadores e os aparelhos de cada um fica em Tripulacao.cs; o balão, em JogadorCoop.cs.
//
// Com UM jogador, tudo aqui se comporta igual ao jogo de antes: a lista tem um Grumete só, ele é sempre o líder.
public partial class GerenciadorDoJogo
{
    [Header("Coop")]
    [Tooltip("Quem fica fora da tela por este tempo vira balão e vem boiando até a tripulação.")]
    public float tempoForaDaTelaParaVirarBalao = 0.6f;
    [Tooltip("Até esta distância entre o primeiro e o último, a câmera fica no meio do grupo.")]
    public float distanciaJuntos = 4f;
    [Tooltip("A partir desta distância, a câmera vai só com quem está puxando a frente.")]
    public float distanciaEspalhados = 10f;

    // Os itens da Loja de cada jogador (0 = P1). As moedas e as poções de visão são da tripulação inteira.
    // Os do P1 são os de sempre no save; os dos outros ficam no mesmo slot, em chaves próprias (Progresso.cs).
    readonly int[] escudos = new int[Tripulacao.Maximo], elixires = new int[Tripulacao.Maximo];
    readonly bool[] elixirAtivo = new bool[Tripulacao.Maximo], puloDuplo = new bool[Tripulacao.Maximo];
    // Na Loja: para quem é a compra (TAB troca). Sozinho, é sempre o P1.
    public int Comprador { get; private set; }

    public int EscudosDe(int jogador) => escudos[jogador];
    public bool TemEscudoDe(int jogador) => escudos[jogador] > 0;
    public bool ElixirAtivoDe(int jogador) => elixirAtivo[jogador];
    public bool TemPuloDuploDe(int jogador) => puloDuplo[jogador];

    void EscolherComprador(int jogador)
    {
        if (jogador == Comprador) return;
        Comprador = jogador;
        Som("bloco", 0.5f);
    }

    void ZerarItensDaTripulacao()
    {
        Comprador = 0;
        System.Array.Clear(escudos, 0, escudos.Length);
        System.Array.Clear(elixires, 0, elixires.Length);
        System.Array.Clear(elixirAtivo, 0, elixirAtivo.Length);
        System.Array.Clear(puloDuplo, 0, puloDuplo.Length);
        Tripulacao.ZerarMortesNaPartida();
    }

    void ApagarElixires() => System.Array.Clear(elixirAtivo, 0, elixirAtivo.Length);

    // Entrando na fase: cada jogador que tem elixir guardado gasta um (vale a fase inteira). Devolve se alguém gastou.
    bool UsarElixires()
    {
        ApagarElixires();
        bool alguem = false;
        for (int i = 0; i < Tripulacao.Quantos; i++)
        {
            if (elixires[i] <= 0) continue;
            elixires[i]--;
            elixirAtivo[i] = true;
            alguem = true;
        }
        return alguem;
    }

    readonly List<Jogador> jogadores = new List<Jogador>();
    static readonly List<Jogador> SemJogadores = new List<Jogador>();
    Jogador lider;
    bool reiniciando; // apertaram R: a tripulação inteira volta para o save (ninguém vira balão)

    // Todos os Grumetes da fase (vivos, mortos e no balão). No mapa e no título, vazia.
    public static List<Jogador> Jogadores => Instancia != null ? Instancia.jogadores : SemJogadores;

    // O Grumete "principal": quem a câmera segue, para quem os chefes olham, onde a onda mira.
    // Sozinho, é o único. No coop, é quem está puxando a frente (veja EscolherLider); se ele morre, passa para um vivo.
    public Jogador Lider
    {
        get
        {
            if (lider != null && !lider.Morto) return lider;
            foreach (Jogador j in jogadores)
                if (j != null && !j.Morto) return lider = j;
            if (lider == null && jogadores.Count > 0) lider = jogadores[0]; // todos fora de combate: o último que liderou
            return lider;
        }
    }

    // O Grumete vivo mais perto de um lugar (menos "menos", se quiser). null = nenhum.
    public static Jogador JogadorMaisPerto(Vector2 de, Jogador menos = null)
    {
        Jogador maisPerto = null;
        float menor = float.MaxValue;
        foreach (Jogador j in Jogadores)
        {
            if (j == null || j == menos || j.Morto) continue;
            float d = ((Vector2)j.transform.position - de).sqrMagnitude;
            if (d >= menor) continue;
            menor = d;
            maisPerto = j;
        }
        return maisPerto;
    }

    public static bool TemOutroAtivo(Jogador eu) => JogadorMaisPerto(Vector2.zero, eu) != null;

    // Igual ao JogadorVivo, mas olhando o Grumete vivo MAIS PERTO de "perto": é o que as armadilhas usam
    // (no coop, o espinho escondido pula para quem chegou perto dele, não só para o líder).
    public static bool JogadorVivo(Vector2 perto, out Vector2 posicao)
    {
        posicao = Vector2.zero;
        if (Instancia == null || Instancia.Estado != EstadoDoJogo.Jogando) return false;
        Jogador j = JogadorMaisPerto(perto);
        if (j == null) return false;
        posicao = j.transform.position;
        return true;
    }

    // Para as armadilhas olharem TODOS os Grumetes vivos, um por um, sem criar lixo na memória:
    //     for (int i = 0; GerenciadorDoJogo.ProximoVivo(ref i, out Vector2 jogador);) { ... }
    // (fora da fase, ou com a tripulação morta, não devolve ninguém)
    public static bool ProximoVivo(ref int i, out Vector2 posicao) => ProximoVivo(ref i, out posicao, out _);

    public static bool ProximoVivo(ref int i, out Vector2 posicao, out Jogador jogador)
    {
        posicao = Vector2.zero;
        jogador = null;
        if (Instancia == null || Instancia.Estado != EstadoDoJogo.Jogando) return false;
        List<Jogador> todos = Instancia.jogadores;
        while (i < todos.Count)
        {
            Jogador j = todos[i++];
            if (j == null || j.Morto) continue;
            posicao = j.transform.position;
            jogador = j;
            return true;
        }
        return false;
    }

    // Cria um Grumete para cada marujo da tripulação, todos no mesmo lugar (um atravessa o outro).
    void CriarTripulacao(Vector3 lugar, List<float> inversores)
    {
        jogadores.Clear();
        lider = null;
        reiniciando = false;
        for (int i = 0; i < Tripulacao.Quantos; i++)
        {
            Jogador j = ConstrutorDeFase.Criar<Jogador>(i == 0 ? "Jogador" : "Jogador " + (i + 1), raizDaFase, lugar);
            j.Configurar(i);
            j.inversores = inversores;
            jogadores.Add(j);
        }
        foreach (Jogador j in jogadores) SepararDosCompanheiros(j);
    }

    // Um Grumete atravessa o outro (senão um empurrava o outro para o buraco sem querer... isso é trabalho do EMPURRÃO).
    // Chamado de novo quando o colisor dele volta a existir (saiu do balão).
    public void SepararDosCompanheiros(Jogador quem)
    {
        foreach (Jogador outro in jogadores)
            if (outro != null && outro != quem) Physics2D.IgnoreCollision(quem.Colisor, outro.Colisor, true);
    }

    // ------------------------------------------------------------------ a cada quadro (jogando)

    void AtualizarTripulacao()
    {
        if (jogadores.Count < 2) return;
        EscolherLider();

        // Ficou para trás e saiu da tela? Vira balão (sem contar morte) e vem boiando até a tripulação.
        Camera cam = Camera.main;
        if (cam == null) return;
        float meiaLargura = cam.orthographicSize * cam.aspect, centro = cam.transform.position.x;
        Jogador quemPuxa = Lider;
        foreach (Jogador j in jogadores)
        {
            if (j.Morto || j == quemPuxa)
            {
                j.tempoForaDaTela = 0f;
                continue;
            }
            bool fora = Mathf.Abs(j.transform.position.x - centro) > meiaLargura + 0.4f;
            j.tempoForaDaTela = fora ? j.tempoForaDaTela + Time.deltaTime : 0f;
            if (j.tempoForaDaTela < tempoForaDaTelaParaVirarBalao) continue;
            j.EntrarNoBalao();
            string nome = Tripulacao.Nome(j.Indice);
            Avisar(Idioma.T($"{nome} ficou para trás! Estourem o balão.", $"{nome} fell behind! Pop the balloon."), 1.6f);
        }
    }

    // Quem puxa a câmera: um dos dois das pontas (o mais à esquerda ou o mais à direita). Ele continua sendo o
    // líder até o da OUTRA ponta estar se afastando claramente mais rápido (aí a tela passa a ir com esse).
    // Parado na frente esperando os outros, você continua líder; se alguém resolve voltar correndo, a tela vai com ele.
    void EscolherLider()
    {
        Jogador esquerda = null, direita = null;
        foreach (Jogador j in jogadores)
        {
            if (j.Morto) continue;
            float x = j.transform.position.x;
            if (esquerda == null || x < esquerda.transform.position.x) esquerda = j;
            if (direita == null || x > direita.transform.position.x) direita = j;
        }
        if (direita == null) return; // ninguém vivo

        Jogador atual = lider;
        if (atual == null || atual.Morto || (atual != esquerda && atual != direita))
        {
            lider = direita;
            return;
        }
        if (esquerda == direita) return;
        Jogador outro = atual == direita ? esquerda : direita;
        float puxaAtual = atual == direita ? atual.VelocidadeSuave : -atual.VelocidadeSuave;
        float puxaOutro = outro == direita ? outro.VelocidadeSuave : -outro.VelocidadeSuave;
        if (puxaOutro > puxaAtual + 2f) lider = outro;
    }

    // Para onde a câmera olha no coop: juntos, o meio do grupo; espalhados, quem está puxando a frente.
    // Devolve false quando não é com ele (um jogador só, ou no mapa): aí a câmera segue o alvo dela, como antes.
    public static bool FocoDaCamera(out float x)
    {
        x = 0f;
        GerenciadorDoJogo jogo = Instancia;
        if (jogo == null || jogo.jogadores.Count < 2 || jogo.Mapa != null) return false;
        Jogador quemPuxa = jogo.Lider;
        if (quemPuxa == null) return false;
        x = quemPuxa.transform.position.x;

        float min = float.MaxValue, max = float.MinValue;
        int vivos = 0;
        foreach (Jogador j in jogo.jogadores)
        {
            if (j.Morto) continue;
            vivos++;
            min = Mathf.Min(min, j.transform.position.x);
            max = Mathf.Max(max, j.transform.position.x);
        }
        if (vivos < 2) return true;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(jogo.distanciaJuntos, jogo.distanciaEspalhados, max - min));
        x = Mathf.Lerp((min + max) / 2f, x, t);
        return true;
    }

    // Prende um ponto dentro do que a câmera mostra (o balão nunca sai da tela; "margem" em blocos).
    // margemDeCima: a folga no alto da tela (o HUD fica ali; o Grumete do balão precisa de espaço para o balão em cima).
    public Vector3 DentroDaTela(Vector3 ponto, float margem, float margemDeCima = -1f)
    {
        if (margemDeCima < 0f) margemDeCima = margem + 1f;
        Camera cam = Camera.main;
        if (cam == null) return ponto;
        float meiaAltura = cam.orthographicSize, meiaLargura = meiaAltura * cam.aspect;
        Vector3 centro = cam.transform.position;
        ponto.x = Mathf.Clamp(ponto.x, centro.x - meiaLargura + margem, centro.x + meiaLargura - margem);
        ponto.y = Mathf.Clamp(ponto.y, centro.y - meiaAltura + margem, centro.y + meiaAltura - margemDeCima);
        return ponto;
    }

    // Um Grumete foi parar em outro lugar de uma vez (porta das cabines, bandeira que manda de volta):
    // a tripulação inteira vai junto (senão os outros ficavam fora da tela e viravam balão).
    public void LevarTripulacao(Jogador quem, Vector3 destino)
    {
        foreach (Jogador j in jogadores)
            if (j != quem) j.Teletransportar(destino);
    }

    // A luta com o chefe começou e a arena fechou: quem ficou do lado de fora (ou em cima da parede) é puxado para
    // a entrada dela. Sem isso, um entrava, a parede fechava e o resto da tripulação ficava preso lá fora.
    public void PuxarParaAArena(float esquerda, float direita, Vector3 entrada)
    {
        foreach (Jogador j in jogadores)
        {
            if (j == null) continue;
            float x = j.transform.position.x;
            if (x > esquerda + 0.5f && x < direita - 0.5f) continue;
            j.Teletransportar(entrada);
            if (!j.Morto) Efeitos.Poeira(raizDaFase, entrada, 6, 2.5f);
        }
    }

    // R (ou Y no controle): a tripulação INTEIRA volta para o save. Conta como UMA morte e não gasta escudo.
    void ReiniciarDoSave()
    {
        Jogador quem = Lider;
        if (quem == null || quem.Morto) return;
        reiniciando = true;
        quem.Morrer(ignorarEscudo: true);
        reiniciando = false;
        if (Estado != EstadoDoJogo.Morreu) return; // (não deu: ele estava atravessando uma porta, por exemplo)
        foreach (Jogador j in jogadores)
            if (j != quem) j.MorrerJunto();
    }

    // ------------------------------------------------------------------ os títulos da tripulação (fim da fase)

    public struct TituloDaTripulacao
    {
        public string nome;   // "SALVA-VIDAS", "ÂNCORA"...
        public string motivo; // "3 balões estourados"
        public int jogador;   // de quem é (0 = P1)
    }
    // Os títulos da última fase concluída no coop (vazia se jogou sozinho): a Interface mostra no painel.
    public readonly List<TituloDaTripulacao> TitulosDaTripulacao = new List<TituloDaTripulacao>();

    void DarTitulosDaTripulacao()
    {
        TitulosDaTripulacao.Clear();
        if (!Tripulacao.Coop) return;
        Dar(Idioma.T("SALVA-VIDAS", "LIFEGUARD"), m => m.resgates, n => Idioma.T($"estourou {n} balão(ões)", $"popped {n} balloon(s)"));
        Dar(Idioma.T("MÃO-LEVE", "STICKY FINGERS"), m => m.moedas, n => Idioma.T($"pegou {n} moeda(s)", $"grabbed {n} coin(s)"));
        Dar(Idioma.T("ÂNCORA", "DEAD WEIGHT"), m => m.mortes, n => Idioma.T($"morreu {n} vez(es)", $"died {n} time(s)"));
        Dar(Idioma.T("TRAÍRA", "BACKSTABBER"), m => m.empurroes, n => Idioma.T($"deu {n} empurrão(ões)", $"shoved {n} time(s)"));

        // O título vai para quem tem o maior número (empate: quem entrou primeiro). Ninguém pontuou: não tem título.
        void Dar(string nome, System.Func<Tripulacao.Marujo, int> conta, System.Func<int, string> motivo)
        {
            int melhor = -1, maior = 0;
            for (int i = 0; i < Tripulacao.Quantos; i++)
            {
                int valor = conta(Tripulacao.Do(i));
                if (valor <= maior) continue;
                maior = valor;
                melhor = i;
            }
            if (melhor >= 0) TitulosDaTripulacao.Add(new TituloDaTripulacao { nome = nome, motivo = motivo(maior), jogador = melhor });
        }
    }

    // ------------------------------------------------------------------ a tela da Tripulação (no mapa)

    void AbrirTripulacao()
    {
        Tripulacao.AbrirLobby();
        LimparFalas();
        Som("moeda", 0.5f);
    }

    void AtualizarTelaDaTripulacao(int clique)
    {
        int mudou = Tripulacao.AtualizarLobby();
        if (mudou > 0) Som("moeda", 0.7f);
        else if (mudou < 0) Som("bloco", 0.5f);
        if (!Controles.FecharTripulacao() && clique != BotaoVoltar) return;
        Tripulacao.FecharLobby();
        Comprador = 0; // (a tripulação mudou: a Loja volta a comprar para o P1)
        Som(Tripulacao.Coop ? "vitoria" : "bloco", 0.4f);
    }
}
