using System.Collections.Generic;
using UnityEngine;

public class InfoFase
{
    public Vector3 inicio;
    public int largura;
    public int altura;
    public List<float> inversores = new List<float>(); // posições x das linhas 'X'
    public List<Transform> bandeiras = new List<Transform>(); // todas as bandeiras (verdadeiras ou não)
    public int totalDeMoedas;  // moedas '$' + blocos '?' (para a fase secreta: "pegue TODAS")
    public bool temPortas;     // é a Biblioteca Proibida?
}

// Lê o mapa de texto de uma fase (veja Fases.cs) e cria todos os objetos na cena.
public static class ConstrutorDeFase
{
    // As nuvens (as de enfeite E a assassina) andam um pouquinho com a câmera, como o fundo.
    public const float ParalaxeDasNuvens = 0.25f;

    // tentativa = quantas vezes o jogador já morreu nesta fase (escolhe a versão do mapa)
    public static InfoFase Construir(Fase fase, Transform raiz, int tentativa)
    {
        string[] mapa = AplicarMudancas(fase, tentativa);
        int altura = mapa.Length;
        int largura = 0;
        foreach (string linha in mapa) largura = Mathf.Max(largura, linha.Length);

        var info = new InfoFase { largura = largura, altura = altura, inicio = new Vector3(1f, 2f, 0f) };

        char Celula(int x, int linha)
        {
            if (linha < 0 || linha >= altura || x < 0 || x >= mapa[linha].Length) return ' ';
            return mapa[linha][x];
        }
        Vector3 Posicao(int x, int linha) => new Vector3(x, altura - 1 - linha, 0f);
        bool PareceChao(char c) => c == '#' || c == 'C' || c == 'F'; // falso e o que cai são IGUAIS ao chão de verdade
        // De vez em quando um bloco com rachadura ou com florzinha, para o chão não ficar repetitivo.
        // (escolhido pela posição: o chão falso e o que cai continuam IGUAIS ao de verdade)
        string SpriteDoChao(int x, int linha)
        {
            bool topo = !PareceChao(Celula(x, linha - 1));
            bool variado = (x * 7 + linha * 13) % 9 == 0;
            if (topo) return variado ? "chao_topo_florido" : "chao_topo";
            return variado ? "chao_rachado" : "chao";
        }

        // Todo o chão firme vira UM colisor só (CompositeCollider2D).
        // Isso evita o jogador "enganchar" nas emendas entre os blocos.
        var chaoSolido = new GameObject("ChaoSolido");
        chaoSolido.transform.SetParent(raiz, false);
        var corpoChao = chaoSolido.AddComponent<Rigidbody2D>();
        corpoChao.bodyType = RigidbodyType2D.Static;
        var composto = chaoSolido.AddComponent<CompositeCollider2D>();
        composto.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composto.generationType = CompositeCollider2D.GenerationType.Manual;

        var celulasQueCaem = new HashSet<Vector2Int>();
        var celulasQueFogem = new HashSet<Vector2Int>();
        var celulasDePlataforma = new Dictionary<Vector2Int, char>(); // 'j', 'J' e 'D' (a letra diz o tipo)
        var celulasDoRitmo = new Dictionary<Vector2Int, char>();      // 'a', 'b' e 'A' (blocos do ritmo)
        var celulasDeVento = new HashSet<Vector2Int>();               // 'y' (rajada da Ram)
        var celulasDeRedemoinho = new HashSet<Vector2Int>();          // 'n' (redemoinho)
        var placas = new List<(Vector3 lugar, bool beatrice)>();
        var portas = new List<Porta>();

        var fujonas = new List<BandeiraFujona>();
        Vector3? destinoDaFujona = null;

        for (int linha = 0; linha < altura; linha++)
        {
            for (int x = 0; x < mapa[linha].Length; x++)
            {
                char c = mapa[linha][x];
                Vector3 pos = Posicao(x, linha);
                Vector3 chaoDaCelula = pos + Vector3.down * 0.5f;

                if (char.IsDigit(c)) // porta da Beatrice, com o número dela
                {
                    var porta = Criar<Porta>("Porta " + c, raiz, chaoDaCelula);
                    porta.Numerar(c);
                    portas.Add(porta);
                    continue;
                }

                switch (c)
                {
                    case '#': BlocoSolido(chaoSolido.transform, pos, SpriteDoChao(x, linha)); break;
                    case 'B': BlocoSolido(chaoSolido.transform, pos, "tijolo"); break;
                    case 'F': Visual("ChaoFalso", raiz, pos, SpriteDoChao(x, linha), 0); break;
                    case 'C': celulasQueCaem.Add(new Vector2Int(x, linha)); break;
                    case 'P': info.inicio = pos; break;
                    case '?': Criar<BlocoSurpresa>("BlocoMoeda", raiz, pos); info.totalDeMoedas++; break;
                    case 'K': Criar<BlocoSurpresa>("BlocoPegadinha", raiz, pos).soltaInimigo = true; break;
                    case 'I': Criar<BlocoInvisivel>("BlocoInvisivel", raiz, pos); break;
                    case '^': Criar<Espinho>("Espinho", raiz, pos); break;
                    case 'h': Criar<EspinhoEscondido>("EspinhoEscondido", raiz, pos); break;
                    case 'v': Criar<EspinhoQueCai>("EspinhoQueCai", raiz, pos); break;
                    case 'E': Criar<Inimigo>("Inimigo", raiz, pos); break;
                    case 'S': Criar<Mola>("Mola", raiz, pos); break;
                    case 'T': Criar<Esmagador>("Esmagador", raiz, pos); break;
                    case '<': Criar<Serra>("SerraTraseira", raiz, pos).direcao = 1; break;
                    case '>': Criar<Serra>("SerraDianteira", raiz, pos).direcao = -1; break;
                    case 'G': info.bandeiras.Add(Criar<Bandeira>("Bandeira", raiz, chaoDaCelula).transform); break;
                    case 'R': fujonas.Add(Criar<BandeiraFujona>("BandeiraFujona", raiz, chaoDaCelula)); info.bandeiras.Add(fujonas[fujonas.Count - 1].transform); break;
                    case '*': destinoDaFujona = chaoDaCelula; break;
                    case 'Z': info.bandeiras.Add(Criar<BandeiraFalsa>("BandeiraFalsa", raiz, chaoDaCelula).transform); break;
                    case 'o': Criar<NuvemAssassina>("NuvemAssassina", raiz, pos + Vector3.right * 0.5f); break;
                    case 'i': placas.Add((pos, false)); break;
                    case 'Y': placas.Add((pos, true)); break;
                    case '$': Criar<Moeda>("Moeda", raiz, pos); info.totalDeMoedas++; break;
                    case 'm': Criar<MoedaAssassina>("MoedaAssassina", raiz, pos); break;
                    case 'e': Criar<Inimigo>("InimigoDisfarcado", raiz, pos).espinhoso = true; break;
                    case 'M': celulasQueFogem.Add(new Vector2Int(x, linha)); break;
                    case 'j': case 'J': case 'D': celulasDePlataforma[new Vector2Int(x, linha)] = c; break; // plataformas móveis
                    case 'a': case 'b': case 'A': celulasDoRitmo[new Vector2Int(x, linha)] = c; break; // blocos do ritmo (juntados depois do laço)
                    case 't': Criar<EsmagadorDoRitmo>("EsmagadorDoRitmo", raiz, pos); break;
                    case 'l': Criar<FacasDaElsa>("Elsa", raiz, pos); break;
                    case 'k': Criar<FacasDaElsa>("ElsaDoLeque", raiz, pos).Montar(true); break;
                    case 'N': Criar<MiasmaDaBruxa>("MiasmaDaBruxa", raiz, pos); break;
                    case 'y': celulasDeVento.Add(new Vector2Int(x, linha)); break;
                    case 'n': celulasDeRedemoinho.Add(new Vector2Int(x, linha)); break;
                    case 'Q': case 'q': // com chão em cima = ENTERRADO (disfarçado de chão)
                        Criar<MangualDaRem>("Mangual", raiz, pos).Montar(x, c == 'q', PareceChao(Celula(x, linha - 1)) ? SpriteDoChao(x, linha) : null);
                        break;
                    case 'W': info.bandeiras.Add(Criar<BandeiraVolta>("BandeiraVolta", raiz, chaoDaCelula).transform); break;
                    case 'X': info.inversores.Add(x); break;
                    case 'r': Criar<Coelho>("Coelho", raiz, pos); break;
                    case 'w': Criar<BaleiaBranca>("BaleiaBranca", raiz, pos); break;
                    case 'L': Criar<Emilia>("Emilia", raiz, pos); break;
                    case 's': Criar<PontoDeSave>("PontoDeSave", raiz, chaoDaCelula); break;
                    case 'U':
                        // o cano desce até encontrar chão
                        int alturaDoCano = 1;
                        while (linha + alturaDoCano < altura && !PareceChao(Celula(x, linha + alturaDoCano)) && Celula(x, linha + alturaDoCano) != 'B')
                            alturaDoCano++;
                        Criar<Cano>("Cano", raiz, pos).Montar(alturaDoCano);
                        break;
                }
            }
        }

        // Chão que cai: blocos 'C' encostados uns nos outros caem TODOS juntos.
        foreach (List<Vector2Int> grupo in AgruparVizinhos(celulasQueCaem))
        {
            var objeto = new GameObject("ChaoQueCai");
            objeto.transform.SetParent(raiz, false);
            var chaoQueCai = objeto.AddComponent<ChaoQueCai>();
            foreach (Vector2Int celula in grupo)
                chaoQueCai.AdicionarBloco(Posicao(celula.x, celula.y), FabricaDeSprites.Pegar(SpriteDoChao(celula.x, celula.y)));
        }

        // Vento da Ram: células 'y' (ou 'n') encostadas viram UMA zona (o retângulo em volta delas).
        foreach (List<Vector2Int> grupo in AgruparVizinhos(celulasDeVento))
            VentoDaRam.CriarZona(raiz, VentoDaRam.Tipo.Rajada, grupo, altura);
        foreach (List<Vector2Int> grupo in AgruparVizinhos(celulasDeRedemoinho))
            VentoDaRam.CriarZona(raiz, VentoDaRam.Tipo.Redemoinho, grupo, altura);

        // Bloco que foge: blocos 'M' encostados fogem juntos.
        foreach (List<Vector2Int> grupo in AgruparVizinhos(celulasQueFogem))
        {
            var objeto = new GameObject("BlocoQueFoge");
            objeto.transform.SetParent(raiz, false);
            var blocoQueFoge = objeto.AddComponent<BlocoQueFoge>();
            foreach (Vector2Int celula in grupo)
                blocoQueFoge.AdicionarBloco(Posicao(celula.x, celula.y));
        }

        // Plataformas móveis: blocos 'j' (ou 'J', ou 'D') encostados NA MESMA LINHA viram uma plataforma só.
        // Cada plataforma é criada a partir do seu bloco mais à esquerda, no centro do grupo.
        foreach (KeyValuePair<Vector2Int, char> celula in celulasDePlataforma)
        {
            Vector2Int inicio = celula.Key;
            char letra = celula.Value;
            if (celulasDePlataforma.TryGetValue(inicio + Vector2Int.left, out char vizinha) && vizinha == letra) continue; // não é a primeira do grupo
            int blocos = 1;
            while (celulasDePlataforma.TryGetValue(new Vector2Int(inicio.x + blocos, inicio.y), out char seguinte) && seguinte == letra)
                blocos++;
            Vector3 centro = Posicao(inicio.x, inicio.y) + Vector3.right * ((blocos - 1) / 2f);
            Criar<PlataformaMovel>("PlataformaMovel", raiz, centro).Montar(PlataformaMovel.TipoDaLetra(letra), blocos);
        }

        // Blocos do ritmo: letras iguais encostadas NA MESMA LINHA viram um bloco só (um colisor só, sem emendas).
        BlocoDoRitmo.MontarTodos(raiz, celulasDoRitmo, altura);

        foreach (BandeiraFujona fujona in fujonas)
            if (destinoDaFujona.HasValue) fujona.destino = destinoDaFujona.Value;

        // Placas (Puck e Beatrice): textos na ordem da esquerda para a direita.
        placas.Sort((a, b) => a.lugar.x.CompareTo(b.lugar.x));
        for (int i = 0; i < placas.Count; i++)
        {
            var placa = Criar<Placa>("Placa", raiz, placas[i].lugar);
            placa.texto = fase.placas != null && i < fase.placas.Length ? fase.placas[i] : "...";
            if (placas[i].beatrice) placa.VirarBeatrice();
        }

        // Portas da Beatrice: liga os pares da lista "portas" da fase (ida e volta, sempre iguais).
        if (portas.Count > 0)
        {
            info.temPortas = true;
            LigarPortas(fase, portas);
        }

        // Paredes invisíveis nas bordas da fase.
        ParedeInvisivel(chaoSolido.transform, new Vector3(-1f, altura / 2f, 0f), altura * 3);
        ParedeInvisivel(chaoSolido.transform, new Vector3(largura, altura / 2f, 0f), altura * 3);
        composto.GenerateGeometry();

        Decorar(raiz, mapa, largura, altura, fase.nome.GetHashCode());
        return info;
    }

