using System.Collections.Generic;
using UnityEngine;

public enum EstadoDoJogo { Titulo, Conquistas, Mapa, Jogando, Pausado, Morreu, FaseConcluida, Vitoria, Creditos, Historia }

// O "cérebro" do jogo: carrega as fases e o mapa, conta mortes e moedas e cuida dos estados
// (título, conquistas, mapa do mundo, jogando, pausa, morte, fase concluída, vitória, créditos).
// Caminho normal: Título -> Mapa -> Fase -> (passou) -> Mapa -> próxima fase ... -> Vitória -> Créditos.
// O título e a pausa têm "submenus" (Opções e Como Jogar) que abrem por cima deles, sem mudar o estado:
// assim a pausa continua sendo pausa (o jogo parado) enquanto você mexe no volume.
// Regra dos menus: ESC sempre volta UM nível (submenu -> menu; pausa -> jogo; mapa -> título).
// Quem desenha a tela é a Interface; os sons da morte ficam em SonsDaMorte e a música em Musica.
// Falas e créditos ficam em Textos; conquistas em Conquistas; o que é salvo, em Progresso.
//
// Ele se cria SOZINHO quando você aperta Play em qualquer cena
// (veja IniciarAutomaticamente), então não precisa arrastar nada para a cena.
public partial class GerenciadorDoJogo : MonoBehaviour
{
    public static GerenciadorDoJogo Instancia { get; private set; }
    // O Grumete "principal" (no coop, quem está puxando a frente: veja GerenciadorCoop.cs). Todos: Jogadores.
    public static Jogador JogadorAtual => Instancia != null ? Instancia.Lider : null;
    public static bool Pausado => Instancia != null && Instancia.Estado == EstadoDoJogo.Pausado;

    [Header("Configuração")]
    [Tooltip("Cor de fundo das fases que não têm corDoFundo em Fases.cs. Se faltar cor, repete do começo.")]
    public Color[] coresDoFundo =
    {
        new Color32(150, 215, 235, 255), // azul claro
        new Color32(245, 190, 120, 255), // laranja
        new Color32(130, 210, 200, 255), // verde-água
        new Color32(240, 150, 205, 255), // rosa
        new Color32(245, 222, 110, 255), // amarelo
        new Color32(195, 165, 230, 255), // lilás
        new Color32(245, 185, 195, 255), // rosa claro
        new Color32(185, 150, 120, 255), // madeira (as cabines do Navio Fantasma)
        new Color32(60, 80, 170, 255),   // azul-marinho (o "verdadeiro final")
    };
    public Color corDaFaseSecreta = new Color32(250, 230, 150, 255); // dourado
    [Tooltip("Ponto do mapa em que o 'Novo jogo' começa (0 = primeira fase). Útil para testar uma fase específica.")]
    public int faseInicial = 0;

    [Header("Morte")]
    public float tempoAposMorte = 1.3f;
    [Tooltip("Duração da morte a partir da 3ª morte na mesma fase (cansa ver a animação inteira toda vez).")]
    public float tempoAposMorteCurta = 0.7f;
    public int mortesParaEncurtar = 3;
    [Tooltip("Depois desse tempo dá para pular a animação da morte com Espaço/Enter.")]
    public float tempoParaPularMorte = 0.5f;
    [Tooltip("Quantos X vermelhos ficam no lugar das últimas mortes.")]
    public int marcasDeMorte = 5;
    [Tooltip("Com quantas mortes o ponto de save \"muda de lugar\" (volta para o anterior). Sempre o mesmo número: dá para contar.")]
    public int mortesParaOSaveMudar = 3;

    [Header("Fases")]
    public float tempoAposConcluir = 2.2f;
    public float tempoComASereia = 3.5f; // fim da última fase: um tempinho a mais com a Sereia (antes do painel)
    [Tooltip("No fim da fase aparece o painel com os melhores tempos. Ele fica na tela até o jogador apertar ENTER/ESPAÇO, "
             + "mas só aceita a tecla depois desse tempo (para não pular sem querer, ainda com o dedo no pulo).")]
    public float tempoMinimoNoPainel = 1f;

    [Header("Loja")]
    [Tooltip("Quanto custa o escudo na Loja do mapa (em moedas da campanha).")]
    public int precoDoEscudo = 15;
    [Tooltip("Quantos escudos o jogador pode carregar de uma vez.")]
    public int maximoDeEscudos = 3;
    [Tooltip("Quanto custa o elixir de super salto (vale para a próxima fase em que você entrar).")]
    public int precoDoElixir = 20;
    [Tooltip("Quantos elixires o jogador pode carregar de uma vez.")]
    public int maximoDeElixires = 3;
    [Tooltip("Quanto custa a poção que mostra os espinhos escondidos.")]
    public int precoDaPocaoDeEspinhos = 15;
    [Tooltip("Quanto custa a poção que mostra o chão que cai e o chão falso.")]
    public int precoDaPocaoDoChao = 15;
    [Tooltip("Quantas poções de cada tipo o jogador pode carregar de uma vez.")]
    public int maximoDePocoes = 3;
    [Tooltip("Quanto custa o duplo salto (compra uma vez e vale para a partida inteira).")]
    public int precoDoPuloDuplo = 45;

    [Header("Modo difícil")]
    [Tooltip("Quantas moedas o tesouro roubado dá para gastar na Loja (100 pagam os 3 escudos e o duplo salto).")]
    public int moedasDoTesouro = 100;

    // ------------------------------------------------------------------ estado (lido pela Interface)

    public EstadoDoJogo Estado { get; private set; }
    public int FaseAtual { get; private set; }
    public int Mortes { get; private set; }
    public int MortesNaFase { get; private set; }
    // (girando a roleta, o prêmio já está nas moedas, mas só APARECE quando ela para: veja GirarRoleta)
    public int Moedas => moedas - (Girando ? premioPendente : 0) + moedasNaFase;
    // Escudos comprados na Loja e ainda não usados: cada um te salva de UMA morte (veja Jogador.Morrer).
    // (no coop cada jogador tem os SEUS escudos, elixires e duplo salto; as moedas e as poções são da tripulação.
    //  Estes aqui são os do "Comprador": quem está comprando na Loja; fora dela, o P1. Veja GerenciadorCoop.cs.)
    public int Escudos { get => escudos[Comprador]; private set => escudos[Comprador] = value; }
    public bool TemEscudo => Escudos > 0;
    // Elixires de super salto guardados. Entrar numa fase pelo mapa gasta um, e ele vale a fase inteira (ElixirAtivo).
    public int Elixires { get => elixires[Comprador]; private set => elixires[Comprador] = value; }
    public bool ElixirAtivo { get => elixirAtivo[Comprador]; private set => elixirAtivo[Comprador] = value; }
    // Poções guardadas. Entrar numa fase que tenha o que mostrar gasta uma de cada, e ela vale a fase inteira.
    public int PocoesDeEspinhos { get; private set; }
    public int PocoesDoChao { get; private set; }
    public bool VendoEspinhos { get; private set; }
    public bool VendoChaoFalso { get; private set; }
    // O duplo salto (comprado uma vez na Loja, vale para a partida inteira).
    public bool TemPuloDuplo { get => puloDuplo[Comprador]; private set => puloDuplo[Comprador] = value; }
    public float TempoTotal { get; private set; }
    // Tempo nesta fase, desde que entrou pelo mapa (o tempo das mortes conta). É o tempo do recorde da fase.
    public float TempoNaFase { get; private set; }

    // Como foi a última fase concluída: os números, os recordes batidos e as medalhas (a Interface mostra).
    public struct ResultadoDaFase
    {
        public float tempo;
        public int mortes, moedas, totalDeMoedas;
        public Progresso.Novidade novidades;    // quais recordes da fase foram batidos agora
        public Medalhas.Conjunto medalhas;      // todas as medalhas que a fase tem
        public Medalhas.Conjunto medalhasNovas; // as que vieram agora
        // O painel de melhores tempos (Progresso.MelhoresTempos), já com este tempo, se ele entrou.
        public List<Progresso.TempoDoRanking> ranking;
        public int posicaoNoRanking; // 0 = o mais rápido, -1 = ficou fora dos 5
        public int vezesConcluida;   // quantas vezes já passou dessa fase (contando esta)
        public int numero;           // muda a cada fase concluída (a Interface remonta os textos quando muda)
    }
    int resultadosMostrados;

    // Fim da fase: há quanto tempo a tela de "fase concluída" está aberta, e se o jogador já mandou continuar.
    public float TempoNaConclusao { get; private set; }
    bool mandouContinuar;
    // Na última fase a Sereia comemora primeiro; o painel de melhores tempos aparece depois.
    float InicioDoPainel => EhUltimaFase && Sereia != null ? tempoComASereia : 0f;
    public bool MostrandoPainel => Estado == EstadoDoJogo.FaseConcluida && TempoNaConclusao >= InicioDoPainel;
    public bool PodeContinuar => MostrandoPainel && !mandouContinuar && TempoNaConclusao >= InicioDoPainel + tempoMinimoNoPainel;
    public ResultadoDaFase Resultado { get; private set; }
    public string Mensagem { get; private set; } = "";
    public string Aviso => timerAviso > 0f ? aviso : "";
    public Vector3 PosicaoInicial { get; private set; }
    public List<Transform> Bandeiras { get; private set; } = new List<Transform>();
    public bool EhUltimaFase => FaseAtual == Fases.Todas.Length - 1;
    public bool NaSecreta => FaseAtual == Fases.IndiceSecreto;
    // O modo difícil (o Grumete roubou o tesouro): tempestade em tudo e um tsunami em cada fase. É do slot (Progresso.ModoDificil).
    public bool ModoDificil { get; private set; }
    public static bool Tempestade => Instancia != null && Instancia.ModoDificil;
    // Para os efeitos de tela: a última fase é de noite, e as cabines têm luz quente.
    public bool FaseNoturna => Mapa == null && (DadosDaFase.noite || DadosDaFase.escura);
    public bool NasCabines => Mapa == null && nasCabines;
    public Fase DadosDaFase => Fases.Dados(FaseAtual);

    // 0 = acabou de morrer, 1 = vai renascer
    public float ProgressoDaMorte => Estado == EstadoDoJogo.Morreu ? Mathf.Clamp01(1f - timerEstado / duracaoDaMorte) : 0f;
    public bool PodePularMorte => Estado == EstadoDoJogo.Morreu && duracaoDaMorte - timerEstado >= tempoParaPularMorte;
    // A transição está levando para dentro de uma fase? (a tela vermelha dos tentáculos mostra o nome dela)
    public bool TituloNaTransicao { get; private set; }
    public int FaseDaTransicao { get; private set; }

    // 1 = acabou de renascer (clarão), cai até 0
    public float Renascendo => Mathf.Clamp01(timerRenascer / DuracaoRenascer);
    // Troca de fase: 0 = tela normal, 1 = tela toda coberta pelas sombras
    public float Transicao
    {
        get
        {
            float saindo = Estado == EstadoDoJogo.FaseConcluida ? 1f - timerEstado / DuracaoTransicao : 0f;
            if (trocaPendente != null) saindo = 1f - timerSaida / DuracaoTransicao;
            float entrando = timerEntrada / DuracaoTransicao;
            return Mathf.Clamp01(Mathf.Max(saindo, entrando));
        }
    }

    public readonly List<Placa> placas = new List<Placa>();
    public readonly List<Porta> portas = new List<Porta>();
    public Sereia Sereia { get; set; }
    // O chefe da fase (a Baleia Branca, a Cozinheira...), null se a fase não tem chefe. Veja Chefe.cs.
    public Chefe Chefe { get; set; }
    // Já venceu o chefe nesta fase? (se morrer depois, ele não volta)
    public bool ChefeDerrotado { get; private set; }
    // Em que etapa da luta o jogador chegou (como os checkpoints do chefe final do Helltaker):
    // morreu na etapa 2? Renasce já na etapa 2. Zera ao entrar na fase pelo mapa.
    public int EtapaDoChefe { get; set; }

    // Fala de um personagem no mapa (depois de passar de fase). null = nenhuma.
    public Textos.Fala? Fala { get; private set; }
    // As falas que vêm depois da atual (o chefe e o Deus da Riqueza, depois dos chefes): ENTER ou o tempo passam.
    readonly Queue<Textos.Fala> proximasFalas = new Queue<Textos.Fala>();
    // Créditos: quantos segundos já rolaram. A duração cresce com o número de linhas (Textos.Creditos),
    // assim a velocidade da rolagem fica parecida mesmo se vocês acrescentarem mais nomes.
    public float TempoDosCreditos { get; private set; }
    public static float DuracaoDosCreditos => 8f + Textos.Creditos.Length * 0.8f;
    // Os créditos foram abertos pelo menu do título? (aí ENTER/ESC volta para o título, sem recomeçar nada)
    public bool CreditosDoTitulo { get; private set; }

    // ------------------------------------------------------------------ menus

    // Menu do título. "Continuar" só aparece se tiver jogo salvo; "Sair" some no navegador (WebGL).
    public enum OpcaoDoMenu { Continuar, CarregarJogo, NovoJogo, Opcoes, ComoJogar, Conquistas, Creditos, TrocarIdioma, Sair, Tempestade }
    public readonly List<OpcaoDoMenu> opcoesDoMenu = new List<OpcaoDoMenu>();
    public int OpcaoSelecionada { get; private set; }

    // Menu de pausa (a ordem aqui é a ordem na tela).
    public enum ItemDaPausa { Continuar, ReiniciarFase, Opcoes, ComoJogar, VoltarAoMapa, VoltarAoTitulo }
    public static readonly int TotalDaPausa = System.Enum.GetValues(typeof(ItemDaPausa)).Length;
    public int OpcaoDaPausa { get; private set; }

    // Submenus que abrem por cima do título ou da pausa.
    public enum Submenu { Nenhum, Opcoes, ComoJogar, Slots }
    public Submenu SubmenuAberto { get; private set; }

    // A tela dos SLOTS (do título): "Carregar jogo" (só os slots com partida) ou "Novo jogo" (qualquer slot;
    // num slot ocupado, pergunta antes de apagar). DEL apaga um slot (também pergunta).
    // A ordem de CIMA/BAIXO: Slot 1, Slot 2, Slot 3 e Voltar (= Progresso.TotalDeSlots).
    public bool SlotsParaNovoJogo { get; private set; }

