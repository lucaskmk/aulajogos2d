using UnityEngine;
using Tela = ArteDosBlocos.Tela;

// ARTE DO MAPA (parte 2): os enfeites que ficam em cima do chão. Palmeiras (e as palmeiras mortas das
// ilhas do Kraken), rochedos, as casinhas do porto com telhado de telhas e janela acesa, o castelo do porto,
// o forte da Capitã, o navio fantasma (as Cabines), naufrágios, baús de tesouro, flores, a Rocha da Caveira,
// os navios no mar, o farol e os bichinhos (gaivotas, borboletas, fumaça...).
//
// Tudo com as rampas e a luz de CIMA-ESQUERDA dos blocos: o lado esquerdo e o topo de cada coisa pegam sol,
// o lado direito e a parte de baixo ficam na sombra azul-arroxeada, e o contorno usa o tom mais escuro
// do próprio material. A SOMBRA no chão de cada enfeite é um desenho separado ("mapa_sombra_..."),
// desenhado embaixo de todos os enfeites: assim a sombra de uma árvore nunca cobre a árvore da frente.
public static partial class ArteDoMapa
{
    // ---------------------------------------------------------------- mais rampas

    static readonly Color32[] Magia = ArteDosBlocos.Magia, Ouro = ArteDosBlocos.Ouro;
    static readonly Color32[] Neve =
    {
        C(50, 56, 98), C(88, 104, 156), C(138, 160, 206), C(190, 208, 236), C(230, 238, 250), C(255, 255, 252),
    };
    static readonly Color32[] Reboco = // parede de casa (reboco claro)
    {
        C(54, 40, 56), C(102, 80, 86), C(156, 128, 116), C(204, 178, 146), C(234, 216, 180), C(250, 242, 218),
    };
    static readonly Color32[] PedraClara = // as muralhas brancas do castelo real
    {
        C(44, 46, 78), C(82, 86, 122), C(126, 130, 162), C(172, 174, 198), C(214, 214, 228), C(242, 240, 240), C(255, 252, 240),
    };
    static readonly Color32[] TelhaVermelha =
    {
        C(52, 18, 32), C(98, 30, 40), C(148, 48, 46), C(194, 78, 58), C(226, 120, 80), C(248, 170, 116),
    };
    static readonly Color32[] TelhaAzul =
    {
        C(20, 22, 48), C(34, 46, 88), C(52, 74, 130), C(80, 112, 168), C(122, 154, 200), C(176, 202, 230),
    };
    static readonly Color32[] TelhaMusgo =
    {
        C(26, 30, 30), C(52, 56, 42), C(84, 86, 50), C(120, 116, 62), C(160, 150, 82), C(204, 192, 120),
    };
    static readonly Color32[] PinhoSombrio = // as folhas secas das palmeiras mortas (ilhas do Kraken)
    {
        C(14, 12, 26), C(26, 22, 44), C(40, 34, 66), C(58, 50, 90), C(80, 72, 116), C(108, 102, 142),
    };
    static readonly Color32 ContornoEscuro = C(24, 18, 36), Vidro = C(30, 34, 70);

    const float LuzX = ArteDosBlocos.LuzX, LuzY = ArteDosBlocos.LuzY;

    // ---------------------------------------------------------------- registro

