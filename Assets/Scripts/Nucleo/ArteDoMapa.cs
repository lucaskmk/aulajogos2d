using System.Collections.Generic;
using UnityEngine;

// ARTE DO MAPA DO MUNDO (visual "v3", o mesmo acabamento dos blocos das fases: veja ArteDosBlocos).
//
// O CHÃO do mapa inteiro (grama, terra seca, areia, água e o abismo) é UMA imagem só, pintada pixel a
// pixel uma vez e guardada (cache). Por que não um quadradinho por célula, como antes?
//  - As passagens de um terreno para o outro ficam ORGÂNICAS: a beira do lago é uma curva com praia e
//    espuma, e não a borda reta de um quadrado. Um "ruído" (manchas aleatórias suaves) entorta as bordas.
//  - A água sabe a DISTÂNCIA até a margem (um "mapa de distâncias"): fica mais escura no meio (fundo),
//    clarinha perto da margem, com espuma na beira e o barranco de terra fazendo sombra em cima.
//  - É bem mais leve: 1 desenho no lugar de ~2400.
// Os enfeites (árvores, casas, castelo...) continuam sendo sprites separados (ArteDoMapaEnfeites.cs),
// porque balançam no vento, ficam na frente ou atrás do Subaru e projetam sombra.
//
// As mesmas ideias de pixel art dos blocos: RAMPAS de cores com hue shifting (sombra azul-arroxeada,
// luz dourada), DITHERING (matriz de Bayer) nas passagens, luz de CIMA-ESQUERDA, oclusão e contorno colorido.
//
// O caminho de terra, as pontes e as "ondinhas" animadas da água também moram aqui, cada um num
// atlas (uma imagem com todos os recortes), para a placa de vídeo desenhar tudo de uma vez.
public static partial class ArteDoMapa
{
    const int P = FabricaDeSprites.PixelsPorUnidade; // 16 pixels por célula do mapa

    static Color32 C(int r, int g, int b) => ArteDosBlocos.C(r, g, b);
    static float Sorteio(int x, int y, int semente) => ArteDosBlocos.Sorteio(x, y, semente);
    static int Tom(float claridade, int tons, int x, int y, float mistura = 0.6f) => ArteDosBlocos.Tom(claridade, tons, x, y, mistura);

    // ---------------------------------------------------------------- rampas novas (escuro -> claro)

    static readonly Color32[] Capim = ArteDosBlocos.Capim, Terra = ArteDosBlocos.Terra, Pedra = ArteDosBlocos.Pedra;
    static readonly Color32[] Agua =
    {
        C(16, 28, 64), C(22, 46, 96), C(28, 72, 128), C(40, 104, 158), C(62, 140, 184), C(104, 182, 204), C(168, 222, 224),
    };
    static readonly Color32[] AguaSombria = // o mar escuro da região sombria
    {
        C(10, 10, 30), C(16, 18, 46), C(24, 28, 66), C(34, 42, 88), C(50, 62, 112), C(76, 90, 140), C(118, 130, 172),
    };
    static readonly Color32[] Areia =
    {
        C(92, 66, 62), C(138, 102, 80), C(182, 144, 100), C(212, 180, 124), C(234, 208, 150), C(248, 234, 188),
    };
    static readonly Color32[] CapimSombrio = // a "grama morta" da região sombria (o miasma da Bruxa)
    {
        C(16, 12, 28), C(28, 22, 44), C(42, 34, 60), C(58, 50, 76), C(76, 68, 92), C(98, 90, 108), C(124, 118, 124),
    };
    static readonly Color32[] Escuridao = // o fundo do abismo
    {
        C(8, 4, 14), C(14, 9, 24), C(22, 15, 36), C(32, 23, 50), C(46, 34, 66),
    };
    static readonly Color32 Espuma = C(236, 248, 244);
    static readonly Color32[] Estrada = // a terra batida do caminho (mais clara que a terra seca, para aparecer bem)
    {
        C(70, 44, 44), C(118, 82, 62), C(164, 124, 86), C(198, 162, 112), C(224, 194, 142), C(244, 222, 174),
    };

    // ---------------------------------------------------------------- ruído

