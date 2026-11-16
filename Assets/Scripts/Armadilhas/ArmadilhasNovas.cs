using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mecânicas novas para as fases das ilhas (e as bônus). As letras:
//  )  CANHÃO DO NAVIO: entrou nele (andando, pela esquerda), ele te engole, mira (0,5 s) e DISPARA o Grumete em arco
//     para a direita (14 blocos/s para a frente, 13 para cima: uns 9 blocos de distância). No ar, o lado não é
//     controlado por meio segundo. O segundo canhão da fase às vezes mira num espinho :)
//  _  PRANCHA: tábua sobre o mar (blocos '_' lado a lado = uma prancha). Ficou em cima mais de 0,35 s, ela range,
//     tomba e você cai. Volta 1,2 s depois. Atravesse correndo (ou pulando de uma ponta para a outra).
//  !  BAÚ QUE MORDE (mímico): igualzinho ao baú do tesouro. Chegou a 2 blocos, ele chacoalha (0,35 s) e MORDE
//     para a frente (1,3 bloco). Pule na cabeça dele: ele cai e solta 3 moedas.
//  |  CLONE ESPELHADO: linha invisível. Cruzou, aparece um Grumete fantasma 12 blocos à frente que anda ESPELHADO
//     (você vai para a direita, ele vem para a esquerda) e copia os seus pulos 0,5 s DEPOIS. Encostou, morreu.
//     Pule quando ele estiver no chão (ele só pula depois). O trecho entre a linha e ele tem que ser plano.
public class Canhao : MonoBehaviour
{
    public Vector2 tiro = new Vector2(14f, 13f);
    SpriteRenderer desenho;
    bool ocupado;

    void Awake()
    {
        desenho = gameObject.AddComponent<SpriteRenderer>(); // (o construtor põe no chão da célula: pivô na base)
        desenho.sprite = FabricaDeSprites.Pegar("canhao");
        desenho.sortingOrder = 11; // na frente do Grumete (ele fica "dentro")
        Sombra.Adicionar(desenho);
    }

    void Update()
    {
        if (ocupado) return;
        foreach (Jogador j in GerenciadorDoJogo.Jogadores)
        {
            if (j == null || j.Morto || j.NaBolha) continue;
            Vector2 p = j.transform.position;
            if (Mathf.Abs(p.x - transform.position.x) < 0.45f && p.y > transform.position.y && p.y < transform.position.y + 1.4f)
            {
                StartCoroutine(Atirar(j));
                return;
            }
        }
    }

    IEnumerator Atirar(Jogador j)
    {
        ocupado = true;
        GerenciadorDoJogo.Som("bloco", 0.6f);
        j.Congelar();
        j.Teletransportar(transform.position + Vector3.up * 0.5f);
        j.Desenho.enabled = false;
        Vector3 origem = transform.position;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime) // mira: treme
        {
            transform.position = origem + new Vector3(Random.Range(-0.03f, 0.03f), 0f, 0f);
            yield return null;
        }
        transform.position = origem;
        if (j != null)
        {
            j.Desenho.enabled = true;
            j.Liberar();
            j.Teletransportar(transform.position + new Vector3(0.6f, 0.9f, 0f));
            j.Lancar(tiro);
        }
        GerenciadorDoJogo.Som("trovao", 0.5f);
        CameraSeguir.Tremer(0.15f, 0.2f);
        Efeitos.Poeira(transform.parent, transform.position + new Vector3(0.6f, 0.8f, 0f), 10, 3f);
        yield return new WaitForSeconds(0.8f);
        ocupado = false;
    }
}

public class Prancha : MonoBehaviour
{
    readonly List<Transform> tabuas = new List<Transform>();
    readonly List<Collider2D> colisores = new List<Collider2D>();
    float esquerda = float.MaxValue, direita = float.MinValue, topo;
    float timerEmCima;
    bool caida;

    public void AdicionarTabua(Vector3 posicao)
    {
        var tabua = ConstrutorDeFase.Visual("Tabua", transform, posicao + Vector3.up * 0.35f, "prancha", 2);
        var colisor = tabua.AddComponent<BoxCollider2D>();
        colisor.size = new Vector2(1f, 0.3f);
        tabuas.Add(tabua.transform);
        colisores.Add(colisor);
        esquerda = Mathf.Min(esquerda, posicao.x - 0.5f);
        direita = Mathf.Max(direita, posicao.x + 0.5f);
        topo = posicao.y + 0.5f;
    }

