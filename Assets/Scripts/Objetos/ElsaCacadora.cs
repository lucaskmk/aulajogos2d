using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 'H' - ELSA GRANHIERTE, a CAÇADORA DE ENTRANHAS (CHEFE). A fase dela é escura e a luta é no breu:
// de dia ela é só uma moça de capa preta; no escuro, só se veem os OLHOS dela (roxos), andando pela sala.
//
// O 'H' marca o começo da arena (na linha em que o jogador anda); a arena tem larguraDaArena blocos
// a partir dele, com chão reto. Os dois DEGRAUS de tijolo dentro da arena são criados por este script
// (ColunasDosDegraus): assim a luta sabe exatamente onde eles estão. Entrou, paredes de sombra fecham a
// arena, a câmera trava e a luta começa.
//
// Igual à Baleia Branca (a luta foi feita no mesmo molde, estilo chefe final do HELLTAKER):
//  - tudo mata com UM toque, mas todo ataque AVISA antes: os olhos ficam VERMELHOS, aparece uma linha
//    vermelha piscando (com luz, para dar para ver no escuro) e ela pisca mais rápido no finalzinho;
//  - os padrões são FIXOS (nada é sorteado): dá para decorar;
//  - 3 ETAPAS de 2 golpes. Morreu? Volta na hora, no começo da arena, JÁ na etapa em que estava
//    (o GerenciadorDoJogo guarda a etapa em EtapaDoChefe). A introdução só aparece na 1ª vez.
//
// OS ATAQUES (todos com faca, a kukri dela, que tem LUZ PRÓPRIA):
//   FACA MIRADA  - a faca aparece na mão, mira em você (linha lilás) e TRAVA (linha vermelha): saia da linha!
//   LEQUE        - igual, com 3 ou 5 facas: fique no VÃO entre as linhas.
//   CHUVA        - marcas vermelhas no chão; as facas caem do teto em cima delas: fique ENTRE as marcas.
//   CORTE RENTE  - linha vermelha no pé: num piscar de olhos ela atravessa a arena cortando o chão. PULE.
//   CORTE ALTO   - linha vermelha na altura do pulo: NÃO pule.
//   BUMERANGUE   - ela joga uma faca reto, a faca crava na parede do outro lado... e VOLTA pelo mesmo caminho.
//   ATRÁS DE VOCÊ - os olhos piscam e somem (e ela ri)... e aparecem vermelhos ATRÁS do Subaru: PULE.
//  Etapa 1: um ataque de cada vez. Etapa 2: mais rápida e juntando dois. Etapa 3: mais rápida ainda e
//  a escuridão FECHA (só sobra um círculo pequeno em volta do Subaru).
//
// COMO VENCER: no fim de cada rodada aparece um CRISTAL DE LUZ (uma lâmpada mágica do Roswaal) num lugar
// fixo da arena (às vezes lá no alto, em cima de um degrau). Encostou nele: CLARÃO! A arena acende por
// uns segundos e a Elsa, ofuscada, fica TONTA no chão, à vista. PULE NA CABEÇA DELA. Não deu tempo?
// Ela levanta rindo, a luz apaga e a rodada recomeça.
//
// Como a Baleia, os perigos não usam colisores: cada faca (ou corte) é uma "Zona" (um segmento com
// grossura, tipo uma salsicha) e a própria Elsa testa, a cada quadro, se o Subaru encostou em alguma.
public class ElsaCacadora : Chefe
{
    const int EtapasDaLuta = 3;
    const int GolpesDaEtapa = 2;

    public float larguraDaArena = 24f;

    // Colunas (contadas a partir do 'H') onde começam os dois degraus de 3 tijolos (1 bloco acima do chão).
    public static readonly int[] ColunasDosDegraus = { 3, 18 };
    const int LarguraDoDegrau = 3;

    // ------------------------------------------------------------------ ajustes de cada etapa (1, 2, 3)

    static readonly float[] TempoDeAviso = { 0.6f, 0.48f, 0.38f };
    static readonly float[] TempoMirando = { 0.45f, 0.4f, 0.35f };     // a faca segue você antes de travar
    static readonly float[] VelocidadeDaFaca = { 13f, 15f, 17f };
    static readonly float[] VelocidadeDoBumerangue = { 11f, 12.5f, 14f };
    static readonly float[] TempoCravada = { 0.6f, 0.55f, 0.5f };       // o bumerangue fica na parede antes de voltar
    static readonly float[] TempoAtordoada = { 3.2f, 2.8f, 2.5f };
    static readonly float[] IntervaloNoCristal = { 1.7f, 1.5f, 1.3f }; // facas enquanto você busca o cristal
    const float AvisoMinimoRente = 0.55f; // tudo que pede PULO avisa pelo menos isso (precisa de tempo para acertar o pulo)
    const float DuracaoDoCorte = 0.2f;    // quanto tempo o rastro do corte mata
    const float RaioDoCristal = 0.8f;     // encostou (o meio do Subaru a menos disso do cristal) = clarão

    // Alturas acima do chão (e "grossura" de cada coisa que mata).
    const float AlturaDoCorteRente = 0.35f, RaioDoCorteRente = 0.33f;   // mata até 0,68 do chão: pule
    const float AlturaDoCorteAlto = 1.65f, RaioDoCorteAlto = 0.3f;      // de 1,35 a 1,95: não pule
    const float AlturaDaFacaRente = 0.4f, AlturaDaFacaAlta = 1.65f;     // bumerangue
    const float RaioDaFaca = 0.12f;
    const float AlcanceDoAtras = 6f;      // o corte "atrás de você" tem 6 blocos
    const float DistanciaDoAtras = 1.8f;  // ela aparece a 1,8 bloco do Subaru

    // Lugares FIXOS (x a partir da borda esquerda da arena, y a partir do chão).
    // "Poleiros": de onde ela joga as facas (só os olhos aparecem lá).
    static readonly Vector2 PoleiroEsquerdo = new Vector2(1.5f, 6.5f);
    static readonly Vector2 PoleiroDireito = new Vector2(22.5f, 6.5f);
    static readonly Vector2 PoleiroDoTopo = new Vector2(12f, 8.5f);
    static readonly Vector2 PoleiroBaixoEsquerdo = new Vector2(6.5f, 4.5f);
    static readonly Vector2 PoleiroBaixoDireito = new Vector2(17.5f, 4.5f);

    // Chuva: alterna A e B. Onde é seguro numa é exatamente onde cai a outra (dê UM passo para o lado).
    static readonly float[] ChuvaA = { 1f, 3f, 5f, 7f, 9f, 11f, 13f, 15f, 17f, 19f, 21f, 23f };
    static readonly float[] ChuvaB = { 2f, 4f, 6f, 8f, 10f, 12f, 14f, 16f, 18f, 20f, 22f };

    // O cristal de cada rodada (x, altura do meio dele) e onde a Elsa cai tonta (x). Rodada = etapa * 2 + golpe.
    // Altura 1 = no chão; 2,6 a 3,4 = precisa pular; 3 = em cima de um degrau; 4,4 = subir no degrau E pular.
    static readonly Vector2[] Cristais =
    {
        new Vector2(12f, 1f), new Vector2(4.5f, 3f), new Vector2(19.5f, 4.4f),
        new Vector2(22.5f, 2.6f), new Vector2(4.5f, 4.4f), new Vector2(12f, 3.3f),
    };
    static readonly float[] OndeElaCai = { 22.5f, 15.5f, 11.5f, 14.5f, 12.5f, 7.5f };

    // Medidas do desenho (pixels / 16 = blocos).
    const float AlturaDosOlhosNoCorpo = 19.5f / 16f; // dos pés até os olhos, no desenho "elsa_chefe"
    const float AlturaDaCabecaTonta = 18f / 16f;     // dos pés ao topo da cabeça, no desenho "elsa_tonta"
    const float MeiaLarguraTonta = 0.5f;

    // Ordem de desenho: os avisos, as facas, o cristal e os olhos ficam POR CIMA da escuridão da etapa 3.
    const int OrdemDoCorpo = 8, OrdemDaEscuridao = 50, OrdemDasBeiradas = 54, OrdemDosAvisos = 55,
              OrdemDosAtaques = 56, OrdemDoCristal = 57, OrdemDosOlhos = 58;

    // A luz da fase escura (a mesma do GerenciadorDoJogo.CarregarFase) e a do clarão.
    // (o clarão fica abaixo de 0,8: acima disso o Luzes acha que é dia e apaga as luzinhas)
    const float LuzDoEscuro = 0.22f, LuzDoClarao = 0.7f;
    static readonly Color CorDoEscuro = new Color(0.7f, 0.72f, 1f);

