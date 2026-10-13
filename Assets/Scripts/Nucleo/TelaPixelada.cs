using UnityEngine;

// PIXEL NÍTIDO + TILT-SHIFT: a câmera do jogo desenha numa imagem PEQUENA (16 pixels de tela por bloco,
// do tamanho exato da pixel art: uma fase de 15 blocos de altura vira uma imagem de 240 pixels de altura),
// e essa imagem é mostrada na tela AUMENTADA por um número inteiro (3x, 4x, 5x...) com filtro "Point".
//
// Por que isso?
//  1. Pixel nítido de verdade: numa tela de 1080 pixels, cada pixel do desenho virava 4,5 pixels de tela.
//     Meio pixel não existe, então alguns viravam 4 e outros 5, e isso muda conforme a câmera anda:
//     a pixel art "tremia" e parecia travada. Com escala inteira, todo pixel tem o mesmo tamanho, sempre.
//  2. A câmera anda de pixel em pixel (CameraSeguir chama AlinharAoPixel): nada treme.
//  3. FICA MAIS LEVE: a placa de vídeo pinta ~100 mil pixels em vez de ~2 milhões (céu, montanhas, névoa
//     do mapa, luzes e pós-processamento, tudo por cima um do outro). Em computador fraco e no navegador,
//     é a maior diferença de desempenho do jogo.
//
// Sobra uma faixa em cima e embaixo quando a altura da tela não é múltipla da imagem (1080 / 240 = 4,5:
// usa 4x e sobram 60 pixels em cima e embaixo). Essas faixas são preenchidas com a própria imagem,
// bem borrada e escura (como os vídeos de celular deitados), e não ficam pretas.
//
// TILT-SHIFT (o "ar de maquete" do HD-2D): uma cópia pequena e borrada da imagem é desenhada por cima,
// só nas faixas de cima e de baixo, em tiras cada vez mais transparentes. O meio continua nítido.
//
// A interface (OnGUI) é desenhada DEPOIS, direto na tela: os textos continuam nítidos, no tamanho da tela.
// Quem converte posição do mundo em posição da tela usa MundoParaTela (e não cam.WorldToScreenPoint, que
// passaria a responder em pixels da imagem pequena).
//
// SEGURANÇA: se qualquer coisa falhar (não conseguiu criar a imagem), desliga e o jogo desenha do jeito
// normal, direto na tela. Dá para desligar no painel F10 (AjustesVisuais.pixelNitido).
[DefaultExecutionOrder(1000)] // depois de tudo que mexe na câmera (CameraSeguir, chefes...)
public class TelaPixelada : MonoBehaviour
{
    const int PixelsPorBloco = FabricaDeSprites.PixelsPorUnidade;
    const int TirasDoTilt = 8; // em quantas tiras cada faixa do tilt-shift é dividida

    static TelaPixelada instancia;

    RenderTexture principal;              // a imagem do jogo, pequena e nítida
    RenderTexture metade, quarto, oitavo; // cópias cada vez menores: quanto menor, mais borrada fica quando aumenta
    Camera camAlvo;                       // a câmera do jogo que está desenhando na imagem pequena
    Camera apresentacao;                  // uma câmera que não desenha nada, só limpa a tela (veja CriarApresentacao)
    int largura, altura;                  // tamanho da imagem pequena
    Rect area;                            // onde a imagem aparece na tela (em coordenadas da interface: y para baixo)
    bool falhou;

    // A imagem pequena está ligada agora?
    public static bool Ativa => instancia != null && instancia.camAlvo != null && instancia.principal != null;

    void Awake() => instancia = this;

    void OnDestroy()
    {
        Desligar();
        if (apresentacao != null) Destroy(apresentacao.gameObject);
        if (instancia == this) instancia = null;
    }

    // ------------------------------------------------------------------ montar a imagem pequena

    void LateUpdate()
    {
        Camera cam = Camera.main;
        bool ligar = AjustesVisuais.pixelNitido && !falhou && cam != null && Screen.width > 0 && Screen.height > 0;
        if (!ligar || (camAlvo != null && camAlvo != cam))
        {
            Desligar();
            if (!ligar) return;
        }

        // Altura da imagem = altura que a câmera enxerga, em pixels da arte (fase: 15 blocos = 240 pixels).
        int novaAltura = Mathf.Max(16, Mathf.RoundToInt(cam.orthographicSize * 2f * PixelsPorBloco));
        // A escala: o maior número INTEIRO que cabe na tela (ou, se pedirem, a que preenche a tela toda).
        float escala = Screen.height / (float)novaAltura;
        if (AjustesVisuais.escalaInteira && escala >= 1f) escala = Mathf.Floor(escala);
        // A largura sai da tela: o suficiente para cobrir a tela inteira nessa escala.
        int novaLargura = Mathf.Max(16, Mathf.CeilToInt(Screen.width / escala));

        bool perdida = principal != null && (!principal.IsCreated() || !metade.IsCreated() || !quarto.IsCreated() || !oitavo.IsCreated());
        if (principal == null || perdida || novaLargura != largura || novaAltura != altura)
        {
            if (!Criar(novaLargura, novaAltura)) return;
        }

        // Onde a imagem aparece: centralizada (a sobra de cada lado é igual).
        float w = largura * escala, h = altura * escala;
        area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

        if (apresentacao == null) CriarApresentacao();
        apresentacao.enabled = true;
        camAlvo = cam;
        if (cam.targetTexture != principal) cam.targetTexture = principal;
        cam.aspect = largura / (float)altura;
    }

