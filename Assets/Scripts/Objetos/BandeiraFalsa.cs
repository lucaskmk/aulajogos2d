using UnityEngine;

// 'Z' - parece a bandeira de chegada... mas quando você encosta, ela fica vermelha,
// brotam espinhos e você morre. A de verdade está mais pra frente.
public class BandeiraFalsa : MonoBehaviour
{
    SpriteRenderer visual;
    bool revelada;

    void Awake()
    {
        visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = FabricaDeSprites.Pegar("bandeira");
        visual.sortingOrder = 1;
        Sombra.Adicionar(visual);

        var area = gameObject.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(0.5f, 3f);
        area.offset = new Vector2(0f, 1.5f);
    }

    void OnTriggerEnter2D(Collider2D outro)
    {
        var jogador = outro.GetComponent<Jogador>();
        if (jogador == null || jogador.Morto) return;

        if (!revelada)
        {
            revelada = true;
            visual.sprite = FabricaDeSprites.Pegar("bandeira_falsa");
            GerenciadorDoJogo.Som("risada");
            for (int i = -1; i <= 1; i++)
                ConstrutorDeFase.Visual("EspinhoDaBandeira", transform, transform.position + new Vector3(i, 0.5f, 0f), "espinho", 2);
        }
        jogador.Morrer();
    }
}
