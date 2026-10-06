using UnityEngine;

// Música de fundo no clima de abertura de anime "dark" (piano + orquestra + banda).
// Composição ORIGINAL, gerada por código, em ré menor (menor harmônica: o dó# dá a tensão).
// Estrutura (16 compassos, repete em loop):
//  - Intro  (4): acordes "martelados" no piano em ritmo sincopado, com o baixo em oitavas junto;
//  - Refrão (8): melodia de violino + piano, piano fazendo arpejo em colcheias, cordas e bateria;
//  - Ponte  (4): notas longas, bateria em meio-tempo, uma escala descendo no piano e virada de caixa
//                que joga de volta para a intro.
// Os instrumentos são sintetizados: piano com harmônicos que somem em tempos diferentes (soa de verdade),
// cordas com várias vozes levemente desafinadas, violino com vibrato e um reverb de sala por cima.
// Três jeitos de tocar:
//  - Fase:  152 bpm, completo;
//  - Mapa:  98 bpm, só piano e cordas (calmo e misterioso);
//  - Chefe: 168 bpm, bumbo em toda batida e baixo mais pesado.
public static class FabricaDeEpico
{
    public enum Jeito { Fase, Mapa, Chefe }

    const int Taxa = 22050;
    const int Compassos = 16;
    const int Re4 = 62; // a melodia está escrita em semitons a partir do ré 4

    // Acorde de cada compasso: fundamental em semitons a partir de ré, e se é menor.
    // Intro: Dm Bb Gm A | Refrão: Dm Bb F C Gm Bb A Dm | Ponte: Bb C A A
    static readonly int[] Raizes = { 0, -4, 5, 7, 0, -4, 3, -2, 5, -4, 7, 0, -4, -2, 7, 7 };
    static readonly bool[] Menores = { true, false, true, false, true, false, false, false, true, false, false, true, false, false, false, false };

    // Melodia do refrão: (compasso do refrão, início em semicolcheias, duração em semicolcheias, nota a partir do ré 4)
    static readonly int[,] Refrao =
    {
        { 0, 0, 4, 12 }, { 0, 4, 2, 10 }, { 0, 6, 2, 12 }, { 0, 8, 6, 15 }, { 0, 14, 2, 14 },
        { 1, 0, 6, 12 }, { 1, 6, 2, 10 }, { 1, 8, 4, 8 },  { 1, 12, 2, 10 }, { 1, 14, 2, 12 },
        { 2, 0, 8, 10 }, { 2, 8, 4, 7 },  { 2, 12, 2, 3 }, { 2, 14, 2, 7 },
        { 3, 0, 12, 5 }, { 3, 12, 2, 7 }, { 3, 14, 2, 8 },
        { 4, 0, 4, 12 }, { 4, 4, 2, 8 },  { 4, 6, 2, 12 }, { 4, 8, 6, 17 }, { 4, 14, 2, 15 },
        { 5, 0, 6, 15 }, { 5, 6, 2, 12 }, { 5, 8, 4, 8 },  { 5, 12, 2, 12 }, { 5, 14, 2, 15 },
        { 6, 0, 8, 14 }, { 6, 8, 4, 11 }, { 6, 12, 2, 14 }, { 6, 14, 2, 17 },
        { 7, 0, 4, 15 }, { 7, 4, 2, 14 }, { 7, 6, 2, 11 }, { 7, 8, 8, 12 },
    };

    // Melodia da ponte (notas longas, crescendo)
    static readonly int[,] Ponte = { { 0, 0, 16, 12 }, { 1, 0, 16, 14 }, { 2, 0, 8, 14 }, { 2, 8, 8, 11 } };

    // Escala descendo no último compasso (lá 5 até sol 3, ré menor harmônica)
    static readonly int[] Descida = { 19, 17, 15, 14, 12, 11, 8, 7, 5, 3, 2, 0, -1, -4, -5, -7 };

    // Ritmo dos acordes martelados da intro: (início, duração) em semicolcheias
    static readonly int[,] Martelo = { { 0, 2 }, { 2, 2 }, { 6, 2 }, { 8, 2 }, { 10, 4 }, { 14, 2 } };
    static readonly int[,] MarteloFinal = { { 0, 2 }, { 2, 2 }, { 6, 2 }, { 8, 8 } };

    // Transposição por fase (pequena, para o piano não embolar no grave nem ficar estridente no agudo)
    static readonly int[] Tons = { 0, 2, -2, 3, -3, 1, -1, 4, -4 };

