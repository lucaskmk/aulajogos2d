using System.Collections.Generic;

// Textos "de história": as falas no mapa depois de cada fase e os créditos do final.
// É só editar aqui.
public static class Textos
{
    // ------------------------------------------------------------------ falas no mapa

    public struct Fala
    {
        public string quem;   // nome que aparece na caixa
        public string sprite; // desenho (FabricaDeSprites)
        public string texto;  // {0} vira o número de mortes na fase
    }

    static Fala Louro(string texto) => new Fala { quem = "Louro", sprite = "louro", texto = texto };
    static Fala Marina(string texto) => new Fala { quem = "Marina", sprite = "sereia", texto = texto };
    static Fala Doris(string texto) => new Fala { quem = "Dóris", sprite = "fantasma", texto = texto };

    // Uma fala para depois de cada fase, pelo NOME da fase (assim dá para reordenar as fases à vontade).
    // Mudou o nome de uma fase em Fases.cs? Mude aqui também (senão aparece a FalaPadrao).
    static readonly Dictionary<string, Fala> DepoisDaFase = new Dictionary<string, Fala>
    {
        { "Bem-vindo :)", Louro("Viu só? Tranquilo! Só {0} morte(s)... por enquanto. Currupaco!") },
        { "Confia em mim (e corre!)", Marina("Grumete, você confiou no Louro E fugiu de uma SERRA? Toma cuidado, tá?") },
        { "O Final (mudei de ideia)", Louro("\"O Final\". Hahaha. Você acreditou mesmo? E pegou TODAS as moedas? Dizem que isso abre um caminho...") },
        { "A Capitã Âncora", Louro("Venceu a Capitã! Ela mandou pedir desculpas pelas {0} vez(es). Mentira, ela não mandou.") },
        { "Cadê a direita? (Foge, bloco!)", Doris("Bombordo é estibordo... e os blocos fogem. Buuu. Você está chegando perto do meu navio.") },
        { "Abordagem!", Louro("Abordagem completa! Vermelho cai, amarelo dispara. E você caiu {0} vez(es) no mar. Anotou?") },
        { "A Gruta da Cozinheira", Louro("Venceu a Cozinheira! Ela quase te cozinhou {0} vez(es). E fugiu pro navio fantasma. Adivinha pra onde a gente vai?") },
        { "O Navio Fantasma", Doris("Você achou a porta certa E dançou no meu convés. Não que eu estivesse esperando. Buuu.") },
        { "Vento em Popa!", Louro("O vento mandou dizer: \"o grumete voou {0} vez(es). Previsível.\"") },
        { "A Baleia Branca", Marina("Grumete! Foi o KRAKEN que mandou a Baleia. A lula vermelha me prendeu no covil dela. Vem logo!") },
        { "O Tsunami (CORRE!)", Louro("O tsunami te pegou {0} vez(es). Isso foi o Kraken espirrando. Imagina quando ela ficar brava.") },
        { "O Verdadeiro Final (juro)", Louro("Ops. Esqueci de falar do Kraken Vermelho. Ela rouba moedas. Boa sorte! :)") },
    };

    // ------------------------------------------------------------------ a história nas falas do mapa

    // Quem mais fala depois dos chefes (veja a história em Historia.cs): o chefe, livre da maldição, e o
    // Deus da Riqueza, de olho no tesouro dele. O retrato é o desenho do jogo ("capita_parada"...).
    static Fala Capita(string texto) => new Fala { quem = "Capitã", sprite = "capita_parada", texto = texto };
    static Fala Cozinheira(string texto) => new Fala { quem = "Cozinheira", sprite = "cozinheira_chefe", texto = texto };
    static Fala Deus(string texto) => new Fala { quem = "Deus da Riqueza", sprite = "deus_da_riqueza", texto = texto };

    // Falas que vêm DEPOIS da fala normal da fase (uma atrás da outra: ENTER passa), pelo nome da fase.
    static readonly Dictionary<string, Fala[]> DepoisDaFala = new Dictionary<string, Fala[]>
    {
        {
            "A Capitã Âncora", new[]
            {
                Capita("Ai, minha cabeça... Desculpa, Grumete! Era a maldição que me deixava furiosa. E eu MANDEI pedir desculpas, papagaio!"),
                Deus("Muito bem, pirata. Cada alma que você liberta devolve um pouco do equilíbrio. Meu tesouro está mais perto..."),
            }
        },
        {
            "A Gruta da Cozinheira", new[]
            {
                Cozinheira("Hihi... ops! Eu queria mesmo te cozinhar? Que vergonha! Era a maldição. Vou me esconder no navio fantasma até passar."),
                Deus("Mais uma alma livre. Continue, pirata. O Kraken já sente você chegando."),
            }
        },
        {
            "A Baleia Branca", new[]
            {
                Deus("A Baleia voltou a ser só uma baleia. Agora só falta o Kraken Vermelho. E o MEU tesouro."),
            }
        },
    };

