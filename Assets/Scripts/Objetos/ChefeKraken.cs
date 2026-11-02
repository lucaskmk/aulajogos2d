using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 'g' - o KRAKEN VERMELHO, o chefe FINAL ("O Kraken Vermelho", fase 13). A lula gigante vermelha que prendeu a Marina,
// mandou a Baleia Branca atrás do Grumete e levantou o tsunami. Os desenhos ficam em ArteDoKraken.cs.
//
// O 'g' marca o começo da arena (na linha em que o jogador anda): são larguraDaArena blocos de convés LISO e, logo
// depois, a POÇA dela (4 colunas sem chão no mapa), de onde ela sai. Entrou, a parede de caixotes fecha atrás de
// você, a câmera trava e a luta começa. É o mesmo esquema dos outros chefes (ChefeCapita.cs):
//  - tudo que é VERMELHO mata com UM toque, mas todo ataque AVISA antes (linha vermelha, marca no chão);
//  - os padrões são FIXOS: dá para decorar;
//  - são 3 ETAPAS de 2 golpes. Morreu? Volta na hora, no começo da arena, JÁ na etapa em que estava.
//
// REGRA DE OURO: só a PONTA dos tentáculos mata (o braço é só o caminho dela), e a tinta.
// O tentáculo AMARELO (o agarrão) NÃO mata: ele ROUBA moedas (veja "AS MOEDAS", lá embaixo).
// COMO VENCER: depois de cada sequência de ataques ela CANSA e deita a cabeça no convés, tonta (olho em espiral):
// é a hora de pular na CABEÇA dela (3 / 2,6 / 2,2 s). Cuidado para não cair na poça!
//
// OS ATAQUES
//  CHICOTADA: uma linha vermelha mostra a altura; o tentáculo sai da poça, varre a arena inteira e volta.
//     Linha BAIXA = PULE (na ida e na volta). Linha ALTA = NÃO pule. A partir da etapa 2 a volta pode vir na
//     OUTRA altura (a linha fraquinha mostra; ela pisca de novo na ponta, antes de voltar).
//  PANCADA: a marca no chão segue você, trava (fica vermelha) e um tentáculo DESPENCA do céu ali. Saia de baixo.
//  AGARRÃO: bolhas AMARELAS na poça... e o tentáculo amarelo vem rente ao chão, atrás de você. Pule por cima!
//     Se ele te pegar, não mata: ROUBA moedas e te joga para trás.
//  TINTA (etapa 2 em diante): marcas no chão mostram onde as bolas de tinta vão cair. Saia delas.
//  ETAPA 2 em diante: COMBOS. Só vêm juntos os ataques que se desviam do mesmo jeito (a chicotada ALTA, que pede
//     para ficar no chão, com a pancada ou a tinta, que pedem para andar). O agarrão, que pede pulo, vem sempre sozinho.
//  ETAPA 3: ela fica FURIOSA (sobrancelhas e aura vermelha) e os ataques vêm colados uns nos outros.
//
// AS MOEDAS (o roubo): cada agarrão leva até 5 / 7 / 10 moedas (primeiro as que você pegou nesta tentativa, depois as
// guardadas; nunca fica negativo; sem moedas, ela só te empurra). O roubo é SALVO na hora (fechar o jogo não devolve).
// Ela guarda tudo o que roubou: a cada pisão devolve METADE, e quando é derrotada devolve o resto (direto para as
// moedas guardadas: morrer depois não perde). Morrer NÃO devolve nada. O escudo não serve contra o roubo.
public class ChefeKraken : Chefe
{
    const int EtapasDaLuta = 3;
    const int GolpesDaEtapa = 2;

    public int vidaMaxima = EtapasDaLuta * GolpesDaEtapa;
    public float larguraDaArena = 21f;

    // O que a barra de vida (Interface) lê. Tudo isso vem da classe base Chefe (veja Chefe.cs).
    public override string Nome => "KRAKEN VERMELHO";
    public override int Vida => vida;
    public override float VidaMostrada => vidaMostrada;
    public override bool EmCombate => emCombate;
    public override int VidaMaxima => vidaMaxima;
    public override int EtapaAtual => etapa;
    public override int Etapas => EtapasDaLuta;
    public override int GolpesPorEtapa => GolpesDaEtapa;
    public override Musicas.Tema Musica => Musicas.Tema.Kraken;
    public override Vector3 PontoDeRenascer => new Vector3(esquerda + 0.5f, chao + 0.5f, 0f); // a casa do 'g'

    // ------------------------------------------------------------------ ajustes de cada etapa (1, 2, 3)

    static readonly float[] TempoDeAviso = { 0.6f, 0.5f, 0.42f };        // (o chicote baixo pede pulo: nunca menos que isso)
    static readonly float[] TempoCansada = { 3f, 2.6f, 2.2f };            // a chance de pisar na cabeça dela
    static readonly float[] VelocidadeDoChicote = { 13f, 15f, 17f };     // blocos por segundo
    static readonly float[] VelocidadeDoAgarrao = { 7.5f, 8.5f, 9.5f };  // (o Grumete anda 7: correndo, ele te alcança. Pule!)
    static readonly float[] TempoSeguindo = { 0.9f, 0.75f, 0.6f };        // a marca da pancada segue você isso antes de travar
    static readonly float[] Respiro = { 0.55f, 0.45f, 0.35f };            // pausa entre os ataques
    static readonly int[] MoedasPorAgarrao = { 5, 7, 10 };

    const float AlturaBaixa = 0.45f, AlturaAlta = 1.7f; // alturas da chicotada (centro da ponta, acima do chão)
    const float RaioDaPonta = 0.45f, RaioDaTinta = 0.38f, RaioDoRespingo = 0.8f;
    const float DuracaoDaTinta = 0.75f;                 // o voo de cada bola de tinta
    const float AlturaDaCabeca = 2.55f;                 // o topo do manto, deitada (onde se pisa)

    // Ordem de desenho: o corpo fica ATRÁS dos blocos (some embaixo do convés, só aparece na poça).
    const int OrdemDoCorpo = -3, OrdemDaCabeca = 6, OrdemDosGomos = 7, OrdemDaPonta = 8, OrdemDosAvisos = 55, OrdemDasEstrelas = 58;

    // A "caixa" do Grumete que conta para morrer (um tiquinho menor que o desenho: é mais justo).
    const float MeiaLarguraDoJogador = 0.27f, MeiaAlturaDoJogador = 0.42f;
    const float FolgaDoAcerto = 0.15f; // de raspão não conta

