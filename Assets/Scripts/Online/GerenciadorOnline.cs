using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// A tela JOGAR ONLINE do título (a outra metade do GerenciadorDoJogo.cs; quem desenha é InterfaceOnline.cs).
//
//   Início:  Criar sala (ou Ver a sala) / Entrar numa sala / Convidar pela Steam (Remote Play Together) / Voltar
//   Sala:    o código para mandar aos amigos, quem está dentro, a qualidade da imagem, copiar o código, fechar a sala
//   Entrar:  digitar o código (ou o IP), as salas achadas na rede de casa, Entrar
//
// Na tela de digitar, as letras viram texto: só as SETAS andam (W, A, S e D aparecem nos códigos).
public partial class GerenciadorDoJogo
{
    public enum TelaOnline { Inicio, Sala, Entrar }
    public enum ItemOnline { CriarSala, VerSala, EntrarNumaSala, RemotePlay, Voltar, CopiarCodigo, Qualidade, FecharSala, Digitar, Conectar, SalaDaRede }

    public struct LinhaOnline
    {
        public ItemOnline item;
        public int sala; // (SalaDaRede: qual da lista)
    }

    public TelaOnline TelaDoOnline { get; private set; }
    public int OpcaoDoOnline { get; private set; }
    public string DigitadoNoOnline { get; private set; } = "";
    public string AvisoDoOnline { get; private set; } = "";
    public readonly List<LinhaOnline> linhasDoOnline = new List<LinhaOnline>();
    public List<BuscaNaRede.Sala> SalasNaRede { get; private set; } = new List<BuscaNaRede.Sala>();
    float timerDoAvisoOnline;
    BuscaNaRede busca;
    bool eraConvidado, escutandoLetras;

    void AbrirOnline()
    {
        SubmenuAberto = Submenu.Online;
        IrParaTelaOnline(Anfitriao.Aberto ? TelaOnline.Sala : TelaOnline.Inicio);
        Som("moeda", 0.5f);
    }

    void IrParaTelaOnline(TelaOnline tela)
    {
        TelaDoOnline = tela;
        OpcaoDoOnline = 0;
        if (tela == TelaOnline.Entrar)
        {
            busca ??= new BuscaNaRede();
            if (!escutandoLetras && Keyboard.current != null)
            {
                Keyboard.current.onTextInput += LetraDigitada;
                escutandoLetras = true;
            }
        }
        else PararDeEscutar();
        MontarLinhasDoOnline();
    }

    void PararDeEscutar()
    {
        busca?.Dispose();
        busca = null;
        if (escutandoLetras && Keyboard.current != null) Keyboard.current.onTextInput -= LetraDigitada;
        escutandoLetras = false;
    }

    void FecharOnline()
    {
        PararDeEscutar();
        SubmenuAberto = Submenu.Nenhum;
        Som("bloco", 0.4f);
    }

    void AvisarNoOnline(string texto, float tempo = 4f)
    {
        AvisoDoOnline = texto;
        timerDoAvisoOnline = tempo;
    }

    void MontarLinhasDoOnline()
    {
        linhasDoOnline.Clear();
        void Por(ItemOnline item, int sala = 0) => linhasDoOnline.Add(new LinhaOnline { item = item, sala = sala });
        switch (TelaDoOnline)
        {
            case TelaOnline.Inicio:
                Por(Anfitriao.Aberto ? ItemOnline.VerSala : ItemOnline.CriarSala);
                Por(ItemOnline.EntrarNumaSala);
                if (Steam.Ligada) Por(ItemOnline.RemotePlay);
                Por(ItemOnline.Voltar);
                break;
            case TelaOnline.Sala:
                Por(ItemOnline.CopiarCodigo);
                Por(ItemOnline.Qualidade);
                if (Steam.Ligada) Por(ItemOnline.RemotePlay);
                Por(ItemOnline.FecharSala);
                Por(ItemOnline.Voltar);
                break;
            case TelaOnline.Entrar:
                Por(ItemOnline.Digitar);
                Por(ItemOnline.Conectar);
                for (int i = 0; i < SalasNaRede.Count && i < 4; i++) Por(ItemOnline.SalaDaRede, i);
                Por(ItemOnline.Voltar);
                break;
        }
        OpcaoDoOnline = Mathf.Clamp(OpcaoDoOnline, 0, linhasDoOnline.Count - 1);
    }

    // As letras que a pessoa digita (no código da sala). Vem do Input System, fora do Update.
    void LetraDigitada(char c)
    {
        if (SubmenuAberto != Submenu.Online || TelaDoOnline != TelaOnline.Entrar || Convidado.Ativo) return;
        if (!(char.IsLetterOrDigit(c) || c == '.' || c == ':' || c == '-') || DigitadoNoOnline.Length >= 40) return;
        DigitadoNoOnline += char.ToUpperInvariant(c);
        OpcaoDoOnline = 0; // (digitou: o foco vai para a caixa do código)
    }

    // ------------------------------------------------------------------ a cada quadro (Update, quando o submenu é o Online)

