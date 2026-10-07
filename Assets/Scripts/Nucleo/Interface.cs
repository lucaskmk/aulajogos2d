using UnityEngine;

// Tudo o que é desenhado "por cima" do jogo: HUD, título com menu, conquistas, mapa, pausa (com volume),
// tela de morte, fase concluída, vitória, créditos, falas dos personagens e a transição entre fases.
// Usa o OnGUI (IMGUI) do Unity. Os textos usam a fonte pixel de FabricaDeSprites.TextoEmPixel.
// Ela só LÊ o estado do GerenciadorDoJogo; quem decide as coisas é ele.
public class Interface : MonoBehaviour
{
    static readonly Color32 CorMarinho = new Color32(44, 52, 130, 255);
    static readonly Color32 Preto = new Color32(20, 20, 28, 255);
    static readonly Color32 Branco = new Color32(255, 255, 255, 255);
    static readonly Color32 Amarelo = new Color32(255, 217, 51, 255);
    static readonly Color32 Verde = new Color32(128, 255, 128, 255);
    static readonly Color32 Vermelho = new Color32(255, 102, 102, 255);
    static readonly Color32 Rosa = new Color32(255, 153, 255, 255);
    static readonly Color32 Lilas = new Color32(215, 170, 245, 255);

    GerenciadorDoJogo jogo;
    GUIStyle estiloFala, estiloFalaEsquerda;
    Texture2D fundoEscuro;
    float escala;

    void Awake() => jogo = GetComponent<GerenciadorDoJogo>();

    void OnGUI()
    {
        if (jogo == null) return;
        escala = Screen.height / 720f;
        PrepararEstilos();
        float w = Screen.width, h = Screen.height;
        EstadoDoJogo estado = jogo.Estado;

        DesenharFalaDoPuck();
        DesenharDicaDasPortas();
        DesenharRetornoPelaMorte();
        bool lutando = jogo.Chefe != null && jogo.Chefe.EmCombate;
        if ((estado == EstadoDoJogo.Jogando || estado == EstadoDoJogo.Morreu || estado == EstadoDoJogo.FaseConcluida) && !lutando)
            DesenharBarraDeProgresso();
        if (lutando && estado != EstadoDoJogo.Titulo) DesenharVidaDoChefe(jogo.Chefe);
        if (estado != EstadoDoJogo.Titulo && estado != EstadoDoJogo.Vitoria
            && estado != EstadoDoJogo.Conquistas && estado != EstadoDoJogo.Creditos)
            DesenharHud();

        if (estado == EstadoDoJogo.Jogando && jogo.Aviso != "")
            Texto(jogo.Aviso, w / 2f, h * 0.22f, 3.5f, 0.5f, Rosa, w * 0.9f);

        switch (estado)
        {
            case EstadoDoJogo.Titulo: DesenharTitulo(); break;
            case EstadoDoJogo.Conquistas: DesenharConquistas(); break;
            case EstadoDoJogo.Creditos: DesenharCreditos(); break;
            case EstadoDoJogo.Mapa: DesenharMapa(); break;
            case EstadoDoJogo.Pausado: DesenharPausa(); break;
            case EstadoDoJogo.Morreu: DesenharMorte(); break;
            case EstadoDoJogo.FaseConcluida: DesenharFaseConcluida(); break;
            case EstadoDoJogo.Vitoria: DesenharVitoria(); break;
        }

        DesenharAvisoDeConquista();
        DesenharTransicao(); // por cima de tudo
    }

