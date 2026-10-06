using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 'w' - a BALEIA BRANCA, chefe da última fase (Re:Zero).
// O 'w' marca o começo da arena (na linha em que o jogador anda). Quando você entra, paredes de névoa
// fecham a arena, a câmera trava, toca a música de chefe e a luta começa. Ela tem 3 de vida e repete:
//  1) INVESTIDA: atravessa a tela. Alta = não pule; baixa = pule por cima. Um "!" mostra a altura.
//  2) CHUVA DE NÉVOA: bolas de névoa caem do céu; uma sombra no chão mostra onde vão cair.
//  3) MERGULHO: ela cai do céu em cima de você (fuja da sombra!) e fica ATORDOADA no chão.
//     É a hora de PULAR NA CABEÇA dela. Cada pulo tira 1 de vida.
// A cada golpe ela fica mais rápida e ataca mais. Derrotada, a névoa some e o caminho abre.
// Se você morrer DEPOIS de vencer, renasce depois da arena (não precisa lutar de novo).
public class BaleiaBranca : MonoBehaviour
{
    public int vidaMaxima = 3;
    public float larguraDaArena = 26f;
    public float velocidade = 15f;        // investida
    public float tamanho = 1.3f;          // escala do desenho quando ela está na frente
    public float alturaAlta = 2.6f;       // centro da baleia na investida alta, medido a partir do chão
    public float alturaBaixa = 0.7f;
    public float tempoAtordoada = 2.6f;

    public int Vida { get; private set; }
    public float VidaMostrada { get; private set; } // a barra de vida "escorre" até a vida de verdade
    public bool EmCombate { get; private set; }

    enum Estado { Esperando, Lutando, Atordoada, Derrotada }
    Estado estado;
    float chao, inicio, fim;
    float timerAtordoada, timerPiscar;

    Transform baleia;
    SpriteRenderer desenho;
    Perigo perigo;
    SpriteRenderer aviso;
    readonly List<GameObject> paredes = new List<GameObject>();
    readonly List<Transform> estrelas = new List<Transform>();

    void Awake()
    {
        chao = transform.position.y - 0.5f;
        inicio = transform.position.x;
        fim = inicio + larguraDaArena;
        Vida = vidaMaxima;
        VidaMostrada = Vida;

        var objeto = ConstrutorDeFase.Visual("Baleia", transform, transform.position, "baleia", 8, false);
        baleia = objeto.transform;
        desenho = objeto.GetComponent<SpriteRenderer>();
        desenho.enabled = false;
        // área que mata: um pouco menor que o desenho, para ser justo
        perigo = Perigo.Adicionar(objeto, new Vector2(2.55f, 0.85f), new Vector2(-0.1f, -0.06f));
        Perigo.TornarMovel(objeto);
        perigo.ativo = false;

        aviso = ConstrutorDeFase.Visual("Aviso", transform, transform.position, "aviso", 20, false).GetComponent<SpriteRenderer>();
        aviso.enabled = false;

        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.Chefe = this;
    }

    void Start()
    {
        // já venceu nesta fase (morreu depois)? Então ela não volta.
        if (GerenciadorDoJogo.Instancia.ChefeDerrotado) estado = Estado.Derrotada;
    }

    void OnDestroy()
    {
        if (GerenciadorDoJogo.Instancia != null && GerenciadorDoJogo.Instancia.Chefe == this)
            GerenciadorDoJogo.Instancia.Chefe = null;
    }

    void Update()
    {
        VidaMostrada = Mathf.MoveTowards(VidaMostrada, Vida, 1.5f * Time.deltaTime);

        // piscando depois de levar um golpe
        if (timerPiscar > 0f)
        {
            timerPiscar -= Time.deltaTime;
            desenho.color = (int)(timerPiscar * 16f) % 2 == 0 ? new Color(1f, 0.45f, 0.45f) : Color.white;
            if (timerPiscar <= 0f) desenho.color = Color.white;
        }

        switch (estado)
        {
            case Estado.Esperando:
                if (GerenciadorDoJogo.JogadorVivo(out Vector2 jogador) && jogador.x > inicio + 1.5f)
                    StartCoroutine(Lutar());
                break;

            case Estado.Atordoada:
                timerAtordoada -= Time.deltaTime;
                GirarEstrelas();
                TestarPisao();
                break;
        }
    }

    // ------------------------------------------------------------------ a luta

