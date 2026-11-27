using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// O CONVIDADO da Tripulação Online (a ideia geral está em Protocolo.cs).
//
// SalaRemota é a conexão com o anfitrião (sem nada de tela: o teste automático usa ela direto).
// Convidado é o que aparece: a imagem que chega cobre a tela inteira, o som que chega toca, e os botões daqui
// (teclado ou controle) vão para lá. Enquanto isso, o jogo daqui fica parado embaixo (GerenciadorDoJogo.Update).
public sealed class SalaRemota : IDisposable
{
    public enum Situacao { Conectando, Dentro, Recusado, Caiu }
    public Situacao Estado { get; private set; } = Situacao.Conectando;
    public string Motivo { get; private set; } = "";
    public string NomeDaSala { get; private set; } = "";
    public int Numero { get; private set; }
    public int PingMs { get; private set; } = -1;
    public int QuadrosRecebidos { get; private set; }
    public int PedacosDeSom { get; private set; }
    public readonly SomRecebido som = new SomRecebido();
    public string Endereco { get; }

    // O quadro mais novo que chegou (JPEG) e se ele já foi mostrado.
    public byte[] Quadro { get; private set; }
    public bool QuadroNovo { get; set; }

    volatile Conexao conexao;
    Protocolo.Botoes ultimoEnviado;
    float proximoEnvio, proximoPing, ultimaMensagem;
    static readonly System.Diagnostics.Stopwatch Relogio = System.Diagnostics.Stopwatch.StartNew();

    public SalaRemota(string host, int porta, string nome)
    {
        Endereco = host + (porta != Protocolo.PortaPadrao ? ":" + porta : "");
        ultimaMensagem = Time.unscaledTime;
        Task.Run(async () =>
        {
            var tcp = new TcpClient();
            try
            {
                Task tentativa = tcp.ConnectAsync(host, porta);
                if (await Task.WhenAny(tentativa, Task.Delay(7000)) != tentativa) throw new TimeoutException();
                await tentativa; // (se deu erro, ele aparece aqui)
                var c = new Conexao(tcp);
                c.Enviar(Protocolo.Tipo.Ola, Protocolo.Ola(nome));
                conexao = c;
            }
            catch (Exception e)
            {
                try { tcp.Close(); } catch { }
                Motivo = e is TimeoutException || (e is SocketException s && s.SocketErrorCode == SocketError.TimedOut)
                    ? Idioma.T("ninguém respondeu nesse endereço (a sala está aberta? a porta está aberta no roteador?)",
                               "nobody answered at that address (is the room open? is the port open on the router?)")
                    : e is SocketException s2 && s2.SocketErrorCode == SocketError.ConnectionRefused
                        ? Idioma.T("o computador respondeu, mas não tem sala aberta nele", "the computer answered, but there is no room open on it")
                        : e is SocketException s3 && s3.SocketErrorCode == SocketError.HostNotFound
                            ? Idioma.T("não achei esse endereço", "could not find that address")
                            : Idioma.T("não consegui conectar: ", "could not connect: ") + e.Message;
                Estado = Situacao.Caiu;
            }
        });
    }

