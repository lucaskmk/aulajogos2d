// A STEAM: conquistas, placares, o nome do jogador, a "presença" (o que aparece para os amigos) e o convite do
// Remote Play Together. Usa o pacote Steamworks.NET (Packages/manifest.json).
//
// Sem a Steam aberta (ou fora dela: o build do itch, o editor sem steam_appid.txt) tudo aqui vira "não faz nada",
// e o jogo funciona igual. No WebGL o pacote nem existe (o código abaixo some com o #if).
//
// PARA LIGAR DE VERDADE (depois de criar o app no Steamworks):
//   1. Trocar AppId abaixo pelo número do jogo (a demo tem outro número: AppIdDaDemo).
//   2. Cadastrar no Steamworks as conquistas com os MESMOS ids de Conquistas.Todas (Ferramentas/conquistas_steam.py
//      gera a lista) e os placares com os nomes de Placares (Speedrun.cs). Publicar as mudanças do Steamworks.
//   3. Para testar no editor: um arquivo steam_appid.txt na raiz do projeto com o número (não vai para o git).
// Os detalhes estão em PUBLICAR_E_VENDER.md.
using UnityEngine;

public static class Steam
{
    // 480 é o "Spacewar", o app de teste da Valve: serve para testar a integração antes de ter o número do jogo.
    public const uint AppId = 480;
    public const uint AppIdDaDemo = 480;
    static uint AppIdDesteBuild => Demo.Ativa ? AppIdDaDemo : AppId;
    static bool TemNumeroDeVerdade => AppIdDesteBuild != 480;

    public static bool Ligada { get; private set; }
    public static string NomeDoJogador { get; private set; } = "";

#if UNITY_STANDALONE && !CILADA_SEM_STEAM
    static Steamworks.CallResult<Steamworks.LeaderboardFindResult_t> achouPlacar;
    static readonly System.Collections.Generic.Dictionary<string, Steamworks.SteamLeaderboard_t> placares =
        new System.Collections.Generic.Dictionary<string, Steamworks.SteamLeaderboard_t>();
    static readonly System.Collections.Generic.Queue<(string nome, int valor)> paraEnviar = new System.Collections.Generic.Queue<(string, int)>();
    static bool procurandoPlacar;
    static string placarProcurado;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
#if UNITY_STANDALONE && !CILADA_SEM_STEAM
        if (Ligada) return;
        try
        {
            // Aberto fora da Steam (clicou no .exe)? A Steam abre o jogo de novo do jeito certo. Só com o número de verdade
            // (com o 480 isso abriria o Spacewar) e nunca no editor.
            if (!Application.isEditor && TemNumeroDeVerdade && Steamworks.SteamAPI.RestartAppIfNecessary(new Steamworks.AppId_t(AppIdDesteBuild)))
            {
                Application.Quit();
                return;
            }
            if (!Steamworks.Packsize.Test())
            {
                Debug.LogWarning("[Steam] a biblioteca da Steam está com a versão errada: a Steam fica desligada");
                return;
            }
            Ligada = Steamworks.SteamAPI.Init();
        }
        catch (System.DllNotFoundException)
        {
            Ligada = false;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Steam] não deu para ligar: " + e.Message);
            Ligada = false;
        }
        if (!Ligada)
        {
            Debug.Log("[Steam] desligada (sem a Steam aberta ou fora dela): o jogo roda sem conquistas e placares da Steam");
            return;
        }
        NomeDoJogador = Steamworks.SteamFriends.GetPersonaName();
        Debug.Log("[Steam] ligada: " + NomeDoJogador);
        Conquistas.AoDesbloquear += DesbloquearNaSteam;
        Placares.AoEnviar += EnviarPlacar;
        new GameObject("Steam") { hideFlags = HideFlags.HideInHierarchy }.AddComponent<SteamPorQuadro>();
        Application.quitting += Desligar;
