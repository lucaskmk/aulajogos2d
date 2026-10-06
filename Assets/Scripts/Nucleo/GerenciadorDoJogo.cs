using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// O "cérebro" do jogo: carrega as fases, conta mortes e moedas,
// controla as telas (título, morte, fase concluída, vitória) e desenha o HUD.
//
// Ele se cria SOZINHO quando você aperta Play em qualquer cena
// (veja IniciarAutomaticamente), então não precisa arrastar nada para a cena.
public class GerenciadorDoJogo : MonoBehaviour
{
    public static GerenciadorDoJogo Instancia { get; private set; }
    public static Jogador JogadorAtual => Instancia != null ? Instancia.jogador : null;

    enum Estado { Titulo, Jogando, Morreu, FaseConcluida, Vitoria }

    [Header("Configuração")]
    public Color corDoCeu = new Color32(107, 140, 255, 255);
    public float tempoAposMorte = 1.3f;
    public float tempoAposConcluir = 2.2f;
    [Tooltip("Fase inicial (0 = primeira). Útil para testar uma fase específica.")]
    public int faseInicial = 0;

    Estado estado;
    int faseAtual;
    int mortes, mortesNaFase;
    int moedas, moedasNaFase;
    float tempoTotal;
    float timerEstado;
    string mensagem = "";
    string aviso = "";
    float timerAviso;

    public Vector3 PosicaoInicial { get; private set; }
    List<Transform> bandeiras = new List<Transform>();

    Transform raizDaFase;
    Jogador jogador;
    CameraSeguir cameraSeguir;
    AudioSource fonteDeAudio;
    Dictionary<string, AudioClip> sons;

    public readonly List<Placa> placas = new List<Placa>();

    const string ChaveRecorde = "cilada_recorde_mortes";

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

        VoltarAoTitulo();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

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
        cam.backgroundColor = corDoCeu;
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

        faseAtual = indice;
        moedasNaFase = 0;

        raizDaFase = new GameObject("Fase " + (indice + 1)).transform;
        InfoFase info = ConstrutorDeFase.Construir(Fases.Todas[indice], raizDaFase, mortesNaFase);