    // No modo difícil (o Grumete roubou o tesouro) as falas são outras: ninguém está agradecendo mais.
    static readonly Dictionary<string, Fala[]> NaTempestade = new Dictionary<string, Fala[]>
    {
        { "Bem-vindo :)", new[] { Louro("Bem-vindo de volta! Agora com tsunami. Eu falei pra roubar, né? Currupaco!") } },
        { "Confia em mim (e corre!)", new[] { Marina("Ouvi dizer que você roubou o tesouro. Grumete... por quê?") } },
        { "O Final (mudei de ideia)", new[] { Louro("\"O Final\" de novo. Viu só? Nem roubando o jogo acaba. Hahaha.") } },
        {
            "A Capitã Âncora", new[]
            {
                Capita("A maldição voltou, Grumete, e a culpa é sua! Da próxima vez a âncora vai na testa."),
                Deus("Ladrão. Cada ilha que você cruza afunda mais um pouco. Devolva o que é MEU."),
            }
        },
        { "Cadê a direita? (Foge, bloco!)", new[] { Doris("Até os blocos fogem de você agora, ladrãozinho. Buuu.") } },
        { "Abordagem!", new[] { Louro("Abordagem completa, {0} queda(s) no mar! O ouro pesa, né?") } },
        {
            "A Gruta da Cozinheira", new[]
            {
                Cozinheira("Hihi! Voltei a ser má, e adivinha de quem é a culpa? Vou guardar uma faca pra você."),
                Deus("Meu ouro não é seu, pirata. O mar inteiro vai cobrar."),
            }
        },
        { "O Navio Fantasma", new[] { Doris("Até eu devolvia. E olha que eu sou fantasma. Buuu.") } },
        { "Vento em Popa!", new[] { Louro("O vento segura a onda, mas não segura a sua consciência. Currupaco!") } },
        {
            "A Baleia Branca", new[]
            {
                Marina("A Baleia era só uma baleia de novo, Grumete. Até você roubar."),
                Deus("Falta pouco para o fim do mar. Ainda dá para me devolver."),
            }
        },
        { "O Tsunami (CORRE!)", new[] { Louro("Essa onda já te pegou {0} vez(es). E agora ela está em TODAS as ilhas. Que honra!") } },
        { "O Verdadeiro Final (juro)", new[] { Louro("Ops. O Kraken voltou. E ela QUER o baú de volta. Boa sorte! :)") } },
    };

    // A primeira fala no mapa, logo depois de roubar.
    public static readonly Fala ComecoDaTempestade =
        Louro("Currupaco! 100 moedas no bolso e um tsunami em cada ilha. Aperta L e gasta na Loja. Eu falei pra roubar!");

    // A fala de depois da fase e as que vêm atrás dela (a primeira é a de Escolher).
    public static Fala[] FalasDepoisDaFase(int faseConcluida, int mortes)
    {
        if (GerenciadorDoJogo.Tempestade && faseConcluida != Fases.IndiceSecreto
            && NaTempestade.TryGetValue(Fases.Dados(faseConcluida).nome, out Fala[] naTempestade))
            return naTempestade;
        Fala primeira = Escolher(faseConcluida, mortes);
        if (faseConcluida == Fases.IndiceSecreto || !DepoisDaFala.TryGetValue(Fases.Dados(faseConcluida).nome, out Fala[] depois))
            return new[] { primeira };
        var todas = new Fala[depois.Length + 1];
        todas[0] = primeira;
        depois.CopyTo(todas, 1);
        return todas;
    }

    // Fala genérica para uma fase sem fala própria.
    static readonly Fala FalaPadrao = Louro("Passou! Com só {0} morte(s). Eu tô impressionado. Um pouco.");

    public static readonly Fala SecretaLiberada =
        Louro("Todas as moedas?! Abriu um caminho na lagoa! Aperte S no ponto 3.");

    public static readonly Fala DepoisDaSecreta =
        Louro("Você achou a ilha secreta! Sério, como? Nem eu lembrava dela.");

    // ------------------------------------------------------------------ tutorial da loja

