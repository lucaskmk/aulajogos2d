using System;

// Os sons da VOLTA DA MARÉ (a morte), compostos e sintetizados por código (nada gravado, nada copiado):
//  - Volta():   uma ONDA do mar que cresce, cresce... e quebra num SINO de navio ("DONG"), com um "tum" grave
//               junto. O sino bate em SegundoDoSino: o SonsDaMorte agenda o som para o sino bater EXATAMENTE
//               quando o jogador renasce. Depois a onda volta para o mar e o sino fica ressoando;
//  - Lamento(): uma frase curtinha e triste de acordeão (em ré menor), que toca assim que você morre.
// Como as músicas dos chefes, este arquivo é C# puro (sem Unity): a ferramenta Ferramentas/ouvir_musicas
// também gera estes sons em WAV. Quem transforma em AudioClip é o SonsDaMorte.
public static class SomDaMare
{
    public const int Taxa = 22050;
    public const float SegundoDoSino = 1.05f; // quando o sino bate (o "pico" da Volta)

    // ------------------------------------------------------------------ a volta (onda + sino)

    public static float[] Volta()
    {
        float duracao = SegundoDoSino + 1.9f;
        var som = new float[(int)(duracao * Taxa)];
        uint ruido = 12345u;
        float f1 = 0f, f2 = 0f;
        for (int i = 0; i < som.Length; i++)
        {
            float t = i / (float)Taxa;
            // a onda: ruído que vai ficando mais forte e mais "aberto" (o filtro abre) até quebrar no sino,
            // e depois some devagar, cada vez mais abafado (a água voltando)
            float antes = Math.Clamp(t / SegundoDoSino, 0f, 1f);
            float depois = Math.Max(0f, t - SegundoDoSino);
            float corte = t < SegundoDoSino ? 250f + 2800f * antes * antes : 3050f * MathF.Exp(-depois / 0.45f) + 200f;
            float a = 1f - MathF.Exp(-2f * MathF.PI * corte / Taxa);
            float branco = Ruido(ref ruido);
            f1 += (branco - f1) * a;
            f2 += (f1 - f2) * a;
            float forca = t < SegundoDoSino ? 0.35f * antes * antes : 0.45f * MathF.Exp(-depois / 0.55f);
            float vaiEVem = 1f + 0.25f * MathF.Sin(2f * MathF.PI * 2.2f * t); // a água "respirando"
            som[i] += f2 * forca * vaiEVem * 2.2f;
        }
        Sino(som, (int)(SegundoDoSino * Taxa), 196f, 0.55f, ref ruido); // sol 3
        Tum(som, (int)(SegundoDoSino * Taxa), 0.7f);
        Normalizar(som, 0.9f);
        return som;
    }

    // Um sino de bronze: harmônicos "tortos" (não são múltiplos exatos da nota), cada um sumindo num tempo,
    // e a batida do badalo no começo.
    static readonly float[] Parciais = { 0.5f, 1f, 1.19f, 1.5f, 2f, 2.52f, 3.01f };
    static readonly float[] ForcaDosParciais = { 0.55f, 1f, 0.45f, 0.3f, 0.55f, 0.22f, 0.16f };
    static readonly float[] QuedaDosParciais = { 2.2f, 1.6f, 1.1f, 0.8f, 0.7f, 0.4f, 0.3f };

    static void Sino(float[] som, int inicio, float freq, float volume, ref uint ruido)
    {
        for (int i = inicio; i < som.Length; i++)
        {
            float t = (i - inicio) / (float)Taxa;
            float soma = 0f;
            for (int p = 0; p < Parciais.Length; p++)
                soma += ForcaDosParciais[p] * MathF.Sin(2f * MathF.PI * freq * Parciais[p] * t) * MathF.Exp(-t / QuedaDosParciais[p]);
            if (t < 0.006f) soma += Ruido(ref ruido) * (1f - t / 0.006f) * 1.5f; // o badalo
            som[i] += soma * volume * Math.Min(1f, t / 0.002f);
        }
    }

    // O "tum" grave por baixo do sino (dá o peso do renascimento).
    static void Tum(float[] som, int inicio, float volume)
    {
        double fase = 0.0;
        for (int i = inicio; i < som.Length; i++)
        {
            float t = (i - inicio) / (float)Taxa;
            fase += (48f + 60f * MathF.Exp(-t / 0.06f)) / Taxa;
            som[i] += MathF.Sin(2f * MathF.PI * (float)fase) * MathF.Exp(-t / 0.5f) * volume;
        }
    }

    // ------------------------------------------------------------------ o lamento (acordeão)

    // (nota MIDI, duração em segundos): lá, ré, fá, mi, ré.
    static readonly (int nota, float duracao)[] Frase = { (69, 0.3f), (74, 0.3f), (77, 0.45f), (76, 0.15f), (74, 1.1f) };

    public static float[] Lamento()
    {
        float total = 0f;
        foreach (var n in Frase) total += n.duracao;
        var som = new float[(int)((total + 0.4f) * Taxa)];
        float inicio = 0f;
        foreach (var n in Frase)
        {
            Acordeao(som, (int)(inicio * Taxa), (int)(n.duracao * Taxa), Freq(n.nota), 0.22f);
            inicio += n.duracao;
        }
        // o baixo segurando o ré (e a quinta, lá) por baixo de tudo, como o fole do acordeão
        Acordeao(som, 0, (int)(total * Taxa), Freq(50), 0.12f);
        Acordeao(som, 0, (int)(total * Taxa), Freq(57), 0.08f);
        Normalizar(som, 0.8f);
        return som;
    }

    // Acordeão: duas palhetas levemente desafinadas (o "batimento" típico), filtradas, com o fole tremendo.
    static void Acordeao(float[] som, int inicio, int duracao, float freq, float volume)
    {
        int fim = Math.Min(som.Length - inicio, duracao + (int)(0.12f * Taxa));
        float faseA = 0f, faseB = 0f, f1 = 0f, f2 = 0f;
        float a = 1f - MathF.Exp(-2f * MathF.PI * 2400f / Taxa);
        for (int i = 0; i < fim; i++)
        {
            float t = i / (float)Taxa;
            faseA += freq * 1.004f / Taxa; faseA -= MathF.Floor(faseA);
            faseB += freq * 0.996f / Taxa; faseB -= MathF.Floor(faseB);
            float palhetas = (2f * faseA - 1f) + (faseB < 0.4f ? 0.8f : -0.53f);
            f1 += (palhetas - f1) * a;
            f2 += (f1 - f2) * a;
            float envelope = Math.Min(1f, t / 0.03f) * (i < duracao ? 1f : 1f - (i - duracao) / (0.12f * Taxa));
            float fole = 1f + 0.08f * MathF.Sin(2f * MathF.PI * 5.5f * t);
            som[inicio + i] += f2 * envelope * fole * volume;
        }
    }

    // ------------------------------------------------------------------ ajudantes

    static float Freq(int midi) => 440f * MathF.Pow(2f, (midi - 69) / 12f);

    static float Ruido(ref uint estado)
    {
        estado ^= estado << 13;
        estado ^= estado >> 17;
        estado ^= estado << 5;
        return estado / (float)uint.MaxValue * 2f - 1f;
    }

    static void Normalizar(float[] som, float pico)
    {
        float maximo = 0.0001f;
        foreach (float v in som) maximo = Math.Max(maximo, Math.Abs(v));
        for (int i = 0; i < som.Length; i++) som[i] *= pico / maximo;
    }
}
