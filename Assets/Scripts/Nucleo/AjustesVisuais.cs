using UnityEngine;

// Os "botões" de cada efeito visual, num lugar só. Quem lê: TelaPixelada (pixel nítido e tilt-shift),
// Vento, FeixesDeLuz (raios de sol), SombrasDeNuvens e EfeitosDeTela (bloom e cor por hora do dia).
// Quem muda: o painel de testes (tecla F10, veja PainelDeGraficos). Os valores de fábrica estão aqui embaixo:
// achou um valor bom no painel? É só copiar (o painel tem um botão que copia tudo) e colar aqui.
//
// A opção do menu de pausa "Gráficos: HD-2D / Simples" (Opcoes.GraficosHD2D) continua mandando:
// no Simples, os efeitos pesados ficam desligados mesmo que estejam ligados aqui.
public static class AjustesVisuais
{
    // Tela em baixa resolução (16 pixels por bloco), aumentada com escala inteira: pixel nítido (TelaPixelada).
    public static bool pixelNitido;
    // true = só aumenta 2x, 3x, 4x... (pode sobrar uma faixa em cima e embaixo); false = estica até preencher.
    public static bool escalaInteira;

    public static bool vento;            // plantas balançando (Vento)
    public static float forcaDoVento;    // 0 = parado, 1 = normal, 2 = ventania

    public static bool raios;            // raios de sol aditivos (FeixesDeLuz)
    public static float forcaDosRaios;

    public static bool nuvens;           // sombras de nuvens passando pela fase (SombrasDeNuvens)
    public static float forcaDasNuvens;  // 1 = escurece uns 30% onde a nuvem passa
    public static float velocidadeDasNuvens;

    public static bool bloom;            // brilho em volta do que é bem claro (EfeitosDeTela)
    public static float forcaDoBloom;
    public static float limiteDoBloom;   // somado ao limite de cada clima (+ = só o que é MAIS claro brilha)

    public static bool tiltShift;        // desfoque nas faixas de cima e de baixo (ar de maquete: TelaPixelada)
    public static float tamanhoDoTilt;   // altura de cada faixa desfocada (0,22 = 22% da tela)
    public static float forcaDoTilt;     // 0 a 1

    public static bool corPorHora;       // fases de dia: manhã no começo, entardecer dourado no fim
    public static float horaManual;      // -1 = automática (pela posição do Subaru); 0 a 1 = força uma hora

    public static bool mostrarFPS;       // contador de quadros por segundo (no painel)

    // Os valores de fábrica. Necessário também porque o projeto está com "Enter Play Mode Options"
    // (sem recarregar o domínio): sem isso, o que mudou no painel ficaria para o próximo Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ValoresDeFabrica()
    {
        pixelNitido = true;
        escalaInteira = true;
        vento = true; forcaDoVento = 1f;
        raios = true; forcaDosRaios = 1f;
        nuvens = true; forcaDasNuvens = 1f; velocidadeDasNuvens = 1f;
        bloom = true; forcaDoBloom = 1f; limiteDoBloom = 0f;
        tiltShift = true; tamanhoDoTilt = 0.22f; forcaDoTilt = 0.85f;
        corPorHora = true; horaManual = -1f;
        mostrarFPS = false;
    }

    static AjustesVisuais() => ValoresDeFabrica();

    // O que vale de verdade (junta o painel com a opção HD-2D / Simples do menu de pausa).
    public static bool RaiosLigados => raios && Opcoes.GraficosHD2D;
    public static bool NuvensLigadas => nuvens && Opcoes.GraficosHD2D;
    public static bool TiltShiftLigado => tiltShift && Opcoes.GraficosHD2D;
    public static bool CorPorHoraLigada => corPorHora && Opcoes.GraficosHD2D;

    // Texto com todos os valores (o botão "Copiar valores" do painel).
    public static string Resumo()
    {
        return "AjustesVisuais: "
            + $"pixelNitido={pixelNitido}, escalaInteira={escalaInteira}, "
            + $"vento={vento} forca={forcaDoVento:0.00}, "
            + $"raios={raios} forca={forcaDosRaios:0.00}, "
            + $"nuvens={nuvens} forca={forcaDasNuvens:0.00} velocidade={velocidadeDasNuvens:0.00}, "
            + $"bloom={bloom} forca={forcaDoBloom:0.00} limite={limiteDoBloom:+0.00;-0.00}, "
            + $"tiltShift={tiltShift} tamanho={tamanhoDoTilt:0.00} forca={forcaDoTilt:0.00}, "
            + $"corPorHora={corPorHora} horaManual={horaManual:0.00}, "
            + $"graficos={(Opcoes.GraficosHD2D ? "HD-2D" : "Simples")}";
    }
}
