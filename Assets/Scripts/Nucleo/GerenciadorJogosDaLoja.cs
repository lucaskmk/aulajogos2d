using UnityEngine;

// OS JOGOS DE HABILIDADE da Loja (a outra metade do GerenciadorDoJogo.cs): ficam ao lado da roleta, mas aqui não
// tem sorte nem aposta. Cada rodada gasta uma FICHA (não moedas), e as fichas se ganha passando de fase:
// o Louro dá algumas no começo e mais um punhado a cada fase concluída, até um máximo. O prêmio é em moedas e
// depende só de você.
//
//   PONTARIA: o ponteiro vai e volta pela barra. ENTER para: no VERDE (bem no meio) paga o prêmio cheio; no AMARELO
//             (perto), o menor; fora, nada. A cada acerto no verde seguido, o ponteiro fica mais rápido.
//   REFLEXO:  espere o "JÁ!" e aperte ENTER o mais rápido que conseguir. Apertou antes? Queimou a largada.
//
// Quem desenha a tela é a Interface (InterfaceJogosDaLoja.cs).
public partial class GerenciadorDoJogo
{
    [Header("Jogos de habilidade (Loja)")]
    [Tooltip("Quantas fichas uma partida nova tem (para o jogador conhecer os jogos).")]
    public int fichasIniciais = 3;
    [Tooltip("Quantas fichas o Louro dá a cada fase concluída.")]
    public int fichasPorFase = 2;
    public int maximoDeFichas = 9;
    [Tooltip("O prêmio (em moedas) de acertar em cheio e de acertar perto.")]
    public int premioEmCheio = 10;
    public int premioPerto = 4;

    [Header("Pontaria")]
    [Tooltip("Quantas vezes por segundo o ponteiro atravessa a barra.")]
    public float velocidadeDoPonteiro = 1.15f;
    [Tooltip("A cada acerto no verde seguido, o ponteiro fica isto mais rápido (até 6 acertos).")]
    public float aceleraPorAcerto = 0.15f;
    [Tooltip("Metade da largura do VERDE e do AMARELO, como fração da barra.")]
    public float meiaLarguraDoVerde = 0.045f;
    public float meiaLarguraDoAmarelo = 0.14f;

    [Header("Reflexo")]
    [Tooltip("Até este tempo (em segundos) depois do JÁ, o prêmio é o cheio; até o segundo, o menor.")]
    public float reflexoRapido = 0.3f;
    public float reflexoBom = 0.45f;
    [Tooltip("Quanto tempo o JÁ espera antes de desistir de você.")]
    public float reflexoMaximo = 1.5f;

    public enum JogoDeHabilidade { Pontaria, Reflexo }
    public enum FaseDaRodada { Parado, Valendo, Resultado }
    public static readonly int TotalDeJogos = System.Enum.GetValues(typeof(JogoDeHabilidade)).Length;

    public bool JogosAbertos { get; private set; }
    public JogoDeHabilidade JogoEscolhido { get; private set; }
    public FaseDaRodada Rodada { get; private set; }
    public int Fichas { get; private set; }
    public float Ponteiro { get; private set; }        // 0 = ponta esquerda da barra, 1 = ponta direita
    public int AcertosSeguidos { get; private set; }   // acertos no verde em sequência (deixam o ponteiro mais rápido)
    public bool SinalDoReflexo { get; private set; }   // o "JÁ!" apareceu
    public float TempoDeReacao { get; private set; }   // o tempo da última reação, em segundos (-1 = queimou a largada)
    public int UltimoPremioDoJogo { get; private set; }

    // (o clique no botão grande da tela dos jogos)
    public const int BotaoJogar = -40;

    float tempoDaRodada, esperaDoReflexo, sentidoDoPonteiro = 1f;

    void AbrirJogos()
    {
        JogosAbertos = true;
        Rodada = FaseDaRodada.Parado;
        timerMensagemDaLoja = 0f;
        Som("moeda", 0.5f);
    }

    void FecharJogos()
    {
        JogosAbertos = false;
        Rodada = FaseDaRodada.Parado;
        Som("bloco", 0.4f);
    }

    // Chamado quando uma fase é concluída: o Louro dá mais fichas.
    void GanharFichas() => Fichas = Mathf.Min(maximoDeFichas, Fichas + fichasPorFase);

