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

    static void Camada(Transform raiz, string sprite, float fator, int ordem, Color cor, int larguraFase)
    {
        var objeto = new GameObject("Paralaxe " + sprite);
        objeto.transform.SetParent(raiz, false);
        objeto.AddComponent<Paralaxe>().fator = fator;

        // Repete a imagem lado a lado com folga, para cobrir a tela em qualquer ponto da fase.
        Sprite desenho = FabricaDeSprites.Pegar(sprite);
        float largura = desenho.bounds.size.x;
        for (float x = -20f - largura; x < larguraFase + 20f + largura; x += largura)
        {
            var pedaco = new GameObject("Pedaco");
            pedaco.transform.SetParent(objeto.transform, false);
            pedaco.transform.localPosition = new Vector3(x, -1f, 0f); // a base fica escondida atrás do chão
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
