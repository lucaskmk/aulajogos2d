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
    Texture2D fundoDoTitulo; // a ilustração da tela de título (Assets/Resources/Arte/titulo_fundo.png)
    float escala;

    // Textos do HUD guardados: só são montados de novo quando o número muda.
    // (montar "Mortes: " + número a cada quadro cria lixo na memória; veja FabricaDeSprites.TextoEmPixel)
    struct TextoComNumero { public int valor; public string texto; public Idioma.Lingua idioma; }
    TextoComNumero textoMortes, textoMoedas, textoTempo, textoDaFase, textoDoMapa, textoDoChefe, textoDaPagina;
    TextoComNumero textoDosEscudos, textoDoSaldo, textoDoPreco, textoDaBolsa, textoDosElixires, textoDaAposta, textoDoPremio;

    // Mouse nos menus (veja Mouse(), lá embaixo).
    Vector2 mouseAntes;
    bool mouseConhecido; // já sabemos onde o mouse estava? (no 1º quadro não dá para saber se ele "mexeu")
    bool mouseMexeu;     // o mouse se mexeu desde o último quadro? Só aí ele seleciona (senão brigaria com o teclado)
    bool usarMouse;      // falso com o painel F10 aberto (os cliques são dele)

    static string ComNumero(ref TextoComNumero guardado, int valor, System.Func<int, string> montar)
    {
        if (guardado.texto == null || guardado.valor != valor || guardado.idioma != Idioma.Atual) // (trocou o idioma: monta de novo)
        {
            guardado.valor = valor;
            guardado.idioma = Idioma.Atual;
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
        if (!jogo.NoTutorial) louroNaTela = false; // o próximo tutorial começa com o Louro chegando voando de novo
        bool submenu = jogo.SubmenuAberto != GerenciadorDoJogo.Submenu.Nenhum; // Opções ou Como Jogar abertos

        if (!jogo.MostrandoPainel) // no painel de melhores tempos, a fala do Louro e a dica das portas atrapalham a leitura
        {
            DesenharFalaDoLouro();
            DesenharDicaDasPortas();
        }
        DesenharRetornoPelaMorte();
        bool lutando = jogo.Chefe != null && jogo.Chefe.EmCombate;
        if ((estado == EstadoDoJogo.Jogando || estado == EstadoDoJogo.Morreu || estado == EstadoDoJogo.FaseConcluida) && !lutando)
            DesenharBarraDeProgresso();
        if (lutando && estado != EstadoDoJogo.Titulo && !submenu) DesenharVidaDoChefe(jogo.Chefe);
        if (estado != EstadoDoJogo.Titulo && estado != EstadoDoJogo.Vitoria && estado != EstadoDoJogo.Historia
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
            case EstadoDoJogo.Historia: DesenharHistoria(); break;
        }

        // botão direito do mouse = ESC (volta uma tela), em qualquer menu
        bool emMenu = estado == EstadoDoJogo.Titulo || estado == EstadoDoJogo.Pausado
                      || estado == EstadoDoJogo.Conquistas || estado == EstadoDoJogo.Creditos || estado == EstadoDoJogo.Historia
                      || (estado == EstadoDoJogo.Mapa && jogo.LojaAberta);
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
        string mortes = ComNumero(ref textoMortes, jogo.Mortes, n => Idioma.T("Mortes: ", "Deaths: ") + n);
        Rect areaDasMortes;
        if (jogo.Estado == EstadoDoJogo.Mapa)
        {
            areaDasMortes = Texto(mortes, margem, 10f * escala, pixel, 0f, Branco);
        }
        else
        {
            string nome = jogo.NaSecreta ? fase.nome
                : ComNumero(ref textoDaFase, jogo.FaseAtual, f => $"{Idioma.T("Fase", "Level")} {f + 1}/{Fases.Todas.Length}: {Idioma.Traduzir(Fases.Dados(f).nome)}");
            Texto(nome, margem, 10f * escala, pixel, 0f, Branco, w * 0.3f);
            areaDasMortes = Texto(mortes, margem, linha2, pixel, 0f, Branco);
        }
        DesenharItensNoHud(areaDasMortes);
        Texto(ComNumero(ref textoMoedas, jogo.Moedas, n => Idioma.T("Moedas: ", "Coins: ") + n), w - margem, 10f * escala, pixel, 1f, Amarelo);
        if (Opcoes.MostrarTempo) // o cronômetro pode ser escondido em Opções
            Texto(ComNumero(ref textoTempo, Mathf.FloorToInt(jogo.TempoTotal), t => FormatarTempo(t)), w - margem, linha2, pixel, 1f, Branco);
    }

    // Os itens da Loja, ao lado das mortes: o escudo (com "x2" se tiver mais de um), o elixir (na fase, só se
    // estiver valendo, piscando; no mapa, quantos você tem) e a bota do duplo salto.
    void DesenharItensNoHud(Rect areaDasMortes)
    {
        float alto = areaDasMortes.height * 1.15f, x = areaDasMortes.xMax + 14f * escala, y = areaDasMortes.center.y - alto / 2f;
        if (jogo.TemEscudo)
        {
            float largo = alto * 11f / 12f;
            GUI.DrawTexture(new Rect(x, y, largo, alto), FabricaDeSprites.Pegar("escudo").texture);
            x += largo + 6f * escala;
            if (jogo.Escudos > 1)
                x = Texto(ComNumero(ref textoDosEscudos, jogo.Escudos, n => "x" + n), x, areaDasMortes.y, 2.5f, 0f, Branco).xMax + 10f * escala;
        }
        bool naFase = jogo.Estado != EstadoDoJogo.Mapa;
        if (naFase ? jogo.ElixirAtivo : jogo.Elixires > 0)
        {
            float largo = alto * 12f / 13f;
            Color antes = GUI.color;
            if (naFase) GUI.color = new Color(1f, 1f, 1f, 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f));
            GUI.DrawTexture(new Rect(x, y, largo, alto), FabricaDeSprites.Pegar("elixir").texture);
            GUI.color = antes;
            x += largo + 6f * escala;
            if (!naFase && jogo.Elixires > 1)
                x = Texto(ComNumero(ref textoDosElixires, jogo.Elixires, n => "x" + n), x, areaDasMortes.y, 2.5f, 0f, Branco).xMax + 10f * escala;
        }
        if (jogo.TemPuloDuplo)
            GUI.DrawTexture(new Rect(x, y, alto * 14f / 12f, alto), FabricaDeSprites.Pegar("bota_alada").texture);
    }

    void DesenharTitulo()
    {
        float w = Screen.width, h = Screen.height;
        // a ilustração cobre a tela inteira (corta as sobras); por cima, um véu escuro mais leve para o menu ler bem
        if (fundoDoTitulo == null) fundoDoTitulo = Resources.Load<Texture2D>("Arte/titulo_fundo");
        if (fundoDoTitulo != null)
        {
            GUI.DrawTexture(new Rect(0, 0, w, h), fundoDoTitulo, ScaleMode.ScaleAndCrop);
            Color antes = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
            GUI.color = antes;
        }
        else GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        // o logo: letras douradas com a borda de madeira escura (o nome fica em Textos.NomeDoJogo)
        DesenharTextoEmPixel(FabricaDeSprites.TextoEmPixel(Idioma.Traduzir(Textos.NomeDoJogo), CorDoLogo, CorDaBordaDoLogo, CorDaBordaDoLogo, comAcentos: true), w / 2f, h * 0.07f, 130f * escala);
        Texto("toda morte é só o começo da viagem (de novo, e de novo...)", w / 2f, h * 0.07f + 140f * escala, 2.5f, 0.5f, Branco, w * 0.9f);

        // até 9 opções: Continuar, Carregar jogo, Novo jogo, Opções, Como jogar, Conquistas, Créditos, Idioma, Sair
        // (com muitas, elas ficam mais juntinhas e um pouco menores, para não encostar no recorde lá embaixo)
        int total = jogo.opcoesDoMenu.Count;
        float y = h * 0.36f, passo = Mathf.Min(40f * escala, (h * 0.78f - y) / Mathf.Max(1, total));
        float tamanho = passo < 38f * escala ? 2.6f : 3f;
        for (int i = 0; i < total; i++)
        {
            if (jogo.opcoesDoMenu[i] == GerenciadorDoJogo.OpcaoDoMenu.TrocarIdioma)
                OpcaoDasBandeiras(y, tamanho, passo, i, i == jogo.OpcaoSelecionada); // o idioma: as bandeiras
            else OpcaoDeMenu(TextoDaOpcao(jogo.opcoesDoMenu[i]), y, tamanho, passo, i, i == jogo.OpcaoSelecionada);
            y += passo;
        }

        int recorde = Progresso.Recorde;
        if (recorde >= 0)
            Texto(Idioma.T($"Recorde: zerou com {recorde} mortes", $"Record: beat the game with {recorde} deaths"), w / 2f, h * 0.8f, 2f, 0.5f, Verde, w * 0.9f);

        // rodapé discreto com os controles básicos (a lista completa fica em "Como jogar")
        Texto("A/D ou SETAS: andar    ESPAÇO/W: pular    R: reiniciar    ESC: pausa", w / 2f, h * 0.88f, 1.8f, 0.5f, CorDoRodape, w * 0.95f);
        Rodape("SETAS ou MOUSE: escolher    ENTER: confirmar    ESC: voltar");
    }

    // A escolha do idioma no menu do título: as bandeiras do Brasil (Português) e dos EUA (English), lado a lado.
    // A do idioma atual fica acesa, com a moldura dourada; a outra, apagadinha. Clicar numa bandeira escolhe aquele
    // idioma; no teclado, ESQUERDA/DIREITA ou ENTER trocam (como as outras opções, a linha fica selecionada).
    void OpcaoDasBandeiras(float y, float pixel, float alturaDaLinha, int indice, bool selecionada)
    {
        float w = Screen.width;
        // as duas com o mesmo tamanho de "pixel" (pixel art certinha): a do Brasil dá a altura, do tamanho das letras do menu
        float px = 32f * escala * pixel / 3f / ArteDasBandeiras.AlturaDoBrasil;
        float altura = ArteDasBandeiras.AlturaDoBrasil * px, larguraBR = ArteDasBandeiras.LarguraDoBrasil * px;
        float larguraEUA = ArteDasBandeiras.LarguraDosEUA * px, alturaEUA = ArteDasBandeiras.AlturaDosEUA * px;
        float espaco = 28f * escala, total = larguraBR + espaco + larguraEUA;
        float meio = y + 7.5f * pixel * escala; // o meio das letras das outras opções
        var brasil = new Rect(w / 2f - total / 2f, meio - altura / 2f, larguraBR, altura);
        var eua = new Rect(brasil.xMax + espaco, meio - alturaEUA / 2f, larguraEUA, alturaEUA);

        // a linha toda (para o mouse e a faixa), como em OpcaoDeMenu
        var area = new Rect(w / 2f - total / 2f - 60f * escala, meio - alturaDaLinha / 2f, total + 120f * escala, alturaDaLinha);
        if (selecionada)
        {
            DesenharFaixa(area);
            if (Time.unscaledTime % 0.8f < 0.55f)
            {
                Texto(">", brasil.x - 14f * escala, meio - 7.5f * pixel * escala, pixel, 1f, Amarelo);
                Texto("<", eua.xMax + 14f * escala, meio - 7.5f * pixel * escala, pixel, 0f, Amarelo);
            }
        }
        Bandeira(brasil, "bandeira_brasil", !Idioma.Ingles, GerenciadorDoJogo.BotaoBandeiraBrasil);
        Bandeira(eua, "bandeira_eua", Idioma.Ingles, GerenciadorDoJogo.BotaoBandeiraEUA);
        Mouse(area, indice, true); // (depois das bandeiras: o clique numa bandeira já foi "gasto" por ela)
    }

    void Bandeira(Rect lugar, string desenho, bool escolhida, int botao)
    {
        bool emCima = Mouse(lugar, botao, false);
        float pulo = escolhida ? Mathf.Round(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 2f) * escala : 0f;
        lugar.y -= pulo;
        Color antes = GUI.color;
        GUI.color = escolhida ? Color.white : new Color(1f, 1f, 1f, emCima ? 0.75f : 0.4f);
        GUI.DrawTexture(lugar, FabricaDeSprites.Pegar(desenho).texture);
        GUI.color = antes;
        if (escolhida)
        {
            float e = 3f * escala;
            DesenharBorda(new Rect(lugar.x - e, lugar.y - e, lugar.width + 2f * e, lugar.height + 2f * e),
                Color.Lerp(CorDoFilete, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)));
        }
    }

    static string TextoDaOpcao(GerenciadorDoJogo.OpcaoDoMenu opcao)
    {
        switch (opcao)
        {
            case GerenciadorDoJogo.OpcaoDoMenu.Continuar: // (o último slot usado)
            {
                int slot = Progresso.SlotAtual + 1, fase = Progresso.FaseSalva;
                if (fase == Fases.IndiceSecreto) return Idioma.T($"Continuar (slot {slot}, fase secreta)", $"Continue (slot {slot}, secret level)");
                return Idioma.T($"Continuar (slot {slot}, fase {fase + 1})", $"Continue (slot {slot}, level {fase + 1})");
            }
            case GerenciadorDoJogo.OpcaoDoMenu.CarregarJogo: return "Carregar jogo";
            case GerenciadorDoJogo.OpcaoDoMenu.Conquistas:
                return Idioma.T("Conquistas", "Achievements") + $" ({Conquistas.Quantidade}/{Conquistas.Todas.Length})";
            case GerenciadorDoJogo.OpcaoDoMenu.Opcoes: return "Opções";
            case GerenciadorDoJogo.OpcaoDoMenu.ComoJogar: return "Como jogar";
            case GerenciadorDoJogo.OpcaoDoMenu.Creditos: return "Créditos";
            case GerenciadorDoJogo.OpcaoDoMenu.TrocarIdioma: // escrito nas duas línguas: quem não lê português acha fácil
                return Idioma.Ingles ? "Language / Idioma: < English >" : "Idioma / Language: < Português >";
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
        Texto(Idioma.T("CONQUISTAS", "ACHIEVEMENTS") + $" {Conquistas.Quantidade}/{Conquistas.Todas.Length}", w / 2f, 20f * escala, 5f, 0.5f, Amarelo);
        // A lista ROLA: cada conquista numa linha de tamanho fixo (o nome em cima, a descrição embaixo),
        // com as letras sempre do mesmo tamanho. CIMA/BAIXO ou a rodinha do mouse rolam; a barrinha mostra onde está.
        Conquistas.Conquista[] todas = Conquistas.Todas;
        float largura = Mathf.Min(980f * escala, w - 120f * escala);
        var lista = new Rect((w - largura) / 2f, 84f * escala, largura, h - 84f * escala - 100f * escala);
        float alturaDaLinha = 64f * escala;
        float visiveis = lista.height / alturaDaLinha, maximo = Mathf.Max(0f, todas.Length - visiveis);
        jogo.LimitarRolagemDasConquistas(maximo);
        Event e = Event.current;
        if (e.type == EventType.ScrollWheel)
        {
            jogo.RolarConquistas(Mathf.Sign(e.delta.y) * 1.5f);
            e.Use();
        }
        if (e.type == EventType.Repaint) // a rolagem desliza até o lugar (em vez de pular)
            rolagemDasConquistas = Mathf.Lerp(rolagemDasConquistas, jogo.RolagemDasConquistas, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));

        float larguraDoTexto = lista.width - 60f * escala;
        GUI.BeginGroup(lista); // (o que passar das bordas da lista não aparece)
        for (int i = 0; i < todas.Length; i++)
        {
            float y = (i - rolagemDasConquistas) * alturaDaLinha;
            if (y < -alturaDaLinha || y > lista.height) continue;
            Conquistas.Conquista c = todas[i];
            bool tem = Conquistas.Tem(c.id);
            Retangulo(new Rect(0f, y + 3f * escala, lista.width - 24f * escala, alturaDaLinha - 6f * escala),
                new Color(1f, 1f, 1f, tem ? 0.08f : 0.035f));
            Texto((tem ? "* " : "- ") + Idioma.Traduzir(c.nome), 16f * escala, y + 9f * escala, 2.5f, 0f,
                tem ? Amarelo : new Color32(150, 150, 165, 255), larguraDoTexto);
            Texto(c.descricao, 34f * escala, y + 36f * escala, 2f, 0f, tem ? Branco : new Color32(130, 130, 145, 255), larguraDoTexto - 18f * escala);
        }
        GUI.EndGroup();

        // a barrinha de rolagem (só se nem tudo couber)
        if (maximo > 0f)
        {
            var trilho = new Rect(lista.xMax - 10f * escala, lista.y + 3f * escala, 6f * escala, lista.height - 6f * escala);
            Retangulo(trilho, new Color(1f, 1f, 1f, 0.12f));
            float tamanho = trilho.height * Mathf.Clamp01(visiveis / todas.Length);
            float onde = (trilho.height - tamanho) * Mathf.Clamp01(rolagemDasConquistas / maximo);
            Retangulo(new Rect(trilho.x, trilho.y + onde, trilho.width, tamanho), (Color)CorDoNome);
        }

        Texto("CIMA/BAIXO ou RODINHA: rolar", w / 2f, h - 88f * escala, 1.8f, 0.5f, CorDoRodape, w * 0.9f);
        BotaoDeTexto("Voltar (ENTER ou ESC)", w / 2f, h - 54f * escala, 2.2f, 0.5f, GerenciadorDoJogo.BotaoVoltar);
    }

    float rolagemDasConquistas; // a rolagem mostrada (desliza até jogo.RolagemDasConquistas)

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

    static readonly string[] Turma = { "jogador", "sereia", "louro_grande", "fantasma" };

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
            if (titulo) DesenharTextoEmPixel(FabricaDeSprites.TextoEmPixel(Idioma.Traduzir(Textos.NomeDoJogo), CorDoLogo, CorDaBordaDoLogo, CorDaBordaDoLogo, comAcentos: true), w / 2f, yLinha, 70f * escala);
            else Texto(texto, w / 2f, yLinha, secao ? 3f : 2.5f, 0.5f, secao ? Amarelo : Branco, w * 0.9f);
        }

        // Grumete, Sereia, Louro e Dóris pulando na parte de baixo
        for (int i = 0; i < Turma.Length; i++)
        {
            Texture desenho = FabricaDeSprites.Pegar(Turma[i]).texture;
            float ampliar = Turma[i] == "louro_grande" ? 2.4f : 4f; // (o Louro grande tem o dobro de pixels: fica do tamanho da turma)
            float altura = desenho.height * ampliar * escala, largura = desenho.width * ampliar * escala;
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
        if (jogo.LojaAberta)
        {
            DesenharLoja();
            return;
        }
        GUI.DrawTexture(new Rect(0, h - 135f * escala, w, 135f * escala), fundoEscuro);

        if (jogo.NoTutorial)
        {
            DesenharTutorialNoMapa();
            return;
        }
        if (jogo.Fala.HasValue)
        {
            DesenharFalaNoMapa(jogo.Fala.Value);
            return;
        }
        if (!mapa.Andando && jogo.LojaLiberada) BotaoDaLoja(false);
        float larguraDoMeio = w - 2f * (RetanguloDoBotaoDaLoja().width + 40f * escala); // (sem passar por baixo do botão)

        int fase = mapa.Selecionado;
        bool secreta = fase == Fases.IndiceSecreto;
        string nome = secreta ? Fases.Secreta.nome : ComNumero(ref textoDoMapa, fase, f => $"{Idioma.T("Fase", "Level")} {f + 1}: {Idioma.Traduzir(Fases.Todas[f].nome)}");
        Texto(nome, w / 2f, h - 125f * escala, 3.5f, 0.5f, Amarelo, larguraDoMeio);
        // já passou dessa fase alguma vez? Mostra o recorde e as medalhas dela; senão, a situação nesta partida
        PrepararRecordeDoMapa(fase);
        if (temRecordeGuardado && !mapa.Andando) DesenharRecordeNoMapa(h - 80f * escala);
        else
        {
            string situacao = secreta ? "a fase secreta!" : fase < jogo.FaseMaisLonge ? "já passou (dá para jogar de novo)" : "ainda não passou nesta partida";
            Texto(situacao, w / 2f, h - 78f * escala, 2f, 0.5f, secreta ? Rosa : fase < jogo.FaseMaisLonge ? Verde : Lilas, larguraDoMeio);
        }
        string comandos = mapa.Andando ? "..."
            : jogo.LojaLiberada ? "SETAS: andar     ENTER: jogar     L: loja     ESC: título"
            : "SETAS: andar     ENTER: jogar     ESC: título"; // a Loja só aparece depois da 2ª fase
        Texto(comandos, w / 2f, h - 42f * escala, 2f, 0.5f, Branco, larguraDoMeio);
    }

    // ------------------------------------------------------------------ loja

    // O botão da Loja no mapa: no canto de baixo à direita, dentro da faixa escura (com a moeda pulando de leve).
    Rect RetanguloDoBotaoDaLoja()
    {
        float largura = 176f * escala, altura = 92f * escala;
        return new Rect(Screen.width - largura - 18f * escala, Screen.height - altura - 22f * escala, largura, altura);
    }

    // Desenha o botão (e cuida do mouse). destaque: o Louro do tutorial está apontando para ele.
    void BotaoDaLoja(bool destaque)
    {
        Rect botao = RetanguloDoBotaoDaLoja();
        bool emCima = Mouse(botao, GerenciadorDoJogo.BotaoLoja, false);
        float pulso = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
        Moldura(botao, emCima || destaque ? new Color(0.25f, 0.18f, 0.08f, 0.96f) : CorDoFundoDaFala);
        if (emCima || destaque) DesenharBorda(botao, Color.Lerp(CorDoFilete, Color.white, pulso * 0.6f));
        float lado = 40f * escala, pulo = Mathf.Round(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 3f) * escala;
        GUI.DrawTexture(new Rect(botao.x + 18f * escala, botao.center.y - lado / 2f - pulo, lado * 8f / 14f, lado), FabricaDeSprites.Pegar("moeda").texture);
        Texto("LOJA", botao.x + 52f * escala, botao.y + 18f * escala, 3.6f, 0f, emCima || destaque ? Branco : CorDoLogo);
        Texto("tecla L", botao.x + 54f * escala, botao.y + 58f * escala, 1.8f, 0f, CorDoRodape);
    }

    // A Loja: o saldo lá em cima, os três itens à esquerda, a ROLETA no meio e a aposta à direita.
    void DesenharLoja()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        Texto("LOJA DO PORTO", w / 2f, 18f * escala, 5f, 0.5f, CorDoLogo, w * 0.9f);
        DesenharSaldo(w / 2f, 100f * escala);

        // as três colunas: a roleta no meio e as laterais com o espaço que sobrar
        float diametro = Mathf.Min(300f * escala, w * 0.3f), margem = 24f * escala;
        float largura = Mathf.Min(390f * escala, (w - diametro - 4f * margem) / 2f);
        var centro = new Vector2(w / 2f, 330f * escala);
        bool tutorial = jogo.NoTutorial;

        // os itens
        float y = 150f * escala, alto = 112f * escala, passo = alto + 14f * escala;
        CartaoDoItem(new Rect(margem, y, largura, alto), GerenciadorDoJogo.ItemDaLoja.Escudo, "escudo", "ESCUDO", jogo.precoDoEscudo,
            ComNumero(ref textoDaBolsa, jogo.Escudos, n => Idioma.T("tem ", "own ") + $"{n}/{jogo.maximoDeEscudos}"), jogo.TemEscudo,
            "Te salva de UMA morte.", "Guarde até 3.", false);
        CartaoDoItem(new Rect(margem, y + passo, largura, alto), GerenciadorDoJogo.ItemDaLoja.Elixir, "elixir", "ELIXIR", jogo.precoDoElixir,
            ComNumero(ref textoDosElixires, jogo.Elixires, n => Idioma.T("tem ", "own ") + $"{n}/{jogo.maximoDeElixires}"), jogo.Elixires > 0,
            "Pulo bem mais alto. Entrar", "numa fase gasta um.", tutorial);
        CartaoDoItem(new Rect(margem, y + 2f * passo, largura, alto), GerenciadorDoJogo.ItemDaLoja.PuloDuplo, "bota_alada", "DUPLO SALTO",
            jogo.precoDoPuloDuplo, jogo.TemPuloDuplo ? "já tem!" : "", jogo.TemPuloDuplo,
            "Pule de novo no ar.", "Vale a partida toda.", tutorial);

        DesenharRoleta(centro, diametro, tutorial);
        DesenharAposta(new Rect(w - margem - largura, y, largura, 3f * passo - 14f * escala), tutorial);

        // voltar
        float yVoltar = 548f * escala;
        OpcaoDeMenu("Voltar ao mapa", yVoltar, 2.8f, 40f * escala, (int)GerenciadorDoJogo.ItemDaLoja.Voltar,
            jogo.OpcaoDaLoja == (int)GerenciadorDoJogo.ItemDaLoja.Voltar);

        if (tutorial) // a cutscene do Louro: a fala dele no lugar das mensagens e do rodapé
        {
            GerenciadorDoJogo.PassoDoTutorial passoDoLouro = jogo.Tutorial;
            string dica = passoDoLouro == GerenciadorDoJogo.PassoDoTutorial.Comprar ? "ENTER: comprar"
                : passoDoLouro == GerenciadorDoJogo.PassoDoTutorial.Fechar ? "ESC: voltar" : "ENTER >";
            Rect caixa = DesenharCaixaDoTutorial(dica, 1f);
            DesenharLouroVoando(PoleiroNaCaixa(caixa));
            return;
        }

        // a resposta da compra ou da roleta
        string mensagem = jogo.MensagemDaLoja;
        if (mensagem != "")
            Texto(mensagem, w / 2f, 598f * escala, 2.5f, 0.5f, jogo.MensagemDaLojaBoa ? Verde : Rosa, w * 0.9f);
        Rodape(jogo.Girando ? "girando... boa sorte!"
            : "CIMA/BAIXO ou MOUSE: escolher    ESQUERDA/DIREITA: aposta e cor    ENTER: comprar/girar    ESC ou L: voltar");
    }

    // O saldo: [moeda] 23 moedas. Quando a roleta paga, um "+N" dourado sobe do lado.
    void DesenharSaldo(float centroX, float y)
    {
        string saldo = ComNumero(ref textoDoSaldo, jogo.Moedas, n => n == 1 ? Idioma.T("1 moeda", "1 coin") : n + Idioma.T(" moedas", " coins"));
        float icone = 30f * escala;
        Rect medida = MedirTexto(saldo, 0f, y, 3f, 0f);
        float x = centroX - (icone + 10f * escala + medida.width) / 2f;
        GUI.DrawTexture(new Rect(x, y + (medida.height - icone) / 2f, icone * 8f / 14f, icone), FabricaDeSprites.Pegar("moeda").texture);
        Texto(saldo, x + icone + 10f * escala, y, 3f, 0f, Amarelo);
        float t = jogo.TempoDesdeOResultado;
        if (!jogo.Girando && jogo.UltimoPremio > 0 && t < 1.6f)
        {
            Color antes = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01((1.6f - t) / 0.5f));
            Texto(ComNumero(ref textoDoPremio, jogo.UltimoPremio, n => "+" + n), x + icone + 22f * escala + medida.width, y - t * 30f * escala, 3f, 0f, Verde);
            GUI.color = antes;
        }
    }

    // Um cartão de item: o desenho, o nome, o preço, quantos você tem e o que ele faz. Escolhido: borda dourada.
    void CartaoDoItem(Rect cartao, GerenciadorDoJogo.ItemDaLoja item, string sprite, string nome, int preco, string tem, bool temAlgum,
        string linha1, string linha2, bool apagado)
    {
        bool escolhido = jogo.OpcaoDaLoja == (int)item;
        Color antes = GUI.color;
        if (apagado) GUI.color = new Color(1f, 1f, 1f, 0.35f);
        Moldura(cartao, CorDoFundoDaFala);
        if (escolhido) DesenharBorda(cartao, Color.Lerp(CorDoFilete, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)));
        float m = 12f * escala, lado = cartao.height - 2f * m - 8f * escala;
        Texture desenho = FabricaDeSprites.Pegar(sprite).texture;
        float escalaDoDesenho = lado / Mathf.Max(desenho.width, desenho.height);
        float pulo = escolhido ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 5f * escala : 0f;
        GUI.DrawTexture(new Rect(cartao.x + m + 4f * escala + (lado - desenho.width * escalaDoDesenho) / 2f,
            cartao.center.y - desenho.height * escalaDoDesenho / 2f - pulo, desenho.width * escalaDoDesenho, desenho.height * escalaDoDesenho), desenho);
        float x = cartao.x + 2f * m + lado, larguraDoTexto = cartao.xMax - m - x;
        Texto(nome, x, cartao.y + m, 2.6f, 0f, escolhido ? Amarelo : Branco, larguraDoTexto * 0.62f);
        if (tem != "") Texto(tem, cartao.xMax - m, cartao.y + m + 4f * escala, 1.7f, 1f, temAlgum ? Verde : CorDoRodape, larguraDoTexto * 0.36f);
        Texto(linha1, x, cartao.y + m + 34f * escala, 1.6f, 0f, CorDoRodape, larguraDoTexto);
        Texto(linha2, x, cartao.y + m + 54f * escala, 1.6f, 0f, CorDoRodape, larguraDoTexto);
        // o preço, com a moedinha
        float icone = 16f * escala, yPreco = cartao.yMax - m - icone;
        GUI.DrawTexture(new Rect(x, yPreco, icone * 8f / 14f, icone), FabricaDeSprites.Pegar("moeda").texture);
        Texto(preco.ToString(), x + icone * 0.75f, yPreco - 2f * escala, 2f, 0f, jogo.Moedas >= preco ? Amarelo : Rosa);
        GUI.color = antes;
        if (!apagado && !jogo.Girando) Mouse(cartao, (int)item, true);
    }

    // ------------------------------------------------------------------ a roleta (desenho)

    // As moedas que pulam da roleta quando você ganha (só desenho: andam no Repaint, com o tempo real).
    struct MoedaVoando { public Vector2 lugar, velocidade; public float vida, giro; }
    readonly List<MoedaVoando> moedasVoando = new List<MoedaVoando>();
    float resultadoFestejado = -1f; // o TempoDesdeOResultado de quando a chuva de moedas começou (para não repetir)

    void DesenharRoleta(Vector2 centro, float diametro, bool apagada)
    {
        Color antes = GUI.color;
        if (apagada) GUI.color = new Color(1f, 1f, 1f, 0.35f);
        float t = jogo.TempoDesdeOResultado;
        bool acabouDeParar = !jogo.Girando && jogo.UltimaCasa >= 0 && t < 2.5f;
        bool ganhou = acabouDeParar && jogo.UltimoPremio > 0;

        // perdeu: a roleta treme um pouquinho; ganhou: um brilho da cor da casa pulsa em volta
        if (acabouDeParar && !ganhou && t < 0.4f) centro.x += Mathf.Sin(t * 70f) * 5f * escala * (1f - t / 0.4f);
        if (ganhou && Mathf.Repeat(t * 3f, 1f) < 0.6f)
        {
            Color brilho = CorDaCasaNaTela(jogo.UltimaCasa);
            brilho.a = 0.35f * GUI.color.a;
            Retangulo(new Rect(centro.x - diametro * 0.56f, centro.y - diametro * 0.56f, diametro * 1.12f, diametro * 1.12f), brilho);
        }
        // a sombra embaixo
        Retangulo(new Rect(centro.x - diametro * 0.46f + 8f * escala, centro.y - diametro * 0.46f + 10f * escala, diametro * 0.92f, diametro * 0.92f), new Color(0f, 0f, 0f, 0.35f));

        // a roda (girando em volta do centro: o GUI.matrix é guardado e devolvido)
        Matrix4x4 matriz = GUI.matrix;
        GUIUtility.RotateAroundPivot(jogo.AnguloDaRoleta, centro);
        GUI.DrawTexture(new Rect(centro.x - diametro / 2f, centro.y - diametro / 2f, diametro, diametro), FabricaDeSprites.Pegar("roleta").texture);
        GUI.matrix = matriz;

        // o ponteiro: dá um "tranco" para o lado cada vez que um pino passa por ele
        float tamanhoDaCasa = 360f / GerenciadorDoJogo.CasasDaRoleta;
        float fracao = Mathf.Repeat(jogo.AnguloDaRoleta + tamanhoDaCasa / 2f, tamanhoDaCasa) / tamanhoDaCasa;
        float tranco = jogo.Girando ? -22f * Mathf.Pow(1f - fracao, 4f) : 0f;
        Texture ponteiro = FabricaDeSprites.Pegar("ponteiro_da_roleta").texture;
        float pw = 30f * escala, ph = pw * ponteiro.height / ponteiro.width;
        var topo = new Vector2(centro.x, centro.y - diametro / 2f - 8f * escala);
        GUIUtility.RotateAroundPivot(tranco, topo);
        GUI.DrawTexture(new Rect(topo.x - pw / 2f, topo.y, pw, ph), ponteiro);
        GUI.matrix = matriz;

        // no meio: o prêmio, quando você ganha
        if (ganhou)
        {
            int multiplicador = GerenciadorDoJogo.Multiplicador(GerenciadorDoJogo.CorDaCasa(jogo.UltimaCasa));
            float pulso = 1f + 0.15f * Mathf.Sin(t * 14f) * Mathf.Clamp01(1.5f - t);
            Texto(multiplicador == 14 ? "x14!" : "x2!", centro.x, centro.y - 18f * escala * pulso, 4.5f * pulso, 0.5f, Branco);
        }
        DesenharMoedasVoando(centro, ganhou, t);

        // o histórico: as últimas casas, da mais nova (à esquerda, com a borda) para a mais velha
        float lado = 18f * escala, folga = 6f * escala;
        int quantas = jogo.HistoricoDaRoleta.Count;
        float x0 = centro.x - (10 * (lado + folga) - folga) / 2f, yHistorico = centro.y + diametro / 2f + 20f * escala;
        for (int i = 0; i < 10; i++)
        {
            var quadrado = new Rect(x0 + i * (lado + folga), yHistorico, lado, lado);
            if (i >= quantas) { Retangulo(quadrado, new Color(1f, 1f, 1f, 0.08f)); continue; }
            Retangulo(quadrado, CorDaCasaNaTela(jogo.HistoricoDaRoleta[i]));
            if (i == 0) DesenharBorda(quadrado, CorDoFilete);
        }
        GUI.color = antes;
    }

    static Color CorDaCasaNaTela(int casa)
    {
        switch (GerenciadorDoJogo.CorDaCasa(casa))
        {
            case GerenciadorDoJogo.CorDaRoleta.Verde: return new Color32(52, 196, 100, 255);
            case GerenciadorDoJogo.CorDaRoleta.Vermelho: return new Color32(222, 52, 60, 255);
            default: return new Color32(40, 36, 54, 255);
        }
    }

    // Ganhou: moedas pulam da roleta para cima e caem girando (mais moedas para prêmios maiores).
    void DesenharMoedasVoando(Vector2 centro, bool ganhou, float t)
    {
        if (ganhou && t < 0.2f && resultadoFestejado != jogo.HistoricoDaRoleta.Count + jogo.UltimoPremio * 1000f)
        {
            resultadoFestejado = jogo.HistoricoDaRoleta.Count + jogo.UltimoPremio * 1000f;
            int quantas = Mathf.Clamp(10 + jogo.UltimoPremio / 4, 10, 60);
            for (int i = 0; i < quantas; i++)
                moedasVoando.Add(new MoedaVoando
                {
                    lugar = centro,
                    velocidade = new Vector2(Random.Range(-420f, 420f), Random.Range(-720f, -320f)) * escala,
                    vida = Random.Range(1.1f, 1.8f),
                    giro = Random.Range(0f, 10f),
                });
        }
        if (Event.current.type == EventType.Repaint) // (o OnGUI roda várias vezes por quadro: anda uma vez só)
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = moedasVoando.Count - 1; i >= 0; i--)
            {
                MoedaVoando m = moedasVoando[i];
                m.velocidade.y += 1500f * escala * dt;
                m.lugar += m.velocidade * dt;
                m.vida -= dt;
                m.giro += dt * 12f;
                if (m.vida <= 0f) moedasVoando.RemoveAt(i);
                else moedasVoando[i] = m;
            }
        }
        Texture moeda = FabricaDeSprites.Pegar("moeda").texture;
        float alto = 22f * escala;
        foreach (MoedaVoando m in moedasVoando)
        {
            float largura = alto * 8f / 14f * Mathf.Abs(Mathf.Cos(m.giro)); // girando: fica fininha e volta
            GUI.DrawTexture(new Rect(m.lugar.x - largura / 2f, m.lugar.y - alto / 2f, Mathf.Max(1f, largura), alto), moeda);
        }
    }

    // A aposta, à direita da roleta: o valor (com - e +), as três cores (com quanto cada uma paga) e o GIRAR.
    void DesenharAposta(Rect area, bool apagada)
    {
        Color antes = GUI.color;
        if (apagada) GUI.color = new Color(1f, 1f, 1f, 0.35f);
        Moldura(area, CorDoFundoDaFala);
        float m = 16f * escala, x = area.x + m, larguraDentro = area.width - 2f * m;
        bool livre = !apagada && !jogo.Girando;
        Texto("ROLETA", area.center.x, area.y + m, 3f, 0.5f, CorDoLogo, larguraDentro);

        // o valor da aposta: [-]  10  [+]
        float yAposta = area.y + 62f * escala, alto = 44f * escala;
        var linha = new Rect(x, yAposta, larguraDentro, alto);
        bool escolhida = jogo.OpcaoDaLoja == (int)GerenciadorDoJogo.ItemDaLoja.Aposta;
        if (escolhida) DesenharFaixa(linha);
        var menos = new Rect(x, yAposta, alto, alto);
        var mais = new Rect(linha.xMax - alto, yAposta, alto, alto);
        BotaoQuadrado(menos, "-", livre ? GerenciadorDoJogo.BotaoApostaMenos : 0, livre);
        BotaoQuadrado(mais, "+", livre ? GerenciadorDoJogo.BotaoApostaMais : 0, livre);
        Texto(ComNumero(ref textoDaAposta, jogo.Aposta, n => Idioma.T("aposta ", "bet ") + n), linha.center.x, yAposta + 10f * escala, 2.5f, 0.5f,
            jogo.Moedas >= jogo.Aposta ? Amarelo : Rosa, larguraDentro - 2f * alto - 8f * escala);

        // as cores
        float yCores = yAposta + alto + 18f * escala, largo = (larguraDentro - 2f * 8f * escala) / 3f, altoDaCor = 64f * escala;
        var cores = new[] { GerenciadorDoJogo.CorDaRoleta.Vermelho, GerenciadorDoJogo.CorDaRoleta.Preto, GerenciadorDoJogo.CorDaRoleta.Verde };
        var nomes = new[] { "VERM.", "PRETO", "VERDE" };
        var botoes = new[] { GerenciadorDoJogo.BotaoVermelho, GerenciadorDoJogo.BotaoPreto, GerenciadorDoJogo.BotaoVerde };
        var casas = new[] { 1, 2, 0 }; // uma casa de cada cor (só para pegar a cor do desenho)
        bool corEscolhida = jogo.OpcaoDaLoja == (int)GerenciadorDoJogo.ItemDaLoja.Cor;
        for (int i = 0; i < 3; i++)
        {
            var botao = new Rect(x + i * (largo + 8f * escala), yCores, largo, altoDaCor);
            bool essa = jogo.CorEscolhida == cores[i];
            bool emCima = livre && Mouse(botao, botoes[i], false);
            Retangulo(botao, Color.Lerp(CorDaCasaNaTela(casas[i]), Color.black, essa || emCima ? 0f : 0.4f));
            DesenharBorda(botao, essa ? (corEscolhida ? Color.Lerp(CorDoFilete, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) : CorDoFilete)
                                      : new Color(0f, 0f, 0f, 0.6f));
            Texto(nomes[i], botao.center.x, botao.y + 10f * escala, 1.8f, 0.5f, Branco, largo - 8f * escala);
            Texto(cores[i] == GerenciadorDoJogo.CorDaRoleta.Verde ? "x14" : "x2", botao.center.x, botao.y + 36f * escala, 2.2f, 0.5f,
                essa ? Amarelo : Branco, largo - 8f * escala);
        }

        // GIRAR!
        float yGirar = yCores + altoDaCor + 22f * escala;
        var girar = new Rect(x, yGirar, larguraDentro, 56f * escala);
        bool girarEscolhido = jogo.OpcaoDaLoja == (int)GerenciadorDoJogo.ItemDaLoja.Girar;
        bool emCimaDoGirar = livre && Mouse(girar, (int)GerenciadorDoJogo.ItemDaLoja.Girar, true);
        float pulso = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
        Moldura(girar, jogo.Girando ? new Color(0.2f, 0.2f, 0.25f, 0.95f)
            : girarEscolhido || emCimaDoGirar ? new Color(0.55f, 0.32f, 0.08f, 0.98f) : new Color(0.35f, 0.2f, 0.06f, 0.95f));
        if (girarEscolhido) DesenharBorda(girar, Color.Lerp(CorDoFilete, Color.white, pulso));
        Texto(jogo.Girando ? "GIRANDO..." : "GIRAR!", girar.center.x, girar.y + 14f * escala, 3f, 0.5f,
            jogo.Girando ? CorDoRodape : Amarelo, larguraDentro);

        Texto("vermelho e preto pagam 2x", area.center.x, girar.yMax + 16f * escala, 1.6f, 0.5f, CorDoRodape, larguraDentro);
        Texto("o verde (só uma casa) paga 14x", area.center.x, girar.yMax + 38f * escala, 1.6f, 0.5f, CorDoRodape, larguraDentro);
        GUI.color = antes;
    }

    // Um botãozinho quadrado com um sinal ("-", "+"). botao = o número do clique (0 = desligado).
    void BotaoQuadrado(Rect area, string sinal, int botao, bool ligado)
    {
        bool emCima = ligado && Mouse(area, botao, false);
        Moldura(area, emCima ? new Color(0.45f, 0.3f, 0.1f, 0.98f) : new Color(0.25f, 0.16f, 0.07f, 0.95f));
        Texto(sinal, area.center.x, area.y + 10f * escala, 3f, 0.5f, ligado ? Branco : CorDoRodape);
    }

    // ------------------------------------------------------------------ tutorial da loja (a cutscene do Louro)

    // No mapa: o Louro chega voando do canto de cima e pousa na caixa de fala; depois voa até o botão LOJA
    // e fica apontando para ele (com uma seta piscando) até você apertar L.
    void DesenharTutorialNoMapa()
    {
        float w = Screen.width;
        GerenciadorDoJogo.PassoDoTutorial passo = jogo.Tutorial;
        bool chegando = passo == GerenciadorDoJogo.PassoDoTutorial.Chegada;
        // a caixa aparece suave quando ele termina de chegar
        float alfa = chegando ? Mathf.Clamp01((jogo.TempoNoPasso - (GerenciadorDoJogo.DuracaoDaChegadaDoLouro - 0.35f)) / 0.35f) : 1f;
        bool apontando = passo == GerenciadorDoJogo.PassoDoTutorial.AbrirLoja;
        Rect caixa = DesenharCaixaDoTutorial(apontando ? "aperte L" : "ENTER >", alfa);
        if (!apontando)
        {
            DesenharLouroVoando(PoleiroNaCaixa(caixa));
            return;
        }
        Rect botao = RetanguloDoBotaoDaLoja();
        BotaoDaLoja(true);
        float vaiEVolta = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 8f * escala;
        Texto(">", botao.x - 10f * escala - vaiEVolta, botao.center.y - 14f * escala, 3f, 1f, Amarelo);
        DesenharLouroVoando(new Vector2(botao.center.x, botao.y - 4f * escala)); // pousado em cima do botão
    }

    // A fala do Louro no tutorial: no mesmo lugar das falas do mapa (a faixa de baixo).
    Rect DesenharCaixaDoTutorial(string dica, float alfa)
    {
        float w = Screen.width, h = Screen.height;
        float largura = Mathf.Min(900f * escala, w - 2f * (RetanguloDoBotaoDaLoja().width + 40f * escala)), x = (w - largura) / 2f, y = h - 112f * escala;
        float altura = 100f * escala;
        if (alfa > 0f) altura = CaixaDeFala(x, y, largura, "Louro", "louro", jogo.TextoDoTutorial, alfa, dica, altura);
        return new Rect(x, y, largura, altura);
    }

    // Onde o Louro pousa: em cima da moldura da caixa de fala, perto do canto direito.
    Vector2 PoleiroNaCaixa(Rect caixa) => new Vector2(caixa.xMax - 70f * escala, caixa.y + 4f * escala);

    // O Louro do tutorial, grandão, voando de um poleiro para outro. "pes" = onde ficam os pés dele.
    // Voando, ele "bate as asas" (estica e encolhe rápido); pousado, dá pulinhos. O desenho olha para a esquerda.
    Vector2 posicaoDoLouro;
    bool louroNaTela;

    void DesenharLouroVoando(Vector2 pes)
    {
        if (!louroNaTela) // começa fora da tela, no canto de cima à direita
        {
            posicaoDoLouro = new Vector2(Screen.width + 80f * escala, -40f * escala);
            louroNaTela = true;
        }
        Vector2 antes = posicaoDoLouro;
        if (Event.current.type == EventType.Repaint) // (o OnGUI roda várias vezes por quadro: anda só uma)
        {
            float distancia = Vector2.Distance(posicaoDoLouro, pes);
            float velocidade = Mathf.Max(500f * escala, distancia * 3.5f); // rápido longe, freando perto
            posicaoDoLouro = Vector2.MoveTowards(posicaoDoLouro, pes, velocidade * Time.unscaledDeltaTime);
        }
        bool voando = Vector2.Distance(posicaoDoLouro, pes) > 1f;
        Texture desenho = FabricaDeSprites.Pegar("louro_grande").texture;
        float alto = 76f * escala, largo = alto * desenho.width / desenho.height;
        float asas = voando ? Mathf.Sin(Time.unscaledTime * 32f) : 0f;
        float pulinho = voando ? 0f : Mathf.Round(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 2f) * 3f * escala;
        float lx = largo * (1f + 0.1f * asas), ly = alto * (1f - 0.14f * asas);
        var area = new Rect(posicaoDoLouro.x - lx / 2f, posicaoDoLouro.y - ly - pulinho, lx, ly);
        bool paraDireita = voando && pes.x > antes.x + 0.5f;
        if (paraDireita) GUI.DrawTextureWithTexCoords(area, desenho, new Rect(1f, 0f, -1f, 1f)); // virado
        else GUI.DrawTexture(area, desenho);
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
        else if (jogo.SubmenuAberto == GerenciadorDoJogo.Submenu.Slots) DesenharSlots();
        else DesenharComoJogar();
    }

    // ------------------------------------------------------------------ os slots (Carregar jogo / Novo jogo)

    // Os textos de cada cartão, montados só quando os slots mudam (ou o idioma): [slot, 0] = a fase, [slot, 1] = os números.
    readonly string[,] textosDosSlots = new string[Progresso.TotalDeSlots, 4]; // fase, mortes, moedas, tempo e data
    int versaoDosTextosDosSlots = -1;
    Idioma.Lingua idiomaDosSlots;

    void PrepararTextosDosSlots()
    {
        if (versaoDosTextosDosSlots == jogo.VersaoDosSlots && idiomaDosSlots == Idioma.Atual) return;
        versaoDosTextosDosSlots = jogo.VersaoDosSlots;
        idiomaDosSlots = Idioma.Atual;
        for (int i = 0; i < Progresso.TotalDeSlots; i++)
        {
            Progresso.ResumoDoSlot r = jogo.ResumoDoSlot(i);
            if (!r.temJogo)
            {
                textosDosSlots[i, 0] = textosDosSlots[i, 1] = textosDosSlots[i, 2] = textosDosSlots[i, 3] = "";
                continue;
            }
            textosDosSlots[i, 0] = r.fase == Fases.IndiceSecreto ? Idioma.Traduzir(Fases.Secreta.nome)
                : r.fase >= 0 && r.fase < Fases.Todas.Length ? $"{Idioma.T("Fase", "Level")} {r.fase + 1}: {Idioma.Traduzir(Fases.Todas[r.fase].nome)}" : "?";
            if (r.dificil) textosDosSlots[i, 0] = Idioma.T("TEMPESTADE - ", "STORM - ") + textosDosSlots[i, 0];
            // (as mortes e as moedas aparecem com os desenhos: a caveirinha do mapa e a moeda)
            textosDosSlots[i, 1] = r.mortes.ToString();
            textosDosSlots[i, 2] = r.moedas.ToString();
            textosDosSlots[i, 3] = $"{FormatarTempo(r.tempo)}    {FormatarData(r.data)}";
        }
    }

    // "2026-11-05" -> "05/11/26" (em inglês, "11/05/26"). Sem data (save de antes dos slots): nada.
    static string FormatarData(string data)
    {
        string[] partes = string.IsNullOrEmpty(data) ? null : data.Split('-');
        if (partes == null || partes.Length != 3 || partes[0].Length < 2) return "";
        string ano = partes[0].Substring(partes[0].Length - 2);
        return Idioma.Ingles ? $"{partes[1]}/{partes[2]}/{ano}" : $"{partes[2]}/{partes[1]}/{ano}";
    }

    void DesenharSlots()
    {
        float w = Screen.width, h = Screen.height;
        bool novo = jogo.SlotsParaNovoJogo, perguntando = jogo.SlotPerguntando >= 0;
        PrepararTextosDosSlots();
        Texto(novo ? "NOVO JOGO" : "CARREGAR JOGO", w / 2f, h * 0.05f, 5f, 0.5f, Lilas, w * 0.9f);
        Texto(novo ? "Escolha um slot para a partida nova." : "Escolha a partida que quer continuar.",
            w / 2f, h * 0.05f + 54f * escala, 2f, 0.5f, CorDoRodape, w * 0.9f);

        // (cartões altos o bastante para as três linhas caberem DENTRO da moldura, com folga embaixo)
        float largura = Mathf.Min(760f * escala, w - 80f * escala), alto = 128f * escala, passo = alto + 12f * escala;
        float x = (w - largura) / 2f, y = 128f * escala;
        for (int i = 0; i < Progresso.TotalDeSlots; i++)
            CartaoDoSlot(new Rect(x, y + i * passo, largura, alto), i, novo, perguntando);

        int voltar = Progresso.TotalDeSlots;
        OpcaoDeMenu("Voltar", y + voltar * passo + 6f * escala, 2.8f, 40f * escala, voltar, !perguntando && jogo.SlotSelecionado == voltar);

        if (perguntando) DesenharPerguntaDoSlot();
        else Rodape("SETAS ou MOUSE: escolher    ENTER: confirmar    DEL: apagar    ESC: voltar");
    }

    // Um cartão: "SLOT 1", a fase em que o Grumete está e os números (mortes, moedas, tempo, data), ou "vazio".
    // Carregando, os vazios ficam apagadinhos (não dá para escolher).
    void CartaoDoSlot(Rect cartao, int slot, bool novo, bool perguntando)
    {
        Progresso.ResumoDoSlot r = jogo.ResumoDoSlot(slot);
        bool escolhido = jogo.SlotSelecionado == slot || jogo.SlotPerguntando == slot;
        Color antes = GUI.color;
        if (!r.temJogo && !novo) GUI.color = new Color(1f, 1f, 1f, 0.4f);
        Moldura(cartao, CorDoFundoDaFala);
        if (escolhido) DesenharBorda(cartao, Color.Lerp(CorDoFilete, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)));

        float m = 16f * escala, lado = cartao.height - 2f * m;
        // o Grumete (só se tiver partida), pulando de leve no cartão escolhido
        if (r.temJogo)
        {
            Texture grumete = FabricaDeSprites.Pegar("jogador").texture;
            float escalaDoDesenho = lado * 0.8f / Mathf.Max(grumete.width, grumete.height);
            float pulo = escolhido ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 5f * escala : 0f;
            GUI.DrawTexture(new Rect(cartao.x + m + (lado - grumete.width * escalaDoDesenho) / 2f,
                cartao.center.y - grumete.height * escalaDoDesenho / 2f - pulo, grumete.width * escalaDoDesenho, grumete.height * escalaDoDesenho), grumete);
        }
        // as três linhas, de cima para baixo, todas dentro da moldura (a de baixo termina antes da borda)
        // e no máximo da largura que sobra (texto comprido encolhe, não sai do cartão)
        float xTexto = cartao.x + 2f * m + lado, larguraDoTexto = cartao.xMax - m - xTexto;
        float yNome = cartao.y + m - 4f * escala, yFase = cartao.y + 0.40f * cartao.height, yNumeros = cartao.y + 0.66f * cartao.height;
        Texto("SLOT " + (slot + 1), xTexto, yNome, 2.6f, 0f, escolhido ? Amarelo : Branco, larguraDoTexto * 0.6f);
        if (r.temJogo)
        {
            Texto(textosDosSlots[slot, 0], xTexto, yFase, 2f, 0f, Branco, larguraDoTexto);
            // [caveira] 23   [moeda] 41   12:34   05/11/26
            const float pixelDosNumeros = 1.8f;
            float icone = 20f * escala, meio = yNumeros + 7.5f * pixelDosNumeros * escala, x = xTexto;
            x = IconeComNumero("caveira", textosDosSlots[slot, 1], x, meio, icone, pixelDosNumeros) + 22f * escala;
            x = IconeComNumero("moeda", textosDosSlots[slot, 2], x, meio, icone, pixelDosNumeros) + 22f * escala;
            Texto(textosDosSlots[slot, 3], x, yNumeros, pixelDosNumeros, 0f, CorDoRodape, Mathf.Max(10f, cartao.xMax - m - x));
        }
        else Texto(novo ? "- vazio: começa aqui -" : "- vazio -", xTexto, yFase + 8f * escala, 2f, 0f, CorDoRodape, larguraDoTexto);
        GUI.color = antes;

        if (perguntando) return; // com a pergunta aberta, o mouse é só dela
        // o "apagar" (antes do cartão: o clique nele não vale como clique no cartão)
        if (r.temJogo) BotaoDaLixeira(new Rect(cartao.xMax - m - 30f * escala, cartao.y + m - 4f * escala, 30f * escala, 34f * escala),
            GerenciadorDoJogo.BotaoApagarSlot - slot);
        if (r.temJogo || novo) Mouse(cartao, slot, true);
    }

    // Um desenho do jogo (do tamanho "lado" de altura) e um número ao lado, centrados na altura "meio".
    // Devolve onde terminou (para o próximo vir depois).
    float IconeComNumero(string desenho, string numero, float x, float meio, float lado, float pixel)
    {
        Texture textura = FabricaDeSprites.Pegar(desenho).texture;
        float largura = lado * textura.width / textura.height;
        GUI.DrawTexture(new Rect(x, meio - lado / 2f, largura, lado), textura);
        x += largura + 6f * escala;
        return Texto(numero, x, meio - 7.5f * pixel * escala, pixel, 0f, Branco).xMax;
    }

    // A lixeira (apaga o slot): fica vermelhinha e um pouco maior com o mouse em cima.
    void BotaoDaLixeira(Rect area, int botao)
    {
        bool emCima = Mouse(area, botao, false);
        if (emCima) DesenharFaixa(area);
        Texture lixeira = FabricaDeSprites.Pegar("lixeira").texture;
        float altura = area.height * (emCima ? 0.95f : 0.82f), largura = altura * lixeira.width / lixeira.height;
        Color antes = GUI.color;
        if (emCima) GUI.color = new Color(1f, 0.55f, 0.55f, antes.a);
        GUI.DrawTexture(new Rect(area.center.x - largura / 2f, area.center.y - altura / 2f, largura, altura), lixeira);
        GUI.color = antes;
    }

    // "Apagar o Slot 2?" ou "Começar um jogo novo no Slot 2?", com SIM e NÃO.
    void DesenharPerguntaDoSlot()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        float largura = Mathf.Min(720f * escala, w - 60f * escala), altura = 190f * escala;
        var caixa = new Rect((w - largura) / 2f, (h - altura) / 2f, largura, altura);
        Moldura(caixa, CorDoFundoDaFala);
        int n = jogo.SlotPerguntando + 1;
        string pergunta = jogo.PerguntaDeApagar ? Idioma.T($"Apagar a partida do Slot {n}?", $"Delete the game in Slot {n}?")
            : Idioma.T($"Começar um jogo novo no Slot {n}?", $"Start a new game in Slot {n}?");
        Texto(pergunta, caixa.center.x, caixa.y + 24f * escala, 2.8f, 0.5f, Amarelo, largura - 40f * escala);
        Texto(jogo.PerguntaDeApagar ? "Os recordes e as medalhas desse slot também somem. As conquistas ficam." : "A partida, os recordes e as medalhas dele vão ser apagados.",
            caixa.center.x, caixa.y + 72f * escala, 1.9f, 0.5f, Branco, largura - 40f * escala);
        BotaoDeTexto("SIM (ENTER)", caixa.center.x - largura * 0.22f, caixa.yMax - 56f * escala, 2.6f, 0.5f, GerenciadorDoJogo.BotaoSim);
        BotaoDeTexto("NÃO (ESC)", caixa.center.x + largura * 0.22f, caixa.yMax - 56f * escala, 2.6f, 0.5f, GerenciadorDoJogo.BotaoNao);
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
            case GerenciadorDoJogo.ItemDasOpcoes.Musica: return Idioma.T("Música: < ", "Music: < ") + Barra(Opcoes.Musica) + " >";
            case GerenciadorDoJogo.ItemDasOpcoes.Efeitos: return Idioma.T("Efeitos: < ", "Sound FX: < ") + Barra(Opcoes.Efeitos) + " >";
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
        Texto(ComNumero(ref textoDaPagina, p, n => $"{Idioma.Traduzir(Textos.ComoJogar[n].titulo)}  ({n + 1}/{Textos.ComoJogar.Length})"),
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

    // Fim da fase: o título, as 3 medalhas e, embaixo, o painel com os melhores tempos da fase.
    // Fica na tela até o jogador apertar ENTER (veja GerenciadorDoJogo.PodeContinuar).
    void DesenharFaseConcluida()
    {
        float w = Screen.width, h = Screen.height;
        if (jogo.EhUltimaFase && jogo.Sereia != null && !jogo.MostrandoPainel)
        {
            // última fase: primeiro a Sereia comemora (como antes); o painel vem depois
            DesenharFalaDaSereia();
            DesenharResultado(h * 0.66f);
            return;
        }
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro); // véu escuro: o painel fica fácil de ler
        float y = 48f * escala;
        Texto("FASE CONCLUÍDA!", w / 2f, y, 5f, 0.5f, Verde, w * 0.9f);
        string comentario = jogo.MortesNaFase == 0 ? "Sem morrer?! Impossível." : Idioma.T($"Só {jogo.MortesNaFase} morte(s) nessa fase.", $"Only {jogo.MortesNaFase} death(s) in this level.");
        Texto(comentario, w / 2f, y + 82f * escala, 2.5f, 0.5f, Branco, w * 0.9f);
        DesenharResultado(y + 128f * escala, avisoDePrimeiraVez: false); // o painel já diz que é a primeira vez
        DesenharPainelDeTempos(y + 286f * escala);
        if (jogo.PodeContinuar)
            BotaoDeTexto("ENTER: continuar", w / 2f, h - 62f * escala, 2.2f, 0.5f, GerenciadorDoJogo.BotaoProxima);
    }

    // ------------------------------------------------------------------ painel de melhores tempos

    // Textos do painel, montados uma vez por fase concluída (não a cada quadro).
    int numeroDoPainel = -1;
    readonly string[,] celulasDoPainel = new string[Progresso.TamanhoDoRanking, 4]; // posição, tempo, mortes, data
    readonly bool[] linhaOcupada = new bool[Progresso.TamanhoDoRanking];
    string vezesDoPainel, resumoDoPainel;
    Color32 corDoResumo;
    static readonly Color32 Apagado = new Color32(110, 110, 128, 255);
    // Onde fica cada coluna (fração da largura do painel) e o alinhamento do texto nela.
    static readonly float[] ColunasDoPainel = { 0.05f, 0.30f, 0.53f, 0.74f };
    static readonly float[] AlinhamentoDasColunas = { 0f, 0.5f, 0.5f, 0.5f };
    static readonly string[] TitulosDasColunas = { "pos.", "tempo", "mortes", "data" };

    void PrepararPainelDeTempos(GerenciadorDoJogo.ResultadoDaFase r)
    {
        if (r.numero == numeroDoPainel) return;
        numeroDoPainel = r.numero;
        List<Progresso.TempoDoRanking> ranking = r.ranking ?? new List<Progresso.TempoDoRanking>();
        for (int i = 0; i < Progresso.TamanhoDoRanking; i++)
        {
            linhaOcupada[i] = i < ranking.Count;
            celulasDoPainel[i, 0] = Posicao(i + 1);
            if (!linhaOcupada[i])
            {
                celulasDoPainel[i, 1] = Idioma.T("--:--,-", "--:--.-");
                celulasDoPainel[i, 2] = celulasDoPainel[i, 3] = "-";
                continue;
            }
            Progresso.TempoDoRanking t = ranking[i];
            celulasDoPainel[i, 1] = FormatarTempoFino(t.tempo);
            celulasDoPainel[i, 2] = t.mortes < 0 ? "-" : t.mortes.ToString();
            celulasDoPainel[i, 3] = string.IsNullOrEmpty(t.data) ? "-" : t.data;
        }

        bool primeira = (r.novidades & Progresso.Novidade.Primeira) != 0;
        vezesDoPainel = r.vezesConcluida <= 1 ? "primeira vez aqui!" : Idioma.T($"concluída {r.vezesConcluida} vezes", $"cleared {r.vezesConcluida} times");
        int ultimo = ranking.Count - 1;
        if (primeira)
        {
            resumoDoPainel = "Primeiro tempo salvo! Volte para melhorar.";
            corDoResumo = Lilas;
        }
        else if (r.posicaoNoRanking == 0)
        {
            // bateu o recorde: quanto mais rápido que o antigo 1º lugar (que agora é o 2º)
            resumoDoPainel = ranking.Count > 1 ? Idioma.T($"NOVO MELHOR TEMPO! {FormatarDiferenca(ranking[1].tempo - r.tempo)} mais rápido", $"NEW BEST TIME! {FormatarDiferenca(ranking[1].tempo - r.tempo)} faster") : "NOVO MELHOR TEMPO!";
            corDoResumo = Verde;
        }
        else if (r.posicaoNoRanking > 0)
        {
            resumoDoPainel = Idioma.T($"{r.posicaoNoRanking + 1}º melhor tempo (+{FormatarDiferenca(r.tempo - ranking[0].tempo)} do recorde)",
                $"{Posicao(r.posicaoNoRanking + 1)} best time (+{FormatarDiferenca(r.tempo - ranking[0].tempo)} behind the record)");
            corDoResumo = Amarelo;
        }
        else
        {
            resumoDoPainel = ultimo >= 0
                ? Idioma.T($"Fora do top {Progresso.TamanhoDoRanking}: faltou {FormatarDiferenca(r.tempo - ranking[ultimo].tempo)}",
                           $"Not in the top {Progresso.TamanhoDoRanking}: missed by {FormatarDiferenca(r.tempo - ranking[ultimo].tempo)}")
                : Idioma.T($"Fora do top {Progresso.TamanhoDoRanking}", $"Not in the top {Progresso.TamanhoDoRanking}");
            corDoResumo = Rosa;
        }
    }

    // O painel: "MELHORES TEMPOS", as colunas (posição, tempo, mortes, data) e os 5 tempos mais rápidos da fase.
    // A linha do tempo que você acabou de fazer fica amarela, com a faixa lilás e um "< VOCÊ" na ponta.
    void DesenharPainelDeTempos(float y)
    {
        GerenciadorDoJogo.ResultadoDaFase r = jogo.Resultado;
        PrepararPainelDeTempos(r);
        float w = Screen.width;
        float largura = Mathf.Min(w * 0.7f, 620f * escala), x0 = (w - largura) / 2f;
        float folga = 12f * escala, passo = 31f * escala;
        var caixa = new Rect(x0, y, largura, 292f * escala);
        GUI.DrawTexture(caixa, fundoEscuro);
        GUI.DrawTexture(caixa, fundoEscuro);
        DesenharBorda(caixa, new Color32(226, 176, 78, 255)); // o mesmo filete dourado das caixas de fala

        float yTopo = y + folga;
        Texto("MELHORES TEMPOS", x0 + folga, yTopo, 2.5f, 0f, Amarelo, largura * 0.6f);
        Texto(vezesDoPainel, x0 + largura - folga, yTopo + 8f * escala, 1.6f, 1f, CorDoRodape, largura * 0.38f);

        float yColunas = yTopo + 44f * escala;
        for (int c = 0; c < 4; c++)
            Texto(TitulosDasColunas[c], x0 + largura * ColunasDoPainel[c], yColunas, 1.6f, AlinhamentoDasColunas[c], CorDoRodape);

        float yLinha = yColunas + 30f * escala;
        for (int i = 0; i < Progresso.TamanhoDoRanking; i++)
        {
            bool voce = i == r.posicaoNoRanking;
            if (voce) DesenharFaixa(new Rect(x0 + 4f * escala, yLinha - 2f * escala, largura - 8f * escala, passo - 2f * escala));
            Color32 cor = !linhaOcupada[i] ? Apagado : voce ? Amarelo : i == 0 ? CorDoLogo : Branco;
            for (int c = 0; c < 4; c++)
                Texto(celulasDoPainel[i, c], x0 + largura * ColunasDoPainel[c], yLinha, 1.9f, AlinhamentoDasColunas[c], cor);
            if (voce) Texto("< você", x0 + largura - folga, yLinha, 1.9f, 1f, Amarelo);
            yLinha += passo;
        }

        Texto(resumoDoPainel, w / 2f, yLinha + 8f * escala, 2f, 0.5f, corDoResumo, largura - 2f * folga);
    }

    // Moldura fininha (2 px na tela de 720p) em volta de uma caixa.
    void DesenharBorda(Rect caixa, Color cor)
    {
        float e = Mathf.Max(1f, 2f * escala);
        Color corOriginal = GUI.color;
        GUI.color = cor;
        GUI.DrawTexture(new Rect(caixa.x, caixa.y, caixa.width, e), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(caixa.x, caixa.yMax - e, caixa.width, e), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(caixa.x, caixa.y, e, caixa.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(caixa.xMax - e, caixa.y, e, caixa.height), Texture2D.whiteTexture);
        GUI.color = corOriginal;
    }

    // Diferença de tempo: "3,2s" (ou "01:05,3" se passar de um minuto).
    static string FormatarDiferenca(float segundos)
    {
        if (segundos >= 60f) return FormatarTempoFino(segundos);
        int decimos = Mathf.Max(0, Mathf.RoundToInt(segundos * 10f));
        return $"{decimos / 10}{Virgula}{decimos % 10}s";
    }

    // ------------------------------------------------------------------ recordes e medalhas

    // Textos da tela de resultado, montados uma vez só (não a cada quadro).
    readonly string[] textosDoResultado = new string[3];
    readonly string[] metasDoResultado = new string[3];
    int numeroDoResultado = -1;
    // O recorde de cada coluna (na mesma ordem de Medalhas.Ordem: tempo, mortes, moedas).
    static readonly Progresso.Novidade[] RecordeDaColuna = { Progresso.Novidade.Tempo, Progresso.Novidade.Mortes, Progresso.Novidade.Moedas };

    // Os números da fase em 3 colunas (tempo, mortes, moedas), cada uma com a sua medalha em cima.
    // Medalha nova pulsa; recorde batido ganha um "NOVO!" amarelo embaixo.
    void DesenharResultado(float y, bool avisoDePrimeiraVez = true)
    {
        GerenciadorDoJogo.ResultadoDaFase r = jogo.Resultado;
        if (r.numero != numeroDoResultado)
        {
            numeroDoResultado = r.numero;
            textosDoResultado[0] = FormatarTempoFino(r.tempo);
            textosDoResultado[1] = r.mortes == 1 ? Idioma.T("1 morte", "1 death") : r.mortes + Idioma.T(" mortes", " deaths");
            textosDoResultado[2] = $"{r.moedas}/{r.totalDeMoedas}" + Idioma.T(" moedas", " coins");
            metasDoResultado[0] = Idioma.T("meta ", "goal ") + FormatarTempo(Medalhas.TempoAlvo(jogo.FaseAtual));
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
        if (primeira && avisoDePrimeiraVez) Texto("recordes salvos para esta fase", w / 2f, y + icone + 96f * escala, 1.8f, 0.5f, Lilas, w * 0.9f);
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
        textosDoRecorde[0] = $"{FormatarTempoFino(r.tempo)} ({Idioma.T("meta", "goal")} {FormatarTempo(Medalhas.TempoAlvo(fase))})";
        textosDoRecorde[1] = r.mortes == 1 ? Idioma.T("1 morte", "1 death") : r.mortes + Idioma.T(" mortes", " deaths");
        textosDoRecorde[2] = $"{r.moedas}/{Medalhas.TotalDeMoedas(fase)}" + Idioma.T(" moedas", " coins");
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

        GerenciadorDoJogo.Desfecho desfecho = jogo.FinalDoJogo;
        bool rei = desfecho == GerenciadorDoJogo.Desfecho.Rei;

        // a Marina (a sereia), grandona, do lado esquerdo (no final do rei, ela não ficou: só o Louro, cobrando)
        // (o Louro é o grande, o das telas grandes, virado para a direita: olhando para o texto)
        Texture sereia = FabricaDeSprites.Pegar(rei ? "louro_grande" : "sereia").texture;
        float alturaDaSereia = h * (rei ? 0.36f : 0.55f), larguraDaSereia = alturaDaSereia * sereia.width / sereia.height;
        float pulinho = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 10f * escala;
        var lugarDaSereia = new Rect(w * 0.17f - larguraDaSereia / 2f, h * 0.3f - pulinho, larguraDaSereia, alturaDaSereia);
        if (rei) GUI.DrawTextureWithTexCoords(lugarDaSereia, sereia, new Rect(1f, 0f, -1f, 1f));
        else GUI.DrawTexture(lugarDaSereia, sereia);

        float centro = w * 0.6f, largura = w * 0.7f;
        Texto(rei ? "O OURO É SEU!" : desfecho == GerenciadorDoJogo.Desfecho.Arrependido ? "FINAL VERDADEIRO!" : "VOCÊ ZEROU!", centro, h * 0.1f, 8f, 0.5f, Amarelo, largura);
        Texto(rei ? "Louro: \"Eu falei pra roubar. Agora me paga.\""
            : desfecho == GerenciadorDoJogo.Desfecho.Arrependido ? "Marina: \"Demorou, mas você voltou.\""
            : "Marina: \"Obrigada por voltar, Grumete!\"", centro, h * 0.27f, 2.5f, 0.5f, Lilas, largura);
        Texto(Idioma.T("Mortes: ", "Deaths: ") + jogo.Mortes, centro, h * 0.38f, 3.5f, 0.5f, Branco);
        Texto(Idioma.T("Moedas: ", "Coins: ") + jogo.Moedas, centro, h * 0.38f + 50f * escala, 3.5f, 0.5f, Amarelo);
        Texto(Idioma.T("Tempo: ", "Time: ") + FormatarTempo(jogo.TempoTotal), centro, h * 0.38f + 100f * escala, 3.5f, 0.5f, Branco);
        Texto(Idioma.T("Título: ", "Title: ") + Idioma.Traduzir(GerenciadorDoJogo.Titulo(jogo.Mortes)), centro, h * 0.38f + 165f * escala, 3.5f, 0.5f, Verde, largura);
        if (Time.unscaledTime % 1f < 0.65f)
            Texto("Aperte ENTER para ver os créditos", centro, h * 0.85f, 3f, 0.5f, Branco, largura);
    }

    // ------------------------------------------------------------------ a história (cutscenes)

    // Um quadro da história: a imagem cobrindo a tela (chegando bem devagar mais perto), aparecendo do escuro,
    // e o texto numa caixa de madeira embaixo, letra por letra. Os pontinhos mostram em que quadro está.
    void DesenharHistoria()
    {
        Historia.Quadro[] quadros = jogo.HistoriaAtual;
        if (quadros == null) return;
        float w = Screen.width, h = Screen.height, t = jogo.TempoNoQuadro;
        int atual = Mathf.Clamp(jogo.QuadroDaHistoria, 0, quadros.Length - 1);
        Historia.Quadro quadro = quadros[atual];
        Retangulo(new Rect(0, 0, w, h), Color.black);

        // a imagem: cobre a tela inteira (16:9) e "chega mais perto" 4% em 10 segundos
        Texture2D imagem = Historia.Imagem(quadro.imagem);
        if (imagem != null)
        {
            float zoom = 1f + 0.04f * Mathf.Clamp01(t / 10f);
            float largura = Mathf.Max(w, h * 16f / 9f) * zoom, altura = largura * 9f / 16f;
            GUI.DrawTexture(new Rect((w - largura) / 2f, (h - altura) / 2f, largura, altura), imagem);
        }
        float escuro = 1f - Mathf.Clamp01(t / 0.45f); // aparece do escuro
        if (escuro > 0f) Retangulo(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, escuro));

        // o texto, letra por letra
        string texto = Idioma.Traduzir(quadro.texto);
        int visiveis = Historia.LetrasVisiveis(t);
        float margem = 40f * escala, alturaDaCaixa = 118f * escala;
        var caixa = new Rect(margem, quadro.textoEmCima ? 22f * escala : h - alturaDaCaixa - 22f * escala, w - 2f * margem, alturaDaCaixa);
        Moldura(caixa, CorDoFundoDaFala);
        const float pixel = 2.4f;
        float m = 20f * escala;
        string[] linhas = QuebrarEmLinhas(texto, Mathf.FloorToInt((caixa.width - 2f * m) / (pixel * escala)));
        float y = caixa.y + m - 4f * escala;
        int resta = visiveis;
        foreach (string linha in linhas)
        {
            if (resta <= 0) break;
            Texto(resta >= linha.Length ? linha : linha.Substring(0, resta), caixa.x + m, y, pixel, 0f, Branco);
            resta -= linha.Length + 1; // (+1: o espaço entre as linhas)
            y += AlturaDaLinha * pixel * escala;
        }
        bool terminou = visiveis >= texto.Length;
        if (jogo.MostrandoOpcoes) DesenharOpcoesDaHistoria(caixa);
        if (terminou && Time.unscaledTime % 1f < 0.65f)
            Texto(jogo.MostrandoOpcoes ? "SETAS ou MOUSE: escolher    ENTER: confirmar" : atual < quadros.Length - 1 ? "ENTER >" : "ENTER: continuar",
                caixa.xMax - m, caixa.yMax - m - 10f * escala, 1.8f, 1f, CorDoNome);

        // em que quadro está: um pontinho por quadro
        float lado = 8f * escala, espaco = 6f * escala;
        float x0 = w / 2f - (quadros.Length * (lado + espaco) - espaco) / 2f;
        for (int i = 0; i < quadros.Length; i++)
            Retangulo(new Rect(x0 + i * (lado + espaco), caixa.yMax + 8f * escala, lado, lado),
                i == atual ? (Color)CorDoNome : i < atual ? new Color(1f, 1f, 1f, 0.6f) : new Color(1f, 1f, 1f, 0.2f));

        // pular (ESC) no canto de cima, e o clique em qualquer lugar passa o quadro
        // (com o texto em cima, o "pular" desce para o canto de baixo)
        if (!jogo.MostrandoOpcoes) BotaoDeTexto("ESC: pular", w - 20f * escala, quadro.textoEmCima ? h - 40f * escala : 16f * escala, 2f, 1f, GerenciadorDoJogo.BotaoVoltar);
        Mouse(new Rect(0, 0, w, h), GerenciadorDoJogo.BotaoProxima, false);
    }

    // A escolha do final: as opções lado a lado, logo acima da caixa do texto.
    void DesenharOpcoesDaHistoria(Rect caixa)
    {
        string[] opcoes = jogo.OpcoesDaHistoria;
        float alto = 62f * escala, espaco = 24f * escala;
        float largura = Mathf.Min(380f * escala, (caixa.width - espaco) / opcoes.Length);
        float x = caixa.center.x - (opcoes.Length * largura + (opcoes.Length - 1) * espaco) / 2f;
        float y = caixa.y - alto - 12f * escala;
        for (int i = 0; i < opcoes.Length; i++)
        {
            var botao = new Rect(x + i * (largura + espaco), y, largura, alto);
            Mouse(botao, i, true);
            Moldura(botao, CorDoFundoDaFala);
            bool escolhida = i == jogo.OpcaoDaHistoria;
            if (escolhida) DesenharBorda(botao, Color.Lerp(CorDoFilete, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)));
            Texto(opcoes[i], botao.center.x, botao.y + 12f * escala, 2.8f, 0.5f, escolhida ? Amarelo : Branco, largura - 20f * escala);
        }
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
        texto = Idioma.Traduzir(texto); // (antes de quebrar em linhas)
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
        // tem retrato pintado (Assets/Resources/Arte/retrato_<nome>)? Ele preenche o quadro, sem o pulinho
        Sprite pintado = FabricaDeSprites.Imagem("retrato_" + retrato);
        if (pintado != null)
        {
            float borda = 4f * escala;
            GUI.DrawTexture(new Rect(quadro.x + borda, quadro.y + borda, lado - 2f * borda, lado - 2f * borda), pintado.texture);
        }
        Texture desenho = pintado != null ? null : FabricaDeSprites.Pegar(retrato).texture;
        if (desenho != null)
        {
            float escalaDoDesenho = Mathf.Floor((lado - 12f * escala) / Mathf.Max(desenho.width, desenho.height));
            if (escalaDoDesenho < 1f) escalaDoDesenho = (lado - 12f * escala) / Mathf.Max(desenho.width, desenho.height);
            float dw = desenho.width * escalaDoDesenho, dh = desenho.height * escalaDoDesenho;
            float pulo = Mathf.Round(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 2f) * escalaDoDesenho * 0.5f;
            GUI.DrawTexture(new Rect(quadro.center.x - dw / 2f, quadro.center.y - dh / 2f - pulo, dw, dh), desenho);
        }

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
        string texto = string.Format(Idioma.Traduzir(fala.texto), jogo.MortesNaFase); // (traduz antes de pôr o número)
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
        Texto(jogo.ModoDificil ? "Você veio me buscar... com o baú?" : "Grumete! Você voltou!", tela.x, y - 50f * escala, 3f, 0.5f, Lilas, Screen.width * 0.6f);
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
        if (p > 0f && jogo.TituloNaTransicao) DesenharTituloDaFase(jogo.FaseDaTransicao, p);
    }

    // O nome da fase no meio da tela vermelha (aparece quando os tentáculos já cobriram quase tudo).
    void DesenharTituloDaFase(int fase, float p)
    {
        float alfa = Mathf.SmoothStep(0f, 1f, (p - 0.45f) / 0.45f);
        if (alfa <= 0f) return;
        float w = Screen.width, h = Screen.height;
        bool secreta = fase == Fases.IndiceSecreto;
        string numero = secreta ? Idioma.T("FASE SECRETA", "SECRET LEVEL") : $"{Idioma.T("FASE", "LEVEL")} {fase + 1}/{Fases.Todas.Length}";
        string nome = Idioma.Traduzir(Fases.Dados(fase).nome);
        Color antes = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alfa);
        Texto(numero, w / 2f, h * 0.4f, 2.8f, 0.5f, CorDoRodape, w * 0.8f);
        Texto(nome, w / 2f, h * 0.4f + 46f * escala, 5f, 0.5f, CorDoLogo, w * 0.85f);
        GUI.color = antes;
    }

    // p: 0 = nada, 1 = sombras no máximo. escuridaoMaxima: o quanto a tela fica escura no fim.
    void DesenharSombras(float p, float escuridaoMaxima)
    {
        float w = Screen.width, h = Screen.height;
        Color corOriginal = GUI.color;
        GUI.color = new Color(0.12f, 0f, 0.03f, Mathf.SmoothStep(0f, escuridaoMaxima, p)); // o fundo do mar do Kraken (vermelho-escuro)
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
        string nomeDoChefe = Idioma.Traduzir(chefe.Nome);
        if (textoDoChefe.texto == null || textoDoChefe.valor != etapa || textoDoChefe.idioma != Idioma.Atual
            || !textoDoChefe.texto.StartsWith(nomeDoChefe, System.StringComparison.Ordinal))
        {
            textoDoChefe.valor = etapa;
            textoDoChefe.idioma = Idioma.Atual;
            textoDoChefe.texto = $"{nomeDoChefe} - {Idioma.T("ETAPA", "STAGE")} {chefe.EtapaAtual + 1}/{chefe.Etapas}";
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
    // O texto é traduzido aqui (veja Idioma.Traduzir e Traducoes.cs): todo texto fixo da tela passa por aqui.
    Rect Texto(string texto, float x, float y, float pixel, float alinhamento, Color32 cor, float larguraMaxima = 0f)
    {
        if (string.IsNullOrEmpty(texto)) return new Rect(x, y, 0f, 0f);
        texto = Idioma.Traduzir(texto);
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
        texto = Idioma.Traduzir(texto);
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
        texto = Idioma.Traduzir(texto); // (antes de pôr o "> <" em volta)
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

    // Os décimos de segundo: "3,2s" em português, "3.2s" em inglês.
    static string Virgula => Idioma.T(",", ".");

    // A posição no ranking: "1º", "2º"... (em inglês: "1st", "2nd", "3rd", "4th"...).
    static string Posicao(int n)
    {
        if (!Idioma.Ingles) return n + "º";
        int dezena = n % 100;
        string fim = dezena >= 11 && dezena <= 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return n + fim;
    }

    static string FormatarTempo(float segundos) => FormatarTempo(Mathf.FloorToInt(segundos));
    static string FormatarTempo(int total) => $"{total / 60:00}:{total % 60:00}";

    // Com o décimo de segundo (para os recordes das fases): "01:23,4".
    static string FormatarTempoFino(float segundos)
    {
        int decimos = Mathf.FloorToInt(segundos * 10f);
        return $"{decimos / 600:00}:{decimos / 10 % 60:00}{Virgula}{decimos % 10}";
    }
}
