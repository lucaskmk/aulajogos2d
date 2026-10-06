using UnityEngine;

// Câmera que segue o jogador na horizontal, sem mostrar nada fora da fase,
// e que sabe "tremer" quando algo pesado cai.
[RequireComponent(typeof(Camera))]
public class CameraSeguir : MonoBehaviour
{
    public float suavidade = 0.12f;
    public float olharAFrente = 2f;

    Transform alvo;
    Camera cam;
    float limiteEsquerdo, limiteDireito, alturaFixa;
    float velocidadeX;
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
        limiteEsquerdo = -0.5f;
        limiteDireito = larguraFase - 0.5f;
        velocidadeX = 0f;
        transform.position = new Vector3(PosicaoDesejada(), alturaFixa, -10f);
    }

    public static void Tremer(float forca, float duracao)
    {
        if (instancia == null) return;
        instancia.forcaTremor = forca;
        instancia.tempoTremor = duracao;
    }

    // Pula direto para o jogador, sem deslizar (usado quando ele atravessa uma porta).
    public static void Centralizar()
    {
        if (instancia == null || instancia.alvo == null) return;
        instancia.velocidadeX = 0f;
        instancia.transform.position = new Vector3(instancia.PosicaoDesejada(), instancia.alturaFixa, -10f);
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

        float x = Mathf.SmoothDamp(transform.position.x, PosicaoDesejada(), ref velocidadeX, suavidade);
        Vector3 posicao = new Vector3(x, alturaFixa, -10f);

        if (tempoTremor > 0f)
        {
            tempoTremor -= Time.deltaTime;
            posicao += (Vector3)(Random.insideUnitCircle * forcaTremor);
        }
        transform.position = posicao;
    }
}
