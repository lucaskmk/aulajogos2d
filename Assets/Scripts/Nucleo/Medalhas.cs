using UnityEngine;

// As MEDALHAS de cada fase (rejogabilidade). Cada fase tem 3, uma para cada recorde (veja Progresso):
//  - RELÓGIO: passar a fase dentro do tempo-alvo (o tempo conta desde que você entra pelo mapa, com as mortes);
//  - CORAÇÃO: passar a fase sem morrer nenhuma vez;
//  - MOEDA:   pegar TODAS as moedas da fase numa tentativa só (morrer zera as moedas da fase).
//    (numa fase sem moeda nenhuma, a medalha de moeda vem junto com a primeira vitória)
// As medalhas vêm dos recordes, que valem para sempre: dá para ganhar cada uma numa tentativa diferente.
// Elas aparecem no mapa do mundo (do lado de cada fase que você já passou) e na tela de "fase concluída".
// Os tempos-alvo são uma estimativa: depois de testar, ajuste no campo tempoAlvo de cada fase (Fases.cs).
public static class Medalhas
{
    [System.Flags]
    public enum Conjunto { Nenhuma = 0, Tempo = 1, SemMorrer = 2, Moedas = 4, Todas = Tempo | SemMorrer | Moedas }

    // A ordem em que as medalhas aparecem (no mapa e nas telas).
    public static readonly Conjunto[] Ordem = { Conjunto.Tempo, Conjunto.SemMorrer, Conjunto.Moedas };

    public static string Desenho(Conjunto medalha) =>
        medalha == Conjunto.Tempo ? "medalha_tempo" : medalha == Conjunto.SemMorrer ? "medalha_coracao" : "medalha_moeda";

    // As medalhas que você já tem nesta fase.
    public static Conjunto DaFase(int fase)
    {
        Progresso.RecordeDaFase recorde = Progresso.RecordeDe(fase);
        if (!recorde.existe) return Conjunto.Nenhuma;
        Conjunto medalhas = Conjunto.Nenhuma;
        if (recorde.tempo <= TempoAlvo(fase)) medalhas |= Conjunto.Tempo;
        if (recorde.mortes == 0) medalhas |= Conjunto.SemMorrer;
        if (recorde.moedas >= TotalDeMoedas(fase)) medalhas |= Conjunto.Moedas;
        return medalhas;
    }

    public static int Quantas(Conjunto medalhas)
    {
        int total = 0;
        foreach (Conjunto m in Ordem) if ((medalhas & m) != 0) total++;
        return total;
    }

    // O tempo-alvo da fase, em segundos. Se a fase não disser (tempoAlvo = 0), é calculado pelo tamanho dela:
    // 15 s + 0,45 s por coluna, arredondado para cima de 5 em 5 segundos.
    public static float TempoAlvo(int fase)
    {
        Fase dados = Fases.Dados(fase);
        if (dados.tempoAlvo > 0f) return dados.tempoAlvo;
        int largura = 0;
        foreach (string linha in dados.mapa) largura = Mathf.Max(largura, linha.Length);
        return Mathf.Ceil((15f + 0.45f * largura) / 5f) * 5f;
    }

    // Quantas moedas a fase tem: as '$' e os blocos '?' (igual ao ConstrutorDeFase; a moeda assassina não conta).
    public static int TotalDeMoedas(int fase)
    {
        int total = 0;
        foreach (string linha in Fases.Dados(fase).mapa)
            foreach (char c in linha)
                if (c == '$' || c == '?') total++;
        return total;
    }

    // ------------------------------------------------------------------ desenhos

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("medalha_tempo", () => FabricaDeSprites.DeArte(ArteTempo, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("medalha_coracao", () => FabricaDeSprites.DeArte(ArteCoracao, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("medalha_moeda", () => FabricaDeSprites.DeArte(ArteMoeda, FabricaDeSprites.Centro));
    }

    // As três têm a mesma borda dourada (y) com contorno (k); muda só o miolo.
    // Relógio: fundo azul (e) com os ponteiros brancos (w).
    static readonly string[] ArteTempo =
    {
        "..kkkkk..",
        ".kyyyyyk.",
        "kyyeweyyk",
        "kyeeweeyk",
        "kyeewweyk",
        "kyeeeeeyk",
        "kyyeeeyyk",
        ".kyyyyyk.",
        "..kkkkk..",
    };

    // Coração: fundo rosa (p) com o coração vermelho (r) e um brilho (w).
    static readonly string[] ArteCoracao =
    {
        "..kkkkk..",
        ".kyyyyyk.",
        "kyypppyyk",
        "kyrwprpyk",
        "kyrrrrryk",
        "kyprrrpyk",
        "kyyprpyyk",
        ".kyyyyyk.",
        "..kkkkk..",
    };

    // Moeda: fundo laranja (o) com o "$" branco (w).
    static readonly string[] ArteMoeda =
    {
        "..kkkkk..",
        ".kyyyyyk.",
        "kyywwwyyk",
        "kyowoooyk",
        "kyoowooyk",
        "kyooowoyk",
        "kyywwwyyk",
        ".kyyyyyk.",
        "..kkkkk..",
    };
}
