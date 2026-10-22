using System.Collections.Generic;
using UnityEngine;

// Tudo o que é desenhado "por cima" do jogo: HUD, título com menu, opções, como jogar, conquistas, mapa,
// pausa, tela de morte, fase concluída, vitória, créditos, falas dos personagens e a transição entre fases.
// Usa o OnGUI (IMGUI) do Unity. Os textos usam a fonte pixel de FabricaDeSprites.TextoEmPixel.
// Ela só LÊ o estado do GerenciadorDoJogo; quem decide as coisas é ele. A única "conversa" no outro
// sentido é o mouse: só a Interface sabe ONDE cada opção foi desenhada, então ela avisa o
// GerenciadorDoJogo quando o mouse passa por cima de uma opção ou clica nela (veja "mouse nos menus").
public class Interface : MonoBehaviour
{
    static readonly Color32 CorDoLogo = new Color32(255, 206, 72, 255);   // dourado (o logo)
    static readonly Color32 CorDaBordaDoLogo = new Color32(74, 40, 22, 255); // madeira escura (a borda do logo)
    static readonly Color32 Preto = new Color32(20, 20, 28, 255);
    static readonly Color32 Branco = new Color32(255, 255, 255, 255);
    static readonly Color32 Amarelo = new Color32(255, 217, 51, 255);
    static readonly Color32 Verde = new Color32(128, 255, 128, 255);
    static readonly Color32 Vermelho = new Color32(255, 102, 102, 255);
    static readonly Color32 Rosa = new Color32(255, 153, 255, 255);
    static readonly Color32 Lilas = new Color32(215, 170, 245, 255);
    static readonly Color32 CorDoRodape = new Color32(200, 190, 220, 255); // cinza-lilás: discreto

    GerenciadorDoJogo jogo;
    Texture2D fundoEscuro;
    float escala;

    // Textos do HUD guardados: só são montados de novo quando o número muda.
    // (montar "Mortes: " + número a cada quadro cria lixo na memória; veja FabricaDeSprites.TextoEmPixel)
    struct TextoComNumero { public int valor; public string texto; }
    TextoComNumero textoMortes, textoMoedas, textoTempo, textoDaFase, textoDoMapa, textoDoChefe, textoDaPagina;

    // Mouse nos menus (veja Mouse(), lá embaixo).
    Vector2 mouseAntes;
    bool mouseConhecido; // já sabemos onde o mouse estava? (no 1º quadro não dá para saber se ele "mexeu")
    bool mouseMexeu;     // o mouse se mexeu desde o último quadro? Só aí ele seleciona (senão brigaria com o teclado)
    bool usarMouse;      // falso com o painel F10 aberto (os cliques são dele)

    static string ComNumero(ref TextoComNumero guardado, int valor, System.Func<int, string> montar)
    {
        if (guardado.texto == null || guardado.valor != valor)
        {
            guardado.valor = valor;
            guardado.texto = montar(valor);
        }
        return guardado.texto;
    }

    void Awake() => jogo = GetComponent<GerenciadorDoJogo>();

    void OnGUI()
    {
        if (jogo == null) return;
        escala = Screen.height / 720f;
        PrepararEstilos();
        float w = Screen.width, h = Screen.height;
        EstadoDoJogo estado = jogo.Estado;

        // Primeiro a imagem do jogo (quando a câmera desenha em baixa resolução: veja TelaPixelada),
        // depois todo o resto por cima, nítido.
        TelaPixelada.DesenharNaTela();
        PrepararMouse();
        if (estado != EstadoDoJogo.Mapa) faseDoRecordeGuardado = -2; // ao voltar ao mapa, o recorde pode ter mudado
        bool submenu = jogo.SubmenuAberto != GerenciadorDoJogo.Submenu.Nenhum; // Opções ou Como Jogar abertos

        DesenharFalaDoLouro();
        DesenharDicaDasPortas();
        DesenharRetornoPelaMorte();
        bool lutando = jogo.Chefe != null && jogo.Chefe.EmCombate;
        if ((estado == EstadoDoJogo.Jogando || estado == EstadoDoJogo.Morreu || estado == EstadoDoJogo.FaseConcluida) && !lutando)
            DesenharBarraDeProgresso();
        if (lutando && estado != EstadoDoJogo.Titulo && !submenu) DesenharVidaDoChefe(jogo.Chefe);
        if (estado != EstadoDoJogo.Titulo && estado != EstadoDoJogo.Vitoria
            && estado != EstadoDoJogo.Conquistas && estado != EstadoDoJogo.Creditos && !submenu)
            DesenharHud();

        // (com uma caixa de fala aberta no alto, o aviso desce para baixo dela)
        if (estado == EstadoDoJogo.Jogando && jogo.Aviso != "")
            Texto(jogo.Aviso, w / 2f, PlacaPerto(out _, out _) ? 240f * escala : h * 0.22f, 3.5f, 0.5f, Rosa, w * 0.9f);

        switch (estado)
        {
            case EstadoDoJogo.Titulo:
                if (submenu) DesenharSubmenu();
                else DesenharTitulo();
                break;
            case EstadoDoJogo.Conquistas: DesenharConquistas(); break;
            case EstadoDoJogo.Creditos: DesenharCreditos(); break;
            case EstadoDoJogo.Mapa: DesenharMapa(); break;
            case EstadoDoJogo.Pausado:
                if (submenu) DesenharSubmenu();
                else DesenharPausa();
                break;
            case EstadoDoJogo.Morreu: DesenharMorte(); break;
            case EstadoDoJogo.FaseConcluida: DesenharFaseConcluida(); break;
            case EstadoDoJogo.Vitoria: DesenharVitoria(); break;
        }

        // botão direito do mouse = ESC (volta uma tela), em qualquer menu
        bool emMenu = estado == EstadoDoJogo.Titulo || estado == EstadoDoJogo.Pausado
                      || estado == EstadoDoJogo.Conquistas || estado == EstadoDoJogo.Creditos;
        Event evento = Event.current;
        if (usarMouse && emMenu && evento.type == EventType.MouseDown && evento.button == 1)
        {
            jogo.MouseClicou(GerenciadorDoJogo.BotaoVoltar, 0);
            evento.Use();
        }

        DesenharAvisoDeConquista();
        DesenharTransicao(); // por cima de tudo
        PainelDeGraficos.Desenhar(); // o painel de testes do visual (F10), por cima até da transição
    }

    void PrepararEstilos()
    {
        if (fundoEscuro == null)
        {
            fundoEscuro = new Texture2D(1, 1);
            fundoEscuro.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
            fundoEscuro.Apply();
        }
    }

    // ------------------------------------------------------------------ telas

    void DesenharHud()
    {
        float w = Screen.width;
        Fase fase = jogo.DadosDaFase;
        const float pixel = 2.5f;
        float margem = 20f * escala, linha2 = 46f * escala;
        string mortes = ComNumero(ref textoMortes, jogo.Mortes, n => "Mortes: " + n);
        if (jogo.Estado == EstadoDoJogo.Mapa)
        {
            Texto(mortes, margem, 10f * escala, pixel, 0f, Branco);
        }
        else
        {
            string nome = jogo.NaSecreta ? fase.nome
                : ComNumero(ref textoDaFase, jogo.FaseAtual, f => $"Fase {f + 1}/{Fases.Todas.Length}: {Fases.Dados(f).nome}");
            Texto(nome, margem, 10f * escala, pixel, 0f, Branco, w * 0.3f);
            Texto(mortes, margem, linha2, pixel, 0f, Branco);
        }
        Texto(ComNumero(ref textoMoedas, jogo.Moedas, n => "Moedas: " + n), w - margem, 10f * escala, pixel, 1f, Amarelo);
        if (Opcoes.MostrarTempo) // o cronômetro pode ser escondido em Opções
            Texto(ComNumero(ref textoTempo, Mathf.FloorToInt(jogo.TempoTotal), t => FormatarTempo(t)), w - margem, linha2, pixel, 1f, Branco);
    }