    public static AudioClip Compor(int semente, Jeito jeito)
    {
        var sorteio = new System.Random(777 + semente * 61);
        bool mapa = jeito == Jeito.Mapa, chefe = jeito == Jeito.Chefe;
        int tom = chefe ? -1 : mapa ? 0 : Tons[Mathf.Abs(semente) % Tons.Length]; // chefe: dó# menor, mais escuro
        float bpm = chefe ? 168f : mapa ? 98f : 152f;

        int batida = Mathf.RoundToInt(60f / bpm * Taxa);
        int semi = batida / 4, porCompasso = batida * 4, total = porCompasso * Compassos;

        var piano = new float[total];
        var cordas = new float[total];
        var violino = new float[total];
        var baixo = new float[total];
        var bateria = new float[total];

        for (int c = 0; c < Compassos; c++)
        {
            int inicio = c * porCompasso;
            int[] acorde = Voicing(Re4 + tom + Raizes[c], Menores[c], Re4 + tom - 2);
            int grave = Re4 + tom - 26 + Mod12(Raizes[c] + 2); // fundamental entre dó 2 e si 2
            bool intro = c < 4, ponte = c >= 12;

            // cordas: o acorde segurado o compasso inteiro (crescendo na ponte)
            float vc = mapa ? 0.05f : intro ? 0.035f : ponte ? 0.05f + (c - 12) * 0.008f : 0.045f;
            foreach (int n in acorde) Cordas(cordas, inicio, porCompasso, Freq(n), vc);
            Cordas(cordas, inicio, porCompasso, Freq(grave + 12), vc * 0.8f);

            if (intro && !mapa)
            {
                // acordes martelados: piano (nota de cima dobrada uma oitava acima) e baixo em oitavas no mesmo ritmo
                int[,] ritmo = c == 3 ? MarteloFinal : Martelo;
                for (int i = 0; i < ritmo.GetLength(0); i++)
                {
                    int t = inicio + ritmo[i, 0] * semi, d = ritmo[i, 1] * semi;
                    foreach (int n in acorde) Piano(piano, t, d, Freq(n), 0.16f);
                    Piano(piano, t, d, Freq(acorde[2] + 12), 0.12f);
                    Piano(piano, t, d, Freq(grave), 0.2f);
                    Piano(piano, t, d, Freq(grave + 12), 0.16f);
                    Baixo(baixo, t, d, Freq(grave), 0.16f);
                    Bumbo(bateria, t, 0.5f);
                }
                if (c == 0 || c == 2) Prato(bateria, inicio, sorteio, 0.09f);
                Caixa(bateria, inicio + 10 * semi, sorteio, 0.22f);
            }
            else
            {
                // piano: arpejo em colcheias (sobe e desce pelo acorde) + fundamental em oitavas
                int[] subida = { 0, 1, 2, 3, 2, 1, 2, 3 };
                for (int e = 0; e < 8; e++)
                {
                    int k = subida[e];
                    int nota = k == 3 ? acorde[0] + 12 : acorde[k];
                    float v = (mapa ? 0.1f : 0.075f) * (e % 2 == 0 ? 1f : 0.8f);
                    Piano(piano, inicio + e * 2 * semi, 4 * semi, Freq(nota), v);
                }
                Piano(piano, inicio, porCompasso / 2, Freq(grave), mapa ? 0.2f : 0.16f);
                Piano(piano, inicio, porCompasso / 2, Freq(grave + 12), mapa ? 0.14f : 0.12f);
                if (mapa) Piano(piano, inicio + porCompasso / 2, porCompasso / 2, Freq(grave + 7), 0.1f);

                // baixo: colcheias pulsando (no mapa, uma nota longa)
                if (mapa) Baixo(baixo, inicio, porCompasso, Freq(grave), 0.12f);
                else
                    for (int e = 0; e < 8; e++)
                    {
                        if (ponte && e % 2 == 1 && c < Compassos - 1) continue; // meio-tempo na ponte
                        int oitava = chefe && e % 2 == 1 ? 12 : 0;
                        Baixo(baixo, inicio + e * 2 * semi, 2 * semi, Freq(grave + oitava), 0.18f);
                    }

                if (!mapa) Bateria(bateria, inicio, batida, c, chefe, sorteio);
            }

            // último compasso: escala descendo no piano, levando de volta para a intro
            if (c == Compassos - 1)
                for (int s = 0; s < 16; s++)
                    Piano(piano, inicio + s * semi, 2 * semi, Freq(Re4 + tom + Descida[s]), 0.06f + s * 0.004f);
        }

        // melodia: violino + piano dobrando (no mapa, só piano)
        for (int i = 0; i < Refrao.GetLength(0); i++)
            Melodia(violino, piano, 4 + Refrao[i, 0], Refrao[i, 1], Refrao[i, 2], Re4 + tom + Refrao[i, 3], semi, porCompasso, mapa);
        for (int i = 0; i < Ponte.GetLength(0); i++)
            Melodia(violino, piano, 12 + Ponte[i, 0], Ponte[i, 1], Ponte[i, 2], Re4 + tom + Ponte[i, 3], semi, porCompasso, mapa);

        // mistura: piano, cordas e violino passam pelo reverb; baixo e bateria ficam "secos" (mais punch)
        var sala = new float[total];
        for (int i = 0; i < total; i++) sala[i] = piano[i] + cordas[i] + violino[i];
        float[] reverb = Reverb(sala, mapa ? 0.86f : 0.8f);
        float molhado = mapa ? 0.4f : 0.28f;

        var dados = new float[total];
        float maximo = 0.0001f;
        for (int i = 0; i < total; i++)
        {
            float amostra = sala[i] + reverb[i] * molhado + baixo[i] + bateria[i];
            dados[i] = (float)System.Math.Tanh(amostra * 1.3f);
            maximo = Mathf.Max(maximo, Mathf.Abs(dados[i]));
        }
        for (int i = 0; i < total; i++) dados[i] *= 0.85f / maximo;

        var clipe = AudioClip.Create("musica_epica_" + semente + "_" + jeito, total, 1, Taxa, false);
        clipe.SetData(dados, 0);
        return clipe;
    }

