using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;

// O ANFITRIÃO da Tripulação Online (veja Protocolo.cs para a ideia geral).
//
// Abre a sala (um TcpListener), aceita até 3 convidados e, para cada um, liga um CONTROLE VIRTUAL no Input System.
// Daí em diante o jogo não sabe que é online: o convidado entra na tripulação na tela T do mapa, apertando PULAR,
// igual a quem liga um controle de verdade. Quando ele sai (ou a conexão cai), o controle é desligado, e a tripulação
// já trata isso (é como desligar o cabo do controle).
//
// A cada quadro (no máximo 30 por segundo) a tela inteira (o jogo e a interface) é copiada, encolhida, lida da placa
// de vídeo sem travar (AsyncGPUReadback), comprimida em JPEG numa linha separada e mandada a todos.
// O som sai do AudioListener (OuvidoDaRede).
public class Anfitriao : MonoBehaviour
{
    public static Anfitriao Instancia { get; private set; }
    public static bool Aberto => Instancia != null;

    // A qualidade da transmissão (Opções da sala): a altura da imagem e a qualidade do JPEG.
    // 360p a 30 quadros dá uns 4 a 8 Mbit/s por convidado, dependendo da fase.
    public enum Qualidade { Leve, Normal, Alta }
    public static Qualidade QualidadeEscolhida = Qualidade.Normal;
    static int AlturaDaImagem => QualidadeEscolhida == Qualidade.Leve ? 270 : QualidadeEscolhida == Qualidade.Alta ? 540 : 360;
    static int QualidadeDoJpeg => QualidadeEscolhida == Qualidade.Leve ? 55 : QualidadeEscolhida == Qualidade.Alta ? 75 : 65;
    const float QuadrosPorSegundo = 30f;

    public class Convidado
    {
        public Conexao conexao;
        public Gamepad controle;
        public string nome = "";
        public int numero;            // 1, 2 ou 3 (a ordem em que entrou na sala)
        public int pingMs = -1;
        public bool disseOla;
        public float chegouEm, ultimaMensagem;
        public Protocolo.Botoes atual;
        public readonly Queue<Protocolo.Botoes> fila = new Queue<Protocolo.Botoes>();
    }

    readonly List<Convidado> convidados = new List<Convidado>();
    public IReadOnlyList<Convidado> Convidados => convidados;
    // Para o som (que sai de outra linha): a lista de conexões de agora, trocada inteira quando muda.
    static volatile Conexao[] paraOSom = Array.Empty<Conexao>();
    readonly ConcurrentQueue<TcpClient> chegando = new ConcurrentQueue<TcpClient>();

    TcpListener ouvinte;
    AnuncioNaRede anuncio;
    public PortaNoRoteador Roteador { get; private set; }
    public int Porta { get; private set; }
    public string NomeDaSala { get; private set; } = "";
    public IPAddress IpLocal { get; private set; } = IPAddress.Loopback;
    public string Erro { get; private set; } = "";
    public int QuadrosEnviados { get; private set; }
    volatile bool fechando;
    bool rodavaEmSegundoPlano;

    // O código que o anfitrião passa para os amigos: o do IP público (se o UPnP abriu a porta) ou o da rede local.
    public string Codigo
    {
        get
        {
            if (Roteador != null && Roteador.Estado == PortaNoRoteador.Situacao.Aberta && Roteador.IpPublico != null
                && !Rede.Privado(Roteador.IpPublico) && !Rede.Cgnat(Roteador.IpPublico))
                return CodigoDaSala.Gerar(Roteador.IpPublico, Porta);
            return CodigoDaSala.Gerar(IpLocal, Porta);
        }
    }

    // Dá para entrar pela internet (e não só pela rede de casa)?
    public bool AbertaNaInternet => Roteador != null && Roteador.Estado == PortaNoRoteador.Situacao.Aberta
                                    && Roteador.IpPublico != null && !Rede.Privado(Roteador.IpPublico) && !Rede.Cgnat(Roteador.IpPublico);
    public bool TemCgnat => Roteador != null && Roteador.IpPublico != null && Rede.Cgnat(Roteador.IpPublico);

    // ------------------------------------------------------------------ abrir e fechar

