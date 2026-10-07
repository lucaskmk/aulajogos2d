using UnityEngine;

// Os desenhos da BALEIA BRANCA (o chefe final), feitos por código como o resto do jogo.
// Ela é uma ORCA gigante com um CHIFRE de osso espiralado na testa:
//  - corpo preto-azulado por cima e barriga branca, com a "mancha" branca atrás do olho (a marca da orca);
//  - nadadeira dorsal alta (com um rasgo de batalha), nadadeira peitoral grande e o rabo em "V";
//  - olho vermelho brilhando, cicatrizes de garra e um pouco de névoa em volta.
// Ela sempre é desenhada olhando para a ESQUERDA, com o pivô no centro (o mapa do mundo também usa "baleia").
//
// Como o desenho é feito: em vez de escrever 96 x 40 letrinhas à mão, cada PARTE do corpo é uma forma
// geométrica (elipses, curvas, segmentos). Primeiro a gente descobre "que parte" é cada pixel (a máscara),
// depois pinta: contorno preto na borda de cada parte, luz em cima, sombra embaixo, detalhes por cima.
// Assim dá para fazer os quadros da animação só mudando um número (o rabo sobe e desce, a boca abre...).
//
// Sprites registrados aqui (use com FabricaDeSprites.Pegar):
//   "baleia", "baleia_0".."baleia_3"  nadando (o rabo batendo)
//   "baleia_boca"                     boca aberta (rugido / cuspindo névoa)
//   "baleia_carga"                    chifre aceso (vai atirar o raio)
//   "baleia_tonta"                    atordoada no chão (olho em X e língua de fora)
//   e os ataques: "baleia_raio_0..2", "baleia_linha", "baleia_faixa", "baleia_elo", "baleia_bola_0..1",
//   "baleia_onda_0..1", "baleia_olho", "baleia_alvo", "baleia_rachadura", "baleia_escuridao", "baleia_brilho_chifre".
public static class ArteDaBaleia
{
    public const int Largura = 96, Altura = 40;
    public const int QuadrosDoRabo = 4;

    // Pontos do desenho (em pixels, contando do canto de baixo à esquerda) que a luta precisa saber.
    public static readonly Vector2 PixelDoOlho = new Vector2(19f, 18f);
    public static readonly Vector2 PixelDaPontaDoChifre = new Vector2(2f, 39f);
    public static readonly Vector2 PixelDaBoca = new Vector2(6f, 13f);

    // Converte um ponto do desenho para unidades do mundo, a partir do centro (pivô) da baleia olhando para a esquerda.
    public static Vector2 EmUnidades(Vector2 pixel) =>
        new Vector2(pixel.x - Largura / 2f, pixel.y - Altura / 2f) / FabricaDeSprites.PixelsPorUnidade;

    // ------------------------------------------------------------------ cores

    static readonly Color32 Contorno = FabricaDeSprites.Cor('k');
    // corpo preto-azulado
    static readonly Color32 CorpoSombra = new Color32(20, 22, 38, 255);
    static readonly Color32 Corpo = new Color32(34, 38, 62, 255);
    static readonly Color32 CorpoLuz = new Color32(58, 66, 104, 255);
    static readonly Color32 CorpoBrilho = new Color32(110, 124, 180, 255);
    // barriga e manchas brancas
    static readonly Color32 Branco = new Color32(240, 242, 252, 255);
    static readonly Color32 BrancoSombra = new Color32(196, 202, 230, 255);
    static readonly Color32 BrancoSombraForte = new Color32(150, 156, 198, 255);
    // "sela" cinza atrás da nadadeira dorsal
    static readonly Color32 Sela = new Color32(84, 90, 122, 255);
    static readonly Color32 SelaLuz = new Color32(112, 118, 150, 255);
    // chifre de osso
    static readonly Color32 OssoClaro = new Color32(244, 236, 210, 255);
    static readonly Color32 Osso = new Color32(214, 198, 160, 255);
    static readonly Color32 OssoSombra = new Color32(160, 140, 108, 255);
    // chifre aceso (vai atirar)
    static readonly Color32 ChifreAceso = new Color32(255, 250, 255, 255);
    static readonly Color32 ChifreAcesoMeio = new Color32(226, 186, 255, 255);
    static readonly Color32 ChifreAcesoSombra = new Color32(170, 100, 240, 255);
    static readonly Color32 AuraRoxa = new Color32(176, 96, 230, 130);
    // olho, boca e cicatrizes
    static readonly Color32 Olho = new Color32(235, 40, 50, 255);
    static readonly Color32 OlhoBrilho = new Color32(255, 214, 214, 255);
    static readonly Color32 OlhoAura = new Color32(255, 50, 60, 110);
    static readonly Color32 Goela = new Color32(96, 16, 36, 255);
    static readonly Color32 Lingua = new Color32(222, 92, 118, 255);
    static readonly Color32 Dente = new Color32(252, 250, 238, 255);
    static readonly Color32 Cicatriz = new Color32(150, 112, 138, 255);
    static readonly Color32 CicatrizClara = new Color32(196, 160, 182, 255);
    static readonly Color32 CicatrizNoBranco = new Color32(206, 150, 172, 255);
    static readonly Color32 Tonta = new Color32(255, 222, 90, 255);

