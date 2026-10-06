using UnityEngine;

// Música de fundo no clima de abertura de anime "dark" (tipo as aberturas de Re:Zero).
// Composição ORIGINAL, gerada por código:
//  - tom menor, com a escala menor HARMÔNICA (a sétima sobe: aquele clima dramático/misterioso);
//  - cordas (várias "serras" levemente desafinadas = som de orquestra/sintetizador);
//  - arpejo rápido de piano/sintetizador em semicolcheias;
//  - baixo pulsando em colcheias;
//  - melodia de "violino" que cresce no refrão (segunda metade);
//  - bateria de rock (bumbo, caixa, chimbal, prato no começo de cada parte e virada no fim).
// Três jeitos de tocar:
//  - Fase:  refrão o tempo todo, em loop, 150 bpm;
//  - Mapa:  calma e misteriosa, sem bateria;
//  - Chefe: mais rápida e pesada (luta contra a Baleia Branca).
public static class FabricaDeEpico
{
    public enum Jeito { Fase, Mapa, Chefe }

    const int Taxa = 16000;
    const int Compassos = 16; // 8 de "verso" + 8 de "refrão", e repete

    // Escala menor harmônica (semitons): lá, si, dó, ré, mi, fá, sol#.
    static readonly int[] Escala = { 0, 2, 3, 5, 7, 8, 11 };

    // Acordes em graus da escala (0 = i, 5 = VI, 6 = VII...). Um acorde por compasso.
    // O V (grau 4) é MAIOR por causa da menor harmônica: é ele que dá a "tensão".
    static readonly int[][] Progressoes =
    {
        new[] { 0, 5, 2, 6, 0, 5, 3, 4 },   // i - VI - III - VII | i - VI - iv - V
        new[] { 0, 6, 5, 4, 0, 6, 5, 4 },   // i - VII - VI - V (a "andaluza")
        new[] { 5, 6, 0, 0, 3, 4, 0, 4 },   // VI - VII - i | iv - V - i - V
        new[] { 0, 3, 6, 2, 5, 3, 4, 4 },   // i - iv - VII - III | VI - iv - V - V
    };

    static readonly int[] Tons = { 0, 5, -2, 3, -4, 2, -5, 7, -1 };

    public static AudioClip Compor(int semente, Jeito jeito)
    {
        var sorteio = new System.Random(777 + semente * 61);
        int[] progressao = Progressoes[Mathf.Abs(semente) % Progressoes.Length];
        int tom = 57 + Tons[Mathf.Abs(semente) % Tons.Length]; // lá 3 = 57
        float bpm = jeito == Jeito.Chefe ? 168f : jeito == Jeito.Mapa ? 112f : 150f;

        int batida = Mathf.RoundToInt(60f / bpm * Taxa);
        int semi = batida / 4, porCompasso = batida * 4, total = porCompasso * Compassos;

        var cordas = new float[total];
        var arpejo = new float[total];
        var baixo = new float[total];
        var melodia = new float[total];
        var bateria = new float[total];

        for (int c = 0; c < Compassos; c++)
        {
            int grau = progressao[c % progressao.Length];
            int inicio = c * porCompasso;
            bool refrao = jeito != Jeito.Mapa || c >= Compassos / 2; // nas fases: refrão o tempo todo, em loop
            int[] acorde = { Nota(tom, grau), Nota(tom, grau + 2), Nota(tom, grau + 4) };

            // cordas: o acorde inteiro, entrando devagar (no refrão, mais forte e uma oitava a mais)
            foreach (int n in acorde) Cordas(cordas, inicio, porCompasso, Frequencia(n), refrao ? 0.05f : 0.035f);
            if (refrao) Cordas(cordas, inicio, porCompasso, Frequencia(acorde[0] + 12), 0.03f);

            // arpejo: sobe e desce pelas notas do acorde em semicolcheias
            int[] subida = { 0, 1, 2, 3, 4, 3, 2, 1 };
            for (int s = 0; s < 16; s++)
            {
                int k = subida[s % subida.Length];
                int nota = acorde[k % 3] + 12 * (k / 3) + 12;
                Pluck(arpejo, inicio + s * semi, semi * 2, Frequencia(nota), jeito == Jeito.Mapa ? 0.05f : 0.06f);
            }

            // baixo: colcheias na fundamental (oitava pulando no fim do compasso)
            if (jeito != Jeito.Mapa)
                for (int e = 0; e < 8; e++)
                {
                    int nota = acorde[0] - 24 + (e == 7 ? 12 : 0);
                    Baixo(baixo, inicio + e * batida / 2, batida / 2, Frequencia(nota), 0.22f);
                }
            else Baixo(baixo, inicio, porCompasso, Frequencia(acorde[0] - 24), 0.2f);

            if (jeito != Jeito.Mapa) Bateria(bateria, inicio, batida, c, jeito == Jeito.Chefe, sorteio);
        }

        ComporMelodia(melodia, progressao, tom, semi, porCompasso, sorteio, jeito);
        Eco(melodia, batida * 3 / 4, 0.28f);
        Eco(arpejo, batida * 3 / 2, 0.2f);

        // mistura, um filtro leve para tirar o "chiado" das serras e uma saturação suave (dá peso)
        var dados = new float[total];
        float filtrado = 0f, maximo = 0.0001f;
        for (int i = 0; i < total; i++)
        {
            float amostra = cordas[i] + arpejo[i] + baixo[i] + melodia[i] + bateria[i];
            filtrado += (amostra - filtrado) * 0.55f;
            dados[i] = (float)System.Math.Tanh(filtrado * 1.6f);
            maximo = Mathf.Max(maximo, Mathf.Abs(dados[i]));
        }
        for (int i = 0; i < total; i++) dados[i] *= 0.8f / maximo;

        var clipe = AudioClip.Create("musica_epica_" + semente + "_" + jeito, total, 1, Taxa, false);
        clipe.SetData(dados, 0);
        return clipe;
    }

