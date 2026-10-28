using System.Collections.Generic;
using UnityEngine;

// Tudo o que fica salvo entre uma partida e outra (PlayerPrefs):
//  - a partida em andamento, para "Continuar" do título (onde o Grumete está no mapa,
//    até onde ele chegou, mortes, moedas e tempo);
//  - a fase mais longe que você já chegou em qualquer partida (só informativo: o mapa usa a da partida);
//  - o recorde (zerar com menos mortes);
//  - os recordes de CADA FASE (tempo, mortes e moedas), que dão as medalhas (veja Medalhas);
//  - os 5 MELHORES TEMPOS de cada fase (o painel do fim da fase: veja Interface.DesenharPainelDeTempos).
public static class Progresso
{
    const string ChaveRecorde = "cilada_recorde_mortes";
    const string ChaveTemJogo = "cilada_salvo_tem_jogo";
    const string ChaveFase = "cilada_salvo_fase";
    const string ChaveMaisLonge = "cilada_salvo_mais_longe";
    const string ChaveMortes = "cilada_salvo_mortes";
    const string ChaveMoedas = "cilada_salvo_moedas";
    const string ChaveTempo = "cilada_salvo_tempo";
    const string ChaveValida = "cilada_salvo_valida";
    const string ChaveFaseMaxima = "cilada_fase_maxima";
    const string ChaveMortesPorFase = "cilada_salvo_mortes_por_fase";
    const string ChaveSecreta = "cilada_secreta_liberada";
    const string ChaveQuantasFases = "cilada_salvo_quantas_fases";
    const string ChaveEscudos = "cilada_salvo_escudos";
    const string ChaveElixires = "cilada_salvo_elixires";
    const string ChavePuloDuplo = "cilada_salvo_pulo_duplo";
    const string ChaveTutorialDaLoja = "cilada_tutorial_loja";

    // Uma partida salva quando o jogo tinha OUTRA lista de fases (ex.: a versão antiga, de 10 fases) não
    // dá para continuar: os números das fases mudaram de lugar. Nesse caso o "Continuar" some (Novo Jogo).
    public static bool TemJogoSalvo =>
        PlayerPrefs.GetInt(ChaveTemJogo, 0) == 1 && PlayerPrefs.GetInt(ChaveQuantasFases, 0) == Fases.Todas.Length;
    public static int FaseSalva => PlayerPrefs.GetInt(ChaveFase, 0);
    public static int FaseMaxima => PlayerPrefs.GetInt(ChaveFaseMaxima, 0);

    // A fase secreta (vale para sempre depois de liberada).
    public static bool SecretaLiberada => PlayerPrefs.GetInt(ChaveSecreta, 0) == 1;

    public static void LiberarSecreta()
    {
        PlayerPrefs.SetInt(ChaveSecreta, 1);
        PlayerPrefs.Save();
    }

    // Mortes em cada fase nesta partida (as caveiras do mapa), guardadas como "3,0,12,...".
    public static void SalvarMortesPorFase(int[] mortes)
    {
        PlayerPrefs.SetString(ChaveMortesPorFase, string.Join(",", mortes));
        PlayerPrefs.Save();
    }

    public static void CarregarMortesPorFase(int[] mortes)
    {
        string[] partes = PlayerPrefs.GetString(ChaveMortesPorFase, "").Split(',');
        for (int i = 0; i < mortes.Length; i++)
            mortes[i] = i < partes.Length && int.TryParse(partes[i], out int n) ? n : 0;
    }

    // fase: onde o Grumete está. maisLonge: a fase mais longe liberada NESTA partida.
    // valida: a partida foi jogada em ordem desde a fase 1? Só assim ela vale para o recorde.
    public static void Salvar(int fase, int maisLonge, int mortes, int moedas, float tempo, bool valida)
    {
        PlayerPrefs.SetInt(ChaveTemJogo, 1);
        PlayerPrefs.SetInt(ChaveQuantasFases, Fases.Todas.Length);
        PlayerPrefs.SetInt(ChaveFase, fase);
        PlayerPrefs.SetInt(ChaveMaisLonge, maisLonge);
        PlayerPrefs.SetInt(ChaveMortes, mortes);
        PlayerPrefs.SetInt(ChaveMoedas, moedas);
        PlayerPrefs.SetFloat(ChaveTempo, tempo);
        PlayerPrefs.SetInt(ChaveValida, valida ? 1 : 0);
        int maxima = Mathf.Max(fase == Fases.IndiceSecreto ? 0 : fase, maisLonge); // a secreta não conta
        if (maxima > FaseMaxima) PlayerPrefs.SetInt(ChaveFaseMaxima, maxima);
        PlayerPrefs.Save();
    }

