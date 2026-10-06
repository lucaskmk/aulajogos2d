using UnityEngine;

// Fundo em camadas, estilo Castlevania: cada camada anda numa velocidade diferente
// quando a câmera se move, e isso dá a impressão de profundidade (parece 3D, mas é 2D).
//
// "fator" diz o quanto a camada acompanha a câmera:
//   1 = fica parada na tela (infinitamente longe: o céu, o sol);
//   0 = anda junto com a fase (está "colada" no chão);
//   negativo = está NA FRENTE da fase (passa mais rápido que o chão: a folhagem perto da câmera).
//
// Do fundo para a frente: céu em degradê -> sol/lua e raios de luz -> montanhas -> castelo ->
// floresta -> árvores -> neblina -> (a fase) -> folhagem da frente.
[DefaultExecutionOrder(100)] // roda depois da CameraSeguir, para não tremer
public class Paralaxe : MonoBehaviour
{
    public float fator;

    static readonly Color Escuro = new Color(0.15f, 0.1f, 0.3f);

    // As camadas são pintadas com a cor de fundo da fase: as distantes puxam para o claro (ar, "neblina"),
    // as próximas para o escuro. "noite" deixa tudo mais escuro, com lua no lugar do sol.
    public static void Criar(Transform raiz, int larguraFase, Color corDoFundo, bool noite = false)
    {
        Color claro = noite ? new Color(0.45f, 0.45f, 0.75f) : Color.white;

        // céu em degradê: mais forte em cima, mais claro perto do horizonte
        Color topo = noite ? Color.Lerp(corDoFundo, Color.black, 0.45f) : Color.Lerp(corDoFundo, Escuro, 0.12f);
        Color horizonte = Color.Lerp(corDoFundo, claro, noite ? 0.25f : 0.45f);
        Faixa(raiz, "Ceu", FabricaDeSprites.Degrade(topo, horizonte), 1f, -30, Color.white, -1f, 17f);

        SolOuLua(raiz, noite);

        Camada(raiz, "fundo_montanhas", 0.93f, -27, Color.Lerp(corDoFundo, claro, noite ? 0.1f : 0.35f), larguraFase, 3f);
        // (as ordens pulam o -23 de propósito: é onde a Baleia Branca nada no fundo)
        Camada(raiz, "fundo_castelo", 0.85f, -24, Color.Lerp(corDoFundo, Escuro, noite ? 0.35f : 0.15f), larguraFase);
        Camada(raiz, "fundo_floresta", 0.6f, -22, Color.Lerp(corDoFundo, Escuro, noite ? 0.5f : 0.3f), larguraFase);
        Camada(raiz, "fundo_arvores", 0.35f, -20, Color.Lerp(corDoFundo, Escuro, noite ? 0.65f : 0.45f), larguraFase);

        // neblina baixinha, entre o fundo e a fase
        Faixa(raiz, "Neblina", FabricaDeSprites.Pegar("neblina"), 1f, -19, new Color(claro.r, claro.g, claro.b, 0.35f), -0.5f, 3.5f);

        // folhagem bem perto da câmera, escura, só na beirada de baixo da tela (moitas redondas:
        // nada pontudo, para ninguém confundir com espinho)
        Camada(raiz, "frente_folhas", -0.4f, 40, Color.Lerp(corDoFundo, Escuro, 0.8f) * new Color(1f, 1f, 1f, 0.8f), larguraFase, -1.3f);
    }

    // Versão pequena, para o horizonte do mapa do mundo: só a floresta e as árvores, encolhidas,
    // com a base em "baseY". Quanto mais longe, mais a cor puxa para a do céu (parece neblina).
    public static void CriarHorizonte(Transform raiz, int largura, float baseY, float escala, Color corDoCeu, Color corDoChao)
    {
        Camada(raiz, "fundo_floresta", 0.7f, -24, Color.Lerp(corDoCeu, Escuro, 0.25f), largura, baseY, escala);
        Camada(raiz, "fundo_arvores", 0.45f, -22, Color.Lerp(corDoChao, Escuro, 0.2f), largura, baseY - 0.1f, escala);
    }