    // Chamado uma vez por quadro (na linha principal).
    public void Atualizar()
    {
        Conexao c = conexao;
        if (c == null || Estado == Situacao.Caiu || Estado == Situacao.Recusado) return;
        int lidas = 0;
        while (lidas++ < 500 && c.Receber(out Mensagem m))
        {
            ultimaMensagem = Time.unscaledTime;
            switch (m.tipo)
            {
                case Protocolo.Tipo.BemVindo:
                    Numero = m.dados.Length > 0 ? m.dados[0] : 0;
                    NomeDaSala = Protocolo.Texto(m.dados, 1);
                    Estado = Situacao.Dentro;
                    break;
                case Protocolo.Tipo.Recusado:
                    Motivo = Protocolo.Texto(m.dados);
                    Estado = Situacao.Recusado;
                    c.Fechar(Motivo);
                    return;
                case Protocolo.Tipo.Quadro:
                    Quadro = m.dados;
                    QuadroNovo = true;
                    QuadrosRecebidos++;
                    break;
                case Protocolo.Tipo.Som:
                    som.Escrever(m.dados);
                    PedacosDeSom++;
                    break;
                case Protocolo.Tipo.Ping:
                    c.Enviar(Protocolo.Tipo.Pong, m.dados);
                    break;
                case Protocolo.Tipo.Pong:
                    PingMs = (int)Math.Max(0, Relogio.ElapsedMilliseconds - Protocolo.Numero(m.dados));
                    break;
                case Protocolo.Tipo.Tchau:
                    string porque = Protocolo.Texto(m.dados);
                    Motivo = porque.Length > 0 ? porque : Idioma.T("o anfitrião fechou a sala", "the host closed the room");
                    Estado = Situacao.Caiu;
                    c.Fechar(Motivo);
                    return;
            }
        }
        if (!c.Aberta)
        {
            Motivo = c.Motivo;
            Estado = Situacao.Caiu;
            return;
        }
        if (Time.unscaledTime - ultimaMensagem > 10f)
        {
            Motivo = Idioma.T("o anfitrião parou de responder", "the host stopped responding");
            Estado = Situacao.Caiu;
            c.Fechar(Motivo);
            return;
        }
        if (Estado == Situacao.Dentro && Time.unscaledTime >= proximoPing)
        {
            proximoPing = Time.unscaledTime + 1f;
            c.Enviar(Protocolo.Tipo.Ping, Protocolo.Numero(Relogio.ElapsedMilliseconds));
        }
    }

    // Os botões daqui: vão quando mudam (e a cada 1/4 de segundo, para o anfitrião saber que ainda estamos aqui).
    public void EnviarBotoes(Protocolo.Botoes b)
    {
        Conexao c = conexao;
        if (c == null || Estado != Situacao.Dentro) return;
        if (b.Equals(ultimoEnviado) && Time.unscaledTime < proximoEnvio) return;
        ultimoEnviado = b;
        proximoEnvio = Time.unscaledTime + 0.25f;
        c.Enviar(Protocolo.Tipo.Entrada, b.Escrever());
    }

    public void Dispose()
    {
        Conexao c = conexao;
        if (c != null) c.Despedir(Idioma.T("você saiu da sala", "you left the room"));
        if (Estado != Situacao.Recusado) Estado = Situacao.Caiu;
    }
}

// O som que chega (24 kHz, mono): um anel com uns 100 ms de folga, lido pela linha do som e esticado para a
// taxa da placa de som. Se acumular demais (a rede engasgou e depois mandou tudo de uma vez), pula para perto do agora.
public sealed class SomRecebido
{
    const int Taxa = 24000;
    readonly float[] anel = new float[Taxa * 2];
    int escrita, leitura, quantos;
    double fracao;
    bool tocando;
    const int Folga = Taxa / 10, Demais = Taxa * 2 / 5; // 100 ms para começar; mais de 400 ms acumulados: pula

    public void Escrever(byte[] mu)
    {
        lock (anel)
        {
            foreach (byte b in mu)
            {
                if (quantos == anel.Length) { leitura = (leitura + 1) % anel.Length; quantos--; }
                anel[escrita] = Protocolo.DeMuLaw(b);
                escrita = (escrita + 1) % anel.Length;
                quantos++;
            }
        }
    }

    public int Guardados { get { lock (anel) return quantos; } }

