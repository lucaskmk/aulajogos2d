using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// As ciladas "AO VIVO": a fase muda enquanto você olha (o jeito do Level Devil), e o próprio jogo mente.
//
//  'd'  CHÃO QUE DESLIZA: parece chão. Quando um Grumete chega a 1,5 bloco da ponta esquerda do pedaço,
//       ele treme (0,25 s) e escorrega para a DIREITA a largura dele inteira: abre um buraco onde estava e
//       tampa o buraco que tinha logo depois. Regra da casa: no mapa, logo à direita dos 'd' tem que ter um
//       buraco do MESMO tamanho (o pedaço desliza para dentro dele) e no máximo 3 de largura (dá para pular).
//       Decorou: pule antes da ponta e caia no pedaço que deslizou.
//  'V'  O HUD CAI: linha invisível. Cruzou, o "Mortes: N" do canto da tela se solta, voa até em cima de você,
//       treme (0,6 s) e despenca. Esmaga quem estiver embaixo e vira um bloco de verdade (dá para subir nele).
//       Corra (ou volte) enquanto ele treme. Volta para o canto na próxima tentativa.
//  'p'  PAUSA FALSA: linha invisível. Cruzou, aparece a tela de PAUSA... mas o jogo continua rodando por trás
//       (1,6 s, o véu é mais claro que o de verdade, dá para ver). Quem solta o controle para "ler" o menu cai no
//       que vem depois. Só engana uma vez por tentativa.
public class ChaoQueDesliza : MonoBehaviour
{
    public float velocidade = 9f;
    public float alcance = 1.5f;

    readonly List<Transform> blocos = new List<Transform>();
    float esquerda, largura;
    bool foi;

    public void AdicionarBloco(Vector3 posicao, Sprite sprite)
    {
        var bloco = new GameObject("Bloco");
        bloco.transform.SetParent(transform, false);
        bloco.transform.position = posicao;
        var desenho = bloco.AddComponent<SpriteRenderer>();
        desenho.sprite = sprite;
        Sombra.Adicionar(desenho);
        bloco.AddComponent<BoxCollider2D>().size = Vector2.one;
        blocos.Add(bloco.transform);
        if (blocos.Count == 1) esquerda = posicao.x - 0.5f;
        esquerda = Mathf.Min(esquerda, posicao.x - 0.5f);
    }

    void Start()
    {
        // largura: quantas colunas diferentes o pedaço ocupa
        var colunas = new HashSet<int>();
        foreach (Transform b in blocos) colunas.Add(Mathf.RoundToInt(b.position.x));
        largura = colunas.Count;
        // (o corpo cinemático deixa o Grumete em cima ser levado pela física sem tremer)
        var corpo = gameObject.AddComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Kinematic;
    }

    void Update()
    {
        if (foi) return;
        for (int i = 0; GerenciadorDoJogo.ProximoVivo(ref i, out Vector2 jogador);)
        {
            float topo = MaiorAltura() + 0.5f;
            if (jogador.x > esquerda - alcance && jogador.x < esquerda + largura && jogador.y > topo - 0.2f && jogador.y < topo + 3f)
            {
                StartCoroutine(Deslizar());
                return;
            }
        }
    }

    float MaiorAltura()
    {
        float y = float.MinValue;
        foreach (Transform b in blocos) y = Mathf.Max(y, b.position.y);
        return y;
    }

    IEnumerator Deslizar()
    {
        foi = true;
        GerenciadorDoJogo.Som("armadilha", 0.8f);
        Vector3 origem = transform.position;
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            transform.position = origem + new Vector3(Random.Range(-0.05f, 0.05f), 0f, 0f);
            yield return null;
        }
        transform.position = origem;
        float andou = 0f;
        float topo = MaiorAltura() + 0.5f;
        while (andou < largura)
        {
            float passo = Mathf.Min(velocidade * Time.deltaTime, largura - andou);
            andou += passo;
            transform.position = origem + Vector3.right * andou;
            // quem está em cima vai junto
            foreach (Jogador j in GerenciadorDoJogo.Jogadores)
            {
                if (j == null || j.Morto || !j.NoChao) continue;
                Vector2 p = j.transform.position;
                if (p.x > transform.position.x + esquerda - origem.x - 0.3f && p.x < transform.position.x + esquerda - origem.x + largura + 0.3f
                    && p.y > topo && p.y < topo + 1.2f)
                    j.Carregar(new Vector2(passo, 0f));
            }
            yield return null;
        }
        Efeitos.Poeira(transform.parent, new Vector3(esquerda + largura * 1.5f, topo, 0f), 6, 2f);
        CameraSeguir.Tremer(0.08f, 0.15f);
    }
}

// 'V' - o "Mortes: N" do HUD se solta e cai em você.
public class HudQueCai : MonoBehaviour
{
    public static bool Caiu { get; private set; }   // a Interface esconde as mortes do canto enquanto isso

    const float Espera = 0.6f, Gravidade = 55f;
    bool foi;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => Caiu = false;

    // A fase recomeçou: o HUD volta para o canto.
    public static void Recolocar() => Caiu = false;

    void Update()
    {
        if (foi) return;
        for (int i = 0; GerenciadorDoJogo.ProximoVivo(ref i, out Vector2 jogador, out Jogador quem);)
            if (Mathf.Abs(jogador.x - transform.position.x) < 0.5f)
            {
                StartCoroutine(Cair(quem));
                return;
            }
    }