    // ------------------------------------------------------------------ registro

    // Como esse desenho é a "pose" da baleia: qual quadro do rabo, boca aberta, chifre aceso...
    struct Pose
    {
        public int rabo;          // 0..3 (o rabo sobe e desce)
        public bool boca;         // boca aberta, mostrando os dentes
        public bool chifreAceso;  // chifre brilhando roxo (vai atirar)
        public bool tonta;        // atordoada: olho em X e língua de fora
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        FabricaDeSprites.Registrar("baleia", () => Orca(new Pose()));
        for (int i = 0; i < QuadrosDoRabo; i++)
        {
            int quadro = i; // cópia para a função lembrar o valor certo
            FabricaDeSprites.Registrar("baleia_" + quadro, () => Orca(new Pose { rabo = quadro }));
        }
        FabricaDeSprites.Registrar("baleia_boca", () => Orca(new Pose { rabo = 1, boca = true }));
        FabricaDeSprites.Registrar("baleia_carga", () => Orca(new Pose { rabo = 0, chifreAceso = true }));
        FabricaDeSprites.Registrar("baleia_tonta", () => Orca(new Pose { rabo = 2, tonta = true, boca = true }));

        // ataques e efeitos
        for (int i = 0; i < 3; i++)
        {
            int quadro = i;
            FabricaDeSprites.Registrar("baleia_raio_" + quadro, () => FabricaDeSprites.Procedural(1, 16, FabricaDeSprites.Centro, (x, y) => CorDoRaio(y, quadro)));
        }
        FabricaDeSprites.Registrar("baleia_linha", () => FabricaDeSprites.Procedural(1, 5, FabricaDeSprites.Centro, (x, y) => CorDaLinha(y)));
        FabricaDeSprites.Registrar("baleia_faixa", () => FabricaDeSprites.Procedural(1, 16, FabricaDeSprites.Centro, (x, y) => CorDaFaixa(y)));
        FabricaDeSprites.Registrar("baleia_elo", () => FabricaDeSprites.Procedural(16, 10, FabricaDeSprites.Centro, CorDoElo));
        for (int i = 0; i < 2; i++)
        {
            int quadro = i;
            FabricaDeSprites.Registrar("baleia_bola_" + quadro, () => FabricaDeSprites.Procedural(14, 14, FabricaDeSprites.Centro, (x, y) => CorDaBola(x, y, quadro)));
            FabricaDeSprites.Registrar("baleia_onda_" + quadro, () => FabricaDeSprites.Procedural(16, 14, new Vector2(0.5f, 0f), (x, y) => CorDaOnda(x, y, quadro)));
        }
        FabricaDeSprites.Registrar("baleia_olho", () => FabricaDeSprites.Procedural(9, 9, FabricaDeSprites.Centro, CorDoBrilhoDoOlho));
        FabricaDeSprites.Registrar("baleia_brilho_chifre", () => FabricaDeSprites.Procedural(24, 24, FabricaDeSprites.Centro, CorDoBrilhoDoChifre));
        FabricaDeSprites.Registrar("baleia_alvo", () => FabricaDeSprites.Procedural(32, 8, FabricaDeSprites.Centro, CorDoAlvo));
        FabricaDeSprites.Registrar("baleia_rachadura", () => FabricaDeSprites.Procedural(16, 16, FabricaDeSprites.Centro, CorDaRachadura));
        FabricaDeSprites.Registrar("baleia_escuridao", () => FabricaDeSprites.Procedural(256, 128, FabricaDeSprites.Centro, CorDaEscuridao));
    }

    // ------------------------------------------------------------------ a orca

    // Partes do corpo (a "máscara"): cada pixel sabe a que parte pertence.
    const int Vazio = 0, Silhueta = 1, Peitoral = 2, Chifre = 3;

    static Sprite Orca(Pose pose)
    {
        Color32[,] tela = PintarOrca(pose);
        return FabricaDeSprites.Procedural(Largura, Altura, FabricaDeSprites.Centro, (x, y) => tela[x, y]);
    }

