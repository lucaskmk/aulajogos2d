using System.Collections.Generic;
using UnityEngine;

// Transforma as músicas de Musicas (compostas e sintetizadas por código, composição original) em AudioClips.
//  - Fases: a "Jiga do Grumete", cada fase num tom um pouco diferente (para não cansar);
//  - Mapa:  a "Balada do Arquipélago";
//  - Chefes: cada um tem a SUA música de luta (veja DoChefe).
// Gerar uma música demora um pouquinho (meio segundo, mais ou menos), então cada uma é gerada uma vez só
// e fica guardada.
public static class FabricaDeMusica
{
    public const int MusicaDoMapa = 100; // "fase" especial: a música do mapa do mundo

    // Tom de cada fase, em semitons a partir de ré menor (pequeno, para a melodia não ficar grave nem estridente).
    static readonly int[] Tons = { 0, 2, -2, 3, -3, 1, -1 };

    static readonly Dictionary<int, AudioClip> cacheDasFases = new Dictionary<int, AudioClip>(); // chave: o tom
    static readonly Dictionary<Musicas.Tema, AudioClip> cacheDosTemas = new Dictionary<Musicas.Tema, AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void LimparCache()
    {
        cacheDasFases.Clear();
        cacheDosTemas.Clear();
    }

    // A música de uma fase (ou a do mapa, com MusicaDoMapa).
    public static AudioClip DaFase(int fase)
    {
        if (fase == MusicaDoMapa) return DoTema(Musicas.Tema.Mapa);
        int tom = Tons[Mathf.Abs(fase) % Tons.Length];
        if (cacheDasFases.TryGetValue(tom, out AudioClip pronta) && pronta != null) return pronta;
        AudioClip clipe = Criar("musica_fase_" + tom, Musicas.Compor(Musicas.Tema.Fase, tom));
        cacheDasFases[tom] = clipe;
        return clipe;
    }

    // A música da luta de um chefe. O GerenciadorDoJogo já chama isto quando a fase do chefe carrega
    // (atrás da transição), e na hora da luta ela já está pronta.
    public static AudioClip DoChefe(Musicas.Tema tema) => DoTema(tema);

    static AudioClip DoTema(Musicas.Tema tema)
    {
        if (cacheDosTemas.TryGetValue(tema, out AudioClip pronta) && pronta != null) return pronta;
        AudioClip clipe = Criar("musica_" + tema.ToString().ToLowerInvariant(), Musicas.Compor(tema));
        cacheDosTemas[tema] = clipe;
        return clipe;
    }

    static AudioClip Criar(string nome, float[] amostras)
    {
        var clipe = AudioClip.Create(nome, amostras.Length, 1, Musicas.Taxa, false);
        clipe.SetData(amostras, 0);
        return clipe;
    }
}
