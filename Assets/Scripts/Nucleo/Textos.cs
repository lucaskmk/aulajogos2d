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

    // Uma fala para depois de cada fase (na ordem das fases).
    static readonly Fala[] DepoisDaFase =
    {
        Puck("Viu só? Tranquilo! Só {0} morte(s)... por enquanto."),
        Puck("Eu disse \"confia em mim\". Você confiou. {0} vez(es)."),
        Emilia("Subaru, você estava fugindo de uma SERRA? Toma cuidado, tá?"),
        Puck("\"O Final (sem pegadinhas)\". Hahaha. Você acreditou mesmo?"),
        Puck("Pegou TODAS as moedas? Dizem que isso abre um caminho escondido..."),
        Puck("Esquerda é direita, direita é esquerda. {0} morte(s). Faz sentido."),
        Beatrice("Hmpf. Você está chegando perto da minha biblioteca, de fato."),
        Beatrice("Você achou a porta certa. Não que eu estivesse esperando, kashira."),
    };

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
        return DepoisDaFase[faseConcluida % DepoisDaFase.Length];
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
