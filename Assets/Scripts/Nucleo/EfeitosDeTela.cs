using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Pós-processamento (os "shaders" da tela inteira, do URP), criado por código:
//  - Bloom: as partes bem claras (moedas, brilhos, luzes, sol) brilham em volta;
//  - Vinheta: as bordas da tela ficam um pouco escuras (roxo da Bruxa);
//  - Cor: um pouco mais de saturação e contraste; tom diferente na fase noturna e na biblioteca;
//  - Retorno pela Morte: ao morrer a tela perde a cor e ganha aberração cromática (as cores "se separam");
//    no renascimento a imagem dá um "soco" de distorção de lente;
//  - Pausa: tudo meio sem cor.
public class EfeitosDeTela : MonoBehaviour
{
    public float saturacao = 12f;
    public float contraste = 10f;

    GerenciadorDoJogo jogo;
    Bloom bloom;
    Vignette vinheta;
    ColorAdjustments cor;
    ChromaticAberration aberracao;
    LensDistortion distorcao;

    static readonly Color RoxoDaBruxa = new Color(0.12f, 0f, 0.18f);

    void Awake()
    {
        jogo = GetComponent<GerenciadorDoJogo>();

        var volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        var perfil = ScriptableObject.CreateInstance<VolumeProfile>();

        bloom = perfil.Add<Bloom>(true);
        bloom.threshold.value = 0.9f;  // só o que é bem claro (luzes, nuvens, brilhos) brilha
        bloom.intensity.value = 0.5f;
        bloom.scatter.value = 0.65f;

        vinheta = perfil.Add<Vignette>(true);
        vinheta.color.value = RoxoDaBruxa;
        vinheta.intensity.value = 0.25f;
        vinheta.smoothness.value = 0.45f;

        cor = perfil.Add<ColorAdjustments>(true);
        aberracao = perfil.Add<ChromaticAberration>(true);
        distorcao = perfil.Add<LensDistortion>(true);

        volume.sharedProfile = perfil;
        LigarNaCamera();
    }

    // A câmera só aplica pós-processamento se isso estiver ligado nela.
    public static void LigarNaCamera()
    {
        Camera cam = Camera.main;
        if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    void Update()
    {
        float morte = jogo.ProgressoDaMorte;
        float renascer = jogo.Renascendo;
        bool pausa = jogo.Estado == EstadoDoJogo.Pausado;

        cor.saturation.value = Mathf.Lerp(saturacao, -75f, morte) - (pausa ? 55f : 0f);
        cor.contrast.value = contraste + morte * 15f;
        cor.postExposure.value = jogo.FaseNoturna ? -0.2f : 0f;
        cor.colorFilter.value = jogo.FaseNoturna ? new Color(0.85f, 0.9f, 1.15f)
            : jogo.NaBiblioteca ? new Color(1.08f, 0.97f, 0.86f)
            : Color.white;

        aberracao.intensity.value = Mathf.Max(morte * 0.85f, renascer * 0.5f);
        distorcao.intensity.value = -0.35f * renascer * renascer;
        vinheta.intensity.value = 0.25f + morte * 0.3f + (jogo.FaseNoturna ? 0.1f : 0f);
    }
}