    void DesenharTitulo()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        // o logo: letras douradas com a borda de madeira escura (o nome fica em Textos.NomeDoJogo)
        DesenharTextoEmPixel(FabricaDeSprites.TextoEmPixel(Textos.NomeDoJogo, CorDoLogo, CorDaBordaDoLogo, CorDaBordaDoLogo, comAcentos: true), w / 2f, h * 0.07f, 130f * escala);
        Texto("toda morte é só o começo da viagem (de novo, e de novo...)", w / 2f, h * 0.07f + 140f * escala, 2.5f, 0.5f, Branco, w * 0.9f);

        // até 7 opções: Continuar, Novo jogo, Opções, Como jogar, Conquistas, Créditos, Sair
        float passo = 40f * escala, y = h * 0.36f;
        for (int i = 0; i < jogo.opcoesDoMenu.Count; i++)
        {
            OpcaoDeMenu(TextoDaOpcao(jogo.opcoesDoMenu[i]), y, 3f, passo, i, i == jogo.OpcaoSelecionada);
            y += passo;
        }

        int recorde = Progresso.Recorde;
        if (recorde >= 0)
            Texto($"Recorde: zerou com {recorde} mortes", w / 2f, h * 0.8f, 2f, 0.5f, Verde, w * 0.9f);

