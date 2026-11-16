using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// AS MECÂNICAS DO MAR DE BAIXO (o mundo 2) e a maré das ilhas. As letras:
//  ~  ÁGUA: células '~' encostadas = um "tanque" (o retângulo em volta delas). Dentro dele o Grumete NADA
//     (JogadorNaAgua.cs): braçadas, queda lenta e 8 segundos de ar com a cabeça debaixo d'água.
//  -  MARÉ: igual à água, mas o nível SOBE e DESCE (6 s: baixa 2,5 s, sobe 1 s, cheia 1,5 s, desce 1 s).
//     Na maré baixa a água fica só na linha de baixo do retângulo.
//  @  BOLHA DE AR: encostou, o ar enche todo. Volta 3 s depois.
//  %  BOLHA FALSA: igualzinha à '@'... estoura e não dá ar nenhum (e não volta).
//  f  PEIXE-LANTERNA: no escuro só aparece a luz, igual a uma moeda boiando. Chegou perto (1,6 bloco),
//     a luz pisca vermelho (0,35 s) e a boca abre e avança (0,3 s). É a boca que mata. Passe longe ou rápido.
//  =  CORRENTEZA: zona (como o vento 'y') que empurra para a ESQUERDA, sem parar, 4 blocos/s. Dentro d'água
//     o Grumete nada a 5: dá para vencer, devagar. Os risquinhos mostram a força.
//  x  OSTRA: plataforma de 2 blocos (a letra marca a ponta esquerda). Pisou, 0,5 s depois ela treme
//     (os dentes aparecem) e 0,3 s depois FECHA: quem estiver em cima morre. Abre de novo 1,2 s depois.
//  z  ÁGUA-VIVA: boia de leve; cair em cima dela quica alto (força 16).
//  &  ÁGUA-VIVA QUE QUEIMA: igualzinha à 'z'... mas encostou, morreu. Decore qual é qual.
public class AguaDoMar : MonoBehaviour
{
    static readonly List<AguaDoMar> ativas = new List<AguaDoMar>();

    Rect area;          // o retângulo da água (em blocos), com o topo da zona inteira
    bool mare;
    float topoAtual;    // a superfície agora
    float tempo;
    SpriteRenderer agua, espuma;

    const float CicloDaMare = 6f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => ativas.Clear();

    // O ponto está dentro de alguma água? (e onde fica a superfície dela)
    public static bool Dentro(Vector2 ponto, out float superficie)
    {
        foreach (AguaDoMar a in ativas)
        {
            if (a == null) continue;
            if (ponto.x >= a.area.xMin && ponto.x <= a.area.xMax && ponto.y >= a.area.yMin && ponto.y <= a.topoAtual)
            {
                superficie = a.topoAtual;
                return true;
            }
        }
        superficie = 0f;
        return false;
    }

    public static void Criar(Transform raiz, List<Vector2Int> celulas, int altura, bool ehMare)
    {
        int xMin = int.MaxValue, xMax = int.MinValue, lMin = int.MaxValue, lMax = int.MinValue;
        foreach (Vector2Int c in celulas)
        {
            xMin = Mathf.Min(xMin, c.x); xMax = Mathf.Max(xMax, c.x);
            lMin = Mathf.Min(lMin, c.y); lMax = Mathf.Max(lMax, c.y);
        }
        var objeto = new GameObject(ehMare ? "Mare" : "Agua");
        objeto.transform.SetParent(raiz, false);
        var agua = objeto.AddComponent<AguaDoMar>();
        // (linha 0 do texto é o topo: o y do mundo da linha l é altura - 1 - l)
        float baixo = altura - 1 - lMax - 0.5f, cima = altura - 1 - lMin + 0.5f;
        agua.area = new Rect(xMin - 0.5f, baixo, xMax - xMin + 1f, cima - baixo);
        agua.mare = ehMare;
        agua.topoAtual = ehMare ? baixo + 1f : cima;
        agua.Montar();
        ativas.Add(agua);
    }

