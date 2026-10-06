using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// O mapa do mundo (estilo Super Mario World): cada ponto é uma fase, ligados por caminhos.
// O Subaru anda de ponto em ponto; em cada ponto fica o "personagem" daquela fase.
//
// O mapa começa coberto pela NÉVOA (o miasma da Bruxa): você só enxerga em volta das fases
// que já liberou. Ninguém sabe onde fica a última fase até chegar lá. Quando passa de fase,
// a névoa vai sumindo ao longo do caminho novo e o ponto da próxima fase aparece.
//
// Cores dos pontos: verde = já passou nesta partida, amarelo = liberada.
// Embaixo de cada ponto, uma caveira com quantas vezes você morreu nela nesta partida.
// Tem também uma fase SECRETA numa ilha do lago (aparece quando você pega todas as moedas da fase 5).
// Quem lê as teclas é o GerenciadorDoJogo; aqui só tem o desenho, a névoa e a caminhada.
public class MapaDoMundo : MonoBehaviour
{
    // O mapa é desenhado em texto, igual às fases. Cada caractere é um bloco.
    //  .  grama            +  caminho           =  ponte (caminho por cima da água)
    //  ~  água             T  árvore            ^  montanha
    //  h  casa             f  flores            M  mansão (a base do desenho fica aqui)
    //  c  céu (o horizonte lá em cima, com as camadas de paralaxe; a névoa não cobre)
    //  1 a 9, A a G  as fases (A = 10, B = 11 ... G = 16). O caminho de '+' e '=' tem que ligar o 1 ao 2,
    //         o 2 ao 3, e assim por diante (o 9 ao A).
    //  S  a fase secreta (ligada por caminho ao ponto da Fases.FaseDoSegredo)
    // Regiões: a capital, o rio, a floresta, os morros, o lago, o campo de flores, as montanhas e a mansão.
    static readonly string[] Desenho =
    {
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            ".h.h..h......~~....TT.TT..T.T...^.............f.^^^^........",
            ".........h..~~.....T.TT.TTT.......^.++++.....ff.f...........",
            "..h....h....~~+++2+++..T..T.++++4++++..++5+++f..............",
            "...........~~.+.....+TT.TTTT+.^...^......+..+..f+++7++...M..",
            "....1++++++==++...TT+.TTT...+T....^^....~=..+f..+....+......",
            "...........~~.....T.+TT....T+T.......~~~~=~~=..f+....8......",
            "........h...~~....T.++++3++++..^^^^.~~~~.S.~==6++...^+^.fff.",
            ".h.h..h......~~...TTT.T...TTTT^.^...~~~~f.f~~~f.f.^^.+++9...",
            ".....h.......~~...TTT..TTTTT.........~~~~~~~~..f^^^.^^^.....",
            ".............~~...TTTTTT.TT.TT.^.^^.....~~....f.^^^^.^^fff..",
            "............~~....TTT...TTT.T.^...^..........fff^.^.^^..fff.",
            "............~~....TT......TTTT..^.^^........ff..^^^.^....f..",
    };

    // O "personagem" de cada fase, que fica do lado do ponto.
    static readonly string[] Personagens =
    {
        "puck", "inimigo", "serra", "nuvem_malvada", "coelho", "inimigo_espinhos", "esmagador", "beatrice", "emilia",
    };

    public static int Largura => Desenho[0].Length;
    public static int Altura => Desenho.Length;
    public static readonly Color CorDoChao = new Color32(140, 205, 115, 255);

    static readonly Color CorConcluida = new Color(0.45f, 0.95f, 0.45f);
    static readonly Color CorLiberada = new Color(1f, 0.85f, 0.25f);
    static readonly Color CorSecreta = new Color(0.85f, 0.6f, 1f);
    static readonly Color32 Branco = new Color32(255, 255, 255, 255), Preto = new Color32(20, 20, 28, 255);

    const float RaioDoPonto = 3.6f;    // quanto a névoa abre em volta de uma fase liberada
    const float RaioDoCaminho = 1.8f;  // e em volta do caminho
    const int OrdemDaNevoa = 30;
    // O chão (água, ponte, terra) fica ATRÁS das sombras (Sombra.Ordem = -9), para tudo projetar sombra nele.
    const int OrdemDaAgua = -14, OrdemDoCaminho = -13;

