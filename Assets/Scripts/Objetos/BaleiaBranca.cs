using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 'w' - a BALEIA BRANCA, o chefe final (a última fase é só dela). Ela é uma ORCA gigante com um
// CHIFRE na testa (os desenhos ficam em ArteDaBaleia.cs).
//
// O 'w' marca o começo da arena (na linha em que o jogador anda); a arena tem larguraDaArena blocos
// a partir dele. Entrou, paredes de névoa fecham a arena, a câmera trava e a luta começa.
//
// A luta é inspirada no chefe final do HELLTAKER:
//  - tudo mata com UM toque, mas todo ataque AVISA antes (faixa vermelha, linha piscando, "!",
//    marca no chão, chifre brilhando). Quanto mais avançada a etapa, mais curto o aviso;
//  - os padrões são FIXOS (as posições não são sorteadas): dá para decorar, como no Helltaker;
//  - são 3 ETAPAS de 2 golpes cada. Morreu? Volta na hora, no começo da arena, JÁ na etapa em que
//    estava (o GerenciadorDoJogo guarda a etapa em EtapaDoChefe). A introdução longa é só na 1ª vez.
//
//  ETAPA 1 - um ataque de cada vez (para aprender; o Puck dá dicas):
//     INVESTIDA ALTA (não pule), BARBATANA rasgando o chão (pule), CHUVA DE NÉVOA (fique entre as marcas),
//     CORRENTES (fique nos vãos), ONDA DE CHOQUE (pule), RAIO DO CHIFRE (não fique parado).
//  ETAPA 2 - os mesmos ataques, mais rápidos e DOIS AO MESMO TEMPO.
//  ETAPA 3 - o chão desmorona em dois lugares, tudo fica ainda mais rápido, ela dá uma FINTA no mergulho
//     e, no último golpe, a névoa ESCURECE a arena (só aparecem os avisos, os ataques e o olho dela).
//  No fim de cada rodada ela dá um MERGULHO: a marca no chão segue você, trava (fica vermelha) e ela
//  despenca ali. Fugiu? Ela fica ATORDOADA no chão: PULE NA CABEÇA DELA. É o único jeito de dar dano.
//
// Os perigos daqui não usam colisores: cada ataque cria uma "Zona" (um segmento com grossura, tipo uma
// salsicha) e a própria Baleia testa, a cada quadro, se o Subaru encostou em alguma. Assim um raio
// inclinado, uma bola e uma onda usam a MESMA regra. O corpo dela usa retângulos (CaixasDoCorpo).
public class BaleiaBranca : MonoBehaviour
{
    public const int Etapas = 3;
    public const int GolpesPorEtapa = 2;

    public int vidaMaxima = Etapas * GolpesPorEtapa;
    public float larguraDaArena = 24f;
    public float tamanho = 1f; // escala do desenho (96 x 40 pixels = 6 x 2,5 blocos)

    // Colunas da arena (contadas a partir do 'w') em que o MAPA tem um buraco no chão.
    // A Baleia tampa esses buracos com blocos frágeis, que desmoronam no começo da etapa 3.
    public static readonly int[] ColunasFrageis = { 7, 8, 15, 16 };

    // ------------------------------------------------------------------ ajustes de cada etapa (1, 2, 3)

    static readonly float[] TempoDeAviso = { 0.6f, 0.48f, 0.38f };
    static readonly float[] TempoAtordoada = { 2.6f, 2.2f, 1.9f };
    static readonly float[] VelocidadeDaInvestida = { 17f, 20f, 23f };
    static readonly float[] VelocidadeDaOnda = { 9f, 10.5f, 12f };
    static readonly float[] TempoSeguindo = { 1.3f, 1.1f, 0.9f }; // mergulho: quanto tempo a marca segue você
    const float AvisoMinimoRente = 0.55f; // corrente rente ao chão: precisa de tempo para acertar o pulo
    const float TravaDoMergulho = 0.55f;  // a marca do mergulho fica parada (vermelha) antes de ela cair
    const float TravaMinimaDoChifre = 0.45f; // a mira do chifre fica vermelha pelo menos isso antes do tiro

    // Padrões FIXOS, em blocos a partir da borda esquerda da arena.
    // A chuva alterna entre A e B: onde é seguro numa é exatamente onde cai a outra (dê um passo para o lado).
    static readonly float[] ChuvaA = { 1f, 5f, 9f, 13f, 17f, 21f };
    static readonly float[] ChuvaB = { 3f, 7f, 11f, 15f, 19f, 23f };
    static readonly float[] CorrentesA = { 2f, 7f, 12f, 17f, 22f };
    static readonly float[] CorrentesB = { 4.5f, 9.5f, 14.5f, 19.5f };
    static readonly float[] Nenhuma = { };
    static readonly float[] Alta = { 2f };     // corrente deitada na altura do pulo: NÃO pule
    static readonly float[] Rente = { 0.3f };  // corrente deitada rente ao chão: PULE

    // Ordem de desenho: os avisos e os ataques ficam POR CIMA da escuridão da etapa 3.
    const int OrdemSubmersa = -3, OrdemDaBaleia = 8, OrdemDaEscuridao = 50, OrdemDaNevoa = 52,
              OrdemDosAvisos = 55, OrdemDosAtaques = 56, OrdemDoOlho = 58;

    // A "caixa" do Subaru que conta para morrer (um tiquinho menor que o desenho: é mais justo).
    const float MeiaLarguraDoJogador = 0.27f, MeiaAlturaDoJogador = 0.42f;

    // Partes do corpo que matam, em PIXELS do desenho (olhando para a esquerda): x, y, largura, altura.
    static readonly Rect[] CaixasDoCorpo =
    {
        new Rect(5, 10, 55, 17),  // cabeça e corpo
        new Rect(7, 28, 8, 4),    // base do chifre
        new Rect(3, 32, 6, 6),    // ponta do chifre
        new Rect(44, 28, 11, 8),  // nadadeira dorsal
        new Rect(29, 1, 7, 8),    // nadadeira peitoral
        new Rect(60, 15, 20, 8),  // rabo
        new Rect(80, 13, 12, 13), // pontas do rabo
    };

    static readonly Color Vermelho = new Color(1f, 0.2f, 0.3f, 1f);
    static readonly Color VermelhoFraco = new Color(1f, 0.2f, 0.3f, 0.35f);
    static readonly Color Lilas = new Color(0.8f, 0.55f, 1f, 0.55f);
    static readonly Color Amarelo = new Color(1f, 0.85f, 0.3f, 1f);

    // ------------------------------------------------------------------ estado

    public int Vida { get; private set; }
    public float VidaMostrada { get; private set; } // a barra de vida "escorre" até a vida de verdade
    public bool EmCombate { get; private set; }
    public int EtapaAtual => etapa + 1;             // 1, 2 ou 3 (para a barra de vida)

