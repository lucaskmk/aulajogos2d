using UnityEngine;

// A tela dos JOGOS DE HABILIDADE da Loja (a outra metade da Interface.cs; as regras ficam em
// GerenciadorJogosDaLoja.cs) e os "destaques": o que ainda tem para fazer no jogo (medalhas, modo tempestade,
// coop), mostrado no título, no mapa e na tela de vitória.
public partial class Interface
{
    static readonly Color CorDoVerde = new Color32(52, 196, 100, 255), CorDoAmarelo = new Color32(236, 190, 60, 255),
        CorDoFora = new Color32(54, 40, 62, 255), CorDoVermelho = new Color32(222, 52, 60, 255);

    TextoComNumero textoDasFichas, textoDasFichasNaTela;

    // A linha "Jogos de habilidade (3 fichas)" da Loja, entre a roleta e o "Voltar ao mapa".
    void OpcaoDosJogos(float y)
    {
        string texto = ComNumero(ref textoDasFichas, jogo.Fichas, n => Idioma.T($"Jogos de habilidade ({n} fichas)", $"Skill games ({n} tokens)"));
        OpcaoDeMenu(texto, y, 2.2f, 30f * escala, (int)GerenciadorDoJogo.ItemDaLoja.Jogos,
            jogo.OpcaoDaLoja == (int)GerenciadorDoJogo.ItemDaLoja.Jogos);
    }

    void DesenharJogosDaLoja()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        GUI.DrawTexture(new Rect(0, 0, w, h), fundoEscuro);
        Texto(Idioma.T("JOGOS DO LOURO", "THE PARROT'S GAMES"), w / 2f, 18f * escala, 5f, 0.5f, CorDoLogo, w * 0.9f);
        DesenharSaldo(w / 2f, 100f * escala);
        Texto(ComNumero(ref textoDasFichasNaTela, jogo.Fichas, n => Idioma.T($"FICHAS: {n}/{jogo.maximoDeFichas}", $"TOKENS: {n}/{jogo.maximoDeFichas}")),
            w / 2f, 140f * escala, 2.2f, 0.5f, jogo.Fichas > 0 ? Verde : Vermelho);

        bool pontaria = jogo.JogoEscolhido == GerenciadorDoJogo.JogoDeHabilidade.Pontaria;
        bool valendo = jogo.Rodada == GerenciadorDoJogo.FaseDaRodada.Valendo;
        float yNome = 186f * escala;
        Texto(pontaria ? Idioma.T("PONTARIA", "AIM") : Idioma.T("REFLEXO", "REFLEX"), w / 2f, yNome, 4f, 0.5f, Amarelo);
        if (!valendo) // (no meio da rodada não dá para trocar de jogo)
        {
            BotaoDeTexto("<", w / 2f - 190f * escala, yNome + 4f * escala, 3.5f, 0.5f, GerenciadorDoJogo.BotaoAnterior);
            BotaoDeTexto(">", w / 2f + 190f * escala, yNome + 4f * escala, 3.5f, 0.5f, GerenciadorDoJogo.BotaoProxima);
        }
        Texto(pontaria
                ? Idioma.T("O ponteiro vai e volta. Pare ele no VERDE. Cada acerto seguido deixa ele mais rápido.",
                           "The pointer sweeps back and forth. Stop it on the GREEN. Each hit in a row makes it faster.")
                : Idioma.T("Espere o JÁ! e aperte o mais rápido que conseguir. Apertou antes? Queimou a largada.",
                           "Wait for GO! and press as fast as you can. Pressed early? False start."),
            w / 2f, 244f * escala, 2f, 0.5f, Branco, w * 0.9f);

        if (pontaria) DesenharPontaria(new Rect(w / 2f - 290f * escala, 310f * escala, 580f * escala, 56f * escala));
        else DesenharReflexo(new Rect(w / 2f - 190f * escala, 286f * escala, 380f * escala, 120f * escala));

