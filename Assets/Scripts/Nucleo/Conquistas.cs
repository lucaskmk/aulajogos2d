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
        new Conquista("primeira_morte", "A maré te trouxe", "Morrer pela primeira vez."),
        new Conquista("cem_mortes", "Grumete imortal", "Morrer 100 vezes numa partida."),
        new Conquista("bandeira_falsa", "Confiou na bandeira", "Morrer para uma bandeira falsa."),
        new Conquista("moeda_assassina", "Ganância", "Pegar uma moeda que não era moeda."),
        new Conquista("nuvem", "Olhou para o céu", "Ser mordido por uma nuvem."),
        new Conquista("sem_morrer", "Suspeito de hack", "Passar de uma fase sem morrer."),
        new Conquista("pular_morte", "Sem tempo para luto", "Pular a animação da morte 20 vezes."),
        new Conquista("caranguejos", "Pescador de caranguejos", "Pisar em 10 caranguejos."),
        new Conquista("ratos", "Controle de pragas", "Pisar em 10 Ratos do Porão."),
        new Conquista("baleia", "Caçador de Baleias", "Derrotar a Baleia Branca."),
        new Conquista("portas", "Viajante das cabines", "Atravessar 20 portas das Cabines Assombradas."),
        new Conquista("cabines", "Buuu!", "Encontrar a Dóris nas Cabines Assombradas."),
        new Conquista("save_mudou", "Mudaram meu save!", "Ver o ponto de save mudar de lugar."),
        new Conquista("secreta", "Explorador", "Terminar a fase secreta."),
        new Conquista("expressa", "Passageiro da Expressa", "Ir até o fim na plataforma expressa sem pular."),
        new Conquista("cozinheira", "Picadinho", "Levar uma faca da Cozinheira."),
        new Conquista("cozinheira_vencida", "Acenderam a luz", "Derrotar a Cozinheira do navio fantasma."),
        new Conquista("mare_negra", "Lanche do Kraken", "Ser pego pelos tentáculos da Maré Negra."),
        new Conquista("capita_furiosa", "Fúria da Capitã", "Ser pego pela Capitã Âncora depois que ela fica furiosa."),
        new Conquista("capita_vencida", "Âncora levantada", "Derrotar a Capitã Âncora (pulando na cabeça dela, coitada)."),
        new Conquista("vento_forte", "Levado pelo vento", "Morrer empurrado por uma rajada de vento (ou largado por um redemoinho)."),
        new Conquista("fora_do_ritmo", "Fora do Ritmo", "Pisar no bloco do ritmo que não era do ritmo."),
        new Conquista("medalhas", "Perfeccionista", "Ganhar as 3 medalhas de uma fase (tempo, sem morrer e moedas)."),
        new Conquista("zerou", "Terra à vista!", "Zerar o jogo."),
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
