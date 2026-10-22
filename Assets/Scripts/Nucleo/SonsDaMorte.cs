using UnityEngine;

// Os sons da Volta da Maré (a morte), todos gerados por código em SomDaMare:
//  - a "volta": a onda que cresce e quebra no SINO de navio, agendada para que o sino bata
//    EXATAMENTE quando o jogador renasce;
//  - o "lamento": a frase curtinha de acordeão, que toca só os primeiros segundos e vai sumindo.
public class SonsDaMorte : MonoBehaviour
{
    const float DuracaoDoLamento = 2f;      // quanto tempo o lamento toca (curtinho: morre-se MUITO neste jogo)
    const float DuracaoDoFadeDoLamento = 0.8f; // nos últimos 0,8 s ele vai sumindo
    const float VolumeDoLamento = 0.7f;

    AudioSource fonteDaVolta;
    AudioClip clipeDaVolta;
    float picoDaVolta; // em que segundo do som o sino bate

    AudioSource fonteDoLamento;
    float timerDoLamento;

    // O lamento está tocando? (a música de fundo fica baixinha enquanto isso)
    public bool OstTocando => fonteDoLamento.isPlaying;

    void Awake()
    {
        fonteDaVolta = gameObject.AddComponent<AudioSource>();
        fonteDaVolta.playOnAwake = false;
        clipeDaVolta = CriarClipe("volta_da_mare", SomDaMare.Volta());
        fonteDaVolta.clip = clipeDaVolta;
        picoDaVolta = SomDaMare.SegundoDoSino;

        fonteDoLamento = gameObject.AddComponent<AudioSource>();
        fonteDoLamento.playOnAwake = false;
        fonteDoLamento.clip = CriarClipe("lamento_da_mare", SomDaMare.Lamento());
    }

    static AudioClip CriarClipe(string nome, float[] amostras)
    {
        var clipe = AudioClip.Create(nome, amostras.Length, 1, SomDaMare.Taxa, false);
        clipe.SetData(amostras, 0);
        return clipe;
    }

    // Chamado quando o jogador morre. "duracao" = quantos segundos até ele renascer.
    public void Tocar(float duracao)
    {
        TocarVolta(duracao);
        TocarLamento();
    }

    // O jogador pulou a animação: renasce AGORA, então o sino tem que bater agora.
    public void PularParaOPico()
    {
        fonteDaVolta.Stop();
        fonteDaVolta.Play();
        fonteDaVolta.time = picoDaVolta;
    }

    void Update()
    {
        // Fade-out: o volume cai até zero nos últimos segundos e aí o lamento para.
        if (!fonteDoLamento.isPlaying) return;
        timerDoLamento -= Time.deltaTime;
        if (timerDoLamento <= 0f) fonteDoLamento.Stop();
        else fonteDoLamento.volume = Mathf.Clamp01(timerDoLamento / DuracaoDoFadeDoLamento) * VolumeDoLamento * Opcoes.Musica;
    }

    void TocarLamento()
    {
        fonteDoLamento.Stop(); // morreu de novo? recomeça do início
        fonteDoLamento.volume = VolumeDoLamento * Opcoes.Musica;
        fonteDoLamento.Play();
        timerDoLamento = DuracaoDoLamento;
    }

    // Agenda a volta para que o sino bata EXATAMENTE quando o jogador renasce.
    void TocarVolta(float duracao)
    {
        fonteDaVolta.Stop(); // morreu de novo durante o som? recomeça
        fonteDaVolta.volume = Opcoes.Efeitos;
        float atraso = duracao - picoDaVolta;
        if (atraso >= 0f)
        {
            fonteDaVolta.PlayDelayed(atraso);
        }
        else
        {
            fonteDaVolta.Play();
            fonteDaVolta.time = -atraso; // a espera é mais curta que o começo do som: pula um pedaço da onda
        }
    }
}
