using System.Collections.Generic;
using System.Text.RegularExpressions;

// As DICAS DE TECLA na tela ("ENTER: jogar", "ESC: voltar", "tecla L"...) viram os botões do CONTROLE quando o
// jogador está usando um (veja Controles.UsandoControle): "A: jogar", "B: voltar", "botão X".
//
// A Interface passa por aqui só os textos que são dica (rodapés, botões de texto e as dicas das caixas de fala),
// já traduzidos: as regras abaixo valem para o português e para o inglês. A página "Como jogar", que lista o
// teclado E o controle, não passa por aqui. Os botões seguem o que o Controles.cs faz de verdade:
//   A = confirmar e pular     B = voltar     X = Loja     Y = Tripulação, reiniciar e apagar slot
//   START = pausar (e, no mapa, voltar ao título)     RB = trocar o comprador na Loja
public static class DicasDoControle
{
    // (a ordem importa: as regras mais específicas vêm antes das gerais)
    static readonly (Regex de, string para)[] Regras =
    {
        Regra(@"A/D ou SETAS", "DIRECIONAL"), Regra(@"A/D or ARROWS", "D-PAD"),
        Regra(@"SETAS ou MOUSE", "DIRECIONAL"), Regra(@"ARROWS or MOUSE", "D-PAD"),
        Regra(@"CIMA/BAIXO ou MOUSE", "CIMA/BAIXO"), Regra(@"UP/DOWN or MOUSE", "UP/DOWN"),
        Regra(@"\bSETAS\b", "DIRECIONAL"), Regra(@"\bARROWS\b", "D-PAD"),
        Regra(@"ESPAÇO/W", "A"), Regra(@"SPACE/W", "A"), Regra(@"\bESPAÇO\b", "A"), Regra(@"\bSPACE\b", "A"),
        Regra(@"ENTER ou CLIQUE", "A"), Regra(@"ENTER or CLICK", "A"),
        Regra(@"ESC ou L\b", "B ou X"), Regra(@"ESC or L\b", "B or X"),
        Regra(@"\bESC: (pausa|pause|título|title)", "START: $1"),
        Regra(@"\bL: (loja|shop)", "X: $1"), Regra(@"\bT: (coop|co-op)", "Y: $1"), Regra(@"\bR: (reiniciar|restart)", "Y: $1"),
        Regra(@"\bDEL: ", "Y: "),
        Regra(@"\btecla L\b", "botão X"), Regra(@"\bL key\b", "X button"),
        Regra(@"\btecla T\b", "botão Y"), Regra(@"\bT key\b", "Y button"),
        Regra(@"\baperte L\b", "aperte X"), Regra(@"\bpress L\b", "press X"),
        Regra(@"\bS: entrar", "BAIXO: entrar"), Regra(@"\bS: enter", "DOWN: enter"),
        Regra(@"\bTAB (troca|switches)", "RB $1"),
        Regra(@"\bENTER\b", "A"), Regra(@"\bESC\b", "B"),
    };

    static (Regex, string) Regra(string padrao, string para) => (new Regex(padrao, RegexOptions.CultureInvariant), para);

    // As dicas já trocadas (trocar de novo a cada quadro criaria lixo na memória).
    static readonly Dictionary<string, string> prontas = new Dictionary<string, string>();

    public static string Trocar(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        if (prontas.TryGetValue(texto, out string pronta)) return pronta;
        string trocada = texto;
        foreach ((Regex de, string para) in Regras) trocada = de.Replace(trocada, para);
        if (prontas.Count > 300) prontas.Clear();
        prontas[texto] = trocada;
        return trocada;
    }
}
