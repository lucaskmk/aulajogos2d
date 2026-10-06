using UnityEngine;

// Música de fundo das fases e do mapa: "indie aconchegante" (no clima de Stardew Valley)
// com harmonia de BOSSA NOVA. Composição original, gerada por código:
//  - violão de nylon (som de corda dedilhada, feito com o algoritmo Karplus-Strong),
//    tocando acordes com sétima e nona na levada sincopada da bossa;
//  - baixo de bossa (fundamental e quinta, "um... e-dois");
//  - flauta suave fazendo a melodia, com eco;
//  - chocalho, aro de caixa marcando a clave e um surdo bem baixinho;
//  - progressões bem brasileiras: ii-V-I, dominante secundária, o "iv menor" (Fmaj7 -> Fm6)
//    e o acorde que desce meio tom (substituto do trítono).
// Cada fase tem outra progressão, outro tom e outra melodia.
public static class FabricaDeBossa
{
    const int Taxa = 16000;
    const float Batidas = 100f;      // bossa: calma, mas com balanço
    const int Compassos = 16;        // 16 compassos e repete

    // ------------------------------------------------------------------ acordes

    // Notas do acorde em semitons a partir da fundamental (vozes do violão, sem a fundamental:
    // quem toca a fundamental é o baixo, como na bossa).
    static readonly int[] Maj9 = { 4, 7, 11, 14 };
    static readonly int[] Maj7 = { 4, 7, 11, 12 };
    static readonly int[] SeisNove = { 4, 9, 14, 16 };
    static readonly int[] Menor9 = { 3, 7, 10, 14 };
    static readonly int[] Menor7 = { 3, 7, 10, 12 };
    static readonly int[] Menor6 = { 3, 7, 9, 12 };
    static readonly int[] Dom9 = { 4, 10, 14, 16 };
    static readonly int[] Dom7b9 = { 4, 10, 13, 16 };
    static readonly int[] Dom13 = { 4, 10, 14, 21 };
    static readonly int[] Dim7 = { 3, 6, 9, 12 };

    struct Acorde
    {
        public int raiz;   // semitons a partir do tom (0 = I)
        public int[] vozes;
        public int quinta; // quinta do baixo (7, ou 6 no diminuto)
        public Acorde(int raiz, int[] vozes, int quinta = 7) { this.raiz = raiz; this.vozes = vozes; this.quinta = quinta; }
    }

    // Cada progressão tem 8 acordes (1 por compasso); a música toca duas vezes, com melodia variando.
    static readonly Acorde[][] Progressoes =
    {
        // I - VI7 - ii - V (a "volta" mais brasileira que existe)
        new[] { new Acorde(0, Maj9), new Acorde(9, Dom7b9), new Acorde(2, Menor9), new Acorde(7, Dom13),
                new Acorde(4, Menor7), new Acorde(9, Dom7b9), new Acorde(2, Menor9), new Acorde(7, Dom9) },
        // IV - iv menor - iii - VI7 - ii - V - I (o "iv menor" dá aquela saudade)
        new[] { new Acorde(5, Maj7), new Acorde(5, Menor6), new Acorde(4, Menor7), new Acorde(9, Dom7b9),
                new Acorde(2, Menor9), new Acorde(7, Dom9), new Acorde(0, Maj9), new Acorde(0, SeisNove) },
        // I - II7 - ii - bII7 (desce meio tom de volta para o I)
        new[] { new Acorde(0, Maj7), new Acorde(0, Maj7), new Acorde(2, Dom9), new Acorde(2, Dom9),
                new Acorde(2, Menor7), new Acorde(2, Menor7), new Acorde(1, Dom9), new Acorde(1, Dom7b9) },
        // I - #i dim - ii - V (o diminuto de passagem, bem violão de roda)
        new[] { new Acorde(0, SeisNove), new Acorde(1, Dim7, 6), new Acorde(2, Menor9), new Acorde(7, Dom13),
                new Acorde(0, Maj9), new Acorde(4, Menor7), new Acorde(5, Maj7), new Acorde(7, Dom9) },
    };