    public void Ler(float[] dados, int canais, int taxaDaSaida)
    {
        lock (anel)
        {
            if (!tocando && quantos >= Folga) tocando = true;
            if (quantos > Demais)
            {
                int pular = quantos - Folga;
                leitura = (leitura + pular) % anel.Length;
                quantos -= pular;
            }
            double passo = Taxa / (double)Math.Max(8000, taxaDaSaida);
            for (int i = 0; i + canais <= dados.Length; i += canais)
            {
                float v = 0f;
                if (tocando && quantos >= 2)
                {
                    float a = anel[leitura], b = anel[(leitura + 1) % anel.Length];
                    v = a + (b - a) * (float)fracao;
                    fracao += passo;
                    while (fracao >= 1.0 && quantos > 1)
                    {
                        fracao -= 1.0;
                        leitura = (leitura + 1) % anel.Length;
                        quantos--;
                    }
                }
                else tocando = false; // acabou o que tinha: espera juntar a folga de novo (melhor um silêncio que um chiado)
                for (int c = 0; c < canais; c++) dados[i + c] = v;
            }
        }
    }
}

public class Convidado : MonoBehaviour
{
    public static Convidado Instancia { get; private set; }
    public static bool Ativo => Instancia != null;

    SalaRemota sala;
    Texture2D imagem;
    float abriuEm, timerDeSair, segurandoSelect;
    bool perguntandoSeSai;
    readonly List<AudioSource> silenciados = new List<AudioSource>();
    float proximoSilencio;

    public SalaRemota Sala => sala;

    public static void Entrar(string host, int porta)
    {
        if (Instancia != null) Sair();
        var objeto = new GameObject("Convidado (Tripulação Online)");
        DontDestroyOnLoad(objeto);
        Instancia = objeto.AddComponent<Convidado>();
        Instancia.sala = new SalaRemota(host, porta, Online.NomeDoJogador());
        Instancia.abriuEm = Time.unscaledTime;
        Debug.Log($"[Online] entrando na sala {host}:{porta}");
    }

    public static void Sair()
    {
        if (Instancia != null) Destroy(Instancia.gameObject);
    }

    // Por que a última sala acabou (a tela Online mostra quando o jogo daqui volta).
    public static string UltimoMotivo { get; private set; } = "";

    void Awake()
    {
        imagem = new Texture2D(2, 2, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "Online: tela do anfitrião" };
        var tocador = new GameObject("Som do anfitrião");
        tocador.transform.SetParent(transform, false);
        var fonte = tocador.AddComponent<AudioSource>();
        // (um som "de mentira", tudo 1, em laço: o TocadorDoSom troca pelo som que chega. Assim a fonte nunca para.)
        var umSegundo = AudioClip.Create("som da rede", AudioSettings.outputSampleRate, 1, AudioSettings.outputSampleRate, false);
        var uns = new float[umSegundo.samples];
        for (int i = 0; i < uns.Length; i++) uns[i] = 1f;
        umSegundo.SetData(uns, 0);
        fonte.clip = umSegundo;
        fonte.loop = true;
        fonte.spatialBlend = 0f;
        fonte.ignoreListenerPause = true;
        fonte.bypassListenerEffects = true;
        tocador.AddComponent<TocadorDoSom>().dono = this;
        fonte.Play();
    }

    void OnDestroy()
    {
        if (sala != null)
        {
            if (sala.Estado != SalaRemota.Situacao.Dentro && sala.Motivo != "") UltimoMotivo = sala.Motivo;
            sala.Dispose();
        }
        foreach (AudioSource s in silenciados) if (s != null) s.mute = false;
        silenciados.Clear();
        if (imagem != null) Destroy(imagem);
        if (Instancia == this) Instancia = null;
        Debug.Log("[Online] saí da sala");
    }