    void AtualizarOnline(int clique)
    {
        timerDoAvisoOnline -= Time.unscaledDeltaTime;
        if (timerDoAvisoOnline <= 0f) AvisoDoOnline = "";

        if (TelaDoOnline == TelaOnline.Entrar && busca != null)
        {
            List<BuscaNaRede.Sala> achadas = busca.Salas();
            if (achadas.Count != SalasNaRede.Count || !MesmasSalas(achadas, SalasNaRede))
            {
                SalasNaRede = achadas;
                MontarLinhasDoOnline();
            }
        }
        if (TelaDoOnline == TelaOnline.Sala && !Anfitriao.Aberto) IrParaTelaOnline(TelaOnline.Inicio);

        Keyboard teclado = Keyboard.current;
        Gamepad controle = Controles.Local;
        bool digitando = TelaDoOnline == TelaOnline.Entrar;
        // na tela de digitar, só as setas e o direcional andam, e ESPAÇO e BACKSPACE são do texto
        bool cima = digitando ? (teclado != null && teclado.upArrowKey.wasPressedThisFrame) || (controle != null && controle.dpad.up.wasPressedThisFrame) : Controles.CimaApertou();
        bool baixo = digitando ? (teclado != null && teclado.downArrowKey.wasPressedThisFrame) || (controle != null && controle.dpad.down.wasPressedThisFrame) : Controles.BaixoApertou();
        bool esquerda = digitando ? false : Controles.EsquerdaApertou();
        bool direita = digitando ? false : Controles.DireitaApertou();
        bool confirmar = digitando ? (teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame)) || (controle != null && controle.buttonSouth.wasPressedThisFrame)
                                   : Controles.Confirmar();
        bool voltar = digitando ? (teclado != null && teclado.escapeKey.wasPressedThisFrame) || (controle != null && controle.buttonEast.wasPressedThisFrame)
                                : Controles.Voltar();

        if (digitando && teclado != null)
        {
            if (teclado.backspaceKey.wasPressedThisFrame && DigitadoNoOnline.Length > 0)
                DigitadoNoOnline = DigitadoNoOnline.Substring(0, DigitadoNoOnline.Length - 1);
            if ((teclado.leftCtrlKey.isPressed || teclado.rightCtrlKey.isPressed) && teclado.vKey.wasPressedThisFrame)
            {
                // colar (o código veio pelo zap): fica só o que pode ter num código
                string colado = GUIUtility.systemCopyBuffer ?? "";
                if (DigitadoNoOnline.Length > 0 && DigitadoNoOnline[DigitadoNoOnline.Length - 1] == 'V') // (o "v" do CTRL+V também virou letra)
                    DigitadoNoOnline = DigitadoNoOnline.Substring(0, DigitadoNoOnline.Length - 1);
                foreach (char c in colado.Trim())
                    if ((char.IsLetterOrDigit(c) || c == '.' || c == ':' || c == '-') && DigitadoNoOnline.Length < 40) DigitadoNoOnline += char.ToUpperInvariant(c);
                OpcaoDoOnline = 0;
            }
        }