    static readonly int[] Tons = { 0, 5, -3, 2, -5, 3, -2, 4, -4 };
    static readonly int[] Pentatonica = { 0, 2, 4, 7, 9 }; // a melodia "fofa" fica na pentatônica maior

    // ------------------------------------------------------------------ ritmo

    // Levada do violão em semicolcheias (16 por compasso, em 2 compassos): a batida da bossa.
    static readonly int[] LevadaDoViolao = { 0, 3, 6, 10, 12, 16, 19, 22, 24, 27, 30 };
    // Clave do aro de caixa (2 compassos).
    static readonly int[] Clave = { 0, 3, 6, 10, 13, 16, 19, 22, 26, 29 };

    public static AudioClip Compor(int fase)
    {
        var sorteio = new System.Random(4321 + fase * 131);
        Acorde[] progressao = Progressoes[Mathf.Abs(fase) % Progressoes.Length];
        int tom = Tons[Mathf.Abs(fase) % Tons.Length];

        int amostrasPorBatida = Mathf.RoundToInt(60f / Batidas * Taxa);
        int semicolcheia = amostrasPorBatida / 4;
        int porCompasso = amostrasPorBatida * 4;
        int total = porCompasso * Compassos;

        var violao = new float[total];
        var baixo = new float[total];
        var melodia = new float[total];
        var percussao = new float[total];

        for (int compasso = 0; compasso < Compassos; compasso++)
        {
            Acorde acorde = progressao[compasso % progressao.Length];
            int inicio = compasso * porCompasso;
            int raiz = 48 + tom + acorde.raiz; // fundamental do violão (Dó 3 = 48)

            // violão: acorde "dedilhado" (as cordas soam um pouquinho uma depois da outra)
            foreach (int passo in LevadaDoViolao)
            {
                int posicao = (compasso % 2) * 16;
                if (passo < posicao || passo >= posicao + 16) continue;
                int t = inicio + (passo - posicao) * semicolcheia;
                float forca = passo % 16 == 0 ? 1f : 0.75f;
                for (int v = 0; v < acorde.vozes.Length; v++)
                    Corda(violao, t + v * 60, Frequencia(raiz + 12 + acorde.vozes[v]), 0.09f * forca, 0.7f, sorteio);
            }

            // baixo de bossa: fundamental no 1, quinta no "e" do 2, fundamental no 3, quinta no "e" do 4
            int notaBaixo = 36 + tom + acorde.raiz;
            Baixo(baixo, inicio, amostrasPorBatida * 3 / 2, Frequencia(notaBaixo), 0.28f);
            Baixo(baixo, inicio + amostrasPorBatida * 3 / 2, amostrasPorBatida / 2, Frequencia(notaBaixo + acorde.quinta - 12), 0.22f);
            Baixo(baixo, inicio + amostrasPorBatida * 2, amostrasPorBatida * 3 / 2, Frequencia(notaBaixo), 0.26f);
            Baixo(baixo, inicio + amostrasPorBatida * 7 / 2, amostrasPorBatida / 2, Frequencia(notaBaixo + acorde.quinta - 12), 0.2f);

            // percussão
            for (int s = 0; s < 16; s++)
                Chocalho(percussao, inicio + s * semicolcheia, sorteio, s % 4 == 2 ? 0.035f : 0.018f);
            foreach (int passo in Clave)
            {
                int posicao = (compasso % 2) * 16;
                if (passo >= posicao && passo < posicao + 16) Aro(percussao, inicio + (passo - posicao) * semicolcheia, sorteio);
            }
            Surdo(percussao, inicio);
            Surdo(percussao, inicio + amostrasPorBatida * 2);
        }

        ComporMelodia(melodia, progressao, tom, semicolcheia, porCompasso, sorteio);
        Eco(melodia, amostrasPorBatida * 3 / 4, 0.3f);

        var dados = new float[total];
        float maximo = 0.0001f;
        for (int i = 0; i < total; i++)
        {
            dados[i] = violao[i] + baixo[i] + melodia[i] + percussao[i];
            maximo = Mathf.Max(maximo, Mathf.Abs(dados[i]));
        }
        for (int i = 0; i < total; i++) dados[i] *= 0.75f / maximo;

        var clipe = AudioClip.Create("musica_bossa_" + fase, total, 1, Taxa, false);
        clipe.SetData(dados, 0);
        return clipe;
    }