    enum Estado { Esperando, Lutando, Atordoada, Derrotada }
    Estado estado;
    int etapa;
    bool parado;          // o jogador morreu: tudo congela até a fase recomeçar
    bool livre;           // ela está "à toa", flutuando no alto (o Update cuida de mexer nela)
    bool corpoPerigoso;   // encostar no corpo dela mata?
    bool submersa;        // nadando DENTRO do chão (só a barbatana e o chifre aparecem)
    bool golpeada;        // levou um pisão nesta tontura
    float chao, esquerda, direita, meio;
    float relogio, timerPiscar;
    int emParalelo;       // quantos ataques "soltos" (Paralelo) ainda estão rodando
    Vector2 descanso;     // onde ela flutua entre um ataque e outro
    Sprite poseFixa;      // boca aberta, chifre aceso, tonta... (null = nadando, o rabo batendo)

    Transform baleia;
    SpriteRenderer desenho, olho, brilhoDoChifre, escuridao;
    Sprite[] quadros, quadrosDaBola, quadrosDaOnda, quadrosDoRaio;
    Sprite spriteBoca, spriteCarga, spriteTonta;

    // Uma área que mata: tudo a menos de "raio" do segmento a-b (uma bola é um segmento de tamanho zero).
    class Zona { public Vector2 a, b; public float raio; }
    readonly List<Zona> zonas = new List<Zona>();

    readonly List<GameObject> paredes = new List<GameObject>();
    readonly List<Transform> estrelas = new List<Transform>();
    readonly List<GameObject> blocosFrageis = new List<GameObject>();
    readonly HashSet<string> dicasMostradas = new HashSet<string>();

    float Aviso => TempoDeAviso[etapa];
    Vector3 PontoDeRenascer => new Vector3(esquerda + 0.5f, chao + 0.5f, 0f); // a casa do 'w'

    // ------------------------------------------------------------------ Unity

    void Awake()
    {
        chao = transform.position.y - 0.5f;
        esquerda = transform.position.x - 0.5f;   // borda esquerda da casa do 'w'
        direita = esquerda + larguraDaArena;
        meio = (esquerda + direita) / 2f;
        descanso = new Vector2(meio, chao + 9f);
        Vida = vidaMaxima;
        VidaMostrada = Vida;

        quadros = new Sprite[ArteDaBaleia.QuadrosDoRabo];
        for (int i = 0; i < quadros.Length; i++) quadros[i] = FabricaDeSprites.Pegar("baleia_" + i);
        quadrosDaBola = new[] { FabricaDeSprites.Pegar("baleia_bola_0"), FabricaDeSprites.Pegar("baleia_bola_1") };
        quadrosDaOnda = new[] { FabricaDeSprites.Pegar("baleia_onda_0"), FabricaDeSprites.Pegar("baleia_onda_1") };
        quadrosDoRaio = new[] { FabricaDeSprites.Pegar("baleia_raio_0"), FabricaDeSprites.Pegar("baleia_raio_1"), FabricaDeSprites.Pegar("baleia_raio_2") };
        spriteBoca = FabricaDeSprites.Pegar("baleia_boca");
        spriteCarga = FabricaDeSprites.Pegar("baleia_carga");
        spriteTonta = FabricaDeSprites.Pegar("baleia_tonta");

        desenho = Desenho("Baleia", "baleia", transform.position, OrdemDaBaleia);
        baleia = desenho.transform;
        desenho.enabled = false;
        Luzes.Ponto(baleia, new Color(0.85f, 0.85f, 1f), 5f, 0.5f); // ela "brilha" um pouco no escuro

        // o olho vermelho e o brilho do chifre ficam por cima de tudo (até da escuridão)
        olho = Desenho("Olho", "baleia_olho", transform.position, OrdemDoOlho);
        olho.transform.SetParent(baleia, true);
        olho.enabled = false;
        Luzes.Ponto(olho.transform, new Color(1f, 0.25f, 0.3f), 2.5f, 0.9f);
        brilhoDoChifre = Desenho("BrilhoDoChifre", "baleia_brilho_chifre", transform.position, OrdemDoOlho);
        brilhoDoChifre.transform.SetParent(baleia, true);
        brilhoDoChifre.enabled = false;

        TamparOsBuracos();
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
        VidaMostrada = Mathf.MoveTowards(VidaMostrada, Vida, 1.5f * Time.deltaTime);
        relogio += Time.deltaTime; // Time.deltaTime é 0 na pausa: tudo congela sozinho

        if (estado == Estado.Esperando)
        {
            if (GerenciadorDoJogo.JogadorVivo(out Vector2 entrando) && entrando.x > esquerda + 2f)
                StartCoroutine(Lutar(renasceu: false));
            return;
        }

        // O jogador morreu: para TUDO na hora (a fase inteira vai ser recriada quando ele renascer).
        if (EmCombate && !parado && !GerenciadorDoJogo.Pausado && !GerenciadorDoJogo.JogadorVivo(out _))
        {
            parado = true;
            StopAllCoroutines();
            zonas.Clear();
            corpoPerigoso = false;
        }
        if (parado) return;

        Flutuar();
        Animar();
        if (estado == Estado.Atordoada)
        {
            GirarEstrelas();
            TestarPisao();
        }
        TestarPerigos();
        if (escuridao != null && GerenciadorDoJogo.JogadorVivo(out Vector2 jogador))
            escuridao.transform.position = jogador;
    }

    // Quando está "à toa", ela flutua lá no alto, balançando, sempre olhando para o Subaru.
    void Flutuar()
    {
        if (!livre || !desenho.enabled) return;
        Vector2 alvo = descanso + new Vector2(Mathf.Sin(relogio * 0.8f) * 1.5f, Mathf.Sin(relogio * 2.2f) * 0.3f);
        baleia.position = Vector2.MoveTowards(baleia.position, alvo, 9f * Time.deltaTime);
        if (GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) Olhar(jogador.x);
    }

    // Escolhe o desenho (rabo batendo ou uma pose), pisca depois de apanhar e posiciona o olho e o chifre.
    void Animar()
    {
        desenho.sprite = poseFixa != null ? poseFixa : quadros[(int)(relogio * 8f) % quadros.Length];

        if (timerPiscar > 0f)
        {
            timerPiscar -= Time.deltaTime;
            desenho.color = (int)(timerPiscar * 16f) % 2 == 0 ? new Color(1f, 0.45f, 0.45f) : Color.white;
            if (timerPiscar <= 0f) desenho.color = Color.white;
        }

        olho.enabled = desenho.enabled && !submersa && poseFixa != spriteTonta;
        olho.transform.position = Ponto(ArteDaBaleia.PixelDoOlho);
        brilhoDoChifre.enabled = desenho.enabled && poseFixa == spriteCarga;
        brilhoDoChifre.transform.position = PontaDoChifre;
        brilhoDoChifre.transform.localScale = Vector3.one * (0.8f + 0.25f * Mathf.Sin(relogio * 30f));
    }

    // ------------------------------------------------------------------ a luta