    IEnumerator Lutar()
    {
        estado = Estado.Lutando;
        EmCombate = true;
        FecharArena();
        CameraSeguir.Travar(inicio - 1f, fim + 1f);
        GerenciadorDoJogo.Instancia.TocarMusicaDoChefe();
        GerenciadorDoJogo.Som("rugido");
        CameraSeguir.Tremer(0.12f, 1f);
        GerenciadorDoJogo.Instancia.Avisar("A BALEIA BRANCA!", 2.5f);

        yield return NadarNoFundo(2.5f);
        GerenciadorDoJogo.Instancia.Avisar("Pule na cabeça dela quando ela cair!", 3f);

        while (Vida > 0)
        {
            int raiva = vidaMaxima - Vida;     // 0, 1, 2: quanto mais apanha, mais brava
            float furia = 1f + raiva * 0.3f;

            for (int i = 0; i <= raiva; i++)    // 1, 2 e depois 3 investidas
            {
                yield return Investida(Random.value < 0.5f, furia);
                if (!Ativa()) yield break;
                yield return new WaitForSeconds(0.5f / furia);
            }

            yield return ChuvaDeNevoa(5 + raiva * 3, furia);
            if (!Ativa()) yield break;

            yield return Mergulho(furia);
            if (!Ativa()) yield break;
            yield return new WaitForSeconds(0.6f);
        }

        yield return SerDerrotada();
    }

    // Se o jogador morreu, ela some (a fase vai recomeçar). Na pausa ela só espera.
    bool Ativa()
    {
        if (GerenciadorDoJogo.Pausado || GerenciadorDoJogo.JogadorVivo(out _)) return true;
        perigo.ativo = false;
        desenho.enabled = false;
        aviso.enabled = false;
        return false;
    }