    static readonly Color Vermelho = new Color(1f, 0.2f, 0.3f, 1f);
    static readonly Color VermelhoFraco = new Color(1f, 0.2f, 0.3f, 0.35f);
    static readonly Color VermelhoBemFraco = new Color(1f, 0.2f, 0.3f, 0.15f);
    static readonly Color AmareloDoAviso = new Color(1f, 0.85f, 0.2f, 1f);
    static readonly Color CorDaAura = new Color(1f, 0.25f, 0.3f, 0.7f);
    static readonly Color CorDaTinta = new Color(0.16f, 0.04f, 0.13f, 0.9f);

    // ------------------------------------------------------------------ estado

    enum Estado { Esperando, Lutando, Cansada, Derrotada }
    Estado estado;
    int vida;
    float vidaMostrada;
    bool emCombate;
    int etapa;
    bool parado;          // o jogador morreu: tudo congela até a fase recomeçar
    bool furiosa;         // etapa 3
    bool pegouNesteAgarrao; // o agarrão só rouba uma vez por bote
    float chao, esquerda, direita, xDoCorpo;
    float relogio, timerPiscar, timerAura, timerDor;
    float alturaDoCorpo;  // onde o corpo está (em relação ao normal): 0 = de pé na poça, negativo = afundado

    SpriteRenderer corpo, cabeca, saco;
    Transform efeitos;    // pai de tudo que é passageiro (avisos, marcas, tinta...): some de uma vez no Arrumar

    // Um tentáculo: uma fileira de gomos da raiz até a ponta (numa curva), e a ponta (o "taco", que mata).
    class Tentaculo
    {
        public readonly List<SpriteRenderer> gomos = new List<SpriteRenderer>();
        public SpriteRenderer ponta;
        public Vector2 raiz, fim;
        public float curva, fase;
        public bool visivel;
    }
    const int MaximoDeGomos = 56;
    const float DistanciaEntreGomos = 0.48f;
    readonly List<Tentaculo> vermelhos = new List<Tentaculo>(); // 0: chicotada; 1 e 2: pancadas; 3 e 4: enfeite (balançando na poça)
    Tentaculo amarelo;
    int proximaPancada = 1;

    // Uma área que mata (ou que ROUBA): tudo a menos de "raio" do segmento a-b. Igual à da Capitã.
    class Zona { public Vector2 a, b; public float raio; public bool rouba; }
    readonly List<Zona> zonas = new List<Zona>();

    readonly List<GameObject> paredes = new List<GameObject>();
    readonly List<Transform> estrelas = new List<Transform>();
    readonly HashSet<string> dicasMostradas = new HashSet<string>();

    float Aviso => TempoDeAviso[etapa];
    Vector3 LugarDoCorpo => new Vector3(xDoCorpo, chao - 1.2f + alturaDoCorpo + 0.15f * Mathf.Sin(relogio * 1.6f), 0f);
    Vector2 Boca => new Vector2(xDoCorpo - 0.6f, chao + 1.6f + alturaDoCorpo);
    // deitada: o desenho tem o pivô na ponta da direita, embaixo (veja ArteDoKraken)
    float XDaCabecaDeitada => direita + 0.6f;

    // ------------------------------------------------------------------ Unity

    void Awake()
    {
        chao = transform.position.y - 0.5f;
        esquerda = transform.position.x - 0.5f; // borda esquerda da casa do 'g'
        direita = esquerda + larguraDaArena;     // daqui para a frente é a poça
        xDoCorpo = direita + 2.6f;
        vida = vidaMaxima;
        vidaMostrada = vida;

        efeitos = new GameObject("Efeitos").transform;
        efeitos.SetParent(transform, false);

        corpo = ConstrutorDeFase.Visual("Kraken", transform, LugarDoCorpo, "kraken_corpo_normal", OrdemDoCorpo, false).GetComponent<SpriteRenderer>();
        cabeca = ConstrutorDeFase.Visual("CabecaDeitada", transform, new Vector3(XDaCabecaDeitada, chao, 0f), "kraken_cansada", OrdemDaCabeca, false)
            .GetComponent<SpriteRenderer>();
        cabeca.enabled = false;
        saco = ConstrutorDeFase.Visual("SacoDeMoedas", transform, transform.position, "kraken_saco", OrdemDaPonta + 1, false).GetComponent<SpriteRenderer>();
        saco.enabled = false;

        for (int i = 0; i < 5; i++) vermelhos.Add(CriarTentaculo(false, i * 1.7f));
        amarelo = CriarTentaculo(true, 0.5f);
        Luzes.Ponto(corpo.transform, new Color(1f, 0.35f, 0.3f), 6f, 0.8f, Vector3.up * 3.5f); // (só nas fases escuras)

        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.Chefe = this;
    }

    Tentaculo CriarTentaculo(bool ehAmarelo, float fase)
    {
        var t = new Tentaculo { fase = fase };
        for (int i = 0; i < MaximoDeGomos; i++)
        {
            var gomo = ConstrutorDeFase.Visual("Gomo", transform, transform.position, ehAmarelo ? "kraken_gomo_amarelo" : "kraken_gomo", OrdemDosGomos, false);
            t.gomos.Add(gomo.GetComponent<SpriteRenderer>());
        }
        t.ponta = ConstrutorDeFase.Visual("Ponta", transform, transform.position, ehAmarelo ? "kraken_ponta_amarela" : "kraken_ponta", OrdemDaPonta, false)
            .GetComponent<SpriteRenderer>();
        Posicionar(t);
        return t;
    }

    void Start()
    {
        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        if (jogo == null) return;
        // já venceu nesta fase (morreu depois)? Ela fugiu para o fundo do mar.
        if (jogo.ChefeDerrotado)
        {
            estado = Estado.Derrotada;
            corpo.enabled = false;
            return;
        }
        // renasceu dentro da arena (morreu na luta)? A luta volta na hora, sem a introdução.
        if (jogo.EhPontoDeSave(PontoDeRenascer)) StartCoroutine(Lutar(renasceu: true));
        else alturaDoCorpo = -6f; // escondida na poça até você chegar
    }

    void OnDestroy()
    {
        if (GerenciadorDoJogo.Instancia != null && GerenciadorDoJogo.Instancia.Chefe == this)
            GerenciadorDoJogo.Instancia.Chefe = null;
    }

    void Update()
    {
        vidaMostrada = Mathf.MoveTowards(vidaMostrada, vida, 1.5f * Time.deltaTime);
        relogio += Time.deltaTime; // Time.deltaTime é 0 na pausa: tudo congela sozinho

        if (estado == Estado.Derrotada && !emCombate) return; // fugiu

        if (estado == Estado.Esperando)
        {
            Animar();
            if (GerenciadorDoJogo.JogadorVivo(out Vector2 entrando) && entrando.x > esquerda + 2f)
                StartCoroutine(Lutar(renasceu: false));
            return;
        }

        // O jogador morreu: para TUDO na hora (a fase inteira vai ser recriada quando ele renascer).
        if (emCombate && !parado && !GerenciadorDoJogo.Pausado && !GerenciadorDoJogo.JogadorVivo(out _))
        {
            parado = true;
            StopAllCoroutines();
            zonas.Clear();
        }
        if (parado) return;

        Animar();
        GirarEstrelas();
        if (TestarPisao()) return;
        TestarPerigos();
    }