        // rodapé discreto com os controles básicos (a lista completa fica em "Como jogar")
        Texto("A/D ou SETAS: andar    ESPAÇO/W: pular    R: reiniciar    ESC: pausa", w / 2f, h * 0.88f, 1.8f, 0.5f, CorDoRodape, w * 0.95f);
        Rodape("SETAS ou MOUSE: escolher    ENTER: confirmar    ESC: voltar");
    }

    static string TextoDaOpcao(GerenciadorDoJogo.OpcaoDoMenu opcao)
    {
        switch (opcao)
        {
            case GerenciadorDoJogo.OpcaoDoMenu.Continuar:
                return Progresso.FaseSalva == Fases.IndiceSecreto ? "Continuar (fase secreta)" : $"Continuar (fase {Progresso.FaseSalva + 1})";
            case GerenciadorDoJogo.OpcaoDoMenu.Conquistas:
                return $"Conquistas ({Conquistas.Quantidade}/{Conquistas.Todas.Length})";
            case GerenciadorDoJogo.OpcaoDoMenu.Opcoes: return "Opções";
            case GerenciadorDoJogo.OpcaoDoMenu.ComoJogar: return "Como jogar";
            case GerenciadorDoJogo.OpcaoDoMenu.Creditos: return "Créditos";
            case GerenciadorDoJogo.OpcaoDoMenu.Sair: return "Sair";
            default:
                return "Novo jogo";
        }
    }

    // Lista de conquistas: as que você tem em amarelo, as que faltam apagadinhas.
    void DesenharConquistas()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        Texto($"CONQUISTAS {Conquistas.Quantidade}/{Conquistas.Todas.Length}", w / 2f, 20f * escala, 5f, 0.5f, Amarelo);
        float y = 90f * escala, passo = (h - 160f * escala) / Conquistas.Todas.Length;
        foreach (Conquistas.Conquista c in Conquistas.Todas)
        {
            bool tem = Conquistas.Tem(c.id);
            Texto((tem ? "* " : "- ") + c.nome, w * 0.08f, y, 2.2f, 0f, tem ? Amarelo : new Color32(150, 150, 165, 255), w * 0.4f);
            Texto(c.descricao, w * 0.5f, y, 2.2f, 0f, tem ? Branco : new Color32(130, 130, 145, 255), w * 0.47f);
            y += passo;
        }
        BotaoDeTexto("Voltar (ENTER ou ESC)", w / 2f, h - 50f * escala, 2.2f, 0.5f, GerenciadorDoJogo.BotaoVoltar);
    }

    // Aviso de conquista nova: aparece em cima, no meio, por uns segundos.
    void DesenharAvisoDeConquista()
    {
        Conquistas.Conquista c = Conquistas.Recente;
        float t = Time.unscaledTime - Conquistas.QuandoFoi;
        if (c == null || t > 3.5f) return;
        float w = Screen.width;
        float entrada = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((3.5f - t) / 0.3f); // desliza para dentro e para fora
        float largura = 520f * escala, altura = 76f * escala;
        var caixa = new Rect((w - largura) / 2f, -altura + entrada * (altura + 90f * escala), largura, altura);
        GUI.DrawTexture(caixa, fundoEscuro);
        GUI.DrawTexture(caixa, fundoEscuro);
        Texto("CONQUISTA!", caixa.center.x, caixa.y + 6f * escala, 2.5f, 0.5f, Amarelo);
        Texto(c.nome, caixa.center.x, caixa.y + 38f * escala, 2.2f, 0.5f, Branco, largura - 20f * escala);
    }

    static readonly string[] Turma = { "jogador", "sereia", "louro", "fantasma" };

    // Créditos subindo devagar, com o pessoal pulando embaixo.
    void DesenharCreditos()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        float progresso = jogo.TempoDosCreditos / GerenciadorDoJogo.DuracaoDosCreditos;
        float linha = 46f * escala;
        float y = h - progresso * (h + Textos.Creditos.Length * linha);
        for (int i = 0; i < Textos.Creditos.Length; i++)
        {
            string texto = Textos.Creditos[i];
            bool titulo = i == 0, secao = texto == texto.ToUpperInvariant() && texto.Length > 0 && !titulo;
            float yLinha = y + i * linha;
            if (yLinha < -60f * escala || yLinha > h) continue;
            if (titulo) DesenharTextoEmPixel(FabricaDeSprites.TextoEmPixel(Textos.NomeDoJogo, CorDoLogo, CorDaBordaDoLogo, CorDaBordaDoLogo, comAcentos: true), w / 2f, yLinha, 70f * escala);
            else Texto(texto, w / 2f, yLinha, secao ? 3f : 2.5f, 0.5f, secao ? Amarelo : Branco, w * 0.9f);
        }

        // Grumete, Sereia, Louro e Dóris pulando na parte de baixo
        for (int i = 0; i < Turma.Length; i++)
        {
            Texture desenho = FabricaDeSprites.Pegar(Turma[i]).texture;
            float altura = desenho.height * 4f * escala, largura = desenho.width * 4f * escala;
            float pulo = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f + i)) * 20f * escala;
            GUI.DrawTexture(new Rect(w / 2f + (i - 1.5f) * 120f * escala - largura / 2f, h - altura - 20f * escala - pulo, largura, altura), desenho);
        }

        BotaoDeTexto("Voltar (ESC)", w - 20f * escala, 16f * escala, 2f, 1f, GerenciadorDoJogo.BotaoVoltar);
    }

    // Mapa do mundo: o nome da fase escolhida e os comandos, numa faixa escura embaixo.
    void DesenharMapa()
    {
        MapaDoMundo mapa = jogo.Mapa;
        if (mapa == null) return;
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, h - 135f * escala, w, 135f * escala), fundoEscuro);

        if (jogo.Fala.HasValue)
        {
            DesenharFalaNoMapa(jogo.Fala.Value);
            return;
        }

        int fase = mapa.Selecionado;
        bool secreta = fase == Fases.IndiceSecreto;
        string nome = secreta ? Fases.Secreta.nome : ComNumero(ref textoDoMapa, fase, f => $"Fase {f + 1}: {Fases.Todas[f].nome}");
        Texto(nome, w / 2f, h - 125f * escala, 3.5f, 0.5f, Amarelo, w * 0.9f);
        // já passou dessa fase alguma vez? Mostra o recorde e as medalhas dela; senão, a situação nesta partida
        PrepararRecordeDoMapa(fase);
        if (temRecordeGuardado && !mapa.Andando) DesenharRecordeNoMapa(h - 80f * escala);
        else
        {
            string situacao = secreta ? "a fase secreta!" : fase < jogo.FaseMaisLonge ? "já passou (dá para jogar de novo)" : "ainda não passou nesta partida";
            Texto(situacao, w / 2f, h - 78f * escala, 2f, 0.5f, secreta ? Rosa : fase < jogo.FaseMaisLonge ? Verde : Lilas, w * 0.9f);
        }
        string comandos = mapa.Andando ? "..." : "SETAS: andar     ENTER: jogar     ESC: título";
        Texto(comandos, w / 2f, h - 42f * escala, 2f, 0.5f, Branco, w * 0.9f);
    }

    // Na mesma ordem de GerenciadorDoJogo.ItemDaPausa.
    static readonly string[] NomesDaPausa = { "Continuar", "Reiniciar fase", "Opções", "Como jogar", "Voltar ao mapa", "Voltar ao título" };

    void DesenharPausa()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        Texto("PAUSA", w / 2f, h * 0.15f, 7f, 0.5f, Lilas); // (abaixo da barra de vida do chefe, se tiver)

        float passo = 46f * escala, y = h * 0.34f;
        for (int i = 0; i < NomesDaPausa.Length; i++)
        {
            OpcaoDeMenu(NomesDaPausa[i], y, 3f, passo, i, i == jogo.OpcaoDaPausa);
            y += passo;
        }

        Texto("(o progresso fica salvo)    Q: voltar ao mapa", w / 2f, h * 0.84f, 2f, 0.5f, Branco, w * 0.9f);
        Rodape("SETAS ou MOUSE: escolher    ENTER: confirmar    ESC: continuar");
    }

    // ------------------------------------------------------------------ submenus: Opções e Como Jogar

    // Os submenus abrem por cima do título OU da pausa (é a mesma tela nos dois).
    void DesenharSubmenu()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        if (jogo.SubmenuAberto == GerenciadorDoJogo.Submenu.Opcoes) DesenharOpcoes();
        else DesenharComoJogar();
    }

    // Na mesma ordem de GerenciadorDoJogo.ItemDasOpcoes.
    static readonly string[] ExplicacaoDasOpcoes =
    {
        "Volume da música de fundo.",
        "Volume dos efeitos: pulo, moedas, armadilhas...",
        "HD-2D: fundo desfocado, luzes e névoa. Simples: mais leve.",
        "Liga ou desliga a tela cheia.",
        "Mostra o tempo da partida no canto da tela.",
        "Volta para o menu anterior. (as opções já ficam salvas)",
    };

    void DesenharOpcoes()
    {
        float w = Screen.width, h = Screen.height;
        Texto("OPÇÕES", w / 2f, h * 0.08f, 7f, 0.5f, Lilas);

        float passo = 50f * escala, y = h * 0.3f;
        for (int i = 0; i < GerenciadorDoJogo.TotalDasOpcoes; i++)
        {
            OpcaoDeMenu(TextoDaConfiguracao((GerenciadorDoJogo.ItemDasOpcoes)i), y, 3f, passo, i, i == jogo.OpcaoDasOpcoes);
            y += passo;
        }

        int escolhida = Mathf.Clamp(jogo.OpcaoDasOpcoes, 0, ExplicacaoDasOpcoes.Length - 1);
        Texto(ExplicacaoDasOpcoes[escolhida], w / 2f, h * 0.78f, 2.2f, 0.5f, Lilas, w * 0.9f);
        Rodape("SETAS: escolher e mudar    ENTER ou CLIQUE: mudar    ESC: voltar");
    }

    // O texto de cada linha das opções, com o valor atual: "Música: < ********-- >".
    static string TextoDaConfiguracao(GerenciadorDoJogo.ItemDasOpcoes item)
    {
        switch (item)
        {
            case GerenciadorDoJogo.ItemDasOpcoes.Musica: return "Música: < " + Barra(Opcoes.Musica) + " >";
            case GerenciadorDoJogo.ItemDasOpcoes.Efeitos: return "Efeitos: < " + Barra(Opcoes.Efeitos) + " >";
            case GerenciadorDoJogo.ItemDasOpcoes.Graficos: return Opcoes.GraficosHD2D ? "Gráficos: < HD-2D >" : "Gráficos: < Simples >";
            case GerenciadorDoJogo.ItemDasOpcoes.TelaCheia: return Opcoes.TelaCheia ? "Tela cheia: < Sim >" : "Tela cheia: < Não >";
            case GerenciadorDoJogo.ItemDasOpcoes.Cronometro: return Opcoes.MostrarTempo ? "Cronômetro: < Sim >" : "Cronômetro: < Não >";
            default: return "Voltar";
        }
    }

    // Volume de 0 a 1 vira uma barrinha de 10 pedaços: "******----".
    static readonly string[] Barras = new string[11];
    static string Barra(float volume)
    {
        int cheios = Mathf.Clamp(Mathf.RoundToInt(volume * 10f), 0, 10);
        if (Barras[cheios] == null) Barras[cheios] = new string('*', cheios).PadRight(10, '-'); // guardada: não cria lixo
        return Barras[cheios];
    }

    // Como Jogar: título, a página atual (tecla à esquerda, o que faz à direita) e os botões embaixo.
    void DesenharComoJogar()
    {
        float w = Screen.width, h = Screen.height;
        int total = Textos.ComoJogar.Length;
        int p = Mathf.Clamp(jogo.PaginaDoComoJogar, 0, total - 1);
        Textos.PaginaDeAjuda pagina = Textos.ComoJogar[p];

        Texto("COMO JOGAR", w / 2f, h * 0.04f, 5f, 0.5f, Lilas);
        Texto(ComNumero(ref textoDaPagina, p, n => $"{Textos.ComoJogar[n].titulo}  ({n + 1}/{Textos.ComoJogar.Length})"),
            w / 2f, h * 0.04f + 80f * escala, 3f, 0.5f, Amarelo, w * 0.9f);

        float y = h * 0.24f, passo = Mathf.Min(54f * escala, h * 0.6f / Mathf.Max(1, pagina.linhas.Length));
        foreach (Textos.LinhaDeAjuda linha in pagina.linhas)
        {
            Texto(linha.tecla, w * 0.05f, y, 2.2f, 0f, Rosa, w * 0.33f);
            Texto(linha.texto, w * 0.40f, y, 2.2f, 0f, Branco, w * 0.56f);
            y += passo;
        }

        float yBotoes = h * 0.86f;
        if (p > 0) BotaoDeTexto("< anterior", w * 0.2f, yBotoes, 2.5f, 0.5f, GerenciadorDoJogo.BotaoAnterior);
        BotaoDeTexto("Voltar", w * 0.5f, yBotoes, 2.5f, 0.5f, GerenciadorDoJogo.BotaoVoltar);
        if (p < total - 1) BotaoDeTexto("próxima >", w * 0.8f, yBotoes, 2.5f, 0.5f, GerenciadorDoJogo.BotaoProxima);
        Rodape("ESQUERDA/DIREITA: trocar página    ENTER: próxima    ESC: voltar");
    }

    // Linha de ajuda bem embaixo da tela, pequena e discreta.
    void Rodape(string texto) => Texto(texto, Screen.width / 2f, Screen.height * 0.93f, 1.8f, 0.5f, CorDoRodape, Screen.width * 0.95f);

    void DesenharMorte()
    {
        float w = Screen.width, h = Screen.height;
        // "DEAD > CONTINUE", igual ao ímã do coelhinho
        DesenharTextoEmPixel(FabricaDeSprites.TextoEmPixel("DEAD", new Color32(230, 40, 50, 255), Color.white, Preto), w / 2f, h * 0.17f, 110f * escala);
        if (Time.time % 0.5f < 0.35f)
            DesenharTextoEmPixel(FabricaDeSprites.TextoEmPixel("> CONTINUE", Color.white, Preto), w / 2f, h * 0.17f + 125f * escala, 36f * escala);
        Texto(jogo.Mensagem, w / 2f, h * 0.48f, 3.5f, 0.5f, Vermelho, w * 0.9f);
        if (jogo.PodePularMorte)
            Texto("ESPAÇO: pular", w / 2f, h * 0.88f, 2f, 0.5f, Branco);
    }

    void DesenharFaseConcluida()
    {
        float w = Screen.width, h = Screen.height;
        if (jogo.EhUltimaFase && jogo.Sereia != null)
        {
            DesenharFalaDaSereia();
            DesenharResultado(h * 0.66f);
            return;
        }
        Texto("FASE CONCLUÍDA!", w / 2f, h * 0.3f, 7f, 0.5f, Verde, w * 0.9f);
        string comentario = jogo.MortesNaFase == 0 ? "Sem morrer?! Impossível." : $"Só {jogo.MortesNaFase} morte(s) nessa fase.";
        Texto(comentario, w / 2f, h * 0.3f + 110f * escala, 3f, 0.5f, Branco, w * 0.9f);
        DesenharResultado(h * 0.3f + 175f * escala);
    }

    // ------------------------------------------------------------------ recordes e medalhas

    // Textos da tela de resultado, montados uma vez só (não a cada quadro).
    readonly string[] textosDoResultado = new string[3];
    readonly string[] metasDoResultado = new string[3];
    float tempoDoResultado = -1f;
    // O recorde de cada coluna (na mesma ordem de Medalhas.Ordem: tempo, mortes, moedas).
    static readonly Progresso.Novidade[] RecordeDaColuna = { Progresso.Novidade.Tempo, Progresso.Novidade.Mortes, Progresso.Novidade.Moedas };

    // Os números da fase em 3 colunas (tempo, mortes, moedas), cada uma com a sua medalha em cima.
    // Medalha nova pulsa; recorde batido ganha um "NOVO!" amarelo embaixo.
    void DesenharResultado(float y)
    {
        GerenciadorDoJogo.ResultadoDaFase r = jogo.Resultado;
        if (!Mathf.Approximately(r.tempo, tempoDoResultado))
        {
            tempoDoResultado = r.tempo;
            textosDoResultado[0] = FormatarTempoFino(r.tempo);
            textosDoResultado[1] = r.mortes == 1 ? "1 morte" : r.mortes + " mortes";
            textosDoResultado[2] = $"{r.moedas}/{r.totalDeMoedas} moedas";
            metasDoResultado[0] = "meta " + FormatarTempo(Medalhas.TempoAlvo(jogo.FaseAtual));
            metasDoResultado[1] = "meta: 0";
            metasDoResultado[2] = "meta: todas";
        }
        bool primeira = (r.novidades & Progresso.Novidade.Primeira) != 0;
        float w = Screen.width, icone = 44f * escala;
        for (int i = 0; i < 3; i++)
        {
            Medalhas.Conjunto medalha = Medalhas.Ordem[i];
            float x = w * (0.3f + 0.2f * i);
            bool tem = (r.medalhas & medalha) != 0, nova = (r.medalhasNovas & medalha) != 0;
            DesenharMedalha(medalha, tem, nova, x, y + icone / 2f, icone);
            Texto(textosDoResultado[i], x, y + icone + 8f * escala, 2.5f, 0.5f, Branco, w * 0.19f);
            Texto(metasDoResultado[i], x, y + icone + 40f * escala, 1.8f, 0.5f, CorDoRodape, w * 0.19f);
            if ((r.novidades & RecordeDaColuna[i]) != 0 && Time.unscaledTime % 0.6f < 0.42f)
                Texto("NOVO RECORDE!", x, y + icone + 64f * escala, 2f, 0.5f, Amarelo, w * 0.19f);
        }
        if (primeira) Texto("recordes salvos para esta fase", w / 2f, y + icone + 96f * escala, 1.8f, 0.5f, Lilas, w * 0.9f);
    }

    // Uma medalha: acesa (ganhou), apagada (falta) ou pulsando (acabou de ganhar).
    static void DesenharMedalha(Medalhas.Conjunto medalha, bool tem, bool nova, float centroX, float centroY, float tamanho)
    {
        Texture desenho = FabricaDeSprites.Pegar(Medalhas.Desenho(medalha)).texture;
        float lado = tamanho * (nova ? 1f + 0.15f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) : 1f);
        Color corOriginal = GUI.color;
        GUI.color = tem ? Color.white : new Color(0.3f, 0.3f, 0.36f, 0.8f);
        GUI.DrawTexture(new Rect(centroX - lado / 2f, centroY - lado / 2f, lado, lado), desenho);
        GUI.color = corOriginal;
    }

    // Recorde da fase escolhida no mapa (montado só quando a fase escolhida muda).
    int faseDoRecordeGuardado = -2;
    bool temRecordeGuardado;
    Medalhas.Conjunto medalhasGuardadas;
    readonly string[] textosDoRecorde = new string[3];

    void PrepararRecordeDoMapa(int fase)
    {
        if (fase == faseDoRecordeGuardado) return;
        faseDoRecordeGuardado = fase;
        Progresso.RecordeDaFase r = Progresso.RecordeDe(fase);
        temRecordeGuardado = r.existe;
        if (!r.existe) return;
        medalhasGuardadas = Medalhas.DaFase(fase);
        textosDoRecorde[0] = $"{FormatarTempoFino(r.tempo)} (meta {FormatarTempo(Medalhas.TempoAlvo(fase))})";
        textosDoRecorde[1] = r.mortes == 1 ? "1 morte" : r.mortes + " mortes";
        textosDoRecorde[2] = $"{r.moedas}/{Medalhas.TotalDeMoedas(fase)} moedas";
    }

    // A linha de recorde no mapa: "RECORDE  [relógio] 00:41.2 (meta 00:55)  [coração] 2 mortes  [moeda] 5/7 moedas".
    void DesenharRecordeNoMapa(float y)
    {
        float w = Screen.width, icone = 26f * escala, folga = 8f * escala, espaco = 34f * escala;
        const float pixel = 2f;
        // mede tudo antes, para centralizar a linha
        float largura = MedirTexto("RECORDE", 0f, y, pixel, 0f).width + espaco;
        for (int i = 0; i < 3; i++) largura += icone + folga + MedirTexto(textosDoRecorde[i], 0f, y, pixel, 0f).width + (i < 2 ? espaco : 0f);
        float escalaDaLinha = Mathf.Min(1f, w * 0.94f / largura); // tela estreita: encolhe tudo junto
        float x = (w - largura * escalaDaLinha) / 2f;
        x += Texto("RECORDE", x, y, pixel * escalaDaLinha, 0f, Amarelo).width + espaco * escalaDaLinha;
        float alturaDoTexto = MedirTexto("0", 0f, y, pixel * escalaDaLinha, 0f).height;
        for (int i = 0; i < 3; i++)
        {
            Medalhas.Conjunto medalha = Medalhas.Ordem[i];
            DesenharMedalha(medalha, (medalhasGuardadas & medalha) != 0, false, x + icone * escalaDaLinha / 2f, y + alturaDoTexto / 2f, icone * escalaDaLinha);
            x += (icone + folga) * escalaDaLinha;
            x += Texto(textosDoRecorde[i], x, y, pixel * escalaDaLinha, 0f, Branco).width + espaco * escalaDaLinha;
        }
    }

    void DesenharVitoria()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);

        // a Marina (a sereia), grandona, do lado esquerdo
        Texture sereia = FabricaDeSprites.Pegar("sereia").texture;
        float alturaDaSereia = h * 0.55f, larguraDaSereia = alturaDaSereia * sereia.width / sereia.height;
        float pulinho = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 10f * escala;
        GUI.DrawTexture(new Rect(w * 0.17f - larguraDaSereia / 2f, h * 0.3f - pulinho, larguraDaSereia, alturaDaSereia), sereia);

        float centro = w * 0.6f, largura = w * 0.7f;
        Texto("VOCÊ ZEROU!", centro, h * 0.1f, 8f, 0.5f, Amarelo, largura);
        Texto("Marina: \"Obrigada por voltar, Grumete!\"", centro, h * 0.27f, 2.5f, 0.5f, Lilas, largura);
        Texto($"Mortes: {jogo.Mortes}", centro, h * 0.38f, 3.5f, 0.5f, Branco);
        Texto($"Moedas: {jogo.Moedas}", centro, h * 0.38f + 50f * escala, 3.5f, 0.5f, Amarelo);
        Texto($"Tempo: {FormatarTempo(jogo.TempoTotal)}", centro, h * 0.38f + 100f * escala, 3.5f, 0.5f, Branco);
        Texto($"Título: {GerenciadorDoJogo.Titulo(jogo.Mortes)}", centro, h * 0.38f + 165f * escala, 3.5f, 0.5f, Verde, largura);
        if (Time.unscaledTime % 1f < 0.65f)
            Texto("Aperte ENTER para ver os créditos", centro, h * 0.85f, 3f, 0.5f, Branco, largura);
    }

    // ------------------------------------------------------------------ caixas de fala

    // A caixa de fala (do Louro, da Dóris e das falas do mapa): moldura de madeira com um filete dourado,
    // fundo azul-marinho quase opaco, o retrato de quem fala à esquerda, uma etiqueta com o nome em cima
    // e o texto na fonte pixel, quebrado em linhas que cabem na caixa.
    static readonly Color CorDaMoldura = new Color32(70, 42, 26, 255), CorDaMolduraEscura = new Color32(36, 20, 14, 255),
        CorDoFilete = new Color32(226, 176, 78, 255), CorDoFundoDaFala = new Color32(16, 20, 42, 242),
        CorDoFundoDoRetrato = new Color32(40, 54, 96, 255);
    static readonly Color32 CorDoNome = new Color32(255, 214, 96, 255);
    const float PixelDaFala = 2.4f;     // tamanho de cada "pixel" da letra (em telas de 720p)
    const float AlturaDaLinha = 11f;    // distância entre as linhas, em pixels da fonte
    const int MaximoDeLinhas = 4;

    readonly Dictionary<(string, int), string[]> linhasGuardadas = new Dictionary<(string, int), string[]>();

    // Quebra o texto em linhas de no máximo "largura" pixels da fonte. Os "\n" do texto viram espaço (eles
    // eram para a caixa antiga, mais estreita): assim nenhuma palavra fica sozinha numa linha.
    string[] QuebrarEmLinhas(string texto, int largura)
    {
        if (linhasGuardadas.TryGetValue((texto, largura), out string[] prontas)) return prontas;
        var linhas = new List<string>();
        string linha = "";
        foreach (string palavra in texto.Replace('\n', ' ').Split(' '))
        {
            if (palavra.Length == 0) continue;
            string tentativa = linha.Length == 0 ? palavra : linha + " " + palavra;
            if (linha.Length > 0 && FabricaDeSprites.LarguraDoTextoEmPixel(tentativa) > largura)
            {
                linhas.Add(linha);
                linha = palavra;
            }
            else linha = tentativa;
        }
        linhas.Add(linha);
        string[] resultado = linhas.ToArray();
        if (linhasGuardadas.Count > 200) linhasGuardadas.Clear();
        linhasGuardadas[(texto, largura)] = resultado;
        return resultado;
    }

    // Um retângulo cheio de uma cor (a transparência geral vem do GUI.color).
    static void Retangulo(Rect area, Color cor)
    {
        Color antes = GUI.color;
        GUI.color = new Color(cor.r, cor.g, cor.b, cor.a * antes.a);
        GUI.DrawTexture(area, Texture2D.whiteTexture);
        GUI.color = antes;
    }

    // A moldura: borda escura por fora, a madeira, o filete dourado e o fundo.
    void Moldura(Rect area, Color fundo)
    {
        float p = Mathf.Max(1f, Mathf.Round(2f * escala));
        Retangulo(area, CorDaMolduraEscura);
        Retangulo(new Rect(area.x + p, area.y + p, area.width - 2f * p, area.height - 2f * p), CorDaMoldura);
        Retangulo(new Rect(area.x + 3f * p, area.y + 3f * p, area.width - 6f * p, area.height - 6f * p), CorDoFilete);
        Retangulo(new Rect(area.x + 4f * p, area.y + 4f * p, area.width - 8f * p, area.height - 8f * p), fundo);
        // os "pregos" dourados nos quatro cantos da madeira
        foreach (Vector2 canto in new[] { new Vector2(area.x + p, area.y + p), new Vector2(area.xMax - 3f * p, area.y + p),
                                          new Vector2(area.x + p, area.yMax - 3f * p), new Vector2(area.xMax - 3f * p, area.yMax - 3f * p) })
            Retangulo(new Rect(canto.x, canto.y, 2f * p, 2f * p), CorDoFilete);
    }

    // Desenha a caixa inteira. Devolve a altura que ela ocupou. "dica" (opcional) pisca no canto de baixo.
    float CaixaDeFala(float x, float y, float largura, string quem, string retrato, string texto, float alfa, string dica = null, float alturaMinima = 0f)
    {
        Color antes = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alfa);
        float m = 14f * escala;                                  // margem de dentro
        float lado = 64f * escala;                               // o quadradinho do retrato
        float pixel = PixelDaFala;
        float areaDoTexto = largura - lado - 3f * m;
        string[] linhas = QuebrarEmLinhas(texto, Mathf.FloorToInt(areaDoTexto / (pixel * escala)));
        while (linhas.Length > MaximoDeLinhas && pixel > 1.7f) // não coube: letra um pouco menor
        {
            pixel -= 0.2f;
            linhas = QuebrarEmLinhas(texto, Mathf.FloorToInt(areaDoTexto / (pixel * escala)));
        }
        float alturaDoTexto = linhas.Length * AlturaDaLinha * pixel * escala;
        float altura = Mathf.Max(alturaMinima, Mathf.Max(lado, alturaDoTexto) + 2f * m);
        var caixa = new Rect(x, y, largura, altura);
        Moldura(caixa, CorDoFundoDaFala);

        // o retrato (o desenho de quem fala, numa moldura pequena), pulando de leve
        var quadro = new Rect(x + m, y + (altura - lado) / 2f, lado, lado);
        Moldura(quadro, CorDoFundoDoRetrato);
        Texture desenho = FabricaDeSprites.Pegar(retrato).texture;
        float escalaDoDesenho = Mathf.Floor((lado - 12f * escala) / Mathf.Max(desenho.width, desenho.height));
        if (escalaDoDesenho < 1f) escalaDoDesenho = (lado - 12f * escala) / Mathf.Max(desenho.width, desenho.height);
        float dw = desenho.width * escalaDoDesenho, dh = desenho.height * escalaDoDesenho;
        float pulo = Mathf.Round(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 2f) * escalaDoDesenho * 0.5f;
        GUI.DrawTexture(new Rect(quadro.center.x - dw / 2f, quadro.center.y - dh / 2f - pulo, dw, dh), desenho);

        // a etiqueta com o nome, pendurada em cima da caixa
        float xDoTexto = quadro.xMax + m;
        Rect nome = MedirTexto(quem, 0f, 0f, 2.2f, 0f);
        var etiqueta = new Rect(xDoTexto - 8f * escala, y - nome.height - 6f * escala, nome.width + 16f * escala, nome.height + 10f * escala);
        Moldura(etiqueta, CorDaMoldura);
        Texto(quem, etiqueta.x + 8f * escala, etiqueta.y + 5f * escala, 2.2f, 0f, CorDoNome);

        // o texto, alinhado à esquerda e centralizado na altura da caixa
        float yDoTexto = y + (altura - alturaDoTexto) / 2f;
        for (int i = 0; i < linhas.Length; i++)
            Texto(linhas[i], xDoTexto, yDoTexto + (i * AlturaDaLinha - 3f) * pixel * escala, pixel, 0f, Branco);

        if (dica != null && Time.unscaledTime % 1f < 0.65f)
            Texto(dica, caixa.xMax - m, caixa.yMax - m - 12f * escala, 1.8f, 1f, CorDoNome);
        GUI.color = antes;
        return altura;
    }

    // Fala de um personagem depois de passar de fase: a caixa fica DENTRO da faixa escura de baixo do mapa
    // (por cima das últimas linhas do mapa, que são só enfeite): não tampa nenhuma fase.
    void DesenharFalaNoMapa(Textos.Fala fala)
    {
        float w = Screen.width, h = Screen.height;
        float largura = Mathf.Min(900f * escala, w - 40f * escala);
        string texto = string.Format(fala.texto, jogo.MortesNaFase);
        CaixaDeFala((w - largura) / 2f, h - 112f * escala, largura, fala.quem, fala.sprite, texto, 1f, "ENTER >", 100f * escala);
    }

    // ------------------------------------------------------------------ falas

    // O Louro (ou a Dóris) falando: a caixa aparece no ALTO da tela, embaixo do HUD, quando o Grumete chega
    // perto (só a placa mais perto). Fica sempre no mesmo lugar: assim nunca tampa o Grumete nem o chão.
    void DesenharFalaDoLouro()
    {
        if (!PlacaPerto(out Placa maisPerto, out float menor)) return;
        float alfa = Mathf.Clamp01((3.5f - menor) / 0.6f); // aparece suave quando você chega perto
        float w = Screen.width;
        float largura = Mathf.Min(820f * escala, w - 40f * escala);
        CaixaDeFala((w - largura) / 2f, 126f * escala, largura, maisPerto.quem, maisPerto.Retrato, maisPerto.texto, alfa);
    }

    // A placa mais perto do Grumete, se ele estiver perto o bastante para ela falar (3,5 blocos).
    bool PlacaPerto(out Placa maisPerto, out float distancia)
    {
        maisPerto = null;
        distancia = float.MaxValue;
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (jogador == null || jogo.Estado == EstadoDoJogo.Titulo) return false;
        foreach (Placa placa in jogo.placas)
        {
            if (placa == null) continue;
            float d = Vector2.Distance(placa.transform.position, jogador.transform.position);
            if (d < distancia) { distancia = d; maisPerto = placa; }
        }
        return maisPerto != null && distancia <= 3.5f;
    }

    // "S: entrar" piscando em cima da porta da Dóris em que o jogador está parado.
    void DesenharDicaDasPortas()
    {
        Camera cam = Camera.main;
        if (cam == null || jogo.Estado != EstadoDoJogo.Jogando) return;
        foreach (Porta porta in jogo.portas)
        {
            if (porta == null || !Porta.PodeEntrar || !porta.JogadorNaFrente()) continue;
            Vector3 tela = TelaPixelada.MundoParaTela(cam, porta.transform.position + Vector3.up * 2.3f);
            if (Time.unscaledTime % 0.8f < 0.55f)
                Texto("S: entrar", tela.x, Screen.height - tela.y - 30f * escala, 2.2f, 0.5f, Lilas);
        }
    }

    void DesenharFalaDaSereia()
    {
        Camera cam = Camera.main;
        if (cam == null || jogo.Sereia == null) return;
        Vector3 tela = TelaPixelada.MundoParaTela(cam, jogo.Sereia.transform.position + Vector3.up * 2.2f);
        float y = Screen.height - tela.y;
        Texto("Grumete! Você voltou!", tela.x, y - 50f * escala, 3f, 0.5f, Lilas, Screen.width * 0.6f);
    }

    // ------------------------------------------------------------------ efeitos de tela

    // Mãos de sombra: (posição ao longo da borda 0..1, borda: 0 = baixo, 1 = esquerda, 2 = direita, atraso 0..1)
    static readonly Vector3[] MaosDaSombra =
    {
        new Vector3(0.12f, 0, 0.00f), new Vector3(0.36f, 0, 0.15f), new Vector3(0.64f, 0, 0.05f), new Vector3(0.88f, 0, 0.20f),
        new Vector3(0.40f, 1, 0.10f), new Vector3(0.78f, 1, 0.25f),
        new Vector3(0.35f, 2, 0.20f), new Vector3(0.72f, 2, 0.00f),
    };

    // A Volta da Maré: ao morrer a tela escurece e os tentáculos do Kraken avançam das bordas;
    // no pico do áudio você renasce com um clarão.
    void DesenharRetornoPelaMorte()
    {
        if (jogo.Estado == EstadoDoJogo.Morreu)
            DesenharSombras(jogo.ProgressoDaMorte, 0.85f);

        float clarao = jogo.Renascendo;
        if (clarao > 0f)
        {
            Color corOriginal = GUI.color;
            GUI.color = new Color(0.85f, 0.7f, 1f, clarao * clarao * 0.9f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = corOriginal;
        }
    }

    // Troca de fase: as mesmas sombras cobrem a tela e depois saem.
    void DesenharTransicao()
    {
        float p = jogo.Transicao;
        if (p > 0f) DesenharSombras(p, 1f);
    }

    // p: 0 = nada, 1 = sombras no máximo. escuridaoMaxima: o quanto a tela fica escura no fim.
    void DesenharSombras(float p, float escuridaoMaxima)
    {
        float w = Screen.width, h = Screen.height;
        Color corOriginal = GUI.color;
        GUI.color = new Color(0.06f, 0f, 0.1f, Mathf.SmoothStep(0f, escuridaoMaxima, p));
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);

        Texture mao = FabricaDeSprites.Pegar("tentaculo").texture;
        float largura = 100f * escala, altura = largura * 1.5f; // a arte tem 16x24
        GUI.color = Color.white;
        Matrix4x4 matrizOriginal = GUI.matrix;
        for (int i = 0; i < MaosDaSombra.Length; i++)
        {
            Vector3 m = MaosDaSombra[i];
            float avanco = Mathf.Clamp01((p - m.z) / (1f - m.z));
            if (avanco <= 0f) continue;
            float alcance = altura * 0.85f * avanco + Mathf.Sin(Time.unscaledTime * 7f + i * 1.7f) * 5f * escala;

            // a mão é desenhada "para cima" e girada para sair da borda certa
            Vector2 pivo = m.y == 0 ? new Vector2(m.x * w, h) : new Vector2(m.y == 1 ? 0f : w, m.x * h);
            float angulo = m.y == 0 ? 0f : (m.y == 1 ? 90f : -90f);
            GUI.matrix = matrizOriginal;
            GUIUtility.RotateAroundPivot(angulo, pivo);
            GUI.DrawTexture(new Rect(pivo.x - largura / 2f, pivo.y - alcance, largura, altura), mao);
        }
        GUI.matrix = matrizOriginal;
        GUI.color = corOriginal;
    }

    // Barra de vida do chefe (a Baleia Branca, a Cozinheira...), no topo da tela (no lugar da barra de progresso).
    // Mostra a ETAPA da luta e a barra dividida: cada etapa é um bloco de 2 golpes, separado por uma
    // divisão mais grossa, com uma bolinha embaixo (cheia = etapa vencida, piscando = etapa atual).
    void DesenharVidaDoChefe(Chefe chefe)
    {
        float largura = Mathf.Min(Screen.width * 0.4f, 560f * escala), altura = 16f * escala;
        var barra = new Rect((Screen.width - largura) / 2f, 52f * escala, largura, altura);
        int etapa = chefe.EtapaAtual * 100 + chefe.Etapas; // muda quando a etapa muda (ou quando o chefe é outro)
        if (textoDoChefe.texto == null || textoDoChefe.valor != etapa || !textoDoChefe.texto.StartsWith(chefe.Nome, System.StringComparison.Ordinal))
        {
            textoDoChefe.valor = etapa;
            textoDoChefe.texto = $"{chefe.Nome} - ETAPA {chefe.EtapaAtual + 1}/{chefe.Etapas}";
        }
        string titulo = textoDoChefe.texto;
        Texto(titulo, Screen.width / 2f, 12f * escala, 2.5f, 0.5f, Branco);

        Color corOriginal = GUI.color;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(barra.x - 3f * escala, barra.y - 3f * escala, barra.width + 6f * escala, barra.height + 6f * escala), Texture2D.whiteTexture);
        GUI.color = new Color(0.25f, 0.05f, 0.1f);
        GUI.DrawTexture(barra, Texture2D.whiteTexture);
        float vida = chefe.VidaMostrada / chefe.VidaMaxima;
        GUI.color = new Color(1f, 0.85f, 0.85f); // a parte que "escorre" fica clarinha
        GUI.DrawTexture(new Rect(barra.x, barra.y, barra.width * vida, barra.height), Texture2D.whiteTexture);
        GUI.color = new Color(0.9f, 0.15f, 0.25f);
        GUI.DrawTexture(new Rect(barra.x, barra.y, barra.width * chefe.Vida / chefe.VidaMaxima, barra.height), Texture2D.whiteTexture);
        GUI.color = Color.black; // divisões entre os pedaços de vida (mais grossas entre uma etapa e outra)
        for (int i = 1; i < chefe.VidaMaxima; i++)
        {
            float grossura = i % chefe.GolpesPorEtapa == 0 ? 5f : 2f;
            GUI.DrawTexture(new Rect(barra.x + barra.width * i / chefe.VidaMaxima - grossura * escala / 2f, barra.y, grossura * escala, barra.height), Texture2D.whiteTexture);
        }

        // uma bolinha por etapa, embaixo do meio de cada bloco: vencida, atual (piscando) ou ainda não
        int etapas = chefe.VidaMaxima / chefe.GolpesPorEtapa;
        float lado = 8f * escala;
        for (int i = 0; i < etapas; i++)
        {
            // a barra esvazia da direita para a esquerda, então a 1ª etapa é o bloco da DIREITA
            float centro = barra.x + barra.width * (etapas - i - 0.5f) / etapas;
            var bolinha = new Rect(centro - lado / 2f, barra.yMax + 6f * escala, lado, lado);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(bolinha.x - escala, bolinha.y - escala, bolinha.width + 2f * escala, bolinha.height + 2f * escala), Texture2D.whiteTexture);
            bool vencida = i < chefe.EtapaAtual, atual = i == chefe.EtapaAtual;
            GUI.color = vencida ? new Color(1f, 0.85f, 0.3f)
                : atual && Mathf.Repeat(Time.unscaledTime * 2f, 1f) < 0.6f ? new Color(0.9f, 0.15f, 0.25f)
                : new Color(0.25f, 0.05f, 0.1f);
            GUI.DrawTexture(bolinha, Texture2D.whiteTexture);
        }
        GUI.color = corOriginal;
    }

    // Barrinha no topo: do início da fase até a bandeira mais distante.
    // (Usa a mais distante de propósito, para não entregar qual é a bandeira de verdade.
    //  E se a bandeira fujona fugir, o fim da barra foge junto.)
    void DesenharBarraDeProgresso()
    {
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (jogador == null) return;
        float fim = float.MinValue;
        foreach (Transform bandeira in jogo.Bandeiras)
            if (bandeira != null) fim = Mathf.Max(fim, bandeira.position.x);
        float inicio = jogo.PosicaoInicial.x;
        if (fim <= inicio) return;

        float progresso = Mathf.Clamp01((jogador.transform.position.x - inicio) / (fim - inicio));

        float largura = Mathf.Min(Screen.width * 0.36f, 520f * escala);
        float altura = 12f * escala;
        var barra = new Rect((Screen.width - largura) / 2f, 58f * escala, largura, altura); // na altura da 2ª linha do HUD

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

        // Grumete andando na barra
        Texture grumete = FabricaDeSprites.Pegar("jogador").texture;
        float alturaJogador = 32f * escala, larguraJogador = alturaJogador * grumete.width / grumete.height;
        GUI.DrawTexture(new Rect(barra.x + barra.width * progresso - larguraJogador / 2f, barra.center.y - alturaJogador / 2f, larguraJogador, alturaJogador),
            grumete);
    }

    // ------------------------------------------------------------------ texto em pixel

    // Escreve com a fonte pixel. pixel = tamanho de cada "pixel" da letra (em telas de 720p);
    // alinhamento: 0 = x é a esquerda do texto, 0.5 = o centro, 1 = a direita.
    // Se passar de larguraMaxima, o texto encolhe para caber.
    // Devolve o retângulo onde o texto foi desenhado (útil para saber se o mouse está em cima).
    Rect Texto(string texto, float x, float y, float pixel, float alinhamento, Color32 cor, float larguraMaxima = 0f)
    {
        if (string.IsNullOrEmpty(texto)) return new Rect(x, y, 0f, 0f);
        // (a conversão para MAIÚSCULAS acontece lá dentro, só na primeira vez: aqui não cria nenhuma string nova)
        Texture2D textura = FabricaDeSprites.TextoEmPixel(texto, cor, Preto, Preto, comAcentos: true, maiusculas: true);
        Rect area = AreaDoTexto(textura, x, y, pixel, alinhamento, larguraMaxima);
        GUI.DrawTexture(area, textura);
        return area;
    }

    // Onde um texto FICARIA, sem desenhar (o tamanho não depende da cor, então mede com a textura branca).
    Rect MedirTexto(string texto, float x, float y, float pixel, float alinhamento, float larguraMaxima = 0f)
    {
        if (string.IsNullOrEmpty(texto)) return new Rect(x, y, 0f, 0f);
        Texture2D textura = FabricaDeSprites.TextoEmPixel(texto, Branco, Preto, Preto, comAcentos: true, maiusculas: true);
        return AreaDoTexto(textura, x, y, pixel, alinhamento, larguraMaxima);
    }

    Rect AreaDoTexto(Texture2D textura, float x, float y, float pixel, float alinhamento, float larguraMaxima)
    {
        float tamanho = pixel * escala;
        float largura = textura.width * tamanho;
        if (larguraMaxima > 0f && largura > larguraMaxima)
        {
            tamanho *= larguraMaxima / largura;
            largura = larguraMaxima;
        }
        return new Rect(x - largura * alinhamento, y, largura, textura.height * tamanho);
    }

    // ------------------------------------------------------------------ menus e mouse

    // Uma opção de menu (título, pausa, opções), centralizada. A selecionada fica amarela, com "> <" piscando
    // e uma faixa lilás pulsando atrás. alturaDaLinha = a distância até a próxima opção (é a área do mouse).
    void OpcaoDeMenu(string texto, float y, float pixel, float alturaDaLinha, int indice, bool selecionada)
    {
        float w = Screen.width;
        Rect medida = MedirTexto(texto, w / 2f, y, pixel, 0.5f, w * 0.9f);
        // área do mouse: a linha toda, mais larga que o texto (fácil de acertar) e sem buraco entre as linhas
        var area = new Rect(w / 2f - medida.width / 2f - 50f * escala, y + (medida.height - alturaDaLinha) / 2f,
            medida.width + 100f * escala, alturaDaLinha);
        if (selecionada) DesenharFaixa(area);
        string mostrado = selecionada && Time.unscaledTime % 0.8f < 0.55f ? "> " + texto + " <" : texto;
        Texto(mostrado, w / 2f, y, pixel, 0.5f, selecionada ? Amarelo : Branco, w * 0.9f);
        Mouse(area, indice, true);
    }

    // Botão de texto (Voltar, página anterior/próxima): fica amarelo com o mouse em cima; o clique vai para o
    // GerenciadorDoJogo como "botao" (GerenciadorDoJogo.BotaoVoltar...). O teclado tem as próprias teclas.
    void BotaoDeTexto(string texto, float x, float y, float pixel, float alinhamento, int botao)
    {
        Rect medida = MedirTexto(texto, x, y, pixel, alinhamento, Screen.width * 0.9f);
        var area = new Rect(medida.x - 12f * escala, medida.y, medida.width + 24f * escala, medida.height);
        bool emCima = Mouse(area, botao, false);
        if (emCima) DesenharFaixa(area);
        Texto(texto, x, y, pixel, alinhamento, emCima ? Amarelo : Branco, Screen.width * 0.9f);
    }

    // Faixa lilás translúcida que "respira" atrás da opção escolhida.
    static void DesenharFaixa(Rect area)
    {
        Color corOriginal = GUI.color;
        float pulso = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
        GUI.color = new Color(0.84f, 0.67f, 0.96f, 0.12f + 0.12f * pulso);
        GUI.DrawTexture(area, Texture2D.whiteTexture);
        GUI.color = corOriginal;
    }

    // Uma vez por OnGUI: o mouse mexeu desde a última vez? (o OnGUI roda várias vezes por quadro, uma por "evento")
    void PrepararMouse()
    {
        Event e = Event.current;
        usarMouse = !PainelDeGraficos.Aberto;
        mouseMexeu = false;
        if (e.type != EventType.Repaint && e.type != EventType.MouseMove && e.type != EventType.MouseDrag) return;
        if (mouseConhecido && (e.mousePosition - mouseAntes).sqrMagnitude > 1f) mouseMexeu = true;
        mouseAntes = e.mousePosition;
        mouseConhecido = true;
    }

    // O mouse está em cima de "area"? Se ele MEXEU, seleciona a opção (só quando selecionarAoPassar);
    // clique com o botão esquerdo escolhe. O lado do clique (-1 = metade esquerda, +1 = direita) serve para
    // diminuir/aumentar nas opções. Use() "gasta" o clique, para nada mais embaixo pegar o mesmo clique.
    bool Mouse(Rect area, int opcao, bool selecionarAoPassar)
    {
        if (!usarMouse) return false;
        Event e = Event.current;
        if (!area.Contains(e.mousePosition)) return false;
        if (selecionarAoPassar && mouseMexeu) jogo.MouseEmCima(opcao);
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            jogo.MouseClicou(opcao, e.mousePosition.x < area.center.x ? -1 : 1);
            e.Use();
        }
        return true;
    }

    // Desenha um texto de FabricaDeSprites.TextoEmPixel centralizado em (centroX, topo), com a altura pedida.
    static void DesenharTextoEmPixel(Texture2D texto, float centroX, float topo, float altura)
    {
        float largura = altura * texto.width / texto.height;
        GUI.DrawTexture(new Rect(centroX - largura / 2f, topo, largura, altura), texto);
    }

    static string FormatarTempo(float segundos) => FormatarTempo(Mathf.FloorToInt(segundos));
    static string FormatarTempo(int total) => $"{total / 60:00}:{total % 60:00}";

    // Com o décimo de segundo (para os recordes das fases): "01:23,4".
    static string FormatarTempoFino(float segundos)
    {
        int decimos = Mathf.FloorToInt(segundos * 10f);
        return $"{decimos / 600:00}:{decimos / 10 % 60:00},{decimos % 10}";
    }
}