    public float velocidade = 6f;

    public Transform Subaru { get; private set; }
    public int Selecionado { get; private set; }
    public bool Andando { get; private set; }

    int liberado;    // última fase liberada
    int concluidas;  // fases já passadas nesta partida
    readonly List<Vector2Int> pontos = new List<Vector2Int>();               // célula de cada fase
    readonly List<List<Vector2Int>> caminhos = new List<List<Vector2Int>>(); // caminhos[i]: do ponto i até o i+1
    readonly Dictionary<Vector2Int, SpriteRenderer> terra = new Dictionary<Vector2Int, SpriteRenderer>();
    SpriteRenderer[,] nevoa;
    float[,] tamanhoDaNevoa;
    SpriteRenderer[] desenhosDosPontos;
    Transform[] personagens;
    SpriteRenderer desenhoSubaru;
    float timerPasso;
    int nascendo = -1; // ponto que está aparecendo agora (animação própria)

    // Fase secreta
    bool secretaLiberada;
    Vector2Int? pontoSecreto;
    List<Vector2Int> caminhoSecreto;
    SpriteRenderer desenhoSecreto;
    Transform personagemSecreto;

    // no: onde o Subaru começa (pode ser Fases.IndiceSecreto). mortes: mortes por fase (a última é a da secreta).
    // andarPara: se >= 0, ele anda sozinho até lá (acabou de passar de fase).
    // caminhoNovo: o caminho até "andarPara" acabou de ser liberado (a névoa some aos pouquinhos).
    // mostrarSecreta: a fase secreta acabou de ser liberada (aparece com animação).
    public static MapaDoMundo Criar(Transform raiz, int no, int liberado, int concluidas, int[] mortes,
        bool secretaLiberada, int andarPara = -1, bool caminhoNovo = false, bool mostrarSecreta = false)
    {
        var mapa = new GameObject("MapaDoMundo").AddComponent<MapaDoMundo>();
        mapa.transform.SetParent(raiz, false);
        mapa.LerDesenho();
        int ultimo = mapa.pontos.Count - 1;
        mapa.liberado = Mathf.Clamp(liberado, 0, ultimo);
        mapa.concluidas = concluidas;
        mapa.secretaLiberada = secretaLiberada && mapa.pontoSecreto.HasValue;
        mapa.Selecionado = no == Fases.IndiceSecreto && mapa.secretaLiberada ? no : Mathf.Clamp(no, 0, ultimo);
        if (andarPara > ultimo) andarPara = -1;
        mapa.Montar(andarPara, caminhoNovo && andarPara >= 0, mostrarSecreta && mapa.secretaLiberada, mortes);
        return mapa;
    }

    // Posição no mundo do centro de uma célula (a linha 0 do texto é o topo).
    static Vector3 Mundo(Vector2Int celula) => new Vector3(celula.x, Altura - 1 - celula.y, 0f);

    static char Celula(Vector2Int c) =>
        c.y >= 0 && c.y < Altura && c.x >= 0 && c.x < Desenho[c.y].Length ? Desenho[c.y][c.x] : ' ';

    // Número da fase de um ponto do mapa: '1' a '9' = fases 1 a 9, 'A' a 'G' = fases 10 a 16. -1 = não é fase.
    static int IndiceDoPonto(char c)
    {
        if (c >= '1' && c <= '9') return c - '1';
        if (c >= 'A' && c <= 'G') return 9 + (c - 'A');
        return -1;
    }

    static bool EhPonto(char c) => IndiceDoPonto(c) >= 0 || c == 'S';
    static bool DaPraAndar(char c) => c == '+' || c == '=' || EhPonto(c);

