using UnityEngine;

// ARTE DOS BLOCOS (visual "v3"): chão, terra, pedra, caixote, bloco "?", bloco usado, espinho e cano,
// desenhados por fórmula com muito mais detalhe e volume (estilo Octopath Traveler / Stardew com mods).
//
// As ideias de pixel art usadas aqui (vale a pena procurar cada uma!):
//  - RAMPA DE CORES: cada material tem 6 ou 7 tons, do mais escuro ao mais claro. Os escuros puxam
//    para o azul-arroxeado (sombra "fria") e os claros para o amarelo (luz do sol "quente"). Isso é o
//    "hue shifting": muito mais vivo do que só escurecer/clarear a mesma cor.
//  - LUZ DE CIMA-ESQUERDA: o sol fica no alto, à esquerda. Beiradas viradas para lá ficam claras,
//    as viradas para baixo/direita ficam escuras (é por isso que a sombrinha dos blocos cai para a direita).
//  - OCLUSÃO AMBIENTE: cantinhos e o que fica embaixo de uma "aba" (a grama que escorre) recebem menos
//    luz: ficam mais escuros. É o que dá a sensação de volume.
//  - CONTORNO COLORIDO: a borda de cada material usa o tom mais escuro DELE (verde bem escuro, marrom bem
//    escuro), e não preto puro, que deixa tudo "chapado".
//  - DITHERING: na passagem de um tom para o outro, os pixels se alternam num padrão (matriz de Bayer),
//    em vez de uma linha reta: a transição fica macia mesmo com poucas cores.
//  - VARIAÇÕES: cada tipo de bloco tem várias versões (pedrinhas, raízes, rachaduras e musgo em lugares
//    diferentes), escolhidas pela POSIÇÃO do bloco na fase (veja ConstrutorDeFase.SpriteDoChao).
//    A textura de fundo é "periódica" (a beirada da direita continua na da esquerda), então qualquer
//    versão encosta em qualquer outra sem emenda.
//
// Os blocos continuam com 16 x 16 pixels (1 bloco). Só o bloco com grama em cima é um pouco mais alto
// (16 x 19): as pontas do capim passam 3 pixels acima do bloco, e o pivô fica no meio da parte de 16 x 16,
// então ele continua encaixado no mesmo lugar (o Grumete pisa no topo do bloco, entre as folhinhas).
//
// Nomes dos blocos do chão: "chao_<tipo>_<versão>_<bordas>", por exemplo "chao_topo_2_eb":
//   tipo   = topo (grama em cima), terra (logo abaixo) ou pedra (mais fundo);
//   versão = 0 a 3;
//   bordas = os lados ABERTOS (sem chão vizinho): e = esquerda, d = direita, b = baixo.
public static class ArteDosBlocos
{
    public const int Versoes = 4;

    // ---------------------------------------------------------------- paletas (escuro -> claro)
    // (internal: o ArteDoMapa usa as mesmas rampas, a mesma luz e o mesmo dithering, para o mapa combinar com as fases)

    internal static Color32 C(int r, int g, int b) => new Color32((byte)r, (byte)g, (byte)b, 255);

    internal static readonly Color32[] Capim =
    {
        C(18, 40, 38), C(28, 66, 44), C(44, 96, 46), C(70, 128, 50), C(106, 158, 56), C(152, 188, 70), C(206, 214, 112),
    };
    internal static readonly Color32[] Terra =
    {
        C(30, 18, 28), C(50, 30, 36), C(74, 46, 42), C(100, 64, 50), C(128, 86, 60), C(160, 114, 78), C(194, 150, 104),
    };
    internal static readonly Color32[] Pedra =
    {
        C(26, 26, 42), C(44, 44, 64), C(66, 66, 86), C(92, 92, 110), C(122, 120, 134), C(156, 152, 158), C(194, 188, 182),
    };
    internal static readonly Color32[] Madeira =
    {
        C(38, 20, 24), C(70, 38, 32), C(106, 62, 40), C(144, 90, 50), C(180, 124, 66), C(216, 168, 100),
    };
    internal static readonly Color32[] Ferro =
    {
        C(22, 22, 34), C(52, 54, 72), C(92, 96, 116), C(144, 148, 164), C(206, 206, 210),
    };
    internal static readonly Color32[] Magia =
    {
        C(28, 12, 44), C(56, 24, 86), C(90, 42, 136), C(128, 68, 178), C(168, 108, 218), C(210, 158, 244), C(240, 214, 255),
    };
    internal static readonly Color32[] Ouro =
    {
        C(70, 34, 22), C(150, 88, 28), C(214, 146, 38), C(248, 204, 76), C(255, 240, 168),
    };
    internal static readonly Color32[] Aco =
    {
        C(22, 24, 40), C(52, 58, 82), C(92, 100, 124), C(140, 148, 168), C(194, 200, 212), C(238, 238, 232),
    };
    internal static readonly Color32[] Cano =
    {
        C(12, 32, 30), C(22, 62, 44), C(36, 96, 54), C(58, 132, 62), C(98, 170, 78), C(162, 208, 112), C(222, 240, 176),
    };
    internal static readonly Color32 PetalaRosa = C(232, 96, 122), PetalaAmarela = C(250, 206, 84), PetalaBranca = C(240, 236, 226), Miolo = C(255, 230, 140);

    // Direção de onde vem a luz (para cima e para a esquerda).
    internal const float LuzX = -0.62f, LuzY = 0.78f;

    // ---------------------------------------------------------------- sorteios e ruído

