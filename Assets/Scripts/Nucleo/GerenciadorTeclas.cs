using UnityEngine;
using UnityEngine.InputSystem;

// A tela de TROCAR AS TECLAS (Opções > Teclas): a outra metade do GerenciadorDoJogo.cs. As regras de quais teclas
// podem ser trocadas ficam em Teclas.cs; quem desenha é a Interface (InterfaceTeclas.cs).
//
// CIMA/BAIXO escolhem a ação; ENTER (ou clique) pede a tecla nova ("aperte a tecla..."); a próxima tecla apertada
// vira a tecla da ação (ESC cancela). Embaixo: "Restaurar o padrão" e "Voltar".
public partial class GerenciadorDoJogo
{
    public int OpcaoDasTeclas { get; private set; }
    public bool EsperandoTecla { get; private set; }
    // As linhas da tela: uma por ação, depois "Restaurar o padrão" e "Voltar".
    public static int LinhaDeRestaurar => Teclas.Total;
    public static int LinhaDeVoltarDasTeclas => Teclas.Total + 1;
    public static int TotalDeLinhasDasTeclas => Teclas.Total + 2;
    public string AvisoDasTeclas { get; private set; } = "";
    float timerDoAvisoDasTeclas;

    void AbrirTeclas()
    {
        SubmenuAberto = Submenu.Teclas;
        OpcaoDasTeclas = 0;
        EsperandoTecla = false;
        AvisoDasTeclas = "";
        Som("moeda", 0.5f);
    }

    void AvisarNasTeclas(string texto)
    {
        AvisoDasTeclas = texto;
        timerDoAvisoDasTeclas = 2.5f;
    }

    void AtualizarTeclas(int clique)
    {
        timerDoAvisoDasTeclas -= Time.unscaledDeltaTime;
        if (timerDoAvisoDasTeclas <= 0f) AvisoDasTeclas = "";

        if (EsperandoTecla)
        {
            Keyboard teclado = Keyboard.current;
            if (teclado != null && teclado.escapeKey.wasPressedThisFrame) // ESC: desiste de trocar
            {
                EsperandoTecla = false;
                Som("bloco", 0.4f);
                return;
            }
            Key tecla = Teclas.ApertadaAgora();
            if (tecla == Key.None) return;
            var acao = (Teclas.Acao)OpcaoDasTeclas;
            if (!Teclas.Definir(acao, tecla))
            {
                AvisarNasTeclas(Idioma.T("Essa tecla não pode (setas, ENTER e ESC são dos menus e do 2º jogador).",
                                         "That key is not allowed (arrows, ENTER and ESC belong to the menus and player 2)."));
                Som("bloco", 0.6f);
                return;
            }
            EsperandoTecla = false;
            AvisarNasTeclas(Teclas.Descricao(acao) + ": " + Teclas.Nome(acao));
            Som("moeda", 0.6f);
            return;
        }

        if (Controles.Voltar() || clique == BotaoVoltar)
        {
            SubmenuAberto = Submenu.Opcoes; // volta para as Opções, de onde veio
            Som("bloco", 0.4f);
            return;
        }
        if (Controles.CimaApertou()) OpcaoDasTeclas = Girar(OpcaoDasTeclas, -1, TotalDeLinhasDasTeclas);
        if (Controles.BaixoApertou()) OpcaoDasTeclas = Girar(OpcaoDasTeclas, 1, TotalDeLinhasDasTeclas);

        bool escolheu = Controles.Confirmar();
        if (clique >= 0 && clique < TotalDeLinhasDasTeclas)
        {
            OpcaoDasTeclas = clique;
            escolheu = true;
        }
        if (!escolheu) return;

        if (OpcaoDasTeclas == LinhaDeVoltarDasTeclas)
        {
            SubmenuAberto = Submenu.Opcoes;
            Som("bloco", 0.4f);
        }
        else if (OpcaoDasTeclas == LinhaDeRestaurar)
        {
            Teclas.Restaurar();
            AvisarNasTeclas(Idioma.T("Teclas de volta ao padrão.", "Keys back to default."));
            Som("moeda", 0.6f);
        }
        else
        {
            EsperandoTecla = true; // (a tecla é lida a partir do PRÓXIMO quadro: o ENTER deste não conta)
            Som("moeda", 0.4f);
        }
    }
}