    // Abre a sala. upnp: pedir ao roteador para abrir a porta; anunciar: aparecer na lista da rede local.
    public static bool Abrir(int porta = Protocolo.PortaPadrao, bool upnp = true, bool anunciar = true)
    {
        if (Instancia != null) return true;
        var objeto = new GameObject("Anfitriao (Tripulação Online)");
        DontDestroyOnLoad(objeto);
        var anfitriao = objeto.AddComponent<Anfitriao>();
        if (!anfitriao.Comecar(porta, upnp, anunciar))
        {
            ultimoErro = anfitriao.Erro;
            Destroy(objeto);
            Instancia = null;
            return false;
        }
        return true;
    }

    // Por que a última tentativa de abrir a sala falhou (a porta já está em uso, sem rede...).
    static string ultimoErro = "";
    public static string UltimoErro => ultimoErro;

    public static void Fechar()
    {
        if (Instancia != null) Destroy(Instancia.gameObject);
    }

    bool Comecar(int porta, bool upnp, bool anunciar)
    {
        Instancia = this;
        Porta = porta;
        NomeDaSala = Online.NomeDoJogador();
        try
        {
            ouvinte = new TcpListener(IPAddress.Any, porta);
            ouvinte.Start();
        }
        catch (Exception e)
        {
            Erro = e is SocketException s && s.SocketErrorCode == SocketError.AddressAlreadyInUse
                ? Idioma.T($"a porta {porta} já está em uso (outra sala aberta?)", $"port {porta} is already in use (another room open?)")
                : e.Message;
            return false;
        }
        new Thread(Aceitar) { IsBackground = true, Name = "Cilada sala (aceitar)" }.Start();
        IpLocal = Rede.IpLocal();
        if (upnp) Roteador = new PortaNoRoteador(porta);
        if (anunciar) anuncio = new AnuncioNaRede(porta, NomeDaSala);

        // a sala não pode parar quando a janela perde o foco (o anfitrião foi ver o zap)
        rodavaEmSegundoPlano = Application.runInBackground;
        Application.runInBackground = true;
        StartCoroutine(Filmar());
        Debug.Log($"[Online] sala aberta na porta {porta} (rede local: {IpLocal})");
        return true;
    }

    void Aceitar()
    {
        while (!fechando)
        {
            try { chegando.Enqueue(ouvinte.AcceptTcpClient()); }
            catch { if (fechando) return; Thread.Sleep(100); }
        }
    }

    void OnDestroy()
    {
        fechando = true;
        foreach (Convidado c in convidados)
        {
            c.conexao.Despedir("a sala fechou", Protocolo.Tipo.Tchau, Protocolo.Texto(Idioma.T("o anfitrião fechou a sala", "the host closed the room")));
            Desligar(c);
        }
        convidados.Clear();
        paraOSom = Array.Empty<Conexao>();
        OuvidoDaRede.Ligado = false;
        try { ouvinte?.Stop(); } catch { }
        anuncio?.Dispose();
        Roteador?.Fechar();
        while (chegando.TryDequeue(out TcpClient t)) { try { t.Close(); } catch { } }
        if (telaCheia != null) telaCheia.Release();
        if (imagemPequena != null) imagemPequena.Release();
        Application.runInBackground = rodavaEmSegundoPlano;
        if (Instancia == this) Instancia = null;
        Debug.Log("[Online] sala fechada");
    }

    // ------------------------------------------------------------------ a cada quadro

    void Update()
    {
        // quem acabou de chegar: vira um convidado "esperando o Olá"
        while (chegando.TryDequeue(out TcpClient tcp))
        {
            var c = new Convidado { conexao = new Conexao(tcp), chegouEm = Time.unscaledTime, ultimaMensagem = Time.unscaledTime };
            convidados.Add(c);
        }

        for (int i = convidados.Count - 1; i >= 0; i--)
        {
            Convidado c = convidados[i];
            LerMensagens(c);
            float agora = Time.unscaledTime;
            if (!c.disseOla && agora - c.chegouEm > 5f) c.conexao.Fechar("não disse olá");
            if (agora - c.ultimaMensagem > 10f) c.conexao.Fechar(Idioma.T("parou de responder", "stopped responding"));
            if (!c.conexao.Aberta)
            {
                if (c.disseOla) Debug.Log($"[Online] {c.nome} saiu ({c.conexao.Motivo})");
                Desligar(c);
                convidados.RemoveAt(i);
                AtualizarListaDoSom();
                continue;
            }
            if (c.disseOla) AplicarBotoes(c);
        }

        // de segundo em segundo: o ping de cada um
        if (Time.unscaledTime >= proximoPing)
        {
            proximoPing = Time.unscaledTime + 1f;
            foreach (Convidado c in convidados)
                if (c.disseOla) c.conexao.Enviar(Protocolo.Tipo.Ping, Protocolo.Numero(Relogio.ElapsedMilliseconds));
        }
        OuvidoDaRede.Ligado = paraOSom.Length > 0;
        if (OuvidoDaRede.Ligado) GarantirOuvido();
    }

