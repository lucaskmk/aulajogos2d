using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// O que o resto do jogo precisa saber do online (o resto fica em Anfitriao.cs, Convidado.cs e Protocolo.cs).
public static class Online
{
    // No navegador (WebGL) não existe TCP: o online some do menu.
    public static bool Disponivel => Application.platform != RuntimePlatform.WebGLPlayer;

    // Os controles virtuais dos convidados (o anfitrião liga um para cada um).
    static readonly Dictionary<InputDevice, string> remotos = new Dictionary<InputDevice, string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => remotos.Clear();

    public static void MarcarRemoto(InputDevice aparelho, string nome) => remotos[aparelho] = nome;
    public static void DesmarcarRemoto(InputDevice aparelho) => remotos.Remove(aparelho);

    // Este controle é de um convidado? (os menus do anfitrião não obedecem aos convidados: veja Controles.Local)
    public static bool EhRemoto(InputDevice aparelho) => aparelho != null && remotos.ContainsKey(aparelho);

    // O nome do convidado dono deste controle (a tela da Tripulação mostra), ou null se o controle é daqui.
    public static string NomeDoRemoto(InputDevice aparelho) => aparelho != null && remotos.TryGetValue(aparelho, out string nome) ? nome : null;

    // O nome que aparece para os outros (na lista de salas e na tripulação). Na Steam, o nome da conta.
    public static string NomeDoJogador()
    {
        string nome = Steam.NomeDoJogador;
        if (string.IsNullOrEmpty(nome))
        {
            try { nome = System.Environment.UserName; } catch { nome = null; }
        }
        if (string.IsNullOrEmpty(nome)) nome = "Grumete";
        return nome.Length > 20 ? nome.Substring(0, 20) : nome;
    }

    // Pela linha de comando (para testar dois jogos no mesmo computador, ou um atalho na área de trabalho):
    //   CiladaAVista.exe -cilada-sala                 abre a sala logo ao abrir o jogo
    //   CiladaAVista.exe -cilada-entrar 127.0.0.1     entra na sala desse endereço (ou código)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void LinhaDeComando()
    {
        if (!Disponivel || Application.isEditor) return;
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-cilada-sala") Anfitriao.Abrir();
            else if (args[i] == "-cilada-entrar" && i + 1 < args.Length && CodigoDaSala.Ler(args[i + 1], out string host, out int porta))
                Convidado.Entrar(host, porta);
        }
    }

    // O jogo que está aqui está mostrando a tela de OUTRO computador (somos convidados)?
    // O GerenciadorDoJogo fica parado enquanto isso (senão as teclas mexeriam no título daqui por baixo da imagem).
    public static bool SomosConvidados => Convidado.Ativo;
}
