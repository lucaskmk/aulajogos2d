using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A BASE dos dois chefes do Mar de Baixo (o Tubarão-Martelo e a Mãe do Mar). O molde é o mesmo dos chefes das ilhas
// (Chefe.cs): 3 etapas de 2 golpes (3 no coop com 3 ou 4), padrões FIXOS, aviso antes de todo ataque e o dano só no
// PISÃO NA CABEÇA, na hora em que o chefe fica tonto no fim de cada rodada. Morreu, volta na mesma etapa.
//
// O que fica aqui: a arena (paredes e teto), a entrada, as etapas, o pisão, a derrota e as ZONAS que matam
// (segmentos com raio, como a Capitã e a Baleia: o Grumete é uma "salsicha" em pé). Os ataques ficam em cada chefe.
public abstract class ChefeDoMar : Chefe
{
    protected const int EtapasDaLuta = 3;
    static int GolpesDaEtapa => GolpesNoCoop(2);

    public float larguraDaArena = 22f;

    // quanto tempo o chefe fica tonto (a chance de pisar), por etapa
    protected static readonly float[] TempoTonto = { 3f, 2.7f, 2.4f };
    // o aviso antes de cada ataque, por etapa (nunca menos de 0,55 s no que pede pulo)
    protected static readonly float[] TempoDeAviso = { 0.6f, 0.5f, 0.42f };
    protected float Aviso => TempoDeAviso[etapa];
    protected float AvisoDePulo => Mathf.Max(0.55f, TempoDeAviso[etapa]);

    protected enum Estado { Esperando, Lutando, Tonto, Derrotado }
    protected Estado estado;
    protected int vida, etapa;
    float vidaMostrada;
    bool emCombate, parado;
    protected float chao, esquerda, direita, meio;
    protected Transform corpo, efeitos;
    protected SpriteRenderer desenho;
    protected float timerPiscar;

    protected class Zona { public Vector2 a, b; public float raio; }
    protected readonly List<Zona> zonas = new List<Zona>();
    readonly List<GameObject> paredes = new List<GameObject>();
    readonly List<Transform> estrelas = new List<Transform>();

    const float MeiaLarguraDoJogador = 0.27f, MeiaAlturaDoJogador = 0.42f, FolgaDoAcerto = 0.15f;
    protected const int OrdemDoChefe = 8, OrdemDosAvisos = 55;

    public override int Vida => vida;
    public override float VidaMostrada => vidaMostrada;
    public override bool EmCombate => emCombate;
    public override int VidaMaxima => EtapasDaLuta * GolpesDaEtapa;
    public override int EtapaAtual => etapa;
    public override int Etapas => EtapasDaLuta;
    public override int GolpesPorEtapa => GolpesDaEtapa;
    public override Vector3 PontoDeRenascer => new Vector3(esquerda + 0.5f, chao + 0.5f, 0f);

    // ------------------------------------------------------------------ o que cada chefe diz

    protected abstract string SpriteNormal { get; }
    protected abstract string SpriteTonto { get; }
    protected abstract string ConquistaAoVencer { get; }
    protected abstract string FalaDeEntrada { get; }
    protected abstract string DicaDeEntrada { get; }
    protected abstract string FalaDaEtapa2 { get; }
    protected abstract string FalaDaEtapa3 { get; }
    protected abstract string FalaDaDerrota { get; }
    protected abstract float TopoDaCabeca { get; }       // acima do pé do desenho (tonto)
    protected abstract float MeiaLarguraDaCabeca { get; }
    protected virtual float Escala => 1f;                 // o desenho é pequeno: o chefe é desenhado maior
    protected abstract IEnumerator Rodada(int qual);      // 0..5 (etapa * 2 + rodada): termina chamando Tontear()

    // ------------------------------------------------------------------ Unity

    protected virtual void Awake()
    {
        chao = transform.position.y - 0.5f;
        esquerda = transform.position.x - 0.5f;
        direita = esquerda + larguraDaArena;
        meio = (esquerda + direita) / 2f;
        vida = VidaMaxima;
        vidaMostrada = vida;
        efeitos = new GameObject("Efeitos").transform;
        efeitos.SetParent(transform, false);
        desenho = ConstrutorDeFase.Visual("Chefe", transform, new Vector3(direita - 5f, chao, 0f), SpriteNormal, OrdemDoChefe).GetComponent<SpriteRenderer>();
        corpo = desenho.transform;
        desenho.flipX = true;
        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.Chefe = this;
    }

