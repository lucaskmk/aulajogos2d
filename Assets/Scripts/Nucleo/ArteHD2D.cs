using UnityEngine;

// O "clima" de cada fase: decide a cor do pós-processamento, dos feixes de luz e da névoa.
public enum Clima { Dia, Noite, Escuro, Biblioteca }

// Desenhos do visual "HD-2D" (o estilo de Octopath Traveler: pixel art com luz e profundidade de "foto").
//
// Na fotografia, quando a câmera foca em algo perto, o que está longe fica DESFOCADO (borrado), e o que
// está muito perto da lente também. É isso que dá a sensação de "maquete" (o efeito tilt-shift).
// Normalmente isso é feito com um shader na placa de vídeo... mas aqui fazemos do jeito mais simples
// possível: borramos as IMAGENS uma única vez, na hora em que são criadas (blur "por CPU"), e guardamos.
// Nada é recalculado durante o jogo, então fica leve até no navegador (WebGL).
//
// O plano do jogo (blocos, Subaru, inimigos) continua NÍTIDO: só o fundo e a frente são borrados.
public static class ArteHD2D
{
    // Registra os desenhos novos na FabricaDeSprites: depois é só usar FabricaDeSprites.Pegar("nome").
    // (eles só são criados de verdade na primeira vez que alguém pede, e ficam guardados no cache)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        // Camadas do fundo, desfocadas: quanto mais longe, mais borrada (raio maior e mais passadas).
        FabricaDeSprites.Registrar("fundo_montanhas_hd", () => VersaoDesfocada("fundo_montanhas", 3, 3));
        FabricaDeSprites.Registrar("fundo_castelo_hd", () => VersaoDesfocada("fundo_castelo", 2, 3));
        FabricaDeSprites.Registrar("fundo_floresta_hd", () => VersaoDesfocada("fundo_floresta", 1, 3));
        FabricaDeSprites.Registrar("fundo_arvores_hd", () => VersaoDesfocada("fundo_arvores", 1, 2));

        // Luz: o feixe (god ray) e a auréola em volta do sol/lua.
        FabricaDeSprites.Registrar("hd2d_feixe", () => Suave(FabricaDeSprites.Procedural(32, 128, new Vector2(0.5f, 1f), CorFeixe)));
        FabricaDeSprites.Registrar("hd2d_aureola", () => Suave(FabricaDeSprites.Procedural(64, 64, FabricaDeSprites.Centro, CorAureola)));

