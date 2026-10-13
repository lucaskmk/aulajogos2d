using UnityEngine;

// As opções do jogo (tela de OPÇÕES, aberta pelo título ou pela pausa), salvas em PlayerPrefs:
// volume da música e dos efeitos, tipo de gráficos, cronômetro no HUD e tela cheia.
public static class Opcoes
{
    const string ChaveMusica = "cilada_volume_musica";
    const string ChaveEfeitos = "cilada_volume_efeitos";
    const string ChaveGraficos = "cilada_graficos_hd2d";
    const string ChaveCronometro = "cilada_mostrar_tempo";

    static float musica = -1f, efeitos = -1f; // -1 = ainda não leu do PlayerPrefs
    static int graficos = -1;                 // -1 = ainda não leu; 1 = HD-2D; 0 = Simples
    static int cronometro = -1;               // -1 = ainda não leu; 1 = mostra o tempo no HUD; 0 = esconde

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar()
    {
        musica = efeitos = -1f;
        graficos = -1;
        cronometro = -1;
    }

    // Mostrar o tempo da partida no canto do HUD? (as mortes sempre aparecem: elas são o "placar" do jogo)
    public static bool MostrarTempo
    {
        get
        {
            if (cronometro < 0) cronometro = PlayerPrefs.GetInt(ChaveCronometro, 1);
            return cronometro == 1;
        }
        set
        {
            cronometro = value ? 1 : 0;
            PlayerPrefs.SetInt(ChaveCronometro, cronometro);
            PlayerPrefs.Save();
        }
    }

    // ------------------------------------------------------------------ tela cheia e sair

    // Tela cheia NÃO é salva aqui: o próprio Unity já lembra dela entre uma vez e outra (no computador).
    public static bool TelaCheia => Screen.fullScreen;

    public static void AlternarTelaCheia()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // No navegador quem decide é o navegador: o Unity pede a tela cheia no próximo clique/tecla
            // do jogador (por segurança, uma página só entra em tela cheia com uma ação da pessoa).
            Screen.fullScreen = !Screen.fullScreen;
            return;
        }
        if (Screen.fullScreen)
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed); // volta para uma janela de tamanho normal
        else
        {
            Resolution monitor = Screen.currentResolution; // a resolução do monitor
            Screen.SetResolution(monitor.width, monitor.height, FullScreenMode.FullScreenWindow);
        }
    }

    // Dá para "sair do jogo"? No navegador (WebGL) não: quem fecha é a aba. Então a opção some.
    public static bool PodeSair => Application.platform != RuntimePlatform.WebGLPlayer;

    public static void SairDoJogo()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // no Editor, "sair" é parar o Play
#else
        Application.Quit();
#endif
    }

    // Gráficos "HD-2D" (padrão): fundo desfocado, feixes de luz, primeiro plano e cores de cinema.
    // "Simples": o visual antigo, mais leve (para computador fraco ou navegador travando).
    public static bool GraficosHD2D
    {
        get
        {
            if (graficos < 0) graficos = PlayerPrefs.GetInt(ChaveGraficos, 1);
            return graficos == 1;
        }
        set
        {
            graficos = value ? 1 : 0;
            PlayerPrefs.SetInt(ChaveGraficos, graficos);
            PlayerPrefs.Save();
        }
    }

    public static float Musica
    {
        get
        {
            if (musica < 0f) musica = PlayerPrefs.GetFloat(ChaveMusica, 0.8f);
            return musica;
        }
        set
        {
            musica = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(ChaveMusica, musica);
            PlayerPrefs.Save();
        }
    }

    public static float Efeitos
    {
        get
        {
            if (efeitos < 0f) efeitos = PlayerPrefs.GetFloat(ChaveEfeitos, 1f);
            return efeitos;
        }
        set
        {
            efeitos = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(ChaveEfeitos, efeitos);
            PlayerPrefs.Save();
        }
    }
}