    void Update()
    {
        sala.Atualizar();
        if (sala.QuadroNovo)
        {
            sala.QuadroNovo = false;
            imagem.LoadImage(sala.Quadro, false);
        }

        // o som do jogo daqui (o título, a música) fica mudo: só vale o que vem do anfitrião
        if (Time.unscaledTime >= proximoSilencio)
        {
            proximoSilencio = Time.unscaledTime + 0.5f;
            foreach (AudioSource s in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (s.transform.IsChildOf(transform) || s.mute) continue;
                s.mute = true;
                silenciados.Add(s);
            }
        }

        if (sala.Estado == SalaRemota.Situacao.Caiu || sala.Estado == SalaRemota.Situacao.Recusado)
        {
            // fica uns segundos mostrando por quê, e volta para o jogo daqui (ENTER volta na hora)
            timerDeSair += Time.unscaledDeltaTime;
            Keyboard t = Keyboard.current;
            if (timerDeSair > 6f || (t != null && (t.enterKey.wasPressedThisFrame || t.escapeKey.wasPressedThisFrame))
                || (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame)))
            {
                UltimoMotivo = sala.Motivo;
                Destroy(gameObject);
            }
            return;
        }

        // sair: ESC (pergunta antes) ou segurar SELECT por 2 s no controle
        Keyboard teclado = Keyboard.current;
        Gamepad controle = Gamepad.current;
        if (perguntandoSeSai)
        {
            if ((teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.sKey.wasPressedThisFrame || teclado.yKey.wasPressedThisFrame))
                || (controle != null && controle.buttonSouth.wasPressedThisFrame))
            {
                UltimoMotivo = Idioma.T("você saiu da sala", "you left the room");
                Destroy(gameObject);
                return;
            }
            if ((teclado != null && (teclado.escapeKey.wasPressedThisFrame || teclado.nKey.wasPressedThisFrame))
                || (controle != null && controle.buttonEast.wasPressedThisFrame))
                perguntandoSeSai = false;
            sala.EnviarBotoes(default); // (enquanto pergunta, o Grumete daqui fica parado lá)
            return;
        }
        if (teclado != null && teclado.escapeKey.wasPressedThisFrame) perguntandoSeSai = true;
        segurandoSelect = controle != null && controle.selectButton.isPressed ? segurandoSelect + Time.unscaledDeltaTime : 0f;
        if (segurandoSelect > 2f) { perguntandoSeSai = true; segurandoSelect = 0f; }

        sala.EnviarBotoes(Application.isFocused ? LerBotoes() : default);
    }

    // O teclado e o controle daqui viram os botões de UM controle lá (o anfitrião não sabe a diferença).
    // No teclado valem as teclas de Opções > Teclas (as do "teclado 1") e as setas.
    static Protocolo.Botoes LerBotoes()
    {
        uint bits = 0;
        void Por(GamepadButton botao, bool apertado) { if (apertado) bits |= 1u << (int)botao; }

        Keyboard t = Keyboard.current;
        if (t != null)
        {
            Por(GamepadButton.DpadLeft, Teclas.Segurando(Teclas.Acao.Esquerda) || t.leftArrowKey.isPressed);
            Por(GamepadButton.DpadRight, Teclas.Segurando(Teclas.Acao.Direita) || t.rightArrowKey.isPressed);
            Por(GamepadButton.DpadDown, Teclas.Segurando(Teclas.Acao.Baixo) || t.downArrowKey.isPressed);
            bool pulo = Teclas.Segurando(Teclas.Acao.Pular) || Teclas.Segurando(Teclas.Acao.PularDeNovo) || t.upArrowKey.isPressed;
            Por(GamepadButton.South, pulo);
            Por(GamepadButton.DpadUp, t.upArrowKey.isPressed || Teclas.Segurando(Teclas.Acao.Pular));
            Por(GamepadButton.West, Teclas.Segurando(Teclas.Acao.Empurrar) || t.rightShiftKey.isPressed);
            Por(GamepadButton.East, Teclas.Segurando(Teclas.Acao.Balao) || t.rightCtrlKey.isPressed);
            Por(GamepadButton.North, Teclas.Segurando(Teclas.Acao.Coop));
            Por(GamepadButton.Start, t.enterKey.isPressed || t.numpadEnterKey.isPressed);
            Por(GamepadButton.RightShoulder, t.tabKey.isPressed);
        }

        sbyte x = 0, y = 0;
        Gamepad c = Gamepad.current;
        if (c != null)
        {
            Por(GamepadButton.DpadUp, c.dpad.up.isPressed);
            Por(GamepadButton.DpadDown, c.dpad.down.isPressed);
            Por(GamepadButton.DpadLeft, c.dpad.left.isPressed);
            Por(GamepadButton.DpadRight, c.dpad.right.isPressed);
            Por(GamepadButton.North, c.buttonNorth.isPressed);
            Por(GamepadButton.East, c.buttonEast.isPressed);
            Por(GamepadButton.South, c.buttonSouth.isPressed);
            Por(GamepadButton.West, c.buttonWest.isPressed);
            Por(GamepadButton.LeftShoulder, c.leftShoulder.isPressed);
            Por(GamepadButton.RightShoulder, c.rightShoulder.isPressed);
            Por(GamepadButton.Start, c.startButton.isPressed);
            Por(GamepadButton.Select, c.selectButton.isPressed);
            Vector2 a = c.leftStick.ReadValue();
            x = (sbyte)Mathf.RoundToInt(Mathf.Clamp(a.x, -1f, 1f) * 127f);
            y = (sbyte)Mathf.RoundToInt(Mathf.Clamp(a.y, -1f, 1f) * 127f);
        }
        return new Protocolo.Botoes { bits = bits, x = x, y = y };
    }

    // ------------------------------------------------------------------ a tela

    static Texture2D preto;
    static readonly Color32 Branco = new Color32(255, 255, 255, 255), Escuro = new Color32(20, 20, 28, 255),
        Amarelo = new Color32(255, 217, 51, 255), Ciano = new Color32(140, 240, 255, 255), Cinza = new Color32(200, 190, 220, 255),
        Vermelho = new Color32(255, 102, 102, 255);

    void OnGUI()
    {
        GUI.depth = -1000; // por cima de tudo (a Interface do jogo daqui continua desenhando embaixo)
        if (Event.current.type != EventType.Repaint) return;
        float w = Screen.width, h = Screen.height, escala = h / 720f;
        if (preto == null)
        {
            preto = new Texture2D(1, 1);
            preto.SetPixel(0, 0, Color.black);
            preto.Apply();
        }
        GUI.DrawTexture(new Rect(0, 0, w, h), preto);

        bool temImagem = sala.QuadrosRecebidos > 0 && imagem.width > 2;
        if (temImagem) GUI.DrawTexture(new Rect(0, 0, w, h), imagem, ScaleMode.ScaleToFit);

        switch (sala.Estado)
        {
            case SalaRemota.Situacao.Conectando:
                Escrever(Idioma.T("ENTRANDO NA SALA...", "JOINING THE ROOM..."), w / 2f, h * 0.42f, 4f, 0.5f, Amarelo);
                Escrever(sala.Endereco, w / 2f, h * 0.52f, 2.2f, 0.5f, Cinza);
                Escrever(Idioma.T("ESC: desistir", "ESC: give up"), w / 2f, h * 0.9f, 1.8f, 0.5f, Cinza);
                break;
            case SalaRemota.Situacao.Caiu:
            case SalaRemota.Situacao.Recusado:
                GUI.color = new Color(1f, 1f, 1f, 0.75f);
                GUI.DrawTexture(new Rect(0, 0, w, h), preto);
                GUI.color = Color.white;
                Escrever(sala.Estado == SalaRemota.Situacao.Recusado ? Idioma.T("A SALA NÃO DEIXOU ENTRAR", "THE ROOM DID NOT LET YOU IN")
                                                                    : Idioma.T("FIM DA SALA ONLINE", "ONLINE ROOM ENDED"), w / 2f, h * 0.38f, 4f, 0.5f, Vermelho);
                Escrever(sala.Motivo, w / 2f, h * 0.5f, 2f, 0.5f, Branco, w * 0.9f);
                Escrever(Idioma.T("ENTER: voltar", "ENTER: back"), w / 2f, h * 0.9f, 1.8f, 0.5f, Cinza);
                break;
            case SalaRemota.Situacao.Dentro:
                if (!temImagem) Escrever(Idioma.T("ESPERANDO A IMAGEM DO ANFITRIÃO...", "WAITING FOR THE PICTURE FROM THE HOST..."), w / 2f, h * 0.45f, 3f, 0.5f, Amarelo);
                // nos primeiros 10 s: como entrar na tripulação e quais são as teclas
                if (Time.unscaledTime - abriuEm < 12f || perguntandoSeSai)
                {
                    float y = h * 0.04f;
                    Escrever(Idioma.T($"NA SALA DE {sala.NomeDaSala.ToUpperInvariant()}", $"IN THE ROOM OF {sala.NomeDaSala.ToUpperInvariant()}"), w / 2f, y, 2.6f, 0.5f, Ciano, w * 0.9f);
                    Escrever(Idioma.T("Para jogar: o anfitrião abre a TRIPULAÇÃO (T no mapa) e você aperta PULAR.",
                                      "To play: the host opens the CREW screen (T on the map) and you press JUMP."), w / 2f, y + 30f * escala, 1.7f, 0.5f, Branco, w * 0.95f);
                    Escrever(Idioma.T("Teclado: A/D andar  W/ESPAÇO pular  S porta  E empurrar  Q balão  ENTER start  ESC sair",
                                      "Keyboard: A/D move  W/SPACE jump  S door  E push  Q balloon  ENTER start  ESC leave"), w / 2f, y + 52f * escala, 1.5f, 0.5f, Cinza, w * 0.95f);
                }
                if (sala.PingMs >= 0)
                {
                    Color32 cor = sala.PingMs < 80 ? new Color32(128, 255, 128, 255) : sala.PingMs < 160 ? Amarelo : Vermelho;
                    Escrever($"PING {sala.PingMs} MS", w - 12f * escala, h - 26f * escala, 1.5f, 1f, cor);
                }
                break;
        }

        if (perguntandoSeSai)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.7f);
            GUI.DrawTexture(new Rect(0, h * 0.4f, w, h * 0.2f), preto);
            GUI.color = Color.white;
            Escrever(Idioma.T("SAIR DA SALA?", "LEAVE THE ROOM?"), w / 2f, h * 0.43f, 3.5f, 0.5f, Amarelo);
            Escrever(Idioma.T("ENTER/A: sair    ESC/B: continuar", "ENTER/A: leave    ESC/B: stay"), w / 2f, h * 0.52f, 2f, 0.5f, Branco);
        }
    }

    static void Escrever(string texto, float x, float y, float pixel, float alinhamento, Color32 cor, float larguraMaxima = 0f)
    {
        if (string.IsNullOrEmpty(texto)) return;
        Texture2D t = FabricaDeSprites.TextoEmPixel(texto, cor, Escuro, Escuro, comAcentos: true, maiusculas: true);
        float tamanho = pixel * Screen.height / 720f;
        float largura = t.width * tamanho;
        if (larguraMaxima > 0f && largura > larguraMaxima)
        {
            tamanho *= larguraMaxima / largura;
            largura = larguraMaxima;
        }
        GUI.DrawTexture(new Rect(x - largura * alinhamento, y, largura, t.height * tamanho), t);
    }

    // Só o TocadorDoSom (outra linha) lê daqui.
    public SomRecebido Som => sala?.som;
}

// Na fonte de som do convidado: troca o "som de mentira" pelo que chega do anfitrião.
public class TocadorDoSom : MonoBehaviour
{
    public Convidado dono;
    int taxa;
    SomRecebido som;

    void Start() => taxa = AudioSettings.outputSampleRate;
    void Update() => som = dono != null ? dono.Som : null;

    void OnAudioFilterRead(float[] dados, int canais)
    {
        SomRecebido s = som;
        if (s == null || taxa <= 0) { Array.Clear(dados, 0, dados.Length); return; }
        s.Ler(dados, canais, taxa);
    }
}