    // Acha os pontos das fases e o caminho entre cada par (busca em largura pelas células de caminho).
    void LerDesenho()
    {
        var achados = new SortedDictionary<int, Vector2Int>();
        for (int y = 0; y < Altura; y++)
            for (int x = 0; x < Desenho[y].Length; x++)
            {
                int indice = IndiceDoPonto(Desenho[y][x]);
                if (indice >= 0) achados[indice] = new Vector2Int(x, y);
                if (Desenho[y][x] == 'S') pontoSecreto = new Vector2Int(x, y);
            }
        foreach (var par in achados)
            if (par.Key == pontos.Count && pontos.Count < Fases.Todas.Length) pontos.Add(par.Value);

        for (int i = 0; i + 1 < pontos.Count; i++)
        {
            List<Vector2Int> caminho = AcharCaminho(pontos[i], pontos[i + 1]);
            if (caminho == null)
            {
                Debug.LogWarning($"MapaDoMundo: não achei caminho entre as fases {i + 1} e {i + 2}.");
                caminho = new List<Vector2Int> { pontos[i + 1] };
            }
            caminhos.Add(caminho);
        }

        if (pontoSecreto.HasValue && Fases.FaseDoSegredo < pontos.Count)
        {
            caminhoSecreto = AcharCaminho(pontos[Fases.FaseDoSegredo], pontoSecreto.Value);
            if (caminhoSecreto == null) pontoSecreto = null; // sem caminho, sem fase secreta no mapa
        }
    }

    static readonly Vector2Int[] Direcoes = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

    // Devolve as células do caminho, sem a de saída e com a de chegada.
    static List<Vector2Int> AcharCaminho(Vector2Int de, Vector2Int ate)
    {
        var veioDe = new Dictionary<Vector2Int, Vector2Int>();
        var fila = new Queue<Vector2Int>();
        fila.Enqueue(de);
        veioDe[de] = de;
        while (fila.Count > 0)
        {
            Vector2Int atual = fila.Dequeue();
            if (atual == ate) break;
            foreach (Vector2Int d in Direcoes)
            {
                Vector2Int vizinha = atual + d;
                if (veioDe.ContainsKey(vizinha) || !DaPraAndar(Celula(vizinha))) continue;
                if (EhPonto(Celula(vizinha)) && vizinha != ate) continue; // não atravessa outras fases
                veioDe[vizinha] = atual;
                fila.Enqueue(vizinha);
            }
        }
        if (!veioDe.ContainsKey(ate)) return null;

        var caminho = new List<Vector2Int>();
        for (Vector2Int c = ate; c != de; c = veioDe[c]) caminho.Add(c);
        caminho.Reverse();
        return caminho;
    }

    // ------------------------------------------------------------------ montagem

