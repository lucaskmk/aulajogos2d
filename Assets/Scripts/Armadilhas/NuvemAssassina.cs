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
        visual.sortingOrder = -10; // igual às nuvens do cenário
        Perigo.Adicionar(gameObject, new Vector2(1.6f, 0.7f), Vector2.zero).conquista = "nuvem";
        Perigo.TornarMovel(gameObject); // ela se mexe (flutua e tem paralaxe)
        // e flutua igual a elas, na mesma camada de paralaxe
        Animacao.Adicionar(gameObject, Animacao.Tipo.Flutuar, 0.4f, 0.4f, ConstrutorDeFase.ParalaxeDasNuvens);
    }

    void Update()
    {
        bool perto = GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)
                     && Vector2.Distance(jogador, transform.position) < distanciaParaMostrarACara;
        visual.sprite = perto ? malvada : normal;
    }
}
