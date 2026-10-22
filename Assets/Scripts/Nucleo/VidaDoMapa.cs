using System.Collections.Generic;
using UnityEngine;

// A VIDA do mapa do mundo: tudo o que mexe sozinho (fora o vento nas plantas, que é do Vento.cs, e a
// névoa, que é da NevoaDoMapa).
//  - a água: ondinhas, espuma "respirando" na margem e brilhos do sol (quadros prontos da ArteDoMapa);
//  - bandeirinhas tremulando nas torres do castelo e do forte;
//  - fumaça saindo das chaminés das casas (e indo com o vento, para a direita);
//  - cristais e a casa assombrada pulsando de leve;
//  - borboletas em volta das flores e vaga-lumes na região sombria;
//  - bandos de pássaros cruzando o céu de vez em quando;
//  - folhas (e cinzas, na região sombria) voando no vento, mais rápido quando passa uma rajada;
//  - sombras de nuvens grandes passando devagar pelo chão (como nas fases: veja SombrasDeNuvens).
//
// Um componente só cuida de TUDO isso (um Update para centenas de coisas é bem mais leve que centenas de
// Updates). Nada é criado depois da montagem: os objetos só mudam de lugar, de cor ou de quadro. E o que
// está longe da câmera nem é mexido (ninguém está vendo).
[DefaultExecutionOrder(110)] // depois da câmera (CameraSeguir): o que segue a câmera não treme
public class VidaDoMapa : MonoBehaviour
{
    const float QuadrosDaAguaPorSegundo = 4f;
    const int OrdemDaFumaca = 18, OrdemDosBichos = 17, OrdemDosPassaros = 20, OrdemDasFolhas = 22;

    // ---------------------------------------------------------------- água
    readonly List<SpriteRenderer> aguas = new List<SpriteRenderer>();
    readonly List<Sprite[]> quadrosDasAguas = new List<Sprite[]>();
    int quadroDaAgua = -1;

    // ---------------------------------------------------------------- desenhos animados por quadros
    struct Quadros { public SpriteRenderer desenho; public Sprite[] quadros; public float porSegundo, fase; public int atual; }
    readonly List<Quadros> animados = new List<Quadros>();

    // ---------------------------------------------------------------- brilhos pulsando
    struct Pulso { public SpriteRenderer desenho; public Color cor; public Vector3 escala; public float velocidade, fase, minimo; }
    readonly List<Pulso> pulsos = new List<Pulso>();

    // ---------------------------------------------------------------- fumaça
    const int BaforadasPorChamine = 3;
    const float VidaDaBaforada = 2.6f;
    struct Chamine { public Vector3 boca; public Transform[] baforadas; public SpriteRenderer[] desenhos; public float fase; }
    readonly List<Chamine> chamines = new List<Chamine>();

    // ---------------------------------------------------------------- borboletas e vaga-lumes
    struct Bicho { public Transform corpo; public SpriteRenderer desenho; public Vector3 casa; public float fase, raio; public bool vagaLume; public Color cor; }
    readonly List<Bicho> bichos = new List<Bicho>();
    Sprite[] asas;

    // ---------------------------------------------------------------- pássaros
    struct Passaro { public Transform corpo; public SpriteRenderer desenho; public float inicio, altura, velocidade, fase; public Vector2 noBando; }
    readonly List<Passaro> passaros = new List<Passaro>();
    Sprite[] batidas;
    float larguraDoMapa;

    // ---------------------------------------------------------------- folhas no vento
    Transform[] folhas;
    SpriteRenderer[] desenhosDasFolhas;
    float[] faseDaFolha;
    Sprite[] giros;
    float inicioDaSombra, alturaDoMapa;

    // ---------------------------------------------------------------- sombras de nuvens
    const float VelocidadeDasNuvens = 0.5f; // blocos por segundo, para a direita (com o vento)
    Transform[] nuvens;
    SpriteRenderer[] desenhosDasNuvens;
    float[] inicioDaNuvem;
    bool multiplicar, nuvensVisiveis = true;
    float deriva;