    IEnumerator Lutar(bool renasceu)
    {
        estado = Estado.Lutando;
        EmCombate = true;
        FecharArena();
        CameraSeguir.Travar(esquerda - 1f, direita + 1f);
        GerenciadorDoJogo.Instancia.TocarMusicaDoChefe();
        GerenciadorDoJogo.Instancia.SalvarPonto(PontoDeRenascer); // morreu? renasce aqui, no começo da arena

        etapa = Mathf.Clamp(GerenciadorDoJogo.Instancia.EtapaDoChefe, 0, Etapas - 1);
        Vida = vidaMaxima - etapa * GolpesPorEtapa;
        VidaMostrada = Vida;

        if (renasceu) yield return Pronto();
        else yield return Introducao();

        while (true)
        {
            if (etapa == Etapas - 1) yield return Desmoronar();

            // repete as rodadas da etapa até levar os 2 golpes dela
            int vidaNoFimDaEtapa = vidaMaxima - (etapa + 1) * GolpesPorEtapa;
            while (Vida > vidaNoFimDaEtapa)
            {
                int golpe = vidaMaxima - etapa * GolpesPorEtapa - Vida; // 0 = 1º golpe da etapa, 1 = 2º
                yield return Rodada(etapa * GolpesPorEtapa + golpe);
            }
            if (Vida <= 0) break;

            etapa++;
            GerenciadorDoJogo.Instancia.EtapaDoChefe = etapa; // o "checkpoint" do chefe
            yield return NovaEtapa();
        }

        yield return SerDerrotada();
    }

    // Cada rodada é uma sequência FIXA de ataques que termina com o mergulho (a chance de dar dano).
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

    // ETAPA 1: um ataque de cada vez.
    IEnumerator Etapa1Rodada1()
    {
        yield return Investida(alta: true, daDireita: true);
        yield return Investida(alta: false, daDireita: false);
        yield return Chuva(0, 1, 0);
        yield return new WaitForSeconds(0.4f);
        yield return Mergulho(finta: false);
    }

    IEnumerator Etapa1Rodada2()
    {
        yield return Correntes(CorrentesA, Nenhuma);
        yield return new WaitForSeconds(0.2f);
        yield return Correntes(CorrentesB, Nenhuma);
        yield return new WaitForSeconds(0.3f);
        yield return OndaDeChoque(daDireita: true, quantas: 2, intervalo: 1.3f);
        yield return EsperarTodos();
        yield return RaioDoChifre(3);
        yield return Mergulho(finta: false);
    }

    // ETAPA 2: dois ataques ao mesmo tempo.
    IEnumerator Etapa2Rodada1()
    {
        Paralelo(Chuva(0, 1, 0, 1));                       // dê um passo a cada bola...
        yield return new WaitForSeconds(0.5f);
        yield return Investida(alta: true, daDireita: false); // ...sem pular!
        yield return EsperarTodos();
        yield return Correntes(CorrentesA, Alta);
        yield return new WaitForSeconds(0.2f);
        yield return Correntes(CorrentesB, Rente);
        yield return new WaitForSeconds(0.3f);
        yield return Anel(10, 0f, 5f);
        yield return OndaDeChoque(daDireita: true, quantas: 2, intervalo: 0.45f); // juntinhas: um pulo só, comprido
        yield return EsperarTodos();
        yield return Mergulho(finta: false);
    }

    IEnumerator Etapa2Rodada2()
    {
        Paralelo(Investida(alta: false, daDireita: true)); // a barbatana passa no meio da chuva: pule no vão
        yield return Chuva(1, 0, 1, 0);
        yield return EsperarTodos();
        yield return Correntes(CorrentesA, Nenhuma);
        yield return new WaitForSeconds(0.15f);
        yield return Correntes(CorrentesB, Alta);
        yield return new WaitForSeconds(0.15f);
        yield return Correntes(CorrentesA, Rente);
        yield return new WaitForSeconds(0.3f);
        Paralelo(Atrasar(1.2f, OndaDeChoque(daDireita: false, quantas: 1, intervalo: 0f)));
        yield return RaioDoChifre(3);
        yield return EsperarTodos();
        yield return Mergulho(finta: false);
    }

    // ETAPA 3: o chão já desmoronou; tudo mais rápido e o mergulho tem FINTA (ela cai duas vezes).
    IEnumerator Etapa3Rodada1()
    {
        Paralelo(Chuva(0, 1, 0, 1, 0));
        yield return new WaitForSeconds(0.4f);
        yield return Investida(alta: false, daDireita: false);
        yield return EsperarTodos();
        yield return Correntes(CorrentesA, Alta);
        yield return new WaitForSeconds(0.1f);
        yield return Correntes(CorrentesB, Rente);
        yield return new WaitForSeconds(0.1f);
        yield return Correntes(CorrentesA, Rente);
        yield return new WaitForSeconds(0.3f);
        yield return Anel(12, 15f, 5.5f);
        yield return RaioDoChifre(4);
        yield return EsperarTodos();
        yield return OndaDeChoque(daDireita: true, quantas: 2, intervalo: 0.45f);
        yield return new WaitForSeconds(0.9f);
        yield return OndaDeChoque(daDireita: true, quantas: 1, intervalo: 0f);
        yield return EsperarTodos();
        yield return Mergulho(finta: true);
        yield return Mergulho(finta: false);
    }

    // O ataque DESESPERADO (o último golpe): a névoa escurece tudo e ela mistura todos os ataques.
    IEnumerator Etapa3Rodada2()
    {
        yield return Escurecer();
        Paralelo(Chuva(0, 1, 0));
        yield return new WaitForSeconds(0.3f);
        yield return Investida(alta: true, daDireita: true);
        yield return EsperarTodos();
        yield return Correntes(CorrentesB, Rente);
        yield return new WaitForSeconds(0.1f);
        yield return Correntes(CorrentesA, Alta);
        yield return new WaitForSeconds(0.3f);
        Paralelo(Atrasar(0.6f, OndaDeChoque(daDireita: false, quantas: 2, intervalo: 0.45f)));
        yield return RaioDoChifre(3);
        yield return EsperarTodos();
        yield return Mergulho(finta: true);
        yield return Mergulho(finta: false);
    }

    // ------------------------------------------------------------------ começo, etapas e fim

    // Só na primeira vez: ela passa lá no fundo (pequena e transparente) e depois vem para a frente.
    IEnumerator Introducao()
    {
        GerenciadorDoJogo.Som("rugido");
        CameraSeguir.Tremer(0.12f, 1f);
        GerenciadorDoJogo.Instancia.Avisar("A BALEIA BRANCA!", 2.2f);

        desenho.enabled = true;
        desenho.sortingOrder = -23; // entre o castelo e a floresta do fundo
        desenho.color = new Color(1f, 1f, 1f, 0.55f);
        desenho.flipX = true;       // olhando para a direita
        baleia.localScale = Vector3.one * 0.6f;
        olho.sortingOrder = -22;
        for (float t = 0f; t < 2.2f; t += Time.deltaTime)
        {
            float x = Mathf.Lerp(esquerda - 3f, direita + 3f, t / 2.2f);
            baleia.position = new Vector3(x, chao + 8f + Mathf.Sin(t * 2f) * 0.3f, 0f);
            yield return null;
        }

        // agora de verdade: grande, na frente, entrando pela direita
        desenho.color = Color.white;
        olho.sortingOrder = OrdemDoOlho;
        MostrarBaleia(OrdemDaBaleia);
        baleia.position = new Vector3(direita + 4f, descanso.y, 0f);
        yield return NadarAte(descanso, 1f);
        livre = true;
        yield return Pose(spriteBoca, 0.7f);
        GerenciadorDoJogo.Som("rugido");
        CameraSeguir.Tremer(0.2f, 0.6f);
        GerenciadorDoJogo.Instancia.Avisar("Fuja de tudo. Pule na CABEÇA dela quando ela cair!", 2.5f);
        yield return new WaitForSeconds(1.2f);
    }

