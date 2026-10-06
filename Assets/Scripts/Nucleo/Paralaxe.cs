using UnityEngine;

// Fundo em camadas, estilo Castlevania: cada camada anda numa velocidade diferente
// quando a câmera se move, e isso dá a impressão de profundidade (parece 3D, mas é 2D).
//
// "fator" diz o quanto a camada acompanha a câmera:
//   0 = anda junto com a fase (está "colada" no chão), 1 = fica parada na tela (infinitamente longe).
[DefaultExecutionOrder(100)] // roda depois da CameraSeguir, para não tremer
public class Paralaxe : MonoBehaviour
{
    public float fator;

    // As camadas são pintadas com a cor de fundo da fase, cada vez mais escura quanto mais perto.
    public static void Criar(Transform raiz, int larguraFase, Color corDoFundo)
    {
        var escuro = new Color(0.15f, 0.1f, 0.3f);
        // (as ordens pulam o -23 de propósito: é onde a Baleia Branca nada no fundo)
        Camada(raiz, "fundo_castelo", 0.85f, -24, Color.Lerp(corDoFundo, escuro, 0.15f), larguraFase);
        Camada(raiz, "fundo_floresta", 0.6f, -22, Color.Lerp(corDoFundo, escuro, 0.3f), larguraFase);
        Camada(raiz, "fundo_arvores", 0.35f, -20, Color.Lerp(corDoFundo, escuro, 0.45f), larguraFase);
    }

    // Versão pequena, para o horizonte do mapa do mundo: só a floresta e as árvores, encolhidas,
    // com a base em "baseY". Quanto mais longe, mais a cor puxa para a do céu (parece neblina).
    public static void CriarHorizonte(Transform raiz, int largura, float baseY, float escala, Color corDoCeu, Color corDoChao)
    {
        var escuro = new Color(0.15f, 0.1f, 0.3f);
        Camada(raiz, "fundo_floresta", 0.7f, -24, Color.Lerp(corDoCeu, escuro, 0.25f), largura, baseY, escala);
        Camada(raiz, "fundo_arvores", 0.45f, -22, Color.Lerp(corDoChao, escuro, 0.2f), largura, baseY - 0.1f, escala);
    }

    static void Camada(Transform raiz, string sprite, float fator, int ordem, Color cor, int larguraFase, float baseY = -1f, float escala = 1f)
    {
        var objeto = new GameObject("Paralaxe " + sprite);
        objeto.transform.SetParent(raiz, false);
        objeto.AddComponent<Paralaxe>().fator = fator;

        // Repete a imagem lado a lado com folga, para cobrir a tela em qualquer ponto da fase.
        Sprite desenho = FabricaDeSprites.Pegar(sprite);
        float largura = desenho.bounds.size.x * escala;
        for (float x = -20f - largura; x < larguraFase + 20f + largura; x += largura)
        {
            var pedaco = new GameObject("Pedaco");
            pedaco.transform.SetParent(objeto.transform, false);
            pedaco.transform.localPosition = new Vector3(x, baseY, 0f); // nas fases, a base fica escondida atrás do chão
            pedaco.transform.localScale = Vector3.one * escala;
            var visual = pedaco.AddComponent<SpriteRenderer>();
            visual.sprite = desenho;
            visual.color = cor;
            visual.sortingOrder = ordem;
        }
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        transform.position = new Vector3(cam.transform.position.x * fator, 0f, 0f);
    }
}
