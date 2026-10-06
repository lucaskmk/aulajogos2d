using System.Collections.Generic;
using UnityEngine;

// Músicas geradas por código (composição original, nada copiado).
// Fases, mapa e a luta contra a Baleia usam a música "épica de anime" de FabricaDeEpico.
// (A função Compor daqui é o lofi 8-bit antigo, guardado como alternativa.)
public static class FabricaDeMusica
{
    const int Taxa = 16000;          // qualidade baixinha de propósito: combina com o clima "lofi" (e gera mais rápido)
    const float Batidas = 72f;       // batidas por minuto (bem calminho)
    const int Compassos = 8;         // o trecho que fica repetindo (loop)

    public const int MusicaDoMapa = 100;  // "fase" especial: a música do mapa do mundo
    public const int MusicaDoChefe = 101; // a luta contra a Baleia Branca: mais rápida, tensa, bumbo em toda batida

    static readonly Dictionary<int, AudioClip> cache = new Dictionary<int, AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void LimparCache() => cache.Clear();

    // Sequências de acordes (graus da escala maior, 0 = primeiro grau). Cada acorde dura 1 compasso.
    static readonly int[][] Progressoes =
    {
        new[] { 1, 4, 0, 5 }, // ii - V - I - vi   (a clássica do jazz/lofi)
        new[] { 3, 2, 1, 0 }, // IV - iii - ii - I (descendo devagar)
        new[] { 5, 3, 0, 4 }, // vi - IV - I - V
        new[] { 0, 5, 3, 4 }, // I - vi - IV - V
    };

    // A do chefe: vi - IV - V - V (do tom menor relativo, mais "épico").
    static readonly int[] ProgressaoDoChefe = { 5, 3, 4, 4 };

    // Tom de cada fase (em semitons a partir de Dó). A última fase fica mais grave e misteriosa.
    static readonly int[] Tons = { 0, -3, 2, -5, 5, -2, 3, -7 };

    static readonly int[] EscalaMaior = { 0, 2, 4, 5, 7, 9, 11 };

    public static AudioClip Lofi(int fase)
    {
        if (cache.TryGetValue(fase, out AudioClip pronta) && pronta != null) return pronta;
        FabricaDeEpico.Jeito jeito = fase == MusicaDoChefe ? FabricaDeEpico.Jeito.Chefe
            : fase == MusicaDoMapa ? FabricaDeEpico.Jeito.Mapa : FabricaDeEpico.Jeito.Fase;
        AudioClip clipe = FabricaDeEpico.Compor(fase, jeito);
        cache[fase] = clipe;
        return clipe;
    }