        PosicaoInicial = info.inicio;
        bandeiras = info.bandeiras;
        jogador = ConstrutorDeFase.Criar<Jogador>("Jogador", raizDaFase, info.inicio);
        jogador.inversores = info.inversores;
        aviso = "";
        cameraSeguir.Configurar(jogador.transform, info.largura, info.altura);
    }

    void VoltarAoTitulo()
    {
        mortes = 0;
        mortesNaFase = 0;
        moedas = 0;
        tempoTotal = 0f;
        faseAtual = Mathf.Clamp(faseInicial, 0, Fases.Todas.Length - 1);
        CarregarFase(faseAtual);
        jogador.Congelar();
        estado = Estado.Titulo;
    }

    void Update()
    {
        switch (estado)
        {
            case Estado.Titulo:
                if (Controles.Confirmar())
                {
                    estado = Estado.Jogando;
                    jogador.Liberar();
                }
                break;

            case Estado.Jogando:
                tempoTotal += Time.deltaTime;
                timerAviso -= Time.deltaTime;
                if (Controles.Reiniciar()) jogador.Morrer(); // reiniciar conta como morte. Óbvio.
                if (Controles.Voltar()) VoltarAoTitulo();
                break;

            case Estado.Morreu:
                tempoTotal += Time.deltaTime;
                timerEstado -= Time.deltaTime;
                if (timerEstado <= 0f)
                {
                    CarregarFase(faseAtual);
                    estado = Estado.Jogando;
                }
                break;

            case Estado.FaseConcluida:
                timerEstado -= Time.deltaTime;
                if (timerEstado <= 0f)
                {
                    if (faseAtual + 1 < Fases.Todas.Length)
                    {
                        mortesNaFase = 0;
                        CarregarFase(faseAtual + 1);
                        estado = Estado.Jogando;
                    }
                    else
                    {
                        estado = Estado.Vitoria;
                        SalvarRecorde();
                    }
                }
                break;

            case Estado.Vitoria:
                if (Controles.Confirmar()) VoltarAoTitulo();
                break;
        }
    }

    // ------------------------------------------------------------------ chamados pelos objetos

    public void JogadorMorreu(bool caiuNoBuraco)
    {
        if (estado != Estado.Jogando) return;
        mortes++;
        mortesNaFase++;
        estado = Estado.Morreu;
        timerEstado = tempoAposMorte;
        mensagem = EscolherMensagem();
        Som("morte");
        if (!caiuNoBuraco) CameraSeguir.Tremer(0.2f, 0.2f);
    }

    public void FaseConcluida()
    {
        if (estado != Estado.Jogando) return;
        estado = Estado.FaseConcluida;
        timerEstado = tempoAposConcluir;
        moedas += moedasNaFase;
        moedasNaFase = 0;
        jogador.Congelar();
        Som("vitoria");
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
        if (j == null || j.Morto || Instancia.estado != Estado.Jogando)
        {
            posicao = Vector2.zero;
            return false;
        }
        posicao = j.transform.position;
        return true;
    }

    string EscolherMensagem()
    {
        switch (mortes)
        {
            case 1: return "Primeira de muitas...";
            case 10: return "10 mortes! Tá indo bem (mentira)";
            case 25: return "25 mortes. Já pensou em jogar outra coisa?";
            case 50: return "50 MORTES! Parabéns pela persistência!";
            case 100: return "100 mortes. Respeito.";
        }
        Fase fase = Fases.Todas[faseAtual];
        if (mortesNaFase == 1 && fase.mudancas != null && fase.mudancas.Length > 0)
            return "Ah, e eu mudei umas coisinhas. :)";
        if (mortesNaFase == 5) return "Dica: nem tudo é o que parece.";
        return Fases.MensagensDeMorte[Random.Range(0, Fases.MensagensDeMorte.Length)];
    }

    void SalvarRecorde()
    {
        int recorde = PlayerPrefs.GetInt(ChaveRecorde, int.MaxValue);
        if (mortes < recorde)
        {
            PlayerPrefs.SetInt(ChaveRecorde, mortes);
            PlayerPrefs.Save();
        }
    }

    static string Titulo(int mortes)
    {
        if (mortes < 10) return "Suspeito de hack";
        if (mortes < 30) return "Sobrevivente";
        if (mortes < 60) return "Teimoso profissional";
        if (mortes < 100) return "Colecionador de mortes";
        return "Lenda da persistência";
    }

    // ------------------------------------------------------------------ interface (HUD e telas)

    GUIStyle estiloHud, estiloGrande, estiloMedio, estiloPlaca;
    Texture2D fundoEscuro;

    void PrepararEstilos(float escala)
    {
        if (fundoEscuro == null)
        {
            fundoEscuro = new Texture2D(1, 1);
            fundoEscuro.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
            fundoEscuro.Apply();
        }
        estiloHud = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(26 * escala), fontStyle = FontStyle.Bold };
        estiloGrande = new GUIStyle(estiloHud) { fontSize = Mathf.RoundToInt(80 * escala), alignment = TextAnchor.MiddleCenter };
        estiloMedio = new GUIStyle(estiloHud) { fontSize = Mathf.RoundToInt(34 * escala), alignment = TextAnchor.MiddleCenter };
        estiloPlaca = new GUIStyle(estiloHud) { fontSize = Mathf.RoundToInt(22 * escala), alignment = TextAnchor.MiddleCenter, wordWrap = true };
    }

    void OnGUI()
    {
        float escala = Screen.height / 720f;
        PrepararEstilos(escala);
        float w = Screen.width, h = Screen.height;

        DesenharPlacas(escala);
        if (estado == Estado.Jogando || estado == Estado.Morreu || estado == Estado.FaseConcluida)
            DesenharBarraDeProgresso(escala);

        if (estado != Estado.Titulo && estado != Estado.Vitoria)
        {
            Fase fase = Fases.Todas[faseAtual];
            estiloHud.alignment = TextAnchor.UpperLeft;
            TextoComSombra(new Rect(20 * escala, 12 * escala, w, 40 * escala), $"Fase {faseAtual + 1}/{Fases.Todas.Length}: {fase.nome}", estiloHud);
            TextoComSombra(new Rect(20 * escala, 44 * escala, w, 40 * escala), $"Mortes: {mortes}", estiloHud);
            estiloHud.alignment = TextAnchor.UpperRight;
            TextoComSombra(new Rect(0, 12 * escala, w - 20 * escala, 40 * escala), $"Moedas: {moedas + moedasNaFase}", estiloHud);
            TextoComSombra(new Rect(0, 44 * escala, w - 20 * escala, 40 * escala), FormatarTempo(tempoTotal), estiloHud);
        }

        if (estado == Estado.Jogando && timerAviso > 0f && aviso != "")
            TextoComSombra(new Rect(0, h * 0.22f, w, 60 * escala), aviso, estiloMedio, new Color(1f, 0.6f, 1f));

        switch (estado)
        {
            case Estado.Titulo:
                GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
                estiloGrande.fontSize = Mathf.RoundToInt(120 * escala);
                TextoComSombra(new Rect(0, h * 0.18f, w, 150 * escala), "CILADA!", estiloGrande, new Color(1f, 0.85f, 0.2f));
                TextoComSombra(new Rect(0, h * 0.18f + 130 * escala, w, 50 * escala), "um jogo de plataforma nada confiável", estiloMedio);
                if (Time.unscaledTime % 1f < 0.65f)
                    TextoComSombra(new Rect(0, h * 0.6f, w, 50 * escala), "Aperte ENTER para começar", estiloMedio, new Color(1f, 0.85f, 0.2f));
                estiloPlaca.fontSize = Mathf.RoundToInt(22 * escala);
                TextoComSombra(new Rect(0, h * 0.75f, w, 90 * escala),
                    "A/D ou SETAS: andar    ESPAÇO/W: pular (segure para ir mais alto)\nR: reiniciar fase    ESC: voltar ao título", estiloPlaca);
                int recorde = PlayerPrefs.GetInt(ChaveRecorde, -1);
                if (recorde >= 0)
                    TextoComSombra(new Rect(0, h * 0.9f, w, 40 * escala), $"Recorde: zerou com {recorde} mortes", estiloPlaca);
                break;

            case Estado.Morreu:
                TextoComSombra(new Rect(0, h * 0.35f, w, 100 * escala), mensagem, estiloMedio, new Color(1f, 0.4f, 0.4f));
                break;

            case Estado.FaseConcluida:
                TextoComSombra(new Rect(0, h * 0.3f, w, 100 * escala), "FASE CONCLUÍDA!", estiloGrande, new Color(0.5f, 1f, 0.5f));
                string comentario = mortesNaFase == 0 ? "Sem morrer?! Impossível." : $"Só {mortesNaFase} morte(s) nessa fase.";
                TextoComSombra(new Rect(0, h * 0.3f + 100 * escala, w, 50 * escala), comentario, estiloMedio);
                break;

            case Estado.Vitoria:
                GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
                TextoComSombra(new Rect(0, h * 0.15f, w, 100 * escala), "VOCÊ ZEROU!", estiloGrande, new Color(1f, 0.85f, 0.2f));
                TextoComSombra(new Rect(0, h * 0.38f, w, 50 * escala), $"Mortes: {mortes}", estiloMedio);
                TextoComSombra(new Rect(0, h * 0.38f + 50 * escala, w, 50 * escala), $"Moedas: {moedas}", estiloMedio);
                TextoComSombra(new Rect(0, h * 0.38f + 100 * escala, w, 50 * escala), $"Tempo: {FormatarTempo(tempoTotal)}", estiloMedio);
                TextoComSombra(new Rect(0, h * 0.38f + 170 * escala, w, 50 * escala), $"Título: {Titulo(mortes)}", estiloMedio, new Color(0.5f, 1f, 0.5f));
                if (Time.unscaledTime % 1f < 0.65f)
                    TextoComSombra(new Rect(0, h * 0.85f, w, 50 * escala), "Aperte ENTER para jogar de novo", estiloMedio);
                break;
        }
    }

    // Barrinha no topo: do início da fase até a bandeira mais distante.
    // (Usa a mais distante de propósito, para não entregar qual é a bandeira de verdade.
    //  E se a bandeira fujona fugir, o fim da barra foge junto.)
    void DesenharBarraDeProgresso(float escala)
    {
        if (jogador == null) return;
        float fim = float.MinValue;
        foreach (Transform bandeira in bandeiras)
            if (bandeira != null) fim = Mathf.Max(fim, bandeira.position.x);
        float inicio = PosicaoInicial.x;
        if (fim <= inicio) return;

        float progresso = Mathf.Clamp01((jogador.transform.position.x - inicio) / (fim - inicio));

        float largura = Mathf.Min(Screen.width * 0.36f, 520f * escala);
        float altura = 12f * escala;
        var barra = new Rect((Screen.width - largura) / 2f, 96f * escala, largura, altura); // abaixo do HUD, para não cobrir o nome da fase

        Color corOriginal = GUI.color;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(barra.x - 3f * escala, barra.y - 3f * escala, barra.width + 6f * escala, barra.height + 6f * escala), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, 0.35f);
        GUI.DrawTexture(barra, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.85f, 0.2f);
        GUI.DrawTexture(new Rect(barra.x, barra.y, barra.width * progresso, barra.height), Texture2D.whiteTexture);
        GUI.color = corOriginal;

        // bandeira no fim da barra
        float alturaBandeira = 42f * escala;
        GUI.DrawTexture(new Rect(barra.xMax - alturaBandeira / 6f, barra.yMax - alturaBandeira, alturaBandeira / 3f, alturaBandeira),
            FabricaDeSprites.Pegar("bandeira").texture);

        // gatinho andando na barra
        float tamanhoGato = 28f * escala;
        GUI.DrawTexture(new Rect(barra.x + barra.width * progresso - tamanhoGato / 2f, barra.center.y - tamanhoGato / 2f, tamanhoGato, tamanhoGato),
            FabricaDeSprites.Pegar("jogador").texture);
    }

    void DesenharPlacas(float escala)
    {
        Camera cam = Camera.main;
        if (cam == null || jogador == null || estado == Estado.Titulo) return;

        foreach (Placa placa in placas)
        {
            if (placa == null || Vector2.Distance(placa.transform.position, jogador.transform.position) > 3.5f) continue;

            Vector3 tela = cam.WorldToScreenPoint(placa.transform.position + Vector3.up * 1.4f);
            float largura = 420 * escala, altura = 80 * escala;
            var caixa = new Rect(tela.x - largura / 2f, Screen.height - tela.y - altura, largura, altura);
            GUI.DrawTexture(caixa, fundoEscuro);
            GUI.Label(caixa, placa.texto, estiloPlaca);
        }
    }

    static void TextoComSombra(Rect area, string texto, GUIStyle estilo, Color? cor = null)
    {
        Color original = estilo.normal.textColor;
        float sombra = Mathf.Max(2f, estilo.fontSize / 16f);
        estilo.normal.textColor = Color.black;
        GUI.Label(new Rect(area.x + sombra, area.y + sombra, area.width, area.height), texto, estilo);
        estilo.normal.textColor = cor ?? Color.white;
        GUI.Label(area, texto, estilo);
        estilo.normal.textColor = original;
    }

    static string FormatarTempo(float segundos)
    {
        int total = Mathf.FloorToInt(segundos);
        return $"{total / 60:00}:{total % 60:00}";
    }
}