    // ------------------------------------------------------------------ ajudantes

    // "1-4" = a porta 1 leva para a 4 e a 4 volta para a 1.
    static void LigarPortas(Fase fase, List<Porta> portas)
    {
        Porta Achar(char numero) => portas.Find(p => p.numero == numero);
        foreach (string par in fase.portas ?? new string[0])
        {
            Porta a = par.Length == 3 ? Achar(par[0]) : null, b = par.Length == 3 ? Achar(par[2]) : null;
            if (a == null || b == null)
            {
                Debug.LogWarning($"Par de portas inválido na fase \"{fase.nome}\": {par}");
                continue;
            }
            a.destino = b;
            b.destino = a;
        }
        foreach (Porta porta in portas)
            if (porta.destino == null) Debug.LogWarning($"A porta {porta.numero} da fase \"{fase.nome}\" não leva a lugar nenhum.");
    }

    // Devolve o mapa da fase já com as trocas da tentativa atual (veja "MUDANÇAS" em Fases.cs).
    static string[] AplicarMudancas(Fase fase, int tentativa)
    {
        if (fase.mudancas == null || fase.mudancas.Length == 0 || tentativa <= 0) return fase.mapa;

        string[] trocas = fase.mudancas[(tentativa - 1) % fase.mudancas.Length];
        var linhas = new char[fase.mapa.Length][];
        int largura = 0;
        foreach (string linha in fase.mapa) largura = Mathf.Max(largura, linha.Length);
        for (int i = 0; i < fase.mapa.Length; i++) linhas[i] = fase.mapa[i].PadRight(largura).ToCharArray();

        foreach (string troca in trocas)
        {
            string[] partes = troca.Split(',');
            int x = int.Parse(partes[0]);
            int linha = int.Parse(partes[1]);
            char c = partes[2][0] == '.' ? ' ' : partes[2][0];
            if (linha >= 0 && linha < linhas.Length && x >= 0 && x < largura) linhas[linha][x] = c;
            else Debug.LogWarning($"Mudança fora do mapa na fase \"{fase.nome}\": {troca}");
        }

        var resultado = new string[linhas.Length];
        for (int i = 0; i < linhas.Length; i++) resultado[i] = new string(linhas[i]).TrimEnd();
        return resultado;
    }