    bool Criar(int novaLargura, int novaAltura)
    {
        Liberar();
        largura = novaLargura;
        altura = novaAltura;
        principal = NovaImagem("Tela pixelada", largura, altura, 24, FilterMode.Point);
        metade = NovaImagem("Tela 1/2", largura / 2, altura / 2, 0, FilterMode.Bilinear);
        quarto = NovaImagem("Tela 1/4", largura / 4, altura / 4, 0, FilterMode.Bilinear);
        oitavo = NovaImagem("Tela 1/8", largura / 8, altura / 8, 0, FilterMode.Bilinear);
        if (principal == null || metade == null || quarto == null || oitavo == null)
        {
            Debug.LogWarning("TelaPixelada: não deu para criar a imagem em baixa resolução. Desenhando direto na tela.");
            falhou = true;
            Desligar();
            return false;
        }
        return true;
    }

    static RenderTexture NovaImagem(string nome, int w, int h, int profundidade, FilterMode filtro)
    {
        var imagem = new RenderTexture(Mathf.Max(1, w), Mathf.Max(1, h), profundidade, RenderTextureFormat.ARGB32)
        {
            name = nome,
            filterMode = filtro,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            antiAliasing = 1,
            hideFlags = HideFlags.DontSave,
        };
        if (imagem.Create()) return imagem;
        Destroy(imagem);
        return null;
    }

    // Com a câmera do jogo desenhando numa imagem, NENHUMA câmera desenharia na tela, e ela ficaria com
    // lixo do quadro anterior (no editor aparece "No cameras rendering"). Esta câmera não enxerga nada
    // (cullingMask = 0), fica bem longe da fase e só pinta a tela de preto antes da interface.
    void CriarApresentacao()
    {
        var objeto = new GameObject("Camera de apresentacao (TelaPixelada)");
        objeto.transform.SetParent(transform, false);
        objeto.transform.position = new Vector3(0f, -100000f, -10f);
        apresentacao = objeto.AddComponent<Camera>(); // (sem a etiqueta "MainCamera": Camera.main continua sendo a do jogo)
        apresentacao.orthographic = true;
        apresentacao.orthographicSize = 1f;
        apresentacao.cullingMask = 0;
        apresentacao.clearFlags = CameraClearFlags.SolidColor;
        apresentacao.backgroundColor = Color.black;
        apresentacao.depth = 100f; // depois da câmera do jogo
        apresentacao.allowMSAA = false;
        apresentacao.allowHDR = false;
    }

    // Volta ao normal: a câmera do jogo desenha direto na tela.
    void Desligar()
    {
        if (camAlvo != null)
        {
            if (camAlvo.targetTexture == principal) camAlvo.targetTexture = null;
            camAlvo.ResetAspect();
            camAlvo = null;
        }
        if (apresentacao != null) apresentacao.enabled = false;
        Liberar();
    }

    void Liberar()
    {
        Soltar(ref principal);
        Soltar(ref metade);
        Soltar(ref quarto);
        Soltar(ref oitavo);
        largura = altura = 0;
    }

    static void Soltar(ref RenderTexture imagem)
    {
        if (imagem == null) return;
        imagem.Release();
        Destroy(imagem);
        imagem = null;
    }

    // ------------------------------------------------------------------ ajudantes para os outros scripts

    // Arredonda a posição da câmera para caber certinho na grade de pixels (CameraSeguir usa).
    // Sem a imagem pequena, devolve a posição sem mexer (andar de 1/16 em 1/16 de bloco numa tela
    // grande ficaria "aos pulinhos").
    public static Vector3 AlinharAoPixel(Vector3 posicao, Camera cam)
    {
        if (!Ativa || cam == null || cam != instancia.camAlvo) return posicao;
        // Arredonda a BEIRADA da imagem (e não o meio): assim os pixels dos desenhos caem exatamente
        // em cima dos pixels da imagem, mesmo que ela tenha largura ímpar.
        float meiaLargura = cam.orthographicSize * cam.aspect, meiaAltura = cam.orthographicSize;
        float esquerda = Mathf.Round((posicao.x - meiaLargura) * PixelsPorBloco) / PixelsPorBloco;
        float baixo = Mathf.Round((posicao.y - meiaAltura) * PixelsPorBloco) / PixelsPorBloco;
        return new Vector3(esquerda + meiaLargura, baixo + meiaAltura, posicao.z);
    }

