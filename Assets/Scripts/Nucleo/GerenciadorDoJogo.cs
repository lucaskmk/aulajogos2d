using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum EstadoDoJogo { Titulo, Mapa, Jogando, Pausado, Morreu, FaseConcluida, Vitoria }

// O "cérebro" do jogo: carrega as fases e o mapa, conta mortes e moedas e cuida dos estados
// (título, mapa do mundo, jogando, pausa, morte, fase concluída, vitória).
// Caminho normal: Título -> Mapa -> Fase -> (passou) -> Mapa -> próxima fase ... -> Vitória.
// Quem desenha a tela é a Interface; os sons da morte ficam em SonsDaMorte e a música em Musica.
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
        new Color32(60, 80, 170, 255),   // azul-marinho (o "verdadeiro final")
    };
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
    public Emilia Emilia { get; set; }

    // ------------------------------------------------------------------ menu do título e mapa

    public enum OpcaoDoMenu { Continuar, NovoJogo }
    public readonly List<OpcaoDoMenu> opcoesDoMenu = new List<OpcaoDoMenu>();
    public int OpcaoSelecionada { get; private set; }

    public MapaDoMundo Mapa { get; private set; }
    // Fases já passadas nesta partida (os pontos verdes do mapa).
    public int FaseMaisLonge { get; private set; }
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
        PrepararLuz();
        fonteDeAudio = gameObject.AddComponent<AudioSource>();
        sons = FabricaDeSons.CriarTodos();
        sonsDaMorte = gameObject.AddComponent<SonsDaMorte>();
        musica = gameObject.AddComponent<Musica>();
        gameObject.AddComponent<Interface>();

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

    // O template 2D do URP usa sprites "iluminados": sem uma luz global tudo ficaria preto.
    static void PrepararLuz()
    {
        if (FindAnyObjectByType<Light2D>() != null) return;
        var luz = new GameObject("Global Light 2D").AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Global;
        luz.intensity = 1f;
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
        Mapa = null;

        raizDaFase = new GameObject("Fase " + (indice + 1)).transform;
        Color corDoFundo = coresDoFundo.Length > 0 ? coresDoFundo[indice % coresDoFundo.Length] : Color.cyan;
        if (Camera.main != null) Camera.main.backgroundColor = corDoFundo;
        InfoFase info = ConstrutorDeFase.Construir(Fases.Todas[indice], raizDaFase, MortesNaFase);
        Paralaxe.Criar(raizDaFase, info.largura, corDoFundo); // castelo e floresta ao fundo
        MarcaDaMorte.Criar(raizDaFase, lugaresDasMortes);

        PosicaoInicial = info.inicio;
        Bandeiras = info.bandeiras;
        jogador = ConstrutorDeFase.Criar<Jogador>("Jogador", raizDaFase, info.inicio);
        jogador.inversores = info.inversores;
        aviso = "";
        cameraSeguir.Configurar(jogador.transform, info.largura, info.altura);
        musica.TocarDaFase(indice); // se a música já é essa, continua de onde estava
    }

    // Mostra o mapa do mundo com o Subaru no ponto "no".
    // andarPara >= 0: ele anda sozinho até lá (acabou de passar de fase); caminhoNovo: o caminho acabou de abrir.
    void MostrarMapa(int no, int andarPara = -1, bool caminhoNovo = false)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (raizDaFase != null) Destroy(raizDaFase.gameObject);

        raizDaFase = new GameObject("Mapa").transform;
        jogador = null;
        Emilia = null;
        Bandeiras = new List<Transform>();
        FaseAtual = Mathf.Clamp(no, 0, Fases.Todas.Length - 1);
        if (Camera.main != null) Camera.main.backgroundColor = MapaDoMundo.CorDoChao;

        Mapa = MapaDoMundo.Criar(raizDaFase, FaseAtual, FaseLiberada, FaseMaisLonge, andarPara, caminhoNovo);
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
        MostrarMapa(Progresso.TemJogoSalvo ? Progresso.FaseSalva : Mathf.Clamp(faseInicial, 0, Fases.Todas.Length - 1));
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
        if (fase > FaseMaisLonge) partidaValida = false; // pulou fases pelo mapa: não vale recorde
        MortesNaFase = 0;
        CarregarFase(fase);
        Estado = EstadoDoJogo.Jogando;
        SalvarProgresso();
    }

    void Update()
    {
        timerRenascer -= Time.deltaTime;
        timerEntrada -= Time.unscaledDeltaTime; // tempo real: a transição funciona até com o jogo pausado
        AjustarMusica();

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
                }
                if (timerEstado <= 0f) Renascer();
                break;

            case EstadoDoJogo.FaseConcluida:
                timerEstado -= Time.deltaTime;
                if (timerEstado <= 0f) ProximaFase();
                break;

            case EstadoDoJogo.Vitoria:
                if (Controles.Confirmar()) Trocar(VoltarAoTitulo);
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
        if (!EhUltimaFase)
        {
            // volta para o mapa e o Subaru anda sozinho até a próxima fase
            int proxima = FaseAtual + 1;
            bool caminhoNovo = proxima > FaseLiberada;
            FaseMaisLonge = Mathf.Max(FaseMaisLonge, proxima);
            MostrarMapa(FaseAtual, proxima, caminhoNovo);
            Progresso.Salvar(proxima, FaseMaisLonge, Mortes, moedas, TempoTotal, partidaValida);
            return;
        }

        Estado = EstadoDoJogo.Vitoria;
        Progresso.Zerou(Fases.Todas.Length);
        if (partidaValida) Progresso.TentarSalvarRecorde(Mortes);
    }

    void Pausar(bool pausar)
    {
        Estado = pausar ? EstadoDoJogo.Pausado : EstadoDoJogo.Jogando;
        Time.timeScale = pausar ? 0f : 1f;
        AudioListener.pause = pausar; // a música de fundo ignora isso e só fica mais baixa
    }

    void SalvarProgresso() => Progresso.Salvar(FaseDoSubaru, FaseMaisLonge, Mortes, moedas, TempoTotal, partidaValida);

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
        int fase;
        if (opcao == OpcaoDoMenu.Continuar)
        {
            Progresso.Carregar(out fase, out int maisLonge, out int mortesSalvas, out moedas, out float tempoSalvo, out partidaValida);
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
    void AtualizarMapa()
    {
        if (Controles.Pausar())
        {
            SalvarProgresso();
            Trocar(VoltarAoTitulo);
            return;
        }
        if (Mapa.Andando) return;
        if (Controles.DireitaApertou() || Controles.CimaApertou()) Mapa.Mover(1);
        else if (Controles.EsquerdaApertou() || Controles.BaixoApertou()) Mapa.Mover(-1);
        else if (Controles.Confirmar())
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
        GuardarLugarDaMorte(jogador.transform.position, caiuNoBuraco);

        Estado = EstadoDoJogo.Morreu;
        duracaoDaMorte = MortesNaFase >= mortesParaEncurtar ? tempoAposMorteCurta : tempoAposMorte;
        timerEstado = duracaoDaMorte;
        Mensagem = EscolherMensagem();
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
        moedas += moedasNaFase;
        moedasNaFase = 0;
        jogador.Congelar();
        Som("vitoria");
        if (EhUltimaFase && Emilia != null) Emilia.Comemorar();
    }

    public void GanharMoeda() => moedasNaFase++;

    // Mensagem rápida no meio da tela (controles invertidos, volta pro começo...).
    public void Avisar(string texto, float duracao = 2f)
    {
        aviso = texto;
        timerAviso = duracao;
    }

    public static void Som(string nome, float volume = 1f)
    {
        if (Instancia == null || !Instancia.sons.TryGetValue(nome, out AudioClip clipe)) return;
        Instancia.fonteDeAudio.PlayOneShot(clipe, volume);
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
        Fase fase = Fases.Todas[FaseAtual];
        if (MortesNaFase == 1 && fase.mudancas != null && fase.mudancas.Length > 0)
            return "Ah, e eu mudei umas coisinhas. :)";
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
