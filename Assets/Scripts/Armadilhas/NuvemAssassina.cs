using UnityEngine;

// 'o' - igualzinha às nuvens do cenário... mas tem dentes. E mata.
public class NuvemAssassina : MonoBehaviour
{
    public float distanciaParaMostrarACara = 2.5f;

    SpriteRenderer visual;
    Sprite normal, malvada;

    void Awake()
    {
        normal = FabricaDeSprites.Pegar("nuvem");
        malvada = FabricaDeSprites.Pegar("nuvem_malvada");
        visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = normal;
        visual.sortingOrder = -9;
        Perigo.Adicionar(gameObject, new Vector2(1.6f, 0.7f), Vector2.zero);
    }

    void Update()
    {
        bool perto = GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)
                     && Vector2.Distance(jogador, transform.position) < distanciaParaMostrarACara;
        visual.sprite = perto ? malvada : normal;
    }
}
