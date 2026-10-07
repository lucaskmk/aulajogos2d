using UnityEngine;

// Conquistas (salvas em PlayerPrefs, valem para sempre, não só para a partida).
// Para desbloquear de qualquer lugar do código: Conquistas.Desbloquear("id").
// Para as de "faça X vezes": Conquistas.Contar("id", quantasVezes).
// A Interface mostra um aviso quando uma nova é desbloqueada, e a lista fica no título.
public static class Conquistas
{
    public class Conquista
    {
        public string id, nome, descricao;
        public Conquista(string id, string nome, string descricao)
        {
            this.id = id;
            this.nome = nome;
            this.descricao = descricao;
        }
    }

    public static readonly Conquista[] Todas =
    {
        new Conquista("primeira_morte", "Retorno pela Morte", "Morrer pela primeira vez."),
        new Conquista("cem_mortes", "Natsuki Subaru honorário", "Morrer 100 vezes numa partida."),
        new Conquista("bandeira_falsa", "Confiou na bandeira", "Morrer para uma bandeira falsa."),
        new Conquista("moeda_assassina", "Ganância", "Pegar uma moeda que não era moeda."),
        new Conquista("nuvem", "Olhou para o céu", "Ser mordido por uma nuvem."),
        new Conquista("sem_morrer", "Suspeito de hack", "Passar de uma fase sem morrer."),
        new Conquista("pular_morte", "Sem tempo para luto", "Pular a animação da morte 20 vezes."),
        new Conquista("mabeast", "Domador de Mabeasts", "Pisar em 10 Mabeasts."),
        new Conquista("coelhos", "Controle de pragas", "Pisar em 10 Grandes Coelhos."),
        new Conquista("baleia", "Caçador de Baleias", "Derrotar a Baleia Branca."),
        new Conquista("portas", "Viajante da biblioteca", "Atravessar 20 portas da Beatrice."),
        new Conquista("biblioteca", "Kashira!", "Encontrar a Beatrice na Biblioteca Proibida."),
        new Conquista("save_mudou", "Mudaram meu save!", "Ver o ponto de save mudar de lugar."),
        new Conquista("secreta", "Explorador", "Terminar a fase secreta."),
        new Conquista("expressa", "Passageiro da Expressa", "Ir até o fim na plataforma expressa sem pular."),
        new Conquista("elsa", "Entranhas à mostra", "Levar uma faca da Elsa."),
        new Conquista("miasma", "Abraço da Bruxa", "Ser pego pelo miasma da Bruxa da Inveja."),
        new Conquista("rem_brava", "Oni de cabelo azul", "Ser pego pela Rem depois que ela vira oni."),
        new Conquista("rem_vencida", "Faxina cancelada", "Derrotar a Rem (pulando na cabeça dela, coitada)."),
        new Conquista("vento_da_ram", "Levado pelo vento", "Morrer empurrado por uma rajada da Ram (ou largado por um redemoinho)."),
        new Conquista("fora_do_ritmo", "Fora do Ritmo", "Pisar no bloco do ritmo que não era do ritmo."),
        new Conquista("zerou", "Começando do zero", "Zerar o jogo."),
    };

    const string Prefixo = "cilada_conquista_";

    // A última desbloqueada e quando (tempo real), para o aviso na tela.
    public static Conquista Recente { get; private set; }
    public static float QuandoFoi { get; private set; } = -100f;

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar()
    {
        Recente = null;
        QuandoFoi = -100f;
    }

    public static bool Tem(string id) => PlayerPrefs.GetInt(Prefixo + id, 0) == 1;

    public static int Quantidade
    {
        get
        {
            int total = 0;
            foreach (Conquista c in Todas) if (Tem(c.id)) total++;
            return total;
        }
    }

    public static void Desbloquear(string id)
    {
        if (Tem(id)) return;
        Conquista conquista = System.Array.Find(Todas, c => c.id == id);
        if (conquista == null)
        {
            Debug.LogWarning("Conquista desconhecida: " + id);
            return;
        }
        PlayerPrefs.SetInt(Prefixo + id, 1);
        PlayerPrefs.Save();
        Recente = conquista;
        QuandoFoi = Time.unscaledTime;
        GerenciadorDoJogo.Som("conquista");
    }

    // Soma 1 no contador da conquista e desbloqueia quando chegar na meta.
    public static void Contar(string id, int meta)
    {
        if (Tem(id)) return;
        int vezes = PlayerPrefs.GetInt(Prefixo + id + "_vezes", 0) + 1;
        PlayerPrefs.SetInt(Prefixo + id + "_vezes", vezes);
        if (vezes >= meta) Desbloquear(id);
    }
}