    // O corpo boiando (e a cara certa), a aura da furiosa e os tentáculos.
    void Animar()
    {
        corpo.transform.position = LugarDoCorpo;
        timerDor -= Time.deltaTime;
        corpo.sprite = FabricaDeSprites.Pegar(timerDor > 0f ? "kraken_corpo_dor" : furiosa ? "kraken_corpo_brava" : "kraken_corpo_normal");

        if (timerPiscar > 0f)
        {
            timerPiscar -= Time.deltaTime;
            Color cor = (int)(timerPiscar * 16f) % 2 == 0 ? new Color(1f, 0.55f, 0.55f) : Color.white;
            if (timerPiscar <= 0f) cor = Color.white;
            corpo.color = cabeca.color = cor;
        }

        if (furiosa && estado != Estado.Derrotada && corpo.enabled)
        {
            timerAura -= Time.deltaTime;
            if (timerAura <= 0f)
            {
                timerAura = 0.06f;
                Vector3 lugar = corpo.transform.position + new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(2f, 7.5f), 0f);
                Particula.Criar(transform, lugar, new Vector2(0f, Random.Range(1f, 2f)), 0.5f, 0.4f, CorDaAura);
            }
        }

        // Dois tentáculos de enfeite balançando na poça, dos lados dela (nunca entram na arena: não machucam).
        for (int i = 3; i <= 4; i++)
        {
            Tentaculo t = vermelhos[i];
            t.visivel = corpo.enabled && alturaDoCorpo > -3f;
            float lado = i == 3 ? -1f : 1f;
            t.raiz = new Vector2(xDoCorpo + lado * 1.3f, chao - 1.5f + alturaDoCorpo);
            t.fim = new Vector2(xDoCorpo + lado * (2.2f + 0.5f * Mathf.Sin(relogio * 1.3f + t.fase)),
                                chao + 2.5f + alturaDoCorpo + 0.8f * Mathf.Sin(relogio * 1.7f + t.fase));
            t.fim.x = Mathf.Max(t.fim.x, direita + 0.3f);
            t.curva = lado * 0.8f;
        }