    void Montar(int andarPara, bool caminhoNovo, bool mostrarSecreta, int[] mortes)
    {
        var sorteio = new System.Random(2024);
        for (int y = 0; y < Altura; y++)
            for (int x = 0; x < Desenho[y].Length; x++)
                MontarCelula(new Vector2Int(x, y), sorteio);

        // Pontos, números e personagens.
        int n = pontos.Count;
        desenhosDosPontos = new SpriteRenderer[n];
        personagens = new Transform[n];
        for (int i = 0; i < n; i++)
        {
            desenhosDosPontos[i] = ConstrutorDeFase.Visual("Ponto " + (i + 1), transform, Mundo(pontos[i]), "no_mapa", 2).GetComponent<SpriteRenderer>();
            var numero = new GameObject("Numero");
            numero.transform.SetParent(desenhosDosPontos[i].transform, false);
            var desenhoNumero = numero.AddComponent<SpriteRenderer>();
            desenhoNumero.sprite = FabricaDeSprites.SpriteDeTexto((i + 1).ToString(), new Color32(20, 20, 28, 255), new Color32(255, 255, 255, 0));
            desenhoNumero.sortingOrder = 3;

            var personagem = ConstrutorDeFase.Visual("Personagem", transform, LugarDoPersonagem(i, 0f), Personagens[i % Personagens.Length], 9);
            personagens[i] = personagem.transform;
            if (!EmPe(i)) personagem.transform.localScale = Vector3.one * 0.8f;

            if (i <= liberado && mortes != null && i < mortes.Length) Caveira(pontos[i], mortes[i]);
        }

        // A fase secreta: um ponto lilás com "?" e uma moeda girando em cima.
        if (pontoSecreto.HasValue)
        {
            desenhoSecreto = ConstrutorDeFase.Visual("PontoSecreto", transform, Mundo(pontoSecreto.Value), "no_mapa", 2).GetComponent<SpriteRenderer>();
            desenhoSecreto.color = CorSecreta;
            var numero = new GameObject("Numero");
            numero.transform.SetParent(desenhoSecreto.transform, false);
            var desenhoNumero = numero.AddComponent<SpriteRenderer>();
            desenhoNumero.sprite = FabricaDeSprites.SpriteDeTexto("?", Preto, new Color32(255, 255, 255, 0));
            desenhoNumero.sortingOrder = 3;
            personagemSecreto = ConstrutorDeFase.Visual("Moeda", transform, Mundo(pontoSecreto.Value) + new Vector3(0.95f, 0.75f, 0f), "moeda", 9).transform;
            bool aparece = secretaLiberada && !mostrarSecreta;
            desenhoSecreto.transform.localScale = aparece ? Vector3.one : Vector3.zero;
            personagemSecreto.gameObject.SetActive(aparece);
            if (secretaLiberada && mortes != null && mortes.Length > pontos.Count) Caveira(pontoSecreto.Value, mortes[pontos.Count]);
        }

        // O que já foi descoberto: em volta das fases liberadas e dos caminhos entre elas.
        // (O caminho novo e a fase nova ficam escondidos: vão aparecer com animação.)
        var descoberto = new bool[Largura, Altura];
        bool tudoDescoberto = liberado == n - 1 && !(caminhoNovo && andarPara == n - 1); // chegou ao fim: sem névoa
        for (int i = 0; i <= liberado && !tudoDescoberto; i++)
        {
            if (caminhoNovo && i == andarPara) continue;
            Descobrir(descoberto, pontos[i], RaioDoPonto);
            if (i > 0 && !(caminhoNovo && i == andarPara))
                foreach (Vector2Int c in caminhos[i - 1]) Descobrir(descoberto, c, RaioDoCaminho);
        }
        if (secretaLiberada && !mostrarSecreta)
        {
            Descobrir(descoberto, pontoSecreto.Value, RaioDoPonto - 1f);
            foreach (Vector2Int c in caminhoSecreto) Descobrir(descoberto, c, RaioDoCaminho);
        }
        if (!tudoDescoberto) CriarNevoa(descoberto);
        else
        {
            nevoa = new SpriteRenderer[Largura, Altura];
            tamanhoDaNevoa = new float[Largura, Altura];
        }

        if (caminhoNovo)
        {
            foreach (Vector2Int c in caminhos[andarPara - 1])
                if (terra.TryGetValue(c, out SpriteRenderer t)) t.color = new Color(1f, 1f, 1f, 0f);
            nascendo = andarPara;
            desenhosDosPontos[andarPara].transform.localScale = Vector3.zero;
        }

        DecorarCeu();
        SombrasDeNuvem();

        // O Subaru.
        desenhoSubaru = ConstrutorDeFase.Visual("Subaru", transform, PosicaoNoPonto(Selecionado), "jogador", 10).GetComponent<SpriteRenderer>();
        Subaru = desenhoSubaru.transform;

        if (andarPara >= 0 || mostrarSecreta) StartCoroutine(LiberarEAndar(andarPara, caminhoNovo, mostrarSecreta));
    }

    // Caveirinha com o número de mortes, embaixo do ponto.
    void Caveira(Vector2Int ponto, int mortes)
    {
        if (mortes <= 0) return;
        Vector3 lugar = Mundo(ponto) + new Vector3(-0.3f, -0.8f, 0f);
        var caveira = ConstrutorDeFase.Visual("Caveira", transform, lugar, "caveira", 4);
        caveira.transform.localScale = Vector3.one * 1.3f;
        var numero = new GameObject("Mortes");
        numero.transform.SetParent(transform, false);
        numero.transform.position = lugar + new Vector3(0.5f + 0.15f * (mortes.ToString().Length - 1), 0f, 0f);
        var desenho = numero.AddComponent<SpriteRenderer>();
        desenho.sprite = FabricaDeSprites.SpriteDeTexto(mortes.ToString(), Branco, Preto);
        desenho.sortingOrder = 4;
        numero.transform.localScale = Vector3.one * 0.8f;
    }

