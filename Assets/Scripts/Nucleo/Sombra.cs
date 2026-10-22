using UnityEngine;

// Sombrinha "projetada" no fundo, 2 pixels para a direita e para baixo (o visual de adesivo).
// Copia o desenho do objeto a cada quadro, então acompanha animações, viradas e quando ele some.
//
// DESEMPENHO: uma fase tem centenas de blocos e enfeites com sombra, e o mapa do mundo tem mais de mil.
// Copiar tudo a cada quadro (desenho, virada, cor, posição) custa caro e não serve para nada quando o
// objeto nunca muda. Por isso existe a sombra "parada": ela copia o desenho no primeiro quadro e
// depois se DESLIGA (enabled = false: o Unity nem chama mais o LateUpdate dela). Como ela é "filha" do
// objeto, continua no lugar certo sozinha. Use parada para blocos, pedras, casas...; e a normal para
// quem anda, vira ou troca de desenho (o Grumete, os bichos).
public class Sombra : MonoBehaviour
{
    public const int Ordem = -9; // atrás de tudo, mas na frente das nuvens e dos brilhos
    static readonly Vector3 Deslocamento = new Vector3(2f, -2f, 0f) / FabricaDeSprites.PixelsPorUnidade;
    static readonly Color Cor = new Color(0.12f, 0.05f, 0.25f, 0.28f);

    SpriteRenderer original, desenho;
    bool parada;

    // O desenho da sombra (o Vento usa para trocar o quadro da sombra junto com o da planta).
    public SpriteRenderer Desenho => desenho;

    public static Sombra Adicionar(SpriteRenderer original, bool parada = false)
    {
        var objeto = new GameObject("Sombra");
        objeto.transform.SetParent(original.transform, false);
        var sombra = objeto.AddComponent<Sombra>();
        sombra.original = original;
        sombra.parada = parada;
        sombra.desenho = objeto.AddComponent<SpriteRenderer>();
        sombra.desenho.sortingOrder = Ordem;
        sombra.Copiar();
        return sombra;
    }

    // Daqui para a frente a sombra não se atualiza mais sozinha (quem mexe no desenho cuida dela).
    public void Parar() => parada = true;

    void LateUpdate()
    {
        Copiar();
        // a sombra parada copia uma última vez no primeiro quadro (pega a cor e a escala que o objeto
        // ganhou depois de criado) e depois desliga
        if (parada) enabled = false;
    }

    void Copiar()
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
