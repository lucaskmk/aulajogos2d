using UnityEngine;

// A tela JOGAR ONLINE (a outra metade da Interface.cs; a lógica fica em GerenciadorOnline.cs).
public partial class Interface
{
    void DesenharOnline()
    {
        float w = Screen.width, h = Screen.height;
        Texto(Idioma.T("TRIPULAÇÃO ONLINE", "ONLINE CREW"), w / 2f, h * 0.04f, 6f, 0.5f, Lilas);

        float y = h * 0.17f;
        switch (jogo.TelaDoOnline)
        {
            case GerenciadorDoJogo.TelaOnline.Inicio:
                Texto(Idioma.T("Até 4 piratas, cada um no seu computador. Quem cria a sala roda o jogo; os amigos veem a tela",
                               "Up to 4 pirates, each on their own computer. Whoever opens the room runs the game; friends see the screen"),
                    w / 2f, y, 1.7f, 0.5f, CorDoRodape, w * 0.94f);
                Texto(Idioma.T("dele e jogam como se tivessem ligado mais um controle (o progresso fica no save de quem abriu a sala).",
                               "and play as if they had plugged in another controller (progress goes to the host's save)."),
                    w / 2f, y + 22f * escala, 1.7f, 0.5f, CorDoRodape, w * 0.94f);
                y = h * 0.34f;
                break;

            case GerenciadorDoJogo.TelaOnline.Sala:
                y = DesenharInfoDaSala(y);
                break;

            case GerenciadorDoJogo.TelaOnline.Entrar:
                Texto(Idioma.T("Digite o código que o anfitrião mandou (ou o IP dele). CTRL+V cola.",
                               "Type the code the host sent you (or their IP). CTRL+V pastes."), w / 2f, y, 1.8f, 0.5f, CorDoRodape, w * 0.94f);
                y = h * 0.25f;
                break;
        }

        float passo = Mathf.Min(40f * escala, (h * 0.86f - y) / Mathf.Max(1, jogo.linhasDoOnline.Count));
        for (int i = 0; i < jogo.linhasDoOnline.Count; i++)
        {
            GerenciadorDoJogo.LinhaOnline linha = jogo.linhasDoOnline[i];
            bool escolhida = i == jogo.OpcaoDoOnline;
            if (linha.item == GerenciadorDoJogo.ItemOnline.SalaDaRede && (i == 0 || jogo.linhasDoOnline[i - 1].item != GerenciadorDoJogo.ItemOnline.SalaDaRede))
            {
                Texto(Idioma.T("SALAS NA SUA REDE:", "ROOMS ON YOUR NETWORK:"), w / 2f, y + 4f * escala, 1.7f, 0.5f, Ciano);
                y += passo * 0.7f;
            }
            OpcaoDeMenu(TextoDaLinhaOnline(linha, escolhida), y, linha.item == GerenciadorDoJogo.ItemOnline.Digitar ? 3f : 2.4f, passo, i, escolhida);
            y += passo;
            if (linha.item == GerenciadorDoJogo.ItemOnline.Conectar && jogo.SalasNaRede.Count == 0)
            {
                Texto(Idioma.T("(procurando salas na sua rede...)", "(looking for rooms on your network...)"), w / 2f, y + 2f * escala, 1.6f, 0.5f, CorDoRodape);
                y += passo * 0.8f;
            }
        }

        if (jogo.AvisoDoOnline != "") Texto(jogo.AvisoDoOnline, w / 2f, h * 0.86f, 2f, 0.5f, Amarelo, w * 0.92f);
        Rodape(jogo.TelaDoOnline == GerenciadorDoJogo.TelaOnline.Entrar
            ? Idioma.T("digite o código    SETAS: escolher    ENTER: entrar    ESC: voltar", "type the code    ARROWS: choose    ENTER: join    ESC: back")
            : Idioma.T("SETAS: escolher    ENTER: confirmar    ESC: voltar", "ARROWS: choose    ENTER: confirm    ESC: back"));
    }