        if (voltar || clique == BotaoVoltar)
        {
            if (TelaDoOnline == TelaOnline.Inicio) FecharOnline();
            else { IrParaTelaOnline(TelaOnline.Inicio); Som("bloco", 0.4f); }
            return;
        }
        if (linhasDoOnline.Count == 0) MontarLinhasDoOnline();
        if (cima) OpcaoDoOnline = Girar(OpcaoDoOnline, -1, linhasDoOnline.Count);
        if (baixo) OpcaoDoOnline = Girar(OpcaoDoOnline, 1, linhasDoOnline.Count);
        if (clique >= 0 && clique < linhasDoOnline.Count)
        {
            OpcaoDoOnline = clique;
            confirmar = true;
        }
        LinhaOnline linha = linhasDoOnline[OpcaoDoOnline];
        if (linha.item == ItemOnline.Qualidade && (esquerda || direita))
        {
            TrocarQualidade(direita ? 1 : -1);
            return;
        }
        if (!confirmar) return;
        Escolher(linha);
    }

    static bool MesmasSalas(List<BuscaNaRede.Sala> a, List<BuscaNaRede.Sala> b)
    {
        for (int i = 0; i < a.Count; i++)
            if (a[i].host != b[i].host || a[i].porta != b[i].porta || a[i].nome != b[i].nome) return false;
        return true;
    }

    void TrocarQualidade(int direcao)
    {
        int total = System.Enum.GetValues(typeof(Anfitriao.Qualidade)).Length;
        Anfitriao.QualidadeEscolhida = (Anfitriao.Qualidade)Girar((int)Anfitriao.QualidadeEscolhida, direcao, total);
        Som("moeda", 0.4f);
    }

    void Escolher(LinhaOnline linha)
    {
        switch (linha.item)
        {
            case ItemOnline.CriarSala:
                if (Anfitriao.Abrir())
                {
                    IrParaTelaOnline(TelaOnline.Sala);
                    Som("conquista", 0.5f);
                }
                else
                {
                    AvisarNoOnline(Idioma.T("Não deu para abrir a sala: ", "Could not open the room: ") + Anfitriao.UltimoErro);
                    Som("bloco", 0.6f);
                }
                return;
            case ItemOnline.VerSala:
                IrParaTelaOnline(TelaOnline.Sala);
                Som("moeda", 0.5f);
                return;
            case ItemOnline.EntrarNumaSala:
                IrParaTelaOnline(TelaOnline.Entrar);
                Som("moeda", 0.5f);
                return;
            case ItemOnline.RemotePlay:
                if (Steam.ConvidarPeloRemotePlay()) AvisarNoOnline(Idioma.T("A janela da Steam abriu: escolha os amigos.", "The Steam window opened: pick your friends."));
                else AvisarNoOnline(Idioma.T("A Steam não abriu o convite (o overlay da Steam está ligado?).", "Steam did not open the invite (is the Steam overlay on?)"));
                Som("moeda", 0.5f);
                return;
            case ItemOnline.Voltar:
                if (TelaDoOnline == TelaOnline.Inicio) FecharOnline();
                else { IrParaTelaOnline(TelaOnline.Inicio); Som("bloco", 0.4f); }
                return;
            case ItemOnline.CopiarCodigo:
                if (Anfitriao.Instancia == null) return;
                GUIUtility.systemCopyBuffer = Anfitriao.Instancia.Codigo;
                AvisarNoOnline(Idioma.T("Código copiado! Mande para os amigos.", "Code copied! Send it to your friends."));
                Som("moeda", 0.6f);
                return;
            case ItemOnline.Qualidade:
                TrocarQualidade(1);
                return;
            case ItemOnline.FecharSala:
                Anfitriao.Fechar();
                IrParaTelaOnline(TelaOnline.Inicio);
                AvisarNoOnline(Idioma.T("A sala foi fechada.", "The room was closed."));
                Som("bloco", 0.5f);
                return;
            case ItemOnline.Digitar:
            case ItemOnline.Conectar:
                if (!CodigoDaSala.Ler(DigitadoNoOnline, out string host, out int porta))
                {
                    AvisarNoOnline(DigitadoNoOnline.Length == 0
                        ? Idioma.T("Digite o código que o anfitrião mandou (ou o IP dele).", "Type the code the host sent you (or their IP).")
                        : Idioma.T("Esse código não existe: confira as letras (são 7 ou 10).", "That code does not exist: check the letters (7 or 10)."));
                    Som("bloco", 0.6f);
                    return;
                }
                EntrarComoConvidado(host, porta);
                return;
            case ItemOnline.SalaDaRede:
                if (linha.sala >= SalasNaRede.Count) return;
                BuscaNaRede.Sala s = SalasNaRede[linha.sala];
                if (!s.versaoCerta)
                {
                    AvisarNoOnline(Idioma.T("Essa sala é de outra versão do jogo: atualizem os dois.", "That room is from another game version: update both."));
                    Som("bloco", 0.6f);
                    return;
                }
                EntrarComoConvidado(s.host, s.porta);
                return;
        }
    }

    void EntrarComoConvidado(string host, int porta)
    {
        if (Anfitriao.Aberto)
        {
            AvisarNoOnline(Idioma.T("Feche a sua sala antes de entrar na de outro.", "Close your room before joining another."));
            Som("bloco", 0.6f);
            return;
        }
        Som("moeda", 0.6f);
        Convidado.Entrar(host, porta);
        eraConvidado = true;
    }

    // Chamado no começo do Update: somos convidados? O jogo daqui fica parado (a imagem do anfitrião cobre a tela).
    // Ao voltar, a tela Online mostra por que a sala acabou.
    bool ParadoComoConvidado()
    {
        if (Convidado.Ativo) return true;
        if (eraConvidado)
        {
            eraConvidado = false;
            if (SubmenuAberto == Submenu.Online) AvisarNoOnline(Convidado.UltimoMotivo, 6f);
        }
        return false;
    }

    // ------------------------------------------------------------------ a "presença" na Steam (o que os amigos veem)

    float proximaPresenca;
    string ultimaPresenca;

    void AtualizarPresenca()
    {
        if (!Steam.Ligada || Time.unscaledTime < proximaPresenca) return;
        proximaPresenca = Time.unscaledTime + 5f;
        string texto;
        switch (Estado)
        {
            case EstadoDoJogo.Jogando:
            case EstadoDoJogo.Morreu:
            case EstadoDoJogo.Pausado:
                texto = Fases.Existe(FaseAtual) ? Idioma.T("Jogando: ", "Playing: ") + Idioma.Traduzir(Fases.Dados(FaseAtual).nome) : Idioma.T("Jogando", "Playing");
                break;
            case EstadoDoJogo.Mapa: texto = Idioma.T("No mapa", "On the map"); break;
            default: texto = Idioma.T("No menu", "In the menu"); break;
        }
        if (Tripulacao.Coop) texto += Idioma.T($" (tripulação de {Tripulacao.Quantos})", $" (crew of {Tripulacao.Quantos})");
        if (Anfitriao.Aberto) texto += Idioma.T(" - sala online aberta", " - online room open");
        if (texto == ultimaPresenca) return;
        ultimaPresenca = texto;
        Steam.Presenca(texto);
    }
}
