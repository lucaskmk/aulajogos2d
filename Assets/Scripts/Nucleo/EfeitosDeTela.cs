using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Pós-processamento (os "shaders" da tela inteira, do URP), criado por código:
//  - Bloom: as partes bem claras (luzes, lampiões, brilhos) brilham em volta;
//  - Vinheta: as bordas da tela ficam um pouco escuras (roxo da Bruxa);
//  - Cor: um pouco mais de saturação e contraste;
//  - Retorno pela Morte: ao morrer a tela perde a cor e ganha aberração cromática (as cores "se separam");
//    no renascimento a imagem dá um "soco" de distorção de lente;
//  - Pausa: tudo meio sem cor.
//
// Com os gráficos em HD-2D (Opcoes.GraficosHD2D) entra a "correção de cor de cinema", que muda com o clima da fase
// (e, nas fases de dia, com a HORA: manhã no começo da fase, entardecer dourado no fim; veja LuzDoDia):
//  - SplitToning: pinta as SOMBRAS de azul-arroxeado (frio) e as partes CLARAS de dourado (quente).
//    É o truque mais usado em filme e em Octopath Traveler: o contraste quente/frio dá "volume" à imagem;
//  - ShadowsMidtonesHighlights: escurece e esfria só as sombras, esquenta só as luzes;
//  - WhiteBalance: a "temperatura" da luz (dia dourado, noite azul, biblioteca cor de sépia).
// Nada disso é shader escrito por nós: são efeitos prontos do URP, só ajustados por código.
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
    SplitToning tons;
    ShadowsMidtonesHighlights sombrasELuzes;
    WhiteBalance balancoDeBranco;

    static readonly Color RoxoDaBruxa = new Color(0.12f, 0f, 0.18f);
    static readonly Color Cinza = new Color(0.5f, 0.5f, 0.5f); // no SplitToning, cinza = "não pinta nada"
    static readonly Vector4 Neutro = new Vector4(1f, 1f, 1f, 0f); // no ShadowsMidtonesHighlights, (1,1,1,0) = "não muda nada"

    // Os números de cada clima (só valem no HD-2D).
    struct Visual
    {
        public float limiteDoBloom, forcaDoBloom;
        public Color corDoBloom;
        public Color sombras, luzes;  // SplitToning: cor das sombras e das partes claras
        public float equilibrio;      // SplitToning: >0 pinta mais as partes claras, <0 mais as sombras
        public Vector4 sombrasSMH, luzesSMH; // ShadowsMidtonesHighlights: (r, g, b, claridade)
        public float temperatura, matiz;     // WhiteBalance: >0 mais quente (amarelo), <0 mais frio (azul)
        public float saturacaoExtra;
        public float contrasteExtra;         // somado ao contraste (mais = sombras mais fundas e luzes mais fortes)
        public float exposicao;              // ColorAdjustments.postExposure: <0 escurece a imagem inteira
    }

    // O bloom tem um "limite" de claridade: só o que passa dele brilha. No HD-2D a luz global é mais fraca
    // (Luzes.AjustarAoVisual), então os sprites brancos ficam abaixo de 1; o que passa do limite é o que a
    // LUZ DO SOL (no alto, à esquerda) e as luzinhas (lampiões, lanterna) deixam mais claro que o branco.
    //
    // Visual "v3" (escuro e vivo, à la Octopath): contraste alto, sombras azul-arroxeadas bem fundas,
    // luzes douradas, saturação extra e uma exposição um pouco mais baixa.
    static readonly Visual Dia = new Visual
    {
        limiteDoBloom = 0.98f, forcaDoBloom = 0.6f, corDoBloom = new Color(1f, 0.86f, 0.62f),
        sombras = new Color(0.34f, 0.36f, 0.64f), luzes = new Color(0.7f, 0.57f, 0.38f), equilibrio = 8f,
        sombrasSMH = new Vector4(0.86f, 0.9f, 1.16f, -0.1f), luzesSMH = new Vector4(1.1f, 1.02f, 0.86f, 0.03f),
        temperatura = 10f, matiz = 2f, saturacaoExtra = 12f, contrasteExtra = 16f, exposicao = -0.12f,
    };
    // O fim de tarde das fases de dia: tudo mais quente e dourado, sombras arroxeadas (o "golden hour").
    static readonly Visual Entardecer = new Visual
    {
        limiteDoBloom = 0.92f, forcaDoBloom = 0.75f, corDoBloom = new Color(1f, 0.7f, 0.4f),
        sombras = new Color(0.42f, 0.32f, 0.64f), luzes = new Color(0.8f, 0.52f, 0.28f), equilibrio = 14f,
        sombrasSMH = new Vector4(0.9f, 0.84f, 1.14f, -0.12f), luzesSMH = new Vector4(1.16f, 1f, 0.78f, 0.04f),
        temperatura = 28f, matiz = 8f, saturacaoExtra = 16f, contrasteExtra = 20f, exposicao = -0.18f,
    };
    static readonly Visual Noite = new Visual
    {
        limiteDoBloom = 0.85f, forcaDoBloom = 0.8f, corDoBloom = new Color(0.72f, 0.82f, 1f),
        sombras = new Color(0.3f, 0.34f, 0.66f), luzes = new Color(0.58f, 0.56f, 0.5f), equilibrio = -12f,
        sombrasSMH = new Vector4(0.84f, 0.9f, 1.18f, -0.1f), luzesSMH = new Vector4(1.04f, 1f, 0.94f, 0.02f),
        temperatura = -18f, matiz = 4f, saturacaoExtra = 4f, contrasteExtra = 16f, exposicao = -0.08f,
    };
    static readonly Visual NoEscuro = new Visual
    {
        limiteDoBloom = 0.9f, forcaDoBloom = 0.85f, corDoBloom = new Color(0.9f, 0.82f, 1f),
        sombras = new Color(0.34f, 0.3f, 0.6f), luzes = new Color(0.62f, 0.54f, 0.42f), equilibrio = -15f,
        sombrasSMH = new Vector4(0.88f, 0.86f, 1.14f, -0.06f), luzesSMH = new Vector4(1.06f, 1f, 0.92f, 0.02f),
        temperatura = -8f, matiz = 3f, saturacaoExtra = 6f, contrasteExtra = 14f, exposicao = 0f,
    };
    static readonly Visual Biblioteca = new Visual
    {
        limiteDoBloom = 0.9f, forcaDoBloom = 0.7f, corDoBloom = new Color(1f, 0.78f, 0.5f),
        sombras = new Color(0.4f, 0.32f, 0.52f), luzes = new Color(0.72f, 0.56f, 0.36f), equilibrio = 8f,
        sombrasSMH = new Vector4(0.9f, 0.86f, 1.02f, -0.1f), luzesSMH = new Vector4(1.1f, 1f, 0.84f, 0.03f),
        temperatura = 22f, matiz = 3f, saturacaoExtra = -6f, contrasteExtra = 18f, exposicao = -0.1f,
    };

    // Mistura dois visuais (t = 0: o primeiro; t = 1: o segundo). Usado para passar da manhã ao entardecer.
    static Visual Misturar(Visual a, Visual b, float t)
    {
        return new Visual
        {
            limiteDoBloom = Mathf.Lerp(a.limiteDoBloom, b.limiteDoBloom, t),
            forcaDoBloom = Mathf.Lerp(a.forcaDoBloom, b.forcaDoBloom, t),
            corDoBloom = Color.Lerp(a.corDoBloom, b.corDoBloom, t),
            sombras = Color.Lerp(a.sombras, b.sombras, t),
            luzes = Color.Lerp(a.luzes, b.luzes, t),
            equilibrio = Mathf.Lerp(a.equilibrio, b.equilibrio, t),
            sombrasSMH = Vector4.Lerp(a.sombrasSMH, b.sombrasSMH, t),
            luzesSMH = Vector4.Lerp(a.luzesSMH, b.luzesSMH, t),
            temperatura = Mathf.Lerp(a.temperatura, b.temperatura, t),
            matiz = Mathf.Lerp(a.matiz, b.matiz, t),
            saturacaoExtra = Mathf.Lerp(a.saturacaoExtra, b.saturacaoExtra, t),
            contrasteExtra = Mathf.Lerp(a.contrasteExtra, b.contrasteExtra, t),
            exposicao = Mathf.Lerp(a.exposicao, b.exposicao, t),
        };
    }

    void Awake()
    {
        jogo = GetComponent<GerenciadorDoJogo>();

        var volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        var perfil = ScriptableObject.CreateInstance<VolumeProfile>();

        bloom = perfil.Add<Bloom>(true);
        bloom.threshold.value = 0.97f; // só o que é bem branco brilha, e de leve
        bloom.intensity.value = 0.25f;
        bloom.scatter.value = 0.65f;

        vinheta = perfil.Add<Vignette>(true);
        vinheta.color.value = RoxoDaBruxa;
        vinheta.intensity.value = 0.25f;
        vinheta.smoothness.value = 0.45f;

        cor = perfil.Add<ColorAdjustments>(true);
        aberracao = perfil.Add<ChromaticAberration>(true);
        distorcao = perfil.Add<LensDistortion>(true);
        tons = perfil.Add<SplitToning>(true);
        sombrasELuzes = perfil.Add<ShadowsMidtonesHighlights>(true);
        balancoDeBranco = perfil.Add<WhiteBalance>(true);

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
        bool hd = Opcoes.GraficosHD2D;

        // O clima: no mapa (e no título, que mostra o mapa) é sempre "dia".
        Clima clima = jogo.Mapa != null ? Clima.Dia : Paralaxe.ClimaDa(jogo.DadosDaFase);
        LuzDoDia.Atualizar(jogo, clima); // a hora do dia (manhã -> entardecer) e o sol atrás das nuvens
        Luzes.AjustarAoVisual(hd && jogo.Mapa == null); // no HD-2D a luz do céu é mais fraca (quem brilha é o sol)
        Visual v = clima == Clima.Noite ? Noite : clima == Clima.Escuro ? NoEscuro : clima == Clima.Biblioteca ? Biblioteca
            : Misturar(Dia, Entardecer, LuzDoDia.Entardecer);

        // Bloom: pode ser desligado ou ajustado no painel F10 (AjustesVisuais).
        bloom.active = AjustesVisuais.bloom;

        cor.saturation.value = Mathf.Lerp(saturacao + (hd ? v.saturacaoExtra : 0f), -75f, morte) - (pausa ? 55f : 0f);
        float noHD = hd ? 1f - morte : 0f; // os extras do HD-2D somem ao morrer (a tela fica cinza)
        cor.contrast.value = contraste + morte * 15f + v.contrasteExtra * noHD;
        cor.postExposure.value = (jogo.FaseNoturna ? -0.2f : 0f) + v.exposicao * noHD;
        // (no HD-2D quem dá o tom da fase é o WhiteBalance; no Simples, um filtro de cor como antes)
        cor.colorFilter.value = hd ? Color.white
            : jogo.FaseNoturna ? new Color(0.85f, 0.9f, 1.15f)
            : jogo.NaBiblioteca ? new Color(1.08f, 0.97f, 0.86f)
            : Color.white;

        aberracao.intensity.value = Mathf.Max(morte * 0.85f, renascer * 0.5f);
        distorcao.intensity.value = -0.35f * renascer * renascer;
        vinheta.intensity.value = 0.25f + morte * 0.3f + (jogo.FaseNoturna ? 0.1f : 0f) + (hd ? 0.12f : 0f);

        // A correção de cor de cinema só no HD-2D. Ao morrer (tela cinza) e na pausa ela some aos poucos:
        // "forca" = 1 normal, 0 = nada (os valores neutros).
        tons.active = hd;
        sombrasELuzes.active = hd;
        balancoDeBranco.active = hd;
        if (!hd)
        {
            bloom.threshold.value = 0.97f + AjustesVisuais.limiteDoBloom;
            bloom.intensity.value = 0.25f * AjustesVisuais.forcaDoBloom;
            bloom.scatter.value = 0.65f;
            bloom.tint.value = Color.white;
            return;
        }

        float forca = (1f - morte) * (pausa ? 0.5f : 1f);
        bloom.threshold.value = v.limiteDoBloom + AjustesVisuais.limiteDoBloom;
        bloom.intensity.value = v.forcaDoBloom * (1f - morte * 0.5f) * AjustesVisuais.forcaDoBloom;
        bloom.scatter.value = 0.72f;
        bloom.tint.value = v.corDoBloom;

        tons.shadows.value = Color.Lerp(Cinza, v.sombras, forca);
        tons.highlights.value = Color.Lerp(Cinza, v.luzes, forca);
        tons.balance.value = v.equilibrio;

        sombrasELuzes.shadows.value = Vector4.Lerp(Neutro, v.sombrasSMH, forca);
        sombrasELuzes.highlights.value = Vector4.Lerp(Neutro, v.luzesSMH, forca);

        balancoDeBranco.temperature.value = v.temperatura * forca;
        balancoDeBranco.tint.value = v.matiz * forca;
    }
}
