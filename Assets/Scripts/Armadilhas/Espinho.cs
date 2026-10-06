using UnityEngine;

// '^' - espinho comum, parado, sempre visível. O mais honesto do jogo.
public class Espinho : MonoBehaviour
{
    void Awake()
    {
        var visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = FabricaDeSprites.Pegar("espinho");
        visual.sortingOrder = 1;
        Sombra.Adicionar(visual);
        Perigo.Adicionar(gameObject, new Vector2(0.75f, 0.45f), new Vector2(0f, -0.25f));
    }
}
