using System.Collections.Generic;
using UnityEngine;

// Tudo o que fica salvo entre uma partida e outra (PlayerPrefs):
//  - as partidas em andamento, uma em cada SLOT (3 slots): onde o Grumete está no mapa, até onde ele chegou,
//    mortes, moedas, tempo, itens da Loja e a fase secreta. O título tem "Continuar" (o último slot usado),
//    "Carregar jogo" e "Novo jogo" (os dois abrem a tela dos slots). Tudo de partida usa o SlotAtual;
//  - a fase mais longe que você já chegou em qualquer partida (só informativo: o mapa usa a da partida);
//  - o recorde (zerar com menos mortes), que vale para o jogo todo (aparece no título);
//  - os recordes de CADA FASE (tempo, mortes e moedas), que dão as medalhas (veja Medalhas), e
//    os 5 MELHORES TEMPOS de cada fase (o painel do fim da fase: veja Interface.DesenharPainelDeTempos).
//    Esses são DE CADA SLOT: um slot não vê os recordes do outro, e apagar o slot apaga eles.
public static class Progresso
{
    // ------------------------------------------------------------------ os slots

    // De todos os slots (valem para sempre, como as conquistas):
    const string ChaveRecorde = "cilada_recorde_mortes";
    const string ChaveFaseMaxima = "cilada_fase_maxima";
    const string ChaveTutorialDaLoja = "cilada_tutorial_loja";
    const string ChaveUltimoSlot = "cilada_ultimo_slot";   // o slot do "Continuar"
    const string ChaveMigrou = "cilada_slots_migrado";     // o save de antes dos slots já foi para o Slot 1?

    // De cada slot (a chave de verdade é "cilada_slot1_salvo_fase", "cilada_slot2_salvo_fase"...).
    const string ChaveTemJogo = "salvo_tem_jogo";
    const string ChaveFase = "salvo_fase";
    const string ChaveMaisLonge = "salvo_mais_longe";
    const string ChaveMortes = "salvo_mortes";
    const string ChaveMoedas = "salvo_moedas";
    const string ChaveTempo = "salvo_tempo";
    const string ChaveValida = "salvo_valida";
    const string ChaveMortesPorFase = "salvo_mortes_por_fase";
    const string ChaveSecreta = "secreta_liberada";
    const string ChaveQuantasFases = "salvo_quantas_fases";
    const string ChaveEscudos = "salvo_escudos";
    const string ChaveElixires = "salvo_elixires";
    const string ChavePuloDuplo = "salvo_pulo_duplo";
    const string ChaveData = "salvo_data";                 // quando foi jogado por último ("2026-11-05")

    // Todas as chaves de um slot (para apagar o slot e para trazer o save antigo). 'i' = int, 'f' = float, 's' = texto.
    static readonly (string nome, char tipo)[] ChavesDoSlot =
    {
        (ChaveTemJogo, 'i'), (ChaveFase, 'i'), (ChaveMaisLonge, 'i'), (ChaveMortes, 'i'), (ChaveMoedas, 'i'),
        (ChaveTempo, 'f'), (ChaveValida, 'i'), (ChaveMortesPorFase, 's'), (ChaveSecreta, 'i'), (ChaveQuantasFases, 'i'),
        (ChaveEscudos, 'i'), (ChaveElixires, 'i'), (ChavePuloDuplo, 'i'), (ChaveData, 's'),
    };

    public const int TotalDeSlots = 3;

    static int slotAtual = -1; // -1 = ainda não leu do PlayerPrefs

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => slotAtual = -1;

    // O slot da partida (0, 1 ou 2; na tela aparece "Slot 1, 2, 3"). Fica salvo: é o do "Continuar".
    public static int SlotAtual
    {
        get
        {
            if (slotAtual < 0)
            {
                TrazerSaveAntigo();
                slotAtual = Mathf.Clamp(PlayerPrefs.GetInt(ChaveUltimoSlot, 0), 0, TotalDeSlots - 1);
            }
            return slotAtual;
        }
        set
        {
            slotAtual = Mathf.Clamp(value, 0, TotalDeSlots - 1);
            PlayerPrefs.SetInt(ChaveUltimoSlot, slotAtual);
            PlayerPrefs.Save();
        }
    }

