using System.Collections;
using UnityEngine;

// '{' - O TUBARÃO-MARTELO, o ferreiro do fundo (chefe da fase 4 do Mar). Arena de 22 blocos num bolsão de ar,
// chão LISO. Os ataques (todos com aviso):
//  MARTELADA: a marca no chão segue você e trava; ele salta em cima e, ao bater, solta DUAS ONDAS de choque rente
//     ao chão, uma para cada lado (pule por cima).
//  BIGORNAS: marcas no chão; as bigornas despencam do teto ali (saia de baixo).
//  BRASAS: ele cospe brasas em arco em 3 marcas perto de você.
//  No fim de cada rodada ele bate o martelo com tanta força que a cabeça (o martelo) FICA PRESA no chão: tonto.
//  É A HORA: pule na cabeça dele.
public class ChefeMartelo : ChefeDoMar
{
    static readonly float[] VelocidadeDaOnda = { 8f, 9.5f, 11f };

    public override string Nome => "TUBARÃO-MARTELO";
    public override Musicas.Tema Musica => Musicas.Tema.Capita;
    protected override string SpriteNormal => "martelo";
    protected override string SpriteTonto => "martelo_tonto";
    protected override string ConquistaAoVencer => "martelo_vencido";
    protected override string FalaDeEntrada => Idioma.T("O TUBARÃO-MARTELO! (ele forja âncoras com a cabeça)", "THE HAMMERHEAD! (he forges anchors with his head)");
    protected override string DicaDeEntrada => Idioma.T("Pule as ONDAS. Quando a cabeça dele PRENDER no chão, pule nela!", "Jump the WAVES. When his head gets STUCK in the floor, stomp it!");
    protected override string FalaDaEtapa2 => Idioma.T("ETAPA 2/3 - a forja esquentou!", "STAGE 2/3 - the forge heated up!");
    protected override string FalaDaEtapa3 => Idioma.T("ETAPA 3/3 - O MARTELO FICOU FURIOSO!!", "STAGE 3/3 - THE HAMMERHEAD IS FURIOUS!!");
    protected override string FalaDaDerrota => Idioma.T("O MARTELO DESMAIOU! (que dor de cabeça)", "THE HAMMERHEAD FAINTED! (what a headache)");
    protected override float TopoDaCabeca => 1.75f;
    protected override float MeiaLarguraDaCabeca => 1.1f;
    protected override float Escala => 1.6f;

    protected override IEnumerator Rodada(int qual)
    {
        switch (qual)
        {
            case 0: // etapa 1: um de cada vez
                yield return Martelada(1);
                yield return Bigornas(new[] { -2f, 0f, 2f }, 1);
                yield return Brasas();
                yield return Martelada(1);
                break;
            case 1:
                yield return Bigornas(new[] { -3f, -1f, 1f, 3f }, 1);
                yield return Martelada(2);
                yield return Brasas();
                yield return Martelada(1);
                break;
            case 2: // etapa 2: combinações
                yield return Martelada(2);
                Paralelo(Bigornas(new[] { -4f, 4f }, 1));
                yield return Brasas();
                yield return EsperarTodos();
                yield return Martelada(1);
                break;
            case 3:
                yield return Bigornas(new[] { -3f, -1f, 1f, 3f }, 2);
                yield return Martelada(2);
                yield return Brasas();
                yield return Martelada(1);
                break;
            case 4: // etapa 3: tudo junto e mais rápido
                yield return Martelada(3);
                Paralelo(Bigornas(new[] { -5f, -2f, 2f, 5f }, 1));
                yield return Brasas();
                yield return EsperarTodos();
                yield return Martelada(1);
                break;
            default:
                Paralelo(Brasas());
                yield return Bigornas(new[] { -3f, 0f, 3f }, 2);
                yield return EsperarTodos();
                yield return Martelada(3);
                break;
        }
        yield return Tontear();
    }

