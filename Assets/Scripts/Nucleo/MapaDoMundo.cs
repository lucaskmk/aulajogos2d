using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// O mapa do mundo (estilo Super Mario World): cada ponto é uma fase, ligados por um caminho.
// O Subaru anda de ponto em ponto; em cada ponto fica o "personagem" daquela fase.
// No fim do caminho fica a mansão, com a Emilia esperando.
//
// Cores dos pontos: verde = já passou nesta partida, amarelo = liberada, cinza = trancada
// (fase trancada mostra o personagem só como sombra, de mistério).
//
// Quem lê as teclas é o GerenciadorDoJogo; aqui só tem o desenho e a caminhada.
public class MapaDoMundo : MonoBehaviour
{
    // Onde fica cada fase no mapa (em blocos; x para a direita, y para cima).
    // Se criar mais fases, acrescente pontos aqui.
    public static readonly Vector2[] Pontos =
    {
        new Vector2(2.5f, 4f),
        new Vector2(6.5f, 5.5f),
        new Vector2(10.5f, 4f),
        new Vector2(15f, 5f),
        new Vector2(20f, 4.5f),
        new Vector2(22.5f, 8f),
        new Vector2(17.5f, 9.5f),
        new Vector2(12f, 10.5f),
    };

    // O "personagem" de cada fase, que fica em cima do ponto.
    static readonly string[] Personagens =
    {
        "puck", "inimigo", "serra", "nuvem_malvada", "coelho", "inimigo_espinhos", "esmagador", "emilia",
    };

    public const int Largura = 26, Altura = 15;
    public static readonly Color CorDoChao = new Color32(140, 205, 115, 255);

    static readonly Color CorConcluida = new Color(0.45f, 0.95f, 0.45f);
    static readonly Color CorLiberada = new Color(1f, 0.85f, 0.25f);
    static readonly Color CorTrancada = new Color(0.55f, 0.55f, 0.62f);

    public float velocidade = 6f;

    public Transform Subaru { get; private set; }
    public int Selecionado { get; private set; }
    public bool Andando { get; private set; }

    int liberado;    // última fase liberada
    int concluidas;  // fases já passadas nesta partida
    SpriteRenderer desenhoSubaru;
    SpriteRenderer[] pontos;
    Transform[] personagens;
    SpriteRenderer[] desenhosDosPersonagens;
    readonly List<List<SpriteRenderer>> trilhas = new List<List<SpriteRenderer>>(); // trilhas[i] liga o ponto i ao i+1
    float timerPasso;
    int nascendo = -1; // ponto que está aparecendo agora (animação própria)

    static int QuantidadeDePontos => Mathf.Min(Pontos.Length, Fases.Todas.Length);

    // no: onde o Subaru começa. andarPara: se >= 0, ele anda sozinho até lá (acabou de passar de fase).
    // caminhoNovo: o caminho até "andarPara" acabou de ser liberado (aparece aos pouquinhos).
    public static MapaDoMundo Criar(Transform raiz, int no, int liberado, int concluidas, int andarPara = -1, bool caminhoNovo = false)
    {
        var mapa = new GameObject("MapaDoMundo").AddComponent<MapaDoMundo>();
        mapa.transform.SetParent(raiz, false);
        mapa.liberado = Mathf.Clamp(liberado, 0, QuantidadeDePontos - 1);
        mapa.concluidas = concluidas;
        mapa.Selecionado = Mathf.Clamp(no, 0, QuantidadeDePontos - 1);
        mapa.Montar(andarPara, caminhoNovo);
        return mapa;
    }

