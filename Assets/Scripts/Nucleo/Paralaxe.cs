using UnityEngine;
using UnityEngine.Rendering.Universal;

// Fundo em camadas, estilo Castlevania: cada camada anda numa velocidade diferente
// quando a câmera se move, e isso dá a impressão de profundidade (parece 3D, mas é 2D).
//
// "fator" diz o quanto a camada acompanha a câmera:
//   1 = fica parada na tela (infinitamente longe: o céu, o sol);
//   0 = anda junto com a fase (está "colada" no chão);
//   negativo = está NA FRENTE da fase (passa mais rápido que o chão: a folhagem perto da câmera).
//
// Do fundo para a frente: céu em degradê -> sol/lua e raios de luz -> ilhas e o vulcão lá longe ->
// o galeão no horizonte -> palmeiras -> o mato da praia -> neblina -> (a fase) -> folhagem da frente.
[DefaultExecutionOrder(100)] // roda depois da CameraSeguir, para não tremer
public class Paralaxe : MonoBehaviour
{
    public float fator;

    static readonly Color Escuro = new Color(0.15f, 0.1f, 0.3f);

    // O fundo da fase atual fica todo dentro de um objeto só ("Cenario de fundo"): assim dá para
    // apagar e montar de novo quando o jogador troca os gráficos (HD-2D / Simples) no menu de pausa.
    static Transform fundoAtual;
    static int ultimaLargura;
    static Color ultimaCor;
    static bool ultimaNoite, ultimoHD;
    static Clima ultimoClima;

    // A largura da fase atual (em blocos): os efeitos de luz usam para saber a "hora do dia" (LuzDoDia).
    public static int LarguraDaFase => ultimaLargura;

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => fundoAtual = null;

    // O clima da fase: escura > noite > cabines (tem portas) > dia.
    public static Clima ClimaDa(Fase dados)
    {
        if (dados == null) return Clima.Dia;
        if (dados.escura) return Clima.Escuro;
        if (dados.noite) return Clima.Noite;
        if (dados.portas != null) return Clima.Cabines;
        return Clima.Dia;
    }

    static Clima ClimaAtual()
    {
        var jogo = GerenciadorDoJogo.Instancia;
        return jogo != null ? ClimaDa(jogo.DadosDaFase) : Clima.Dia;
    }

    // Quando o Grumete morre, a fase INTEIRA é destruída e montada de novo (GerenciadorDoJogo.CarregarFase).
    // O fundo é igualzinho ao de antes, então não precisa ser refeito: Soltar() tira ele de dentro da fase
    // que vai ser destruída, e o Criar() logo em seguida pega ele de volta (se nada mudou).
    public static void Soltar()
    {
        if (fundoAtual != null) fundoAtual.SetParent(null, false);
    }

    // Monta o fundo de novo (com o visual escolhido agora nas opções). Não faz nada fora de uma fase.
    public static void Recriar()
    {
        if (fundoAtual == null || fundoAtual.parent == null) return;
        Transform raiz = fundoAtual.parent;
        Destroy(fundoAtual.gameObject);
        fundoAtual = null;
        Criar(raiz, ultimaLargura, ultimaCor, ultimaNoite);
    }