    static AudioClip Compor(int fase)
    {
        var sorteio = new System.Random(1234 + fase * 97);
        bool chefe = fase == MusicaDoChefe;
        int[] progressao = chefe ? ProgressaoDoChefe : Progressoes[fase % Progressoes.Length];
        int tom = chefe ? -3 : Tons[fase % Tons.Length];

        float batida = 60f / (chefe ? 118f : Batidas);
        int amostrasPorBatida = Mathf.RoundToInt(batida * Taxa);
        int total = amostrasPorBatida * 4 * Compassos;

        PrepararVibrato(total);
        var acordes = new float[total];
        var baixo = new float[total];
        var melodia = new float[total];
        var bateria = new float[total];
        var ritmos = new int[4]; // um ritmo de melodia para cada acorde (repete na segunda metade)
        for (int i = 0; i < ritmos.Length; i++) ritmos[i] = sorteio.Next(Ritmos.Length);

        for (int compasso = 0; compasso < Compassos; compasso++)
        {
            int grau = progressao[compasso % progressao.Length];
            int inicio = compasso * 4 * amostrasPorBatida;

            // Acorde de 4 notas (tríade + sétima), tocado devagarinho no compasso inteiro.
            for (int n = 0; n < 4; n++)
            {
                float freq = Frequencia(60 + tom + NotaDaEscala(grau + n * 2));
                Nota(acordes, inicio + n * 200, 4 * amostrasPorBatida, freq, 0.07f, Onda.Triangulo, ataque: 0.25f);
            }

            // Baixo: fundamental nas batidas 1 e 3, e uma "passagem" no fim do compasso.
            float fundamental = Frequencia(36 + tom + NotaDaEscala(grau));
            Nota(baixo, inicio, Mathf.RoundToInt(1.7f * amostrasPorBatida), fundamental, 0.22f, Onda.Triangulo);
            Nota(baixo, inicio + 2 * amostrasPorBatida, Mathf.RoundToInt(1.2f * amostrasPorBatida), fundamental, 0.2f, Onda.Triangulo);
            Nota(baixo, inicio + Mathf.RoundToInt(3.5f * amostrasPorBatida), amostrasPorBatida / 2,
                Frequencia(36 + tom + NotaDaEscala(grau + 4)), 0.14f, Onda.Triangulo);

            ComporMelodia(melodia, sorteio, ritmos, compasso, grau, tom, inicio, amostrasPorBatida);
            ComporBateria(bateria, sorteio, inicio, amostrasPorBatida, chefe);
        }

        Eco(melodia, Mathf.RoundToInt(0.75f * amostrasPorBatida), 0.35f);

        // Mistura tudo, passa um filtro "abafado" (tira o agudo do 8-bit) e põe o chiado do vinil.
        var dados = new float[total];
        float filtrado = 0f, maximo = 0.0001f;
        for (int i = 0; i < total; i++)
        {
            float amostra = acordes[i] + baixo[i] + melodia[i] + bateria[i];
            filtrado += (amostra - filtrado) * 0.35f;
            float chiado = (float)(sorteio.NextDouble() * 2.0 - 1.0) * 0.006f;
            if (sorteio.Next(9000) == 0) chiado += 0.12f; // estalinho do disco
            dados[i] = filtrado + chiado;
            maximo = Mathf.Max(maximo, Mathf.Abs(dados[i]));
        }
        for (int i = 0; i < total; i++) dados[i] *= 0.8f / maximo;

        var clipe = AudioClip.Create("musica_lofi_" + fase, total, 1, Taxa, false);
        clipe.SetData(dados, 0);
        return clipe;
    }

    // Melodia de 8 colcheias por compasso. Os compassos 5 a 8 repetem a ideia dos 4 primeiros com variação.
    static readonly int[][] Ritmos =
    {
        new[] { 1, 0, 0, 1, 0, 0, 1, 0 },
        new[] { 1, 0, 1, 0, 0, 0, 0, 0 },
        new[] { 0, 0, 1, 0, 1, 0, 1, 1 },
        new[] { 1, 0, 0, 0, 1, 1, 0, 0 },
        new[] { 0, 1, 0, 1, 0, 0, 1, 0 },
    };

    static void ComporMelodia(float[] trilha, System.Random sorteio, int[] ritmos, int compasso, int grau, int tom, int inicio, int amostrasPorBatida)
    {
        int[] ritmo = Ritmos[ritmos[compasso % ritmos.Length]];
        bool ultimo = compasso == Compassos - 1;

        int degrau = grau + 7; // começa perto do acorde, uma oitava acima
        for (int passo = 0; passo < 8; passo++)
        {
            if (ritmo[passo] == 0 || (ultimo && passo > 0)) continue;
            // nota do acorde nos tempos fortes, passinhos na escala nos fracos
            if (passo % 2 == 0) degrau = grau + 7 + 2 * sorteio.Next(0, 3);
            else degrau += sorteio.Next(-1, 2);

            int swing = passo % 2 == 1 ? amostrasPorBatida / 6 : 0; // colcheia "atrasadinha"
            int posicao = inicio + passo * amostrasPorBatida / 2 + swing;
            int duracao = ultimo ? 4 * amostrasPorBatida : amostrasPorBatida / 2;
            Nota(trilha, posicao, duracao, Frequencia(60 + tom + NotaDaEscala(degrau)), 0.09f, Onda.Pulso);
        }
    }

    static void ComporBateria(float[] trilha, System.Random sorteio, int inicio, int amostrasPorBatida, bool chefe)
    {
        for (int b = 0; b < 4; b++)
        {
            int tempo = inicio + b * amostrasPorBatida;
            if (chefe || b == 0 || b == 2) Bumbo(trilha, tempo); // no chefe, bumbo em toda batida
            if (b == 1 || b == 3) Caixa(trilha, tempo, sorteio);
            Chimbal(trilha, tempo, sorteio, 0.03f);
            Chimbal(trilha, tempo + amostrasPorBatida / 2 + amostrasPorBatida / 6, sorteio, 0.018f); // com swing
        }
    }

