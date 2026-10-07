using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 'u' - a REM, chefe da fase 6 ("Oni de Cabelo Azul"). A empregada de cabelo azul com o MANGUAL
// (a bola de espinhos na corrente; os desenhos dela ficam em ArteDaRem.cs e os da bola em MangualDaRem.cs).
//
// O 'u' marca o começo da arena (na linha em que o jogador anda); a arena tem larguraDaArena blocos
// a partir dele e o chão dela precisa ser LISO (sem buracos). Entrou, paredes de tijolo fecham a arena,
// a câmera trava e a luta começa. É o mesmo esquema da Baleia Branca (BaleiaBranca.cs), mais curto e mais fácil:
//  - tudo mata com UM toque, mas todo ataque AVISA antes (linha vermelha, anel vermelho, marca no chão);
//  - os padrões são FIXOS: dá para decorar;
//  - são 3 ETAPAS de 2 golpes. Morreu? Volta na hora, no começo da arena, JÁ na etapa em que estava.
//
// REGRA DE OURO: só a BOLA mata (e a Rem só quando cai de um SALTO em cima de você).
// Girando em cima da cabeça dela, a bola é só enfeite. Ela só machuca quando a Rem ATACA, sempre depois do aviso.
//
// OS ATAQUES
//  ARREMESSO: uma linha vermelha mostra a altura; a bola voa reto e volta puxada pela corrente.
//     Linha BAIXA (rente ao chão) = PULE (na ida e na volta). Linha ALTA = NÃO pule (fique no chão).
//     A partir da etapa 2 a volta pode vir na OUTRA altura (a linha fraquinha mostra; ela pisca de novo na ponta).
//  GIRO: um anel vermelho mostra por onde a bola vai passar. Fique LONGE (fora do anel) ou COLADO nela
//     (dentro do anel, "no olho do furacão", sem pular!). Na etapa 2 ela também anda girando.
//  SALTO: a marca no chão segue você, trava (fica vermelha) e ela cai ali. Saia de baixo.
//  GOLPE DE CIMA: a marca segue você, trava, e a bola despenca ali soltando PEDRAS rolando para os dois lados (pule).
//     Aí a bola fica PRESA no chão e a Rem fica puxando a corrente... É A HORA: PULE NA CABEÇA DELA!
//  ETAPA 3: ela vira ONI (chifre, olho vermelho, aura) e tudo fica mais rápido, com combos.
public class ChefeRem : Chefe
{
    const int EtapasDaLuta = 3;
    const int GolpesDaEtapa = 2;

    public int vidaMaxima = EtapasDaLuta * GolpesDaEtapa;
    public float larguraDaArena = 20f;

    // O que a barra de vida (Interface) lê. Tudo isso vem da classe base Chefe (veja Chefe.cs).
    public override string Nome => "REM";
    public override int Vida => vida;
    public override float VidaMostrada => vidaMostrada; // a barra "escorre" até a vida de verdade
    public override bool EmCombate => emCombate;
    public override int VidaMaxima => vidaMaxima;
    public override int EtapaAtual => etapa;            // 0, 1 ou 2
    public override int Etapas => EtapasDaLuta;
    public override int GolpesPorEtapa => GolpesDaEtapa;

    // ------------------------------------------------------------------ ajustes de cada etapa (1, 2, 3)

    static readonly float[] TempoDeAviso = { 0.6f, 0.5f, 0.4f };
    static readonly float[] Respiro = { 0.5f, 0.4f, 0.25f };          // pausa entre um ataque e outro
    static readonly float[] TempoSeguindo = { 0.7f, 0.55f, 0.45f };   // a marca do salto/golpe segue você
    static readonly float[] TempoPresa = { 2.2f, 1.9f, 1.6f };        // quanto tempo a bola fica presa no chão
    static readonly float[] VelocidadeDoArremesso = { 13f, 15f, 17f };
    static readonly float[] VelocidadeDaPedra = { 6f, 7f, 8.5f };
    static readonly float[] VelocidadeDoGiro = { 300f, 340f, 400f };  // graus por segundo

    const float AlturaBaixa = 0.45f, AlturaAlta = 1.6f; // alturas do arremesso (centro da bola, acima do chão)
    const float AlcanceDoArremesso = 9f;
    const float PausaNaPonta = 0.25f;                    // a bola "quica" na ponta antes de voltar
    const float DuracaoDoSalto = 0.55f, AlturaDoSalto = 3.5f;
    const float VelocidadeAndando = 2f;                  // giro andando (etapas 2 e 3)
    const float DuracaoDoGiroAndando = 2.4f;
    const float DistanciaDaParede = 4.2f;                // girando, ela não chega mais perto da parede do que isso
    const float AlturaDoGiro = 1f;                       // o centro do giro (a mão dela), acima do chão
    const float AlturaDoTombo = 6f;                      // golpe de cima: de onde a bola despenca
    const float RaioDoImpacto = 0.6f;
    const float RaioDaPedra = 0.3f;
    const float AtrasoDasPedras = 0.3f;                  // as pedras brotam do buraco antes de rolar

    // Ordem de desenho: a bola fica ATRÁS dos blocos (entra no chão), como no mangual normal.
    const int OrdemDosElos = -2, OrdemDaBola = -1, OrdemDaRem = 8, OrdemDosAvisos = 55, OrdemDasEstrelas = 58;

    // A "caixa" do Subaru que conta para morrer (um tiquinho menor que o desenho: é mais justo).
    const float MeiaLarguraDoJogador = 0.27f, MeiaAlturaDoJogador = 0.42f;
    // O corpo da Rem (só machuca quando ela cai de um salto) e o topo da cabeça (onde se pisa).
    const float MeiaLarguraDoCorpo = 0.45f, AlturaDoCorpo = 1.55f, TopoDaCabeca = 1.625f;

    // Onde fica a mão dela em cada pose (olhando para a direita), a partir dos pés. Mesma ordem do enum Pose.
    static readonly Vector2[] Maos =
    {
        new Vector2(0.42f, 0.47f), // parada
        new Vector2(0.41f, 0.75f), // girando
        new Vector2(0.35f, 0.75f), // golpe
        new Vector2(0.5f, 0.59f),  // presa
        new Vector2(0.42f, 0.47f), // atordoada
    };