    bool EmPe(int i) // personagens com o pé no chão (arte com a base no pivô)
    {
        string nome = Personagens[i % Personagens.Length];
        return nome == "emilia" || nome == "beatrice";
    }

    void MontarCelula(Vector2Int c, System.Random sorteio)
    {
        Vector3 p = Mundo(c);
        int linha = c.y; // quanto mais embaixo, mais "na frente"
        switch (Celula(c))
        {
            case '~':
                Agua(p, sorteio);
                break;
            case '=':
                Agua(p, sorteio);
                ConstrutorDeFase.Visual("Ponte", transform, p, "ponte", OrdemDoCaminho, false);
                break;
            case '+':
                terra[c] = ConstrutorDeFase.Visual("Caminho", transform, p, "terra", OrdemDoCaminho, false).GetComponent<SpriteRenderer>();
                break;
            case 'T':
                var arvore = ConstrutorDeFase.Visual("Arvore", transform, p + Vector3.down * 0.5f, "arvore", linha - 4);
                Animacao.Adicionar(arvore, Animacao.Tipo.Balancar, 1.5f, 2.5f); // balançando no vento
                break;
            case '^':
                ConstrutorDeFase.Visual("Montanha", transform, p, "montanha", linha - 6);
                break;
            case 'h':
                ConstrutorDeFase.Visual("Casa", transform, p, "casa", linha - 6);
                break;
            case 'f':
                for (int i = 0; i < 2; i++)
                {
                    var flor = ConstrutorDeFase.Visual("Flor", transform, p + new Vector3((float)sorteio.NextDouble() - 0.5f, (float)sorteio.NextDouble() * 0.5f - 0.5f, 0f), "flor", -7);
                    Animacao.Adicionar(flor, Animacao.Tipo.Balancar, 2.5f, 6f);
                }
                break;
            case 'M':
                var mansao = ConstrutorDeFase.Visual("Mansao", transform, p + Vector3.down * 0.5f, "fundo_castelo", linha - 6, false);
                mansao.transform.localScale = Vector3.one * 0.28f;
                mansao.GetComponent<SpriteRenderer>().color = new Color(0.82f, 0.74f, 0.95f);
                break;
            case '.':
                if (sorteio.Next(8) == 0)
                    ConstrutorDeFase.Visual("Tufo", transform, p + Vector3.down * 0.3f, "tufo", -8);
                break;
        }
    }

    void Agua(Vector3 p, System.Random sorteio)
    {
        var agua = ConstrutorDeFase.Visual("Agua", transform, p, "agua_0", OrdemDaAgua, false).GetComponent<SpriteRenderer>();
        AnimacaoDeQuadros.Adicionar(agua, FabricaDeSprites.Quadros("agua", FabricaDeSprites.QuadrosDaAgua), 3f);
        if (sorteio.Next(6) == 0) // brilhinho do sol na água
        {
            var brilho = ConstrutorDeFase.Visual("Brilho", transform, p + new Vector3(0.2f, 0.2f, 0f), "brilho", OrdemDaAgua + 2, false);
            brilho.transform.localScale = Vector3.one * 0.6f;
            Animacao.Adicionar(brilho, Animacao.Tipo.Piscar, 3f, 0.9f);
        }
    }

    static void Descobrir(bool[,] descoberto, Vector2Int centro, float raio)
    {
        for (int y = 0; y < Altura; y++)
            for (int x = 0; x < Largura; x++)
                if (Vector2Int.Distance(new Vector2Int(x, y), centro) <= raio) descoberto[x, y] = true;
    }

    void CriarNevoa(bool[,] descoberto)
    {
        nevoa = new SpriteRenderer[Largura, Altura];
        tamanhoDaNevoa = new float[Largura, Altura];
        var sorteio = new System.Random(77);
        for (int y = 0; y < Altura; y++)
            for (int x = 0; x < Largura; x++)
            {
                if (descoberto[x, y] || Celula(new Vector2Int(x, y)) == 'c') continue;
                var bolota = ConstrutorDeFase.Visual("Nevoa", transform, Mundo(new Vector2Int(x, y)), "nevoa", OrdemDaNevoa, false);
                tamanhoDaNevoa[x, y] = 1.6f + (float)sorteio.NextDouble() * 0.4f;
                bolota.transform.localScale = Vector3.one * tamanhoDaNevoa[x, y];
                bolota.transform.rotation = Quaternion.Euler(0f, 0f, sorteio.Next(4) * 90f);
                nevoa[x, y] = bolota.GetComponent<SpriteRenderer>();
            }
    }