    void Montar(int andarPara, bool caminhoNovo)
    {
        int n = QuantidadeDePontos;

        // Caminho pontilhado entre os pontos.
        for (int i = 0; i < n - 1; i++)
        {
            var trilha = new List<SpriteRenderer>();
            Vector2 a = Pontos[i], b = Pontos[i + 1];
            float distancia = Vector2.Distance(a, b);
            int quantidade = Mathf.FloorToInt((distancia - 1f) / 0.55f);
            for (int k = 1; k <= quantidade; k++)
            {
                Vector2 p = Vector2.Lerp(a, b, (0.5f + k * 0.55f) / distancia);
                var ponto = ConstrutorDeFase.Visual("Trilha", transform, p, "trilha", 1, false).GetComponent<SpriteRenderer>();
                bool visivel = i + 1 <= liberado && !(caminhoNovo && i + 1 == andarPara);
                ponto.color = new Color(1f, 1f, 1f, visivel ? 1f : 0.25f);
                trilha.Add(ponto);
            }
            trilhas.Add(trilha);
        }

        // Pontos, números e personagens.
        pontos = new SpriteRenderer[n];
        personagens = new Transform[n];
        desenhosDosPersonagens = new SpriteRenderer[n];
        for (int i = 0; i < n; i++)
        {
            pontos[i] = ConstrutorDeFase.Visual("Ponto " + (i + 1), transform, Pontos[i], "no_mapa", 2).GetComponent<SpriteRenderer>();
            var numero = new GameObject("Numero");
            numero.transform.SetParent(pontos[i].transform, false);
            var desenhoNumero = numero.AddComponent<SpriteRenderer>();
            desenhoNumero.sprite = FabricaDeSprites.SpriteDeTexto((i + 1).ToString(), new Color32(20, 20, 28, 255), new Color32(255, 255, 255, 0));
            desenhoNumero.sortingOrder = 3;

            string nome = Personagens[i % Personagens.Length];
            Vector3 lugar = (Vector3)Pontos[i] + new Vector3(0.95f, nome == "emilia" ? -0.3f : 0.75f, 0f);
            var personagem = ConstrutorDeFase.Visual("Personagem", transform, lugar, nome, 4);
            personagens[i] = personagem.transform;
            desenhosDosPersonagens[i] = personagem.GetComponent<SpriteRenderer>();
            personagem.transform.localScale = Vector3.one * (nome == "emilia" ? 1f : 0.8f);
        }
        if (caminhoNovo && andarPara >= 0 && andarPara < n)
        {
            nascendo = andarPara;
            pontos[andarPara].transform.localScale = Vector3.zero; // ainda vai "nascer"
        }

        Decorar();

        // O Subaru.
        desenhoSubaru = ConstrutorDeFase.Visual("Subaru", transform, PosicaoNoPonto(Selecionado), "jogador", 10).GetComponent<SpriteRenderer>();
        Subaru = desenhoSubaru.transform;

        if (andarPara >= 0 && andarPara < n) StartCoroutine(LiberarEAndar(andarPara, caminhoNovo));
    }

    // Mansão no fim do caminho, a Baleia Branca nadando no céu, árvores, flores e nuvens.
    void Decorar()
    {
        Vector2 fim = Pontos[QuantidadeDePontos - 1];
        var mansao = ConstrutorDeFase.Visual("Mansao", transform, new Vector3(fim.x, fim.y + 0.45f, 0f), "fundo_castelo", -6, false);
        mansao.transform.localScale = Vector3.one * 0.28f;
        mansao.GetComponent<SpriteRenderer>().color = new Color(0.82f, 0.74f, 0.95f);

        var baleia = ConstrutorDeFase.Visual("Baleia", transform, new Vector3(19.5f, 12.3f, 0f), "baleia", -5, false);
        baleia.transform.localScale = Vector3.one * 0.45f;
        baleia.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.8f);
        Animacao.Adicionar(baleia, Animacao.Tipo.Flutuar, 0.25f, 2.5f);

        var sorteio = new System.Random(2024);
        for (int i = 0; i < 70; i++)
        {
            var lugar = new Vector2((float)sorteio.NextDouble() * Largura, 0.3f + (float)sorteio.NextDouble() * (Altura - 2.5f));
            if (PertoDoCaminho(lugar, 1.4f) || Vector2.Distance(lugar, fim + Vector2.up) < 4f) continue;
            string sprite = EnfeitesDoMapa[sorteio.Next(EnfeitesDoMapa.Length)];
            var enfeite = ConstrutorDeFase.Visual("Enfeite", transform, lugar, sprite, sprite == "arvore" ? -3 : -7);
            if (sprite != "arvore") Animacao.Adicionar(enfeite, Animacao.Tipo.Balancar, 2.5f, 6f);
        }