    void Start()
    {
        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        if (jogo == null) return;
        if (jogo.ChefeDerrotado)
        {
            estado = Estado.Derrotado;
            Desmaiado();
            return;
        }
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
        if (estado == Estado.Derrotado && !emCombate) return;
        if (estado == Estado.Esperando)
        {
            Respirar();
            if (GerenciadorDoJogo.JogadorVivo(out Vector2 entrando) && entrando.x > esquerda + 2f)
                StartCoroutine(Lutar(renasceu: false));
            return;
        }
        if (emCombate && !parado && !GerenciadorDoJogo.Pausado && !GerenciadorDoJogo.JogadorVivo(out _))
        {
            parado = true;
            StopAllCoroutines();
            zonas.Clear();
        }
        if (parado) return;

        Respirar();
        if (timerPiscar > 0f)
        {
            timerPiscar -= Time.deltaTime;
            desenho.color = (int)(timerPiscar * 16f) % 2 == 0 ? new Color(1f, 0.45f, 0.45f) : Color.white;
            if (timerPiscar <= 0f) desenho.color = Color.white;
        }
        GirarEstrelas();
        if (TestarPisao()) return;
        TestarPerigos();
    }

    // um respiro de leve (o desenho sobe e desce): o chefe parece vivo
    void Respirar()
    {
        if (estado == Estado.Derrotado) return;
        float k = estado == Estado.Tonto ? 0.03f : 0.06f;
        corpo.localScale = new Vector3(Escala * (1f + k * Mathf.Sin(Time.time * 2.4f)), Escala * (1f - k * Mathf.Sin(Time.time * 2.4f)), 1f);
    }

    // ------------------------------------------------------------------ a luta

    IEnumerator Lutar(bool renasceu)
    {
        estado = Estado.Lutando;
        emCombate = true;
        FecharArena();
        GerenciadorDoJogo.Instancia.PuxarParaAArena(esquerda, direita, PontoDeRenascer);
        CameraSeguir.Travar(esquerda - 1f, direita + 1f);
        GerenciadorDoJogo.Instancia.TocarMusicaDoChefe();
        GerenciadorDoJogo.Instancia.SalvarPonto(PontoDeRenascer);
        etapa = Mathf.Clamp(GerenciadorDoJogo.Instancia.EtapaDoChefe, 0, EtapasDaLuta - 1);
        vida = VidaMaxima - etapa * GolpesDaEtapa;
        vidaMostrada = vida;
        if (renasceu)
        {
            GerenciadorDoJogo.Instancia.Avisar(Idioma.T($"ETAPA {etapa + 1}/{EtapasDaLuta} - PRONTO?", $"STAGE {etapa + 1}/{EtapasDaLuta} - READY?"), 1.3f);
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            GerenciadorDoJogo.Som("armadilha");
            GerenciadorDoJogo.Instancia.Avisar(FalaDeEntrada, 2f);
            CameraSeguir.Tremer(0.12f, 0.5f);
            yield return new WaitForSeconds(1.2f);
            GerenciadorDoJogo.Instancia.Avisar(DicaDeEntrada, 2.8f);
            yield return new WaitForSeconds(1.6f);
        }
        yield return Combate();
    }

    protected override IEnumerator Combate()
    {
        while (true)
        {
            int vidaNoFimDaEtapa = VidaMaxima - (etapa + 1) * GolpesDaEtapa;
            while (vida > vidaNoFimDaEtapa)
            {
                int golpe = VidaMaxima - etapa * GolpesDaEtapa - vida;
                yield return RodarRodada(Rodada(RodadaDoGolpe(etapa, golpe)));
            }
            if (vida <= 0) break;
            etapa++;
            GerenciadorDoJogo.Instancia.EtapaDoChefe = etapa;
            timerPiscar = 0.8f;
            GerenciadorDoJogo.Instancia.Avisar(etapa == 1 ? FalaDaEtapa2 : FalaDaEtapa3, 2.2f);
            GerenciadorDoJogo.Som(etapa == 1 ? "armadilha" : "rugido", 0.7f);
            CameraSeguir.Tremer(0.2f, 0.7f);
            yield return new WaitForSeconds(1.4f);
        }
        yield return SerDerrotado();
    }

