using System.Collections.Generic;
using UnityEngine;

// "Volta da Maré" como mecânica: no lugar de cada uma das últimas mortes nesta fase fica um X vermelho
// (X marca o lugar!). E no lugar da ÚLTIMA morte fica fincada a BANDEIRA DE MORTO: se foi num buraco ou num
// chão falso, ela fica na altura do chão, bem em cima de onde você afundou. Assim, na volta, dá para ver onde
// o chão mente. As marcas mais antigas são mais apagadas. Nada disso encosta em nada: é só desenho.
public class MarcaDaMorte : MonoBehaviour
{
    SpriteRenderer desenho;
    float transparencia;
    float fase;
    bool ehBandeira;

    // A bandeira pirata (16 x 32 pixels): o mastro fica na coluna 3 do desenho.
    static readonly string[] ArteDaBandeira =
    {
        "...kk...........",
        "..kyyk..........",
        "..kkkkkkkkkkkkk.",
        "..kbkrrrrrrrrrrk",
        "..kdkrrrwwwwrrrk",
        "..kbkrrwwwwwwrrk",
        "..kdkrrwkwwkwrrk",
        "..kbkrrwkwwkwrrk",
        "..kdkrrwwwwwwrrk",
        "..kbkrrrwkkwrrrk",
        "..kdkrrrwwwwrrk.",
        "..kbkrrrrwwrrrk.",
        "..kdkrrrrrrrk.k.",
        "..kbkkrrkrrkk...",
        "..kdk.kk.kk.....",
        "..kbk...........",
        "..kdk...........",
        "..kbk...........",
        "..kdk...........",
        "..kbk...........",
        "..kdk...........",
        "..kbk...........",
        "..kdk...........",
        "..kbk...........",
        "..kdk...........",
        "..kbk...........",
        "..kdk...........",
        "..kbk...........",
        "..kdk...........",
        ".kdddk..........",
        "kdbbbdk.........",
        "kkkkkkk.........",
    };
    const float DesvioDoMastro = 4.5f / FabricaDeSprites.PixelsPorUnidade; // do meio do desenho até o mastro

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos() =>
        FabricaDeSprites.Registrar("bandeira_de_morto", () => FabricaDeSprites.DeArte(ArteDaBandeira, FabricaDeSprites.Base));

    // lugares: do mais antigo para o mais recente. bandeira: o pé da bandeira da última morte (null = ainda não morreu).
    public static void Criar(Transform raiz, List<Vector3> lugares, Vector3? bandeira)
    {
        int quantosX = bandeira.HasValue ? lugares.Count - 1 : lugares.Count; // a última morte ganha a bandeira no lugar do X
        for (int i = 0; i < quantosX; i++)
        {
            var objeto = ConstrutorDeFase.Visual("MarcaDaMorte", raiz, lugares[i], "marca_x", 1, false);
            objeto.transform.localScale = Vector3.one * 0.6f;
            objeto.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(i * 2.3f + lugares[i].x) * 25f); // cada uma meio torta
            var marca = objeto.AddComponent<MarcaDaMorte>();
            marca.desenho = objeto.GetComponent<SpriteRenderer>();
            marca.transparencia = Mathf.Lerp(0.25f, 0.6f, (i + 1f) / lugares.Count);
            marca.fase = i * 1.3f;
            Luzes.Ponto(objeto.transform, Luzes.Lilas, 1.2f, 0.25f + 0.1f * i / Mathf.Max(1, lugares.Count - 1));
        }
        if (bandeira.HasValue) FincarBandeira(raiz, bandeira.Value);
    }

    static void FincarBandeira(Transform raiz, Vector3 pe)
    {
        // (ordem 6: na frente do chão, para aparecer mesmo em cima de um chão falso)
        var objeto = ConstrutorDeFase.Visual("BandeiraDeMorto", raiz, pe + Vector3.right * DesvioDoMastro, "bandeira_de_morto", 6, false);
        var marca = objeto.AddComponent<MarcaDaMorte>();
        marca.desenho = objeto.GetComponent<SpriteRenderer>();
        marca.ehBandeira = true;
        Luzes.Ponto(objeto.transform, Luzes.Lilas, 2.5f, 0.6f, Vector3.up * 1.5f); // nas fases escuras ela também aparece
    }

    void Update()
    {
        if (ehBandeira) // balança de leve, presa pelo pé
        {
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.2f) * 2.5f);
            return;
        }
        // "pulsa" devagar, como se estivesse viva
        float pulso = 0.8f + 0.2f * Mathf.Sin(Time.time * 2f + fase);
        desenho.color = new Color(1f, 1f, 1f, transparencia * pulso);
    }
}