    // A cutscene que está passando (veja Historia.cs): os quadros, o quadro atual e há quanto tempo ele está na tela.
    public Historia.Quadro[] HistoriaAtual { get; private set; }
    public int QuadroDaHistoria { get; private set; }
    public float TempoNoQuadro { get; private set; }
    System.Action depoisDaHistoria;
    // A cutscene termina numa ESCOLHA? (null = não): as opções, a selecionada e o que fazer com a resposta.
    public string[] OpcoesDaHistoria { get; private set; }
    public int OpcaoDaHistoria { get; private set; }
    System.Action<int> depoisDaEscolha;
    // As opções só aparecem no último quadro, com o texto já todo na tela.
    public bool MostrandoOpcoes => OpcoesDaHistoria != null && QuadroDaHistoria >= HistoriaAtual.Length - 1
        && TempoNoQuadro >= Historia.TempoParaTerminar(Idioma.Traduzir(HistoriaAtual[QuadroDaHistoria].texto));
    const float EsperaDaEscolha = 0.5f; // (quem vem apertando ENTER para passar os textos não escolhe sem querer)

    // Qual final o jogador viu (a tela de "VOCÊ ZEROU!" muda com ele).
    public enum Desfecho { Devolveu, Arrependido, Rei }
    public Desfecho FinalDoJogo { get; private set; }

    public int SlotSelecionado { get; private set; }
    public int SlotPerguntando { get; private set; } = -1;  // -1 = sem pergunta; senão, o slot da pergunta
    public bool PerguntaDeApagar { get; private set; }      // a pergunta é "apagar?" (senão é "começar um jogo novo por cima?")
    public int VersaoDosSlots { get; private set; }         // muda quando os resumos mudam (a Interface monta os textos de novo)
    readonly Progresso.ResumoDoSlot[] resumos = new Progresso.ResumoDoSlot[Progresso.TotalDeSlots];
    public Progresso.ResumoDoSlot ResumoDoSlot(int slot) => resumos[slot];

    // Loja: abre por cima do mapa (tecla L ou o botão no canto de baixo à direita). Vende o escudo, o elixir de
    // super salto, o duplo salto e as duas poções de visão, e tem a ROLETA no meio. A ordem aqui é a ordem em que CIMA/BAIXO andam.
    // A tela de conquistas rola (em "linhas": 0 = o topo). O máximo vem da Interface (que sabe quantas cabem na tela).
    public float RolagemDasConquistas { get; private set; }
    float maximoDaRolagem;

    public void RolarConquistas(float linhas) =>
        RolagemDasConquistas = Mathf.Clamp(RolagemDasConquistas + linhas, 0f, maximoDaRolagem);

    public void LimitarRolagemDasConquistas(float maximo)
    {
        maximoDaRolagem = maximo;
        RolagemDasConquistas = Mathf.Clamp(RolagemDasConquistas, 0f, Mathf.Max(0f, maximo));
    }

    public enum ItemDaLoja { Escudo, Elixir, PuloDuplo, PocaoDeEspinhos, PocaoDoChao, Aposta, Cor, Girar, Jogos, Voltar }
    public static readonly int TotalDaLoja = System.Enum.GetValues(typeof(ItemDaLoja)).Length;
    public bool LojaAberta { get; private set; }
    public int OpcaoDaLoja { get; private set; }
    // A resposta da última tentativa de compra ("Escudo comprado!", "Faltam 3 moedas."...). Some depois de uns segundos.
    public string MensagemDaLoja => timerMensagemDaLoja > 0f ? mensagemDaLoja : "";
    public bool MensagemDaLojaBoa { get; private set; }
    public bool PodeComprarEscudo => Escudos < maximoDeEscudos && moedas >= precoDoEscudo;
    string mensagemDaLoja = "";
    float timerMensagemDaLoja;

    // A ROLETA da Loja: 15 casas (1 verde, 7 vermelhas e 7 pretas, alternando). Você escolhe quanto apostar e
    // em que cor: vermelho e preto pagam 2x a aposta, verde paga 14x (a casa sempre leva uma vantagem).
    public enum CorDaRoleta { Vermelho, Preto, Verde }
    public const int CasasDaRoleta = 15;
    public static readonly int[] ValoresDaAposta = { 1, 5, 10, 25, 50, 100 };
    public static CorDaRoleta CorDaCasa(int casa) => casa == 0 ? CorDaRoleta.Verde : casa % 2 == 1 ? CorDaRoleta.Vermelho : CorDaRoleta.Preto;
    public static int Multiplicador(CorDaRoleta cor) => cor == CorDaRoleta.Verde ? 14 : 2;
    public int Aposta => ValoresDaAposta[indiceDaAposta];
    public CorDaRoleta CorEscolhida { get; private set; }
    public bool Girando { get; private set; }
    public float AnguloDaRoleta { get; private set; }   // em graus, no sentido do relógio (a Interface só desenha)
    public int UltimaCasa { get; private set; } = -1;   // onde ela parou da última vez (-1 = ainda não girou)
    public int UltimoPremio { get; private set; }        // o que voltou na última jogada (0 = perdeu)
    public float TempoDesdeOResultado { get; private set; } = 99f;
    public readonly List<int> HistoricoDaRoleta = new List<int>(); // as últimas casas, da mais nova para a mais velha
    const float DuracaoDoGiro = 4.2f;
    int indiceDaAposta = 1, casaSorteada, premioPendente;
    float anguloInicial, anguloFinal, tempoGirando;

    // A Loja só abre depois da 2ª fase (chegou na 3ª = passou da 2ª). Na primeira vez, o Louro aparece voando
    // no mapa, explica como abrir a Loja e faz você comprar o primeiro escudo (veja "tutorial da loja").
    public const int FaseQueLiberaALoja = 2; // índice da 3ª fase
    public bool LojaLiberada => ModoDificil || FaseLiberada >= FaseQueLiberaALoja; // (quem roubou já conhece a Loja, e tem ouro para gastar)
    public enum PassoDoTutorial { Nenhum, Chegada, Moedas, AbrirLoja, Comprar, Comprou, Fechar }
    public PassoDoTutorial Tutorial { get; private set; }
    public bool NoTutorial => Tutorial != PassoDoTutorial.Nenhum;
    public float TempoNoPasso { get; private set; }  // a Interface usa para animar (o Louro chegando voando)
    public string TextoDoTutorial { get; private set; } = "";
    public const float DuracaoDaChegadaDoLouro = 1.1f; // a fala só aparece (e o ENTER só vale) depois disso
    bool DeveMostrarTutorialDaLoja => LojaLiberada && !Progresso.TutorialDaLojaVisto;

    // Tela de opções (a ordem aqui é a ordem na tela). "Voltar" também é uma opção, para quem usa só o teclado.
    public enum ItemDasOpcoes { Musica, Efeitos, Graficos, TelaCheia, Cronometro, Voltar }
    public static readonly int TotalDasOpcoes = System.Enum.GetValues(typeof(ItemDasOpcoes)).Length;
    public int OpcaoDasOpcoes { get; private set; }

    // Como Jogar: em que página está (as páginas ficam em Textos.ComoJogar).
    public int PaginaDoComoJogar { get; private set; }

    // Mouse: a Interface (que sabe ONDE cada opção foi desenhada) avisa quando o mouse passa por cima
    // de uma opção ou clica nela. Além do número da opção, o clique pode ser num destes "botões":
    public const int BotaoVoltar = -2, BotaoAnterior = -3, BotaoProxima = -4, BotaoLoja = -5;
    // (na roleta da Loja: diminuir e aumentar a aposta, e escolher cada cor direto)
    public const int BotaoApostaMenos = -6, BotaoApostaMais = -7, BotaoVermelho = -8, BotaoPreto = -9, BotaoVerde = -10;
    // (na tela dos slots: "sim" e "não" da pergunta, e o "apagar" de cada slot: BotaoApagarSlot - número do slot)
    public const int BotaoSim = -11, BotaoNao = -12, BotaoApagarSlot = -20;
    // (no título: as bandeiras do idioma)
    public const int BotaoBandeiraBrasil = -13, BotaoBandeiraEUA = -14;
    // (no mapa: o botão da Tripulação, o coop)
    public const int BotaoTripulacao = -15;
    // (na Loja, no coop: o rostinho de cada jogador, para escolher quem compra: BotaoComprador - número do jogador)
    public const int BotaoComprador = -30;
    const int SemClique = -1;

    public MapaDoMundo Mapa { get; private set; }
    // Fases já passadas nesta partida (os pontos verdes do mapa).
    public int FaseMaisLonge { get; private set; }
    // Mortes em cada fase nesta partida (as caveiras do mapa). A última posição é a da fase secreta.
    readonly int[] mortesPorFase = new int[Fases.Todas.Length + 1];
    static int Posicao(int fase) => fase == Fases.IndiceSecreto ? Fases.Todas.Length : fase;
    // Até que ponto do mapa dá para andar: o que você já alcançou NESTA partida.
    // (Novo Jogo começa com o mapa todo na névoa de novo; o F9 libera tudo só na partida atual.)
    public int FaseLiberada => Mathf.Clamp(Mathf.Max(FaseMaisLonge, faseInicial), 0, Fases.Todas.Length - 1);

    // ------------------------------------------------------------------ interno

    const float DuracaoRenascer = 0.7f;
    const float DuracaoTransicao = 0.45f;
    static readonly Color CorDaTempestade = new Color32(72, 86, 100, 255); // o céu fechado do modo difícil
    static readonly Color LuzDaTempestade = new Color(0.86f, 0.92f, 1f);

    int moedas, moedasNaFase;
    float timerEstado, duracaoDaMorte = 1f;
    float timerRenascer, timerEntrada;
    string aviso = "";
    float timerAviso;
    bool partidaValida; // só partidas jogadas em ordem desde a fase 1 valem recorde
    int moedasParaOSegredo;  // quantas moedas a fase atual tem (pegou todas na fase do segredo = fase secreta)
    bool nasCabines;       // a fase atual tem as portas da Dóris
    bool secretaNova;        // acabou de liberar a fase secreta (o mapa mostra o caminho aparecendo)
    float timerFala;

    // Ponto de save: onde você renasce nesta fase (null = no começo).
    Vector3? pontoDeSave;
    // Os checkpoints desta fase, na ordem em que você pegou. Cada um GUARDA as moedas que você tinha quando passou
    // por ele (quantas e quais): morreu? Você renasce com elas, e as que já foram pegas não aparecem de novo.
    class SavePego
    {
        public Vector3 lugar;
        public int moedas;                                         // as moedas da fase guardadas neste checkpoint
        public HashSet<Vector2Int> pegas = new HashSet<Vector2Int>(); // quais (pelo lugar): não voltam a aparecer
    }
    readonly List<SavePego> savesPegos = new List<SavePego>();
    readonly HashSet<Vector2Int> moedasPegas = new HashSet<Vector2Int>(); // as moedas pegas nesta tentativa (com as guardadas)
    int mortesNoSave; // mortes desde que você pegou o ponto de save atual (na 3ª ele "muda de lugar")

    // Troca de tela com as sombras: primeiro cobre a tela, depois executa a troca.
    System.Action trocaPendente;
    float timerSaida;

    // Volta da Maré: onde você morreu nas últimas tentativas desta fase.
    readonly List<Vector3> lugaresDasMortes = new List<Vector3>();
    readonly List<int> donosDasMortes = new List<int>(); // qual jogador morreu em cada lugar (na mesma ordem)
    int faseDasMarcas = -1;
    // O pé da bandeira de morto de CADA jogador (a última morte dele nesta fase) e as bandeiras fincadas na fase.
    readonly Vector3?[] bandeirasDeMorto = new Vector3?[Tripulacao.Maximo];
    readonly GameObject[] bandeirasFincadas = new GameObject[Tripulacao.Maximo];

    Transform raizDaFase;
    Jogador jogador => Lider; // (os Grumetes da fase ficam em GerenciadorCoop.cs)
    CameraSeguir cameraSeguir;
    AudioSource fonteDeAudio;
    Dictionary<string, AudioClip> sons;
    SonsDaMorte sonsDaMorte;
    Musica musica;

    // ------------------------------------------------------------------ inicialização

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void IniciarAutomaticamente()
    {
        if (FindAnyObjectByType<GerenciadorDoJogo>() == null)
            new GameObject("GerenciadorDoJogo").AddComponent<GerenciadorDoJogo>();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
        // Diagnóstico: confirma no Console qual versão do visual está rodando (veja AjustesVisuais.Versao).
        Debug.Log($"[Cilada à Vista] {AjustesVisuais.Versao} rodando. {AjustesVisuais.Resumo()}");

        PrepararCamera();
        Luzes.PrepararGlobal();
        fonteDeAudio = gameObject.AddComponent<AudioSource>();
        fonteDeAudio.ignoreListenerPause = true; // os sons dos menus tocam até com o jogo pausado
        sons = FabricaDeSons.CriarTodos();
        sonsDaMorte = gameObject.AddComponent<SonsDaMorte>();
        musica = gameObject.AddComponent<Musica>();
        gameObject.AddComponent<Interface>();
        gameObject.AddComponent<EfeitosDeTela>();
        gameObject.AddComponent<TelaPixelada>(); // pixel nítido: a câmera desenha em baixa resolução (e fica mais leve)
        gameObject.AddComponent<PainelDeGraficos>(); // F10: liga/desliga e ajusta cada efeito visual, e mostra o FPS

        VoltarAoTitulo();
    }

    void OnDestroy()
    {
        if (Instancia != this) return;
        Instancia = null;
        Time.timeScale = 1f; // não deixa o editor "travado" se parar o Play no meio da pausa
        AudioListener.pause = false;
    }

    void OnApplicationQuit()
    {
        if (EmPartida) SalvarProgresso();
    }

    // Tem uma partida acontecendo (mapa ou fase)? No título, nas conquistas, na vitória e nos créditos NÃO:
    // ali os números (mortes, tempo...) já foram zerados ou a partida acabou, e salvar apagaria o "Continuar".
    bool EmPartida => Estado == EstadoDoJogo.Mapa || Estado == EstadoDoJogo.Jogando || Estado == EstadoDoJogo.Pausado
                      || Estado == EstadoDoJogo.Morreu || Estado == EstadoDoJogo.FaseConcluida;

    // Onde o Grumete está: no mapa, o ponto escolhido; numa fase, a própria fase.
    int FaseDoGrumete => Estado == EstadoDoJogo.Mapa && Mapa != null ? Mapa.Selecionado : FaseAtual;

