// Teste automático do COOP (Tripulacao.cs, JogadorCoop.cs, GerenciadorCoop.cs): entra no Play sozinho, cria controles
// de mentira, monta uma tripulação e confere o balão de resgate, a câmera, o pulo na cabeça, o empurrão, a volta
// para o save e os títulos do fim da fase, numa fase reta feita só para o teste.
//
// Como rodar:
//  - no editor: menu "Cilada (teste)/Testar coop" (com o jogo PARADO; ele aperta Play e Stop sozinho);
//  - pela linha de comando: Unity.exe -batchmode -projectPath <pasta> -executeMethod TesteDoCoop.RodarEFechar -logFile coop.log
//    (sai com código 0 se tudo passou). O resultado fica no Console / no log, nas linhas "[TesteCoop]".
//
// ATENÇÃO: o teste joga uma fase de verdade, então mexe no save do slot atual DO EDITOR (mortes, recordes da fase 1).
// Não afeta o jogo publicado. Pode apagar este arquivo quando quiser.
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class TesteDoCoop
{
    static int falhas, conferidos;
    static bool fecharNoFim, esperandoOPlay;
    static Fase faseDeVerdade;
    static readonly List<Gamepad> controles = new List<Gamepad>();
    static Keyboard teclado;
    static InputSettings.BackgroundBehavior focoAntes;
    static InputSettings.EditorInputBehaviorInPlayMode entradaAntes;
    static string pastaDasFotos;

    [MenuItem("Cilada (teste)/Testar coop")]
    public static void Rodar()
    {
        if (Application.isPlaying) { Debug.LogWarning("[TesteCoop] pare o jogo antes de rodar o teste"); return; }
        falhas = conferidos = 0;
        esperandoOPlay = true;
        EditorApplication.update -= EsperarOPlay;
        EditorApplication.update += EsperarOPlay;
        EditorApplication.EnterPlaymode();
    }

    // Para a linha de comando: roda o teste e fecha o Unity (código de saída = número de falhas).
    // Logo que o editor abre, os pacotes dele pedem uma recarga dos scripts; se ela cai no meio do Play, o jogo perde
    // tudo o que estava na memória. Então o pedido fica guardado (SessionState atravessa a recarga) e o Play só
    // começa depois de o editor ficar uns segundos quieto.
    const string ChavePendente = "TesteDoCoop.pendente", ChaveFechar = "TesteDoCoop.fechar";
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
            Debug.LogError("[TesteCoop] FALHOU: os scripts recarregaram no meio do Play (o teste perdeu o estado)");
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
        pastaDasFotos = System.Environment.GetEnvironmentVariable("CILADA_FOTOS"); // (opcional: uma pasta para as fotos da tela)
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

    // Se o roteiro travar (um erro no meio dele para a corrotina sem avisar), o teste termina sozinho como falha.
    static double inicio;
    static bool terminou;
    const double TempoMaximo = 480; // (com a janela aberta e tirando fotos, o roteiro leva uns 4 minutos; sem janela, 1)

    static void Vigiar()
    {
        if (terminou) { EditorApplication.update -= Vigiar; return; }
        if (EditorApplication.timeSinceStartup - inicio < TempoMaximo) return;
        falhas++;
        Debug.LogError("[TesteCoop] FALHOU: o roteiro não terminou em " + TempoMaximo + " s (travou no meio; veja o erro acima)");
        Terminar();
    }

    static void Conferir(bool ok, string oque)
    {
        conferidos++;
        if (ok) Debug.Log("[TesteCoop] ok: " + oque);
        else { falhas++; Debug.LogError("[TesteCoop] FALHOU: " + oque); }
    }

    // ------------------------------------------------------------------ ajudantes

    static GerenciadorDoJogo Jogo => GerenciadorDoJogo.Instancia;
    static Jogador P(int i) => GerenciadorDoJogo.Jogadores[i];

    static object Chamar(string metodo, params object[] argumentos) =>
        typeof(GerenciadorDoJogo).GetMethod(metodo, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Jogo, argumentos);

    static object Campo(object dono, string nome) =>
        dono.GetType().GetField(nome, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(dono);

    static int BandeirasDeMorto(string sprite = null)
    {
        int quantas = 0;
        foreach (MarcaDaMorte marca in Object.FindObjectsByType<MarcaDaMorte>(FindObjectsSortMode.None))
            if (marca.name == "BandeiraDeMorto" && (sprite == null || marca.GetComponent<SpriteRenderer>().sprite == FabricaDeSprites.Pegar(sprite))) quantas++;
        return quantas;
    }

    static void Estado(EstadoDoJogo estado) => typeof(GerenciadorDoJogo).GetProperty("Estado").SetValue(Jogo, estado);

    // O controle "c" fica com o analógico em x e os botões dados apertados (até a próxima chamada).
    static void Controle(int c, float x = 0f, params GamepadButton[] botoes)
    {
        var estado = new GamepadState { leftStick = new Vector2(x, 0f) };
        foreach (GamepadButton botao in botoes) estado = estado.WithButton(botao);
        InputSystem.QueueStateEvent(controles[c], estado);
    }

    static void Teclas(params Key[] teclas) => InputSystem.QueueStateEvent(teclado, new KeyboardState(teclas));

    static IEnumerator Quadros(int quantos)
    {
        for (int i = 0; i < quantos; i++) yield return null;
    }

    // Aperta e solta os botões de um controle (2 quadros apertado, 2 solto).
    static IEnumerator Apertar(int c, params GamepadButton[] botoes)
    {
        Controle(c, 0f, botoes);
        yield return Quadros(2);
        Controle(c);
        yield return Quadros(2);
    }

    static IEnumerator ApertarTecla(Key tecla)
    {
        Teclas(tecla);
        yield return Quadros(2);
        Teclas();
        yield return Quadros(2);
    }

    // Espera até "condicao" valer (ou "limite" segundos). Devolve em "deu" se valeu.
    static IEnumerator Ate(System.Func<bool> condicao, float limite, System.Action<bool> deu)
    {
        float fim = Time.time + limite;
        while (!condicao() && Time.time < fim) yield return null;
        deu(condicao());
    }

    static IEnumerator Foto(string nome)
    {
        // (sem janela, no -batchmode, o quadro nunca "termina de desenhar": aí não tem foto)
        if (string.IsNullOrEmpty(pastaDasFotos) || Application.isBatchMode) yield break;
        yield return new WaitForEndOfFrame();
        try { ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(pastaDasFotos, nome + ".png")); }
        catch (System.Exception e) { Debug.Log("[TesteCoop] (sem foto: " + e.Message + ")"); }
        yield return Quadros(2);
    }

    // Uma fase reta de 90 blocos: chão, o começo na esquerda e a bandeira lá na direita. Nada mata.
    static Fase FaseReta()
    {
        const int largura = 90;
        var mapa = new string[15];
        for (int linha = 0; linha < 15; linha++)
        {
            var celulas = new char[largura];
            for (int x = 0; x < largura; x++) celulas[x] = linha >= 13 ? '#' : ' ';
            if (linha == 12) { celulas[3] = 'P'; celulas[45] = 's'; celulas[86] = 'G'; }
            mapa[linha] = new string(celulas);
        }
        return new Fase { nome = "Teste do coop", mapa = mapa };
    }

    // ------------------------------------------------------------------ o roteiro

    static IEnumerator Roteiro()
    {
        bool deu = false;
        yield return Quadros(5);
        Debug.Log("[TesteCoop] começou");
        if (System.Environment.GetEnvironmentVariable("CILADA_SO_VITORIA") == "1")
        {
            // rodada rápida, só para fotografar a tela de vitória com o "E agora?" (precisa da janela aberta)
            Estado(EstadoDoJogo.Vitoria);
            yield return new WaitForSeconds(1f);
            yield return Foto("7_vitoria");
            Conferir(Jogo.Estado == EstadoDoJogo.Vitoria, "a tela de vitória abriu");
            Terminar();
            yield break;
        }

        // controles de mentira (e o jogo ouvindo eles mesmo sem a janela em foco)
        focoAntes = InputSystem.settings.backgroundBehavior;
        entradaAntes = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        controles.Clear();
        for (int i = 0; i < 4; i++) controles.Add(InputSystem.AddDevice<Gamepad>("ControleDeTeste" + (i + 1)));
        teclado = InputSystem.AddDevice<Keyboard>("TecladoDeTeste");
        excecoes = 0;
        primeiraExcecao = "";
        Application.logMessageReceived -= NotarExcecao;
        Application.logMessageReceived += NotarExcecao;
        faseDeVerdade = Fases.Todas[0];
        Fases.Todas[0] = FaseReta();
        yield return Quadros(3);

        // ---------------------------------------------------------------- a tela da Tripulação
        Conferir(Jogo.Estado == EstadoDoJogo.Titulo, "o jogo abre no título: " + Jogo.Estado);
        Conferir(Application.productName == "Cilada à Vista!", "o nome do produto é o do jogo: " + Application.productName);
        Conferir(Jogo.opcoesDoMenu.Contains(GerenciadorDoJogo.OpcaoDoMenu.Tempestade) == Conquistas.Tem("zerou"),
            "o MODO TEMPESTADE só aparece no menu para quem já zerou (zerou: " + Conquistas.Tem("zerou") + ")");
        yield return Foto("0_titulo");
        Conferir(!Tripulacao.Coop && Tripulacao.Quantos == 1, "começa com um jogador só");
        Estado(EstadoDoJogo.Mapa); // (o mapa já está montado atrás do título)
        yield return Quadros(2);
        yield return Apertar(0, GamepadButton.North); // Y abre a Tripulação
        if (!Tripulacao.LobbyAberto)
        {
            Debug.Log("[TesteCoop] (o Y não abriu a Tripulação: tem fala ou tutorial no mapa; abrindo direto)");
            Chamar("AbrirTripulacao");
        }
        Conferir(Tripulacao.LobbyAberto && Tripulacao.NoLobby == 0, "a Tripulação abre vazia: " + Tripulacao.NoLobby);
        yield return Foto("1_tripulacao_vazia");
        yield return Apertar(0, GamepadButton.South);
        yield return Apertar(1, GamepadButton.South);
        yield return Apertar(2, GamepadButton.South);
        yield return ApertarTecla(Key.W);
        Conferir(Tripulacao.NoLobby == 4, "3 controles e o teclado entraram: " + Tripulacao.NoLobby);
        yield return Apertar(3, GamepadButton.South);
        Conferir(Tripulacao.NoLobby == 4, "o 5º não entra (máximo 4): " + Tripulacao.NoLobby);
        yield return Apertar(0, GamepadButton.South);
        Conferir(Tripulacao.NoLobby == 4, "quem já entrou não entra duas vezes");
        Conferir(Tripulacao.Do(3).esquema == Tripulacao.Esquema.TecladoWASD && Tripulacao.Do(3).tecladoInteiro, "o único no teclado fica com o teclado inteiro");
        yield return Foto("2_tripulacao_cheia");
        yield return ApertarTecla(Key.Q);
        Conferir(Tripulacao.NoLobby == 3, "Q tira o teclado: " + Tripulacao.NoLobby);
        yield return Apertar(2, GamepadButton.East);
        Conferir(Tripulacao.NoLobby == 2, "B tira o controle 3: " + Tripulacao.NoLobby);
        yield return ApertarTecla(Key.W);
        yield return ApertarTecla(Key.UpArrow);
        Conferir(Tripulacao.NoLobby == 4 && !Tripulacao.Do(2).tecladoInteiro && Tripulacao.Do(3).esquema == Tripulacao.Esquema.TecladoSetas,
            "dois no teclado: um no WASD e outro nas setas");
        Teclas(Key.D);
        yield return Quadros(2);
        Conferir(Tripulacao.Do(2).Horizontal() > 0f && Tripulacao.Do(3).Horizontal() == 0f, "D anda só o do WASD");
        Teclas(Key.LeftArrow);
        yield return Quadros(2);
        Conferir(Tripulacao.Do(2).Horizontal() == 0f && Tripulacao.Do(3).Horizontal() < 0f, "a seta anda só o das setas");
        Teclas();
        yield return ApertarTecla(Key.Q);
        yield return ApertarTecla(Key.RightCtrl);
        Conferir(Tripulacao.NoLobby == 2, "os dois do teclado saíram: " + Tripulacao.NoLobby);
        yield return Apertar(0, GamepadButton.Start); // START fecha
        Conferir(!Tripulacao.LobbyAberto && Tripulacao.Coop && Tripulacao.Quantos == 2, "fechou com 2 jogadores (coop ligado)");
        Conferir(Jogo.Estado == EstadoDoJogo.Mapa, "fechar a Tripulação não entra na fase: " + Jogo.Estado);

        // ---------------------------------------------------------------- as dicas de tecla acompanham o aparelho em uso
        yield return Apertar(0, GamepadButton.LeftShoulder);
        Conferir(Controles.UsandoControle, "apertou um botão do controle: as dicas passam a ser de controle");
        string dica = DicasDoControle.Trocar("SETAS: andar     ENTER: jogar     L: loja     T: coop     ESC: título");
        Conferir(dica == "DIRECIONAL: andar     A: jogar     X: loja     Y: coop     START: título", "a dica do mapa no controle: " + dica);
        dica = DicasDoControle.Trocar("ARROWS or MOUSE: choose    ENTER: confirm    ESC: back");
        Conferir(dica == "D-PAD: choose    A: confirm    B: back", "a dica do menu, em inglês: " + dica);
        Conferir(DicasDoControle.Trocar("ESCUDO comprado! ESCOLHA a fase.") == "ESCUDO comprado! ESCOLHA a fase.", "palavras com ESC no meio não mudam");
        yield return ApertarTecla(Key.LeftShift);
        Conferir(!Controles.UsandoControle, "apertou uma tecla: as dicas voltam a ser de teclado");
        Conferir(Controles.AtalhosDeTeste, "no editor os atalhos de teste (F7 a F10) existem");

        // ---------------------------------------------------------------- a Loja no coop: os itens são de cada um
        FieldInfo campoDasMoedas = typeof(GerenciadorDoJogo).GetField("moedas", BindingFlags.NonPublic | BindingFlags.Instance);
        campoDasMoedas.SetValue(Jogo, 100);
        Chamar("AbrirLoja");
        Conferir(Jogo.LojaAberta && Jogo.Comprador == 0, "a Loja abre comprando para o P1");
        yield return ApertarTecla(Key.Tab);
        Conferir(Jogo.Comprador == 1, "TAB troca o comprador para o P2: " + Jogo.Comprador);
        Chamar("ComprarEscudo");
        Conferir(Jogo.EscudosDe(1) == 1 && Jogo.EscudosDe(0) == 0, $"o escudo foi só para o P2 (P1 {Jogo.EscudosDe(0)}, P2 {Jogo.EscudosDe(1)})");
        Conferir(Jogo.Moedas == 100 - Jogo.precoDoEscudo, "as moedas saíram do bolso da tripulação: " + Jogo.Moedas);
        yield return Foto("2b_loja_do_coop");

        // ---------------------------------------------------------------- os jogos de habilidade (fichas, sem aposta)
        Chamar("AbrirJogos");
        Conferir(Jogo.JogosAbertos && Jogo.Fichas == Jogo.fichasIniciais, "os jogos abrem com as fichas iniciais: " + Jogo.Fichas);
        yield return Foto("2c_jogos_do_louro");
        int moedasAntesDosJogos = Jogo.Moedas;
        yield return ApertarTecla(Key.Enter);
        Conferir(Jogo.Rodada == GerenciadorDoJogo.FaseDaRodada.Valendo && Jogo.Fichas == Jogo.fichasIniciais - 1 && Jogo.Moedas == moedasAntesDosJogos,
            "ENTER começa a pontaria gastando 1 ficha (e nenhuma moeda)");
        float ponteiroAntes = Jogo.Ponteiro;
        yield return Quadros(3);
        Conferir(Jogo.Ponteiro != ponteiroAntes, "o ponteiro anda");
        yield return Ate(() => Mathf.Abs(Jogo.Ponteiro - 0.5f) < 0.02f, 5f, v => deu = v);
        Chamar("PararOPonteiro"); // (parado bem no verde; a tecla é conferida no reflexo, logo abaixo)
        Conferir(deu && Jogo.UltimoPremioDoJogo == Jogo.premioEmCheio && Jogo.Moedas == moedasAntesDosJogos + Jogo.premioEmCheio && Jogo.AcertosSeguidos == 1,
            "parar no verde paga o prêmio cheio: +" + Jogo.UltimoPremioDoJogo);
        yield return Foto("2d_pontaria_no_verde");

        yield return ApertarTecla(Key.RightArrow);
        Conferir(Jogo.JogoEscolhido == GerenciadorDoJogo.JogoDeHabilidade.Reflexo, "a seta troca para o REFLEXO");
        yield return ApertarTecla(Key.Enter);
        yield return Ate(() => Jogo.SinalDoReflexo, 5f, v => deu = v);
        Teclas(Key.Enter);
        yield return Ate(() => Jogo.Rodada == GerenciadorDoJogo.FaseDaRodada.Resultado, 2f, v => deu = v);
        Teclas();
        yield return Quadros(2);
        Conferir(deu && Jogo.TempoDeReacao > 0f && Jogo.UltimoPremioDoJogo > 0, $"apertar logo depois do JÁ paga (reagiu em {Jogo.TempoDeReacao:0.000} s, +{Jogo.UltimoPremioDoJogo})");
        int moedasAntesDeQueimar = Jogo.Moedas;
        yield return ApertarTecla(Key.Enter); // começa...
        yield return ApertarTecla(Key.Enter); // ...e aperta antes do JÁ
        Conferir(Jogo.Rodada == GerenciadorDoJogo.FaseDaRodada.Resultado && Jogo.TempoDeReacao < 0f && Jogo.Moedas == moedasAntesDeQueimar,
            "apertar antes do JÁ queima a largada (sem prêmio)");
        Conferir(Jogo.Fichas == 0, "as 3 fichas acabaram: " + Jogo.Fichas);
        yield return ApertarTecla(Key.Enter);
        Conferir(Jogo.Rodada != GerenciadorDoJogo.FaseDaRodada.Valendo && Jogo.Moedas == moedasAntesDeQueimar, "sem ficha não começa outra rodada");
        yield return ApertarTecla(Key.Escape);
        Conferir(!Jogo.JogosAbertos && Jogo.LojaAberta, "ESC volta dos jogos para a Loja");
        Chamar("FecharLoja");

        // ---------------------------------------------------------------- a fase, com 2 jogadores
        Chamar("JogarFase", 0);
        yield return Quadros(5);
        Conferir(Jogo.Estado == EstadoDoJogo.Jogando && GerenciadorDoJogo.Jogadores.Count == 2, "a fase começa com 2 Grumetes: " + GerenciadorDoJogo.Jogadores.Count);
        yield return Ate(() => P(0).NoChao && P(1).NoChao, 2f, v => deu = v);
        Conferir(deu, "os dois pousam no chão (um não empurra o outro)");
        Conferir(Mathf.Abs(P(0).transform.position.x - P(1).transform.position.x) < 0.05f, "nascem no mesmo lugar e não se empurram");
        int mortesAntes = Jogo.Mortes;

        // a câmera vai com quem corre; quem fica para trás vira balão (sem contar morte)
        float xDaCamera = Camera.main.transform.position.x;
        Controle(0, 1f);
        yield return Ate(() => P(1).NaBolha, 8f, v => deu = v);
        Conferir(deu, "P2 parado enquanto P1 corre: P2 saiu da tela e virou balão");
        Conferir(Camera.main.transform.position.x > xDaCamera + 3f, "a câmera foi com o P1: " + xDaCamera + " -> " + Camera.main.transform.position.x);
        Conferir(Jogo.Mortes == mortesAntes, "ficar para trás não conta morte");
        Conferir(GerenciadorDoJogo.JogadorAtual == P(0), "o líder é o P1");
        Controle(0);
        yield return Foto("3_p2_no_balao");

        // o balão vem boiando até ficar em cima do P1
        yield return Ate(() => Vector2.Distance(P(1).transform.position, (Vector2)P(0).transform.position + Vector2.up * 1.9f) < 0.5f, 12f, v => deu = v);
        Conferir(deu, "o balão chegou em cima da cabeça do P1 (distância " + Vector2.Distance(P(1).transform.position, P(0).transform.position) + ")");
        yield return Quadros(30);
        yield return Foto("3b_balao_em_cima");
        Conferir(P(1).NaBolha, "parado embaixo dele o balão NÃO estoura sozinho");
        float meiaLargura = Camera.main.orthographicSize * Camera.main.aspect;
        Conferir(Mathf.Abs(P(1).transform.position.x - Camera.main.transform.position.x) < meiaLargura, "o balão está dentro da tela");

        // P1 pula e estoura
        Controle(0, 0f, GamepadButton.South);
        yield return Ate(() => !P(1).NaBolha, 1.5f, v => deu = v);
        Controle(0);
        Conferir(deu && !P(1).Morto, "P1 pulou e estourou o balão: P2 voltou ao jogo");
        Conferir(Tripulacao.Do(0).resgates == 1, "o resgate contou para o P1: " + Tripulacao.Do(0).resgates);
        Conferir(P(1).Invencivel, "P2 sai do balão piscando (sem poder morrer)");
        Controle(1, 1f); // (sai de cima do P1: o pulo na cabeça não tem limite, ele ficaria quicando ali)
        yield return new WaitForSeconds(0.5f);
        Controle(1);
        yield return Ate(() => P(0).NoChao && P(1).NoChao && !P(1).Invencivel, 4f, v => deu = v);
        Conferir(deu, "os dois de volta no chão");

        // o escudo é de cada um: o do P2 salva o P2 (e o P1 continua sem nenhum)
        P(1).Morrer();
        Conferir(!P(1).Morto && Jogo.EscudosDe(1) == 0 && Jogo.Mortes == mortesAntes, "o escudo do P2 salvou o P2 (e foi gasto)");
        yield return Ate(() => P(1).NoChao && !P(1).Invencivel, 4f, v => deu = v);

        // morrer com um companheiro vivo: vira balão, a morte conta, a fase continua
        P(1).Morrer();
        Conferir(P(1).Morto && Jogo.Estado == EstadoDoJogo.Jogando, "P2 morreu e a fase continua: " + Jogo.Estado);
        Conferir(Jogo.Mortes == mortesAntes + 1 && Tripulacao.Do(1).mortes == 1, "a morte contou (total e do P2)");
        Conferir(Tripulacao.Do(1).mortesNaPartida == 1 && Tripulacao.Do(0).mortesNaPartida == 0, "a contagem de mortes é de cada jogador (P1 0, P2 1)");
        Conferir(BandeirasDeMorto() == 1 && BandeirasDeMorto("bandeira_de_morto_2") == 1, "a bandeira de morto do P2 (azul) foi fincada na hora: " + BandeirasDeMorto());
        yield return Ate(() => P(1).NaBolha, 2.5f, v => deu = v);
        Conferir(deu, "depois da animação da morte, P2 reaparece no balão");

        // o último vivo morre: a tripulação perdeu e todo mundo volta para o save
        P(0).Morrer();
        Conferir(Jogo.Estado == EstadoDoJogo.Morreu, "P1 (o último vivo) morreu: tela de morte: " + Jogo.Estado);
        yield return Ate(() => Jogo.Estado == EstadoDoJogo.Jogando, 5f, v => deu = v);
        yield return Quadros(3);
        Conferir(deu && GerenciadorDoJogo.Jogadores.Count == 2 && !P(0).Morto && !P(1).Morto, "renasceram os dois");
        Conferir(Jogo.MortesNaFase == 2, "2 mortes na fase: " + Jogo.MortesNaFase);
        Conferir(BandeirasDeMorto() == 2 && BandeirasDeMorto("bandeira_de_morto") == 1 && BandeirasDeMorto("bandeira_de_morto_2") == 1,
            "depois de renascer, cada um tem a sua bandeira de morto (a vermelha do P1 e a azul do P2): " + BandeirasDeMorto());
        Conferir(Tripulacao.Do(0).mortesNaPartida == 1 && Tripulacao.Do(1).mortesNaPartida == 1, "contagem por jogador: P1 1, P2 1");
        yield return Ate(() => P(0).NoChao && P(1).NoChao, 2f, v => deu = v);

        // pulo na cabeça do companheiro
        P(1).Teletransportar(P(0).transform.position + Vector3.up * 2.5f);
        yield return Ate(() => P(1).Corpo.linearVelocity.y > 3f, 2f, v => deu = v);
        Conferir(deu, "P2 caiu na cabeça do P1 e quicou (vy = " + P(1).Corpo.linearVelocity.y + ")");
        // sem limite: parado em cima do P1, ele quica de novo toda vez que cai
        int quiques = 1;
        bool subindo = true;
        yield return Ate(() =>
        {
            float vy = P(1).Corpo.linearVelocity.y;
            if (subindo && vy < -1f) subindo = false;
            else if (!subindo && vy > 3f) { subindo = true; quiques++; }
            return quiques >= 4;
        }, 6f, v => deu = v);
        Conferir(deu, "o pulo na cabeça não tem limite: quicou " + quiques + " vezes seguidas");

        // segurando PULAR, o pulo na cabeça sai mais alto que o pulo normal
        P(1).Teletransportar(P(0).transform.position + Vector3.right * 4f); // (longe do P1: um pulo normal, sem cabeça embaixo)
        yield return new WaitForSeconds(0.3f);
        float chao = P(1).transform.position.y;
        Controle(1, 0f, GamepadButton.South);
        yield return new WaitForSeconds(0.15f);
        float topoNormal = chao;
        yield return Ate(() => { topoNormal = Mathf.Max(topoNormal, P(1).transform.position.y); return P(1).NoChao && topoNormal > chao + 1f; }, 3f, v => deu = v);
        Controle(1);
        yield return new WaitForSeconds(1.3f); // (o quique fraco de antes ainda está "descansando")
        P(1).Teletransportar(P(0).transform.position + Vector3.up * 2.5f);
        Controle(1, 0f, GamepadButton.South);
        float topoNaCabeca = chao;
        yield return new WaitForSeconds(0.15f); // (a física ainda não sabe que ele saiu do chão)
        yield return Ate(() => { topoNaCabeca = Mathf.Max(topoNaCabeca, P(1).transform.position.y); return P(1).NoChao && topoNaCabeca > chao + 1f; }, 4f, v => deu = v);
        Controle(1);
        Conferir(topoNaCabeca > topoNormal + 0.5f, $"pulo na cabeça segurando PULAR é mais alto: {topoNaCabeca - chao:0.00} blocos contra {topoNormal - chao:0.00} do pulo normal");
        yield return Ate(() => P(0).NoChao && P(1).NoChao, 3f, v => deu = v);

        // empurrão: P1 (olhando para a direita) joga o P2 para longe
        P(1).Teletransportar(P(0).transform.position + Vector3.right * 0.8f);
        yield return Quadros(4);
        float xAntes = P(1).transform.position.x;
        yield return Apertar(0, GamepadButton.West);
        yield return new WaitForSeconds(0.5f);
        Conferir(P(1).transform.position.x > xAntes + 1f, "o empurrão jogou o P2 para a frente: " + (P(1).transform.position.x - xAntes) + " blocos");
        Conferir(Tripulacao.Do(0).empurroes == 1, "o empurrão contou para o P1");
        yield return Ate(() => P(0).NoChao && P(1).NoChao, 3f, v => deu = v);

        // virar balão de propósito (segurando B)
        Controle(1, 0f, GamepadButton.East);
        yield return Ate(() => P(1).NaBolha, 2f, v => deu = v);
        Controle(1);
        Conferir(deu && Jogo.MortesNaFase == 2, "segurando B o P2 vira balão (sem contar morte)");
        yield return Quadros(60); // (o balão recém-criado ainda não estoura)
        // o P1 não vira balão sozinho: não tem ninguém para estourar
        Controle(0, 0f, GamepadButton.East);
        yield return new WaitForSeconds(0.8f);
        Controle(0);
        Conferir(!P(0).NaBolha, "o último vivo não pode virar balão de propósito");

        // R: a tripulação inteira volta para o save, contando UMA morte
        Controle(0, 0f, GamepadButton.North);
        yield return Quadros(2);
        Controle(0);
        Conferir(Jogo.Estado == EstadoDoJogo.Morreu && Jogo.MortesNaFase == 3, "Y (reiniciar) conta uma morte só: " + Jogo.MortesNaFase + " / " + Jogo.Estado);
        Conferir(Tripulacao.Do(0).mortesNaPartida == 1 && Tripulacao.Do(1).mortesNaPartida == 1, "o reinício não entra na conta de nenhum jogador");
        yield return Ate(() => Jogo.Estado == EstadoDoJogo.Jogando, 5f, v => deu = v);
        yield return Quadros(3);
        yield return Ate(() => P(0).NoChao && P(1).NoChao, 2f, v => deu = v);

        // a moeda conta para quem pegou, e o fim da fase dá os títulos
        Jogo.GanharMoeda(new Vector3(40f, 5f, 0f), P(1));
        yield return Foto("4_dois_na_fase");
        Jogo.FaseConcluida();
        Conferir(Jogo.Estado == EstadoDoJogo.FaseConcluida, "fase concluída");
        Conferir(Jogo.Fichas == Jogo.fichasPorFase, "passar de fase dá fichas para os jogos da Loja: " + Jogo.Fichas);
        var titulos = new List<string>();
        foreach (GerenciadorDoJogo.TituloDaTripulacao titulo in Jogo.TitulosDaTripulacao)
            titulos.Add($"{Tripulacao.Nome(titulo.jogador)} {titulo.nome} ({titulo.motivo})");
        Conferir(Jogo.TitulosDaTripulacao.Count == 4, "os 4 títulos saíram: " + string.Join(" | ", titulos));
        yield return new WaitForSeconds(1.2f);
        yield return Foto("5_fim_da_fase");

        // ---------------------------------------------------------------- a Gruta da Cozinheira (a fase de verdade), a dois
        Chamar("JogarFase", 6);
        yield return Quadros(5);
        var cozinheira = Jogo.Chefe as ChefeCozinheira;
        Conferir(Jogo.Estado == EstadoDoJogo.Jogando && cozinheira != null, "a Gruta carrega com a chefe: " + Fases.Todas[6].nome);
        Conferir(Object.FindObjectsByType<PontoDeSave>(FindObjectsSortMode.None).Length == 2, "a Gruta tem 2 pontos de save (o novo, depois das 3 cozinheiras)");
        float alturaDaDoLeque = -1f;
        foreach (FacasDaCozinheira facas in Object.FindObjectsByType<FacasDaCozinheira>(FindObjectsSortMode.None))
            if (facas.leque) alturaDaDoLeque = facas.transform.position.y;
        Conferir(Mathf.Approximately(alturaDaDoLeque, 9f), "a cozinheira do leque subiu 3 blocos (altura 9): " + alturaDaDoLeque);
        yield return Ate(() => P(0).NoChao && P(1).NoChao, 2f, v => deu = v);

        // um entra na arena e o outro fica lá no começo da fase: a parede fecha e ele é puxado para dentro
        float esquerda = (float)Campo(cozinheira, "esquerda"), direita = (float)Campo(cozinheira, "direita"), chaoDaArena = (float)Campo(cozinheira, "chao");
        P(0).Teletransportar(new Vector3(esquerda + 3f, chaoDaArena + 0.6f, 0f));
        yield return Ate(() => cozinheira.EmCombate, 2f, v => deu = v);
        yield return Quadros(2);
        Conferir(deu && P(1).transform.position.x > esquerda && P(1).transform.position.x < direita,
            "a arena fechou e o P2, que tinha ficado no começo da fase, foi puxado para dentro (x = " + P(1).transform.position.x + ")");

        // parado em cima do degrau, o corte rente agora pega (o P1 fica congelado: nada mata ele durante o teste)
        P(0).Teletransportar(new Vector3(esquerda + 15f, chaoDaArena + 0.6f, 0f)); // (longe do degrau do P2)
        P(0).Congelar();
        P(1).Teletransportar(new Vector3(esquerda + 4.5f, chaoDaArena + 2.6f, 0f));
        yield return new WaitForSeconds(0.4f);
        Conferir(P(1).NoChao && P(1).transform.position.y > chaoDaArena + 2.3f, "P2 está em pé em cima do degrau");
        MethodInfo corte = typeof(ChefeCozinheira).GetMethod("Corte", BindingFlags.NonPublic | BindingFlags.Instance);
        cozinheira.StartCoroutine((IEnumerator)corte.Invoke(cozinheira, new object[] { false, true }));
        yield return Ate(() => P(1).Morto, 2.5f, v => deu = v);
        Conferir(deu, "o corte rente passou também em cima do degrau e pegou o P2 (antes, ali era seguro)");

        // etapa 3: no breu, CADA Grumete tem o seu círculo de luz (o P2 já voltou, pendurado no balão)
        yield return Ate(() => P(1).NaBolha, 2.5f, v => deu = v);
        typeof(ChefeCozinheira).GetMethod("CriarEscuridao", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(cozinheira, null);
        yield return Quadros(3);
        var escuridao = cozinheira.GetComponentInChildren<EscuridaoDaTripulacao>();
        Conferir(escuridao != null, "a escuridão da etapa 3 foi criada");
        if (escuridao != null)
        {
            byte noP1 = escuridao.AlfaEm(P(0).transform.position), noP2 = escuridao.AlfaEm(P(1).transform.position);
            Vector2 longe = ((Vector2)P(0).transform.position + (Vector2)P(1).transform.position) / 2f + Vector2.up * 6f;
            Conferir(noP1 == 0 && noP2 == 0, $"cada jogador tem luz em volta (escuridão no P1 {noP1}, no P2 {noP2})");
            Conferir(Vector2.Distance(P(0).transform.position, P(1).transform.position) > 3f, "os dois estão longe um do outro (são dois círculos de luz)");
            Conferir(escuridao.AlfaEm(longe) == 255, "longe dos dois continua um breu");
        }
        yield return Foto("6_escuridao_com_duas_luzes");
        P(0).Liberar();

        // morrer muitas vezes no chefe NÃO dá ajuda nenhuma: a graça do jogo é ser difícil (não tem modo fácil)
        for (int morte = 0; morte < 7; morte++)
        {
            Chamar("ReiniciarDoSave");
            yield return Ate(() => Jogo.Estado == EstadoDoJogo.Jogando && !P(0).Morto, 5f, v => deu = v);
            yield return Quadros(2);
        }
        Conferir(deu && Jogo.EscudosDe(0) == 0 && Jogo.EscudosDe(1) == 0, "7 mortes seguidas no chefe e ninguém ganhou escudo de graça");

        // ---------------------------------------------------------------- os outros três chefes, a dois: a arena e 9 s de luta
        foreach (int faseDoChefe in new[] { 3, 9, 12 })
        {
            Chamar("JogarFase", faseDoChefe);
            yield return Quadros(5);
            Chefe chefe = Jogo.Chefe;
            string nomeDoChefe = Fases.Todas[faseDoChefe].nome;
            Conferir(chefe != null && Jogo.Estado == EstadoDoJogo.Jogando, nomeDoChefe + ": a fase carrega com o chefe");
            if (chefe == null) continue;
            yield return Ate(() => P(0).NoChao && P(1).NoChao, 2f, v => deu = v);
            float esq = (float)Campo(chefe, "esquerda"), dir = (float)Campo(chefe, "direita"), piso = (float)Campo(chefe, "chao");
            int excecoesAntes = excecoes;
            P(0).Teletransportar(new Vector3(esq + 3f, piso + 0.6f, 0f));
            yield return Ate(() => chefe.EmCombate, 3f, v => deu = v);
            yield return Quadros(2);
            Conferir(deu && P(1).transform.position.x > esq && P(1).transform.position.x < dir,
                nomeDoChefe + ": a arena fechou e puxou o P2 para dentro (x = " + P(1).transform.position.x + ")");
            P(0).Congelar(); // (congelados, nada mata: é só para ver a luta rodar com dois alvos)
            P(1).Teletransportar(new Vector3(esq + 9f, piso + 0.6f, 0f));
            P(1).Congelar();
            yield return new WaitForSeconds(9f);
            Conferir(excecoes == excecoesAntes && chefe != null && chefe.EmCombate, nomeDoChefe + ": 9 s de luta a dois sem nenhum erro" + (excecoes == excecoesAntes ? "" : " -> " + primeiraExcecao));
            Conferir(GerenciadorDoJogo.AlvoDoChefe(out Vector2 alvoDaVez) && (Vector2.Distance(alvoDaVez, P(0).transform.position) < 0.1f || Vector2.Distance(alvoDaVez, P(1).transform.position) < 0.1f),
                nomeDoChefe + ": o alvo da vez é um dos dois Grumetes");
        }

        // ---------------------------------------------------------------- sozinho continua igual
        Tripulacao.Definir(new Tripulacao.Marujo());
        Chamar("JogarFase", 0);
        yield return Quadros(5);
        Conferir(GerenciadorDoJogo.Jogadores.Count == 1 && !Tripulacao.Coop, "sozinho: um Grumete só");
        yield return Ate(() => P(0).NoChao, 2f, v => deu = v);
        float xSolo = P(0).transform.position.x;
        Controle(0, 1f);
        yield return new WaitForSeconds(0.6f);
        Controle(0);
        Conferir(P(0).transform.position.x > xSolo + 2f, "sozinho: qualquer controle anda o Grumete");
        Teclas(Key.RightArrow);
        xSolo = P(0).transform.position.x;
        yield return new WaitForSeconds(0.4f);
        Teclas();
        Conferir(P(0).transform.position.x > xSolo + 1f, "sozinho: as setas também andam");
        Controle(0, 0f, GamepadButton.East);
        yield return new WaitForSeconds(0.8f);
        Controle(0);
        Conferir(!P(0).NaBolha, "sozinho: não existe balão");
        P(0).Morrer();
        Conferir(Jogo.Estado == EstadoDoJogo.Morreu, "sozinho: morreu, tela de morte (como sempre)");
        yield return Ate(() => Jogo.Estado == EstadoDoJogo.Jogando, 5f, v => deu = v);
        Conferir(deu && GerenciadorDoJogo.Jogadores.Count == 1, "sozinho: renasce");

        // o ponto de save é FIXO: morrer várias vezes depois dele não tira o checkpoint
        yield return Ate(() => P(0).NoChao, 2f, v => deu = v);
        P(0).Teletransportar(new Vector3(45f, P(0).transform.position.y, 0f));
        yield return Ate(() => Jogo.PontoDeSave.HasValue, 2f, v => deu = v);
        Conferir(deu, "sozinho: encostou no ponto de save e ele ativou");
        for (int morte = 0; morte < 5; morte++)
        {
            yield return Ate(() => Jogo.Estado == EstadoDoJogo.Jogando && !P(0).Morto, 5f, v => deu = v);
            yield return Quadros(2);
            P(0).Morrer();
            yield return Ate(() => Jogo.Estado == EstadoDoJogo.Morreu, 1f, v => deu = v);
        }
        yield return Ate(() => Jogo.Estado == EstadoDoJogo.Jogando && !P(0).Morto, 5f, v => deu = v);
        yield return Quadros(2);
        Conferir(Jogo.PontoDeSave.HasValue && Mathf.Abs(P(0).transform.position.x - 45f) < 1.5f,
            "5 mortes depois, o save continua lá e o Grumete renasce nele (x = " + P(0).transform.position.x + ")");

        // ---------------------------------------------------------------- trocar as teclas (Opções > Teclas)
        yield return Ate(() => P(0).NoChao, 2f, v => deu = v);
        Conferir(global::Teclas.Proibida(Key.Enter) && global::Teclas.Proibida(Key.UpArrow) && !global::Teclas.Definir(global::Teclas.Acao.Pular, Key.Escape), "ENTER, ESC e as setas não podem ser escolhidas");
        Conferir(global::Teclas.Definir(global::Teclas.Acao.Pular, Key.K) && global::Teclas.De(global::Teclas.Acao.Pular) == Key.K, "o pulo foi trocado para a tecla K");
        Teclas(Key.W);
        yield return new WaitForSeconds(0.2f);
        Teclas();
        Conferir(P(0).NoChao, "depois da troca, o W não pula mais");
        Teclas(Key.K);
        yield return Ate(() => P(0).Corpo.linearVelocity.y > 3f, 1f, v => deu = v);
        Teclas();
        Conferir(deu, "a tecla K pula");
        global::Teclas.Definir(global::Teclas.Acao.Loja, Key.D); // (D era "andar para a direita": as duas trocam)
        Conferir(global::Teclas.De(global::Teclas.Acao.Loja) == Key.D && global::Teclas.De(global::Teclas.Acao.Direita) == Key.L, "escolher uma tecla em uso troca as duas ações de tecla");
        Conferir(global::Teclas.TrocarNasDicas("SETAS: andar     ENTER: jogar     L: loja     ESC: título") == "SETAS: andar     ENTER: jogar     D: loja     ESC: título",
            "a dica da tela mostra a tecla nova da Loja");
        global::Teclas.Restaurar();
        Conferir(!global::Teclas.Mudaram && global::Teclas.De(global::Teclas.Acao.Pular) == Key.W, "restaurar volta tudo ao padrão");

        // ---------------------------------------------------------------- o save em arquivo e os sons novos
        Guardado.SetInt("cilada_teste_do_arquivo", 42);
        Guardado.Save();
        Conferir(System.IO.File.Exists(Guardado.Caminho) && System.IO.File.ReadAllText(Guardado.Caminho).Contains("cilada_teste_do_arquivo"),
            "o save vai para um arquivo: " + Guardado.Caminho);
        Guardado.DeleteKey("cilada_teste_do_arquivo");
        Guardado.Save();
        Conferir(!Guardado.HasKey("cilada_teste_do_arquivo") && Guardado.GetInt("cilada_teste_do_arquivo", -1) == -1, "apagar uma chave tira ela do arquivo");
        var sons = (Dictionary<string, AudioClip>)Campo(Jogo, "sons");
        bool temOsSons = true;
        foreach (string som in new[] { "corrente", "ancora", "faca", "crava", "onda", "tic", "tac", "madeira" }) temOsSons &= sons.ContainsKey(som) && sons[som].samples > 0;
        Conferir(temOsSons, "os 8 sons próprios das mecânicas existem");
        Conferir(System.Array.Find(Conquistas.Todas, c => c.id == "save_mudou") == null, "a conquista do save que muda saiu da lista (" + Conquistas.Todas.Length + " conquistas)");

        // ---------------------------------------------------------------- a fase muda depois da primeira morte (fase 2 de verdade)
        Chamar("JogarFase", 1);
        yield return Quadros(5);
        int espinhosAntes = Object.FindObjectsByType<EspinhoEscondido>(FindObjectsSortMode.None).Length;
        Chamar("ReiniciarDoSave");
        yield return Ate(() => Jogo.Estado == EstadoDoJogo.Jogando && !P(0).Morto, 5f, v => deu = v);
        yield return Quadros(3);
        int espinhosDepois = Object.FindObjectsByType<EspinhoEscondido>(FindObjectsSortMode.None).Length;
        Conferir(deu && espinhosDepois == espinhosAntes + 1, $"fase 2: depois da primeira morte apareceu um espinho escondido a mais ({espinhosAntes} -> {espinhosDepois})");

        // ---------------------------------------------------------------- começar pelo MODO TEMPESTADE do menu (num slot de teste)
        typeof(GerenciadorDoJogo).GetField("novoNaTempestade", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(Jogo, true);
        Chamar("ComecarNoSlot", 2, true);
        yield return Ate(() => Jogo.Estado == EstadoDoJogo.Mapa, 4f, v => deu = v);
        yield return Quadros(3);
        Conferir(deu && Jogo.ModoDificil && Jogo.Moedas >= Jogo.moedasDoTesouro && Jogo.Fala.HasValue && Jogo.LojaLiberada,
            $"novo jogo pelo Modo Tempestade: cai no mapa, com a tempestade ligada, {Jogo.Moedas} moedas, a Loja aberta e a fala do Louro");
        Conferir(Jogo.EscudosDe(0) == 0 && Jogo.Mortes == 0, "o jogo novo da tempestade começa zerado (sem itens e sem mortes)");
        yield return Foto("8_mapa_na_tempestade");

        Conferir(excecoes == 0, "nenhum erro no Console durante o roteiro inteiro" + (excecoes == 0 ? "" : $" ({excecoes}): {primeiraExcecao}"));

        // (só para a foto: a tela de vitória com o "E agora?")
        if (!string.IsNullOrEmpty(pastaDasFotos) && !Application.isBatchMode)
        {
            Estado(EstadoDoJogo.Vitoria);
            yield return Quadros(3);
            yield return Foto("7_vitoria");
        }

        Terminar();
    }

    // Qualquer erro vermelho no Console durante o roteiro (uma exceção num chefe, por exemplo) é falha do teste.
    static int excecoes;
    static string primeiraExcecao = "";

    static void NotarExcecao(string mensagem, string pilha, LogType tipo)
    {
        if (tipo != LogType.Exception) return;
        if (excecoes++ == 0) primeiraExcecao = mensagem + " | " + pilha.Split('\n')[0];
    }

    static void Terminar()
    {
        if (terminou) return;
        terminou = true;
        Application.logMessageReceived -= NotarExcecao;
        if (faseDeVerdade != null) Fases.Todas[0] = faseDeVerdade;
        foreach (Gamepad controle in controles) InputSystem.RemoveDevice(controle);
        if (teclado != null) InputSystem.RemoveDevice(teclado);
        InputSystem.settings.backgroundBehavior = focoAntes;
        InputSystem.settings.editorInputBehaviorInPlayMode = entradaAntes;
        if (falhas == 0) Debug.Log($"[TesteCoop] TODOS OS {conferidos} TESTES PASSARAM");
        else Debug.LogError($"[TesteCoop] {falhas} de {conferidos} teste(s) falharam");
        EditorApplication.ExitPlaymode();
        if (!fecharNoFim) return;
        SessionState.SetBool(ChaveFechar, false);
        EditorApplication.update += FecharQuandoParar;
    }

    // Fecha o Unity depois que o Play parou de verdade (código de saída = número de falhas).
    static void FecharQuandoParar()
    {
        if (EditorApplication.isPlaying) return;
        EditorApplication.update -= FecharQuandoParar;
        EditorApplication.Exit(falhas);
    }
}
