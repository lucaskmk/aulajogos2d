using UnityEngine;

// RAIOS DE SOL ("god rays"): feixes de luz diagonais, como o sol entrando por entre as nuvens, e
// grãozinhos de poeira dourada flutuando DENTRO deles (igual poeira num raio de sol na janela).
//  - de dia: feixes dourados de manhã, que vão ficando alaranjados no entardecer (LuzDoDia.Entardecer);
//  - na biblioteca: feixes cor de sépia (luz velha entrando pelas janelas);
//  - de noite: poucos feixes de luar azulados, bem fracos;
//  - no escuro: nada (nem é criado).
//
// A luz é ADITIVA (shader LuzAditiva): ela SOMA com a cor da tela, como luz de verdade, em vez de pintar
// por cima. Onde passa do branco, o bloom do pós-processamento faz brilhar. (Se o shader não estiver
// disponível, os feixes são desenhados do jeito antigo, meio transparentes, um pouco mais fortes.)
//
// Os feixes "respiram" (a força e a largura sobem e descem devagar), balançam um tiquinho, andam com
// paralaxe lenta e SOMEM quando uma nuvem passa na frente do sol (LuzDoDia.Cobertura, das SombrasDeNuvens).
// Para não precisar de um feixe a cada pedaço da fase, eles "dão a volta" em torno da câmera
// (quem sai por um lado da tela volta pelo outro), como as partículas do Ambiente.
// Nada é criado durante o jogo: tudo é montado uma vez em Criar() e só reposicionado no LateUpdate.
[DefaultExecutionOrder(105)] // depois da câmera (CameraSeguir), da paralaxe e das SombrasDeNuvens
public class FeixesDeLuz : MonoBehaviour
{
    const float Fator = 0.85f;         // paralaxe: 1 = parado na tela, 0 = anda junto com a fase
    const float Angulo = -27f;         // inclinação dos feixes (negativo = descem para a esquerda; o sol fica à direita)
    const float Comprimento = 8f;      // comprimento do desenho do feixe, em blocos (128 pixels / 16)
    const int OrdemDosFeixes = -12;    // na frente das árvores e da neblina, atrás das nuvens e da fase

    // A cor do sol no entardecer (as fases de dia vão da cor normal até esta).
    static readonly Color CorDoEntardecer = new Color(1f, 0.6f, 0.3f);

    Transform[] feixes;
    SpriteRenderer[] desenhos;
    float[] baseX, fase, alfa, largura, intensidade;
    Color corDoFeixe;
    bool dia;          // só de dia o sol muda de cor com a hora
    bool aditivo;      // está usando o shader LuzAditiva?
    bool visivel = true;

    Transform[] graos;
    SpriteRenderer[] desenhosDosGraos;
    int[] feixeDoGrao;
    float[] aoLongo, deLado, faseDoGrao, velocidadeDoGrao;
    Color corDoGrao;

    public static FeixesDeLuz Criar(Transform raiz, Clima clima)
    {
        if (clima == Clima.Escuro) return null;

        int quantidade; float forca; Color cor, corDoGrao; int quantosGraos;
        switch (clima)
        {
            case Clima.Noite:
                quantidade = 3; forca = 0.35f; cor = new Color(0.6f, 0.72f, 1f);
                quantosGraos = 6; corDoGrao = new Color(0.75f, 0.85f, 1f);
                break;
            case Clima.Biblioteca:
                quantidade = 4; forca = 0.85f; cor = new Color(1f, 0.78f, 0.5f);
                quantosGraos = 24; corDoGrao = new Color(1f, 0.85f, 0.55f);
                break;
            default: // Dia
                quantidade = 6; forca = 1f; cor = new Color(1f, 0.9f, 0.66f);
                quantosGraos = 30; corDoGrao = new Color(1f, 0.92f, 0.6f);
                break;
        }

        var objeto = new GameObject("Feixes de luz");
        objeto.transform.SetParent(raiz, false);
        var luz = objeto.AddComponent<FeixesDeLuz>();
        luz.corDoFeixe = cor;
        luz.corDoGrao = corDoGrao;
        luz.dia = clima == Clima.Dia;
        luz.Montar(quantidade, forca, quantosGraos);
        return luz;
    }

