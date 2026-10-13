using UnityEngine;
using Tela = ArteDosBlocos.Tela;

// ARTE DO MAPA (parte 2): os enfeites que ficam em cima do chão. Árvores, pinheiros, montanhas com neve,
// casinhas com telhado de telhas e janela acesa, o castelo real, a mansão do Roswaal, a Biblioteca,
// ruínas, cristais, flores, a Grande Árvore Flügel e os bichinhos (pássaros, borboletas, fumaça...).
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
    static readonly Color32[] TelhaLilas = // os telhados da mansão do Roswaal
    {
        C(36, 22, 56), C(64, 40, 92), C(98, 64, 132), C(136, 98, 168), C(178, 142, 202), C(218, 192, 230),
    };
    static readonly Color32[] PinhoSombrio = // pinheiros da região sombria
    {
        C(14, 12, 26), C(26, 22, 44), C(40, 34, 66), C(58, 50, 90), C(80, 72, 116), C(108, 102, 142),
    };
    static readonly Color32[] Folhagem = // a copa clara (verde-dourado) da Grande Árvore e das árvores mais jovens
    {
        C(22, 44, 40), C(36, 76, 46), C(62, 112, 48), C(100, 148, 54), C(146, 180, 66), C(196, 208, 98), C(236, 230, 150),
    };
    static readonly Color32 ContornoEscuro = C(24, 18, 36), Vidro = C(30, 34, 70);

    const float LuzX = ArteDosBlocos.LuzX, LuzY = ArteDosBlocos.LuzY;

    // ---------------------------------------------------------------- registro

    // Onde fica a boca da chaminé de cada casa (em blocos, a partir do pé da casa): a fumaça sai daqui.
    public static readonly Vector3 Chamine = new Vector3(3.5f / P, 19.5f / P, 0f);
    // As pontas das torres (onde ficam as bandeirinhas), em blocos a partir do pé do desenho.
    public static readonly Vector3[] TorresDoCastelo = { new Vector3(0f, 63f / P, 0f), new Vector3(-17.5f / P, 55f / P, 0f), new Vector3(17.5f / P, 55f / P, 0f) };
    public static readonly Vector3[] TorresDaMansao = { new Vector3(0f, 47f / P, 0f) };
    public const int VersoesDaArvore = 3, VersoesDaMontanha = 3, VersoesDaCasa = 3, VersoesDasFlores = 4, QuadrosDaBandeira = 4;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        Vector2 baseDoDesenho = FabricaDeSprites.Base, centro = FabricaDeSprites.Centro;
        RegistrarCaminhos();
        FabricaDeSprites.Registrar("mapa_ponte", () => Ponte(false, false));
        FabricaDeSprites.Registrar("mapa_ponte_empe", () => Ponte(true, false));
        FabricaDeSprites.Registrar("mapa_ponte_corda", () => Ponte(false, true));
        FabricaDeSprites.Registrar("mapa_ponte_corda_empe", () => Ponte(true, true));
        for (int v = 0; v < VersoesDaArvore; v++) { int vv = v; FabricaDeSprites.Registrar("mapa_arvore_" + v, () => Arvore(vv)); }
        for (int v = 0; v < VersoesDaMontanha; v++) { int vv = v; FabricaDeSprites.Registrar("mapa_montanha_" + v, () => Montanha(vv)); }
        for (int v = 0; v < VersoesDaCasa; v++) { int vv = v; FabricaDeSprites.Registrar("mapa_casa_" + v, () => Casa(vv)); }
        for (int v = 0; v < VersoesDasFlores; v++) { int vv = v; FabricaDeSprites.Registrar("mapa_flores_" + v, () => Flores(vv)); }
        FabricaDeSprites.Registrar("mapa_pinheiro", Pinheiro);
        FabricaDeSprites.Registrar("mapa_arvore_grande", ArvoreGrande);
        FabricaDeSprites.Registrar("mapa_castelo", Castelo);
        FabricaDeSprites.Registrar("mapa_mansao", Mansao);
        FabricaDeSprites.Registrar("mapa_biblioteca", Biblioteca);
        FabricaDeSprites.Registrar("mapa_ruina", Ruina);
        FabricaDeSprites.Registrar("mapa_cristal", Cristal);
        FabricaDeSprites.Registrar("mapa_brilho", () => Brilho(16, new Color32(255, 255, 255, 255)));
        FabricaDeSprites.Registrar("mapa_sombra_p", () => SombraNoChao(12, 5));
        FabricaDeSprites.Registrar("mapa_sombra_m", () => SombraNoChao(18, 7));
        FabricaDeSprites.Registrar("mapa_sombra_g", () => SombraNoChao(26, 9));
        FabricaDeSprites.Registrar("mapa_sombra_castelo", () => SombraNoChao(84, 16));
        FabricaDeSprites.Registrar("mapa_sombra_arvore_grande", () => SombraNoChao(56, 14));
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

    // ---------------------------------------------------------------- árvores

    static readonly Vector3[][] BolasDaArvore =
    {
        // 0: carvalho redondo
        new[] { new Vector3(10f, 14.5f, 6.2f), new Vector3(5.5f, 12.5f, 4.2f), new Vector3(14.5f, 12.5f, 4.4f), new Vector3(7.5f, 18.5f, 4.8f), new Vector3(12.8f, 18.5f, 4.6f), new Vector3(10f, 21.5f, 3.8f) },
        // 1: copa mais larga e baixa (com frutinhas)
        new[] { new Vector3(10f, 13.5f, 6f), new Vector3(4.8f, 12.5f, 4.2f), new Vector3(15.2f, 12.8f, 4.2f), new Vector3(8f, 17.5f, 5f), new Vector3(13f, 17f, 4.6f), new Vector3(10.5f, 20f, 3.6f) },
        // 2: alta e fina (um álamo, mais claro)
        new[] { new Vector3(10f, 11.5f, 4.6f), new Vector3(10f, 15.5f, 5f), new Vector3(10.5f, 19.5f, 4.4f), new Vector3(10f, 22.8f, 3f), new Vector3(7.2f, 13.5f, 3.2f), new Vector3(13f, 16.5f, 3.2f) },
    };

    static Sprite Arvore(int versao)
    {
        var t = new Tela(20, 26);
        Vector3[] bolas = BolasDaArvore[versao];
        Copa(t, bolas, versao == 2 ? Folhagem : Capim, 300 + versao * 11, versao == 2 ? 0.04f : 0f);
        if (versao == 1)
            foreach (Vector2Int f in new[] { new Vector2Int(5, 14), new Vector2Int(12, 12), new Vector2Int(9, 18), new Vector2Int(15, 16), new Vector2Int(7, 11) })
            {
                t.Cor(f.x, f.y, C(222, 60, 64));     // maçãs (com um brilhinho)
                t.Cor(f.x, f.y - 1, C(140, 30, 50));
            }
        int fundo = versao == 2 ? 7 : 8;
        Tronco(t, 9, 11, 0, fundo + 1, fundo + 1);
        // raízes saindo do pé
        t.Pintar(8, 0, Madeira, 2); t.Pintar(12, 0, Madeira, 0); t.Pintar(8, 1, Madeira, 3);
        return t.Criar(new Vector2(10f / 20f, 0f));
    }

    // A Grande Árvore Flügel: tronco enorme com raízes, copa gigante verde-dourada.
    static Sprite ArvoreGrande()
    {
        var t = new Tela(56, 64);
        var bolas = new[]
        {
            new Vector3(28f, 36f, 13f), new Vector3(15f, 32f, 9f), new Vector3(41f, 32f, 9.5f), new Vector3(19f, 44f, 10f),
            new Vector3(37f, 45f, 10f), new Vector3(28f, 52f, 9f), new Vector3(8f, 37f, 6f), new Vector3(48f, 38f, 6f),
            new Vector3(24f, 57f, 5.5f), new Vector3(33f, 58f, 5f),
        };
        Copa(t, bolas, Folhagem, 401);
        // tronco largo (de 22 a 33) que se abre em raízes embaixo
        for (int y = 0; y <= 24; y++)
        {
            float abre = Mathf.Max(0f, 6f - y) * 1.2f;
            int x0 = Mathf.RoundToInt(22f - abre), x1 = Mathf.RoundToInt(33f + abre);
            for (int x = x0; x <= x1; x++)
            {
                if (t.Cheio(x, y)) continue;
                float u = (x - x0 + 0.5f) / (x1 - x0 + 1);
                float v = 0.75f - u * 0.6f + (Ruido(x / 1.5f, y / 4f, 403) - 0.5f) * 0.35f; // casca com veios
                if (y >= 20) v -= 0.25f;
                int tom = Tom(v, Madeira.Length, x, y);
                if (x == x0 || x == x1) tom = 0;
                t.Pintar(x, y, Madeira, tom);
            }
        }
        // musgo e luzinhas na copa (é uma árvore mágica)
        for (int k = 0; k < 9; k++)
        {
            int x = 10 + (int)(Sorteio(k, 0, 405) * 36f), y = 28 + (int)(Sorteio(k, 1, 405) * 26f);
            if (t.RampaEm(x, y) == Folhagem) t.Cor(x, y, C(255, 246, 190));
        }
        return t.Criar(new Vector2(28f / 56f, 0f));
    }

    // Pinheiro sombrio: quatro "andares" de galhos, com luz à esquerda e neve nenhuma (é a terra da Bruxa).
    static Sprite Pinheiro()
    {
        var t = new Tela(16, 26);
        bool Dentro(int x, int y)
        {
            float dx = Mathf.Abs(x + 0.5f - 8f);
            for (int andar = 0; andar < 4; andar++)
            {
                float baseDoAndar = 4f + andar * 5f, topo = baseDoAndar + 8f - andar * 0.5f;
                float largura = 7.5f - andar * 1.5f;
                if (y >= baseDoAndar && y <= topo && dx <= (topo - y) / (topo - baseDoAndar) * largura + 0.5f) return true;
            }
            return false;
        }
        for (int y = 0; y < 26; y++)
            for (int x = 0; x < 16; x++)
            {
                if (!Dentro(x, y)) continue;
                float nx = (x + 0.5f - 8f) / 7f;
                float v = 0.62f - nx * 0.5f + (Sorteio(x, y, 411) - 0.5f) * 0.2f;
                if (!Dentro(x, y - 1)) v -= 0.1f;     // embaixo de cada andar: sombra
                else if (!Dentro(x, y + 2)) v += 0.2f; // em cima de cada andar: luz
                t.Pintar(x, y, PinhoSombrio, Tom(v, PinhoSombrio.Length, x, y));
            }
        Contorno(t, Dentro, PinhoSombrio, 0, 2);
        Tronco(t, 7, 8, 0, 4, 4);
        return t.Criar(new Vector2(0.5f, 0f));
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
        float neve = versao == 2 ? 99f : pico * 0.52f;
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

    // ---------------------------------------------------------------- castelo real e mansão

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

    // O castelo real da capital (Lugunica): muralha branca com portão, torres com telhados azuis em cone
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

    // A mansão do Roswaal: um casarão largo (duas alas e o corpo do meio) com telhados lilás.
    static Sprite Mansao()
    {
        const int w = 74, h = 50;
        var t = new Tela(w, h);
        int meio = 37;
        // o corpo do meio, mais alto, com uma torrinha de relógio
        Torre(t, Reboco, meio - 4, meio + 4, 30, 40);
        Cone(t, TelhaLilas, meio + 0.5f, 6f, 40, 48);
        Parede(t, Reboco, meio - 12, meio + 12, 0, 26, 0.84f);
        Telhado(t, TelhaLilas, meio - 13, meio + 13, 27, 34);
        // as duas alas
        Parede(t, Reboco, 4, meio - 13, 0, 18, 0.8f);
        Telhado(t, TelhaLilas, 3, meio - 12, 19, 26);
        Parede(t, Reboco, meio + 13, w - 5, 0, 18, 0.66f);
        Telhado(t, TelhaLilas, meio + 12, w - 4, 19, 26);
        // janelas (algumas acesas) e a porta grande
        for (int k = 0; k < 3; k++)
        {
            Janela(t, 8 + k * 7, 10, 2, 4, k == 2);
            Janela(t, 8 + k * 7, 3, 2, 4, false);
            Janela(t, meio + 17 + k * 7, 10, 2, 4, k == 0);
            Janela(t, meio + 17 + k * 7, 3, 2, 4, k == 2);
        }
        for (int k = 0; k < 3; k++) Janela(t, meio - 9 + k * 8, 17, 3, 5, k != 1);
        Janela(t, meio - 1, 33, 3, 4, true); // o relógio... ou a janela do escritório do Roswaal
        for (int y = 0; y <= 8; y++)
            for (int x = meio - 3; x <= meio + 3; x++)
                t.Pintar(x, y, Madeira, y == 8 ? 0 : x == meio - 3 ? 3 : x == meio + 3 ? 0 : 2);
        ContornoDeFora(t, ContornoEscuro);
        return t.Criar(new Vector2(meio / (float)w, 0f));
    }

    // ---------------------------------------------------------------- biblioteca, ruína, cristal

    // Torre da Biblioteca Proibida: pedra, cúpula roxa (a magia da Beatrice) e janelas brilhando.
    static Sprite Biblioteca()
    {
        const int w = 26, h = 36;
        var t = new Tela(w, h);
        Torre(t, Pedra, 6, 19, 0, 22);
        for (int x = 4; x <= 21; x++) { t.Pintar(x, 23, Pedra, x == 21 ? 0 : 5); t.Pintar(x, 22, Pedra, x == 21 ? 0 : 2); } // a beirada
        for (int y = 24; y <= 33; y++) // a cúpula
            for (int x = 5; x <= 20; x++)
            {
                float nx = (x + 0.5f - 13f) / 8f, ny = (y + 0.5f - 24f) / 10f;
                if (nx * nx + ny * ny > 1f) continue;
                float v = 0.2f + 0.8f * LuzDaBola(nx, ny);
                int tom = Tom(v, Magia.Length, x, y);
                if (((x - 5) % 4) == 0) tom--; // as nervuras da cúpula
                t.Pintar(x, y, Magia, tom);
            }
        t.Pintar(12, 34, Ouro, 4); t.Pintar(13, 34, Ouro, 3); t.Pintar(12, 35, Ouro, 4); // a estrelinha na ponta
        for (int y = 0; y <= 7; y++) // a porta, com a escuridão mágica lá dentro
            for (int x = 11; x <= 14; x++)
            {
                if (y == 7 && (x == 11 || x == 14)) continue;
                t.Pintar(x, y, Magia, y >= 5 ? 2 : 1);
            }
        foreach (int jx in new[] { 8, 16 })
            for (int y = 12; y <= 16; y++)
                for (int x = jx; x <= jx + 1; x++)
                    t.Pintar(x, y, Magia, y == 16 ? 6 : x == jx ? 5 : 4);
        ContornoDeFora(t, ContornoEscuro);
        return t.Criar(new Vector2(13f / w, 0f));
    }

    // Coluna quebrada das ruínas antigas, com musgo em cima e um pedaço caído do lado.
    static Sprite Ruina()
    {
        const int w = 18, h = 16;
        var t = new Tela(w, h);
        int Topo(int x) => x < 4 || x > 8 ? -1 : 13 - (int)(Sorteio(x, 0, 461) * 3f) - (x == 8 ? 2 : 0);
        for (int x = 4; x <= 8; x++)
            for (int y = 2; y <= Topo(x); y++)
            {
                float u = (x - 4 + 0.5f) / 5f;
                int tom = u < 0.3f ? 5 : u < 0.7f ? 4 : 2;
                if (x == 6 && y > 3 && y < Topo(x) - 1) tom--; // a canelura da coluna
                if (y == Topo(x)) tom = Sorteio(x, y, 463) < 0.6f ? -1 : 6;
                if (tom < 0) t.Pintar(x, y, Capim, 3);
                else t.Pintar(x, y, Pedra, tom);
            }
        for (int x = 2; x <= 10; x++) { t.Pintar(x, 1, Pedra, x <= 3 ? 5 : x >= 9 ? 1 : 3); t.Pintar(x, 0, Pedra, 1); } // a base
        for (int x = 12; x <= 16; x++) // o pedaço caído (deitado)
            for (int y = 0; y <= 3; y++)
            {
                if ((x == 12 || x == 16) && y == 3) continue;
                t.Pintar(x, y, Pedra, y == 3 ? 5 : y == 0 ? 1 : x == 16 ? 2 : 4);
            }
        t.Pintar(13, 4, Capim, 3); t.Pintar(14, 4, Capim, 4);
        ContornoDeFora(t, ContornoEscuro);
        return t.Criar(new Vector2(7f / w, 0f));
    }

    // Três cristais mágicos com faces: a da esquerda pega luz, a da direita fica no escuro.
    static Sprite Cristal()
    {
        const int w = 16, h = 16;
        var t = new Tela(w, h);
        void Um(float meio, float largura, int altura)
        {
            for (int y = 0; y <= altura; y++)
            {
                float meia = y > altura - 4 ? largura * (altura - y + 0.5f) / 4f : largura;
                for (int x = Mathf.FloorToInt(meio - meia); x <= Mathf.CeilToInt(meio + meia) - 1; x++)
                {
                    float dx = x + 0.5f - meio;
                    if (Mathf.Abs(dx) > meia) continue;
                    int tom = dx < -meia * 0.3f ? 5 : dx < meia * 0.3f ? 3 : 1;
                    if (y == altura - 3 && dx < 0f) tom = 6; // o brilho na quina
                    t.Pintar(x, y, Magia, tom);
                }
            }
        }
        Um(4f, 2f, 8);
        Um(12f, 2f, 7);
        Um(8f, 2.5f, 13);
        ContornoDeFora(t, C(30, 14, 48));
        return t.Criar(new Vector2(0.5f, 0f));
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

    // Brilho redondo e suave (os cristais e a biblioteca "acesos"): branco, transparente nas bordas.
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

    // Passarinho de longe: um "v" escuro (asas para cima ou para baixo).
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
