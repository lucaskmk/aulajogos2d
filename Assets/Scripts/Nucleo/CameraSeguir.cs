using UnityEngine;

// Câmera que segue o jogador na horizontal, sem mostrar nada fora da fase,
// e que sabe "tremer" quando algo pesado cai.
//
// Ela roda no LateUpdate: DEPOIS que o Subaru já andou neste quadro (o Rigidbody2D dele tem interpolação,
// então a posição dele já está "lisa"). E com a tela pixelada (TelaPixelada), a posição final é
// arredondada para a grade de pixels, senão a pixel art tremeria. O arredondamento vale só para o que vai
// para a tela: a conta do "deslizar" (xSuave) continua com a posição exata, para não emperrar.
[RequireComponent(typeof(Camera))]
public class CameraSeguir : MonoBehaviour
{
    public float suavidade = 0.12f;
    public float olharAFrente = 2f;

    Transform alvo;
    Camera cam;
    float limiteEsquerdo, limiteDireito, alturaFixa;
    float esquerdaDaFase, direitaDaFase; // os limites normais (para destravar depois da luta)
    float velocidadeX;
    float xSuave; // a posição exata (sem arredondar para o pixel)
    float tempoTremor, forcaTremor;

    static CameraSeguir instancia;

    void Awake()
    {
        cam = GetComponent<Camera>();
        instancia = this;
    }

    public void Configurar(Transform novoAlvo, int larguraFase, int alturaFase)
    {
        alvo = novoAlvo;
        cam.orthographicSize = alturaFase / 2f;
        alturaFixa = (alturaFase - 1) / 2f;
        limiteEsquerdo = esquerdaDaFase = -0.5f;
        limiteDireito = direitaDaFase = larguraFase - 0.5f;
        velocidadeX = 0f;
        xSuave = PosicaoDesejada();
        transform.position = TelaPixelada.AlinharAoPixel(new Vector3(xSuave, alturaFixa, -10f), cam);
    }

    public static void Tremer(float forca, float duracao)
    {
        if (instancia == null) return;
        instancia.forcaTremor = forca;
        instancia.tempoTremor = duracao;
    }

    // Prende a câmera entre dois x (a arena do chefe). Destravar() volta ao normal.
    public static void Travar(float esquerda, float direita)
    {
        if (instancia == null) return;
        instancia.limiteEsquerdo = esquerda;
        instancia.limiteDireito = direita;
    }

    public static void Destravar()
    {
        if (instancia == null) return;
        instancia.limiteEsquerdo = instancia.esquerdaDaFase;
        instancia.limiteDireito = instancia.direitaDaFase;
    }

    // Pula direto para o jogador, sem deslizar (usado quando ele atravessa uma porta).
    public static void Centralizar()
    {
        if (instancia == null || instancia.alvo == null) return;
        instancia.velocidadeX = 0f;
        instancia.xSuave = instancia.PosicaoDesejada();
        instancia.transform.position = TelaPixelada.AlinharAoPixel(new Vector3(instancia.xSuave, instancia.alturaFixa, -10f), instancia.cam);
    }

    float PosicaoDesejada()
    {
        float meiaLargura = cam.orthographicSize * cam.aspect;
        float x = alvo != null ? alvo.position.x + olharAFrente : 0f;
        if (limiteDireito - limiteEsquerdo <= meiaLargura * 2f) return (limiteEsquerdo + limiteDireito) / 2f;
        return Mathf.Clamp(x, limiteEsquerdo + meiaLargura, limiteDireito - meiaLargura);
    }

    void LateUpdate()
    {
        if (alvo == null) return;

        xSuave = Mathf.SmoothDamp(xSuave, PosicaoDesejada(), ref velocidadeX, suavidade);
        Vector3 posicao = new Vector3(xSuave, alturaFixa, -10f);

        if (tempoTremor > 0f)
        {
            tempoTremor -= Time.deltaTime;
            posicao += (Vector3)(Random.insideUnitCircle * forcaTremor);
        }
        transform.position = TelaPixelada.AlinharAoPixel(posicao, cam);
    }
}
