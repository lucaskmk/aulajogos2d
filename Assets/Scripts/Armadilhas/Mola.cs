using UnityEngine;

// 'S' - mola que joga o jogador lá pra cima (às vezes direto num espinho).
public class Mola : MonoBehaviour
{
    public float forca = 22f;

    SpriteRenderer visual;
    float timerApertada;

    void Awake()
    {
        visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = FabricaDeSprites.Pegar("mola");
        visual.sortingOrder = 2;

        var area = gameObject.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(0.9f, 0.5f);
        area.offset = new Vector2(0f, -0.2f);
    }

    void OnTriggerEnter2D(Collider2D outro) => Testar(outro);
    void OnTriggerStay2D(Collider2D outro) => Testar(outro);

    void Testar(Collider2D outro)
    {
        var jogador = outro.GetComponent<Jogador>();
        if (jogador == null || jogador.Morto || jogador.Corpo.linearVelocity.y > 0.5f) return;

        jogador.Quicar(forca, false);
        GerenciadorDoJogo.Som("mola");
        visual.sprite = FabricaDeSprites.Pegar("mola_apertada");
        timerApertada = 0.15f;
    }

    void Update()
    {
        if (timerApertada <= 0f) return;
        timerApertada -= Time.deltaTime;
        if (timerApertada <= 0f) visual.sprite = FabricaDeSprites.Pegar("mola");
    }
}