    void AtualizarJogosDaLoja(int clique)
    {
        bool apertou = Controles.Confirmar() || clique == BotaoJogar;
        if (Rodada != FaseDaRodada.Valendo)
        {
            if (Controles.Voltar() || Controles.Pausar() || clique == BotaoVoltar)
            {
                FecharJogos();
                return;
            }
            int troca = Controles.EsquerdaApertou() || clique == BotaoAnterior ? -1 : Controles.DireitaApertou() || clique == BotaoProxima ? 1 : 0;
            if (troca != 0)
            {
                JogoEscolhido = (JogoDeHabilidade)(((int)JogoEscolhido + troca + TotalDeJogos) % TotalDeJogos);
                Rodada = FaseDaRodada.Parado;
                timerMensagemDaLoja = 0f;
                Som("bloco", 0.5f);
            }
            else if (apertou) ComecarRodada();
            return;
        }

        // valendo!
        tempoDaRodada += Time.deltaTime;
        if (JogoEscolhido == JogoDeHabilidade.Pontaria)
        {
            float velocidade = velocidadeDoPonteiro * (1f + Mathf.Min(AcertosSeguidos, 6) * aceleraPorAcerto);
            float p = Ponteiro + sentidoDoPonteiro * velocidade * Time.deltaTime;
            if (p >= 1f) { p = 2f - p; sentidoDoPonteiro = -1f; }
            else if (p <= 0f) { p = -p; sentidoDoPonteiro = 1f; }
            Ponteiro = Mathf.Clamp01(p);
            if (apertou) PararOPonteiro();
            return;
        }

        if (!SinalDoReflexo && tempoDaRodada >= esperaDoReflexo)
        {
            SinalDoReflexo = true;
            tempoDaRodada = 0f;
            Som("conquista", 0.5f);
        }
        if (apertou) Reagir();
        else if (SinalDoReflexo && tempoDaRodada > reflexoMaximo)
        {
            TempoDeReacao = tempoDaRodada;
            TerminarRodada(0, Idioma.T("Dormiu, Grumete? Currupaco!", "Asleep, Deckhand? Squawk!"));
        }
    }

    void ComecarRodada()
    {
        if (Fichas <= 0)
        {
            AvisarNaLoja(Idioma.T("Sem fichas! Passe de fase para o Louro te dar mais.", "No tokens! Clear a level and the parrot gives you more."), false);
            Som("bloco", 0.6f);
            return;
        }
        Fichas--;
        SalvarItensDaLoja(); // gastou, gastou: sair do jogo no meio da rodada não devolve a ficha
        Rodada = FaseDaRodada.Valendo;
        tempoDaRodada = 0f;
        timerMensagemDaLoja = 0f;
        UltimoPremioDoJogo = 0;
        if (JogoEscolhido == JogoDeHabilidade.Pontaria)
        {
            Ponteiro = 0f;
            sentidoDoPonteiro = 1f;
            Som("serra", 0.3f);
        }
        else
        {
            SinalDoReflexo = false;
            TempoDeReacao = 0f;
            esperaDoReflexo = Random.Range(1.2f, 3.2f); // (não dá para decorar o momento: é reflexo, não ritmo)
            Som("bloco", 0.4f);
        }
    }

    void PararOPonteiro()
    {
        float distancia = Mathf.Abs(Ponteiro - 0.5f);
        if (distancia <= meiaLarguraDoVerde)
        {
            AcertosSeguidos++;
            TerminarRodada(premioEmCheio, Idioma.T($"NA MOSCA! +{premioEmCheio} moedas. O ponteiro ficou mais rápido!", $"BULLSEYE! +{premioEmCheio} coins. The pointer got faster!"));
            return;
        }
        AcertosSeguidos = 0;
        if (distancia <= meiaLarguraDoAmarelo) TerminarRodada(premioPerto, Idioma.T($"Quase! +{premioPerto} moedas.", $"Close! +{premioPerto} coins."));
        else TerminarRodada(0, Idioma.T("Passou longe... Currupaco!", "Way off... Squawk!"));
    }

    void Reagir()
    {
        if (!SinalDoReflexo)
        {
            TempoDeReacao = -1f;
            TerminarRodada(0, Idioma.T("Queimou a largada! Espere o JÁ.", "False start! Wait for the GO."));
            return;
        }
        TempoDeReacao = tempoDaRodada;
        string tempo = TempoDeReacao.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        if (TempoDeReacao <= reflexoRapido)
            TerminarRodada(premioEmCheio, Idioma.T($"{tempo} s: RAIO! +{premioEmCheio} moedas.", $"{tempo} s: LIGHTNING! +{premioEmCheio} coins."));
        else if (TempoDeReacao <= reflexoBom)
            TerminarRodada(premioPerto, Idioma.T($"{tempo} s: bom! +{premioPerto} moedas.", $"{tempo} s: good! +{premioPerto} coins."));
        else TerminarRodada(0, Idioma.T($"{tempo} s: devagar demais...", $"{tempo} s: too slow..."));
    }

    void TerminarRodada(int premio, string mensagem)
    {
        Rodada = FaseDaRodada.Resultado;
        UltimoPremioDoJogo = premio;
        moedas += premio;
        AvisarNaLoja(mensagem, premio > 0);
        if (premio > 0)
        {
            Som("moeda", 0.8f);
            if (premio >= premioEmCheio) Som("conquista", 0.6f);
        }
        else Som("pancada", 0.5f);
        SalvarProgresso();
    }
}
