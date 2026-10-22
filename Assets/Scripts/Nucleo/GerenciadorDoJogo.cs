using System.Collections.Generic;
using UnityEngine;

public enum EstadoDoJogo { Titulo, Conquistas, Mapa, Jogando, Pausado, Morreu, FaseConcluida, Vitoria, Creditos }

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
public class GerenciadorDoJogo : MonoBehaviour
{
    public static GerenciadorDoJogo Instancia { get; private set; }
    public static Jogador JogadorAtual => Instancia != null ? Instancia.jogador : null;
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
        new Color32(185, 150, 120, 255), // madeira (as Cabines Assombradas)
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
    [Tooltip("Chance de o ponto de save \"mudar de lugar\" quando você morre (0 a 1).")]
    public float chanceDoSaveMudar = 0.25f;

    [Header("Fases")]
    public float tempoAposConcluir = 2.2f;
    public float tempoComASereia = 3.5f; // fim da última fase: um tempinho a mais com a Sereia

    // ------------------------------------------------------------------ estado (lido pela Interface)

    public EstadoDoJogo Estado { get; private set; }
    public int FaseAtual { get; private set; }
    public int Mortes { get; private set; }
    public int MortesNaFase { get; private set; }
    public int Moedas => moedas + moedasNaFase;
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
    }
    public ResultadoDaFase Resultado { get; private set; }
    public string Mensagem { get; private set; } = "";
    public string Aviso => timerAviso > 0f ? aviso : "";
    public Vector3 PosicaoInicial { get; private set; }
    public List<Transform> Bandeiras { get; private set; } = new List<Transform>();
    public bool EhUltimaFase => FaseAtual == Fases.Todas.Length - 1;
    public bool NaSecreta => FaseAtual == Fases.IndiceSecreto;
    // Para os efeitos de tela: a última fase é de noite, e as cabines têm luz quente.
    public bool FaseNoturna => Mapa == null && (DadosDaFase.noite || DadosDaFase.escura);
    public bool NasCabines => Mapa == null && nasCabines;
    public Fase DadosDaFase => Fases.Dados(FaseAtual);

    // 0 = acabou de morrer, 1 = vai renascer
    public float ProgressoDaMorte => Estado == EstadoDoJogo.Morreu ? Mathf.Clamp01(1f - timerEstado / duracaoDaMorte) : 0f;
    public bool PodePularMorte => Estado == EstadoDoJogo.Morreu && duracaoDaMorte - timerEstado >= tempoParaPularMorte;
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
    // Créditos: quantos segundos já rolaram. A duração cresce com o número de linhas (Textos.Creditos),
    // assim a velocidade da rolagem fica parecida mesmo se vocês acrescentarem mais nomes.
    public float TempoDosCreditos { get; private set; }
    public static float DuracaoDosCreditos => 8f + Textos.Creditos.Length * 0.8f;
    // Os créditos foram abertos pelo menu do título? (aí ENTER/ESC volta para o título, sem recomeçar nada)
    public bool CreditosDoTitulo { get; private set; }

    // ------------------------------------------------------------------ menus

    // Menu do título. "Continuar" só aparece se tiver jogo salvo; "Sair" some no navegador (WebGL).
    public enum OpcaoDoMenu { Continuar, NovoJogo, Opcoes, ComoJogar, Conquistas, Creditos, Sair }
    public readonly List<OpcaoDoMenu> opcoesDoMenu = new List<OpcaoDoMenu>();
    public int OpcaoSelecionada { get; private set; }

    // Menu de pausa (a ordem aqui é a ordem na tela).
    public enum ItemDaPausa { Continuar, ReiniciarFase, Opcoes, ComoJogar, VoltarAoMapa, VoltarAoTitulo }
    public static readonly int TotalDaPausa = System.Enum.GetValues(typeof(ItemDaPausa)).Length;
    public int OpcaoDaPausa { get; private set; }

    // Submenus que abrem por cima do título ou da pausa.
    public enum Submenu { Nenhum, Opcoes, ComoJogar }
    public Submenu SubmenuAberto { get; private set; }

    // Tela de opções (a ordem aqui é a ordem na tela). "Voltar" também é uma opção, para quem usa só o teclado.
    public enum ItemDasOpcoes { Musica, Efeitos, Graficos, TelaCheia, Cronometro, Voltar }
    public static readonly int TotalDasOpcoes = System.Enum.GetValues(typeof(ItemDasOpcoes)).Length;
    public int OpcaoDasOpcoes { get; private set; }

    // Como Jogar: em que página está (as páginas ficam em Textos.ComoJogar).
    public int PaginaDoComoJogar { get; private set; }

    // Mouse: a Interface (que sabe ONDE cada opção foi desenhada) avisa quando o mouse passa por cima
    // de uma opção ou clica nela. Além do número da opção, o clique pode ser num destes "botões":
    public const int BotaoVoltar = -2, BotaoAnterior = -3, BotaoProxima = -4;
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

    // Troca de tela com as sombras: primeiro cobre a tela, depois executa a troca.
    System.Action trocaPendente;
    float timerSaida;

    // Volta da Maré: onde você morreu nas últimas tentativas desta fase.
    readonly List<Vector3> lugaresDasMortes = new List<Vector3>();
    int faseDasMarcas = -1;

    Transform raizDaFase;
    Jogador jogador;
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
            faseDasMarcas = indice;
        }

        FaseAtual = indice;
        moedasNaFase = 0;
        Sereia = null;
        Chefe = null;
        Mapa = null;

        raizDaFase = new GameObject(indice == Fases.IndiceSecreto ? "Fase secreta" : "Fase " + (indice + 1)).transform;
        // a cor do céu vem da própria fase (Fases.cs); se ela não disser, usa a lista de cores daqui
        Color32? corDaFase = Fases.Dados(indice).corDoFundo;
        Color corDoFundo = corDaFase.HasValue ? (Color)corDaFase.Value
            : indice == Fases.IndiceSecreto ? corDaFaseSecreta
            : coresDoFundo.Length > 0 ? coresDoFundo[indice % coresDoFundo.Length] : Color.cyan;
        if (Camera.main != null) Camera.main.backgroundColor = corDoFundo;
        // a luz da fase vem ANTES de montar: assim as luzinhas só são criadas se a fase for escura
        Fase dados = Fases.Dados(indice);
        bool cabines = dados.portas != null;
        if (dados.escura) Luzes.Ambiente(0.22f, new Color(0.7f, 0.72f, 1f));
        else if (dados.noite) Luzes.Ambiente(0.55f, new Color(0.75f, 0.8f, 1f));
        else if (cabines) Luzes.Ambiente(0.72f, new Color(1f, 0.92f, 0.8f));
        else Luzes.Ambiente(indice == Fases.IndiceSecreto ? 1f : 0.93f, Color.white);
        InfoFase info = ConstrutorDeFase.Construir(dados, raizDaFase, MortesNaFase);
        bool noite = dados.noite || dados.escura;
        Paralaxe.Criar(raizDaFase, info.largura, corDoFundo, noite); // céu, montanhas, castelo, floresta...
        MarcaDaMorte.Criar(raizDaFase, lugaresDasMortes);

        PosicaoInicial = info.inicio;
        Bandeiras = info.bandeiras;
        moedasParaOSegredo = info.totalDeMoedas;
        nasCabines = info.temPortas;
        PrepararClima(indice, noite);
        // renasce no ponto de save, se tiver um
        jogador = ConstrutorDeFase.Criar<Jogador>("Jogador", raizDaFase, pontoDeSave ?? info.inicio);
        jogador.inversores = info.inversores;
        if (dados.escura) Luzes.Ponto(jogador.transform, new Color(1f, 0.95f, 0.85f), 6f, 1.6f); // no escuro: a "lanterna" do Grumete
        else if (noite || nasCabines) Luzes.Ponto(jogador.transform, new Color(0.9f, 0.9f, 1f), 4.5f, 0.7f); // o Grumete "ilumina" em volta
        aviso = "";
        cameraSeguir.Configurar(jogador.transform, info.largura, info.altura);

        // Fase de chefe: a música da luta é gerada AGORA (atrás da transição), para a luta não engasgar.
        // Renasceu dentro da arena (a luta vai continuar)? A música do chefe continua tocando, sem recomeçar.
        if (Chefe != null) FabricaDeMusica.DoChefe(Chefe.Musica);
        if (Chefe != null && !ChefeDerrotado && EhPontoDeSave(Chefe.PontoDeRenascer)) TocarMusicaDoChefe();
        else musica.TocarDaFase(indice); // se a música já é essa, continua de onde estava
    }

    // Partículas de cada fase (a luz é ajustada em CarregarFase, antes de montar a fase): noite com vaga-lumes, cabines com poeira dourada,
    // fases com "petalas" (Fases.cs) com pétalas, a secreta com brilhos e as outras com pólen.
    void PrepararClima(int indice, bool noite)
    {
        Ambiente.Tipo tipo = Ambiente.Tipo.Polen;
        if (noite) tipo = Ambiente.Tipo.VagaLumes;
        else if (nasCabines) tipo = Ambiente.Tipo.Poeira;
        else if (indice == Fases.IndiceSecreto) tipo = Ambiente.Tipo.Brilhos;
        else if (Fases.Dados(indice).petalas) tipo = Ambiente.Tipo.Petalas;
        Ambiente.Criar(raizDaFase, tipo, tipo == Ambiente.Tipo.VagaLumes ? 16 : tipo == Ambiente.Tipo.Poeira ? 40 : 26);
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
        jogador = null;
        Sereia = null;
        Bandeiras = new List<Transform>();
        FaseAtual = no == Fases.IndiceSecreto && Progresso.SecretaLiberada ? no : Mathf.Clamp(no, 0, Fases.Todas.Length - 1);
        if (Camera.main != null) Camera.main.backgroundColor = MapaDoMundo.CorDoChao;
        Luzes.Ambiente(1f, Color.white);

        Mapa = MapaDoMundo.Criar(raizDaFase, FaseAtual, FaseLiberada, FaseMaisLonge, mortesPorFase,
            Progresso.SecretaLiberada, andarPara, caminhoNovo, mostrarSecreta);
        cameraSeguir.Configurar(Mapa.Grumete, MapaDoMundo.Largura, MapaDoMundo.Altura);
        musica.TocarDaFase(FabricaDeMusica.MusicaDoMapa);
        Estado = EstadoDoJogo.Mapa;
    }

    void VoltarAoTitulo()
    {
        Mortes = 0;
        MortesNaFase = 0;
        moedas = 0;
        TempoTotal = 0f;
        FaseMaisLonge = 0;
        partidaValida = true;
        faseDasMarcas = -1; // partida nova: apaga as marcas das mortes
        pontoDeSave = null;
        Fala = null;
        System.Array.Clear(mortesPorFase, 0, mortesPorFase.Length);
        MostrarMapa(Progresso.TemJogoSalvo ? Progresso.FaseSalva : faseInicial);
        Estado = EstadoDoJogo.Titulo; // o mapa aparece escurecido atrás do título
        MontarMenu();
    }

    // Cobre a tela com as sombras e, quando ela estiver toda coberta, executa "depois".
    void Trocar(System.Action depois)
    {
        trocaPendente = depois;
        timerSaida = DuracaoTransicao;
    }

    void JogarFase(int fase)
    {
        if (fase != Fases.IndiceSecreto && fase > FaseMaisLonge) partidaValida = false; // pulou fases pelo mapa: não vale recorde
        MortesNaFase = 0;
        TempoNaFase = 0f;
        pontoDeSave = null;
        ChefeDerrotado = false;
        EtapaDoChefe = 0;
        Fala = null;
        CarregarFase(fase);
        Estado = EstadoDoJogo.Jogando;
        SalvarProgresso();
    }

    void Update()
    {
        timerRenascer -= Time.deltaTime;
        timerEntrada -= Time.unscaledDeltaTime; // tempo real: a transição funciona até com o jogo pausado
        AjustarMusica();
        // O clique do mouse é pego (e apagado) ANTES da transição: um clique no meio dela é ignorado.
        int clique = PegarClique(out int lado);
        if (Fala.HasValue)
        {
            timerFala -= Time.unscaledDeltaTime;
            if (timerFala <= 0f) Fala = null;
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
                break;

            case EstadoDoJogo.Mapa:
                AtualizarMapa();
                break;

            case EstadoDoJogo.Jogando:
                TempoTotal += Time.deltaTime;
                TempoNaFase += Time.deltaTime;
                timerAviso -= Time.deltaTime;
                if (Controles.Pausar()) Pausar(true);
                else if (Controles.Reiniciar()) jogador.Morrer(); // reiniciar conta como morte. Óbvio.
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
                timerEstado -= Time.deltaTime;
                if (timerEstado <= 0f) ProximaFase();
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
            MostrarFala(Textos.Escolher(concluida, MortesNaFase));
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
            MostrarFala(secretaNova ? Textos.SecretaLiberada : Textos.Escolher(concluida, MortesNaFase));
            secretaNova = false;
            Progresso.Salvar(proxima, FaseMaisLonge, Mortes, moedas, TempoTotal, partidaValida);
            Progresso.SalvarMortesPorFase(mortesPorFase);
            return;
        }

        Estado = EstadoDoJogo.Vitoria;
        Progresso.Zerou(Fases.Todas.Length);
        Conquistas.Desbloquear("zerou");
        if (partidaValida) Progresso.TentarSalvarRecorde(Mortes);
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
    }

    void MostrarFala(Textos.Fala fala)
    {
        Fala = fala;
        timerFala = 7f;
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
                Trocar(() =>
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
        else AtualizarComoJogar(clique);
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
        else if (SubmenuAberto != Submenu.Nenhum) return;
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
        if (Progresso.TemJogoSalvo) opcoesDoMenu.Add(OpcaoDoMenu.Continuar);
        opcoesDoMenu.Add(OpcaoDoMenu.NovoJogo);
        opcoesDoMenu.Add(OpcaoDoMenu.Opcoes);
        opcoesDoMenu.Add(OpcaoDoMenu.ComoJogar);
        opcoesDoMenu.Add(OpcaoDoMenu.Conquistas);
        opcoesDoMenu.Add(OpcaoDoMenu.Creditos);
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
        if (Controles.CimaApertou()) OpcaoSelecionada = Girar(OpcaoSelecionada, -1, opcoesDoMenu.Count);
        if (Controles.BaixoApertou()) OpcaoSelecionada = Girar(OpcaoSelecionada, 1, opcoesDoMenu.Count);

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
            case OpcaoDoMenu.Sair:
                Opcoes.SairDoJogo();
                return;
        }
        Som("vitoria", 0.4f);
        int fase;
        if (opcao == OpcaoDoMenu.Continuar && Progresso.TemJogoSalvo) // (confere de novo: sem save, vira "Novo jogo")
        {
            Progresso.Carregar(out fase, out int maisLonge, out int mortesSalvas, out moedas, out float tempoSalvo, out partidaValida);
            Progresso.CarregarMortesPorFase(mortesPorFase);
            FaseMaisLonge = maisLonge;
            Mortes = mortesSalvas;
            TempoTotal = tempoSalvo;
        }
        else
        {
            fase = Mathf.Clamp(faseInicial, 0, Fases.Todas.Length - 1);
            partidaValida = fase == 0;
            Progresso.ApagarSecreta(); // partida nova: a fase secreta precisa ser achada de novo
        }
        Trocar(() => MostrarMapa(fase));
    }

    // No mapa: setas andam pelo caminho, Enter entra na fase, Esc volta ao título.
    // No ponto da fase do segredo, seta para baixo vai para a fase secreta (se ela já apareceu).
    void AtualizarMapa()
    {
        if (Fala.HasValue && Controles.Confirmar())
        {
            Fala = null; // Enter fecha a fala do personagem
            return;
        }
        if (Controles.Pausar())
        {
            SalvarProgresso();
            Trocar(VoltarAoTitulo);
            return;
        }
        if (Mapa.Andando) return;

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
            Trocar(() => JogarFase(fase));
        }
    }

    // ------------------------------------------------------------------ chamados pelos objetos

    public void JogadorMorreu(bool caiuNoBuraco)
    {
        if (Estado != EstadoDoJogo.Jogando) return;
        Mortes++;
        MortesNaFase++;
        mortesPorFase[Posicao(FaseAtual)]++;
        GuardarLugarDaMorte(jogador.transform.position, caiuNoBuraco);
        Conquistas.Desbloquear("primeira_morte");
        if (Mortes >= 100) Conquistas.Desbloquear("cem_mortes");

        // Às vezes o ponto de save "muda de lugar" e você volta para o começo. :)
        // (menos nas fases de chefe: aí já seria maldade demais)
        bool saveMudou = pontoDeSave.HasValue && Chefe == null && Random.value < chanceDoSaveMudar;
        if (saveMudou)
        {
            pontoDeSave = null;
            Conquistas.Desbloquear("save_mudou");
        }

        Estado = EstadoDoJogo.Morreu;
        duracaoDaMorte = MortesNaFase >= mortesParaEncurtar ? tempoAposMorteCurta : tempoAposMorte;
        timerEstado = duracaoDaMorte;
        Mensagem = saveMudou ? "O ponto de save mudou de lugar... :)" : EscolherMensagem();
        sonsDaMorte.Tocar(duracaoDaMorte);
        if (!caiuNoBuraco) CameraSeguir.Tremer(0.2f, 0.2f);
    }

    void GuardarLugarDaMorte(Vector3 lugar, bool caiuNoBuraco)
    {
        if (caiuNoBuraco) lugar.y = 0.2f; // no fundo do buraco, onde ainda aparece na tela
        lugaresDasMortes.Add(lugar);
        while (lugaresDasMortes.Count > marcasDeMorte) lugaresDasMortes.RemoveAt(0);
    }

    public void FaseConcluida()
    {
        if (Estado != EstadoDoJogo.Jogando) return;
        Estado = EstadoDoJogo.FaseConcluida;
        timerEstado = EhUltimaFase && Sereia != null ? tempoComASereia : tempoAposConcluir;
        if (MortesNaFase == 0) Conquistas.Desbloquear("sem_morrer");
        if (nasCabines) Conquistas.Desbloquear("cabines");
        if (NaSecreta) Conquistas.Desbloquear("secreta");

        // Pegou TODAS as moedas da fase do segredo (sem morrer no meio)? Aparece a fase secreta no mapa.
        secretaNova = FaseAtual == Fases.FaseDoSegredo && !Progresso.SecretaLiberada
                      && moedasParaOSegredo > 0 && moedasNaFase >= moedasParaOSegredo;
        if (secretaNova) Progresso.LiberarSecreta();

        RegistrarRecordes();

        moedas += moedasNaFase;
        moedasNaFase = 0;
        jogador.Congelar();
        Som("vitoria");
        if (EhUltimaFase && Sereia != null) Sereia.Comemorar();
    }

    // Recordes e medalhas da fase que acabou de ser concluída (valem para sempre: Progresso e Medalhas).
    // Chamado ANTES de as moedas da fase irem para o total (aí elas zeram).
    void RegistrarRecordes()
    {
        Medalhas.Conjunto antes = Medalhas.DaFase(FaseAtual);
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
        Resultado = resultado;
        if (resultado.medalhas == Medalhas.Conjunto.Todas) Conquistas.Desbloquear("medalhas");
        // bateu recorde ou ganhou medalha: um segundinho a mais na tela, para dar tempo de ler
        bool novidade = resultado.medalhasNovas != Medalhas.Conjunto.Nenhuma
                        || (resultado.novidades & ~Progresso.Novidade.Primeira) != Progresso.Novidade.Nada;
        if (novidade) timerEstado += 1f;
    }

    public void GanharMoeda() => moedasNaFase++;

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
        musica.TocarDaFase(FaseAtual);
    }

    // Ponto de save (chamados pelo PontoDeSave).
    public void SalvarPonto(Vector3 lugar) => pontoDeSave = lugar;
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
        Jogador j = Instancia != null ? Instancia.jogador : null;
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
        return Fases.MensagensDeMorte[Random.Range(0, Fases.MensagensDeMorte.Length)];
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