        foreach (Tentaculo t in vermelhos) Posicionar(t);
        Posicionar(amarelo);
    }

    // Os gomos vão da raiz até perto da ponta numa curva (com uma ondinha), e a ponta fica no fim, virada para fora.
    void Posicionar(Tentaculo t)
    {
        Vector2 a = t.raiz, b = t.fim;
        Vector2 d = b - a;
        float comprimento = d.magnitude;
        Vector2 deLado = comprimento > 0.01f ? new Vector2(-d.y, d.x) / comprimento : Vector2.up;
        Vector2 controle = (a + b) / 2f + deLado * t.curva;
        int quantos = t.visivel ? Mathf.Clamp(Mathf.CeilToInt(comprimento / DistanciaEntreGomos), 2, MaximoDeGomos) : 0;
        for (int i = 0; i < t.gomos.Count; i++)
        {
            bool aparece = i < quantos;
            t.gomos[i].enabled = aparece;
            if (!aparece) continue;
            float s = (i + 0.5f) / quantos * 0.95f;
            Vector2 p = (1 - s) * (1 - s) * a + 2 * (1 - s) * s * controle + s * s * b;
            p += deLado * 0.14f * Mathf.Sin(relogio * 6f + i * 0.8f + t.fase) * Mathf.Sin(s * Mathf.PI);
            t.gomos[i].transform.position = p;
            t.gomos[i].transform.localScale = Vector3.one * Mathf.Lerp(1.35f, 0.75f, s);
        }
        t.ponta.enabled = t.visivel;
        if (!t.visivel) return;
        Vector2 direcao = b - controle; // a tangente da curva na ponta
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        t.ponta.transform.position = b;
        t.ponta.transform.rotation = Quaternion.Euler(0f, 0f, angulo);
        t.ponta.flipY = direcao.x < 0f; // as ventosas sempre para baixo
    }

    // ------------------------------------------------------------------ a luta

    IEnumerator Lutar(bool renasceu)
    {
        estado = Estado.Lutando;
        emCombate = true;
        FecharArena();
        CameraSeguir.Travar(esquerda - 0.5f, direita + 5.2f); // a arena e ela, na poça
        GerenciadorDoJogo.Instancia.TocarMusicaDoChefe();
        GerenciadorDoJogo.Instancia.SalvarPonto(PontoDeRenascer); // morreu? renasce aqui, no começo da arena

        etapa = Mathf.Clamp(GerenciadorDoJogo.Instancia.EtapaDoChefe, 0, EtapasDaLuta - 1);
        vida = vidaMaxima - etapa * GolpesDaEtapa;
        vidaMostrada = vida;
        furiosa = etapa == EtapasDaLuta - 1;

        if (renasceu)
        {
            alturaDoCorpo = 0f;
            yield return Pronto();
        }
        else yield return Introducao();
        yield return Combate();
    }

    // O laço da luta (depois de cada pisão ele recomeça daqui, pela etapa e pela vida: veja Chefe.cs).
    protected override IEnumerator Combate()
    {
        while (true)
        {
            int vidaNoFimDaEtapa = vidaMaxima - (etapa + 1) * GolpesDaEtapa;
            while (vida > vidaNoFimDaEtapa)
            {
                int golpe = vidaMaxima - etapa * GolpesDaEtapa - vida; // 0 = 1º golpe da etapa, 1 = 2º
                yield return RodarRodada(Rodada(etapa * GolpesDaEtapa + golpe));
            }
            if (vida <= 0) break;

            etapa++;
            GerenciadorDoJogo.Instancia.EtapaDoChefe = etapa; // o "checkpoint" do chefe
            yield return NovaEtapa();
        }

        yield return SerDerrotada();
    }

    // Cada rodada é uma sequência FIXA de ataques que termina com ela CANSADA (a chance de dar dano).
    // Se você não pisar a tempo, ela acorda e a mesma rodada recomeça.
    IEnumerator Rodada(int qual)
    {
        switch (qual)
        {
            case 0: return Etapa1Rodada1();
            case 1: return Etapa1Rodada2();
            case 2: return Etapa2Rodada1();
            case 3: return Etapa2Rodada2();
            case 4: return Etapa3Rodada1();
            default: return Etapa3Rodada2();
        }
    }

    // ETAPA 1: um ataque de cada vez (o Louro ensina).
    IEnumerator Etapa1Rodada1()
    {
        yield return Chicotada(AlturaBaixa, AlturaBaixa);
        yield return Pausa();
        yield return Pancada(1);
        yield return Pausa();
        yield return Agarrao();
        yield return Pausa();
        yield return Cansar();
    }

    IEnumerator Etapa1Rodada2()
    {
        yield return Chicotada(AlturaAlta, AlturaAlta);
        yield return Pausa();
        yield return Agarrao();
        yield return Pausa();
        yield return Chicotada(AlturaBaixa, AlturaBaixa);
        yield return Pancada(1);
        yield return Pausa();
        yield return Cansar();
    }

    // ETAPA 2: a tinta, a volta da chicotada em outra altura e as pancadas em dupla.
    IEnumerator Etapa2Rodada1()
    {
        yield return Chicotada(AlturaBaixa, AlturaAlta);
        yield return Pausa();
        yield return Tinta(3);
        yield return Pancada(2);
        yield return Pausa();
        yield return Agarrao();
        yield return Pausa();
        yield return Cansar();
    }

    // Os ataques que vêm JUNTOS são sempre os que se desviam do mesmo jeito: a chicotada ALTA (fique no chão)
    // com a pancada ou a tinta (ande para o lado). O agarrão (que pede pulo) nunca vem junto com a pancada nem
    // com a tinta: pulando por cima dele, a pancada ou a tinta te pegariam no ar.
    IEnumerator Etapa2Rodada2()
    {
        Paralelo(Chicotada(AlturaAlta, AlturaAlta)); // a chicotada alta passa ENQUANTO as pancadas caem
        yield return Pancada(2);
        yield return EsperarTodos();
        yield return Pausa();
        yield return Agarrao();
        yield return Tinta(4);
        yield return Pausa();
        yield return Cansar();
    }

    // ETAPA 3: FURIOSA. Os ataques vêm colados uns nos outros, e os combos da etapa 2 ficam mais rápidos.
    IEnumerator Etapa3Rodada1()
    {
        yield return Chicotada(AlturaBaixa, AlturaAlta);
        Paralelo(Tinta(4));
        yield return Chicotada(AlturaAlta, AlturaAlta);
        yield return EsperarTodos();
        yield return Agarrao();
        yield return Pancada(2);
        yield return Pausa();
        yield return Cansar();
    }

    IEnumerator Etapa3Rodada2()
    {
        yield return Chicotada(AlturaAlta, AlturaBaixa);
        yield return Chicotada(AlturaBaixa, AlturaBaixa);
        yield return Agarrao();
        Paralelo(Chicotada(AlturaAlta, AlturaAlta));
        yield return Pancada(3);
        yield return EsperarTodos();
        yield return Tinta(5);
        yield return Pausa();
        yield return Cansar();
    }

    // ------------------------------------------------------------------ começo, etapas e fim

    // Só na primeira vez: a poça borbulha, ela SOBE e o Louro explica.
    IEnumerator Introducao()
    {
        GerenciadorDoJogo.Instancia.Avisar("A poça tá borbulhando...", 1.4f);
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            if (Random.value < 0.4f) Bolhas(new Vector2(xDoCorpo + Random.Range(-1.8f, 1.8f), chao - 0.5f), AmareloDoAviso, 1);
            yield return null;
        }
        GerenciadorDoJogo.Som("rugido", 0.8f);
        CameraSeguir.Tremer(0.25f, 1.2f);
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            alturaDoCorpo = Mathf.Lerp(-6f, 0f, Mathf.SmoothStep(0f, 1f, t / 1.2f));
            if (Random.value < 0.5f) Respingo(new Vector2(xDoCorpo + Random.Range(-2f, 2f), chao));
            yield return null;
        }
        alturaDoCorpo = 0f;
        GerenciadorDoJogo.Instancia.Avisar("O KRAKEN VERMELHO! (e ela tá de olho nas suas moedas)", 2.2f);
        GerenciadorDoJogo.Som("risada", 0.7f);
        yield return new WaitForSeconds(1.4f);
        GerenciadorDoJogo.Instancia.Avisar("Vermelho MATA. Amarelo ROUBA. Quando ela CANSAR e deitar no chão, pula na CABEÇA!", 3f);
        yield return new WaitForSeconds(1.6f);
    }

    // Depois de morrer: "PRONTO?" e a luta volta na etapa em que estava.
    IEnumerator Pronto()
    {
        GerenciadorDoJogo.Instancia.Avisar($"ETAPA {etapa + 1}/{EtapasDaLuta} - PRONTO?", 1.3f);
        GerenciadorDoJogo.Som("rugido", 0.35f);
        yield return new WaitForSeconds(1.5f); // um respiro: o 1º ataque não pode pegar quem acabou de nascer
    }

    IEnumerator NovaEtapa()
    {
        timerPiscar = 0.8f;
        if (etapa < EtapasDaLuta - 1)
        {
            GerenciadorDoJogo.Instancia.Avisar("ETAPA 2/3 - ela começou a cuspir TINTA!", 2f);
            GerenciadorDoJogo.Som("rugido", 0.6f);
            CameraSeguir.Tremer(0.15f, 0.6f);
            yield return new WaitForSeconds(1.3f);
            yield break;
        }

        GerenciadorDoJogo.Instancia.Avisar("ETAPA 3/3 - O KRAKEN FICOU FURIOSO!!", 2.2f);
        GerenciadorDoJogo.Som("rugido", 1f);
        GerenciadorDoJogo.Som("risada", 0.5f);
        CameraSeguir.Tremer(0.3f, 1f);
        furiosa = true;
        for (int i = 0; i < 28; i++)
        {
            float angulo = i * Mathf.PI * 2f / 28f;
            Particula.Criar(transform, corpo.transform.position + Vector3.up * 4f, new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * 5f, 0.7f, 0.5f, CorDaAura);
        }
        yield return new WaitForSeconds(1.5f);
    }

    IEnumerator SerDerrotada()
    {
        estado = Estado.Derrotada;
        zonas.Clear();
        ApagarEstrelas();
        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        Conquistas.Desbloquear("kraken_vencida");
        if (!jogo.KrakenRoubou) Conquistas.Desbloquear("kraken_sem_roubo");
        int devolveu = jogo.DevolverMoedas(jogo.MoedasComOKraken);
        // a partir daqui, se morrer (caiu na poça...), renasce DEPOIS da poça
        jogo.ChefeVencido(new Vector3(direita + 5.5f, chao + 0.5f, 0f));
        jogo.Avisar(devolveu > 0 ? $"O KRAKEN FUGIU! E devolveu {devolveu} moeda(s)!" : "O KRAKEN FUGIU PRO FUNDO DO MAR!", 3f);
        GerenciadorDoJogo.Som("pisao");
        GerenciadorDoJogo.Som("rugido", 0.7f);
        CameraSeguir.Tremer(0.25f, 1f);
        if (devolveu > 0) StartCoroutine(ChuvaDeMoedas(Mathf.Min(devolveu, 20)));

        // a cabeça escorrega de volta para a poça e ela afunda de vez (só sobem as bolhas)
        timerPiscar = 1.2f;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            cabeca.transform.position = new Vector3(Mathf.Lerp(XDaCabecaDeitada, XDaCabecaDeitada + 6f, t / 0.5f), chao, 0f);
            yield return null;
        }
        cabeca.enabled = false;
        corpo.enabled = false;
        for (float t = 0f; t < 1.4f; t += Time.deltaTime)
        {
            if (Random.value < 0.5f) Bolhas(new Vector2(xDoCorpo + Random.Range(-1.8f, 1.8f), chao - 0.3f), new Color(1f, 0.8f, 0.8f, 0.8f), 2);
            yield return null;
        }
        foreach (Tentaculo t in vermelhos) t.visivel = false;
        AbrirArena();
        CameraSeguir.Destravar();
        emCombate = false;
    }

    // ------------------------------------------------------------------ os ataques

    // CHICOTADA: a linha mostra a altura; a ponta sai da poça, varre a arena até a parede e volta.
    IEnumerator Chicotada(float alturaIda, float alturaVolta)
    {
        bool baixa = alturaIda < 1f;
        Dica(baixa ? "Linha BAIXA: PULA quando ela passar!" : "Linha ALTA: fica no CHÃO, não pula!");
        float y1 = chao + alturaIda, y2 = chao + alturaVolta;
        float inicio = direita + 0.6f, fim = esquerda + 0.5f;
        bool muda = Mathf.Abs(alturaVolta - alturaIda) > 0.1f;
        var linha = Linha(fim, direita, y1);
        var linhaDaVolta = muda ? Linha(fim, direita, y2) : null;
        if (linhaDaVolta != null) linhaDaVolta.color = VermelhoBemFraco;
        GerenciadorDoJogo.Som("armadilha", 0.5f);
        for (float t = 0f; t < Aviso; t += Time.deltaTime)
        {
            linha.color = Piscando(t, Aviso) ? Vermelho : VermelhoFraco;
            yield return null;
        }
        Destroy(linha.gameObject);

        Tentaculo tt = vermelhos[0];
        tt.raiz = new Vector2(direita + 2f, chao - 1f);
        tt.curva = -0.6f;
        tt.visivel = true;
        var zona = NovaZona(RaioDaPonta, false);
        GerenciadorDoJogo.Som("rugido", 0.3f);
        yield return MoverPonta(tt, zona, new Vector2(inicio, y1), new Vector2(fim, y1), VelocidadeDoChicote[etapa]);

        if (muda)
        {
            // na parede: a ponta para de machucar, a outra linha pisca forte e ela volta na outra altura
            zonas.Remove(zona);
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                linhaDaVolta.color = Piscando(t, 0.35f) ? Vermelho : VermelhoFraco;
                tt.fim = Vector2.Lerp(new Vector2(fim, y1), new Vector2(fim, y2), Mathf.SmoothStep(0f, 1f, t / 0.35f));
                yield return null;
            }
            Destroy(linhaDaVolta.gameObject);
            zona = NovaZona(RaioDaPonta, false);
        }
        else yield return new WaitForSeconds(0.12f);

        yield return MoverPonta(tt, zona, new Vector2(fim, y2), new Vector2(inicio, y2), VelocidadeDoChicote[etapa]);
        zonas.Remove(zona);
        tt.visivel = false;
    }

    // PANCADA: a marca no chão segue você, trava e o tentáculo despenca do céu ali (fica plantado um instante).
    IEnumerator Pancada(int vezes)
    {
        Dica("Saia da marca VERMELHA: vem tentáculo do céu!");
        for (int i = 0; i < vezes; i++)
        {
            float x = AlvoNoChao();
            var marca = Desenho("MarcaDaPancada", "baleia_alvo", new Vector3(x, chao + 0.15f, 0f), OrdemDosAvisos);
            marca.transform.localScale = new Vector3(RaioDaPonta * 2f + 0.3f, 1f, 1f);
            float seguir = i == 0 ? TempoSeguindo[etapa] : TempoSeguindo[etapa] * 0.7f;
            for (float t = 0f; t < seguir + Aviso; t += Time.deltaTime)
            {
                bool travou = t >= seguir;
                if (!travou) x = Mathf.MoveTowards(x, AlvoNoChao(), 9f * Time.deltaTime);
                marca.transform.position = new Vector3(x, chao + 0.15f, 0f);
                marca.color = !travou ? VermelhoFraco : Piscando(t - seguir, Aviso) ? Vermelho : VermelhoFraco;
                yield return null;
            }

            Tentaculo tt = vermelhos[proximaPancada];
            proximaPancada = proximaPancada == 1 ? 2 : 1; // duas pancadas seguidas usam braços diferentes
            tt.raiz = new Vector2(x + (x < (esquerda + direita) / 2f ? 2.5f : -2.5f), chao + 15f);
            tt.curva = 0.8f;
            tt.visivel = true;
            var zona = NovaZona(RaioDaPonta + 0.1f, false);
            yield return MoverPonta(tt, zona, new Vector2(x, chao + 13f), new Vector2(x, chao + 0.55f), 34f);
            GerenciadorDoJogo.Som("pancada", 0.8f);
            CameraSeguir.Tremer(0.12f, 0.2f);
            Efeitos.Poeira(transform, new Vector3(x - 0.5f, chao + 0.1f, 0f), 5, 2.5f);
            Efeitos.Poeira(transform, new Vector3(x + 0.5f, chao + 0.1f, 0f), 5, 2.5f);
            Destroy(marca.gameObject);
            yield return new WaitForSeconds(0.3f); // plantado (ainda mata quem entrar embaixo)
            zonas.Remove(zona);
            StartCoroutine(Recolher(tt, new Vector2(x, chao + 14f), 14f)); // sobe sozinho: a próxima pancada já pode vir
            if (i < vezes - 1) yield return new WaitForSeconds(0.1f);
        }
    }

    // AGARRÃO: bolhas amarelas na poça, e o tentáculo amarelo vem rente ao chão atrás de você. Não mata: ROUBA.
    IEnumerator Agarrao()
    {
        Dica("Tentáculo AMARELO: não mata, mas ROUBA suas moedas! Pula por cima!", 1);
        GerenciadorDoJogo.Som("armadilha", 0.4f);
        for (float t = 0f; t < 0.7f; t += Time.deltaTime)
        {
            if (Random.value < 0.5f) Bolhas(new Vector2(direita + Random.Range(0.3f, 1.6f), chao - 0.3f), AmareloDoAviso, 1);
            yield return null;
        }

        Tentaculo tt = amarelo;
        tt.raiz = new Vector2(direita + 1.6f, chao - 1f);
        tt.curva = 0.5f;
        tt.visivel = true;
        pegouNesteAgarrao = false;
        var zona = NovaZona(0.42f, true);
        float x = direita + 0.6f, altura = chao + 0.42f;
        GerenciadorDoJogo.Som("pulo", 0.5f);
        for (float t = 0f; t < 3.2f && x > esquerda + 0.6f && !pegouNesteAgarrao; t += Time.deltaTime)
        {
            // pulou por cima (ficou para trás da ponta)? Ela desiste e volta.
            if (GerenciadorDoJogo.JogadorVivo(out Vector2 jogador) && jogador.x > x + 1f) break;
            x -= VelocidadeDoAgarrao[etapa] * Time.deltaTime;
            tt.fim = new Vector2(x, altura + 0.08f * Mathf.Sin(t * 22f));
            zona.a = zona.b = tt.fim;
            yield return null;
        }
        zonas.Remove(zona);
        saco.enabled = pegouNesteAgarrao;
        yield return Recolher(tt, new Vector2(direita + 0.6f, altura), pegouNesteAgarrao ? 10f : 13f);
        saco.enabled = false;
    }

    // TINTA: as marcas mostram onde as bolas vão cair; ela cospe todas (uma atrás da outra) e elas caem nas marcas.
    IEnumerator Tinta(int quantas)
    {
        Dica("Bolas de TINTA: saia das marcas no chão!", 1);
        float[] desvios = { 0f, -2.6f, 2.6f, -5.2f, 5.2f };
        float jogadorX = GerenciadorDoJogo.JogadorVivo(out Vector2 jogador) ? jogador.x : (esquerda + direita) / 2f;
        var alvos = new float[quantas];
        var marcas = new SpriteRenderer[quantas];
        for (int i = 0; i < quantas; i++)
        {
            alvos[i] = Mathf.Clamp(jogadorX + desvios[i % desvios.Length], esquerda + 0.7f, direita - 0.7f);
            marcas[i] = Desenho("MarcaDaTinta", "baleia_alvo", new Vector3(alvos[i], chao + 0.15f, 0f), OrdemDosAvisos);
            marcas[i].transform.localScale = new Vector3(RaioDoRespingo * 2f, 1f, 1f);
        }
        GerenciadorDoJogo.Som("armadilha", 0.4f);
        for (float t = 0f; t < Aviso + 0.2f; t += Time.deltaTime)
        {
            foreach (SpriteRenderer marca in marcas) marca.color = Piscando(t, Aviso + 0.2f) ? Vermelho : VermelhoFraco;
            yield return null;
        }

        // o voo: cada bola sai 0,12 s depois da outra, numa parábola da boca dela até a marca
        var bolas = new SpriteRenderer[quantas];
        var zonasDasBolas = new Zona[quantas];
        var caiu = new bool[quantas];
        Vector2 boca = Boca;
        float total = DuracaoDaTinta + 0.12f * (quantas - 1);
        for (float t = 0f; t < total + 0.12f; t += Time.deltaTime)
        {
            for (int i = 0; i < quantas; i++)
            {
                float p = (t - 0.12f * i) / DuracaoDaTinta;
                if (p < 0f || caiu[i]) continue;
                if (bolas[i] == null)
                {
                    bolas[i] = Desenho("Tinta", "kraken_tinta", boca, OrdemDaPonta);
                    zonasDasBolas[i] = NovaZona(RaioDaTinta, false);
                    GerenciadorDoJogo.Som("pulo", 0.3f);
                }
                Vector2 lugar = Vector2.Lerp(boca, new Vector2(alvos[i], chao + 0.35f), Mathf.Min(p, 1f)) + Vector2.up * Mathf.Sin(Mathf.Min(p, 1f) * Mathf.PI) * 3.5f;
                bolas[i].transform.position = lugar;
                zonasDasBolas[i].a = zonasDasBolas[i].b = lugar;
                if (p >= 1f)
                {
                    caiu[i] = true;
                    StartCoroutine(Respingar(new Vector2(alvos[i], chao), zonasDasBolas[i]));
                    Destroy(bolas[i].gameObject);
                    Destroy(marcas[i].gameObject);
                }
            }
            yield return null;
        }
    }

    // A bola caiu: o respingo mata em volta por um instante e fica uma mancha (que não mata).
    IEnumerator Respingar(Vector2 lugar, Zona zona)
    {
        zona.raio = RaioDoRespingo;
        zona.a = zona.b = lugar + Vector2.up * 0.3f;
        GerenciadorDoJogo.Som("bloco", 0.6f);
        for (int i = 0; i < 8; i++)
            Particula.Criar(efeitos, lugar + Vector2.up * 0.2f, new Vector2(Random.Range(-3f, 3f), Random.Range(1.5f, 4f)), 0.5f, 0.35f, CorDaTinta);
        var mancha = Desenho("Mancha", "kraken_mancha", new Vector3(lugar.x, chao, 0f), 2);
        yield return new WaitForSeconds(0.12f);
        zonas.Remove(zona);
        for (float t = 0f; t < 1.2f && mancha != null; t += Time.deltaTime)
        {
            mancha.color = new Color(1f, 1f, 1f, 1f - t / 1.2f);
            yield return null;
        }
        if (mancha != null) Destroy(mancha.gameObject);
    }

    // CANSADA: ela afunda, deita a cabeça no convés (tonta, com estrelinhas) e espera. É a hora de pisar!
    IEnumerator Cansar()
    {
        GerenciadorDoJogo.Instancia.Avisar(etapa == 0 ? "ELA CANSOU! Pula na CABEÇA dela! (cuidado com a poça)" : "CANSOU! AGORA!", 1.6f);
        GerenciadorDoJogo.Som("risada", 0.25f);
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            alturaDoCorpo = Mathf.Lerp(0f, -9f, t / 0.35f);
            yield return null;
        }
        corpo.enabled = false;
        cabeca.enabled = true;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / 0.3f);
            cabeca.transform.position = new Vector3(Mathf.Lerp(XDaCabecaDeitada + 6f, XDaCabecaDeitada, p), chao, 0f);
            yield return null;
        }
        cabeca.transform.position = new Vector3(XDaCabecaDeitada, chao, 0f);
        GerenciadorDoJogo.Som("pancada", 0.6f);
        CameraSeguir.Tremer(0.1f, 0.25f);
        Efeitos.Poeira(transform, new Vector3(direita - 3f, chao + 0.1f, 0f), 6, 2.5f);
        CriarEstrelas();

        estado = Estado.Cansada;
        float duracao = TempoCansada[etapa];
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            // no finalzinho ela treme: vai acordar!
            float tremor = duracao - t < 0.6f ? Mathf.Sin(t * 70f) * 0.06f : 0f;
            cabeca.transform.position = new Vector3(XDaCabecaDeitada + tremor, chao, 0f);
            yield return null;
        }
        estado = Estado.Lutando;
        ApagarEstrelas();
        yield return Levantar();
        yield return Pausa();
    }

    // A cabeça volta para a poça e o corpo sobe de novo.
    IEnumerator Levantar()
    {
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            cabeca.transform.position = new Vector3(Mathf.Lerp(XDaCabecaDeitada, XDaCabecaDeitada + 6f, t / 0.3f), chao, 0f);
            yield return null;
        }
        cabeca.enabled = false;
        corpo.enabled = true;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            alturaDoCorpo = Mathf.Lerp(-9f, 0f, Mathf.SmoothStep(0f, 1f, t / 0.4f));
            yield return null;
        }
        alturaDoCorpo = 0f;
    }

    // Leva a ponta de a até b, em linha reta e com velocidade constante (a zona, se tiver, vai junto).
    IEnumerator MoverPonta(Tentaculo t, Zona zona, Vector2 a, Vector2 b, float velocidade)
    {
        t.fim = a;
        while (t.fim != b)
        {
            t.fim = Vector2.MoveTowards(t.fim, b, velocidade * Time.deltaTime);
            if (zona != null) zona.a = zona.b = t.fim;
            yield return null;
        }
    }

    // Recolhe o tentáculo (sem machucar) até "para" e esconde.
    IEnumerator Recolher(Tentaculo t, Vector2 para, float velocidade)
    {
        while (t.fim != para)
        {
            t.fim = Vector2.MoveTowards(t.fim, para, velocidade * Time.deltaTime);
            if (saco.enabled && t == amarelo) saco.transform.position = t.fim + Vector2.up * 0.5f;
            yield return null;
        }
        t.visivel = false;
    }

    // ------------------------------------------------------------------ o pisão e o roubo

    // Pulou na cabeça dela, caindo, por cima? Só vale com ela CANSADA (deitada no convés). Devolve true se acertou.
    bool TestarPisao()
    {
        if (!Vulneravel || estado != Estado.Cansada) return false;
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (!PesNaCabeca(jogador, direita - 3.4f, direita - 0.2f, chao + AlturaDaCabeca)) return false;
        LevarGolpe(jogador);
        return true;
    }

    void LevarGolpe(Jogador jogador)
    {
        vida--;
        timerPiscar = 0.8f;
        timerDor = 1.2f;
        jogador.Quicar(14f, true);
        jogador.Corpo.linearVelocity = new Vector2(-5f, jogador.Corpo.linearVelocity.y); // para longe da poça
        GerenciadorDoJogo.Som("pisao");
        GerenciadorDoJogo.Som("pancada", 0.6f);
        CameraSeguir.Tremer(0.2f, 0.3f);
        Efeitos.Poeira(transform, new Vector3(direita - 1.8f, chao + AlturaDaCabeca, 0f), 8, 3f);

        // ela devolve METADE do que roubou (as moedas voam de volta)
        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        int devolveu = vida > 0 ? jogo.DevolverMoedas((jogo.MoedasComOKraken + 1) / 2) : 0;

        string[] falas = { "ACERTOU! Ela ficou VERMELHA. Mais vermelha.", "Ela tá tonta! Mais um!", "SÓ MAIS UM! VAI, GRUMETE!" };
        if (devolveu > 0) jogo.Avisar($"Ela soltou {devolveu} moeda(s)!", 1.6f);
        else if (vida > 0 && vida % GolpesDaEtapa != 0) jogo.Avisar(falas[etapa], 1.6f);
        Golpeado(); // para a rodada (Chefe.cs): depois vêm o Arrumar, o Reagir e o Combate de novo
        if (devolveu > 0) StartCoroutine(ChuvaDeMoedas(Mathf.Min(devolveu, 12))); // (depois do Golpeado: ele para as corrotinas)
    }

    // O agarrão encostou: rouba (até) as moedas da etapa e te joga para trás. Não mata.
    void Roubar(Jogador jogador)
    {
        pegouNesteAgarrao = true;
        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        int levou = jogo.RoubarMoedas(MoedasPorAgarrao[etapa]);
        jogador.Corpo.linearVelocity = new Vector2(-9f, 9f); // o tranco
        CameraSeguir.Tremer(0.12f, 0.2f);
        if (levou > 0)
        {
            Conquistas.Desbloquear("kraken_roubou");
            jogo.Avisar($"O KRAKEN ROUBOU {levou} MOEDA(S)!", 1.8f);
            GerenciadorDoJogo.Som("risada", 0.8f);
            for (int i = 0; i < Mathf.Min(levou, 10); i++)
                Particula.Criar(efeitos, jogador.transform.position, new Vector2(Random.Range(0.5f, 4f), Random.Range(2f, 5f)), 0.6f, 0.4f, new Color(1f, 0.85f, 0.25f));
        }
        else
        {
            jogo.Avisar("Sem moedas?! Que pobreza. Ela só te empurrou.", 1.8f);
            GerenciadorDoJogo.Som("pancada", 0.5f);
        }
    }

    // As moedas devolvidas voando da cabeça dela para você (só o efeito: elas já foram para as suas moedas).
    IEnumerator ChuvaDeMoedas(int quantas)
    {
        for (int i = 0; i < quantas; i++)
        {
            StartCoroutine(Efeitos.MoedaSaltando(transform, new Vector3(direita - 1.8f + Random.Range(-1f, 1f), chao + 2.6f, 0f)));
            GerenciadorDoJogo.Som("moeda", 0.4f);
            yield return new WaitForSeconds(0.06f);
        }
    }

    // Um pisão interrompeu a rodada: apaga os avisos e ataques pela metade e esconde os tentáculos.
    protected override void Arrumar()
    {
        zonas.Clear();
        foreach (Transform filho in efeitos) Destroy(filho.gameObject); // avisos, marcas, tinta, estrelas...
        estrelas.Clear();
        for (int i = 0; i < 3; i++) vermelhos[i].visivel = false;
        amarelo.visivel = false;
        saco.enabled = false;
        if (estado == Estado.Cansada) estado = Estado.Lutando;
    }

    // Levou o pisão: a cabeça treme (cara de dor) e ela volta para a poça. Depois vem a próxima rodada.
    protected override IEnumerator Reagir()
    {
        GerenciadorDoJogo.Som("rugido", 0.5f);
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            cabeca.transform.position = new Vector3(XDaCabecaDeitada + Mathf.Sin(t * 60f) * 0.1f, chao, 0f);
            yield return null;
        }
        if (vida <= 0) yield break; // o último golpe: a cena da derrota começa daqui
        yield return Levantar();
        yield return Pausa();
    }

    // ------------------------------------------------------------------ ajudantes

    // Onde a pancada mira: em cima do Grumete (sem sair da arena).
    float AlvoNoChao()
    {
        if (!GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) return (esquerda + direita) / 2f;
        return Mathf.Clamp(jogador.x, esquerda + 0.7f, direita - 0.7f);
    }

    IEnumerator Pausa()
    {
        yield return new WaitForSeconds(Respiro[etapa]);
    }

    // Um desenho passageiro (aviso, marca, tinta...): fica em "efeitos", que o Arrumar apaga de uma vez.
    SpriteRenderer Desenho(string nome, string sprite, Vector3 lugar, int ordem) =>
        ConstrutorDeFase.Visual(nome, efeitos, lugar, sprite, ordem, false).GetComponent<SpriteRenderer>();

    // Uma linha de aviso deitada, de x1 até x2, na altura y.
    SpriteRenderer Linha(float x1, float x2, float y)
    {
        var linha = Desenho("AvisoDaChicotada", "baleia_linha", Vector3.zero, OrdemDosAvisos);
        Vector2 a = new Vector2(x1, y), b = new Vector2(x2, y);
        linha.transform.position = (a + b) / 2f;
        linha.transform.localScale = new Vector3((b - a).magnitude * FabricaDeSprites.PixelsPorUnidade, 1.4f, 1f);
        return linha;
    }

    // Avisos piscam, e piscam mais rápido no finalzinho ("vai AGORA").
    static bool Piscando(float t, float total) => Mathf.Repeat(t * (total - t < 0.2f ? 22f : 9f), 1f) < 0.6f;

    // Dicas do Louro: só até a etapa "ateAEtapa" e cada uma só uma vez por tentativa.
    void Dica(string texto, int ateAEtapa = 0)
    {
        if (etapa > ateAEtapa || !dicasMostradas.Add(texto)) return;
        GerenciadorDoJogo.Instancia.Avisar(texto, 1.3f);
    }

    void Bolhas(Vector2 lugar, Color cor, int quantas)
    {
        for (int i = 0; i < quantas; i++)
            Particula.Criar(efeitos, lugar, new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(1.5f, 3f)), 0.6f, 0.3f, cor);
    }

    void Respingo(Vector2 lugar)
    {
        Particula.Criar(efeitos, lugar, new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(3f, 6f)), 0.6f, 0.4f,
                        Random.value < 0.5f ? new Color(0.85f, 0.95f, 1f, 0.9f) : new Color(1f, 0.6f, 0.6f, 0.8f));
    }

    // ------------------------------------------------------------------ zonas que matam (ou roubam)

    Zona NovaZona(float raio, bool rouba)
    {
        var zona = new Zona { raio = raio, rouba = rouba };
        zona.a = zona.b = new Vector2(-9999f, -9999f); // longe de tudo até o ataque dizer onde está
        zonas.Add(zona);
        return zona;
    }

    void TestarPerigos()
    {
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (jogador == null || jogador.Morto || estado == Estado.Derrotada) return;
        Vector2 p = (Vector2)jogador.transform.position + new Vector2(0f, -0.02f); // centro da caixa dele

        // o Grumete vira uma "salsicha" em pé: 3 pontos no meio dele, com a meia largura de raio
        for (int i = 0; i < zonas.Count; i++)
        {
            Zona zona = zonas[i];
            if (zona.rouba && pegouNesteAgarrao) continue;
            bool encostou = false;
            for (int k = -1; k <= 1 && !encostou; k++)
            {
                Vector2 ponto = p + new Vector2(0f, k * (MeiaAlturaDoJogador - MeiaLarguraDoJogador));
                if (DistanciaAoSegmento(ponto, zona.a, zona.b) < zona.raio + MeiaLarguraDoJogador - FolgaDoAcerto) encostou = true;
            }
            if (!encostou) continue;
            if (zona.rouba)
            {
                Roubar(jogador);
                continue;
            }
            jogador.Morrer();
            return;
        }
    }

    static float DistanciaAoSegmento(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float comprimento2 = Vector2.Dot(ab, ab);
        float t = comprimento2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / comprimento2) : 0f;
        return Vector2.Distance(p, a + ab * t);
    }

    // ------------------------------------------------------------------ arena e efeitos
    // (os ataques "soltos", ao mesmo tempo, ficam em Chefe.cs: Paralelo, EsperarTodos e PararSoltos)

    // Uma parede de caixotes atrás de você e uma parede invisível na beira da poça (quem fica na frente é ela).
    void FecharArena()
    {
        var parede = new GameObject("ParedeDaArena");
        parede.transform.SetParent(transform, false);
        parede.transform.position = new Vector3(esquerda - 0.5f, chao + 7f, 0f);
        parede.AddComponent<BoxCollider2D>().size = new Vector2(1f, 20f);
        for (int linha = 0; linha < 13; linha++)
            ConstrutorDeFase.Visual("Tijolo", parede.transform, new Vector3(esquerda - 0.5f, chao + 0.5f + linha, 0f), "tijolo", 12, false);
        Efeitos.Poeira(transform, new Vector3(esquerda - 0.5f, chao + 0.3f, 0f), 6, 3f);
        paredes.Add(parede);

        var beira = new GameObject("BeiraDaPoca");
        beira.transform.SetParent(transform, false);
        beira.transform.position = new Vector3(direita + 0.5f, chao + 7f, 0f);
        beira.AddComponent<BoxCollider2D>().size = new Vector2(1f, 20f);
        paredes.Add(beira);
        GerenciadorDoJogo.Som("bloco");
    }

    void AbrirArena()
    {
        foreach (GameObject parede in paredes)
        {
            if (parede == null) continue;
            parede.GetComponent<BoxCollider2D>().enabled = false;
            StartCoroutine(Sumir(parede));
        }
        paredes.Clear();
    }

    static IEnumerator Sumir(GameObject parede)
    {
        SpriteRenderer[] desenhos = parede.GetComponentsInChildren<SpriteRenderer>();
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            foreach (SpriteRenderer d in desenhos) if (d != null) d.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }
        Destroy(parede);
    }

    // Estrelinhas girando em cima da cabeça enquanto ela está tonta.
    void CriarEstrelas()
    {
        for (int i = 0; i < 3; i++)
            estrelas.Add(Desenho("Estrela", "brilho", cabeca.transform.position, OrdemDasEstrelas).transform);
    }

    void GirarEstrelas()
    {
        Vector2 centro = new Vector2(direita - 1.2f, chao + AlturaDaCabeca + 0.4f);
        for (int i = 0; i < estrelas.Count; i++)
        {
            if (estrelas[i] == null) continue;
            float angulo = relogio * 5f + i * 2.1f;
            estrelas[i].position = centro + new Vector2(Mathf.Cos(angulo) * 0.8f, Mathf.Sin(angulo) * 0.2f);
        }
    }

    void ApagarEstrelas()
    {
        foreach (Transform estrela in estrelas) if (estrela != null) Destroy(estrela.gameObject);
        estrelas.Clear();
    }
}