    IEnumerator Cair(Jogador alvo)
    {
        foi = true;
        Caiu = true;
        Camera cam = Camera.main;
        var jogo = GerenciadorDoJogo.Instancia;
        string texto = Idioma.T("MORTES: ", "DEATHS: ") + (jogo != null ? jogo.Mortes : 0);
        Texture2D textura = FabricaDeSprites.TextoEmPixel(texto, new Color32(255, 255, 255, 255), new Color32(20, 20, 28, 255), new Color32(20, 20, 28, 160));
        textura.filterMode = FilterMode.Point;
        // do tamanho de uns 4 blocos de largura (e alguns de altura)
        float ppu = textura.width / 4.5f;
        var sprite = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
        var objeto = new GameObject("HudQueCai");
        objeto.transform.SetParent(transform.parent, false);
        var desenho = objeto.AddComponent<SpriteRenderer>();
        desenho.sprite = sprite;
        desenho.sortingOrder = 40;
        Vector2 tamanho = new Vector2(textura.width / ppu, textura.height / ppu);
        GerenciadorDoJogo.Som("pancada", 0.5f);

        // sai do canto de cima à esquerda da tela e voa até em cima do Grumete
        Vector3 canto = cam != null
            ? cam.transform.position + new Vector3(-cam.orthographicSize * cam.aspect + tamanho.x / 2f + 0.4f, cam.orthographicSize - tamanho.y / 2f - 0.3f, 10f)
            : transform.position + Vector3.up * 6f;
        canto.z = 0f;
        float alturaDaQueda = cam != null ? cam.transform.position.y + cam.orthographicSize - tamanho.y / 2f - 0.3f : transform.position.y + 6f;
        for (float t = 0f; t < Espera; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / (Espera * 0.6f)));
            float alvoX = alvo != null ? alvo.transform.position.x : transform.position.x;
            Vector3 destino = new Vector3(alvoX, alturaDaQueda, 0f);
            objeto.transform.position = Vector3.Lerp(canto, destino, k) + (Vector3)(Random.insideUnitCircle * 0.06f);
            if (cam != null) canto = cam.transform.position + new Vector3(-cam.orthographicSize * cam.aspect + tamanho.x / 2f + 0.4f, alturaDaQueda - cam.transform.position.y, 10f);
            canto.z = 0f;
            desenho.color = (int)(t * 16f) % 2 == 0 ? Color.white : new Color(1f, 0.55f, 0.55f);
            yield return null;
        }
        desenho.color = Color.white;

        // despenca: mata quem pegar embaixo e para no chão (vira bloco)
        var perigo = Perigo.Adicionar(objeto, new Vector2(tamanho.x * 0.92f, 0.4f), new Vector2(0f, -tamanho.y / 2f + 0.2f));
        Perigo.TornarMovel(objeto);
        var filtro = new ContactFilter2D { useTriggers = false };
        var acertos = new List<RaycastHit2D>();
        float velocidade = 0f;
        while (objeto.transform.position.y > -5f)
        {
            velocidade = Mathf.Min(velocidade + Gravidade * Time.deltaTime, 24f);
            float passo = velocidade * Time.deltaTime;
            Vector2 baixo = (Vector2)objeto.transform.position + Vector2.down * (tamanho.y / 2f);
            int n = Physics2D.BoxCast(baixo + Vector2.up * 0.05f, new Vector2(tamanho.x * 0.9f, 0.05f), 0f, Vector2.down, filtro, acertos, passo + 0.05f);
            bool bateu = false;
            for (int k = 0; k < n; k++)
            {
                if (acertos[k].collider.GetComponent<Jogador>() != null || acertos[k].collider.transform.IsChildOf(objeto.transform)) continue;
                objeto.transform.position += Vector3.down * Mathf.Max(0f, acertos[k].distance - 0.05f);
                bateu = true;
                break;
            }
            if (bateu) break;
            objeto.transform.position += Vector3.down * passo;
            yield return null;
        }
        perigo.ativo = false;
        Destroy(perigo.GetComponent<BoxCollider2D>());
        var solido = objeto.AddComponent<BoxCollider2D>();
        solido.size = tamanho * new Vector2(0.95f, 0.8f);
        GerenciadorDoJogo.Som("pancada", 0.8f);
        CameraSeguir.Tremer(0.25f, 0.25f);
        Efeitos.Poeira(transform.parent, objeto.transform.position + Vector3.down * tamanho.y / 2f, 10, 3f);
    }
}

// 'p' - a tela de pausa aparece, mas o jogo não pausa.
public class PausaFalsa : MonoBehaviour
{
    public const float Duracao = 1.6f;
    static float ate;
    public static bool Ativa => Time.time < ate && GerenciadorDoJogo.Instancia != null
                                && GerenciadorDoJogo.Instancia.Estado == EstadoDoJogo.Jogando;
    bool foi;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => ate = 0f;

    public static void Recolocar() => ate = 0f;

    void Update()
    {
        if (foi) return;
        for (int i = 0; GerenciadorDoJogo.ProximoVivo(ref i, out Vector2 jogador);)
            if (Mathf.Abs(jogador.x - transform.position.x) < 0.5f)
            {
                foi = true;
                ate = Time.time + Duracao;
                GerenciadorDoJogo.Som("bloco", 0.5f);
                return;
            }
    }
}