    // Depois de morrer: 1 segundo de "PRONTO?" e a luta volta na etapa em que estava.
    IEnumerator Pronto()
    {
        GerenciadorDoJogo.Instancia.Avisar($"ETAPA {EtapaAtual}/{Etapas} - PRONTO?", 1.3f);
        baleia.position = new Vector3(meio, chao + 16f, 0f);
        MostrarBaleia(OrdemDaBaleia);
        livre = true;
        GerenciadorDoJogo.Som("rugido", 0.6f);
        yield return new WaitForSeconds(1.6f); // um respiro: o 1º ataque não pode pegar quem acabou de nascer
    }

    IEnumerator NovaEtapa()
    {
        GerenciadorDoJogo.Instancia.Avisar(etapa == 1 ? "ETAPA 2/3 - ela ficou BRAVA!" : "ETAPA 3/3 - ela tá DESESPERADA!", 2f);
        GerenciadorDoJogo.Som("rugido");
        CameraSeguir.Tremer(0.25f, 1f);
        timerPiscar = 0.8f;
        yield return Pose(spriteBoca, 1.4f);
    }

    // O chão da arena racha e despenca nas colunas frágeis (fica o abismo, marcado com névoa roxa).
    IEnumerator Desmoronar()
    {
        if (blocosFrageis.Count == 0) yield break;
        GerenciadorDoJogo.Instancia.Avisar("O CHÃO! SAI DAÍ!", 1.5f);
        GerenciadorDoJogo.Som("armadilha");
        CameraSeguir.Tremer(0.1f, 1.2f);
        var origens = new List<Vector3>();
        foreach (GameObject bloco in blocosFrageis)
        {
            origens.Add(bloco.transform.position);
            Desenho("Rachadura", "baleia_rachadura", bloco.transform.position, 1).transform.SetParent(bloco.transform, true);
        }
        for (float t = 0f; t < 1.1f; t += Time.deltaTime) // tremendo
        {
            for (int i = 0; i < blocosFrageis.Count; i++)
                blocosFrageis[i].transform.position = origens[i] + (Vector3)(Random.insideUnitCircle * 0.05f);
            yield return null;
        }

        GerenciadorDoJogo.Som("pancada");
        foreach (GameObject bloco in blocosFrageis) bloco.GetComponent<BoxCollider2D>().enabled = false;
        for (int coluna = 0; coluna < ColunasFrageis.Length; coluna++) // névoa saindo do abismo
        {
            var nevoa = Desenho("NevoaDoAbismo", "nevoa", new Vector3(esquerda + ColunasFrageis[coluna] + 0.5f, chao - 0.2f, 0f), OrdemDaNevoa);
            nevoa.color = new Color(0.75f, 0.45f, 1f, 0.6f);
            nevoa.transform.localScale = Vector3.one * 1.3f;
            Animacao.Adicionar(nevoa.gameObject, Animacao.Tipo.Flutuar, 2f, 0.12f);
        }
        for (float queda = 0f, t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            queda += 40f * Time.deltaTime;
            foreach (GameObject bloco in blocosFrageis) bloco.transform.position += Vector3.down * queda * Time.deltaTime;
            yield return null;
        }
        foreach (GameObject bloco in blocosFrageis) Destroy(bloco);
        blocosFrageis.Clear();
    }

    IEnumerator Escurecer()
    {
        if (escuridao != null) yield break; // já está escuro (ela não acertou o último golpe e repetiu)
        GerenciadorDoJogo.Instancia.Avisar("A NÉVOA ENGOLIU TUDO!", 2f);
        GerenciadorDoJogo.Som("rugido");
        escuridao = Desenho("Escuridao", "baleia_escuridao", baleia.position, OrdemDaEscuridao);
        escuridao.transform.localScale = Vector3.one * 3.2f; // cobre a tela; o buraco em volta do Subaru tem ~3 blocos
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            escuridao.color = new Color(1f, 1f, 1f, t);
            yield return null;
        }
        escuridao.color = Color.white;
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator SerDerrotada()
    {
        estado = Estado.Derrotada;
        corpoPerigoso = false;
        zonas.Clear();
        ApagarEstrelas();
        livre = false;
        poseFixa = spriteTonta;
        Conquistas.Desbloquear("baleia");
        // a partir daqui, se morrer (caiu num buraco...), renasce DEPOIS da arena
        GerenciadorDoJogo.Instancia.ChefeVencido(new Vector3(direita + 1.5f, chao + 0.5f, 0f));
        GerenciadorDoJogo.Instancia.Avisar("A BALEIA BRANCA FOI DERROTADA!", 3f);

        // treme, pisca e afunda na névoa girando (e a escuridão vai embora)
        GerenciadorDoJogo.Som("rugido");
        CameraSeguir.Tremer(0.25f, 1.2f);
        timerPiscar = 1.2f;
        Vector3 de = baleia.position;
        for (float t = 0f; t < 2f; t += Time.deltaTime)
        {
            float p = t / 2f;
            baleia.position = de + Vector3.down * p * p * 6f + (Vector3)(Random.insideUnitCircle * 0.1f * (1f - p));
            baleia.rotation = Quaternion.Euler(0f, 0f, (desenho.flipX ? -40f : 40f) * p);
            if (timerPiscar <= 0f) desenho.color = new Color(1f, 1f, 1f, 1f - p);
            if (escuridao != null) escuridao.color = new Color(1f, 1f, 1f, 1f - p);
            if (Random.value < 0.3f) Efeitos.Poeira(transform, baleia.position + (Vector3)(Random.insideUnitCircle * 2f), 3, 3f);
            yield return null;
        }
        desenho.enabled = false;
        olho.enabled = false;
        if (escuridao != null) Destroy(escuridao.gameObject);

        AbrirArena();
        CameraSeguir.Destravar();
        EmCombate = false;
    }

    // ------------------------------------------------------------------ os ataques