    float proximoPing;
    static readonly System.Diagnostics.Stopwatch Relogio = System.Diagnostics.Stopwatch.StartNew();

    void LerMensagens(Convidado c)
    {
        int lidas = 0;
        while (lidas++ < 400 && c.conexao.Receber(out Mensagem m))
        {
            c.ultimaMensagem = Time.unscaledTime;
            switch (m.tipo)
            {
                case Protocolo.Tipo.Ola:
                    if (c.disseOla) break;
                    ReceberOla(c, m.dados);
                    break;
                case Protocolo.Tipo.Entrada:
                    if (c.disseOla && Protocolo.Botoes.Ler(m.dados, out Protocolo.Botoes b))
                    {
                        if (c.fila.Count > 60) c.fila.Clear(); // atrasou demais: vale só o que ele aperta agora
                        c.fila.Enqueue(b);
                    }
                    break;
                case Protocolo.Tipo.Ping:
                    c.conexao.Enviar(Protocolo.Tipo.Pong, m.dados);
                    break;
                case Protocolo.Tipo.Pong:
                    c.pingMs = (int)Math.Max(0, Relogio.ElapsedMilliseconds - Protocolo.Numero(m.dados));
                    break;
                case Protocolo.Tipo.Tchau:
                    c.conexao.Fechar(Idioma.T("saiu da sala", "left the room"));
                    break;
            }
        }
    }

    void ReceberOla(Convidado c, byte[] dados)
    {
        int versao = dados != null && dados.Length >= 4 ? BitConverter.ToInt32(dados, 0) : -1;
        string nome = Protocolo.Texto(dados, 4).Trim();
        if (nome.Length == 0) nome = "Grumete";
        if (nome.Length > 20) nome = nome.Substring(0, 20);
        if (versao != Protocolo.Versao)
        {
            Recusar(c, Idioma.T($"versões diferentes do jogo (a sala é a {Protocolo.Versao}, a sua é a {versao}): atualizem os dois",
                                $"different game versions (room {Protocolo.Versao}, yours {versao}): update both"));
            return;
        }
        int dentro = 0;
        foreach (Convidado outro in convidados) if (outro.disseOla) dentro++;
        if (dentro >= Protocolo.MaximoDeConvidados)
        {
            Recusar(c, Idioma.T("a sala está cheia (a tripulação é de 4)", "the room is full (the crew has 4)"));
            return;
        }

        c.disseOla = true;
        c.nome = nome;
        c.numero = ProximoNumero();
        c.controle = InputSystem.AddDevice<Gamepad>("Online" + c.numero);
        Online.MarcarRemoto(c.controle, c.nome);
        byte[] texto = Protocolo.Texto(NomeDaSala);
        var resposta = new byte[1 + texto.Length];
        resposta[0] = (byte)c.numero;
        texto.CopyTo(resposta, 1);
        c.conexao.Enviar(Protocolo.Tipo.BemVindo, resposta);
        AtualizarListaDoSom();
        Debug.Log($"[Online] {c.nome} entrou na sala ({c.conexao.Endereco}) como o controle {c.controle.name}");
    }

    void Recusar(Convidado c, string motivo)
    {
        c.disseOla = false;
        c.conexao.Despedir(motivo, Protocolo.Tipo.Recusado, Protocolo.Texto(motivo));
        Debug.Log("[Online] recusei uma conexão: " + motivo);
    }

    int ProximoNumero()
    {
        for (int n = 1; ; n++)
        {
            bool usado = false;
            foreach (Convidado c in convidados) if (c.disseOla && c.numero == n) usado = true;
            if (!usado) return n;
        }
    }

    void Desligar(Convidado c)
    {
        if (c.controle == null) return;
        Online.DesmarcarRemoto(c.controle);
        if (c.controle.added) InputSystem.RemoveDevice(c.controle);
        c.controle = null;
    }

