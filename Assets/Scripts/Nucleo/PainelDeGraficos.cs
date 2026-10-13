using UnityEngine;
using UnityEngine.InputSystem;

// PAINEL DE TESTES DO VISUAL: aperte F10 (ou a tecla à esquerda do 1) durante o jogo.
// Liga, desliga e ajusta cada efeito na hora, com o jogo rodando, e mostra os quadros por segundo (FPS).
// Os valores ficam em AjustesVisuais (e voltam ao normal no próximo Play). Achou uma combinação boa?
// O botão "Copiar valores" põe tudo na área de transferência, para colar e guardar em AjustesVisuais.
//
// Para investigar "travadas":
//  - FPS: quadros por segundo (60 = liso). "pior" = o quadro mais demorado do último meio segundo:
//    se o FPS é bom mas o "pior" passa de 30 ms, o jogo dá TRANCOS de vez em quando;
//  - GC: quantas vezes o coletor de lixo da memória rodou. Se esse número sobe toda hora, algum script
//    está criando lixo a cada quadro (e cada coleta pode causar um tranco).
//
// Usa o IMGUI (OnGUI), como o resto da interface. É desenhado pela Interface, por cima de tudo.
public class PainelDeGraficos : MonoBehaviour
{
    static PainelDeGraficos instancia;

    bool aberto;
    float somaDoTempo, piorQuadro;
    int quadros;
    float fps, piorMostrado;
    int coletasNoInicio, coletas;

    void Awake()
    {
        instancia = this;
        coletasNoInicio = System.GC.CollectionCount(0);
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    void Update()
    {
        var teclado = Keyboard.current;
        if (teclado != null && (teclado.f10Key.wasPressedThisFrame || teclado.backquoteKey.wasPressedThisFrame))
            aberto = !aberto;

        // FPS: média de meio segundo, e o pior quadro nesse tempo (tempo real: funciona até na pausa)
        float dt = Time.unscaledDeltaTime;
        somaDoTempo += dt;
        quadros++;
        piorQuadro = Mathf.Max(piorQuadro, dt);
        if (somaDoTempo >= 0.5f)
        {
            fps = quadros / somaDoTempo;
            piorMostrado = piorQuadro * 1000f;
            somaDoTempo = 0f;
            quadros = 0;
            piorQuadro = 0f;
            coletas = System.GC.CollectionCount(0) - coletasNoInicio;
        }
    }

    // Chamado pela Interface, no fim do OnGUI.
    public static void Desenhar()
    {
        if (instancia != null && instancia.aberto) instancia.DesenharPainel();
    }

    // ------------------------------------------------------------------ o painel

    const float Largura = 360f, AlturaDaLinha = 22f;
    float x, y; // onde está escrevendo agora (em "pixels de 720p": o painel é aumentado junto com a tela)

    void DesenharPainel()
    {
        Matrix4x4 matrizOriginal = GUI.matrix;
        Color corOriginal = GUI.color;
        float escala = Mathf.Max(1f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
        GUI.color = Color.white;

        GUI.Box(new Rect(8f, 8f, Largura + 16f, 532f), "Painel de gráficos (F10 fecha)");
        x = 16f;
        y = 32f;

        Linha($"FPS: {fps:0}   pior quadro: {piorMostrado:0.0} ms   GC: {coletas}");
        Linha("Tela: " + TelaPixelada.Descricao);

        if (GUI.Button(new Rect(x, y, Largura, AlturaDaLinha - 2f), "Gráficos (menu de pausa): " + (Opcoes.GraficosHD2D ? "HD-2D" : "Simples")))
        {
            Opcoes.GraficosHD2D = !Opcoes.GraficosHD2D;
            Paralaxe.Recriar();
        }
        y += AlturaDaLinha + 4f;

        AjustesVisuais.pixelNitido = Botao("Pixel nítido (tela em baixa resolução)", AjustesVisuais.pixelNitido);
        AjustesVisuais.escalaInteira = Botao("   Só escala inteira (2x, 3x, 4x...)", AjustesVisuais.escalaInteira);

        AjustesVisuais.vento = Botao("Vento nas plantas", AjustesVisuais.vento);
        AjustesVisuais.forcaDoVento = Deslizar("   força", AjustesVisuais.forcaDoVento, 0f, 2f);

        AjustesVisuais.raios = Botao("Raios de sol (aditivos) *", AjustesVisuais.raios);
        AjustesVisuais.forcaDosRaios = Deslizar("   força", AjustesVisuais.forcaDosRaios, 0f, 2.5f);

        AjustesVisuais.nuvens = Botao("Sombras de nuvens *", AjustesVisuais.nuvens);
        AjustesVisuais.forcaDasNuvens = Deslizar("   força", AjustesVisuais.forcaDasNuvens, 0f, 2f);
        AjustesVisuais.velocidadeDasNuvens = Deslizar("   velocidade", AjustesVisuais.velocidadeDasNuvens, 0f, 5f);

        AjustesVisuais.bloom = Botao("Bloom (brilho)", AjustesVisuais.bloom);
        AjustesVisuais.forcaDoBloom = Deslizar("   força", AjustesVisuais.forcaDoBloom, 0f, 3f);
        AjustesVisuais.limiteDoBloom = Deslizar("   limite (+ = menos coisa brilha)", AjustesVisuais.limiteDoBloom, -0.5f, 0.5f);

        AjustesVisuais.tiltShift = Botao("Tilt-shift (bordas desfocadas) *", AjustesVisuais.tiltShift);
        AjustesVisuais.tamanhoDoTilt = Deslizar("   tamanho", AjustesVisuais.tamanhoDoTilt, 0f, 0.45f);
        AjustesVisuais.forcaDoTilt = Deslizar("   força", AjustesVisuais.forcaDoTilt, 0f, 1f);

        AjustesVisuais.corPorHora = Botao("Cor pela hora do dia (fases de dia) *", AjustesVisuais.corPorHora);
        float hora = Deslizar(AjustesVisuais.horaManual < 0f ? "   hora: automática" : "   hora (0 manhã, 1 tarde)",
            AjustesVisuais.horaManual, -0.1f, 1f);
        AjustesVisuais.horaManual = hora < 0f ? -1f : hora;

        y += 4f;
        if (GUI.Button(new Rect(x, y, Largura / 2f - 4f, AlturaDaLinha), "Valores de fábrica")) AjustesVisuais.ValoresDeFabrica();
        if (GUI.Button(new Rect(x + Largura / 2f + 4f, y, Largura / 2f - 4f, AlturaDaLinha), "Copiar valores"))
            GUIUtility.systemCopyBuffer = AjustesVisuais.Resumo();
        y += AlturaDaLinha + 4f;
        Linha("* só com os gráficos em HD-2D");

        GUI.matrix = matrizOriginal;
        GUI.color = corOriginal;
    }

    void Linha(string texto)
    {
        GUI.Label(new Rect(x, y, Largura, AlturaDaLinha), texto);
        y += AlturaDaLinha;
    }

    bool Botao(string texto, bool valor)
    {
        bool novo = GUI.Toggle(new Rect(x, y, Largura, AlturaDaLinha), valor, " " + texto);
        y += AlturaDaLinha;
        return novo;
    }

    float Deslizar(string texto, float valor, float minimo, float maximo)
    {
        GUI.Label(new Rect(x, y, 200f, AlturaDaLinha), texto + (valor >= minimo ? $": {valor:0.00}" : ""));
        float novo = GUI.HorizontalSlider(new Rect(x + 205f, y + 6f, Largura - 205f, AlturaDaLinha), valor, minimo, maximo);
        y += AlturaDaLinha;
        return novo;
    }
}