    string TextoDaLinhaOnline(GerenciadorDoJogo.LinhaOnline linha, bool escolhida)
    {
        switch (linha.item)
        {
            case GerenciadorDoJogo.ItemOnline.CriarSala: return Idioma.T("Criar uma sala (eu sou o anfitrião)", "Open a room (I am the host)");
            case GerenciadorDoJogo.ItemOnline.VerSala: return Idioma.T("Ver a minha sala", "See my room");
            case GerenciadorDoJogo.ItemOnline.EntrarNumaSala: return Idioma.T("Entrar na sala de um amigo", "Join the room of a friend");
            case GerenciadorDoJogo.ItemOnline.RemotePlay: return Idioma.T("Convidar amigos da Steam (Remote Play Together)", "Invite Steam friends (Remote Play Together)");
            case GerenciadorDoJogo.ItemOnline.CopiarCodigo: return Idioma.T("Copiar o código da sala", "Copy the room code");
            case GerenciadorDoJogo.ItemOnline.Qualidade:
            {
                string q = Anfitriao.QualidadeEscolhida == Anfitriao.Qualidade.Leve ? Idioma.T("Leve (internet fraca)", "Light (slow internet)")
                    : Anfitriao.QualidadeEscolhida == Anfitriao.Qualidade.Alta ? Idioma.T("Alta (rede de casa)", "High (home network)")
                    : Idioma.T("Normal", "Normal");
                return Idioma.T("Imagem: < ", "Picture: < ") + q + " >";
            }
            case GerenciadorDoJogo.ItemOnline.FecharSala: return Idioma.T("Fechar a sala", "Close the room");
            case GerenciadorDoJogo.ItemOnline.Digitar:
            {
                string cursor = escolhida && Time.unscaledTime % 1f < 0.5f ? " <" : "  "; // (a fonte não tem "_")
                return (jogo.DigitadoNoOnline.Length == 0 && !escolhida ? Idioma.T("(código)", "(code)") : jogo.DigitadoNoOnline) + cursor;
            }
            case GerenciadorDoJogo.ItemOnline.Conectar: return Idioma.T("Entrar", "Join");
            case GerenciadorDoJogo.ItemOnline.SalaDaRede:
            {
                if (linha.sala >= jogo.SalasNaRede.Count) return "";
                BuscaNaRede.Sala s = jogo.SalasNaRede[linha.sala];
                return Idioma.T("Sala de ", "Room of ") + s.nome + (s.versaoCerta ? "" : Idioma.T(" (outra versão)", " (other version)"));
            }
            default: return "Voltar";
        }
    }

    // O código grande, de onde dá para entrar, e quem está na sala.
    float DesenharInfoDaSala(float y)
    {
        float w = Screen.width;
        Anfitriao sala = Anfitriao.Instancia;
        if (sala == null) return y;
        Texto(Idioma.T("CÓDIGO DA SALA", "ROOM CODE"), w / 2f, y, 2f, 0.5f, CorDoRodape);
        Texto(sala.Codigo, w / 2f, y + 22f * escala, 7f, 0.5f, Amarelo);
        y += 82f * escala;

        string onde;
        Color32 cor;
        PortaNoRoteador roteador = sala.Roteador;
        if (sala.AbertaNaInternet)
        {
            onde = Idioma.T("Aberta na internet: qualquer amigo entra com o código.", "Open on the internet: any friend can join with the code.");
            cor = Verde;
        }
        else if (roteador != null && roteador.Estado == PortaNoRoteador.Situacao.Procurando)
        {
            onde = Idioma.T("Pedindo ao roteador para abrir a porta... (por enquanto, só na rede de casa)", "Asking the router to open the port... (for now, home network only)");
            cor = CorDoRodape;
        }
        else if (sala.TemCgnat)
        {
            onde = Idioma.T("Seu provedor usa CGNAT: pela internet, só com uma VPN de jogos (Radmin VPN, ZeroTier) ou a Steam.",
                            "Your provider uses CGNAT: over the internet only with a gaming VPN (Radmin VPN, ZeroTier) or Steam.");
            cor = Vermelho;
        }
        else
        {
            onde = Idioma.T($"Só na rede de casa (o roteador não abriu a porta {sala.Porta}). Abra a porta TCP {sala.Porta} no roteador ou use uma VPN de jogos.",
                            $"Home network only (the router did not open port {sala.Porta}). Open TCP port {sala.Porta} on the router or use a gaming VPN.");
            cor = Rosa;
        }
        Texto(onde, w / 2f, y, 1.6f, 0.5f, cor, w * 0.94f);
        y += 22f * escala;
        Texto(Idioma.T($"Na rede de casa: {sala.IpLocal}", $"On the home network: {sala.IpLocal}"), w / 2f, y, 1.6f, 0.5f, CorDoRodape, w * 0.94f);
        y += 30f * escala;

        if (sala.Convidados.Count == 0)
            Texto(Idioma.T("Ninguém entrou ainda. No mapa, abra a TRIPULAÇÃO (T): quem chegar entra apertando PULAR.",
                           "Nobody joined yet. On the map, open the CREW screen (T): whoever arrives joins by pressing JUMP."), w / 2f, y, 1.7f, 0.5f, Branco, w * 0.94f);
        else
        {
            float x = w / 2f;
            string lista = "";
            foreach (Anfitriao.Convidado c in sala.Convidados)
            {
                if (!c.disseOla) continue;
                if (lista.Length > 0) lista += "     ";
                lista += c.nome + (c.pingMs >= 0 ? $" ({c.pingMs} ms)" : "");
            }
            Texto(Idioma.T("NA SALA: ", "IN THE ROOM: ") + lista, x, y, 2f, 0.5f, Verde, w * 0.94f);
        }
        return y + 40f * escala;
    }
}