    static readonly Color Vermelho = new Color(1f, 0.2f, 0.3f, 1f);
    static readonly Color VermelhoFraco = new Color(1f, 0.2f, 0.3f, 0.35f);
    static readonly Color VermelhoBemFraco = new Color(1f, 0.2f, 0.3f, 0.15f);
    static readonly Color CorDaAura = new Color(1f, 0.3f, 0.45f, 0.7f);

    // ------------------------------------------------------------------ estado

    public enum Pose { Parada, Girando, Golpe, Presa, Atordoada }

    enum Estado { Esperando, Lutando, Presa, Derrotada }
    Estado estado;
    int vida;
    float vidaMostrada;
    bool emCombate;
    int etapa;
    bool parado;          // o jogador morreu: tudo congela até a fase recomeçar
    bool oni;             // etapa 3: chifre, olho vermelho e aura
    bool bolaNaCabeca;    // a bola girando em cima da cabeça dela (só enfeite; o Animar cuida dela)
    bool corpoPerigoso;   // caindo de um salto: encostar nela mata
    bool golpeada;        // levou um pisão nesta "presa"
    Pose pose = Pose.Girando;
    float chao, esquerda, direita, meio;
    float relogio, timerPiscar, timerAura, giroNaCabeca, tremor;
    int emParalelo;

    Transform rem, bola;
    SpriteRenderer desenho;
    Sprite[] sprites, spritesOni;
    readonly List<SpriteRenderer> elos = new List<SpriteRenderer>();
    const int MaximoDeElos = 40;
    const float DistanciaEntreElos = 0.42f;

    // Uma área que mata: tudo a menos de "raio" do segmento a-b (uma bola é um segmento de tamanho zero).
    // Igual à da Baleia Branca: cada ataque cria a sua e a Rem testa todas a cada quadro.
    class Zona { public Vector2 a, b; public float raio; }
    readonly List<Zona> zonas = new List<Zona>();
    Zona zonaDaBola; // != null enquanto a bola está machucando

    readonly List<GameObject> paredes = new List<GameObject>();
    readonly List<Transform> estrelas = new List<Transform>();
    readonly HashSet<string> dicasMostradas = new HashSet<string>();

    float Aviso => TempoDeAviso[etapa];
    float Lado => desenho.flipX ? -1f : 1f; // para que lado ela está olhando
    Vector3 PontoDeRenascer => new Vector3(esquerda + 0.5f, chao + 0.5f, 0f); // a casa do 'u'
    Vector2 Mao => (Vector2)rem.position + new Vector2(Maos[(int)pose].x * Lado, Maos[(int)pose].y);
    Vector2 CentroDoGiro => new Vector2(rem.position.x, chao + AlturaDoGiro);

    // ------------------------------------------------------------------ Unity

    void Awake()
    {
        chao = transform.position.y - 0.5f;
        esquerda = transform.position.x - 0.5f;   // borda esquerda da casa do 'u'
        direita = esquerda + larguraDaArena;
        meio = (esquerda + direita) / 2f;
        vida = vidaMaxima;
        vidaMostrada = vida;

        int poses = ArteDaRem.Poses.Length;
        sprites = new Sprite[poses];
        spritesOni = new Sprite[poses];
        for (int i = 0; i < poses; i++)
        {
            sprites[i] = FabricaDeSprites.Pegar(ArteDaRem.NomeDoSprite(i, false));
            spritesOni[i] = FabricaDeSprites.Pegar(ArteDaRem.NomeDoSprite(i, true));
        }

        // A Rem começa no lado direito da arena, olhando para quem chega (para a esquerda).
        desenho = ConstrutorDeFase.Visual("Rem", transform, new Vector3(direita - 4f, chao, 0f), "rem_girando", OrdemDaRem)
            .GetComponent<SpriteRenderer>();
        rem = desenho.transform;
        desenho.flipX = true;

        // A bola (o mesmo desenho do mangual 'Q') e os elos da corrente.
        bola = ConstrutorDeFase.Visual("Bola", transform, rem.position, "mangual", OrdemDaBola, false).transform;
        for (int i = 0; i < MaximoDeElos; i++)
        {
            string elo = i % 2 == 0 ? "mangual_elo" : "mangual_elo_lado"; // alternando: parece corrente de verdade
            elos.Add(ConstrutorDeFase.Visual("Elo", transform, rem.position, elo, OrdemDosElos, false).GetComponent<SpriteRenderer>());
        }
        bolaNaCabeca = true;

        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.Chefe = this;
    }

    void Start()
    {
        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        if (jogo == null) return;
        // já venceu nesta fase (morreu depois)? Ela fica lá, desmaiada.
        if (jogo.ChefeDerrotado)
        {
            estado = Estado.Derrotada;
            Desmaiada();
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

        if (estado == Estado.Derrotada && !emCombate) return; // desmaiada no chão

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
            zonaDaBola = null;
            corpoPerigoso = false;
        }
        if (parado) return;

        Animar();
        if (estado == Estado.Presa) TestarPisao();
        if (pose == Pose.Atordoada) GirarEstrelas();
        TestarPerigos();
    }

