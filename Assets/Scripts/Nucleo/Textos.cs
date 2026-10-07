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

    // Coloque aqui os nomes do grupo.
    public static readonly string[] Creditos =
    {
        "Re:CILADA!",
        "",
        "DESENVOLVIMENTO",
        "(coloque os nomes do grupo aqui)",
        "",
        "PROGRAMAÇÃO, ARTE E SONS",
        "Tudo gerado por código",
        "(pixel art em texto e sons 8-bit)",
        "",
        "MÚSICA",
        "Lofi 8-bit gerada por código",
        "Retorno pela Morte e OST: Re:Zero",
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
    };
}