    // n marteladas seguidas; a última de cada rodada é a que prende a cabeça (aí ele não solta onda: vem o Tontear)
    IEnumerator Martelada(int vezes)
    {
        for (int i = 0; i < vezes; i++)
        {
            // a marca segue você e trava (vermelha)
            var marca = Desenho("MarcaDaMartelada", "baleia_alvo", new Vector3(AlvoX(), chao + 0.1f, 0f), OrdemDosAvisos);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                marca.transform.position = new Vector3(Mathf.MoveTowards(marca.transform.position.x, AlvoX(), 9f * Time.deltaTime), chao + 0.1f, 0f);
                yield return null;
            }
            marca.color = new Color(1f, 0.3f, 0.3f);
            yield return new WaitForSeconds(Aviso * 0.6f);
            float x = marca.transform.position.x;
            Destroy(marca.gameObject);
            yield return Saltar(x, 3.5f, 0.55f, 0.9f);
            // as ondas de choque saem para os dois lados
            Paralelo(OndaNoChao(corpo.position.x - 1.2f, -1f, VelocidadeDaOnda[etapa]));
            Paralelo(OndaNoChao(corpo.position.x + 1.2f, 1f, VelocidadeDaOnda[etapa]));
            yield return new WaitForSeconds(AvisoDePulo);
        }
    }

    IEnumerator Bigornas(float[] deslocamentos, int vezes)
    {
        for (int v = 0; v < vezes; v++)
        {
            float centro = AlvoX();
            foreach (float d in deslocamentos)
            {
                float x = Mathf.Clamp(centro + d + (v % 2) * 1f, esquerda + 0.7f, direita - 0.7f);
                Paralelo(CairDoTeto(x, "bigorna", Aviso + 0.25f, 0.5f));
            }
            yield return new WaitForSeconds(Aviso + 0.6f);
        }
    }

    IEnumerator Brasas()
    {
        OlharParaOJogador();
        float alvo = AlvoX();
        GerenciadorDoJogo.Som("risada", 0.4f);
        foreach (float d in new[] { -2f, 0f, 2f })
            Paralelo(Brasa(Mathf.Clamp(alvo + d, esquerda + 0.6f, direita - 0.6f)));
        yield return new WaitForSeconds(Aviso + 0.9f);
    }

    IEnumerator Brasa(float x)
    {
        Paralelo(MarcarChao(x, Aviso + 0.2f));
        yield return new WaitForSeconds(Aviso * 0.5f);
        Vector3 de = corpo.position + Vector3.up * 1.2f, ate = new Vector3(x, chao + 0.3f, 0f);
        var brasa = Desenho("Brasa", "brasa", de, OrdemDoChefe + 1);
        Zona z = NovaZona(Vector2.zero, Vector2.zero, 0.3f);
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            float p = t / 0.6f;
            Vector3 lugar = Vector3.Lerp(de, ate, p) + Vector3.up * 3f * p * (1f - p);
            z.a = z.b = lugar;
            if (brasa != null) brasa.transform.position = lugar;
            yield return null;
        }
        // queima o chão um tiquinho
        z.a = new Vector2(x - 0.3f, chao + 0.2f);
        z.b = new Vector2(x + 0.3f, chao + 0.2f);
        yield return new WaitForSeconds(0.5f);
        zonas.Remove(z);
        if (brasa != null) Destroy(brasa.gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("martelo", () => FabricaDeSprites.DeArte(ArteMartelo, FabricaDeSprites.Base));
        FabricaDeSprites.Registrar("martelo_tonto", () => FabricaDeSprites.DeArte(ArteMarteloTonto, FabricaDeSprites.Base));
        FabricaDeSprites.Registrar("bigorna", () => FabricaDeSprites.DeArte(ArteBigorna, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("brasa", () => FabricaDeSprites.DeArte(new[] { ".oo.", "oyyo", "oyyo", ".oo." }, FabricaDeSprites.Centro));
    }

    static readonly string[] ArteMartelo =
    {
        "kkkkkkkkkkkkkkkkkkkkkkkkkkkkkk..",
        "kSSSSSSSSSSSSSSSSSSSSSSSSSSSSSk.",
        "kwkSSSSSSSSSSSSSSSSSSSSSSSSSkwk.",
        "kkkSSSSSSSSSSSSSSSSSSSSSSSSSkkk.",
        ".kkkkkkkkkkSSSSSSSSkkkkkkkkkk...",
        "..........kSSSSSSSSk............",
        ".........kSSSSSSSSSSk...........",
        "........kSSSSSSSSSSSSk......kk..",
        ".......kSSSSSSSSSSSSSSk....kSk..",
        "......kSSSSwwwwwwSSSSSSk..kSSk..",
        ".....kSSSSwwwwwwwwSSSSSSkkSSSk..",
        ".....kSSSwkwkwkwkwwSSSSSSSSSk...",
        ".....kSSSwwwwwwwwwwSSSSSSSSk....",
        "......kSSSSSSSSSSSSSSSSSSSk.....",
        ".......kkSSSSSSSSSSSSSkkk.......",
        ".........kkkSSSSSkkkkk..........",
        "...........kkSSSk...............",
        "............kkkk................",
    };

    static readonly string[] ArteMarteloTonto =
    {
        "kkkkkkkkkkkkkkkkkkkkkkkkkkkkkk..",
        "kSSSSSSSSSSSSSSSSSSSSSSSSSSSSSk.",
        "kwkSSSSSSSSSSSSSSSSSSSSSSSSSkwk.",
        "kkkSSSSSSSSSSSSSSSSSSSSSSSSSkkk.",
        ".kkkkkkkkkkSSSSSSSSkkkkkkkkkk...",
        "..........kSSSSSSSSk............",
        ".........kSSSSSSSSSSk...........",
        "........kSSkSkSSSSSSSk......kk..",
        ".......kSSSSkSSSSSSSSSk....kSk..",
        "......kSSSkSkSSSSSSSSSSk..kSSk..",
        ".....kSSSSSSSSSSSSSSSSSSkkSSSk..",
        ".....kSSSSSwwwwwwSSSSSSSSSSk....",
        ".....kSSSSwwwwwwwwSSSSSSSSk.....",
        "......kSSSSSSSSSSSSSSSSSSSk.....",
        ".......kkSSSSSSSSSSSSSkkk.......",
        ".........kkkSSSSSkkkkk..........",
        "...........kkSSSk...............",
        "............kkkk................",
    };

    static readonly string[] ArteBigorna =
    {
        "kkkkkkkkkkkkkk",
        "kSSSSSSSSSSSSk",
        ".kkSSSSSSSSkk.",
        "...kSSSSSSk...",
        "...kSSSSSSk...",
        "..kSSSSSSSSk..",
        ".kkkkkkkkkkkk.",
    };
}

// '}' - A MÃE DO MAR (a chefe final do Mar de Baixo, fase 7). Uma água-viva gigante de coroa. Arena de 22 blocos.
//  ONDAS: ela bate na água e manda ondas rente ao chão de um lado (a seta avisa) - na etapa 2, dos dois lados.
//  CHUVA DE ÁGUAS-VIVAS: marcas no chão; águas-vivas caem ali.
//  PILARES: tentáculos brotam do chão nas marcas e ficam um tempinho (fique nos vãos).
//  CORRENTEZA: ela puxa você para perto dela (segure para o outro lado) com um anel em volta.
//  No fim de cada rodada ela afunda até o chão, cansada: tonta. Pule na cabeça (a coroa).
public class ChefeMaeDoMar : ChefeDoMar
{
    static readonly float[] VelocidadeDaOnda = { 7f, 8.5f, 10f };
    float alturaFlutuando = 2.5f;

    public override string Nome => "MÃE DO MAR";
    public override Musicas.Tema Musica => Musicas.Tema.Baleia;
    protected override string SpriteNormal => "mae_do_mar";
    protected override string SpriteTonto => "mae_do_mar_tonta";
    protected override string ConquistaAoVencer => "mae_vencida";
    protected override string FalaDeEntrada => Idioma.T("A MÃE DO MAR! (o Kraken era a filhinha dela)", "THE SEA MOTHER! (the Kraken was her little one)");
    protected override string DicaDeEntrada => Idioma.T("Fuja das ondas e dos tentáculos. Quando ela AFUNDAR cansada, pule na COROA!", "Dodge the waves and tentacles. When she SINKS tired, stomp the CROWN!");
    protected override string FalaDaEtapa2 => Idioma.T("ETAPA 2/3 - a maré subiu!", "STAGE 2/3 - the tide is rising!");
    protected override string FalaDaEtapa3 => Idioma.T("ETAPA 3/3 - A MÃE DO MAR ESTÁ FURIOSA!!", "STAGE 3/3 - THE SEA MOTHER IS FURIOUS!!");
    protected override string FalaDaDerrota => Idioma.T("A MÃE DO MAR CAIU! (as correntes cantam de novo)", "THE SEA MOTHER FELL! (the currents sing again)");
    protected override float TopoDaCabeca => 2.8f;
    protected override float MeiaLarguraDaCabeca => 1.3f;
    protected override float Escala => 2.6f;

    protected override void Awake()
    {
        base.Awake();
        corpo.position = new Vector3(meio + 4f, chao + alturaFlutuando, 0f);
    }

    protected override IEnumerator Rodada(int qual)
    {
        yield return Flutuar(qual % 2 == 0 ? meio + 5f : meio - 5f);
        switch (qual)
        {
            case 0:
                yield return Ondas(false);
                yield return Chuva(new[] { -2f, 1f, 3f });
                yield return Pilares(new[] { -6f, -2f, 2f, 6f });
                break;
            case 1:
                yield return Chuva(new[] { -3f, 0f, 3f });
                yield return Ondas(false);
                yield return Puxar();
                break;
            case 2:
                yield return Ondas(true);
                Paralelo(Chuva(new[] { -4f, 0f, 4f }));
                yield return Pilares(new[] { -7f, -3f, 1f, 5f, 9f });
                yield return EsperarTodos();
                break;
            case 3:
                Paralelo(Puxar());
                yield return Chuva(new[] { -2f, 2f });
                yield return EsperarTodos();
                yield return Ondas(true);
                break;
            case 4:
                yield return Pilares(new[] { -8f, -4f, 0f, 4f, 8f });
                Paralelo(Ondas(true));
                yield return Chuva(new[] { -3f, -1f, 1f, 3f });
                yield return EsperarTodos();
                yield return Puxar();
                break;
            default:
                Paralelo(Puxar());
                yield return Ondas(false);
                yield return EsperarTodos();
                yield return Pilares(new[] { -9f, -5f, -1f, 3f, 7f });
                yield return Ondas(true);
                break;
        }
        // cansada: afunda até o chão (é a hora do pisão)
        yield return Descer();
        yield return Tontear();
        yield return Subir();
    }

    IEnumerator Flutuar(float x)
    {
        Vector3 de = corpo.position, ate = new Vector3(Mathf.Clamp(x, esquerda + 2f, direita - 2f), chao + alturaFlutuando, 0f);
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            corpo.position = Vector3.Lerp(de, ate, Mathf.SmoothStep(0f, 1f, t / 0.8f));
            yield return null;
        }
        OlharParaOJogador();
    }

    IEnumerator Descer()
    {
        Vector3 de = corpo.position;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            corpo.position = new Vector3(de.x, Mathf.Lerp(de.y, chao, t / 0.5f), 0f);
            yield return null;
        }
        corpo.position = new Vector3(de.x, chao, 0f);
        GerenciadorDoJogo.Som("pancada", 0.6f);
    }

    IEnumerator Subir()
    {
        Vector3 de = corpo.position;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            corpo.position = new Vector3(de.x, Mathf.Lerp(chao, chao + alturaFlutuando, t / 0.5f), 0f);
            yield return null;
        }
    }

    // ondas rente ao chão: de um lado (o mais longe de você) ou dos dois (a segunda sai depois)
    IEnumerator Ondas(bool dosDoisLados)
    {
        float alvo = AlvoX();
        float lado = alvo > meio ? 1f : -1f; // a onda vem do lado oposto ao seu e corre na sua direção
        var seta = Desenho("SetaDaOnda", "aviso", new Vector3(lado > 0 ? esquerda + 1f : direita - 1f, chao + 1.5f, 0f), OrdemDosAvisos);
        for (float t = 0f; t < AvisoDePulo; t += Time.deltaTime)
        {
            if (seta != null) seta.enabled = Piscando(t, AvisoDePulo);
            yield return null;
        }
        if (seta != null) Destroy(seta.gameObject);
        GerenciadorDoJogo.Som("onda", 0.6f);
        Paralelo(OndaNoChao(lado > 0 ? esquerda + 0.5f : direita - 0.5f, lado, VelocidadeDaOnda[etapa], 0.7f));
        if (dosDoisLados)
        {
            yield return new WaitForSeconds(0.9f);
            var seta2 = Desenho("SetaDaOnda", "aviso", new Vector3(lado > 0 ? direita - 1f : esquerda + 1f, chao + 1.5f, 0f), OrdemDosAvisos);
            yield return new WaitForSeconds(AvisoDePulo);
            if (seta2 != null) Destroy(seta2.gameObject);
            Paralelo(OndaNoChao(lado > 0 ? direita - 0.5f : esquerda + 0.5f, -lado, VelocidadeDaOnda[etapa], 0.7f));
        }
        yield return EsperarTodos();
    }

    IEnumerator Chuva(float[] deslocamentos)
    {
        float centro = AlvoX();
        foreach (float d in deslocamentos)
            Paralelo(CairDoTeto(Mathf.Clamp(centro + d, esquerda + 0.6f, direita - 0.6f), "agua_viva", Aviso + 0.3f, 0.4f));
        yield return EsperarTodos();
    }

    IEnumerator Pilares(float[] deslocamentos)
    {
        foreach (float d in deslocamentos) Paralelo(Pilar(Mathf.Clamp(meio + d, esquerda + 0.6f, direita - 0.6f)));
        yield return EsperarTodos();
    }

    IEnumerator Pilar(float x)
    {
        yield return MarcarChao(x, Aviso + 0.35f, 1.2f);
        var tentaculo = Desenho("Pilar", "baleia_linha", Vector3.zero, OrdemDoChefe + 1);
        tentaculo.color = new Color(0.85f, 0.4f, 1f);
        Zona z = NovaZona(new Vector2(x, chao), new Vector2(x, chao), 0.45f);
        for (float t = 0f; t < 0.15f; t += Time.deltaTime)
        {
            float alto = 3.2f * t / 0.15f;
            z.b = new Vector2(x, chao + alto);
            Esticar(tentaculo.transform, new Vector2(x, chao), new Vector2(x, chao + alto + 0.01f), 6f);
            yield return null;
        }
        GerenciadorDoJogo.Som("crava", 0.4f);
        yield return new WaitForSeconds(0.7f);
        zonas.Remove(z);
        if (tentaculo != null) Destroy(tentaculo.gameObject);
    }

    // puxa os Grumetes para perto dela (segure para o outro lado), com um anel que mata em volta do corpo
    IEnumerator Puxar()
    {
        var anel = Desenho("Anel", "capita_anel", corpo.position, OrdemDosAvisos);
        anel.transform.localScale = Vector3.one * 1.6f;
        for (float t = 0f; t < Aviso + 0.3f; t += Time.deltaTime)
        {
            if (anel != null) anel.enabled = Piscando(t, Aviso + 0.3f);
            yield return null;
        }
        GerenciadorDoJogo.Som("onda", 0.5f);
        Zona z = NovaZona(corpo.position, corpo.position, 1.3f);
        for (float t = 0f; t < 1.6f; t += Time.deltaTime)
        {
            z.a = z.b = (Vector2)corpo.position + Vector2.up * 1.2f;
            if (anel != null) anel.transform.position = corpo.position + Vector3.up * 1.2f;
            foreach (Jogador j in GerenciadorDoJogo.Jogadores)
                if (j != null && !j.Morto) j.Empurrar(Mathf.Sign(corpo.position.x - j.transform.position.x) * 4.2f);
            yield return null;
        }
        zonas.Remove(z);
        if (anel != null) Destroy(anel.gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("mae_do_mar", () => FabricaDeSprites.DeArte(ArteMae(false), FabricaDeSprites.Base));
        FabricaDeSprites.Registrar("mae_do_mar_tonta", () => FabricaDeSprites.DeArte(ArteMae(true), FabricaDeSprites.Base));
    }

    static string[] ArteMae(bool tonta)
    {
        string olhos = tonta ? "kPPPPkPkPPPPPPPkPkPPPPk" : "kPPPPwkwPPPPPPPwkwPPPPk";
        return new[]
        {
            ".........y.y.y.........",
            "........yyyyyyy........",
            ".......kyoyyyoyk.......",
            ".....kkPPPPPPPPPkk.....",
            "...kkPPwwPPPPPPPPPkk...",
            "..kPPwwPPPPPPPPPPPPPk..",
            ".kPPwPPPPPPPPPPPPPPPPk.",
            olhos,
            "kPPPPPPPPPPPPPPPPPPPPPk",
            "kPPPPPPPkkkkkkkPPPPPPPk",
            "kVVVVVVVVVVVVVVVVVVVVVk",
            ".kkkkkkkkkkkkkkkkkkkkk.",
            "..V..V..V..V..V..V..V..",
            "..V.V..V..V..V..V..V...",
            ".V..V.V..V..V..V..V.V..",
            ".V.V..V.V..V..V.V..V...",
            "..V..V..V.V..V..V.V..V.",
            "..V.V..V..V..V.V..V..V.",
        };
    }
}
