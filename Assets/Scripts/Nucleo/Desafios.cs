using UnityEngine;

// OS DESAFIOS (aparecem depois de zerar): um jeito diferente de passar por uma fase que você já conhece.
// Nada de facilitar: cada desafio só tira alguma coisa (as moedas, o duplo salto, o escudo, as bolhas de ar)
// ou pede pressa. No mapa, o ponto da fase mostra o desafio dela; passou cumprindo, fica marcado para sempre
// (vale para o jogo todo, como as conquistas). A conquista "Desafiante" vem com 4, e "Sem desculpas" com todos.
//
// O que conta é a TENTATIVA em que você passou (morrer no meio zera os contadores; o relógio é o da fase toda).
public static class Desafios
{
    public enum Regra { Tempo, SemMoedas, SemPisao, SemDuploSalto, SemBolhaDeAr, SemEscudo, SemMorrer }

    public class Desafio
    {
        public string id, titulo, descricao;
        public int fase;
        public Regra regra;
        public float tempo;
        public Desafio(string id, int fase, Regra regra, string titulo, string descricao, float tempo = 0f)
        {
            this.id = id; this.fase = fase; this.regra = regra; this.titulo = titulo; this.descricao = descricao; this.tempo = tempo;
        }
    }

    public static readonly Desafio[] Todos =
    {
        new Desafio("relampago", 0, Regra.Tempo, "Relâmpago", "Passar a fase 1 em até 16 segundos.", 16f),
        new Desafio("mao_fechada", 2, Regra.SemMoedas, "Mão fechada", "Passar a fase 3 sem pegar NENHUMA moeda."),
        new Desafio("pacifista", 4, Regra.SemPisao, "Pacifista", "Passar a fase 5 sem pisar em nenhum bicho."),
        new Desafio("sem_escudo_capita", 3, Regra.SemEscudo, "Sem armadura", "Vencer a Capitã sem gastar escudo."),
        new Desafio("sem_asas", 10, Regra.SemDuploSalto, "Sem asas", "Passar o Tsunami sem usar o duplo salto."),
        new Desafio("perfeito_12", 11, Regra.SemMorrer, "Juro mesmo", "Passar O Verdadeiro Final sem morrer."),
        new Desafio("folego", Fases.PrimeiroDoMar + 1, Regra.SemBolhaDeAr, "Fôlego de sereia", "Passar Bolha que Mente sem pegar bolha de ar."),
        new Desafio("forja_rapida", Fases.PrimeiroDoMar + 3, Regra.Tempo, "Ferro quente", "Vencer o Tubarão-Martelo em até 100 segundos.", 100f),
    };

    const string Prefixo = "cilada_desafio_";

    public static bool Feito(Desafio d) => Guardado.GetInt(Prefixo + d.id, 0) == 1;

    public static int Quantos
    {
        get
        {
            int n = 0;
            foreach (Desafio d in Todos) if (Feito(d)) n++;
            return n;
        }
    }

    // Os desafios só aparecem depois de zerar (vencer o Kraken).
    public static bool Liberados => Progresso.FaseMaxima >= Fases.Todas.Length - 1 || Progresso.MarLiberado;

    public static Desafio DaFase(int fase)
    {
        foreach (Desafio d in Todos) if (d.fase == fase) return d;
        return null;
    }

    // Contadores da TENTATIVA (zerados quando a fase é montada de novo: GerenciadorDoJogo.CarregarFase).
    public static int Pisoes, DuplosSaltos, BolhasDeAr, Escudos;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ZerarTentativa() => Pisoes = DuplosSaltos = BolhasDeAr = Escudos = 0;

    // Fim da fase: cumpriu o desafio dela? Devolve o desafio cumprido AGORA (null = nenhum).
    public static Desafio Conferir(int fase, float tempoNaFase, int moedasNaTentativa, int mortesNaFase)
    {
        if (!Liberados) return null;
        Desafio d = DaFase(fase);
        if (d == null || Feito(d)) return null;
        bool cumpriu;
        switch (d.regra)
        {
            case Regra.Tempo: cumpriu = tempoNaFase <= d.tempo; break;
            case Regra.SemMoedas: cumpriu = moedasNaTentativa == 0; break;
            case Regra.SemPisao: cumpriu = Pisoes == 0; break;
            case Regra.SemDuploSalto: cumpriu = DuplosSaltos == 0; break;
            case Regra.SemBolhaDeAr: cumpriu = BolhasDeAr == 0; break;
            case Regra.SemEscudo: cumpriu = Escudos == 0; break;
            default: cumpriu = mortesNaFase == 0; break;
        }
        if (!cumpriu) return null;
        Guardado.SetInt(Prefixo + d.id, 1);
        Guardado.Save();
        if (Quantos >= 4) Conquistas.Desbloquear("desafiante");
        if (Quantos >= Todos.Length) Conquistas.Desbloquear("todos_desafios");
        return d;
    }
}
