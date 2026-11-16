// Teste automático do CONTEÚDO NOVO (16/11/2026): o Mar de Baixo (nado, ar, bolhas), o mapa do Mar, as fases bônus,
// o replay das mortes, o chefe do Mar, o chão que desliza e o fantasma do speedrun. Joga sozinho (um Grumete).
//
// Como rodar:
//  - no editor: menu "Cilada (teste)/Testar conteúdo novo" (com o jogo PARADO);
//  - pela linha de comando: Unity.exe -batchmode -projectPath <pasta> -executeMethod TesteDoConteudo.RodarEFechar -logFile conteudo.log
//    (sai com código 0 se tudo passou). O resultado fica nas linhas "[TesteConteudo]".
//
// ATENÇÃO: mexe no save do slot atual DO EDITOR (libera o Mar e uma fase bônus, grava recordes). Guarde o save antes.
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class TesteDoConteudo
{
    static int falhas, conferidos;
    static bool fecharNoFim, esperandoOPlay, terminou;
    static double inicio, horaDeComecar;
    const double TempoMaximo = 300;
    const string ChavePendente = "TesteDoConteudo.pendente", ChaveFechar = "TesteDoConteudo.fechar";

    [MenuItem("Cilada (teste)/Testar conteúdo novo")]
    public static void Rodar()
    {
        if (Application.isPlaying) { Debug.LogWarning("[TesteConteudo] pare o jogo antes de rodar o teste"); return; }
        falhas = conferidos = 0;
        esperandoOPlay = true;
        EditorApplication.update -= EsperarOPlay;
        EditorApplication.update += EsperarOPlay;
        EditorApplication.EnterPlaymode();
    }

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
            Debug.LogError("[TesteConteudo] FALHOU: os scripts recarregaram no meio do Play");
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
        Application.logMessageReceived -= NotarExcecao;
        Application.logMessageReceived += NotarExcecao;
        excecoes = 0;
        GerenciadorDoJogo.Instancia.StartCoroutine(Roteiro());
    }

    static int excecoes;
    static string primeiraExcecao = "";
    static void NotarExcecao(string texto, string pilha, LogType tipo)
    {
        if (tipo != LogType.Exception && tipo != LogType.Error) return;
        if (texto.StartsWith("[TesteConteudo]")) return;
        if (excecoes++ == 0) primeiraExcecao = texto + "\n" + pilha;
    }

    static void Vigiar()
    {
        if (terminou) { EditorApplication.update -= Vigiar; return; }
        if (EditorApplication.timeSinceStartup - inicio < TempoMaximo) return;
        falhas++;
        Debug.LogError("[TesteConteudo] FALHOU: o roteiro não terminou em " + TempoMaximo + " s");
        Terminar();
    }

    static void Conferir(bool ok, string oque)
    {
        conferidos++;
        if (ok) Debug.Log("[TesteConteudo] ok: " + oque);
        else { falhas++; Debug.LogError("[TesteConteudo] FALHOU: " + oque); }
    }

    static GerenciadorDoJogo Jogo => GerenciadorDoJogo.Instancia;
    static Jogador P => GerenciadorDoJogo.JogadorAtual;

    static object Chamar(string metodo, params object[] argumentos) =>
        typeof(GerenciadorDoJogo).GetMethod(metodo, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).Invoke(Jogo, argumentos);

    static void Estado(EstadoDoJogo estado) => typeof(GerenciadorDoJogo).GetProperty("Estado").SetValue(Jogo, estado);

    static IEnumerator Ate(System.Func<bool> condicao, float limite, System.Action<bool> deu)
    {
        float t = 0f;
        while (!condicao() && t < limite) { t += Time.deltaTime; yield return null; }
        deu(condicao());
    }

    // Leva o Grumete para a coluna x, na linha (do texto) r da fase atual.
    static void Por(float x, int linha) => P.Teletransportar(new Vector3(x, 15 - 1 - linha, 0f));

    static IEnumerator Roteiro()
    {
        bool deu = false;
        yield return null;
        Debug.Log("[TesteConteudo] começou");

        // ---------------------------------------------------------------- os índices das fases extras
        Conferir(Fases.Dados(Fases.PrimeiroDoMar).nome == "Mergulho de Cabeça", "a fase 200 é a primeira do Mar: " + Fases.Dados(Fases.PrimeiroDoMar).nome);
        Conferir(Fases.EhBonus(Fases.PrimeiroBonus) && Fases.OrigemDoBonus(Fases.PrimeiroBonus) == 5, "a bônus 1 sai da fase 6 (índice 5)");
        Conferir(Fases.MarDeBaixo.Length == 7 && Fases.Bonus.Length == 3, "7 fases no Mar e 3 bônus");
        Conferir(Fases.PrimeiroBonus != FabricaDeMusica.MusicaDoMapa, "o índice das bônus não bate com o da música do mapa");

        // ---------------------------------------------------------------- o mapa do Mar
        Progresso.LiberarMar();
        Chamar("MostrarMapa", Fases.PrimeiroDoMar, -1, false, false);
        Estado(EstadoDoJogo.Mapa);
        yield return null;
        Conferir(MapaDoMundo.MundoAtual == MapaDoMundo.QualMundo.MarDeBaixo && Jogo.Mapa.FaseSelecionada == Fases.PrimeiroDoMar,
            "o mapa do Mar abre no ponto 1 (fase " + Jogo.Mapa.FaseSelecionada + ")");
        Chamar("MostrarMapa", Fases.Todas.Length - 1, -1, false, false);
        yield return null;
        Conferir(MapaDoMundo.MundoAtual == MapaDoMundo.QualMundo.Ilhas, "voltando, o mapa é o das ilhas");

        // ---------------------------------------------------------------- o Mar 1: nadar e respirar
        Chamar("JogarFase", Fases.PrimeiroDoMar);
        yield return new WaitForSeconds(0.3f);
        Conferir(Jogo.Estado == EstadoDoJogo.Jogando && Jogo.NoMar, "entrou na fase do Mar: " + Jogo.DadosDaFase.nome);
        Por(24f, 11); // no meio do primeiro tanque
        yield return new WaitForSeconds(0.3f);
        Conferir(P.NaAgua && P.Submerso, "dentro do tanque o Grumete está na água e submerso");
        float arAntes = P.Ar;
        yield return new WaitForSeconds(1.5f);
        Conferir(P.Ar < arAntes - 1f, $"debaixo d'água o ar cai: {arAntes:0.0} -> {P.Ar:0.0}");
        float yNaAgua = P.transform.position.y;
        yield return new WaitForSeconds(1f);
        Conferir(P.transform.position.y > yNaAgua - 4f, "na água ele afunda devagar");
        // a bolha de ar enche o ar (a de verdade fica na coluna 25, linha 12)
        Por(25f, 12);
        yield return new WaitForSeconds(0.3f);
        Conferir(P.Ar > P.arMaximo - 0.5f, $"a bolha de ar encheu o ar: {P.Ar:0.0}");
        // fora d'água o ar volta
        Por(5f, 7);
        yield return new WaitForSeconds(1f);
        Conferir(!P.NaAgua && P.Ar >= P.arMaximo - 0.01f, "fora d'água o ar fica cheio");
        // afogar: fica no fundo do tanque até o ar acabar
        int mortes = Jogo.MortesNaFase;
        Por(18f, 13);
        yield return Ate(() => Jogo.MortesNaFase > mortes, 10f, v => deu = v);
        Conferir(deu, "sem ar, o Grumete se afoga (morte)");
        yield return Ate(() => Jogo.Estado == EstadoDoJogo.Jogando, 3f, v => deu = v);

        // ---------------------------------------------------------------- o replay das mortes
        yield return new WaitForSeconds(0.6f); // (grava um pedaço desta tentativa)
        Jogo.FaseConcluida();
        yield return null;
        Conferir(Jogo.MostrandoReplay && Object.FindAnyObjectByType<ReplayDasMortes>() != null, "morreu na fase: o fim da fase começa pelo replay das mortes");
        Conferir(GravadorDeTentativas.Tentativas >= 2, "o gravador guardou as tentativas: " + GravadorDeTentativas.Tentativas);
        yield return new WaitForSeconds(0.5f);
        // ENTER pula o replay: aqui, pula direto pelo tempo
        yield return Ate(() => !Jogo.MostrandoReplay, 12f, v => deu = v);
        Conferir(deu && Jogo.MostrandoPainel, "depois do replay vem o painel de tempos");

        // ---------------------------------------------------------------- o chefe do Mar (Tubarão-Martelo)
        Chamar("JogarFase", Fases.PrimeiroDoMar + 3);
        yield return new WaitForSeconds(0.3f);
        Conferir(Jogo.Chefe is ChefeMartelo, "a fase 4 do Mar tem o Tubarão-Martelo");
        Conferir(Jogo.Chefe != null && Jogo.Chefe.GolpesPorEtapa == 2 && Jogo.Chefe.VidaMaxima == 6, "sozinho: 2 golpes por etapa (vida 6)");
        Por(22f, 12);
        yield return Ate(() => Jogo.Chefe != null && Jogo.Chefe.EmCombate, 4f, v => deu = v);
        Conferir(deu, "entrando na arena, a luta começa");
        // fica parado no canto: algum ataque tem que pegar (o chefe ataca de verdade)
        mortes = Jogo.MortesNaFase;
        yield return Ate(() => Jogo.MortesNaFase > mortes, 25f, v => deu = v);
        Conferir(deu, "parado, o Grumete leva um ataque do Martelo (os ataques machucam)");
        yield return Ate(() => Jogo.Estado == EstadoDoJogo.Jogando, 3f, v => deu = v);
        Conferir(Jogo.EhPontoDeSave(Jogo.Chefe.PontoDeRenascer), "morreu na luta: renasce na entrada da arena");

        // ---------------------------------------------------------------- a bônus 3: o chão que desliza
        Progresso.LiberarBonus(2);
        Chamar("JogarFase", Fases.PrimeiroBonus + 2);
        yield return new WaitForSeconds(0.3f);
        // (a fase tem 3: o primeiro é o das colunas 20 a 22; os blocos são filhos, então compara pelo primeiro bloco)
        ChaoQueDesliza desliza = null;
        foreach (ChaoQueDesliza c in Object.FindObjectsByType<ChaoQueDesliza>(FindObjectsSortMode.None))
            if (c.transform.childCount > 0 && (desliza == null || c.transform.GetChild(0).position.x < desliza.transform.GetChild(0).position.x)) desliza = c;
        Conferir(desliza != null, "a bônus 3 tem o chão que desliza");
        Vector3 antes = desliza != null ? desliza.transform.position : Vector3.zero;
        Por(18.7f, 12);
        yield return new WaitForSeconds(1.2f);
        Conferir(desliza != null && desliza.transform.position.x > antes.x + 2.5f, "chegando perto, o chão desliza para a direita: " + (desliza != null ? (desliza.transform.position.x - antes.x).ToString("0.0") : "?"));

        // ---------------------------------------------------------------- o fantasma do speedrun
        bool speedrunAntes = Opcoes.Speedrun;
        Opcoes.Speedrun = true;
        Chamar("JogarFase", Fases.PrimeiroBonus);
        yield return new WaitForSeconds(1f);
        Jogo.FaseConcluida(); // passou (sem morrer: sem replay)
        yield return null;
        Conferir(!Jogo.MostrandoReplay, "sem morrer, não tem replay");
        Conferir(Speedrun.Carregar(Fases.PrimeiroBonus) != null, "o fantasma do recorde foi salvo");
        Chamar("JogarFase", Fases.PrimeiroBonus);
        yield return new WaitForSeconds(0.3f);
        Conferir(Object.FindAnyObjectByType<FantasmaDoRecorde>() != null, "com o speedrun ligado, o fantasma aparece na fase");
        Opcoes.Speedrun = speedrunAntes;

        // ---------------------------------------------------------------- nada de erro no caminho
        Conferir(excecoes == 0, "nenhum erro no Console durante o teste" + (excecoes > 0 ? ": " + primeiraExcecao : ""));
        Terminar();
    }

    static void Terminar()
    {
        if (terminou) return;
        terminou = true;
        Application.logMessageReceived -= NotarExcecao;
        if (falhas == 0) Debug.Log($"[TesteConteudo] TODOS OS {conferidos} TESTES PASSARAM");
        else Debug.LogError($"[TesteConteudo] {falhas} de {conferidos} teste(s) falharam");
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
