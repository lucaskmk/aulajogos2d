using UnityEngine;

// Volume da música e dos efeitos e o tipo de gráficos (ajustados no menu de pausa e salvos em PlayerPrefs).
public static class Opcoes
{
    const string ChaveMusica = "cilada_volume_musica";
    const string ChaveEfeitos = "cilada_volume_efeitos";
    const string ChaveGraficos = "cilada_graficos_hd2d";

    static float musica = -1f, efeitos = -1f; // -1 = ainda não leu do PlayerPrefs
    static int graficos = -1;                 // -1 = ainda não leu; 1 = HD-2D; 0 = Simples

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar()
    {
        musica = efeitos = -1f;
        graficos = -1;
    }

    // Gráficos "HD-2D" (padrão): fundo desfocado, feixes de luz, primeiro plano e cores de cinema.
    // "Simples": o visual antigo, mais leve (para computador fraco ou navegador travando).
    public static bool GraficosHD2D
    {
        get
        {
            if (graficos < 0) graficos = PlayerPrefs.GetInt(ChaveGraficos, 1);
            return graficos == 1;
        }
        set
        {
            graficos = value ? 1 : 0;
            PlayerPrefs.SetInt(ChaveGraficos, graficos);
            PlayerPrefs.Save();
        }
    }

    public static float Musica
    {
        get
        {
            if (musica < 0f) musica = PlayerPrefs.GetFloat(ChaveMusica, 0.8f);
            return musica;
        }
        set
        {
            musica = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(ChaveMusica, musica);
            PlayerPrefs.Save();
        }
    }

    public static float Efeitos
    {
        get
        {
            if (efeitos < 0f) efeitos = PlayerPrefs.GetFloat(ChaveEfeitos, 1f);
            return efeitos;
        }
        set
        {
            efeitos = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(ChaveEfeitos, efeitos);
            PlayerPrefs.Save();
        }
    }
}
