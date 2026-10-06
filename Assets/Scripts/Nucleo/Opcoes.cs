using UnityEngine;

// Volume da música e dos efeitos (ajustados no menu de pausa e salvos em PlayerPrefs).
public static class Opcoes
{
    const string ChaveMusica = "cilada_volume_musica";
    const string ChaveEfeitos = "cilada_volume_efeitos";

    static float musica = -1f, efeitos = -1f; // -1 = ainda não leu do PlayerPrefs

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => musica = efeitos = -1f;

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
