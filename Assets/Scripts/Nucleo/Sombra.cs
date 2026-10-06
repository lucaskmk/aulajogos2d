using UnityEngine;

// Sombrinha "projetada" no fundo, 2 pixels para a direita e para baixo (o visual dos ímãs de Re:Zero).
// Copia o desenho do objeto a cada quadro, então acompanha animações, viradas e quando ele some.
public class Sombra : MonoBehaviour
{
    public const int Ordem = -9; // atrás de tudo, mas na frente das nuvens e dos brilhos
    static readonly Vector3 Deslocamento = new Vector3(2f, -2f, 0f) / FabricaDeSprites.PixelsPorUnidade;
    static readonly Color Cor = new Color(0.12f, 0.05f, 0.25f, 0.28f);

    SpriteRenderer original, desenho;

    public static void Adicionar(SpriteRenderer original)
    {
        var objeto = new GameObject("Sombra");
        objeto.transform.SetParent(original.transform, false);
        var sombra = objeto.AddComponent<Sombra>();
        sombra.original = original;
        sombra.desenho = objeto.AddComponent<SpriteRenderer>();
        sombra.desenho.sortingOrder = Ordem;
        sombra.LateUpdate();
    }

    void LateUpdate()
    {
        if (original == null) return;
        desenho.sprite = original.sprite;
        desenho.flipX = original.flipX;
        desenho.flipY = original.flipY;
        desenho.enabled = original.enabled; // bloco invisível continua sem sombra
        desenho.color = new Color(Cor.r, Cor.g, Cor.b, Cor.a * original.color.a);
        transform.position = original.transform.position + Deslocamento;
    }
}