    public static void Carregar(out int fase, out int maisLonge, out int mortes, out int moedas, out float tempo, out bool valida)
    {
        fase = PlayerPrefs.GetInt(ChaveFase, 0);
        maisLonge = PlayerPrefs.GetInt(ChaveMaisLonge, 0);
        mortes = PlayerPrefs.GetInt(ChaveMortes, 0);
        moedas = PlayerPrefs.GetInt(ChaveMoedas, 0);
        tempo = PlayerPrefs.GetFloat(ChaveTempo, 0f);
        valida = PlayerPrefs.GetInt(ChaveValida, 0) == 1;
    }

    // O que foi comprado na Loja e ainda não foi usado (escudos e elixires) e o duplo salto. Fazem parte da
    // partida salva (um Novo Jogo zera).
    public static int EscudosSalvos => PlayerPrefs.GetInt(ChaveEscudos, 0);
    public static int ElixiresSalvos => PlayerPrefs.GetInt(ChaveElixires, 0);
    public static bool PuloDuploSalvo => PlayerPrefs.GetInt(ChavePuloDuplo, 0) == 1;

    public static void SalvarItensDaLoja(int escudos, int elixires, bool puloDuplo)
    {
        PlayerPrefs.SetInt(ChaveEscudos, escudos);
        PlayerPrefs.SetInt(ChaveElixires, elixires);
        PlayerPrefs.SetInt(ChavePuloDuplo, puloDuplo ? 1 : 0);
        PlayerPrefs.Save();
    }

    // O Louro já apresentou a Loja (a cutscene depois da 2ª fase)? Vale para sempre, como as conquistas:
    // só acontece uma vez (um Novo Jogo NÃO mostra de novo). Para rever: menu "Cilada (teste)" no Editor.
    public static bool TutorialDaLojaVisto => PlayerPrefs.GetInt(ChaveTutorialDaLoja, 0) == 1;

    public static void MarcarTutorialDaLoja()
    {
        PlayerPrefs.SetInt(ChaveTutorialDaLoja, 1);
        PlayerPrefs.Save();
    }

    public static void ApagarSecreta()
    {
        PlayerPrefs.SetInt(ChaveSecreta, 0);
        PlayerPrefs.Save();
    }

    // Zerou: não tem mais o que continuar, mas todas as fases ficam liberadas no mapa.
    public static void Zerou(int totalDeFases)
    {
        PlayerPrefs.SetInt(ChaveTemJogo, 0);
        PlayerPrefs.SetInt(ChaveFaseMaxima, totalDeFases - 1);
        PlayerPrefs.Save();
    }

    // -1 = ainda não tem recorde
    public static int Recorde => PlayerPrefs.GetInt(ChaveRecorde, -1);

