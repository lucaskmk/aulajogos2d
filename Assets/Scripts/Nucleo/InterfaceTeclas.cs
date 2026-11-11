using UnityEngine;

// A tela de TROCAR AS TECLAS (a outra metade da Interface.cs; a lógica fica em GerenciadorTeclas.cs e Teclas.cs).
public partial class Interface
{
    readonly string[] textosDasTeclas = new string[Teclas.Total];
    int versaoDosTextosDasTeclas = -1;
    Idioma.Lingua idiomaDasTeclas;

    void DesenharTeclas()
    {
        float w = Screen.width, h = Screen.height;
        Texto(Idioma.T("TECLAS", "KEYS"), w / 2f, h * 0.04f, 6f, 0.5f, Lilas);

        // os textos ("Pular: W") só são montados de novo quando uma tecla (ou o idioma) muda
        if (versaoDosTextosDasTeclas != Teclas.Versao || idiomaDasTeclas != Idioma.Atual)
        {
            versaoDosTextosDasTeclas = Teclas.Versao;
            idiomaDasTeclas = Idioma.Atual;
            for (int i = 0; i < Teclas.Total; i++)
                textosDasTeclas[i] = Teclas.Descricao((Teclas.Acao)i) + ":  " + Teclas.Nome((Teclas.Acao)i);
        }

        float passo = Mathf.Min(38f * escala, h * 0.62f / GerenciadorDoJogo.TotalDeLinhasDasTeclas), y = h * 0.19f;
        for (int i = 0; i < Teclas.Total; i++)
        {
            bool escolhida = i == jogo.OpcaoDasTeclas;
            string texto = escolhida && jogo.EsperandoTecla
                ? Teclas.Descricao((Teclas.Acao)i) + ":  " + Idioma.T("aperte a tecla nova...", "press the new key...")
                : textosDasTeclas[i];
            OpcaoDeMenu(texto, y, 2.4f, passo, i, escolhida);
            y += passo;
        }
        y += passo * 0.3f;
        OpcaoDeMenu(Idioma.T("Restaurar o padrão", "Restore defaults"), y, 2.4f, passo, GerenciadorDoJogo.LinhaDeRestaurar,
            jogo.OpcaoDasTeclas == GerenciadorDoJogo.LinhaDeRestaurar);
        y += passo;
        OpcaoDeMenu("Voltar", y, 2.4f, passo, GerenciadorDoJogo.LinhaDeVoltarDasTeclas, jogo.OpcaoDasTeclas == GerenciadorDoJogo.LinhaDeVoltarDasTeclas);

        if (jogo.AvisoDasTeclas != "") Texto(jogo.AvisoDasTeclas, w / 2f, h * 0.86f, 2f, 0.5f, Verde, w * 0.9f);
        else
            Texto(Idioma.T("As setas, o SHIFT e o CTRL da direita são do 2º jogador no teclado e não mudam.",
                           "The arrows, right SHIFT and right CTRL belong to player 2 on the keyboard and do not change."),
                w / 2f, h * 0.86f, 1.8f, 0.5f, CorDoRodape, w * 0.95f);
        Rodape(jogo.EsperandoTecla ? Idioma.T("aperte a tecla nova    ESC: cancelar", "press the new key    ESC: cancel")
            : Idioma.T("CIMA/BAIXO: escolher    ENTER: trocar a tecla    ESC: voltar", "UP/DOWN: choose    ENTER: change the key    ESC: back"));
    }
}