        for (int i = 0; i < 4; i++)
        {
            var nuvem = ConstrutorDeFase.Visual("Nuvem", transform, new Vector3(3f + i * 6.5f, 12.5f - (i % 2) * 1.5f, 0f), "nuvem", 15, false);
            nuvem.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.85f);
            Animacao.Adicionar(nuvem, Animacao.Tipo.Flutuar, 0.3f, 1.2f);
        }
    }

    static readonly string[] EnfeitesDoMapa = { "arvore", "flor", "flor", "tufo" };

    static bool PertoDoCaminho(Vector2 lugar, float distancia)
    {
        for (int i = 0; i < QuantidadeDePontos; i++)
        {
            if (Vector2.Distance(lugar, Pontos[i]) < distancia + 0.6f) return true;
            if (i + 1 < QuantidadeDePontos && DistanciaAteSegmento(lugar, Pontos[i], Pontos[i + 1]) < distancia) return true;
        }
        return false;
    }

    static float DistanciaAteSegmento(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
        return Vector2.Distance(p, a + ab * t);
    }

    // O Subaru fica com os pés no centro do ponto.
    static Vector3 PosicaoNoPonto(int i) => (Vector3)Pontos[i] + Vector3.up * 0.45f;

    // ------------------------------------------------------------------ andar

    // direcao: +1 = próxima fase, -1 = anterior. Devolve false se não dá para ir (trancada ou fim do caminho).
    public bool Mover(int direcao)
    {
        int destino = Selecionado + direcao;
        if (Andando || destino < 0 || destino > liberado || destino >= QuantidadeDePontos)
        {
            if (!Andando) GerenciadorDoJogo.Som("bloco", 0.4f);
            return false;
        }
        StartCoroutine(Andar(destino));
        return true;
    }

    IEnumerator Andar(int destino)
    {
        Andando = true;
        GerenciadorDoJogo.Som("pulo", 0.3f);
        Vector3 alvo = PosicaoNoPonto(destino);
        desenhoSubaru.flipX = alvo.x < Subaru.position.x;
        while (Subaru.position != alvo)
        {
            Subaru.position = Vector3.MoveTowards(Subaru.position, alvo, velocidade * Time.deltaTime);
            timerPasso += Time.deltaTime;
            desenhoSubaru.sprite = FabricaDeSprites.Pegar((int)(timerPasso * 10f) % 2 == 0 ? "jogador" : "jogador_andando");
            yield return null;
        }
        desenhoSubaru.sprite = FabricaDeSprites.Pegar("jogador");
        Selecionado = destino;
        Andando = false;
    }

    // Acabou de passar de fase: o caminho até a próxima aparece pontinho por pontinho e o Subaru vai até lá.
    IEnumerator LiberarEAndar(int destino, bool caminhoNovo)
    {
        Andando = true;
        yield return new WaitForSeconds(0.7f); // espera a transição de tela

        if (caminhoNovo)
        {
            int trecho = Mathf.Min(destino, Selecionado); // trilha entre os dois pontos
            if (trecho < trilhas.Count)
                foreach (SpriteRenderer ponto in trilhas[trecho])
                {
                    ponto.color = Color.white;
                    GerenciadorDoJogo.Som("moeda", 0.15f);
                    yield return new WaitForSeconds(0.06f);
                }

            // o ponto novo "nasce" com um pulinho
            GerenciadorDoJogo.Som("mola", 0.6f);
            Transform novo = pontos[destino].transform;
            nascendo = destino;
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                novo.localScale = Vector3.one * Mathf.Sin(t / 0.35f * Mathf.PI * 0.75f) / 0.7071f;
                yield return null;
            }
            novo.localScale = Vector3.one;
            nascendo = -1;
            Efeitos.Poeira(transform, novo.position, 8, 2.5f);
        }

        yield return Andar(destino);
    }

    // ------------------------------------------------------------------ animação

    void Update()
    {
        for (int i = 0; i < pontos.Length; i++)
        {
            bool trancada = i > liberado;
            pontos[i].color = trancada ? CorTrancada : i < concluidas ? CorConcluida : CorLiberada;
            desenhosDosPersonagens[i].color = trancada ? new Color(0f, 0f, 0f, 0.45f) : Color.white;

            // personagem pulando de leve; o do ponto escolhido pula mais
            float altura = i == Selecionado && !Andando ? 0.18f : 0.06f;
            personagens[i].localPosition = (Vector3)Pontos[i]
                + new Vector3(0.95f, (Personagens[i % Personagens.Length] == "emilia" ? -0.3f : 0.75f) + Mathf.Abs(Mathf.Sin(Time.time * 4f + i)) * altura, 0f);
            if (Personagens[i % Personagens.Length] == "serra") personagens[i].Rotate(0f, 0f, -180f * Time.deltaTime);

            // o ponto em que o Subaru está "pulsa" de leve
            if (i != nascendo)
                pontos[i].transform.localScale = Vector3.one * (i == Selecionado && !Andando ? 1f + 0.08f * Mathf.Sin(Time.time * 6f) : 1f);
        }

        // parado, o Subaru respira
        if (!Andando) Subaru.localScale = new Vector3(1f, 1f + 0.04f * Mathf.Sin(Time.time * 3f), 1f);
    }
}