    // Melodia: um "motivo" de 2 compassos que se repete acompanhando os acordes.
    // No verso ela é mais espaçada; no refrão sobe uma oitava e fica mais cheia.
    static void ComporMelodia(float[] trilha, int[] progressao, int tom, int semi, int porCompasso, System.Random sorteio, Jeito jeito)
    {
        // ritmo do motivo (em semicolcheias, dentro de 2 compassos) e o "desenho" das notas (graus relativos)
        int[][] ritmos =
        {
            new[] { 0, 6, 8, 12, 16, 22, 24 },
            new[] { 0, 4, 6, 8, 14, 16, 20, 24, 28 },
            new[] { 0, 3, 6, 12, 16, 19, 22, 28 },
        };
        int[] ritmo = ritmos[sorteio.Next(ritmos.Length)];
        var desenho = new int[ritmo.Length];
        int passo = 0;
        for (int i = 0; i < desenho.Length; i++)
        {
            passo += sorteio.Next(-2, 3);
            if (i == 0) passo = 0;
            desenho[i] = Mathf.Clamp(passo, -3, 5);
        }

        for (int c = 0; c < Compassos; c += 2)
        {
            bool refrao = jeito != Jeito.Mapa || c >= Compassos / 2;
            if (!refrao && c % 4 == 2) continue; // no mapa, a melodia "responde" e descansa
            int grau = progressao[c % progressao.Length];
            for (int i = 0; i < ritmo.Length; i++)
            {
                int inicio = c * porCompasso + ritmo[i] * semi;
                int fim = i + 1 < ritmo.Length ? ritmo[i + 1] : 32;
                int duracao = (fim - ritmo[i]) * semi;
                int nota = Nota(tom, grau + desenho[i]) + (refrao ? 12 : 0);
                Violino(trilha, inicio, duracao, Frequencia(nota), refrao ? 0.11f : 0.08f);
            }
        }
    }

    // ------------------------------------------------------------------ instrumentos

    // Cordas: 3 "serras" um pouquinho desafinadas entre si (som cheio), entrando devagar.
    static void Cordas(float[] trilha, int inicio, int duracao, float freq, float volume)
    {
        float f1 = 0f, f2 = 0.33f, f3 = 0.66f;
        for (int i = 0; i < duracao; i++)
        {
            int j = (inicio + i) % trilha.Length;
            f1 += freq / Taxa; f2 += freq * 1.006f / Taxa; f3 += freq * 0.994f / Taxa;
            float serra = (f1 % 1f) + (f2 % 1f) + (f3 % 1f) - 1.5f;
            float t = i / (float)duracao;
            float envelope = Mathf.Min(1f, t * 5f) * Mathf.Min(1f, (1f - t) * 8f);
            trilha[j] += serra * volume * envelope;
        }
    }

    // Notinha curta e brilhante (piano/sintetizador do arpejo).
    static void Pluck(float[] trilha, int inicio, int duracao, float freq, float volume)
    {
        float fase = 0f;
        for (int i = 0; i < duracao; i++)
        {
            int j = (inicio + i) % trilha.Length;
            fase += freq / Taxa;
            float t = i / (float)duracao;
            float onda = Mathf.Sin(2f * Mathf.PI * fase) * 0.7f + ((fase % 1f) < 0.5f ? 0.3f : -0.3f);
            trilha[j] += onda * volume * (1f - t) * (1f - t);
        }
    }

    static void Baixo(float[] trilha, int inicio, int duracao, float freq, float volume)
    {
        float fase = 0f;
        for (int i = 0; i < duracao; i++)
        {
            int j = (inicio + i) % trilha.Length;
            fase += freq / Taxa;
            float t = i / (float)duracao;
            float onda = (fase % 1f) * 2f - 1f; // serra: baixo "rasgado"
            trilha[j] += onda * volume * Mathf.Min(1f, t * 40f) * (1f - t * 0.6f);
        }
    }

