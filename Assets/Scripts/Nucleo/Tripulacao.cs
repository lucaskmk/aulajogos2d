using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// A TRIPULAÇÃO: o coop local, até 4 jogadores no mesmo computador.
//
// Cada jogador é um "marujo" com o seu aparelho: metade do teclado (WASD ou SETAS) ou um controle.
// Sozinho (1 marujo), vale tudo ao mesmo tempo, como sempre foi: teclado inteiro e qualquer controle.
//
// Quem entra e quem sai é decidido na tela da Tripulação (tecla T no mapa): cada um aperta PULAR no seu
// aparelho para entrar e BALÃO para sair. A ordem de entrada é a ordem dos jogadores (P1, P2, P3, P4).
//
// As teclas de cada aparelho:
//   TECLADO (WASD):  A/D anda, W ou ESPAÇO pula, S entra na porta, E empurra, Q (segurando) vira balão
//   TECLADO (SETAS): setas andam, CIMA pula, BAIXO entra na porta, SHIFT direito empurra, CTRL direito (segurando) vira balão
//   CONTROLE:        direcional/analógico anda, A pula, BAIXO entra na porta, X empurra, B (segurando) vira balão
// Se só UM marujo usa o teclado, ele fica com o teclado inteiro (WASD e setas).
public static class Tripulacao
{
    public const int Maximo = 4;

    public enum Esquema { Todos, TecladoWASD, TecladoSetas, Controle }

    // A cor de cada jogador (a bandana, a etiqueta em cima da cabeça, o balão e o placar).
    public static readonly Color32[] Cores =
    {
        new Color32(238, 76, 80, 255),   // P1: vermelho
        new Color32(86, 148, 244, 255),  // P2: azul
        new Color32(104, 212, 92, 255),  // P3: verde
        new Color32(255, 214, 80, 255),  // P4: amarelo
    };

    public static string Nome(int indice) => "P" + (indice + 1);

    // O desenho do Grumete de cada jogador (ArteDosPersonagens registra as bandanas coloridas).
    public static string Sprite(int indice, bool andando = false) =>
        (indice <= 0 ? "jogador" : "jogador_" + (indice + 1)) + (andando ? "_andando" : "");

    public class Marujo
    {
        public Esquema esquema = Esquema.Todos;
        public Gamepad controle;
        public bool tecladoInteiro; // é o único no teclado: fica com WASD e setas

        // O placar da fase (zera ao entrar pelo mapa): vira os títulos no fim da fase.
        public int mortes, moedas, resgates, empurroes;
        // Quantas vezes este jogador morreu desde que a partida foi aberta (o número ao lado do rostinho dele).
        public int mortesNaPartida;

        public void ZerarPlacar() => mortes = moedas = resgates = empurroes = 0;

        bool Wasd => esquema == Esquema.Todos || esquema == Esquema.TecladoWASD || (esquema == Esquema.TecladoSetas && tecladoInteiro);
        bool Setas => esquema == Esquema.Todos || esquema == Esquema.TecladoSetas || (esquema == Esquema.TecladoWASD && tecladoInteiro);
        Gamepad Controle => esquema == Esquema.Todos ? Gamepad.current
            : esquema == Esquema.Controle && controle != null && controle.added ? controle : null;

        public bool NoTeclado => esquema == Esquema.TecladoWASD || esquema == Esquema.TecladoSetas;

        public float Horizontal()
        {
            float x = 0f;
            Keyboard teclado = Keyboard.current;
            if (teclado != null)
            {
                // (as teclas do "teclado 1" são as de Opções > Teclas: veja Teclas.cs)
                if ((Wasd && Teclas.Segurando(Teclas.Acao.Esquerda)) || (Setas && teclado.leftArrowKey.isPressed)) x -= 1f;
                if ((Wasd && Teclas.Segurando(Teclas.Acao.Direita)) || (Setas && teclado.rightArrowKey.isPressed)) x += 1f;
            }
            Gamepad c = Controle;
            if (c != null && x == 0f)
            {
                float analogico = c.leftStick.x.ReadValue();
                if (Mathf.Abs(analogico) > 0.3f) x = Mathf.Sign(analogico);
                if (c.dpad.left.isPressed) x = -1f;
                if (c.dpad.right.isPressed) x = 1f;
            }
            return x;
        }