    public static VidaDoMapa Criar(Transform raiz, int largura, int altura, float horizonte, float inicioDaSombra)
    {
        var vida = new GameObject("Vida do mapa").AddComponent<VidaDoMapa>();
        vida.transform.SetParent(raiz, false);
        vida.larguraDoMapa = largura;
        vida.alturaDoMapa = altura;
        vida.inicioDaSombra = inicioDaSombra;
        vida.asas = new[] { FabricaDeSprites.Pegar("mapa_borboleta_0"), FabricaDeSprites.Pegar("mapa_borboleta_1") };
        vida.batidas = new[] { FabricaDeSprites.Pegar("mapa_passaro_0"), FabricaDeSprites.Pegar("mapa_passaro_1") };
        vida.giros = new[] { FabricaDeSprites.Pegar("mapa_folha_0"), FabricaDeSprites.Pegar("mapa_folha_1") };
        vida.CriarPassaros(horizonte);
        vida.CriarFolhas();
        return vida;
    }

    // ---------------------------------------------------------------- quem monta o mapa chama estes

    public void Agua(SpriteRenderer desenho, Sprite[] quadros)
    {
        aguas.Add(desenho);
        quadrosDasAguas.Add(quadros);
    }

    public void Animar(SpriteRenderer desenho, Sprite[] quadros, float porSegundo)
    {
        animados.Add(new Quadros { desenho = desenho, quadros = quadros, porSegundo = porSegundo, fase = desenho.transform.position.x * 0.31f, atual = -1 });
    }

    // O brilho aumenta e diminui (a transparência vai de "minimo" até a cor original).
    public void Pulsar(SpriteRenderer desenho, float velocidade, float minimo)
    {
        pulsos.Add(new Pulso
        {
            desenho = desenho, cor = desenho.color, escala = desenho.transform.localScale,
            velocidade = velocidade, minimo = minimo, fase = desenho.transform.position.x * 1.3f + desenho.transform.position.y,
        });
    }

    // Uma chaminé soltando fumaça em "boca" (posição no mundo).
    public void Fumaca(Vector3 boca)
    {
        var c = new Chamine
        {
            boca = boca,
            baforadas = new Transform[BaforadasPorChamine],
            desenhos = new SpriteRenderer[BaforadasPorChamine],
            fase = Mathf.Repeat(boca.x * 0.37f + boca.y * 0.11f, 1f),
        };
        Sprite fumaca = FabricaDeSprites.Pegar("mapa_fumaca");
        for (int k = 0; k < BaforadasPorChamine; k++)
        {
            var objeto = new GameObject("Fumaca");
            objeto.transform.SetParent(transform, false);
            objeto.transform.position = boca;
            c.desenhos[k] = objeto.AddComponent<SpriteRenderer>();
            c.desenhos[k].sprite = fumaca;
            c.desenhos[k].sortingOrder = OrdemDaFumaca;
            c.baforadas[k] = objeto.transform;
        }
        chamines.Add(c);
    }

    // Borboleta voando em volta de "casa" (cor das asas) ou um vaga-lume piscando (cor null).
    public void Bichinho(Vector3 casa, Color? corDasAsas)
    {
        bool vagaLume = !corDasAsas.HasValue;
        var objeto = new GameObject(vagaLume ? "VagaLume" : "Borboleta");
        objeto.transform.SetParent(transform, false);
        objeto.transform.position = casa;
        var desenho = objeto.AddComponent<SpriteRenderer>();
        desenho.sprite = vagaLume ? FabricaDeSprites.Pegar("hd2d_pixel") : asas[0];
        desenho.sortingOrder = OrdemDosBichos;
        Color cor = corDasAsas ?? new Color(1f, 0.92f, 0.45f);
        desenho.color = cor;
        if (vagaLume)
        {
            objeto.transform.localScale = Vector3.one * 2f; // 2 x 2 pixels
            // uma auréola fraquinha em volta (se o shader de luz existir, ela "soma" com a tela)
            var aura = new GameObject("Aura");
            aura.transform.SetParent(objeto.transform, false);
            aura.transform.localScale = Vector3.one * 0.3f;
            var desenhoDaAura = aura.AddComponent<SpriteRenderer>();
            desenhoDaAura.sprite = FabricaDeSprites.Pegar("mapa_brilho");
            desenhoDaAura.color = new Color(1f, 0.85f, 0.4f, 0.35f);
            desenhoDaAura.sortingOrder = OrdemDosBichos - 1;
            MateriaisHD2D.UsarLuzAditiva(desenhoDaAura);
        }
        float semente = casa.x * 12.9898f + casa.y * 78.233f;
        bichos.Add(new Bicho
        {
            corpo = objeto.transform, desenho = desenho, casa = casa, vagaLume = vagaLume, cor = cor,
            fase = Mathf.Repeat(Mathf.Sin(semente) * 43758.55f, 6.283f), raio = vagaLume ? 0.9f : 0.7f,
        });
    }