    // As camadas são pintadas com a cor de fundo da fase: as distantes puxam para o claro (ar, "neblina"),
    // as próximas para o escuro. "noite" deixa tudo mais escuro, com lua no lugar do sol.
    // Com os gráficos em HD-2D (Opcoes.GraficosHD2D), as camadas de longe ficam desfocadas e azuladas
    // (a "névoa atmosférica": quanto mais longe, mais a cor some no ar), aparecem feixes de luz
    // e um primeiro plano desfocado passa na frente da fase. Veja ArteHD2D, FeixesDeLuz e PrimeiroPlano.
    public static void Criar(Transform raiz, int larguraFase, Color corDoFundo, bool noite = false)
    {
        bool hd = Opcoes.GraficosHD2D;
        Clima clima = ClimaAtual();

        // Sobrou o fundo da tentativa anterior (veja Soltar)? Se é da mesma fase, com o mesmo visual, reaproveita.
        if (fundoAtual != null && fundoAtual.parent == null)
        {
            bool igual = larguraFase == ultimaLargura && corDoFundo == ultimaCor && noite == ultimaNoite
                         && hd == ultimoHD && clima == ultimoClima;
            if (igual)
            {
                fundoAtual.SetParent(raiz, false);
                return;
            }
            Destroy(fundoAtual.gameObject);
        }

        ultimaLargura = larguraFase;
        ultimaCor = corDoFundo;
        ultimaNoite = noite;
        ultimoHD = hd;
        ultimoClima = clima;
        fundoAtual = new GameObject("Cenario de fundo").transform;
        fundoAtual.SetParent(raiz, false);
        raiz = fundoAtual;

        Color claro = noite ? new Color(0.45f, 0.45f, 0.75f) : Color.white;

        // céu em degradê: mais forte em cima, mais claro perto do horizonte
        Color topo = noite ? Color.Lerp(corDoFundo, Color.black, 0.45f) : Color.Lerp(corDoFundo, Escuro, 0.12f);
        Color horizonte = Color.Lerp(corDoFundo, claro, noite ? 0.25f : 0.45f);
        if (hd)
        {
            // HD-2D: céu de verdade, com 3 cores. Lá no alto, um azul fundo e saturado (o "zênite");
            // no meio, a cor da fase mais viva; no horizonte, uma luz quente e clara (o sol baixo).
            Color zenite = noite ? new Color(0.04f, 0.05f, 0.16f) : new Color(0.13f, 0.24f, 0.52f);
            Color meio = Saturar(corDoFundo, 1.15f) * (noite ? 0.45f : 0.88f);
            meio.a = 1f;
            topo = Color.Lerp(meio, zenite, noite ? 0.6f : 0.55f);
            horizonte = Color.Lerp(corDoFundo, noite ? new Color(0.45f, 0.42f, 0.7f) : new Color(1f, 0.8f, 0.58f), noite ? 0.3f : 0.55f);
            Faixa(raiz, "Ceu", FabricaDeSprites.Degrade(topo, meio, horizonte), 1f, -30, Color.white, -1f, 17f);
        }
        else Faixa(raiz, "Ceu", FabricaDeSprites.Degrade(topo, horizonte), 1f, -30, Color.white, -1f, 17f);

        SolOuLua(raiz, noite, hd, clima);

        Color montanhas = Color.Lerp(corDoFundo, claro, noite ? 0.1f : 0.35f);
        Color castelo = Color.Lerp(corDoFundo, claro, noite ? 0.05f : 0.22f); // (o galeão: lá longe, um pouco mais claro que a fase)
        Color floresta = Color.Lerp(corDoFundo, Escuro, noite ? 0.5f : 0.3f);
        Color arvores = Color.Lerp(corDoFundo, Escuro, noite ? 0.65f : 0.45f);

        if (hd)
        {
            // HD-2D: cores mais "de verdade". Cada camada mistura a cor da fase com a cor natural dela
            // (ilhas azuladas, o galeão de madeira escura, palmeiras e mato verde-escuros), e as de perto
            // ficam bem mais escuras e saturadas: é o contraste que dá profundidade.
            montanhas = Color.Lerp(corDoFundo, new Color(0.5f, 0.58f, 0.8f), 0.5f);
            castelo = Color.Lerp(corDoFundo, new Color(0.4f, 0.34f, 0.42f), 0.5f); // (o galeão)
            floresta = Color.Lerp(corDoFundo, new Color(0.16f, 0.34f, 0.38f), 0.55f);
            arvores = Color.Lerp(corDoFundo, new Color(0.1f, 0.26f, 0.2f), 0.6f);
            if (noite)
            {
                montanhas *= 0.55f; castelo *= 0.5f; floresta *= 0.45f; arvores *= 0.4f;
            }
            // Névoa atmosférica: a cor do ar perto do horizonte, puxada para o azul.
            // Quanto mais longe a camada, mais ela "afunda" nessa cor.
            Color ar = Color.Lerp(horizonte, noite ? new Color(0.3f, 0.35f, 0.65f) : new Color(0.72f, 0.82f, 1f), 0.35f);
            montanhas = Color.Lerp(montanhas, ar, 0.5f);
            castelo = Color.Lerp(castelo, ar, 0.45f); // (o galeão: bem longe, quase sumindo no ar)
            floresta = Color.Lerp(floresta, ar, 0.14f);
            arvores = Color.Lerp(arvores, ar, 0.06f);
            montanhas.a = castelo.a = floresta.a = arvores.a = 1f;
        }
        string sufixo = hd ? "_hd" : ""; // "fundo_galeao_hd" = a versão com a luz pintada

        Camada(raiz, "fundo_ilhas" + sufixo, 0.93f, -27, montanhas, larguraFase, 3f);
        // (as ordens pulam o -23 de propósito: é onde a Baleia Branca nada no fundo)
        Camada(raiz, "fundo_galeao" + sufixo, 0.85f, -24, castelo, larguraFase);
        Camada(raiz, "fundo_palmeiras" + sufixo, 0.6f, -22, floresta, larguraFase);
        Camada(raiz, "fundo_mato" + sufixo, 0.35f, -20, arvores, larguraFase);

        // neblina baixinha, entre o fundo e a fase
        Faixa(raiz, "Neblina", FabricaDeSprites.Pegar("neblina"), 1f, -19, new Color(claro.r, claro.g, claro.b, hd ? 0.45f : 0.35f), -0.5f, 3.5f);

        if (!hd)
        {
            // folhagem bem perto da câmera, escura, só na beirada de baixo da tela (moitas redondas:
            // nada pontudo, para ninguém confundir com espinho)
            Camada(raiz, "frente_folhas", -0.4f, 40, Color.Lerp(corDoFundo, Escuro, 0.8f) * new Color(1f, 1f, 1f, 0.8f), larguraFase, -1.3f);
            return;
        }

        // ---- só no HD-2D ----
        FeixesDeLuz.Criar(raiz, clima); // raios de sol e poeira dourada (no escuro não cria nada)
        SombrasDeNuvens.Criar(raiz, clima); // manchas de sombra das nuvens passando (só de dia e de noite)

        // Primeiro plano desfocado: escuro (contra a luz) e meio transparente nas bordas borradas.
        // Nas cabines (dentro do navio) não tem capim; no escuro nem daria para ver.
        if (clima == Clima.Dia || clima == Clima.Noite)
        {
            Color frente = Color.Lerp(corDoFundo, Escuro, noite ? 0.9f : 0.82f);
            frente.a = 0.8f; // meio transparente e mais baixo: nunca pode esconder um espinho do chão
            PrimeiroPlano.Criar(raiz, "capim", "hd2d_frente_capim", -0.55f, false, frente, 1.35f);
            frente.a = 0.7f;
            PrimeiroPlano.Criar(raiz, "galho", "hd2d_frente_galho", -0.75f, true, frente, 1f, 5f);
        }
    }