        Texto(pontaria
                ? Idioma.T($"VERDE: +{jogo.premioEmCheio} moedas     AMARELO: +{jogo.premioPerto}     FORA: nada",
                           $"GREEN: +{jogo.premioEmCheio} coins     YELLOW: +{jogo.premioPerto}     OUTSIDE: nothing")
                : Idioma.T($"ATÉ 0,30 s: +{jogo.premioEmCheio} moedas     ATÉ 0,45 s: +{jogo.premioPerto}     DEPOIS: nada",
                           $"UNDER 0.30 s: +{jogo.premioEmCheio} coins     UNDER 0.45 s: +{jogo.premioPerto}     AFTER: nothing"),
            w / 2f, 430f * escala, 1.9f, 0.5f, CorDoRodape, w * 0.9f);
        Texto(Idioma.T("Cada rodada gasta 1 ficha (não moedas). O Louro dá fichas quando você passa de fase.",
                       "Each round costs 1 token (not coins). The parrot hands out tokens when you clear a level."),
            w / 2f, 462f * escala, 1.8f, 0.5f, CorDoRodape, w * 0.9f);

        string mensagem = jogo.MensagemDaLoja;
        if (mensagem != "") Texto(mensagem, w / 2f, 506f * escala, 2.6f, 0.5f, jogo.MensagemDaLojaBoa ? Verde : Rosa, w * 0.9f);