    void Montar(int quantidade, float forca, int quantosGraos)
    {
        var sorteio = new System.Random(27); // sempre o mesmo sorteio: a fase fica igual a cada morte
        Sprite desenho = FabricaDeSprites.Pegar("hd2d_feixe");
        feixes = new Transform[quantidade];
        desenhos = new SpriteRenderer[quantidade];
        baseX = new float[quantidade]; fase = new float[quantidade];
        alfa = new float[quantidade]; largura = new float[quantidade]; intensidade = new float[quantidade];
        for (int i = 0; i < quantidade; i++)
        {
            var feixe = new GameObject("Feixe");
            feixe.transform.SetParent(transform, false);
            var visual = feixe.AddComponent<SpriteRenderer>();
            visual.sprite = desenho;
            visual.sortingOrder = OrdemDosFeixes;
            aditivo = MateriaisHD2D.UsarLuzAditiva(visual); // (se não der, fica o material normal)
            feixes[i] = feixe.transform;
            desenhos[i] = visual;
            // espalhados pelo vão (0 = começo, 1 = fim), com um tremidinho para não ficarem certinhos demais
            baseX[i] = (i + 0.5f + (float)(sorteio.NextDouble() - 0.5) * 0.4f) / quantidade;
            fase[i] = (float)sorteio.NextDouble() * 10f;
            largura[i] = 0.8f + (float)sorteio.NextDouble() * 1.8f; // largos e finos misturados
            alfa[i] = forca * (0.32f + (float)sorteio.NextDouble() * 0.25f) * (largura[i] > 2f ? 0.75f : 1f);
        }

        // poeira dourada: cada grão "mora" num feixe, numa posição ao longo dele e um pouco para o lado.
        // Cada grão é 1 ou 2 pixels de verdade (com a tela em baixa resolução, menos que isso pisca e some).
        Sprite pixel = FabricaDeSprites.Pegar("hd2d_pixel");
        graos = new Transform[quantosGraos];
        desenhosDosGraos = new SpriteRenderer[quantosGraos];
        feixeDoGrao = new int[quantosGraos];
        aoLongo = new float[quantosGraos]; deLado = new float[quantosGraos];
        faseDoGrao = new float[quantosGraos]; velocidadeDoGrao = new float[quantosGraos];
        for (int i = 0; i < quantosGraos; i++)
        {
            var grao = new GameObject("Grao de poeira");
            grao.transform.SetParent(transform, false);
            grao.transform.localScale = Vector3.one * (sorteio.NextDouble() < 0.7 ? 1f : 2f);
            var visual = grao.AddComponent<SpriteRenderer>();
            visual.sprite = pixel;
            visual.sortingOrder = OrdemDosFeixes + 1;
            graos[i] = grao.transform;
            desenhosDosGraos[i] = visual;
            feixeDoGrao[i] = i % quantidade;
            aoLongo[i] = 0.15f + (float)sorteio.NextDouble() * 0.7f;
            deLado[i] = (float)(sorteio.NextDouble() - 0.5);
            faseDoGrao[i] = (float)sorteio.NextDouble() * 10f;
            velocidadeDoGrao[i] = 0.004f + (float)sorteio.NextDouble() * 0.01f;
        }
    }

    // Liga/desliga todos os desenhos (pelo painel F10 ou pela opção Simples), sem destruir nada.
    void Mostrar(bool sim)
    {
        visivel = sim;
        foreach (SpriteRenderer d in desenhos) d.enabled = sim;
        foreach (SpriteRenderer d in desenhosDosGraos) d.enabled = sim;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || feixes == null) return;
        bool ligado = AjustesVisuais.RaiosLigados;
        if (ligado != visivel) Mostrar(ligado);
        if (!ligado) return;