    // A chave de verdade de "nome" num slot. As chaves ficam guardadas (o HUD pergunta toda hora: sem lixo na memória).
    static readonly Dictionary<string, string[]> chavesProntas = new Dictionary<string, string[]>();
    static string NoSlot(string nome, int slot)
    {
        if (!chavesProntas.TryGetValue(nome, out string[] chaves))
        {
            chaves = new string[TotalDeSlots];
            for (int i = 0; i < TotalDeSlots; i++) chaves[i] = "cilada_slot" + (i + 1) + "_" + nome;
            chavesProntas[nome] = chaves;
        }
        return chaves[slot];
    }
    static string NoSlot(string nome) => NoSlot(nome, SlotAtual);

    // Antes dos slots, a partida salva ficava em "cilada_salvo_fase"... e os recordes das fases em
    // "cilada_recorde_v2_..." (de todos). Na primeira vez, tudo isso vira o Slot 1
    // (as chaves antigas ficam lá, sem uso: assim nada se perde).
    static void TrazerSaveAntigo()
    {
        TrazerPartidaAntiga();
        TrazerRecordesAntigos();
    }

    static void TrazerPartidaAntiga()
    {
        if (PlayerPrefs.GetInt(ChaveMigrou, 0) == 1) return;
        foreach ((string nome, char tipo) in ChavesDoSlot)
        {
            string antiga = "cilada_" + nome, nova = NoSlot(nome, 0);
            if (!PlayerPrefs.HasKey(antiga) || PlayerPrefs.HasKey(nova)) continue;
            if (tipo == 'i') PlayerPrefs.SetInt(nova, PlayerPrefs.GetInt(antiga));
            else if (tipo == 'f') PlayerPrefs.SetFloat(nova, PlayerPrefs.GetFloat(antiga));
            else PlayerPrefs.SetString(nova, PlayerPrefs.GetString(antiga));
        }
        PlayerPrefs.SetInt(ChaveMigrou, 1);
        PlayerPrefs.Save();
    }

    // Uma partida salva quando o jogo tinha OUTRA lista de fases (ex.: a versão antiga, de 10 fases) não
    // dá para continuar: os números das fases mudaram de lugar. Nesse caso o slot aparece vazio.
    public static bool TemJogoNoSlot(int slot)
    {
        TrazerSaveAntigo();
        return PlayerPrefs.GetInt(NoSlot(ChaveTemJogo, slot), 0) == 1
            && PlayerPrefs.GetInt(NoSlot(ChaveQuantasFases, slot), 0) == Fases.Todas.Length;
    }

    public static bool TemJogoSalvo => TemJogoNoSlot(SlotAtual); // (o do "Continuar")

    public static bool AlgumJogoSalvo
    {
        get
        {
            for (int i = 0; i < TotalDeSlots; i++) if (TemJogoNoSlot(i)) return true;
            return false;
        }
    }

    // O que aparece no cartão de cada slot.
    public struct ResumoDoSlot
    {
        public bool temJogo;
        public int fase;      // onde o Grumete está no mapa (pode ser Fases.IndiceSecreto)
        public int mortes, moedas;
        public float tempo;   // segundos
        public string data;   // "2026-11-05" ("" = não sabemos: save de antes dos slots)
    }

    public static ResumoDoSlot Resumo(int slot)
    {
        if (!TemJogoNoSlot(slot)) return default;
        return new ResumoDoSlot
        {
            temJogo = true,
            fase = PlayerPrefs.GetInt(NoSlot(ChaveFase, slot), 0),
            mortes = PlayerPrefs.GetInt(NoSlot(ChaveMortes, slot), 0),
            moedas = PlayerPrefs.GetInt(NoSlot(ChaveMoedas, slot), 0),
            tempo = PlayerPrefs.GetFloat(NoSlot(ChaveTempo, slot), 0f),
            data = PlayerPrefs.GetString(NoSlot(ChaveData, slot), ""),
        };
    }

