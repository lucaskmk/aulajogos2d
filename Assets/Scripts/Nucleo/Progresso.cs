using UnityEngine;

// Tudo o que fica salvo entre uma partida e outra (PlayerPrefs):
//  - a partida em andamento, para "Continuar" do título (onde o Subaru está no mapa,
//    até onde ele chegou, mortes, moedas e tempo);
//  - a fase mais longe que você já chegou em qualquer partida (libera os pontos do mapa);
//  - o recorde (zerar com menos mortes).
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

    // fase: onde o Subaru está. maisLonge: a fase mais longe liberada NESTA partida.
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

    // Modo de teste (F9 no título ou no mapa): todos os pontos do mapa e a fase secreta ficam liberados.
    public static void LiberarTudo(int totalDeFases)
    {
        PlayerPrefs.SetInt(ChaveFaseMaxima, totalDeFases - 1);
        PlayerPrefs.SetInt(ChaveSecreta, 1);
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
}