    // A "caixa" do Subaru que conta para morrer (um tiquinho menor que o desenho: é mais justo).
    const float MeiaLarguraDoJogador = 0.27f, MeiaAlturaDoJogador = 0.42f;

    static readonly Color Vermelho = new Color(1f, 0.2f, 0.3f, 1f);
    static readonly Color VermelhoFraco = new Color(1f, 0.2f, 0.3f, 0.35f);
    static readonly Color Lilas = new Color(0.8f, 0.55f, 1f, 0.55f);
    static readonly Color CorDoCristal = new Color(1f, 0.92f, 0.6f);
    static readonly Color CorDoRastro = new Color(0.9f, 0.8f, 1f, 1f);

    // ------------------------------------------------------------------ estado

    // O que a barra de vida (Interface) lê (veja Chefe.cs).
    public override string Nome => "ELSA";
    public override int Vida => vida;
    public override float VidaMostrada => vidaMostrada;
    public override bool EmCombate => emCombate;
    public override int VidaMaxima => EtapasDaLuta * GolpesDaEtapa;
    public override int EtapaAtual => etapa;
    public override int Etapas => EtapasDaLuta;
    public override int GolpesPorEtapa => GolpesDaEtapa;

    enum Estado { Esperando, Lutando, Atordoada, Derrotada }
    Estado estado;
    int vida, etapa;
    float vidaMostrada;
    bool emCombate;
    bool parado;          // o jogador morreu: tudo congela até a fase recomeçar
    bool golpeada;        // levou um pisão nesta tontura
    bool arremessando;    // um arremesso está mirando (no cristal, ela espera um acabar para começar outro)
    float chao, esquerda, direita, meio;
    float relogio;

    // Os olhos (o que se vê dela no escuro): aparecem/somem aos poucos e ficam vermelhos no aviso.
    Transform olhos;
    SpriteRenderer desenhoDosOlhos;
    Light2D luzDosOlhos;
    float intensidadeDosOlhos;
    Sprite olhosNormais, olhosBravos;
    float aberturaDosOlhos;       // 0 = sumidos, 1 = abertos
    bool olhosAbertos, olhosVermelhos, olhosPiscando;

    // O corpo inteiro (só aparece no clarão, na tontura e no corte).
    SpriteRenderer corpo;
    Sprite spriteDePe, spriteTonta;

    SpriteRenderer escuridao;     // etapa 3: a escuridão que só deixa um círculo em volta do Subaru
    Transform efeitos;            // pai de tudo que é passageiro (avisos, facas, marcas...): some de uma vez
    Transform cristal;
    Light2D luzDoClarao;

    // Uma área que mata: tudo a menos de "raio" do segmento a-b.
    class Zona { public Vector2 a, b; public float raio; }
    readonly List<Zona> zonas = new List<Zona>();

    readonly List<GameObject> paredes = new List<GameObject>();
    readonly List<Transform> estrelas = new List<Transform>();
    readonly List<Rect> degraus = new List<Rect>();
    readonly List<Coroutine> soltos = new List<Coroutine>();
    readonly HashSet<string> dicasMostradas = new HashSet<string>();
    int emParalelo;

    float Aviso => TempoDeAviso[etapa];
    float AvisoDePulo => Mathf.Max(Aviso, AvisoMinimoRente);
    Vector3 PontoDeRenascer => new Vector3(esquerda + 0.5f, chao + 0.5f, 0f); // a casa do 'H'
    Vector2 Ponto(Vector2 relativo) => new Vector2(esquerda + relativo.x, chao + relativo.y);

    // ------------------------------------------------------------------ Unity

    void Awake()
    {
        chao = transform.position.y - 0.5f;
        esquerda = transform.position.x - 0.5f;   // borda esquerda da casa do 'H'
        direita = esquerda + larguraDaArena;
        meio = (esquerda + direita) / 2f;
        vida = VidaMaxima;
        vidaMostrada = vida;

        efeitos = new GameObject("Efeitos").transform;
        efeitos.SetParent(transform, false);

        olhosNormais = FabricaDeSprites.Pegar("olhos_elsa");
        olhosBravos = FabricaDeSprites.Pegar("olhos_elsa_bravos");
        desenhoDosOlhos = Desenho("Olhos", "olhos_elsa", Ponto(PoleiroDoTopo), OrdemDosOlhos);
        olhos = desenhoDosOlhos.transform;
        olhos.SetParent(transform, true);
        olhos.localScale = Vector3.one * 1.4f;
        luzDosOlhos = Luzes.Ponto(olhos, FacasDaElsa.CorDosOlhos, 1.6f, 1f);
        if (luzDosOlhos != null) intensidadeDosOlhos = luzDosOlhos.intensity;

        spriteDePe = FabricaDeSprites.Pegar("elsa_chefe");
        spriteTonta = FabricaDeSprites.Pegar("elsa_tonta");
        corpo = Desenho("Elsa", "elsa_chefe", transform.position, OrdemDoCorpo);
        corpo.transform.SetParent(transform, true);
        corpo.enabled = false;

        MontarDegraus();
        AnimarOlhos();
        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.Chefe = this;
    }