        string botao = !valendo ? Idioma.T("ENTER: jogar (1 ficha)", "ENTER: play (1 token)")
            : pontaria ? Idioma.T("ENTER: PARAR!", "ENTER: STOP!") : Idioma.T("ENTER: quando aparecer o JÁ!", "ENTER: when you see GO!");
        BotaoDeTexto(botao, w / 2f, 556f * escala, 3f, 0.5f, GerenciadorDoJogo.BotaoJogar);
        if (!valendo) BotaoDeTexto(Idioma.T("Voltar para a Loja", "Back to the shop"), w / 2f, 612f * escala, 2.2f, 0.5f, GerenciadorDoJogo.BotaoVoltar);
        Rodape(Idioma.T("ESQUERDA/DIREITA: trocar de jogo    ENTER: jogar    ESC: voltar para a Loja",
                        "LEFT/RIGHT: switch game    ENTER: play    ESC: back to the shop"));
    }

    // A barra da pontaria: escura nas pontas, amarela perto do meio e verde bem no meio, com o ponteiro por cima.
    void DesenharPontaria(Rect barra)
    {
        Moldura(new Rect(barra.x - 8f * escala, barra.y - 8f * escala, barra.width + 16f * escala, barra.height + 16f * escala), CorDoFundoDaFala);
        Retangulo(barra, CorDoFora);
        float meio = barra.center.x;
        float amarelo = jogo.meiaLarguraDoAmarelo * barra.width, verde = jogo.meiaLarguraDoVerde * barra.width;
        Retangulo(new Rect(meio - amarelo, barra.y, 2f * amarelo, barra.height), CorDoAmarelo);
        Retangulo(new Rect(meio - verde, barra.y, 2f * verde, barra.height), CorDoVerde);

        // o ponteiro: uma haste branca com contorno escuro, mais alta que a barra (parado antes da rodada, fica na ponta)
        float x = barra.x + barra.width * (jogo.Rodada == GerenciadorDoJogo.FaseDaRodada.Parado ? 0f : jogo.Ponteiro);
        float largura = 8f * escala, sobra = 12f * escala;
        Retangulo(new Rect(x - largura / 2f - 2f * escala, barra.y - sobra - 2f * escala, largura + 4f * escala, barra.height + 2f * sobra + 4f * escala), Preto);
        Retangulo(new Rect(x - largura / 2f, barra.y - sobra, largura, barra.height + 2f * sobra), Color.white);
        if (jogo.AcertosSeguidos > 0)
            Texto(Idioma.T($"acertos seguidos: {jogo.AcertosSeguidos}", $"hits in a row: {jogo.AcertosSeguidos}"), meio, barra.yMax + 22f * escala, 1.8f, 0.5f, Verde);
    }

    // O quadro do reflexo: cinza parado, vermelho no "ESPERE...", verde no "JÁ!" e o tempo da reação no fim.
    void DesenharReflexo(Rect quadro)
    {
        GerenciadorDoJogo.FaseDaRodada rodada = jogo.Rodada;
        bool sinal = rodada == GerenciadorDoJogo.FaseDaRodada.Valendo && jogo.SinalDoReflexo;
        Color fundo = rodada == GerenciadorDoJogo.FaseDaRodada.Valendo ? (sinal ? CorDoVerde : CorDoVermelho) : CorDoFora;
        Moldura(new Rect(quadro.x - 8f * escala, quadro.y - 8f * escala, quadro.width + 16f * escala, quadro.height + 16f * escala), CorDoFundoDaFala);
        Retangulo(quadro, fundo);
        string texto;
        if (rodada == GerenciadorDoJogo.FaseDaRodada.Valendo) texto = sinal ? Idioma.T("JÁ!", "GO!") : Idioma.T("ESPERE...", "WAIT...");
        else if (rodada == GerenciadorDoJogo.FaseDaRodada.Resultado)
            texto = jogo.TempoDeReacao < 0f ? Idioma.T("QUEIMOU!", "FALSE START!")
                : jogo.TempoDeReacao.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " s";
        else texto = Idioma.T("PRONTO?", "READY?");
        Rect medida = MedirTexto(texto, 0f, 0f, 6f, 0f, quadro.width - 30f * escala);
        Texto(texto, quadro.center.x, quadro.center.y - medida.height / 2f, 6f, 0.5f, Branco, quadro.width - 30f * escala);
    }

    // ------------------------------------------------------------------ destaques: o que ainda tem para fazer

    // As medalhas do slot atual (3 por fase da campanha). Contar lê o save de cada fase: só refaz a conta de vez em quando.
    int medalhasGanhas = -1;
    float quandoConteiAsMedalhas = -10f;
    string textoDasMedalhas = "";
    Idioma.Lingua idiomaDasMedalhas;
    public static int TotalDeMedalhas => Fases.Todas.Length * Medalhas.Ordem.Length;

    string TextoDasMedalhas()
    {
        if (medalhasGanhas >= 0 && Time.unscaledTime - quandoConteiAsMedalhas < 1f && idiomaDasMedalhas == Idioma.Atual) return textoDasMedalhas;
        quandoConteiAsMedalhas = Time.unscaledTime;
        idiomaDasMedalhas = Idioma.Atual;
        int ganhas = 0;
        for (int fase = 0; fase < Fases.Todas.Length; fase++) ganhas += Medalhas.Quantas(Medalhas.DaFase(fase));
        medalhasGanhas = ganhas;
        textoDasMedalhas = Idioma.T("Medalhas: ", "Medals: ") + ganhas + "/" + TotalDeMedalhas;
        return textoDasMedalhas;
    }

    // No título: as medalhas do último slot e o lembrete do coop, numa linha discreta embaixo do recorde.
    void DesenharDestaquesNoTitulo(float y)
    {
        string coop = Dica(Idioma.T("Coop local até 4: tecla T no mapa", "Local co-op for up to 4: T key on the map"));
        string texto = Progresso.TemJogoSalvo ? TextoDasMedalhas() + "      " + coop : coop;
        Texto(texto, Screen.width / 2f, y, 1.9f, 0.5f, Lilas, Screen.width * 0.95f);
    }

    // Na tela de vitória: "E AGORA?", com o que ainda falta fazer (para o jogador não achar que acabou).
    void DesenharOQueFalta(float centro, float y, float largura)
    {
        float linha = 27f * escala;
        Texto(Idioma.T("E AGORA?", "WHAT NOW?"), centro, y, 2.6f, 0.5f, Amarelo, largura);
        y += linha + 6f * escala;
        Texto(TextoDasMedalhas() + Idioma.T("  (relógio, coração e moeda em cada fase)", "  (clock, heart and coin on every level)"), centro, y, 1.9f, 0.5f, Branco, largura);
        y += linha;
        Texto(jogo.ModoDificil
                ? Idioma.T("Você está no MODO TEMPESTADE: um tsunami em toda fase. Tem outro final te esperando.",
                           "You are in STORM MODE: a tsunami on every level. Another ending is waiting.")
                : Idioma.T("MODO TEMPESTADE liberado no título: as mesmas fases, com um tsunami atrás de você.",
                           "STORM MODE unlocked on the title screen: the same levels, with a tsunami chasing you."),
            centro, y, 1.9f, 0.5f, Rosa, largura);
        y += linha;
        Texto(Progresso.SecretaLiberada
                ? Idioma.T("Chame até 3 amigos: tecla T no mapa (coop na mesma tela).", "Bring up to 3 friends: T key on the map (same-screen co-op).")
                : Idioma.T("Tem uma fase SECRETA escondida no mapa. E dá para jogar com até 3 amigos (tecla T).",
                           "There is a SECRET level hidden on the map. And you can play with up to 3 friends (T key)."),
            centro, y, 1.9f, 0.5f, Lilas, largura);
    }
}