    // Versão pequena, para o horizonte do mapa do mundo: o mar aberto, com as ilhas e o vulcão lá longe e
    // os galeões passando, encolhidos e com a base em "baseY". Quanto mais longe, mais a cor puxa para a do céu.
    public static void CriarHorizonte(Transform raiz, int largura, float baseY, float escala, Color corDoCeu, Color corDoMar)
    {
        Camada(raiz, "fundo_ilhas", 0.7f, -24, Color.Lerp(corDoCeu, Escuro, 0.18f), largura, baseY, escala);
        Camada(raiz, "fundo_galeao", 0.45f, -22, Color.Lerp(Color.Lerp(corDoMar, corDoCeu, 0.4f), Escuro, 0.2f), largura, baseY - 0.6f, escala);
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

    // Onde fica o sol (ou a lua) em relação à câmera: no alto, à ESQUERDA. É de lá que vem a luz de todos
    // os desenhos (os blocos são claros em cima-esquerda e a sombrinha deles cai para a direita).
    public static readonly Vector3 PosicaoDoSol = new Vector3(-8f, 11.3f, 0f);

    // Sol com raios de luz (de dia) ou lua com estrelas (de noite), lá longe, no canto de cima.
    // No HD-2D o sol/lua ganha uma auréola grande e suave (um "bloom" desenhado), e os raios
    // simples dão lugar aos feixes de luz (FeixesDeLuz).
    static void SolOuLua(Transform raiz, bool noite, bool hd, Clima clima)
    {
        Transform ceu = NovaCamada(raiz, "SolOuLua", 0.97f).transform;
        var astro = ConstrutorDeFase.Visual(noite ? "Lua" : "Sol", ceu, Vector3.zero, noite ? "lua" : "sol", -29, false);
        astro.transform.localPosition = PosicaoDoSol;
        astro.transform.localScale = Vector3.one * 1.4f;

        if (hd && clima != Clima.Escuro)
        {
            // A LUZ DO SOL de verdade (luz 2D do URP): uma luz enorme e quente vinda do sol, no alto à esquerda.
            // O canto de cima-esquerda da tela fica dourado e o de baixo-direita fica na sombra azulada da
            // luz global (que no HD-2D é mais fraca: veja Luzes.AjustarAoVisual). É o contraste "quente x frio"
            // do Octopath Traveler. Ela anda junto com a câmera (está na camada do sol).
            var objetoDaLuz = new GameObject("Luz do sol");
            objetoDaLuz.transform.SetParent(ceu, false);
            objetoDaLuz.transform.localPosition = PosicaoDoSol + new Vector3(-2f, 2f, 0f);
            var luz = objetoDaLuz.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.color = noite ? new Color(0.55f, 0.65f, 1f) : clima == Clima.Cabines ? new Color(1f, 0.75f, 0.45f) : new Color(1f, 0.8f, 0.52f);
            luz.intensity = noite ? 0.3f : clima == Clima.Cabines ? 0.4f : 0.6f;
            luz.pointLightOuterRadius = 30f;
            luz.pointLightInnerRadius = 3f;
            luz.falloffIntensity = 0.35f;
        }

        if (hd)
        {
            var aureola = ConstrutorDeFase.Visual("Aureola", ceu, Vector3.zero, "hd2d_aureola", -28, false);
            aureola.transform.localPosition = PosicaoDoSol;
            aureola.transform.localScale = Vector3.one * (noite ? 2.2f : 3.2f);
            var brilho = aureola.GetComponent<SpriteRenderer>();
            brilho.color = noite ? new Color(0.7f, 0.8f, 1f, 0.35f) : new Color(1f, 0.85f, 0.55f, 0.55f);
            // luz ADITIVA (soma com o céu): o miolo passa do branco e o bloom faz o sol brilhar de verdade
            if (MateriaisHD2D.UsarLuzAditiva(brilho)) brilho.color *= new Color(1f, 1f, 1f, 0.8f);
            aureola.AddComponent<Pulsar>().velocidade = 0.35f;
        }

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

        if (hd) return; // no HD-2D quem faz os raios são os FeixesDeLuz

        // raios de luz descendo do sol, pulsando devagar
        float[] angulos = { -18f, -32f, -48f };
        for (int i = 0; i < angulos.Length; i++)
        {
            var raio = ConstrutorDeFase.Visual("RaioDeLuz", ceu, Vector3.zero, "raio_de_luz", -26, false);
            raio.transform.localPosition = PosicaoDoSol;
            raio.transform.localRotation = Quaternion.Euler(0f, 0f, -angulos[i]); // o sol está à esquerda: os raios descem para a direita
            raio.transform.localScale = new Vector3(1.6f + i * 0.4f, 1.6f, 1f);
            raio.AddComponent<Pulsar>().fase = i * 1.7f;
        }
    }

    // Deixa a cor mais viva (fator > 1) ou mais cinza (fator < 1), mantendo a claridade.
    static Color Saturar(Color c, float fator)
    {
        float cinza = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
        return new Color(Mathf.Clamp01(cinza + (c.r - cinza) * fator), Mathf.Clamp01(cinza + (c.g - cinza) * fator),
                         Mathf.Clamp01(cinza + (c.b - cinza) * fator), c.a);
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        transform.position = new Vector3(cam.transform.position.x * fator, 0f, 0f);
    }
}
