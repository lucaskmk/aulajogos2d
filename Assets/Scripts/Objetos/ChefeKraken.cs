using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 'g' - o KRAKEN VERMELHO, o chefe FINAL ("O Kraken Vermelho", fase 13). A lula gigante vermelha que prendeu a Marina,
// mandou a Baleia Branca atrás do Grumete e levantou o tsunami. Os desenhos ficam em ArteDoKraken.cs.
//
// O 'g' marca o começo da arena (na linha em que o jogador anda): são larguraDaArena blocos do convés de um navio em
// alto-mar (as tábuas são dela: ela cria e ela quebra) e, logo depois, o MAR (4 colunas sem chão no mapa), de onde ela sai. Entrou, a parede de caixotes fecha atrás de
// você, a câmera trava e a luta começa. É o mesmo esquema dos outros chefes (ChefeCapita.cs):
//  - tudo que é VERMELHO mata com UM toque, mas todo ataque AVISA antes (linha vermelha, marca no chão);
//  - os padrões são FIXOS: dá para decorar;
//  - são 3 ETAPAS de 2 golpes. Morreu? Volta na hora, no começo da arena, JÁ na etapa em que estava.
// Ela é o chefe FINAL: de longe a luta mais difícil do jogo. Uma lula tem muitos braços, e ela usa
// vários AO MESMO TEMPO (a chuva de tentáculos tem até 13 de uma vez).
//
// REGRA DE OURO: só a PONTA dos tentáculos mata (o braço é só o caminho dela), as colunas que brotam do chão,
// a tinta, o MAR (o convés quebrado) e o BICO dela (na sucção). O tentáculo AMARELO (o agarrão) NÃO mata: ele ROUBA moedas.
// COMO VENCER: depois de cada sequência de ataques ela CANSA e deita a cabeça no convés, tonta (olho em espiral):
// é a hora de pular na CABEÇA dela (3 / 2,6 / 2,2 s). Como todo chefe: 3 etapas de 2 golpes, e cada etapa é um checkpoint.
// Cuidado para não cair no mar!
//
// OS ATAQUES (o que deixa ela difícil não são números menores: são ataques que pedem COORDENAÇÃO)
//  RAJADA DE CHICOTADAS: uma linha vermelha mostra a altura de cada chicotada; o tentáculo varre a arena de um lado
//     ao outro (UMA vez só, e sai por cima). Linha BAIXA = PULE. Linha ALTA = fique no CHÃO. Elas vêm em sequência,
//     do mesmo lado e com o mesmo intervalo: em qualquer lugar da arena, o ritmo é o mesmo ("baixa, alta, baixa...").
//  PÊNDULO: um tentáculo pendurado lá em cima balança de parede a parede. No MEIO ele passa rente ao chão (pule quando
//     ele passar); perto das PAREDES ele passa alto (fique no chão). A trilha pontilhada mostra o caminho antes.
//  CONVÉS PODRE: as tábuas do convés racham e caem no mar SÓ onde você pisa (quase um segundo depois) e voltam
//     um segundo e meio depois. Fique se mexendo! Da etapa 2 em diante vem junto com uma rajada de chicotadas.
//  CHUVA DE TENTÁCULOS: ondas de até 13 tentáculos despencando do céu nas marcas vermelhas. Fique no buraco entre elas;
//     a onda seguinte cai nos buracos da anterior (ande meio passo).
//  ERUPÇÃO: igual à chuva, mas os tentáculos BROTAM do convés, em colunas altas (não dá para pular por cima).
//  AGARRÃO (amarelo): bolhas amarelas e o tentáculo vem rente ao chão atrás de você. Pule por cima! Ele não mata:
//     ROUBA moedas e te joga para trás. Da etapa 2 em diante vem um de cada lado (o AGARRÃO DUPLO).
//  TINTA: as marcas mostram onde as bolas de tinta vão cair. Saia delas.
//  SUCÇÃO (etapa 2 em diante): ela abre o bico e PUXA tudo para o mar. Segure para a esquerda: o bico mata.
//  TINTA NA CARA (etapa 2 em diante, como a lula do Mario Kart): ela cospe tinta na TELA, e borrões escuros escorrem
//     por cima de tudo por uns segundos (os ataques continuam: lembre o que você viu).
//  Combos: só vêm juntos ataques que se desviam do mesmo jeito (a chicotada ALTA, que pede chão, com a chuva, a
//     erupção ou a tinta, que pedem para andar). Na etapa 3 o pêndulo vem junto com a erupção (pule DENTRO do buraco).
//  ETAPA 3: ela fica FURIOSA (sobrancelhas e aura vermelha): tudo mais rápido e as rodadas mais longas.
//
// AS MOEDAS (o roubo): cada agarrão leva até 6 / 9 / 12 moedas (primeiro as que você pegou nesta tentativa, depois as
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

    static readonly float[] TempoDeAviso = { 0.5f, 0.44f, 0.4f };        // (nunca menos que 0,4: a chicotada baixa pede pulo)
    static readonly float[] TempoCansada = { 3f, 2.6f, 2.2f };            // a chance de pisar na cabeça dela (a janela padrão)
    static readonly float[] VelocidadeDoChicote = { 19f, 21f, 24f };     // blocos por segundo
    static readonly float[] IntervaloDaRajada = { 0.95f, 0.9f, 0.88f };   // entre uma chicotada e outra (o pulo dura ~0,82 s)
    static readonly float[] PeriodoDoPendulo = { 2.6f, 2.3f, 2.1f };      // uma ida e volta inteira (no meio, ele passa a cada meio período)
    static readonly float[] RachaEm = { 0.8f, 0.72f, 0.65f };            // o convés podre: pisou, a tábua cai isso depois
    static readonly float[] VelocidadeDoAgarrao = { 8.5f, 9.5f, 10.5f }; // (o Grumete anda 7: correndo, ele te alcança. Pule!)
    static readonly float[] Respiro = { 0.3f, 0.2f, 0.12f };              // pausa entre os ataques
    static readonly float[] ForcaDaSuccao = { 3.5f, 4f, 4.5f };          // o Grumete anda 7: segurando para a esquerda, ainda anda
    static readonly int[] MoedasPorAgarrao = { 6, 9, 12 };

    const float AlturaBaixa = 0.45f, AlturaAlta = 1.7f; // alturas da chicotada (centro da ponta, acima do chão)
    const float RaioDaPonta = 0.45f, RaioDaTinta = 0.38f, RaioDoRespingo = 0.8f, RaioDaChuva = 0.55f, RaioDaColuna = 0.42f;
    const float DuracaoDaTinta = 0.6f;                  // o voo de cada bola de tinta
    const float AlturaDaCabeca = 2.55f;                 // o topo do manto, deitada (onde se pisa)
    const float AlturaDaColuna = 3.6f;                  // a erupção (mais alta que o pulo)
    const float PlantadoNaChuva = 0.3f;                 // quanto tempo os tentáculos da chuva ficam cravados
    const float PivoDoPendulo = 13.5f, ComprimentoDoPendulo = 13f, AberturaDoPendulo = 55f; // (o pivô fica logo acima da tela)
    const float TabuaVolta = 1.5f;                   // a tábua que caiu volta depois disso
    const int CasasDaChuva = 14;                        // as "casas" da chuva e da erupção: uma a cada 1,5 bloco, de parede a parede
    const float EspacoDasCasas = 1.5f;

    // Ordem de desenho: o corpo fica ATRÁS dos blocos (some embaixo do convés, só aparece na poça).
    // Os borrões da TINTA NA CARA cobrem tudo, até os avisos.
    const int OrdemDoCorpo = -3, OrdemDaCabeca = 6, OrdemDosGomos = 7, OrdemDaPonta = 10,
              OrdemDosAvisos = 55, OrdemDasEstrelas = 58, OrdemDosBorroes = 62;

    // A "caixa" do Grumete que conta para morrer (um tiquinho menor que o desenho: é mais justo).
    const float MeiaLarguraDoJogador = 0.27f, MeiaAlturaDoJogador = 0.42f;
    const float FolgaDoAcerto = 0.15f; // de raspão não conta

    static readonly Color Vermelho = new Color(1f, 0.2f, 0.3f, 1f);
    static readonly Color VermelhoFraco = new Color(1f, 0.2f, 0.3f, 0.35f);
    static readonly Color AmareloDoAviso = new Color(1f, 0.85f, 0.2f, 1f);
    static readonly Color CorDaAura = new Color(1f, 0.25f, 0.3f, 0.7f);
    static readonly Color CorDaTinta = new Color(0.16f, 0.04f, 0.13f, 0.9f);
    static readonly Color CorDaAgua = new Color(0.7f, 0.85f, 1f, 0.8f);

    // Os padrões da chuva e da erupção (casas de 0 a 13, uma a cada 1,5 bloco; 1 = cai um tentáculo ali).
    // As casas vão de parede a parede: encostar num canto não salva. A onda seguinte sempre deixa livre uma casa
    // VIZINHA (ou a duas casas) de uma casa livre da anterior.
    const string Pares = "10101010101010", Impares = "01010101010101";
    static readonly string[] Alternada = { Pares, Impares };
    static readonly string[] AlternadaLonga = { Pares, Impares, Pares, Impares };
    static readonly string[] Cerco = { "11110000001111", "11111100111111", "11111011111111", Impares }; // fecha das pontas para o meio

    // ------------------------------------------------------------------ estado

    enum Estado { Esperando, Lutando, Cansada, Derrotada }
    Estado estado;
    int vida;
    float vidaMostrada;
    bool emCombate;
    int etapa;
    bool parado;          // o jogador morreu: tudo congela até a fase recomeçar
    bool furiosa;         // etapa 3
    float succao;         // > 0 enquanto ela está sugando (a força, em blocos por segundo)
    float chao, esquerda, direita, xDoCorpo;
    float relogio, timerPiscar, timerAura, timerDor;
    float alturaDoCorpo;  // onde o corpo está (em relação ao normal): 0 = de pé na poça, negativo = afundado

    SpriteRenderer corpo, cabeca, saco;

    // O convés da arena: uma tábua por coluna (com o pedaço do casco embaixo). No CONVÉS PODRE, a tábua em que
    // você pisa racha e cai no mar, e volta depois.
    enum EstadoDaTabua { Firme, Rachando, Caida }
    class Tabua
    {
        public BoxCollider2D colisor;
        public Transform desenho;  // a tábua e o casco embaixo dela (caem juntos)
        public float x, timer;
        public EstadoDaTabua estado;
    }
    readonly List<Tabua> tabuas = new List<Tabua>();
    bool convesPodre;     // as tábuas racham onde você pisa?
    Transform efeitos;    // pai de tudo que é passageiro (avisos, marcas, tinta...): some de uma vez no Arrumar

    // Um tentáculo: uma fileira de gomos da raiz até a ponta (numa curva), e a ponta (o "taco", que mata).
    // Os gomos são criados quando precisa (um tentáculo da chuva é curto; o da chicotada atravessa a arena).
    class Tentaculo
    {
        public readonly List<SpriteRenderer> gomos = new List<SpriteRenderer>();
        public SpriteRenderer ponta;
        public Vector2 raiz, fim;
        public float curva, fase;
        public bool visivel, emUso, amarelo, coluna;
    }
    const float DistanciaEntreGomos = 0.5f;
    readonly List<Tentaculo> tentaculos = new List<Tentaculo>(); // os de ataque (pegos e devolvidos)
    readonly List<Tentaculo> enfeites = new List<Tentaculo>();   // dois balançando na poça, dos lados dela

    // Uma área que mata (ou que ROUBA): tudo a menos de "raio" do segmento a-b. Igual à da Capitã.
    class Zona { public Vector2 a, b; public float raio; public bool rouba, pegou; }
    readonly List<Zona> zonas = new List<Zona>();

    readonly List<GameObject> paredes = new List<GameObject>();
    readonly List<Transform> estrelas = new List<Transform>();
    readonly HashSet<string> dicasMostradas = new HashSet<string>();

    float Aviso => TempoDeAviso[etapa];
    Vector3 LugarDoCorpo => new Vector3(xDoCorpo, chao - 1.2f + alturaDoCorpo + 0.15f * Mathf.Sin(relogio * 1.6f), 0f);
    Vector2 Boca => new Vector2(xDoCorpo - 0.6f, chao + 1.6f + alturaDoCorpo);
    // deitada: o desenho tem o pivô na ponta da direita, embaixo (veja ArteDoKraken)
    float XDaCabecaDeitada => direita + 0.6f;
    float XDaCasa(int casa) => esquerda + 0.5f + casa * EspacoDasCasas;

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

        // o convés da arena (as tábuas são criadas aqui: o mapa não tem chão embaixo da arena)
        for (int i = 0; i < Mathf.RoundToInt(larguraDaArena); i++) tabuas.Add(CriarTabua(i));

        for (int i = 0; i < 2; i++) enfeites.Add(CriarTentaculo(false, i * 1.7f));
        // os braços da chuva (até 13 de uma vez) já nascem prontos: criar centenas de gomos no meio da luta engasgaria
        for (int i = 0; i < 16; i++)
        {
            Tentaculo t = CriarTentaculo(false, i * 1.3f);
            t.raiz = Vector2.zero;
            t.fim = Vector2.up * 17f;
            t.visivel = true;
            Posicionar(t);
            t.visivel = false;
            Posicionar(t);
            tentaculos.Add(t);
        }
        Luzes.Ponto(corpo.transform, new Color(1f, 0.35f, 0.3f), 6f, 0.8f, Vector3.up * 3.5f); // (só nas fases escuras)

        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.Chefe = this;
    }

    Tentaculo CriarTentaculo(bool ehAmarelo, float fase)
    {
        var t = new Tentaculo { fase = fase, amarelo = ehAmarelo };
        t.ponta = ConstrutorDeFase.Visual("Ponta", transform, transform.position, ehAmarelo ? "kraken_ponta_amarela" : "kraken_ponta", OrdemDaPonta, false)
            .GetComponent<SpriteRenderer>();
        t.ponta.enabled = false;
        return t;
    }

    // Pega um tentáculo livre (cria um novo se todos estão ocupados). Devolva com Soltar quando ele sair de cena.
    Tentaculo Pegar(bool ehAmarelo, bool coluna = false)
    {
        Tentaculo livre = tentaculos.Find(t => !t.emUso && t.amarelo == ehAmarelo);
        if (livre == null)
        {
            livre = CriarTentaculo(ehAmarelo, tentaculos.Count * 1.3f);
            tentaculos.Add(livre);
        }
        livre.emUso = true;
        livre.visivel = true;
        livre.coluna = coluna;
        livre.ponta.flipX = false;
        return livre;
    }

    void Soltar(Tentaculo t)
    {
        t.emUso = false;
        t.visivel = false;
        Posicionar(t);
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
            succao = 0f;
        }
        if (parado) return;

        Animar();
        GirarEstrelas();
        if (TestarPisao()) return;
        TestarPerigos();
    }

    // A sucção puxa o Grumete para o mar (a força entra na física, como o vento: veja Jogador.Empurrar).
    void FixedUpdate()
    {
        if (succao <= 0f || parado) return;
        foreach (Jogador jogador in GerenciadorDoJogo.Jogadores) // (coop: puxa a tripulação inteira)
            if (jogador != null && !jogador.Morto) jogador.Empurrar(succao);
    }

    // O corpo boiando (e a cara certa), a aura da furiosa, a escuridão e os tentáculos.
    void Animar()
    {
        corpo.transform.position = LugarDoCorpo;
        timerDor -= Time.deltaTime;
        corpo.sprite = FabricaDeSprites.Pegar(timerDor > 0f ? "kraken_corpo_dor" : furiosa || succao > 0f ? "kraken_corpo_brava" : "kraken_corpo_normal");

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

        AnimarConves();

        // Dois tentáculos de enfeite balançando na poça, dos lados dela (nunca entram na arena: não machucam).
        for (int i = 0; i < enfeites.Count; i++)
        {
            Tentaculo t = enfeites[i];
            t.visivel = corpo.enabled && alturaDoCorpo > -3f;
            float lado = i == 0 ? -1f : 1f;
            t.raiz = new Vector2(xDoCorpo + lado * 1.3f, chao - 1.5f + alturaDoCorpo);
            t.fim = new Vector2(xDoCorpo + lado * (2.2f + 0.5f * Mathf.Sin(relogio * 1.3f + t.fase)),
                                chao + 2.5f + alturaDoCorpo + 0.8f * Mathf.Sin(relogio * 1.7f + t.fase));
            t.fim.x = Mathf.Max(t.fim.x, direita + 0.3f);
            t.curva = lado * 0.8f;
            Posicionar(t);
        }
        foreach (Tentaculo t in tentaculos) if (t.emUso) Posicionar(t);
    }

    // Os gomos vão da raiz até perto da ponta numa curva (com uma ondinha), e a ponta fica no fim, virada para fora.
    void Posicionar(Tentaculo t)
    {
        Vector2 a = t.raiz, b = t.fim;
        Vector2 d = b - a;
        float comprimento = d.magnitude;
        Vector2 deLado = comprimento > 0.01f ? new Vector2(-d.y, d.x) / comprimento : Vector2.up;
        Vector2 controle = (a + b) / 2f + deLado * t.curva;
        int quantos = t.visivel ? Mathf.Max(2, Mathf.CeilToInt(comprimento / DistanciaEntreGomos)) : 0;
        while (t.gomos.Count < quantos)
        {
            var gomo = ConstrutorDeFase.Visual("Gomo", transform, transform.position, t.amarelo ? "kraken_gomo_amarelo" : "kraken_gomo", OrdemDosGomos, false);
            t.gomos.Add(gomo.GetComponent<SpriteRenderer>());
        }
        for (int i = 0; i < t.gomos.Count; i++)
        {
            bool aparece = i < quantos;
            t.gomos[i].enabled = aparece;
            if (!aparece) continue;
            float s = (i + 0.5f) / quantos * 0.95f;
            Vector2 p = (1 - s) * (1 - s) * a + 2 * (1 - s) * s * controle + s * s * b;
            p += deLado * 0.14f * Mathf.Sin(relogio * 6f + i * 0.8f + t.fase) * Mathf.Sin(s * Mathf.PI);
            t.gomos[i].transform.position = p;
            t.gomos[i].transform.localScale = Vector3.one * (t.coluna ? 1.2f : Mathf.Lerp(1.35f, 0.75f, s));
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
        GerenciadorDoJogo.Instancia.PuxarParaAArena(esquerda, direita, PontoDeRenascer); // (coop: ninguém fica do lado de fora)
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

    const float B = AlturaBaixa, A = AlturaAlta; // (para escrever as rajadas de um jeito curto)

    // ETAPA 1: o pêndulo, o convés podre e as rajadas, cada um de uma vez.
    IEnumerator Etapa1Rodada1()
    {
        yield return Rajada(false, B, A, B);
        yield return Pausa();
        yield return Pendulo(4);
        yield return Pausa();
        yield return Chuva(Alternada);
        yield return Agarrao(false);
        yield return Pausa();
        yield return Cansar();
    }

    IEnumerator Etapa1Rodada2()
    {
        yield return ConvesPodre(5f, null);
        yield return Pausa();
        yield return Rajada(true, A, B, B);
        Paralelo(Chicotada(AlturaAlta, false));
        yield return Erupcao(Alternada);
        yield return EsperarTodos();
        yield return Agarrao(true);
        yield return Pausa();
        yield return Cansar();
    }

    // ETAPA 2: a tinta na cara, a sucção, o agarrão duplo e as chicotadas com o convés podre.
    IEnumerator Etapa2Rodada1()
    {
        yield return TintaNaCara();
        yield return Succao(3f, Tinta(5));
        Paralelo(Pendulo(5));
        yield return new WaitForSeconds(0.6f);
        yield return Tinta(4); // as bolas caem enquanto o pêndulo balança: ande desviando dele
        yield return EsperarTodos();
        yield return Rajada(false, B, B, A, B);
        yield return AgarraoDuplo();
        yield return Pausa();
        yield return Cansar();
    }

    IEnumerator Etapa2Rodada2()
    {
        yield return ConvesPodre(5.5f, Rajada(false, A, A, A, A)); // as tábuas caem onde você pisa e as chicotadas ALTAS pedem chão: ande sem pular
        yield return Rajada(true, B, A, B, B);
        Paralelo(Chicotada(AlturaAlta, false));
        yield return Chuva(Cerco);
        yield return EsperarTodos();
        yield return AgarraoDuplo();
        yield return Pausa();
        yield return Cansar();
    }

    // ETAPA 3: FURIOSA. Os ataques de coordenação JUNTOS.
    IEnumerator Etapa3Rodada1()
    {
        yield return TintaNaCara();
        yield return ConvesPodre(7f, Rajada(false, B, A, B, A, B)); // pulando as baixas e andando nas altas, sem parar em cima de tábua nenhuma
        yield return Rajada(false, B, A, B, A, B);
        yield return Succao(3f, Erupcao(new[] { Impares, Pares, Impares }));
        yield return AgarraoDuplo();
        yield return Pausa();
        yield return Cansar();
    }

    IEnumerator Etapa3Rodada2()
    {
        Paralelo(Pendulo(7));
        yield return new WaitForSeconds(0.4f);
        yield return Erupcao(AlternadaLonga); // as colunas brotam enquanto o pêndulo passa: pule DENTRO do buraco
        yield return EsperarTodos();
        yield return TintaNaCara();
        Paralelo(Chicotada(AlturaAlta, true));
        yield return Chuva(Cerco);
        yield return EsperarTodos();
        yield return Succao(2.5f, Tinta(7));
        yield return AgarraoDuplo();
        yield return Pausa();
        yield return Cansar();
    }

    // ------------------------------------------------------------------ começo, etapas e fim

    // Só na primeira vez: a poça borbulha, ela SOBE e o Louro explica.
    IEnumerator Introducao()
    {
        GerenciadorDoJogo.Instancia.Avisar("O mar tá borbulhando...", 1.4f);
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
        GerenciadorDoJogo.Instancia.Avisar("O KRAKEN VERMELHO! E ela tem MUITOS braços.", 2.2f);
        GerenciadorDoJogo.Som("risada", 0.7f);
        yield return new WaitForSeconds(1.4f);
        GerenciadorDoJogo.Instancia.Avisar("Vermelho MATA. Amarelo ROUBA. Quando ela CANSAR e deitar no chão, pula na CABEÇA!", 3f);
        yield return new WaitForSeconds(1.6f);
    }

    // Depois de morrer: "PRONTO?" e a luta volta na etapa em que estava.
    IEnumerator Pronto()
    {
        GerenciadorDoJogo.Instancia.Avisar(Idioma.T($"ETAPA {etapa + 1}/{EtapasDaLuta} - PRONTO?", $"STAGE {etapa + 1}/{EtapasDaLuta} - READY?"), 1.3f);
        GerenciadorDoJogo.Som("rugido", 0.35f);
        yield return new WaitForSeconds(1.5f); // um respiro: o 1º ataque não pode pegar quem acabou de nascer
    }

    IEnumerator NovaEtapa()
    {
        timerPiscar = 0.8f;
        if (etapa < EtapasDaLuta - 1)
        {
            GerenciadorDoJogo.Instancia.Avisar("ETAPA 2/3 - ela vai SUGAR você e jogar TINTA na sua cara!", 2.2f);
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
        succao = 0f;
        ConsertarConves();
        ApagarEstrelas();
        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        Conquistas.Desbloquear("kraken_vencida");
        if (!jogo.KrakenRoubou) Conquistas.Desbloquear("kraken_sem_roubo");
        int devolveu = jogo.DevolverMoedas(jogo.MoedasComOKraken);
        // a partir daqui, se morrer (caiu no mar...), renasce DEPOIS do mar
        jogo.ChefeVencido(new Vector3(direita + 5.5f, chao + 0.5f, 0f));
        jogo.Avisar(devolveu > 0 ? Idioma.T($"O KRAKEN FUGIU! E devolveu {devolveu} moeda(s)!", $"THE KRAKEN FLED! And gave back {devolveu} coin(s)!")
            : "O KRAKEN FUGIU PRO FUNDO DO MAR!", 3f);
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
        foreach (Tentaculo t in enfeites) { t.visivel = false; Posicionar(t); }
        AbrirArena();
        CameraSeguir.Destravar();
        emCombate = false;
    }

    // ------------------------------------------------------------------ os ataques

    // RAJADA: várias chicotadas seguidas, do MESMO lado, com o MESMO intervalo (em qualquer lugar da arena, uma passa
    // "intervalo" segundos depois da outra). alturas: B = baixa (pule), A = alta (fique no chão).
    IEnumerator Rajada(bool daEsquerda, params float[] alturas)
    {
        Dica("Linha BAIXA: PULA quando a ponta passar! Linha ALTA: fica no CHÃO!");
        for (int i = 0; i < alturas.Length; i++)
        {
            Paralelo(Chicotada(alturas[i], daEsquerda));
            if (i < alturas.Length - 1) yield return new WaitForSeconds(IntervaloDaRajada[etapa]);
        }
        yield return EsperarTodos();
    }

    // CHICOTADA: a linha mostra a altura (e de que lado ela vem); a ponta varre a arena de um lado ao outro, UMA vez,
    // e sai por cima. O aviso vem ANTES do tempo da chicotada (a ponta sai exatamente quando o aviso acaba).
    IEnumerator Chicotada(float altura, bool daEsquerda)
    {
        float y = chao + altura;
        float de = daEsquerda ? esquerda - 0.8f : direita + 0.8f, ate = daEsquerda ? direita + 0.8f : esquerda - 0.8f;
        var linha = Linha(esquerda, direita, y);
        var seta = Desenho("SetaDaChicotada", "aviso", new Vector3(daEsquerda ? esquerda + 0.6f : direita - 0.6f, y + 0.9f, 0f), OrdemDosAvisos);
        GerenciadorDoJogo.Som("armadilha", 0.4f);
        for (float t = 0f; t < Aviso; t += Time.deltaTime)
        {
            Color cor = Piscando(t, Aviso) ? Vermelho : VermelhoFraco;
            linha.color = seta.color = cor;
            yield return null;
        }
        Destroy(linha.gameObject);
        Destroy(seta.gameObject);

        Tentaculo tt = Pegar(false);
        tt.raiz = daEsquerda ? new Vector2(esquerda - 3f, chao + 9f) : new Vector2(direita + 2f, chao - 1f);
        tt.curva = daEsquerda ? 1.2f : -0.6f;
        var zona = NovaZona(RaioDaPonta, false);
        GerenciadorDoJogo.Som("rugido", 0.25f);
        yield return MoverPonta(tt, zona, new Vector2(de, y), new Vector2(ate, y), VelocidadeDoChicote[etapa]);
        zonas.Remove(zona);
        yield return Recolher(tt, new Vector2(ate, chao + 12f), 30f); // sai por cima (não machuca mais)
    }

    // CHUVA: cada onda é um padrão de casas (veja Pares, Impares...): as marcas piscam e os tentáculos despencam do
    // céu, ficam cravados um instante e sobem. A onda seguinte já começa a avisar enquanto a anterior está cravada.
    IEnumerator Chuva(string[] ondas)
    {
        Dica("CHUVA DE TENTÁCULOS: fica no BURACO entre as marcas! A próxima onda cai nos buracos.");
        foreach (string onda in ondas)
        {
            var casas = Casas(onda);
            var marcas = Marcar(casas, RaioDaChuva);
            yield return PiscarMarcas(marcas, Aviso + 0.1f + (succao > 0f ? 0.45f : 0f));
            var queda = new List<Tentaculo>();
            var zonasDaOnda = new List<Zona>();
            foreach (int casa in casas)
            {
                float x = XDaCasa(casa);
                Tentaculo tt = Pegar(false);
                tt.raiz = new Vector2(x + (casa % 2 == 0 ? 0.8f : -0.8f), chao + 16f);
                tt.curva = 0.4f;
                tt.fim = new Vector2(x, chao + 13f);
                queda.Add(tt);
                zonasDaOnda.Add(NovaZona(RaioDaChuva, false));
            }
            // todos caem juntos
            for (float y = chao + 13f; ; )
            {
                y = Mathf.MoveTowards(y, chao + 0.55f, 42f * Time.deltaTime);
                for (int i = 0; i < queda.Count; i++)
                {
                    queda[i].fim = new Vector2(queda[i].fim.x, y);
                    zonasDaOnda[i].a = zonasDaOnda[i].b = queda[i].fim;
                }
                if (y <= chao + 0.55f) break;
                yield return null;
            }
            foreach (SpriteRenderer marca in marcas) Destroy(marca.gameObject);
            GerenciadorDoJogo.Som("pancada", 0.9f);
            CameraSeguir.Tremer(0.14f, 0.2f);
            foreach (Tentaculo tt in queda) Efeitos.Poeira(transform, new Vector3(tt.fim.x, chao + 0.1f, 0f), 3, 2f);
            yield return new WaitForSeconds(PlantadoNaChuva);
            foreach (Zona zona in zonasDaOnda) zonas.Remove(zona);
            foreach (Tentaculo tt in queda) StartCoroutine(RecolherESoltar(tt, new Vector2(tt.fim.x, chao + 15f), 30f));
        }
    }

    // ERUPÇÃO: como a chuva, mas os tentáculos BROTAM do convés em colunas altas (a coluna inteira mata).
    IEnumerator Erupcao(string[] ondas)
    {
        Dica("O CHÃO! Os tentáculos vão BROTAR nas marcas: fica no buraco!", 1);
        foreach (string onda in ondas)
        {
            var casas = Casas(onda);
            var marcas = Marcar(casas, RaioDaColuna);
            foreach (int casa in casas) Bolhas(new Vector2(XDaCasa(casa), chao + 0.1f), new Color(1f, 0.5f, 0.45f, 0.9f), 2);
            // durante a sucção, andar para a esquerda é mais lento: o aviso fica mais longo
            yield return PiscarMarcas(marcas, Aviso + 0.1f + (succao > 0f ? 0.45f : 0f));
            var colunas = new List<Tentaculo>();
            var zonasDaOnda = new List<Zona>();
            foreach (int casa in casas)
            {
                float x = XDaCasa(casa);
                Tentaculo tt = Pegar(false, coluna: true);
                tt.raiz = new Vector2(x, chao - 2.5f);
                tt.curva = 0.15f;
                tt.fim = new Vector2(x, chao - 0.5f);
                colunas.Add(tt);
                zonasDaOnda.Add(NovaZona(RaioDaColuna, false));
            }
            foreach (SpriteRenderer marca in marcas) Destroy(marca.gameObject);
            GerenciadorDoJogo.Som("pancada", 0.7f);
            CameraSeguir.Tremer(0.12f, 0.25f);
            for (float y = chao - 0.5f; ; )
            {
                y = Mathf.MoveTowards(y, chao + AlturaDaColuna, 30f * Time.deltaTime);
                for (int i = 0; i < colunas.Count; i++)
                {
                    colunas[i].fim = new Vector2(colunas[i].fim.x, y);
                    zonasDaOnda[i].a = new Vector2(colunas[i].fim.x, chao);
                    zonasDaOnda[i].b = colunas[i].fim;
                }
                if (y >= chao + AlturaDaColuna) break;
                yield return null;
            }
            yield return new WaitForSeconds(PlantadoNaChuva);
            foreach (Zona zona in zonasDaOnda) zonas.Remove(zona);
            foreach (Tentaculo tt in colunas) StartCoroutine(RecolherESoltar(tt, new Vector2(tt.fim.x, chao - 2f), 22f));
        }
    }

    // AGARRÃO: bolhas amarelas e o tentáculo amarelo vem rente ao chão atrás de você. Não mata: ROUBA.
    // daEsquerda: ele vem por cima da parede de caixotes, do outro lado.
    IEnumerator Agarrao(bool daEsquerda)
    {
        Dica("Tentáculo AMARELO: não mata, mas ROUBA suas moedas! Pula por cima!");
        GerenciadorDoJogo.Som("armadilha", 0.4f);
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            if (Random.value < 0.5f)
                Bolhas(new Vector2(daEsquerda ? esquerda + Random.Range(0.2f, 1f) : direita + Random.Range(0.3f, 1.6f), chao + (daEsquerda ? 0.3f : -0.3f)), AmareloDoAviso, 1);
            yield return null;
        }

        Tentaculo tt = Pegar(true);
        tt.raiz = daEsquerda ? new Vector2(esquerda - 2.5f, chao + 7f) : new Vector2(direita + 1.6f, chao - 1f);
        tt.curva = daEsquerda ? 1f : 0.5f;
        var zona = NovaZona(0.42f, true);
        float sentido = daEsquerda ? 1f : -1f;
        float x = daEsquerda ? esquerda + 0.2f : direita + 0.6f, altura = chao + 0.42f;
        float limite = daEsquerda ? direita - 0.6f : esquerda + 0.6f;
        GerenciadorDoJogo.Som("pulo", 0.5f);
        for (float t = 0f; t < 3f && (x - limite) * sentido < 0f && !zona.pegou; t += Time.deltaTime)
        {
            // pulou por cima (ficou para trás da ponta)? Ele desiste e volta.
            if (GerenciadorDoJogo.AlvoDoChefe(out Vector2 jogador) && (x - jogador.x) * sentido > 1f) break;
            x += sentido * VelocidadeDoAgarrao[etapa] * Time.deltaTime;
            tt.fim = new Vector2(x, altura + 0.08f * Mathf.Sin(t * 22f));
            zona.a = zona.b = tt.fim;
            yield return null;
        }
        zonas.Remove(zona);
        bool levou = zona.pegou;
        if (levou) saco.enabled = true;
        yield return Recolher(tt, daEsquerda ? new Vector2(esquerda - 0.5f, chao + 4f) : new Vector2(direita + 0.6f, altura), levou ? 11f : 15f, levou);
        if (levou) saco.enabled = false;
        Soltar(tt);
    }

    // AGARRÃO DUPLO: um de cada lado, ao mesmo tempo.
    IEnumerator AgarraoDuplo()
    {
        Dica("AGARRÃO DUPLO! Um de cada lado: pula os dois!", 1);
        Paralelo(Agarrao(true));
        yield return Agarrao(false);
        yield return EsperarTodos();
    }

    // TINTA: as marcas mostram onde as bolas vão cair; ela cospe todas (uma atrás da outra) e elas caem nas marcas.
    IEnumerator Tinta(int quantas)
    {
        Dica("Bolas de TINTA: saia das marcas no chão!", 1);
        float[] desvios = { 0f, -2.4f, 2.4f, -4.8f, 4.8f, -7.2f, 7.2f };
        float jogadorX = GerenciadorDoJogo.AlvoDoChefe(out Vector2 jogador) ? jogador.x : (esquerda + direita) / 2f;
        var alvos = new float[quantas];
        var marcas = new SpriteRenderer[quantas];
        for (int i = 0; i < quantas; i++)
        {
            alvos[i] = Mathf.Clamp(jogadorX + desvios[i % desvios.Length], esquerda + 0.7f, direita - 0.7f);
            marcas[i] = Desenho("MarcaDaTinta", "baleia_alvo", new Vector3(alvos[i], chao + 0.15f, 0f), OrdemDosAvisos);
            marcas[i].transform.localScale = new Vector3(LarguraDaMarca(RaioDoRespingo), 1f, 1f);
        }
        GerenciadorDoJogo.Som("armadilha", 0.4f);
        for (float t = 0f; t < Aviso + 0.2f; t += Time.deltaTime)
        {
            foreach (SpriteRenderer marca in marcas) marca.color = Piscando(t, Aviso + 0.2f) ? Vermelho : VermelhoFraco;
            yield return null;
        }

        // o voo: cada bola sai 0,1 s depois da outra, numa parábola da boca dela até a marca
        var bolas = new SpriteRenderer[quantas];
        var zonasDasBolas = new Zona[quantas];
        var caiu = new bool[quantas];
        Vector2 boca = Boca;
        float total = DuracaoDaTinta + 0.1f * (quantas - 1);
        for (float t = 0f; t < total + 0.1f; t += Time.deltaTime)
        {
            for (int i = 0; i < quantas; i++)
            {
                float p = (t - 0.1f * i) / DuracaoDaTinta;
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

    // PÊNDULO: um tentáculo pendurado logo acima da tela balança de parede a parede. No meio ele passa rente ao chão
    // (pule quando ele passar); perto das paredes ele passa alto (fique no chão). "meiosBalancos": quantas vezes ele
    // atravessa a arena. A trilha pontilhada mostra o caminho antes de ele começar.
    IEnumerator Pendulo(int meiosBalancos)
    {
        Dica("PÊNDULO! No MEIO, pula quando ele passar. Perto das PAREDES, fica no CHÃO.", 1);
        Vector2 pivo = new Vector2((esquerda + direita) / 2f, chao + PivoDoPendulo);
        float abertura = AberturaDoPendulo * Mathf.Deg2Rad;
        Vector2 Ponta(float angulo) => pivo + new Vector2(Mathf.Sin(angulo), -Mathf.Cos(angulo)) * ComprimentoDoPendulo;

        // a trilha (pontinhos piscando) e o tentáculo descendo até a ponta da esquerda
        var trilha = new List<SpriteRenderer>();
        for (int i = 0; i <= 14; i++)
        {
            var ponto = Desenho("TrilhaDoPendulo", "brilho", Ponta(Mathf.Lerp(-abertura, abertura, i / 14f)), OrdemDosAvisos);
            ponto.transform.localScale = Vector3.one * 0.6f;
            trilha.Add(ponto);
        }
        Tentaculo tt = Pegar(false);
        tt.raiz = pivo;
        tt.curva = 0f;
        GerenciadorDoJogo.Som("armadilha", 0.5f);
        float aviso = Aviso + 0.4f;
        for (float t = 0f; t < aviso; t += Time.deltaTime)
        {
            foreach (SpriteRenderer ponto in trilha) ponto.color = Piscando(t, aviso) ? Vermelho : VermelhoFraco;
            tt.fim = Vector2.Lerp(pivo, Ponta(-abertura), t / aviso);
            yield return null;
        }
        foreach (SpriteRenderer ponto in trilha) Destroy(ponto.gameObject);

        // o balanço: ângulo = -abertura * cos(2 pi t / período) (começa na ponta da esquerda, alto)
        float periodo = PeriodoDoPendulo[etapa];
        var zona = NovaZona(RaioDaPonta + 0.05f, false);
        float duracao = meiosBalancos * periodo / 2f;
        float ultimoLado = -1f;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float fase = 2f * Mathf.PI * t / periodo;
            float angulo = -abertura * Mathf.Cos(fase);
            tt.fim = Ponta(angulo);
            tt.curva = -Mathf.Sin(fase) * 1.4f; // o braço "entorta" para trás do movimento
            zona.a = zona.b = tt.fim;
            float lado = Mathf.Sign(angulo);
            if (lado != ultimoLado) { GerenciadorDoJogo.Som("rugido", 0.2f); ultimoLado = lado; } // o "vuuush" no meio
            yield return null;
        }
        zonas.Remove(zona);
        yield return Recolher(tt, pivo, 25f);
    }

    // CONVÉS PODRE: por "duracao" segundos, a tábua em que você pisa racha e cai no mar (e volta depois).
    // "junto": um ataque que acontece ENQUANTO o convés está podre (a rajada de chicotadas, nas etapas 2 e 3).
    IEnumerator ConvesPodre(float duracao, IEnumerator junto)
    {
        Dica("O CONVÉS TÁ PODRE! Onde você pisa, cai. Não fica parado!", 2);
        if (etapa > 0) GerenciadorDoJogo.Instancia.Avisar("O convés tá rachando! Não para!", 1.4f);
        GerenciadorDoJogo.Som("bloco", 0.8f);
        CameraSeguir.Tremer(0.08f, 0.4f);
        yield return new WaitForSeconds(0.5f);
        convesPodre = true;
        float comecou = Time.time;
        if (junto != null) yield return junto;
        while (Time.time - comecou < duracao) yield return null;
        convesPodre = false;
    }

    Tabua CriarTabua(int coluna)
    {
        float x = esquerda + 0.5f + coluna;
        var objeto = new GameObject("Tabua");
        objeto.transform.SetParent(transform, false);
        objeto.transform.position = new Vector3(x, chao - 0.5f, 0f);
        var colisor = objeto.AddComponent<BoxCollider2D>();
        colisor.size = Vector2.one;
        var desenho = new GameObject("Desenho").transform;
        desenho.SetParent(objeto.transform, false);
        bool ultima = coluna == Mathf.RoundToInt(larguraDaArena) - 1; // a beirada do convés, do lado do mar
        // a tábua (com o estilo do chão da fase) e o casco embaixo
        ConstrutorDeFase.Visual("Conves", desenho, objeto.transform.position, ArteDosBlocos.NomeDoChao(0, coluna, false, ultima, false), 0, false, true);
        ConstrutorDeFase.Visual("Casco", desenho, objeto.transform.position + Vector3.down, ArteDosBlocos.NomeDoChao(1, coluna, false, ultima, true), 0, false, true);
        return new Tabua { colisor = colisor, desenho = desenho, x = x };
    }

    // Racha a tábua em que o Grumete pisa (no convés podre), derruba as rachadas e traz de volta as que caíram.
    void AnimarConves()
    {
        foreach (Tabua tabua in tabuas)
        {
            switch (tabua.estado)
            {
                case EstadoDaTabua.Firme:
                    if (convesPodre && AlguemPisando(tabua.x))
                    {
                        tabua.estado = EstadoDaTabua.Rachando;
                        tabua.timer = RachaEm[etapa];
                        GerenciadorDoJogo.Som("bloco", 0.3f);
                    }
                    break;
                case EstadoDaTabua.Rachando:
                    tabua.timer -= Time.deltaTime;
                    tabua.desenho.localPosition = new Vector3(Mathf.Sin(Time.time * 70f) * 0.05f, 0f, 0f); // treme
                    if (tabua.timer <= 0f)
                    {
                        tabua.estado = EstadoDaTabua.Caida;
                        tabua.timer = TabuaVolta;
                        tabua.colisor.enabled = false;
                        Efeitos.Poeira(transform, new Vector3(tabua.x, chao, 0f), 3, 2f);
                    }
                    break;
                case EstadoDaTabua.Caida:
                    tabua.timer -= Time.deltaTime;
                    float caiu = TabuaVolta - tabua.timer;
                    tabua.desenho.localPosition = new Vector3(0f, -Mathf.Min(caiu * caiu * 18f, 12f), 0f); // despenca no mar
                    if (tabua.timer <= 0f) VoltarTabua(tabua);
                    break;
            }
        }
    }

    // Tem um Grumete em pé nesta tábua do convés? (no coop, qualquer um da tripulação racha a tábua)
    bool AlguemPisando(float xDaTabua)
    {
        foreach (Jogador jogador in GerenciadorDoJogo.Jogadores)
        {
            if (jogador == null || jogador.Morto || !jogador.NoChao) continue;
            Vector2 pes = (Vector2)jogador.transform.position + Vector2.down * 0.48f;
            if (Mathf.Abs(pes.x - xDaTabua) < 0.8f && Mathf.Abs(pes.y - chao) < 0.2f) return true;
        }
        return false;
    }

    void VoltarTabua(Tabua tabua)
    {
        tabua.estado = EstadoDaTabua.Firme;
        tabua.colisor.enabled = true;
        tabua.desenho.localPosition = Vector3.zero;
        Efeitos.Poeira(transform, new Vector3(tabua.x, chao + 0.1f, 0f), 3, 1.5f);
    }

    // O convés inteiro de volta (fim da luta, pisão): nenhuma tábua fica quebrada.
    void ConsertarConves()
    {
        convesPodre = false;
        foreach (Tabua tabua in tabuas) if (tabua.estado != EstadoDaTabua.Firme) VoltarTabua(tabua);
    }

    // SUCÇÃO: ela abre o bico e puxa tudo para a poça por "duracao" segundos (segure para a esquerda: o bico, na beira
    // da poça, mata). "junto": um ataque de andar (a tinta, a erupção) que acontece ENQUANTO ela suga.
    IEnumerator Succao(float duracao, IEnumerator junto)
    {
        Dica("Ela tá SUGANDO! Segura pra ESQUERDA, longe do bico!", 2);
        GerenciadorDoJogo.Som("rugido", 0.7f);
        // o aviso: a água começa a correr para a poça antes de puxar de verdade
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            Correnteza();
            yield return null;
        }
        succao = ForcaDaSuccao[etapa];
        var bico = NovaZona(0.6f, false);
        bico.a = new Vector2(direita - 0.2f, chao + 0.3f);
        bico.b = new Vector2(direita - 0.2f, chao + 2.8f);
        CameraSeguir.Tremer(0.06f, duracao);
        if (junto != null) Paralelo(junto);
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            Correnteza();
            yield return null;
        }
        yield return EsperarTodos();
        succao = 0f;
        zonas.Remove(bico);
    }

    // As gotinhas correndo para a poça (o vento da sucção).
    void Correnteza()
    {
        if (Random.value > 0.6f) return;
        var lugar = new Vector2(Random.Range(esquerda, direita), chao + Random.Range(0.1f, 4f));
        Particula.Criar(efeitos, lugar, new Vector2(Random.Range(6f, 10f), Random.Range(-0.5f, 0.5f)), 0.4f, 0.25f, CorDaAgua);
    }

    // TINTA NA CARA (como a lula do Mario Kart): ela cospe uma bola de tinta na TELA, e borrões escuros grudam por cima
    // de tudo (até dos avisos), escorrem devagar e somem. Os ataques seguintes começam por baixo deles.
    // Os borrões são filhos da câmera (ficam parados na tela) e nunca tampam o meio da tela inteiro.
    IEnumerator TintaNaCara()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;
        GerenciadorDoJogo.Instancia.Avisar("TINTA NA CARA! Lembra onde estão as coisas!", 1.4f);
        GerenciadorDoJogo.Som("risada", 0.6f);

        // a bola sai da boca dela e cresce, vindo na direção da tela
        var bola = Desenho("BolaNaTela", "kraken_tinta", Boca, OrdemDosBorroes);
        Vector3 de = Boca;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            float p = t / 0.4f;
            Vector3 meio = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            bola.transform.position = Vector3.Lerp(de, meio, p);
            bola.transform.localScale = Vector3.one * Mathf.Lerp(1f, 14f, p * p);
            yield return null;
        }
        Destroy(bola.gameObject);
        GerenciadorDoJogo.Som("bloco", 1f);
        CameraSeguir.Tremer(0.12f, 0.2f);

        float duracao = 4.5f + etapa; // dura uns segundos (mais nas etapas altas) e não espera: os ataques continuam
        Vector2[] lugares = { new Vector2(-8.5f, 3.5f), new Vector2(7.5f, 4.5f), new Vector2(-4f, -2.5f), new Vector2(9f, -2f),
                              new Vector2(1.5f, 5.5f), new Vector2(-11f, -4f), new Vector2(4f, -4.5f) };
        int quantos = 4 + etapa;
        for (int i = 0; i < quantos && i < lugares.Length; i++)
            StartCoroutine(Borrao(cam.transform, lugares[i], i, duracao));
    }

    // Um borrão grudado na tela: escorre devagar e some no fim.
    IEnumerator Borrao(Transform cam, Vector2 lugar, int qual, float duracao)
    {
        var borrao = ConstrutorDeFase.Visual("Borrao", efeitos, cam.position, "kraken_borrao_" + (qual % 3), OrdemDosBorroes, false)
            .GetComponent<SpriteRenderer>();
        float tamanho = 1.5f + 0.25f * (qual % 3);
        float giro = qual * 47f;
        for (float t = 0f; t < duracao && borrao != null; t += Time.deltaTime)
        {
            float entra = Mathf.Min(t / 0.12f, 1f);
            float some = Mathf.Clamp01((duracao - t) / 1f);
            Vector3 centro = new Vector3(cam.position.x, cam.position.y, 0f);
            borrao.transform.position = centro + (Vector3)(lugar + Vector2.down * (0.25f * t)); // escorrendo
            borrao.transform.localScale = new Vector3(tamanho * Mathf.Lerp(0.3f, 1f, entra), tamanho * Mathf.Lerp(0.3f, 1f, entra) * (1f + 0.04f * t), 1f);
            borrao.transform.rotation = Quaternion.Euler(0f, 0f, giro);
            borrao.color = new Color(1f, 1f, 1f, some);
            yield return null;
        }
        if (borrao != null) Destroy(borrao.gameObject);
    }

    // CANSADA: ela afunda, deita a cabeça no convés (tonta, com estrelinhas) e espera. É a hora de pisar!
    IEnumerator Cansar()
    {
        GerenciadorDoJogo.Instancia.Avisar(etapa == 0 ? "ELA CANSOU! Pula na CABEÇA dela! (cuidado com o mar)" : "CANSOU! AGORA!", 1.6f);
        GerenciadorDoJogo.Som("risada", 0.25f);
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            alturaDoCorpo = Mathf.Lerp(0f, -9f, t / 0.3f);
            yield return null;
        }
        corpo.enabled = false;
        cabeca.enabled = true;
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / 0.25f);
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
            float tremor = duracao - t < 0.5f ? Mathf.Sin(t * 70f) * 0.06f : 0f;
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
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            cabeca.transform.position = new Vector3(Mathf.Lerp(XDaCabecaDeitada, XDaCabecaDeitada + 6f, t / 0.25f), chao, 0f);
            yield return null;
        }
        cabeca.enabled = false;
        corpo.enabled = true;
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            alturaDoCorpo = Mathf.Lerp(-9f, 0f, Mathf.SmoothStep(0f, 1f, t / 0.35f));
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

    // Recolhe o tentáculo (sem machucar) até "para". comSaco: o saco de moedas vai junto (o agarrão que roubou).
    IEnumerator Recolher(Tentaculo t, Vector2 para, float velocidade, bool comSaco = false)
    {
        while (t.fim != para)
        {
            t.fim = Vector2.MoveTowards(t.fim, para, velocidade * Time.deltaTime);
            if (comSaco) saco.transform.position = t.fim + Vector2.up * 0.5f;
            yield return null;
        }
        if (!t.amarelo) Soltar(t);
    }

    IEnumerator RecolherESoltar(Tentaculo t, Vector2 para, float velocidade)
    {
        yield return Recolher(t, para, velocidade);
        Soltar(t);
    }

    // As casas marcadas com '1' num padrão ("1010...").
    static List<int> Casas(string padrao)
    {
        var casas = new List<int>();
        for (int i = 0; i < padrao.Length && i < CasasDaChuva; i++) if (padrao[i] == '1') casas.Add(i);
        return casas;
    }

    List<SpriteRenderer> Marcar(List<int> casas, float raio)
    {
        var marcas = new List<SpriteRenderer>();
        foreach (int casa in casas)
        {
            var marca = Desenho("MarcaDaChuva", "baleia_alvo", new Vector3(XDaCasa(casa), chao + 0.15f, 0f), OrdemDosAvisos);
            marca.transform.localScale = new Vector3(LarguraDaMarca(raio), 1f, 1f);
            marcas.Add(marca);
        }
        GerenciadorDoJogo.Som("armadilha", 0.35f);
        return marcas;
    }

    // A marca no chão ("baleia_alvo" tem 2 blocos de largura) do tamanho da área que mata de verdade: o raio do
    // perigo mais a meia largura do Grumete, menos a folga (é a mesma conta do TestarPerigos).
    static float LarguraDaMarca(float raio) => raio + MeiaLarguraDoJogador - FolgaDoAcerto; // (2 blocos x esta escala = 2 x o alcance)

    static IEnumerator PiscarMarcas(List<SpriteRenderer> marcas, float duracao)
    {
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            foreach (SpriteRenderer marca in marcas) if (marca != null) marca.color = Piscando(t, duracao) ? Vermelho : VermelhoFraco;
            yield return null;
        }
    }

    // ------------------------------------------------------------------ o pisão e o roubo

    // Pulou na cabeça dela, caindo, por cima? Só vale com ela CANSADA (deitada no convés). Devolve true se acertou.
    bool TestarPisao()
    {
        if (!Vulneravel || estado != Estado.Cansada) return false;
        Jogador jogador = QuemPisou(direita - 3.4f, direita - 0.2f, chao + AlturaDaCabeca);
        if (jogador == null) return false;
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
        if (devolveu > 0) jogo.Avisar(Idioma.T($"Ela soltou {devolveu} moeda(s)!", $"She dropped {devolveu} coin(s)!"), 1.6f);
        else if (vida > 0 && vida % GolpesDaEtapa != 0) jogo.Avisar(falas[etapa], 1.6f);
        Golpeado(); // para a rodada (Chefe.cs): depois vêm o Arrumar, o Reagir e o Combate de novo
        if (devolveu > 0) StartCoroutine(ChuvaDeMoedas(Mathf.Min(devolveu, 12))); // (depois do Golpeado: ele para as corrotinas)
    }

    // O agarrão encostou: rouba (até) as moedas da etapa e te joga para trás. Não mata.
    void Roubar(Jogador jogador, Zona zona)
    {
        zona.pegou = true;
        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        int levou = jogo.RoubarMoedas(MoedasPorAgarrao[etapa]);
        float lado = jogador.transform.position.x < (esquerda + direita) / 2f ? 1f : -1f; // para o meio da arena
        jogador.Corpo.linearVelocity = new Vector2(9f * lado, 9f); // o tranco
        CameraSeguir.Tremer(0.12f, 0.2f);
        if (levou > 0)
        {
            Conquistas.Desbloquear("kraken_roubou");
            jogo.Avisar(Idioma.T($"O KRAKEN ROUBOU {levou} MOEDA(S)!", $"THE KRAKEN STOLE {levou} COIN(S)!"), 1.8f);
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
        succao = 0f;
        ConsertarConves();
        foreach (Transform filho in efeitos) Destroy(filho.gameObject); // avisos, marcas, tinta, borrões, estrelas...
        estrelas.Clear();
        foreach (Tentaculo t in tentaculos) Soltar(t);
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

    // (coop) os perigos do chefe valem para cada Grumete da tripulação
    void TestarPerigos()
    {
        List<Jogador> todos = GerenciadorDoJogo.Jogadores;
        for (int i = 0; i < todos.Count; i++) TestarPerigos(todos[i]);
    }

    void TestarPerigos(Jogador jogador)
    {
        if (jogador == null || jogador.Morto || estado == Estado.Derrotada) return;
        Vector2 p = (Vector2)jogador.transform.position + new Vector2(0f, -0.02f); // centro da caixa dele

        // o Grumete vira uma "salsicha" em pé: 3 pontos no meio dele, com a meia largura de raio
        for (int i = 0; i < zonas.Count; i++)
        {
            Zona zona = zonas[i];
            if (zona.rouba && zona.pegou) continue;
            bool encostou = false;
            for (int k = -1; k <= 1 && !encostou; k++)
            {
                Vector2 ponto = p + new Vector2(0f, k * (MeiaAlturaDoJogador - MeiaLarguraDoJogador));
                if (DistanciaAoSegmento(ponto, zona.a, zona.b) < zona.raio + MeiaLarguraDoJogador - FolgaDoAcerto) encostou = true;
            }
            if (!encostou) continue;
            if (zona.rouba)
            {
                Roubar(jogador, zona);
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
