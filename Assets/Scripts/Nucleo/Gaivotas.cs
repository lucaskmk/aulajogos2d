using UnityEngine;

// GAIVOTAS: bandos voando pelo céu em três distâncias, como nos Castlevania novos.
//   - longe: pequenas, claras e azuladas (somem no ar), devagar, atrás das ilhas;
//   - no meio: um pouco maiores, entre o galeão e as palmeiras;
//   - perto: uma ou duas ENORMES e escuras (contra a luz), passando rápido NA FRENTE da fase, só no alto da tela.
// Cada distância anda num "fator" de paralaxe diferente (veja Paralaxe): é isso que dá a profundidade.
// Elas voam para a direita (com o vento), batem as asas de vez em quando e planam no resto do tempo.
[DefaultExecutionOrder(101)] // depois da câmera e da Paralaxe
public class Gaivotas : MonoBehaviour
{
    // Asas para cima e asas para baixo. '.' = transparente, 'w' = branco, 'g' = ponta cinza, 'o' = bico.
    static readonly string[] AsasEmCima =
    {
        "g.............g",
        "wg...........gw",
        ".ww.........ww.",
        "..www.....www..",
        "....wwwwwwwo...",
        "......www......",
    };
    static readonly string[] AsasEmBaixo =
    {
        "...............",
        "...............",
        "...wwwwwwwwo...",
        "..wwwwwwwww....",
        ".ww.........ww.",
        "g.............g",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        FabricaDeSprites.Registrar("gaivota_0", () => Desenhar(AsasEmCima));
        FabricaDeSprites.Registrar("gaivota_1", () => Desenhar(AsasEmBaixo));
    }

    static Sprite Desenhar(string[] arte)
    {
        int altura = arte.Length, largura = arte[0].Length;
        return FabricaDeSprites.Procedural(largura, altura, FabricaDeSprites.Centro, (x, y) =>
        {
            switch (arte[altura - 1 - y][x]) // a arte é escrita de cima para baixo; a textura começa embaixo
            {
                case 'w': return new Color32(246, 246, 250, 255);
                case 'g': return new Color32(92, 96, 112, 255);
                case 'o': return new Color32(240, 160, 60, 255);
                default: return new Color32(0, 0, 0, 0);
            }
        });
    }

    struct Distancia
    {
        public int quantas;
        public float tamanho, velocidade, fator, alturaMin, alturaMax;
        public int ordem;
        public Color cor;
    }

    class Gaivota
    {
        public Transform corpo;
        public SpriteRenderer desenho;
        public Distancia d;
        public float x, y, fase, velocidade;
    }

    Gaivota[] bando;

    public static Gaivotas Criar(Transform raiz, Clima clima)
    {
        if (clima != Clima.Dia) return null; // de noite, nas cabines e no escuro não tem gaivota

        var objeto = new GameObject("Gaivotas");
        objeto.transform.SetParent(raiz, false);
        var gaivotas = objeto.AddComponent<Gaivotas>();
        gaivotas.Montar();
        return gaivotas;
    }

    void Montar()
    {
        Distancia[] distancias =
        {
            new Distancia { quantas = 6, tamanho = 0.45f, velocidade = 1.1f, fator = 0.9f, alturaMin = 9f, alturaMax = 14f,
                            ordem = -26, cor = new Color(0.78f, 0.84f, 0.97f, 0.9f) },
            new Distancia { quantas = 3, tamanho = 0.9f, velocidade = 2f, fator = 0.55f, alturaMin = 10.5f, alturaMax = 15f,
                            ordem = -21, cor = new Color(0.95f, 0.96f, 1f) },
            new Distancia { quantas = 1, tamanho = 2.6f, velocidade = 6f, fator = -0.6f, alturaMin = 14f, alturaMax = 15.5f,
                            ordem = 41, cor = new Color(0.12f, 0.1f, 0.2f, 0.85f) },
        };
        int total = 0;
        foreach (var d in distancias) total += d.quantas;
        bando = new Gaivota[total];

        float meiaLargura = MeiaLargura();
        int i = 0;
        foreach (var d in distancias)
            for (int k = 0; k < d.quantas; k++)
            {
                var g = new Gaivota { d = d };
                var objeto = new GameObject("Gaivota");
                objeto.transform.SetParent(transform, false);
                g.corpo = objeto.transform;
                g.corpo.localScale = Vector3.one * d.tamanho;
                g.desenho = objeto.AddComponent<SpriteRenderer>();
                g.desenho.sprite = FabricaDeSprites.Pegar("gaivota_0");
                g.desenho.color = d.cor;
                g.desenho.sortingOrder = d.ordem;
                // as de longe e do meio já começam espalhadas pela tela; a de perto começa fora dela
                Reposicionar(g, d.fator < 0f ? -meiaLargura - Random.Range(10f, 40f) : Random.Range(-meiaLargura, meiaLargura));
                bando[i++] = g;
            }
    }

    static float MeiaLargura()
    {
        Camera cam = Camera.main;
        return cam != null ? cam.orthographicSize * cam.aspect : 16f;
    }

    // "naTela": onde ela aparece, em relação ao meio da tela
    void Reposicionar(Gaivota g, float naTela)
    {
        Camera cam = Camera.main;
        float camX = cam != null ? cam.transform.position.x : 0f;
        g.x = camX * (1f - g.d.fator) + naTela;
        g.y = Random.Range(g.d.alturaMin, g.d.alturaMax);
        g.fase = Random.Range(0f, 10f);
        g.velocidade = g.d.velocidade * Random.Range(0.8f, 1.2f);
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || bando == null) return;
        float camX = cam.transform.position.x;
        float meiaLargura = MeiaLargura();
        float t = Time.time;

        foreach (var g in bando)
        {
            g.x += g.velocidade * Time.deltaTime;
            float naTela = camX * g.d.fator + g.x - camX;
            float margem = 2f * g.d.tamanho;
            // saiu pela direita (ou ficou muito para trás quando a câmera andou): volta pela esquerda
            if (naTela > meiaLargura + margem) Reposicionar(g, -meiaLargura - margem - (g.d.fator < 0f ? Random.Range(15f, 45f) : Random.Range(0f, 6f)));
            else if (naTela < -meiaLargura - 60f) Reposicionar(g, -meiaLargura - margem);

            // bate as asas uns 2 segundos, plana uns 2 segundos; sobe e desce de leve
            float ciclo = (t + g.fase) % 4f;
            bool batendo = ciclo < 2f && Mathf.Sin((t + g.fase) * 14f) > 0f;
            g.desenho.sprite = FabricaDeSprites.Pegar(batendo ? "gaivota_1" : "gaivota_0");
            float onda = Mathf.Sin((t + g.fase) * 1.3f) * 0.35f * g.d.tamanho;
            g.corpo.position = new Vector3(camX * g.d.fator + g.x, g.y + onda, 0f);
        }
    }
}