    // Onde fica a boca da chaminé de cada casa (em blocos, a partir do pé da casa): a fumaça sai daqui.
    public static readonly Vector3 Chamine = new Vector3(3.5f / P, 19.5f / P, 0f);
    // As pontas das torres (onde ficam as bandeirinhas), em blocos a partir do pé do desenho.
    public static readonly Vector3[] TorresDoCastelo = { new Vector3(0f, 63f / P, 0f), new Vector3(-17.5f / P, 55f / P, 0f), new Vector3(17.5f / P, 55f / P, 0f) };
    public static readonly Vector3[] TorresDoForte = { new Vector3(0f, 47f / P, 0f), new Vector3(-28.5f / P, 35f / P, 0f), new Vector3(27.5f / P, 35f / P, 0f) };
    public const int VersoesDaPalmeira = 3, VersoesDaMontanha = 3, VersoesDaCasa = 3, VersoesDasFlores = 4, QuadrosDaBandeira = 4;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        Vector2 baseDoDesenho = FabricaDeSprites.Base, centro = FabricaDeSprites.Centro;
        RegistrarCaminhos();
        FabricaDeSprites.Registrar("mapa_ponte", () => Ponte(false, false));
        FabricaDeSprites.Registrar("mapa_ponte_empe", () => Ponte(true, false));
        FabricaDeSprites.Registrar("mapa_ponte_corda", () => Ponte(false, true));
        FabricaDeSprites.Registrar("mapa_ponte_corda_empe", () => Ponte(true, true));
        for (int v = 0; v < VersoesDaPalmeira; v++) { int vv = v; FabricaDeSprites.Registrar("mapa_palmeira_" + v, () => Palmeira(vv)); }
        for (int v = 0; v < VersoesDaMontanha; v++) { int vv = v; FabricaDeSprites.Registrar("mapa_montanha_" + v, () => Montanha(vv)); }
        for (int v = 0; v < VersoesDaCasa; v++) { int vv = v; FabricaDeSprites.Registrar("mapa_casa_" + v, () => Casa(vv)); }
        for (int v = 0; v < VersoesDasFlores; v++) { int vv = v; FabricaDeSprites.Registrar("mapa_flores_" + v, () => Flores(vv)); }
        FabricaDeSprites.Registrar("mapa_palmeira_morta", PalmeiraMorta);
        FabricaDeSprites.Registrar("mapa_rocha_caveira", RochaDaCaveira);
        FabricaDeSprites.Registrar("mapa_castelo", Castelo);
        FabricaDeSprites.Registrar("mapa_forte", FortePirata);
        FabricaDeSprites.Registrar("mapa_navio_fantasma", NavioFantasma);
        FabricaDeSprites.Registrar("mapa_naufragio", Naufragio);
        FabricaDeSprites.Registrar("mapa_bau", Bau);
        FabricaDeSprites.Registrar("mapa_navio", Navio);
        FabricaDeSprites.Registrar("mapa_farol", Farol);
        FabricaDeSprites.Registrar("mapa_brilho", () => Brilho(16, new Color32(255, 255, 255, 255)));
        FabricaDeSprites.Registrar("mapa_sombra_p", () => SombraNoChao(12, 5));
        FabricaDeSprites.Registrar("mapa_sombra_m", () => SombraNoChao(18, 7));
        FabricaDeSprites.Registrar("mapa_sombra_g", () => SombraNoChao(26, 9));
        FabricaDeSprites.Registrar("mapa_sombra_castelo", () => SombraNoChao(84, 16));
        FabricaDeSprites.Registrar("mapa_sombra_rocha", () => SombraNoChao(48, 13));
        FabricaDeSprites.Registrar("mapa_fumaca", Fumaca);
        FabricaDeSprites.Registrar("mapa_passaro_0", () => Passaro(0));
        FabricaDeSprites.Registrar("mapa_passaro_1", () => Passaro(1));
        FabricaDeSprites.Registrar("mapa_borboleta_0", () => Borboleta(0));
        FabricaDeSprites.Registrar("mapa_borboleta_1", () => Borboleta(1));
        FabricaDeSprites.Registrar("mapa_folha_0", () => Folha(0));
        FabricaDeSprites.Registrar("mapa_folha_1", () => Folha(1));
        for (int q = 0; q < QuadrosDaBandeira; q++) { int qq = q; FabricaDeSprites.Registrar("mapa_bandeira_" + q, () => Bandeira(qq)); }
        FabricaDeSprites.Registrar("mapa_sombra_nuvem", SombraDeNuvem);
    }

    // ---------------------------------------------------------------- ajudantes

    // Pinta a borda de dentro de uma forma: escura nos lados da sombra (direita, embaixo), mais clara nos da luz.
    static void Contorno(Tela t, System.Func<int, int, bool> dentro, Color32[] r, int escuro, int claro)
    {
        for (int y = 0; y < t.altura; y++)
            for (int x = 0; x < t.largura; x++)
            {
                if (!dentro(x, y)) continue;
                bool dir = !dentro(x + 1, y), baixo = !dentro(x, y - 1), esq = !dentro(x - 1, y), cima = !dentro(x, y + 1);
                if (dir || baixo) t.Pintar(x, y, r, escuro);
                else if (esq || cima) t.Pintar(x, y, r, claro);
            }
    }

    // A luz numa esfera (bola de folhas, pedra redonda): clara em cima-esquerda.
    static float LuzDaBola(float nx, float ny) => 0.5f + 0.42f * (nx * LuzX + ny * LuzY) + 0.08f * (1f - nx * nx - ny * ny);

    // Copa de folhas feita de várias "bolas", cada uma iluminada como uma esfera, com tufos de folhas
    // (manchas pequenas um tom acima/abaixo) e pontinhos de sol do lado da luz.
    static void Copa(Tela t, Vector3[] bolas, Color32[] r, int semente, float claridade = 0f)
    {
        bool Dentro(int x, int y)
        {
            foreach (Vector3 b in bolas)
            {
                float dx = x + 0.5f - b.x, dy = y + 0.5f - b.y;
                if (dx * dx + dy * dy <= b.z * b.z) return true;
            }
            return false;
        }
        float fundo = 999f;
        foreach (Vector3 b in bolas) fundo = Mathf.Min(fundo, b.y - b.z);
        for (int y = 0; y < t.altura; y++)
            for (int x = 0; x < t.largura; x++)
            {
                if (!Dentro(x, y)) continue;
                float melhor = -99f, luz = 0f;
                foreach (Vector3 b in bolas)
                {
                    float nx = (x + 0.5f - b.x) / b.z, ny = (y + 0.5f - b.y) / b.z;
                    float folga = 1f - Mathf.Sqrt(nx * nx + ny * ny);
                    if (folga > melhor) { melhor = folga; luz = LuzDaBola(nx, ny); }
                }
                float v = 0.08f + 0.82f * luz + claridade + (Ruido(x / 2.2f, y / 2.2f, semente) - 0.5f) * 0.32f;
                v -= Mathf.Clamp01((fundo + 3f - y) / 3f) * 0.18f; // embaixo da copa: oclusão
                int tom = Tom(v, r.Length, x, y);
                if (luz > 0.78f && Sorteio(x, y, semente + 1) < 0.22f) tom = r.Length - 1; // pontinhos de sol
                t.Pintar(x, y, r, tom);
            }
        Contorno(t, Dentro, r, 0, 2);
        // folhinhas soltas na beirada da copa (a silhueta não fica redonda demais)
        for (int y = 1; y < t.altura - 1; y++)
            for (int x = 1; x < t.largura - 1; x++)
                if (!Dentro(x, y) && Dentro(x, y - 1) && Sorteio(x, y, semente + 2) < 0.18f)
                    t.Pintar(x, y, r, x < t.largura / 2 ? 2 : 0);
    }

    // Tronco de madeira: claro à esquerda, escuro à direita, com a sombra da copa em cima.
    static void Tronco(Tela t, int x0, int x1, int y0, int y1, int sombraEm)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                if (t.Cheio(x, y)) continue;
                float u = (x - x0 + 0.5f) / (x1 - x0 + 1);
                int tom = u < 0.34f ? 4 : u < 0.67f ? 3 : 1;
                if (x == x1) tom = 0;
                if (y >= sombraEm - 2) tom -= 2;
                if (Sorteio(x, y, 191) < 0.2f) tom--;
                t.Pintar(x, y, Madeira, tom);
            }
    }

    // ---------------------------------------------------------------- palmeiras e a Rocha da Caveira

    // Uma folha de palmeira: sai da copa (cx, cy) na direção "angulo" (em graus) e vai caindo com o peso.
    // A nervura é grossa perto da copa; embaixo dela ficam pendurados os folíolos (os "dentes" da folha).
    // As folhas viradas para a esquerda (para o sol) são mais claras. "escuro" baixa os tons (folhas de trás).
    static void FolhaDePalmeira(Tela t, Color32[] r, float cx, float cy, float angulo, float comprimento, float queda, int escuro)
    {
        float a = angulo * Mathf.Deg2Rad, dx = Mathf.Cos(a), dy = Mathf.Sin(a);
        int passos = Mathf.CeilToInt(comprimento * 2f);
        int luz = (dx < -0.25f ? 5 : dx > 0.35f ? 3 : 4) - escuro;
        for (int k = 0; k <= passos; k++)
        {
            float s = k / (float)passos;
            int px = Mathf.FloorToInt(cx + dx * comprimento * s), py = Mathf.FloorToInt(cy + dy * comprimento * s - queda * s * s);
            int tom = luz - (s > 0.8f ? 1 : 0);
            t.Pintar(px, py, r, tom);
            if (s < 0.45f) t.Pintar(px, py + 1, r, tom + 1); // perto da copa a folha é larga e pega luz em cima
            if (s > 0.12f && (k & 1) == 0)
            {
                int pendura = s < 0.6f ? 2 : 1;
                for (int j = 1; j <= pendura; j++) t.Pintar(px, py - j, r, tom - j);
            }
        }
    }

    // O tronco da palmeira: fino, curvado e com anéis, claro à esquerda e escuro à direita.
    static float TroncoDePalmeira(Tela t, float pe, float inclinacao, int topo, int escuro)
    {
        float Meio(float y) => pe + inclinacao * Mathf.Pow(y / topo, 1.7f);
        for (int y = 0; y <= topo; y++)
        {
            float meio = Meio(y), meia = y < 3 ? 2f : 1.6f - y / (float)topo * 0.5f; // mais grosso no pé
            int x0 = Mathf.RoundToInt(meio - meia), x1 = Mathf.Max(x0, Mathf.RoundToInt(meio + meia) - 1);
            for (int x = x0; x <= x1; x++)
            {
                float u = (x - x0 + 0.5f) / (x1 - x0 + 1);
                int tom = (u < 0.4f ? 4 : u < 0.75f ? 3 : 1) - escuro;
                if (y % 3 == 0) tom--;       // os anéis
                if (y >= topo - 1) tom--;    // embaixo da copa: sombra
                t.Pintar(x, y, Madeira, tom);
            }
        }
        return Meio(topo);
    }

    // As 3 palmeiras: uma inclinada para a direita, uma para a esquerda e uma alta e reta.
    static readonly float[] InclinacaoDaPalmeira = { 4.5f, -3.5f, 1.2f };
    static readonly int[] AlturaDaPalmeira = { 17, 16, 20 };

    static Sprite Palmeira(int versao)
    {
        var t = new Tela(26, 28);
        int topo = AlturaDaPalmeira[versao];
        float giro = -InclinacaoDaPalmeira[versao] * 3f; // a copa acompanha um pouco a inclinação
        float cx = TroncoDePalmeira(t, 13f, InclinacaoDaPalmeira[versao], topo, 0), cy = topo + 1.5f;
        // folhas de trás (mais escuras), depois os cocos, depois as da frente
        FolhaDePalmeira(t, Capim, cx, cy, 150f + giro, 8f, 4f, 2);
        FolhaDePalmeira(t, Capim, cx, cy, 32f + giro, 8f, 4f, 2);
        foreach (Vector2Int c in new[] { new Vector2Int(-2, -2), new Vector2Int(1, -2), new Vector2Int(-1, -3) })
        {
            int x = Mathf.FloorToInt(cx) + c.x, y = Mathf.FloorToInt(cy) + c.y;
            t.Pintar(x, y, Madeira, 1); t.Pintar(x + 1, y, Madeira, 0); t.Pintar(x, y + 1, Madeira, 2); t.Pintar(x + 1, y + 1, Madeira, 1);
        }
        FolhaDePalmeira(t, Capim, cx, cy, 178f + giro, 10f, 6.5f, 0);
        FolhaDePalmeira(t, Capim, cx, cy, 122f + giro, 7f, 3f, 0);
        FolhaDePalmeira(t, Capim, cx, cy, 88f + giro, 4f, 1f, 0);
        FolhaDePalmeira(t, Capim, cx, cy, 58f + giro, 7f, 3f, 1);
        FolhaDePalmeira(t, Capim, cx, cy, 2f + giro, 10f, 6.5f, 1);
        return t.Criar(new Vector2(13f / 26f, 0f));
    }

    // Palmeira morta (das ilhas do Kraken): tronco escuro e poucas folhas secas, quebradas e penduradas.
    static Sprite PalmeiraMorta()
    {
        var t = new Tela(20, 26);
        float cx = TroncoDePalmeira(t, 10f, -2f, 18, 1), cy = 19.5f;
        FolhaDePalmeira(t, PinhoSombrio, cx, cy, 165f, 7f, 7f, 0);
        FolhaDePalmeira(t, PinhoSombrio, cx, cy, 120f, 5f, 4f, 1);
        FolhaDePalmeira(t, PinhoSombrio, cx, cy, 40f, 6f, 6f, 1);
        FolhaDePalmeira(t, PinhoSombrio, cx, cy, 5f, 5f, 6f, 2);
        return t.Criar(new Vector2(0.5f, 0f));
    }

    // A Rocha da Caveira, perto do covil da Baleia Branca: um rochedo enorme no formato de uma caveira,
    // com os olhos fundos (lá dentro, um brilho roxo) e os dentes.
    static Sprite RochaDaCaveira()
    {
        const int w = 44, h = 40;
        var t = new Tela(w, h);
        const float cx = 22f, cy = 22f;
        bool Cranio(int x, int y)
        {
            float nx = (x + 0.5f - cx) / 17f, ny = (y + 0.5f - cy) / 15f;
            return nx * nx + ny * ny <= 1f + (Ruido(x * 0.4f, y * 0.4f, 491) - 0.5f) * 0.12f;
        }
        bool Queixo(int x, int y) => y >= 3 && y <= 14 && x >= 11 && x <= 32 && !((x == 11 || x == 32) && y <= 4);
        bool Base(int x, int y) => y <= 4 + Ruido(x * 0.3f, 0f, 493) * 3f && x >= 2 && x <= w - 3;
        bool Dentro(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && (Cranio(x, y) || Queixo(x, y) || Base(x, y));
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!Dentro(x, y)) continue;
                float nx = (x + 0.5f - cx) / 17f, ny = (y + 0.5f - cy) / 15f;
                float v = 0.12f + 0.8f * LuzDaBola(Mathf.Clamp(nx, -1f, 1f), Mathf.Clamp(ny, -1f, 1f));
                v += (Ruido(x / 2.5f, y / 2.5f, 495) - 0.5f) * 0.25f; // a rocha cheia de marcas
                if (!Cranio(x, y)) v -= 0.12f;                        // o queixo e a base ficam na sombra do crânio
                t.Pintar(x, y, Pedra, Tom(v, Pedra.Length, x, y));
            }
        // os olhos: dois buracos fundos com uma brasa roxa no meio
        foreach (float ox in new[] { 14.5f, 29.5f })
            for (int y = 15; y <= 27; y++)
                for (int x = (int)ox - 6; x <= (int)ox + 6; x++)
                {
                    float nx = (x + 0.5f - ox) / 5f, ny = (y + 0.5f - 21f) / 4.5f;
                    float d = nx * nx + ny * ny;
                    if (d > 1f) continue;
                    if (d > 0.75f && ny > 0f) t.Pintar(x, y, Pedra, 1); // a borda de cima do buraco, na sombra
                    else t.Cor(x, y, d < 0.12f ? (Color32)Magia[4] : d < 0.3f ? (Color32)Magia[1] : C(14, 10, 24));
                }
        // o nariz (um triângulo de ponta-cabeça)
        for (int y = 12; y <= 16; y++)
            for (int x = 19; x <= 25; x++)
                if (Mathf.Abs(x + 0.5f - 22f) <= (y - 11.5f) * 0.7f) t.Cor(x, y, C(14, 10, 24));
        // os dentes: blocos claros com frestas escuras
        for (int x = 13; x <= 30; x++)
            for (int y = 6; y <= 10; y++)
            {
                bool fresta = (x - 13) % 3 == 2 || y == 8;
                if (fresta) t.Cor(x, y, C(14, 10, 24));
                else t.Pintar(x, y, Pedra, y == 10 || y == 7 ? 6 : 5);
            }
        // rachaduras
        for (int k = 0; k < 10; k++) t.Pintar(30 + k / 2, 33 - k, Pedra, 1);
        for (int k = 0; k < 6; k++) t.Pintar(8 + k, 28 + (k & 1), Pedra, 1);
        ContornoDeFora(t, ContornoEscuro);
        return t.Criar(new Vector2(cx / w, 0f));
    }

    // ---------------------------------------------------------------- montanhas

    static Sprite Montanha(int versao)
    {
        const int w = 26, h = 23;
        var t = new Tela(w, h);
        float pico = versao == 0 ? 21.5f : versao == 1 ? 18.5f : 13.5f;
        float meio = versao == 1 ? 11.5f : versao == 2 ? 13.5f : 13f;
        int semente = 420 + versao * 7;
        float Cume(int x) => meio + (Ruido(x * 0.4f, 0f, semente) - 0.5f) * 2f; // a crista entre o lado do sol e o da sombra
        bool Dentro(int x, int y)
        {
            if (x < 0 || x >= w || y < 0) return false;
            float dx = (x + 0.5f - meio) / 12.5f;
            float altura = pico * (1f - Mathf.Pow(Mathf.Abs(dx), 1.15f)) + (Ruido(x * 0.6f, 3f, semente + 1) - 0.5f) * 3f;
            return y + 0.5f <= altura;
        }
        const float neve = 99f; // (ilha tropical: rochedo sem neve no topo)
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!Dentro(x, y)) continue;
                bool sol = x + 0.5f < Cume(y);
                float camada = Ruido(x * 0.3f, y * 0.9f, semente + 2); // faixas de rocha
                bool comNeve = y > neve + (Ruido(x * 0.7f, 0f, semente + 3) - 0.5f) * 4f - (sol ? 0f : 1.5f);
                if (comNeve)
                {
                    int tom = sol ? (camada > 0.6f ? 5 : 4) : (camada > 0.55f ? 3 : 2);
                    t.Pintar(x, y, Neve, tom);
                }
                else if (versao == 2 && y < 4 + Ruido(x * 0.5f, 0f, semente + 4) * 3f && Ruido(x * 0.5f, y * 0.5f, semente + 4) > 0.4f)
                {
                    // morro mais baixo: o capim sobe um pouco pelo pé
                    float v = (sol ? 0.55f : 0.3f) + (camada - 0.5f) * 0.2f;
                    t.Pintar(x, y, Capim, Tom(v, Capim.Length, x, y));
                }
                else
                {
                    float v = (sol ? 0.68f : 0.36f) + (camada - 0.5f) * 0.22f - (y < 3 ? 0.1f : 0f);
                    t.Pintar(x, y, Pedra, Tom(v, Pedra.Length, x, y));
                }
            }
        Contorno(t, Dentro, Pedra, 0, 3);
        // a neve escorre um pixelzinho por cima do contorno do lado do sol
        for (int y = 1; y < h; y++)
            for (int x = 0; x < w; x++)
                if (t.RampaEm(x, y) == Neve && t.RampaEm(x, y - 1) == Pedra && Sorteio(x, y, semente + 5) < 0.4f)
                    t.Pintar(x, y - 1, Neve, 2);
        // o pé da montanha some na grama (tufinhos em cima da pedra)
        for (int x = 0; x < w; x++)
            if (Dentro(x, 0) && Sorteio(x, 0, semente + 6) < 0.55f)
            {
                t.Pintar(x, 0, Capim, 2);
                if (Sorteio(x, 1, semente + 6) < 0.4f) t.Pintar(x, 1, Capim, 3);
            }
        return t.Criar(new Vector2(meio / w, 0f));
    }

    // ---------------------------------------------------------------- casas

    static Sprite Casa(int versao)
    {
        const int w = 20, h = 21;
        var t = new Tela(w, h);
        Color32[] telha = versao == 0 ? TelhaVermelha : versao == 1 ? TelhaAzul : TelhaMusgo;
        // chaminé (atrás do telhado): pedra
        for (int y = 14; y <= 19; y++)
            for (int x = 12; x <= 14; x++)
                t.Pintar(x, y, Pedra, y == 19 ? 1 : x == 12 ? 5 : x == 13 ? 4 : 2);
        // parede da frente (x 3 a 14) e o lado (x 15 a 17), que fica na sombra
        for (int y = 1; y <= 9; y++)
            for (int x = 3; x <= 17; x++)
            {
                bool lado = x >= 15;
                if (lado && y > 9 - (x - 15)) continue;   // o lado sobe em degrau até o telhado
                float v = lado ? 0.38f : 0.72f - (x - 3) * 0.012f;
                if (y >= 8) v -= 0.22f;                   // embaixo do beiral: sombra
                int tom = Tom(v + (Sorteio(x, y, 431 + versao) - 0.5f) * 0.1f, Reboco.Length, x, y);
                t.Pintar(x, y, Reboco, tom);
            }
        // vigas de madeira (enxaimel) na frente
        for (int y = 1; y <= 9; y++) { t.Pintar(3, y, Madeira, 2); t.Pintar(14, y, Madeira, 1); }
        for (int x = 3; x <= 14; x++) t.Pintar(x, 1, Madeira, 1);
        // porta e janelas acesas (luz quente lá dentro)
        for (int y = 2; y <= 6; y++)
            for (int x = 8; x <= 10; x++)
                t.Pintar(x, y, Madeira, y == 6 ? 0 : x == 8 ? 3 : 2);
        t.Pintar(10, 4, Ouro, 3);
        foreach (int jx in new[] { 5, 12 })
        {
            // janelinha 2 x 2 com a luz quente lá de dentro, moldura de madeira e floreira embaixo
            t.Pintar(jx, 5, Ouro, 4); t.Pintar(jx + 1, 5, Ouro, 3);
            t.Pintar(jx, 4, Ouro, 3); t.Pintar(jx + 1, 4, Ouro, 2);
            for (int x = jx - 1; x <= jx + 2; x++) t.Pintar(x, 6, Madeira, 1);
            t.Pintar(jx - 1, 3, Capim, 4); t.Pintar(jx, 3, Capim, 3); t.Cor(jx + 1, 3, ArteDosBlocos.PetalaRosa); t.Pintar(jx + 2, 3, Capim, 2);
        }
        // telhado: duas águas, a de trás pega sol (mais clara), a da frente fica de "frente" para nós
        for (int y = 9; y <= 17; y++)
        {
            int beiral = y <= 13 ? 1 : 1 + (y - 13); // o telhado vai afinando em cima (os lados inclinados)
            for (int x = beiral; x <= 19 - beiral; x++)
            {
                bool tras = y >= 15;
                float v = tras ? 0.72f : 0.5f - (x - beiral) * 0.012f;
                int fileira = (y - 9) / 2, seam = (x + fileira * 2) % 4;
                int tom = Tom(v, telha.Length, x, y);
                if ((y - 9) % 2 == 0) tom--;      // a borda de cada fileira de telhas
                else if (seam == 0) tom--;         // as emendas entre as telhas
                if (y == 15) tom = telha.Length - 1; // a cumeeira pega luz
                if (y == 9) tom = 1;                // o beiral
                if (x == beiral || x == 19 - beiral) tom = x == beiral ? Mathf.Min(tom, 2) : 0;
                t.Pintar(x, y, telha, tom);
            }
        }
        // a chaminé passa por cima da água de trás
        for (int y = 16; y <= 19; y++)
            for (int x = 12; x <= 14; x++)
                t.Pintar(x, y, Pedra, y == 19 ? 1 : x == 12 ? 5 : x == 13 ? 4 : 2);
        t.Pintar(13, 20, Pedra, 0); t.Pintar(12, 20, Pedra, 2); t.Pintar(14, 20, Pedra, 1);
        // contorno escuro em volta de tudo
        var silhueta = new bool[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) silhueta[y * w + x] = t.Cheio(x, y);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (silhueta[y * w + x]) continue;
                bool encosta = (x > 0 && silhueta[y * w + x - 1]) || (x < w - 1 && silhueta[y * w + x + 1]) || (y > 0 && silhueta[(y - 1) * w + x]) || (y < h - 1 && silhueta[(y + 1) * w + x]);
                if (encosta && y >= 1) t.Cor(x, y, ContornoEscuro);
            }
        for (int x = 3; x <= 17; x++) t.Cor(x, 0, ContornoEscuro);
        return t.Criar(new Vector2(10f / w, 0f));
    }

    // ---------------------------------------------------------------- castelo, forte e as peças deles

    // Torre redonda: sombreada como um cilindro (luz à esquerda), com tijolos.
    static void Torre(Tela t, Color32[] r, int x0, int x1, int y0, int y1)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float u = (x - x0 + 0.5f) / (x1 - x0 + 1);
                float v = 0.86f - 0.7f * Mathf.Pow(u, 1.3f) + (u < 0.15f ? -0.12f : 0f);
                int tom = Tom(v, r.Length, x, y);
                bool rejunte = (y - y0) % 4 == 3 || ((y - y0) / 4 % 2 == 0 ? (x - x0) % 5 == 2 : (x - x0) % 5 == 4);
                if (rejunte) tom--;
                if (x == x1) tom = 0;
                t.Pintar(x, y, r, tom);
            }
    }

    // Parede reta (a frente de um prédio): clara, com tijolos e um degradê de luz da esquerda.
    static void Parede(Tela t, Color32[] r, int x0, int x1, int y0, int y1, float claridade)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float v = claridade - (x - x0) / (float)(x1 - x0 + 1) * 0.16f + (Sorteio(x, y, 451) - 0.5f) * 0.08f;
                if (y == y1) v += 0.1f;
                int tom = Tom(v, r.Length, x, y);
                bool rejunte = (y - y0) % 4 == 3 || ((y - y0) / 4 % 2 == 0 ? (x - x0) % 6 == 0 : (x - x0) % 6 == 3);
                if (rejunte) tom--;
                if (x == x1) tom = 0;
                t.Pintar(x, y, r, tom);
            }
    }

    // Telhado em cone (das torres): triângulo com luz à esquerda e "escamas" de telha.
    static void Cone(Tela t, Color32[] r, float meio, float raio, int yBase, int yTopo)
    {
        for (int y = yBase; y <= yTopo; y++)
        {
            float meia = raio * (yTopo - y + 0.5f) / (yTopo - yBase + 1) + 0.3f;
            for (int x = Mathf.FloorToInt(meio - meia); x <= Mathf.CeilToInt(meio + meia); x++)
            {
                float dx = (x + 0.5f - meio) / Mathf.Max(0.6f, meia);
                if (Mathf.Abs(dx) > 1f) continue;
                float v = 0.62f - dx * 0.38f;
                int tom = Tom(v, r.Length, x, y);
                if ((y - yBase) % 3 == 0 && ((x + y) & 1) == 0) tom--; // escamas
                if (y == yBase) tom = 1;                                // o beiral faz sombra
                if (dx > 0.8f) tom = 0;
                t.Pintar(x, y, r, tom);
            }
        }
    }

    // Telhado "de duas águas" de um prédio largo (visto de frente e um pouco de cima).
    static void Telhado(Tela t, Color32[] r, int x0, int x1, int yBase, int yTopo)
    {
        for (int y = yBase; y <= yTopo; y++)
        {
            int recuo = (y - yBase) / 2;
            for (int x = x0 + recuo; x <= x1 - recuo; x++)
            {
                float v = 0.48f + (y - yBase) / (float)(yTopo - yBase + 1) * 0.3f - (x - x0) / (float)(x1 - x0 + 1) * 0.12f;
                int tom = Tom(v, r.Length, x, y);
                if ((y - yBase) % 2 == 0) tom--;
                if (y == yBase) tom = 1;
                if (y == yTopo) tom = r.Length - 1;
                if (x == x1 - recuo) tom = 0;
                else if (x == x0 + recuo) tom = Mathf.Min(tom + 1, r.Length - 1);
                t.Pintar(x, y, r, tom);
            }
        }
    }

    // Janela: escura (vidro) ou acesa (luz quente), com um arco em cima.
    static void Janela(Tela t, int x0, int y0, int largura, int altura, bool acesa)
    {
        for (int y = y0; y < y0 + altura; y++)
            for (int x = x0; x < x0 + largura; x++)
            {
                bool arco = y == y0 + altura - 1 && largura >= 2 && (x == x0 || x == x0 + largura - 1);
                if (arco) continue;
                if (acesa) t.Pintar(x, y, Ouro, y == y0 + altura - 1 || x == x0 ? 4 : 3);
                else t.Cor(x, y, x == x0 && y == y0 + altura - 2 ? C(110, 130, 190) : Vidro);
            }
    }

    // Ameias (os "dentinhos" em cima da muralha).
    static void Ameias(Tela t, Color32[] r, int x0, int x1, int y)
    {
        for (int x = x0; x <= x1; x++)
            if ((x - x0) % 4 < 2)
            {
                t.Pintar(x, y, r, (x - x0) % 4 == 0 ? 5 : 3);
                t.Pintar(x, y + 1, r, (x - x0) % 4 == 0 ? 6 : 4);
            }
    }

    // Contorno escuro em volta de tudo que foi pintado (pixel vazio encostado no desenho).
    static void ContornoDeFora(Tela t, Color32 cor)
    {
        int w = t.largura, h = t.altura;
        var cheio = new bool[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) cheio[y * w + x] = t.Cheio(x, y);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (cheio[y * w + x]) continue;
                bool encosta = (x > 0 && cheio[y * w + x - 1]) || (x < w - 1 && cheio[y * w + x + 1]) || (y > 0 && cheio[(y - 1) * w + x]) || (y < h - 1 && cheio[(y + 1) * w + x]);
                if (encosta) t.Cor(x, y, cor);
            }
    }

    // O castelo do porto: muralha branca com portão, torres com telhados azuis em cone
    // e a torre principal no meio. As bandeirinhas são desenhos separados (tremulam: veja a VidaDoMapa).
    static Sprite Castelo()
    {
        const int w = 82, h = 66;
        var t = new Tela(w, h);
        int meio = 41;
        // torre principal (atrás de tudo) e as duas torres do castelo
        Torre(t, PedraClara, meio - 6, meio + 6, 30, 50);
        Cone(t, TelhaAzul, meio + 0.5f, 8.5f, 50, 62);
        Torre(t, PedraClara, meio - 22, meio - 13, 24, 44);
        Cone(t, TelhaAzul, meio - 17f, 6.5f, 44, 54);
        Torre(t, PedraClara, meio + 13, meio + 22, 24, 44);
        Cone(t, TelhaAzul, meio + 18f, 6.5f, 44, 54);
        // o palácio (no meio) com telhado azul
        Parede(t, PedraClara, meio - 14, meio + 14, 14, 32, 0.78f);
        Telhado(t, TelhaAzul, meio - 15, meio + 15, 33, 40);
        for (int k = 0; k < 4; k++)
        {
            Janela(t, meio - 11 + k * 7, 24, 3, 5, k == 1);
            Janela(t, meio - 11 + k * 7, 17, 3, 4, k == 2);
        }
        Janela(t, meio - 1, 42, 3, 5, true); // a janela acesa da torre principal
        Janela(t, meio - 18, 34, 2, 4, false);
        Janela(t, meio + 17, 34, 2, 4, true);
        // a muralha da frente, com as torrinhas dos cantos e o portão
        Parede(t, PedraClara, 6, w - 7, 0, 14, 0.7f);
        Ameias(t, PedraClara, 6, w - 7, 15);
        Torre(t, PedraClara, 1, 10, 0, 22);
        Cone(t, TelhaAzul, 6f, 6f, 22, 30);
        Torre(t, PedraClara, w - 11, w - 2, 0, 22);
        Cone(t, TelhaAzul, w - 6f, 6f, 22, 30);
        for (int y = 0; y <= 10; y++) // o portão em arco, com a grade levantada
            for (int x = meio - 5; x <= meio + 5; x++)
            {
                float dx = x + 0.5f - meio;
                if (y > 6 && dx * dx + (y - 6) * (y - 6) * 1.6f > 30f) continue;
                bool grade = (x - meio) % 3 == 0 || y == 9;
                t.Cor(x, y, grade && y > 6 ? C(70, 64, 80) : y <= 1 ? C(60, 44, 40) : C(20, 16, 30));
            }
        ContornoDeFora(t, ContornoEscuro);
        return t.Criar(new Vector2(meio / (float)w, 0f));
    }

    // A boca de um canhão na muralha: o buraco escuro e o cano de ferro saindo dele.
    static void Canhao(Tela t, int cx, int cy)
    {
        for (int y = cy - 1; y <= cy + 2; y++)
            for (int x = cx - 2; x <= cx + 2; x++)
                t.Cor(x, y, C(20, 16, 30));
        for (int x = cx - 1; x <= cx + 1; x++) { t.Pintar(x, cy, Pedra, x == cx - 1 ? 3 : 2); t.Pintar(x, cy + 1, Pedra, x == cx - 1 ? 4 : 3); }
        t.Cor(cx, cy, C(8, 6, 14)); // a boca do cano
    }

    // O forte da Capitã Âncora: muralha de pedra com canhões, as duas torres dos cantos e a torre de vigia
    // de madeira no meio. As bandeiras (pretas, de pirata) tremulam nas pontas (TorresDoForte).
    static Sprite FortePirata()
    {
        const int w = 74, h = 50;
        var t = new Tela(w, h);
        int meio = 37;
        // a torre de vigia (atrás de tudo)
        Torre(t, Madeira, meio - 6, meio + 6, 16, 37);
        Cone(t, TelhaVermelha, meio + 0.5f, 9f, 38, 47);
        Janela(t, meio - 1, 29, 3, 4, true);
        Janela(t, meio - 4, 21, 2, 3, false);
        Janela(t, meio + 3, 21, 2, 3, true);
        // as torres dos cantos
        Torre(t, Pedra, 3, 14, 0, 27);
        Cone(t, TelhaVermelha, 8.5f, 7.5f, 28, 35);
        Torre(t, Pedra, w - 15, w - 4, 0, 27);
        Cone(t, TelhaVermelha, w - 9.5f, 7.5f, 28, 35);
        Janela(t, 8, 18, 2, 4, true);
        Janela(t, w - 10, 18, 2, 4, false);
        // a muralha da frente, com as ameias, os canhões e o portão de madeira
        Parede(t, Pedra, 15, w - 16, 0, 16, 0.74f);
        Ameias(t, Pedra, 15, w - 16, 17);
        foreach (int x in new[] { 20, 28, 46, 54 }) Canhao(t, x, 9);
        for (int y = 0; y <= 10; y++)
            for (int x = meio - 4; x <= meio + 4; x++)
            {
                float dx = x + 0.5f - meio;
                if (y > 7 && dx * dx + (y - 7) * (y - 7) * 2f > 20f) continue;
                int tom = x == meio - 4 ? 3 : x == meio + 4 ? 0 : (x - meio) % 2 == 0 ? 1 : 2; // as tábuas do portão
                if (y == 3 || y == 7) tom = 0;                                                // as travas
                t.Pintar(x, y, Madeira, tom);
            }
        ContornoDeFora(t, ContornoEscuro);
        return t.Criar(new Vector2(meio / (float)w, 0f));
    }

    // ---------------------------------------------------------------- navio fantasma, naufrágio, baú, navio e farol

    // As Cabines Assombradas: um navio fantasma encalhado nas pedras, com as velas rasgadas e as janelas
    // da popa brilhando de roxo (a magia da Dóris).
    static Sprite NavioFantasma()
    {
        const int w = 34, h = 36;
        var t = new Tela(w, h);
        // o mastro e a vela rasgada (atrás do casco)
        for (int y = 8; y <= 33; y++) { t.Pintar(17, y, Madeira, 2); t.Pintar(18, y, Madeira, 0); }
        for (int x = 15; x <= 20; x++) t.Pintar(x, 31, Madeira, x == 15 ? 3 : 1); // o cesto da gávea
        for (int y = 16; y <= 29; y++)
            for (int x = 8; x <= 27; x++)
            {
                float u = (x - 8 + 0.5f) / 20f;
                float barriga = Mathf.Sin(u * Mathf.PI) * 1.5f; // a vela "estufada" pelo vento
                if (y < 16 + barriga * 0.6f || y > 29 - barriga * 0.3f) continue;
                if (Ruido(x * 0.45f, y * 0.45f, 501) > 0.66f) continue;          // os rasgos
                if (y < 19 && Sorteio(x, y, 503) < 0.35f) continue;               // a barra de baixo, esfiapada
                float v = 0.82f - u * 0.45f + (Sorteio(x, y, 505) - 0.5f) * 0.1f;
                t.Pintar(x, y, Neve, Tom(v, Neve.Length, x, y));
            }
        // o casco: largo no convés, estreito embaixo; a popa (esquerda) é alta, com as janelas
        for (int y = 0; y <= 17; y++)
            for (int x = 1; x <= 32; x++)
            {
                bool casco = y <= 10 && x >= 9 - y * 0.7f && x <= 25 + y * 0.65f;
                bool popa = y > 10 && y <= 17 && x >= 2 && x <= 11;
                bool proa = y > 10 && y <= 13 && x >= 27 && x <= 32 - (y - 11);
                if (!casco && !popa && !proa) continue;
                float v = 0.55f - (x - 1) / 32f * 0.25f;
                int tom = Tom(v, Madeira.Length, x, y) - 1; // madeira velha, escura
                if (y % 3 == 0) tom--;                       // as tábuas
                if (y == 10 && casco) tom = 3;                // a amurada pega luz
                t.Pintar(x, y, Madeira, tom);
            }
        foreach (int jx in new[] { 3, 6, 9 }) // as janelas da popa, acesas de roxo
            for (int y = 13; y <= 15; y++) { t.Pintar(jx, y, Magia, y == 15 ? 6 : 5); t.Pintar(jx + 1, y, Magia, 4); }
        for (int k = 0; k < 4; k++) t.Pintar(14 + k * 3, 6, Magia, 3); // vigias com a luz de dentro
        // as pedras onde ele encalhou
        for (int x = 0; x < w; x++)
            for (int y = 0; y <= 2; y++)
                if (Ruido(x * 0.5f, y, 507) > 0.42f + y * 0.15f) t.Pintar(x, y, Pedra, y == 2 ? 4 : 2);
        ContornoDeFora(t, ContornoEscuro);
        return t.Criar(new Vector2(17f / w, 0f));
    }

    // Um navio naufragado na areia: o casco quebrado, as costelas de madeira de fora e o mastro caído.
    static Sprite Naufragio()
    {
        const int w = 18, h = 16;
        var t = new Tela(w, h);
        // o mastro caído (na diagonal, atrás)
        for (int k = 0; k <= 11; k++) { t.Pintar(9 + k / 2 + k % 2, 2 + k, Madeira, 2); t.Pintar(10 + k / 2 + k % 2, 2 + k, Madeira, 0); }
        // as costelas do casco: arcos de madeira saindo da areia
        foreach (int cx in new[] { 3, 6, 9, 12 })
        {
            int altura = cx == 3 ? 9 : cx == 6 ? 11 : cx == 9 ? 8 : 6;
            for (int y = 1; y <= altura; y++)
            {
                int x = cx + (y > altura - 3 ? 1 : 0);
                t.Pintar(x, y, Madeira, 3); t.Pintar(x + 1, y, Madeira, 1);
            }
        }
        // um pedaço do costado ainda inteiro (as tábuas)
        for (int y = 1; y <= 4; y++)
            for (int x = 2; x <= 13; x++)
                t.Pintar(x, y, Madeira, (y % 2 == 0 ? 2 : 3) - (x > 10 ? 1 : 0));
        // a areia por cima do pé
        for (int x = 0; x < w; x++)
        {
            t.Pintar(x, 0, Areia, 3);
            if (Sorteio(x, 1, 511) < 0.5f) t.Pintar(x, 1, Areia, 4);
        }
        ContornoDeFora(t, ContornoEscuro);
        return t.Criar(new Vector2(7f / w, 0f));
    }

    // O baú do tesouro, aberto e cheio de moedas de ouro (com um brilho na pilha).
    static Sprite Bau()
    {
        const int w = 16, h = 14;
        var t = new Tela(w, h);
        // a tampa aberta (atrás)
        for (int y = 8; y <= 11; y++)
            for (int x = 3; x <= 12; x++)
                t.Pintar(x, y, Madeira, y == 11 ? 4 : x == 12 ? 1 : 2);
        for (int x = 3; x <= 12; x++) t.Pintar(x, 9, Pedra, x == 12 ? 2 : 4); // a cinta de ferro da tampa
        // as moedas transbordando
        for (int y = 6; y <= 8; y++)
            for (int x = 2; x <= 13; x++)
            {
                if (y == 8 && (x < 4 || x > 11)) continue;
                int tom = 2 + ((x + y) % 3 == 0 ? 2 : 1) - (x > 10 ? 1 : 0);
                t.Pintar(x, y, Ouro, tom);
            }
        t.Pintar(5, 8, Ouro, 4); t.Cor(6, 9, C(255, 255, 255)); // o brilho
        // a caixa: madeira com as cintas de ferro e a fechadura de ouro
        for (int y = 0; y <= 5; y++)
            for (int x = 2; x <= 13; x++)
            {
                int tom = x <= 3 ? 4 : x >= 12 ? 1 : 3;
                if (y == 5) tom = 4;
                t.Pintar(x, y, Madeira, tom);
                if (x == 4 || x == 11) t.Pintar(x, y, Pedra, x == 4 ? 4 : 2);
            }
        t.Pintar(7, 3, Ouro, 3); t.Pintar(8, 3, Ouro, 2); t.Pintar(7, 2, Ouro, 2); t.Pintar(8, 2, Ouro, 1);
        ContornoDeFora(t, ContornoEscuro);
        return t.Criar(new Vector2(0.5f, 0f));
    }

    // Um navio pirata navegando: casco com a faixa dourada e as portinholas dos canhões, duas velas e a
    // bandeira preta com a caveira lá no alto. O pivô fica na linha da água.
    static Sprite Navio()
    {
        const int w = 30, h = 32;
        var t = new Tela(w, h);
        const int agua = 3;
        // os mastros (atrás das velas)
        for (int y = agua + 6; y <= 29; y++) { t.Pintar(11, y, Madeira, 2); t.Pintar(12, y, Madeira, 0); }
        for (int y = agua + 6; y <= 26; y++) { t.Pintar(20, y, Madeira, 2); t.Pintar(21, y, Madeira, 0); }
        // as velas: brancas, estufadas para a frente (luz à esquerda)
        void Vela(int x0, int x1, int y0, int y1)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float u = (x - x0 + 0.5f) / (x1 - x0 + 1), s = (y - y0 + 0.5f) / (y1 - y0 + 1);
                    float v = 0.92f - u * 0.5f + Mathf.Sin(s * Mathf.PI) * 0.08f;
                    if ((y - y0) % 4 == 3) v -= 0.12f; // as costuras
                    t.Pintar(x, y, Reboco, Tom(v, Reboco.Length, x, y));
                }
        }
        Vela(6, 17, agua + 11, agua + 24);
        Vela(16, 25, agua + 10, agua + 21);
        // a bandeira preta com a caveira
        for (int y = 27; y <= 30; y++)
            for (int x = 13; x <= 18; x++)
                t.Cor(x, y, C(26, 22, 34));
        t.Cor(15, 29, C(240, 236, 228)); t.Cor(16, 29, C(240, 236, 228)); t.Cor(15, 28, C(200, 196, 196)); t.Cor(16, 28, C(200, 196, 196));
        // o casco
        for (int y = 0; y <= agua + 7; y++)
            for (int x = 0; x < w; x++)
            {
                float s = y / (float)(agua + 7);
                bool casco = x >= 6 - s * 4f && x <= 22 + s * 5f;
                bool popa = y > agua + 4 && x >= 1 && x <= 8;
                bool proa = y >= agua + 5 && x >= 26 && x <= 29 - (y - agua - 5);
                if (!casco && !popa && !proa) continue;
                float v = 0.62f - x / (float)w * 0.3f;
                int tom = Tom(v, Madeira.Length, x, y);
                if (y < agua) tom -= 2;          // embaixo da linha da água: escuro e esverdeado
                if (y == agua + 6) tom = 4;       // a amurada pega luz
                t.Pintar(x, y, Madeira, tom);
                if (y == agua + 3) t.Pintar(x, y, Ouro, x < 14 ? 3 : 2); // a faixa dourada
            }
        for (int x = 7; x <= 22; x += 4) { t.Cor(x, agua + 1, C(20, 16, 30)); t.Cor(x + 1, agua + 1, C(20, 16, 30)); } // as portinholas
        for (int y = agua + 8; y <= agua + 9; y++) for (int x = 2; x <= 7; x++) t.Pintar(x, y, Madeira, x == 2 ? 4 : 2); // o castelo da popa
        t.Pintar(4, agua + 8, Ouro, 4); // a lanterna da popa
        ContornoDeFora(t, ContornoEscuro);
        return t.Criar(new Vector2(15f / w, agua / (float)h));
    }

    // O farol do porto: torre listrada de vermelho e branco em cima das pedras, com a lanterna acesa.
    static Sprite Farol()
    {
        const int w = 14, h = 38;
        var t = new Tela(w, h);
        for (int x = 0; x < w; x++) // as pedras do pé
            for (int y = 0; y <= 3; y++)
                if (Ruido(x * 0.6f, y * 0.6f, 521) > 0.25f + y * 0.12f) t.Pintar(x, y, Pedra, y >= 2 ? 4 : 2);
        for (int y = 3; y <= 26; y++) // a torre (afina para cima), com as listras
        {
            float meia = 4.2f - (y - 3) / 23f * 1.4f;
            for (int x = 0; x < w; x++)
            {
                float dx = x + 0.5f - 7f;
                if (Mathf.Abs(dx) > meia) continue;
                float v = 0.82f - (dx + meia) / (2f * meia) * 0.55f;
                bool listra = (y - 3) / 5 % 2 == 1;
                Color32[] r = listra ? TelhaVermelha : Reboco;
                t.Pintar(x, y, r, Tom(v, r.Length, x, y));
            }
        }
        for (int x = 2; x <= 11; x++) t.Pintar(x, 27, Pedra, x == 11 ? 1 : 3); // a sacada
        for (int y = 28; y <= 32; y++) // a lanterna acesa
            for (int x = 4; x <= 9; x++)
            {
                if (x == 4 || x == 9 || y == 28) t.Pintar(x, y, Pedra, 2); // a armação
                else t.Pintar(x, y, Ouro, y >= 31 ? 4 : 3);
            }
        Cone(t, TelhaVermelha, 7f, 4f, 33, 37);
        ContornoDeFora(t, ContornoEscuro);
        return t.Criar(new Vector2(7f / w, 0f));
    }

    // ---------------------------------------------------------------- flores, sombras e bichinhos

    // Moita de flores (algumas folhas e 4 ou 5 flores de cores diferentes).
    static Sprite Flores(int versao)
    {
        const int w = 14, h = 9;
        var t = new Tela(w, h);
        Color32[][] cores =
        {
            new[] { ArteDosBlocos.PetalaRosa, ArteDosBlocos.PetalaAmarela, ArteDosBlocos.PetalaBranca },
            new[] { C(140, 170, 255), ArteDosBlocos.PetalaBranca, C(200, 150, 255) },
            new[] { C(236, 90, 70), ArteDosBlocos.PetalaAmarela, C(250, 150, 60) },
            new[] { ArteDosBlocos.PetalaRosa, C(255, 200, 220), ArteDosBlocos.PetalaBranca },
        };
        for (int x = 1; x < w - 1; x++) // folhas embaixo
            for (int y = 0; y <= 2; y++)
                if (Sorteio(x, y, 471 + versao) < 0.75f - y * 0.2f) t.Pintar(x, y, Capim, y == 0 ? 2 : 3 + (x < 7 ? 1 : 0));
        for (int k = 0; k < 5; k++)
        {
            int x = 2 + (int)(Sorteio(k, versao, 473) * 10f), y = 3 + (int)(Sorteio(k, versao, 475) * 4f);
            Color32 p = cores[versao][k % 3];
            for (int c = 1; c < y; c++) t.Pintar(x, c, Capim, 2 + (c & 1)); // caule
            t.Cor(x, y, ArteDosBlocos.Miolo);
            t.Cor(x - 1, y, p); t.Cor(x + 1, y, p); t.Cor(x, y + 1, p);
            t.Cor(x, y - 1, Color32.Lerp(p, ContornoEscuro, 0.35f));
        }
        return t.Criar(new Vector2(0.5f, 0f));
    }

    // Sombra no chão: uma elipse azul-arroxeada transparente, com a borda em dithering.
    static Sprite SombraNoChao(int largura, int altura)
    {
        var cor = new Color32(26, 16, 60, 92);
        return FabricaDeSprites.Procedural(largura, altura, FabricaDeSprites.Centro, (x, y) =>
        {
            float nx = (x + 0.5f) / largura * 2f - 1f, ny = (y + 0.5f) / altura * 2f - 1f;
            float d = nx * nx + ny * ny;
            if (d > 1f) return FabricaDeSprites.Transparente;
            if (d > 0.62f && Limiar(x, y) + 0.5f < (d - 0.62f) / 0.38f) return FabricaDeSprites.Transparente;
            return cor;
        });
    }

    // Brilho redondo e suave (os baús, o farol e o navio fantasma "acesos"): branco, transparente nas bordas.
    static Sprite Brilho(int tamanho, Color32 cor)
    {
        return FabricaDeSprites.Procedural(tamanho, tamanho, FabricaDeSprites.Centro, (x, y) =>
        {
            float nx = (x + 0.5f) / tamanho * 2f - 1f, ny = (y + 0.5f) / tamanho * 2f - 1f;
            float a = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny));
            return new Color32(cor.r, cor.g, cor.b, (byte)(a * a * 255f));
        });
    }

    // Baforada de fumaça: bolinha cinza-clara com luz em cima-esquerda.
    static Sprite Fumaca()
    {
        return FabricaDeSprites.Procedural(6, 6, FabricaDeSprites.Centro, (x, y) =>
        {
            float nx = (x + 0.5f - 3f) / 3f, ny = (y + 0.5f - 3f) / 3f;
            float d = nx * nx + ny * ny;
            if (d > 1f || (d > 0.7f && ((x + y) & 1) == 0)) return FabricaDeSprites.Transparente;
            return LuzDaBola(nx, ny) > 0.55f ? new Color32(240, 236, 232, 255) : new Color32(186, 182, 200, 255);
        });
    }

    // Gaivota de longe: um "v" escuro (asas para cima ou para baixo).
    static Sprite Passaro(int quadro)
    {
        string[] arte = quadro == 0
            ? new[] { "k.....k", ".k...k.", "..kkk..", }
            : new[] { ".......", "..kkk..", "kk...kk", };
        return FabricaDeSprites.DeArte(arte, FabricaDeSprites.Centro);
    }

    // Borboleta: asas brancas (a VidaDoMapa pinta de cores diferentes), abertas ou fechadas.
    static Sprite Borboleta(int quadro)
    {
        string[] arte = quadro == 0 ? new[] { "ww.ww", "wwkww", ".w.w." } : new[] { ".....", ".wkw.", "..w.." };
        return FabricaDeSprites.DeArte(arte, FabricaDeSprites.Centro);
    }

    // Folhinha voando no vento (duas posições: de frente e de lado).
    static Sprite Folha(int quadro)
    {
        string[] arte = quadro == 0 ? new[] { ".ag", "gG." } : new[] { "a..", ".G." };
        return FabricaDeSprites.DeArte(arte, FabricaDeSprites.Centro);
    }

    // Bandeirinha tremulando (branca: o mapa pinta da cor de cada castelo). Pivô no mastro.
    static Sprite Bandeira(int quadro)
    {
        const int w = 9, h = 8;
        return FabricaDeSprites.Procedural(w, h, new Vector2(0.5f / w, 0f), (x, y) =>
        {
            if (x == 0) return y <= 7 ? (Color32)C(60, 48, 52) : FabricaDeSprites.Transparente; // o mastro
            float onda = Mathf.Sin(x * 0.9f - quadro * Mathf.PI / 2f) * Mathf.Min(1f, x / 3f);
            float meio = 5.5f + onda * 0.9f;
            float meia = 1.6f - x * 0.08f;
            if (Mathf.Abs(y + 0.5f - meio) > meia) return FabricaDeSprites.Transparente;
            bool dobra = Mathf.Cos(x * 0.9f - quadro * Mathf.PI / 2f) < -0.3f; // a parte da dobra fica na sombra
            return dobra ? new Color32(176, 170, 190, 255) : new Color32(255, 255, 255, 255);
        });
    }

    // Sombra de nuvem para o mapa: uma mancha grande, de borda bem macia (branca: quem dá a cor é o mapa).
    static Sprite SombraDeNuvem()
    {
        const int w = 128, h = 64;
        var sprite = FabricaDeSprites.Procedural(w, h, FabricaDeSprites.Centro, (x, y) =>
        {
            float nx = (x + 0.5f) / w * 2f - 1f, ny = (y + 0.5f) / h * 2f - 1f;
            float forma = 1f - (nx * nx + ny * ny) + (Ruido(x / 14f, y / 10f, 481) - 0.5f) * 0.9f;
            float a = Mathf.Clamp01(forma * 1.6f);
            return new Color32(255, 255, 255, (byte)(a * a * (3f - 2f * a) * 255f));
        }, 8); // 8 pixels por bloco: a mancha cobre 16 x 8 blocos
        sprite.texture.filterMode = FilterMode.Bilinear; // bem borrada (é só a sombra)
        return sprite;
    }
}