    static Paralaxe NovaCamada(Transform raiz, string nome, float fator)
    {
        var objeto = new GameObject("Paralaxe " + nome);
        objeto.transform.SetParent(raiz, false);
        var camada = objeto.AddComponent<Paralaxe>();
        camada.fator = fator;
        return camada;
    }

    static void Camada(Transform raiz, string sprite, float fator, int ordem, Color cor, int larguraFase, float baseY = -1f, float escala = 1f)
    {
        Transform camada = NovaCamada(raiz, sprite, fator).transform;

        // Repete a imagem lado a lado com folga, para cobrir a tela em qualquer ponto da fase.
        // (camada da frente anda mais que a câmera, então precisa de mais pedaços)
        Sprite desenho = FabricaDeSprites.Pegar(sprite);
        float largura = desenho.bounds.size.x * escala;
        float ate = larguraFase * Mathf.Max(1f, 1f - fator) + 20f + largura;
        for (float x = -20f - largura; x < ate; x += largura)
        {
            var pedaco = new GameObject("Pedaco");
            pedaco.transform.SetParent(camada, false);
            pedaco.transform.localPosition = new Vector3(x, baseY, 0f); // nas fases, a base fica escondida atrás do chão
            pedaco.transform.localScale = Vector3.one * escala;
            var visual = pedaco.AddComponent<SpriteRenderer>();
            visual.sprite = desenho;
            visual.color = cor;
            visual.sortingOrder = ordem;
        }
    }

    // Uma faixa que acompanha a câmera e cobre a tela toda na horizontal (céu, neblina).
    static void Faixa(Transform raiz, string nome, Sprite desenho, float fator, int ordem, Color cor, float baseY, float altura)
    {
        Transform camada = NovaCamada(raiz, nome, fator).transform;
        var faixa = new GameObject(nome);
        faixa.transform.SetParent(camada, false);
        faixa.transform.localPosition = new Vector3(0f, baseY, 0f);
        Vector3 tamanho = desenho.bounds.size;
        faixa.transform.localScale = new Vector3(60f / tamanho.x, altura / tamanho.y, 1f);
        var visual = faixa.AddComponent<SpriteRenderer>();
        visual.sprite = desenho;
        visual.color = cor;
        visual.sortingOrder = ordem;
    }

    // Sol com raios de luz (de dia) ou lua com estrelas (de noite), lá longe, no canto de cima.
    static void SolOuLua(Transform raiz, bool noite)
    {
        Transform ceu = NovaCamada(raiz, "SolOuLua", 0.97f).transform;
        var astro = ConstrutorDeFase.Visual(noite ? "Lua" : "Sol", ceu, Vector3.zero, noite ? "lua" : "sol", -29, false);
        astro.transform.localPosition = new Vector3(8f, 11.3f, 0f);
        astro.transform.localScale = Vector3.one * 1.4f;

        if (noite)
        {
            var sorteio = new System.Random(9);
            for (int i = 0; i < 40; i++)
            {
                var estrela = ConstrutorDeFase.Visual("Estrela", ceu, Vector3.zero, "brilho", -29, false);
                estrela.transform.localPosition = new Vector3(sorteio.Next(-40, 160), 6f + (float)sorteio.NextDouble() * 9f, 0f);
                estrela.transform.localScale = Vector3.one * (0.3f + (float)sorteio.NextDouble() * 0.5f);
                Animacao.Adicionar(estrela, Animacao.Tipo.Piscar, 2f + (float)sorteio.NextDouble() * 3f, 0.8f);
            }
            return;
        }

        // raios de luz descendo do sol, pulsando devagar
        float[] angulos = { -18f, -32f, -48f };
        for (int i = 0; i < angulos.Length; i++)
        {
            var raio = ConstrutorDeFase.Visual("RaioDeLuz", ceu, Vector3.zero, "raio_de_luz", -26, false);
            raio.transform.localPosition = new Vector3(8f, 11.3f, 0f);
            raio.transform.localRotation = Quaternion.Euler(0f, 0f, angulos[i]);
            raio.transform.localScale = new Vector3(1.6f + i * 0.4f, 1.6f, 1f);
            raio.AddComponent<Pulsar>().fase = i * 1.7f;
        }
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        transform.position = new Vector3(cam.transform.position.x * fator, 0f, 0f);
    }
}
