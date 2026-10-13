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

    static Fala Puck(string texto) => new Fala { quem = "Puck", sprite = "puck", texto = texto };
    static Fala Emilia(string texto) => new Fala { quem = "Emilia", sprite = "emilia", texto = texto };
    static Fala Beatrice(string texto) => new Fala { quem = "Beatrice", sprite = "beatrice", texto = texto };

    // Uma fala para depois de cada fase, pelo NOME da fase (assim dá para reordenar as fases à vontade).
    static readonly Dictionary<string, Fala> DepoisDaFase = new Dictionary<string, Fala>
    {
        { "Bem-vindo :)", Puck("Viu só? Tranquilo! Só {0} morte(s)... por enquanto.") },
        { "Confia em mim", Puck("Eu disse \"confia em mim\". Você confiou. {0} vez(es).") },
        { "Corre!", Emilia("Subaru, você estava fugindo de uma SERRA? Toma cuidado, tá?") },
        { "O Final (sem pegadinhas)", Puck("\"O Final (sem pegadinhas)\". Hahaha. Você acreditou mesmo?") },
        { "Eu mudo de ideia", Puck("Pegou TODAS as moedas? Dizem que isso abre um caminho escondido...") },
        { "Cadê a direita?", Puck("Esquerda é direita, direita é esquerda. {0} morte(s). Faz sentido.") },
        { "Foge, bloco!", Beatrice("Hmpf. Você está chegando perto da minha biblioteca, de fato.") },
        { "Ponte para a Capital", Puck("Vermelho cai, amarelo dispara. E você caiu {0} vez(es). Anotou?") },
        { "Biblioteca Proibida", Beatrice("Você achou a porta certa. Não que eu estivesse esperando, kashira.") },
        { "Oni de Cabelo Azul", Puck("Venceu a Rem! Ela mandou pedir desculpas pelas {0} vez(es). Mentira, ela não mandou.") },
        { "Voa, Barusu!", Puck("A Ram mandou dizer: \"Barusu voou {0} vez(es). Previsível.\"") },
        { "Dança das Gêmeas", Puck("Tic, tic, TAC... e você caiu {0} vez(es). A Ram mandou dizer que você dança muito mal.") },
        { "A Caçadora de Entranhas", Puck("Venceu a Elsa! Ela só viu suas entranhas {0} vez(es). E disse que volta. Ela SEMPRE volta.") },
        { "Eu Te Amo (CORRE!)", Puck("A Bruxa te abraçou {0} vezes. Ela gosta MESMO de você, hein? Eu ficaria preocupado.") },
        { "O Verdadeiro Final (juro)", Puck("Ops. Esqueci de falar da Baleia Branca. Boa sorte! :)") },
    };

    // Fala genérica para uma fase sem fala própria.
    static readonly Fala FalaPadrao = Puck("Passou! Com só {0} morte(s). Eu tô impressionado. Um pouco.");

    public static readonly Fala SecretaLiberada =
        Puck("Todas as moedas?! Abriu um caminho no lago! Aperte S no ponto 5.");

    public static readonly Fala DepoisDaSecreta =
        Puck("Você achou a fase secreta! Sério, como? Nem eu lembrava dela.");

    static readonly Fala[] SemMorrer =
    {
        Puck("Nenhuma morte?! Suspeito de hack."),
        Emilia("Nenhuma vez? Subaru, você é incrível!"),
        Beatrice("Sem morrer? Que sorte irritante, de fato."),
    };

    public static Fala Escolher(int faseConcluida, int mortes)
    {
        if (faseConcluida == Fases.IndiceSecreto) return DepoisDaSecreta;
        if (mortes == 0) return SemMorrer[faseConcluida % SemMorrer.Length];
        return DepoisDaFase.TryGetValue(Fases.Dados(faseConcluida).nome, out Fala fala) ? fala : FalaPadrao;
    }

    // ------------------------------------------------------------------ créditos

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
        var linhas = new List<string> { "Re:CILADA!", "", "EQUIPE" };
        linhas.AddRange(Equipe);
        linhas.AddRange(new[]
        {
            "",
            "UM JOGO DE FÃ",
            "Trabalho de faculdade, sem fins lucrativos.",
            "Re:Zero - Começando a Vida em Outro Mundo",
            "pertence a Tappei Nagatsuki, Kadokawa e White Fox.",
            "Personagens e nomes são dos seus donos.",
            "",
            "ÁUDIOS DO ANIME RE:ZERO",
            "O som do Retorno pela Morte",
            "e o trecho da OST que toca quando você morre",
            "são do anime Re:Zero (direitos dos donos).",
            "",
            "FEITO PELA EQUIPE",
            "Programação, fases e pegadinhas",
            "Pixel art desenhada por código",
            "Música orquestral original (piano e cordas)",
            "gerada por código",
            "Efeitos sonoros 8-bit gerados por código",
            "",
            "INSPIRADO EM",
            "Re:Zero (Tappei Nagatsuki)",
            "Cat Mario e Level Devil",
            "Super Mario World (o mapa)",
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
                new LinhaDeAjuda("S ou BAIXO", "entrar nas portas da Beatrice"),
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
                new LinhaDeAjuda("MORRER FAZ PARTE", "é o Retorno pela Morte: você volta e tenta de novo."),
                new LinhaDeAjuda("PLACAS DO PUCK", "leia as dicas... ou não confie nelas. :)"),
                new LinhaDeAjuda("CRISTAL DE SAVE", "encostou, você renasce ali. Às vezes ele muda de lugar."),
                new LinhaDeAjuda("CHEFES", "desvie e, quando ele ficar tonto, PULE NA CABEÇA dele!"),
                new LinhaDeAjuda("ETAPAS DO CHEFE", "cada etapa vencida vale como checkpoint da luta."),
                new LinhaDeAjuda("BARRA NO TOPO", "o quanto falta até a bandeira (a mais longe, hehe)."),
                new LinhaDeAjuda("SALVAR", "o jogo salva sozinho: dá para sair e Continuar depois."),
            },
        },
    };
}