        Vector3 posCam = cam.transform.position;
        float meiaTela = cam.orthographicSize * cam.aspect;
        float topo = posCam.y + cam.orthographicSize + 1.5f; // os feixes nascem um pouco acima da tela
        float t = Time.time;
        // A "volta" é um pouco mais larga que a tela + o quanto o feixe anda para a esquerda ao descer.
        // O feixe é esticado até atravessar a tela inteira na diagonal.
        // (calculado a cada quadro porque o tamanho da câmera muda de uma fase para outra)
        float vao = meiaTela * 2f + 14f;
        float escalaY = (cam.orthographicSize * 2f + 4f) / Comprimento / Mathf.Cos(Angulo * Mathf.Deg2Rad);

        // direção do feixe (para baixo e para a esquerda) e a direção "de lado" (perpendicular a ele)
        float rad = Angulo * Mathf.Deg2Rad;
        Vector2 descida = new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));
        Vector2 lado = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        float comprimentoReal = Comprimento * escalaY;

        // onde o "vão" começa na tela: da beirada esquerda até passar da direita
        float inicio = posCam.x - meiaTela - 2f;
        float deslocamento = posCam.x * Fator; // paralaxe

        // A cor e a força de agora: de dia o sol fica alaranjado no entardecer; nuvem na frente do sol apaga os raios.
        Color cor = dia ? Color.Lerp(corDoFeixe, CorDoEntardecer, LuzDoDia.Entardecer) : corDoFeixe;
        float nuvem = 1f - 0.85f * LuzDoDia.Cobertura;
        // luz aditiva soma com a tela: precisa de bem menos força que a pintura transparente para o mesmo brilho
        float forca = AjustesVisuais.forcaDosRaios * nuvem * (aditivo ? 0.6f : 1f);

        for (int i = 0; i < feixes.Length; i++)
        {
            // Repeat = "dá a volta": fica sempre entre o começo e o fim do vão
            float x = inicio + Mathf.Repeat(baseX[i] * vao + deslocamento - inicio, vao);
            float respira = 0.5f + 0.5f * Mathf.Sin(t * 0.45f + fase[i]);
            float largo = largura[i] * (1f + 0.12f * Mathf.Sin(t * 0.3f + fase[i] * 1.7f));
            feixes[i].position = new Vector3(x, topo, 0f);
            feixes[i].localRotation = Quaternion.Euler(0f, 0f, Angulo + 1.5f * Mathf.Sin(t * 0.2f + fase[i]));
            feixes[i].localScale = new Vector3(largo, escalaY, 1f);
            intensidade[i] = alfa[i] * (0.45f + 0.55f * respira);
            Color c = cor;
            c.a = Mathf.Clamp01(intensidade[i] * forca);
            desenhos[i].color = c;
        }

        for (int i = 0; i < graos.Length; i++)
        {
            int k = feixeDoGrao[i];
            // o grão desce devagar ao longo do feixe; chegou no fim, volta lá para cima
            aoLongo[i] += velocidadeDoGrao[i] * Time.deltaTime;
            if (aoLongo[i] > 0.9f) aoLongo[i] = 0.12f;
            float balanco = Mathf.Sin(t * 0.8f + faseDoGrao[i]);
            float afastamento = (deLado[i] + 0.15f * balanco) * largura[k] * 1.2f;
            Vector3 p = feixes[k].position;
            p.x += descida.x * aoLongo[i] * comprimentoReal + lado.x * afastamento;
            p.y += descida.y * aoLongo[i] * comprimentoReal + lado.y * afastamento + 0.2f * Mathf.Sin(t * 0.5f + faseDoGrao[i] * 2f);
            graos[i].position = p;

            // pisca de leve e some perto das pontas do feixe (sin vai a 0 no começo e no fim)
            Color c = Color.Lerp(corDoGrao, cor, 0.5f);
            float pontas = Mathf.Sin(Mathf.PI * (aoLongo[i] - 0.12f) / 0.78f);
            c.a = Mathf.Clamp01(pontas) * (0.35f + 0.5f * Mathf.Abs(Mathf.Sin(t * 1.3f + faseDoGrao[i])))
                  * Mathf.Clamp01(intensidade[k] * 2.2f * nuvem * AjustesVisuais.forcaDosRaios);
            desenhosDosGraos[i].color = c;
        }
    }
}
