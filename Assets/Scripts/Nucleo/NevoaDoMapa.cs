using System.Collections.Generic;
using UnityEngine;

// A NÉVOA do mapa do mundo (o miasma da Bruxa), que esconde o que o Subaru ainda não descobriu.
//
// Antes eram ~2000 "bolotas" (um sprite por célula). Agora são nuvens macias pintadas numa IMAGEM só
// (na verdade 3 camadas iguais, com pequenas diferenças), metade da resolução do mapa e com filtro
// "Bilinear": a borda fica suave e redonda, sem serrilhado.
//
// Como funciona:
//  - Cada célula do mapa tem uma DENSIDADE (1 = coberta, 0 = descoberta). A imagem mistura as densidades
//    das células vizinhas (fica macia) e soma um RUÍDO (manchas aleatórias): onde passa da metade, é névoa.
//    Por isso a borda é orgânica, cheia de "rolinhos" de nuvem, e não segue os quadradinhos do mapa.
//  - Cada pixel da névoa é iluminado como uma nuvem de verdade: o lado de cima-esquerda dos rolinhos pega
//    o sol (branco-creme), o de baixo-direita fica na sombra (lilás-azulado). E a névoa faz uma sombra
//    no chão descoberto logo abaixo dela.
//  - RESPIRAR: duas camadas "extras", um pouquinho maiores que a principal, aparecem e somem devagar
//    (cada uma no seu ritmo) e andam um tiquinho: a beirada da névoa incha e murcha sozinha.
//    Isso custa só trocar a cor (transparência) de 2 sprites por quadro: nada é recalculado.
//  - REVELAR: as células descobertas perdem a densidade aos poucos (meio segundo). Só enquanto alguma
//    célula está sumindo a imagem é repintada, e só no pedaço que mudou.
public class NevoaDoMapa : MonoBehaviour
{
    const int PixelsPorCelula = 8;   // metade dos 16 do mapa: o filtro Bilinear deixa macio na tela
    const float TempoParaSumir = 0.6f;
    const float Ruido = 0.55f;       // quanto o ruído entorta a borda
    const float Inchaco = 0.07f;     // quanto as camadas que "respiram" passam da principal

    // As cores da névoa, da sombra mais funda (lilás-azulada) até a luz do sol (branco quentinho).
    static readonly Color32[] Cores =
    {
        new Color32(64, 54, 104, 255), new Color32(92, 84, 136, 255), new Color32(124, 118, 166, 255), new Color32(160, 158, 196, 255),
        new Color32(196, 196, 222, 255), new Color32(226, 226, 240, 255), new Color32(246, 242, 240, 255), new Color32(255, 250, 232, 255),
    };
    static readonly Color32 CorDaSombra = new Color32(28, 20, 66, 255);

    int largura, altura, linhasDeCeu;      // em células
    int texLargura, texAltura;             // em pixels da imagem
    float[] densidade;                     // por célula (linha 0 = a de cima do desenho)
    bool[] sumindo;
    readonly List<int> celulasSumindo = new List<int>();
    readonly List<float> inicioDoSumico = new List<float>();
    float inicioDaSombria;                 // coluna onde começa a região sombria (a névoa lá é mais escura)

    Texture2D[] texturas;
    Color32[][] pixels;
    SpriteRenderer[] camadas;
    float[] alturaDaNevoa, densidadeMacia; // buffers reaproveitados (para não alocar a cada quadro)
    float ultimaPintura = -1f;
    int sujoX0 = int.MaxValue, sujoY0 = int.MaxValue, sujoX1 = -1, sujoY1 = -1; // o pedaço que ainda falta repintar

    // O ruído de cada camada, calculado uma vez só (é sempre o mesmo mapa) e guardado.
    static float[][] ruidos;
    static int ruidoLargura, ruidoAltura;

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => ruidos = null;

    // descoberto[x, y]: true = sem névoa. ceu: quantas linhas de cima são céu (a névoa só "sobe" um pouco nelas).
    public static NevoaDoMapa Criar(Transform pai, bool[,] descoberto, int linhasDeCeu, float inicioDaSombria, int ordem)
    {
        var nevoa = new GameObject("Nevoa").AddComponent<NevoaDoMapa>();
        nevoa.transform.SetParent(pai, false);
        nevoa.Montar(descoberto, linhasDeCeu, inicioDaSombria, ordem);
        return nevoa;
    }