    void Montar()
    {
        agua = ConstrutorDeFase.Visual("Agua", transform, Vector3.zero, "agua_do_mar", 22, sombra: false).GetComponent<SpriteRenderer>();
        agua.color = new Color(0.25f, 0.55f, 0.95f, 0.38f);
        espuma = ConstrutorDeFase.Visual("Superficie", transform, Vector3.zero, "superficie_do_mar", 23, sombra: false).GetComponent<SpriteRenderer>();
        Ajustar();
    }

    void OnDestroy() => ativas.Remove(this);

    void Update()
    {
        if (!mare) return;
        tempo += Time.deltaTime;
        float t = tempo % CicloDaMare, k;
        if (t < 2.5f) k = 0f;
        else if (t < 3.5f) k = Mathf.SmoothStep(0f, 1f, t - 2.5f);
        else if (t < 5f) k = 1f;
        else k = Mathf.SmoothStep(1f, 0f, t - 5f);
        topoAtual = Mathf.Lerp(area.yMin + 1f, area.yMax, k);
        if (t >= 2.4f && t < 2.4f + Time.deltaTime) GerenciadorDoJogo.Som("onda", 0.25f); // (o barulho da maré subindo)
        Ajustar();
    }

    void Ajustar()
    {
        float alto = Mathf.Max(0.05f, topoAtual - area.yMin);
        agua.transform.position = new Vector3(area.center.x, area.yMin + alto / 2f, 0f);
        agua.transform.localScale = new Vector3(area.width, alto, 1f);
        espuma.transform.position = new Vector3(area.center.x, topoAtual - 0.05f, 0f);
        espuma.transform.localScale = new Vector3(area.width, 1f, 1f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        // um quadrado de 1x1 bloco (esticado até o tamanho da água)
        FabricaDeSprites.Registrar("agua_do_mar", () => FabricaDeSprites.Procedural(4, 4, FabricaDeSprites.Centro,
            (x, y) => new Color32(255, 255, 255, 255), 4));
        // a linha clara da superfície, com ondinhas
        FabricaDeSprites.Registrar("superficie_do_mar", () => FabricaDeSprites.Procedural(16, 3, FabricaDeSprites.Centro,
            (x, y) => y == 1 + (x / 4 % 2) ? new Color32(220, 240, 255, 200) : y == 0 ? new Color32(120, 190, 255, 110) : FabricaDeSprites.Transparente, 16));
        FabricaDeSprites.Registrar("bolha_ar", () => FabricaDeSprites.DeArte(ArteBolha, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("peixe_lanterna", () => FabricaDeSprites.DeArte(ArtePeixe, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("peixe_lanterna_boca", () => FabricaDeSprites.DeArte(ArtePeixeBoca, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("ostra", () => FabricaDeSprites.DeArte(ArteOstra, FabricaDeSprites.Base));
        FabricaDeSprites.Registrar("ostra_fechada", () => FabricaDeSprites.DeArte(ArteOstraFechada, FabricaDeSprites.Base));
        FabricaDeSprites.Registrar("agua_viva", () => FabricaDeSprites.DeArte(ArteAguaViva, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("risco_correnteza", () => FabricaDeSprites.Procedural(8, 1, FabricaDeSprites.Centro,
            (x, y) => new Color32(220, 240, 255, (byte)(40 + x * 22)), 16));
    }

    static readonly string[] ArteBolha =
    {
        "...llll...",
        "..lwwEEl..",
        ".lwwEEEEl.",
        "lwwEEEEEEl",
        "lwEEEEEEEl",
        "lEEEEEEEBl",
        "lEEEEEEEBl",
        ".lEEEEEBl.",
        "..lEEBBl..",
        "...llll...",
    };

    static readonly string[] ArtePeixe =
    {
        "........y.....",
        ".......yy.....",
        "......k.......",
        ".....k........",
        "..kkkkkkk.....",
        ".kvvvvvvvk..kk",
        "kvvvwkvvvvkkvk",
        "kvvvvvvvvvvvvk",
        "kvwkwkwvvvkkvk",
        ".kvvvvvvvk..kk",
        "..kkkkkkk.....",
    };

    static readonly string[] ArtePeixeBoca =
    {
        "....kkkkkkk.......",
        "..kkvvvvvvvkk.....",
        ".kvvvwkvvvvvvk..kk",
        "kw.w.w.w.vvvvvkkvk",
        "k........vvvvvvvvk",
        "k........vvvvvvvvk",
        "kw.w.w.w.vvvvvkkvk",
        ".kvvvvvvvvvvvk..kk",
        "..kkvvvvvvvkk.....",
        "....kkkkkkk.......",
    };

    static readonly string[] ArteOstra =
    {
        "kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk",
        "kmmmmmmZmmmmmmmmmmmmmmZmmmmmmmmk",
        "kMmmmmmmmmmmmZmmmmmmmmmmmmmmmmMk",
        ".kMMMMMMMMMMMMMMMMMMMMMMMMMMMMk.",
        "..kkkkkkkkkkkkkkkkkkkkkkkkkkkk..",
        "...kpppppppppppppppppppppppppk..",
        "...kkkkkkkkkkkkkkkkkkkkkkkkkk...",
    };

    static readonly string[] ArteOstraFechada =
    {
        "..........kkkkkkkkkkkk..........",
        "......kkkkmmmmmmmmmmmmkkkk......",
        "...kkkmmmmmmmZmmmmmmmmmmmmkkk...",
        ".kkmmmmmmmmmmmmmmmmmZmmmmmmmmkk.",
        "kwkwkwkwkwkwkwkwkwkwkwkwkwkwkwkk",
        "kMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMk",
        ".kkkkkkkkkkkkkkkkkkkkkkkkkkkkkk.",
    };

    static readonly string[] ArteAguaViva =
    {
        "....kkkkkk....",
        "..kkPPPPPPkk..",
        ".kPPwwPPPPPPk.",
        "kPPwwPPPPPPPPk",
        "kPPPPPPPPPPPPk",
        "kkkkkkkkkkkkkk",
        ".V.V.V..V.V.V.",
        ".V..V.V..V.V..",
        "V..V..V.V..V..",
        ".V..V..V..V...",
    };
}

// '@' e '%' - a bolha de ar (e a falsa).
public class BolhaDeAr : MonoBehaviour
{
    public bool falsa;
    SpriteRenderer desenho;
    Vector3 origem;
    float voltaEm = -1f;

    void Awake()
    {
        desenho = gameObject.AddComponent<SpriteRenderer>();
        desenho.sprite = FabricaDeSprites.Pegar("bolha_ar");
        desenho.sortingOrder = 6;
        var area = gameObject.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = 0.4f;
        origem = transform.position;
    }

    void Update()
    {
        transform.position = origem + Vector3.up * 0.12f * Mathf.Sin(Time.time * 2f + origem.x);
        if (voltaEm > 0f && Time.time >= voltaEm)
        {
            voltaEm = -1f;
            desenho.enabled = true;
            GetComponent<Collider2D>().enabled = true;
        }
    }

    void OnTriggerEnter2D(Collider2D outro)
    {
        var jogador = outro.GetComponent<Jogador>();
        if (jogador == null || jogador.Morto || !desenho.enabled) return;
        desenho.enabled = false;
        GetComponent<Collider2D>().enabled = false;
        Efeitos.Poeira(transform.parent, transform.position, 6, 1.8f);
        if (falsa)
        {
            GerenciadorDoJogo.Som("pop", 0.6f);
            Conquistas.Desbloquear("bolha_falsa");
            GerenciadorDoJogo.Instancia?.Avisar(Idioma.T("Era só uma bolha. :)", "It was just a bubble. :)"), 1.4f);
            return; // (a falsa não volta)
        }
        jogador.Respirar();
        Desafios.BolhasDeAr++;
        voltaEm = Time.time + 3f;
    }
}

// 'f' - o peixe-lanterna.
public class PeixeLanterna : MonoBehaviour
{
    SpriteRenderer isca, corpo;
    GameObject boca;
    float lado = -1f; // a boca fica do lado do jogador (escolhido no bote)
    bool atacando;

    void Awake()
    {
        // a isca é IGUALZINHA a uma moeda
        isca = gameObject.AddComponent<SpriteRenderer>();
        isca.sprite = FabricaDeSprites.Pegar("moeda");
        isca.sortingOrder = 5;
        Luzes.Ponto(transform, Luzes.Dourada, 1.6f, 0.6f);
        corpo = ConstrutorDeFase.Visual("Corpo", transform, transform.position + new Vector3(0.6f, -0.7f, 0f), "peixe_lanterna", 4, false).GetComponent<SpriteRenderer>();
        corpo.color = new Color(1f, 1f, 1f, 0.12f); // quase invisível no escuro
    }

    void Update()
    {
        if (atacando) return;
        isca.transform.localScale = new Vector3(Mathf.Cos(Time.time * 3f), 1f, 1f); // gira como moeda
        for (int i = 0; GerenciadorDoJogo.ProximoVivo(ref i, out Vector2 jogador);)
            if (Vector2.Distance(jogador, transform.position) < 1.6f)
            {
                lado = Mathf.Sign(jogador.x - transform.position.x);
                StartCoroutine(Bote());
                return;
            }
    }

    IEnumerator Bote()
    {
        atacando = true;
        isca.transform.localScale = Vector3.one;
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            isca.color = (int)(t * 20f) % 2 == 0 ? new Color(1f, 0.35f, 0.35f) : Color.white;
            yield return null;
        }
        GerenciadorDoJogo.Som("risada", 0.5f);
        boca = ConstrutorDeFase.Visual("Boca", transform, transform.position, "peixe_lanterna_boca", 7, false);
        boca.GetComponent<SpriteRenderer>().flipX = lado < 0f;
        Perigo.Adicionar(boca, new Vector2(1.0f, 0.55f), Vector2.zero);
        Perigo.TornarMovel(boca);
        Vector3 de = transform.position, ate = transform.position + Vector3.right * lado * 1.8f;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            boca.transform.position = Vector3.Lerp(de, ate, t / 0.3f);
            yield return null;
        }
        yield return new WaitForSeconds(0.25f);
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            boca.transform.position = Vector3.Lerp(ate, de, t / 0.4f);
            yield return null;
        }
        Destroy(boca);
        isca.color = Color.white;
        yield return new WaitForSeconds(0.8f);
        atacando = false;
    }
}

// '=' - a correnteza (empurra para a esquerda).
public class Correnteza : MonoBehaviour
{
    public float forca = 4f;
    Rect area;
    readonly List<Transform> riscos = new List<Transform>();

    public static void Criar(Transform raiz, List<Vector2Int> celulas, int altura)
    {
        int xMin = int.MaxValue, xMax = int.MinValue, lMin = int.MaxValue, lMax = int.MinValue;
        foreach (Vector2Int c in celulas)
        {
            xMin = Mathf.Min(xMin, c.x); xMax = Mathf.Max(xMax, c.x);
            lMin = Mathf.Min(lMin, c.y); lMax = Mathf.Max(lMax, c.y);
        }
        var objeto = new GameObject("Correnteza");
        objeto.transform.SetParent(raiz, false);
        var c2 = objeto.AddComponent<Correnteza>();
        float baixo = altura - 1 - lMax - 0.5f, cima = altura - 1 - lMin + 0.5f;
        c2.area = new Rect(xMin - 0.5f, baixo, xMax - xMin + 1f, cima - baixo);
        int n = Mathf.Max(3, Mathf.RoundToInt(c2.area.width * c2.area.height / 4f));
        for (int i = 0; i < n; i++)
        {
            var risco = ConstrutorDeFase.Visual("Risco", objeto.transform, Vector3.zero, "risco_correnteza", 21, sombra: false).transform;
            risco.position = new Vector3(Random.Range(c2.area.xMin, c2.area.xMax), Random.Range(c2.area.yMin, c2.area.yMax), 0f);
            c2.riscos.Add(risco);
        }
    }

    void Update()
    {
        foreach (Transform r in riscos)
        {
            Vector3 p = r.position + Vector3.left * forca * 1.6f * Time.deltaTime;
            if (p.x < area.xMin) p = new Vector3(area.xMax, Random.Range(area.yMin, area.yMax), 0f);
            r.position = p;
        }
    }

    void FixedUpdate()
    {
        foreach (Jogador j in GerenciadorDoJogo.Jogadores)
        {
            if (j == null || j.Morto) continue;
            if (area.Contains(j.transform.position)) j.Empurrar(-forca);
        }
    }
}

// 'x' - a ostra que morde.
public class Ostra : MonoBehaviour
{
    SpriteRenderer desenho;
    BoxCollider2D tampa;
    bool fechando;

    void Awake()
    {
        // a letra marca a ponta ESQUERDA: a ostra ocupa esta célula e a da direita
        transform.position += new Vector3(0.5f, -0.5f, 0f);
        desenho = gameObject.AddComponent<SpriteRenderer>();
        desenho.sprite = FabricaDeSprites.Pegar("ostra");
        desenho.sortingOrder = 3;
        Sombra.Adicionar(desenho);
        tampa = gameObject.AddComponent<BoxCollider2D>();
        tampa.size = new Vector2(1.9f, 0.3f);
        tampa.offset = new Vector2(0f, 0.28f);
    }

    void Update()
    {
        if (fechando) return;
        for (int i = 0; GerenciadorDoJogo.ProximoVivo(ref i, out Vector2 p, out Jogador j);)
            if (j.NoChao && EmCima(p))
            {
                StartCoroutine(Fechar());
                return;
            }
    }

    bool EmCima(Vector2 p) => Mathf.Abs(p.x - transform.position.x) < 1.15f && p.y > transform.position.y + 0.3f && p.y < transform.position.y + 1.4f;

    IEnumerator Fechar()
    {
        fechando = true;
        yield return new WaitForSeconds(0.5f);
        Vector3 origem = transform.position;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime) // treme e mostra os dentes
        {
            transform.position = origem + new Vector3(Random.Range(-0.04f, 0.04f), 0f, 0f);
            desenho.sprite = FabricaDeSprites.Pegar((int)(t * 20f) % 2 == 0 ? "ostra_fechada" : "ostra");
            yield return null;
        }
        transform.position = origem;
        desenho.sprite = FabricaDeSprites.Pegar("ostra_fechada");
        GerenciadorDoJogo.Som("pancada", 0.7f);
        CameraSeguir.Tremer(0.08f, 0.12f);
        foreach (Jogador j in GerenciadorDoJogo.Jogadores)
            if (j != null && !j.Morto && EmCima(j.transform.position)) j.Morrer();
        tampa.enabled = false;
        yield return new WaitForSeconds(1.2f);
        desenho.sprite = FabricaDeSprites.Pegar("ostra");
        tampa.enabled = true;
        yield return new WaitForSeconds(0.3f);
        fechando = false;
    }
}

// 'z' e '&' - a água-viva (quica) e a que queima (mata). As duas iguaizinhas.
public class AguaViva : MonoBehaviour
{
    public bool queima;
    Vector3 origem;
    float timerApertada;

    void Awake()
    {
        var desenho = gameObject.AddComponent<SpriteRenderer>();
        desenho.sprite = FabricaDeSprites.Pegar("agua_viva");
        desenho.sortingOrder = 4;
        Luzes.Ponto(transform, Luzes.Lilas, 1.4f, 0.35f);
        origem = transform.position;
    }

    public AguaViva Preparar(bool queimar)
    {
        queima = queimar;
        if (queima) Perigo.Adicionar(gameObject, new Vector2(0.8f, 0.6f), Vector2.zero);
        else
        {
            var area = gameObject.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = new Vector2(0.9f, 0.5f);
            area.offset = new Vector2(0f, 0.1f);
        }
        Perigo.TornarMovel(gameObject);
        return this;
    }

    void Update()
    {
        transform.position = origem + Vector3.up * 0.25f * Mathf.Sin(Time.time * 1.6f + origem.x * 0.7f);
        if (timerApertada > 0f)
        {
            timerApertada -= Time.deltaTime;
            transform.localScale = new Vector3(1.2f, 0.75f, 1f);
        }
        else transform.localScale = Vector3.one;
    }

    void OnTriggerEnter2D(Collider2D outro) => Testar(outro);
    void OnTriggerStay2D(Collider2D outro) => Testar(outro);

    void Testar(Collider2D outro)
    {
        if (queima) return; // (quem mata é o Perigo)
        var jogador = outro.GetComponent<Jogador>();
        if (jogador == null || jogador.Morto || jogador.Corpo.linearVelocity.y > 0.5f) return;
        jogador.Quicar(16f, false);
        GerenciadorDoJogo.Som("mola", 0.7f);
        timerApertada = 0.15f;
    }
}