    void Start()
    {
        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        if (jogo == null) return;
        // já venceu nesta fase (morreu depois)? Então ela não volta.
        if (jogo.ChefeDerrotado)
        {
            estado = Estado.Derrotada;
            return;
        }
        // renasceu dentro da arena (morreu na luta)? A luta volta na hora, sem a introdução.
        if (jogo.EhPontoDeSave(PontoDeRenascer)) StartCoroutine(Lutar(renasceu: true));
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

        if (estado == Estado.Esperando)
        {
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

        AnimarOlhos();
        if (estado == Estado.Atordoada)
        {
            GirarEstrelas();
            TestarPisao();
        }
        TestarPerigos();
        if (escuridao != null && GerenciadorDoJogo.JogadorVivo(out Vector2 jogador))
            escuridao.transform.position = jogador;
    }

    // ------------------------------------------------------------------ a luta

    IEnumerator Lutar(bool renasceu)
    {
        estado = Estado.Lutando;
        emCombate = true;
        FecharArena();
        CameraSeguir.Travar(esquerda - 1f, direita + 1f);
        GerenciadorDoJogo.Instancia.TocarMusicaDoChefe();
        GerenciadorDoJogo.Instancia.SalvarPonto(PontoDeRenascer); // morreu? renasce aqui, no começo da arena

        etapa = Mathf.Clamp(GerenciadorDoJogo.Instancia.EtapaDoChefe, 0, Etapas - 1);
        vida = VidaMaxima - etapa * GolpesPorEtapa;
        vidaMostrada = vida;
        if (etapa == Etapas - 1) CriarEscuridao(); // renasceu na etapa 3: já começa no breu

        if (renasceu) yield return Pronto();
        else yield return Introducao();

        while (true)
        {
            // repete as rodadas da etapa até levar os 2 golpes dela
            int vidaNoFimDaEtapa = VidaMaxima - (etapa + 1) * GolpesPorEtapa;
            while (vida > vidaNoFimDaEtapa)
            {
                int rodada = etapa * GolpesPorEtapa + (VidaMaxima - etapa * GolpesPorEtapa - vida);
                yield return Rodada(rodada);
                yield return BuscarOCristal(rodada);
            }
            if (vida <= 0) break;

            etapa++;
            GerenciadorDoJogo.Instancia.EtapaDoChefe = etapa; // o "checkpoint" do chefe
            yield return NovaEtapa();
        }

        yield return SerDerrotada();
    }

    // Cada rodada é uma sequência FIXA de ataques. Depois dela vem o cristal (a chance de dar dano).
    IEnumerator Rodada(int qual)
    {
        soltos.Clear();
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

    // ETAPA 1: um ataque de cada vez (o Puck explica cada um).
    IEnumerator Etapa1Rodada1()
    {
        yield return Arremesso(PoleiroEsquerdo, 1);
        yield return Arremesso(PoleiroDireito, 1);
        yield return Arremesso(PoleiroDoTopo, 1);
        yield return EsperarTodos();
        yield return Corte(alto: false, daEsquerda: true);
        yield return new WaitForSeconds(0.4f);
        yield return Corte(alto: true, daEsquerda: false);
        yield return new WaitForSeconds(0.4f);
        yield return Chuva(0, 1);
        yield return EsperarTodos();
    }

    IEnumerator Etapa1Rodada2()
    {
        yield return Arremesso(PoleiroEsquerdo, 3);
        yield return Arremesso(PoleiroDireito, 3);
        yield return EsperarTodos();
        yield return Bumerangue(alta: false, daEsquerda: true);
        yield return new WaitForSeconds(0.3f);
        yield return Atras();
        yield return new WaitForSeconds(0.4f);
        yield return Atras();
        yield return EsperarTodos();
    }

    // ETAPA 2: mais rápida e juntando dois ataques.
    IEnumerator Etapa2Rodada1()
    {
        Paralelo(Chuva(0, 1, 0));                 // um passo a cada onda de facas...
        yield return new WaitForSeconds(0.4f);
        yield return Arremesso(PoleiroEsquerdo, 1); // ...e um pulinho (ou passinho) para sair da linha
        yield return Arremesso(PoleiroDireito, 1);
        yield return EsperarTodos();
        yield return Arremesso(PoleiroDoTopo, 5);
        yield return EsperarTodos();
        yield return Corte(alto: true, daEsquerda: true);
        yield return new WaitForSeconds(0.25f);
        yield return Corte(alto: false, daEsquerda: false);
        yield return new WaitForSeconds(0.3f);
        yield return Bumerangue(alta: false, daEsquerda: false);
        yield return EsperarTodos();
    }

    IEnumerator Etapa2Rodada2()
    {
        yield return Atras();
        yield return new WaitForSeconds(0.25f);
        yield return Atras();
        yield return EsperarTodos();
        yield return Arremesso(PoleiroBaixoEsquerdo, 5);
        yield return Arremesso(PoleiroBaixoDireito, 5);
        yield return EsperarTodos();
        Paralelo(Chuva(1, 0));                    // fique no vão da chuva...
        yield return new WaitForSeconds(0.3f);
        yield return Corte(alto: true, daEsquerda: true); // ...e NÃO pule
        yield return EsperarTodos();
        yield return Bumerangue(alta: true, daEsquerda: true);
        yield return Bumerangue(alta: false, daEsquerda: false);
        yield return EsperarTodos();
    }

    // ETAPA 3: no breu, mais rápida, mais ataques seguidos.
    IEnumerator Etapa3Rodada1()
    {
        yield return Arremesso(PoleiroBaixoEsquerdo, 1);
        yield return Arremesso(PoleiroBaixoDireito, 1);
        yield return Arremesso(PoleiroDoTopo, 1);
        yield return EsperarTodos();
        Paralelo(Chuva(0, 1, 0, 1));
        yield return new WaitForSeconds(0.3f);
        yield return Corte(alto: false, daEsquerda: true);
        yield return new WaitForSeconds(0.5f);
        yield return Corte(alto: true, daEsquerda: false);
        yield return EsperarTodos();
        yield return Arremesso(PoleiroDoTopo, 5);
        yield return EsperarTodos();
        yield return Atras();
        yield return new WaitForSeconds(0.2f);
        yield return Atras();
        yield return new WaitForSeconds(0.2f);
        yield return Atras();
        yield return EsperarTodos();
    }

    IEnumerator Etapa3Rodada2()
    {
        yield return Arremesso(PoleiroEsquerdo, 5);
        yield return Arremesso(PoleiroDireito, 5);
        yield return EsperarTodos();
        yield return Bumerangue(alta: false, daEsquerda: true);
        yield return Atras();
        yield return EsperarTodos();
        Paralelo(Chuva(1, 0, 1));
        yield return new WaitForSeconds(0.4f);
        yield return Arremesso(PoleiroEsquerdo, 1);
        yield return Arremesso(PoleiroDireito, 1);
        yield return EsperarTodos();
        yield return Corte(alto: false, daEsquerda: false);
        yield return new WaitForSeconds(0.3f);
        yield return Corte(alto: true, daEsquerda: true);
        yield return new WaitForSeconds(0.3f);
        yield return Corte(alto: false, daEsquerda: true);
        yield return EsperarTodos();
    }

    // ------------------------------------------------------------------ começo, etapas e fim

    // Só na primeira vez: os olhos acendem no alto, um relâmpago mostra quem é... e apaga de novo.
    IEnumerator Introducao()
    {
        GerenciadorDoJogo.Som("risada");
        GerenciadorDoJogo.Instancia.Avisar("ELSA, A CAÇADORA DE ENTRANHAS!", 2.4f);
        olhos.position = Ponto(PoleiroDoTopo);
        olhosAbertos = true;
        yield return new WaitForSeconds(1.2f);

        // o relâmpago: por um instante dá para ver a Elsa inteira (sorrindo)
        Luzes.Ambiente(LuzDoClarao, Color.white);
        MostrarCorpo(spriteDePe, (Vector2)olhos.position - new Vector2(0f, AlturaDosOlhosNoCorpo));
        GerenciadorDoJogo.Som("pancada");
        CameraSeguir.Tremer(0.12f, 0.3f);
        yield return new WaitForSeconds(0.18f);
        Luzes.Ambiente(LuzDoEscuro, CorDoEscuro);
        corpo.enabled = false;
        yield return new WaitForSeconds(0.7f);

        GerenciadorDoJogo.Instancia.Avisar("Fuja das facas. Ache o CRISTAL DE LUZ e pule na CABEÇA dela!", 2.8f);
        yield return new WaitForSeconds(1.5f);
    }

    // Depois de morrer: "PRONTO?" e a luta volta na etapa em que estava.
    IEnumerator Pronto()
    {
        GerenciadorDoJogo.Instancia.Avisar($"ETAPA {etapa + 1}/{Etapas} - PRONTO?", 1.3f);
        olhos.position = Ponto(PoleiroDoTopo);
        olhosAbertos = true;
        GerenciadorDoJogo.Som("risada", 0.5f);
        yield return new WaitForSeconds(1.6f); // um respiro: o 1º ataque não pode pegar quem acabou de nascer
    }

    IEnumerator NovaEtapa()
    {
        GerenciadorDoJogo.Som("risada");
        CameraSeguir.Tremer(0.15f, 0.6f);
        if (etapa == 1)
        {
            GerenciadorDoJogo.Instancia.Avisar("ETAPA 2/3 - ela tá se DIVERTINDO!", 2f);
            yield return new WaitForSeconds(1.6f);
            yield break;
        }
        GerenciadorDoJogo.Instancia.Avisar("ETAPA 3/3 - A ESCURIDÃO FECHOU!", 2f);
        CriarEscuridao();
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            escuridao.color = new Color(1f, 1f, 1f, t);
            yield return null;
        }
        escuridao.color = Color.white;
        yield return new WaitForSeconds(0.8f);
    }

    // Etapa 3: um desenho preto do tamanho da tela com um buraco em volta do Subaru (o Update leva junto).
    void CriarEscuridao()
    {
        if (escuridao != null) return;
        Vector3 lugar = GerenciadorDoJogo.JogadorVivo(out Vector2 jogador) ? (Vector3)jogador : transform.position;
        escuridao = Desenho("Escuridao", "elsa_escuridao", lugar, OrdemDaEscuridao);
        escuridao.transform.SetParent(transform, true);
        escuridao.transform.localScale = Vector3.one * 2.4f; // o buraco claro fica com uns 2 blocos de raio
    }

    IEnumerator SerDerrotada()
    {
        estado = Estado.Derrotada;
        zonas.Clear();
        Conquistas.Desbloquear("elsa_vencida");
        // a partir daqui, se morrer (caiu num buraco...), renasce DEPOIS da arena
        GerenciadorDoJogo.Instancia.ChefeVencido(new Vector3(direita + 1.5f, chao + 0.5f, 0f));
        GerenciadorDoJogo.Instancia.Avisar("A ELSA FOI DERROTADA! (ela fugiu rindo...)", 3f);

        // ela levanta, ri e some correndo para a direita, ainda no clarão
        GerenciadorDoJogo.Som("risada");
        corpo.sprite = spriteDePe;
        Vector3 de = corpo.transform.position;
        for (float t = 0f; t < 1.4f; t += Time.deltaTime)
        {
            float p = t / 1.4f;
            corpo.transform.position = de + Vector3.right * p * p * 14f;
            corpo.flipX = false;
            corpo.color = new Color(1f, 1f, 1f, 1f - p * p);
            if (escuridao != null) escuridao.color = new Color(1f, 1f, 1f, 0f);
            yield return null;
        }
        corpo.enabled = false;
        olhosAbertos = false;
        if (escuridao != null) Destroy(escuridao.gameObject);
        Luzes.Ambiente(LuzDoEscuro, CorDoEscuro); // a fase continua escura (é a fase dela, afinal)
        if (luzDoClarao != null) Destroy(luzDoClarao.gameObject);

        AbrirArena();
        CameraSeguir.Destravar();
        emCombate = false;
    }

    // ------------------------------------------------------------------ o cristal e o clarão

    // O cristal aparece num lugar fixo; enquanto você não encosta, ela continua jogando facas.
    IEnumerator BuscarOCristal(int rodada)
    {
        Vector2 lugar = Ponto(Cristais[rodada]);
        var desenhoDoCristal = Desenho("CristalDeLuz", "cristal_de_luz", lugar, OrdemDoCristal);
        cristal = desenhoDoCristal.transform;
        cristal.SetParent(transform, true);
        Animacao.Adicionar(cristal.gameObject, Animacao.Tipo.Flutuar, 2.5f, 0.12f);
        Luzes.Ponto(cristal, CorDoCristal, 3f, 1.2f);
        GerenciadorDoJogo.Som("moeda");
        Dica(Cristais[rodada].y > 4f ? "Cristal lá no alto! Suba no DEGRAU e pule!" : "Um CRISTAL DE LUZ! Encosta nele!");

        float proxima = 0.8f;
        while (true)
        {
            bool vivo = GerenciadorDoJogo.JogadorVivo(out Vector2 jogador); // (na pausa ele "não está vivo")
            if (vivo && Vector2.Distance(jogador, cristal.position) < RaioDoCristal)
                break;
            proxima -= Time.deltaTime;
            if (vivo && proxima <= 0f && !arremessando)
            {
                // ela joga do lado de LONGE (para dar tempo de ver a faca vindo)
                bool jogadorNaEsquerda = jogador.x < meio;
                Paralelo(Arremesso(jogadorNaEsquerda ? PoleiroDireito : PoleiroEsquerdo, etapa == 1 ? 3 : 1));
                proxima = IntervaloNoCristal[etapa];
            }
            yield return null;
        }

        // CLARÃO! As facas no ar somem na luz, ela fica ofuscada.
        Vector3 ondeEstava = cristal.position;
        Destroy(cristal.gameObject);
        cristal = null;
        PararSoltos();
        LimparEfeitos();
        yield return Clarao(ondeEstava, OndeElaCai[rodada]);
    }

    IEnumerator Clarao(Vector3 ondeAcendeu, float xDaTontura)
    {
        GerenciadorDoJogo.Som("vitoria", 0.6f);
        GerenciadorDoJogo.Som("pancada", 0.6f);
        CameraSeguir.Tremer(0.15f, 0.3f);
        Luzes.Ambiente(LuzDoClarao, Color.white);
        var objetoDaLuz = new GameObject("LuzDoClarao");
        objetoDaLuz.transform.SetParent(transform, false);
        objetoDaLuz.transform.position = ondeAcendeu;
        luzDoClarao = Luzes.Ponto(objetoDaLuz.transform, Color.white, 30f, 1.4f);
        for (int i = 0; i < 12; i++)
            Particula.Criar(efeitos, ondeAcendeu, Random.insideUnitCircle * 6f, 0.5f, 0.5f, CorDoCristal);
        if (escuridao != null) escuridao.color = new Color(1f, 1f, 1f, 0f);

        // ela aparece no chão, tonta, com as mãos nos olhos
        olhosAbertos = false;
        aberturaDosOlhos = 0f;
        MostrarCorpo(spriteTonta, new Vector2(esquerda + xDaTontura, chao));
        CriarEstrelas();
        estado = Estado.Atordoada;
        golpeada = false;
        if (etapa == 0) GerenciadorDoJogo.Instancia.Avisar("OFUSCADA! Pule na CABEÇA dela!", 1.6f);
        else GerenciadorDoJogo.Instancia.Avisar("NA CABEÇA!", 1f);

        Vector3 caida = corpo.transform.position;
        float tempo = TempoAtordoada[etapa];
        for (float t = 0f; t < tempo && !golpeada; t += Time.deltaTime)
        {
            // meio segundo antes de acordar ela treme e a luz começa a falhar (aviso)
            bool acordando = t > tempo - 0.5f;
            corpo.transform.position = acordando ? caida + (Vector3)(Random.insideUnitCircle * 0.05f) : caida;
            if (luzDoClarao != null) luzDoClarao.enabled = !acordando || Mathf.Repeat(t * 12f, 1f) < 0.6f;
            yield return null;
        }
        corpo.transform.position = caida;
        ApagarEstrelas();
        if (estado == Estado.Atordoada) estado = Estado.Lutando;
        if (vida <= 0) yield break; // levou o último golpe: a cena da derrota cuida do resto

        if (!golpeada) GerenciadorDoJogo.Som("risada", 0.7f); // não deu tempo: ela levanta rindo
        corpo.sprite = spriteDePe;
        for (float t = 0f; t < 0.35f; t += Time.deltaTime) // some na sombra
        {
            corpo.color = new Color(1f, 1f, 1f, 1f - t / 0.35f);
            yield return null;
        }
        corpo.enabled = false;
        corpo.color = Color.white;

        // a luz volta a apagar (em dois passos) e os olhos voltam lá no alto
        Luzes.Ambiente(0.45f, CorDoEscuro);
        yield return new WaitForSeconds(0.15f);
        Luzes.Ambiente(LuzDoEscuro, CorDoEscuro);
        if (luzDoClarao != null) Destroy(luzDoClarao.gameObject);
        if (escuridao != null) escuridao.color = Color.white;
        olhos.position = Ponto(PoleiroDoTopo);
        olhosAbertos = true;
        yield return new WaitForSeconds(0.9f); // respiro antes da próxima rodada
    }

    // Caiu em cima da cabeça dela (caindo, por cima)?
    void TestarPisao()
    {
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (golpeada || jogador == null || jogador.Morto || jogador.Corpo.linearVelocity.y > 0.5f) return;
        Vector3 pes = jogador.transform.position + Vector3.down * 0.5f;
        Vector3 base_ = corpo.transform.position;
        float cabeca = base_.y + AlturaDaCabecaTonta;
        if (Mathf.Abs(pes.x - base_.x) < MeiaLarguraTonta + 0.2f && pes.y > cabeca - 0.5f && pes.y < cabeca + 0.8f)
            LevarGolpe(jogador);
    }

    void LevarGolpe(Jogador jogador)
    {
        vida--;
        golpeada = true;
        jogador.Quicar(14f, true);
        GerenciadorDoJogo.Som("pisao");
        GerenciadorDoJogo.Som("risada", 0.4f); // ela ri até apanhando
        CameraSeguir.Tremer(0.2f, 0.3f);
        Efeitos.Poeira(transform, corpo.transform.position + Vector3.up * 1f, 8, 3f);
        string[] falas = { "ACERTOU! Ela gostou... (?)", "Mais um! Ela tá rindo menos!", "SÓ MAIS UM! VAI, SUBARU!" };
        if (vida > 0 && vida % GolpesPorEtapa != 0) GerenciadorDoJogo.Instancia.Avisar(falas[etapa], 1.6f);
    }

    // ------------------------------------------------------------------ os ataques

    // FACA MIRADA (quantas = 1) e LEQUE (3 ou 5): as facas aparecem na mão e seguem você (linha lilás),
    // depois TRAVAM (olhos e linhas vermelhos) e voam reto. Saia da linha enquanto ela está vermelha!
    IEnumerator Arremesso(Vector2 poleiro, int quantas)
    {
        arremessando = true;
        Dica(quantas == 1 ? "Olhos VERMELHOS = faca! Saia da LINHA!" : "Leque! Fique no VÃO entre as linhas!");
        yield return MoverOlhos(Ponto(poleiro), 0.35f);

        float abertura = quantas >= 5 ? 14f : 16f; // graus entre uma faca e a próxima
        var facas = new List<SpriteRenderer>();
        var linhas = new List<LinhaDeAviso>();
        for (int i = 0; i < quantas; i++)
        {
            facas.Add(FacasDaElsa.DesenharFaca(efeitos, olhos.position, OrdemDosAtaques, out _));
            linhas.Add(NovoAviso());
        }
        GerenciadorDoJogo.JogadorVivo(out Vector2 alvo);
        Vector2 origem = olhos.position;
        GerenciadorDoJogo.Som("pop", 0.6f);

        float mira = TempoMirando[etapa], trava = Aviso;
        var direcoes = new Vector2[quantas];
        for (float t = 0f; t < mira + trava; t += Time.deltaTime)
        {
            bool travou = t >= mira;
            if (!travou && GerenciadorDoJogo.JogadorVivo(out Vector2 agora)) alvo = agora;
            if (travou && !olhosVermelhos) GerenciadorDoJogo.Som("armadilha", 0.4f); // travou!
            olhosVermelhos = travou;
            for (int i = 0; i < quantas; i++)
            {
                direcoes[i] = Girar(DirecaoPara(origem, alvo), (i - (quantas - 1) / 2f) * abertura);
                Vector2 lugar = origem + direcoes[i] * 0.6f;
                facas[i].transform.position = lugar;
                FacasDaElsa.ApontarDesenho(facas[i], direcoes[i]);
                Vector2 ponta = lugar + direcoes[i] * 0.6f;
                linhas[i].Mostrar(ponta, ponta + direcoes[i] * Alcance(ponta, direcoes[i]),
                    !travou ? Lilas : Piscando(t - mira, trava) ? Vermelho : VermelhoFraco, travou ? 1f : 0.6f);
            }
            yield return null;
        }
        foreach (LinhaDeAviso linha in linhas) linha.Apagar();
        olhosVermelhos = false;

        GerenciadorDoJogo.Som("serra", 0.45f); // "ziiing"
        for (int i = 0; i < quantas; i++)
            Paralelo(Voar(facas[i], facas[i].transform.position, direcoes[i], VelocidadeDaFaca[etapa]));
        arremessando = false;
        yield return new WaitForSeconds(0.15f);
    }

    // Uma faca voando reto até cravar em algo (chão, parede da arena ou degrau).
    IEnumerator Voar(SpriteRenderer faca, Vector2 lugar, Vector2 direcao, float velocidade)
    {
        Zona zona = NovaZona(RaioDaFaca);
        float percurso = Alcance(lugar + direcao * 0.6f, direcao); // até a PONTA encostar
        float rastro = 0f;
        for (float andou = 0f; andou < percurso;)
        {
            andou = Mathf.Min(percurso, andou + velocidade * Time.deltaTime);
            Vector2 agora = lugar + direcao * andou;
            faca.transform.position = agora;
            PosicionarZona(zona, agora, direcao);
            rastro -= Time.deltaTime;
            if (rastro <= 0f)
            {
                rastro = 0.03f;
                Particula.Criar(efeitos, agora, Vector2.zero, 0.18f, 0.55f, FacasDaElsa.CorDoRastro);
            }
            yield return null;
        }
        zonas.Remove(zona);
        yield return Cravar(faca, direcao);
    }

    // TUNK! Faíscas, e a faca vai apagando até sumir (cravada não mata mais).
    IEnumerator Cravar(SpriteRenderer faca, Vector2 direcao)
    {
        Vector2 ponta = (Vector2)faca.transform.position + direcao * 0.6f;
        for (int i = 0; i < 5; i++)
            Particula.Criar(efeitos, ponta, -direcao * Random.Range(1.5f, 4f) + Random.insideUnitCircle * 2.5f,
                Random.Range(0.15f, 0.3f), Random.Range(0.35f, 0.6f), FacasDaElsa.CorDaFaisca);
        GerenciadorDoJogo.Som("bloco", 0.35f);
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            faca.color = new Color(1f, 1f, 1f, 1f - t / 0.3f);
            yield return null;
        }
        Destroy(faca.gameObject);
    }