    public static void TentarSalvarRecorde(int mortes)
    {
        if (Recorde >= 0 && mortes >= Recorde) return;
        PlayerPrefs.SetInt(ChaveRecorde, mortes);
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------ recordes de cada fase

    // Valem para sempre, como as conquistas (um Novo Jogo NÃO apaga). Cada número é o MELHOR que você já fez
    // naquela fase, cada um por si: o menor tempo, o menor número de mortes e o maior número de moedas.
    // Ficam guardados pelo NÚMERO da fase ("secreta" para a fase secreta).
    public struct RecordeDaFase
    {
        public bool existe;  // já passou dessa fase alguma vez?
        public float tempo;  // segundos, desde que entrou pela fase no mapa (o tempo das mortes conta)
        public int mortes;
        public int moedas;   // as da tentativa em que passou (morrer zera as moedas da fase)
    }

    // O que uma passada pela fase trouxe de novo. Primeira = nunca tinha passado (aí não é "recorde batido").
    [System.Flags]
    public enum Novidade { Nada = 0, Primeira = 1, Tempo = 2, Mortes = 4, Moedas = 8 }

    static string ChaveDaFase(int fase) => "cilada_recorde_" + (fase == Fases.IndiceSecreto ? "secreta" : "fase" + fase) + "_";

    public static RecordeDaFase RecordeDe(int fase)
    {
        string chave = ChaveDaFase(fase);
        if (PlayerPrefs.GetInt(chave + "existe", 0) != 1) return default;
        return new RecordeDaFase
        {
            existe = true,
            tempo = PlayerPrefs.GetFloat(chave + "tempo", 0f),
            mortes = PlayerPrefs.GetInt(chave + "mortes", 0),
            moedas = PlayerPrefs.GetInt(chave + "moedas", 0),
        };
    }

    // Guarda o que você acabou de fazer (só o que for MELHOR que o recorde) e diz o que foi novidade.
    public static Novidade RegistrarFase(int fase, float tempo, int mortes, int moedas)
    {
        RecordeDaFase antes = RecordeDe(fase);
        string chave = ChaveDaFase(fase);
        Novidade novidade = antes.existe ? Novidade.Nada : Novidade.Primeira;
        if (!antes.existe || tempo < antes.tempo)
        {
            PlayerPrefs.SetFloat(chave + "tempo", tempo);
            if (antes.existe) novidade |= Novidade.Tempo;
        }
        if (!antes.existe || mortes < antes.mortes)
        {
            PlayerPrefs.SetInt(chave + "mortes", mortes);
            if (antes.existe) novidade |= Novidade.Mortes;
        }
        if (!antes.existe || moedas > antes.moedas)
        {
            PlayerPrefs.SetInt(chave + "moedas", moedas);
            if (antes.existe) novidade |= Novidade.Moedas;
        }
        PlayerPrefs.SetInt(chave + "existe", 1);
        PlayerPrefs.Save();
        return novidade;
    }

    // ------------------------------------------------------------------ melhores tempos de cada fase

    // O painel do fim da fase mostra os 5 tempos mais rápidos que você já fez em cada fase, de qualquer partida
    // (um Novo Jogo NÃO apaga, como os recordes). Usa o MESMO relógio do recorde: o TempoNaFase do
    // GerenciadorDoJogo (desde que entrou pela fase no mapa, contando o tempo das mortes).
    // Fica guardado como texto, do mais rápido para o mais lento: "41.234|2|27/10/26;55.1|5|26/10/26;..."
    public const int TamanhoDoRanking = 5;

    public struct TempoDoRanking
    {
        public float tempo;  // segundos
        public int mortes;   // -1 = não sabemos (tempo que veio do recorde de antes de o painel existir)
        public string data;  // "27/10/26" ("" = não sabemos)
    }

    // Número com PONTO, sempre: no Windows em português o float viraria "41,2" e quebraria a leitura.
    static readonly System.Globalization.CultureInfo Invariante = System.Globalization.CultureInfo.InvariantCulture;

    public static List<TempoDoRanking> MelhoresTempos(int fase)
    {
        var lista = new List<TempoDoRanking>();
        string chave = ChaveDaFase(fase);
        if (!PlayerPrefs.HasKey(chave + "ranking"))
        {
            // fase que você já tinha passado antes do painel existir: o recorde antigo vira o 1º lugar
            RecordeDaFase recorde = RecordeDe(fase);
            if (recorde.existe) lista.Add(new TempoDoRanking { tempo = recorde.tempo, mortes = -1, data = "" });
            return lista;
        }
        foreach (string item in PlayerPrefs.GetString(chave + "ranking", "").Split(';'))
        {
            string[] partes = item.Split('|');
            if (partes.Length < 3 || !float.TryParse(partes[0], System.Globalization.NumberStyles.Float, Invariante, out float tempo))
                continue;
            lista.Add(new TempoDoRanking
            {
                tempo = tempo,
                mortes = int.TryParse(partes[1], out int mortes) ? mortes : -1,
                data = partes[2],
            });
        }
        lista.Sort((a, b) => a.tempo.CompareTo(b.tempo));
        if (lista.Count > TamanhoDoRanking) lista.RemoveRange(TamanhoDoRanking, lista.Count - TamanhoDoRanking);
        return lista;
    }

    // Quantas vezes você já passou dessa fase, somando todas as partidas.
    public static int VezesConcluida(int fase) =>
        PlayerPrefs.GetInt(ChaveDaFase(fase) + "vezes", RecordeDe(fase).existe ? 1 : 0);

    // Coloca o tempo que você acabou de fazer no ranking da fase e conta mais uma vez concluída.
    // Devolve a posição (0 = o mais rápido) ou -1 se não entrou entre os 5.
    // Chamar ANTES de RegistrarFase (assim o recorde antigo ainda conta como o 1º lugar, se for o caso).
    public static int RegistrarNoRanking(int fase, float tempo, int mortes)
    {
        string chave = ChaveDaFase(fase);
        PlayerPrefs.SetInt(chave + "vezes", VezesConcluida(fase) + 1);
        List<TempoDoRanking> lista = MelhoresTempos(fase);
        int posicao = 0;
        while (posicao < lista.Count && lista[posicao].tempo <= tempo) posicao++; // empate: quem fez antes fica na frente
        if (posicao >= TamanhoDoRanking)
        {
            PlayerPrefs.Save();
            return -1;
        }
        lista.Insert(posicao, new TempoDoRanking
        {
            tempo = tempo,
            mortes = mortes,
            data = System.DateTime.Now.ToString("dd/MM/yy", Invariante),
        });
        if (lista.Count > TamanhoDoRanking) lista.RemoveAt(lista.Count - 1);
        var partes = new string[lista.Count];
        for (int i = 0; i < lista.Count; i++)
            partes[i] = lista[i].tempo.ToString("R", Invariante) + "|" + lista[i].mortes + "|" + lista[i].data;
        PlayerPrefs.SetString(chave + "ranking", string.Join(";", partes));
        PlayerPrefs.Save();
        return posicao;
    }
}