    void AtualizarListaDoSom()
    {
        var lista = new List<Conexao>();
        foreach (Convidado c in convidados) if (c.disseOla && c.conexao.Aberta) lista.Add(c.conexao);
        paraOSom = lista.ToArray();
    }

    // Os botões que chegaram vão para o controle virtual. Um cuidado: se um botão foi apertado E solto entre dois
    // quadros do jogo, o "soltar" fica para o quadro seguinte (senão o jogo nunca veria o aperto: um pulo perdido).
    void AplicarBotoes(Convidado c)
    {
        if (c.fila.Count == 0 || c.controle == null || !c.controle.added) return;
        uint apertadosAgora = 0;
        bool mudou = false;
        while (c.fila.Count > 0)
        {
            Protocolo.Botoes b = c.fila.Peek();
            uint soltos = c.atual.bits & ~b.bits;
            if ((soltos & apertadosAgora) != 0) break;
            apertadosAgora |= b.bits & ~c.atual.bits;
            c.atual = b;
            c.fila.Dequeue();
            mudou = true;
        }
        if (!mudou) return;
        var estado = new GamepadState { buttons = c.atual.bits, leftStick = new Vector2(c.atual.x / 127f, c.atual.y / 127f) };
        InputSystem.QueueStateEvent(c.controle, estado);
    }

    // Expulsar da sala (na tela Online do anfitrião).
    public void Expulsar(Convidado c)
    {
        c.conexao.Despedir("expulso", Protocolo.Tipo.Tchau, Protocolo.Texto(Idioma.T("o anfitrião tirou você da sala", "the host removed you from the room")));
    }

    // ------------------------------------------------------------------ a imagem

    RenderTexture telaCheia, imagemPequena;
    bool lendo;
    int codificando;
    float proximoQuadro;

    IEnumerator Filmar()
    {
        var fimDoQuadro = new WaitForEndOfFrame();
        while (true)
        {
            yield return fimDoQuadro;
            if (paraOSom.Length == 0 || lendo || Volatile.Read(ref codificando) > 0 || Time.unscaledTime < proximoQuadro) continue;
            bool alguemLivre = false;
            foreach (Conexao c in paraOSom) if (!c.QuadroNaFila) alguemLivre = true;
            if (!alguemLivre) continue; // todo mundo ainda recebendo o anterior: não adianta fazer outro
            proximoQuadro = Time.unscaledTime + 1f / QuadrosPorSegundo;
            try { CopiarATela(); }
            catch (Exception e)
            {
                lendo = false;
                Debug.LogWarning("[Online] não consegui copiar a tela: " + e.Message);
                proximoQuadro = Time.unscaledTime + 2f;
            }
        }
    }

    void CopiarATela()
    {
        int w = Screen.width, h = Screen.height;
        if (w < 16 || h < 16) return;
        if (telaCheia == null || telaCheia.width != w || telaCheia.height != h)
        {
            if (telaCheia != null) telaCheia.Release();
            telaCheia = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { name = "Online: tela" };
        }
        int alto = Mathf.Min(AlturaDaImagem, h);
        int largo = Mathf.Max(16, Mathf.RoundToInt(alto * (float)w / h) & ~1);
        if (imagemPequena == null || imagemPequena.width != largo || imagemPequena.height != alto)
        {
            if (imagemPequena != null) imagemPequena.Release();
            imagemPequena = new RenderTexture(largo, alto, 0, RenderTextureFormat.ARGB32) { name = "Online: transmissão", filterMode = FilterMode.Bilinear };
        }
        ScreenCapture.CaptureScreenshotIntoRenderTexture(telaCheia);
        // (no DirectX a cópia da tela vem de cabeça para baixo)
        if (SystemInfo.graphicsUVStartsAtTop) Graphics.Blit(telaCheia, imagemPequena, new Vector2(1f, -1f), new Vector2(0f, 1f));
        else Graphics.Blit(telaCheia, imagemPequena);
        lendo = true;
        AsyncGPUReadback.Request(imagemPequena, 0, TextureFormat.RGBA32, Lido);
    }

