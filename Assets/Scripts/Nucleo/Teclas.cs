using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// AS TECLAS QUE DÁ PARA TROCAR (Opções > Teclas). São as do "teclado 1": andar, pular, porta, reiniciar, Loja,
// coop, empurrar e balão. Ficam guardadas com as outras opções (Guardado), valem para o jogo todo.
//
// O que NÃO troca, de propósito:
//  - as SETAS, o SHIFT e o CTRL da direita: são o "teclado 2" (o segundo jogador no mesmo teclado);
//  - ENTER, ESC e as teclas dos menus: senão dava para ficar preso num menu;
//  - os botões do controle.
// Trocar uma tecla por outra que já está em uso faz as duas ações trocarem de tecla (nunca ficam duas iguais).
public static class Teclas
{
    public enum Acao { Esquerda, Direita, Pular, PularDeNovo, Baixo, Reiniciar, Loja, Coop, Empurrar, Balao }
    public static readonly int Total = System.Enum.GetValues(typeof(Acao)).Length;

    static readonly Key[] Padrao = { Key.A, Key.D, Key.W, Key.Space, Key.S, Key.R, Key.L, Key.T, Key.E, Key.Q };
    static Key[] atuais;
    // Muda a cada troca (quem guarda textos com o nome das teclas refaz quando isto muda).
    public static int Versao { get; private set; }

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => atuais = null;

    static string Chave(int acao) => "cilada_tecla_" + (Acao)acao;

    static void Carregar()
    {
        if (atuais != null) return;
        atuais = new Key[Total];
        for (int i = 0; i < Total; i++)
        {
            int guardada = Guardado.GetInt(Chave(i), (int)Padrao[i]);
            atuais[i] = System.Enum.IsDefined(typeof(Key), guardada) && !Proibida((Key)guardada) ? (Key)guardada : Padrao[i];
        }
    }

    public static Key De(Acao acao)
    {
        Carregar();
        return atuais[(int)acao];
    }

    public static bool Mudaram
    {
        get
        {
            Carregar();
            for (int i = 0; i < Total; i++)
                if (atuais[i] != Padrao[i]) return true;
            return false;
        }
    }

    static KeyControl Controle(Acao acao)
    {
        Keyboard teclado = Keyboard.current;
        return teclado != null ? teclado[De(acao)] : null;
    }

    public static bool Apertou(Acao acao) => Controle(acao)?.wasPressedThisFrame ?? false;
    public static bool Segurando(Acao acao) => Controle(acao)?.isPressed ?? false;

    // Teclas que não podem ser escolhidas (veja o comentário lá em cima).
    public static bool Proibida(Key tecla)
    {
        switch (tecla)
        {
            case Key.None:
            case Key.Escape:
            case Key.Enter:
            case Key.NumpadEnter:
            case Key.Tab:
            case Key.Backspace:
            case Key.Delete:
            case Key.UpArrow:
            case Key.DownArrow:
            case Key.LeftArrow:
            case Key.RightArrow:
            case Key.RightShift:
            case Key.RightCtrl:
            case Key.LeftWindows:
            case Key.RightWindows:
            case Key.PrintScreen:
                return true;
        }
        return tecla >= Key.F1 && tecla <= Key.F12;
    }

    // Troca a tecla de uma ação. Se outra ação usava essa tecla, ela fica com a antiga desta. Devolve false se a tecla é proibida.
    public static bool Definir(Acao acao, Key tecla)
    {
        Carregar();
        if (Proibida(tecla)) return false;
        Key antiga = atuais[(int)acao];
        for (int i = 0; i < Total; i++)
            if (i != (int)acao && atuais[i] == tecla) atuais[i] = antiga;
        atuais[(int)acao] = tecla;
        Guardar();
        return true;
    }

    public static void Restaurar()
    {
        Carregar();
        for (int i = 0; i < Total; i++) atuais[i] = Padrao[i];
        Guardar();
    }

    static void Guardar()
    {
        for (int i = 0; i < Total; i++) Guardado.SetInt(Chave(i), (int)atuais[i]);
        Guardado.Save();
        Versao++;
    }