    void Update()
    {
        if (caida) return;
        bool alguem = false;
        foreach (Jogador j in GerenciadorDoJogo.Jogadores)
        {
            if (j == null || j.Morto || !j.NoChao) continue;
            Vector2 p = j.transform.position;
            if (p.x > esquerda - 0.2f && p.x < direita + 0.2f && p.y > topo && p.y < topo + 1.2f) alguem = true;
        }
        timerEmCima = alguem ? timerEmCima + Time.deltaTime : 0f;
        // range enquanto alguém está em cima
        float tremida = alguem ? Mathf.Sin(Time.time * 60f) * 2f * timerEmCima : 0f;
        foreach (Transform t in tabuas) t.localRotation = Quaternion.Euler(0f, 0f, tremida);
        if (timerEmCima > 0.35f) StartCoroutine(Tombar());
    }

    IEnumerator Tombar()
    {
        caida = true;
        GerenciadorDoJogo.Som("madeira", 0.7f);
        foreach (Collider2D c in colisores) c.enabled = false;
        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            foreach (Transform tb in tabuas) tb.localRotation = Quaternion.Euler(0f, 0f, -70f * t / 0.2f);
            yield return null;
        }
        yield return new WaitForSeconds(1.2f);
        foreach (Transform tb in tabuas) tb.localRotation = Quaternion.identity;
        foreach (Collider2D c in colisores) c.enabled = true;
        timerEmCima = 0f;
        caida = false;
    }
}

public class Mimico : MonoBehaviour
{
    SpriteRenderer desenho;
    bool ocupado, morto;
    GameObject mordida;

    void Awake()
    {
        desenho = gameObject.AddComponent<SpriteRenderer>(); // (no chão da célula, como o baú do fim)
        desenho.sprite = FabricaDeSprites.Pegar("mapa_bau");
        desenho.sortingOrder = 3;
        transform.localScale = Vector3.one * 2f; // do tamanho do baú do fim da última fase
        Sombra.Adicionar(desenho);
    }

    void Update()
    {
        if (ocupado || morto) return;
        // pisão: alguém caindo em cima
        foreach (Jogador j in GerenciadorDoJogo.Jogadores)
        {
            if (j == null || j.Morto) continue;
            Vector2 p = j.transform.position;
            if (j.Corpo.linearVelocity.y < -0.5f && Mathf.Abs(p.x - transform.position.x) < 0.75f && p.y > transform.position.y + 0.9f && p.y < transform.position.y + 1.7f)
            {
                j.Quicar(10f, true);
                StartCoroutine(Derrotado(j));
                return;
            }
        }
        for (int i = 0; GerenciadorDoJogo.ProximoVivo(ref i, out Vector2 jogador);)
            if (Mathf.Abs(jogador.x - transform.position.x) < 2f && Mathf.Abs(jogador.y - (transform.position.y + 0.5f)) < 1f)
            {
                StartCoroutine(Morder(Mathf.Sign(jogador.x - transform.position.x)));
                return;
            }
    }

    IEnumerator Morder(float lado)
    {
        ocupado = true;
        Vector3 origem = transform.position;
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            transform.position = origem + new Vector3(Random.Range(-0.05f, 0.05f), Mathf.Abs(Mathf.Sin(t * 40f)) * 0.08f, 0f);
            yield return null;
        }
        transform.position = origem;
        if (morto) yield break;
        GerenciadorDoJogo.Som("risada", 0.6f);
        mordida = ConstrutorDeFase.Visual("Mordida", transform.parent, origem + new Vector3(lado * 0.9f, 0.5f, 0f), "mimico_dentes", 12, false);
        mordida.GetComponent<SpriteRenderer>().flipX = lado < 0f;
        Perigo.Adicionar(mordida, new Vector2(1.3f, 0.8f), Vector2.zero);
        Perigo.TornarMovel(mordida);
        yield return new WaitForSeconds(0.3f);
        Destroy(mordida);
        yield return new WaitForSeconds(0.7f);
        ocupado = false;
    }

    IEnumerator Derrotado(Jogador quem)
    {
        morto = true;
        if (mordida != null) Destroy(mordida);
        GerenciadorDoJogo.Som("pisao");
        Conquistas.Contar("caranguejos", 10); // (pisão conta como pescar)
        Desafios.Pisoes++;
        for (int i = 0; i < 3; i++)
            StartCoroutine(Efeitos.MoedaSaltando(transform.parent, transform.position + new Vector3(-0.4f + i * 0.4f, 1f, 0f)));
        // (vão direto para o bolso: não contam como moedas da fase, senão o baú "completaria" o segredo da fase 3)
        GerenciadorDoJogo.Instancia?.GanharMoedasDoBau(3);
        GerenciadorDoJogo.Som("moeda");
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            transform.localScale = new Vector3(2f, 2f * (1f - t / 0.3f * 0.6f), 1f);
            yield return null;
        }
        Destroy(gameObject, 0.4f);
    }
}