    void PrepararCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var objeto = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = objeto.AddComponent<Camera>();
            objeto.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = coresDoFundo[0];
        cameraSeguir = cam.GetComponent<CameraSeguir>();
        if (cameraSeguir == null) cameraSeguir = cam.gameObject.AddComponent<CameraSeguir>();
    }


    // ------------------------------------------------------------------ fases

    void CarregarFase(int indice)
    {
        Paralaxe.Soltar(); // o fundo (céu, montanhas, luz...) é reaproveitado se for a mesma fase de novo
        if (raizDaFase != null) Destroy(raizDaFase.gameObject);

        if (indice != faseDasMarcas)
        {
            lugaresDasMortes.Clear();
            donosDasMortes.Clear();
            System.Array.Clear(bandeirasDeMorto, 0, bandeirasDeMorto.Length);
            faseDasMarcas = indice;
        }

        FaseAtual = indice;
        // renasceu num checkpoint: volta com as moedas que ele guardou (e elas não aparecem de novo na fase)
        SavePego save = SaveAtual();
        moedasNaFase = save != null ? save.moedas : 0;
        moedasPegas.Clear();
        if (save != null) moedasPegas.UnionWith(save.pegas);
        Sereia = null;
        Chefe = null;
        Mapa = null;

        raizDaFase = new GameObject(indice == Fases.IndiceSecreto ? "Fase secreta" : "Fase " + (indice + 1)).transform;
        // a cor do céu vem da própria fase (Fases.cs); se ela não disser, usa a lista de cores daqui
        Color32? corDaFase = Fases.Dados(indice).corDoFundo;
        Color corDoFundo = corDaFase.HasValue ? (Color)corDaFase.Value
            : indice == Fases.IndiceSecreto ? corDaFaseSecreta
            : coresDoFundo.Length > 0 ? coresDoFundo[indice % coresDoFundo.Length] : Color.cyan;
        if (ModoDificil) corDoFundo = Color.Lerp(corDoFundo, CorDaTempestade, 0.75f);
        if (Camera.main != null) Camera.main.backgroundColor = corDoFundo;
        // a luz da fase vem ANTES de montar: assim as luzinhas só são criadas se a fase for escura
        Fase dados = Fases.Dados(indice);
        bool cabines = dados.portas != null;
        if (dados.escura) Luzes.Ambiente(0.22f, new Color(0.7f, 0.72f, 1f));
        else if (dados.noite) Luzes.Ambiente(0.68f, new Color(0.75f, 0.8f, 1f)); // (de noite o fundo pintado ainda aparece bem)
        else if (cabines) Luzes.Ambiente(0.72f, new Color(1f, 0.92f, 0.8f));
        else if (ModoDificil) Luzes.Ambiente(0.9f, LuzDaTempestade); // (clara o bastante para enxergar as armadilhas com a onda atrás)
        else Luzes.Ambiente(indice == Fases.IndiceSecreto ? 1f : 0.93f, Color.white);
        InfoFase info = ConstrutorDeFase.Construir(dados, raizDaFase, MortesNaFase);
        bool noite = dados.noite || dados.escura;
        Paralaxe.Criar(raizDaFase, info.largura, corDoFundo, noite); // céu, montanhas, castelo, floresta...
        MarcaDaMorte.Criar(raizDaFase, lugaresDasMortes, donosDasMortes, bandeirasDeMorto, bandeirasFincadas);

        PosicaoInicial = info.inicio;
        Bandeiras = info.bandeiras;
        moedasParaOSegredo = info.totalDeMoedas;
        nasCabines = info.temPortas;
        PrepararClima(indice, noite);
        // renasce no ponto de save, se tiver um
        CriarTripulacao(pontoDeSave ?? info.inicio, info.inversores); // (um Grumete por jogador: GerenciadorCoop.cs)
        foreach (Jogador grumete in jogadores)
        {
            if (dados.escura) Luzes.Ponto(grumete.transform, new Color(1f, 0.95f, 0.85f), 6f, 1.6f); // no escuro: a "lanterna" do Grumete
            else if (noite || nasCabines) Luzes.Ponto(grumete.transform, new Color(0.9f, 0.9f, 1f), 4.5f, 0.7f); // o Grumete "ilumina" em volta
        }
        aviso = "";
        cameraSeguir.Configurar(jogador.transform, info.largura, info.altura);

        // Fase de chefe: a música da luta é gerada AGORA (atrás da transição), para a luta não engasgar.
        // Renasceu dentro da arena (a luta vai continuar)? A música do chefe continua tocando, sem recomeçar.
        if (Chefe != null) FabricaDeMusica.DoChefe(Chefe.Musica);
        if (Chefe != null && !ChefeDerrotado && EhPontoDeSave(Chefe.PontoDeRenascer)) TocarMusicaDoChefe();
        else musica.TocarDaFase(indice); // se a música já é essa, continua de onde estava
    }

    // Partículas de cada fase (a luz é ajustada em CarregarFase, antes de montar a fase): a que a fase escolheu (Fase.clima);
    // senão, noite com vaga-lumes, cabines com poeira dourada, fases com "petalas" (Fases.cs) com pétalas, a secreta com brilhos
    // e as outras com pólen.
    void PrepararClima(int indice, bool noite)
    {
        Ambiente.Tipo tipo = Ambiente.Tipo.Polen;
        if (Fases.Dados(indice).clima.HasValue) tipo = Fases.Dados(indice).clima.Value; // a fase escolheu (Fases.cs)
        else if (noite) tipo = Ambiente.Tipo.VagaLumes;
        else if (nasCabines) tipo = Ambiente.Tipo.Poeira;
        else if (indice == Fases.IndiceSecreto) tipo = Ambiente.Tipo.Brilhos;
        else if (Fases.Dados(indice).petalas) tipo = Ambiente.Tipo.Petalas;
        if (ModoDificil && Fases.Dados(indice).bioma != Bioma.Gruta) // (dentro da gruta não chove)
        {
            tipo = Ambiente.Tipo.Chuva;
            Relampago.Criar(raizDaFase);
        }
        Ambiente.Criar(raizDaFase, tipo, tipo == Ambiente.Tipo.VagaLumes || tipo == Ambiente.Tipo.Almas ? 16
            : tipo == Ambiente.Tipo.Poeira ? 40 : tipo == Ambiente.Tipo.Chuva ? 70 : 26);
    }

    // Mostra o mapa do mundo com o Grumete no ponto "no".
    // andarPara >= 0: ele anda sozinho até lá (acabou de passar de fase); caminhoNovo: o caminho acabou de abrir.
    void MostrarMapa(int no, int andarPara = -1, bool caminhoNovo = false, bool mostrarSecreta = false)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SubmenuAberto = Submenu.Nenhum;
        if (raizDaFase != null) Destroy(raizDaFase.gameObject);

        raizDaFase = new GameObject("Mapa").transform;
        jogadores.Clear();
        lider = null;
        Sereia = null;
        Bandeiras = new List<Transform>();
        FaseAtual = no == Fases.IndiceSecreto && Progresso.SecretaLiberada ? no : Mathf.Clamp(no, 0, Fases.Todas.Length - 1);
        if (Camera.main != null) Camera.main.backgroundColor = ModoDificil ? Color.Lerp(MapaDoMundo.CorDoChao, CorDaTempestade, 0.7f) : MapaDoMundo.CorDoChao;
        if (ModoDificil) Luzes.Ambiente(0.85f, LuzDaTempestade);
        else Luzes.Ambiente(1f, Color.white);

        Mapa = MapaDoMundo.Criar(raizDaFase, FaseAtual, FaseLiberada, FaseMaisLonge, mortesPorFase,
            Progresso.SecretaLiberada, andarPara, caminhoNovo, mostrarSecreta);
        cameraSeguir.Configurar(Mapa.Grumete, MapaDoMundo.Largura, MapaDoMundo.Altura);
        if (ModoDificil)
        {
            Ambiente.Criar(raizDaFase, Ambiente.Tipo.Chuva, 110, 35); // (ordem 35: a chuva cai por cima da névoa do mapa)
            Relampago.Criar(raizDaFase);
        }
        musica.TocarDaFase(FabricaDeMusica.MusicaDoMapa);
        LojaAberta = false;
        Tutorial = PassoDoTutorial.Nenhum;
        ApagarElixires(); // saiu da fase: o elixir acabou (o de todo mundo)
        VendoEspinhos = false; // (e as poções também)
        VendoChaoFalso = false;
        Estado = EstadoDoJogo.Mapa;
    }

    void VoltarAoTitulo()
    {
        Mortes = 0;
        MortesNaFase = 0;
        moedas = 0;
        ZerarItensDaTripulacao(); // (os itens e a contagem de mortes de cada jogador)
        Escudos = 0;
        Elixires = 0;
        ElixirAtivo = false;
        PocoesDeEspinhos = 0;
        PocoesDoChao = 0;
        Fichas = fichasIniciais; // (as fichas dos jogos de habilidade da Loja)
        JogosAbertos = false;
        VendoEspinhos = false;
        VendoChaoFalso = false;
        TemPuloDuplo = false;
        LojaAberta = false;
        Tutorial = PassoDoTutorial.Nenhum;
        TempoTotal = 0f;
        FaseMaisLonge = 0;
        partidaValida = true;
        faseDasMarcas = -1; // partida nova: apaga as marcas das mortes
        pontoDeSave = null;
        savesPegos.Clear();
        mortesNoSave = 0;
        LimparFalas();
        System.Array.Clear(mortesPorFase, 0, mortesPorFase.Length);
        ModoDificil = Progresso.TemJogoSalvo && Progresso.ModoDificil; // (o título mostra o mapa do "Continuar")
        MostrarMapa(Progresso.TemJogoSalvo ? Progresso.FaseSalva : faseInicial);
        Estado = EstadoDoJogo.Titulo; // o mapa aparece escurecido atrás do título
        MontarMenu();
    }

    // Cobre a tela com as sombras e, quando ela estiver toda coberta, executa "depois".
    void Trocar(System.Action depois)
    {
        trocaPendente = depois;
        timerSaida = DuracaoTransicao;
        TituloNaTransicao = false;
    }

    // Igual ao Trocar, mas a tela da transição mostra o nome da fase em que o Grumete vai entrar.
    void TrocarParaAFase(int fase, System.Action depois)
    {
        Trocar(depois);
        TituloNaTransicao = true;
        FaseDaTransicao = fase;
    }

    void JogarFase(int fase)
    {
        if (fase != Fases.IndiceSecreto && fase > FaseMaisLonge) partidaValida = false; // pulou fases pelo mapa: não vale recorde
        MortesNaFase = 0;
        TempoNaFase = 0f;
        pontoDeSave = null;
        savesPegos.Clear();
        mortesNoSave = 0;
        MoedasComOKraken = 0;
        KrakenRoubou = false;
        ChefeDerrotado = false;
        EtapaDoChefe = 0;
        Tripulacao.ZerarPlacares();
        LimparFalas();
        CarregarFase(fase);
        Estado = EstadoDoJogo.Jogando;
        // tem elixir guardado? Entrar na fase gasta um, e ele vale a fase inteira (até depois de morrer)
        // (no coop, cada jogador gasta o dele)
        if (UsarElixires())
        {
            Avisar("ELIXIR DE SUPER SALTO! Você pula mais alto nesta fase.", 2.5f);
            Som("mola", 0.6f);
        }
        UsarPocoes(fase);
        SalvarProgresso();
    }

    // Só gasta a poção se a fase tiver o que ela mostra.
    void UsarPocoes(int fase)
    {
        Fase dados = Fases.Dados(fase);
        VendoEspinhos = PocoesDeEspinhos > 0 && Fases.TemLetras(dados, "h");
        VendoChaoFalso = PocoesDoChao > 0 && Fases.TemLetras(dados, "CF");
        if (VendoEspinhos) PocoesDeEspinhos--;
        if (VendoChaoFalso) PocoesDoChao--;
        if (!VendoEspinhos && !VendoChaoFalso) return;
        Avisar(VendoEspinhos && VendoChaoFalso ? "POÇÕES ATIVAS! Espinhos escondidos e chão traiçoeiro aparecem."
            : VendoEspinhos ? "POÇÃO DOS ESPINHOS! Os escondidos aparecem nesta fase."
            : "POÇÃO DO CHÃO! O chão que cai aparece nesta fase.", 2.5f);
        Som("conquista", 0.5f);
    }

    void Update()
    {
        timerRenascer -= Time.deltaTime;
        timerEntrada -= Time.unscaledDeltaTime; // tempo real: a transição funciona até com o jogo pausado
        if (timerEntrada <= 0f && trocaPendente == null) TituloNaTransicao = false; // a fase já apareceu: some o nome
        AjustarMusica();
        // O clique do mouse é pego (e apagado) ANTES da transição: um clique no meio dela é ignorado.
        int clique = PegarClique(out int lado);
        if (Fala.HasValue)
        {
            timerFala -= Time.unscaledDeltaTime;
            if (timerFala <= 0f) PassarFala();
        }

        if (trocaPendente != null)
        {
            timerSaida -= Time.unscaledDeltaTime;
            if (timerSaida > 0f) return;
            System.Action troca = trocaPendente;
            trocaPendente = null;
            troca();
            timerEntrada = DuracaoTransicao; // as sombras saem da tela
            return;
        }

        switch (Estado)
        {
            case EstadoDoJogo.Titulo:
                if (SubmenuAberto != Submenu.Nenhum) AtualizarSubmenu(clique, lado);
                else AtualizarMenu(clique);
                break;

            case EstadoDoJogo.Conquistas:
                if (Controles.Confirmar() || Controles.Pausar() || Controles.Voltar() || clique == BotaoVoltar)
                {
                    Estado = EstadoDoJogo.Titulo;
                    Som("bloco", 0.4f);
                }
                else if (Controles.CimaApertou()) RolarConquistas(-1f);
                else if (Controles.BaixoApertou()) RolarConquistas(1f);
                break;

            case EstadoDoJogo.Mapa:
                AtualizarMapa(clique, lado);
                break;

            case EstadoDoJogo.Jogando:
                TempoTotal += Time.deltaTime;
                TempoNaFase += Time.deltaTime;
                timerAviso -= Time.deltaTime;
                if (Controles.Pausar()) Pausar(true);
                else if (Controles.Reiniciar()) ReiniciarDoSave(); // reiniciar conta como morte. Óbvio. (e não gasta o escudo)
                else AtualizarTripulacao(); // coop: quem puxa a câmera e quem ficou para trás
                break;

            case EstadoDoJogo.Pausado:
                if (SubmenuAberto != Submenu.Nenhum) AtualizarSubmenu(clique, lado);
                else AtualizarPausa(clique);
                break;

            case EstadoDoJogo.Morreu:
                TempoTotal += Time.deltaTime;
                TempoNaFase += Time.deltaTime;
                timerEstado -= Time.deltaTime;
                if (timerEstado > 0f && PodePularMorte && (Controles.Confirmar() || Controles.PuloApertou()))
                {
                    timerEstado = 0f;
                    sonsDaMorte.PularParaOPico();
                    Conquistas.Contar("pular_morte", 20);
                }
                if (timerEstado <= 0f) Renascer();
                break;

            case EstadoDoJogo.FaseConcluida:
                // O painel de melhores tempos fica na tela até o jogador mandar continuar (ENTER/ESPAÇO ou clique).
                // Até lá o timer fica parado logo antes de as sombras da transição começarem a cobrir a tela.
                TempoNaConclusao += Time.deltaTime;
                if (PodeContinuar && (Controles.Confirmar() || clique == BotaoProxima))
                {
                    mandouContinuar = true;
                    timerEstado = Mathf.Min(timerEstado, DuracaoTransicao);
                    Som("bloco", 0.4f);
                }
                timerEstado -= Time.deltaTime;
                if (!mandouContinuar) timerEstado = Mathf.Max(timerEstado, DuracaoTransicao);
                if (timerEstado <= 0f) ProximaFase();
                break;

            case EstadoDoJogo.Historia:
                AtualizarHistoria(clique);
                break;

            case EstadoDoJogo.Vitoria:
                if (Controles.Confirmar())
                    Trocar(() => AbrirCreditos(false));
                break;

            case EstadoDoJogo.Creditos:
                TempoDosCreditos += Time.deltaTime;
                if (Controles.Confirmar() || Controles.Pausar() || Controles.Voltar() || clique == BotaoVoltar
                    || TempoDosCreditos > DuracaoDosCreditos)
                    FecharCreditos();
                break;
        }
    }

    void Renascer()
    {
        CarregarFase(FaseAtual);
        Estado = EstadoDoJogo.Jogando;
        timerRenascer = DuracaoRenascer; // é aqui que o áudio está no pico
        CameraSeguir.Tremer(0.15f, 0.25f);
    }

    void ProximaFase()
    {
        timerEntrada = DuracaoTransicao; // as sombras saem da tela
        int concluida = FaseAtual;
        if (concluida == Fases.IndiceSecreto)
        {
            MostrarMapa(concluida);
            MostrarFalas(Textos.FalasDepoisDaFase(concluida, MortesNaFase));
            SalvarProgresso();
            return;
        }
        if (!EhUltimaFase)
        {
            // volta para o mapa e o Grumete anda sozinho até a próxima fase
            int proxima = concluida + 1;
            bool caminhoNovo = proxima > FaseLiberada;
            FaseMaisLonge = Mathf.Max(FaseMaisLonge, proxima);
            MostrarMapa(concluida, proxima, caminhoNovo, secretaNova);
            if (secretaNova || !DeveMostrarTutorialDaLoja) // (senão o Louro do tutorial chega logo que o Grumete parar)
                MostrarFalas(secretaNova ? new[] { Textos.SecretaLiberada } : Textos.FalasDepoisDaFase(concluida, MortesNaFase));
            secretaNova = false;
            Progresso.Salvar(proxima, FaseMaisLonge, Mortes, moedas, TempoTotal, partidaValida);
            SalvarItensDaLoja();
            Progresso.SalvarMortesPorFase(mortesPorFase);
            return;
        }

        // chegou no baú! Salva tudo AGORA: fechar o jogo no meio da cutscene não perde nada (o "Continuar" volta na escolha)
        FaseMaisLonge = Mathf.Max(FaseMaisLonge, concluida);
        Progresso.Zerou(Fases.Todas.Length);
        Conquistas.Desbloquear("zerou");
        if (partidaValida && !ModoDificil) Progresso.TentarSalvarRecorde(Mortes);
        SalvarProgresso();
        Progresso.SalvarEscolhaPendente(true);
        TocarEscolhaDoTesouro();
    }

    // ------------------------------------------------------------------ o final: devolver ou roubar

    void TocarEscolhaDoTesouro()
    {
        if (ModoDificil) TocarEscolha(Historia.EscolhaNaTempestade, Historia.OpcoesNaTempestade, EscolheuNaTempestade);
        else TocarEscolha(Historia.Escolha, Historia.OpcoesDaEscolha, EscolheuOTesouro);
    }

    void EscolheuOTesouro(int opcao)
    {
        Progresso.SalvarEscolhaPendente(false);
        if (opcao == 0)
        {
            FinalDoJogo = Desfecho.Devolveu;
            Conquistas.Desbloquear("devolveu");
            TocarHistoria(Historia.Final, () => Estado = EstadoDoJogo.Vitoria);
            return;
        }
        Conquistas.Desbloquear("roubou");
        ComecarTempestade(); // (salva antes da cutscene: fechar o jogo no meio dela não desfaz o roubo)
        TocarHistoria(Historia.Roubou, () =>
        {
            MostrarMapa(0);
            MostrarFala(Textos.ComecoDaTempestade);
        });
    }

    // Roubou: o jogo recomeça do começo, com o ouro no bolso e o mar inteiro atrás do Grumete.
    void ComecarTempestade()
    {
        ModoDificil = true;
        Progresso.SalvarModoDificil(true);
        moedas += moedasDoTesouro;
        Mortes = 0;
        TempoTotal = 0f;
        FaseMaisLonge = 0;
        partidaValida = true;
        faseDasMarcas = -1;
        System.Array.Clear(mortesPorFase, 0, mortesPorFase.Length);
        Progresso.Salvar(0, 0, 0, moedas, 0f, true);
        Progresso.SalvarMortesPorFase(mortesPorFase);
        SalvarItensDaLoja();
    }

    void EscolheuNaTempestade(int opcao)
    {
        Progresso.SalvarEscolhaPendente(false);
        if (opcao == 0)
        {
            // devolveu, arrependido: a tempestade acaba (o slot fica com o mapa aberto, em dia de sol)
            ModoDificil = false;
            Progresso.SalvarModoDificil(false);
            FinalDoJogo = Desfecho.Arrependido;
            Conquistas.Desbloquear("arrependido");
            TocarHistoria(Historia.FinalVerdadeiro, () => Estado = EstadoDoJogo.Vitoria);
            return;
        }
        FinalDoJogo = Desfecho.Rei;
        Conquistas.Desbloquear("rei_do_mar");
        TocarHistoria(Historia.FinalDoRei, () => Estado = EstadoDoJogo.Vitoria);
    }

    // ------------------------------------------------------------------ a história (cutscenes)

    // Começa a cutscene. "depois" acontece quando ela acabar (ou for pulada), já com a tela escura da transição.
    void TocarHistoria(Historia.Quadro[] quadros, System.Action depois)
    {
        HistoriaAtual = quadros;
        QuadroDaHistoria = 0;
        TempoNoQuadro = 0f;
        depoisDaHistoria = depois;
        OpcoesDaHistoria = null;
        Estado = EstadoDoJogo.Historia;
    }

    // Uma cutscene que termina numa escolha: "escolha" recebe a opção (já com a tela escura da transição).
    void TocarEscolha(Historia.Quadro[] quadros, string[] opcoes, System.Action<int> escolha)
    {
        TocarHistoria(quadros, null);
        OpcoesDaHistoria = opcoes;
        OpcaoDaHistoria = 0;
        depoisDaEscolha = escolha;
    }

    // ENTER (ou clique): se o texto ainda está aparecendo, mostra ele todo; senão, o próximo quadro. ESC pula tudo.
    void AtualizarHistoria(int clique)
    {
        TempoNoQuadro += Time.unscaledDeltaTime;
        if (Controles.Voltar() || clique == BotaoVoltar)
        {
            if (OpcoesDaHistoria == null)
            {
                AcabarHistoria();
                return;
            }
            // com escolha, o ESC só pula até ela: dá para pular a conversa, não a decisão
            if (QuadroDaHistoria < HistoriaAtual.Length - 1)
            {
                QuadroDaHistoria = HistoriaAtual.Length - 1;
                TempoNoQuadro = 0f;
            }
            return;
        }
        if (MostrandoOpcoes)
        {
            AtualizarEscolha(clique);
            return;
        }
        if (!Controles.Confirmar() && clique != BotaoProxima) return;
        float paraTerminar = Historia.TempoParaTerminar(Idioma.Traduzir(HistoriaAtual[QuadroDaHistoria].texto));
        if (TempoNoQuadro < paraTerminar)
        {
            TempoNoQuadro = paraTerminar; // o texto inteiro de uma vez
            return;
        }
        if (QuadroDaHistoria >= HistoriaAtual.Length - 1)
        {
            AcabarHistoria();
            return;
        }
        QuadroDaHistoria++;
        TempoNoQuadro = 0f;
        Som("bloco", 0.35f);
    }

    void AtualizarEscolha(int clique)
    {
        if (Controles.EsquerdaApertou() || Controles.DireitaApertou() || Controles.CimaApertou() || Controles.BaixoApertou())
            OpcaoDaHistoria = Girar(OpcaoDaHistoria, 1, OpcoesDaHistoria.Length);
        if (TempoNoQuadro < Historia.TempoParaTerminar(Idioma.Traduzir(HistoriaAtual[QuadroDaHistoria].texto)) + EsperaDaEscolha) return;

        bool clicou = clique >= 0 && clique < OpcoesDaHistoria.Length;
        if (clicou) OpcaoDaHistoria = clique;
        if (!clicou && !Controles.Confirmar()) return;

        System.Action<int> escolha = depoisDaEscolha;
        int opcao = OpcaoDaHistoria;
        depoisDaEscolha = null;
        OpcoesDaHistoria = null;
        Som("moeda", 0.4f);
        Trocar(() =>
        {
            HistoriaAtual = null;
            escolha(opcao);
        });
    }

    void AcabarHistoria()
    {
        System.Action depois = depoisDaHistoria;
        depoisDaHistoria = null;
        Som("moeda", 0.4f);
        Trocar(() =>
        {
            HistoriaAtual = null;
            depois?.Invoke();
        });
    }

    void Pausar(bool pausar)
    {
        if (pausar) OpcaoDaPausa = 0; // a pausa sempre abre em "Continuar"
        SubmenuAberto = Submenu.Nenhum;
        Estado = pausar ? EstadoDoJogo.Pausado : EstadoDoJogo.Jogando;
        Time.timeScale = pausar ? 0f : 1f;
        AudioListener.pause = pausar; // a música de fundo ignora isso e só fica mais baixa
    }

    void SalvarProgresso()
    {
        Progresso.Salvar(FaseDoGrumete, FaseMaisLonge, Mortes, moedas, TempoTotal, partidaValida);
        Progresso.SalvarMortesPorFase(mortesPorFase);
        SalvarItensDaLoja();
    }

    void SalvarItensDaLoja()
    {
        Progresso.SalvarFichas(Fichas);
        Progresso.SalvarItensDaLoja(escudos[0], elixires[0], puloDuplo[0], PocoesDeEspinhos, PocoesDoChao);
        for (int i = 1; i < Tripulacao.Maximo; i++) Progresso.SalvarItensDoJogador(i, escudos[i], elixires[i], puloDuplo[i]);
    }

    void MostrarFala(Textos.Fala fala)
    {
        proximasFalas.Clear();
        Fala = fala;
        timerFala = 7f;
    }

    // Várias falas, uma atrás da outra (a primeira já aparece).
    void MostrarFalas(Textos.Fala[] falas)
    {
        MostrarFala(falas[0]);
        for (int i = 1; i < falas.Length; i++) proximasFalas.Enqueue(falas[i]);
    }

    // A fala atual acabou: vem a próxima da fila (ou nenhuma).
    void PassarFala()
    {
        if (proximasFalas.Count > 0)
        {
            Fala = proximasFalas.Dequeue();
            timerFala = 7f;
            Som("bloco", 0.3f);
        }
        else Fala = null;
    }

    void LimparFalas()
    {
        proximasFalas.Clear();
        Fala = null;
    }

    void AjustarMusica()
    {
        float volume = 1f;
        if (Estado == EstadoDoJogo.Morreu || sonsDaMorte.OstTocando) volume = 0.15f; // a OST da Volta da Maré aparece
        else if (Estado == EstadoDoJogo.Pausado && SubmenuAberto != Submenu.Opcoes) volume = 0.35f; // nas opções: volume de verdade, para ajustar
        musica.Abafar(volume);
    }

    // ------------------------------------------------------------------ menu de pausa

    // Cima/baixo escolhe, ENTER (ou clique) confirma, ESC continua o jogo. Q é atalho para voltar ao mapa.
    void AtualizarPausa(int clique)
    {
        if (Controles.Pausar() || Controles.Voltar() || clique == BotaoVoltar)
        {
            Pausar(false);
            return;
        }
        if (Controles.VoltarAoMapa())
        {
            EscolherNaPausa(ItemDaPausa.VoltarAoMapa);
            return;
        }
        if (Controles.CimaApertou()) OpcaoDaPausa = Girar(OpcaoDaPausa, -1, TotalDaPausa);
        if (Controles.BaixoApertou()) OpcaoDaPausa = Girar(OpcaoDaPausa, 1, TotalDaPausa);

        if (clique >= 0 && clique < TotalDaPausa)
        {
            OpcaoDaPausa = clique;
            EscolherNaPausa((ItemDaPausa)clique);
        }
        else if (Controles.Confirmar()) EscolherNaPausa((ItemDaPausa)OpcaoDaPausa);
    }

    void EscolherNaPausa(ItemDaPausa item)
    {
        int fase = FaseAtual;
        switch (item)
        {
            case ItemDaPausa.Continuar:
                Pausar(false);
                break;
            case ItemDaPausa.ReiniciarFase:
                // recomeça a fase do ZERO (sem ponto de save e com o chefe inteiro), como se entrasse pelo mapa
                Som("vitoria", 0.4f);
                TrocarParaAFase(fase, () =>
                {
                    Pausar(false);
                    JogarFase(fase);
                });
                break;
            case ItemDaPausa.Opcoes:
                AbrirSubmenu(Submenu.Opcoes);
                break;
            case ItemDaPausa.ComoJogar:
                AbrirSubmenu(Submenu.ComoJogar);
                break;
            case ItemDaPausa.VoltarAoMapa:
                Som("vitoria", 0.4f);
                Trocar(() =>
                {
                    MostrarMapa(fase); // volta para o mapa, no ponto desta fase (MostrarMapa tira a pausa)
                    SalvarProgresso();
                });
                break;
            case ItemDaPausa.VoltarAoTitulo:
                Som("vitoria", 0.4f);
                Trocar(() =>
                {
                    SalvarProgresso(); // salva ANTES: o VoltarAoTitulo zera as mortes, o tempo...
                    VoltarAoTitulo();
                });
                break;
        }
    }

    // ------------------------------------------------------------------ submenus (Opções e Como Jogar)

    void AbrirSubmenu(Submenu qual)
    {
        SubmenuAberto = qual;
        OpcaoDasOpcoes = 0;
        PaginaDoComoJogar = 0;
        Som("moeda", 0.5f);
    }

    void FecharSubmenu()
    {
        SubmenuAberto = Submenu.Nenhum; // volta para o menu de onde veio (título ou pausa)
        Som("bloco", 0.4f);
    }

    void AtualizarSubmenu(int clique, int lado)
    {
        if (SubmenuAberto == Submenu.Opcoes) AtualizarOpcoes(clique, lado);
        else if (SubmenuAberto == Submenu.Slots) AtualizarSlots(clique);
        else AtualizarComoJogar(clique);
    }

    // ------------------------------------------------------------------ os slots (Carregar jogo / Novo jogo)

    bool novoNaTempestade; // o "Novo jogo" escolhido no menu é o do modo tempestade

    void AbrirSlots(bool paraNovoJogo)
    {
        if (!paraNovoJogo) novoNaTempestade = false;
        AbrirSubmenu(Submenu.Slots);
        SlotsParaNovoJogo = paraNovoJogo;
        SlotPerguntando = -1;
        LerResumos();
        // começa no slot que faz mais sentido: carregando, o último usado (se tiver partida) ou o primeiro com
        // partida; num jogo novo, o primeiro vazio (se todos estiverem ocupados, o último usado)
        SlotSelecionado = Progresso.SlotAtual;
        for (int i = 0; i < Progresso.TotalDeSlots; i++)
        {
            if (paraNovoJogo ? resumos[SlotSelecionado].temJogo && !resumos[i].temJogo : !resumos[SlotSelecionado].temJogo && resumos[i].temJogo)
            {
                SlotSelecionado = i;
                break;
            }
        }
    }

    void LerResumos()
    {
        for (int i = 0; i < Progresso.TotalDeSlots; i++) resumos[i] = Progresso.Resumo(i);
        VersaoDosSlots++;
    }

    void AtualizarSlots(int clique)
    {
        // a pergunta aberta ("apagar?" / "começar por cima?"): ENTER ou SIM confirma, ESC ou NÃO cancela
        if (SlotPerguntando >= 0)
        {
            if (Controles.Voltar() || clique == BotaoVoltar || clique == BotaoNao)
            {
                SlotPerguntando = -1;
                Som("bloco", 0.4f);
            }
            else if (Controles.Confirmar() || clique == BotaoSim) ResponderSim();
            return;
        }

        int voltar = Progresso.TotalDeSlots;
        if (Controles.Voltar() || clique == BotaoVoltar)
        {
            FecharSlots();
            return;
        }
        if (Controles.CimaApertou()) SlotSelecionado = Girar(SlotSelecionado, -1, voltar + 1);
        if (Controles.BaixoApertou()) SlotSelecionado = Girar(SlotSelecionado, 1, voltar + 1);

        // apagar: DEL (ou o "apagar" do cartão, com o mouse)
        int apagar = clique <= BotaoApagarSlot && clique > BotaoApagarSlot - Progresso.TotalDeSlots ? BotaoApagarSlot - clique
                   : Controles.Apagar() && SlotSelecionado < voltar ? SlotSelecionado : -1;
        if (apagar >= 0)
        {
            SlotSelecionado = apagar;
            if (resumos[apagar].temJogo) Perguntar(apagar, true);
            else Som("bloco", 0.6f); // vazio: nada para apagar
            return;
        }

        bool confirmou = Controles.Confirmar();
        if (clique >= 0 && clique <= voltar)
        {
            SlotSelecionado = clique;
            confirmou = true;
        }
        if (!confirmou) return;

        if (SlotSelecionado == voltar)
        {
            FecharSlots();
            return;
        }
        Progresso.ResumoDoSlot resumo = resumos[SlotSelecionado];
        if (!SlotsParaNovoJogo)
        {
            if (resumo.temJogo) ComecarNoSlot(SlotSelecionado, false);
            else Som("bloco", 0.6f); // carregar um slot vazio? não dá
        }
        else if (resumo.temJogo) Perguntar(SlotSelecionado, false); // tem partida: pergunta antes de apagar
        else ComecarNoSlot(SlotSelecionado, true);
    }

    void Perguntar(int slot, bool apagar)
    {
        SlotPerguntando = slot;
        PerguntaDeApagar = apagar;
        Som("pop", 0.6f);
    }

    void ResponderSim()
    {
        int slot = SlotPerguntando;
        SlotPerguntando = -1;
        if (!PerguntaDeApagar)
        {
            ComecarNoSlot(slot, true); // o jogo novo apaga o slot
            return;
        }
        Progresso.ApagarSlot(slot);
        LerResumos();
        Som("pancada", 0.5f);
        // carregando, sem mais nenhuma partida salva: não sobrou o que carregar
        if (!SlotsParaNovoJogo && !Progresso.AlgumJogoSalvo) FecharSlots();
    }

    // Volta para o título (o menu é montado de novo: o "Continuar" e o "Carregar jogo" podem ter sumido).
    void FecharSlots()
    {
        FecharSubmenu();
        MontarMenu();
        // fica em cima do botão de onde veio
        int de = opcoesDoMenu.IndexOf(SlotsParaNovoJogo ? OpcaoDoMenu.NovoJogo : OpcaoDoMenu.CarregarJogo);
        OpcaoSelecionada = de >= 0 ? de : opcoesDoMenu.IndexOf(OpcaoDoMenu.NovoJogo);
    }

    // Opções: cima/baixo escolhe; esquerda/direita muda; ENTER também muda (o volume dá a volta: 100% -> 0%).
    // Com o mouse: clicar na metade ESQUERDA da linha diminui, na metade DIREITA aumenta.
    void AtualizarOpcoes(int clique, int lado)
    {
        if (Controles.Voltar() || clique == BotaoVoltar)
        {
            FecharSubmenu();
            return;
        }
        if (Controles.CimaApertou()) OpcaoDasOpcoes = Girar(OpcaoDasOpcoes, -1, TotalDasOpcoes);
        if (Controles.BaixoApertou()) OpcaoDasOpcoes = Girar(OpcaoDasOpcoes, 1, TotalDasOpcoes);

        int mudanca = Controles.DireitaApertou() ? 1 : Controles.EsquerdaApertou() ? -1 : 0;
        bool confirmou = Controles.Confirmar();
        if (clique >= 0 && clique < TotalDasOpcoes)
        {
            OpcaoDasOpcoes = clique;
            mudanca = lado;
            confirmou = false;
        }
        else if (clique >= 0) return;

        var item = (ItemDasOpcoes)OpcaoDasOpcoes;
        if (item == ItemDasOpcoes.Voltar)
        {
            if (confirmou || clique >= 0) FecharSubmenu();
            return;
        }
        if (mudanca == 0 && !confirmou) return;

        switch (item)
        {
            case ItemDasOpcoes.Musica:
                Opcoes.Musica = MudarVolume(Opcoes.Musica, mudanca, confirmou);
                break;
            case ItemDasOpcoes.Efeitos:
                Opcoes.Efeitos = MudarVolume(Opcoes.Efeitos, mudanca, confirmou);
                break;
            case ItemDasOpcoes.Graficos:
                Opcoes.GraficosHD2D = !Opcoes.GraficosHD2D;
                Paralaxe.Recriar(); // monta o fundo de novo com o visual novo (o pós-processamento muda sozinho)
                break;
            case ItemDasOpcoes.TelaCheia:
                Opcoes.AlternarTelaCheia();
                break;
            case ItemDasOpcoes.Cronometro:
                Opcoes.MostrarTempo = !Opcoes.MostrarTempo;
                break;
        }
        Som("moeda", 0.6f); // já toca no volume novo dos efeitos: serve de "teste"
    }

    // Volume de 10 em 10%. "darAVolta": com ENTER, depois de 100% vem 0%.
    static float MudarVolume(float volume, int mudanca, bool darAVolta)
    {
        if (darAVolta) return volume >= 0.95f ? 0f : Mathf.Round(volume * 10f + 1f) / 10f;
        return Mathf.Round(volume * 10f + mudanca) / 10f; // (Opcoes já segura entre 0 e 1)
    }

    // Como Jogar: esquerda/direita troca de página; ENTER vai para a próxima (na última, fecha); ESC fecha.
    void AtualizarComoJogar(int clique)
    {
        if (Controles.Voltar() || clique == BotaoVoltar)
        {
            FecharSubmenu();
            return;
        }
        int ultima = Textos.ComoJogar.Length - 1;
        int mudanca = Controles.DireitaApertou() || clique == BotaoProxima ? 1
                    : Controles.EsquerdaApertou() || clique == BotaoAnterior ? -1 : 0;
        if (Controles.Confirmar())
        {
            if (PaginaDoComoJogar >= ultima)
            {
                FecharSubmenu();
                return;
            }
            mudanca = 1;
        }
        int nova = Mathf.Clamp(PaginaDoComoJogar + mudanca, 0, ultima);
        if (nova == PaginaDoComoJogar) return;
        PaginaDoComoJogar = nova;
        Som("bloco", 0.5f);
    }

    // ------------------------------------------------------------------ créditos

    void AbrirCreditos(bool peloTitulo)
    {
        Estado = EstadoDoJogo.Creditos;
        TempoDosCreditos = 0f;
        CreditosDoTitulo = peloTitulo;
    }

    void FecharCreditos()
    {
        if (CreditosDoTitulo)
        {
            Estado = EstadoDoJogo.Titulo; // abriu pelo título: só volta para ele
            Som("bloco", 0.4f);
        }
        else Trocar(VoltarAoTitulo); // depois de zerar: título "de verdade" (zera a partida)
    }

    // ------------------------------------------------------------------ mouse nos menus

    int cliquePendente = SemClique, ladoDoClique;
    EstadoDoJogo estadoDoClique;
    Submenu submenuDoClique;

    // Chamado pela Interface quando o mouse passa por cima de uma opção: ela fica selecionada.
    // (a Interface só chama isso quando o mouse MEXE: o mouse parado não "rouba" a seleção do teclado)
    public void MouseEmCima(int opcao)
    {
        if (trocaPendente != null || opcao < 0) return;
        int antes;
        if (SubmenuAberto == Submenu.Opcoes)
        {
            antes = OpcaoDasOpcoes;
            if (opcao < TotalDasOpcoes) OpcaoDasOpcoes = opcao;
            if (OpcaoDasOpcoes != antes) Som("bloco", 0.5f);
        }
        else if (SubmenuAberto == Submenu.Slots)
        {
            if (SlotPerguntando >= 0 || opcao > Progresso.TotalDeSlots) return;
            antes = SlotSelecionado;
            SlotSelecionado = opcao;
            if (SlotSelecionado != antes) Som("bloco", 0.5f);
        }
        else if (SubmenuAberto != Submenu.Nenhum) return;
        else if (Estado == EstadoDoJogo.Mapa && LojaAberta)
        {
            antes = OpcaoDaLoja;
            if (opcao < TotalDaLoja) OpcaoDaLoja = opcao;
            if (OpcaoDaLoja != antes) Som("bloco", 0.5f);
        }
        else if (Estado == EstadoDoJogo.Titulo)
        {
            antes = OpcaoSelecionada;
            if (opcao < opcoesDoMenu.Count) OpcaoSelecionada = opcao;
            if (OpcaoSelecionada != antes) Som("bloco", 0.5f);
        }
        else if (Estado == EstadoDoJogo.Pausado)
        {
            antes = OpcaoDaPausa;
            if (opcao < TotalDaPausa) OpcaoDaPausa = opcao;
            if (OpcaoDaPausa != antes) Som("bloco", 0.5f);
        }
        else if (MostrandoOpcoes)
        {
            antes = OpcaoDaHistoria;
            if (opcao < OpcoesDaHistoria.Length) OpcaoDaHistoria = opcao;
            if (OpcaoDaHistoria != antes) Som("bloco", 0.5f);
        }
    }

    // Chamado pela Interface no clique. O clique fica GUARDADO e é tratado no próximo Update, junto com o
    // teclado (assim toda a lógica dos menus fica num lugar só). lado: -1 = metade esquerda, +1 = direita.
    public void MouseClicou(int opcao, int lado)
    {
        cliquePendente = opcao;
        ladoDoClique = lado;
        estadoDoClique = Estado;
        submenuDoClique = SubmenuAberto;
    }

    // Pega o clique guardado (e apaga). Se a tela mudou desde o clique, ele não vale mais.
    int PegarClique(out int lado)
    {
        int clique = cliquePendente;
        lado = ladoDoClique;
        cliquePendente = SemClique;
        if (clique == SemClique || estadoDoClique != Estado || submenuDoClique != SubmenuAberto || trocaPendente != null)
            return SemClique;
        return clique;
    }

    // Anda "direcao" casas numa lista de "total" opções, dando a volta nas pontas. Toca o som de mover.
    static int Girar(int atual, int direcao, int total)
    {
        Som("bloco", 0.5f);
        return (atual + direcao + total) % total;
    }

    // ------------------------------------------------------------------ menu do título

    void MontarMenu()
    {
        opcoesDoMenu.Clear();
        if (Progresso.TemJogoSalvo) opcoesDoMenu.Add(OpcaoDoMenu.Continuar);       // o último slot usado
        if (Progresso.AlgumJogoSalvo) opcoesDoMenu.Add(OpcaoDoMenu.CarregarJogo);  // a tela dos slots
        opcoesDoMenu.Add(OpcaoDoMenu.NovoJogo);                                     // a tela dos slots (escolhe onde)
        // Já zerou alguma vez? O MODO TEMPESTADE (o difícil) aparece no menu: antes só dava para chegar nele roubando o tesouro.
        if (Conquistas.Tem("zerou")) opcoesDoMenu.Add(OpcaoDoMenu.Tempestade);
        opcoesDoMenu.Add(OpcaoDoMenu.Opcoes);
        opcoesDoMenu.Add(OpcaoDoMenu.ComoJogar);
        opcoesDoMenu.Add(OpcaoDoMenu.Conquistas);
        opcoesDoMenu.Add(OpcaoDoMenu.Creditos);
        opcoesDoMenu.Add(OpcaoDoMenu.TrocarIdioma); // Português <-> English (veja Idioma.cs)
        if (Opcoes.PodeSair) opcoesDoMenu.Add(OpcaoDoMenu.Sair); // no navegador não existe "sair do jogo"
        OpcaoSelecionada = 0;
        SubmenuAberto = Submenu.Nenhum;
    }

    // Cima/baixo escolhe, ENTER (ou clique) confirma. ESC no título não sai do jogo: só pula para "Sair".
    void AtualizarMenu(int clique)
    {
        if (Controles.Voltar() || clique == BotaoVoltar)
        {
            int sair = opcoesDoMenu.IndexOf(OpcaoDoMenu.Sair);
            if (sair >= 0 && sair != OpcaoSelecionada)
            {
                OpcaoSelecionada = sair;
                Som("bloco", 0.5f);
            }
            return;
        }
        // clicou numa das bandeiras: aquele idioma (a linha das bandeiras fica selecionada)
        if (clique == BotaoBandeiraBrasil || clique == BotaoBandeiraEUA)
        {
            OpcaoSelecionada = Mathf.Max(0, opcoesDoMenu.IndexOf(OpcaoDoMenu.TrocarIdioma));
            Idioma.Atual = clique == BotaoBandeiraEUA ? Idioma.Lingua.Ingles : Idioma.Lingua.Portugues;
            Som("moeda", 0.5f);
            return;
        }
        if (Controles.CimaApertou()) OpcaoSelecionada = Girar(OpcaoSelecionada, -1, opcoesDoMenu.Count);
        if (Controles.BaixoApertou()) OpcaoSelecionada = Girar(OpcaoSelecionada, 1, opcoesDoMenu.Count);
        // no botão do idioma, esquerda/direita também trocam (como nas Opções)
        if (opcoesDoMenu[OpcaoSelecionada] == OpcaoDoMenu.TrocarIdioma && (Controles.EsquerdaApertou() || Controles.DireitaApertou()))
        {
            Comecar(OpcaoDoMenu.TrocarIdioma);
            return;
        }

        if (clique >= 0 && clique < opcoesDoMenu.Count)
        {
            OpcaoSelecionada = clique;
            Comecar(opcoesDoMenu[clique]);
        }
        else if (Controles.Confirmar()) Comecar(opcoesDoMenu[OpcaoSelecionada]);
    }

    void Comecar(OpcaoDoMenu opcao)
    {
        switch (opcao)
        {
            case OpcaoDoMenu.Conquistas:
                Estado = EstadoDoJogo.Conquistas;
                RolagemDasConquistas = 0f; // sempre abre no topo da lista
                Som("moeda", 0.5f);
                return;
            case OpcaoDoMenu.Opcoes:
                AbrirSubmenu(Submenu.Opcoes);
                return;
            case OpcaoDoMenu.ComoJogar:
                AbrirSubmenu(Submenu.ComoJogar);
                return;
            case OpcaoDoMenu.Creditos:
                AbrirCreditos(true);
                Som("moeda", 0.5f);
                return;
            case OpcaoDoMenu.TrocarIdioma:
                Idioma.Trocar();
                Som("moeda", 0.5f);
                return;
            case OpcaoDoMenu.Sair:
                Opcoes.SairDoJogo();
                return;
            case OpcaoDoMenu.CarregarJogo:
                AbrirSlots(false);
                return;
            case OpcaoDoMenu.NovoJogo:
                novoNaTempestade = false;
                AbrirSlots(true);
                return;
            case OpcaoDoMenu.Tempestade: // um jogo novo já no modo difícil (escolhe o slot igual ao "Novo jogo")
                novoNaTempestade = true;
                AbrirSlots(true);
                return;
        }
        // Continuar: o último slot usado (sem partida nele, escolhe onde começar)
        if (Progresso.TemJogoSalvo) ComecarNoSlot(Progresso.SlotAtual, false);
        else AbrirSlots(true);
    }

    // Começa a partida no slot: carrega a que está lá ou (novo) apaga o slot e começa do zero.
    void ComecarNoSlot(int slot, bool novo)
    {
        Progresso.SlotAtual = slot;
        if (novo) Progresso.ApagarSlot(slot);
        ModoDificil = !novo && Progresso.ModoDificil;
        SubmenuAberto = Submenu.Nenhum;
        Som("vitoria", 0.4f);
        int fase;
        if (!novo && Progresso.TemJogoSalvo) // (confere de novo: sem save, vira "Novo jogo")
        {
            Progresso.Carregar(out fase, out int maisLonge, out int mortesSalvas, out moedas, out float tempoSalvo, out partidaValida);
            Progresso.CarregarMortesPorFase(mortesPorFase);
            FaseMaisLonge = maisLonge;
            Mortes = mortesSalvas;
            TempoTotal = tempoSalvo;
            Escudos = Mathf.Max(0, Progresso.EscudosSalvos);
            Elixires = Mathf.Max(0, Progresso.ElixiresSalvos);
            PocoesDeEspinhos = Mathf.Max(0, Progresso.PocoesDeEspinhosSalvas);
            PocoesDoChao = Mathf.Max(0, Progresso.PocoesDoChaoSalvas);
            Fichas = Mathf.Clamp(Progresso.FichasSalvas(fichasIniciais), 0, maximoDeFichas);
            TemPuloDuplo = Progresso.PuloDuploSalvo;
            for (int i = 1; i < Tripulacao.Maximo; i++) // (os itens dos outros jogadores da tripulação)
            {
                escudos[i] = Mathf.Max(0, Progresso.EscudosSalvosDe(i));
                elixires[i] = Mathf.Max(0, Progresso.ElixiresSalvosDe(i));
                puloDuplo[i] = Progresso.PuloDuploSalvoDe(i);
            }
        }
        else
        {
            fase = Mathf.Clamp(faseInicial, 0, Fases.Todas.Length - 1);
            partidaValida = fase == 0;
            Progresso.ApagarSecreta(); // partida nova: a fase secreta precisa ser achada de novo
        }
        if (!novo)
        {
            if (Progresso.EscolhaPendente) Trocar(TocarEscolhaDoTesouro); // fechou o jogo na frente do baú: volta para ele
            else Trocar(() => MostrarMapa(fase));
            return;
        }
        // jogo novo no MODO TEMPESTADE (pelo menu, depois de zerar): direto para o mapa, com o tesouro no bolso
        if (novoNaTempestade)
        {
            novoNaTempestade = false;
            ComecarTempestade();
            Trocar(() =>
            {
                MostrarMapa(0);
                MostrarFala(Textos.TempestadeDoMenu);
                SalvarProgresso();
            });
            return;
        }
        // jogo novo: primeiro a história (a abertura), depois o mapa
        Trocar(() => TocarHistoria(Historia.Abertura, () =>
        {
            MostrarMapa(fase);
            SalvarProgresso(); // o slot já aparece ocupado (mesmo se sair antes de jogar)
        }));
    }

    // No mapa: setas andam pelo caminho, Enter entra na fase, Esc volta ao título.
    // No ponto da fase do segredo, seta para baixo vai para a fase secreta (se ela já apareceu).
    void AtualizarMapa(int clique, int lado)
    {
        if (Tripulacao.LobbyAberto)
        {
            AtualizarTelaDaTripulacao(clique); // (e mais nada: ENTER aqui fecha a tela, não entra na fase)
            return;
        }
        if (LojaAberta)
        {
            AtualizarRoleta();
            if (NoTutorial) AtualizarTutorialNaLoja(clique);
            else AtualizarLoja(clique, lado);
            return;
        }
        if (NoTutorial)
        {
            if (Controles.Pausar()) // ESC: volta ao título (o tutorial recomeça quando voltar ao mapa)
            {
                SalvarProgresso();
                Trocar(VoltarAoTitulo);
                return;
            }
            AtualizarTutorialNoMapa(clique);
            return;
        }
        if (Fala.HasValue && Controles.Confirmar())
        {
            PassarFala(); // Enter fecha a fala do personagem (e mostra a próxima, se tiver)
            return;
        }
        if (Controles.Pausar())
        {
            SalvarProgresso();
            Trocar(VoltarAoTitulo);
            return;
        }
        if (Mapa.Andando) return;

        // passou da 2ª fase e ainda não conhece a Loja: o Louro chega voando (depois da fala, se tiver uma)
        if (DeveMostrarTutorialDaLoja && !Fala.HasValue)
        {
            IniciarTutorialDaLoja();
            return;
        }

        if (LojaLiberada && (Controles.AbrirLoja() || clique == BotaoLoja))
        {
            AbrirLoja();
            return;
        }

        // T: a tela da Tripulação (coop local até 4: cada um aperta PULAR no seu aparelho para entrar)
        if (Controles.AbrirTripulacao() || clique == BotaoTripulacao)
        {
            AbrirTripulacao();
            return;
        }

        if (Controles.LiberarTudo())
        {
            // Modo de teste: abre o mapa inteiro (todas as fases, os chefes e a secreta), SÓ nesta partida.
            // Um Novo Jogo volta tudo para a névoa. Não vale para o recorde.
            FaseMaisLonge = Fases.Todas.Length - 1;
            partidaValida = false;
            Progresso.LiberarSecreta();
            SalvarProgresso();
            Som("vitoria", 0.6f);
            MostrarMapa(Mapa.Selecionado);
            MostrarFala(Textos.ModoDeTeste);
            return;
        }

        if (Controles.AlternarTempestade())
        {
            // Modo de teste: liga e desliga o modo difícil sem precisar zerar o jogo. Não vale para o recorde.
            ModoDificil = !ModoDificil;
            partidaValida = false;
            Progresso.SalvarModoDificil(ModoDificil);
            SalvarProgresso();
            Som("vitoria", 0.6f);
            MostrarMapa(Mapa.Selecionado);
            MostrarFala(ModoDificil ? Textos.TesteTempestadeLigada : Textos.TesteTempestadeDesligada);
            return;
        }

        if (Controles.IrParaOFinal())
        {
            // Modo de teste: vai direto para a escolha do baú, como se tivesse vencido o Kraken.
            FaseMaisLonge = Fases.Todas.Length - 1;
            partidaValida = false;
            SalvarProgresso();
            Progresso.SalvarEscolhaPendente(true);
            Trocar(TocarEscolhaDoTesouro);
            return;
        }

        bool direita = Controles.DireitaApertou(), esquerda = Controles.EsquerdaApertou();
        bool cima = Controles.CimaApertou(), baixo = Controles.BaixoApertou();
        if (Mapa.Selecionado == Fases.IndiceSecreto)
        {
            if (direita || esquerda || cima) Mapa.VoltarDaSecreta();
        }
        else if (baixo && Mapa.Selecionado == Fases.FaseDoSegredo && Progresso.SecretaLiberada) Mapa.IrParaSecreta();
        else if (direita || cima) Mapa.Mover(1);
        else if (esquerda || baixo) Mapa.Mover(-1);

        if (!Mapa.Andando && Controles.Confirmar())
        {
            int fase = Mapa.Selecionado;
            Som("vitoria", 0.4f);
            TrocarParaAFase(fase, () => JogarFase(fase));
        }
    }

    // ------------------------------------------------------------------ loja

    void AbrirLoja()
    {
        Comprador = Mathf.Clamp(Comprador, 0, Tripulacao.Quantos - 1);
        LojaAberta = true;
        OpcaoDaLoja = (int)ItemDaLoja.Escudo;
        LimparFalas();
        timerMensagemDaLoja = 0f;
        Som("moeda", 0.5f);
    }

    void FecharLoja()
    {
        LojaAberta = false;
        JogosAbertos = false;
        Som("bloco", 0.4f);
    }

    // Na Loja: CIMA/BAIXO escolhem, ENTER compra (ou gira a roleta), ESQUERDA/DIREITA mudam a aposta e a cor,
    // ESC (ou L de novo) fecha. O mouse também funciona. Com a roleta girando, nada responde: espere ela parar.
    void AtualizarLoja(int clique, int lado)
    {
        timerMensagemDaLoja -= Time.deltaTime;
        if (Girando) return;
        if (JogosAbertos) // a tela dos jogos de habilidade, por cima da Loja (GerenciadorJogosDaLoja.cs)
        {
            AtualizarJogosDaLoja(clique);
            return;
        }
        if (Controles.Voltar() || Controles.Pausar() || Controles.AbrirLoja() || clique == BotaoVoltar)
        {
            FecharLoja();
            return;
        }
        if (Controles.CimaApertou()) OpcaoDaLoja = Girar(OpcaoDaLoja, -1, TotalDaLoja);
        else if (Controles.BaixoApertou()) OpcaoDaLoja = Girar(OpcaoDaLoja, 1, TotalDaLoja);
        // coop: para QUEM é a compra (TAB troca; ou clique no rostinho)
        if (Tripulacao.Coop)
        {
            if (Controles.TrocarComprador()) EscolherComprador((Comprador + 1) % Tripulacao.Quantos);
            else if (clique <= BotaoComprador && clique > BotaoComprador - Tripulacao.Quantos) EscolherComprador(BotaoComprador - clique);
        }
        int mudar = Controles.EsquerdaApertou() ? -1 : Controles.DireitaApertou() ? 1 : 0;
        if (mudar != 0 && (ItemDaLoja)OpcaoDaLoja == ItemDaLoja.Aposta) MudarAposta(mudar);
        else if (mudar != 0 && (ItemDaLoja)OpcaoDaLoja == ItemDaLoja.Cor) EscolherCor((CorDaRoleta)(((int)CorEscolhida + mudar + 3) % 3));

        // os botões próprios da roleta (mouse)
        switch (clique)
        {
            case BotaoApostaMenos: OpcaoDaLoja = (int)ItemDaLoja.Aposta; MudarAposta(-1); return;
            case BotaoApostaMais: OpcaoDaLoja = (int)ItemDaLoja.Aposta; MudarAposta(1); return;
            case BotaoVermelho: OpcaoDaLoja = (int)ItemDaLoja.Cor; EscolherCor(CorDaRoleta.Vermelho); return;
            case BotaoPreto: OpcaoDaLoja = (int)ItemDaLoja.Cor; EscolherCor(CorDaRoleta.Preto); return;
            case BotaoVerde: OpcaoDaLoja = (int)ItemDaLoja.Cor; EscolherCor(CorDaRoleta.Verde); return;
        }

        bool escolheu = Controles.Confirmar();
        if (clique >= 0 && clique < TotalDaLoja)
        {
            OpcaoDaLoja = clique;
            escolheu = true;
        }
        if (!escolheu) return;
        switch ((ItemDaLoja)OpcaoDaLoja)
        {
            case ItemDaLoja.Escudo: ComprarEscudo(); break;
            case ItemDaLoja.Elixir: ComprarElixir(); break;
            case ItemDaLoja.PuloDuplo: ComprarPuloDuplo(); break;
            case ItemDaLoja.PocaoDeEspinhos: ComprarPocaoDeEspinhos(); break;
            case ItemDaLoja.PocaoDoChao: ComprarPocaoDoChao(); break;
            case ItemDaLoja.Aposta: MudarAposta(clique >= 0 && lado < 0 ? -1 : 1); break; // (clique: metade esquerda diminui)
            case ItemDaLoja.Cor: EscolherCor((CorDaRoleta)(((int)CorEscolhida + 1) % 3)); break;
            case ItemDaLoja.Girar: GirarRoleta(); break;
            case ItemDaLoja.Jogos: AbrirJogos(); break;
            default: FecharLoja(); break;
        }
    }

    // Paga "preco" (se der): devolve false e explica por que não deu.
    bool Pagar(int preco)
    {
        if (moedas >= preco)
        {
            moedas -= preco;
            return true;
        }
        int faltam = preco - moedas;
        AvisarNaLoja(faltam == 1 ? "Falta 1 moeda." : Idioma.T($"Faltam {faltam} moedas.", $"You need {faltam} more coins."), false);
        Som("bloco", 0.6f);
        return false;
    }

    void ComprarEscudo()
    {
        if (Escudos >= maximoDeEscudos)
        {
            AvisarNaLoja(maximoDeEscudos == 1 ? "Você já tem um escudo!"
                : Idioma.T($"Você já tem {maximoDeEscudos} escudos (o máximo)!", $"You already have {maximoDeEscudos} shields (the max)!"), false);
            Som("bloco", 0.6f);
            return;
        }
        if (!Pagar(precoDoEscudo)) return;
        Escudos++;
        AvisarNaLoja("Escudo comprado! Ele te salva de UMA morte.", true);
        Som("conquista", 0.6f);
        SalvarProgresso();
    }

    void ComprarElixir()
    {
        if (Elixires >= maximoDeElixires)
        {
            AvisarNaLoja(Idioma.T($"Você já tem {maximoDeElixires} elixires (o máximo)!", $"You already have {maximoDeElixires} elixirs (the max)!"), false);
            Som("bloco", 0.6f);
            return;
        }
        if (!Pagar(precoDoElixir)) return;
        Elixires++;
        AvisarNaLoja("Elixir comprado! Ele vale a PRÓXIMA fase em que você entrar.", true);
        Som("conquista", 0.6f);
        SalvarProgresso();
    }

    // Confere o máximo e cobra: devolve false (e explica) se não deu.
    bool PagarPocao(int jaTem, int preco)
    {
        if (jaTem >= maximoDePocoes)
        {
            AvisarNaLoja(Idioma.T($"Você já tem {maximoDePocoes} dessas (o máximo)!", $"You already have {maximoDePocoes} of those (the max)!"), false);
            Som("bloco", 0.6f);
            return false;
        }
        return Pagar(preco);
    }

    void ComprarPocaoDeEspinhos()
    {
        if (!PagarPocao(PocoesDeEspinhos, precoDaPocaoDeEspinhos)) return;
        PocoesDeEspinhos++;
        AvisarNaLoja("Poção comprada! Vale a próxima fase com espinhos escondidos.", true);
        Som("conquista", 0.6f);
        SalvarProgresso();
    }

    void ComprarPocaoDoChao()
    {
        if (!PagarPocao(PocoesDoChao, precoDaPocaoDoChao)) return;
        PocoesDoChao++;
        AvisarNaLoja("Poção comprada! Vale a próxima fase com chão que cai.", true);
        Som("conquista", 0.6f);
        SalvarProgresso();
    }

    void ComprarPuloDuplo()
    {
        if (TemPuloDuplo)
        {
            AvisarNaLoja("Você já tem o duplo salto!", false);
            Som("bloco", 0.6f);
            return;
        }
        if (!Pagar(precoDoPuloDuplo)) return;
        TemPuloDuplo = true;
        AvisarNaLoja("Duplo salto! Aperte PULAR de novo no ar.", true);
        Som("vitoria", 0.5f);
        SalvarProgresso();
    }

    // ------------------------------------------------------------------ a roleta

    void MudarAposta(int direcao)
    {
        int antes = indiceDaAposta;
        indiceDaAposta = Mathf.Clamp(indiceDaAposta + direcao, 0, ValoresDaAposta.Length - 1);
        if (indiceDaAposta != antes) Som("moeda", 0.3f);
    }

    void EscolherCor(CorDaRoleta cor)
    {
        if (cor == CorEscolhida) return;
        CorEscolhida = cor;
        Som("bloco", 0.5f);
    }

    // GIRAR: o resultado é sorteado AGORA e o saldo final (sem a aposta, com o prêmio) já vai para o save:
    // sair do jogo no meio do giro não devolve nem perde nada. Só a animação (e o saldo na tela) espera ela parar.
    void GirarRoleta()
    {
        if (Girando) return;
        if (moedas < Aposta)
        {
            AvisarNaLoja(moedas == 0 ? "Você não tem moedas para apostar." : "Aposta maior que as suas moedas!", false);
            Som("bloco", 0.6f);
            return;
        }
        casaSorteada = Random.Range(0, CasasDaRoleta);
        premioPendente = CorDaCasa(casaSorteada) == CorEscolhida ? Aposta * Multiplicador(CorEscolhida) : 0;
        moedas += premioPendente - Aposta;
        Girando = true;
        SalvarProgresso();

        // a casa N fica no ângulo N * 24 (no sentido do relógio, a partir do ponteiro lá em cima). Para ela parar
        // no ponteiro, a roleta gira até "-ângulo da casa" (com um tiquinho para um lado ou para o outro dentro
        // da casa: parar sempre bem no meio parece de mentira), mais 5 voltas inteiras.
        float tamanho = 360f / CasasDaRoleta;
        float alvo = -(casaSorteada * tamanho) + Random.Range(-0.35f, 0.35f) * tamanho;
        anguloInicial = AnguloDaRoleta;
        float falta = Mathf.Repeat(alvo - anguloInicial, 360f);
        anguloFinal = anguloInicial + falta + 5f * 360f;
        tempoGirando = 0f;
        timerMensagemDaLoja = 0f;
        Som("serra", 0.5f);
    }

    // Anda a animação da roleta (rápida no começo, freando até parar) e paga quando ela para.
    void AtualizarRoleta()
    {
        TempoDesdeOResultado += Time.deltaTime;
        if (!Girando) return;
        float tamanho = 360f / CasasDaRoleta;
        int casaAntes = Mathf.FloorToInt((AnguloDaRoleta + tamanho / 2f) / tamanho);
        tempoGirando += Time.deltaTime;
        float p = Mathf.Clamp01(tempoGirando / DuracaoDoGiro);
        float freando = 1f - Mathf.Pow(1f - p, 3f);
        AnguloDaRoleta = Mathf.Lerp(anguloInicial, anguloFinal, freando);
        if (Mathf.FloorToInt((AnguloDaRoleta + tamanho / 2f) / tamanho) != casaAntes) Som("bloco", 0.25f); // o "tec" de cada casa
        if (p < 1f) return;

        AnguloDaRoleta = Mathf.Repeat(anguloFinal, 360f);
        Girando = false;
        UltimaCasa = casaSorteada;
        UltimoPremio = premioPendente;
        premioPendente = 0;
        TempoDesdeOResultado = 0f;
        HistoricoDaRoleta.Insert(0, casaSorteada);
        if (HistoricoDaRoleta.Count > 10) HistoricoDaRoleta.RemoveAt(HistoricoDaRoleta.Count - 1);
        string onde = Idioma.Traduzir(CorDaCasa(UltimaCasa) == CorDaRoleta.Verde ? "no VERDE" : CorDaCasa(UltimaCasa) == CorDaRoleta.Vermelho ? "no vermelho" : "no preto");
        if (UltimoPremio > 0)
        {
            AvisarNaLoja(Idioma.T($"Caiu {onde}! Você ganhou {UltimoPremio} moedas!", $"It landed {onde}! You won {UltimoPremio} coins!"), true);
            Som("conquista", 0.8f);
            Som("moeda", 0.8f);
        }
        else
        {
            AvisarNaLoja(Idioma.T($"Caiu {onde}... a casa venceu. Currupaco!", $"It landed {onde}... the house wins. Squawk!"), false);
            Som("pancada", 0.5f);
        }
    }

    void AvisarNaLoja(string texto, bool boa)
    {
        mensagemDaLoja = texto;
        MensagemDaLojaBoa = boa;
        timerMensagemDaLoja = 3f;
    }

    // ------------------------------------------------------------------ tutorial da loja (a cutscene do Louro)

    // O passo a passo (os textos ficam em Textos.Loja...):
    //   no mapa:   Chegada (ele chega voando) > Moedas > AbrirLoja (ele voa até o botão LOJA e espera o L)
    //   na Loja:   Comprar (só dá para comprar; se faltar moeda, ele completa) > Comprou > Fechar (ESC)
    // Durante o tutorial o resto do mapa fica parado (dá para sair com ESC: aí ele recomeça na próxima vez).
    void IniciarTutorialDaLoja()
    {
        Comprador = 0; // (o escudo de presente do Louro é do P1)
        LimparFalas();
        IrParaOPasso(PassoDoTutorial.Chegada);
        Som("mola", 0.4f); // "fuuuu": o Louro chegando
    }

    void IrParaOPasso(PassoDoTutorial passo)
    {
        Tutorial = passo;
        TempoNoPasso = 0f;
        switch (passo)
        {
            case PassoDoTutorial.Chegada: TextoDoTutorial = Textos.LojaChegada; break;
            case PassoDoTutorial.Moedas: TextoDoTutorial = Textos.LojaMoedas; break;
            case PassoDoTutorial.AbrirLoja: TextoDoTutorial = Textos.LojaComoAbrir; break;
            case PassoDoTutorial.Comprar: TextoDoTutorial = Textos.LojaComprar; break;
            case PassoDoTutorial.Comprou: TextoDoTutorial = Textos.LojaComprou; break;
            case PassoDoTutorial.Fechar: TextoDoTutorial = Textos.LojaFechar; break;
            default: TextoDoTutorial = ""; break;
        }
    }

    // ENTER passa a fala; um tempinho de espera em cada passo para não pular sem querer.
    bool PassarFalaDoTutorial()
    {
        float espera = Tutorial == PassoDoTutorial.Chegada ? DuracaoDaChegadaDoLouro : 0.3f;
        return TempoNoPasso >= espera && Controles.Confirmar();
    }

    void AtualizarTutorialNoMapa(int clique)
    {
        TempoNoPasso += Time.deltaTime;
        switch (Tutorial)
        {
            case PassoDoTutorial.Chegada:
                if (PassarFalaDoTutorial()) { Som("bloco", 0.4f); IrParaOPasso(PassoDoTutorial.Moedas); }
                break;
            case PassoDoTutorial.Moedas:
                if (PassarFalaDoTutorial()) { Som("mola", 0.3f); IrParaOPasso(PassoDoTutorial.AbrirLoja); }
                break;
            case PassoDoTutorial.AbrirLoja:
                if (Controles.AbrirLoja() || clique == BotaoLoja)
                {
                    AbrirLoja();
                    ComecarACompraDoTutorial();
                }
                break;
            default: // (os outros passos são dentro da Loja)
                AbrirLoja();
                ComecarACompraDoTutorial();
                break;
        }
    }

    // Entrou na Loja pelo tutorial: se não der para pagar o escudo, o Louro completa as moedas (só desta vez).
    void ComecarACompraDoTutorial()
    {
        if (Escudos >= maximoDeEscudos) // (já tinha escudo: de uma partida salva antes da Loja ter tutorial)
        {
            IrParaOPasso(PassoDoTutorial.Comprou);
            TextoDoTutorial = Textos.LojaJaTinha;
            return;
        }
        IrParaOPasso(PassoDoTutorial.Comprar);
        int faltam = precoDoEscudo - moedas;
        if (faltam <= 0) return;
        moedas += faltam;
        TextoDoTutorial = string.Format(Idioma.Traduzir(Textos.LojaComprarComAjuda), // (traduz antes de pôr o número)
            faltam == 1 ? Idioma.T("1 moeda", "1 coin") : faltam + Idioma.T(" moedas", " coins"));
        Som("moeda", 0.7f);
        SalvarProgresso();
    }

    // Na Loja, durante o tutorial: não dá para sair antes de comprar. Depois, ENTER passa a fala e ESC fecha.
    void AtualizarTutorialNaLoja(int clique)
    {
        TempoNoPasso += Time.deltaTime;
        switch (Tutorial)
        {
            case PassoDoTutorial.Comprar:
                OpcaoDaLoja = (int)ItemDaLoja.Escudo; // só a compra (o "Voltar ao mapa" espera)
                if (TempoNoPasso >= 0.3f && (Controles.Confirmar() || clique == (int)ItemDaLoja.Escudo))
                {
                    int antes = Escudos;
                    ComprarEscudo();
                    if (Escudos > antes) IrParaOPasso(PassoDoTutorial.Comprou);
                }
                break;
            case PassoDoTutorial.Comprou:
                OpcaoDaLoja = (int)ItemDaLoja.Escudo;
                if (PassarFalaDoTutorial()) { Som("bloco", 0.4f); IrParaOPasso(PassoDoTutorial.Fechar); }
                break;
            case PassoDoTutorial.Fechar:
                OpcaoDaLoja = (int)ItemDaLoja.Voltar;
                if (TempoNoPasso >= 0.3f && (Controles.Voltar() || Controles.Pausar() || Controles.AbrirLoja() || Controles.Confirmar()
                                             || clique == BotaoVoltar || clique == (int)ItemDaLoja.Voltar))
                    TerminarTutorialDaLoja();
                break;
            default: // (abriu a Loja num passo do mapa: começa a compra)
                ComecarACompraDoTutorial();
                break;
        }
    }

    void TerminarTutorialDaLoja()
    {
        IrParaOPasso(PassoDoTutorial.Nenhum);
        Progresso.MarcarTutorialDaLoja();
        FecharLoja();
        MostrarFala(Textos.LojaTchau);
    }

    // Chamado pelo Jogador quando algo ia matar ele: se tiver escudo, gasta um e ele NÃO morre.
    // (jogador: de quem é o escudo. No coop cada um gasta o seu.)
    public bool UsarEscudo(int jogador = 0)
    {
        if (Estado != EstadoDoJogo.Jogando || escudos[jogador] <= 0) return false;
        escudos[jogador]--;
        SalvarItensDaLoja(); // gastou, gastou: sair do jogo agora não devolve o escudo
        Avisar("O ESCUDO TE SALVOU!", 2f);
        Som("pancada", 0.7f);
        Som("moeda", 0.5f);
        return true;
    }

    // ------------------------------------------------------------------ chamados pelos objetos

    public void JogadorMorreu(Jogador quem, bool caiuNoBuraco)
    {
        if (Estado != EstadoDoJogo.Jogando) return;
        Mortes++;
        MortesNaFase++;
        mortesPorFase[Posicao(FaseAtual)]++;
        if (!reiniciando) // (R é da tripulação inteira: não entra na conta de ninguém)
        {
            quem.Marujo.mortes++;
            quem.Marujo.mortesNaPartida++;
        }
        if (!quem.Reiniciou) // (apertar R não marca o lugar: a bandeira continua onde você morreu de verdade)
        {
            bandeirasDeMorto[quem.Indice] = quem.LugarDaBandeira(caiuNoBuraco); // (cada jogador tem a sua bandeira)
            GuardarLugarDaMorte(quem.transform.position, caiuNoBuraco, quem.Indice);
        }
        Conquistas.Desbloquear("primeira_morte");
        if (Mortes >= 100) Conquistas.Desbloquear("cem_mortes");

        // COOP: ainda tem companheiro vivo? Então ninguém volta para o checkpoint: quem morreu vira um balão,
        // que os outros têm que estourar (JogadorCoop.cs). A morte conta, mas a fase continua.
        if (!reiniciando && TemOutroAtivo(quem))
        {
            // a fase não vai recomeçar agora: a bandeira dele é fincada na hora (a antiga sai)
            if (!quem.Reiniciou)
            {
                if (bandeirasFincadas[quem.Indice] != null) Destroy(bandeirasFincadas[quem.Indice]);
                bandeirasFincadas[quem.Indice] = MarcaDaMorte.FincarBandeira(raizDaFase, bandeirasDeMorto[quem.Indice].Value, quem.Indice);
            }
            quem.VirarBalaoDepois();
            Som("morte", 0.6f);
            if (!caiuNoBuraco) CameraSeguir.Tremer(0.15f, 0.2f);
            string nome = Tripulacao.Nome(quem.Indice);
            Avisar(Idioma.T($"{nome} virou balão! Estourem ele.", $"{nome} is on a balloon! Pop it."), 1.6f);
            return;
        }

        // Na 3ª morte desde que você pegou o ponto de save, ele "muda de lugar" e você volta para o checkpoint ANTERIOR
        // (ou para o começo, se era o primeiro). :) Sempre na 3ª: nunca antes, nunca depois.
        // (menos nas fases de chefe: lá o save da arena e a etapa da luta nunca se perdem; e no modo difícil, onde com a onda a 3ª morte vem rápido)
        if (pontoDeSave.HasValue && Chefe == null) mortesNoSave++;
        bool saveMudou = pontoDeSave.HasValue && Chefe == null && !ModoDificil && !Fases.Dados(FaseAtual).saveFiel && mortesNoSave >= mortesParaOSaveMudar;
        if (saveMudou) // (e as moedas voltam a ser as que o checkpoint anterior guardou)
        {
            savesPegos.Remove(SaveAtual());
            pontoDeSave = savesPegos.Count > 0 ? savesPegos[savesPegos.Count - 1].lugar : (Vector3?)null;
            mortesNoSave = 0;
            Conquistas.Desbloquear("save_mudou");
        }

        Estado = EstadoDoJogo.Morreu;
        duracaoDaMorte = MortesNaFase >= mortesParaEncurtar ? tempoAposMorteCurta : tempoAposMorte;
        timerEstado = duracaoDaMorte;
        Mensagem = saveMudou ? "O ponto de save mudou de lugar... :)" : EscolherMensagem();
        sonsDaMorte.Tocar(duracaoDaMorte);
        if (!caiuNoBuraco) CameraSeguir.Tremer(0.2f, 0.2f);
    }

    void GuardarLugarDaMorte(Vector3 lugar, bool caiuNoBuraco, int jogador)
    {
        if (caiuNoBuraco) lugar.y = 0.2f; // no fundo do buraco, onde ainda aparece na tela
        lugaresDasMortes.Add(lugar);
        donosDasMortes.Add(jogador);
        // (a última morte de cada jogador vira bandeira, não X: com mais bandeiras, cabem mais mortes na lista)
        int bandeiras = 0;
        foreach (Vector3? bandeira in bandeirasDeMorto)
            if (bandeira.HasValue) bandeiras++;
        while (lugaresDasMortes.Count > marcasDeMorte + Mathf.Max(0, bandeiras - 1))
        {
            lugaresDasMortes.RemoveAt(0);
            donosDasMortes.RemoveAt(0);
        }
    }

    public void FaseConcluida()
    {
        if (Estado != EstadoDoJogo.Jogando) return;
        Estado = EstadoDoJogo.FaseConcluida;
        timerEstado = EhUltimaFase && Sereia != null ? tempoComASereia : tempoAposConcluir;
        TempoNaConclusao = 0f;
        mandouContinuar = false;
        if (MortesNaFase == 0) Conquistas.Desbloquear("sem_morrer");
        if (nasCabines) Conquistas.Desbloquear("cabines");
        if (NaSecreta) Conquistas.Desbloquear("secreta");

        // Pegou TODAS as moedas da fase do segredo? Aparece a fase secreta no mapa.
        // (as guardadas num checkpoint contam: morrer depois dele não perde nenhuma)
        secretaNova = FaseAtual == Fases.FaseDoSegredo && !Progresso.SecretaLiberada
                      && moedasParaOSegredo > 0 && moedasNaFase >= moedasParaOSegredo;
        if (secretaNova) Progresso.LiberarSecreta();

        RegistrarRecordes();

        moedas += moedasNaFase;
        moedasNaFase = 0;
        GanharFichas(); // (o Louro dá fichas para os jogos de habilidade da Loja)
        DarTitulosDaTripulacao();
        foreach (Jogador grumete in jogadores) grumete.Congelar();
        Som("vitoria");
        if (EhUltimaFase && Sereia != null && !ModoDificil) Sereia.Comemorar(); // (quem roubou não ganha coraçõezinhos)
    }

    // Recordes e medalhas da fase que acabou de ser concluída (valem para sempre: Progresso e Medalhas).
    // Chamado ANTES de as moedas da fase irem para o total (aí elas zeram).
    void RegistrarRecordes()
    {
        Medalhas.Conjunto antes = Medalhas.DaFase(FaseAtual);
        // o ranking ANTES do recorde (veja Progresso.RegistrarNoRanking)
        int posicaoNoRanking = Progresso.RegistrarNoRanking(FaseAtual, TempoNaFase, MortesNaFase);
        var resultado = new ResultadoDaFase
        {
            tempo = TempoNaFase,
            mortes = MortesNaFase,
            moedas = moedasNaFase,
            totalDeMoedas = Medalhas.TotalDeMoedas(FaseAtual),
            novidades = Progresso.RegistrarFase(FaseAtual, TempoNaFase, MortesNaFase, moedasNaFase),
        };
        resultado.medalhas = Medalhas.DaFase(FaseAtual);
        resultado.medalhasNovas = resultado.medalhas & ~antes;
        resultado.ranking = Progresso.MelhoresTempos(FaseAtual);
        resultado.posicaoNoRanking = posicaoNoRanking;
        resultado.vezesConcluida = Progresso.VezesConcluida(FaseAtual);
        resultado.numero = ++resultadosMostrados;
        Resultado = resultado;
        if (resultado.medalhas == Medalhas.Conjunto.Todas) Conquistas.Desbloquear("medalhas");
        // bateu recorde ou ganhou medalha: um segundinho a mais na tela, para dar tempo de ler
        bool novidade = resultado.medalhasNovas != Medalhas.Conjunto.Nenhuma
                        || (resultado.novidades & ~Progresso.Novidade.Primeira) != Progresso.Novidade.Nada;
        if (novidade) timerEstado += 1f;
    }

    // "lugar": onde a moeda estava (a moeda '$' ou o bloco '?'): o checkpoint lembra quais já foram pegas.
    // "quem": o Grumete que pegou (no coop, conta para o título de Mão-leve; as moedas são da tripulação).
    public void GanharMoeda(Vector3 lugar, Jogador quem = null)
    {
        if (quem != null) quem.Marujo.moedas++;
        moedasNaFase++;
        moedasPegas.Add(ChaveDaMoeda(lugar));
    }

    // Essa moeda já foi pega e está guardada no checkpoint em que você renasceu? (aí ela não aparece de novo)
    public bool MoedaGuardada(Vector3 lugar) => moedasPegas.Contains(ChaveDaMoeda(lugar));

    // As moedas ficam nas casas da fase (de meio em meio bloco): o lugar arredondado serve de "nome" da moeda.
    static Vector2Int ChaveDaMoeda(Vector3 lugar) => new Vector2Int(Mathf.RoundToInt(lugar.x * 2f), Mathf.RoundToInt(lugar.y * 2f));

    // O checkpoint em que você renasce agora (null = no começo da fase).
    SavePego SaveAtual()
    {
        for (int i = savesPegos.Count - 1; i >= 0; i--)
            if (EhPontoDeSave(savesPegos[i].lugar)) return savesPegos[i];
        return null;
    }

    // Guarda no checkpoint atual as moedas de agora.
    void GuardarMoedasNoSave()
    {
        SavePego save = SaveAtual();
        if (save == null) return;
        save.moedas = moedasNaFase;
        save.pegas = new HashSet<Vector2Int>(moedasPegas);
    }

    // ------------------------------------------------------------------ o Kraken rouba moedas (ChefeKraken.cs)

    // As moedas que o Kraken roubou e ainda não devolveu (só valem nesta visita à fase: sair dela, perdeu).
    public int MoedasComOKraken { get; private set; }
    public bool KrakenRoubou { get; private set; } // ela roubou alguma vez nesta luta? (a conquista "Bolso fechado")

    // Tira até "quantas" moedas: primeiro as desta tentativa, depois as guardadas (nunca fica negativo).
    // As guardadas são salvas na hora: fechar o jogo não devolve. Devolve quantas ela levou.
    public int RoubarMoedas(int quantas)
    {
        int daFase = Mathf.Clamp(quantas, 0, moedasNaFase);
        int guardadas = Mathf.Clamp(quantas - daFase, 0, moedas);
        // as da fase saem primeiro das que ainda não estavam no checkpoint; se precisar mais, o checkpoint perde também
        // (senão, morrendo, elas voltariam E o Kraken ainda devolveria depois: moeda em dobro)
        SavePego save = SaveAtual();
        if (save != null)
        {
            int foraDoSave = Mathf.Max(0, moedasNaFase - save.moedas);
            save.moedas -= Mathf.Clamp(daFase - foraDoSave, 0, save.moedas);
        }
        moedasNaFase -= daFase;
        moedas -= guardadas;
        int levou = daFase + guardadas;
        if (levou == 0) return 0;
        MoedasComOKraken += levou;
        KrakenRoubou = true;
        if (guardadas > 0) Progresso.SalvarMoedas(moedas);
        return levou;
    }

    // O Kraken devolve (até) "quantas" das moedas que roubou, direto para as moedas guardadas (morrer depois não perde).
    public int DevolverMoedas(int quantas)
    {
        int volta = Mathf.Clamp(quantas, 0, MoedasComOKraken);
        if (volta == 0) return 0;
        MoedasComOKraken -= volta;
        moedas += volta;
        Progresso.SalvarMoedas(moedas);
        return volta;
    }

    // Chamado pelos chefes quando a luta começa: cada chefe tem a sua música (Musicas).
    // Se ela já está tocando (morreu e voltou para a luta), continua de onde estava.
    public void TocarMusicaDoChefe()
    {
        if (Chefe != null) musica.TocarClipe(FabricaDeMusica.DoChefe(Chefe.Musica));
    }

    public void ChefeVencido(Vector3 renascerEm)
    {
        ChefeDerrotado = true;
        pontoDeSave = renascerEm;
        savesPegos.Add(new SavePego { lugar = renascerEm }); // venceu o chefe: as moedas de até agora ficam guardadas
        GuardarMoedasNoSave();
        musica.TocarDaFase(FaseAtual);
    }

    // Ponto de save (chamados pelo PontoDeSave).
    public void SalvarPonto(Vector3 lugar)
    {
        if (!EhPontoDeSave(lugar))
        {
            savesPegos.Add(new SavePego { lugar = lugar });
            mortesNoSave = 0; // save novo: a contagem das 3 mortes recomeça
        }
        pontoDeSave = lugar;
        GuardarMoedasNoSave(); // as moedas de até agora ficam guardadas: morrer não perde mais elas
    }

    // Quantas moedas da fase estão guardadas no checkpoint atual (para o aviso do PontoDeSave).
    public int MoedasNoSave => SaveAtual()?.moedas ?? 0;
    public Vector3? PontoDeSave => pontoDeSave; // onde o Grumete renasce (null = no começo da fase)
    public bool EhPontoDeSave(Vector3 lugar) => pontoDeSave.HasValue && Vector3.Distance(pontoDeSave.Value, lugar) < 0.1f;

    // Mensagem rápida no meio da tela (controles invertidos, volta pro começo...).
    public void Avisar(string texto, float duracao = 2f)
    {
        aviso = texto;
        timerAviso = duracao;
    }

    public static void Som(string nome, float volume = 1f)
    {
        if (Instancia == null || !Instancia.sons.TryGetValue(nome, out AudioClip clipe)) return;
        Instancia.fonteDeAudio.PlayOneShot(clipe, volume * Opcoes.Efeitos);
    }

    public static bool JogadorVivo(out Vector2 posicao)
    {
        Jogador j = Instancia != null ? Instancia.Lider : null;
        if (j == null || j.Morto || Instancia.Estado != EstadoDoJogo.Jogando)
        {
            posicao = Vector2.zero;
            return false;
        }
        posicao = j.transform.position;
        return true;
    }

    string EscolherMensagem()
    {
        switch (Mortes)
        {
            case 1: return "Volta da Maré desbloqueada. :)";
            case 10: return "10 mortes! Tá indo bem (mentira)";
            case 25: return "25 mortes. Já pensou em jogar outra coisa?";
            case 50: return "50 MORTES! Parabéns pela persistência!";
            case 100: return "100 mortes. Respeito.";
        }
        Fase fase = DadosDaFase;
        if (MortesNaFase == 1 && fase.mudancas != null && fase.mudancas.Length > 0)
            return "Ah, e eu mudei umas coisinhas. :)";
        if (nasCabines && Random.value < 0.4f) return "Lembra para onde cada porta leva? Buuu.";
        if (MortesNaFase == 5) return "Dica: nem tudo é o que parece.";
        // só as frases que combinam com esta fase (as com etiqueta: "cozinheira:..." só onde tem a Cozinheira)
        var combinam = new List<string>();
        foreach (string frase in Fases.MensagensDeMorte)
        {
            int doisPontos = frase.IndexOf(':');
            bool temEtiqueta = doisPontos > 0 && doisPontos < 12 && frase.Substring(0, doisPontos).ToLowerInvariant() == frase.Substring(0, doisPontos);
            if (!temEtiqueta) combinam.Add(frase);
            else if (Fases.TemNaFase(fase, frase.Substring(0, doisPontos))) combinam.Add(frase.Substring(doisPontos + 1));
        }
        return combinam[Random.Range(0, combinam.Count)];
    }

    public static string Titulo(int mortes)
    {
        if (mortes < 10) return "Suspeito de hack";
        if (mortes < 30) return "Sobrevivente";
        if (mortes < 60) return "Teimoso profissional";
        if (mortes < 100) return "Colecionador de marés";
        return "Grumete imortal";
    }
}