    void Montar(bool[,] descoberto, int ceu, float sombria, int ordem)
    {
        largura = descoberto.GetLength(0);
        altura = descoberto.GetLength(1);
        linhasDeCeu = ceu;
        inicioDaSombria = sombria;
        densidade = new float[largura * altura];
        sumindo = new bool[largura * altura];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                densidade[y * largura + x] = descoberto[x, y] || y < linhasDeCeu ? 0f : 1f;

        texLargura = largura * PixelsPorCelula;
        texAltura = altura * PixelsPorCelula;
        PrepararRuidos();
        alturaDaNevoa = new float[texLargura * texAltura];
        densidadeMacia = new float[texLargura * texAltura];

        texturas = new Texture2D[3];
        pixels = new Color32[3][];
        camadas = new SpriteRenderer[3];
        for (int k = 0; k < 3; k++)
        {
            texturas[k] = new Texture2D(texLargura, texAltura, TextureFormat.RGBA32, false)
            {
                name = "Nevoa " + k,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            pixels[k] = new Color32[texLargura * texAltura];
            var sprite = Sprite.Create(texturas[k], new Rect(0, 0, texLargura, texAltura), Vector2.zero, PixelsPorCelula, 0, SpriteMeshType.FullRect);
            var objeto = new GameObject(k == 0 ? "Nevoa" : "Nevoa respirando");
            objeto.transform.SetParent(transform, false);
            objeto.transform.localPosition = new Vector3(-0.5f, -0.5f, 0f); // a célula (0, 0) tem o centro em (0, 0)
            camadas[k] = objeto.AddComponent<SpriteRenderer>();
            camadas[k].sprite = sprite;
            camadas[k].sortingOrder = ordem + (k == 0 ? 0 : k);
        }
        Pintar(0, 0, largura - 1, altura - 1);
    }

    void OnDestroy()
    {
        // imagens criadas por código não somem sozinhas: sem isso, cada visita ao mapa vazaria memória
        if (camadas != null)
            foreach (SpriteRenderer c in camadas)
                if (c != null && c.sprite != null) Destroy(c.sprite);
        if (texturas != null)
            foreach (Texture2D t in texturas)
                if (t != null) Destroy(t);
    }

    // Três ruídos diferentes (um por camada), do tamanho da imagem.
    void PrepararRuidos()
    {
        if (ruidos != null && ruidoLargura == texLargura && ruidoAltura == texAltura) return;
        ruidoLargura = texLargura;
        ruidoAltura = texAltura;
        ruidos = new float[3][];
        for (int k = 0; k < 3; k++)
        {
            var r = new float[texLargura * texAltura];
            for (int y = 0; y < texAltura; y++)
                for (int x = 0; x < texLargura; x++)
                    r[y * texLargura + x] = Fractal(x, y, k * 7 + 501);
            ruidos[k] = r;
        }
    }

    // Ruído "de nuvem": rolinhos grandes + médios + pequenos (o mesmo truque do chão do mapa).
    static float Fractal(int x, int y, int semente)
    {
        return 0.5f * RuidoSuave(x / 13f, y / 11f, semente) + 0.32f * RuidoSuave(x / 5.5f, y / 5f, semente + 1)
             + 0.18f * RuidoSuave(x / 2.4f, y / 2.4f, semente + 2);
    }

    static float RuidoSuave(float x, float y, int semente)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float tx = x - x0, ty = y - y0;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        float a = Mathf.Lerp(ArteDosBlocos.Sorteio(x0, y0, semente), ArteDosBlocos.Sorteio(x0 + 1, y0, semente), tx);
        float b = Mathf.Lerp(ArteDosBlocos.Sorteio(x0, y0 + 1, semente), ArteDosBlocos.Sorteio(x0 + 1, y0 + 1, semente), tx);
        return Mathf.Lerp(a, b, ty);
    }

    // A densidade de uma célula (fora do mapa: a da beirada). Nas linhas de céu, a névoa da linha de
    // baixo "sobe" um pouco e vai sumindo: parece um banco de nuvens no horizonte.
    float Densidade(int x, int y)
    {
        x = Mathf.Clamp(x, 0, largura - 1);
        y = Mathf.Clamp(y, 0, altura - 1);
        if (y < linhasDeCeu)
        {
            int falta = linhasDeCeu - y; // 1 = a linha de céu logo acima da terra
            return densidade[linhasDeCeu * largura + x] * (falta == 1 ? 0.62f : falta == 2 ? 0.25f : 0f);
        }
        return densidade[y * largura + x];
    }