    public static T Criar<T>(string nome, Transform pai, Vector3 posicao) where T : Component
    {
        var objeto = new GameObject(nome);
        objeto.transform.SetParent(pai, false);
        objeto.transform.position = posicao;
        return objeto.AddComponent<T>();
    }

    // Cria um objeto só com desenho. Com sombra = true ele ganha a sombrinha projetada (veja Sombra.cs).
    public static GameObject Visual(string nome, Transform pai, Vector3 posicao, string sprite, int ordem, bool sombra = true)
    {
        var objeto = new GameObject(nome);
        objeto.transform.SetParent(pai, false);
        objeto.transform.position = posicao;
        var visual = objeto.AddComponent<SpriteRenderer>();
        visual.sprite = FabricaDeSprites.Pegar(sprite);
        visual.sortingOrder = ordem;
        if (sombra) Sombra.Adicionar(visual);
        return objeto;
    }

    static void BlocoSolido(Transform chao, Vector3 posicao, string sprite)
    {
        var bloco = Visual("Bloco", chao, posicao, sprite, 0);
        var colisor = bloco.AddComponent<BoxCollider2D>();
        colisor.size = Vector2.one;
        colisor.compositeOperation = Collider2D.CompositeOperation.Merge;
    }

    static void ParedeInvisivel(Transform chao, Vector3 posicao, float altura)
    {
        var parede = new GameObject("ParedeInvisivel");
        parede.transform.SetParent(chao, false);
        parede.transform.position = posicao;
        var colisor = parede.AddComponent<BoxCollider2D>();
        colisor.size = new Vector2(1f, altura);
        colisor.compositeOperation = Collider2D.CompositeOperation.Merge;
    }

