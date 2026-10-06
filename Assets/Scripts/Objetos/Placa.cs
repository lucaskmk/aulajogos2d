using UnityEngine;

// 'i' - placa com uma "dica". Não confie nela.
// O texto aparece na tela quando o jogador chega perto (desenhado pelo GerenciadorDoJogo).
public class Placa : MonoBehaviour
{
    public string texto;

    void Awake()
    {
        var visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = FabricaDeSprites.Pegar("placa");
        visual.sortingOrder = -1;
        Sombra.Adicionar(visual);
    }

    void OnEnable()
    {
        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.placas.Add(this);
    }

    void OnDisable()
    {
        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.placas.Remove(this);
    }
}