    static void Melodia(float[] violino, float[] piano, int compasso, int inicio16, int dur16, int nota, int semi, int porCompasso, bool mapa)
    {
        int t = compasso * porCompasso + inicio16 * semi, d = dur16 * semi;
        if (mapa)
        {
            Piano(piano, t, d, Freq(nota + 12), 0.13f);
            return;
        }
        Violino(violino, t, d, Freq(nota + 12), 0.1f);
        Violino(violino, t, d, Freq(nota), 0.07f);
        Piano(piano, t, d, Freq(nota + 12), 0.1f);
    }

    // As três notas do acorde, todas entre "piso" e uma oitava acima (posição fechada).
    static int[] Voicing(int raiz, bool menor, int piso)
    {
        int[] intervalos = { 0, menor ? 3 : 4, 7 };
        var notas = new int[3];
        for (int i = 0; i < 3; i++)
        {
            int n = raiz + intervalos[i];
            while (n < piso) n += 12;
            while (n >= piso + 12) n -= 12;
            notas[i] = n;
        }
        System.Array.Sort(notas);
        return notas;
    }

    static int Mod12(int x) => ((x % 12) + 12) % 12;

    // ------------------------------------------------------------------ instrumentos

    // Piano: soma de harmônicos (um pouco "esticados", como numa corda de verdade). Os harmônicos agudos somem
    // antes dos graves, as notas agudas somem antes das graves, e cada nota tem duas "cordas" levemente
    // desafinadas (o batimento dá o som de piano). Ao soltar a tecla, a nota abafa rapidinho.
    static void Piano(float[] trilha, int inicio, int duracao, float freq, float volume)
    {
        int soltar = Taxa / 12;
        int fim = Mathf.Min(duracao + soltar, Taxa * 3);
        float brilho = Mathf.Clamp01(1.3f - freq / 1400f);
        float abafa = Mathf.Exp(-1f / (soltar * 0.25f));
        for (int h = 1; h <= 7; h++)
        {
            float fh = h * freq * Mathf.Sqrt(1f + 0.0004f * h * h);
            if (fh > Taxa * 0.45f) break;
            float amp = volume / Mathf.Pow(h, 1.1f) * (h == 1 ? 1f : brilho);
            float decai = Mathf.Exp(-(0.8f + 0.9f * h + freq / 350f) / Taxa);
            int cordas = h <= 3 ? 2 : 1; // os harmônicos de cima com uma corda só (gera mais rápido, quase não muda o som)
            for (int corda = 0; corda < cordas; corda++)
            {
                double w = 2.0 * System.Math.PI * fh * (cordas == 1 ? 1.0 : corda == 0 ? 0.9993 : 1.0007) / Taxa;
                float cw = (float)System.Math.Cos(w), sw = (float)System.Math.Sin(w);
                float s = 0f, co = 1f; // oscilador por rotação: rápido, sem chamar seno a cada amostra
                float env = amp / cordas;
                for (int i = 0; i < fim; i++)
                {
                    float ns = s * cw + co * sw;
                    co = co * cw - s * sw;
                    s = ns;
                    env *= i < duracao ? decai : abafa;
                    trilha[(inicio + i) % trilha.Length] += s * env * (i < 40 ? i / 40f : 1f);
                }
            }
        }
        // o "toque" do martelo: um estalinho curto
        float anterior = 0f;
        uint ruido = (uint)inicio * 2654435761u + 1u;
        for (int i = 0; i < 120; i++)
        {
            ruido ^= ruido << 13; ruido ^= ruido >> 17; ruido ^= ruido << 5;
            float r = (ruido / (float)uint.MaxValue) * 2f - 1f;
            anterior += (r - anterior) * 0.3f;
            trilha[(inicio + i) % trilha.Length] += anterior * volume * 0.25f * (1f - i / 120f);
        }
    }

