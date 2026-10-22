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
        { "Confia em mim", Louro("Eu disse \"confia em mim\". Você confiou. {0} vez(es).") },
        { "Corre!", Marina("Grumete, você estava fugindo de uma SERRA? Toma cuidado, tá?") },
        { "O Final (sem pegadinhas)", Louro("\"O Final (sem pegadinhas)\". Hahaha. Você acreditou mesmo?") },
        { "Eu mudo de ideia", Louro("Pegou TODAS as moedas? Dizem que isso abre um caminho escondido...") },
        { "Cadê a direita?", Louro("Bombordo é estibordo, estibordo é bombordo. {0} morte(s). Faz sentido.") },
        { "Foge, bloco!", Doris("Buuu. Você está chegando perto das minhas cabines.") },
        { "Ponte para o Porto", Louro("Vermelho cai, amarelo dispara. E você caiu {0} vez(es). Anotou?") },
        { "Cabines Assombradas", Doris("Você achou a porta certa. Não que eu estivesse esperando. Buuu.") },
        { "A Capitã Âncora", Louro("Venceu a Capitã! Ela mandou pedir desculpas pelas {0} vez(es). Mentira, ela não mandou.") },
        { "Vento em Popa!", Louro("O vento mandou dizer: \"o grumete voou {0} vez(es). Previsível.\"") },
        { "Dança das Marés", Louro("Tic, tic, TAC... e você caiu {0} vez(es). A maré mandou dizer que você dança muito mal.") },
        { "A Cozinheira do Porão", Louro("Venceu a Cozinheira! Ela quase te cozinhou {0} vez(es). E disse que volta. Ela SEMPRE volta.") },
        { "A Maré Negra (CORRE!)", Louro("A Maré Negra te pegou {0} vezes. Ela gosta MESMO de você, hein? Eu ficaria preocupado.") },
        { "O Verdadeiro Final (juro)", Louro("Ops. Esqueci de falar da Baleia Branca. Boa sorte! :)") },
    };

    // Fala genérica para uma fase sem fala própria.
    static readonly Fala FalaPadrao = Louro("Passou! Com só {0} morte(s). Eu tô impressionado. Um pouco.");

    public static readonly Fala SecretaLiberada =
        Louro("Todas as moedas?! Abriu um caminho na lagoa! Aperte S no ponto 5.");

    public static readonly Fala DepoisDaSecreta =
        Louro("Você achou a ilha secreta! Sério, como? Nem eu lembrava dela.");

    // A fala do modo de teste (F9 no mapa).
    public static readonly Fala ModoDeTeste =
        Louro("Modo de teste! Liberei o mapa inteiro, até a Baleia. Não conta pra ninguém. :3");

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
                new LinhaDeAjuda("VALEM PARA SEMPRE", "um Novo jogo não apaga recordes nem medalhas."),
            },
        },
    };
}