    // A densidade num ponto qualquer da imagem (mistura suave das 4 células mais perto).
    float DensidadeEm(float u, float v)
    {
        // u, v em células, com (0, 0) no centro da célula de baixo-esquerda; v cresce para cima
        int x0 = Mathf.FloorToInt(u), v0 = Mathf.FloorToInt(v);
        float tx = u - x0, ty = v - v0;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        float a = Mathf.Lerp(Densidade(x0, altura - 1 - v0), Densidade(x0 + 1, altura - 1 - v0), tx);
        float b = Mathf.Lerp(Densidade(x0, altura - 2 - v0), Densidade(x0 + 1, altura - 2 - v0), tx);
        return Mathf.Lerp(a, b, ty);
    }

    // Repinta as 3 camadas no retângulo de células (x0, y0) a (x1, y1) (com uma folga em volta).
    void Pintar(int x0, int y0, int x1, int y1)
    {
        const int Folga = 2;
        int px0 = Mathf.Max(0, (x0 - Folga) * PixelsPorCelula), px1 = Mathf.Min(texLargura - 1, (x1 + Folga + 1) * PixelsPorCelula - 1);
        // (na imagem, y = 0 é embaixo; no desenho do mapa, a linha 0 é a de cima)
        int py0 = Mathf.Max(0, (altura - 1 - (y1 + Folga)) * PixelsPorCelula), py1 = Mathf.Min(texAltura - 1, (altura - (y0 - Folga)) * PixelsPorCelula - 1);
        const float Passo = 1f / PixelsPorCelula;

        // 0) a densidade "macia" de cada pixel (a mesma para as 3 camadas), com 4 pixels a mais em volta
        int ex0 = Mathf.Max(0, px0 - 4), ex1 = Mathf.Min(texLargura - 1, px1 + 4);
        int ey0 = Mathf.Max(0, py0 - 4), ey1 = Mathf.Min(texAltura - 1, py1 + 4);
        for (int py = ey0; py <= ey1; py++)
        {
            float v = (py + 0.5f) * Passo - 0.5f;
            for (int px = ex0; px <= ex1; px++)
                densidadeMacia[py * texLargura + px] = DensidadeEm((px + 0.5f) * Passo - 0.5f, v);
        }

        for (int k = 0; k < 3; k++)
        {
            float[] ruido = ruidos[k];
            Color32[] saida = pixels[k];
            float extra = k == 0 ? 0f : Inchaco;
            // 1) a "altura" da nuvem em cada pixel: densidade + ruído (com um pixel a mais em volta, para a luz)
            for (int py = Mathf.Max(0, py0 - 1); py <= Mathf.Min(texAltura - 1, py1 + 1); py++)
                for (int px = Mathf.Max(0, px0 - 1); px <= Mathf.Min(texLargura - 1, px1 + 1); px++)
                {
                    int i = py * texLargura + px;
                    alturaDaNevoa[i] = densidadeMacia[i] + (ruido[i] - 0.5f) * Ruido + extra;
                }
            // 2) a cor
            for (int py = py0; py <= py1; py++)
                for (int px = px0; px <= px1; px++)
                {
                    int i = py * texLargura + px;
                    float h = alturaDaNevoa[i];
                    float alfa = Mathf.Clamp01((h - 0.4f) / 0.2f);
                    alfa = alfa * alfa * (3f - 2f * alfa);
                    float sombra = 0f;
                    if (k == 0)
                    {
                        // a sombra que a névoa faz no chão: a névoa "deslocada" para baixo-direita
                        int j = Mathf.Min(texAltura - 1, py + 3) * texLargura + Mathf.Max(0, px - 3);
                        float hs = densidadeMacia[j] + (ruido[j] - 0.5f) * Ruido;
                        sombra = Mathf.Clamp01((hs - 0.35f) / 0.2f) * 0.38f;
                    }
                    if (alfa <= 0.003f && sombra <= 0.003f) { saida[i] = new Color32(0, 0, 0, 0); continue; }

                    // luz de cima-esquerda: onde a nuvem "sobe" para a direita e para baixo, a face olha para o sol
                    float hx = alturaDaNevoa[py * texLargura + Mathf.Min(texLargura - 1, px + 1)] - alturaDaNevoa[py * texLargura + Mathf.Max(0, px - 1)];
                    float hy = alturaDaNevoa[Mathf.Min(texAltura - 1, py + 1) * texLargura + px] - alturaDaNevoa[Mathf.Max(0, py - 1) * texLargura + px];
                    float luz = 0.62f + (0.62f * hx - 0.78f * hy) * 2.2f + (h - 0.5f) * 0.25f + (k == 0 ? 0f : 0.06f);
                    // na beirada a nuvem é mais fina (mais clara); no miolo, mais funda
                    luz += (1f - alfa) * 0.15f;
                    float x = (px + 0.5f) * Passo;
                    float escura = Mathf.Clamp01((x - inicioDaSombria) / 6f); // na região sombria o miasma é mais escuro
                    luz -= escura * 0.3f;
                    float limiar = (ArteDosBlocos.Bayer[(py & 3) * 4 + (px & 3)] + 0.5f) / 16f - 0.5f;
                    int tom = Mathf.Clamp(Mathf.RoundToInt(luz * (Cores.Length - 1) + limiar * 0.7f), 0, Cores.Length - 1);
                    Color32 c = Cores[tom];
                    if (escura > 0f) c = Color32.Lerp(c, new Color32(84, 52, 120, 255), escura * 0.35f);

                    // junta a nuvem com a sombra embaixo dela
                    float total = alfa + sombra * (1f - alfa);
                    float pesoDaNuvem = alfa / total;
                    saida[i] = new Color32(
                        (byte)(c.r * pesoDaNuvem + CorDaSombra.r * (1f - pesoDaNuvem)),
                        (byte)(c.g * pesoDaNuvem + CorDaSombra.g * (1f - pesoDaNuvem)),
                        (byte)(c.b * pesoDaNuvem + CorDaSombra.b * (1f - pesoDaNuvem)),
                        (byte)(total * 255f));
                }
            texturas[k].SetPixels32(saida);
            texturas[k].Apply();
        }
        ultimaPintura = Time.time;
    }