    void Lido(AsyncGPUReadbackRequest pedido)
    {
        lendo = false;
        if (fechando || pedido.hasError) return;
        byte[] pixels = pedido.GetData<byte>().ToArray();
        uint w = (uint)pedido.width, h = (uint)pedido.height;
        int qualidade = QualidadeDoJpeg;
        Interlocked.Increment(ref codificando);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                byte[] jpeg = ImageConversion.EncodeArrayToJPG(pixels, GraphicsFormat.R8G8B8A8_UNorm, w, h, 0, qualidade);
                foreach (Conexao c in paraOSom) c.EnviarQuadro(jpeg);
                Interlocked.Increment(ref quadrosCodificados);
            }
            catch (Exception e) { erroAoCodificar = e.Message; }
            finally { Interlocked.Decrement(ref codificando); }
        });
    }

    int quadrosCodificados;
    string erroAoCodificar;
    public int QuadrosCodificados => Volatile.Read(ref quadrosCodificados);

    // ------------------------------------------------------------------ o som

    // Chamado pela linha do som (OuvidoDaRede): manda um pedaço para todos.
    public static void EnviarSom(byte[] pedaco)
    {
        foreach (Conexao c in paraOSom) c.Enviar(Protocolo.Tipo.Som, pedaco);
    }

    void GarantirOuvido()
    {
        if (ouvido != null && ouvido.isActiveAndEnabled) return;
        AudioListener ouvinteDoJogo = FindAnyObjectByType<AudioListener>();
        if (ouvinteDoJogo == null) return;
        ouvido = ouvinteDoJogo.GetComponent<OuvidoDaRede>();
        if (ouvido == null) ouvido = ouvinteDoJogo.gameObject.AddComponent<OuvidoDaRede>();
    }

    OuvidoDaRede ouvido;

    // ------------------------------------------------------------------ o aviso no canto da tela

    void OnGUI()
    {
        if (erroAoCodificar != null)
        {
            Debug.LogWarning("[Online] erro ao comprimir a imagem: " + erroAoCodificar);
            erroAoCodificar = null;
        }
        if (Event.current.type != EventType.Repaint) return;
        int dentro = paraOSom.Length;
        GUI.depth = -50;
        float escala = Screen.height / 720f;
        string texto = dentro == 0 ? Idioma.T("SALA ONLINE ABERTA", "ONLINE ROOM OPEN") : Idioma.T($"ONLINE: {dentro} NA SALA", $"ONLINE: {dentro} IN THE ROOM");
        Texture2D t = FabricaDeSprites.TextoEmPixel(texto, new Color32(140, 240, 255, 255), new Color32(20, 20, 28, 255), new Color32(20, 20, 28, 255), true, true);
        float tamanho = 1.6f * escala;
        GUI.DrawTexture(new Rect(Screen.width - t.width * tamanho - 12f * escala, Screen.height - t.height * tamanho - 10f * escala, t.width * tamanho, t.height * tamanho), t);
    }
}

// Fica no mesmo objeto do AudioListener: recebe o som final do jogo (já misturado), encolhe para mono a 24 kHz,
// comprime em lei mu e manda em pedaços de 20 ms. Roda na linha do som (não pode usar quase nada do Unity aqui).
public class OuvidoDaRede : MonoBehaviour
{
    public static volatile bool Ligado;
    const int Taxa = 24000, Pedaco = 480; // 20 ms
    double passo, acumulado;
    float soma;
    int contagem, escritos;
    byte[] pedaco = new byte[Pedaco];

    void Awake() => passo = AudioSettings.outputSampleRate / (double)Taxa;
    void OnEnable() => AudioSettings.OnAudioConfigurationChanged += Reconfigurar;
    void OnDisable() => AudioSettings.OnAudioConfigurationChanged -= Reconfigurar;
    void Reconfigurar(bool mudouOAparelho) => passo = AudioSettings.outputSampleRate / (double)Taxa;

    void OnAudioFilterRead(float[] dados, int canais)
    {
        if (!Ligado || canais <= 0 || passo <= 0) return;
        for (int i = 0; i + canais <= dados.Length; i += canais)
        {
            float mono = 0f;
            for (int c = 0; c < canais; c++) mono += dados[i + c];
            soma += mono / canais;
            contagem++;
            acumulado += 1.0;
            if (acumulado < passo) continue;
            acumulado -= passo;
            pedaco[escritos++] = Protocolo.MuLaw(soma / contagem);
            soma = 0f;
            contagem = 0;
            if (escritos < Pedaco) continue;
            Anfitriao.EnviarSom(pedaco);
            pedaco = new byte[Pedaco]; // (o pedaço que saiu ainda está na fila de cada conexão: não dá para reaproveitar)
            escritos = 0;
        }
    }
}