    // INVESTIDA: ela sai da tela e atravessa a arena inteira.
    //  alta  = passa um pouco acima da cabeça do Subaru (faixa vermelha no alto): NÃO pule;
    //  baixa = nada DENTRO do chão, só a barbatana e o chifre aparecem (faixa rente ao chão): PULE.
    IEnumerator Investida(bool alta, bool daDireita)
    {
        Dica(alta ? "Lá no alto! NÃO PULE!" : "Barbatana no chão! PULE!");
        yield return SairPorCima();

        float baixo = alta ? chao + 1.1f : chao, cima = alta ? chao + 3.6f : chao + 0.9f;
        float y = alta ? chao + 2.35f : chao - 0.35f;
        float xDoAviso = daDireita ? direita - 0.7f : esquerda + 0.7f;
        var faixa = Desenho("Faixa", "baleia_faixa", new Vector3(meio, (baixo + cima) / 2f, 0f), OrdemDosAvisos);
        faixa.transform.localScale = new Vector3(larguraDaArena * FabricaDeSprites.PixelsPorUnidade, cima - baixo, 1f);
        var exclamacao = Desenho("Exclamacao", "aviso", new Vector3(xDoAviso, (baixo + cima) / 2f, 0f), OrdemDosAvisos + 1);
        GerenciadorDoJogo.Som("rugido", 0.5f);
        float aviso = Aviso + 0.15f;
        for (float t = 0f; t < aviso; t += Time.deltaTime)
        {
            bool aceso = Piscando(t, aviso);
            faixa.enabled = aceso;
            exclamacao.enabled = aceso;
            if (!alta && Random.value < 0.3f) Efeitos.Poeira(transform, new Vector3(xDoAviso, chao + 0.1f, 0f), 1, 2f); // o chão treme
            yield return null;
        }
        Destroy(faixa.gameObject);
        Destroy(exclamacao.gameObject);

        float sentido = daDireita ? -1f : 1f;
        float x = daDireita ? direita + 4.5f : esquerda - 4.5f, fim = daDireita ? esquerda - 4.5f : direita + 4.5f;
        baleia.position = new Vector3(x, y, 0f);
        desenho.flipX = !daDireita; // o desenho olha para a esquerda; indo para a direita, vira
        poseFixa = alta ? spriteBoca : null;
        submersa = !alta;
        MostrarBaleia(alta ? OrdemDaBaleia : OrdemSubmersa); // submersa: desenhada ATRÁS do chão
        corpoPerigoso = true;
        CameraSeguir.Tremer(0.1f, 0.4f);
        float velocidade = VelocidadeDaInvestida[etapa], proximaPoeira = 0f;
        while ((fim - x) * sentido > 0f)
        {
            x += sentido * velocidade * Time.deltaTime;
            baleia.position = new Vector3(x, y, 0f);
            proximaPoeira -= Time.deltaTime;
            if (!alta && proximaPoeira <= 0f) // a terra voando onde a barbatana corta o chão
            {
                proximaPoeira = 0.05f;
                Efeitos.Poeira(transform, new Vector3(PontaDoChifre.x, chao + 0.1f, 0f), 2, 2.5f);
                Efeitos.Poeira(transform, new Vector3(Ponto(new Vector2(50f, 30f)).x, chao + 0.1f, 0f), 2, 2.5f);
            }
            yield return null;
        }
        corpoPerigoso = false;
        submersa = false;
        poseFixa = null;
        EsconderBaleia();
        yield return Voltar();
    }

    // CHUVA DE NÉVOA: cada "batida" derruba 6 bolas nas posições do padrão A ou B (uma marca no chão avisa).
    IEnumerator Chuva(params int[] padroes)
    {
        Dica("Chuva de névoa! Fique ENTRE as marcas!");
        foreach (int padrao in padroes)
        {
            foreach (float x in padrao == 0 ? ChuvaA : ChuvaB) Paralelo(Gota(esquerda + x, Aviso + 0.2f));
            if (livre) StartCoroutine(Pose(spriteBoca, 0.4f));
            GerenciadorDoJogo.Som("pop", 0.6f);
            yield return new WaitForSeconds(Aviso + 0.3f);
        }
    }

    IEnumerator Gota(float x, float tempo)
    {
        var marca = Desenho("Marca", "baleia_alvo", new Vector3(x, chao + 0.15f, 0f), OrdemDosAvisos);
        marca.transform.localScale = new Vector3(0.85f, 1f, 1f);
        var bola = Desenho("BolaDeNevoa", "baleia_bola_0", new Vector3(x, chao + 13.5f, 0f), OrdemDosAtaques);
        bola.transform.localScale = Vector3.one * 1.5f;
        Zona zona = NovaZona(0.5f);
        float de = chao + 13.5f, ate = chao + 0.6f;
        for (float t = 0f; t < tempo; t += Time.deltaTime)
        {
            float p = t / tempo;
            Vector2 lugar = new Vector2(x, Mathf.Lerp(de, ate, p * p)); // cai cada vez mais rápido
            bola.transform.position = lugar;
            bola.sprite = quadrosDaBola[(int)(t * 10f) % 2];
            zona.a = zona.b = lugar;
            marca.color = Piscando(t, tempo) ? Vermelho : VermelhoFraco;
            yield return null;
        }
        zonas.Remove(zona);
        Efeitos.Poeira(transform, new Vector3(x, chao + 0.2f, 0f), 6, 3f);
        Destroy(bola.gameObject);
        Destroy(marca.gameObject);
    }

    // CORRENTES (o "julgamento" do Helltaker): linhas finas piscam e viram RAIOS que matam.
    // verticais = posições x (a partir da borda esquerda); deitadas = alturas acima do chão.
    IEnumerator Correntes(float[] verticais, float[] deitadas)
    {
        Dica("Correntes! Fique nos VÃOS!");
        bool rente = false;
        foreach (float altura in deitadas) if (altura < 1f) rente = true;
        if (rente) Dica("Corrente no chão! PULE na hora!", ateAEtapa: 1); // ela só aparece a partir da etapa 2
        float aviso = rente ? Mathf.Max(Aviso, AvisoMinimoRente) : Aviso;
        if (livre) poseFixa = spriteCarga;

        foreach (float x in verticais)
            Paralelo(Raio(new Vector2(esquerda + x, chao - 0.2f), new Vector2(esquerda + x, chao + 12.6f), aviso, 0.4f));
        foreach (float altura in deitadas)
            Paralelo(Raio(new Vector2(esquerda, chao + altura), new Vector2(direita, chao + altura), aviso, altura < 1f ? 0.3f : 0.4f));
        GerenciadorDoJogo.Som("armadilha", 0.5f);
        yield return new WaitForSeconds(aviso);
        GerenciadorDoJogo.Som("serra", 0.7f);
        CameraSeguir.Tremer(0.06f, 0.2f);
        yield return new WaitForSeconds(0.45f);
        if (poseFixa == spriteCarga && livre) poseFixa = null;
    }