    // CHUVA DE FACAS: cada "batida" marca o chão (padrão A ou B) e as facas despencam do teto nas marcas.
    IEnumerator Chuva(params int[] padroes)
    {
        Dica("Chuva de facas! Fique ENTRE as marcas!");
        foreach (int padrao in padroes)
        {
            foreach (float x in padrao == 0 ? ChuvaA : ChuvaB) Paralelo(FacaDoTeto(esquerda + x, Aviso + 0.25f));
            GerenciadorDoJogo.Som("pop", 0.6f);
            yield return new WaitForSeconds(Aviso + 0.3f);
        }
    }

    IEnumerator FacaDoTeto(float x, float tempo)
    {
        float pouso = AlturaDoPiso(x); // o chão, ou o topo de um degrau
        var marca = Desenho("Marca", "baleia_alvo", new Vector3(x, pouso + 0.12f, 0f), OrdemDosAvisos);
        marca.transform.localScale = new Vector3(0.5f, 1f, 1f);
        var luzDaMarca = new GameObject("LuzDaMarca"); // separada da marca (a marca está esticada)
        luzDaMarca.transform.SetParent(efeitos, false);
        luzDaMarca.transform.position = marca.transform.position;
        Luzes.Ponto(luzDaMarca.transform, FacasDaElsa.CorDoAviso, 1f, 0.8f);
        SpriteRenderer faca = FacasDaElsa.DesenharFaca(efeitos, new Vector3(x, chao + 13f, 0f), OrdemDosAtaques, out _);
        FacasDaElsa.ApontarDesenho(faca, Vector2.down);
        Zona zona = NovaZona(RaioDaFaca);
        float de = chao + 13f, ate = pouso + 0.6f; // no fim, a ponta encosta no chão
        for (float t = 0f; t < tempo; t += Time.deltaTime)
        {
            float p = t / tempo;
            Vector2 lugar = new Vector2(x, Mathf.Lerp(de, ate, p * p)); // cai cada vez mais rápido
            faca.transform.position = lugar;
            PosicionarZona(zona, lugar, Vector2.down);
            marca.color = Piscando(t, tempo) ? Vermelho : VermelhoFraco;
            yield return null;
        }
        faca.transform.position = new Vector3(x, ate, 0f);
        zonas.Remove(zona);
        Destroy(marca.gameObject);
        Destroy(luzDaMarca);
        yield return Cravar(faca, Vector2.down);
    }