    // A cutscene do Louro que apresenta a Loja, logo depois da 2ª fase (uma vez só). Uma fala por passo,
    // na ordem em que aparecem (veja GerenciadorDoJogo.PassoDoTutorial). A fonte pixel não tem "ª".
    public const string LojaChegada = "Currupaco! Passou da segunda fase? Então você já pode conhecer a LOJA DO PORTO!";
    public const string LojaMoedas = "Sabe as moedas que você pega nas fases? Elas ficam guardadas. Na Loja, elas viram ajudinhas!";
    public const string LojaComoAbrir = "Para abrir a Loja, aperte L aqui no mapa (ou clique em LOJA, ali no canto de baixo). Vai, aperta!";
    public const string LojaComprar = "Esse é o ESCUDO: ele te salva de UMA morte. Aperte ENTER e compre o seu!";
    // {0} vira "3 moedas" (ou "1 moeda"): o que faltava para o escudo, que o Louro completa.
    public const string LojaComprarComAjuda = "Hmm, faltam {0}... Toma, eu completo! Só desta vez, hein? Agora aperte ENTER e compre o ESCUDO.";
    public const string LojaComprou = "Prontinho! Viu a bolha azul no Grumete? Se alguma coisa te pegar, o escudo quebra no seu lugar.";
    public const string LojaJaTinha = "Opa, você já tem um escudo! Ele te salva de UMA morte. Guarda as moedas para o próximo.";
    public const string LojaFechar = "A Loja fica sempre aqui: é só apertar L no mapa. Agora aperte ESC para voltar. Boa viagem!";
    public static readonly Fala LojaTchau = Louro("E não vai gastar esse escudo à toa, hein? Currupaco!");

    // A fala do modo de teste (F9 no mapa).
    public static readonly Fala ModoDeTeste =
        Louro("Modo de teste! Liberei o mapa inteiro, até o Kraken. Não conta pra ninguém. :3");

    // As falas do F8 no mapa (liga e desliga o modo difícil).
    public static readonly Fala TesteTempestadeLigada = Louro("Modo de teste! Liguei a tempestade: um tsunami em cada fase. Currupaco!");
    public static readonly Fala TesteTempestadeDesligada = Louro("Modo de teste! Desliguei a tempestade. O mar acalmou. Por enquanto.");

    static readonly Fala[] SemMorrer =
    {
        Louro("Nenhuma morte?! Suspeito de hack."),
        Marina("Nenhuma vez? Grumete, você é incrível!"),
        Doris("Sem morrer? Que sorte irritante. Buuu."),
    };

    public static Fala Escolher(int faseConcluida, int mortes)
    {
        if (faseConcluida == Fases.IndiceSecreto) return DepoisDaSecreta;
        if (mortes == 0) return SemMorrer[faseConcluida % SemMorrer.Length];
        return DepoisDaFase.TryGetValue(Fases.Dados(faseConcluida).nome, out Fala fala) ? fala : FalaPadrao;
    }

    // ------------------------------------------------------------------ créditos

    // O nome do jogo (o logo do título e dos créditos).
    public const string NomeDoJogo = "CILADA À VISTA!";

    // ===== NOMES DA EQUIPE: troque "Integrante 1" ... "Integrante 4" pelos nomes de vocês. =====
    // Eles aparecem logo no começo dos créditos (dá para pôr mais ou menos nomes: é só uma lista).
    // Dica: escreva com maiúsculas e minúsculas ("Ana Souza"); uma linha TODA em maiúsculas vira título amarelo.
    public static readonly string[] Equipe =
    {
        "Integrante 1",
        "Integrante 2",
        "Integrante 3",
        "Integrante 4",
    };

    // Os créditos que sobem na tela (no fim do jogo e pela opção "Créditos" do título).
    // A 1ª linha vira o logo; linhas TODAS EM MAIÚSCULAS são títulos de seção (em amarelo).
    // A fonte pixel não tem "_", "&", "[" nem setas: evite esses caracteres aqui.
    public static readonly string[] Creditos = MontarCreditos();

    static string[] MontarCreditos()
    {
        var linhas = new List<string> { NomeDoJogo, "", "EQUIPE" };
        linhas.AddRange(Equipe);
        linhas.AddRange(new[]
        {
            "",
            "UM JOGO ORIGINAL",
            "Trabalho de faculdade, sem fins lucrativos.",
            "Personagens, fases e história criados pela equipe.",
            "",
            "FEITO PELA EQUIPE",
            "Programação, fases e pegadinhas",
            "Pixel art desenhada por código",
            "Música original gerada por código",
            "(as fases, o mapa e uma música para cada chefe)",
            "Efeitos sonoros gerados por código",
            "",
            "INSPIRADO EM",
            "Cat Mario e Level Devil (as pegadinhas)",
            "Super Mario World (o mapa)",
            "Helltaker (as lutas de chefe)",
            "Moby Dick, de Herman Melville (a Baleia Branca)",
            "As lendas de marinheiro do Kraken (a lula vermelha)",
            "",
            "FEITO EM UNITY 6",
            "",
            "",
            "Obrigado por jogar!",
            "(e por morrer tantas vezes)",
        });
        return linhas.ToArray();
    }