    // Melodia de flauta: frases de 2 compassos com respiro, notas da pentatônica perto do acorde,
    // ritmo sincopado (antecipa a nota do compasso seguinte, bem bossa). A segunda volta varia a primeira.
    static readonly int[][] Ritmos =
    {
        new[] { 0, 3, 6, 8, 10 },        // em semicolcheias, dentro de um compasso
        new[] { 2, 4, 6, 10, 14 },
        new[] { 0, 6, 10, 12 },
        new[] { 3, 6, 8, 12, 14 },
        new[] { 0, 2, 4, 10 },
    };

    static void ComporMelodia(float[] trilha, Acorde[] progressao, int tom, int semicolcheia, int porCompasso, System.Random sorteio)
    {
        var ritmos = new int[8];
        for (int i = 0; i < ritmos.Length; i++) ritmos[i] = sorteio.Next(Ritmos.Length);
        int grau = 7; // índice na pentatônica (contando oitavas): começa no meio da região
        for (int compasso = 0; compasso < Compassos; compasso++)
        {
            bool respiro = compasso % 4 == 3;            // a cada 4 compassos, uma pausa para respirar
            bool segundaVolta = compasso >= Compassos / 2;
            int[] ritmo = Ritmos[ritmos[compasso % 8]];
            Acorde acorde = progressao[compasso % progressao.Length];
            for (int n = 0; n < ritmo.Length; n++)
            {
                if (respiro && n >= 2) break;
                // passinhos na escala; na segunda volta, um pouco mais aguda
                grau = Mathf.Clamp(grau + sorteio.Next(-2, 3), 4, 12);
                int nota = 72 + tom + NotaDaPentatonica(grau) + (segundaVolta ? 0 : -12);
                // nota que "briga" com o acorde? desce até uma do acorde
                if (!SoaBem(nota - (60 + tom + acorde.raiz), acorde)) nota -= 1;
                int inicio = compasso * porCompasso + ritmo[n] * semicolcheia;
                int duracao = (n + 1 < ritmo.Length ? ritmo[n + 1] - ritmo[n] : 16 - ritmo[n]) * semicolcheia;
                if (respiro && n == 1) duracao = porCompasso / 2;
                Flauta(trilha, inicio, duracao, Frequencia(nota), 0.11f);
            }
        }
    }

    static bool SoaBem(int intervalo, Acorde acorde)
    {
        int i = ((intervalo % 12) + 12) % 12;
        if (i == 0 || i == 7) return true;
        foreach (int voz in acorde.vozes) if (voz % 12 == i) return true;
        return i == 2 || i == 9; // nona e sexta também soam bem na bossa
    }

    static int NotaDaPentatonica(int grau) => (grau / 5) * 12 + Pentatonica[grau % 5];

    // ------------------------------------------------------------------ instrumentos

