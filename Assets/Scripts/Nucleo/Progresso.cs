using UnityEngine;

// Tudo o que fica salvo entre uma partida e outra (PlayerPrefs):
//  - o jogo em andamento, para "Continuar" do título (fase, mortes, moedas e tempo);
//  - a fase mais longe que você já chegou (libera o "Escolher fase");
//  - o recorde (zerar com menos mortes).
public static class Progresso
{
    const string ChaveRecorde = "cilada_recorde_mortes";
    const string ChaveFase = "cilada_salvo_fase";
    const string ChaveMortes = "cilada_salvo_mortes";
    const string ChaveMoedas = "cilada_salvo_moedas";
    const string ChaveTempo = "cilada_salvo_tempo";
    const string ChaveDoInicio = "cilada_salvo_do_inicio";
    const string ChaveFaseMaxima = "cilada_fase_maxima";

    // Só vale "Continuar" se você passou da primeira fase.
    public static bool TemJogoSalvo => PlayerPrefs.GetInt(ChaveFase, 0) > 0;
    public static int FaseSalva => PlayerPrefs.GetInt(ChaveFase, 0);
    public static int FaseMaxima => PlayerPrefs.GetInt(ChaveFaseMaxima, 0);

    // doInicio: a partida começou na fase 1? Só assim ela vale para o recorde.
    public static void Salvar(int fase, int mortes, int moedas, float tempo, bool doInicio)
    {
        PlayerPrefs.SetInt(ChaveFase, fase);
        PlayerPrefs.SetInt(ChaveMortes, mortes);
        PlayerPrefs.SetInt(ChaveMoedas, moedas);
        PlayerPrefs.SetFloat(ChaveTempo, tempo);
        PlayerPrefs.SetInt(ChaveDoInicio, doInicio ? 1 : 0);
        if (fase > FaseMaxima) PlayerPrefs.SetInt(ChaveFaseMaxima, fase);
        PlayerPrefs.Save();
    }

    public static void Carregar(out int fase, out int mortes, out int moedas, out float tempo, out bool doInicio)
    {
        fase = PlayerPrefs.GetInt(ChaveFase, 0);
        mortes = PlayerPrefs.GetInt(ChaveMortes, 0);
        moedas = PlayerPrefs.GetInt(ChaveMoedas, 0);
        tempo = PlayerPrefs.GetFloat(ChaveTempo, 0f);
        doInicio = PlayerPrefs.GetInt(ChaveDoInicio, 0) == 1;
    }

    // Zerou: não tem mais o que continuar, mas libera todas as fases no "Escolher fase".
    public static void Zerou(int totalDeFases)
    {
        PlayerPrefs.SetInt(ChaveFase, 0);
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