    static List<List<Vector2Int>> AgruparVizinhos(HashSet<Vector2Int> celulas)
    {
        var grupos = new List<List<Vector2Int>>();
        var visitadas = new HashSet<Vector2Int>();
        Vector2Int[] direcoes = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int inicio in celulas)
        {
            if (visitadas.Contains(inicio)) continue;
            var grupo = new List<Vector2Int>();
            var fila = new Queue<Vector2Int>();
            fila.Enqueue(inicio);
            visitadas.Add(inicio);
            while (fila.Count > 0)
            {
                Vector2Int atual = fila.Dequeue();
                grupo.Add(atual);
                foreach (Vector2Int d in direcoes)
                {
                    Vector2Int vizinha = atual + d;
                    if (celulas.Contains(vizinha) && visitadas.Add(vizinha)) fila.Enqueue(vizinha);
                }
            }
            grupos.Add(grupo);
        }
        return grupos;
    }

    // Nuvens, tufos de grama, flores e brilhinhos de enfeite (sem colisão).
    // Repare que as nuvens de enfeite são IGUAIS à nuvem assassina. ;)
    static void Decorar(Transform raiz, string[] mapa, int largura, int altura, int semente)
    {
        var decoracao = new GameObject("Decoracao").transform;
        decoracao.SetParent(raiz, false);
        var sorteio = new System.Random(semente);

        for (int x = sorteio.Next(2, 6); x < largura + 4; x += sorteio.Next(7, 13))
        {
            var nuvem = Visual("Nuvem", decoracao, new Vector3(x, altura - 2 - sorteio.Next(0, 5), 0f), "nuvem", -10, false);
            Animacao.Adicionar(nuvem, Animacao.Tipo.Flutuar, 0.4f, 0.4f, ParalaxeDasNuvens);
        }

        // Enfeites em cima do chão: capim, flores, arbustos, pedras, cogumelos, cercas e lampiões (que iluminam).
        int ultimoLampiao = -100;
        for (int x = sorteio.Next(0, 3); x < largura; x += sorteio.Next(2, 5))
        {
            int linha = LinhaDoChao(mapa, x, altura);
            if (linha < 0) continue;
            Vector3 base_ = new Vector3(x + (float)(sorteio.NextDouble() - 0.5) * 0.4f, altura - 1 - linha + 0.5f, 0f);
            bool chaoDosLados = LinhaDoChao(mapa, x - 1, altura) == linha && LinhaDoChao(mapa, x + 1, altura) == linha;

            int sorte = sorteio.Next(100);
            if (sorte < 5 && x - ultimoLampiao > 10 && chaoDosLados)
            {
                var lampiao = Visual("Lampiao", decoracao, base_, "lampiao", -7);
                Luzes.Ponto(lampiao.transform, Luzes.Quente, 4.5f, 0.85f, Vector3.up * 2.15f);
                ultimoLampiao = x;
            }
            else if (sorte < 18 && chaoDosLados) Visual("Arbusto", decoracao, base_, "arbusto", -7);
            else if (sorte < 26 && chaoDosLados) Visual("Cerca", decoracao, base_, "cerca", -7);
            else if (sorte < 35) Visual("Pedra", decoracao, base_, "pedra", -7);
            else if (sorte < 42) Visual("Cogumelo", decoracao, base_, "cogumelo", -7);
            else
            {
                var enfeite = Visual("Enfeite", decoracao, base_, sorte < 65 ? "flor" : "tufo", -8);
                Animacao.Adicionar(enfeite, Animacao.Tipo.Balancar, 2.5f, 6f);
            }
        }

        // Brilhinhos no céu, como no fundo dos ímãs de Re:Zero.
        for (int i = 0; i < largura / 3; i++)
        {
            var brilho = Visual("Brilho", decoracao, new Vector3(sorteio.Next(0, largura), altura - 1 - sorteio.Next(0, 9), 0f), "brilho", -11, false);
            Animacao.Adicionar(brilho, Animacao.Tipo.Piscar, 3f, 0.7f);
        }
    }

    // A linha do primeiro chão firme ('#') de cima para baixo nesta coluna, com espaço livre em cima.
    // -1 se não tiver (buraco, coluna fora da fase ou algo em cima).
    static int LinhaDoChao(string[] mapa, int x, int altura)
    {
        if (x < 0) return -1;
        for (int linha = 1; linha < altura; linha++)
        {
            char c = x < mapa[linha].Length ? mapa[linha][x] : ' ';
            char acima = x < mapa[linha - 1].Length ? mapa[linha - 1][x] : ' ';
            bool livre = acima == ' ' || acima == 'y' || acima == 'n'; // vento não é chão: nasce capim embaixo
            if (c == '#' && livre) return linha;
            if (c != ' ' && c != 'y' && c != 'n') return -1;
        }
        return -1;
    }
}