    // Igual ao cam.WorldToScreenPoint (x para a direita, y para CIMA, em pixels da tela), mas sabe que a
    // imagem do jogo pode estar aumentada e centralizada na tela.
    public static Vector3 MundoParaTela(Camera cam, Vector3 mundo)
    {
        if (!Ativa || cam != instancia.camAlvo) return cam.WorldToScreenPoint(mundo);
        Vector3 v = cam.WorldToViewportPoint(mundo); // 0 a 1 dentro da imagem
        Rect a = instancia.area;
        return new Vector3(a.x + v.x * a.width, Screen.height - a.y - (1f - v.y) * a.height, v.z);
    }

    // ------------------------------------------------------------------ desenhar na tela

    // Chamado pela Interface, no começo do OnGUI (antes de todo o resto da interface).
    public static void DesenharNaTela()
    {
        if (!Ativa || Event.current == null || Event.current.type != EventType.Repaint) return;
        instancia.Desenhar();
    }

    void Desenhar()
    {
        Color corOriginal = GUI.color;
        bool barras = area.y >= 1f;
        bool tilt = AjustesVisuais.TiltShiftLigado && AjustesVisuais.forcaDoTilt > 0f && AjustesVisuais.tamanhoDoTilt > 0f;
        if (barras || tilt) Desfocar();

        // As faixas que sobram: a imagem bem borrada e escura, cobrindo a tela toda.
        if (barras)
        {
            float cobre = Mathf.Max(Screen.width / (float)largura, Screen.height / (float)altura);
            float w = largura * cobre, h = altura * cobre;
            GUI.color = new Color(0.5f, 0.5f, 0.58f, 1f);
            GUI.DrawTexture(new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h), oitavo, ScaleMode.StretchToFill, false);
        }

        // A imagem do jogo, nítida (alphaBlend = false: a transparência da imagem não importa aqui).
        GUI.color = Color.white;
        GUI.DrawTexture(area, principal, ScaleMode.StretchToFill, false);

        if (tilt) DesenharTiltShift();
        GUI.color = corOriginal;
    }

    // Faz as cópias pequenas (cada uma com a metade do tamanho da anterior). Com o filtro "Bilinear",
    // reduzir pela metade tira a MÉDIA de 4 pixels: 3 reduções seguidas = um borrado suave e barato.
    void Desfocar()
    {
        RenderTexture antes = RenderTexture.active;
        principal.filterMode = FilterMode.Bilinear; // só durante a cópia (para tirar a média)
        Graphics.Blit(principal, metade);
        Graphics.Blit(metade, quarto);
        Graphics.Blit(quarto, oitavo);
        principal.filterMode = FilterMode.Point;    // e volta a ser nítida para aparecer na tela
        RenderTexture.active = antes;
    }

    // As faixas de cima e de baixo, em tiras: a da beirada quase toda borrada, a do meio quase nada.
    void DesenharTiltShift()
    {
        float fracao = Mathf.Clamp(AjustesVisuais.tamanhoDoTilt, 0f, 0.5f); // que parte da altura cada faixa ocupa
        float alturaDaTira = area.height * fracao / TirasDoTilt;
        float uvDaTira = fracao / TirasDoTilt;
        for (int k = 0; k < TirasDoTilt; k++)
        {
            // k = 0 é a tira da beirada. A força cai devagar até zero no meio da tela.
            float t = 1f - (k + 0.5f) / TirasDoTilt;
            GUI.color = new Color(1f, 1f, 1f, AjustesVisuais.forcaDoTilt * t * t);

            // faixa de cima (na interface, y cresce para baixo; na imagem, v cresce para cima)
            var cima = new Rect(area.x, area.y + k * alturaDaTira, area.width, alturaDaTira);
            GUI.DrawTextureWithTexCoords(cima, quarto, new Rect(0f, 1f - (k + 1) * uvDaTira, 1f, uvDaTira), true);

            // faixa de baixo
            var baixo = new Rect(area.x, area.yMax - (k + 1) * alturaDaTira, area.width, alturaDaTira);
            GUI.DrawTextureWithTexCoords(baixo, quarto, new Rect(0f, k * uvDaTira, 1f, uvDaTira), true);
        }
    }
}
