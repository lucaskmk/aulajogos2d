using System;
using System.Diagnostics;
using System.IO;
using System.Text;

// Gera as músicas do jogo (Assets/Scripts/Nucleo/Musicas.cs) em arquivos WAV, sem abrir a Unity.
// Assim dá para mexer nas notas, rodar de novo e ouvir na hora.
//
//   dotnet run --project Ferramentas/ouvir_musicas -- Builds/musicas
//
// (O .NET que vem com a Unity serve: <pasta da Unity>/Editor/Data/DotNetSdk/dotnet.exe)
// Para cada música saem dois arquivos: ela uma vez e duas vezes seguidas (para ouvir se o loop emenda).
// A das fases sai também 3 semitons acima e abaixo (os tons mais longe que as fases usam).
// No terminal aparecem a duração, o volume (pico e média) e os níveis de cada parte da mistura.
static class Program
{
    static void Main(string[] args)
    {
        string pasta = args.Length > 0 ? args[0] : "musicas";
        Directory.CreateDirectory(pasta);
        foreach (Musicas.Tema tema in Enum.GetValues(typeof(Musicas.Tema)))
        {
            bool chefe = tema != Musicas.Tema.Fase && tema != Musicas.Tema.Mapa;
            string nome = (chefe ? "chefe_" : "musica_") + tema.ToString().ToLowerInvariant();
            Gerar(pasta, nome, tema, 0);
            if (tema == Musicas.Tema.Fase)
            {
                Gerar(pasta, nome + "_mais3", tema, 3);
                Gerar(pasta, nome + "_menos3", tema, -3);
            }
        }

        // os sons da morte (SomDaMare.cs): a onda com o sino e o lamento de acordeão
        Gravar(Path.Combine(pasta, "morte_volta_da_mare.wav"), SomDaMare.Volta(), 1);
        Gravar(Path.Combine(pasta, "morte_lamento.wav"), SomDaMare.Lamento(), 1);
        Console.WriteLine($"Sons da morte: o sino bate em {SomDaMare.SegundoDoSino:0.00} s.");
        Console.WriteLine("WAVs em: " + Path.GetFullPath(pasta));
    }

    static void Gerar(string pasta, string nome, Musicas.Tema tema, int transpor)
    {
        var relogio = Stopwatch.StartNew();
        float[] musica = Musicas.Compor(tema, transpor);
        relogio.Stop();

        double soma = 0, pico = 0;
        foreach (float v in musica)
        {
            soma += v * v;
            pico = Math.Max(pico, Math.Abs(v));
        }
        Console.WriteLine($"{nome}: {musica.Length / (double)Musicas.Taxa:0.0} s, gerada em {relogio.ElapsedMilliseconds} ms, " +
                          $"pico {pico:0.00}, média (RMS) {Math.Sqrt(soma / musica.Length):0.00}");
        Console.WriteLine($"   {Musicas.RelatorioDoTeste}");

        string caminho = Path.Combine(pasta, nome);
        Gravar(caminho + ".wav", musica, 1);
        Gravar(caminho + "_2x.wav", musica, 2);
    }

    // WAV simples: mono, 16 bits.
    static void Gravar(string caminho, float[] amostras, int vezes)
    {
        int total = amostras.Length * vezes, taxa = Musicas.Taxa;
        using var w = new BinaryWriter(File.Create(caminho));
        w.Write(Encoding.ASCII.GetBytes("RIFF"));
        w.Write(36 + total * 2);
        w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        w.Write(16);
        w.Write((short)1);  // PCM
        w.Write((short)1);  // mono
        w.Write(taxa);
        w.Write(taxa * 2);
        w.Write((short)2);
        w.Write((short)16);
        w.Write(Encoding.ASCII.GetBytes("data"));
        w.Write(total * 2);
        for (int k = 0; k < vezes; k++)
            foreach (float v in amostras)
                w.Write((short)Math.Clamp((int)Math.Round(v * 32767f), -32768, 32767));
    }
}