    IEnumerator SerDerrotado()
    {
        estado = Estado.Derrotado;
        zonas.Clear();
        ApagarEstrelas();
        Conquistas.Desbloquear(ConquistaAoVencer);
        GerenciadorDoJogo.Instancia.ChefeVencido(new Vector3(direita + 1.5f, chao + 0.5f, 0f));
        GerenciadorDoJogo.Instancia.Avisar(FalaDaDerrota, 3f);
        GerenciadorDoJogo.Som("pisao");
        CameraSeguir.Tremer(0.25f, 0.9f);
        timerPiscar = 1.2f;
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            corpo.rotation = Quaternion.Euler(0f, 0f, 90f * (desenho.flipX ? -1f : 1f) * (t / 0.6f));
            yield return null;
        }
        Desmaiado();
        AbrirArena();
        CameraSeguir.Destravar();
        emCombate = false;
    }

    void Desmaiado()
    {
        desenho.sprite = FabricaDeSprites.Pegar(SpriteTonto);
        corpo.rotation = Quaternion.Euler(0f, 0f, 90f * (desenho.flipX ? -1f : 1f));
        corpo.position = new Vector3(corpo.position.x, chao + 0.4f, 0f);
        corpo.localScale = Vector3.one * Escala;
    }

    // ------------------------------------------------------------------ a janela de dano

    // O fim de cada rodada: o chefe fica TONTO (estrelinhas) e dá para pisar na cabeça.
    protected IEnumerator Tontear()
    {
        estado = Estado.Tonto;
        desenho.sprite = FabricaDeSprites.Pegar(SpriteTonto);
        CriarEstrelas();
        GerenciadorDoJogo.Som("tunk", 0.6f);
        yield return new WaitForSeconds(TempoTonto[etapa]);
        ApagarEstrelas();
        desenho.sprite = FabricaDeSprites.Pegar(SpriteNormal);
        estado = Estado.Lutando;
        yield return new WaitForSeconds(0.3f);
    }

    bool TestarPisao()
    {
        if (!Vulneravel || estado != Estado.Tonto) return false;
        float cabeca = corpo.position.y + TopoDaCabeca;
        Jogador jogador = QuemPisou(corpo.position.x - MeiaLarguraDaCabeca, corpo.position.x + MeiaLarguraDaCabeca, cabeca);
        if (jogador == null) return false;
        vida--;
        timerPiscar = 0.8f;
        jogador.Quicar(14f, true);
        GerenciadorDoJogo.Som("pisao");
        GerenciadorDoJogo.Som("pancada", 0.6f);
        CameraSeguir.Tremer(0.2f, 0.3f);
        Efeitos.Poeira(transform, corpo.position + Vector3.up * TopoDaCabeca, 8, 3f);
        Golpeado();
        return true;
    }

    protected override void Arrumar()
    {
        zonas.Clear();
        foreach (Transform filho in efeitos) Destroy(filho.gameObject);
        estrelas.Clear();
        corpo.rotation = Quaternion.identity;
        if (estado == Estado.Tonto) estado = Estado.Lutando;
    }

    protected override IEnumerator Reagir()
    {
        desenho.sprite = FabricaDeSprites.Pegar(SpriteTonto);
        Vector3 de = corpo.position;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            corpo.position = new Vector3(de.x, Mathf.Lerp(de.y, chao, (t / 0.3f) * (t / 0.3f)), 0f);
            yield return null;
        }
        corpo.position = new Vector3(de.x, chao, 0f);
        if (vida <= 0) yield break;
        yield return new WaitForSeconds(0.7f);
        desenho.sprite = FabricaDeSprites.Pegar(SpriteNormal);
        OlharParaOJogador();
        yield return new WaitForSeconds(0.35f);
    }

    // ------------------------------------------------------------------ perigos

    void TestarPerigos()
    {
        foreach (Jogador jogador in GerenciadorDoJogo.Jogadores)
        {
            if (jogador == null || jogador.Morto || estado == Estado.Derrotado) continue;
            Vector2 p = (Vector2)jogador.transform.position + new Vector2(0f, -0.02f);
            bool encostou = false;
            for (int i = 0; i < zonas.Count && !encostou; i++)
                for (int k = -1; k <= 1 && !encostou; k++)
                {
                    Vector2 ponto = p + new Vector2(0f, k * (MeiaAlturaDoJogador - MeiaLarguraDoJogador));
                    if (DistanciaAoSegmento(ponto, zonas[i].a, zonas[i].b) < zonas[i].raio + MeiaLarguraDoJogador - FolgaDoAcerto) encostou = true;
                }
            if (encostou) jogador.Morrer();
        }
    }

    protected static float DistanciaAoSegmento(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float comprimento2 = Vector2.Dot(ab, ab);
        float t = comprimento2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / comprimento2) : 0f;
        return Vector2.Distance(p, a + ab * t);
    }

    protected Zona NovaZona(Vector2 a, Vector2 b, float raio)
    {
        var z = new Zona { a = a, b = b, raio = raio };
        zonas.Add(z);
        return z;
    }

    // ------------------------------------------------------------------ ajudantes dos ataques

    protected SpriteRenderer Desenho(string nome, string sprite, Vector3 lugar, int ordem) =>
        ConstrutorDeFase.Visual(nome, efeitos, lugar, sprite, ordem, false).GetComponent<SpriteRenderer>();

    protected static void Esticar(Transform objeto, Vector2 a, Vector2 b, float grossura)
    {
        Vector2 d = b - a;
        objeto.position = (a + b) / 2f;
        objeto.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        objeto.localScale = new Vector3(Mathf.Max(0.01f, d.magnitude) * FabricaDeSprites.PixelsPorUnidade, grossura, 1f);
    }

    protected static bool Piscando(float t, float total) => Mathf.Repeat(t * (total - t < 0.2f ? 22f : 9f), 1f) < 0.6f;

    // Onde está o Grumete-alvo (no coop, um de cada vez); sem ninguém, o meio da arena.
    protected float AlvoX()
    {
        if (!GerenciadorDoJogo.AlvoDoChefe(out Vector2 jogador)) return meio;
        return Mathf.Clamp(jogador.x, esquerda + 0.8f, direita - 0.8f);
    }

    protected void OlharParaOJogador()
    {
        if (GerenciadorDoJogo.AlvoDoChefe(out Vector2 j)) desenho.flipX = j.x < corpo.position.x;
    }

    // Uma marca no chão que pisca durante "tempo" (vermelha no fim).
    protected IEnumerator MarcarChao(float x, float tempo, float largura = 1f)
    {
        var marca = Desenho("Marca", "baleia_alvo", new Vector3(x, chao + 0.1f, 0f), OrdemDosAvisos);
        marca.transform.localScale = new Vector3(largura, 1f, 1f);
        for (float t = 0f; t < tempo; t += Time.deltaTime)
        {
            if (marca == null) yield break;
            marca.enabled = Piscando(t, tempo);
            yield return null;
        }
        if (marca != null) Destroy(marca.gameObject);
    }

    // Um pulo do chefe até x (arco), em "tempo" segundos. Durante a queda o corpo machuca.
    protected IEnumerator Saltar(float x, float altura, float tempo, float raioDoCorpo)
    {
        Vector3 de = corpo.position, ate = new Vector3(Mathf.Clamp(x, esquerda + 1.5f, direita - 1.5f), chao, 0f);
        desenho.flipX = ate.x < de.x;
        Zona z = null;
        for (float t = 0f; t < tempo; t += Time.deltaTime)
        {
            float p = t / tempo;
            corpo.position = Vector3.Lerp(de, ate, p) + Vector3.up * altura * 4f * p * (1f - p);
            if (p > 0.5f)
            {
                if (z == null) z = NovaZona(Vector2.zero, Vector2.zero, raioDoCorpo);
                z.a = z.b = (Vector2)corpo.position + Vector2.up * raioDoCorpo;
            }
            yield return null;
        }
        corpo.position = ate;
        if (z != null) zonas.Remove(z);
        GerenciadorDoJogo.Som("pancada", 0.8f);
        CameraSeguir.Tremer(0.2f, 0.25f);
        Efeitos.Poeira(transform, ate, 10, 3f);
    }

    // Uma onda rente ao chão que corre de "de" na direção "lado" até a parede (pule por cima).
    protected IEnumerator OndaNoChao(float de, float lado, float velocidade, float altura = 0.55f)
    {
        var desenhoDaOnda = Desenho("Onda", "onda_de_choque", new Vector3(de, chao + altura / 2f, 0f), OrdemDosAvisos - 1);
        desenhoDaOnda.flipX = lado < 0f;
        Zona z = NovaZona(Vector2.zero, Vector2.zero, altura / 2f);
        float x = de;
        while (x > esquerda && x < direita)
        {
            x += lado * velocidade * Time.deltaTime;
            Vector2 c = new Vector2(x, chao + altura / 2f);
            z.a = c + Vector2.left * 0.15f;
            z.b = c + Vector2.right * 0.15f;
            if (desenhoDaOnda != null) desenhoDaOnda.transform.position = c;
            yield return null;
        }
        zonas.Remove(z);
        if (desenhoDaOnda != null) Destroy(desenhoDaOnda.gameObject);
    }

    // Algo que despenca do teto em x (bigorna, água-viva): marca no chão, depois cai rápido e some no chão.
    protected IEnumerator CairDoTeto(float x, string sprite, float aviso, float raio = 0.45f)
    {
        yield return MarcarChao(x, aviso);
        float y = chao + 9f;
        var coisa = Desenho("Caindo", sprite, new Vector3(x, y, 0f), OrdemDoChefe + 1);
        Zona z = NovaZona(Vector2.zero, Vector2.zero, raio);
        float v = 6f;
        while (y > chao + raio)
        {
            v += 40f * Time.deltaTime;
            y -= v * Time.deltaTime;
            z.a = z.b = new Vector2(x, y);
            if (coisa != null) coisa.transform.position = new Vector3(x, y, 0f);
            yield return null;
        }
        zonas.Remove(z);
        GerenciadorDoJogo.Som("tunk", 0.5f);
        Efeitos.Poeira(transform, new Vector3(x, chao, 0f), 5, 2f);
        if (coisa != null) Destroy(coisa.gameObject);
    }

    // ------------------------------------------------------------------ estrelinhas e arena

    void CriarEstrelas()
    {
        ApagarEstrelas();
        for (int i = 0; i < 3; i++) estrelas.Add(Desenho("Estrela", "brilho", corpo.position, OrdemDosAvisos).transform);
    }

    void ApagarEstrelas()
    {
        foreach (Transform e in estrelas) if (e != null) Destroy(e.gameObject);
        estrelas.Clear();
    }

    void GirarEstrelas()
    {
        for (int i = 0; i < estrelas.Count; i++)
        {
            if (estrelas[i] == null) continue;
            float a = Time.time * 5f + i * Mathf.PI * 2f / 3f;
            estrelas[i].position = corpo.position + new Vector3(Mathf.Cos(a) * 0.8f, TopoDaCabeca + 0.35f + Mathf.Sin(a) * 0.15f, 0f);
        }
    }

    void FecharArena()
    {
        foreach (float x in new[] { esquerda - 0.5f, direita + 0.5f })
        {
            var parede = new GameObject("ParedeDaArena");
            parede.transform.SetParent(transform, false);
            parede.transform.position = new Vector3(x, chao + 7f, 0f);
            parede.AddComponent<BoxCollider2D>().size = new Vector2(1f, 20f);
            for (int linha = 0; linha < 13; linha++)
                ConstrutorDeFase.Visual("Coral", parede.transform, new Vector3(x, chao + 0.5f + linha, 0f), "tijolo", 12, false)
                    .GetComponent<SpriteRenderer>().color = new Color(0.65f, 0.75f, 1f);
            Efeitos.Poeira(transform, new Vector3(x, chao + 0.3f, 0f), 6, 3f);
            paredes.Add(parede);
        }
        GerenciadorDoJogo.Som("bloco");
    }

    void AbrirArena()
    {
        foreach (GameObject parede in paredes)
            if (parede != null) Destroy(parede);
        paredes.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarOnda()
    {
        FabricaDeSprites.Registrar("onda_de_choque", () => FabricaDeSprites.DeArte(new[]
        {
            "....ll....",
            "..llwwl...",
            ".lwwEEwl..",
            "lwEEEEEwl.",
            "EEEBBEEEEl",
            "BBBBBBBBBB",
        }, FabricaDeSprites.Centro));
    }
}