    // Apaga um slot: a partida, os recordes e os melhores tempos das fases (as medalhas vêm deles).
    // As conquistas e o recorde do título NÃO: eles valem para o jogo todo.
    public static void ApagarSlot(int slot)
    {
        foreach ((string nome, char _) in ChavesDoSlot) PlayerPrefs.DeleteKey(NoSlot(nome, slot));
        for (int coluna = 0; coluna <= Fases.Todas.Length; coluna++)
            foreach ((string nome, char _) in ChavesDoRecorde) PlayerPrefs.DeleteKey(ChaveNaColuna(slot, coluna) + nome);
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------ a partida (no SlotAtual)

    public static int FaseSalva => PlayerPrefs.GetInt(NoSlot(ChaveFase), 0);
    public static int FaseMaxima => PlayerPrefs.GetInt(ChaveFaseMaxima, 0);

    // A fase secreta (vale para sempre depois de liberada).
    public static bool SecretaLiberada => PlayerPrefs.GetInt(NoSlot(ChaveSecreta), 0) == 1;

    public static void LiberarSecreta()
    {
        PlayerPrefs.SetInt(NoSlot(ChaveSecreta), 1);
        PlayerPrefs.Save();
    }

    // Mortes em cada fase nesta partida (as caveiras do mapa), guardadas como "3,0,12,...".
    public static void SalvarMortesPorFase(int[] mortes)
    {
        PlayerPrefs.SetString(NoSlot(ChaveMortesPorFase), string.Join(",", mortes));
        PlayerPrefs.Save();
    }

    public static void CarregarMortesPorFase(int[] mortes)
    {
        string[] partes = PlayerPrefs.GetString(NoSlot(ChaveMortesPorFase), "").Split(',');
        for (int i = 0; i < mortes.Length; i++)
            mortes[i] = i < partes.Length && int.TryParse(partes[i], out int n) ? n : 0;
    }

    // fase: onde o Grumete está. maisLonge: a fase mais longe liberada NESTA partida.
    // valida: a partida foi jogada em ordem desde a fase 1? Só assim ela vale para o recorde.
    public static void Salvar(int fase, int maisLonge, int mortes, int moedas, float tempo, bool valida)
    {
        PlayerPrefs.SetInt(NoSlot(ChaveTemJogo), 1);
        PlayerPrefs.SetInt(NoSlot(ChaveQuantasFases), Fases.Todas.Length);
        PlayerPrefs.SetInt(NoSlot(ChaveFase), fase);
        PlayerPrefs.SetInt(NoSlot(ChaveMaisLonge), maisLonge);
        PlayerPrefs.SetInt(NoSlot(ChaveMortes), mortes);
        PlayerPrefs.SetInt(NoSlot(ChaveMoedas), moedas);
        PlayerPrefs.SetFloat(NoSlot(ChaveTempo), tempo);
        PlayerPrefs.SetInt(NoSlot(ChaveValida), valida ? 1 : 0);
        PlayerPrefs.SetString(NoSlot(ChaveData), System.DateTime.Now.ToString("yyyy-MM-dd", Invariante));
        int maxima = Mathf.Max(fase == Fases.IndiceSecreto ? 0 : fase, maisLonge); // a secreta não conta
        if (maxima > FaseMaxima) PlayerPrefs.SetInt(ChaveFaseMaxima, maxima);
        PlayerPrefs.Save();
    }

    // Só as moedas guardadas (o Kraken roubou ou devolveu no meio da fase: salva na hora).
    public static void SalvarMoedas(int moedas)
    {
        PlayerPrefs.SetInt(NoSlot(ChaveMoedas), moedas);
        PlayerPrefs.Save();
    }

    public static void Carregar(out int fase, out int maisLonge, out int mortes, out int moedas, out float tempo, out bool valida)
    {
        fase = PlayerPrefs.GetInt(NoSlot(ChaveFase), 0);
        maisLonge = PlayerPrefs.GetInt(NoSlot(ChaveMaisLonge), 0);
        mortes = PlayerPrefs.GetInt(NoSlot(ChaveMortes), 0);
        moedas = PlayerPrefs.GetInt(NoSlot(ChaveMoedas), 0);
        tempo = PlayerPrefs.GetFloat(NoSlot(ChaveTempo), 0f);
        valida = PlayerPrefs.GetInt(NoSlot(ChaveValida), 0) == 1;
    }

    // O que foi comprado na Loja e ainda não foi usado (escudos e elixires) e o duplo salto. Fazem parte da
    // partida salva (um Novo Jogo zera).
    public static int EscudosSalvos => PlayerPrefs.GetInt(NoSlot(ChaveEscudos), 0);
    public static int ElixiresSalvos => PlayerPrefs.GetInt(NoSlot(ChaveElixires), 0);
    public static bool PuloDuploSalvo => PlayerPrefs.GetInt(NoSlot(ChavePuloDuplo), 0) == 1;

    public static void SalvarItensDaLoja(int escudos, int elixires, bool puloDuplo)
    {
        PlayerPrefs.SetInt(NoSlot(ChaveEscudos), escudos);
        PlayerPrefs.SetInt(NoSlot(ChaveElixires), elixires);
        PlayerPrefs.SetInt(NoSlot(ChavePuloDuplo), puloDuplo ? 1 : 0);
        PlayerPrefs.Save();
    }

    // O Louro já apresentou a Loja (a cutscene depois da 2ª fase)? Vale para sempre, como as conquistas:
    // só acontece uma vez (um Novo Jogo NÃO mostra de novo). Para rever: menu "Cilada (teste)" no Editor.
    public static bool TutorialDaLojaVisto => PlayerPrefs.GetInt(ChaveTutorialDaLoja, 0) == 1;

    public static void MarcarTutorialDaLoja()
    {
        PlayerPrefs.SetInt(ChaveTutorialDaLoja, 1);
        PlayerPrefs.Save();
    }

    public static void ApagarSecreta()
    {
        PlayerPrefs.SetInt(NoSlot(ChaveSecreta), 0);
        PlayerPrefs.Save();
    }

    // Zerou: não tem mais o que continuar, mas todas as fases ficam liberadas no mapa.
    public static void Zerou(int totalDeFases)
    {
        PlayerPrefs.SetInt(NoSlot(ChaveTemJogo), 0); // o slot fica livre de novo
        PlayerPrefs.SetInt(ChaveFaseMaxima, totalDeFases - 1);
        PlayerPrefs.Save();
    }

    // -1 = ainda não tem recorde
    public static int Recorde => PlayerPrefs.GetInt(ChaveRecorde, -1);

    public static void TentarSalvarRecorde(int mortes)
    {
        if (Recorde >= 0 && mortes >= Recorde) return;
        PlayerPrefs.SetInt(ChaveRecorde, mortes);
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------ recordes de cada fase

    // São de cada SLOT (o SlotAtual): um slot não vê os recordes do outro; apagar o slot (ou começar um jogo novo
    // por cima dele) apaga os recordes dele. Cada número é o MELHOR que você já fez naquela fase,
    // cada um por si: o menor tempo, o menor número de mortes e o maior número de moedas.
    // Ficam guardados pelo NOME da fase ("secreta" para a fase secreta): assim dá para reordenar as fases sem um
    // recorde ir parar na fase errada. O "v2" é a versão da lista de fases: as 13 fases novas (mais longas)
    // começam do zero, sem herdar os tempos das fases antigas.
    public struct RecordeDaFase
    {
        public bool existe;  // já passou dessa fase alguma vez?
        public float tempo;  // segundos, desde que entrou pela fase no mapa (o tempo das mortes conta)
        public int mortes;
        public int moedas;   // as da tentativa em que passou (morrer zera as moedas da fase)
    }

    // O que uma passada pela fase trouxe de novo. Primeira = nunca tinha passado (aí não é "recorde batido").
    [System.Flags]
    public enum Novidade { Nada = 0, Primeira = 1, Tempo = 2, Mortes = 4, Moedas = 8 }

    // A chave dos recordes de uma fase no slot atual: "cilada_slot1_recorde_v2_acapitaancora_" (+ "tempo", "mortes"...).
    // (um número fora da lista, como o das fases de mentira do teste do painel, usa o jeito antigo, pelo número, sem slot)
    static string ChaveDaFase(int fase)
    {
        if (fase != Fases.IndiceSecreto && (fase < 0 || fase >= Fases.Todas.Length)) return "cilada_recorde_fase" + fase + "_";
        return ChaveNaColuna(SlotAtual, fase == Fases.IndiceSecreto ? Fases.Todas.Length : fase);
    }

    // coluna = o número da fase (a secreta é a última coluna, Fases.Todas.Length)
    static string ChaveNaColuna(int slot, int coluna)
    {
        if (chavesDasFases == null)
        {
            chavesDasFases = new string[TotalDeSlots, Fases.Todas.Length + 1];
            for (int s = 0; s < TotalDeSlots; s++)
                for (int c = 0; c <= Fases.Todas.Length; c++)
                    chavesDasFases[s, c] = "cilada_slot" + (s + 1) + "_" + RaizDaFase(c);
        }
        return chavesDasFases[slot, coluna];
    }
    static string[,] chavesDasFases; // calculadas uma vez só

    // O meio da chave (o mesmo de antes dos slots, quando era "cilada_" + isto). O "v2" é a versão da lista de fases.
    static string RaizDaFase(int coluna) =>
        coluna == Fases.Todas.Length ? "recorde_secreta_" : "recorde_v2_" + Apelido(Fases.Todas[coluna].nome) + "_";

    // O que fica guardado de cada fase ('i' = int, 'f' = float, 's' = texto).
    static readonly (string nome, char tipo)[] ChavesDoRecorde =
        { ("existe", 'i'), ("tempo", 'f'), ("mortes", 'i'), ("moedas", 'i'), ("ranking", 's'), ("vezes", 'i') };

    const string ChaveRecordesMigrados = "cilada_recordes_migrados";

    // Os recordes de antes dos slots (de todos) viram os do Slot 1, onde também foi parar a partida antiga.
    static void TrazerRecordesAntigos()
    {
        if (PlayerPrefs.GetInt(ChaveRecordesMigrados, 0) == 1) return;
        for (int coluna = 0; coluna <= Fases.Todas.Length; coluna++)
        {
            string antiga = "cilada_" + RaizDaFase(coluna), nova = ChaveNaColuna(0, coluna);
            foreach ((string nome, char tipo) in ChavesDoRecorde)
            {
                if (!PlayerPrefs.HasKey(antiga + nome) || PlayerPrefs.HasKey(nova + nome)) continue;
                if (tipo == 'i') PlayerPrefs.SetInt(nova + nome, PlayerPrefs.GetInt(antiga + nome));
                else if (tipo == 'f') PlayerPrefs.SetFloat(nova + nome, PlayerPrefs.GetFloat(antiga + nome));
                else PlayerPrefs.SetString(nova + nome, PlayerPrefs.GetString(antiga + nome));
            }
        }
        PlayerPrefs.SetInt(ChaveRecordesMigrados, 1);
        PlayerPrefs.Save();
    }

    // O nome da fase só com letras e números, sem acento ("A Capitã Âncora" vira "acapitaancora").
    // (com uma tabela própria de acentos, sem string.Normalize: assim funciona igual em qualquer plataforma, até no WebGL)
    const string ComAcento = "áàâãäéèêëíìîïóòôõöúùûüçÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇ";
    const string SemAcento = "aaaaaeeeeiiiiooooouuuucAAAAAEEEEIIIIOOOOOUUUUC";
    static string Apelido(string nome)
    {
        var texto = new System.Text.StringBuilder();
        foreach (char letra in nome)
        {
            int acento = ComAcento.IndexOf(letra);
            char c = acento >= 0 ? SemAcento[acento] : letra;
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) texto.Append(char.ToLowerInvariant(c));
        }
        return texto.ToString();
    }

    public static RecordeDaFase RecordeDe(int fase)
    {
        string chave = ChaveDaFase(fase);
        if (PlayerPrefs.GetInt(chave + "existe", 0) != 1) return default;
        return new RecordeDaFase
        {
            existe = true,
            tempo = PlayerPrefs.GetFloat(chave + "tempo", 0f),
            mortes = PlayerPrefs.GetInt(chave + "mortes", 0),
            moedas = PlayerPrefs.GetInt(chave + "moedas", 0),
        };
    }

    // Guarda o que você acabou de fazer (só o que for MELHOR que o recorde) e diz o que foi novidade.
    public static Novidade RegistrarFase(int fase, float tempo, int mortes, int moedas)
    {
        RecordeDaFase antes = RecordeDe(fase);
        string chave = ChaveDaFase(fase);
        Novidade novidade = antes.existe ? Novidade.Nada : Novidade.Primeira;
        if (!antes.existe || tempo < antes.tempo)
        {
            PlayerPrefs.SetFloat(chave + "tempo", tempo);
            if (antes.existe) novidade |= Novidade.Tempo;
        }
        if (!antes.existe || mortes < antes.mortes)
        {
            PlayerPrefs.SetInt(chave + "mortes", mortes);
            if (antes.existe) novidade |= Novidade.Mortes;
        }
        if (!antes.existe || moedas > antes.moedas)
        {
            PlayerPrefs.SetInt(chave + "moedas", moedas);
            if (antes.existe) novidade |= Novidade.Moedas;
        }
        PlayerPrefs.SetInt(chave + "existe", 1);
        PlayerPrefs.Save();
        return novidade;
    }

    // ------------------------------------------------------------------ melhores tempos de cada fase

    // O painel do fim da fase mostra os 5 tempos mais rápidos que você já fez em cada fase, neste slot
    // (como os recordes: apagar o slot apaga eles). Usa o MESMO relógio do recorde: o TempoNaFase do
    // GerenciadorDoJogo (desde que entrou pela fase no mapa, contando o tempo das mortes).
    // Fica guardado como texto, do mais rápido para o mais lento: "41.234|2|27/10/26;55.1|5|26/10/26;..."
    public const int TamanhoDoRanking = 5;

    public struct TempoDoRanking
    {
        public float tempo;  // segundos
        public int mortes;   // -1 = não sabemos (tempo que veio do recorde de antes de o painel existir)
        public string data;  // "27/10/26" ("" = não sabemos)
    }

    // Número com PONTO, sempre: no Windows em português o float viraria "41,2" e quebraria a leitura.
    static readonly System.Globalization.CultureInfo Invariante = System.Globalization.CultureInfo.InvariantCulture;

    public static List<TempoDoRanking> MelhoresTempos(int fase)
    {
        var lista = new List<TempoDoRanking>();
        string chave = ChaveDaFase(fase);
        if (!PlayerPrefs.HasKey(chave + "ranking"))
        {
            // fase que você já tinha passado antes do painel existir: o recorde antigo vira o 1º lugar
            RecordeDaFase recorde = RecordeDe(fase);
            if (recorde.existe) lista.Add(new TempoDoRanking { tempo = recorde.tempo, mortes = -1, data = "" });
            return lista;
        }
        foreach (string item in PlayerPrefs.GetString(chave + "ranking", "").Split(';'))
        {
            string[] partes = item.Split('|');
            if (partes.Length < 3 || !float.TryParse(partes[0], System.Globalization.NumberStyles.Float, Invariante, out float tempo))
                continue;
            lista.Add(new TempoDoRanking
            {
                tempo = tempo,
                mortes = int.TryParse(partes[1], out int mortes) ? mortes : -1,
                data = partes[2],
            });
        }
        lista.Sort((a, b) => a.tempo.CompareTo(b.tempo));
        if (lista.Count > TamanhoDoRanking) lista.RemoveRange(TamanhoDoRanking, lista.Count - TamanhoDoRanking);
        return lista;
    }

    // Quantas vezes você já passou dessa fase neste slot.
    public static int VezesConcluida(int fase) =>
        PlayerPrefs.GetInt(ChaveDaFase(fase) + "vezes", RecordeDe(fase).existe ? 1 : 0);

    // Coloca o tempo que você acabou de fazer no ranking da fase e conta mais uma vez concluída.
    // Devolve a posição (0 = o mais rápido) ou -1 se não entrou entre os 5.
    // Chamar ANTES de RegistrarFase (assim o recorde antigo ainda conta como o 1º lugar, se for o caso).
    public static int RegistrarNoRanking(int fase, float tempo, int mortes)
    {
        string chave = ChaveDaFase(fase);
        PlayerPrefs.SetInt(chave + "vezes", VezesConcluida(fase) + 1);
        List<TempoDoRanking> lista = MelhoresTempos(fase);
        int posicao = 0;
        while (posicao < lista.Count && lista[posicao].tempo <= tempo) posicao++; // empate: quem fez antes fica na frente
        if (posicao >= TamanhoDoRanking)
        {
            PlayerPrefs.Save();
            return -1;
        }
        lista.Insert(posicao, new TempoDoRanking
        {
            tempo = tempo,
            mortes = mortes,
            data = System.DateTime.Now.ToString("dd/MM/yy", Invariante),
        });
        if (lista.Count > TamanhoDoRanking) lista.RemoveAt(lista.Count - 1);
        var partes = new string[lista.Count];
        for (int i = 0; i < lista.Count; i++)
            partes[i] = lista[i].tempo.ToString("R", Invariante) + "|" + lista[i].mortes + "|" + lista[i].data;
        PlayerPrefs.SetString(chave + "ranking", string.Join(";", partes));
        PlayerPrefs.Save();
        return posicao;
    }
}
