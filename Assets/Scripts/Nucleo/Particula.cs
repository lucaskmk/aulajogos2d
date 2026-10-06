using UnityEngine;

// Pontinho de poeira que sai voando, freia, encolhe e some. Usado em Efeitos.Poeira.
public class Particula : MonoBehaviour
{
    Vector3 velocidade;
    float vida, duracao, tamanho;
    Color cor;
    SpriteRenderer desenho;

    public static void Criar(Transform pai, Vector3 posicao, Vector2 velocidade, float duracao, float tamanho, Color cor)
    {
        var objeto = new GameObject("Poeira");
        objeto.transform.SetParent(pai, false);
        objeto.transform.position = posicao;
        var particula = objeto.AddComponent<Particula>();
        particula.velocidade = velocidade;
        particula.duracao = duracao;
        particula.tamanho = tamanho;
        particula.cor = cor;
        particula.desenho = objeto.AddComponent<SpriteRenderer>();
        particula.desenho.sprite = FabricaDeSprites.Pegar("poeira");
        particula.desenho.sortingOrder = 9;
        particula.Update();
    }

    void Update()
    {
        vida += Time.deltaTime;
        float p = vida / duracao;
        if (p >= 1f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += velocidade * Time.deltaTime;
        velocidade *= 1f - 4f * Time.deltaTime; // freia no ar
        transform.localScale = Vector3.one * tamanho * (1f - 0.5f * p);
        desenho.color = new Color(cor.r, cor.g, cor.b, cor.a * (1f - p));
    }
}