    void PrepararEstilos()
    {
        if (fundoEscuro == null)
        {
            fundoEscuro = new Texture2D(1, 1);
            fundoEscuro.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
            fundoEscuro.Apply();
        }
        estiloFala = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(22 * escala),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
        };
        estiloFala.normal.textColor = Color.white;
        estiloFalaEsquerda = new GUIStyle(estiloFala) { alignment = TextAnchor.UpperLeft };
    }

    // ------------------------------------------------------------------ telas

    void DesenharHud()
    {
        float w = Screen.width;
        Fase fase = jogo.DadosDaFase;
        const float pixel = 2.5f;
        float margem = 20f * escala, linha2 = 46f * escala;
        if (jogo.Estado == EstadoDoJogo.Mapa)
        {
            Texto($"Mortes: {jogo.Mortes}", margem, 10f * escala, pixel, 0f, Branco);
        }
        else
        {
            string nome = jogo.NaSecreta ? fase.nome : $"Fase {jogo.FaseAtual + 1}/{Fases.Todas.Length}: {fase.nome}";
            Texto(nome, margem, 10f * escala, pixel, 0f, Branco, w * 0.3f);
            Texto($"Mortes: {jogo.Mortes}", margem, linha2, pixel, 0f, Branco);
        }
        Texto($"Moedas: {jogo.Moedas}", w - margem, 10f * escala, pixel, 1f, Amarelo);
        Texto(FormatarTempo(jogo.TempoTotal), w - margem, linha2, pixel, 1f, Branco);
    }

    void DesenharTitulo()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        // logo no estilo "Re:ZERO": letras azul-marinho com borda branca
        DesenharTextoEmPixel(FabricaDeSprites.TextoEmPixel("Re:CILADA!", CorMarinho, Color.white, CorMarinho), w / 2f, h * 0.12f, 130f * escala);
        Texto("começando a vida do zero (de novo, e de novo...)", w / 2f, h * 0.12f + 140f * escala, 2.5f, 0.5f, Branco, w * 0.9f);

        float y = h * 0.47f;
        for (int i = 0; i < jogo.opcoesDoMenu.Count; i++)
        {
            bool selecionada = i == jogo.OpcaoSelecionada;
            string texto = TextoDaOpcao(jogo.opcoesDoMenu[i]);
            if (selecionada && Time.unscaledTime % 0.8f < 0.55f) texto = "> " + texto + " <";
            Texto(texto, w / 2f, y, 3.5f, 0.5f, selecionada ? Amarelo : Branco, w * 0.9f);
            y += 52f * escala;
        }

        Texto("A/D ou SETAS: andar    ESPAÇO/W: pular (segure para ir mais alto)", w / 2f, h * 0.8f, 2f, 0.5f, Branco, w * 0.95f);
        Texto("R: reiniciar fase    ESC: pausa    ENTER: confirmar", w / 2f, h * 0.8f + 32f * escala, 2f, 0.5f, Branco, w * 0.95f);
        int recorde = Progresso.Recorde;
        if (recorde >= 0)
            Texto($"Recorde: zerou com {recorde} mortes", w / 2f, h * 0.91f, 2f, 0.5f, Verde);
    }

    static string TextoDaOpcao(GerenciadorDoJogo.OpcaoDoMenu opcao)
    {
        switch (opcao)
        {
            case GerenciadorDoJogo.OpcaoDoMenu.Continuar:
                return Progresso.FaseSalva == Fases.IndiceSecreto ? "Continuar (fase secreta)" : $"Continuar (fase {Progresso.FaseSalva + 1})";
            case GerenciadorDoJogo.OpcaoDoMenu.Conquistas:
                return $"Conquistas ({Conquistas.Quantidade}/{Conquistas.Todas.Length})";
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
        Texto("ENTER ou ESC: voltar", w / 2f, h - 50f * escala, 2.2f, 0.5f, Branco);
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
            if (titulo) DesenharTextoEmPixel(FabricaDeSprites.TextoEmPixel("Re:CILADA!", CorMarinho, Color.white, CorMarinho), w / 2f, yLinha, 70f * escala);
            else Texto(texto, w / 2f, yLinha, secao ? 3f : 2.5f, 0.5f, secao ? Amarelo : Branco, w * 0.9f);
        }

        // Subaru, Emilia, Puck e Beatrice pulando na parte de baixo
        string[] turma = { "jogador", "emilia", "puck", "beatrice" };
        for (int i = 0; i < turma.Length; i++)
        {
            Texture desenho = FabricaDeSprites.Pegar(turma[i]).texture;
            float altura = desenho.height * 4f * escala, largura = desenho.width * 4f * escala;
            float pulo = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f + i)) * 20f * escala;
            GUI.DrawTexture(new Rect(w / 2f + (i - 1.5f) * 120f * escala - largura / 2f, h - altura - 20f * escala - pulo, largura, altura), desenho);
        }
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
        Texto(secreta ? Fases.Secreta.nome : $"Fase {fase + 1}: {Fases.Todas[fase].nome}", w / 2f, h - 125f * escala, 3.5f, 0.5f, Amarelo, w * 0.9f);
        string situacao = secreta ? "a fase secreta!" : fase < jogo.FaseMaisLonge ? "já passou (dá para jogar de novo)" : "ainda não passou nesta partida";
        Texto(situacao, w / 2f, h - 78f * escala, 2f, 0.5f, secreta ? Rosa : fase < jogo.FaseMaisLonge ? Verde : Lilas, w * 0.9f);
        string comandos = mapa.Andando ? "..." : "SETAS: andar     ENTER: jogar     ESC: título";
        Texto(comandos, w / 2f, h - 42f * escala, 2f, 0.5f, Branco, w * 0.9f);
    }

    // Fala de um personagem depois de passar de fase: retrato grande à esquerda e o texto ao lado.
    void DesenharFalaNoMapa(Textos.Fala fala)
    {
        float w = Screen.width, h = Screen.height;
        Texture retrato = FabricaDeSprites.Pegar(fala.sprite).texture;
        float altura = 110f * escala, largura = altura * retrato.width / retrato.height;
        float pulo = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 6f * escala;
        GUI.DrawTexture(new Rect(30f * escala, h - altura - 14f * escala - pulo, largura, altura), retrato);

        float x = 50f * escala + largura;
        Texto(fala.quem + ":", x, h - 125f * escala, 2.5f, 0f, Lilas);
        string texto = string.Format(fala.texto, jogo.MortesNaFase);
        GUI.Label(new Rect(x, h - 95f * escala, w - x - 30f * escala, 70f * escala), texto, estiloFalaEsquerda);
        if (Time.unscaledTime % 1f < 0.65f) Texto("ENTER", w - 30f * escala, h - 30f * escala, 1.8f, 1f, Branco);
    }

    void DesenharPausa()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        Texto("PAUSA", w / 2f, h * 0.18f, 9f, 0.5f, Lilas);

        // volume da música e dos efeitos (cima/baixo escolhe, esquerda/direita muda)
        string[] nomes = { "Música", "Efeitos" };
        float[] valores = { Opcoes.Musica, Opcoes.Efeitos };
        for (int i = 0; i < 2; i++)
        {
            bool escolhida = jogo.OpcaoDaPausa == i;
            string barra = new string('*', Mathf.RoundToInt(valores[i] * 10f)).PadRight(10, '-');
            string texto = $"{nomes[i]}: < {barra} >";
            if (escolhida && Time.unscaledTime % 0.8f < 0.55f) texto = "> " + texto;
            Texto(texto, w / 2f, h * 0.42f + i * 46f * escala, 3f, 0.5f, escolhida ? Amarelo : Branco);
        }

        Texto("ESC: continuar", w / 2f, h * 0.65f, 3f, 0.5f, Branco);
        Texto("Q: voltar ao mapa", w / 2f, h * 0.65f + 46f * escala, 3f, 0.5f, Branco);
        Texto("(o progresso fica salvo)", w / 2f, h * 0.85f, 2f, 0.5f, Branco, w * 0.9f);
    }

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
        if (jogo.EhUltimaFase && jogo.Emilia != null)
        {
            DesenharFalaDaEmilia();
            return;
        }
        Texto("FASE CONCLUÍDA!", w / 2f, h * 0.3f, 7f, 0.5f, Verde, w * 0.9f);
        string comentario = jogo.MortesNaFase == 0 ? "Sem morrer?! Impossível." : $"Só {jogo.MortesNaFase} morte(s) nessa fase.";
        Texto(comentario, w / 2f, h * 0.3f + 110f * escala, 3f, 0.5f, Branco, w * 0.9f);
    }

    void DesenharVitoria()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);

        // a Emilia, grandona, do lado esquerdo
        Texture emilia = FabricaDeSprites.Pegar("emilia").texture;
        float alturaEmilia = h * 0.55f, larguraEmilia = alturaEmilia * emilia.width / emilia.height;
        float pulinho = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 10f * escala;
        GUI.DrawTexture(new Rect(w * 0.17f - larguraEmilia / 2f, h * 0.3f - pulinho, larguraEmilia, alturaEmilia), emilia);

        float centro = w * 0.6f, largura = w * 0.7f;
        Texto("VOCÊ ZEROU!", centro, h * 0.1f, 8f, 0.5f, Amarelo, largura);
        Texto("Emilia: \"Obrigada por voltar, Subaru!\"", centro, h * 0.27f, 2.5f, 0.5f, Lilas, largura);
        Texto($"Mortes: {jogo.Mortes}", centro, h * 0.38f, 3.5f, 0.5f, Branco);
        Texto($"Moedas: {jogo.Moedas}", centro, h * 0.38f + 50f * escala, 3.5f, 0.5f, Amarelo);
        Texto($"Tempo: {FormatarTempo(jogo.TempoTotal)}", centro, h * 0.38f + 100f * escala, 3.5f, 0.5f, Branco);
        Texto($"Título: {GerenciadorDoJogo.Titulo(jogo.Mortes)}", centro, h * 0.38f + 165f * escala, 3.5f, 0.5f, Verde, largura);
        if (Time.unscaledTime % 1f < 0.65f)
            Texto("Aperte ENTER para ver os créditos", centro, h * 0.85f, 3f, 0.5f, Branco, largura);
    }

    // ------------------------------------------------------------------ falas

    void DesenharFalaDoPuck()
    {
        Camera cam = Camera.main;
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (cam == null || jogador == null || jogo.Estado == EstadoDoJogo.Titulo) return;

        foreach (Placa placa in jogo.placas)
        {
            if (placa == null || Vector2.Distance(placa.transform.position, jogador.transform.position) > 3.5f) continue;

            Vector3 tela = cam.WorldToScreenPoint(placa.transform.position + Vector3.up * 1.2f);
            float largura = 420f * escala, altura = 80f * escala;
            float x = Mathf.Clamp(tela.x - largura / 2f, 10f * escala, Screen.width - largura - 10f * escala); // não sai da tela
            var caixa = new Rect(x, Screen.height - tela.y - altura, largura, altura);
            GUI.DrawTexture(caixa, fundoEscuro);
            GUI.Label(new Rect(caixa.x, caixa.y + 8f * escala, caixa.width, caixa.height - 8f * escala), placa.texto, estiloFala);
            Texto(placa.quem + ":", caixa.x + 10f * escala, caixa.y - 14f * escala, 2f, 0f, Lilas);
        }
    }

    // "S: entrar" piscando em cima da porta da Beatrice em que o jogador está parado.
    void DesenharDicaDasPortas()
    {
        Camera cam = Camera.main;
        if (cam == null || jogo.Estado != EstadoDoJogo.Jogando) return;
        foreach (Porta porta in jogo.portas)
        {
            if (porta == null || !Porta.PodeEntrar || !porta.JogadorNaFrente()) continue;
            Vector3 tela = cam.WorldToScreenPoint(porta.transform.position + Vector3.up * 2.3f);
            if (Time.unscaledTime % 0.8f < 0.55f)
                Texto("S: entrar", tela.x, Screen.height - tela.y - 30f * escala, 2.2f, 0.5f, Lilas);
        }
    }

    void DesenharFalaDaEmilia()
    {
        Camera cam = Camera.main;
        if (cam == null || jogo.Emilia == null) return;
        Vector3 tela = cam.WorldToScreenPoint(jogo.Emilia.transform.position + Vector3.up * 2.2f);
        float y = Screen.height - tela.y;
        Texto("Subaru! Você voltou!", tela.x, y - 50f * escala, 3f, 0.5f, Lilas, Screen.width * 0.6f);
    }

    // ------------------------------------------------------------------ efeitos de tela

    // Mãos de sombra: (posição ao longo da borda 0..1, borda: 0 = baixo, 1 = esquerda, 2 = direita, atraso 0..1)
    static readonly Vector3[] MaosDaSombra =
    {
        new Vector3(0.12f, 0, 0.00f), new Vector3(0.36f, 0, 0.15f), new Vector3(0.64f, 0, 0.05f), new Vector3(0.88f, 0, 0.20f),
        new Vector3(0.40f, 1, 0.10f), new Vector3(0.78f, 1, 0.25f),
        new Vector3(0.35f, 2, 0.20f), new Vector3(0.72f, 2, 0.00f),
    };

    // Estilo Re:Zero: ao morrer a tela escurece e mãos de sombra avançam das bordas;
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

        Texture mao = FabricaDeSprites.Pegar("mao_sombra").texture;
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

    // Barra de vida do chefe (a Baleia Branca, a Elsa...), no topo da tela (no lugar da barra de progresso).
    // Mostra a ETAPA da luta e a barra dividida: cada etapa é um bloco de 2 golpes, separado por uma
    // divisão mais grossa, com uma bolinha embaixo (cheia = etapa vencida, piscando = etapa atual).
    void DesenharVidaDoChefe(Chefe chefe)
    {
        float largura = Mathf.Min(Screen.width * 0.4f, 560f * escala), altura = 16f * escala;
        var barra = new Rect((Screen.width - largura) / 2f, 52f * escala, largura, altura);
        Texto($"{chefe.Nome} - ETAPA {chefe.EtapaAtual + 1}/{chefe.Etapas}", Screen.width / 2f, 12f * escala, 2.5f, 0.5f, Branco);

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

        // Subaru andando na barra
        Texture subaru = FabricaDeSprites.Pegar("jogador").texture;
        float alturaJogador = 32f * escala, larguraJogador = alturaJogador * subaru.width / subaru.height;
        GUI.DrawTexture(new Rect(barra.x + barra.width * progresso - larguraJogador / 2f, barra.center.y - alturaJogador / 2f, larguraJogador, alturaJogador),
            subaru);
    }

    // ------------------------------------------------------------------ texto em pixel

    // Escreve com a fonte pixel. pixel = tamanho de cada "pixel" da letra (em telas de 720p);
    // alinhamento: 0 = x é a esquerda do texto, 0.5 = o centro, 1 = a direita.
    // Se passar de larguraMaxima, o texto encolhe para caber.
    void Texto(string texto, float x, float y, float pixel, float alinhamento, Color32 cor, float larguraMaxima = 0f)
    {
        if (string.IsNullOrEmpty(texto)) return;
        Texture2D textura = FabricaDeSprites.TextoEmPixel(texto.ToUpperInvariant(), cor, Preto, Preto, comAcentos: true);
        float tamanho = pixel * escala;
        float largura = textura.width * tamanho;
        if (larguraMaxima > 0f && largura > larguraMaxima)
        {
            tamanho *= larguraMaxima / largura;
            largura = larguraMaxima;
        }
        GUI.DrawTexture(new Rect(x - largura * alinhamento, y, largura, textura.height * tamanho), textura);
    }

    // Desenha um texto de FabricaDeSprites.TextoEmPixel centralizado em (centroX, topo), com a altura pedida.
    static void DesenharTextoEmPixel(Texture2D texto, float centroX, float topo, float altura)
    {
        float largura = altura * texto.width / texto.height;
        GUI.DrawTexture(new Rect(centroX - largura / 2f, topo, largura, altura), texto);
    }

    static string FormatarTempo(float segundos)
    {
        int total = Mathf.FloorToInt(segundos);
        return $"{total / 60:00}:{total % 60:00}";
    }
}