    // A névoa em volta de "centro" vai embora (com animação).
    void Revelar(Vector2Int centro, float raio)
    {
        for (int y = 0; y < Altura; y++)
            for (int x = 0; x < Largura; x++)
            {
                SpriteRenderer bolota = nevoa[x, y];
                if (bolota == null || Vector2Int.Distance(new Vector2Int(x, y), centro) > raio) continue;
                nevoa[x, y] = null;
                StartCoroutine(Sumir(bolota));
            }
    }

    static IEnumerator Sumir(SpriteRenderer bolota)
    {
        Vector3 escala = bolota.transform.localScale;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            float p = t / 0.5f;
            bolota.color = new Color(1f, 1f, 1f, 1f - p);
            bolota.transform.localScale = escala * (1f + p * 0.6f);
            yield return null;
        }
        Destroy(bolota.gameObject);
    }

    // O horizonte lá em cima, igual ao fundo das fases: céu, floresta e árvores ao longe com paralaxe
    // (andam mais devagar que o mapa quando a câmera rola), brilhinhos, a Baleia Branca e nuvens.
    void DecorarCeu()
    {
        int linhasDeCeu = 0;
        while (linhasDeCeu < Altura && Desenho[linhasDeCeu][0] == 'c') linhasDeCeu++;
        if (linhasDeCeu == 0) return;
        float horizonte = Altura - linhasDeCeu - 0.5f; // y onde o céu encontra o chão
        var corDoCeu = new Color32(130, 195, 240, 255);

        var ceu = ConstrutorDeFase.Visual("Ceu", transform, new Vector3(Largura / 2f, horizonte, 0f), "ceu", -26, false);
        ceu.transform.localScale = new Vector3((Largura + 60f) * FabricaDeSprites.PixelsPorUnidade, (linhasDeCeu + 1f) / 2f, 1f);

        Paralaxe.CriarHorizonte(transform, Largura, horizonte - 0.15f, 0.35f, corDoCeu, CorDoChao);

        var sorteio = new System.Random(5);
        for (int i = 0; i < Largura / 4; i++)
        {
            var brilho = ConstrutorDeFase.Visual("Brilho", transform,
                new Vector3(sorteio.Next(0, Largura), horizonte + 1f + (float)sorteio.NextDouble() * (linhasDeCeu - 1f), 0f), "brilho", -25, false);
            Animacao.Adicionar(brilho, Animacao.Tipo.Piscar, 3f, 0.7f);
        }

        // a Baleia Branca nadando lá longe, no céu
        var baleia = ConstrutorDeFase.Visual("Baleia", transform, new Vector3(Largura / 2f, horizonte + linhasDeCeu * 0.6f, 0f), "baleia", -23, false);
        baleia.transform.localScale = Vector3.one * 0.4f;
        baleia.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.75f);
        Animacao.Adicionar(baleia, Animacao.Tipo.Flutuar, 0.05f, Largura / 2f - 4f, 0.6f);

        // nuvens do céu
        for (int i = 0; i < 7; i++)
        {
            var nuvem = ConstrutorDeFase.Visual("Nuvem", transform, new Vector3(3f + i * 9f, horizonte + 1.2f + (i % 2) * 0.9f, 0f), "nuvem", -21, false);
            Animacao.Adicionar(nuvem, Animacao.Tipo.Flutuar, 0.3f, 0.8f, 0.5f);
        }
    }

    // Sombras de nuvem passando devagar pelo chão (escurecem tudo, até o Subaru).
    void SombrasDeNuvem()
    {
        for (int i = 0; i < 4; i++)
        {
            var sombra = ConstrutorDeFase.Visual("SombraDeNuvem", transform, new Vector3(Largura / 2f, 4.5f + i * 1.6f, 0f), "sombra_nuvem", 25, false);
            sombra.GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0.12f);
            sombra.transform.localScale = Vector3.one * (1.4f + i * 0.2f);
            Animacao.Adicionar(sombra, Animacao.Tipo.Flutuar, 0.035f + i * 0.01f, Largura / 2f);
        }
    }

    Vector2Int CelulaDoPonto(int fase) => fase == Fases.IndiceSecreto ? pontoSecreto.Value : pontos[fase];

    // O Subaru fica com os pés no centro do ponto.
    Vector3 PosicaoNoPonto(int fase) => Mundo(CelulaDoPonto(fase)) + Vector3.up * 0.45f;

    Vector3 LugarDoPersonagem(int i, float pulo) => Mundo(pontos[i]) + new Vector3(0.95f, (EmPe(i) ? -0.3f : 0.75f) + pulo, 0f);

    // ------------------------------------------------------------------ andar

    // direcao: +1 = próxima fase, -1 = anterior. Devolve false se não dá para ir (ainda escondida ou fim do caminho).
    public bool Mover(int direcao)
    {
        int destino = Selecionado + direcao;
        if (Andando || destino < 0 || destino > liberado || destino >= pontos.Count)
        {
            if (!Andando) GerenciadorDoJogo.Som("bloco", 0.4f);
            return false;
        }
        StartCoroutine(Andar(destino, Rota(destino)));
        return true;
    }

    // Do ponto da fase do segredo para a fase secreta (e de volta).
    public bool IrParaSecreta()
    {
        if (Andando || !secretaLiberada || Selecionado != Fases.FaseDoSegredo) return false;
        StartCoroutine(Andar(Fases.IndiceSecreto, caminhoSecreto));
        return true;
    }

    public bool VoltarDaSecreta()
    {
        if (Andando || Selecionado != Fases.IndiceSecreto) return false;
        var volta = new List<Vector2Int>(caminhoSecreto);
        volta.Reverse();
        volta.RemoveAt(0);
        volta.Add(pontos[Fases.FaseDoSegredo]);
        StartCoroutine(Andar(Fases.FaseDoSegredo, volta));
        return true;
    }

    // Lista de células por onde o Subaru passa do ponto atual até o vizinho "destino".
    List<Vector2Int> Rota(int destino)
    {
        if (destino == Selecionado + 1) return caminhos[Selecionado];
        var volta = new List<Vector2Int>(caminhos[destino]);
        volta.Reverse();
        volta.RemoveAt(0);         // tira o ponto de onde ele sai
        volta.Add(pontos[destino]); // e põe o de chegada
        return volta;
    }

    IEnumerator Andar(int destino, List<Vector2Int> rota)
    {
        Andando = true;
        GerenciadorDoJogo.Som("pulo", 0.3f);
        Vector3 pes = PosicaoNoPonto(Selecionado);
        foreach (Vector2Int celula in rota)
        {
            Vector3 alvo = Mundo(celula) + Vector3.up * 0.45f;
            if (Mathf.Abs(alvo.x - pes.x) > 0.01f) desenhoSubaru.flipX = alvo.x < pes.x;
            while (pes != alvo)
            {
                pes = Vector3.MoveTowards(pes, alvo, velocidade * Time.deltaTime);
                timerPasso += Time.deltaTime;
                // pulinhos enquanto anda, estilo mapa do Mario
                Subaru.position = pes + Vector3.up * Mathf.Abs(Mathf.Sin(timerPasso * 14f)) * 0.15f;
                desenhoSubaru.sprite = FabricaDeSprites.Pegar((int)(timerPasso * 10f) % 2 == 0 ? "jogador" : "jogador_andando");
                yield return null;
            }
            Efeitos.Poeira(transform, pes + Vector3.down * 0.4f, 1, 1f);
        }
        Subaru.position = pes;
        desenhoSubaru.sprite = FabricaDeSprites.Pegar("jogador");
        Selecionado = destino;
        Andando = false;
    }

    // Acabou de passar de fase: a névoa some ao longo do caminho novo, a fase nova aparece e o Subaru vai até lá.
    // Se a fase secreta acabou de ser liberada, ela aparece primeiro.
    IEnumerator LiberarEAndar(int destino, bool caminhoNovo, bool mostrarSecreta)
    {
        Andando = true;
        yield return new WaitForSeconds(0.7f); // espera a transição de tela

        if (mostrarSecreta)
        {
            foreach (Vector2Int c in caminhoSecreto)
            {
                Revelar(c, RaioDoCaminho);
                GerenciadorDoJogo.Som("moeda", 0.12f);
                yield return new WaitForSeconds(0.1f);
            }
            Revelar(pontoSecreto.Value, RaioDoPonto - 1f);
            GerenciadorDoJogo.Som("conquista");
            yield return Nascer(desenhoSecreto.transform);
            personagemSecreto.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.8f);
        }
        if (destino < 0)
        {
            Andando = false;
            yield break;
        }

        if (caminhoNovo)
        {
            foreach (Vector2Int c in caminhos[destino - 1])
            {
                if (terra.TryGetValue(c, out SpriteRenderer t)) t.color = Color.white;
                Revelar(c, RaioDoCaminho);
                if (c != pontos[destino]) GerenciadorDoJogo.Som("moeda", 0.12f);
                yield return new WaitForSeconds(0.07f);
            }

            // a fase nova "nasce" com um pulinho e a névoa em volta dela vai embora
            // (na última fase a névoa do mapa INTEIRO vai embora)
            bool ultima = destino == pontos.Count - 1;
            Revelar(pontos[destino], ultima ? Largura : RaioDoPonto);
            if (ultima) CameraSeguir.Tremer(0.1f, 0.6f);
            GerenciadorDoJogo.Som("mola", 0.6f);
            yield return Nascer(desenhosDosPontos[destino].transform);
            nascendo = -1;
            yield return new WaitForSeconds(0.3f);
        }

        yield return Andar(destino, Rota(destino));
    }

    // O ponto "nasce" com um pulinho e um pouco de poeira.
    IEnumerator Nascer(Transform ponto)
    {
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            ponto.localScale = Vector3.one * Mathf.Sin(t / 0.35f * Mathf.PI * 0.75f) / 0.7071f;
            yield return null;
        }
        ponto.localScale = Vector3.one;
        Efeitos.Poeira(transform, ponto.position, 8, 2.5f);
    }

    // ------------------------------------------------------------------ animação

    void Update()
    {
        for (int i = 0; i < desenhosDosPontos.Length; i++)
        {
            desenhosDosPontos[i].color = i < concluidas ? CorConcluida : CorLiberada;

            // personagem pulando de leve; o do ponto escolhido pula mais
            float altura = i == Selecionado && !Andando ? 0.18f : 0.06f;
            personagens[i].localPosition = LugarDoPersonagem(i, Mathf.Abs(Mathf.Sin(Time.time * 4f + i)) * altura);
            if (Personagens[i % Personagens.Length] == "serra") personagens[i].Rotate(0f, 0f, -180f * Time.deltaTime);

            // o ponto em que o Subaru está "pulsa" de leve
            if (i != nascendo)
                desenhosDosPontos[i].transform.localScale = Vector3.one * (i == Selecionado && !Andando ? 1f + 0.08f * Mathf.Sin(Time.time * 6f) : 1f);
        }

        // a fase secreta: a moeda gira em cima do ponto, e o ponto pulsa quando o Subaru está nele
        if (personagemSecreto != null && personagemSecreto.gameObject.activeSelf)
        {
            personagemSecreto.localScale = new Vector3(Mathf.Cos(Time.time * 3f), 1f, 1f);
            if (Selecionado == Fases.IndiceSecreto && !Andando)
                desenhoSecreto.transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.time * 6f));
            else desenhoSecreto.transform.localScale = Vector3.one;
        }

        // a névoa "respira" devagar
        if (nevoa != null)
            for (int y = 0; y < Altura; y++)
                for (int x = 0; x < Largura; x++)
                    if (nevoa[x, y] != null)
                        nevoa[x, y].transform.localScale = Vector3.one * tamanhoDaNevoa[x, y] * (1f + 0.06f * Mathf.Sin(Time.time * 1.3f + x * 0.7f + y * 1.1f));

        // parado, o Subaru respira
        if (!Andando) Subaru.localScale = new Vector3(1f, 1f + 0.04f * Mathf.Sin(Time.time * 3f), 1f);
    }
}