    // ------------------------------------------------------------------ como jogar

    // Uma linha da tela "Como Jogar": à esquerda a tecla (ou o assunto), à direita o que ela faz.
    public struct LinhaDeAjuda
    {
        public string tecla;
        public string texto;
        public LinhaDeAjuda(string tecla, string texto)
        {
            this.tecla = tecla;
            this.texto = texto;
        }
    }

    public struct PaginaDeAjuda
    {
        public string titulo;
        public LinhaDeAjuda[] linhas;
    }

    // As páginas da tela "Como Jogar" (esquerda/direita troca de página). Cabem umas 9 linhas por página.
    public static readonly PaginaDeAjuda[] ComoJogar =
    {
        new PaginaDeAjuda
        {
            titulo = "CONTROLES",
            linhas = new[]
            {
                new LinhaDeAjuda("A/D ou SETAS", "andar"),
                new LinhaDeAjuda("ESPAÇO, W ou CIMA", "pular (segure para pular mais alto)"),
                new LinhaDeAjuda("S ou BAIXO", "entrar nas portas das cabines"),
                new LinhaDeAjuda("R", "reiniciar do save (conta como morte. Óbvio.)"),
                new LinhaDeAjuda("ESC", "pausar o jogo / voltar nos menus"),
                new LinhaDeAjuda("ENTER", "confirmar (pula a animação da morte)"),
                new LinhaDeAjuda("NO MAPA", "SETAS andam, ENTER joga. BAIXO? Só se achar o segredo..."),
                new LinhaDeAjuda("MOUSE", "aponte e clique nos menus (botão direito volta)"),
                new LinhaDeAjuda("CONTROLE", "direcional anda, A pula, Y reinicia, START pausa, B volta"),
            },
        },
        new PaginaDeAjuda
        {
            titulo = "DICAS (CONFIE EM MIM)",
            linhas = new[]
            {
                new LinhaDeAjuda("NADA É O QUE PARECE", "chão, moedas, nuvens... até a bandeira pode mentir."),
                new LinhaDeAjuda("MORRER FAZ PARTE", "é a Volta da Maré: você volta e tenta de novo."),
                new LinhaDeAjuda("O LOURO", "o papagaio dá dicas... não confie nelas. :)"),
                new LinhaDeAjuda("CRISTAL DE SAVE", "encostou, você renasce ali. Às vezes ele muda de lugar."),
                new LinhaDeAjuda("CHEFES", "desvie e, quando ele ficar tonto, PULE NA CABEÇA dele!"),
                new LinhaDeAjuda("ETAPAS DO CHEFE", "cada etapa vencida vale como checkpoint da luta."),
                new LinhaDeAjuda("BARRA NO TOPO", "o quanto falta até a bandeira (a mais longe, hehe)."),
                new LinhaDeAjuda("SALVAR", "o jogo salva sozinho: dá para sair e Continuar depois."),
                new LinhaDeAjuda("LOJA (L, NO MAPA)", "depois da fase 2: escudos, elixir de super salto, duplo salto e a roleta."),
            },
        },
        new PaginaDeAjuda
        {
            titulo = "RECORDES E MEDALHAS",
            linhas = new[]
            {
                new LinhaDeAjuda("RECORDES", "cada fase guarda o seu melhor tempo, menos mortes e mais moedas."),
                new LinhaDeAjuda("TEMPO", "conta desde que você entra pelo mapa (as mortes contam!)."),
                new LinhaDeAjuda("MEDALHA DO RELÓGIO", "passar a fase dentro do tempo da meta."),
                new LinhaDeAjuda("MEDALHA DO CORAÇÃO", "passar a fase sem morrer nenhuma vez."),
                new LinhaDeAjuda("MEDALHA DA MOEDA", "pegar TODAS as moedas numa tentativa só."),
                new LinhaDeAjuda("NO MAPA", "as medalhas ficam do lado de cada fase (apagada = falta)."),
                new LinhaDeAjuda("UM POR SLOT", "cada slot tem os seus recordes e medalhas (apagar o slot apaga eles)."),
            },
        },
    };
}