    // CORTE: a linha vermelha avisa a altura; no fim do aviso ela atravessa a arena num piscar de olhos.
    //  rente (no pé) = PULE;  alto (na altura do pulo) = NÃO pule.
    IEnumerator Corte(bool alto, bool daEsquerda)
    {
        Dica(alto ? "Linha ALTA! Fique no CHÃO!" : "Linha no PÉ! PULE assim que ela aparecer!");
        float y = chao + (alto ? AlturaDoCorteAlto : AlturaDoCorteRente);
        float raio = alto ? RaioDoCorteAlto : RaioDoCorteRente;
        float xDeSaida = daEsquerda ? esquerda + 0.6f : direita - 0.6f;
        float xDeChegada = daEsquerda ? direita - 0.6f : esquerda + 0.6f;
        yield return MoverOlhos(new Vector2(xDeSaida, y + 0.35f), 0.3f);

        olhosVermelhos = true;
        float aviso = alto ? Aviso + 0.1f : AvisoDePulo;
        LinhaDeAviso linha = NovoAviso();
        var exclamacao = Desenho("Exclamacao", "aviso", new Vector3(xDeSaida, y + 1.1f, 0f), OrdemDosAvisos + 1);
        GerenciadorDoJogo.Som("armadilha", 0.5f);
        for (float t = 0f; t < aviso; t += Time.deltaTime)
        {
            bool aceso = Piscando(t, aviso);
            linha.Mostrar(new Vector2(esquerda, y), new Vector2(direita, y), aceso ? Vermelho : VermelhoFraco, 1f);
            exclamacao.enabled = aceso;
            yield return null;
        }
        linha.Apagar();
        Destroy(exclamacao.gameObject);
        olhosVermelhos = false;
        // o rastro que mata pega a arena inteira, de parede a parede
        Vector2 de = new Vector2(daEsquerda ? esquerda : direita, y), ate = new Vector2(daEsquerda ? direita : esquerda, y);
        yield return Golpe(de, ate, raio, alto);
        olhos.position = new Vector2(xDeChegada, y + 0.35f);
    }

    // O golpe de kukri em si: o rastro de luz de "de" até "ate" mata por DuracaoDoCorte segundos,
    // e o corpo dela aparece passando como um borrão.
    IEnumerator Golpe(Vector2 de, Vector2 ate, float raio, bool alto)
    {
        Zona zona = NovaZona(raio);
        zona.a = de;
        zona.b = ate;
        GerenciadorDoJogo.Som("serra");
        CameraSeguir.Tremer(0.1f, 0.2f);
        LinhaDeAviso rastro = NovoAviso("elsa_rastro", Color.white);
        float sentido = Mathf.Sign(ate.x - de.x);
        olhosAbertos = false;
        aberturaDosOlhos = 0f;
        float alturaDoCorpo = alto ? de.y - 0.9f : chao;
        for (float t = 0f; t < DuracaoDoCorte; t += Time.deltaTime)
        {
            float p = Mathf.Clamp01(t / (DuracaoDoCorte * 0.6f)); // o corpo passa voando
            MostrarCorpo(spriteDePe, new Vector2(Mathf.Lerp(de.x, ate.x, p), alturaDoCorpo));
            corpo.flipX = sentido < 0f;
            corpo.transform.rotation = Quaternion.Euler(0f, 0f, -25f * sentido);
            corpo.color = new Color(1f, 1f, 1f, 0.7f);
            rastro.Mostrar(de, ate, CorDoRastro, 1.4f - t / DuracaoDoCorte);
            yield return null;
        }
        zonas.Remove(zona);
        corpo.enabled = false;
        corpo.color = Color.white;
        corpo.transform.rotation = Quaternion.identity;
        rastro.Apagar();
        olhosAbertos = true;
    }

