using UnityEngine;

// Música de fundo.
// Se existir Assets/Resources/Sons/musica_fundo (mp3, ogg ou wav), toca ela em todas as fases.
// Dá para repetir só um TRECHO do arquivo (o refrão, por exemplo): coloque em InicioDoTrecho e
// FimDoTrecho o segundo em que ele começa e termina (0 e 0 = a música inteira).
// Se não existir o arquivo, toca a música gerada por código (FabricaDeMusica), uma variação por fase.
// O volume abaixa sozinho na morte (para a OST do Retorno pela Morte aparecer) e na pausa.
public class Musica : MonoBehaviour
{
    const string ArquivoDaMusica = "Sons/musica_fundo"; // em Assets/Resources, sem a extensão

    // ===== COLOQUE AQUI o trecho que fica repetindo (em segundos). 0 e 0 = a música inteira. =====
    const float InicioDoTrecho = 0f;
    const float FimDoTrecho = 0f;

    [Tooltip("Segundo em que o trecho que repete começa (só para o arquivo musica_fundo).")]
    public float inicioDoTrecho = InicioDoTrecho;
    [Tooltip("Segundo em que o trecho termina e volta para o começo dele. 0 = até o fim do arquivo.")]
    public float fimDoTrecho = FimDoTrecho;

    [Range(0f, 1f)] public float volume = 0.4f;
    public float velocidadeDoFade = 1.5f; // quanto do volume muda por segundo

    AudioSource fonte;
    AudioClip arquivo;
    float alvo = 1f; // 0 a 1, multiplicado pelo volume

    void Awake()
    {
        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = true;
        fonte.volume = 0f;
        fonte.ignoreListenerPause = true; // continua tocando (baixinho) com o jogo pausado
        arquivo = Resources.Load<AudioClip>(ArquivoDaMusica);
    }

    public void TocarDaFase(int fase)
    {
        AudioClip clipe = arquivo != null ? arquivo : FabricaDeMusica.Lofi(fase);
        if (fonte.clip == clipe && fonte.isPlaying) return; // já está tocando: não recomeça
        fonte.clip = clipe;
        fonte.volume = 0f; // entra suave
        fonte.Play();
        if (clipe == arquivo && inicioDoTrecho > 0f && inicioDoTrecho < clipe.length) fonte.time = inicioDoTrecho;
    }

    // 1 = volume normal, 0 = mudo. O volume "desliza" até o alvo.
    public void Abafar(float quanto) => alvo = quanto;

    void Update()
    {
        // chegou no fim do trecho? volta para o começo dele
        if (fonte.clip == arquivo && arquivo != null && fimDoTrecho > inicioDoTrecho && fonte.time >= fimDoTrecho)
            fonte.time = inicioDoTrecho;

        // tempo "real": continua funcionando com o jogo pausado (Time.timeScale = 0)
        float maximo = volume * Opcoes.Musica; // o volume da música do menu de pausa
        fonte.volume = Mathf.MoveTowards(fonte.volume, alvo * maximo, velocidadeDoFade * volume * Time.unscaledDeltaTime);
    }
}
