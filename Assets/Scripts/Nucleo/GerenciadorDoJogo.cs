using System.Collections.Generic;
using UnityEngine;

public enum EstadoDoJogo { Titulo, Conquistas, Mapa, Jogando, Pausado, Morreu, FaseConcluida, Vitoria, Creditos }

// O "cérebro" do jogo: carrega as fases e o mapa, conta mortes e moedas e cuida dos estados
// (título, conquistas, mapa do mundo, jogando, pausa, morte, fase concluída, vitória, créditos).
// Caminho normal: Título -> Mapa -> Fase -> (passou) -> Mapa -> próxima fase ... -> Vitória -> Créditos.
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
    [Tooltip("Cor de fundo de cada fase (tons pastel, como os ímãs de Re:Zero). Se faltar cor, repete do começo.")]
    public Color[] coresDoFundo =
    {
        new Color32(150, 215, 235, 255), // azul claro
        new Color32(245, 190, 120, 255), // laranja
        new Color32(130, 210, 200, 255), // verde-água
        new Color32(240, 150, 205, 255), // rosa
        new Color32(245, 222, 110, 255), // amarelo
        new Color32(195, 165, 230, 255), // lilás
        new Color32(245, 185, 195, 255), // rosa claro
        new Color32(185, 150, 120, 255), // madeira (a Biblioteca Proibida)
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
    [Tooltip("Quantas marcas da Bruxa ficam no lugar das últimas mortes.")]
    public int marcasDeMorte = 5;
    [Tooltip("Chance de o ponto de save \"mudar de lugar\" quando você morre (0 a 1).")]
    public float chanceDoSaveMudar = 0.25f;

    [Header("Fases")]
    public float tempoAposConcluir = 2.2f;
    public float tempoComAEmilia = 3.5f; // fim da última fase: um tempinho a mais com a Emilia

    // ------------------------------------------------------------------ estado (lido pela Interface)

    public EstadoDoJogo Estado { get; private set; }
    public int FaseAtual { get; private set; }
    public int Mortes { get; private set; }
    public int MortesNaFase { get; private set; }
    public int Moedas => moedas + moedasNaFase;
    public float TempoTotal { get; private set; }
    public string Mensagem { get; private set; } = "";
    public string Aviso => timerAviso > 0f ? aviso : "";
    public Vector3 PosicaoInicial { get; private set; }
    public List<Transform> Bandeiras { get; private set; } = new List<Transform>();
    public bool EhUltimaFase => FaseAtual == Fases.Todas.Length - 1;
    public bool NaSecreta => FaseAtual == Fases.IndiceSecreto;
    // Para os efeitos de tela: a última fase é de noite, e a biblioteca tem luz quente.
    public bool FaseNoturna => Mapa == null && EhUltimaFase;
    public bool NaBiblioteca => Mapa == null && naBiblioteca;
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
    public Emilia Emilia { get; set; }
    public BaleiaBranca Chefe { get; set; }
    // Já venceu a Baleia nesta fase? (se morrer depois, ela não volta)
    public bool ChefeDerrotado { get; private set; }

    // Fala de um personagem no mapa (depois de passar de fase). null = nenhuma.
    public Textos.Fala? Fala { get; private set; }
    // Menu de pausa: 0 = volume da música, 1 = volume dos efeitos.
    public int OpcaoDaPausa { get; private set; }
    // Créditos: quantos segundos já rolaram.
    public float TempoDosCreditos { get; private set; }
    public const float DuracaoDosCreditos = 24f;

    // ------------------------------------------------------------------ menu do título e mapa

    public enum OpcaoDoMenu { Continuar, NovoJogo, Conquistas }
    public readonly List<OpcaoDoMenu> opcoesDoMenu = new List<OpcaoDoMenu>();
    public int OpcaoSelecionada { get; private set; }

    public MapaDoMundo Mapa { get; private set; }
    // Fases já passadas nesta partida (os pontos verdes do mapa).
    public int FaseMaisLonge { get; private set; }
    // Mortes em cada fase nesta partida (as caveiras do mapa). A última posição é a da fase secreta.
    readonly int[] mortesPorFase = new int[Fases.Todas.Length + 1];
    static int Posicao(int fase) => fase == Fases.IndiceSecreto ? Fases.Todas.Length : fase;
    // Até que ponto do mapa dá para andar: o que você já alcançou em qualquer partida.
    public int FaseLiberada => Mathf.Clamp(Mathf.Max(Progresso.FaseMaxima, FaseMaisLonge, faseInicial), 0, Fases.Todas.Length - 1);

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
    bool naBiblioteca;       // a fase atual tem as portas da Beatrice
    bool secretaNova;        // acabou de liberar a fase secreta (o mapa mostra o caminho aparecendo)
    float timerFala;

    // Ponto de save: onde você renasce nesta fase (null = no começo).
    Vector3? pontoDeSave;

    // Troca de tela com as sombras: primeiro cobre a tela, depois executa a troca.
    System.Action trocaPendente;
    float timerSaida;

    // Retorno pela Morte: onde você morreu nas últimas tentativas desta fase.
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

        PrepararCamera();
        Luzes.PrepararGlobal();
        fonteDeAudio = gameObject.AddComponent<AudioSource>();
        sons = FabricaDeSons.CriarTodos();
        sonsDaMorte = gameObject.AddComponent<SonsDaMorte>();
        musica = gameObject.AddComponent<Musica>();
        gameObject.AddComponent<Interface>();
        gameObject.AddComponent<EfeitosDeTela>();

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
        if (Estado != EstadoDoJogo.Titulo && Estado != EstadoDoJogo.Vitoria) SalvarProgresso();
    }

    // Onde o Subaru está: no mapa, o ponto escolhido; numa fase, a própria fase.
    int FaseDoSubaru => Estado == EstadoDoJogo.Mapa && Mapa != null ? Mapa.Selecionado : FaseAtual;

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
        if (raizDaFase != null) Destroy(raizDaFase.gameObject);

        if (indice != faseDasMarcas)
        {
            lugaresDasMortes.Clear();
            faseDasMarcas = indice;
        }

        FaseAtual = indice;
        moedasNaFase = 0;
        Emilia = null;
        Chefe = null;
        Mapa = null;

        raizDaFase = new GameObject(indice == Fases.IndiceSecreto ? "Fase secreta" : "Fase " + (indice + 1)).transform;
        Color corDoFundo = indice == Fases.IndiceSecreto ? corDaFaseSecreta
            : coresDoFundo.Length > 0 ? coresDoFundo[indice % coresDoFundo.Length] : Color.cyan;
        if (Camera.main != null) Camera.main.backgroundColor = corDoFundo;
        // a luz da fase vem ANTES de montar: assim as luzinhas só são criadas se a fase for escura
        bool fasenoturna = indice == Fases.Todas.Length - 1;
        bool biblioteca = Fases.Dados(indice).portas != null;
        if (fasenoturna) Luzes.Ambiente(0.55f, new Color(0.75f, 0.8f, 1f));
        else if (biblioteca) Luzes.Ambiente(0.72f, new Color(1f, 0.92f, 0.8f));
        else Luzes.Ambiente(indice == Fases.IndiceSecreto ? 1f : 0.93f, Color.white);
        InfoFase info = ConstrutorDeFase.Construir(Fases.Dados(indice), raizDaFase, MortesNaFase);
        bool noite = EhUltimaFase;
        Paralaxe.Criar(raizDaFase, info.largura, corDoFundo, noite); // céu, montanhas, castelo, floresta...
        MarcaDaMorte.Criar(raizDaFase, lugaresDasMortes);

        PosicaoInicial = info.inicio;
        Bandeiras = info.bandeiras;
        moedasParaOSegredo = info.totalDeMoedas;
        naBiblioteca = info.temPortas;
        PrepararClima(indice, noite);
        // renasce no ponto de save, se tiver um
        jogador = ConstrutorDeFase.Criar<Jogador>("Jogador", raizDaFase, pontoDeSave ?? info.inicio);
        jogador.inversores = info.inversores;
        if (noite || naBiblioteca) Luzes.Ponto(jogador.transform, new Color(0.9f, 0.9f, 1f), 4.5f, 0.7f); // o Subaru "ilumina" em volta
        aviso = "";
        cameraSeguir.Configurar(jogador.transform, info.largura, info.altura);
        musica.TocarDaFase(indice); // se a música já é essa, continua de onde estava
    }

    // Partículas de cada fase (a luz é ajustada em CarregarFase, antes de montar a fase): noite com vaga-lumes, biblioteca com poeira dourada,
    // fases rosadas com pétalas, a secreta com brilhos e as outras com pólen.
    void PrepararClima(int indice, bool noite)
    {
        Ambiente.Tipo tipo = Ambiente.Tipo.Polen;
        if (noite) tipo = Ambiente.Tipo.VagaLumes;
        else if (naBiblioteca) tipo = Ambiente.Tipo.Poeira;
        else if (indice == Fases.IndiceSecreto) tipo = Ambiente.Tipo.Brilhos;
        else if (indice == 3 || indice == 6) tipo = Ambiente.Tipo.Petalas;
        Ambiente.Criar(raizDaFase, tipo, tipo == Ambiente.Tipo.VagaLumes ? 16 : tipo == Ambiente.Tipo.Poeira ? 40 : 26);
    }

    // Mostra o mapa do mundo com o Subaru no ponto "no".
    // andarPara >= 0: ele anda sozinho até lá (acabou de passar de fase); caminhoNovo: o caminho acabou de abrir.
    void MostrarMapa(int no, int andarPara = -1, bool caminhoNovo = false, bool mostrarSecreta = false)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (raizDaFase != null) Destroy(raizDaFase.gameObject);

        raizDaFase = new GameObject("Mapa").transform;
        jogador = null;
        Emilia = null;
        Bandeiras = new List<Transform>();
        FaseAtual = no == Fases.IndiceSecreto && Progresso.SecretaLiberada ? no : Mathf.Clamp(no, 0, Fases.Todas.Length - 1);
        if (Camera.main != null) Camera.main.backgroundColor = MapaDoMundo.CorDoChao;
        Luzes.Ambiente(1f, Color.white);

        Mapa = MapaDoMundo.Criar(raizDaFase, FaseAtual, FaseLiberada, FaseMaisLonge, mortesPorFase,
            Progresso.SecretaLiberada, andarPara, caminhoNovo, mostrarSecreta);
        cameraSeguir.Configurar(Mapa.Subaru, MapaDoMundo.Largura, MapaDoMundo.Altura);
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
        pontoDeSave = null;
        ChefeDerrotado = false;
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
                AtualizarMenu();
                break;

            case EstadoDoJogo.Conquistas:
                if (Controles.Confirmar() || Controles.Pausar()) Estado = EstadoDoJogo.Titulo;
                break;

            case EstadoDoJogo.Mapa:
                AtualizarMapa();
                break;

            case EstadoDoJogo.Jogando:
                TempoTotal += Time.deltaTime;
                timerAviso -= Time.deltaTime;
                if (Controles.Pausar()) Pausar(true);
                else if (Controles.Reiniciar()) jogador.Morrer(); // reiniciar conta como morte. Óbvio.
                break;

            case EstadoDoJogo.Pausado:
                AtualizarOpcoes();
                if (Controles.Pausar() || Controles.Confirmar()) Pausar(false);
                else if (Controles.SairParaOTitulo())
                {
                    int fase = FaseAtual;
                    Trocar(() =>
                    {
                        MostrarMapa(fase); // volta para o mapa, no ponto desta fase
                        SalvarProgresso();
                    });
                }
                break;

            case EstadoDoJogo.Morreu:
                TempoTotal += Time.deltaTime;
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
                    Trocar(() =>
                    {
                        Estado = EstadoDoJogo.Creditos;
                        TempoDosCreditos = 0f;
                    });
                break;

            case EstadoDoJogo.Creditos:
                TempoDosCreditos += Time.deltaTime;
                if (Controles.Confirmar() || Controles.Pausar() || TempoDosCreditos > DuracaoDosCreditos)
                    Trocar(VoltarAoTitulo);
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
            // volta para o mapa e o Subaru anda sozinho até a próxima fase
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
        Estado = pausar ? EstadoDoJogo.Pausado : EstadoDoJogo.Jogando;
        Time.timeScale = pausar ? 0f : 1f;
        AudioListener.pause = pausar; // a música de fundo ignora isso e só fica mais baixa
    }

    void SalvarProgresso()
    {
        Progresso.Salvar(FaseDoSubaru, FaseMaisLonge, Mortes, moedas, TempoTotal, partidaValida);
        Progresso.SalvarMortesPorFase(mortesPorFase);
    }

    void MostrarFala(Textos.Fala fala)
    {
        Fala = fala;
        timerFala = 7f;
    }

    // Menu de pausa: cima/baixo escolhe música ou efeitos, esquerda/direita muda o volume.
    void AtualizarOpcoes()
    {
        if (Controles.CimaApertou() || Controles.BaixoApertou()) OpcaoDaPausa = 1 - OpcaoDaPausa;
        int mudanca = Controles.DireitaApertou() ? 1 : Controles.EsquerdaApertou() ? -1 : 0;
        if (mudanca == 0) return;
        if (OpcaoDaPausa == 0) Opcoes.Musica = Mathf.Round(Opcoes.Musica * 10f + mudanca) / 10f;
        else Opcoes.Efeitos = Mathf.Round(Opcoes.Efeitos * 10f + mudanca) / 10f;
        Som("moeda", 0.6f);
    }

    void AjustarMusica()
    {
        float volume = 1f;
        if (Estado == EstadoDoJogo.Morreu || sonsDaMorte.OstTocando) volume = 0.15f; // a OST do Retorno pela Morte aparece
        else if (Estado == EstadoDoJogo.Pausado) volume = 0.35f;
        musica.Abafar(volume);
    }

    // ------------------------------------------------------------------ menu do título

    void MontarMenu()
    {
        opcoesDoMenu.Clear();
        if (Progresso.TemJogoSalvo) opcoesDoMenu.Add(OpcaoDoMenu.Continuar);
        opcoesDoMenu.Add(OpcaoDoMenu.NovoJogo);
        opcoesDoMenu.Add(OpcaoDoMenu.Conquistas);
        OpcaoSelecionada = 0;
    }

    void AtualizarMenu()
    {
        if (Controles.CimaApertou()) MudarOpcao(-1);
        if (Controles.BaixoApertou()) MudarOpcao(1);
        if (Controles.Confirmar()) Comecar(opcoesDoMenu[OpcaoSelecionada]);
    }

    void MudarOpcao(int direcao)
    {
        OpcaoSelecionada = (OpcaoSelecionada + direcao + opcoesDoMenu.Count) % opcoesDoMenu.Count;
        Som("bloco", 0.5f);
    }

    void Comecar(OpcaoDoMenu opcao)
    {
        if (opcao == OpcaoDoMenu.Conquistas)
        {
            Estado = EstadoDoJogo.Conquistas;
            return;
        }
        int fase;
        if (opcao == OpcaoDoMenu.Continuar)
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
        // (menos na fase da Baleia Branca: aí já seria maldade demais)
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
        timerEstado = EhUltimaFase && Emilia != null ? tempoComAEmilia : tempoAposConcluir;
        if (MortesNaFase == 0) Conquistas.Desbloquear("sem_morrer");
        if (naBiblioteca) Conquistas.Desbloquear("biblioteca");
        if (NaSecreta) Conquistas.Desbloquear("secreta");

        // Pegou TODAS as moedas da fase do segredo (sem morrer no meio)? Aparece a fase secreta no mapa.
        secretaNova = FaseAtual == Fases.FaseDoSegredo && !Progresso.SecretaLiberada
                      && moedasParaOSegredo > 0 && moedasNaFase >= moedasParaOSegredo;
        if (secretaNova) Progresso.LiberarSecreta();

        moedas += moedasNaFase;
        moedasNaFase = 0;
        jogador.Congelar();
        Som("vitoria");
        if (EhUltimaFase && Emilia != null) Emilia.Comemorar();
    }

    public void GanharMoeda() => moedasNaFase++;

    // Chamados pela Baleia Branca.
    public void TocarMusicaDoChefe() => musica.TocarDaFase(FabricaDeMusica.MusicaDoChefe);

    public void ChefeVencido(Vector3 renascerEm)
    {
        ChefeDerrotado = true;
        pontoDeSave = renascerEm;
        musica.TocarDaFase(FaseAtual);
    }

    // Ponto de save (chamados pelo PontoDeSave).
    public void SalvarPonto(Vector3 lugar) => pontoDeSave = lugar;
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
            case 1: return "Retorno pela Morte desbloqueado. :)";
            case 10: return "10 mortes! Tá indo bem (mentira)";
            case 25: return "25 mortes. Já pensou em jogar outra coisa?";
            case 50: return "50 MORTES! Parabéns pela persistência!";
            case 100: return "100 mortes. Respeito.";
        }
        Fase fase = DadosDaFase;
        if (MortesNaFase == 1 && fase.mudancas != null && fase.mudancas.Length > 0)
            return "Ah, e eu mudei umas coisinhas. :)";
        if (naBiblioteca && Random.value < 0.4f) return "Lembra para onde cada porta leva? Kashira.";
        if (MortesNaFase == 5) return "Dica: nem tudo é o que parece.";
        return Fases.MensagensDeMorte[Random.Range(0, Fases.MensagensDeMorte.Length)];
    }

    public static string Titulo(int mortes)
    {
        if (mortes < 10) return "Suspeito de hack";
        if (mortes < 30) return "Sobrevivente";
        if (mortes < 60) return "Teimoso profissional";
        if (mortes < 100) return "Colecionador de Retornos";
        return "Natsuki Subaru honorário";
    }
}