    // "Violino": serra suave com vibrato que entra depois do ataque.
    static void Violino(float[] trilha, int inicio, int duracao, float freq, float volume)
    {
        float fase = 0f, filtrado = 0f;
        for (int i = 0; i < duracao; i++)
        {
            int j = (inicio + i) % trilha.Length;
            float s = i / (float)Taxa, t = i / (float)duracao;
            float vibrato = 1f + 0.008f * Mathf.Sin(2f * Mathf.PI * 6f * s) * Mathf.Clamp01(s * 4f);
            fase += freq * vibrato / Taxa;
            float onda = (fase % 1f) * 2f - 1f;
            filtrado += (onda - filtrado) * 0.25f; // tira a aspereza
            float envelope = Mathf.Min(1f, s / 0.03f) * Mathf.Min(1f, (1f - t) * 5f);
            trilha[j] += filtrado * volume * envelope;
        }
    }

    // Bateria de rock. Prato no começo do verso e do refrão; virada de caixa no último compasso de cada parte.
    static void Bateria(float[] trilha, int inicio, int batida, int compasso, bool pesada, System.Random sorteio)
    {
        bool virada = compasso % 4 == 3;
        if (compasso % 4 == 0) Prato(trilha, inicio, sorteio);
        for (int b = 0; b < 4; b++)
        {
            int t = inicio + b * batida;
            if (b == 0 || b == 2 || pesada) Bumbo(trilha, t);
            if (b == 2) Bumbo(trilha, t + batida / 2);
            if (b == 1 || b == 3) Caixa(trilha, t, sorteio, 0.2f);
            Chimbal(trilha, t, sorteio, 0.03f);
            Chimbal(trilha, t + batida / 2, sorteio, 0.02f);
        }
        if (virada)
            for (int s = 0; s < 4; s++) Caixa(trilha, inicio + 3 * batida + s * batida / 4, sorteio, 0.12f + s * 0.03f);
    }

    static void Bumbo(float[] trilha, int inicio)
    {
        int duracao = Taxa / 6;
        float fase = 0f;
        for (int i = 0; i < duracao; i++)
        {
            int j = (inicio + i) % trilha.Length;
            float t = i / (float)duracao;
            fase += Mathf.Lerp(140f, 45f, Mathf.Sqrt(t)) / Taxa;
            trilha[j] += Mathf.Sin(2f * Mathf.PI * fase) * 0.45f * (1f - t) * (1f - t);
        }
    }

    static void Caixa(float[] trilha, int inicio, System.Random sorteio, float volume)
    {
        int duracao = Taxa / 7;
        float fase = 0f;
        for (int i = 0; i < duracao; i++)
        {
            int j = (inicio + i) % trilha.Length;
            float t = i / (float)duracao;
            fase += 190f / Taxa;
            float valor = (float)(sorteio.NextDouble() * 2.0 - 1.0) * 0.8f + Mathf.Sin(2f * Mathf.PI * fase) * 0.4f;
            trilha[j] += valor * volume * (1f - t) * (1f - t) * (1f - t);
        }
    }

    static void Chimbal(float[] trilha, int inicio, System.Random sorteio, float volume)
    {
        int duracao = Taxa / 30;
        float anterior = 0f;
        for (int i = 0; i < duracao; i++)
        {
            int j = (inicio + i) % trilha.Length;
            float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
            float agudo = ruido - anterior;
            anterior = ruido;
            trilha[j] += agudo * volume * (1f - i / (float)duracao);
        }
    }

    static void Prato(float[] trilha, int inicio, System.Random sorteio)
    {
        int duracao = Taxa * 3 / 2;
        float anterior = 0f;
        for (int i = 0; i < duracao; i++)
        {
            int j = (inicio + i) % trilha.Length;
            float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
            float agudo = ruido - anterior;
            anterior = ruido;
            float t = i / (float)duracao;
            trilha[j] += agudo * 0.08f * (1f - t) * (1f - t);
        }
    }

    static void Eco(float[] trilha, int atraso, float retorno)
    {
        int n = trilha.Length;
        var linha = new float[atraso];
        var saida = new float[n];
        for (int i = 0; i < 2 * n; i++)
        {
            float valor = trilha[i % n] + linha[i % atraso] * retorno;
            linha[i % atraso] = valor;
            if (i >= n) saida[i - n] = valor;
        }
        System.Array.Copy(saida, trilha, n);
    }

    // Nota MIDI de um grau da escala menor harmônica (graus podem passar de 7: sobe oitava).
    static int Nota(int tom, int grau)
    {
        int oitava = Mathf.FloorToInt(grau / 7f);
        return tom + oitava * 12 + Escala[grau - oitava * 7];
    }

    static float Frequencia(int notaMidi) => 440f * Mathf.Pow(2f, (notaMidi - 69) / 12f);
}