    // Curva suave de 0 a 1 (começa e termina devagar).
    static float Suave(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    // Quanto o rabo está dobrado para cima (+) ou para baixo (-) neste quadro.
    static float Dobra(int rabo) => new[] { 0f, 2.5f, 0f, -2.5f }[((rabo % 4) + 4) % 4];

    // O quanto a linha do meio do corpo sobe/desce em x (só a parte de trás dobra, junto com o rabo).
    static float Curva(float x, float dobra)
    {
        float t = Mathf.Clamp01((x - 46f) / 48f);
        return dobra * t * t;
    }

    // Contorno de cima e de baixo do tronco: cabeça grande e redonda (o "melão" da orca) na frente,
    // costas quase retas e o corpo afinando até o rabo.
    static float Topo(float x, float dobra)
    {
        float y;
        if (x <= 18f) y = 15f + 13f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow((18f - x) / 16f, 2.2f)), 1f / 2.2f);
        else if (x <= 32f) y = 28f + 1.5f * Suave((x - 18f) / 14f);
        else y = 29.5f - 9f * Suave((x - 32f) / 50f);
        return y + Curva(x, dobra);
    }

    static float Baixo(float x, float dobra, bool boca)
    {
        float y = x <= 30f
            ? 13.5f - 7.5f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow((30f - x) / 28f, 2.2f)), 1f / 2.2f)
            : 6f + 10.5f * Suave((x - 30f) / 52f);
        if (boca && x <= 20f) y = Mathf.Min(y, Queixo(x) - 1.5f); // boca aberta: o queixo desce
        return y + Curva(x, dobra);
    }

    // Linha da boca (de cima) e a do queixo quando a boca está aberta.
    static float LinhaDaBoca(float x) => 14.4f - (x - 2f) * 0.08f;
    static float Queixo(float x) => LinhaDaBoca(x) - Mathf.Max(0f, 20f - x) * 0.42f;

    static bool NoTronco(float x, float y, Pose pose)
    {
        float dobra = Dobra(pose.rabo);
        if (x < 2f || x > 84f) return false;
        return y >= Baixo(x, dobra, pose.boca) && y <= Topo(x, dobra);
    }

    // Nadadeira dorsal: alta, inclinada para trás e com um rasgo (uma "mordida" antiga).
    static bool NaDorsal(float x, float y)
    {
        if (y < 26f || y > 39f) return false;
        float t = (y - 27f) / 12f;
        if (t < 0f) t = 0f;
        float frente = 37f + 13.5f * Mathf.Pow(t, 0.75f);
        float tras = 55.5f - 4f * Mathf.Pow(t, 1.8f);
        if (x >= 53f && (y == 32f || y == 33f)) return false; // o rasgo
        return x >= frente && x <= tras;
    }

    // Distância do ponto p ao segmento a-b (e onde, de 0 a 1, fica o ponto mais perto).
    static float DistanciaAoSegmento(Vector2 p, Vector2 a, Vector2 b, out float onde)
    {
        Vector2 ab = new Vector2(b.x - a.x, b.y - a.y);
        Vector2 ap = new Vector2(p.x - a.x, p.y - a.y);
        float tamanho2 = ab.x * ab.x + ab.y * ab.y;
        onde = tamanho2 > 0f ? Mathf.Clamp01((ap.x * ab.x + ap.y * ab.y) / tamanho2) : 0f;
        float dx = a.x + ab.x * onde - p.x, dy = a.y + ab.y * onde - p.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    // O rabo em "V": duas pontas (de cima e de baixo) que balançam com a dobra.
    static bool NoRabo(float x, float y, float dobra)
    {
        if (x < 78f) return false;
        float meio = 18.5f + Curva(84f, dobra);
        var p = new Vector2(x, y);
        var raiz = new Vector2(81f, meio);
        var pontaDeCima = new Vector2(95f, meio + 9f + dobra * 1.2f);
        var pontaDeBaixo = new Vector2(94f, meio - 8f + dobra * 1.2f);
        float t;
        if (DistanciaAoSegmento(p, raiz, pontaDeCima, out t) <= 3f * (1f - t) + 0.6f) return true;
        if (DistanciaAoSegmento(p, raiz, pontaDeBaixo, out t) <= 2.8f * (1f - t) + 0.6f) return true;
        return x <= 86f && Mathf.Abs(y - meio) <= 2.6f; // o "pescoço" do rabo
    }

    // Nadadeira peitoral: uma elipse grande inclinada para baixo e para trás.
    static bool NaPeitoral(float x, float y, float dobra)
    {
        float dx = x - 31f, dy = y - 6.5f;
        float angulo = -55f * Mathf.Deg2Rad;
        float ao = dx * Mathf.Cos(angulo) + dy * Mathf.Sin(angulo);      // ao longo da nadadeira
        float atravessado = -dx * Mathf.Sin(angulo) + dy * Mathf.Cos(angulo);
        float a = 8f, b = 3.4f;
        return (ao * ao) / (a * a) + (atravessado * atravessado) / (b * b) <= 1f && y >= 0f;
    }

    // O chifre: uma ponta de osso saindo da testa, apontando para a frente e para cima (a ponta curva para cima).
    // "onde" = 0 na base, 1 na ponta; "lado" = distância (com sinal) até o eixo, para as listras da espiral.
    static bool NoChifre(float x, float y, out float onde, out float lado)
    {
        onde = 0f;
        lado = 0f;
        var p = new Vector2(x, y);
        float melhor = 99f;
        const int passos = 40;
        Vector2 anterior = PontoDoChifre(0f);
        for (int i = 1; i <= passos; i++)
        {
            float s = i / (float)passos;
            Vector2 atual = PontoDoChifre(s);
            float d = DistanciaAoSegmento(p, anterior, atual, out float t);
            if (d < melhor)
            {
                melhor = d;
                onde = Mathf.Lerp((i - 1) / (float)passos, s, t);
                // de que lado do eixo está o pixel (positivo = lado de cima/da frente)
                Vector2 eixo = new Vector2(atual.x - anterior.x, atual.y - anterior.y);
                float tamanho = Mathf.Sqrt(eixo.x * eixo.x + eixo.y * eixo.y);
                lado = (eixo.x * (p.y - anterior.y) - eixo.y * (p.x - anterior.x)) / tamanho;
            }
            anterior = atual;
        }
        float grossura = 3.1f * Mathf.Pow(1f - onde, 0.8f) + 0.5f;
        return melhor <= grossura;
    }

    // Eixo do chifre: da base (no alto da testa) até a ponta, com uma curvinha (a ponta "levanta").
    static Vector2 PontoDoChifre(float s)
    {
        var baseDoChifre = new Vector2(15f, 26f);
        Vector2 ponta = PixelDaPontaDoChifre;
        float curva = -Mathf.Sin(s * Mathf.PI) * 1.8f;
        Vector2 eixo = new Vector2(ponta.x - baseDoChifre.x, ponta.y - baseDoChifre.y);
        float tamanho = Mathf.Sqrt(eixo.x * eixo.x + eixo.y * eixo.y);
        Vector2 perpendicular = new Vector2(eixo.y / tamanho, -eixo.x / tamanho); // aponta para trás/cima
        return new Vector2(baseDoChifre.x + eixo.x * s + perpendicular.x * curva,
                           baseDoChifre.y + eixo.y * s + perpendicular.y * curva);
    }

    // Partes brancas da orca: queixo/garganta, barriga e a "pincelada" branca no flanco.
    static bool NoBranco(float x, float y, float dobra)
    {
        float borda;
        if (x < 19f) borda = LinhaDaBoca(x) - 0.6f;
        else if (x < 27f) borda = Mathf.Lerp(LinhaDaBoca(19f) - 0.6f, 11f, (x - 19f) / 8f);
        else if (x < 50f) borda = Mathf.Lerp(11f, 9f, (x - 27f) / 23f);
        else borda = Mathf.Lerp(9f, 7.5f, (x - 50f) / 30f);
        if (y <= borda + Curva(x, dobra)) return true;

        // pincelada do flanco (sobe para trás, atrás da dorsal)
        float cx = 62f, cy = 13.5f + Curva(62f, dobra);
        float angulo = 24f * Mathf.Deg2Rad;
        float dx = x - cx, dy = y - cy;
        float ao = dx * Mathf.Cos(angulo) + dy * Mathf.Sin(angulo);
        float atravessado = -dx * Mathf.Sin(angulo) + dy * Mathf.Cos(angulo);
        return (ao * ao) / 90f + (atravessado * atravessado) / 9f <= 1f;
    }

    // A mancha branca atrás do olho (a marca registrada das orcas).
    static bool NaManchaDoOlho(float x, float y)
    {
        float cx = 27f, cy = 21.6f;
        float angulo = 14f * Mathf.Deg2Rad;
        float dx = x - cx, dy = y - cy;
        float ao = dx * Mathf.Cos(angulo) + dy * Mathf.Sin(angulo);
        float atravessado = -dx * Mathf.Sin(angulo) + dy * Mathf.Cos(angulo);
        return (ao * ao) / 30f + (atravessado * atravessado) / 4.4f <= 1f;
    }

    static bool NaSela(float x, float y, float dobra) =>
        x >= 54f && x <= 68f && y >= Topo(x, dobra) - 3.2f - Mathf.Sin((x - 54f) / 14f * Mathf.PI) * 1.3f;

    // Cicatrizes: três arranhões de garra no flanco e um perto do rabo.
    static bool NaCicatriz(float x, float y, out bool clara)
    {
        clara = false;
        var p = new Vector2(x, y);
        Vector2[,] riscos =
        {
            { new Vector2(35f, 25f), new Vector2(41f, 17f) },
            { new Vector2(38.5f, 26f), new Vector2(44.5f, 18f) },
            { new Vector2(42f, 26f), new Vector2(47f, 19.5f) },
            { new Vector2(66f, 22f), new Vector2(73f, 19f) },
        };
        for (int i = 0; i < riscos.GetLength(0); i++)
        {
            float d = DistanciaAoSegmento(p, riscos[i, 0], riscos[i, 1], out float t);
            if (d < 0.6f && t > 0.04f && t < 0.96f)
            {
                clara = t < 0.5f;
                return true;
            }
        }
        return false;
    }

    static Color32[,] PintarOrca(Pose pose)
    {
        float dobra = Dobra(pose.rabo);
        var parte = new int[Largura, Altura];
        var ondeNoChifre = new float[Largura, Altura];
        var ladoNoChifre = new float[Largura, Altura];

        // 1) a máscara: de trás para a frente (a peitoral e o chifre ficam NA FRENTE do corpo)
        for (int x = 0; x < Largura; x++)
            for (int y = 0; y < Altura; y++)
            {
                if (NoTronco(x, y, pose) || NaDorsal(x, y) || NoRabo(x, y, dobra)) parte[x, y] = Silhueta;
                if (NaPeitoral(x, y, dobra)) parte[x, y] = Peitoral;
                if (NoChifre(x, y, out float onde, out float lado))
                {
                    parte[x, y] = Chifre;
                    ondeNoChifre[x, y] = onde;
                    ladoNoChifre[x, y] = lado;
                }
            }

        int Parte(int x, int y) => x < 0 || y < 0 || x >= Largura || y >= Altura ? Vazio : parte[x, y];
        // borda = vizinho (em cima, embaixo, dos lados) é de outra parte
        bool Borda(int x, int y)
        {
            int p = parte[x, y];
            return Parte(x + 1, y) != p || Parte(x - 1, y) != p || Parte(x, y + 1) != p || Parte(x, y - 1) != p;
        }
        // a peitoral encostando no corpo não ganha contorno "de fora" do lado de cima (fica grudada)
        bool BordaDaPeitoral(int x, int y) =>
            Parte(x + 1, y) == Vazio || Parte(x - 1, y) == Vazio || Parte(x, y - 1) == Vazio || Parte(x, y + 1) == Vazio
            || Parte(x + 1, y) == Silhueta || Parte(x, y - 1) == Silhueta || Parte(x - 1, y) == Silhueta;

        var tela = new Color32[Largura, Altura];

        // 2) pinta cada pixel
        for (int x = 0; x < Largura; x++)
            for (int y = 0; y < Altura; y++)
            {
                int p = parte[x, y];
                if (p == Vazio)
                {
                    tela[x, y] = FabricaDeSprites.Transparente;
                    continue;
                }
                if (p == Chifre)
                {
                    tela[x, y] = Borda(x, y) ? Contorno : CorDoChifre(ondeNoChifre[x, y], ladoNoChifre[x, y], pose.chifreAceso);
                    continue;
                }
                if (p == Peitoral)
                {
                    if (BordaDaPeitoral(x, y)) tela[x, y] = Contorno;
                    else tela[x, y] = Parte(x, y + 2) != Peitoral ? CorpoLuz : (Parte(x, y - 2) == Peitoral ? Corpo : CorpoSombra);
                    continue;
                }

                // silhueta (tronco + dorsal + rabo)
                if (Borda(x, y) && Parte(x, y + 1) != Chifre && Parte(x, y - 1) != Peitoral && !(Parte(x + 1, y) == Peitoral || Parte(x - 1, y) == Peitoral))
                {
                    tela[x, y] = Contorno;
                    continue;
                }
                if (Borda(x, y) && (Parte(x, y + 1) == Vazio || Parte(x, y - 1) == Vazio || Parte(x + 1, y) == Vazio || Parte(x - 1, y) == Vazio))
                {
                    tela[x, y] = Contorno;
                    continue;
                }
                tela[x, y] = CorDoCorpo(x, y, pose, dobra, Parte(x, y + 1) == Vazio || Parte(x, y + 2) == Vazio && Parte(x, y + 1) != Silhueta);
            }

        // 3) detalhes por cima: boca, dentes, olho, cicatrizes
        PintarBoca(tela, pose, parte);
        PintarOlho(tela, pose);

        // 4) por fora: aura do chifre aceso, brilho do olho e névoa
        for (int x = 0; x < Largura; x++)
            for (int y = 0; y < Altura; y++)
            {
                if (parte[x, y] != Vazio) continue;
                if (pose.chifreAceso && PertoDe(parte, x, y, Chifre, 2)) tela[x, y] = AuraRoxa;
                else if (Nevoa(x, y, pose.rabo, parte)) tela[x, y] = new Color32(222, 214, 255, 70);
            }
        return tela;
    }

    static bool PertoDe(int[,] parte, int x, int y, int qual, int raio)
    {
        for (int dx = -raio; dx <= raio; dx++)
            for (int dy = -raio; dy <= raio; dy++)
            {
                int px = x + dx, py = y + dy;
                if (px < 0 || py < 0 || px >= Largura || py >= Altura) continue;
                if (dx * dx + dy * dy <= raio * raio && parte[px, py] == qual) return true;
            }
        return false;
    }

    // Cor de um pixel do corpo (sem contar o contorno): branco, mancha, sela, ou o preto-azulado com luz e sombra.
    static Color32 CorDoCorpo(int x, int y, Pose pose, float dobra, bool pertoDoTopo)
    {
        if (NaCicatriz(x, y, out bool clara))
            return NoBranco(x, y, dobra) ? CicatrizNoBranco : (clara ? CicatrizClara : Cicatriz);

        float topo = Topo(x, dobra), baixo = Baixo(x, dobra, pose.boca);
        bool tronco = x >= 2 && x <= 84 && y <= topo && y >= baixo;

        if (NaManchaDoOlho(x, y)) return y < 21.5f && (x + y) % 2 == 0 && x > 27 ? BrancoSombra : Branco;
        if (tronco && NoBranco(x, y, dobra))
        {
            // sombra na parte de baixo da barriga (o corpo é redondo)
            float alturaNaBarriga = y - baixo;
            if (alturaNaBarriga < 1.5f) return BrancoSombraForte;
            if (alturaNaBarriga < 3f) return (x + y) % 2 == 0 ? BrancoSombra : Branco;
            return Branco;
        }
        if (tronco && NaSela(x, y, dobra)) return y > topo - 1.5f ? SelaLuz : Sela;

        // preto-azulado: brilho na "testa" redonda, luz em cima, sombra embaixo
        if (x >= 5 && x <= 13 && Mathf.Abs(y - (Topo(x, dobra) - 1.8f)) < 0.6f) return CorpoBrilho;
        if (pertoDoTopo) return CorpoLuz;
        float distanciaDoTopo = (tronco ? topo : 39f) - y;
        float distanciaDeBaixo = tronco ? y - baixo : 99f;
        if (distanciaDoTopo < 2.5f) return CorpoLuz;
        if (distanciaDoTopo < 4f) return (x + y) % 2 == 0 ? CorpoLuz : Corpo;
        if (distanciaDeBaixo < 2f) return CorpoSombra;
        return Corpo;
    }

    static Color32 CorDoChifre(float onde, float lado, bool aceso)
    {
        // listras inclinadas = a espiral do chifre (a listra "anda" conforme o lado do eixo)
        int listra = Mathf.FloorToInt(onde * 9f + lado * 0.55f);
        bool sulco = listra % 2 != 0;
        bool iluminado = lado > 0.6f;   // o lado de cima pega a luz
        bool sombra = lado < -1.1f;
        if (aceso)
        {
            if (onde > 0.8f) return ChifreAceso;
            return sulco ? ChifreAcesoSombra : (iluminado ? ChifreAceso : ChifreAcesoMeio);
        }
        if (onde < 0.07f) return OssoSombra;                 // a raiz, enterrada na testa
        if (sulco) return iluminado ? Osso : OssoSombra;      // o sulco da espiral
        return iluminado ? OssoClaro : (sombra ? Osso : OssoClaro);
    }

    static void PintarBoca(Color32[,] tela, Pose pose, int[,] parte)
    {
        float dobra = Dobra(pose.rabo);
        for (int x = 2; x <= 20; x++)
        {
            int linha = Mathf.RoundToInt(LinhaDaBoca(x));
            if (!pose.boca)
            {
                // boca fechada: uma linha escura com pontinhas de dente aparecendo
                if (x <= 19 && linha >= 0 && parte[x, linha] == Silhueta) tela[x, linha] = Contorno;
                if (x >= 5 && x <= 17 && x % 4 == 1 && parte[x, linha - 1] == Silhueta) tela[x, linha - 1] = Dente;
                continue;
            }
            // boca aberta: goela vermelha entre a linha da boca e o queixo, dentes em cima e embaixo
            int queixo = Mathf.RoundToInt(Queixo(x));
            for (int y = queixo; y <= linha; y++)
            {
                if (y < 0 || y >= Altura || parte[x, y] != Silhueta) continue;
                Color32 cor = Goela;
                if (y == linha) cor = Contorno;
                else if (y == queixo) cor = Contorno;
                else if (y == linha - 1 && x % 3 != 0) cor = Dente;                 // dentes de cima
                else if (y == linha - 2 && x % 3 == 1 && x < 17) cor = Dente;
                else if (y == queixo + 1 && x % 3 != 1) cor = Dente;                // dentes de baixo
                else if (y == queixo + 2 && x > 8 && x < 18) cor = Lingua;
                tela[x, y] = cor;
            }
        }
        if (pose.tonta)
        {
            // língua de fora, caindo do canto da boca
            for (int y = 3; y <= 7; y++)
                for (int x = 10; x <= 12; x++)
                {
                    if (y < Mathf.RoundToInt(Queixo(x)) - 0 && (x != 12 || y > 4))
                        tela[x, y] = (x == 10 || y == 3 || (x == 12 && y == 5)) ? Contorno : Lingua;
                }
        }
    }

    // O olho bravo: vermelho brilhante, puxado (a parte da frente mais baixa) e com uma auréola vermelha.
    static readonly string[] DesenhoDoOlho =
    {
        "...aakkka",
        ".aakkrrrk",
        "akkrrWrrk",
        "akrrrrrka",
        ".akkkkka.",
    };

    static void PintarOlho(Color32[,] tela, Pose pose)
    {
        int ox = (int)PixelDoOlho.x, oy = (int)PixelDoOlho.y;
        if (pose.tonta)
        {
            // olho em X (girando de tontura)
            for (int i = -2; i <= 2; i++)
            {
                tela[ox + i, oy + i] = Tonta;
                tela[ox + i, oy - i] = Tonta;
            }
            return;
        }
        for (int linha = 0; linha < DesenhoDoOlho.Length; linha++)
            for (int coluna = 0; coluna < DesenhoDoOlho[linha].Length; coluna++)
            {
                char c = DesenhoDoOlho[linha][coluna];
                int x = ox - 4 + coluna, y = oy + 2 - linha;
                if (c == 'k') tela[x, y] = Contorno;
                else if (c == 'r') tela[x, y] = Olho;
                else if (c == 'W') tela[x, y] = OlhoBrilho;
                else if (c == 'a') tela[x, y] = Color32.Lerp(tela[x, y], Olho, 0.35f); // auréola
            }
    }

    // Névoa fininha em volta da barriga e do rabo (transparente, muda com o quadro do rabo).
    static bool Nevoa(int x, int y, int quadro, int[,] parte)
    {
        if ((y > 14 && x < 80) || x < 36) return false;
        float onda = Mathf.Sin(x * 0.45f + quadro * 1.6f) + Mathf.Sin(x * 0.17f - y * 0.8f + quadro * 0.9f);
        if (onda < 0.9f) return false;
        // só pertinho do corpo
        return PertoDe(parte, x, y, Silhueta, 3) && !PertoDe(parte, x, y, Silhueta, 1);
    }

    // ------------------------------------------------------------------ ataques e efeitos

    // Raio (1 pixel de comprimento, esticado pela luta): miolo branco, roxo nas bordas. 3 quadros "tremendo".
    static Color32 CorDoRaio(int y, int quadro)
    {
        float d = Mathf.Abs(y - 7.5f); // 0 no meio, 7.5 na borda
        float miolo = 1.5f + quadro * 0.8f;
        if (d <= miolo) return FabricaDeSprites.Cor('w');
        if (d <= miolo + 1.5f) return FabricaDeSprites.Cor('P');
        if (d <= miolo + 3f) return FabricaDeSprites.Cor('V');
        if (d <= miolo + 4.5f) return new Color32(92, 40, 132, 170);
        return FabricaDeSprites.Transparente;
    }

    // Linha fina de aviso (esticada; a luta pinta de vermelho ou roxo e faz piscar).
    static Color32 CorDaLinha(int y)
    {
        if (y == 2) return new Color32(255, 255, 255, 255);
        if (y == 1 || y == 3) return new Color32(255, 255, 255, 110);
        return new Color32(255, 255, 255, 40);
    }

    // Faixa de aviso da investida: vermelha transparente com as bordas mais fortes.
    static Color32 CorDaFaixa(int y)
    {
        if (y == 0 || y == 15) return new Color32(255, 70, 80, 220);
        if (y == 1 || y == 14) return new Color32(255, 110, 120, 140);
        return (y / 2) % 2 == 0 ? new Color32(255, 60, 70, 70) : new Color32(255, 60, 70, 45);
    }

    // Elo de corrente (deitado), para enfeitar os raios no estilo "correntes do julgamento".
    static Color32 CorDoElo(int x, int y)
    {
        float dx = (x - 7.5f) / 7.5f, dy = (y - 4.5f) / 4.5f;
        float d = dx * dx + dy * dy;
        float dentroX = (x - 7.5f) / 4.5f, dentroY = (y - 4.5f) / 1.6f;
        if (d > 1f) return FabricaDeSprites.Transparente;
        if (dentroX * dentroX + dentroY * dentroY <= 1f) return FabricaDeSprites.Transparente; // o furo do elo
        if (d > 0.72f) return Contorno;
        return y > 5 ? FabricaDeSprites.Cor('s') : FabricaDeSprites.Cor('S');
    }

    // Bola de névoa: redonda, roxa, com um redemoinho e olhinhos bravos (2 quadros: o redemoinho gira).
    static Color32 CorDaBola(int x, int y, int quadro)
    {
        float dx = x - 6.5f, dy = y - 6.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 6.6f) return FabricaDeSprites.Transparente;
        if (d > 5.6f) return Contorno;
        // olhos bravos: "\ /"
        if ((x == 4 && y == 7) || (x == 5 && y == 6) || (x == 9 && y == 7) || (x == 8 && y == 6)) return Contorno;
        if ((x == 3 && y == 7) || (x == 10 && y == 7)) return new Color32(255, 80, 90, 255);
        if (dy > 2.5f && dx < -0.5f && d > 3f && d < 4.6f) return FabricaDeSprites.Cor('w'); // brilho
        // redemoinho (muda com o quadro)
        float angulo = Mathf.Atan2(dy, dx) + d * 0.8f + quadro * Mathf.PI / 2f;
        if (Mathf.Sin(angulo * 2f) > 0.35f) return FabricaDeSprites.Cor('P');
        return d < 3f ? new Color32(150, 80, 200, 255) : FabricaDeSprites.Cor('V');
    }

    // Onda de choque que corre pelo chão: uma "crista" de névoa inclinada para a frente
    // (desenhada indo para a DIREITA; a luta vira com flipX). A base fica no chão.
    static Color32 CorDaOnda(int x, int y, int quadro)
    {
        float crista = quadro == 0 ? 11.5f : 10.5f;   // onde fica o ponto mais alto
        float alturaAqui;
        if (x <= crista) alturaAqui = 1f + (11f - quadro) * Mathf.Pow(Mathf.Sin(x / crista * Mathf.PI / 2f), 1.4f);
        else alturaAqui = (12f - quadro) * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((x - crista) / (15.5f - crista), 2f)));
        // a "dobra" da crista: um pouquinho de espuma caindo na frente
        if (x > crista + 1f && x < crista + 3.5f && y < alturaAqui - 2f && y > alturaAqui - 5f && (x + y) % 2 == 0)
            return FabricaDeSprites.Transparente;
        if (y > alturaAqui || alturaAqui < 0.5f) return FabricaDeSprites.Transparente;
        if (y > alturaAqui - 1f) return Contorno;
        if (y > alturaAqui - 3f) return FabricaDeSprites.Cor('w');          // espuma em cima
        if (x > crista && y > alturaAqui - 4f) return FabricaDeSprites.Cor('w'); // a frente da crista
        if ((x * 3 + y * 2 + quadro * 5) % 7 == 0) return FabricaDeSprites.Cor('w'); // bolhinhas
        return y > 4 ? FabricaDeSprites.Cor('P') : FabricaDeSprites.Cor('V');
    }

    // Brilho do olho (fica por cima de tudo: é o que você vê no escuro).
    static Color32 CorDoBrilhoDoOlho(int x, int y)
    {
        float dx = x - 4f, dy = y - 4f, d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d <= 1f) return new Color32(255, 230, 230, 255);
        if (d <= 2f) return new Color32(255, 60, 70, 255);
        if (d <= 4.3f) return new Color32(255, 40, 60, (byte)(150 * (1f - (d - 2f) / 2.3f)));
        return FabricaDeSprites.Transparente;
    }

    // Brilho redondo na ponta do chifre carregando (cresce antes do raio).
    static Color32 CorDoBrilhoDoChifre(int x, int y)
    {
        float dx = x - 11.5f, dy = y - 11.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
        bool cruz = (Mathf.Abs(dx) < 1f || Mathf.Abs(dy) < 1f) && d < 11.5f;
        if (d <= 2.5f) return FabricaDeSprites.Cor('w');
        if (d <= 4.5f) return FabricaDeSprites.Cor('P');
        if (cruz) return new Color32(226, 186, 255, (byte)(220 * (1f - d / 11.5f)));
        if (d <= 7f) return new Color32(176, 96, 230, (byte)(150 * (1f - (d - 4.5f) / 2.5f)));
        return FabricaDeSprites.Transparente;
    }

    // Marca no chão de onde algo vai cair (branca: a luta pinta de vermelho ou roxo).
    // Um anel oval com listras por dentro, que aparece bem até no escuro.
    static Color32 CorDoAlvo(int x, int y)
    {
        float ex = (x - 15.5f) / 16f, ey = (y - 3.5f) / 4f, d = ex * ex + ey * ey;
        if (d > 1f) return FabricaDeSprites.Transparente;
        if (d > 0.6f) return new Color32(255, 255, 255, 255);
        return (x + y) % 4 < 2 ? new Color32(255, 255, 255, 120) : new Color32(255, 255, 255, 60);
    }

    // Rachaduras (por cima de um bloco que vai desmoronar).
    static Color32 CorDaRachadura(int x, int y)
    {
        bool risco =
            (Mathf.Abs(y - (15 - x * 0.8f)) < 0.7f && x < 12) ||
            (Mathf.Abs(x - (8 + Mathf.Sin(y * 0.9f) * 2f)) < 0.6f) ||
            (Mathf.Abs(y - (4 + x * 0.35f)) < 0.6f && x > 7);
        return risco ? new Color32(20, 20, 28, 230) : FabricaDeSprites.Transparente;
    }

    // A ESCURIDÃO do ataque final: tudo escuro, menos um buraco redondo em volta do Subaru.
    // A borda do buraco é "pontilhada" (dithering), como nos jogos antigos.
    static Color32 CorDaEscuridao(int x, int y)
    {
        float dx = x - 127.5f, dy = y - 63.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
        const float claro = 15f, escuro = 22f;
        var cor = new Color32(8, 4, 18, 255);
        if (d >= escuro) return cor;
        if (d <= claro) return FabricaDeSprites.Transparente;
        // de 15 a 22 pixels: cada vez mais pontinhos escuros (matriz de Bayer 4x4)
        int[] bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
        float limite = (bayer[(y % 4) * 4 + (x % 4)] + 0.5f) / 16f;
        float p = (d - claro) / (escuro - claro);
        if (p > limite) return cor;
        return new Color32(8, 4, 18, (byte)(160 * p));
    }
}