        public bool PuloApertou()
        {
            Keyboard teclado = Keyboard.current;
            Gamepad c = Controle;
            return (teclado != null && ((Wasd && (Teclas.Apertou(Teclas.Acao.Pular) || Teclas.Apertou(Teclas.Acao.PularDeNovo)))
                                        || (Setas && teclado.upArrowKey.wasPressedThisFrame)))
                || (c != null && c.buttonSouth.wasPressedThisFrame);
        }

        public bool PuloSegurando()
        {
            Keyboard teclado = Keyboard.current;
            Gamepad c = Controle;
            return (teclado != null && ((Wasd && (Teclas.Segurando(Teclas.Acao.Pular) || Teclas.Segurando(Teclas.Acao.PularDeNovo)))
                                        || (Setas && teclado.upArrowKey.isPressed)))
                || (c != null && c.buttonSouth.isPressed);
        }

        // Entrar nas portas das cabines.
        public bool BaixoApertou()
        {
            Keyboard teclado = Keyboard.current;
            Gamepad c = Controle;
            return (teclado != null && ((Wasd && Teclas.Apertou(Teclas.Acao.Baixo)) || (Setas && teclado.downArrowKey.wasPressedThisFrame)))
                || (c != null && (c.dpad.down.wasPressedThisFrame || c.leftStick.down.wasPressedThisFrame));
        }

        // Empurrar o companheiro (só existe no coop).
        public bool EmpurraoApertou()
        {
            Keyboard teclado = Keyboard.current;
            Gamepad c = Controle;
            return (teclado != null && ((Wasd && Teclas.Apertou(Teclas.Acao.Empurrar)) || (Setas && teclado.rightShiftKey.wasPressedThisFrame)))
                || (c != null && c.buttonWest.wasPressedThisFrame);
        }

        // Virar balão de propósito (segurando) e, na tela da Tripulação, sair dela.
        public bool BalaoSegurando()
        {
            Keyboard teclado = Keyboard.current;
            Gamepad c = Controle;
            return (teclado != null && ((Wasd && Teclas.Segurando(Teclas.Acao.Balao)) || (Setas && teclado.rightCtrlKey.isPressed)))
                || (c != null && c.buttonEast.isPressed);
        }

        // O nome do aparelho (na tela da Tripulação).
        public string Aparelho()
        {
            switch (esquema)
            {
                case Esquema.TecladoWASD: return tecladoInteiro ? Idioma.T("TECLADO", "KEYBOARD") : Idioma.T("TECLADO (WASD)", "KEYBOARD (WASD)");
                case Esquema.TecladoSetas: return tecladoInteiro ? Idioma.T("TECLADO", "KEYBOARD") : Idioma.T("TECLADO (SETAS)", "KEYBOARD (ARROWS)");
                case Esquema.Controle:
                    int numero = 0;
                    for (int i = 0; i < Gamepad.all.Count; i++)
                        if (Gamepad.all[i] == controle) numero = i + 1;
                    return numero > 0 ? Idioma.T("CONTROLE ", "GAMEPAD ") + numero : Idioma.T("CONTROLE (desligado)", "GAMEPAD (off)");
                default: return Idioma.T("TECLADO OU CONTROLE", "KEYBOARD OR GAMEPAD");
            }
        }

        // As teclas deste aparelho, numa linha (na tela da Tripulação).
        public string TeclasDoAparelho()
        {
            switch (esquema)
            {
                case Esquema.TecladoWASD: return Idioma.T("W pula   E empurra   Q balão", "W jump   E shove   Q balloon");
                case Esquema.TecladoSetas: return Idioma.T("CIMA pula  SHIFT empurra  CTRL balão", "UP jump  SHIFT shove  CTRL balloon");
                case Esquema.Controle: return Idioma.T("A pula   X empurra   B balão", "A jump   X shove   B balloon");
                default: return "";
            }
        }
    }

    static readonly List<Marujo> marujos = new List<Marujo> { new Marujo() };

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar()
    {
        marujos.Clear();
        marujos.Add(new Marujo());
        LobbyAberto = false;
    }

    public static int Quantos => Mathf.Max(1, marujos.Count);
    public static bool Coop => marujos.Count > 1;
    // (com a tela da Tripulação aberta a lista pode estar vazia: aí devolve um marujo "de qualquer aparelho")
    public static Marujo Do(int indice) => indice >= 0 && indice < marujos.Count ? marujos[indice] : Reserva;
    static readonly Marujo Reserva = new Marujo();

