using UnityEngine;

// SOMBRAS DE NUVENS: manchas escuras e de borda bem suave passando devagar pela fase, como a sombra de
// nuvens grandes num dia de sol (o efeito "cloud shadows" dos mods de Stardew Valley).
//
// O desenho é um "ruído" (manchas aleatórias que se repetem sem emenda na horizontal, veja ArteHD2D)
// esticado para cobrir a tela de cima a baixo. Os pedaços são postos lado a lado e andam com o vento.
// Ela é forte só na parte de baixo da tela (o chão) e some até a metade: o céu não recebe sombra.
//
// Com o shader SombraMultiplicada a tela é MULTIPLICADA (escurece uns 30% onde a nuvem é cheia).
// Se o shader não estiver disponível, um azul-escuro meio transparente faz quase o mesmo papel.
//
// Ela também diz para os raios de sol se tem nuvem na frente do sol (LuzDoDia.Cobertura).
[DefaultExecutionOrder(104)] // depois da câmera, antes dos FeixesDeLuz (que leem a Cobertura)
public class SombrasDeNuvens : MonoBehaviour
{
    const float Velocidade = 0.45f;     // blocos por segundo (o vento leva as nuvens para a direita)
    const int Ordem = 30;               // na frente da fase e das partículas, atrás do primeiro plano (40)

    Transform[] pedacos;
    SpriteRenderer[] desenhos;
    float largura, alturaDoDesenho;
    float forca;                        // 1 = dia; mais fraca de noite
    Color tom;                          // a cor da sombra (azulada: a luz do céu)
    bool multiplicar;                   // está usando o shader SombraMultiplicada?
    float deriva;                       // quanto as nuvens já andaram
    bool visivel = true;

    public static SombrasDeNuvens Criar(Transform raiz, Clima clima)
    {
        if (clima != Clima.Dia && clima != Clima.Noite) return null; // dentro da biblioteca e no escuro: sem nuvens

        var objeto = new GameObject("Sombras de nuvens");
        objeto.transform.SetParent(raiz, false);
        var nuvens = objeto.AddComponent<SombrasDeNuvens>();
        nuvens.forca = clima == Clima.Noite ? 0.5f : 1f;
        nuvens.tom = clima == Clima.Noite ? new Color(0.55f, 0.6f, 0.85f) : new Color(0.6f, 0.65f, 0.82f);
        nuvens.Montar();
        return nuvens;
    }

    void Montar()
    {
        Sprite desenho = FabricaDeSprites.Pegar("hd2d_nuvens");
        largura = desenho.bounds.size.x;
        alturaDoDesenho = desenho.bounds.size.y;
        Material material = MateriaisHD2D.SombraMultiplicada;
        multiplicar = material != null;

        // pedaços que cobrem a tela mais larga possível, com folga (como no PrimeiroPlano)
        int quantos = Mathf.CeilToInt(30f * 2.4f / largura) + 2;
        pedacos = new Transform[quantos];
        desenhos = new SpriteRenderer[quantos];
        for (int i = 0; i < quantos; i++)
        {
            var pedaco = new GameObject("Pedaco");
            pedaco.transform.SetParent(transform, false);
            var visual = pedaco.AddComponent<SpriteRenderer>();
            visual.sprite = desenho;
            visual.sortingOrder = Ordem;
            if (multiplicar) visual.sharedMaterial = material;
            pedacos[i] = pedaco.transform;
            desenhos[i] = visual;
        }
        deriva = 7f; // não começa com a emenda bem no começo da fase
    }

    void OnDisable() => LuzDoDia.Cobertura = 0f;

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || pedacos == null) return;

        bool ligado = AjustesVisuais.NuvensLigadas;
        if (ligado != visivel)
        {
            visivel = ligado;
            foreach (SpriteRenderer d in desenhos) d.enabled = ligado;
        }
        if (!ligado)
        {
            LuzDoDia.Cobertura = 0f;
            return;
        }

        deriva += Velocidade * AjustesVisuais.velocidadeDasNuvens * Time.deltaTime;
        Vector3 posCam = cam.transform.position;
        float meiaTela = cam.orthographicSize * cam.aspect;

        // As manchas estão "no chão" da fase (andam junto com ela) e ainda vão com o vento.
        float origem = deriva;
        float primeiro = origem + Mathf.Floor((posCam.x - meiaTela - origem) / largura) * largura;
        float alturaDaTela = cam.orthographicSize * 2f + 1f;
        var escala = new Vector3(1f, alturaDaTela / alturaDoDesenho, 1f);
        float y = posCam.y - cam.orthographicSize - 0.5f; // o pivô do desenho é o meio da base
        for (int i = 0; i < pedacos.Length; i++)
        {
            pedacos[i].position = new Vector3(primeiro + largura * (i + 0.5f), y, 0f);
            pedacos[i].localScale = escala;
        }

        float f = Mathf.Clamp(forca * AjustesVisuais.forcaDasNuvens, 0f, 2f);
        Color cor = multiplicar
            ? new Color(tom.r, tom.g, tom.b, Mathf.Clamp01(0.8f * f))       // multiplicar: ~30% mais escuro no máximo
            : new Color(0.08f, 0.1f, 0.22f, Mathf.Clamp01(0.3f * f));       // sem o shader: azul-escuro transparente
        for (int i = 0; i < desenhos.Length; i++) desenhos[i].color = cor;

        // O sol fica no alto, à esquerda (Paralaxe.PosicaoDoSol): tem nuvem passando por lá?
        float xDoSol = posCam.x + Paralaxe.PosicaoDoSol.x;
        float u = Mathf.Repeat((xDoSol - origem) / largura, 1f); // (cada pedaço começa em origem + n * largura)
        // (a mesma nuvem, só que lá no alto: a densidade "crua", sem o sumiço do céu)
        LuzDoDia.Cobertura = Mathf.Clamp01(ArteHD2D.DensidadeDaNuvem(u, 0.8f) * Mathf.Min(1f, f));
    }
}