    // ------------------------------------------------------------------ instrumentos

    enum Onda { Triangulo, Pulso }

    // Afinação "bamba" de fita cassete, igual para todos os instrumentos (calculada uma vez só).
    static float[] vibrato;

    static void PrepararVibrato(int total)
    {
        if (vibrato != null && vibrato.Length == total) return;
        vibrato = new float[total];
        for (int i = 0; i < total; i++) vibrato[i] = 1f + 0.003f * Mathf.Sin(2f * Mathf.PI * 0.5f * i / Taxa);
    }

    static void Nota(float[] trilha, int inicio, int duracao, float freq, float volume, Onda onda, float ataque = 0.02f)
    {
        float fase = 0f;
        for (int i = 0; i < duracao; i++)
        {
            int j = inicio + i;
            if (j < 0) continue;
            if (j >= trilha.Length) j -= trilha.Length; // passou do fim? continua no começo (o loop não "corta")
            float t = i / (float)duracao;
            fase += freq * vibrato[j] / Taxa;
            fase -= Mathf.Floor(fase);
            float valor = onda == Onda.Triangulo
                ? 1f - 4f * Mathf.Abs(fase - 0.5f)
                : (fase < 0.25f ? 1f : -0.33f); // pulso fino, mais macio que a quadrada
            float u = 1f - t;
            float envelope = Mathf.Min(1f, t / ataque) * u * Mathf.Sqrt(u);
            trilha[j] += valor * volume * envelope;
        }
    }

    static void Bumbo(float[] trilha, int inicio)
    {
        int duracao = Taxa / 6;
        float fase = 0f;
        for (int i = 0; i < duracao && inicio + i < trilha.Length; i++)
        {
            float t = i / (float)duracao;
            fase += Mathf.Lerp(110f, 40f, t) / Taxa;
            trilha[inicio + i] += Mathf.Sin(2f * Mathf.PI * fase) * 0.35f * (1f - t) * (1f - t);
        }
    }

    static void Caixa(float[] trilha, int inicio, System.Random sorteio)
    {
        int duracao = Taxa / 8;
        float ruido = 0f;
        for (int i = 0; i < duracao && inicio + i < trilha.Length; i++)
        {
            float t = i / (float)duracao;
            ruido += ((float)(sorteio.NextDouble() * 2.0 - 1.0) - ruido) * 0.5f; // ruído mais "grave"
            trilha[inicio + i] += ruido * 0.16f * (1f - t) * (1f - t);
        }
    }

    static void Chimbal(float[] trilha, int inicio, System.Random sorteio, float volume)
    {
        int duracao = Taxa / 30;
        for (int i = 0; i < duracao && inicio + i < trilha.Length; i++)
        {
            float t = i / (float)duracao;
            trilha[inicio + i] += (float)(sorteio.NextDouble() * 2.0 - 1.0) * volume * (1f - t);
        }
    }

    // Eco com retorno. Passa pela trilha DUAS vezes e guarda só a segunda volta:
    // assim o eco do fim da música já aparece no começo e o loop emenda sem "pulo".
    static void Eco(float[] trilha, int atraso, float retorno)
    {
        int n = trilha.Length;
        var linha = new float[atraso]; // o que foi tocado "atraso" amostras atrás
        var saida = new float[n];
        for (int i = 0; i < 2 * n; i++)
        {
            float valor = trilha[i % n] + linha[i % atraso] * retorno;
            linha[i % atraso] = valor;
            if (i >= n) saida[i - n] = valor;
        }
        System.Array.Copy(saida, trilha, n);
    }

    static int NotaDaEscala(int grau)
    {
        int oitava = Mathf.FloorToInt(grau / 7f);
        return oitava * 12 + EscalaMaior[grau - oitava * 7];
    }

    static float Frequencia(int notaMidi) => 440f * Mathf.Pow(2f, (notaMidi - 69) / 12f);
}