#endif
    }

    // O que aparece para os amigos na lista da Steam ("Mar de Baixo 3", "No mapa"...).
    public static void Presenca(string texto)
    {
#if UNITY_STANDALONE && !CILADA_SEM_STEAM
        if (!Ligada) return;
        Steamworks.SteamFriends.SetRichPresence("status", texto);
#endif
    }

    // Abre a janela da Steam para convidar amigos pelo Remote Play Together (eles jogam sem ter o jogo).
    // Devolve falso se não deu (sem a Steam, ou a Steam é velha demais).
    public static bool ConvidarPeloRemotePlay()
    {
#if UNITY_STANDALONE && !CILADA_SEM_STEAM
        if (!Ligada) return false;
        try
        {
            Steamworks.SteamFriends.ActivateGameOverlayRemotePlayTogetherInviteDialog(Steamworks.CSteamID.Nil);
            return Steamworks.SteamUtils.IsOverlayEnabled();
        }
        catch { return false; }
#else
        return false;
#endif
    }

#if UNITY_STANDALONE && !CILADA_SEM_STEAM
    static void DesbloquearNaSteam(string id)
    {
        Steamworks.SteamUserStats.SetAchievement(id);
        Steamworks.SteamUserStats.StoreStats();
    }

    // Os placares precisam ser achados (ou criados) antes de receber pontos, e isso volta depois (CallResult):
    // os pontos esperam numa fila e saem um placar de cada vez.
    static void EnviarPlacar(string nome, int valor)
    {
        paraEnviar.Enqueue((nome, valor));
        Proximo();
    }

    static void Proximo()
    {
        while (!procurandoPlacar && paraEnviar.Count > 0)
        {
            (string nome, int valor) = paraEnviar.Peek();
            if (placares.TryGetValue(nome, out Steamworks.SteamLeaderboard_t placar))
            {
                paraEnviar.Dequeue();
                Steamworks.SteamUserStats.UploadLeaderboardScore(placar,
                    Steamworks.ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, valor, null, 0);
                continue;
            }
            procurandoPlacar = true;
            placarProcurado = nome;
            bool ehTempo = !nome.StartsWith("mortes_");
            achouPlacar ??= Steamworks.CallResult<Steamworks.LeaderboardFindResult_t>.Create(AchouPlacar);
            achouPlacar.Set(Steamworks.SteamUserStats.FindOrCreateLeaderboard(nome,
                Steamworks.ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending,
                ehTempo ? Steamworks.ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeMilliSeconds
                        : Steamworks.ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric));
        }
    }

    static void AchouPlacar(Steamworks.LeaderboardFindResult_t resultado, bool falhou)
    {
        procurandoPlacar = false;
        if (falhou || resultado.m_bLeaderboardFound == 0)
        {
            // não achou: joga fora os pontos desse placar (senão a fila trava nele)
            while (paraEnviar.Count > 0 && paraEnviar.Peek().nome == placarProcurado) paraEnviar.Dequeue();
        }
        else placares[placarProcurado] = resultado.m_hSteamLeaderboard;
        Proximo();
    }

    static void Desligar()
    {
        if (!Ligada) return;
        Ligada = false;
        Steamworks.SteamAPI.Shutdown();
    }

    // A Steam precisa que o jogo chame RunCallbacks todo quadro (é quando as respostas dela chegam).
    class SteamPorQuadro : MonoBehaviour
    {
        void Awake() => DontDestroyOnLoad(gameObject);

        // as conquistas que a pessoa já tem no jogo (de antes da Steam) sobem agora (o save já foi lido)
        void Start()
        {
            if (!Ligada) return;
            foreach (Conquistas.Conquista c in Conquistas.Todas)
                if (Conquistas.Tem(c.id)) Steamworks.SteamUserStats.SetAchievement(c.id);
            Steamworks.SteamUserStats.StoreStats();
        }
        void Update() { if (Ligada) Steamworks.SteamAPI.RunCallbacks(); }
    }
#endif
}