    // Um número "aleatório" de 0 a 1, mas sempre o mesmo para os mesmos (x, y, semente): um hash.
    internal static float Sorteio(int x, int y, int semente)
    {
        unchecked
        {
            uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)semente * 2246822519u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFF) / 65535f;
        }
    }

    // Ruído suave que se repete a cada 16 pixels nas duas direções (a beirada encaixa na beirada oposta).
    // celula = tamanho das "manchas" (2, 4 ou 8 pixels).
    static float RuidoPeriodico(int x, int y, int celula, int semente)
    {
        int n = 16 / celula;
        float fx = (float)x / celula, fy = (float)y / celula;
        int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
        float tx = fx - x0, ty = fy - y0;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        int ax = Volta(x0, n), bx = Volta(x0 + 1, n), ay = Volta(y0, n), by = Volta(y0 + 1, n);
        float a = Mathf.Lerp(Sorteio(ax, ay, semente), Sorteio(bx, ay, semente), tx);
        float b = Mathf.Lerp(Sorteio(ax, by, semente), Sorteio(bx, by, semente), tx);
        return Mathf.Lerp(a, b, ty);
    }

    static int Volta(int v, int n) => ((v % n) + n) % n;

    // Matriz de Bayer 4x4 (o padrão clássico de dithering).
    internal static readonly int[] Bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

    // Converte uma claridade de 0 a 1 no índice de um tom da rampa, com dithering nas passagens.
    internal static int Tom(float claridade, int tons, int x, int y, float mistura = 0.6f)
    {
        float limiar = (Bayer[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f - 0.5f;
        return Mathf.Clamp(Mathf.RoundToInt(claridade * (tons - 1) + limiar * mistura), 0, tons - 1);
    }

    // ---------------------------------------------------------------- a "tela" onde cada bloco é pintado

    // Guarda, para cada pixel, QUAL rampa (material) e QUAL tom dela. Assim dá para escurecer ou clarear
    // um pixel depois (sombra, borda, oclusão) sem trocar de material: é só andar na rampa.
    internal sealed class Tela
    {
        public readonly int largura, altura;
        readonly Color32[][] rampa;
        readonly int[] tom;
        readonly Color32[] direta; // cores fora das rampas (flores); usadas quando rampa == null e alfa > 0

        public Tela(int largura, int altura)
        {
            this.largura = largura;
            this.altura = altura;
            rampa = new Color32[largura * altura][];
            tom = new int[largura * altura];
            direta = new Color32[largura * altura];
        }

        bool Dentro(int x, int y) => x >= 0 && y >= 0 && x < largura && y < altura;

        public void Pintar(int x, int y, Color32[] r, int t)
        {
            if (!Dentro(x, y)) return;
            int i = y * largura + x;
            rampa[i] = r;
            tom[i] = Mathf.Clamp(t, 0, r.Length - 1);
            direta[i] = FabricaDeSprites.Transparente;
        }

        public void Cor(int x, int y, Color32 c)
        {
            if (!Dentro(x, y)) return;
            int i = y * largura + x;
            rampa[i] = null;
            direta[i] = c;
        }

        public void Apagar(int x, int y)
        {
            if (!Dentro(x, y)) return;
            int i = y * largura + x;
            rampa[i] = null;
            direta[i] = FabricaDeSprites.Transparente;
        }

        // Anda "delta" tons na rampa do pixel (negativo = mais escuro).
        public void Mudar(int x, int y, int delta)
        {
            if (!Dentro(x, y)) return;
            int i = y * largura + x;
            if (rampa[i] != null) tom[i] = Mathf.Clamp(tom[i] + delta, 0, rampa[i].Length - 1);
        }

        public bool Cheio(int x, int y) => Dentro(x, y) && (rampa[y * largura + x] != null || direta[y * largura + x].a > 0);
        public Color32[] RampaEm(int x, int y) => Dentro(x, y) ? rampa[y * largura + x] : null;
        public int TomEm(int x, int y) => Dentro(x, y) ? tom[y * largura + x] : 0;

        public Color32 CorFinal(int x, int y)
        {
            int i = y * largura + x;
            return rampa[i] != null ? rampa[i][tom[i]] : direta[i];
        }

        public Sprite Criar(Vector2 pivo) => FabricaDeSprites.Procedural(largura, altura, pivo, CorFinal);
    }

    // ---------------------------------------------------------------- quem pede os desenhos

    // ---------------------------------------------------------------- o "atlas" do chão

    // Todas as combinações de bloco de chão (3 tipos x 4 versões x 8 jeitos de borda = 96) são pintadas
    // numa imagem SÓ (um "atlas", como uma folha de figurinhas), e cada bloco é um recorte dela.
    // Por quê? A placa de vídeo desenha de uma vez só ("batching") os sprites que usam a MESMA imagem.
    // Com 96 imagens separadas, uma fase com 300 blocos viraria centenas de desenhos; com o atlas, poucos.
    // Cada recorte tem 1 pixel de folga em volta, repetindo a beirada (para nunca "vazar" o vizinho).
    static readonly string[] Tipos = { "topo", "terra", "pedra" };
    const int ColunasDoAtlas = 12, CelulaLargura = 18, CelulaAltura = 21; // 16 x 19 + a folga
    static Texture2D atlas;

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => atlas = null;

    // Qual célula do atlas guarda essa combinação.
    static int Celula(int tipo, int versao, int bordas) => (tipo * Versoes + versao) * 8 + bordas;

    static Tela Desenhar(int tipo, int versao, int bordas)
    {
        bool e = (bordas & 1) != 0, d = (bordas & 2) != 0, b = (bordas & 4) != 0;
        if (tipo == 0) return ChaoComGrama(versao, e, d, b, versao == 3);
        if (tipo == 1) return ChaoDeTerra(versao, e, d, b);
        return ChaoDePedra(versao, e, d, b);
    }

    static Texture2D Atlas()
    {
        if (atlas != null) return atlas;
        int total = Tipos.Length * Versoes * 8;
        int linhas = (total + ColunasDoAtlas - 1) / ColunasDoAtlas;
        int largura = ColunasDoAtlas * CelulaLargura, altura = linhas * CelulaAltura;
        var pixels = new Color32[largura * altura];
        for (int tipo = 0; tipo < Tipos.Length; tipo++)
            for (int versao = 0; versao < Versoes; versao++)
                for (int bordas = 0; bordas < 8; bordas++)
                {
                    Tela t = Desenhar(tipo, versao, bordas);
                    int c = Celula(tipo, versao, bordas);
                    int x0 = (c % ColunasDoAtlas) * CelulaLargura, y0 = (c / ColunasDoAtlas) * CelulaAltura;
                    // o desenho vai de (1, 1) até (16, altura) dentro da célula; a folga copia a beirada mais perto
                    for (int y = 0; y < t.altura + 2; y++)
                        for (int x = 0; x < CelulaLargura; x++)
                        {
                            int sx = Mathf.Clamp(x - 1, 0, t.largura - 1), sy = Mathf.Clamp(y - 1, 0, t.altura - 1);
                            pixels[(y0 + y) * largura + x0 + x] = t.CorFinal(sx, sy);
                        }
                }
        atlas = new Texture2D(largura, altura, TextureFormat.RGBA32, false)
        {
            name = "Atlas do chao",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        atlas.SetPixels32(pixels);
        atlas.Apply();
        return atlas;
    }

    // Chamado pela FabricaDeSprites para nomes que ela não conhece. Devolve false se não for um bloco daqui.
    // Ex.: "chao_topo_2_eb" = grama em cima, versão 2, aberto à esquerda e embaixo.
    public static bool TentarCriar(string nome, out Sprite sprite)
    {
        sprite = null;
        if (!nome.StartsWith("chao_")) return false;
        string[] partes = nome.Split('_'); // "chao", tipo, versão, bordas (opcional)
        int tipo = partes.Length >= 3 ? System.Array.IndexOf(Tipos, partes[1]) : -1;
        if (tipo < 0 || !int.TryParse(partes[2], out int versao)) return false;
        string letras = partes.Length > 3 ? partes[3] : "";
        int bordas = (letras.Contains("e") ? 1 : 0) | (letras.Contains("d") ? 2 : 0) | (letras.Contains("b") ? 4 : 0);
        int c = Celula(tipo, Volta(versao, Versoes), bordas);
        int altura = tipo == 0 ? 19 : 16;
        var recorte = new Rect((c % ColunasDoAtlas) * CelulaLargura + 1, (c / ColunasDoAtlas) * CelulaAltura + 1, 16, altura);
        sprite = Sprite.Create(Atlas(), recorte, tipo == 0 ? PivoDaGrama : FabricaDeSprites.Centro,
            FabricaDeSprites.PixelsPorUnidade, 0, SpriteMeshType.FullRect);
        sprite.name = nome;
        return true;
    }

    // O nome do bloco de chão (o ConstrutorDeFase usa). profundidade = quantos blocos de chão tem em cima dele.
    public static string NomeDoChao(int profundidade, int versao, bool esquerda, bool direita, bool baixo)
    {
        string tipo = profundidade <= 0 ? "topo" : profundidade == 1 ? "terra" : "pedra";
        string bordas = (esquerda ? "e" : "") + (direita ? "d" : "") + (baixo ? "b" : "");
        return "chao_" + tipo + "_" + Volta(versao, Versoes) + (bordas.Length > 0 ? "_" + bordas : "");
    }

    // Pivô do bloco com grama: o meio da parte de 16 x 16 (as 3 linhas de cima são as pontas do capim).
    public static readonly Vector2 PivoDaGrama = new Vector2(0.5f, 8f / 19f);

    // ---------------------------------------------------------------- terra

    // Terra: manchas suaves (ruído periódico) + grãozinhos claros e escuros.
    static void PintarTerra(Tela t, int yDe, int yAte, float claridade)
    {
        for (int y = yDe; y <= yAte; y++)
            for (int x = 0; x < 16; x++)
            {
                float v = claridade + (RuidoPeriodico(x, y, 4, 11) - 0.5f) * 0.24f + (RuidoPeriodico(x, y, 2, 23) - 0.5f) * 0.12f;
                int tom = Tom(v, Terra.Length, x, y);
                float grao = Sorteio(x, y, 5);
                if (grao > 0.94f) tom++;
                else if (grao < 0.05f) tom--;
                t.Pintar(x, y, Terra, tom);
            }
    }

    // Pedrinha enterrada: uma elipse com luz de cima-esquerda e uma sombrinha de contato embaixo-direita.
    static void Pedrinha(Tela t, float cx, float cy, float rx, float ry, int ajuste = 0)
    {
        for (int y = Mathf.FloorToInt(cy - ry - 1); y <= Mathf.CeilToInt(cy + ry + 1); y++)
            for (int x = Mathf.FloorToInt(cx - rx - 1); x <= Mathf.CeilToInt(cx + rx + 1); x++)
            {
                float nx = (x + 0.5f - cx) / rx, ny = (y + 0.5f - cy) / ry;
                float d = nx * nx + ny * ny;
                if (d <= 1f)
                {
                    float luz = 0.5f + 0.42f * (nx * LuzX + ny * LuzY) + 0.08f * (1f - d);
                    t.Pintar(x, y, Pedra, Tom(0.28f + 0.62f * luz, Pedra.Length, x, y, 0.5f) + ajuste);
                }
                else if (d <= 1.9f && nx * LuzX + ny * LuzY < -0.2f && t.RampaEm(x, y) == Terra)
                    t.Mudar(x, y, -2); // sombra de contato: a pedrinha "afunda" na terra
            }
    }

    // Raiz: um fio escuro e torto descendo, com um brilhinho de vez em quando no lado iluminado.
    static void Raiz(Tela t, int x0, int yTopo, int comprimento, float fase)
    {
        for (int k = 0; k < comprimento; k++)
        {
            int y = yTopo - k;
            int x = x0 + Mathf.RoundToInt(Mathf.Sin(k * 0.9f + fase) * 1.1f);
            if (t.RampaEm(x, y) != Terra) continue; // não passa por cima das pedrinhas
            t.Pintar(x, y, Terra, 1);
            if (k % 3 == 1 && t.RampaEm(x - 1, y) == Terra) t.Pintar(x - 1, y, Terra, 4);
            if (k == comprimento - 2) t.Pintar(x + 1, y - 1, Terra, 1); // um "galhinho" no fim
        }
    }

    // Pedrinhas e raízes da terra, diferentes em cada versão (mas sempre dentro do bloco).
    static void EnfeitarTerra(Tela t, int versao, int yDe, int yAte, bool raizes, int semente)
    {
        var sorteio = new System.Random(semente * 97 + versao * 13 + 7);
        int pedrinhas = 2 + sorteio.Next(2);
        for (int i = 0; i < pedrinhas; i++)
        {
            float rx = 1.2f + (float)sorteio.NextDouble() * 1.4f;
            float ry = 0.9f + (float)sorteio.NextDouble() * 0.8f;
            float cx = rx + 1.5f + (float)sorteio.NextDouble() * (13f - 2f * rx);
            float cy = yDe + ry + 1f + (float)sorteio.NextDouble() * Mathf.Max(0f, yAte - yDe - 2f * ry - 2f);
            Pedrinha(t, cx, cy, rx, ry);
        }
        // uma pedra maior em algumas versões
        if (versao == 2 && yAte - yDe >= 8) Pedrinha(t, 10.5f, yDe + 3.5f, 3.2f, 2.2f, -1);
        if (!raizes) return;
        int quantas = 1 + sorteio.Next(2);
        for (int i = 0; i < quantas; i++)
            Raiz(t, 2 + sorteio.Next(12), yAte, 3 + sorteio.Next(4), (float)sorteio.NextDouble() * 6f);
    }

    // ---------------------------------------------------------------- bordas abertas (penhasco)

    // Lado sem vizinho: contorno no tom mais escuro do material e uma faixa iluminada (esquerda) ou
    // na sombra (direita, embaixo).
    static void Bordas(Tela t, bool esquerda, bool direita, bool baixo, int yAte)
    {
        for (int y = 0; y <= yAte; y++)
        {
            if (esquerda)
            {
                Contorno(t, 0, y);
                t.Mudar(1, y, +1);
            }
            if (direita)
            {
                Contorno(t, 15, y);
                t.Mudar(14, y, -1);
                if (Sorteio(13, y, 3) < 0.5f) t.Mudar(13, y, -1);
            }
        }
        if (baixo)
            for (int x = 0; x < 16; x++)
            {
                Contorno(t, x, 0);
                t.Mudar(x, 1, -2);
                t.Mudar(x, 2, -1);
                if (Sorteio(x, 3, 9) < 0.4f) t.Mudar(x, 3, -1);
            }
        // cantos de baixo arredondados
        if (baixo && esquerda) t.Apagar(0, 0);
        if (baixo && direita) t.Apagar(15, 0);
    }

    static void Contorno(Tela t, int x, int y)
    {
        Color32[] r = t.RampaEm(x, y);
        if (r != null) t.Pintar(x, y, r, 0);
    }

    // ---------------------------------------------------------------- chão com grama (topo)

    static Tela ChaoComGrama(int versao, bool esquerda, bool direita, bool baixo, bool flores)
    {
        var t = new Tela(16, 19);
        int semente = 100 + versao;

        // 1) terra embaixo (com raízes e pedrinhas)
        PintarTerra(t, 0, 8, 0.47f);
        EnfeitarTerra(t, versao, 0, 6, true, 1);

        // 2) a "aba" de grama: linhas 9 a 15, mais clara em cima (o sol bate) e escura embaixo
        for (int y = 9; y <= 15; y++)
            for (int x = 0; x < 16; x++)
            {
                float v = 0.3f + (y - 9) / 6f * 0.5f + (RuidoPeriodico(x, y, 4, 31) - 0.5f) * 0.2f;
                int tom = Tom(v, Capim.Length, x, y);
                if (y == 15 && Sorteio(x, y, semente + 3) < 0.12f) tom = 6; // pontinhos de sol no topo
                t.Pintar(x, y, Capim, tom);
            }
        // folhinhas desenhadas dentro da massa: riscos curtos escuros e claros (o capim "tem textura")
        for (int x = 0; x < 16; x++)
        {
            float s = Sorteio(x, 1, semente);
            int y0 = 10 + (int)(Sorteio(x, 2, semente) * 5f);
            if (s < 0.35f) { t.Mudar(x, y0, -1); t.Mudar(x, y0 - 1, -1); }
            else if (s > 0.75f) { t.Mudar(x, y0, +1); t.Mudar(x + (s > 0.88f ? 1 : 0), y0 + 1, +1); }
        }

        // 3) a grama "escorre" por cima da terra (comprimento diferente em cada coluna) e faz sombra embaixo
        for (int x = 0; x < 16; x++)
        {
            float onda = RuidoPeriodico(x, 0, 4, 41 + versao);
            int escorre = Mathf.Clamp(Mathf.RoundToInt(onda * 3.2f + (Sorteio(x, 5, semente) - 0.5f) * 1.6f), 0, 3);
            for (int k = 1; k <= escorre; k++)
                t.Pintar(x, 9 - k, Capim, k == escorre ? 1 : 2);
            int sob = 8 - escorre; // a terra logo embaixo da grama fica na sombra (oclusão ambiente)
            t.Mudar(x, sob, -2);
            t.Mudar(x, sob - 1, -1);
        }

        // 4) pontas do capim acima do bloco (linhas 16 a 18): poucas e de alturas diferentes
        for (int x = 0; x < 16; x++)
        {
            if ((esquerda && x <= 1) || (direita && x >= 14)) continue;
            float s = Sorteio(x, 9, semente);
            int altura = s < 0.42f ? 0 : s < 0.7f ? 1 : s < 0.9f ? 2 : 3;
            int inclina = Sorteio(x, 10, semente) < 0.5f ? -1 : 1;
            for (int k = 1; k <= altura; k++)
            {
                int xx = x + (altura == 3 && k == 3 ? inclina : 0);
                bool ponta = k == altura;
                t.Pintar(xx, 15 + k, Capim, ponta ? (Sorteio(x, 11, semente) < 0.3f ? 6 : 5) : 4);
            }
        }

        // 5) florzinhas (na versão "florida")
        if (flores)
        {
            Flor(t, 4, PetalaRosa);
            Flor(t, 11, versao % 2 == 0 ? PetalaAmarela : PetalaBranca);
        }

        // 6) beiradas abertas: a grama faz a curva e escorre pelo lado
        if (esquerda)
        {
            for (int y = 5; y <= 15; y++)
            {
                t.Pintar(0, y, Capim, y >= 14 ? 2 : 1);
                if (y >= 7) t.Pintar(1, y, Capim, y >= 13 ? 5 : 3);
            }
            t.Apagar(0, 15);
            t.Pintar(1, 15, Capim, 3);
        }
        if (direita)
        {
            for (int y = 6; y <= 15; y++)
            {
                t.Pintar(15, y, Capim, y >= 14 ? 1 : 0);
                if (y >= 8) t.Pintar(14, y, Capim, y >= 14 ? 3 : 2);
            }
            t.Apagar(15, 15);
        }
        Bordas(t, esquerda, direita, baixo, 4);
        if (esquerda) t.Pintar(0, 5, Capim, 0);
        return t;
    }

    static void Flor(Tela t, int x, Color32 petala)
    {
        t.Pintar(x, 16, Capim, 3); // caule
        t.Pintar(x, 17, Capim, 2);
        t.Cor(x, 18, Miolo);
        t.Cor(x - 1, 18, petala);
        t.Cor(x + 1, 18, petala);
        t.Cor(x, 17, petala);
        t.Pintar(x + 1, 17, Capim, 4); // folhinha
    }

    // ---------------------------------------------------------------- terra (logo abaixo da grama)

    // Metade de cima é terra (com raízes e pedrinhas), e embaixo a rocha começa a aparecer: as "camadas"
    // de um barranco de verdade. A divisa entre as duas é ondulada.
    static Tela ChaoDeTerra(int versao, bool esquerda, bool direita, bool baixo)
    {
        var t = new Tela(16, 16);
        PintarPedra(t, versao, false);
        var terra = new Tela(16, 16);
        PintarTerra(terra, 0, 15, 0.44f);
        EnfeitarTerra(terra, versao, 8, 15, true, 2);
        for (int x = 0; x < 16; x++)
        {
            int divisa = 6 + Mathf.RoundToInt(RuidoPeriodico(x, 0, 4, 51) * 4f); // de 6 a 10
            for (int y = divisa; y < 16; y++)
                t.Pintar(x, y, terra.RampaEm(x, y), terra.TomEm(x, y));
            t.Mudar(x, divisa, -1); // a terra faz uma sombrinha em cima das pedras
            t.Pintar(x, divisa - 1, Terra, 1);
            t.Mudar(x, divisa - 2, -1);
        }
        Bordas(t, esquerda, direita, baixo, 15);
        return t;
    }

    // ---------------------------------------------------------------- pedra (mais fundo)

    // Pedras irregulares (um "diagrama de Voronoi": cada pixel pertence à semente mais perto), com
    // rejunte de terra escura entre elas. As sementes se repetem a cada 16 pixels: blocos vizinhos encaixam.
    static readonly Vector2[] SementesDaPedra =
    {
        new Vector2(3f, 2.5f), new Vector2(11f, 2f), new Vector2(7f, 8f), new Vector2(14.5f, 8.5f),
        new Vector2(2f, 12f), new Vector2(10f, 13.5f),
    };

    static int CelulaDaPedra(int x, int y, out float folga)
    {
        float melhor = 999f, segunda = 999f;
        int indice = 0;
        for (int i = 0; i < SementesDaPedra.Length; i++)
        {
            float dx = Mathf.Abs(x + 0.5f - SementesDaPedra[i].x), dy = Mathf.Abs(y + 0.5f - SementesDaPedra[i].y);
            dx = Mathf.Min(dx, 16f - dx) * 0.8f; // dá a volta (periódico) e "achata": pedras mais largas que altas
            dy = Mathf.Min(dy, 16f - dy);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d < melhor) { segunda = melhor; melhor = d; indice = i; }
            else if (d < segunda) segunda = d;
        }
        folga = segunda - melhor; // perto de 0 = na divisa entre duas pedras
        return indice;
    }

    static Tela ChaoDePedra(int versao, bool esquerda, bool direita, bool baixo)
    {
        var t = new Tela(16, 16);
        PintarPedra(t, versao, true);
        Bordas(t, esquerda, direita, baixo, 15);
        return t;
    }

    static void PintarPedra(Tela t, int versao, bool musgo)
    {
        var celula = new int[16 * 16];
        var sorteio = new System.Random(300 + versao * 17);
        var tomDaPedra = new float[SementesDaPedra.Length];
        for (int i = 0; i < tomDaPedra.Length; i++) tomDaPedra[i] = 0.42f + (float)sorteio.NextDouble() * 0.16f;

        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int c = CelulaDaPedra(x, y, out float folga);
                bool rejunte = folga < 1.1f;
                celula[y * 16 + x] = rejunte ? -1 : c;
                if (rejunte) t.Pintar(x, y, Terra, folga < 0.5f ? 0 : 1);
                else
                {
                    float v = tomDaPedra[c] + (RuidoPeriodico(x, y, 2, 71) - 0.5f) * 0.14f;
                    t.Pintar(x, y, Pedra, Tom(v, Pedra.Length, x, y));
                }
            }

        // volume de cada pedra: beirada de cima-esquerda clara, de baixo-direita escura
        int CelulaEm(int x, int y) => celula[Volta(y, 16) * 16 + Volta(x, 16)];
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int c = celula[y * 16 + x];
                if (c < 0) continue;
                bool luz = CelulaEm(x - 1, y) != c || CelulaEm(x, y + 1) != c;
                bool sombra = CelulaEm(x + 1, y) != c || CelulaEm(x, y - 1) != c;
                if (luz && !sombra) t.Mudar(x, y, +2);
                else if (sombra && !luz) t.Mudar(x, y, -1);
                if (CelulaEm(x + 1, y - 1) != c && CelulaEm(x, y - 1) != c && CelulaEm(x + 1, y) != c) t.Mudar(x, y, -1);
            }

        // rachaduras: um fio escuro torto dentro de uma ou duas pedras
        int rachaduras = 1 + versao % 2;
        for (int r = 0; r < rachaduras; r++)
        {
            int x = 3 + sorteio.Next(10), y = 3 + sorteio.Next(10);
            int c = celula[y * 16 + x];
            for (int k = 0; k < 5 && c >= 0; k++)
            {
                if (celula[y * 16 + x] != c) break;
                t.Pintar(x, y, Pedra, 0);
                if (t.RampaEm(x + 1, y + 1) == Pedra && celula[Mathf.Min(15, y + 1) * 16 + Mathf.Min(15, x + 1)] == c) t.Mudar(x + 1, y + 1, +1);
                x += sorteio.Next(3) - 1;
                y -= 1;
                if (x < 1 || x > 14 || y < 1) break;
            }
        }

        // musgo: no topo de algumas pedras (versões 1 e 3), pontinhos verdes onde bate luz
        if (musgo && versao % 2 == 1)
            for (int y = 1; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    int c = celula[y * 16 + x];
                    if (c < 0 || c % 2 != versao / 2 || CelulaEm(x, y + 1) == c) continue;
                    t.Pintar(x, y, Capim, Sorteio(x, y, 81) < 0.5f ? 3 : 2);
                    if (Sorteio(x, y, 82) < 0.6f && celula[(y - 1) * 16 + x] == c) t.Pintar(x, y - 1, Capim, 1);
                }
    }

    // ---------------------------------------------------------------- caixote ('B')

    public static Sprite Caixote()
    {
        var t = new Tela(16, 16);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                if (x == 0 || y == 0 || x == 15 || y == 15) { t.Pintar(x, y, Madeira, 0); continue; }
                bool moldura = x <= 2 || x >= 13 || y <= 2 || y >= 13;
                if (moldura)
                {
                    // tábuas da moldura: em cima/esquerda iluminadas, embaixo/direita na sombra
                    int tom = y >= 13 ? (y == 14 ? 5 : 4) : y <= 2 ? (y == 1 ? 1 : 2) : x <= 2 ? (x == 1 ? 4 : 3) : (x == 14 ? 1 : 2);
                    if (Sorteio(x, y, 7) < 0.15f) tom--;
                    t.Pintar(x, y, Madeira, tom);
                    continue;
                }
                // tábuas de dentro (deitadas), com veios
                bool fresta = y == 6 || y == 10;
                if (fresta) { t.Pintar(x, y, Madeira, 0); continue; }
                int baseDaTabua = y == 9 || y == 5 || y == 12 ? 3 : y == 7 || y == 3 || y == 11 ? 2 : 3;
                float veio = RuidoPeriodico(x * 2, y * 8, 4, 91 + y);
                int t2 = baseDaTabua + (veio > 0.68f ? 1 : veio < 0.3f ? -1 : 0);
                t.Pintar(x, y, Madeira, t2);
            }
        // oclusão nas frestas (embaixo de cada tábua fica mais escuro) e sombra da moldura sobre o miolo
        for (int x = 3; x <= 12; x++) { t.Mudar(x, 12, -1); t.Mudar(x, 7, -1); }
        for (int y = 3; y <= 12; y++) t.Mudar(12, y, -1);
        // travessa na diagonal (de baixo-esquerda para cima-direita)
        for (int y = 3; y <= 12; y++)
            for (int x = 3; x <= 12; x++)
            {
                int d = x - y;
                if (d == -1) t.Pintar(x, y, Madeira, 5);
                else if (d == 0) t.Pintar(x, y, Madeira, 4);
                else if (d == 1) t.Pintar(x, y, Madeira, 3);
                else if (d == 2) t.Mudar(x, y, -2); // sombra da travessa
            }
        // cantoneiras de ferro com rebite
        Cantoneira(t, 1, 1, 1, 1);
        Cantoneira(t, 14, 1, -1, 1);
        Cantoneira(t, 1, 14, 1, -1);
        Cantoneira(t, 14, 14, -1, -1);
        return t.Criar(FabricaDeSprites.Centro);
    }

    // Um "L" de ferro no canto (x0, y0), virado para dentro do caixote (dx, dy).
    static void Cantoneira(Tela t, int x0, int y0, int dx, int dy)
    {
        for (int k = 0; k < 3; k++)
        {
            t.Pintar(x0 + dx * k, y0, Ferro, 2);
            t.Pintar(x0, y0 + dy * k, Ferro, 2);
        }
        t.Pintar(x0, y0, Ferro, dy > 0 ? 1 : 3);
        t.Pintar(x0 + dx, y0 + dy, Ferro, 4); // rebite (brilho)
        t.Pintar(x0 + dx * 2, y0 + dy, Ferro, 1);
    }

    // ---------------------------------------------------------------- bloco "?" (mágico) e bloco usado

    static readonly string[] Interrogacao = // 6 x 9, de cima para baixo
    {
        ".####.",
        "##..##",
        "....##",
        "...##.",
        "..##..",
        "..##..",
        "......",
        "..##..",
        "..##..",
    };

    static bool NaInterrogacao(int x, int y)
    {
        int gx = x - 5, gy = 11 - y; // canto de cima-esquerda do símbolo em (5, 11)
        if (gx < 0 || gy < 0 || gx >= 6 || gy >= Interrogacao.Length) return false;
        return Interrogacao[gy][gx] == '#';
    }

    // Moldura chanfrada (bisel): luz em cima-esquerda, sombra em baixo-direita, cantos arredondados.
    static void Bisel(Tela t, Color32[] r, int contorno, int luz1, int luz2, int sombra1, int sombra2)
    {
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                if ((x == 0 || x == 15) && (y == 0 || y == 15)) { t.Apagar(x, y); continue; }
                if (x == 0 || y == 0 || x == 15 || y == 15) { t.Pintar(x, y, r, contorno); continue; }
                bool cimaEsq = x == 1 || y == 14, baixoDir = x == 14 || y == 1;
                if (cimaEsq && baixoDir) { t.Pintar(x, y, r, x + (15 - y) < 15 ? luz2 : sombra2); continue; }
                if (cimaEsq) { t.Pintar(x, y, r, luz1); continue; }
                if (baixoDir) { t.Pintar(x, y, r, sombra1); continue; }
                bool cimaEsq2 = x == 2 || y == 13, baixoDir2 = x == 13 || y == 2;
                if (cimaEsq2 && !baixoDir2) t.Pintar(x, y, r, luz2);
                else if (baixoDir2) t.Pintar(x, y, r, sombra2);
            }
    }

    // brilho: posição da faixa diagonal clara (NaN = sem brilho), como os quadros do bloco antigo.
    public static Sprite BlocoSurpresa(float brilho)
    {
        var t = new Tela(16, 16);
        // miolo com degradê (mais claro em cima) e uns brilhinhos mágicos
        for (int y = 3; y <= 12; y++)
            for (int x = 3; x <= 12; x++)
            {
                float v = 0.38f + (y - 3) / 9f * 0.22f + (RuidoPeriodico(x, y, 4, 121) - 0.5f) * 0.12f;
                t.Pintar(x, y, Magia, Tom(v, Magia.Length, x, y));
            }
        Bisel(t, Magia, 0, 5, 4, 1, 2);
        t.Pintar(4, 11, Magia, 6);
        t.Pintar(11, 5, Magia, 5);
        t.Pintar(10, 11, Magia, 5);

        // sombra do "?" (1 pixel para a direita e para baixo) e o contorno dele
        for (int y = 2; y <= 13; y++)
            for (int x = 2; x <= 13; x++)
                if (!NaInterrogacao(x, y) && NaInterrogacao(x - 1, y + 1)) t.Pintar(x, y, Magia, 1);
        for (int y = 2; y <= 13; y++)
            for (int x = 2; x <= 13; x++)
            {
                if (!NaInterrogacao(x, y)) continue;
                bool bordaCimaEsq = !NaInterrogacao(x - 1, y) || !NaInterrogacao(x, y + 1);
                bool bordaBaixoDir = !NaInterrogacao(x + 1, y) || !NaInterrogacao(x, y - 1);
                int tom = bordaCimaEsq && !bordaBaixoDir ? 4 : bordaBaixoDir && !bordaCimaEsq ? 2 : 3;
                t.Pintar(x, y, Ouro, tom);
            }
        // rebites dourados nos cantos do miolo
        t.Pintar(3, 12, Ouro, 4); t.Pintar(12, 12, Ouro, 3); t.Pintar(3, 3, Ouro, 2); t.Pintar(12, 3, Ouro, 1);

        if (!float.IsNaN(brilho))
            for (int y = 1; y < 15; y++)
                for (int x = 1; x < 15; x++)
                    if (Mathf.Abs(x + y - brilho) <= 1.5f) t.Mudar(x, y, +2);
        return t.Criar(FabricaDeSprites.Centro);
    }

    public static Sprite BlocoUsado()
    {
        var t = new Tela(16, 16);
        for (int y = 3; y <= 12; y++)
            for (int x = 3; x <= 12; x++)
            {
                float v = 0.34f + (RuidoPeriodico(x, y, 4, 131) - 0.5f) * 0.16f + (y - 3) / 9f * 0.08f;
                t.Pintar(x, y, Pedra, Tom(v, Pedra.Length, x, y));
            }
        Bisel(t, Pedra, 0, 4, 3, 1, 1);
        // rebites 2 x 2 nos cantos: escuros, com um pontinho de luz em cima-esquerda
        foreach (int cx in new[] { 3, 11 })
            foreach (int cy in new[] { 4, 12 })
            {
                t.Pintar(cx, cy, Pedra, 5);
                t.Pintar(cx + 1, cy, Pedra, 1);
                t.Pintar(cx, cy - 1, Pedra, 1);
                t.Pintar(cx + 1, cy - 1, Pedra, 0);
            }
        // rachadura
        t.Pintar(8, 10, Pedra, 0); t.Pintar(8, 9, Pedra, 0); t.Pintar(9, 8, Pedra, 0); t.Pintar(9, 7, Pedra, 0); t.Pintar(10, 6, Pedra, 1);
        t.Pintar(9, 10, Pedra, 4); t.Pintar(10, 8, Pedra, 4);
        return t.Criar(FabricaDeSprites.Centro);
    }

    // ---------------------------------------------------------------- espinho

    // Dois espinhos de aço lado a lado (o mesmo formato de antes), com a face da esquerda iluminada,
    // a da direita na sombra, um fio de luz na aresta e uma base de ferro rebitada.
    public static Sprite Espinho()
    {
        var t = new Tela(16, 16);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int indice = x / 8;
                float centro = indice * 8 + 3.5f;
                float distancia = Mathf.Abs(x - centro);
                float meiaLargura = 4f * (1f - y / 14f);
                if (y > 14 || distancia > meiaLargura) continue;
                bool borda = meiaLargura - distancia < 1.1f;
                if (borda) { t.Pintar(x, y, Aco, 0); continue; }
                bool esquerda = x < centro;
                int tom = esquerda ? (distancia < 1f ? 5 : 4) : (distancia < 1f ? 2 : 1);
                if (y > 9 && esquerda) tom = 5; // ponta bem iluminada
                if (y < 4) tom--;               // perto da base, a luz bate menos
                t.Pintar(x, y, Aco, tom);
            }
        // base de ferro
        for (int x = 0; x < 16; x++)
        {
            t.Pintar(x, 0, Aco, 0);
            t.Pintar(x, 1, Aco, x == 0 || x == 15 ? 0 : 2);
            if (x > 0 && x < 15 && Sorteio(x, 1, 141) < 0.3f) t.Mudar(x, 1, -1);
        }
        t.Pintar(1, 1, Aco, 4); t.Pintar(8, 1, Aco, 4); t.Pintar(14, 1, Aco, 3);
        return t.Criar(FabricaDeSprites.Centro);
    }

    // ---------------------------------------------------------------- cano

    // O mesmo formato do cano antigo (32 de largura; a "boca" do topo é mais larga), sombreado como um
    // cilindro: faixa de brilho perto da esquerda, sombra crescendo para a direita.
    public static Sprite CanoDesenho(bool topo)
    {
        var t = new Tela(32, 16);
        for (int y = 0; y < 16; y++)
        {
            bool boca = topo && y >= 5;
            int x0 = boca ? 0 : 2, x1 = boca ? 31 : 29;
            for (int x = x0; x <= x1; x++)
            {
                if (x == x0 || x == x1 || (boca && (y == 15 || y == 5))) { t.Pintar(x, y, Cano, 0); continue; }
                float u = (x - x0) / (float)(x1 - x0);
                float luz = Mathf.Exp(-(u - 0.24f) * (u - 0.24f) / 0.012f);   // faixa de brilho
                float v = 0.62f - 0.55f * Mathf.Pow(Mathf.Max(0f, u - 0.3f) / 0.7f, 1.2f) + 0.36f * luz - (u < 0.08f ? 0.15f : 0f);
                int tom = Tom(v, Cano.Length, x, y, 0.5f);
                if (boca && y == 14) tom++;      // a beirada de cima da boca pega luz
                if (boca && y == 6) tom -= 2;    // embaixo da boca: sombra
                t.Pintar(x, y, Cano, tom);
            }
        }
        // no corpo, logo embaixo da boca, a sombra que a boca faz
        if (topo)
            for (int x = 3; x < 29; x++) { t.Mudar(x, 4, -2); t.Mudar(x, 3, -1); }
        else
            for (int x = 3; x < 29; x++)
                if (Sorteio(x, 0, 151) < 0.25f) t.Mudar(x, 0, -1); // sujeirinha
        return t.Criar(FabricaDeSprites.Centro);
    }

    // ---------------------------------------------------------------- enfeites do chão

    // Arbusto (24 x 12): três "bolas" de folhas, cada uma iluminada como uma esfera (luz de cima-esquerda),
    // com folhinhas soltas (pixels um tom acima/abaixo) e contorno verde bem escuro.
    static readonly Vector3[] BolasDoArbusto = { new Vector3(6f, 4f, 5f), new Vector3(12f, 5.5f, 6f), new Vector3(18f, 4f, 5f) };

    static bool NoArbusto(int x, int y)
    {
        if (y < 0) return false;
        foreach (Vector3 b in BolasDoArbusto)
            if ((x + 0.5f - b.x) * (x + 0.5f - b.x) + (y + 0.5f - b.y) * (y + 0.5f - b.y) <= b.z * b.z) return true;
        return false;
    }

    public static Sprite Arbusto()
    {
        var t = new Tela(24, 12);
        for (int y = 0; y < 12; y++)
            for (int x = 0; x < 24; x++)
            {
                if (!NoArbusto(x, y)) continue;
                // a bola "mais na frente" neste pixel é a que tem mais folga até a própria borda
                float melhor = -99f, luz = 0f;
                foreach (Vector3 b in BolasDoArbusto)
                {
                    float nx = (x + 0.5f - b.x) / b.z, ny = (y + 0.5f - b.y) / b.z;
                    float folga = 1f - Mathf.Sqrt(nx * nx + ny * ny);
                    if (folga > melhor) { melhor = folga; luz = 0.5f + 0.4f * (nx * LuzX + ny * LuzY); }
                }
                float v = 0.12f + 0.75f * luz + (Sorteio(x, y, 171) - 0.5f) * 0.22f;
                if (y < 2) v -= 0.15f; // embaixo, encostado no chão: oclusão
                t.Pintar(x, y, Capim, Tom(v, Capim.Length, x, y));
            }
        for (int y = 0; y < 12; y++)
            for (int x = 0; x < 24; x++)
            {
                if (!NoArbusto(x, y)) continue;
                bool borda = !NoArbusto(x + 1, y) || !NoArbusto(x - 1, y) || !NoArbusto(x, y + 1);
                if (!borda) continue;
                bool iluminada = !NoArbusto(x - 1, y) && NoArbusto(x + 1, y) || !NoArbusto(x, y + 1) && x < 12;
                t.Pintar(x, y, Capim, iluminada ? 2 : 0);
            }
        return t.Criar(FabricaDeSprites.Base);
    }

    // Pedra de enfeite (12 x 8): meia elipse com luz de cima-esquerda e um pouco de musgo em cima.
    public static Sprite PedraDeEnfeite()
    {
        var t = new Tela(12, 8);
        bool Dentro(int a, int b) => b >= 0 && b < 8 && (a + 0.5f - 6f) * (a + 0.5f - 6f) / 30f + (b + 0.5f) * (b + 0.5f) / 49f <= 1f;
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 12; x++)
            {
                if (!Dentro(x, y)) continue;
                float nx = (x + 0.5f - 6f) / 5.5f, ny = (y + 0.5f) / 7f;
                float v = 0.25f + 0.55f * (0.5f + 0.5f * (nx * LuzX + ny * LuzY)) + (Sorteio(x, y, 181) - 0.5f) * 0.12f;
                if (y == 0) v -= 0.2f;
                t.Pintar(x, y, Pedra, Tom(v, Pedra.Length, x, y));
                bool borda = !Dentro(x + 1, y) || !Dentro(x - 1, y) || !Dentro(x, y + 1);
                if (borda) t.Pintar(x, y, Pedra, nx < 0f && ny > 0.3f ? 2 : 0);
                else if (!Dentro(x, y + 2) && x >= 3 && x <= 7 && Sorteio(x, y, 182) < 0.6f) t.Pintar(x, y, Capim, 3); // musgo
            }
        return t.Criar(FabricaDeSprites.Base);
    }

    // ---------------------------------------------------------------- para desenhos em texto (plataformas)

    // Igual ao FabricaDeSprites.DeArte, mas troca as cores "chapadas" de pedra, madeira e grama pelas
    // rampas daqui (com textura e luz de cima-esquerda) e o contorno preto por um azul-arroxeado bem escuro.
    public static Sprite DeArteRica(string[] linhas, Vector2 pivo)
    {
        int altura = linhas.Length, largura = 0;
        foreach (string linha in linhas) largura = Mathf.Max(largura, linha.Length);
        var t = new Tela(largura, altura);
        char Letra(int x, int y) => y < 0 || y >= altura || x < 0 || x >= linhas[altura - 1 - y].Length ? '.' : linhas[altura - 1 - y][x];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                char c = Letra(x, y);
                Color32[] r = null; int tom = 0;
                switch (c)
                {
                    case 'Z': r = Pedra; tom = 5; break;
                    case 'm': r = Pedra; tom = 4; break;
                    case 'M': r = Pedra; tom = 2; break;
                    case 'z': r = Pedra; tom = 1; break;
                    case 's': r = Pedra; tom = 4; break;
                    case 'S': r = Pedra; tom = 2; break;
                    case 'n': r = Madeira; tom = 5; break;
                    case 'b': r = Madeira; tom = 3; break;
                    case 'd': r = Madeira; tom = 1; break;
                    case 'a': r = Capim; tom = 5; break;
                    case 'g': r = Capim; tom = 4; break;
                    case 'G': r = Capim; tom = 2; break;
                    case 'q': r = Capim; tom = 1; break;
                    case 'k': t.Cor(x, y, C(24, 20, 36)); continue;
                }
                if (r == null)
                {
                    Color32 cor = FabricaDeSprites.CorOuTransparente(c);
                    if (cor.a > 0) t.Cor(x, y, cor);
                    continue;
                }
                // textura: alguns pixels um tom acima/abaixo
                float s = RuidoPeriodico(Volta(x, 16), Volta(y, 16), 2, 161 + c);
                if (s > 0.72f) tom++;
                else if (s < 0.26f) tom--;
                t.Pintar(x, y, r, tom);
            }
        // luz de cima-esquerda nas beiradas de cada material, sombra embaixo-direita
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                Color32[] r = t.RampaEm(x, y);
                if (r == null) continue;
                if (t.RampaEm(x, y + 1) != r || t.RampaEm(x - 1, y) != r) t.Mudar(x, y, +1);
                else if (t.RampaEm(x, y - 1) != r || t.RampaEm(x + 1, y) != r) t.Mudar(x, y, -1);
            }
        return t.Criar(pivo);
    }
}