        // Primeiro plano (bem perto da "lente"): desenhado com metade da resolução (8 pixels por bloco)
        // e bem borrado. Capim embaixo; galho com folhas pendurado em cima.
        FabricaDeSprites.Registrar("hd2d_frente_capim", () => Desfocado(LarguraDaFrente, AlturaDoCapim, CorCapim, FabricaDeSprites.Base, 1, 3, true, 8));
        FabricaDeSprites.Registrar("hd2d_frente_galho", () => Desfocado(LarguraDaFrente, AlturaDoGalho, CorGalho, new Vector2(0.5f, 1f), 2, 3, false, 8));
    }

    // ---------------------------------------------------------------- o blur (desfoque)

    // "Box blur": cada pixel vira a MÉDIA dos vizinhos num quadrado de lado (2*raio+1).
    // Feito primeiro na horizontal e depois na vertical (bem mais rápido que o quadrado inteiro).
    // Repetir 2 ou 3 vezes deixa o borrado redondinho, quase igual ao "gaussiano" dos programas de foto.
    //
    // Detalhes importantes:
    //  - na horizontal a imagem "dá a volta" (o fim encosta no começo), porque as camadas se repetem lado a lado
    //    e a emenda não pode aparecer;
    //  - o borrado "vaza" para fora do desenho, então a imagem ganha linhas transparentes de folga em cima
    //    (folgaEmCima) ou embaixo (folgaEmbaixo);
    //  - a cor é multiplicada pela transparência antes de borrar ("pré-multiplicada"): senão a borda pegaria
    //    a cor dos pixels invisíveis (que é preta) e ficaria com um contorno escuro.
    public static Color32[] Desfocar(Color32[] origem, int largura, int altura, int raio, int passadas, int folgaEmCima, int folgaEmbaixo)
    {
        int novaAltura = altura + folgaEmCima + folgaEmbaixo;
        int total = largura * novaAltura;
        var r = new float[total]; var g = new float[total]; var b = new float[total]; var a = new float[total];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                Color32 c = origem[y * largura + x];
                int i = (y + folgaEmbaixo) * largura + x;
                float alfa = c.a / 255f;
                a[i] = alfa;
                r[i] = c.r / 255f * alfa;
                g[i] = c.g / 255f * alfa;
                b[i] = c.b / 255f * alfa;
            }

        var tmp = new float[System.Math.Max(largura, novaAltura)];
        for (int p = 0; p < passadas; p++)
            foreach (float[] canal in new[] { r, g, b, a })
            {
                BorrarLinhas(canal, largura, novaAltura, raio, tmp);
                BorrarColunas(canal, largura, novaAltura, raio, tmp);
            }

        var resultado = new Color32[total];
        for (int i = 0; i < total; i++)
        {
            float alfa = a[i];
            if (alfa <= 0.002f) { resultado[i] = FabricaDeSprites.Transparente; continue; }
            resultado[i] = new Color32(Byte(r[i] / alfa), Byte(g[i] / alfa), Byte(b[i] / alfa), Byte(alfa));
        }
        return resultado;
    }

    static byte Byte(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);

    // Média "deslizante" em cada linha: soma a janela uma vez e depois só tira quem saiu e põe quem entrou.
    static void BorrarLinhas(float[] canal, int largura, int altura, int raio, float[] tmp)
    {
        float lado = 2 * raio + 1;
        for (int y = 0; y < altura; y++)
        {
            int linha = y * largura;
            float soma = 0f;
            for (int k = -raio; k <= raio; k++) soma += canal[linha + Volta(k, largura)];
            for (int x = 0; x < largura; x++)
            {
                tmp[x] = soma / lado;
                soma += canal[linha + Volta(x + raio + 1, largura)] - canal[linha + Volta(x - raio, largura)];
            }
            for (int x = 0; x < largura; x++) canal[linha + x] = tmp[x];
        }
    }

    // O mesmo nas colunas, mas sem dar a volta: fora da imagem vale o pixel da beirada.
    static void BorrarColunas(float[] canal, int largura, int altura, int raio, float[] tmp)
    {
        float lado = 2 * raio + 1;
        for (int x = 0; x < largura; x++)
        {
            float soma = 0f;
            for (int k = -raio; k <= raio; k++) soma += canal[Mathf.Clamp(k, 0, altura - 1) * largura + x];
            for (int y = 0; y < altura; y++)
            {
                tmp[y] = soma / lado;
                soma += canal[Mathf.Clamp(y + raio + 1, 0, altura - 1) * largura + x]
                      - canal[Mathf.Clamp(y - raio, 0, altura - 1) * largura + x];
            }
            for (int y = 0; y < altura; y++) canal[y * largura + x] = tmp[y];
        }
    }

    static int Volta(int x, int largura) => ((x % largura) + largura) % largura;

    // Liga a filtragem "bilinear" (a placa de vídeo mistura os pixels vizinhos): ótima para coisas borradas,
    // porque some o "serrilhado" dos pixels. A repetição na horizontal (Repeat) evita a emenda entre os pedaços.
    static Sprite Suave(Sprite sprite)
    {
        sprite.texture.filterMode = FilterMode.Bilinear;
        sprite.texture.wrapModeU = TextureWrapMode.Repeat;
        sprite.texture.wrapModeV = TextureWrapMode.Clamp;
        return sprite;
    }

    // Pega um desenho que já existe (ex.: "fundo_castelo") e devolve a versão borrada dele.
    static Sprite VersaoDesfocada(string nome, int raio, int passadas)
    {
        Sprite original = FabricaDeSprites.Pegar(nome);
        Texture2D textura = original.texture;
        int folga = raio * passadas; // o quanto o borrado pode "vazar" para cima
        Color32[] borrado = Desfocar(textura.GetPixels32(), textura.width, textura.height, raio, passadas, folga, 0);
        int altura = textura.height + folga;
        return Suave(FabricaDeSprites.Procedural(textura.width, altura, FabricaDeSprites.Base, (x, y) => borrado[y * textura.width + x]));
    }

    // Desenha com a fórmula "cor", borra e devolve. folgaEmCima = o borrado vaza para cima (senão para baixo).
    static Sprite Desfocado(int largura, int altura, System.Func<int, int, Color32> cor, Vector2 pivo, int raio, int passadas, bool folgaEmCima, int pixelsPorUnidade)
    {
        var pixels = new Color32[largura * altura];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                pixels[y * largura + x] = cor(x, y);
        int folga = raio * passadas;
        Color32[] borrado = Desfocar(pixels, largura, altura, raio, passadas, folgaEmCima ? folga : 0, folgaEmCima ? 0 : folga);
        return Suave(FabricaDeSprites.Procedural(largura, altura + folga, pivo, (x, y) => borrado[y * largura + x], pixelsPorUnidade));
    }

    // ---------------------------------------------------------------- luz

    // Feixe de luz (god ray): uma faixa branca, mais forte no meio e sumindo nas laterais (curva "gaussiana"),
    // com uns fios um pouco mais fortes por dentro. Nasce suave lá em cima e vai sumindo para baixo.
    // É branco: a cor (dourada de dia, azulada de noite) vem do SpriteRenderer.color.
    static Color32 CorFeixe(int x, int y)
    {
        float u = (x + 0.5f) / 32f;        // 0 = lado esquerdo, 1 = lado direito
        float v = 1f - (y + 0.5f) / 128f;  // 0 = em cima (perto do céu), 1 = embaixo
        float d = (u - 0.5f) / 0.22f;
        float lado = Mathf.Exp(-d * d);
        float fios = 0.72f + 0.28f * Mathf.Sin(u * 37f + 1.3f) * Mathf.Sin(u * 13f + 0.4f);
        float entrada = Mathf.Clamp01(v / 0.1f);
        float queda = Mathf.Pow(1f - v, 1.5f);
        return new Color32(255, 255, 255, Byte(lado * fios * entrada * queda * 0.95f));
    }

    // Auréola: um brilho redondo e bem suave (um "bloom de mentira" em volta do sol, sempre igual).
    static Color32 CorAureola(int x, int y)
    {
        float d = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 32f;
        float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.4f);
        return new Color32(255, 255, 255, Byte(a * 0.75f));
    }

    // ---------------------------------------------------------------- primeiro plano

    const int LarguraDaFrente = 128; // 16 blocos (a 8 pixels por bloco); repete sem emenda
    const int AlturaDoCapim = 28;
    const int AlturaDoGalho = 24;
    static readonly Color32 Cheio = new Color32(255, 255, 255, 255);

    // Distância na horizontal "dando a volta" na imagem (para repetir sem emenda).
    static float DistanciaX(float x, float centro)
    {
        float d = Mathf.Abs(x - centro) % LarguraDaFrente;
        return Mathf.Min(d, LarguraDaFrente - d);
    }

    // Folhas de capim: (posição x, altura da ponta, quanto a ponta tomba para o lado).
    // As mais altas são poucas: o capim não pode esconder o chão nem os espinhos!
    static readonly Vector3[] Capins =
    {
        new Vector3(4, 17, 2), new Vector3(9, 15, -1), new Vector3(15, 19, 3), new Vector3(23, 16, -2),
        new Vector3(31, 14, 1), new Vector3(38, 18, -3), new Vector3(44, 15, 2), new Vector3(57, 17, -1),
        new Vector3(63, 21, 4), new Vector3(70, 15, -2), new Vector3(79, 17, 2), new Vector3(88, 14, -1),
        new Vector3(95, 18, -3), new Vector3(103, 16, 2), new Vector3(110, 20, -2), new Vector3(118, 15, 1),
        new Vector3(124, 17, -2),
    };

    // Capim e moitas perto da lente (silhueta branca: a cor escura vem do SpriteRenderer.color).
    static Color32 CorCapim(int x, int y)
    {
        if (y < 10) return Cheio; // faixa cheia embaixo (fica escondida abaixo da tela: veja PrimeiroPlano)

        // moitas redondas e baixas
        float t = 2f * Mathf.PI * x / LarguraDaFrente;
        float moita = 11f + 2.5f * Mathf.Abs(Mathf.Sin(t * 3f)) + 1f * Mathf.Sin(t * 7f + 1f);
        if (y < moita) return Cheio;

        // folhas de capim: triângulos finos, tombando um pouco
        foreach (Vector3 c in Capins)
        {
            if (y >= c.y) continue;
            float subida = (y - 10f) / (c.y - 10f); // 0 na base, 1 na ponta
            float centro = c.x + c.z * subida * subida;
            if (DistanciaX(x, centro) <= 2.2f * (1f - subida) + 0.5f) return Cheio;
        }
        return FabricaDeSprites.Transparente;
    }

    // Galho com folhas, pendurado no alto da tela (só em parte da imagem: no resto o céu aparece).
    // y conta de baixo para cima, então "do topo" = AlturaDoGalho - 1 - y.
    // Os 8 pixels de cima (1 bloco) ficam escondidos acima da tela: o galho "entra" por cima nas pontas
    // e desce um pouco no meio, e só as folhas e os cipós aparecem de verdade.
    static Color32 CorGalho(int x, int y)
    {
        float doTopo = AlturaDoGalho - 1 - y;
        if (x >= 76) return FabricaDeSprites.Transparente;

        // o galho: curvado para baixo no meio e afinando para a direita
        float curva = 2f + 8f * Mathf.Sin(Mathf.PI * x / 76f);
        float grossura = 5f - x * 0.04f;
        if (Mathf.Abs(doTopo - curva) < grossura * 0.5f) return Cheio;

        // tufos de folhas pendurados embaixo do galho
        for (int i = 0; i < 5; i++)
        {
            float cx = 10f + i * 14f;
            float cy = 2f + 8f * Mathf.Sin(Mathf.PI * cx / 76f) + 3f + (i % 2) * 1.5f;
            float rx = 5.5f - i * 0.5f, ry = 3.5f;
            float dx = (x - cx) / rx, dy = (doTopo - cy) / ry;
            if (dx * dx + dy * dy <= 1f) return Cheio;
        }

        // cipós: fios finos descendo
        if ((x == 18 || x == 19) && doTopo < 20f) return Cheio;
        if (x == 47 && doTopo < 18f) return Cheio;
        return FabricaDeSprites.Transparente;
    }
}