    public static void ZerarPlacares()
    {
        foreach (Marujo marujo in marujos) marujo.ZerarPlacar();
    }

    public static void ZerarMortesNaPartida()
    {
        foreach (Marujo marujo in marujos) marujo.mortesNaPartida = 0;
    }

    // ------------------------------------------------------------------ a tela da Tripulação

    public static bool LobbyAberto { get; private set; }
    // Quantos marujos estão na tela da Tripulação (ali pode ser 0: ninguém apertou PULAR ainda).
    public static int NoLobby => marujos.Count;

    // Abre a tela. Jogando sozinho, ela começa vazia: cada um aperta PULAR no seu aparelho, na ordem.
    public static void AbrirLobby()
    {
        LobbyAberto = true;
        if (!Coop) marujos.Clear();
    }

    // Fecha a tela. Ninguém ou só um entrou? Volta a ser o jogo de um jogador (qualquer aparelho serve).
    public static void FecharLobby()
    {
        LobbyAberto = false;
        if (marujos.Count == 0) marujos.Add(new Marujo());
        if (marujos.Count == 1)
        {
            marujos[0].esquema = Esquema.Todos;
            marujos[0].controle = null;
        }
        Recalcular();
    }

    // Um quadro da tela: quem apertou PULAR num aparelho livre entra; quem apertou BALÃO sai.
    // Devolve +1 se alguém entrou, -1 se alguém saiu e 0 se nada mudou (para o som).
    public static int AtualizarLobby()
    {
        int mudou = 0;
        Keyboard teclado = Keyboard.current;

        // sair (antes de entrar: o mesmo aperto nunca faz as duas coisas)
        for (int i = marujos.Count - 1; i >= 0; i--)
        {
            Marujo m = marujos[i];
            bool sair = m.esquema == Esquema.Controle
                ? m.controle == null || !m.controle.added || m.controle.buttonEast.wasPressedThisFrame
                : teclado != null && (m.esquema == Esquema.TecladoWASD ? Teclas.Apertou(Teclas.Acao.Balao) : teclado.rightCtrlKey.wasPressedThisFrame);
            if (!sair) continue;
            marujos.RemoveAt(i);
            mudou = -1;
        }

        if (teclado != null)
        {
            if ((Teclas.Apertou(Teclas.Acao.Pular) || Teclas.Apertou(Teclas.Acao.PularDeNovo)) && !Tem(Esquema.TecladoWASD) && Entrar(Esquema.TecladoWASD, null)) mudou = 1;
            if (teclado.upArrowKey.wasPressedThisFrame && !Tem(Esquema.TecladoSetas) && Entrar(Esquema.TecladoSetas, null)) mudou = 1;
        }
        foreach (Gamepad controle in Gamepad.all)
            if (controle.buttonSouth.wasPressedThisFrame && !TemControle(controle) && Entrar(Esquema.Controle, controle)) mudou = 1;

        if (mudou != 0) Recalcular();
        return mudou;
    }

    static bool Entrar(Esquema esquema, Gamepad controle)
    {
        if (marujos.Count >= Maximo) return false;
        marujos.Add(new Marujo { esquema = esquema, controle = controle });
        return true;
    }

    static bool Tem(Esquema esquema)
    {
        foreach (Marujo m in marujos)
            if (m.esquema == esquema) return true;
        return false;
    }

    static bool TemControle(Gamepad controle)
    {
        foreach (Marujo m in marujos)
            if (m.esquema == Esquema.Controle && m.controle == controle) return true;
        return false;
    }

    // Só um marujo no teclado? Ele fica com o teclado inteiro.
    static void Recalcular()
    {
        int noTeclado = 0;
        foreach (Marujo m in marujos)
            if (m.NoTeclado) noTeclado++;
        foreach (Marujo m in marujos) m.tecladoInteiro = m.NoTeclado && noTeclado == 1;
    }

    // Para os testes (e para quem quiser montar a tripulação por código): troca a tripulação inteira de uma vez.
    public static void Definir(params Marujo[] novos)
    {
        marujos.Clear();
        foreach (Marujo m in novos)
            if (marujos.Count < Maximo) marujos.Add(m);
        LobbyAberto = false;
        FecharLobby();
    }
}
