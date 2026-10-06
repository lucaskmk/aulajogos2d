using UnityEngine;

// Música de fundo.
// Se existir Assets/Resources/Sons/musica_fundo (mp3, ogg ou wav), toca ela em todas as fases.
// Se não existir, toca a música lofi 8-bit gerada por código (FabricaDeMusica), uma variação por fase.
// O volume abaixa sozinho na morte (para a OST do Retorno pela Morte aparecer) e na pausa.
public class Musica : MonoBehaviour
{
    const string ArquivoDaMusica = "Sons/musica_fundo"; // em Assets/Resources, sem a extensão

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
    }

    // 1 = volume normal, 0 = mudo. O volume "desliza" até o alvo.
    public void Abafar(float quanto) => alvo = quanto;

    void Update()
    {
        // tempo "real": continua funcionando com o jogo pausado (Time.timeScale = 0)
        fonte.volume = Mathf.MoveTowards(fonte.volume, alvo * volume, velocidadeDoFade * volume * Time.unscaledDeltaTime);
    }
}
