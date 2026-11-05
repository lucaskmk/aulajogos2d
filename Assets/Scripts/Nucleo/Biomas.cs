// O LUGAR de cada fase (Fase.bioma, em Fases.cs): ele escolhe o chão (grama, areia, madeira de navio, pedra...),
// os enfeites em cima do chão, o bicho que anda pela fase (o 'E': caranguejo, sapo, escorpião...) e as
// camadas pintadas do fundo (Assets/Resources/Arte/<pasta>/camada_*.png: veja Ferramentas/prompts_dos_fundos.md).
// Tudo o que é igual entre os lugares (as armadilhas, as moedas, o jogador) não muda.
public enum Bioma { Porto, Selva, Lagoa, Forte, Vila, Navios, Gruta, NavioFantasma, Penhasco, Recife, Praia, Rochedo, AltoMar }

public static class Biomas
{
    // O jeito do chão de cada lugar (ArteDosBlocos pinta um atlas para cada um).
    public static ArteDosBlocos.Estilo Estilo(Bioma bioma)
    {
        switch (bioma)
        {
            case Bioma.Praia: return ArteDosBlocos.Estilo.Areia;
            case Bioma.Forte: return ArteDosBlocos.Estilo.Pedra;
            case Bioma.Gruta: return ArteDosBlocos.Estilo.Gruta;
            case Bioma.Navios:
            case Bioma.AltoMar: return ArteDosBlocos.Estilo.Madeira;
            case Bioma.NavioFantasma: return ArteDosBlocos.Estilo.Fantasma;
            case Bioma.Penhasco:
            case Bioma.Rochedo: return ArteDosBlocos.Estilo.Seca;
            case Bioma.Recife: return ArteDosBlocos.Estilo.Recife;
            default: return ArteDosBlocos.Estilo.Grama;
        }
    }

    // O bicho que anda pela fase (o mesmo inimigo, com outro desenho do mesmo tamanho: veja ArteDosPersonagens).
    public static string Inimigo(Bioma bioma)
    {
        switch (bioma)
        {
            case Bioma.Porto: return "bicho_gaivota";
            case Bioma.Selva: return "bicho_sapo";
            case Bioma.Lagoa: return "bicho_tartaruga";
            case Bioma.Forte: return "bicho_caveira";
            case Bioma.Vila: return "bicho_galinha";
            case Bioma.Navios: return "bicho_macaco";
            case Bioma.Gruta: return "bicho_aranha";
            case Bioma.NavioFantasma: return "bicho_fantasma";
            case Bioma.Penhasco: return "bicho_escorpiao";
            case Bioma.Recife: return "bicho_estrela";
            case Bioma.Rochedo: return "bicho_lagarto";
            case Bioma.AltoMar: return "bicho_polvo";
            default: return "inimigo"; // o caranguejo (a praia)
        }
    }

    // A pasta das camadas pintadas do fundo (dentro de Assets/Resources/Arte). Sem a pasta, usa o fundo de sempre.
    public static string Pasta(Bioma bioma)
    {
        switch (bioma)
        {
            case Bioma.NavioFantasma: return "navio_fantasma";
            case Bioma.AltoMar: return "alto_mar";
            default: return bioma.ToString().ToLowerInvariant();
        }
    }

    // Nuvens de enfeite no céu (na gruta não tem céu; no alto-mar do Kraken o céu pintado da tempestade já basta).
    public static bool TemNuvens(Bioma bioma) => bioma != Bioma.Gruta && bioma != Bioma.AltoMar;

    // O capim desfocado na frente da fase (HD-2D): só onde tem chão de terra. Nos navios, no mar e na gruta, não.
    public static bool TemCapimNaFrente(Bioma bioma) =>
        bioma == Bioma.Porto || bioma == Bioma.Selva || bioma == Bioma.Lagoa || bioma == Bioma.Forte || bioma == Bioma.Vila ||
        bioma == Bioma.Praia || bioma == Bioma.Penhasco || bioma == Bioma.Rochedo;

    // Galhos e moitas na frente da fase: só onde tem árvore.
    public static bool TemGalhosNaFrente(Bioma bioma) =>
        bioma == Bioma.Porto || bioma == Bioma.Selva || bioma == Bioma.Lagoa || bioma == Bioma.Vila || bioma == Bioma.Praia;

    // Sol (ou lua) no céu: na gruta não tem céu, e nas tempestades as nuvens escondem.
    public static bool TemSolOuLua(Bioma bioma) => bioma != Bioma.Gruta && bioma != Bioma.Praia && bioma != Bioma.AltoMar;

    // Pôr do sol: o sol fica baixo, quase encostando no mar.
    public static bool SolBaixo(Bioma bioma) => bioma == Bioma.Navios || bioma == Bioma.Rochedo;

    // Gaivotas: não entram na gruta, no navio fantasma nem nas tempestades.
    public static bool TemGaivotas(Bioma bioma) => TemSolOuLua(bioma) && bioma != Bioma.NavioFantasma;

    // Os enfeites em cima do chão, em porcentagem (o resto, até 100, fica vazio ou vira planta onde tem planta).
    public struct Enfeites
    {
        public int lampiao, arbusto, barril, pedra, estrela, plantas; // plantas = flor e capim
    }

    public static Enfeites EnfeitesDe(Bioma bioma)
    {
        switch (Estilo(bioma))
        {
            case ArteDosBlocos.Estilo.Areia: return new Enfeites { lampiao = 2, arbusto = 10, barril = 8, pedra = 18, estrela = 22, plantas = 10 };
            case ArteDosBlocos.Estilo.Madeira: return new Enfeites { lampiao = 9, barril = 28 };
            case ArteDosBlocos.Estilo.Fantasma: return new Enfeites { lampiao = 7, barril = 14 };
            case ArteDosBlocos.Estilo.Gruta: return new Enfeites { lampiao = 5, pedra = 35 };
            case ArteDosBlocos.Estilo.Pedra: return new Enfeites { lampiao = 6, barril = 10, pedra = 15, plantas = 18 };
            case ArteDosBlocos.Estilo.Seca: return new Enfeites { lampiao = 2, barril = 5, pedra = 28, plantas = 30 };
            case ArteDosBlocos.Estilo.Recife: return new Enfeites { pedra = 30, estrela = 28, plantas = 18 };
            default: return new Enfeites { lampiao = 5, arbusto = 13, barril = 8, pedra = 9, estrela = 7, plantas = 58 }; // a grama de sempre
        }
    }
}