    // Ruído suave ("value noise"): sorteia um número em cada ponto de uma grade e mistura os vizinhos.
    static float Ruido(float x, float y, int semente)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float tx = x - x0, ty = y - y0;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        float a = Mathf.Lerp(Sorteio(x0, y0, semente), Sorteio(x0 + 1, y0, semente), tx);
        float b = Mathf.Lerp(Sorteio(x0, y0 + 1, semente), Sorteio(x0 + 1, y0 + 1, semente), tx);
        return Mathf.Lerp(a, b, ty);
    }

    // Três "oitavas" de ruído (manchas grandes + médias + pequenas): parece natural. escala = tamanho das manchas grandes.
    static float Fractal(float x, float y, float escala, int semente)
    {
        return 0.55f * Ruido(x / escala, y / escala, semente)
             + 0.3f * Ruido(x / (escala * 0.45f), y / (escala * 0.45f), semente + 1)
             + 0.15f * Ruido(x / (escala * 0.2f), y / (escala * 0.2f), semente + 2);
    }

    // Um ruído calculado só a cada 4 pixels e depois "esticado" (misturando os 4 vizinhos): 16 vezes menos contas.
    // Serve para as manchas grandes (que não têm detalhe pequeno mesmo).
    sealed class RuidoGrosso
    {
        const int Passo = 4;
        readonly float[] valores;
        readonly int largura;

        public RuidoGrosso(int larguraEmPixels, int alturaEmPixels, System.Func<int, int, float> ruido)
        {
            largura = larguraEmPixels / Passo + 2;
            int altura = alturaEmPixels / Passo + 2;
            valores = new float[largura * altura];
            for (int y = 0; y < altura; y++)
                for (int x = 0; x < largura; x++)
                    valores[y * largura + x] = ruido(x * Passo, y * Passo);
        }

        public float Em(int px, int py)
        {
            int x0 = px / Passo, y0 = py / Passo;
            float tx = (px % Passo) / (float)Passo, ty = (py % Passo) / (float)Passo;
            int i = y0 * largura + x0;
            float a = valores[i] + (valores[i + 1] - valores[i]) * tx;
            float b = valores[i + largura] + (valores[i + largura + 1] - valores[i + largura]) * tx;
            return a + (b - a) * ty;
        }
    }

    // De -0,47 a 0,47 pelo padrão de Bayer (para "decidir com dithering" entre dois materiais).
    static float Limiar(int x, int y) => (ArteDosBlocos.Bayer[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f - 0.5f;

    // ---------------------------------------------------------------- o terreno inteiro

    // O que o mapa precisa saber das regiões (as colunas vêm do MapaDoMundo).
    public struct Regioes
    {
        public int inicioDoDesfiladeiro, fimDoDesfiladeiro; // terra seca em volta do abismo
        public int inicioDaSombra;                          // daqui para a direita: a região sombria
    }

    public sealed class Terreno
    {
        public Sprite chao;                // o chão inteiro, numa imagem só (pivô no canto de baixo-esquerda)
        public Sprite[][] quadrosDaAgua;   // [água n][quadro]: ondinhas, espuma e brilhos por cima do chão
        public Vector2Int[] celulasDaAgua; // a célula de cada água n
    }

    public const int QuadrosDaAgua = 6;

    static Terreno cache;
    static string[] desenhoDoCache;

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar()
    {
        cache = null;
        desenhoDoCache = null;
        atlasDoCaminho = null;
    }

    // O terreno do desenho (pintado na primeira vez; depois vem do cache, porque é sempre o mesmo mapa).
    public static Terreno PegarTerreno(string[] desenho, Regioes regioes)
    {
        if (cache != null && cache.chao != null && desenhoDoCache == desenho) return cache;
        cache = new Pintor(desenho, regioes).Pintar();
        desenhoDoCache = desenho;
        return cache;
    }

    // Tipos de pixel do chão.
    const byte Ceu = 0, Chao = 1, AguaT = 2, Abismo = 3;

    // Quem pinta o terreno: guarda os "mapas" de cada pixel (tipo, distâncias...) enquanto trabalha.
    sealed class Pintor
    {
        readonly string[] d;
        readonly Regioes reg;
        readonly int W, H, L, A, linhasDeCeu;
        readonly byte[] tipo;
        readonly ushort[] dAgua, dPraia, dAbismo, dBorda; // distâncias (x3) até a margem
        readonly byte[] acima;                            // quantos pixels de água/abismo tem em cima (até a margem)
        float[] celAgua, celAbismo, celProfundo, celMata, celRocha, celFlores;
        bool[] pertoDeAgua, pertoDeAbismo, andavel;
        float[] bordaDaSombra, bordaDoSeco; // o "tremido" das bordas das regiões (só depende da altura do pixel)
        RuidoGrosso claridadeDaGrama;       // manchas grandes e médias da grama (morrinhos)
        RuidoGrosso mataSuave, rochaSuave, floresSuave, profundoSuave, jeitoDaOnda, aguaSuave, abismoSuave; // os campos macios, prontos

        public Pintor(string[] desenho, Regioes regioes)
        {
            d = desenho;
            reg = regioes;
            W = d[0].Length;
            H = d.Length;
            L = W * P;
            A = H * P;
            while (linhasDeCeu < H && d[linhasDeCeu][0] == 'c') linhasDeCeu++;
            tipo = new byte[L * A];
            dAgua = new ushort[L * A]; dPraia = new ushort[L * A]; dAbismo = new ushort[L * A]; dBorda = new ushort[L * A];
            acima = new byte[L * A];
        }

        char Letra(int x, int linha) => d[Mathf.Clamp(linha, 0, H - 1)][Mathf.Clamp(x, 0, W - 1)];

        static bool EhPonto(char c) => (c >= '1' && c <= '9') || (c >= 'A' && c <= 'G') || c == 'S';

        // --------- 1) campos por célula (1 = tem, 0 = não tem), alguns "borrados" para ficarem macios
        void PrepararCelulas()
        {
            int n = W * H;
            celAgua = new float[n]; celAbismo = new float[n]; celMata = new float[n]; celRocha = new float[n]; celFlores = new float[n];
            andavel = new bool[n];
            for (int l = 0; l < H; l++)
                for (int x = 0; x < W; x++)
                {
                    char c = d[l][x];
                    int i = l * W + x;
                    celAgua[i] = c == '~' || c == '=' ? 1f : 0f;
                    celAbismo[i] = c == 'a' || c == '#' ? 1f : 0f;
                    celMata[i] = c == 'T' || c == 'p' || c == 'Y' ? 1f : 0f;
                    celRocha[i] = c == '^' || c == 'o' || c == 'R' || c == '*' ? 1f : 0f;
                    celFlores[i] = c == 'f' ? 1f : 0f;
                    andavel[i] = c == '+' || EhPonto(c);
                }
            // o céu "continua" o que tem embaixo dele (o rio e o abismo vêm lá do horizonte, sem uma margem reta)
            for (int l = 0; l < linhasDeCeu && linhasDeCeu < H; l++)
                for (int x = 0; x < W; x++)
                {
                    celAgua[l * W + x] = celAgua[linhasDeCeu * W + x];
                    celAbismo[l * W + x] = celAbismo[linhasDeCeu * W + x];
                }
            celProfundo = Borrar(Borrar(Borrar(celAgua)));
            celMata = Borrar(celMata);
            celRocha = Borrar(celRocha);
            celFlores = Borrar(celFlores);
            pertoDeAgua = Perto(celAgua);
            pertoDeAbismo = Perto(celAbismo);

            bordaDaSombra = new float[A];
            bordaDoSeco = new float[A];
            for (int py = 0; py < A; py++)
            {
                bordaDaSombra[py] = (Fractal(0, py, 24f, 61) - 0.5f) * 5f;
                bordaDoSeco[py] = (Fractal(5, py, 20f, 67) - 0.5f) * 4f;
            }
            claridadeDaGrama = new RuidoGrosso(L, A, (px, py) => (Fractal(px, py, 46f, 3) - 0.5f) * 0.36f + (Fractal(px, py, 10f, 5) - 0.5f) * 0.2f);
            mataSuave = new RuidoGrosso(L, A, (px, py) => Campo(celMata, px, py));
            rochaSuave = new RuidoGrosso(L, A, (px, py) => Campo(celRocha, px, py));
            floresSuave = new RuidoGrosso(L, A, (px, py) => Campo(celFlores, px, py));
            profundoSuave = new RuidoGrosso(L, A, (px, py) => Campo(celProfundo, px, py));
            jeitoDaOnda = new RuidoGrosso(L, A, (px, py) => Fractal(px, py, 12f, 107));
            aguaSuave = new RuidoGrosso(L, A, (px, py) => Campo(celAgua, px, py));
            abismoSuave = new RuidoGrosso(L, A, (px, py) => Campo(celAbismo, px, py));
        }

        // Média de cada célula com as 8 vizinhas (deixa o campo "macio").
        float[] Borrar(float[] f)
        {
            var r = new float[f.Length];
            for (int l = 0; l < H; l++)
                for (int x = 0; x < W; x++)
                {
                    float soma = 0f;
                    for (int dl = -1; dl <= 1; dl++)
                        for (int dx = -1; dx <= 1; dx++)
                            soma += f[Mathf.Clamp(l + dl, 0, H - 1) * W + Mathf.Clamp(x + dx, 0, W - 1)];
                    r[l * W + x] = soma / 9f;
                }
            return r;
        }

        // A célula ou alguma vizinha tem isso? (só nessas vale a pena calcular a margem pixel a pixel)
        bool[] Perto(float[] f)
        {
            var r = new bool[f.Length];
            for (int l = 0; l < H; l++)
                for (int x = 0; x < W; x++)
                    for (int dl = -1; dl <= 1 && !r[l * W + x]; dl++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (f[Mathf.Clamp(l + dl, 0, H - 1) * W + Mathf.Clamp(x + dx, 0, W - 1)] > 0f) { r[l * W + x] = true; break; }
            return r;
        }

        // O valor de um campo de células num pixel: mistura das 4 células mais perto (com curva suave).
        float Campo(float[] f, int px, int py)
        {
            float u = (px + 0.5f) / P - 0.5f, v = (py + 0.5f) / P - 0.5f; // v = linha contada de baixo
            int x0 = Mathf.FloorToInt(u), v0 = Mathf.FloorToInt(v);
            float tx = u - x0, ty = v - v0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float Em(int x, int vv) => f[Mathf.Clamp(H - 1 - vv, 0, H - 1) * W + Mathf.Clamp(x, 0, W - 1)];
            return Mathf.Lerp(Mathf.Lerp(Em(x0, v0), Em(x0 + 1, v0), tx), Mathf.Lerp(Em(x0, v0 + 1), Em(x0 + 1, v0 + 1), tx), ty);
        }

        // Quanto o pixel está na região sombria (0 a 1), com a borda ondulada pelo ruído.
        float FatorSombrio(int px, int py)
        {
            float x = (px + 0.5f) / P + bordaDaSombra[py];
            return Mathf.Clamp01((x - reg.inicioDaSombra + 0.5f) / 4f);
        }

        // Quanto o pixel está na terra seca do desfiladeiro (0 a 1).
        float FatorSeco(int px, int py)
        {
            float x = (px + 0.5f) / P - 0.5f + bordaDoSeco[py];
            float dentro = Mathf.Min(x - reg.inicioDoDesfiladeiro, reg.fimDoDesfiladeiro - x) + 0.5f;
            return Mathf.Clamp01(dentro / 2f + 0.5f);
        }

        // --------- 2) de que é cada pixel: céu, chão, água ou abismo
        void Classificar()
        {
            for (int py = 0; py < A; py++)
                for (int px = 0; px < L; px++)
                {
                    int x = px / P, linha = H - 1 - py / P, i = py * L + px;
                    if (linha < linhasDeCeu) { tipo[i] = Ceu; continue; }
                    int c = linha * W + x;
                    byte t = Chao;
                    if (pertoDeAgua[c])
                    {
                        // a margem é onde o campo de água passa de 0,5; o ruído entorta a margem.
                        // (nas células de caminho a água quase não avança: o caminho não fica "boiando")
                        float n = Fractal(px, py, 9f, 11) - 0.5f;
                        if (n > 0f && andavel[c]) n *= 0.2f;
                        if (aguaSuave.Em(px, py) + n * 0.6f > 0.5f) t = AguaT;
                    }
                    if (t == Chao && pertoDeAbismo[c])
                    {
                        float n = Fractal(px, py, 6f, 13) - 0.5f;
                        if (n > 0f && andavel[c]) n *= 0.2f;
                        if (abismoSuave.Em(px, py) + n * 0.4f > 0.5f) t = Abismo;
                    }
                    tipo[i] = t;
                }

            // distâncias até a margem (o "x3" é do método: 3 por passo reto, 4 na diagonal)
            Distancias(AguaT, true, dAgua, true); // água: até a margem (o rio continua "para dentro" do horizonte)
            Distancias(AguaT, false, dPraia);  // chão: até a água mais perto
            Distancias(Abismo, true, dAbismo, true); // (o abismo continua "para dentro" do horizonte)
            Distancias(Abismo, false, dBorda);

            // quantos pixels de água (ou de abismo) seguidos tem em cima de cada um: a margem "de cima" vira barranco
            for (int px = 0; px < L; px++)
            {
                int corrida = 0;
                byte anterior = Ceu;
                for (int py = A - 1; py >= 0; py--)
                {
                    byte t = tipo[py * L + px];
                    corrida = t == anterior && (t == AguaT || t == Abismo) ? Mathf.Min(corrida + 1, 250) : 0;
                    anterior = t;
                    acima[py * L + px] = (byte)corrida;
                }
            }
        }

        // "Chamfer distance": duas passadas (de cima para baixo e de baixo para cima) acham, para cada pixel
        // de dentro, a distância até o pixel de fora mais perto. É bem rápido e quase igual à distância de verdade.
        // (dentro = os pixels do tipo "qual" se igual for true; os que NÃO são dele, se for false)
        void Distancias(byte qual, bool igual, ushort[] saida, bool ceuDentro = false)
        {
            const ushort Longe = 3000;
            for (int i = 0; i < saida.Length; i++)
                saida[i] = tipo[i] == Ceu ? (ceuDentro ? Longe : (ushort)0) : (tipo[i] == qual) == igual ? Longe : (ushort)0;
            for (int py = A - 1; py >= 0; py--)
                for (int px = 0; px < L; px++)
                {
                    int i = py * L + px;
                    int m = saida[i];
                    if (m == 0) continue;
                    if (px > 0 && saida[i - 1] + 3 < m) m = saida[i - 1] + 3;
                    if (py < A - 1)
                    {
                        if (saida[i + L] + 3 < m) m = saida[i + L] + 3;
                        if (px > 0 && saida[i + L - 1] + 4 < m) m = saida[i + L - 1] + 4;
                        if (px < L - 1 && saida[i + L + 1] + 4 < m) m = saida[i + L + 1] + 4;
                    }
                    saida[i] = (ushort)m;
                }
            for (int py = 0; py < A; py++)
                for (int px = L - 1; px >= 0; px--)
                {
                    int i = py * L + px;
                    int m = saida[i];
                    if (m == 0) continue;
                    if (px < L - 1 && saida[i + 1] + 3 < m) m = saida[i + 1] + 3;
                    if (py > 0)
                    {
                        if (saida[i - L] + 3 < m) m = saida[i - L] + 3;
                        if (px > 0 && saida[i - L - 1] + 4 < m) m = saida[i - L - 1] + 4;
                        if (px < L - 1 && saida[i - L + 1] + 4 < m) m = saida[i - L + 1] + 4;
                    }
                    saida[i] = (ushort)m;
                }
        }

        byte TipoEm(int px, int py) => px < 0 || px >= L || py < 0 || py >= A ? Chao : tipo[py * L + px];

        // --------- 3) a cor de cada pixel
        Color32[] rampa;   // para os carimbos (tufos) saberem o material de cada pixel
        int[] tomDe;
        Color32[][] rampaDe;

        public Terreno Pintar()
        {
            PrepararCelulas();
            Classificar();

            rampaDe = new Color32[L * A][];
            tomDe = new int[L * A];
            var cores = new Color32[L * A];
            for (int py = 0; py < A; py++)
                for (int px = 0; px < L; px++)
                {
                    int i = py * L + px;
                    switch (tipo[i])
                    {
                        case AguaT: PintarAgua(px, py, i); break;
                        case Abismo: PintarAbismo(px, py, i); break;
                        case Chao: PintarChao(px, py, i); break;
                    }
                }
            Carimbos();

            // a cor final; perto do horizonte a terra fica um pouco "enevoada" (perspectiva aérea do HD-2D)
            int topoDaTerra = (H - linhasDeCeu) * P;
            var neblina = new Color32(176, 202, 222, 255);
            for (int py = 0; py < A; py++)
            {
                float longe = Mathf.Clamp01(1f - (topoDaTerra - 1 - py) / 22f);
                longe = longe * longe * 0.45f;
                for (int px = 0; px < L; px++)
                {
                    int i = py * L + px;
                    if (rampaDe[i] == null) { cores[i] = FabricaDeSprites.Transparente; continue; }
                    Color32 c = rampaDe[i][Mathf.Clamp(tomDe[i], 0, rampaDe[i].Length - 1)];
                    if (rampaDe[i] == CorDireta) c = direta[i];
                    cores[i] = longe > 0f ? Color32.Lerp(c, neblina, longe) : c;
                }
            }

            var textura = new Texture2D(L, A, TextureFormat.RGBA32, false)
            {
                name = "Chao do mapa",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            textura.SetPixels32(cores);
            textura.Apply();
            var terreno = new Terreno
            {
                chao = Sprite.Create(textura, new Rect(0, 0, L, A), Vector2.zero, P, 0, SpriteMeshType.FullRect),
            };
            terreno.chao.name = "mapa_chao";
            QuadrosDaAgua(terreno);
            return terreno;
        }

        // Pixels com cor "solta" (fora das rampas): flores.
        static readonly Color32[] CorDireta = new Color32[1];
        Color32[] direta;

        void Por(int i, Color32[] r, int t)
        {
            rampaDe[i] = r;
            tomDe[i] = t;
        }

        // ----- a água: funda (escura) no meio, clara na margem, barranco e sombra em cima, espuma na beira
        void PintarAgua(int px, int py, int i)
        {
            float distancia = dAgua[i] / 3f;
            float fundo = Mathf.Clamp01(distancia / 14f) * 0.55f + profundoSuave.Em(px, py) * 0.45f;
            float claridade = 0.82f - 0.66f * fundo + (Fractal(px, py, 14f, 23) - 0.5f) * 0.12f;
            float sombrio = FatorSombrio(px, py);
            Color32[] r = sombrio + Limiar(px, py) * 0.6f > 0.5f ? AguaSombria : Agua;
            int t = Tom(claridade, r.Length, px, py, 0.8f);

            int emCima = acima[i];
            bool margemEmCima = TipoEm(px, py + emCima + 1) == Chao;
            int barranco = 2 + (Sorteio(px, 1, 29) < 0.35f ? 1 : 0);
            if (margemEmCima && emCima < barranco)
            {
                // o barranco: a "parede" de terra da margem de cima, vista de frente (com uma linha de luz no topo)
                Color32[] parede = sombrio > 0.5f ? Pedra : Terra;
                Por(i, parede, emCima == 0 ? 4 : emCima == 1 ? 2 : 1);
                return;
            }
            if (margemEmCima && emCima < barranco + 4) t -= emCima < barranco + 2 ? 2 : 1; // a sombra do barranco na água
            else if (distancia <= 1.01f) { Por(i, r, r.Length - 1); return; }              // espuma parada na beira
            else if (distancia <= 2.01f && ((px + py) & 1) == 0) t = Mathf.Max(t, r.Length - 2);
            Por(i, r, t);
        }

        // ----- o abismo: a parede de rocha do outro lado (em cima), em camadas, sumindo no escuro
        void PintarAbismo(int px, int py, int i)
        {
            int emCima = acima[i];
            bool beiraEmCima = TipoEm(px, py + emCima + 1) == Chao;
            int altura = 9 + Mathf.RoundToInt(Fractal(px, 0, 5f, 31) * 6f);
            float lado = dAbismo[i] / 3f;
            if (beiraEmCima && emCima < altura)
            {
                // camadas da rocha: faixas que ondulam um pouco; a de cima pega luz
                int camada = (emCima + Mathf.RoundToInt(Fractal(px, py, 7f, 37) * 3f)) / 3;
                Color32[] r = camada % 2 == 0 ? Terra : Pedra;
                float v = 0.72f - emCima / (float)altura * 0.45f + (Fractal(px, py, 3f, 41) - 0.5f) * 0.2f;
                if (emCima == 0) v = 0.85f;
                int t = Tom(v, r.Length, px, py);
                if (emCima >= altura - 3) // o pé da parede some no escuro (com dithering)
                {
                    float some = (emCima - (altura - 3)) / 3f;
                    if (some + Limiar(px, py) > 0.5f) { Por(i, Escuridao, 3); return; }
                }
                Por(i, r, t);
                return;
            }
            // o fundo: quanto mais longe das bordas, mais escuro; pedrinhas e fendas lá embaixo
            float claro = Mathf.Clamp01(1f - lado / 9f) * 0.5f + (Fractal(px, py, 8f, 43) - 0.5f) * 0.3f;
            if (lado <= 1.01f && !beiraEmCima) claro = 0.85f; // a beirada das paredes dos lados
            int te = Tom(claro, Escuridao.Length, px, py, 0.9f);
            if (Sorteio(px, py, 47) > 0.985f) te = Mathf.Min(te + 2, Escuridao.Length - 1);
            Por(i, Escuridao, te);
        }

        // ----- o chão: grama (normal ou sombria), terra seca, rocha, areia da praia
        void PintarChao(int px, int py, int i)
        {
            float limiar = Limiar(px, py);
            float praia = dPraia[i] / 3f;
            float sombrio = FatorSombrio(px, py);
            bool escuro = sombrio > 0f && sombrio + (Fractal(px, py, 6f, 17) - 0.5f) * 0.5f + limiar * 0.35f > 0.5f;

            // a beira do abismo: a borda de cima pega luz; a de baixo é o "lábio" escuro do penhasco
            float borda = dBorda[i] / 3f;
            if (borda <= 2.01f)
            {
                bool abismoEmbaixo = TipoEm(px, py - 1) == Abismo || TipoEm(px, py - 2) == Abismo;
                bool abismoEmCima = TipoEm(px, py + 1) == Abismo;
                if (abismoEmCima && borda <= 1.01f) { Por(i, Terra, 0); return; }
                if (abismoEmbaixo)
                {
                    // a beirada do penhasco pega o sol (na cor do chão daquele lugar)
                    Color32[] beira = FatorSeco(px, py) > 0.5f ? Terra : escuro ? CapimSombrio : Capim;
                    Por(i, beira, borda <= 1.01f ? 6 : 5);
                    return;
                }
            }

            // a areia da praia (na região sombria, cascalho cinza)
            float larguraDaPraia = praia > 5.5f ? 0f : 2.5f + Fractal(px, py, 10f, 19) * 3f;
            if (praia <= larguraDaPraia)
            {
                Color32[] r = escuro ? Pedra : Areia;
                if (praia <= 1.01f) { Por(i, r, escuro ? 2 : 2); return; } // areia molhada
                float v = 0.75f + (Fractal(px, py, 4f, 21) - 0.5f) * 0.25f - (praia > larguraDaPraia - 1f ? 0.1f : 0f);
                Por(i, r, Tom(v, r.Length, px, py));
                return;
            }

            // relevo bem suave: manchas grandes claras e escuras (morrinhos), mais as médias
            float manchas = claridadeDaGrama.Em(px, py);
            float claridade = 0.53f + manchas;
            float mata = mataSuave.Em(px, py);
            claridade -= mata * 0.3f; // embaixo das árvores é mais escuro (a copa tapa o sol)

            float seco = FatorSeco(px, py);
            if (seco > 0f && seco + (Fractal(px, py, 5f, 71) - 0.5f) * 0.6f + limiar * 0.3f > 0.5f)
            {
                // terra seca e rachada do desfiladeiro
                float v = 0.72f + manchas * 0.8f + (Fractal(px, py, 4f, 73) - 0.5f) * 0.16f;
                int t = Tom(v, Terra.Length, px, py);
                float racha = Mathf.Abs(Fractal(px, py, 11f, 79) - 0.5f);
                if (racha < 0.012f) t = 3;
                else if (racha < 0.024f && Sorteio(px, py, 83) < 0.5f) t--;
                Por(i, Terra, t);
                return;
            }

            float rocha = rochaSuave.Em(px, py);
            if (rocha > 0.25f && rocha + (Fractal(px, py, 5f, 87) - 0.5f) * 0.5f + limiar * 0.25f > 0.62f)
            {
                // chão de pedra solta (pé das montanhas e das ruínas)
                Color32[] r = Fractal(px, py, 4f, 89) > 0.42f ? Pedra : Terra;
                float v = (r == Pedra ? 0.62f : 0.55f) + (Fractal(px, py, 3f, 91) - 0.5f) * 0.3f;
                if (escuro) v -= 0.18f;
                Por(i, r, Tom(v, r.Length, px, py));
                return;
            }

            Por(i, escuro ? CapimSombrio : Capim, Tom(claridade, Capim.Length, px, py));
        }

        // ----- carimbos: tufos de capim, florzinhas, folhas caídas e pedrinhas espalhados (cada um num lugar)
        void Carimbos()
        {
            direta = new Color32[L * A];
            Color32[] petalas = { ArteDosBlocos.PetalaRosa, ArteDosBlocos.PetalaAmarela, ArteDosBlocos.PetalaBranca, C(140, 170, 255), C(236, 90, 70) };
            for (int py = 2; py < A - 3; py++)
                for (int px = 2; px < L - 2; px++)
                {
                    int i = py * L + px;
                    Color32[] r = rampaDe[i];
                    if (r != Capim && r != CapimSombrio) continue;
                    float s = Sorteio(px, py, 97);
                    float flores = floresSuave.Em(px, py);
                    if (s < 0.024f)
                    {
                        // tufo de capim: base escura, folhas para os lados e pontas claras (2 ou 3 de altura)
                        if (rampaDe[i + L] != r || rampaDe[i + 2 * L] != r || rampaDe[i - 1] != r || rampaDe[i + 1] != r) continue;
                        int t = tomDe[i];
                        bool alto = s < 0.008f;
                        Por(i, r, t - 1);
                        Por(i + 1 - L, r, t - 2); // a sombrinha do tufo cai para a direita
                        Por(i - 1 + L, r, t + 1);
                        Por(i + 1 + L, r, t + 1);
                        Por(i + L, r, t);
                        Por(i - 1 + 2 * L, r, t + 2);
                        if (alto) { Por(i + 2 * L, r, t + 1); Por(i + 3 * L, r, t + 2); }
                        else Por(i + 1 + 2 * L, r, t + 2);
                    }
                    else if (s > 0.996f - flores * 0.03f && r == Capim)
                    {
                        // florzinha: um pixel colorido com o caule embaixo
                        int cor = (int)(Sorteio(px, py, 101) * petalas.Length) % petalas.Length;
                        rampaDe[i + L] = CorDireta;
                        direta[i + L] = petalas[cor];
                        Por(i, r, tomDe[i] - 2);
                    }
                    else if (s > 0.97f && mataSuave.Em(px, py) > 0.45f)
                    {
                        // folhinha caída embaixo das árvores
                        Por(i, Sorteio(px, py, 103) < 0.5f ? Terra : r, r == Capim ? 4 : 3);
                    }
                }
        }

        // ----- 4) as ondinhas animadas: um recorte de 16 x 16 por célula com água, em QuadrosDaAgua quadros.
        // Todas as células mostram o MESMO quadro ao mesmo tempo (quem troca é a VidaDoMapa), e o desenho de
        // cada quadro usa a posição do pixel no mapa inteiro: as ondas "passam" de uma célula para a outra sem emenda.
        void QuadrosDaAgua(Terreno terreno)
        {
            var celulas = new List<Vector2Int>();
            for (int l = linhasDeCeu; l < H; l++)
                for (int x = 0; x < W; x++)
                {
                    bool temAgua = false;
                    int px0 = x * P, py0 = (H - 1 - l) * P;
                    for (int y = 0; y < P && !temAgua; y++)
                        for (int k = 0; k < P; k++)
                            if (tipo[(py0 + y) * L + px0 + k] == AguaT) { temAgua = true; break; }
                    if (temAgua) celulas.Add(new Vector2Int(x, l));
                }

            const int Celula = P + 2; // 1 pixel de folga em volta de cada recorte
            int total = celulas.Count * ArteDoMapa.QuadrosDaAgua;
            int colunas = 56, linhas = (total + colunas - 1) / colunas;
            int largura = colunas * Celula, altura = Mathf.Max(1, linhas) * Celula;
            var pixels = new Color32[largura * altura];
            var quadro = new Color32[ArteDoMapa.QuadrosDaAgua][];
            for (int q = 0; q < quadro.Length; q++) quadro[q] = new Color32[P * P];
            for (int n = 0; n < celulas.Count; n++)
            {
                int px0 = celulas[n].x * P, py0 = (H - 1 - celulas[n].y) * P;
                for (int y = 0; y < P; y++)
                    for (int x = 0; x < P; x++)
                        Onda(px0 + x, py0 + y, quadro, y * P + x);
                for (int q = 0; q < quadro.Length; q++)
                {
                    int k = n * ArteDoMapa.QuadrosDaAgua + q;
                    int ax = (k % colunas) * Celula, ay = (k / colunas) * Celula;
                    for (int y = 0; y < Celula; y++)
                        for (int x = 0; x < Celula; x++)
                            pixels[(ay + y) * largura + ax + x] = quadro[q][Mathf.Clamp(y - 1, 0, P - 1) * P + Mathf.Clamp(x - 1, 0, P - 1)];
                }
            }
            var textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false)
            {
                name = "Ondas do mapa",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            textura.SetPixels32(pixels);
            textura.Apply();
            terreno.celulasDaAgua = celulas.ToArray();
            terreno.quadrosDaAgua = new Sprite[celulas.Count][];
            for (int n = 0; n < celulas.Count; n++)
            {
                terreno.quadrosDaAgua[n] = new Sprite[ArteDoMapa.QuadrosDaAgua];
                for (int q = 0; q < ArteDoMapa.QuadrosDaAgua; q++)
                {
                    int k = n * ArteDoMapa.QuadrosDaAgua + q;
                    var recorte = new Rect((k % colunas) * Celula + 1, (k / colunas) * Celula + 1, P, P);
                    terreno.quadrosDaAgua[n][q] = Sprite.Create(textura, recorte, FabricaDeSprites.Centro, P, 0, SpriteMeshType.FullRect);
                }
            }
        }

        // A cor de um pixel das ondinhas em CADA quadro (transparente onde não tem nada).
        // O que não muda de um quadro para o outro (distância até a margem, o "jeito" da onda ali) é calculado uma vez só.
        void Onda(int px, int py, Color32[][] quadros, int j)
        {
            for (int q = 0; q < quadros.Length; q++) quadros[q][j] = FabricaDeSprites.Transparente;
            int i = py * L + px;
            if (tipo[i] != AguaT) return;
            int emCima = acima[i];
            if (TipoEm(px, py + emCima + 1) == Chao && emCima < 3) return; // no barranco, nada
            float distancia = dAgua[i] / 3f;
            float escuro = FatorSombrio(px, py) > 0.5f ? 0.55f : 1f; // no mar sombrio tudo brilha menos
            float jeito = jeitoDaOnda.Em(px, py);
            bool quebrada = Ruido(px / 5f, py / 5f, 109) > 0.45f;

            // brilhinho do sol: um a cada pedaço de 9 x 7 pixels (em 40% deles), cada um aceso num momento
            int gx = px / 9, gy = py / 7;
            float s = Sorteio(gx, gy, 113);
            int cx = gx * 9 + 2 + (int)(Sorteio(gx, gy, 127) * 5f), cy = gy * 7 + 2 + (int)(Sorteio(gx, gy, 131) * 3f);

            for (int q = 0; q < quadros.Length; q++)
            {
                float fase = q / (float)quadros.Length;
                // 1) a espuma da beira "respira" (avança e recua)
                float espuma = 1.1f + 1.3f * (0.5f + 0.5f * Mathf.Sin((fase + jeito) * Mathf.PI * 2f));
                if (distancia <= espuma)
                {
                    bool rala = distancia > espuma - 1f && ((px + py + q) & 1) == 1;
                    quadros[q][j] = rala ? FabricaDeSprites.Transparente : new Color32(Espuma.r, Espuma.g, Espuma.b, (byte)(210 * escuro));
                    continue;
                }
                // 2) uma ondinha chegando na margem (uma linha clara, quebrada, que anda até a espuma)
                float onda = 6.5f - 5f * Mathf.Repeat(fase + jeito * 0.6f, 1f);
                if (Mathf.Abs(distancia - onda) < 0.55f && quebrada)
                {
                    quadros[q][j] = new Color32(196, 238, 236, (byte)(120 * escuro));
                    continue;
                }
                // 3) os brilhinhos do sol no meio da água: acendem, crescem e apagam
                if (distancia > 3f && s < 0.4f && py == cy)
                {
                    int vida = (q + (int)(s * 40f)) % quadros.Length; // em que momento do brilho ele está
                    int meia = vida == 1 ? 1 : vida == 2 ? 2 : vida == 3 ? 1 : vida == 0 ? 0 : -1;
                    if (meia >= 0 && Mathf.Abs(px - cx) <= meia)
                    {
                        quadros[q][j] = new Color32(255, 255, 240, (byte)((px == cx ? 255 : 170) * escuro));
                        continue;
                    }
                }
                // 4) riscos de reflexo andando devagar com a correnteza
                if (distancia > 2f && Ruido((px - q * 1.5f) / 6f, py / 2.2f, 137) > 0.74f)
                    quadros[q][j] = new Color32(190, 230, 240, (byte)(70 * escuro));
            }
        }

    }

    // ---------------------------------------------------------------- o caminho de terra batida

    // Um recorte por "jeito": quais lados do caminho continuam (4 bits: direita, esquerda, cima, baixo),
    // 4 versões de cada (pedrinhas e beiradas em lugares diferentes) e a "praça" redonda das fases.
    // A largura do caminho na beirada da célula é SEMPRE a mesma (10 pixels): qualquer versão encaixa em
    // qualquer outra. No meio da célula a beirada ondula, e o capim avança um pouquinho por cima da terra.
    public const int Direita = 1, Esquerda = 2, Cima = 4, Baixo = 8;
    const int VersoesDoCaminho = 4;
    static Texture2D atlasDoCaminho;

    // nome: "mapa_caminho_<lados>_<versão>" ou "mapa_praca_<lados>"
    public static string NomeDoCaminho(int lados, int versao, bool praca) =>
        praca ? "mapa_praca_" + lados : "mapa_caminho_" + lados + "_" + (versao % VersoesDoCaminho);

    static void RegistrarCaminhos()
    {
        for (int lados = 0; lados < 16; lados++)
        {
            int l = lados;
            for (int v = 0; v < VersoesDoCaminho; v++)
            {
                int vv = v;
                FabricaDeSprites.Registrar(NomeDoCaminho(l, vv, false), () => RecorteDoCaminho(l * (VersoesDoCaminho + 1) + vv));
            }
            FabricaDeSprites.Registrar(NomeDoCaminho(l, 0, true), () => RecorteDoCaminho(l * (VersoesDoCaminho + 1) + VersoesDoCaminho));
        }
    }

    static Sprite RecorteDoCaminho(int indice)
    {
        const int Celula = P + 2, Colunas = VersoesDoCaminho + 1;
        if (atlasDoCaminho == null)
        {
            int largura = Colunas * Celula, altura = 16 * Celula;
            var pixels = new Color32[largura * altura];
            for (int lados = 0; lados < 16; lados++)
                for (int v = 0; v <= VersoesDoCaminho; v++)
                {
                    Color32[] tile = Caminho(lados, v, v == VersoesDoCaminho);
                    int x0 = v * Celula, y0 = lados * Celula;
                    for (int y = 0; y < Celula; y++)
                        for (int x = 0; x < Celula; x++)
                            pixels[(y0 + y) * largura + x0 + x] = tile[Mathf.Clamp(y - 1, 0, P - 1) * P + Mathf.Clamp(x - 1, 0, P - 1)];
                }
            atlasDoCaminho = new Texture2D(largura, altura, TextureFormat.RGBA32, false)
            {
                name = "Atlas do caminho",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            atlasDoCaminho.SetPixels32(pixels);
            atlasDoCaminho.Apply();
        }
        var recorte = new Rect((indice % Colunas) * Celula + 1, (indice / Colunas) * Celula + 1, P, P);
        return Sprite.Create(atlasDoCaminho, recorte, FabricaDeSprites.Centro, P, 0, SpriteMeshType.FullRect);
    }

    // Está dentro do caminho? (coordenadas do recorte, podendo sair um pouco dele: o vizinho continua)
    static bool NoCaminho(int lados, int versao, bool praca, int x, int y)
    {
        float cx = x + 0.5f - 8f, cy = y + 0.5f - 8f;
        float raio = praca ? 7.6f : 5.3f + (Sorteio(versao, 0, 151) - 0.5f) * 0.8f;
        if (cx * cx + cy * cy <= raio * raio) return true;
        // os "braços" que vão até a beirada: meia largura 5 na beirada, ondulando no meio
        bool Braco(float aoLongo, float deLado, int semente)
        {
            if (aoLongo < 0f) return false;
            float ateABeira = 8f - aoLongo;                          // 0 na beirada da célula
            float ondula = Mathf.Clamp01(ateABeira / 4f) * (Ruido(aoLongo * 0.45f, versao * 3.7f, semente) - 0.5f) * 2.4f;
            return Mathf.Abs(deLado - ondula * 0.5f) <= 5f + ondula * 0.5f;
        }
        return ((lados & Direita) != 0 && Braco(cx, cy, 161)) || ((lados & Esquerda) != 0 && Braco(-cx, cy, 163))
            || ((lados & Cima) != 0 && Braco(cy, cx, 167)) || ((lados & Baixo) != 0 && Braco(-cy, cx, 169));
    }

    static Color32[] Caminho(int lados, int versao, bool praca)
    {
        var t = new ArteDosBlocos.Tela(P, P);
        bool Dentro(int x, int y)
        {
            // fora do recorte: continua só se o lado está ligado (e na faixa do meio)
            if (x < 0 && (lados & Esquerda) == 0) return false;
            if (x >= P && (lados & Direita) == 0) return false;
            if (y < 0 && (lados & Baixo) == 0) return false;
            if (y >= P && (lados & Cima) == 0) return false;
            if (x < 0 || x >= P || y < 0 || y >= P) return Mathf.Abs(Mathf.Clamp(x, 0, P - 1) + 0.5f - 8f) <= 5f || Mathf.Abs(Mathf.Clamp(y, 0, P - 1) + 0.5f - 8f) <= 5f;
            return NoCaminho(lados, versao, praca, x, y);
        }
        int semente = 200 + lados * 7 + versao * 31;
        for (int y = 0; y < P; y++)
            for (int x = 0; x < P; x++)
            {
                if (!Dentro(x, y)) continue;
                // terra batida: clara, com manchas e grãozinhos
                float v = 0.72f + (ArteDosBlocos.Sorteio(x, y, semente) - 0.5f) * 0.14f + (Ruido(x / 3f, y / 3f, semente) - 0.5f) * 0.24f;
                // as marcas das rodas das carroças: duas faixas um pouco mais escuras ao longo do caminho
                bool deitado = (lados & (Direita | Esquerda)) != 0, emPe = (lados & (Cima | Baixo)) != 0;
                float trilha = deitado && !emPe ? Mathf.Abs(Mathf.Abs(y + 0.5f - 8f) - 2.5f) : emPe && !deitado ? Mathf.Abs(Mathf.Abs(x + 0.5f - 8f) - 2.5f) : 9f;
                if (trilha < 0.6f && Sorteio(x, y, semente + 3) < 0.7f) v -= 0.16f;
                int tom = Tom(v, Estrada.Length, x, y);
                // beiradas: a de cima e a da esquerda ficam na sombra do barranco de capim; a de baixo pega luz
                bool foraEmCima = !Dentro(x, y + 1), foraEsq = !Dentro(x - 1, y), foraEmbaixo = !Dentro(x, y - 1), foraDir = !Dentro(x + 1, y);
                if (foraEmCima) tom = 0;
                else if (!Dentro(x, y + 2)) tom = Mathf.Min(tom, 2);
                if (foraEsq) tom = Mathf.Min(tom, 1);
                if (foraEmbaixo) tom = 5;
                if (foraDir && !foraEmbaixo) tom = Mathf.Min(tom, 1);
                t.Pintar(x, y, Estrada, tom);
                if (praca && !foraEmCima && !foraEmbaixo && !foraEsq && !foraDir)
                {
                    // a praça das fases: um anel de pedrinhas em volta (o ponto da fase fica no meio)
                    float raio = Mathf.Sqrt((x + 0.5f - 8f) * (x + 0.5f - 8f) + (y + 0.5f - 8f) * (y + 0.5f - 8f));
                    if (raio > 5.6f && raio < 7.2f && Sorteio(x, y, semente + 1) < 0.75f)
                        t.Pintar(x, y, Pedra, (x + y) % 3 == 0 ? 3 : Mathf.RoundToInt(4f + (x - y) * 0.06f));
                }
            }

        // capim "invadindo" a beirada (a borda fica irregular) e pedrinhas no meio da terra
        for (int y = 0; y < P; y++)
            for (int x = 0; x < P; x++)
            {
                if (!t.Cheio(x, y) || t.RampaEm(x, y) != Estrada) continue;
                bool beira = !Dentro(x, y + 1) || !Dentro(x - 1, y) || !Dentro(x + 1, y) || !Dentro(x, y - 1);
                float s = Sorteio(x, y, semente + 2);
                if (beira && s < 0.3f) t.Pintar(x, y, Capim, s < 0.12f ? 4 : 3);
                else if (!beira && s > 0.955f)
                {
                    // pedrinha: clara em cima-esquerda, sombrinha embaixo-direita
                    t.Pintar(x, y, Pedra, 5);
                    if (Dentro(x + 1, y - 1) && t.RampaEm(x + 1, y - 1) == Estrada) t.Mudar(x + 1, y - 1, -2);
                    if (s > 0.985f && Dentro(x + 1, y)) t.Pintar(x + 1, y, Pedra, 3);
                }
            }
        var cores = new Color32[P * P];
        for (int y = 0; y < P; y++)
            for (int x = 0; x < P; x++)
                cores[y * P + x] = t.Cheio(x, y) ? t.CorFinal(x, y) : FabricaDeSprites.Transparente;
        return cores;
    }

    // ---------------------------------------------------------------- pontes

    // Ponte de madeira (sobre a água) ou de corda (sobre o abismo), deitada ou em pé.
    // A de madeira leva a própria sombra na água (2 pixels para baixo e para a direita).
    static readonly Color32[] Madeira = ArteDosBlocos.Madeira;

    static Sprite Ponte(bool emPe, bool corda)
    {
        int w = P + 2, h = P + 3; // folga para a sombra
        var t = new ArteDosBlocos.Tela(w, h);
        var sombra = new Color32(10, 18, 50, 110);
        // o tabuleiro ocupa 10 pixels de largura no meio da célula; (x, y) "deitados": x ao longo da ponte
        for (int a = 0; a < P; a++)
            for (int b = 3; b <= 12; b++)
            {
                int x = emPe ? b : a, y = emPe ? a : b;
                int px = x, py = y + 3; // (a célula começa 3 pixels acima do pé do desenho, por causa da sombra)
                int tabua = a / 3, dentroDaTabua = a % 3;
                bool fresta = dentroDaTabua == 2;
                if (corda && fresta) continue; // entre as tábuas da ponte de corda dá para ver o abismo
                int tom = fresta ? 0 : 3 + (Sorteio(tabua, 0, emPe ? 171 : 173) < 0.3f ? -1 : 0) + (Sorteio(tabua, b, 175) < 0.12f ? 1 : 0);
                if (corda) tom = fresta ? 0 : tom - 1;
                // luz: a beirada de cima (ou da esquerda) do tabuleiro, sombra na de baixo
                if (b == 12) tom++;
                if (b == 3) tom -= 2;
                if (!fresta && dentroDaTabua == 0) tom++;
                t.Pintar(px, py, Madeira, tom);
            }
        // corrimão: cordas (ou vigas) nos dois lados, com pilares
        for (int a = 0; a < P; a++)
            foreach (int b in new[] { 2, 13 })
            {
                int x = emPe ? b : a, y = emPe ? a : b;
                bool pilar = a % 8 == 1;
                if (corda) t.Pintar(x, y + 3, Terra, b == 13 ? 6 : 4);
                else t.Pintar(x, y + 3, Madeira, pilar ? (b == 13 ? 5 : 2) : (b == 13 ? 4 : 1));
                if (pilar && !emPe) t.Pintar(x, y + 4, Madeira, 5); // a cabeça do pilar pega luz
            }
        Color32 Cor(int x, int y)
        {
            if (t.Cheio(x, y)) return t.CorFinal(x, y);
            // sombra na água: o desenho da ponte, 2 pixels para a direita e para baixo
            if (!corda && t.Cheio(x - 2, y + 2)) return sombra;
            return FabricaDeSprites.Transparente;
        }
        var pivo = new Vector2(8f / w, 11f / h);
        return FabricaDeSprites.Procedural(w, h, pivo, Cor);
    }
}
