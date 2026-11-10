using System.Collections.Generic;
using UnityEngine;

// O COOP na Interface (a outra metade da Interface.cs): o botão e a tela da Tripulação no mapa, os rostinhos
// dos jogadores no canto da fase (com o balão em volta de quem está dentro de um) e os títulos da tripulação
// no fim da fase. Os textos daqui usam Idioma.T direto (não precisam de linha em Traducoes.cs).
public partial class Interface
{
    static Color32 CorDoJogador(int indice) => Tripulacao.Cores[indice % Tripulacao.Cores.Length];

    // ------------------------------------------------------------------ o botão no mapa

    // No canto de baixo à ESQUERDA, espelhando o botão da Loja.
    Rect RetanguloDoBotaoDaTripulacao()
    {
        Rect loja = RetanguloDoBotaoDaLoja();
        return new Rect(18f * escala, loja.y, loja.width, loja.height);
    }

    void BotaoDaTripulacao()
    {
        Rect botao = RetanguloDoBotaoDaTripulacao();
        bool emCima = Mouse(botao, GerenciadorDoJogo.BotaoTripulacao, false);
        Moldura(botao, emCima ? new Color(0.25f, 0.18f, 0.08f, 0.96f) : CorDoFundoDaFala);
        if (emCima) DesenharBorda(botao, CorDoFilete);

        // os rostinhos de quem está na tripulação (sozinho: só o P1)
        float lado = 26f * escala, x = botao.x + 16f * escala;
        for (int i = 0; i < Tripulacao.Quantos; i++)
        {
            float pulo = Mathf.Round(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f + i)) * 2f) * escala;
            GUI.DrawTexture(new Rect(x, botao.y + 14f * escala - pulo, lado, lado), FabricaDeSprites.Pegar(Tripulacao.Sprite(i)).texture);
            x += lado + 4f * escala;
        }
        Texto(Idioma.T("COOP", "CO-OP"), botao.x + 16f * escala, botao.y + 44f * escala, 2.6f, 0f, emCima ? Branco : CorDoLogo, botao.width - 32f * escala);
        Texto(Idioma.T("tecla T", "T key"), botao.x + 16f * escala, botao.y + 70f * escala, 1.6f, 0f, CorDoRodape);
    }

    // ------------------------------------------------------------------ a tela da Tripulação

    // Quatro cartões (P1 a P4). Quem já entrou aparece com o Grumete da sua cor e o aparelho; os vazios piscam
    // "aperte PULAR". Embaixo, as teclas de entrar e sair e as regras do coop em duas linhas.
    void DesenharTripulacao()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);

        Texto(Idioma.T("TRIPULAÇÃO", "CREW"), w / 2f, h * 0.04f, 5f, 0.5f, Lilas, w * 0.9f);
        Texto(Idioma.T("Coop local: até 4 jogadores na mesma tela. Cada um aperta PULAR no seu controle para entrar.",
                       "Local co-op: up to 4 players on one screen. Each player presses JUMP on their own device to join."),
            w / 2f, h * 0.04f + 78f * escala, 2f, 0.5f, Branco, w * 0.92f);

        float folga = 16f * escala;
        float largura = Mathf.Min((w - 5f * folga) / 4f, 250f * escala), altura = 250f * escala;
        float x0 = (w - 4f * largura - 3f * folga) / 2f, y = h * 0.04f + 128f * escala;
        int dentro = Tripulacao.NoLobby;
        for (int i = 0; i < Tripulacao.Maximo; i++)
        {
            var cartao = new Rect(x0 + i * (largura + folga), y, largura, altura);
            bool ocupado = i < dentro;
            Color32 cor = CorDoJogador(i);
            Moldura(cartao, ocupado ? new Color(0.09f, 0.11f, 0.22f, 0.96f) : new Color(0.05f, 0.06f, 0.12f, 0.9f));
            if (ocupado) DesenharBorda(cartao, cor);
            Texto(Tripulacao.Nome(i), cartao.center.x, cartao.y + 16f * escala, 3.5f, 0.5f, ocupado ? cor : Apagado);

            // o Grumete da cor do jogador (pulando se já entrou; apagado se o lugar está vazio)
            float lado = 96f * escala;
            float pulo = ocupado ? Mathf.Round(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f + i * 0.8f)) * 4f) * escala * 2f : 0f;
            Color antes = GUI.color;
            if (!ocupado) GUI.color = new Color(1f, 1f, 1f, 0.22f);
            GUI.DrawTexture(new Rect(cartao.center.x - lado / 2f, cartao.y + 62f * escala - pulo, lado, lado), FabricaDeSprites.Pegar(Tripulacao.Sprite(i)).texture);
            GUI.color = antes;

            float yTexto = cartao.y + 172f * escala;
            if (ocupado)
            {
                Tripulacao.Marujo marujo = Tripulacao.Do(i);
                Texto(marujo.Aparelho(), cartao.center.x, yTexto, 1.9f, 0.5f, Branco, largura - 24f * escala);
                Texto(marujo.Teclas(), cartao.center.x, yTexto + 34f * escala, 1.5f, 0.5f, CorDoRodape, largura - 24f * escala);
            }
            else if (i == dentro && Time.unscaledTime % 0.9f < 0.6f)
                Texto(Idioma.T("aperte PULAR", "press JUMP"), cartao.center.x, yTexto + 10f * escala, 2f, 0.5f, Amarelo, largura - 24f * escala);
            else if (i != dentro)
                Texto(Idioma.T("vazio", "empty"), cartao.center.x, yTexto + 10f * escala, 1.8f, 0.5f, Apagado, largura - 24f * escala);
        }

        float yAjuda = y + altura + 22f * escala, linha = 30f * escala;
        Texto(Idioma.T("ENTRAR:  W ou ESPAÇO (teclado)     SETA PARA CIMA (2º no teclado)     botão A (controle)",
                       "JOIN:  W or SPACE (keyboard)     UP ARROW (2nd on keyboard)     A button (gamepad)"),
            w / 2f, yAjuda, 1.9f, 0.5f, Verde, w * 0.94f);
        Texto(Idioma.T("SAIR:  Q     CTRL direito     botão B", "LEAVE:  Q     right CTRL     B button"),
            w / 2f, yAjuda + linha, 1.9f, 0.5f, Vermelho, w * 0.94f);
        Texto(Idioma.T("Quem morre ou fica para trás volta pendurado num BALÃO: pulem nele para estourar. Todo mundo no balão? Volta pro save.",
                       "Die or fall behind and you come back hanging from a BALLOON: jump into it to pop it. Everyone on a balloon? Back to the save."),
            w / 2f, yAjuda + 2.3f * linha, 1.8f, 0.5f, Rosa, w * 0.94f);
        Texto(Idioma.T("Pular na cabeça do amigo te joga mais alto. E dá para EMPURRAR o amigo. É um jogo de cilada. :)",
                       "Jumping on a friend's head launches you higher. And you can SHOVE your friend. It's a trap game. :)"),
            w / 2f, yAjuda + 3.3f * linha, 1.8f, 0.5f, Rosa, w * 0.94f);

        BotaoDeTexto(dentro >= 2 ? Idioma.T("Pronto!", "Ready!") : Idioma.T("Voltar (jogar sozinho)", "Back (play solo)"),
            w / 2f, h * 0.86f, 2.5f, 0.5f, GerenciadorDoJogo.BotaoVoltar);
        Rodape(Idioma.T("ENTER ou ESC: fechar     (no controle: START)", "ENTER or ESC: close     (on a gamepad: START)"));
    }

    // ------------------------------------------------------------------ a Loja no coop

    // Em cima dos itens: "PARA:" e o rostinho de cada jogador. O escolhido fica aceso, com a moldura da cor dele:
    // os escudos, o elixir e o duplo salto comprados vão para ele (as moedas e as poções são da tripulação).
    void DesenharCompradores(float x, float y)
    {
        Rect rotulo = Texto(Idioma.T("PARA:", "FOR:"), x, y + 10f * escala, 2.2f, 0f, CorDoRodape);
        float lado = 36f * escala, xRosto = rotulo.xMax + 10f * escala;
        for (int i = 0; i < Tripulacao.Quantos; i++)
        {
            var area = new Rect(xRosto, y, lado, lado);
            bool escolhido = i == jogo.Comprador;
            bool emCima = Mouse(new Rect(area.x - 3f * escala, area.y - 3f * escala, lado + 6f * escala, lado + 6f * escala), GerenciadorDoJogo.BotaoComprador - i, false);
            Color antes = GUI.color;
            if (!escolhido && !emCima) GUI.color = new Color(1f, 1f, 1f, 0.4f);
            GUI.DrawTexture(area, FabricaDeSprites.Pegar(Tripulacao.Sprite(i)).texture);
            GUI.color = antes;
            if (escolhido) DesenharBorda(new Rect(area.x - 4f * escala, area.y - 4f * escala, lado + 8f * escala, lado + 8f * escala), CorDoJogador(i));
            xRosto += lado + 12f * escala;
        }
        Texto(Tripulacao.Nome(jogo.Comprador) + Idioma.T("   (TAB troca)", "   (TAB switches)"), xRosto + 4f * escala, y + 10f * escala, 1.8f, 0f, CorDoJogador(jogo.Comprador));
    }

    // ------------------------------------------------------------------ os rostinhos na fase

    // No canto de baixo à esquerda: um rostinho por jogador, com a etiqueta "P1"... embaixo.
    // Quem está no balão aparece com o balãozinho amarrado em cima; quem está caindo (morreu agora) fica apagado.
    // Ao lado de cada um: quantas vezes ELE morreu nesta partida (a caveira) e os itens DELE (escudos, elixir, bota).
    readonly string[] textosDasMortes = new string[Tripulacao.Maximo], textosDosEscudosDe = new string[Tripulacao.Maximo];
    readonly int[] mortesMostradas = { -1, -1, -1, -1 }, escudosMostrados = { -1, -1, -1, -1 };

    void DesenharTripulacaoNaFase()
    {
        List<Jogador> jogadores = GerenciadorDoJogo.Jogadores;
        if (jogadores.Count < 2) return;
        float lado = 36f * escala, passo = 104f * escala;
        float x = 22f * escala, y = Screen.height - lado - 34f * escala;
        foreach (Jogador jogador in jogadores)
        {
            if (jogador == null) continue;
            int i = jogador.Indice;
            Color32 cor = CorDoJogador(i);
            Color antes = GUI.color;
            if (jogador.Morto && !jogador.NaBolha) GUI.color = new Color(1f, 1f, 1f, 0.3f);
            GUI.DrawTexture(new Rect(x, y, lado, lado), FabricaDeSprites.Pegar(Tripulacao.Sprite(i)).texture);
            if (jogador.NaBolha)
            {
                // o balãozinho da cor dele, com a cordinha chegando na cabeça (sobe e desce de leve)
                Texture balao = FabricaDeSprites.Pegar(Jogador.SpriteDoBalao(i)).texture;
                float largura = lado * 0.5f, altura = largura * balao.height / balao.width;
                float sobe = Mathf.Round(Mathf.Sin(Time.unscaledTime * 3f + i) * 2f) * escala;
                GUI.DrawTexture(new Rect(x + lado / 2f - largura / 2f, y + lado * 0.2f - altura - sobe, largura, altura), balao);
            }
            GUI.color = antes;
            Texto(Tripulacao.Nome(i), x + lado / 2f, y + lado + 2f * escala, 1.6f, 0.5f, cor);

            // a caveira e as mortes dele (o texto só é montado de novo quando o número muda)
            int mortes = Tripulacao.Do(i).mortesNaPartida;
            if (mortes != mortesMostradas[i])
            {
                mortesMostradas[i] = mortes;
                textosDasMortes[i] = mortes.ToString();
            }
            float xDoLado = x + lado + 6f * escala, icone = 16f * escala;
            GUI.DrawTexture(new Rect(xDoLado, y, icone * 7f / 6f, icone), FabricaDeSprites.Pegar("caveira").texture); // (a caveira tem 7 x 6 pixels)
            Texto(textosDasMortes[i], xDoLado + icone * 7f / 6f + 4f * escala, y - 1f * escala, 1.8f, 0f, Branco);

            // os itens dele: escudo (com "x2" se tiver mais de um), elixir valendo e a bota do duplo salto
            float xItem = xDoLado, yItem = y + lado - icone;
            int escudos = jogo.EscudosDe(i);
            if (escudos > 0)
            {
                GUI.DrawTexture(new Rect(xItem, yItem, icone * 11f / 12f, icone), FabricaDeSprites.Pegar("escudo").texture);
                xItem += icone + 2f * escala;
                if (escudos > 1)
                {
                    if (escudos != escudosMostrados[i])
                    {
                        escudosMostrados[i] = escudos;
                        textosDosEscudosDe[i] = "x" + escudos;
                    }
                    xItem = Texto(textosDosEscudosDe[i], xItem, yItem, 1.5f, 0f, Branco).xMax + 4f * escala;
                }
            }
            if (jogo.ElixirAtivoDe(i))
            {
                GUI.DrawTexture(new Rect(xItem, yItem, icone * 12f / 13f, icone), FabricaDeSprites.Pegar("elixir").texture);
                xItem += icone + 2f * escala;
            }
            if (jogo.TemPuloDuploDe(i))
                GUI.DrawTexture(new Rect(xItem, yItem, icone * 14f / 12f, icone), FabricaDeSprites.Pegar("bota_alada").texture);
            x += passo;
        }
    }

    // ------------------------------------------------------------------ os títulos no fim da fase

    // Uma caixa ao lado do painel de melhores tempos: quem estourou mais balões, quem pegou mais moedas,
    // quem morreu mais e quem mais empurrou os amigos. Só aparece no coop (e só os títulos que alguém mereceu).
    void DesenharTitulosDaTripulacao(float y)
    {
        List<GerenciadorDoJogo.TituloDaTripulacao> titulos = jogo.TitulosDaTripulacao;
        if (titulos.Count == 0) return;
        float w = Screen.width;
        float painel = Mathf.Min(w * 0.7f, 620f * escala), direitaDoPainel = (w + painel) / 2f; // (as mesmas contas do DesenharPainelDeTempos)
        float folga = 12f * escala;
        float largura = Mathf.Min(w - direitaDoPainel - 2f * folga, 300f * escala);
        if (largura < 120f * escala) return; // tela estreita demais: não cabe ao lado do painel
        var caixa = new Rect(direitaDoPainel + folga, y, largura, 292f * escala);
        GUI.DrawTexture(caixa, fundoEscuro);
        GUI.DrawTexture(caixa, fundoEscuro);
        DesenharBorda(caixa, CorDoFilete);

        Texto(Idioma.T("TRIPULAÇÃO", "CREW"), caixa.x + folga, y + folga, 2.5f, 0f, Amarelo, largura - 2f * folga);
        float yLinha = y + folga + 46f * escala, lado = 30f * escala;
        foreach (GerenciadorDoJogo.TituloDaTripulacao titulo in titulos)
        {
            Color32 cor = CorDoJogador(titulo.jogador);
            GUI.DrawTexture(new Rect(caixa.x + folga, yLinha, lado, lado), FabricaDeSprites.Pegar(Tripulacao.Sprite(titulo.jogador)).texture);
            float xTexto = caixa.x + folga + lado + 8f * escala, larguraDoTexto = caixa.xMax - folga - xTexto;
            Texto(Tripulacao.Nome(titulo.jogador) + ": " + titulo.nome, xTexto, yLinha - 2f * escala, 1.9f, 0f, cor, larguraDoTexto);
            Texto(titulo.motivo, xTexto, yLinha + 24f * escala, 1.5f, 0f, CorDoRodape, larguraDoTexto);
            yLinha += 58f * escala;
        }
    }
}