    // A névoa em volta de "centro" começa a sumir (só olha o quadrado em volta do centro).
    public void Revelar(Vector2Int centro, float raio)
    {
        int r = Mathf.CeilToInt(raio);
        for (int y = Mathf.Max(linhasDeCeu, centro.y - r); y <= Mathf.Min(altura - 1, centro.y + r); y++)
            for (int x = Mathf.Max(0, centro.x - r); x <= Mathf.Min(largura - 1, centro.x + r); x++)
            {
                int i = y * largura + x;
                if (densidade[i] <= 0f || sumindo[i] || Vector2Int.Distance(new Vector2Int(x, y), centro) > raio) continue;
                sumindo[i] = true;
                celulasSumindo.Add(i);
                inicioDoSumico.Add(Time.time);
            }
    }

    void Update()
    {
        // 1) as camadas extras "respiram": aparecem e somem devagar, cada uma no seu ritmo, e andam um tiquinho
        float t = Time.time;
        for (int k = 1; k < 3; k++)
        {
            float onda = 0.5f + 0.5f * Mathf.Sin(t * (k == 1 ? 0.55f : 0.37f) + k * 2.1f);
            camadas[k].color = new Color(1f, 1f, 1f, onda);
            camadas[k].transform.localPosition = new Vector3(-0.5f + Mathf.Sin(t * 0.21f + k) * 0.12f, -0.5f + Mathf.Sin(t * 0.17f + k * 1.7f) * 0.06f, 0f);
        }

        // 2) as células que estão sumindo
        if (celulasSumindo.Count == 0 && sujoX1 < 0) return;
        for (int n = celulasSumindo.Count - 1; n >= 0; n--)
        {
            int i = celulasSumindo[n];
            float p = Mathf.Clamp01((t - inicioDoSumico[n]) / TempoParaSumir);
            densidade[i] = 1f - p * p * (3f - 2f * p);
            int x = i % largura, y = i / largura;
            sujoX0 = Mathf.Min(sujoX0, x); sujoX1 = Mathf.Max(sujoX1, x); sujoY0 = Mathf.Min(sujoY0, y); sujoY1 = Mathf.Max(sujoY1, y);
            if (p >= 1f)
            {
                // (tira da lista trocando pelo último: não precisa empurrar o resto)
                int ultimo = celulasSumindo.Count - 1;
                celulasSumindo[n] = celulasSumindo[ultimo];
                inicioDoSumico[n] = inicioDoSumico[ultimo];
                celulasSumindo.RemoveAt(ultimo);
                inicioDoSumico.RemoveAt(ultimo);
            }
        }
        // Um pedaço grande (a onda do fim) repinta no máximo 20 vezes por segundo: fica leve e ainda suave.
        int area = (sujoX1 - sujoX0 + 1) * (sujoY1 - sujoY0 + 1);
        if (area > 300 && t - ultimaPintura < 0.05f && celulasSumindo.Count > 0) return;
        Pintar(sujoX0, sujoY0, sujoX1, sujoY1);
        sujoX0 = sujoY0 = int.MaxValue;
        sujoX1 = sujoY1 = -1;
    }
}