    // Um raio reto de a até b: primeiro a linha de aviso (com um elo de corrente em cada ponta),
    // depois o raio que mata durante "duracao" segundos.
    IEnumerator Raio(Vector2 a, Vector2 b, float aviso, float duracao)
    {
        var linha = Desenho("AvisoDoRaio", "baleia_linha", a, OrdemDosAvisos);
        Esticar(linha.transform, a, b, 1f);
        Vector2 direcao = (b - a).normalized;
        var elos = new[]
        {
            Desenho("Elo", "baleia_elo", a + direcao * 0.4f, OrdemDosAvisos + 1),
            Desenho("Elo", "baleia_elo", b - direcao * 0.4f, OrdemDosAvisos + 1),
        };
        foreach (SpriteRenderer elo in elos) elo.transform.rotation = linha.transform.rotation;
        for (float t = 0f; t < aviso; t += Time.deltaTime)
        {
            linha.color = Piscando(t, aviso) ? Vermelho : VermelhoFraco;
            yield return null;
        }
        Destroy(linha.gameObject);

        var raio = Desenho("Raio", "baleia_raio_0", a, OrdemDosAtaques);
        Zona zona = NovaZona(0.25f);
        zona.a = a;
        zona.b = b;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            raio.sprite = quadrosDoRaio[(int)(t * 24f) % quadrosDoRaio.Length];
            Esticar(raio.transform, a, b, 1f - 0.5f * (t / duracao) * (t / duracao)); // afina no fim
            yield return null;
        }
        zonas.Remove(zona);
        Destroy(raio.gameObject);
        foreach (SpriteRenderer elo in elos) Destroy(elo.gameObject);
    }

    // ONDA DE CHOQUE: ela bate o rabo, um "!" pisca na parede e a onda corre rente ao chão até o outro lado.
    IEnumerator OndaDeChoque(bool daDireita, int quantas, float intervalo)
    {
        Dica("Onda de choque! PULE!");
        float x = daDireita ? direita - 0.3f : esquerda + 0.3f;
        var exclamacao = Desenho("Exclamacao", "aviso", new Vector3(x, chao + 1.6f, 0f), OrdemDosAvisos);
        if (livre) StartCoroutine(Pose(spriteBoca, 0.4f));
        GerenciadorDoJogo.Som("pancada", 0.7f);
        for (float t = 0f; t < Aviso; t += Time.deltaTime)
        {
            exclamacao.enabled = Piscando(t, Aviso);
            if (Random.value < 0.4f) Efeitos.Poeira(transform, new Vector3(x, chao + 0.1f, 0f), 1, 2.5f);
            yield return null;
        }
        Destroy(exclamacao.gameObject);
        for (int i = 0; i < quantas; i++)
        {
            if (i > 0) yield return new WaitForSeconds(intervalo);
            CameraSeguir.Tremer(0.15f, 0.2f);
            GerenciadorDoJogo.Som("pancada");
            Paralelo(OndaCorrendo(x, daDireita ? -1f : 1f));
        }
    }

    IEnumerator OndaCorrendo(float x, float sentido)
    {
        var onda = Desenho("Onda", "baleia_onda_0", new Vector3(x, chao, 0f), OrdemDosAtaques);
        onda.flipX = sentido < 0f; // o desenho vai para a direita
        onda.transform.localScale = Vector3.one * 1.2f;
        Zona zona = NovaZona(0.36f); // mata até 0,72 acima do chão
        float velocidade = VelocidadeDaOnda[etapa];
        float fim = sentido < 0f ? esquerda : direita;
        for (float t = 0f; (fim - x) * sentido > 0f; t += Time.deltaTime)
        {
            x += sentido * velocidade * Time.deltaTime;
            onda.transform.position = new Vector3(x, chao, 0f);
            onda.sprite = quadrosDaOnda[(int)(t * 10f) % 2];
            zona.a = new Vector2(x - 0.2f, chao + 0.36f);
            zona.b = new Vector2(x + 0.2f, chao + 0.36f);
            yield return null;
        }
        zonas.Remove(zona);
        Destroy(onda.gameObject);
    }

    // RAIO DO CHIFRE: ela vai para o canto, o chifre acende e cada tiro tem 3 tempos:
    // MIRA (linha lilás seguindo você) -> TRAVA (linha vermelha piscando) -> FOGO (o raio atravessa a arena).
    // Quem continua andando depois que a linha fica vermelha não é acertado.
    IEnumerator RaioDoChifre(int tiros)
    {
        Dica("Raio do chifre! NÃO FIQUE PARADO!");
        livre = false;
        GerenciadorDoJogo.JogadorVivo(out Vector2 jogador);
        Vector2 canto = new Vector2(jogador.x < meio ? direita - 3.5f : esquerda + 3.5f, chao + 8.5f);
        if (!desenho.enabled)
        {
            baleia.position = new Vector3(canto.x, chao + 16f, 0f);
            MostrarBaleia(OrdemDaBaleia);
        }
        yield return NadarAte(canto, 0.6f);
        poseFixa = spriteCarga;
        GerenciadorDoJogo.Som("rugido", 0.4f);

        for (int i = 0; i < tiros; i++)
        {
            Vector2 alvo = jogador;
            var linha = Desenho("Mira", "baleia_linha", baleia.position, OrdemDosAvisos);
            const float mirando = 0.45f;
            float trava = Mathf.Max(Aviso, TravaMinimaDoChifre);
            for (float t = 0f; t < mirando + trava; t += Time.deltaTime)
            {
                bool travou = t >= mirando;
                if (!travou && GerenciadorDoJogo.JogadorVivo(out Vector2 agora))
                {
                    alvo = agora;
                    Olhar(alvo.x);
                }
                Esticar(linha.transform, PontaDoChifre, AteOndeVai(PontaDoChifre, alvo), travou ? 1f : 0.6f);
                linha.color = !travou ? Lilas : Piscando(t - mirando, trava) ? Vermelho : VermelhoFraco;
                yield return null;
            }
            Destroy(linha.gameObject);

            Vector2 a = PontaDoChifre, b = AteOndeVai(a, alvo);
            var raio = Desenho("RaioDoChifre", "baleia_raio_0", a, OrdemDosAtaques);
            Zona zona = NovaZona(0.3f);
            zona.a = a;
            zona.b = b;
            GerenciadorDoJogo.Som("serra");
            CameraSeguir.Tremer(0.12f, 0.2f);
            Efeitos.Poeira(transform, b, 6, 3f);
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                raio.sprite = quadrosDoRaio[(int)(t * 24f) % quadrosDoRaio.Length];
                Esticar(raio.transform, a, b, 1.3f - t * 2f);
                yield return null;
            }
            zonas.Remove(zona);
            Destroy(raio.gameObject);
            yield return new WaitForSeconds(0.12f);
            GerenciadorDoJogo.JogadorVivo(out jogador);
        }
        poseFixa = null;
        livre = true;
    }

    // O raio sai do chifre, passa pelo alvo e só para no chão ou numa parede da arena.
    Vector2 AteOndeVai(Vector2 de, Vector2 alvo)
    {
        Vector2 direcao = alvo - de;
        if (direcao.magnitude < 0.01f) direcao = Vector2.down;
        direcao = direcao.normalized;
        float alcance = 40f;
        if (direcao.y < -0.01f) alcance = Mathf.Min(alcance, (chao - de.y) / direcao.y);
        if (direcao.x > 0.01f) alcance = Mathf.Min(alcance, (direita - de.x) / direcao.x);
        if (direcao.x < -0.01f) alcance = Mathf.Min(alcance, (esquerda - de.x) / direcao.x);
        return de + direcao * alcance;
    }

    // ANEL DE NÉVOA: ela ruge e solta bolas em círculo, devagar. Ache o vão entre as que descem.
    IEnumerator Anel(int quantas, float giroEmGraus, float velocidade)
    {
        if (livre) StartCoroutine(Pose(spriteBoca, 0.5f));
        GerenciadorDoJogo.Som("rugido", 0.4f);
        Vector2 boca = Ponto(ArteDaBaleia.PixelDaBoca);
        for (int i = 0; i < quantas; i++)
        {
            float angulo = (giroEmGraus + i * 360f / quantas) * Mathf.Deg2Rad;
            Paralelo(Bola(boca, new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)), velocidade));
        }
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator Bola(Vector2 lugar, Vector2 direcao, float velocidade)
    {
        var bola = Desenho("BolaDoAnel", "baleia_bola_0", lugar, OrdemDosAtaques);
        Zona zona = NovaZona(0f);
        float t = 0f;
        while (lugar.y > chao + 0.3f && lugar.y < chao + 15f && lugar.x > esquerda - 1f && lugar.x < direita + 1f)
        {
            t += Time.deltaTime;
            lugar += direcao * velocidade * Time.deltaTime;
            float crescer = Mathf.Min(1f, t * 4f); // nasce pequenininha na boca
            bola.transform.position = lugar;
            bola.transform.localScale = Vector3.one * 1.2f * crescer;
            bola.sprite = quadrosDaBola[(int)(t * 10f) % 2];
            zona.a = zona.b = lugar;
            zona.raio = 0.4f * crescer;
            yield return null;
        }
        zonas.Remove(zona);
        if (lugar.y <= chao + 0.3f) Efeitos.Poeira(transform, new Vector3(lugar.x, chao + 0.2f, 0f), 4, 2f);
        Destroy(bola.gameObject);
    }

    // MERGULHO: ela some por cima; a marca no chão (do tamanho dela) segue você, TRAVA e ela despenca ali.
    // Normal: fica atordoada. Finta (etapa 3): quica e mergulha de novo logo em seguida.
    IEnumerator Mergulho(bool finta)
    {
        Dica("Mergulho! Saia de baixo da marca!");
        yield return SairPorCima();

        // ela nunca cai colada na parede: assim os cantinhos da arena são SEMPRE seguros no mergulho
        float minimo = esquerda + 3.7f, maximo = direita - 3.7f;
        float x = GerenciadorDoJogo.JogadorVivo(out Vector2 jogador) ? Mathf.Clamp(jogador.x, minimo, maximo) : meio;
        var marca = Desenho("MarcaDoMergulho", "baleia_alvo", new Vector3(x, chao + 0.15f, 0f), OrdemDosAvisos);
        marca.transform.localScale = new Vector3(3f, 1.3f, 1f);
        var bordas = new[] // duas linhas mostrando a largura dela: fique FORA delas
        {
            Desenho("Borda", "baleia_linha", Vector3.zero, OrdemDosAvisos),
            Desenho("Borda", "baleia_linha", Vector3.zero, OrdemDosAvisos),
        };
        float seguir = finta ? 0.5f : TempoSeguindo[etapa];
        for (float t = 0f; t < seguir + TravaDoMergulho; t += Time.deltaTime)
        {
            bool travou = t >= seguir;
            if (!travou && GerenciadorDoJogo.JogadorVivo(out jogador))
                x = Mathf.MoveTowards(x, Mathf.Clamp(jogador.x, minimo, maximo), 8f * Time.deltaTime);
            Color cor = !travou ? Lilas : Piscando(t - seguir, TravaDoMergulho) ? Vermelho : VermelhoFraco;
            marca.transform.position = new Vector3(x, chao + 0.15f, 0f);
            marca.color = cor;
            for (int lado = 0; lado < 2; lado++)
            {
                float borda = x + (lado == 0 ? -3f : 3f);
                Esticar(bordas[lado].transform, new Vector2(borda, chao), new Vector2(borda, chao + 3f), 1f);
                bordas[lado].color = cor;
            }
            yield return null;
        }

        // despenca de barriga
        desenho.flipX = x < meio; // de cara para o lado mais largo da arena
        poseFixa = spriteBoca;
        MostrarBaleia(OrdemDaBaleia);
        corpoPerigoso = true;
        float y = chao + 14.5f, pousada = chao + 1.25f * tamanho, velocidade = 25f;
        while (y > pousada)
        {
            velocidade += 120f * Time.deltaTime;
            y = Mathf.Max(pousada, y - velocidade * Time.deltaTime);
            baleia.position = new Vector3(x, y, 0f);
            yield return null;
        }
        Destroy(marca.gameObject);
        foreach (SpriteRenderer borda in bordas) Destroy(borda.gameObject);
        GerenciadorDoJogo.Som("pancada");
        GerenciadorDoJogo.Som("rugido", 0.6f);
        CameraSeguir.Tremer(0.3f, 0.4f);
        Efeitos.Poeira(transform, new Vector3(x - 2f, chao + 0.2f, 0f), 10, 3f);
        Efeitos.Poeira(transform, new Vector3(x + 2f, chao + 0.2f, 0f), 10, 3f);
        yield return new WaitForSeconds(0.12f);
        corpoPerigoso = false;

        if (finta)
        {
            GerenciadorDoJogo.Som("risada", 0.6f);
            yield return SairPorCima();
            yield break;
        }
        yield return Atordoada();
    }

    // Atordoada no chão: não machuca, e as costas dela viram um "botão". Pule em cima!
    IEnumerator Atordoada()
    {
        estado = Estado.Atordoada;
        golpeada = false;
        poseFixa = spriteTonta;
        CriarEstrelas();
        Dica("AGORA! Pule na CABEÇA dela!");
        Vector3 deitada = baleia.position;
        float tempo = TempoAtordoada[etapa];
        for (float t = 0f; t < tempo && !golpeada; t += Time.deltaTime)
        {
            // meio segundo antes de acordar ela treme (aviso)
            baleia.position = t > tempo - 0.5f ? deitada + (Vector3)(Random.insideUnitCircle * 0.06f) : deitada;
            yield return null;
        }
        baleia.position = deitada;
        ApagarEstrelas();
        if (estado == Estado.Atordoada) estado = Estado.Lutando;
        if (Vida <= 0) yield break; // levou o último golpe: fica no chão para a cena da derrota

        poseFixa = null;
        yield return SairPorCima(); // vai embora para cima (sem machucar ninguém)
        yield return Voltar();
    }

    // Pulou na cabeça (nas costas) dela, caindo, por cima?
    void TestarPisao()
    {
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (golpeada || jogador == null || jogador.Morto || jogador.Corpo.linearVelocity.y > 0.5f) return;
        Vector3 pes = jogador.transform.position + Vector3.down * 0.5f;
        CaixaNoMundo(CaixasDoCorpo[0], out Vector2 min, out Vector2 max);
        float costas = baleia.position.y + 0.55f * tamanho;
        if (pes.x > min.x - 0.2f && pes.x < max.x + 0.2f && pes.y > costas - 0.5f && pes.y < costas + 0.8f)
            LevarGolpe(jogador);
    }

    void LevarGolpe(Jogador jogador)
    {
        Vida--;
        golpeada = true;
        timerPiscar = 0.8f;
        jogador.Quicar(14f, true);
        GerenciadorDoJogo.Som("pisao");
        GerenciadorDoJogo.Som("rugido", 0.8f);
        CameraSeguir.Tremer(0.2f, 0.3f);
        Efeitos.Poeira(transform, baleia.position + Vector3.up * 0.8f, 8, 3f);
        string[] falas = { "ACERTOU! Mais um!", "Ela tá tonta! De novo!", "SÓ MAIS UM! VAI, SUBARU!" };
        if (Vida > 0 && Vida % GolpesPorEtapa != 0) GerenciadorDoJogo.Instancia.Avisar(falas[etapa], 1.6f);
    }

    // ------------------------------------------------------------------ movimento e desenho

    IEnumerator SairPorCima()
    {
        livre = false;
        if (!desenho.enabled) yield break;
        poseFixa = null;
        float velocidade = 4f;
        while (baleia.position.y < chao + 16f)
        {
            velocidade += 60f * Time.deltaTime;
            baleia.position += Vector3.up * velocidade * Time.deltaTime;
            yield return null;
        }
        EsconderBaleia();
    }

    // Volta lá de cima para o lugar de descanso (o Update faz ela descer flutuando).
    IEnumerator Voltar()
    {
        if (!desenho.enabled)
        {
            baleia.position = new Vector3(meio, chao + 16f, 0f);
            MostrarBaleia(OrdemDaBaleia);
        }
        livre = true;
        while (baleia.position.y > descanso.y + 0.6f) yield return null;
    }

    IEnumerator NadarAte(Vector2 destino, float duracao)
    {
        Vector2 de = baleia.position;
        Olhar(destino.x);
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            baleia.position = Vector2.Lerp(de, destino, Mathf.SmoothStep(0f, 1f, t / duracao));
            yield return null;
        }
        baleia.position = destino;
    }

    // Troca o desenho por uma pose (boca aberta, chifre aceso...) por um tempinho.
    IEnumerator Pose(Sprite pose, float duracao)
    {
        poseFixa = pose;
        yield return new WaitForSeconds(duracao);
        if (poseFixa == pose) poseFixa = null;
    }

    void MostrarBaleia(int ordem)
    {
        desenho.enabled = true;
        desenho.sortingOrder = ordem;
        baleia.localScale = Vector3.one * tamanho;
        baleia.rotation = Quaternion.identity;
    }

    void EsconderBaleia() => desenho.enabled = false;

    void Olhar(float x)
    {
        if (Mathf.Abs(x - baleia.position.x) > 0.3f) desenho.flipX = x > baleia.position.x;
    }

    // Um ponto do desenho (em pixels) no mundo, levando em conta a posição, o tamanho e o lado em que ela olha.
    Vector2 Ponto(Vector2 pixel)
    {
        Vector2 deslocamento = ArteDaBaleia.EmUnidades(pixel) * baleia.localScale.x;
        if (desenho.flipX) deslocamento.x = -deslocamento.x;
        return (Vector2)baleia.position + deslocamento;
    }

    Vector2 PontaDoChifre => Ponto(ArteDaBaleia.PixelDaPontaDoChifre);

    // Um retângulo do desenho (em pixels) no mundo.
    void CaixaNoMundo(Rect pixels, out Vector2 min, out Vector2 max)
    {
        Vector2 a = Ponto(new Vector2(pixels.x, pixels.y));
        Vector2 b = Ponto(new Vector2(pixels.x + pixels.width, pixels.y + pixels.height));
        min = new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
        max = new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    SpriteRenderer Desenho(string nome, string sprite, Vector3 lugar, int ordem) =>
        ConstrutorDeFase.Visual(nome, transform, lugar, sprite, ordem, false).GetComponent<SpriteRenderer>();

    // Estica um desenho de "1 pixel de comprimento" (linha, raio, faixa) de a até b.
    static void Esticar(Transform objeto, Vector2 a, Vector2 b, float grossura)
    {
        Vector2 d = b - a;
        objeto.position = (a + b) / 2f;
        objeto.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        objeto.localScale = new Vector3(d.magnitude * FabricaDeSprites.PixelsPorUnidade, grossura, 1f);
    }

    // Avisos piscam, e piscam mais rápido no finalzinho (como no Helltaker: "vai disparar AGORA").
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

        bool encostou = corpoPerigoso && EncostaNoCorpo(p);
        // o Subaru vira uma "salsicha" em pé: 3 pontos no meio dele, com a meia largura de raio
        for (int i = 0; i < zonas.Count && !encostou; i++)
            for (int k = -1; k <= 1 && !encostou; k++)
            {
                Vector2 ponto = p + new Vector2(0f, k * (MeiaAlturaDoJogador - MeiaLarguraDoJogador));
                if (DistanciaAoSegmento(ponto, zonas[i].a, zonas[i].b) < zonas[i].raio + MeiaLarguraDoJogador) encostou = true;
            }
        if (encostou) jogador.Morrer();
    }

    bool EncostaNoCorpo(Vector2 p)
    {
        foreach (Rect caixa in CaixasDoCorpo)
        {
            CaixaNoMundo(caixa, out Vector2 min, out Vector2 max);
            if (submersa) min.y = Mathf.Max(min.y, chao); // o que está dentro da terra não conta
            if (max.y <= min.y) continue;
            if (p.x > min.x - MeiaLarguraDoJogador && p.x < max.x + MeiaLarguraDoJogador &&
                p.y > min.y - MeiaAlturaDoJogador && p.y < max.y + MeiaAlturaDoJogador)
                return true;
        }
        return false;
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

    IEnumerator EsperarTodos()
    {
        while (emParalelo > 0) yield return null;
    }

    IEnumerator Atrasar(float segundos, IEnumerator ataque)
    {
        yield return new WaitForSeconds(segundos);
        yield return ataque;
    }

    // ------------------------------------------------------------------ arena e efeitos

    // Tampa os buracos do mapa com blocos frágeis (eles despencam na etapa 3).
    void TamparOsBuracos()
    {
        foreach (int coluna in ColunasFrageis)
            for (int linha = 0; linha < 2; linha++)
            {
                var bloco = ConstrutorDeFase.Visual("BlocoFragil", transform,
                    new Vector3(esquerda + coluna + 0.5f, chao - 0.5f - linha, 0f), linha == 0 ? "chao_topo" : "chao", 0);
                bloco.AddComponent<BoxCollider2D>().size = Vector2.one;
                blocosFrageis.Add(bloco);
            }
    }

    // Paredes de névoa nas duas pontas da arena (colisor de verdade + bolotas de névoa).
    void FecharArena()
    {
        foreach (float x in new[] { esquerda - 0.5f, direita + 0.5f })
        {
            var parede = new GameObject("ParedeDeNevoa");
            parede.transform.SetParent(transform, false);
            parede.transform.position = new Vector3(x, chao + 7f, 0f);
            parede.AddComponent<BoxCollider2D>().size = new Vector2(1f, 20f);
            for (float y = chao; y < chao + 13f; y += 0.8f)
            {
                var bolota = ConstrutorDeFase.Visual("Nevoa", parede.transform, new Vector3(x + Random.Range(-0.2f, 0.2f), y, 0f), "nevoa", 12, false);
                bolota.transform.localScale = Vector3.one * Random.Range(1.3f, 1.7f);
                Animacao.Adicionar(bolota, Animacao.Tipo.Flutuar, 2f, 0.15f);
            }
            paredes.Add(parede);
        }
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

    // Estrelinhas girando em cima da cabeça enquanto ela está atordoada.
    void CriarEstrelas()
    {
        for (int i = 0; i < 3; i++)
            estrelas.Add(Desenho("Estrela", "brilho", baleia.position, OrdemDoOlho).transform);
    }

    void GirarEstrelas()
    {
        Vector2 cabeca = Ponto(new Vector2(20f, 30f));
        for (int i = 0; i < estrelas.Count; i++)
        {
            float angulo = relogio * 5f + i * 2.1f;
            estrelas[i].position = cabeca + new Vector2(Mathf.Cos(angulo) * 1.2f, 0.6f + Mathf.Sin(angulo) * 0.3f);
        }
    }

    void ApagarEstrelas()
    {
        foreach (Transform estrela in estrelas) if (estrela != null) Destroy(estrela.gameObject);
        estrelas.Clear();
    }
}
