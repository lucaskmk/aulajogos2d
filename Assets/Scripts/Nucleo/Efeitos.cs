using System.Collections;
using UnityEngine;

// Pequenas animações reaproveitadas por vários objetos.
public static class Efeitos
{
    // Sobe e desce rapidinho (bloco que levou cabeçada).
    public static IEnumerator Pulinho(Transform alvo, float altura = 0.3f, float duracao = 0.16f)
    {
        Vector3 inicio = alvo.localPosition;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            if (alvo == null) yield break;
            alvo.localPosition = inicio + Vector3.up * Mathf.Sin(t / duracao * Mathf.PI) * altura;
            yield return null;
        }
        if (alvo != null) alvo.localPosition = inicio;
    }

    // Moeda que salta do bloco e some.
    public static IEnumerator MoedaSaltando(Transform pai, Vector3 posicao)
    {
        var moeda = new GameObject("MoedaSaltando");
        moeda.transform.SetParent(pai, false);
        moeda.transform.position = posicao;
        var visual = moeda.AddComponent<SpriteRenderer>();
        visual.sprite = FabricaDeSprites.Pegar("moeda");
        visual.sortingOrder = 4;

        const float duracao = 0.4f;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            if (moeda == null) yield break;
            float p = t / duracao;
            moeda.transform.position = posicao + Vector3.up * (2.5f * p - 1.5f * p * p);
            moeda.transform.localScale = new Vector3(Mathf.Cos(p * Mathf.PI * 6f), 1f, 1f); // "girando"
            yield return null;
        }
        if (moeda != null) Object.Destroy(moeda);
    }
}