    // A tecla que o jogador acabou de apertar (para a tela de trocar). Key.None = nenhuma neste quadro.
    public static Key ApertadaAgora()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null || !teclado.anyKey.wasPressedThisFrame) return Key.None;
        foreach (KeyControl controle in teclado.allKeys)
            if (controle != null && controle.wasPressedThisFrame) return controle.keyCode;
        return Key.None;
    }

    // O nome da tecla como aparece na tela ("A", "ESPAÇO", "NUM 5"...).
    public static string Nome(Key tecla)
    {
        switch (tecla)
        {
            case Key.Space: return Idioma.T("ESPAÇO", "SPACE");
            case Key.LeftShift: return "SHIFT";
            case Key.LeftCtrl: return "CTRL";
            case Key.LeftAlt: return "ALT";
            case Key.RightAlt: return "ALT GR";
            case Key.CapsLock: return "CAPS";
        }
        Keyboard teclado = Keyboard.current;
        string nome = teclado != null ? teclado[tecla].displayName : null;
        if (string.IsNullOrEmpty(nome)) nome = tecla.ToString();
        return nome.ToUpperInvariant();
    }

    public static string Nome(Acao acao) => Nome(De(acao));

    // O que cada ação faz (na tela de trocar as teclas).
    public static string Descricao(Acao acao)
    {
        switch (acao)
        {
            case Acao.Esquerda: return Idioma.T("Andar para a esquerda", "Walk left");
            case Acao.Direita: return Idioma.T("Andar para a direita", "Walk right");
            case Acao.Pular: return Idioma.T("Pular", "Jump");
            case Acao.PularDeNovo: return Idioma.T("Pular (outra tecla)", "Jump (second key)");
            case Acao.Baixo: return Idioma.T("Entrar na porta", "Enter a door");
            case Acao.Reiniciar: return Idioma.T("Reiniciar do save", "Restart from the save");
            case Acao.Loja: return Idioma.T("Loja (no mapa)", "Shop (on the map)");
            case Acao.Coop: return Idioma.T("Tripulação / coop (no mapa)", "Crew / co-op (on the map)");
            case Acao.Empurrar: return Idioma.T("Empurrar (coop)", "Shove (co-op)");
            default: return Idioma.T("Balão (coop, segurando)", "Balloon (co-op, hold)");
        }
    }

    // As dicas da tela que citam uma tecla que foi trocada ("L: loja", "tecla T", "R: reiniciar", "S: entrar")
    // passam a mostrar a tecla nova. (texto já traduzido; só é chamado se alguma tecla mudou)
    static readonly Regex DicaDaLoja = new Regex(@"\bL: (loja|shop)"), DicaDoCoop = new Regex(@"\bT: (coop|co-op)"),
        DicaDeReiniciar = new Regex(@"\bR: (reiniciar|restart)"), DicaDaPorta = new Regex(@"\bS: (entrar|enter)"),
        TeclaDaLoja = new Regex(@"\b(tecla L|L key)\b"), TeclaDoCoop = new Regex(@"\b(tecla T|T key)\b"),
        Aperte = new Regex(@"\b(aperte|press) L\b");

    public static string TrocarNasDicas(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        string loja = Nome(Acao.Loja), coop = Nome(Acao.Coop);
        texto = DicaDaLoja.Replace(texto, loja + ": $1");
        texto = DicaDoCoop.Replace(texto, coop + ": $1");
        texto = DicaDeReiniciar.Replace(texto, Nome(Acao.Reiniciar) + ": $1");
        texto = DicaDaPorta.Replace(texto, Nome(Acao.Baixo) + ": $1");
        texto = TeclaDaLoja.Replace(texto, m => m.Value.StartsWith("tecla") ? "tecla " + loja : loja + " key");
        texto = TeclaDoCoop.Replace(texto, m => m.Value.StartsWith("tecla") ? "tecla " + coop : coop + " key");
        texto = Aperte.Replace(texto, "$1 " + loja);
        return texto;
    }
}
