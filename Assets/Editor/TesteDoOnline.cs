// Teste automático da TRIPULAÇÃO ONLINE (Online/*.cs): abre uma sala de verdade neste computador (127.0.0.1),
// conecta convidados de verdade nela (pela rede, com TCP) e confere: o código da sala, a recusa por versão errada,
// o controle virtual, os botões (inclusive o aperto rápido que cabe entre dois quadros), a entrada na tripulação,
// os menus do anfitrião ignorando o convidado, o ping, a saída e a sala fechada.
// Com a janela do Unity aberta (sem -batchmode), confere também a IMAGEM e o SOM que chegam ao convidado,
// e salva a imagem recebida em CILADA_FOTOS/online_recebida.png (se a variável existir).
//
// Como rodar:
//  - no editor: menu "Cilada (teste)/Testar online" (com o jogo PARADO);
//  - pela linha de comando: Unity.exe -batchmode -projectPath <pasta> -executeMethod TesteDoOnline.RodarEFechar -logFile online.log
//    (sai com código 0 se tudo passou). As linhas do resultado começam com "[TesteOnline]".
// Não mexe no save (não joga nenhuma fase).
using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class TesteDoOnline
{
    const int Porta = 27975; // (outra porta: não briga com uma sala aberta de verdade)
    static int falhas, conferidos;
    static bool fecharNoFim, esperandoOPlay, terminou;
    static double inicio;
    static InputSettings.BackgroundBehavior focoAntes;
    static InputSettings.EditorInputBehaviorInPlayMode entradaAntes;
    static string pastaDasFotos;

    [MenuItem("Cilada (teste)/Testar online")]
    public static void Rodar()
    {
        if (Application.isPlaying) { Debug.LogWarning("[TesteOnline] pare o jogo antes de rodar o teste"); return; }
        falhas = conferidos = 0;
        esperandoOPlay = true;
        EditorApplication.update -= EsperarOPlay;
        EditorApplication.update += EsperarOPlay;
        EditorApplication.EnterPlaymode();
    }

    // (o mesmo cuidado do TesteDoCoop: espera o editor ficar quieto antes do Play, por causa da recarga dos scripts)
    const string ChavePendente = "TesteDoOnline.pendente", ChaveFechar = "TesteDoOnline.fechar";
    static double horaDeComecar;

    public static void RodarEFechar()
    {
        SessionState.SetBool(ChaveFechar, true);
        SessionState.SetBool(ChavePendente, true);
        Agendar();
    }

    [InitializeOnLoadMethod]
    static void DepoisDaRecarga()
    {
        if (!SessionState.GetBool(ChaveFechar, false)) return;
        if (SessionState.GetBool(ChavePendente, false)) Agendar();
        else if (EditorApplication.isPlaying)
        {
            Debug.LogError("[TesteOnline] FALHOU: os scripts recarregaram no meio do Play (o teste perdeu o estado)");
            EditorApplication.Exit(99);
        }
    }

    static void Agendar()
    {
        horaDeComecar = EditorApplication.timeSinceStartup + 15;
        EditorApplication.update -= EsperarOEditor;
        EditorApplication.update += EsperarOEditor;
    }

    static void EsperarOEditor()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            horaDeComecar = EditorApplication.timeSinceStartup + 8;
            return;
        }
        if (EditorApplication.timeSinceStartup < horaDeComecar) return;
        EditorApplication.update -= EsperarOEditor;
        SessionState.SetBool(ChavePendente, false);
        fecharNoFim = true;
        pastaDasFotos = System.Environment.GetEnvironmentVariable("CILADA_FOTOS");
        Rodar();
    }

    static void EsperarOPlay()
    {
        if (!esperandoOPlay || !Application.isPlaying || GerenciadorDoJogo.Instancia == null) return;
        esperandoOPlay = false;
        EditorApplication.update -= EsperarOPlay;
        inicio = EditorApplication.timeSinceStartup;
        terminou = false;
        EditorApplication.update -= Vigiar;
        EditorApplication.update += Vigiar;
        GerenciadorDoJogo.Instancia.StartCoroutine(Roteiro());
    }

    static void Vigiar()
    {
        if (terminou) { EditorApplication.update -= Vigiar; return; }
        if (EditorApplication.timeSinceStartup - inicio < 240) return;
        falhas++;
        Debug.LogError("[TesteOnline] FALHOU: o roteiro não terminou em 240 s (travou no meio; veja o erro acima)");
        Terminar();
    }

    static void Conferir(bool ok, string oque)
    {
        conferidos++;
        if (ok) Debug.Log("[TesteOnline] ok: " + oque);
        else { falhas++; Debug.LogError("[TesteOnline] FALHOU: " + oque); }
    }

    static Gamepad ControleRemoto()
    {
        foreach (Gamepad g in Gamepad.all) if (Online.EhRemoto(g)) return g;
        return null;
    }

    static Protocolo.Botoes Com(params GamepadButton[] botoes)
    {
        uint bits = 0;
        foreach (GamepadButton b in botoes) bits |= 1u << (int)b;
        return new Protocolo.Botoes { bits = bits };
    }

    // Espera (no máximo "segundos") a condição ficar verdadeira, atualizando a sala do convidado a cada quadro.
    static IEnumerator Esperar(System.Func<bool> condicao, float segundos, SalaRemota sala = null)
    {
        float limite = Time.realtimeSinceStartup + segundos;
        while (Time.realtimeSinceStartup < limite)
        {
            sala?.Atualizar();
            if (condicao()) yield break;
            yield return null;
        }
    }

    // ------------------------------------------------------------------ o roteiro

    static IEnumerator Roteiro()
    {
        Application.logMessageReceived += NotarExcecao;
        focoAntes = InputSystem.settings.backgroundBehavior;
        entradaAntes = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        yield return null;

        // --- o código da sala, a lei mu e o menu (sem rede)
        Conferir(CodigoDaSala.Ler(CodigoDaSala.Gerar(IPAddress.Parse("189.45.12.200"), Protocolo.PortaPadrao), out string h, out int p)
                 && h == "189.45.12.200" && p == Protocolo.PortaPadrao, "código da sala: ida e volta na porta padrão (" + CodigoDaSala.Gerar(IPAddress.Parse("189.45.12.200"), Protocolo.PortaPadrao) + ")");
        string comPorta = CodigoDaSala.Gerar(IPAddress.Parse("10.0.0.7"), 30001);
        Conferir(CodigoDaSala.Ler(comPorta.ToLowerInvariant(), out h, out p) && h == "10.0.0.7" && p == 30001, "código com outra porta e em minúsculas (" + comPorta + ")");
        Conferir(CodigoDaSala.Ler("192.168.0.5:4000", out h, out p) && h == "192.168.0.5" && p == 4000, "IP:porta digitado");
        Conferir(!CodigoDaSala.Ler("XYZ", out _, out _) && !CodigoDaSala.Ler("", out _, out _), "código inválido é recusado");
        float pior = 0f;
        for (float v = -1f; v <= 1f; v += 0.01f) pior = Mathf.Max(pior, Mathf.Abs(Protocolo.DeMuLaw(Protocolo.MuLaw(v)) - v));
        Conferir(pior < 0.04f, $"lei mu: o som volta quase igual (erro máximo {pior:0.000})");
        Conferir(GerenciadorDoJogo.Instancia.opcoesDoMenu.Contains(GerenciadorDoJogo.OpcaoDoMenu.Online), "o título tem \"Jogar online\"");

        // --- a sala
        Conferir(Anfitriao.Abrir(Porta, upnp: false, anunciar: false), "abriu a sala na porta " + Porta + " " + Anfitriao.UltimoErro);
        Anfitriao anfitriao = Anfitriao.Instancia;
        if (anfitriao == null) { Terminar(); yield break; }

        // --- versão errada: recusado
        var tcpErrado = new TcpClient();
        tcpErrado.Connect("127.0.0.1", Porta);
        var errada = new Conexao(tcpErrado);
        var olaErrado = Protocolo.Ola("Velho");
        System.BitConverter.GetBytes(999).CopyTo(olaErrado, 0);
        errada.Enviar(Protocolo.Tipo.Ola, olaErrado);
        bool recusado = false;
        float limite = Time.realtimeSinceStartup + 4f;
        while (!recusado && Time.realtimeSinceStartup < limite)
        {
            while (errada.Receber(out Mensagem m)) if (m.tipo == Protocolo.Tipo.Recusado) recusado = true;
            yield return null;
        }
        Conferir(recusado, "convidado com outra versão do jogo é recusado");
        Conferir(ControleRemoto() == null, "o recusado não ganhou controle virtual");
        errada.Fechar("fim");

        // --- o convidado de verdade
        var sala = new SalaRemota("127.0.0.1", Porta, "Testador");
        yield return Esperar(() => sala.Estado != SalaRemota.Situacao.Conectando, 6f, sala);
        Conferir(sala.Estado == SalaRemota.Situacao.Dentro, $"o convidado entrou na sala ({sala.Estado} {sala.Motivo})");
        Conferir(sala.NomeDaSala.Length > 0 && sala.Numero == 1, $"o anfitrião mandou o nome da sala e o número ({sala.NomeDaSala}, {sala.Numero})");
        Gamepad remoto = ControleRemoto();
        Conferir(remoto != null && remoto.added, "o convidado virou um controle virtual no anfitrião");
        Conferir(Online.NomeDoRemoto(remoto) == "Testador", "o controle virtual sabe o nome do convidado");
        if (remoto == null) { sala.Dispose(); Anfitriao.Fechar(); Terminar(); yield break; }

        // --- os botões
        sala.EnviarBotoes(Com(GamepadButton.South));
        yield return Esperar(() => remoto.buttonSouth.isPressed, 3f, sala);
        Conferir(remoto.buttonSouth.isPressed, "o PULAR do convidado chega no controle virtual");
        Conferir(Controles.Local != remoto, "os menus do anfitrião não obedecem ao controle do convidado");
        sala.EnviarBotoes(new Protocolo.Botoes { x = 127 });
        yield return Esperar(() => !remoto.buttonSouth.isPressed && remoto.leftStick.ReadValue().x > 0.9f, 3f, sala);
        Conferir(!remoto.buttonSouth.isPressed && remoto.leftStick.ReadValue().x > 0.9f, "soltar o botão e o analógico também chegam");

        // aperto rápido: aperta e solta o empurrão antes do anfitrião ter um quadro no meio. O jogo precisa ver o aperto.
        sala.EnviarBotoes(Com(GamepadButton.West));
        sala.EnviarBotoes(default);
        bool viuOAperto = false;
        limite = Time.realtimeSinceStartup + 3f;
        while (Time.realtimeSinceStartup < limite)
        {
            sala.Atualizar();
            if (remoto.buttonWest.wasPressedThisFrame) { viuOAperto = true; break; }
            yield return null;
        }
        Conferir(viuOAperto, "um aperto rápido (aperta e solta no mesmo pacote) não se perde");
        yield return Esperar(() => !remoto.buttonWest.isPressed, 2f, sala);

        // --- entrar na tripulação (a tela T do mapa) apertando PULAR, como um controle de verdade
        Tripulacao.AbrirLobby();
        sala.EnviarBotoes(default);
        yield return Esperar(() => !remoto.buttonSouth.isPressed, 1f, sala);
        sala.EnviarBotoes(Com(GamepadButton.South));
        bool entrou = false;
        limite = Time.realtimeSinceStartup + 3f;
        while (Time.realtimeSinceStartup < limite && !entrou)
        {
            sala.Atualizar();
            Tripulacao.AtualizarLobby();
            for (int i = 0; i < Tripulacao.NoLobby; i++)
                if (Tripulacao.Do(i).controle == remoto) entrou = true;
            yield return null;
        }
        Conferir(entrou, "o convidado entra na tripulação apertando PULAR na tela T");
        Tripulacao.FecharLobby();
        sala.EnviarBotoes(default);
        Tripulacao.Definir(new Tripulacao.Marujo());

        // --- o ping
        yield return Esperar(() => sala.PingMs >= 0, 4f, sala);
        Conferir(sala.PingMs >= 0 && sala.PingMs < 500, $"o ping do convidado foi medido ({sala.PingMs} ms)");
        yield return Esperar(() => anfitriao.Convidados.Count > 0 && anfitriao.Convidados[0].pingMs >= 0, 4f, sala);
        Conferir(anfitriao.Convidados.Count > 0 && anfitriao.Convidados[0].pingMs >= 0, "o anfitrião também mede o ping de cada um");

        // --- a imagem e o som (só com a janela: em -batchmode não existe tela nem placa de som)
        if (Application.isBatchMode)
            Debug.Log("[TesteOnline] (sem janela: a imagem e o som não são conferidos; rode sem -batchmode para conferir)");
        else
        {
            yield return Esperar(() => sala.QuadrosRecebidos >= 5, 8f, sala);
            Conferir(sala.QuadrosRecebidos >= 5, $"a imagem chega ao convidado ({sala.QuadrosRecebidos} quadros, {anfitriao.QuadrosCodificados} comprimidos)");
            if (sala.Quadro != null)
            {
                var imagem = new Texture2D(2, 2);
                bool abriu = imagem.LoadImage(sala.Quadro);
                Conferir(abriu && imagem.width >= 200 && imagem.height >= 150, $"a imagem é um JPEG que abre ({imagem.width}x{imagem.height}, {sala.Quadro.Length / 1024} KB)");
                Color32[] px = imagem.GetPixels32();
                int diferentes = 0;
                for (int i = 1; i < px.Length; i += 97)
                    if (Mathf.Abs(px[i].r - px[0].r) + Mathf.Abs(px[i].g - px[0].g) + Mathf.Abs(px[i].b - px[0].b) > 30) diferentes++;
                Conferir(diferentes > 20, "a imagem não é uma tela vazia (tem o título desenhado)");
                if (!string.IsNullOrEmpty(pastaDasFotos))
                {
                    System.IO.Directory.CreateDirectory(pastaDasFotos);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(pastaDasFotos, "online_recebida.png"), imagem.EncodeToPNG());
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(pastaDasFotos, "online_anfitriao.png"));
                }
            }
            yield return Esperar(() => sala.PedacosDeSom >= 10, 5f, sala);
            Conferir(sala.PedacosDeSom >= 10, $"o som chega ao convidado ({sala.PedacosDeSom} pedaços)");
        }

        // --- sair
        sala.Dispose();
        yield return Esperar(() => anfitriao.Convidados.Count == 0 && ControleRemoto() == null, 4f);
        Conferir(anfitriao.Convidados.Count == 0, "o anfitrião percebe que o convidado saiu");
        Conferir(ControleRemoto() == null && !remoto.added, "o controle virtual some quando o convidado sai");

        // --- o convidado "com tela" (o que o jogador usa): o jogo daqui para embaixo, e volta ao sair
        Convidado.Entrar("127.0.0.1", Porta);
        Conferir(Online.SomosConvidados, "Convidado.Entrar liga o modo convidado");
        yield return Esperar(() => Convidado.Instancia == null || Convidado.Instancia.Sala.Estado == SalaRemota.Situacao.Dentro, 6f);
        Conferir(Convidado.Instancia != null && Convidado.Instancia.Sala.Estado == SalaRemota.Situacao.Dentro, "o convidado com tela entra na sala");
        Convidado.Sair();
        yield return null;
        yield return Esperar(() => anfitriao.Convidados.Count == 0, 4f);
        Conferir(!Online.SomosConvidados && anfitriao.Convidados.Count == 0, "sair do modo convidado devolve o jogo daqui");

        // --- sala cheia: 3 convidados entram, o 4º é recusado
        var tres = new SalaRemota[3];
        for (int i = 0; i < 3; i++) tres[i] = new SalaRemota("127.0.0.1", Porta, "Marujo" + (i + 1));
        yield return Esperar(() => System.Array.TrueForAll(tres, s => { s.Atualizar(); return s.Estado == SalaRemota.Situacao.Dentro; }), 8f);
        Conferir(System.Array.TrueForAll(tres, s => s.Estado == SalaRemota.Situacao.Dentro), "3 convidados cabem na sala");
        var quarto = new SalaRemota("127.0.0.1", Porta, "Sobrando");
        yield return Esperar(() => quarto.Estado != SalaRemota.Situacao.Conectando, 6f, quarto);
        Conferir(quarto.Estado == SalaRemota.Situacao.Recusado, $"o 4º convidado é recusado (sala cheia: {quarto.Motivo})");

        // --- fechar a sala: os convidados ficam sabendo
        Anfitriao.Fechar();
        yield return Esperar(() => System.Array.TrueForAll(tres, s => { s.Atualizar(); return s.Estado == SalaRemota.Situacao.Caiu; }), 6f);
        Conferir(System.Array.TrueForAll(tres, s => s.Estado == SalaRemota.Situacao.Caiu), "fechar a sala avisa os convidados (" + tres[0].Motivo + ")");
        Conferir(!Anfitriao.Aberto && ControleRemoto() == null, "a sala fechou e levou os controles virtuais");
        foreach (SalaRemota s in tres) s.Dispose();
        quarto.Dispose();

        var semSala = new SalaRemota("127.0.0.1", Porta, "Atrasado");
        yield return Esperar(() => semSala.Estado != SalaRemota.Situacao.Conectando, 9f, semSala);
        Conferir(semSala.Estado == SalaRemota.Situacao.Caiu, "sem sala aberta, o convidado recebe um aviso (" + semSala.Motivo + ")");
        semSala.Dispose();

        Terminar();
    }

    static void NotarExcecao(string texto, string pilha, LogType tipo)
    {
        if (tipo != LogType.Exception) return;
        falhas++;
        Debug.LogError("[TesteOnline] FALHOU: exceção durante o teste: " + texto);
    }

    static void Terminar()
    {
        if (terminou) return;
        terminou = true;
        Application.logMessageReceived -= NotarExcecao;
        Anfitriao.Fechar();
        Convidado.Sair();
        InputSystem.settings.backgroundBehavior = focoAntes;
        InputSystem.settings.editorInputBehaviorInPlayMode = entradaAntes;
        if (falhas == 0) Debug.Log($"[TesteOnline] TODOS OS {conferidos} TESTES PASSARAM");
        else Debug.LogError($"[TesteOnline] {falhas} de {conferidos} teste(s) falharam");
        EditorApplication.ExitPlaymode();
        if (!fecharNoFim) return;
        SessionState.SetBool(ChaveFechar, false);
        EditorApplication.update += FecharQuandoParar;
    }

    static void FecharQuandoParar()
    {
        if (EditorApplication.isPlaying) return;
        EditorApplication.update -= FecharQuandoParar;
        EditorApplication.Exit(falhas);
    }
}
