using UnityEngine;

// Tudo o que é desenhado "por cima" do jogo: HUD, título com menu, pausa, tela de morte,
// fase concluída, vitória, falas do Puck e da Emilia e a transição entre fases.
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
    GUIStyle estiloFala;
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
        DesenharRetornoPelaMorte();
        if (estado == EstadoDoJogo.Jogando || estado == EstadoDoJogo.Morreu || estado == EstadoDoJogo.FaseConcluida)
            DesenharBarraDeProgresso();
        if (estado != EstadoDoJogo.Titulo && estado != EstadoDoJogo.Vitoria)
            DesenharHud();

        if (estado == EstadoDoJogo.Jogando && jogo.Aviso != "")
            Texto(jogo.Aviso, w / 2f, h * 0.22f, 3.5f, 0.5f, Rosa, w * 0.9f);

        switch (estado)
        {
            case EstadoDoJogo.Titulo: DesenharTitulo(); break;
            case EstadoDoJogo.Pausado: DesenharPausa(); break;
            case EstadoDoJogo.Morreu: DesenharMorte(); break;
            case EstadoDoJogo.FaseConcluida: DesenharFaseConcluida(); break;
            case EstadoDoJogo.Vitoria: DesenharVitoria(); break;
        }

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
    }

    // ------------------------------------------------------------------ telas

    void DesenharHud()
    {
        float w = Screen.width;
        Fase fase = Fases.Todas[jogo.FaseAtual];
        const float pixel = 2.5f;
        float margem = 20f * escala, linha2 = 46f * escala;
        Texto($"Fase {jogo.FaseAtual + 1}/{Fases.Todas.Length}: {fase.nome}", margem, 10f * escala, pixel, 0f, Branco, w * 0.3f);
        Texto($"Mortes: {jogo.Mortes}", margem, linha2, pixel, 0f, Branco);
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
        Texto("R: reiniciar fase    ESC: pausa    ENTER: escolher", w / 2f, h * 0.8f + 32f * escala, 2f, 0.5f, Branco, w * 0.95f);
        int recorde = Progresso.Recorde;
        if (recorde >= 0)
            Texto($"Recorde: zerou com {recorde} mortes", w / 2f, h * 0.91f, 2f, 0.5f, Verde);
    }

    string TextoDaOpcao(GerenciadorDoJogo.OpcaoDoMenu opcao)
    {
        switch (opcao)
        {
            case GerenciadorDoJogo.OpcaoDoMenu.Continuar:
                return $"Continuar (fase {Progresso.FaseSalva + 1})";
            case GerenciadorDoJogo.OpcaoDoMenu.EscolherFase:
                return $"Escolher fase: < {jogo.FaseEscolhida + 1} >";
            default:
                return "Novo jogo";
        }
    }

    void DesenharPausa()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        Texto("PAUSA", w / 2f, h * 0.3f, 9f, 0.5f, Lilas);
        Texto("ESC: continuar", w / 2f, h * 0.52f, 3.5f, 0.5f, Branco);
        Texto("Q: voltar ao título", w / 2f, h * 0.52f + 52f * escala, 3.5f, 0.5f, Branco);
        Texto("(o progresso fica salvo: é só escolher \"Continuar\")", w / 2f, h * 0.75f, 2f, 0.5f, Branco, w * 0.9f);
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
            Texto("Aperte ENTER para jogar de novo", centro, h * 0.85f, 3f, 0.5f, Branco, largura);
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
            var caixa = new Rect(tela.x - largura / 2f, Screen.height - tela.y - altura, largura, altura);
            GUI.DrawTexture(caixa, fundoEscuro);
            GUI.Label(new Rect(caixa.x, caixa.y + 8f * escala, caixa.width, caixa.height - 8f * escala), placa.texto, estiloFala);
            Texto("Puck:", caixa.x + 10f * escala, caixa.y - 14f * escala, 2f, 0f, Lilas);
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