    // Manchas de sombra (de nuvens bem grandes) andando pelo mapa. Com o shader SombraMultiplicada, a tela é
    // MULTIPLICADA por um azul-acinzentado (escurece sem "lavar" as cores); sem ele, um azul transparente.
    public void SombrasDeNuvens(int quantas, float alturaMaxima)
    {
        Sprite desenho = FabricaDeSprites.Pegar("mapa_sombra_nuvem");
        Material material = MateriaisHD2D.SombraMultiplicada;
        multiplicar = material != null;
        nuvens = new Transform[quantas];
        desenhosDasNuvens = new SpriteRenderer[quantas];
        inicioDaNuvem = new float[quantas];
        for (int i = 0; i < quantas; i++)
        {
            var objeto = new GameObject("SombraDeNuvem");
            objeto.transform.SetParent(transform, false);
            objeto.transform.localScale = new Vector3(1f + (i % 3) * 0.25f, 0.8f + (i % 2) * 0.3f, 1f);
            objeto.transform.position = new Vector3(0f, 2.5f + (i * 5.3f) % Mathf.Max(1f, alturaMaxima - 2.5f), 0f);
            desenhosDasNuvens[i] = objeto.AddComponent<SpriteRenderer>();
            desenhosDasNuvens[i].sprite = desenho;
            desenhosDasNuvens[i].sortingOrder = 25; // em cima de tudo do mapa, embaixo da névoa
            if (multiplicar) desenhosDasNuvens[i].sharedMaterial = material;
            nuvens[i] = objeto.transform;
            inicioDaNuvem[i] = i * (larguraDoMapa + 30f) / quantas;
        }
    }

    // ---------------------------------------------------------------- pássaros e folhas

    // Três bandos de 3 a 5 pássaros, cruzando o mapa inteiro da esquerda para a direita (cada um no seu tempo).
    void CriarPassaros(float horizonte)
    {
        var sorteio = new System.Random(31);
        for (int bando = 0; bando < 3; bando++)
        {
            int quantos = 3 + bando % 3;
            float altura = bando == 1 ? horizonte - 4f - sorteio.Next(4) : horizonte + 0.8f + (float)sorteio.NextDouble() * 1.4f;
            float velocidade = 2.2f + (float)sorteio.NextDouble() * 0.8f;
            float inicio = (float)sorteio.NextDouble() * (larguraDoMapa + 40f);
            for (int k = 0; k < quantos; k++)
            {
                var objeto = new GameObject("Passaro");
                objeto.transform.SetParent(transform, false);
                var desenho = objeto.AddComponent<SpriteRenderer>();
                desenho.sprite = batidas[0];
                desenho.sortingOrder = OrdemDosPassaros;
                desenho.color = bando == 1 ? new Color(0.25f, 0.22f, 0.3f, 0.9f) : new Color(0.3f, 0.28f, 0.4f, 0.8f);
                int lado = k % 2 == 0 ? 1 : -1;
                passaros.Add(new Passaro
                {
                    corpo = objeto.transform, desenho = desenho, inicio = inicio, altura = altura, velocidade = velocidade,
                    fase = k * 1.7f + bando, noBando = new Vector2(-((k + 1) / 2) * 0.7f, lado * ((k + 1) / 2) * 0.35f), // formação em "V"
                });
            }
        }
    }