    // BUMERANGUE: a faca voa reto até a parede do outro lado, fica cravada (piscando)... e VOLTA.
    //  rente = pule as duas vezes;  alta = fique no chão as duas vezes.
    IEnumerator Bumerangue(bool alta, bool daEsquerda)
    {
        Dica(alta ? "Faca ALTA! Fique no chão... ela VOLTA!" : "Faca no PÉ! Pule... e pule de novo: ela VOLTA!");
        float y = chao + (alta ? AlturaDaFacaAlta : AlturaDaFacaRente);
        float sentido = daEsquerda ? 1f : -1f;
        Vector2 inicio = new Vector2(daEsquerda ? esquerda + 0.1f : direita - 0.1f, y);
        yield return MoverOlhos(inicio + new Vector2(0.3f * sentido, 0.45f), 0.3f);

        // aviso: olhos vermelhos e a faca na mão, apontada
        olhosVermelhos = true;
        Vector2 direcao = new Vector2(sentido, 0f);
        SpriteRenderer faca = FacasDaElsa.DesenharFaca(efeitos, inicio, OrdemDosAtaques, out _);
        FacasDaElsa.ApontarDesenho(faca, direcao);
        LinhaDeAviso linha = NovoAviso();
        float aviso = alta ? Aviso + 0.1f : AvisoDePulo;
        GerenciadorDoJogo.Som("pop", 0.6f);
        for (float t = 0f; t < aviso; t += Time.deltaTime)
        {
            linha.Mostrar(new Vector2(esquerda, y), new Vector2(direita, y), Piscando(t, aviso) ? Vermelho : VermelhoFraco, 0.7f);
            yield return null;
        }
        linha.Apagar();
        olhosVermelhos = false;
        olhosAbertos = false; // ela se esconde e deixa a faca trabalhar

        // ida: até a ponta encostar na parede do outro lado
        GerenciadorDoJogo.Som("serra", 0.45f);
        float velocidade = VelocidadeDoBumerangue[etapa];
        Zona zona = NovaZona(RaioDaFaca);
        float paredeDoOutroLado = daEsquerda ? direita : esquerda;
        float x = inicio.x;
        while ((paredeDoOutroLado - (x + 0.6f * sentido)) * sentido > 0f)
        {
            x += sentido * velocidade * Time.deltaTime;
            x = sentido > 0f ? Mathf.Min(x, paredeDoOutroLado - 0.6f) : Mathf.Max(x, paredeDoOutroLado + 0.6f);
            MoverBumerangue(faca, zona, new Vector2(x, y), direcao);
            yield return null;
        }

        // cravada na parede (aí não mata: dá para sair de perto), tremendo, com a linha avisando a volta
        GerenciadorDoJogo.Som("bloco", 0.5f);
        Vector2 cravada = new Vector2(x, y);
        zona.a = zona.b = new Vector2(-9999f, -9999f);
        float espera = TempoCravada[etapa];
        for (float t = 0f; t < espera; t += Time.deltaTime)
        {
            faca.transform.position = cravada + Random.insideUnitCircle * 0.03f;
            linha.Mostrar(new Vector2(esquerda, y), new Vector2(direita, y), Piscando(t, espera) ? Vermelho : VermelhoFraco, 0.7f);
            yield return null;
        }
        linha.Apagar();

        // volta: de costas, pelo mesmo caminho, até sair pela parede de onde veio
        GerenciadorDoJogo.Som("serra", 0.35f);
        float saida = daEsquerda ? esquerda - 1f : direita + 1f;
        while ((x - saida) * sentido > 0f)
        {
            x -= sentido * velocidade * Time.deltaTime;
            MoverBumerangue(faca, zona, new Vector2(x, y), -direcao); // a lâmina que mata vai na frente
            faca.transform.Rotate(0f, 0f, 900f * Time.deltaTime); // girando, como um bumerangue
            yield return null;
        }
        zonas.Remove(zona);
        Destroy(faca.gameObject);
        olhos.position = inicio + new Vector2(0.3f * sentido, 0.45f);
        olhosAbertos = true;
    }

    void MoverBumerangue(SpriteRenderer faca, Zona zona, Vector2 lugar, Vector2 direcao)
    {
        faca.transform.position = lugar;
        PosicionarZona(zona, lugar, direcao);
        if (Random.value < 0.4f) Particula.Criar(efeitos, lugar, Vector2.zero, 0.18f, 0.5f, FacasDaElsa.CorDoRastro);
    }

    // ATRÁS DE VOCÊ: os olhos piscam e somem (com uma risadinha)... e aparecem VERMELHOS atrás do Subaru,
    // perto do chão. A linha vermelha mostra o corte rente: PULE.
    IEnumerator Atras()
    {
        Dica("Sumiu?! Ouviu a risada? Ela tá ATRÁS de você! PULE!");
        olhosPiscando = true;
        GerenciadorDoJogo.Som("risada", 0.5f);
        yield return new WaitForSeconds(0.3f);
        olhosPiscando = false;
        olhosAbertos = false;
        aberturaDosOlhos = 0f;
        yield return new WaitForSeconds(0.15f);
        if (!GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) yield break;

        // "atrás" = do lado contrário de onde ele está indo; parado, do lado com mais espaço
        float vx = GerenciadorDoJogo.JogadorAtual.Corpo.linearVelocity.x;
        float lado = vx > 0.5f ? -1f : vx < -0.5f ? 1f : (jogador.x - esquerda > direita - jogador.x ? -1f : 1f);
        float x = jogador.x + lado * DistanciaDoAtras;
        if (x < esquerda + 0.6f || x > direita - 0.6f) // sem espaço atrás: aparece na frente
        {
            lado = -lado;
            x = jogador.x + lado * DistanciaDoAtras;
        }
        float y = chao + AlturaDoCorteRente;
        olhos.position = new Vector2(x, chao + 1.1f);
        olhosAbertos = true;
        aberturaDosOlhos = 1f;
        olhosVermelhos = true;
        GerenciadorDoJogo.Som("pop", 0.8f);

        Vector2 de = new Vector2(x, y);
        Vector2 ate = new Vector2(Mathf.Clamp(x - lado * AlcanceDoAtras, esquerda, direita), y);
        LinhaDeAviso linha = NovoAviso();
        float aviso = AvisoDePulo;
        for (float t = 0f; t < aviso; t += Time.deltaTime)
        {
            linha.Mostrar(de, ate, Piscando(t, aviso) ? Vermelho : VermelhoFraco, 1f);
            yield return null;
        }
        linha.Apagar();
        olhosVermelhos = false;
        yield return Golpe(de, ate, RaioDoCorteRente, false);
        olhos.position = new Vector2(ate.x, chao + 1.1f);
    }

    // ------------------------------------------------------------------ os olhos e o corpo