public class CloneEspelhado : MonoBehaviour
{
    public float distancia = 12f;
    public float atraso = 0.5f;
    bool acordou;
    Jogador alvo;
    Transform clone;
    SpriteRenderer desenho;
    float chao;
    readonly Queue<(float t, float y)> pulos = new Queue<(float, float)>();

    void Update()
    {
        if (!acordou)
        {
            for (int i = 0; GerenciadorDoJogo.ProximoVivo(ref i, out Vector2 p, out Jogador j);)
                if (p.x > transform.position.x && p.x < transform.position.x + 1f)
                {
                    Acordar(j);
                    break;
                }
            return;
        }
        if (clone == null) return;
        if (alvo == null || alvo.Morto)
        {
            Destroy(clone.gameObject);
            return;
        }
        // espelhado em volta do meio do trecho: você vai para a direita, ele vem para a esquerda
        float meio = transform.position.x + distancia / 2f;
        float x = 2f * meio - alvo.transform.position.x;
        // a altura dele é a sua de 0,5 s atrás (ele copia o pulo depois)
        float altura = alvo.transform.position.y - chao;
        pulos.Enqueue((Time.time, altura));
        float alturaDele = 0f;
        while (pulos.Count > 0 && pulos.Peek().t <= Time.time - atraso) alturaDele = pulos.Dequeue().y;
        clone.position = new Vector3(x, chao + Mathf.Max(0f, alturaDele), 0f);
        desenho.flipX = !alvo.Desenho.flipX;
        desenho.sprite = alvo.Desenho.sprite;
        // você passou dele: ele some
        if (alvo.transform.position.x > x + 2.5f)
        {
            Efeitos.Poeira(transform.parent, clone.position, 8, 2f);
            Destroy(clone.gameObject);
        }
    }

    void Acordar(Jogador j)
    {
        acordou = true;
        alvo = j;
        chao = j.transform.position.y;
        GerenciadorDoJogo.Som("risada", 0.7f);
        GerenciadorDoJogo.Instancia?.Avisar(Idioma.T("Quem é aquele?", "Who is that?"), 1.4f);
        var objeto = new GameObject("CloneEspelhado");
        objeto.transform.SetParent(transform.parent, false);
        desenho = objeto.AddComponent<SpriteRenderer>();
        desenho.sortingOrder = 10;
        desenho.color = new Color(0.55f, 0.35f, 0.75f, 0.85f); // o Grumete roxo
        Perigo.Adicionar(objeto, new Vector2(0.55f, 0.8f), Vector2.zero);
        Perigo.TornarMovel(objeto);
        clone = objeto.transform;
        clone.position = new Vector3(transform.position.x + distancia, chao, 0f);
        Efeitos.Poeira(transform.parent, clone.position, 8, 2f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("canhao", () => FabricaDeSprites.DeArte(ArteCanhao, FabricaDeSprites.Base));
        FabricaDeSprites.Registrar("prancha", () => FabricaDeSprites.DeArte(ArtePrancha, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("mimico_dentes", () => FabricaDeSprites.DeArte(ArteDentes, FabricaDeSprites.Centro));
    }

    static readonly string[] ArteCanhao =
    {
        "..........kkkkkk",
        "........kkSSSSSk",
        "......kkSSsSSSSk",
        "....kkSSSSSSSSSk",
        "...kSSSSSSSSSkk.",
        "..kSSSSSSSSkk...",
        ".kSSsSSSSSk.....",
        "kSSSSSSSSSk.....",
        "kSSSSSSSSk......",
        "kkSSSSSSk.......",
        ".kbbbbbbbbbbk...",
        "kbdkkdbbdkkdbk..",
        "kbkddkbbkddkbk..",
        "kbdkkdbbdkkdbk..",
        ".kkkkkkkkkkkk...",
        "................",
    };

    static readonly string[] ArtePrancha =
    {
        "kkkkkkkkkkkkkkkk",
        "nnnnnnbnnnnnnnnb",
        "bbbbbbdbbbbbbbbd",
        "kkkkkkkkkkkkkkkk",
    };

    static readonly string[] ArteDentes =
    {
        "kkkkkkkkkkkkkkkkkkkk",
        "kwkwkwkwkwkwkwkwkwkk",
        ".kwkwkwkwkwkwkwkwk..",
        "..........rrrrrr....",
        ".kwkwkwkwkwkwkwkwk..",
        "kwkwkwkwkwkwkwkwkwkk",
        "kkkkkkkkkkkkkkkkkkkk",
    };
}