    void CriarFolhas()
    {
        const int Quantas = 12;
        folhas = new Transform[Quantas];
        desenhosDasFolhas = new SpriteRenderer[Quantas];
        faseDaFolha = new float[Quantas];
        Camera cam = Camera.main;
        float cx = cam != null ? cam.transform.position.x : 0f;
        var sorteio = new System.Random(17);
        for (int i = 0; i < Quantas; i++)
        {
            var objeto = new GameObject("Folha");
            objeto.transform.SetParent(transform, false);
            objeto.transform.position = new Vector3(cx + (float)sorteio.NextDouble() * 40f - 20f, 1f + (float)sorteio.NextDouble() * (alturaDoMapa - 5f), 0f);
            desenhosDasFolhas[i] = objeto.AddComponent<SpriteRenderer>();
            desenhosDasFolhas[i].sprite = giros[0];
            desenhosDasFolhas[i].sortingOrder = OrdemDasFolhas;
            folhas[i] = objeto.transform;
            faseDaFolha[i] = (float)sorteio.NextDouble() * 10f;
        }
    }

    // ---------------------------------------------------------------- a cada quadro

    void LateUpdate()
    {
        float t = Time.time, dt = Time.deltaTime;
        Camera cam = Camera.main;
        float camX = cam != null ? cam.transform.position.x : larguraDoMapa / 2f;
        float meiaTela = (cam != null ? cam.orthographicSize * cam.aspect : 20f) + 3f;
        float esquerda = camX - meiaTela, direita = camX + meiaTela;

        // água: todas mostram o mesmo quadro (as ondas passam de uma célula para a outra sem emenda)
        int quadro = (int)(t * QuadrosDaAguaPorSegundo) % ArteDoMapa.QuadrosDaAgua;
        if (quadro != quadroDaAgua)
        {
            quadroDaAgua = quadro;
            for (int i = 0; i < aguas.Count; i++)
                if (aguas[i] != null) aguas[i].sprite = quadrosDasAguas[i][quadro];
        }

        for (int i = 0; i < animados.Count; i++)
        {
            Quadros a = animados[i];
            int q = (int)((t + a.fase) * a.porSegundo) % a.quadros.Length;
            if (q == a.atual || a.desenho == null) continue;
            a.atual = q;
            a.desenho.sprite = a.quadros[q];
            animados[i] = a;
        }

        for (int i = 0; i < pulsos.Count; i++)
        {
            Pulso p = pulsos[i];
            if (p.desenho == null) continue;
            float x = p.desenho.transform.position.x;
            if (x < esquerda || x > direita) continue;
            float onda = 0.5f + 0.5f * Mathf.Sin(t * p.velocidade + p.fase);
            p.desenho.color = new Color(p.cor.r, p.cor.g, p.cor.b, p.cor.a * Mathf.Lerp(p.minimo, 1f, onda));
            p.desenho.transform.localScale = p.escala * (0.9f + 0.15f * onda);
        }

        // fumaça: cada baforada nasce na chaminé, sobe, cresce, vai com o vento e some
        float vento = AjustesVisuais.vento ? AjustesVisuais.forcaDoVento : 0f;
        for (int i = 0; i < chamines.Count; i++)
        {
            Chamine c = chamines[i];
            if (c.boca.x < esquerda || c.boca.x > direita) continue;
            for (int k = 0; k < BaforadasPorChamine; k++)
            {
                float p = Mathf.Repeat(t / VidaDaBaforada + c.fase + k / (float)BaforadasPorChamine, 1f);
                float lado = (0.25f + 0.35f * vento) * p * p + Mathf.Sin(p * 7f + c.fase * 6f) * 0.05f;
                c.baforadas[k].position = c.boca + new Vector3(lado, p * 1.1f, 0f);
                c.baforadas[k].localScale = Vector3.one * (0.55f + p * 0.9f);
                float alfa = Mathf.Clamp01(p * 8f) * (1f - p) * 0.85f;
                c.desenhos[k].color = new Color(1f, 1f, 1f, alfa);
            }
        }

        // borboletas e vaga-lumes
        for (int i = 0; i < bichos.Count; i++)
        {
            Bicho b = bichos[i];
            if (b.casa.x < esquerda || b.casa.x > direita) continue;
            float f = b.fase;
            if (b.vagaLume)
            {
                b.corpo.position = b.casa + new Vector3(Mathf.Sin(t * 0.4f + f) * b.raio, Mathf.Sin(t * 0.53f + f * 2f) * b.raio * 0.6f, 0f);
                float brilho = Mathf.Clamp01(Mathf.Sin(t * 1.6f + f) * 1.6f + 0.2f);
                b.desenho.color = new Color(b.cor.r, b.cor.g, b.cor.b, 0.15f + 0.85f * brilho);
            }
            else
            {
                // voinho em "oito", com uns pulinhos para cima, e as asas batendo
                b.corpo.position = b.casa + new Vector3(Mathf.Sin(t * 0.7f + f) * b.raio,
                    0.35f + Mathf.Sin(t * 1.4f + f) * 0.25f + Mathf.Abs(Mathf.Sin(t * 5f + f)) * 0.12f, 0f);
                b.desenho.sprite = asas[(int)(t * 9f + f) & 1];
                b.desenho.flipX = Mathf.Cos(t * 0.7f + f) < 0f;
            }
        }

        // pássaros: o bando atravessa o mapa inteiro e volta a aparecer do outro lado depois de um tempo
        float volta = larguraDoMapa + 40f;
        for (int i = 0; i < passaros.Count; i++)
        {
            Passaro p = passaros[i];
            float x = Mathf.Repeat(p.inicio + t * p.velocidade, volta) - 20f + p.noBando.x;
            bool visivel = x > esquerda && x < direita;
            p.desenho.enabled = visivel;
            if (!visivel) continue;
            float y = p.altura + p.noBando.y + Mathf.Sin(t * 0.8f + p.fase) * 0.25f;
            p.corpo.position = new Vector3(x, y, 0f);
            p.desenho.sprite = batidas[(int)(t * 6f + p.fase) & 1];
        }

        // sombras de nuvens: andam com o vento e dão a volta no mapa
        if (nuvens != null)
        {
            bool ligadas = AjustesVisuais.NuvensLigadas;
            if (ligadas != nuvensVisiveis)
            {
                nuvensVisiveis = ligadas;
                foreach (SpriteRenderer d in desenhosDasNuvens) d.enabled = ligadas;
            }
            if (ligadas)
            {
                deriva += VelocidadeDasNuvens * AjustesVisuais.velocidadeDasNuvens * dt;
                float forca = Mathf.Clamp(AjustesVisuais.forcaDasNuvens, 0f, 2f);
                Color cor = multiplicar ? new Color(0.6f, 0.64f, 0.84f, Mathf.Clamp01(0.75f * forca)) : new Color(0.08f, 0.1f, 0.25f, Mathf.Clamp01(0.22f * forca));
                for (int i = 0; i < nuvens.Length; i++)
                {
                    Vector3 p = nuvens[i].position;
                    p.x = Mathf.Repeat(inicioDaNuvem[i] + deriva, larguraDoMapa + 30f) - 15f;
                    nuvens[i].position = p;
                    desenhosDasNuvens[i].color = cor;
                }
            }
        }

        // folhas no vento: andam para a direita (mais rápido nas rajadas), girando; quem sai da tela volta do outro lado
        if (folhas == null) return;
        for (int i = 0; i < folhas.Length; i++)
        {
            Vector3 p = folhas[i].position;
            float rajada = Mathf.Max(0f, Vento.Balanco(p.x, t)) * vento;
            float f = faseDaFolha[i];
            p.x += (0.5f + 2.6f * rajada) * dt;
            p.y += (Mathf.Sin(t * 2.3f + f) * 0.5f - 0.18f) * dt;
            if (p.x > direita) p.x -= meiaTela * 2f;
            if (p.x < esquerda) p.x += meiaTela * 2f;
            if (p.y < 0.5f) p.y += alturaDoMapa - 5f;
            if (p.y > alturaDoMapa - 3.5f) p.y -= alturaDoMapa - 5f;
            folhas[i].position = p;
            desenhosDasFolhas[i].sprite = giros[(int)(t * 5f + f) & 1];
            desenhosDasFolhas[i].flipX = Mathf.Sin(t * 3.1f + f) < 0f;
            // na região sombria não tem folha verde: são cinzas escuras voando
            bool sombria = p.x > inicioDaSombra;
            float alfa = 0.35f + 0.65f * Mathf.Clamp01(rajada * 2f + 0.3f);
            desenhosDasFolhas[i].color = sombria ? new Color(0.45f, 0.4f, 0.55f, alfa * 0.8f) : new Color(1f, 1f, 1f, alfa);
        }
    }
}