    IEnumerator MoverOlhos(Vector2 destino, float duracao)
    {
        olhosAbertos = true;
        Vector2 de = olhos.position;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            olhos.position = Vector2.Lerp(de, destino, Mathf.SmoothStep(0f, 1f, t / duracao));
            yield return null;
        }
        olhos.position = destino;
    }

    // Abre/fecha aos poucos, pisca quando vai sumir, fica vermelho no aviso. A luz acompanha.
    void AnimarOlhos()
    {
        float alvo = olhosAbertos ? 1f : 0f;
        aberturaDosOlhos = Mathf.MoveTowards(aberturaDosOlhos, alvo, 5f * Time.deltaTime);
        float alfa = aberturaDosOlhos;
        if (olhosPiscando) alfa *= Mathf.Repeat(relogio * 10f, 1f) < 0.5f ? 1f : 0f;
        desenhoDosOlhos.sprite = olhosVermelhos ? olhosBravos : olhosNormais;
        desenhoDosOlhos.color = new Color(1f, 1f, 1f, alfa);
        if (luzDosOlhos == null) return;
        luzDosOlhos.color = olhosVermelhos ? FacasDaElsa.CorDoAviso : FacasDaElsa.CorDosOlhos;
        luzDosOlhos.pointLightOuterRadius = olhosVermelhos ? 2.6f : 1.6f;
        luzDosOlhos.intensity = intensidadeDosOlhos * alfa * (olhosVermelhos ? 1.3f + 0.4f * Mathf.Sin(relogio * 40f) : 0.8f);
    }

    void MostrarCorpo(Sprite pose, Vector2 pes)
    {
        corpo.sprite = pose;
        corpo.transform.position = pes;
        corpo.transform.rotation = Quaternion.identity;
        corpo.flipX = GerenciadorDoJogo.JogadorVivo(out Vector2 jogador) && jogador.x < pes.x; // olha para o Subaru
        corpo.color = Color.white;
        corpo.enabled = true;
    }

    void CriarEstrelas()
    {
        for (int i = 0; i < 3; i++)
            estrelas.Add(Desenho("Estrela", "brilho", corpo.transform.position, OrdemDosOlhos).transform);
    }

    void GirarEstrelas()
    {
        Vector2 cabeca = (Vector2)corpo.transform.position + Vector2.up * (AlturaDaCabecaTonta + 0.3f);
        for (int i = 0; i < estrelas.Count; i++)
        {
            float angulo = relogio * 5f + i * 2.1f;
            if (estrelas[i] != null) estrelas[i].position = cabeca + new Vector2(Mathf.Cos(angulo) * 0.7f, Mathf.Sin(angulo) * 0.2f);
        }
    }

    void ApagarEstrelas()
    {
        foreach (Transform estrela in estrelas) if (estrela != null) Destroy(estrela.gameObject);
        estrelas.Clear();
    }

    // ------------------------------------------------------------------ avisos (linhas com luz)

    // Uma linha esticada de a até b, com luzinhas ao longo dela (no escuro, um desenho sem luz quase
    // não aparece: as luzes é que deixam o aviso visível).
    class LinhaDeAviso
    {
        public GameObject raiz;
        public SpriteRenderer linha;
        public List<Light2D> luzes = new List<Light2D>();
        Vector2 ultimoA, ultimoB;

        public void Mostrar(Vector2 a, Vector2 b, Color cor, float grossura)
        {
            if (raiz == null) return;
            Esticar(linha.transform, a, b, grossura);
            linha.color = cor;
            // as luzes só são (re)colocadas quando a linha muda de lugar
            if ((a - ultimoA).sqrMagnitude < 0.0001f && (b - ultimoB).sqrMagnitude < 0.0001f) return;
            ultimoA = a;
            ultimoB = b;
            int quantas = Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(a, b) / 3f), 1, 9);
            for (int i = 0; i < luzes.Count; i++)
            {
                if (luzes[i] == null) continue;
                luzes[i].enabled = i < quantas;
                luzes[i].transform.position = Vector2.Lerp(a, b, (i + 0.5f) / quantas);
            }
        }

        public void Apagar()
        {
            if (raiz != null) Object.Destroy(raiz);
            raiz = null;
        }
    }

    LinhaDeAviso NovoAviso(string sprite = "baleia_linha", Color? corDaLuz = null)
    {
        var aviso = new LinhaDeAviso { raiz = new GameObject("Aviso") };
        aviso.raiz.transform.SetParent(efeitos, false);
        aviso.linha = ConstrutorDeFase.Visual("Linha", aviso.raiz.transform, transform.position, sprite, OrdemDosAvisos, false).GetComponent<SpriteRenderer>();
        for (int i = 0; i < 9; i++)
        {
            var ponto = new GameObject("LuzDoAviso");
            ponto.transform.SetParent(aviso.raiz.transform, false);
            Light2D luz = Luzes.Ponto(ponto.transform, corDaLuz ?? FacasDaElsa.CorDoAviso, 2f, 0.6f);
            if (luz != null)
            {
                luz.enabled = false;
                aviso.luzes.Add(luz);
            }
        }
        return aviso;
    }

    // ------------------------------------------------------------------ geometria da arena

    // Distância de "de" (andando na direção) até a primeira coisa sólida: chão, paredes da arena,
    // teto (bem lá em cima) ou um degrau. É até onde a faca vai antes de cravar.
    float Alcance(Vector2 de, Vector2 direcao)
    {
        float alcance = 40f;
        if (direcao.y < -0.001f) alcance = Mathf.Min(alcance, (chao - de.y) / direcao.y);
        if (direcao.y > 0.001f) alcance = Mathf.Min(alcance, (chao + 14f - de.y) / direcao.y);
        if (direcao.x > 0.001f) alcance = Mathf.Min(alcance, (direita - de.x) / direcao.x);
        if (direcao.x < -0.001f) alcance = Mathf.Min(alcance, (esquerda - de.x) / direcao.x);
        foreach (Rect degrau in degraus)
        {
            float entrada = EntradaNoRetangulo(de, direcao, degrau);
            if (entrada >= 0f) alcance = Mathf.Min(alcance, entrada);
        }
        return Mathf.Max(0f, alcance);
    }

    // Em que distância o raio (de, direção) entra no retângulo (-1 = não entra). Método das "fatias".
    static float EntradaNoRetangulo(Vector2 de, Vector2 direcao, Rect r)
    {
        float tMin = 0f, tMax = 100f;
        for (int eixo = 0; eixo < 2; eixo++)
        {
            float o = eixo == 0 ? de.x : de.y, d = eixo == 0 ? direcao.x : direcao.y;
            float min = eixo == 0 ? r.xMin : r.yMin, max = eixo == 0 ? r.xMax : r.yMax;
            if (Mathf.Abs(d) < 0.0001f)
            {
                if (o < min || o > max) return -1f;
                continue;
            }
            float t1 = (min - o) / d, t2 = (max - o) / d;
            if (t1 > t2) { float troca = t1; t1 = t2; t2 = troca; }
            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);
            if (tMin > tMax) return -1f;
        }
        return tMin;
    }

    // Altura do piso em x: o topo de um degrau, se tiver um ali, senão o chão.
    float AlturaDoPiso(float x)
    {
        foreach (Rect degrau in degraus)
            if (x > degrau.xMin && x < degrau.xMax) return degrau.yMax;
        return chao;
    }

    static Vector2 DirecaoPara(Vector2 de, Vector2 alvo)
    {
        Vector2 d = alvo - de;
        return d.sqrMagnitude < 0.0001f ? Vector2.down : d.normalized;
    }

    static Vector2 Girar(Vector2 v, float graus)
    {
        float r = graus * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    // Os degraus: 3 tijolos lado a lado com UM colisor só (sem emendas para enganchar) e uma beirada
    // lilás brilhando em cima, desenhada por cima da escuridão (para achar o degrau no breu da etapa 3).
    void MontarDegraus()
    {
        foreach (int coluna in ColunasDosDegraus)
        {
            var degrau = new GameObject("Degrau");
            degrau.transform.SetParent(transform, false);
            Vector2 centro = new Vector2(esquerda + coluna + LarguraDoDegrau / 2f, chao + 1.5f);
            degrau.transform.position = centro;
            degrau.AddComponent<BoxCollider2D>().size = new Vector2(LarguraDoDegrau, 1f);
            for (int i = 0; i < LarguraDoDegrau; i++)
                ConstrutorDeFase.Visual("Tijolo", degrau.transform, new Vector3(esquerda + coluna + i + 0.5f, chao + 1.5f, 0f), "tijolo", 0);
            var beirada = ConstrutorDeFase.Visual("Beirada", degrau.transform, centro, "baleia_linha", OrdemDasBeiradas, false).GetComponent<SpriteRenderer>();
            Esticar(beirada.transform, new Vector2(centro.x - LarguraDoDegrau / 2f, chao + 2f), new Vector2(centro.x + LarguraDoDegrau / 2f, chao + 2f), 0.6f);
            beirada.color = new Color(0.8f, 0.55f, 1f, 0.7f);
            Luzes.Ponto(degrau.transform, Luzes.Lilas, 2f, 0.4f, Vector3.up * 0.6f);
            degraus.Add(new Rect(esquerda + coluna, chao + 1f, LarguraDoDegrau, 1f));
        }
    }

    // Paredes de sombra nas duas pontas da arena (colisor de verdade + bolotas de névoa escura).
    void FecharArena()
    {
        foreach (float x in new[] { esquerda - 0.5f, direita + 0.5f })
        {
            var parede = new GameObject("ParedeDeSombra");
            parede.transform.SetParent(transform, false);
            parede.transform.position = new Vector3(x, chao + 7f, 0f);
            parede.AddComponent<BoxCollider2D>().size = new Vector2(1f, 20f);
            for (float y = chao; y < chao + 13f; y += 0.8f)
            {
                var bolota = ConstrutorDeFase.Visual("Sombra", parede.transform, new Vector3(x + Random.Range(-0.2f, 0.2f), y, 0f), "nevoa", 12, false);
                bolota.GetComponent<SpriteRenderer>().color = new Color(0.45f, 0.25f, 0.6f);
                bolota.transform.localScale = Vector3.one * Random.Range(1.3f, 1.7f);
                Animacao.Adicionar(bolota, Animacao.Tipo.Flutuar, 2f, 0.15f);
            }
            Luzes.Ponto(parede.transform, Luzes.Lilas, 3f, 0.5f, Vector3.down * 5.5f);
            paredes.Add(parede);
        }
    }

    void AbrirArena()
    {
        foreach (GameObject parede in paredes)
        {
            if (parede == null) continue;
            parede.GetComponent<BoxCollider2D>().enabled = false;
            Destroy(parede, 0.05f);
        }
        paredes.Clear();
    }

    // ------------------------------------------------------------------ zonas que matam

    Zona NovaZona(float raio)
    {
        var zona = new Zona { raio = raio };
        zona.a = zona.b = new Vector2(-9999f, -9999f); // longe de tudo até o ataque dizer onde está
        zonas.Add(zona);
        return zona;
    }

    // A área que mata de uma faca: a lâmina (um pouco menor que o desenho, é mais justo).
    static void PosicionarZona(Zona zona, Vector2 centro, Vector2 direcao)
    {
        zona.a = centro - direcao * 0.05f;
        zona.b = centro + direcao * 0.45f;
    }

    void TestarPerigos()
    {
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (jogador == null || jogador.Morto || estado == Estado.Derrotada) return;
        Vector2 p = (Vector2)jogador.transform.position + new Vector2(0f, -0.02f); // centro da caixa dele
        bool encostou = false;
        // o Subaru vira uma "salsicha" em pé: 3 pontos no meio dele, com a meia largura de raio
        for (int i = 0; i < zonas.Count && !encostou; i++)
            for (int k = -1; k <= 1 && !encostou; k++)
            {
                Vector2 ponto = p + new Vector2(0f, k * (MeiaAlturaDoJogador - MeiaLarguraDoJogador));
                if (DistanciaAoSegmento(ponto, zonas[i].a, zonas[i].b) < zonas[i].raio + MeiaLarguraDoJogador) encostou = true;
            }
        if (!encostou) return;
        Conquistas.Desbloquear("elsa"); // "Entranhas à mostra"
        jogador.Morrer();
    }

    static float DistanciaAoSegmento(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float comprimento2 = Vector2.Dot(ab, ab);
        float t = comprimento2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / comprimento2) : 0f;
        return Vector2.Distance(p, a + ab * t);
    }

    // ------------------------------------------------------------------ ataques ao mesmo tempo

    // Começa um ataque "solto" (sem esperar ele acabar). Guardamos a corrotina para poder parar
    // tudo de uma vez no clarão. (o "yield return ataque" roda o ataque DENTRO desta corrotina:
    // parando esta, o ataque para junto)
    void Paralelo(IEnumerator ataque) => soltos.Add(StartCoroutine(Contar(ataque)));

    IEnumerator Contar(IEnumerator ataque)
    {
        emParalelo++;
        yield return ataque;
        emParalelo--;
    }

    IEnumerator EsperarTodos()
    {
        while (emParalelo > 0) yield return null;
    }

    void PararSoltos()
    {
        foreach (Coroutine solto in soltos) if (solto != null) StopCoroutine(solto);
        soltos.Clear();
        emParalelo = 0;
        arremessando = false;
        olhosVermelhos = false;
        olhosPiscando = false;
    }

    // Apaga tudo que é passageiro (avisos, facas, marcas) e as zonas que matam.
    void LimparEfeitos()
    {
        zonas.Clear();
        foreach (Transform filho in efeitos) Destroy(filho.gameObject);
    }

    // ------------------------------------------------------------------ ajudantes

    SpriteRenderer Desenho(string nome, string sprite, Vector3 lugar, int ordem) =>
        ConstrutorDeFase.Visual(nome, efeitos, lugar, sprite, ordem, false).GetComponent<SpriteRenderer>();

    // Estica um desenho de "1 pixel de comprimento" (linha, rastro) de a até b.
    static void Esticar(Transform objeto, Vector2 a, Vector2 b, float grossura)
    {
        Vector2 d = b - a;
        objeto.position = (a + b) / 2f;
        objeto.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        objeto.localScale = new Vector3(d.magnitude * FabricaDeSprites.PixelsPorUnidade, grossura, 1f);
    }

    // Avisos piscam, e piscam mais rápido no finalzinho ("vai AGORA").
    static bool Piscando(float t, float total) => Mathf.Repeat(t * (total - t < 0.2f ? 22f : 9f), 1f) < 0.6f;

    // Dicas do Puck: só na etapa 1, e cada uma só uma vez por tentativa.
    void Dica(string texto)
    {
        if (etapa > 0 || !dicasMostradas.Add(texto)) return;
        GerenciadorDoJogo.Instancia.Avisar(texto, 1.3f);
    }

    // ------------------------------------------------------------------ desenhos

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("elsa_chefe", () => FabricaDeSprites.DeArte(ArteElsaDePe, FabricaDeSprites.Base));
        FabricaDeSprites.Registrar("elsa_tonta", () => FabricaDeSprites.DeArte(ArteElsaTonta, FabricaDeSprites.Base));
        FabricaDeSprites.Registrar("cristal_de_luz", () => FabricaDeSprites.DeArte(ArteCristal, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("elsa_rastro", () => FabricaDeSprites.Procedural(1, 8, FabricaDeSprites.Centro, (x, y) => CorDoRastroDoCorte(y)));
        FabricaDeSprites.Registrar("elsa_escuridao", () => FabricaDeSprites.Procedural(320, 200, FabricaDeSprites.Centro, CorDaEscuridao));
    }

    // O rastro do corte: branco no meio, sumindo nas beiradas.
    static Color32 CorDoRastroDoCorte(int y)
    {
        float meioDoRastro = 1f - Mathf.Abs(y - 3.5f) / 4f;
        return new Color32(255, 255, 255, (byte)(255 * meioDoRastro * meioDoRastro));
    }

    // Preto-arroxeado com um buraco no meio; a borda do buraco vai escurecendo em pontinhos
    // (matriz de Bayer 4x4, o mesmo truque da escuridão da Baleia).
    static Color32 CorDaEscuridao(int x, int y)
    {
        float dx = x - 159.5f, dy = y - 99.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
        const float claro = 13f, escuro = 19f;
        var cor = new Color32(6, 2, 14, 255);
        if (d >= escuro) return cor;
        if (d <= claro) return FabricaDeSprites.Transparente;
        int[] bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
        float limite = (bayer[(y % 4) * 4 + (x % 4)] + 0.5f) / 16f;
        float p = (d - claro) / (escuro - claro);
        if (p > limite) return cor;
        return new Color32(6, 2, 14, (byte)(160 * p));
    }

    // A Elsa de pé: cabelo preto comprido (com franja), olhos lilases, sorriso, roupa e capa pretas
    // com a beirada roxa, e a kukri na mão esquerda (do lado direito do desenho).
    static readonly string[] ArteElsaDePe =
    {
        "....kkkkkkk.....",
        "...khhhhhhhk....",
        "..khhHHhhhhhk...",
        "..khhhhhhhhhk...",
        ".khhhhhhhhhhhk..",
        ".khhkcccccckhhk.",
        ".khkcVwccVwckhk.",
        ".khkccccCccckhk.",
        ".khkkccrrcckkhk.",
        ".khhjkcccckjhhk.",
        "khhjjjvccvjjjhhk",
        "khjjjjjvvjjjjjhk",
        "khjjJjjjjjjjjjhk",
        "khjjJjjjjjjjcckk",
        "khjjJjjjjjjjkdsk",
        "khvjjjjjjjjjvksk",
        "khvjjjjjjjjjvssk",
        ".hvjjjjjjjjjvsSk",
        ".hvvjjjjjjjjvks.",
        ".khvjjjjjjjjvk..",
        "..kvjjjjjjjjvk..",
        "..kvvjjjjjjvvk..",
        "...kvjjjjjjvk...",
        "...kjjk..kjjk...",
        "...kjjk..kjjk...",
        "..kkkkk..kkkkk..",
    };

    // A Elsa ofuscada: sentada no chão, olhos girando e a boca torta.
    static readonly string[] ArteElsaTonta =
    {
        "....kkkkkkk.....",
        "...khhhhhhhk....",
        "..khhHHhhhhhk...",
        ".khhhhhhhhhhhk..",
        ".khhkcccccckhhk.",
        ".khkcPVccPVckhk.",
        ".khkcVPccVPckhk.",
        ".khkkcrcrcckkhk.",
        ".khhjkcccckjhhk.",
        "khhjjjvccvjjjhhk",
        "khjjjjjvvjjjjjhk",
        "khjjJjjjjjjjjjhk",
        "khvjjjjjjjjjjvhk",
        "kvvjjjjjjjjjjvvk",
        "kvjjjjjjjjjjjjvk",
        "kvvjjjjjjjjjjvvk",
        ".kvvvvvvvvvvvvk.",
        "..kkkkkkkkkkkk..",
    };

    // O cristal de luz (a lâmpada mágica do Roswaal): amarelo-claro, com o brilho branco.
    static readonly string[] ArteCristal =
    {
        "....kk....",
        "...kwyk...",
        "..kwyyyk..",
        "..kwyyyk..",
        ".kwyyyyyk.",
        ".kwwyyyyk.",
        "kwwyyyyyyk",
        "kwyyyyyyyk",
        "kwyyyyyyok",
        ".kyyyyyok.",
        ".kyyyyyok.",
        "..kyyyok..",
        "..kyyook..",
        "...kyok...",
        "...kook...",
        "....kk....",
    };
}