    // Corda dedilhada (Karplus-Strong): um "barulhinho" que vai sendo suavizado em volta de um ciclo
    // do tamanho da nota. Soa parecido com violão de nylon.
    static void Corda(float[] trilha, int inicio, float freq, float volume, float duracaoEmSegundos, System.Random sorteio)
    {
        int periodo = Mathf.Max(2, Mathf.RoundToInt(Taxa / freq));
        var ciclo = new float[periodo];
        for (int i = 0; i < periodo; i++) ciclo[i] = (float)(sorteio.NextDouble() * 2.0 - 1.0);
        int duracao = Mathf.RoundToInt(duracaoEmSegundos * Taxa);
        float anterior = 0f;
        for (int i = 0; i < duracao; i++)
        {
            int j = inicio + i;
            if (j >= trilha.Length) j -= trilha.Length; // o fim emenda no começo (loop)
            int k = i % periodo;
            float valor = ciclo[k];
            ciclo[k] = 0.996f * 0.5f * (valor + ciclo[(k + 1) % periodo]);
            anterior += (valor - anterior) * 0.5f; // um pouco mais "abafado", como nylon
            trilha[j] += anterior * volume * (1f - (float)i / duracao);
        }
    }

    static void Baixo(float[] trilha, int inicio, int duracao, float freq, float volume)
    {
        float fase = 0f;
        for (int i = 0; i < duracao; i++)
        {
            int j = (inicio + i) % trilha.Length;
            float t = i / (float)duracao;
            fase += freq / Taxa;
            float valor = Mathf.Sin(2f * Mathf.PI * fase) + 0.25f * Mathf.Sin(4f * Mathf.PI * fase);
            trilha[j] += valor * volume * Mathf.Min(1f, t * 60f) * Mathf.Pow(1f - t, 1.2f);
        }
    }

    // Flauta: senoide com um pouquinho de harmônico, ataque macio e vibrato que entra devagar.
    static void Flauta(float[] trilha, int inicio, int duracao, float freq, float volume)
    {
        float fase = 0f;
        for (int i = 0; i < duracao; i++)
        {
            int j = (inicio + i) % trilha.Length;
            float t = i / (float)duracao, segundos = i / (float)Taxa;
            float vibrato = 1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 5f * segundos) * Mathf.Clamp01(segundos * 3f);
            fase += freq * vibrato / Taxa;
            float valor = Mathf.Sin(2f * Mathf.PI * fase) + 0.15f * Mathf.Sin(4f * Mathf.PI * fase);
            float envelope = Mathf.Min(1f, segundos / 0.04f) * Mathf.Min(1f, (1f - t) * 6f);
            trilha[j] += valor * volume * envelope;
        }
    }

    static void Chocalho(float[] trilha, int inicio, System.Random sorteio, float volume)
    {
        int duracao = Taxa / 25;
        float anterior = 0f;
        for (int i = 0; i < duracao; i++)
        {
            int j = (inicio + i) % trilha.Length;
            float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
            float agudo = ruido - anterior; // tira o grave: fica um "tchh"
            anterior = ruido;
            float t = i / (float)duracao;
            trilha[j] += agudo * volume * Mathf.Min(1f, t * 8f) * (1f - t);
        }
    }

    // Aro de caixa: um "toc" curtinho.
    static void Aro(float[] trilha, int inicio, System.Random sorteio)
    {
        int duracao = Taxa / 40;
        for (int i = 0; i < duracao; i++)
        {
            int j = (inicio + i) % trilha.Length;
            float t = i / (float)duracao;
            float valor = Mathf.Sin(2f * Mathf.PI * 1700f * i / Taxa) * 0.6f + (float)(sorteio.NextDouble() * 2.0 - 1.0) * 0.4f;
            trilha[j] += valor * 0.07f * (1f - t) * (1f - t);
        }
    }

    // Surdo bem baixinho, só para dar o "chão".
    static void Surdo(float[] trilha, int inicio)
    {
        int duracao = Taxa / 5;
        float fase = 0f;
        for (int i = 0; i < duracao; i++)
        {
            int j = (inicio + i) % trilha.Length;
            float t = i / (float)duracao;
            fase += Mathf.Lerp(90f, 50f, t) / Taxa;
            trilha[j] += Mathf.Sin(2f * Mathf.PI * fase) * 0.18f * (1f - t) * (1f - t);
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

    static float Frequencia(int notaMidi) => 440f * Mathf.Pow(2f, (notaMidi - 69) / 12f);
}