    // Nadando lá no fundo, pequena e meio transparente (parece longe), atravessando o céu.
    IEnumerator NadarNoFundo(float duracao)
    {
        desenho.enabled = true;
        desenho.sortingOrder = -23; // entre o castelo e a floresta
        desenho.color = new Color(1f, 1f, 1f, 0.55f);
        desenho.flipX = true;       // olhando para a direita
        baleia.localScale = Vector3.one * 0.6f;
        perigo.ativo = false;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            if (!Ativa()) yield break;
            float x = Mathf.Lerp(inicio - 3f, fim + 3f, t / duracao);
            baleia.position = new Vector3(x, chao + 8f + Mathf.Sin(t * 2f) * 0.3f, 0f);
            yield return null;
        }
        desenho.enabled = false;
        desenho.color = Color.white;
    }

    // Atravessa a arena da direita para a esquerda, alta ou baixa, com um "!" avisando a altura.
    IEnumerator Investida(bool alta, float furia)
    {
        float y = chao + (alta ? alturaAlta : alturaBaixa);
        GerenciadorDoJogo.Instancia.Avisar(alta ? "NÃO PULA!" : "PULA!", 1.3f);
        for (float t = 0f; t < 0.9f / furia + 0.2f; t += Time.deltaTime)
        {
            if (!Ativa()) yield break;
            Camera cam = Camera.main;
            aviso.enabled = (t * 8f) % 2f < 1.2f;
            aviso.transform.position = new Vector3(cam.transform.position.x + cam.orthographicSize * cam.aspect - 0.8f, y, 0f);
            yield return null;
        }
        aviso.enabled = false;

        desenho.enabled = true;
        desenho.sortingOrder = 8;
        desenho.flipX = false; // olhando para a esquerda, para onde vai
        baleia.localScale = Vector3.one * tamanho;
        perigo.ativo = true;
        GerenciadorDoJogo.Som("rugido", 0.5f);
        CameraSeguir.Tremer(0.08f, 0.5f);
        for (float x = fim + 4f; x > inicio - 5f; x -= velocidade * furia * Time.deltaTime)
        {
            if (!Ativa()) yield break;
            baleia.position = new Vector3(x, y, 0f);
            yield return null;
        }
        perigo.ativo = false;
        desenho.enabled = false;
    }

    // Ela fica lá no fundo e "cospe" bolas de névoa que caem na arena (algumas mirando em você).
    IEnumerator ChuvaDeNevoa(int quantas, float furia)
    {
        GerenciadorDoJogo.Instancia.Avisar("CHUVA DE NÉVOA!", 1.5f);
        desenho.enabled = true;
        desenho.sortingOrder = -23;
        desenho.color = new Color(1f, 1f, 1f, 0.55f);
        desenho.flipX = true;
        baleia.localScale = Vector3.one * 0.6f;
        float meio = (inicio + fim) / 2f;

        for (int i = 0; i < quantas; i++)
        {
            if (!Ativa()) yield break;
            baleia.position = new Vector3(meio + Mathf.Sin(Time.time) * 6f, chao + 8f, 0f);
            float x = i % 3 == 0 && GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)
                ? jogador.x
                : Random.Range(inicio + 1f, fim - 1f);
            StartCoroutine(GotaDeNevoa(Mathf.Clamp(x, inicio + 0.5f, fim - 0.5f), furia));
            yield return new WaitForSeconds(0.4f / furia);
        }
        yield return new WaitForSeconds(1.4f);
        desenho.enabled = false;
        desenho.color = Color.white;
    }

    IEnumerator GotaDeNevoa(float x, float furia)
    {
        // primeiro a sombra no chão (cresce), depois a bola cai
        var sombra = ConstrutorDeFase.Visual("SombraDaGota", transform, new Vector3(x, chao + 0.05f, 0f), "sombra_nuvem", 3, false);
        var desenhoSombra = sombra.GetComponent<SpriteRenderer>();
        float espera = 0.8f / furia;
        for (float t = 0f; t < espera; t += Time.deltaTime)
        {
            float p = t / espera;
            sombra.transform.localScale = new Vector3(0.1f + 0.25f * p, 0.1f + 0.2f * p, 1f);
            desenhoSombra.color = new Color(0.2f, 0f, 0.3f, 0.2f + 0.3f * p);
            yield return null;
        }

        var gota = ConstrutorDeFase.Visual("GotaDeNevoa", transform, new Vector3(x, chao + 11f, 0f), "nevoa", 9, false);
        gota.transform.localScale = Vector3.one * 0.8f;
        gota.GetComponent<SpriteRenderer>().color = new Color(0.85f, 0.7f, 1f);
        Perigo.Adicionar(gota, new Vector2(0.6f, 0.6f), Vector2.zero);
        Perigo.TornarMovel(gota);
        float velocidadeDaGota = 0f;
        while (gota.transform.position.y > chao + 0.35f)
        {
            velocidadeDaGota += 40f * Time.deltaTime;
            gota.transform.position += Vector3.down * velocidadeDaGota * Time.deltaTime;
            gota.transform.Rotate(0f, 0f, 200f * Time.deltaTime);
            yield return null;
        }
        Efeitos.Poeira(transform, gota.transform.position, 5, 2f);
        Destroy(gota);
        Destroy(sombra);
    }

    // Cai do céu em cima do jogador (a sombra segue você e depois para) e fica atordoada no chão.
    IEnumerator Mergulho(float furia)
    {
        GerenciadorDoJogo.Instancia.Avisar("CUIDADO EM CIMA!", 1.5f);
        float x = (inicio + fim) / 2f;
        var sombra = ConstrutorDeFase.Visual("SombraDaBaleia", transform, new Vector3(x, chao + 0.05f, 0f), "sombra_nuvem", 3, false);
        var desenhoSombra = sombra.GetComponent<SpriteRenderer>();
        float mirando = 1.4f / furia;
        for (float t = 0f; t < mirando + 0.35f; t += Time.deltaTime)
        {
            if (!Ativa()) { Destroy(sombra); yield break; }
            if (t < mirando && GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) // segue o jogador; no fim, para
                x = Mathf.MoveTowards(x, Mathf.Clamp(jogador.x, inicio + 3f, fim - 3f), 9f * Time.deltaTime);
            float p = Mathf.Clamp01(t / (mirando + 0.35f));
            sombra.transform.position = new Vector3(x, chao + 0.05f, 0f);
            sombra.transform.localScale = new Vector3(0.4f + 0.8f * p, 0.25f + 0.35f * p, 1f);
            desenhoSombra.color = new Color(0f, 0f, 0f, 0.15f + 0.35f * p);
            yield return null;
        }

        // despenca de barriga
        desenho.enabled = true;
        desenho.sortingOrder = 8;
        desenho.color = Color.white;
        desenho.flipX = Random.value < 0.5f;
        baleia.localScale = Vector3.one * tamanho;
        perigo.ativo = true;
        float y = chao + 12f, alvo = chao + 0.95f, queda = 0f;
        while (y > alvo)
        {
            if (!Ativa()) { Destroy(sombra); yield break; }
            queda += 70f * Time.deltaTime;
            y = Mathf.Max(alvo, y - queda * Time.deltaTime);
            baleia.position = new Vector3(x, y, 0f);
            yield return null;
        }
        Destroy(sombra);
        GerenciadorDoJogo.Som("pancada");
        GerenciadorDoJogo.Som("rugido", 0.6f);
        CameraSeguir.Tremer(0.3f, 0.4f);
        Efeitos.Poeira(transform, new Vector3(x - 2f, chao + 0.2f, 0f), 10, 3f);
        Efeitos.Poeira(transform, new Vector3(x + 2f, chao + 0.2f, 0f), 10, 3f);

        // atordoada: não machuca, e a cabeça dela vira um "botão"
        perigo.ativo = false;
        estado = Estado.Atordoada;
        timerAtordoada = tempoAtordoada / Mathf.Sqrt(furia);
        CriarEstrelas();
        while (estado == Estado.Atordoada && timerAtordoada > 0f)
        {
            if (!Ativa()) yield break;
            yield return null;
        }
        if (estado == Estado.Atordoada) estado = Estado.Lutando;
        ApagarEstrelas();
        if (Vida <= 0) yield break; // levou o último golpe: fica no chão para a cena da derrota

        // vai embora para cima
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            baleia.position += Vector3.up * 20f * Time.deltaTime;
            yield return null;
        }
        desenho.enabled = false;
    }

    // Pulou na cabeça dela (caindo, por cima)?
    void TestarPisao()
    {
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (jogador == null || jogador.Morto || jogador.Corpo.linearVelocity.y > 0.1f) return;
        Vector3 pes = jogador.transform.position + Vector3.down * 0.45f;
        float topo = baleia.position.y + 0.55f * tamanho;
        if (Mathf.Abs(pes.x - baleia.position.x) < 2f && pes.y > topo - 0.4f && pes.y < topo + 0.7f)
            LevarGolpe(jogador);
    }

    void LevarGolpe(Jogador jogador)
    {
        Vida--;
        estado = Vida > 0 ? Estado.Lutando : Estado.Derrotada;
        timerPiscar = 0.8f;
        jogador.Quicar(13f, true);
        GerenciadorDoJogo.Som("pisao");
        GerenciadorDoJogo.Som("rugido", 0.8f);
        CameraSeguir.Tremer(0.2f, 0.3f);
        Efeitos.Poeira(transform, baleia.position + Vector3.up * 0.8f, 8, 3f);
        string[] falas = { "", "Mais uma! Ela tá fraca!", "Acertou! Ela ficou BRAVA!" };
        if (Vida > 0) GerenciadorDoJogo.Instancia.Avisar(falas[Mathf.Clamp(Vida, 0, falas.Length - 1)], 2f);
    }

    IEnumerator SerDerrotada()
    {
        estado = Estado.Derrotada;
        ApagarEstrelas();
        perigo.ativo = false;
        GerenciadorDoJogo.Instancia.Avisar("A BALEIA BRANCA FOI DERROTADA!", 3f);
        Conquistas.Desbloquear("baleia");

        // treme, pisca e afunda girando
        GerenciadorDoJogo.Som("rugido");
        CameraSeguir.Tremer(0.25f, 1.2f);
        timerPiscar = 1.2f;
        Vector3 de = baleia.position;
        for (float t = 0f; t < 2f; t += Time.deltaTime)
        {
            float p = t / 2f;
            baleia.position = de + Vector3.down * p * p * 6f + (Vector3)(Random.insideUnitCircle * 0.1f * (1f - p));
            baleia.rotation = Quaternion.Euler(0f, 0f, p * 40f);
            desenho.color = new Color(1f, 1f, 1f, 1f - p);
            if (Random.value < 0.3f) Efeitos.Poeira(transform, baleia.position + (Vector3)(Random.insideUnitCircle * 2f), 3, 3f);
            yield return null;
        }
        desenho.enabled = false;

        AbrirArena();
        CameraSeguir.Destravar();
        EmCombate = false;
        // se morrer daqui pra frente, renasce depois da arena
        GerenciadorDoJogo.Instancia.ChefeVencido(new Vector3(fim + 1.5f, chao + 0.5f, 0f));
    }

    // ------------------------------------------------------------------ arena e efeitos

    // Paredes de névoa nas duas pontas da arena (colisor de verdade + bolotas de névoa).
    void FecharArena()
    {
        foreach (float x in new[] { inicio - 0.5f, fim + 0.5f })
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
            estrelas.Add(ConstrutorDeFase.Visual("Estrela", transform, baleia.position, "brilho", 10, false).transform);
    }

    void GirarEstrelas()
    {
        for (int i = 0; i < estrelas.Count; i++)
        {
            float angulo = Time.time * 5f + i * 2.1f;
            estrelas[i].position = baleia.position + new Vector3(Mathf.Cos(angulo) * 1.2f, 1.4f + Mathf.Sin(angulo) * 0.3f, 0f);
        }
    }

    void ApagarEstrelas()
    {
        foreach (Transform estrela in estrelas) if (estrela != null) Destroy(estrela.gameObject);
        estrelas.Clear();
    }
}
