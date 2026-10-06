using System.Collections.Generic;
using UnityEngine;

// Gera os efeitos sonoros "8-bit" por código (ondas quadradas, ruído...).
// Assim o jogo não depende de nenhum arquivo de áudio.
public static class FabricaDeSons
{
    const int Taxa = 44100;

    public static Dictionary<string, AudioClip> CriarTodos()
    {
        return new Dictionary<string, AudioClip>
        {
            { "pulo", Varredura("pulo", 280f, 640f, 0.14f, 0.22f, true) },
            { "moeda", Notas("moeda", new[] { 988f, 1319f }, new[] { 0.06f, 0.22f }, 0.18f) },
            { "morte", Varredura("morte", 520f, 60f, 0.7f, 0.28f, true) },
            { "armadilha", Ruido("armadilha", 0.22f, 0.3f) },
            { "pisao", Varredura("pisao", 240f, 60f, 0.12f, 0.3f, true) },
            { "mola", Varredura("mola", 180f, 950f, 0.3f, 0.22f, false) },
            { "bloco", Varredura("bloco", 170f, 90f, 0.08f, 0.3f, true) },
            { "pancada", Ruido("pancada", 0.35f, 0.45f) },
            { "serra", Varredura("serra", 900f, 1200f, 0.4f, 0.12f, true) },
            { "risada", Notas("risada", new[] { 620f, 0f, 560f, 0f, 500f, 0f, 440f }, new[] { 0.07f, 0.04f, 0.07f, 0.04f, 0.07f, 0.04f, 0.16f }, 0.18f) },
            { "pop", Varredura("pop", 500f, 1100f, 0.07f, 0.2f, false) },             // coelho se multiplicando
            { "rugido", Varredura("rugido", 120f, 45f, 1.1f, 0.35f, true) },          // Baleia Branca
            { "porta", Varredura("porta", 320f, 140f, 0.3f, 0.2f, true) },            // porta da Beatrice rangendo
            { "conquista", Notas("conquista", new[] { 784f, 988f, 1175f, 1568f }, new[] { 0.08f, 0.08f, 0.08f, 0.3f }, 0.16f) },
            { "vitoria", Notas("vitoria", new[] { 523f, 659f, 784f, 1047f, 0f, 784f, 1047f }, new[] { 0.1f, 0.1f, 0.1f, 0.2f, 0.06f, 0.1f, 0.4f }, 0.18f) },
        };
    }

    // Um tom que "desliza" de uma frequência para outra (pulo, morte, mola...)
    static AudioClip Varredura(string nome, float freqInicial, float freqFinal, float duracao, float volume, bool quadrada)
    {
        int total = Mathf.CeilToInt(duracao * Taxa);
        var dados = new float[total];
        float fase = 0f;
        for (int i = 0; i < total; i++)
        {
            float t = i / (float)total;
            float freq = freqInicial * Mathf.Pow(freqFinal / freqInicial, t);
            fase += 2f * Mathf.PI * freq / Taxa;
            float onda = quadrada ? Mathf.Sign(Mathf.Sin(fase)) : Mathf.Sin(fase);
            dados[i] = onda * volume * Envelope(t);
        }
        return Montar(nome, dados);
    }

    // Uma sequência de notas (0 = silêncio)
    static AudioClip Notas(string nome, float[] frequencias, float[] duracoes, float volume)
    {
        var dados = new List<float>();
        for (int n = 0; n < frequencias.Length; n++)
        {
            int total = Mathf.CeilToInt(duracoes[n] * Taxa);
            for (int i = 0; i < total; i++)
            {
                float t = i / (float)total;
                float valor = frequencias[n] <= 0f ? 0f : Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequencias[n] * i / Taxa));
                dados.Add(valor * volume * Envelope(t));
            }
        }
        return Montar(nome, dados.ToArray());
    }

    // Chiado (explosão, tremor, armadilha)
    static AudioClip Ruido(string nome, float duracao, float volume)
    {
        int total = Mathf.CeilToInt(duracao * Taxa);
        var dados = new float[total];
        var aleatorio = new System.Random(nome.Length * 97);
        float valor = 0f;
        for (int i = 0; i < total; i++)
        {
            if (i % 6 == 0) valor = (float)(aleatorio.NextDouble() * 2.0 - 1.0); // ruído "grosso" estilo 8-bit
            float t = i / (float)total;
            dados[i] = valor * volume * (1f - t) * (1f - t);
        }
        return Montar(nome, dados);
    }

    static float Envelope(float t) => Mathf.Min(1f, t * 40f) * (1f - t);

    static AudioClip Montar(string nome, float[] dados)
    {
        var clipe = AudioClip.Create(nome, dados.Length, 1, Taxa, false);
        clipe.SetData(dados, 0);
        return clipe;
    }
}
