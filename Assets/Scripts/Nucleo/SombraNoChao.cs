using System.Collections.Generic;
using UnityEngine;

// Sombra oval no chão, embaixo de quem está no ar (Subaru, Mabeasts, coelhos, Puck...).
// Quanto mais alto, menor e mais fraquinha. Ajuda muito a saber onde você vai cair.
public class SombraNoChao : MonoBehaviour
{
    public float largura = 0.9f;
    public float alcance = 7f;

    Transform sombra;
    SpriteRenderer desenho;
    Jogador jogador; // se for o Subaru: some quando ele morre
    ContactFilter2D filtro;
    readonly List<RaycastHit2D> acertos = new List<RaycastHit2D>();

    public static SombraNoChao Adicionar(GameObject dono, float largura)
    {
        var componente = dono.AddComponent<SombraNoChao>();
        componente.largura = largura;
        return componente;
    }

    void Awake()
    {
        var objeto = new GameObject("SombraNoChao");
        objeto.transform.SetParent(transform, false);
        sombra = objeto.transform;
        desenho = objeto.AddComponent<SpriteRenderer>();
        desenho.sprite = FabricaDeSprites.Pegar("sombra_oval");
        desenho.sortingOrder = 1; // em cima do chão, embaixo dos personagens
        filtro = new ContactFilter2D { useTriggers = false };
        jogador = GetComponent<Jogador>();
    }

    void LateUpdate()
    {
        int quantidade = Physics2D.Raycast(transform.position, Vector2.down, filtro, acertos, alcance);
        float maisPerto = float.MaxValue;
        Vector2 ponto = Vector2.zero;
        for (int i = 0; i < quantidade; i++)
        {
            Collider2D outro = acertos[i].collider;
            if (outro.transform == transform || outro.transform.IsChildOf(transform) || Inimigo.EhBicho(outro)
                || outro.GetComponent<Jogador>() != null) continue;
            if (acertos[i].distance < maisPerto)
            {
                maisPerto = acertos[i].distance;
                ponto = acertos[i].point;
            }
        }

        bool visivel = maisPerto < alcance && !(jogador != null && jogador.Morto);
        desenho.enabled = visivel;
        if (!visivel) return;

        float altura = Mathf.Clamp01(maisPerto / alcance);
        sombra.position = new Vector3(transform.position.x, ponto.y + 0.03f, 0f);
        sombra.localScale = new Vector3(largura * (1f - altura * 0.6f), 1f - altura * 0.4f, 1f);
        desenho.color = new Color(0f, 0f, 0f, 0.3f * (1f - altura));
    }
}
