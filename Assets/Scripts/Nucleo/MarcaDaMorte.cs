using System.Collections.Generic;
using UnityEngine;

// "Retorno pela Morte" como mecânica: no lugar de cada uma das últimas mortes nesta fase
// fica a marca da mão da Bruxa. Assim você vê onde já morreu (e o Subaru "lembra").
// As marcas mais antigas são mais apagadas. Não encostam em nada: é só desenho.
public class MarcaDaMorte : MonoBehaviour
{
    SpriteRenderer desenho;
    float transparencia;
    float fase;

    // lugares: do mais antigo para o mais recente
    public static void Criar(Transform raiz, List<Vector3> lugares)
    {
        for (int i = 0; i < lugares.Count; i++)
        {
            var objeto = ConstrutorDeFase.Visual("MarcaDaMorte", raiz, lugares[i], "mao_sombra", 1, false);
            objeto.transform.localScale = Vector3.one * 0.6f;
            objeto.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(i * 2.3f + lugares[i].x) * 25f); // cada uma meio torta
            var marca = objeto.AddComponent<MarcaDaMorte>();
            marca.desenho = objeto.GetComponent<SpriteRenderer>();
            marca.transparencia = Mathf.Lerp(0.25f, 0.6f, (i + 1f) / lugares.Count);
            marca.fase = i * 1.3f;
            Luzes.Ponto(objeto.transform, Luzes.Lilas, 1.2f, 0.25f + 0.1f * i / Mathf.Max(1, lugares.Count - 1));
        }
    }

    void Update()
    {
        // "pulsa" devagar, como se estivesse viva
        float pulso = 0.8f + 0.2f * Mathf.Sin(Time.time * 2f + fase);
        desenho.color = new Color(1f, 1f, 1f, transparencia * pulso);
    }
}