    // Cordas: 5 "serras" desafinadas entre si, filtradas (som macio de orquestra), entrando devagar.
    static readonly float[] Desafino = { 1f, 1.004f, 0.996f, 1.0075f, 0.9925f };

    static void Cordas(float[] trilha, int inicio, int duracao, float freq, float volume)
    {
        var fases = new float[Desafino.Length];
        for (int v = 0; v < fases.Length; v++) fases[v] = v * 0.21f;
        float f1 = 0f, f2 = 0f;
        for (int i = 0; i < duracao; i++)
        {
            float serra = 0f;
            for (int v = 0; v < fases.Length; v++)
            {
                fases[v] += freq * Desafino[v] / Taxa;
                if (fases[v] >= 1f) fases[v] -= 1f;
                serra += fases[v];
            }
            serra = serra / fases.Length * 2f - 1f;
            f1 += (serra - f1) * 0.12f;
            f2 += (f1 - f2) * 0.12f;
            float t = i / (float)duracao;
            float envelope = Mathf.Min(1f, t * 4f) * Mathf.Min(1f, (1f - t) * 10f);
            trilha[(inicio + i) % trilha.Length] += f2 * volume * envelope * 2.2f;
        }
    }

    // Violino: serra filtrada com vibrato que entra depois do ataque, e o arco que cresce um pouquinho.
    static void Violino(float[] trilha, int inicio, int duracao, float freq, float volume)
    {
        float fase = 0f, f1 = 0f, f2 = 0f;
        int fim = duracao + Taxa / 20;
        for (int i = 0; i < fim; i++)
        {
            float s = i / (float)Taxa;
            float vibrato = 1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 5.5f * s) * Mathf.Clamp01((s - 0.12f) * 4f);
            fase += freq * vibrato / Taxa;
            if (fase >= 1f) fase -= 1f;
            float onda = fase * 2f - 1f;
            f1 += (onda - f1) * 0.3f;
            f2 += (f1 - f2) * 0.3f;
            float envelope = Mathf.Min(1f, s / 0.04f) * (0.85f + 0.15f * Mathf.Min(1f, s * 2f));
            if (i >= duracao) envelope *= 1f - (i - duracao) / (float)(fim - duracao);
            trilha[(inicio + i) % trilha.Length] += f2 * volume * envelope;
        }
    }

    // Baixo: senoide com um pouco do 2º harmônico (grave e redondo, sem embolar com o piano).
    static void Baixo(float[] trilha, int inicio, int duracao, float freq, float volume)
    {
        float fase = 0f;
        for (int i = 0; i < duracao; i++)
        {
            fase += freq / Taxa;
            if (fase >= 1f) fase -= 1f;
            float t = i / (float)duracao;
            float onda = Mathf.Sin(2f * Mathf.PI * fase) + 0.35f * Mathf.Sin(4f * Mathf.PI * fase);
            float envelope = Mathf.Min(1f, i / 60f) * (1f - t * 0.5f) * Mathf.Min(1f, (1f - t) * 20f);
            trilha[(inicio + i) % trilha.Length] += onda * volume * envelope;
        }
    }

    // Bateria. Refrão: rock com prato a cada 4 compassos e virada no fim da frase.
    // Ponte: meio-tempo, e no último compasso uma virada de caixa crescendo.
    static void Bateria(float[] trilha, int inicio, int batida, int compasso, bool pesada, System.Random sorteio)
    {
        if (compasso == 4 || compasso == 8 || compasso == 12) Prato(trilha, inicio, sorteio, 0.1f);

        if (compasso == Compassos - 1)
        {
            Bumbo(trilha, inicio, 0.5f);
            for (int s = 0; s < 16; s++) Caixa(trilha, inicio + s * batida / 4, sorteio, 0.06f + s * 0.012f);
            return;
        }

        bool ponte = compasso >= 12;
        for (int b = 0; b < 4; b++)
        {
            int t = inicio + b * batida;
            if (ponte)
            {
                if (b == 0) Bumbo(trilha, t, 0.5f);
                if (b == 2) Caixa(trilha, t, sorteio, 0.24f);
                Chimbal(trilha, t, sorteio, 0.025f);
                continue;
            }
            if (b == 0 || b == 2 || pesada) Bumbo(trilha, t, 0.5f);
            if (b == 2) Bumbo(trilha, t + batida / 2, 0.4f);
            if (b == 1 || b == 3) Caixa(trilha, t, sorteio, 0.22f);
            Chimbal(trilha, t, sorteio, 0.035f);
            Chimbal(trilha, t + batida / 2, sorteio, 0.022f);
        }
        if (compasso == 11)
            for (int s = 0; s < 4; s++) Caixa(trilha, inicio + 3 * batida + s * batida / 4, sorteio, 0.12f + s * 0.03f);
    }

    static void Bumbo(float[] trilha, int inicio, float volume)
    {
        int duracao = Taxa / 5;
        float fase = 0f;
        for (int i = 0; i < duracao; i++)
        {
            float t = i / (float)duracao;
            fase += Mathf.Lerp(150f, 42f, Mathf.Sqrt(t)) / Taxa;
            trilha[(inicio + i) % trilha.Length] += Mathf.Sin(2f * Mathf.PI * fase) * volume * (1f - t) * (1f - t);
        }
    }

    static void Caixa(float[] trilha, int inicio, System.Random sorteio, float volume)
    {
        int duracao = Taxa / 6;
        float fase = 0f, anterior = 0f;
        for (int i = 0; i < duracao; i++)
        {
            float t = i / (float)duracao;
            fase += 185f / Taxa;
            float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
            anterior += (ruido - anterior) * 0.6f;
            float corpo = Mathf.Sin(2f * Mathf.PI * fase) * Mathf.Max(0f, 1f - t * 4f);
            trilha[(inicio + i) % trilha.Length] += (anterior * 0.8f * (1f - t) * (1f - t) * (1f - t) + corpo * 0.5f) * volume;
        }
    }

    static void Chimbal(float[] trilha, int inicio, System.Random sorteio, float volume)
    {
        int duracao = Taxa / 28;
        float anterior = 0f;
        for (int i = 0; i < duracao; i++)
        {
            float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
            float agudo = ruido - anterior;
            anterior = ruido;
            trilha[(inicio + i) % trilha.Length] += agudo * volume * (1f - i / (float)duracao);
        }
    }

    static void Prato(float[] trilha, int inicio, System.Random sorteio, float volume)
    {
        int duracao = Taxa * 2;
        float anterior = 0f;
        for (int i = 0; i < duracao; i++)
        {
            float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
            float agudo = ruido - anterior;
            anterior = ruido;
            float t = i / (float)duracao;
            trilha[(inicio + i) % trilha.Length] += agudo * volume * (1f - t) * (1f - t) * (1f - t);
        }
    }

    // Reverb de sala (estilo Schroeder: 4 ecos com realimentação em paralelo + 2 "difusores" em série).
    // Roda duas vezes pela música para o rabo do reverb do fim cair no começo (o loop emenda sem corte).
    static float[] Reverb(float[] entrada, float realimenta)
    {
        int n = entrada.Length;
        var saida = new float[n];
        foreach (int atraso in new[] { 1557, 1617, 1491, 1422 })
        {
            var linha = new float[atraso];
            float amortece = 0f;
            for (int i = 0; i < 2 * n; i++)
            {
                int k = i % atraso;
                float atrasado = linha[k];
                amortece += (atrasado - amortece) * 0.6f; // os agudos somem antes, como numa sala de verdade
                linha[k] = entrada[i % n] + amortece * realimenta;
                if (i >= n) saida[i - n] += atrasado * 0.25f;
            }
        }
        foreach (int atraso in new[] { 225, 556 })
        {
            var linha = new float[atraso];
            var difuso = new float[n];
            for (int i = 0; i < 2 * n; i++)
            {
                int k = i % atraso;
                float atrasado = linha[k];
                float x = saida[i % n];
                float y = -0.5f * x + atrasado;
                linha[k] = x + 0.5f * y;
                if (i >= n) difuso[i - n] = y;
            }
            saida = difuso;
        }
        return saida;
    }

    static float Freq(int notaMidi) => 440f * Mathf.Pow(2f, (notaMidi - 69) / 12f);
}