    // Escolhe o desenho, pisca depois de apanhar, solta a aura de oni e arruma a bola e a corrente.
    void Animar()
    {
        desenho.sprite = (oni ? spritesOni : sprites)[(int)pose];

        if (timerPiscar > 0f)
        {
            timerPiscar -= Time.deltaTime;
            desenho.color = (int)(timerPiscar * 16f) % 2 == 0 ? new Color(1f, 0.45f, 0.45f) : Color.white;
            if (timerPiscar <= 0f) desenho.color = Color.white;
        }

        // a aura do oni: fumacinha vermelha subindo do corpo
        if (oni && estado != Estado.Derrotada)
        {
            timerAura -= Time.deltaTime;
            if (timerAura <= 0f)
            {
                timerAura = 0.06f;
                Vector3 lugar = rem.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 1.6f), 0f);
                Particula.Criar(transform, lugar, new Vector2(0f, Random.Range(1f, 2f)), 0.5f, 0.35f, CorDaAura);
            }
        }

        // Girando em cima da cabeça (enfeite): uma "hélice" achatada, que gira mais rápido quando ela vai atacar.
        if (bolaNaCabeca)
        {
            giroNaCabeca += Time.deltaTime * (oni ? 11f : 8f);
            bola.position = new Vector3(rem.position.x + Mathf.Cos(giroNaCabeca) * 0.9f,
                                        chao + 2.5f + Mathf.Sin(giroNaCabeca) * 0.2f, 0f);
        }
        bola.rotation = Quaternion.Euler(0f, 0f, relogio * 400f); // a bola rola

        PosicionarCorrente();
    }

    // Os elos vão da mão dela até a bola, um a cada DistanciaEntreElos (os que sobram ficam escondidos).
    void PosicionarCorrente()
    {
        Vector2 de = Mao, ate = bola.position;
        Vector2 d = ate - de;
        float comprimento = d.magnitude - MangualDaRem.RaioDaBola; // a corrente termina na beirada da bola
        int quantos = Mathf.Clamp(Mathf.CeilToInt(comprimento / DistanciaEntreElos), 1, MaximoDeElos);
        float angulo = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        Vector2 direcao = d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.right;
        Vector2 deLado = new Vector2(-direcao.y, direcao.x);
        for (int i = 0; i < elos.Count; i++)
        {
            bool aparece = i < quantos && comprimento > 0.1f;
            elos[i].enabled = aparece;
            if (!aparece) continue;
            float quanto = (i + 0.5f) / quantos;
            // a corrente esticada TREME (presa no chão, puxando)
            Vector2 tremida = deLado * Mathf.Sin(relogio * 60f + i * 1.7f) * tremor * Mathf.Sin(quanto * Mathf.PI);
            elos[i].transform.position = de + d.normalized * (comprimento * quanto) + tremida;
            elos[i].transform.rotation = Quaternion.Euler(0f, 0f, angulo);
        }
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

        etapa = Mathf.Clamp(GerenciadorDoJogo.Instancia.EtapaDoChefe, 0, EtapasDaLuta - 1);
        vida = vidaMaxima - etapa * GolpesDaEtapa;
        vidaMostrada = vida;
        oni = etapa == EtapasDaLuta - 1;

        if (renasceu) yield return Pronto();
        else yield return Introducao();

        while (true)
        {
            // repete as rodadas da etapa até levar os 2 golpes dela
            int vidaNoFimDaEtapa = vidaMaxima - (etapa + 1) * GolpesDaEtapa;
            while (vida > vidaNoFimDaEtapa)
            {
                int golpe = vidaMaxima - etapa * GolpesDaEtapa - vida; // 0 = 1º golpe da etapa, 1 = 2º
                yield return Rodada(etapa * GolpesDaEtapa + golpe);
            }
            if (vida <= 0) break;

            etapa++;
            GerenciadorDoJogo.Instancia.EtapaDoChefe = etapa; // o "checkpoint" do chefe
            yield return NovaEtapa();
        }

        yield return SerDerrotada();
    }

    // Cada rodada é uma sequência FIXA de ataques que termina com o golpe de cima (a chance de dar dano).
    // O golpe de cima vem sempre logo depois de um SALTO: assim ela está perto de você quando a bola prende.
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

    // ETAPA 1: um ataque de cada vez (o Puck ensina).
    IEnumerator Etapa1Rodada1()
    {
        yield return Arremesso(AlturaBaixa, AlturaBaixa);
        yield return Giro(-1, andando: false);
        yield return Salto();
        yield return GolpeDeCima();
    }

    IEnumerator Etapa1Rodada2()
    {
        yield return Arremesso(AlturaAlta, AlturaAlta);
        yield return Salto();
        yield return Arremesso(AlturaBaixa, AlturaBaixa);
        yield return Salto();
        yield return GolpeDeCima();
    }

    // ETAPA 2: ela anda girando e a volta do arremesso troca de altura.
    IEnumerator Etapa2Rodada1()
    {
        yield return Giro(1, andando: true);
        yield return Arremesso(AlturaBaixa, AlturaAlta);
        yield return Salto();
        yield return GolpeDeCima();
    }

    IEnumerator Etapa2Rodada2()
    {
        yield return Salto();
        yield return Arremesso(AlturaAlta, AlturaBaixa);
        yield return Giro(-1, andando: false);
        yield return Salto();
        yield return GolpeDeCima();
    }

    // ETAPA 3 (ONI): mais rápido e em COMBOS (quase sem respiro entre um ataque e outro).
    IEnumerator Etapa3Rodada1()
    {
        yield return Salto();
        yield return Salto();
        yield return Arremesso(AlturaBaixa, AlturaAlta);
        yield return Giro(1, andando: true);
        yield return Salto();
        yield return GolpeDeCima();
    }

    IEnumerator Etapa3Rodada2()
    {
        yield return Giro(-1, andando: true);
        yield return Arremesso(AlturaAlta, AlturaBaixa);
        yield return Arremesso(AlturaBaixa, AlturaBaixa);
        yield return Salto();
        yield return Salto();
        yield return Salto();
        yield return GolpeDeCima();
    }

    // ------------------------------------------------------------------ começo, etapas e fim

    // Só na primeira vez: ela percebe você, gira o mangual mais rápido e o Puck explica.
    IEnumerator Introducao()
    {
        GerenciadorDoJogo.Som("armadilha");
        var exclamacao = Desenho("Exclamacao", "aviso", rem.position + Vector3.up * 2.4f, OrdemDosAvisos);
        StartCoroutine(Efeitos.Pulinho(exclamacao.transform, 0.3f, 0.2f));
        GerenciadorDoJogo.Instancia.Avisar("A REM! (ela não parece feliz)", 2f);
        yield return new WaitForSeconds(1f);
        Destroy(exclamacao.gameObject);
        GerenciadorDoJogo.Som("serra", 0.6f);
        CameraSeguir.Tremer(0.1f, 0.4f);
        GerenciadorDoJogo.Instancia.Avisar("Fuja da BOLA. Quando ela ficar PRESA no chão, pule na CABEÇA da Rem!", 2.8f);
        yield return new WaitForSeconds(1.6f);
    }

    // Depois de morrer: "PRONTO?" e a luta volta na etapa em que estava.
    IEnumerator Pronto()
    {
        GerenciadorDoJogo.Instancia.Avisar($"ETAPA {etapa + 1}/{EtapasDaLuta} - PRONTO?", 1.3f);
        GerenciadorDoJogo.Som("serra", 0.4f);
        yield return new WaitForSeconds(1.5f); // um respiro: o 1º ataque não pode pegar quem acabou de nascer
    }

    IEnumerator NovaEtapa()
    {
        timerPiscar = 0.8f;
        if (etapa < EtapasDaLuta - 1)
        {
            GerenciadorDoJogo.Instancia.Avisar("ETAPA 2/3 - ela ficou SÉRIA!", 2f);
            GerenciadorDoJogo.Som("armadilha");
            CameraSeguir.Tremer(0.15f, 0.6f);
            yield return new WaitForSeconds(1.3f);
            yield break;
        }

        // Etapa 3: vira ONI. O chifre brota, o olho fica vermelho e sai uma explosão de fumaça vermelha.
        GerenciadorDoJogo.Instancia.Avisar("ETAPA 3/3 - A REM VIROU ONI!!", 2.2f);
        GerenciadorDoJogo.Som("pancada");
        GerenciadorDoJogo.Som("risada", 0.5f);
        CameraSeguir.Tremer(0.3f, 1f);
        oni = true;
        for (int i = 0; i < 24; i++)
        {
            float angulo = i * Mathf.PI * 2f / 24f;
            Particula.Criar(transform, rem.position + Vector3.up * 0.9f, new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * 4f, 0.6f, 0.5f, CorDaAura);
        }
        yield return new WaitForSeconds(1.5f);
    }

    IEnumerator SerDerrotada()
    {
        estado = Estado.Derrotada;
        corpoPerigoso = false;
        zonas.Clear();
        zonaDaBola = null;
        ApagarEstrelas();
        Conquistas.Desbloquear("rem_vencida");
        // a partir daqui, se morrer (caiu num buraco...), renasce DEPOIS da arena
        GerenciadorDoJogo.Instancia.ChefeVencido(new Vector3(direita + 1.5f, chao + 0.5f, 0f));
        GerenciadorDoJogo.Instancia.Avisar("A REM DESMAIOU! (ela vai lembrar disso)", 3f);
        GerenciadorDoJogo.Som("pisao");
        CameraSeguir.Tremer(0.2f, 0.8f);
        timerPiscar = 1.2f;
        oni = false; // o chifre some: ela voltou ao normal
        pose = Pose.Atordoada;
        for (float t = 0f; t < 0.6f; t += Time.deltaTime) // tomba para trás devagarinho
        {
            rem.rotation = Quaternion.Euler(0f, 0f, 90f * Lado * (t / 0.6f));
            yield return null;
        }
        Desmaiada();
        AbrirArena();
        CameraSeguir.Destravar();
        emCombate = false;
    }

    // Deitada no chão, tonta, com a bola largada do lado. Não machuca ninguém.
    void Desmaiada()
    {
        pose = Pose.Atordoada;
        oni = false;
        Animar();
        rem.rotation = Quaternion.Euler(0f, 0f, 90f * Lado);
        rem.position = new Vector3(rem.position.x, chao + 0.4f, 0f);
        bolaNaCabeca = false;
        bola.position = new Vector3(rem.position.x + Lado * 1.6f, chao + 0.3f, 0f);
        foreach (SpriteRenderer elo in elos) elo.enabled = false;
    }

    // ------------------------------------------------------------------ os ataques

    // ARREMESSO: a bola voa reto na direção do Subaru (até a parede ou até o alcance) e volta.
    IEnumerator Arremesso(float alturaIda, float alturaVolta)
    {
        bool baixa = alturaIda < 1f, troca = alturaVolta != alturaIda;
        Dica(baixa ? "Linha BAIXA: PULE a bola (na ida E na volta)!" : "Linha ALTA: NÃO PULE!");
        if (troca) Dica("A volta vem na OUTRA altura! Olha a linha fraquinha!", ateAEtapa: 1);

        OlharParaOJogador();
        pose = Pose.Girando;
        float lado = Lado;
        float inicio = rem.position.x + lado * 0.6f;
        float parede = lado > 0f ? direita - 0.5f : esquerda + 0.5f;
        float fim = lado > 0f ? Mathf.Min(parede, inicio + AlcanceDoArremesso) : Mathf.Max(parede, inicio - AlcanceDoArremesso);

        // o aviso: a linha da ida pisca; se a volta for em outra altura, a linha dela aparece fraquinha
        var linhaIda = Linha(inicio, fim, chao + alturaIda);
        SpriteRenderer linhaVolta = troca ? Linha(inicio, fim, chao + alturaVolta) : null;
        var exclamacao = Desenho("Exclamacao", "aviso", new Vector3(fim, chao + alturaIda + 0.9f, 0f), OrdemDosAvisos + 1);
        GerenciadorDoJogo.Som("armadilha", 0.6f);
        for (float t = 0f; t < Aviso; t += Time.deltaTime)
        {
            bool aceso = Piscando(t, Aviso);
            linhaIda.color = aceso ? Vermelho : VermelhoFraco;
            if (linhaVolta != null) linhaVolta.color = VermelhoBemFraco;
            exclamacao.enabled = aceso;
            yield return null;
        }
        Destroy(linhaIda.gameObject);
        Destroy(exclamacao.gameObject);

        // IDA
        pose = Pose.Parada; // braço esticado para a frente
        GerenciadorDoJogo.Som("serra", 0.6f);
        yield return MoverBola(new Vector2(inicio, chao + alturaIda), new Vector2(fim, chao + alturaIda), VelocidadeDoArremesso[etapa], true);
        if (Mathf.Abs(fim - parede) < 0.01f)
        {
            GerenciadorDoJogo.Som("pancada", 0.6f);
            CameraSeguir.Tremer(0.08f, 0.15f);
            Efeitos.Poeira(transform, bola.position, 5, 2.5f);
        }

        // NA PONTA: quica e, se a volta for em outra altura, a linha dela pisca de novo antes de a bola voltar
        if (troca)
        {
            linhaVolta.color = Vermelho;
            Vector2 de = bola.position, ate = new Vector2(fim, chao + alturaVolta);
            for (float t = 0f; t < Aviso; t += Time.deltaTime)
            {
                linhaVolta.color = Piscando(t, Aviso) ? Vermelho : VermelhoFraco;
                PosicionarBola(Vector2.Lerp(de, ate, Mathf.Clamp01(t / 0.15f)), true);
                yield return null;
            }
            Destroy(linhaVolta.gameObject);
        }
        else yield return new WaitForSeconds(PausaNaPonta);

        // VOLTA (puxada pela corrente)
        GerenciadorDoJogo.Som("serra", 0.5f);
        yield return MoverBola(new Vector2(fim, chao + alturaVolta), new Vector2(inicio, chao + alturaVolta), VelocidadeDoArremesso[etapa], true);
        GuardarBola();
        yield return Pausa();
    }

    // GIRO: o anel vermelho mostra por onde a bola vai passar. Seguro: LONGE dela ou COLADO nela (sem pular).
    IEnumerator Giro(int sentido, bool andando)
    {
        Dica(andando ? "Ela vem ANDANDO e girando! Fuja ou ande COLADO nela!" : "Giro! Fique LONGE ou COLADO nela (sem pular)!");
        OlharParaOJogador();
        pose = Pose.Girando;
        // andando: ela vai para o lado em que você está, até DistanciaDaParede da parede
        float destino = rem.position.x;
        if (andando)
            destino = Mathf.Clamp(rem.position.x + Lado * 99f, esquerda + DistanciaDaParede, direita - DistanciaDaParede);
        float sentidoDoPasso = Mathf.Sign(destino - rem.position.x);

        // aviso: o anel pisca em volta dela (e um "!" do lado para onde ela vai andar)
        var anel = Desenho("Anel", "rem_anel", CentroDoGiro, OrdemDosAvisos);
        anel.transform.localScale = Vector3.one * ArteDaRem.EscalaDoAnel;
        SpriteRenderer seta = null;
        if (Mathf.Abs(destino - rem.position.x) > 0.1f)
            seta = Desenho("Exclamacao", "aviso", new Vector3(rem.position.x + sentidoDoPasso * 1.2f, chao + 2.6f, 0f), OrdemDosAvisos + 1);
        GerenciadorDoJogo.Som("serra", 0.5f);
        for (float t = 0f; t < Aviso; t += Time.deltaTime)
        {
            bool aceso = Piscando(t, Aviso);
            anel.color = aceso ? Vermelho : VermelhoFraco;
            if (seta != null) seta.enabled = aceso;
            yield return null;
        }
        if (seta != null) Destroy(seta.gameObject);

        // a corrente estica de uma vez (sem machucar: ainda não chegou no anel)...
        bolaNaCabeca = false;
        float angulo = 90f, velocidade = VelocidadeDoGiro[etapa];
        for (float t = 0f; t < 0.15f; t += Time.deltaTime)
        {
            angulo += sentido * velocidade * Time.deltaTime;
            PosicionarBola(CentroDoGiro + Direcao(angulo) * Mathf.Lerp(0.6f, ArteDaRem.RaioDoAnel, t / 0.15f), false);
            yield return null;
        }

        // ...e gira: 2 voltas parada, ou andando até perto da parede do seu lado
        float duracao = andando ? DuracaoDoGiroAndando : 720f / velocidade;
        float proximoSom = 0f;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            if (andando)
                rem.position = new Vector3(Mathf.MoveTowards(rem.position.x, destino, VelocidadeAndando * Time.deltaTime), chao, 0f);
            angulo += sentido * velocidade * Time.deltaTime;
            PosicionarBola(CentroDoGiro + Direcao(angulo) * ArteDaRem.RaioDoAnel, true);
            anel.transform.position = CentroDoGiro;
            anel.color = VermelhoFraco;
            proximoSom -= Time.deltaTime;
            if (proximoSom <= 0f)
            {
                proximoSom = 360f / velocidade;
                GerenciadorDoJogo.Som("serra", 0.35f);
            }
            yield return null;
        }

        // recolhe a corrente (sem machucar)
        Vector2 de = bola.position;
        for (float t = 0f; t < 0.15f; t += Time.deltaTime)
        {
            PosicionarBola(Vector2.Lerp(de, CentroDoGiro + Vector2.up * 1.5f, t / 0.15f), false);
            yield return null;
        }
        Destroy(anel.gameObject);
        GuardarBola();
        yield return Pausa();
    }

    // SALTO: a marca segue você, trava (vermelha) e ela cai ali. O corpo dela só machuca na queda.
    IEnumerator Salto()
    {
        Dica("Ela vai PULAR em você! Saia da marca vermelha!");
        float x = AlvoDoSalto();
        var marca = Desenho("MarcaDoSalto", "baleia_alvo", new Vector3(x, chao + 0.15f, 0f), OrdemDosAvisos);
        marca.transform.localScale = new Vector3((MeiaLarguraDoCorpo + MeiaLarguraDoJogador) / 1f, 1f, 1f); // 2 de largura * 0.72 = a área que mata
        pose = Pose.Parada;
        float seguir = TempoSeguindo[etapa];
        for (float t = 0f; t < seguir + Aviso; t += Time.deltaTime)
        {
            bool travou = t >= seguir;
            if (!travou) x = Mathf.MoveTowards(x, AlvoDoSalto(), 9f * Time.deltaTime);
            marca.transform.position = new Vector3(x, chao + 0.15f, 0f);
            marca.color = !travou ? VermelhoFraco : Piscando(t - seguir, Aviso) ? Vermelho : VermelhoFraco;
            Olhar(x);
            rem.localScale = new Vector3(1f, travou ? 0.88f : 1f, 1f); // agachada: vai pular!
            yield return null;
        }
        rem.localScale = Vector3.one;

        // o pulo: uma parábola de onde ela está até a marca
        pose = Pose.Golpe;
        GerenciadorDoJogo.Som("pulo");
        float de = rem.position.x;
        for (float t = 0f; t < DuracaoDoSalto; t += Time.deltaTime)
        {
            float p = t / DuracaoDoSalto;
            rem.position = new Vector3(Mathf.Lerp(de, x, p), chao + 4f * AlturaDoSalto * p * (1f - p), 0f);
            corpoPerigoso = p > 0.5f; // só na descida
            yield return null;
        }
        rem.position = new Vector3(x, chao, 0f);
        GerenciadorDoJogo.Som("pancada", 0.8f);
        CameraSeguir.Tremer(0.12f, 0.2f);
        Efeitos.Poeira(transform, new Vector3(x - 0.5f, chao + 0.1f, 0f), 5, 2.5f);
        Efeitos.Poeira(transform, new Vector3(x + 0.5f, chao + 0.1f, 0f), 5, 2.5f);
        Destroy(marca.gameObject);
        yield return new WaitForSeconds(0.08f);
        corpoPerigoso = false;
        pose = Pose.Girando;
        OlharParaOJogador();
        yield return Pausa();
    }

    // GOLPE DE CIMA: a bola sobe, a marca segue você, trava e a bola DESPENCA ali (soltando pedras).
    // Depois a bola fica PRESA no chão: a chance de pular na cabeça dela.
    IEnumerator GolpeDeCima()
    {
        Dica("Golpe de cima! Saia da marca e pule as PEDRAS!");
        pose = Pose.Golpe;
        bolaNaCabeca = false;
        Vector2 alto = new Vector2(rem.position.x, chao + 2.6f);
        Vector2 de = bola.position;
        float x = AlvoDoGolpe();
        var marca = Desenho("MarcaDoGolpe", "baleia_alvo", new Vector3(x, chao + 0.15f, 0f), OrdemDosAvisos);
        marca.transform.localScale = new Vector3((RaioDoImpacto + MeiaLarguraDoJogador) * 2f / 2f, 1.2f, 1f);
        float seguir = TempoSeguindo[etapa];
        GerenciadorDoJogo.Som("serra", 0.5f);
        for (float t = 0f; t < seguir + Aviso; t += Time.deltaTime)
        {
            bool travou = t >= seguir;
            if (!travou) x = Mathf.MoveTowards(x, AlvoDoGolpe(), 9f * Time.deltaTime);
            marca.transform.position = new Vector3(x, chao + 0.15f, 0f);
            marca.color = !travou ? VermelhoFraco : Piscando(t - seguir, Aviso) ? Vermelho : VermelhoFraco;
            Olhar(x);
            PosicionarBola(Vector2.Lerp(de, alto, Mathf.Clamp01(t / 0.2f)), false);
            yield return null;
        }

        // a bola sobe lá no alto, em cima da marca (alto demais para machucar)...
        de = bola.position;
        for (float t = 0f; t < 0.18f; t += Time.deltaTime)
        {
            PosicionarBola(Vector2.Lerp(de, new Vector2(x, chao + AlturaDoTombo), t / 0.18f), false);
            yield return null;
        }
        // ...e despenca reto na marca
        float y = chao + AlturaDoTombo, queda = 10f, afundada = chao + 0.1f;
        while (y > afundada)
        {
            queda += 120f * Time.deltaTime;
            y = Mathf.Max(afundada, y - queda * Time.deltaTime);
            PosicionarBola(new Vector2(x, y), true, RaioDoImpacto);
            yield return null;
        }
        Destroy(marca.gameObject);
        GerenciadorDoJogo.Som("pancada");
        CameraSeguir.Tremer(0.25f, 0.3f);
        Efeitos.Poeira(transform, new Vector3(x, chao + 0.2f, 0f), 10, 3.5f);
        yield return new WaitForSeconds(0.06f);
        PosicionarBola(bola.position, false); // presa no chão, a bola não machuca

        // as pedras saem do buraco um instante depois e rolam para os dois lados (pule!)
        Paralelo(Pedra(x, -1f));
        Paralelo(Pedra(x, 1f));

        yield return Presa();
    }

    // A bola está PRESA no chão e a Rem puxa a corrente com toda a força. Pule na cabeça dela!
    IEnumerator Presa()
    {
        estado = Estado.Presa;
        golpeada = false;
        pose = Pose.Presa;
        Olhar(bola.position.x);
        Dica("AGORA! Pule na CABEÇA dela!");
        Vector2 afundada = bola.position;
        float tempo = TempoPresa[etapa];
        SpriteRenderer exclamacao = null;
        for (float t = 0f; t < tempo && !golpeada; t += Time.deltaTime)
        {
            bool quaseSoltando = t > tempo - 0.5f; // meio segundo antes de soltar: treme mais e aparece o "!"
            tremor = quaseSoltando ? 0.12f : 0.05f;
            bola.position = afundada + Random.insideUnitCircle * (quaseSoltando ? 0.08f : 0.03f);
            if (quaseSoltando && exclamacao == null)
            {
                exclamacao = Desenho("Exclamacao", "aviso", afundada + Vector2.up * 1.3f, OrdemDosAvisos);
                GerenciadorDoJogo.Som("armadilha", 0.5f);
            }
            if (Random.value < 0.15f) Efeitos.Poeira(transform, afundada + Vector2.up * 0.2f, 1, 1.5f);
            yield return null;
        }
        tremor = 0f;
        if (exclamacao != null) Destroy(exclamacao.gameObject);
        if (estado == Estado.Presa) estado = Estado.Lutando;

        if (golpeada)
        {
            // tonta por um instante, com estrelinhas
            pose = Pose.Atordoada;
            CriarEstrelas();
            yield return new WaitForSeconds(0.8f);
            ApagarEstrelas();
            if (vida <= 0) yield break; // levou o último golpe: a cena da derrota cuida do resto
        }

        // arranca a bola do chão e puxa de volta (sem machucar)
        pose = Pose.Presa;
        GerenciadorDoJogo.Som("pancada", 0.5f);
        Efeitos.Poeira(transform, afundada + Vector2.up * 0.2f, 6, 3f);
        Vector2 de = bola.position;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            float p = t / 0.3f;
            Vector2 ate = new Vector2(rem.position.x, chao + 2.5f);
            PosicionarBola(Vector2.Lerp(de, ate, p) + Vector2.up * Mathf.Sin(p * Mathf.PI) * 1.5f, false);
            yield return null;
        }
        GuardarBola();
        yield return Pausa();
    }

    // Uma pedra rolando no chão, da marca do golpe até a parede (a Rem não liga se ela passar por ela).
    IEnumerator Pedra(float x, float sentido)
    {
        // primeiro ela "brota" do buraco (sem machucar): é o seu tempo de reagir.
        // Brotando, fica ATRÁS dos blocos (parece sair de dentro do chão); rolando, fica na frente.
        var pedra = Desenho("Pedra", "pedra", new Vector3(x, chao - 0.5f, 0f), OrdemDaBola);
        pedra.transform.localScale = Vector3.one * 1.3f;
        for (float t = 0f; t < AtrasoDasPedras; t += Time.deltaTime)
        {
            pedra.transform.position = new Vector3(x, chao - 0.5f + 0.5f * (t / AtrasoDasPedras), 0f);
            yield return null;
        }
        pedra.sortingOrder = OrdemDaRem + 1;
        Zona zona = NovaZona(RaioDaPedra);
        float velocidade = VelocidadeDaPedra[etapa];
        float fim = sentido < 0f ? esquerda + 0.3f : direita - 0.3f;
        while ((fim - x) * sentido > 0f)
        {
            x += sentido * velocidade * Time.deltaTime;
            pedra.transform.position = new Vector3(x, chao, 0f);
            pedra.transform.rotation = Quaternion.Euler(0f, 0f, -sentido * x * 120f); // rolando
            zona.a = new Vector2(x - 0.15f, chao + RaioDaPedra);
            zona.b = new Vector2(x + 0.15f, chao + RaioDaPedra);
            yield return null;
        }
        zonas.Remove(zona);
        Efeitos.Poeira(transform, new Vector3(x, chao + 0.2f, 0f), 3, 2f);
        Destroy(pedra.gameObject);
    }

    // Pulou na cabeça dela, caindo, por cima?
    void TestarPisao()
    {
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (golpeada || jogador == null || jogador.Morto || jogador.Corpo.linearVelocity.y > 0.5f) return;
        Vector3 pes = jogador.transform.position + Vector3.down * 0.5f;
        float cabeca = rem.position.y + TopoDaCabeca;
        if (Mathf.Abs(pes.x - rem.position.x) < 0.75f && pes.y > cabeca - 0.45f && pes.y < cabeca + 0.8f)
            LevarGolpe(jogador);
    }

    void LevarGolpe(Jogador jogador)
    {
        vida--;
        golpeada = true;
        timerPiscar = 0.8f;
        jogador.Quicar(14f, true);
        GerenciadorDoJogo.Som("pisao");
        GerenciadorDoJogo.Som("pancada", 0.6f);
        CameraSeguir.Tremer(0.2f, 0.3f);
        Efeitos.Poeira(transform, rem.position + Vector3.up * TopoDaCabeca, 8, 3f);
        string[] falas = { "ACERTOU! A tiara entortou!", "Ela tá tonta! Mais um!", "SÓ MAIS UM! VAI, SUBARU!" };
        if (vida > 0 && vida % GolpesDaEtapa != 0) GerenciadorDoJogo.Instancia.Avisar(falas[etapa], 1.6f);
    }

    // ------------------------------------------------------------------ alvos

    // Onde ela quer cair no salto: em cima do Subaru (sem sair da arena).
    float AlvoDoSalto()
    {
        if (!GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) return rem.position.x;
        return Mathf.Clamp(jogador.x, esquerda + 0.8f, direita - 0.8f);
    }

    // Onde a bola do golpe de cima cai: em cima do Subaru, mas nunca colado nela (no mínimo 1,6 blocos)
    // e no máximo a 6 blocos (a corrente não alcança mais longe).
    float AlvoDoGolpe()
    {
        float x = rem.position.x;
        float lado = Lado;
        if (GerenciadorDoJogo.JogadorVivo(out Vector2 jogador) && Mathf.Abs(jogador.x - x) > 0.05f)
            lado = Mathf.Sign(jogador.x - x);
        float distancia = GerenciadorDoJogo.JogadorVivo(out jogador) ? Mathf.Clamp(Mathf.Abs(jogador.x - x), 1.6f, 6f) : 2f;
        float alvo = Mathf.Clamp(x + lado * distancia, esquerda + 0.6f, direita - 0.6f);
        if (Mathf.Abs(alvo - x) < 1.6f) alvo = x - lado * 1.6f; // sem espaço do lado da parede: cai do outro lado
        return alvo;
    }

    // ------------------------------------------------------------------ bola e movimento

    // Leva a bola de a até b, em linha reta, com velocidade constante.
    IEnumerator MoverBola(Vector2 a, Vector2 b, float velocidade, bool machuca)
    {
        bolaNaCabeca = false;
        Vector2 lugar = a;
        PosicionarBola(lugar, machuca);
        while (lugar != b)
        {
            lugar = Vector2.MoveTowards(lugar, b, velocidade * Time.deltaTime);
            PosicionarBola(lugar, machuca);
            yield return null;
        }
    }

    // Põe a bola num lugar. machuca = a bola mata quem encostar (com o raio dado).
    void PosicionarBola(Vector2 lugar, bool machuca, float raio = MangualDaRem.RaioDaBola)
    {
        bola.position = lugar;
        if (machuca)
        {
            if (zonaDaBola == null) zonaDaBola = NovaZona(raio);
            zonaDaBola.raio = raio;
            zonaDaBola.a = zonaDaBola.b = lugar;
        }
        else if (zonaDaBola != null)
        {
            zonas.Remove(zonaDaBola);
            zonaDaBola = null;
        }
    }

    // A bola volta a girar em cima da cabeça (só enfeite).
    void GuardarBola()
    {
        PosicionarBola(bola.position, false);
        bolaNaCabeca = true;
        pose = Pose.Girando;
    }

    IEnumerator Pausa()
    {
        yield return new WaitForSeconds(Respiro[etapa]);
    }

    void Olhar(float x)
    {
        if (Mathf.Abs(x - rem.position.x) > 0.2f) desenho.flipX = x < rem.position.x; // o desenho olha para a direita
    }

    void OlharParaOJogador()
    {
        if (GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) Olhar(jogador.x);
    }

    static Vector2 Direcao(float graus) => new Vector2(Mathf.Cos(graus * Mathf.Deg2Rad), Mathf.Sin(graus * Mathf.Deg2Rad));

    SpriteRenderer Desenho(string nome, string sprite, Vector3 lugar, int ordem) =>
        ConstrutorDeFase.Visual(nome, transform, lugar, sprite, ordem, false).GetComponent<SpriteRenderer>();

    // Uma linha de aviso deitada, de x1 até x2, na altura y.
    SpriteRenderer Linha(float x1, float x2, float y)
    {
        var linha = Desenho("AvisoDoArremesso", "baleia_linha", Vector3.zero, OrdemDosAvisos);
        Esticar(linha.transform, new Vector2(x1, y), new Vector2(x2, y), 1.4f);
        return linha;
    }

    // Estica um desenho de "1 pixel de comprimento" de a até b.
    static void Esticar(Transform objeto, Vector2 a, Vector2 b, float grossura)
    {
        Vector2 d = b - a;
        objeto.position = (a + b) / 2f;
        objeto.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        objeto.localScale = new Vector3(d.magnitude * FabricaDeSprites.PixelsPorUnidade, grossura, 1f);
    }

    // Avisos piscam, e piscam mais rápido no finalzinho ("vai AGORA").
    static bool Piscando(float t, float total) => Mathf.Repeat(t * (total - t < 0.2f ? 22f : 9f), 1f) < 0.6f;

    // Dicas do Puck: só na etapa 1 (ou até "ateAEtapa"), e cada uma só uma vez por tentativa.
    void Dica(string texto, int ateAEtapa = 0)
    {
        if (etapa > ateAEtapa || !dicasMostradas.Add(texto)) return;
        GerenciadorDoJogo.Instancia.Avisar(texto, 1.3f);
    }

    // ------------------------------------------------------------------ zonas que matam

    Zona NovaZona(float raio)
    {
        var zona = new Zona { raio = raio };
        zona.a = zona.b = new Vector2(-9999f, -9999f); // longe de tudo até o ataque dizer onde está
        zonas.Add(zona);
        return zona;
    }

    void TestarPerigos()
    {
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (jogador == null || jogador.Morto || estado == Estado.Derrotada) return;
        Vector2 p = (Vector2)jogador.transform.position + new Vector2(0f, -0.02f); // centro da caixa dele

        bool encostou = corpoPerigoso
            && Mathf.Abs(p.x - rem.position.x) < MeiaLarguraDoCorpo + MeiaLarguraDoJogador
            && p.y > rem.position.y - MeiaAlturaDoJogador && p.y < rem.position.y + AlturaDoCorpo + MeiaAlturaDoJogador;
        // o Subaru vira uma "salsicha" em pé: 3 pontos no meio dele, com a meia largura de raio
        for (int i = 0; i < zonas.Count && !encostou; i++)
            for (int k = -1; k <= 1 && !encostou; k++)
            {
                Vector2 ponto = p + new Vector2(0f, k * (MeiaAlturaDoJogador - MeiaLarguraDoJogador));
                if (DistanciaAoSegmento(ponto, zonas[i].a, zonas[i].b) < zonas[i].raio + MeiaLarguraDoJogador) encostou = true;
            }
        if (!encostou) return;
        jogador.Morrer();
        if (jogador.Morto && oni) Conquistas.Desbloquear("rem_brava"); // pego pela Rem ONI
    }

    static float DistanciaAoSegmento(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float comprimento2 = Vector2.Dot(ab, ab);
        float t = comprimento2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / comprimento2) : 0f;
        return Vector2.Distance(p, a + ab * t);
    }

    // ------------------------------------------------------------------ ataques ao mesmo tempo

    // Começa um ataque "solto" (sem esperar ele acabar) e conta quantos ainda estão rodando.
    void Paralelo(IEnumerator ataque) => StartCoroutine(Contar(ataque));

    IEnumerator Contar(IEnumerator ataque)
    {
        emParalelo++;
        yield return StartCoroutine(ataque);
        emParalelo--;
    }

    // ------------------------------------------------------------------ arena e efeitos

    // Paredes de tijolo nas duas pontas da arena (colisor de verdade + tijolos desenhados).
    void FecharArena()
    {
        foreach (float x in new[] { esquerda - 0.5f, direita + 0.5f })
        {
            var parede = new GameObject("ParedeDaArena");
            parede.transform.SetParent(transform, false);
            parede.transform.position = new Vector3(x, chao + 7f, 0f);
            parede.AddComponent<BoxCollider2D>().size = new Vector2(1f, 20f);
            for (int linha = 0; linha < 13; linha++)
                ConstrutorDeFase.Visual("Tijolo", parede.transform, new Vector3(x, chao + 0.5f + linha, 0f), "tijolo", 12, false);
            Efeitos.Poeira(transform, new Vector3(x, chao + 0.3f, 0f), 6, 3f);
            paredes.Add(parede);
        }
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
            estrelas.Add(Desenho("Estrela", "brilho", rem.position, OrdemDasEstrelas).transform);
    }

    void GirarEstrelas()
    {
        Vector2 cabeca = (Vector2)rem.position + Vector2.up * (TopoDaCabeca + 0.2f);
        for (int i = 0; i < estrelas.Count; i++)
        {
            float angulo = relogio * 5f + i * 2.1f;
            estrelas[i].position = cabeca + new Vector2(Mathf.Cos(angulo) * 0.6f, Mathf.Sin(angulo) * 0.15f);
        }
    }

    void ApagarEstrelas()
    {
        foreach (Transform estrela in estrelas) if (estrela != null) Destroy(estrela.gameObject);
        estrelas.Clear();
    }
}
