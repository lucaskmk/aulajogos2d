using System.Collections.Generic;
using UnityEngine;

// "Volta da Maré" como mecânica: no lugar de cada uma das últimas mortes nesta fase fica um X vermelho
// (X marca o lugar!). E no lugar da ÚLTIMA morte de cada jogador fica fincada a BANDEIRA DE MORTO dele
// (no coop, cada um tem a sua, com o pano da cor do jogador): se foi num buraco ou num
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
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("bandeira_de_morto", () => FabricaDeSprites.DeArte(ArteDaBandeira, FabricaDeSprites.Base));
        // coop: a bandeira de cada jogador tem o pano da cor dele (a do P1 é a vermelha de sempre)
        for (int i = 1; i < Tripulacao.Cores.Length; i++)
        {
            Color32 pano = Tripulacao.Cores[i];
            FabricaDeSprites.Registrar(SpriteDaBandeira(i), () => FabricaDeSprites.Procedural(16, ArteDaBandeira.Length, FabricaDeSprites.Base, (x, y) =>
            {
                string linha = ArteDaBandeira[ArteDaBandeira.Length - 1 - y]; // a primeira linha do texto é o topo
                char c = x < linha.Length ? linha[x] : '.';
                return c == 'r' ? pano : FabricaDeSprites.CorOuTransparente(c);
            }));
        }
    }

    public static string SpriteDaBandeira(int jogador) =>
        jogador <= 0 ? "bandeira_de_morto" : "bandeira_de_morto_" + (jogador % Tripulacao.Cores.Length + 1);

    // lugares: as últimas mortes da fase, da mais antiga para a mais recente; donos: qual jogador morreu em cada uma.
    // bandeiras: o pé da bandeira de CADA jogador (a última morte dele; null = ainda não morreu nesta fase).
    // A última morte de cada um ganha a bandeira (da cor dele) no lugar do X. Devolve em "fincadas" as bandeiras criadas.
    public static void Criar(Transform raiz, List<Vector3> lugares, List<int> donos, Vector3?[] bandeiras, GameObject[] fincadas)
    {
        // a última morte de cada jogador que tem bandeira não ganha X
        var semX = new HashSet<int>();
        for (int jogador = 0; jogador < bandeiras.Length; jogador++)
        {
            if (!bandeiras[jogador].HasValue) continue;
            for (int i = lugares.Count - 1; i >= 0; i--)
                if (donos[i] == jogador) { semX.Add(i); break; }
        }
        for (int i = 0; i < lugares.Count; i++)
        {
            if (semX.Contains(i)) continue;
            var objeto = ConstrutorDeFase.Visual("MarcaDaMorte", raiz, lugares[i], "marca_x", 1, false);
            objeto.transform.localScale = Vector3.one * 0.6f;
            objeto.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(i * 2.3f + lugares[i].x) * 25f); // cada uma meio torta
            var marca = objeto.AddComponent<MarcaDaMorte>();
            marca.desenho = objeto.GetComponent<SpriteRenderer>();
            marca.transparencia = Mathf.Lerp(0.25f, 0.6f, (i + 1f) / lugares.Count);
            marca.fase = i * 1.3f;
            Luzes.Ponto(objeto.transform, Luzes.Lilas, 1.2f, 0.25f + 0.1f * i / Mathf.Max(1, lugares.Count - 1));
        }
        for (int jogador = 0; jogador < bandeiras.Length; jogador++)
        {
            fincadas[jogador] = null;
            if (bandeiras[jogador].HasValue) fincadas[jogador] = FincarBandeira(raiz, bandeiras[jogador].Value, jogador);
        }
    }

    // Finca a bandeira de morto de um jogador (o pano é da cor dele). No coop ela é fincada na hora da morte,
    // sem esperar a fase recomeçar: os outros já veem onde o chão mente.
    public static GameObject FincarBandeira(Transform raiz, Vector3 pe, int jogador)
    {
        // (ordem 6: na frente do chão, para aparecer mesmo em cima de um chão falso)
        var objeto = ConstrutorDeFase.Visual("BandeiraDeMorto", raiz, pe + Vector3.right * DesvioDoMastro, SpriteDaBandeira(jogador), 6, false);
        var marca = objeto.AddComponent<MarcaDaMorte>();
        marca.desenho = objeto.GetComponent<SpriteRenderer>();
        marca.ehBandeira = true;
        marca.fase = jogador * 1.3f; // (cada bandeira balança no seu ritmo)
        Luzes.Ponto(objeto.transform, Luzes.Lilas, 2.5f, 0.6f, Vector3.up * 1.5f); // nas fases escuras ela também aparece
        return objeto;
    }

    void Update()
    {
        if (ehBandeira) // balança de leve, presa pelo pé
        {
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.2f + fase) * 2.5f);
            return;
        }
        // "pulsa" devagar, como se estivesse viva
        float pulso = 0.8f + 0.2f * Mathf.Sin(Time.time * 2f + fase);
        desenho.color = new Color(1f, 1f, 1f, transparencia * pulso);
    }
}
