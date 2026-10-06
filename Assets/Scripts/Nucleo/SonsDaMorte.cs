using UnityEngine;

// Os sons do Retorno pela Morte (Re:Zero):
//  - o áudio "retorno_pela_morte", agendado para que a parte mais alta (o "TUM")
//    caia EXATAMENTE quando o jogador renasce;
//  - a música "ost_morte", que toca só os primeiros segundos e vai sumindo.
// Para trocar os sons, substitua os arquivos em Assets/Resources/Sons mantendo os nomes.
public class SonsDaMorte : MonoBehaviour
{
    const string AudioDaMorte = "Sons/retorno_pela_morte"; // em Assets/Resources, sem a extensão
    const string MusicaDaMorte = "Sons/ost_morte";
    const float DuracaoOst = 4f;       // quanto tempo a música toca
    const float DuracaoFadeOst = 1.5f; // nos últimos 1,5 s ela vai sumindo

    AudioSource fonteDaMorte;
    AudioClip clipeDaMorte;
    float picoDaMorte; // em que segundo do áudio vem a parte mais alta

    AudioSource fonteDaOst;
    float timerOst;

    // A OST está tocando? (a música de fundo fica baixinha enquanto isso)
    public bool OstTocando => fonteDaOst.isPlaying;

    void Awake()
    {
        fonteDaMorte = gameObject.AddComponent<AudioSource>();
        fonteDaMorte.playOnAwake = false;
        clipeDaMorte = Resources.Load<AudioClip>(AudioDaMorte);
        if (clipeDaMorte != null)
        {
            fonteDaMorte.clip = clipeDaMorte;
            picoDaMorte = AcharPico(clipeDaMorte);
        }

        fonteDaOst = gameObject.AddComponent<AudioSource>();
        fonteDaOst.playOnAwake = false;
        fonteDaOst.clip = Resources.Load<AudioClip>(MusicaDaMorte);
    }

    // Chamado quando o jogador morre. "duracao" = quantos segundos até ele renascer.
    public void Tocar(float duracao)
    {
        TocarRetornoPelaMorte(duracao);
        TocarOst();
    }

    // O jogador pulou a animação: renasce AGORA, então o "TUM" tem que tocar agora.
    public void PularParaOPico()
    {
        if (clipeDaMorte == null) return;
        fonteDaMorte.Stop();
        fonteDaMorte.Play();
        fonteDaMorte.time = picoDaMorte;
    }

    void Update()
    {
        // Fade-out: o volume cai até zero nos últimos segundos e aí a música para.
        if (!fonteDaOst.isPlaying) return;
        timerOst -= Time.deltaTime;
        if (timerOst <= 0f) fonteDaOst.Stop();
        else fonteDaOst.volume = Mathf.Clamp01(timerOst / DuracaoFadeOst);
    }

    void TocarOst()
    {
        if (fonteDaOst.clip == null) return;
        fonteDaOst.Stop(); // morreu de novo? recomeça do início
        fonteDaOst.volume = 1f;
        fonteDaOst.Play();
        timerOst = DuracaoOst;
    }

    // Agenda o áudio para que a parte mais alta caia EXATAMENTE quando o jogador renasce.
    void TocarRetornoPelaMorte(float duracao)
    {
        if (clipeDaMorte == null)
        {
            GerenciadorDoJogo.Som("morte"); // sem o arquivo de áudio, usa o som 8-bit
            return;
        }
        fonteDaMorte.Stop(); // morreu de novo durante o áudio? recomeça
        float atraso = duracao - picoDaMorte;
        if (atraso >= 0f)
        {
            fonteDaMorte.PlayDelayed(atraso);
        }
        else
        {
            fonteDaMorte.Play();
            fonteDaMorte.time = -atraso; // a espera é mais curta que o começo do áudio: pula um pedaço
        }
    }

    // Primeiro instante em que o áudio chega à metade do volume máximo (o "TUM").
    static float AcharPico(AudioClip clipe)
    {
        clipe.LoadAudioData();
        var amostras = new float[clipe.samples * clipe.channels];
        if (!clipe.GetData(amostras, 0)) return 0f;

        float maximo = 0f;
        foreach (float a in amostras) maximo = Mathf.Max(maximo, Mathf.Abs(a));
        for (int i = 0; i < amostras.Length; i++)
            if (Mathf.Abs(amostras[i]) >= maximo * 0.5f)
                return (float)(i / clipe.channels) / clipe.frequency;
        return 0f;
    }
}
