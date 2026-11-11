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
    const string ChaveFichas = "salvo_fichas";             // as fichas dos jogos de habilidade da Loja
    const string ChaveEscudos = "salvo_escudos";
    const string ChaveElixires = "salvo_elixires";
    const string ChavePuloDuplo = "salvo_pulo_duplo";
    const string ChavePocoesDeEspinhos = "salvo_pocoes_espinhos";
    const string ChavePocoesDoChao = "salvo_pocoes_chao";
    const string ChaveData = "salvo_data";                 // quando foi jogado por último ("2026-11-05")
    const string ChaveDificil = "salvo_dificil";           // 1 = o Grumete roubou o tesouro: o modo difícil (a tempestade)
    const string ChaveEscolha = "salvo_escolha";           // 1 = chegou no baú e ainda não escolheu (o "Continuar" volta na escolha)

    // Todas as chaves de um slot (para apagar o slot e para trazer o save antigo). 'i' = int, 'f' = float, 's' = texto.
    static readonly (string nome, char tipo)[] ChavesDoSlot =
    {
        (ChaveTemJogo, 'i'), (ChaveFase, 'i'), (ChaveMaisLonge, 'i'), (ChaveMortes, 'i'), (ChaveMoedas, 'i'),
        (ChaveTempo, 'f'), (ChaveValida, 'i'), (ChaveMortesPorFase, 's'), (ChaveSecreta, 'i'), (ChaveQuantasFases, 'i'),
        (ChaveEscudos, 'i'), (ChaveElixires, 'i'), (ChavePuloDuplo, 'i'), (ChaveData, 's'), (ChaveDificil, 'i'), (ChaveEscolha, 'i'),
        (ChavePocoesDeEspinhos, 'i'), (ChavePocoesDoChao, 'i'),
        // (coop: os itens dos jogadores 2, 3 e 4: veja SalvarItensDoJogador)
        (ChaveEscudos + "_p2", 'i'), (ChaveElixires + "_p2", 'i'), (ChavePuloDuplo + "_p2", 'i'),
        (ChaveEscudos + "_p3", 'i'), (ChaveElixires + "_p3", 'i'), (ChavePuloDuplo + "_p3", 'i'),
        (ChaveEscudos + "_p4", 'i'), (ChaveElixires + "_p4", 'i'), (ChavePuloDuplo + "_p4", 'i'),
        (ChaveFichas, 'i'),
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
                slotAtual = Mathf.Clamp(Guardado.GetInt(ChaveUltimoSlot, 0), 0, TotalDeSlots - 1);
            }
            return slotAtual;
        }
        set
        {
            slotAtual = Mathf.Clamp(value, 0, TotalDeSlots - 1);
            Guardado.SetInt(ChaveUltimoSlot, slotAtual);
            Guardado.Save();
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
        if (Guardado.GetInt(ChaveMigrou, 0) == 1) return;
        foreach ((string nome, char tipo) in ChavesDoSlot)
        {
            string antiga = "cilada_" + nome, nova = NoSlot(nome, 0);
            if (!Guardado.HasKey(antiga) || Guardado.HasKey(nova)) continue;
            if (tipo == 'i') Guardado.SetInt(nova, Guardado.GetInt(antiga));
            else if (tipo == 'f') Guardado.SetFloat(nova, Guardado.GetFloat(antiga));
            else Guardado.SetString(nova, Guardado.GetString(antiga));
        }
        Guardado.SetInt(ChaveMigrou, 1);
        Guardado.Save();
    }

    // Uma partida salva quando o jogo tinha OUTRA lista de fases (ex.: a versão antiga, de 10 fases) não
    // dá para continuar: os números das fases mudaram de lugar. Nesse caso o slot aparece vazio.
    public static bool TemJogoNoSlot(int slot)
    {
        TrazerSaveAntigo();
        return Guardado.GetInt(NoSlot(ChaveTemJogo, slot), 0) == 1
            && Guardado.GetInt(NoSlot(ChaveQuantasFases, slot), 0) == Fases.Todas.Length;
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
        public bool dificil;  // no modo difícil?
    }

    public static ResumoDoSlot Resumo(int slot)
    {
        if (!TemJogoNoSlot(slot)) return default;
        return new ResumoDoSlot
        {
            temJogo = true,
            fase = Guardado.GetInt(NoSlot(ChaveFase, slot), 0),
            mortes = Guardado.GetInt(NoSlot(ChaveMortes, slot), 0),
            moedas = Guardado.GetInt(NoSlot(ChaveMoedas, slot), 0),
            tempo = Guardado.GetFloat(NoSlot(ChaveTempo, slot), 0f),
            data = Guardado.GetString(NoSlot(ChaveData, slot), ""),
            dificil = Guardado.GetInt(NoSlot(ChaveDificil, slot), 0) == 1,
        };
    }

    // Apaga um slot: a partida, os recordes e os melhores tempos das fases (as medalhas vêm deles).
    // As conquistas e o recorde do título NÃO: eles valem para o jogo todo.
    public static void ApagarSlot(int slot)
    {
        foreach ((string nome, char _) in ChavesDoSlot) Guardado.DeleteKey(NoSlot(nome, slot));
        for (int coluna = 0; coluna <= Fases.Todas.Length; coluna++)
            foreach ((string nome, char _) in ChavesDoRecorde)
            {
                Guardado.DeleteKey(ChaveNaColuna(slot, coluna) + nome);
                Guardado.DeleteKey(ChaveNaColuna(slot, coluna) + SufixoDificil + nome);
            }
        Guardado.Save();
    }

    // ------------------------------------------------------------------ a partida (no SlotAtual)

    public static int FaseSalva => Guardado.GetInt(NoSlot(ChaveFase), 0);
    public static int FaseMaxima => Guardado.GetInt(ChaveFaseMaxima, 0);

    // A fase secreta (vale para sempre depois de liberada).
    public static bool SecretaLiberada => Guardado.GetInt(NoSlot(ChaveSecreta), 0) == 1;

    public static void LiberarSecreta()
    {
        Guardado.SetInt(NoSlot(ChaveSecreta), 1);
        Guardado.Save();
    }

    // Mortes em cada fase nesta partida (as caveiras do mapa), guardadas como "3,0,12,...".
    public static void SalvarMortesPorFase(int[] mortes)
    {
        Guardado.SetString(NoSlot(ChaveMortesPorFase), string.Join(",", mortes));
        Guardado.Save();
    }

    public static void CarregarMortesPorFase(int[] mortes)
    {
        string[] partes = Guardado.GetString(NoSlot(ChaveMortesPorFase), "").Split(',');
        for (int i = 0; i < mortes.Length; i++)
            mortes[i] = i < partes.Length && int.TryParse(partes[i], out int n) ? n : 0;
    }

    // fase: onde o Grumete está. maisLonge: a fase mais longe liberada NESTA partida.
    // valida: a partida foi jogada em ordem desde a fase 1? Só assim ela vale para o recorde.
    public static void Salvar(int fase, int maisLonge, int mortes, int moedas, float tempo, bool valida)
    {
        Guardado.SetInt(NoSlot(ChaveTemJogo), 1);
        Guardado.SetInt(NoSlot(ChaveQuantasFases), Fases.Todas.Length);
        Guardado.SetInt(NoSlot(ChaveFase), fase);
        Guardado.SetInt(NoSlot(ChaveMaisLonge), maisLonge);
        Guardado.SetInt(NoSlot(ChaveMortes), mortes);
        Guardado.SetInt(NoSlot(ChaveMoedas), moedas);
        Guardado.SetFloat(NoSlot(ChaveTempo), tempo);
        Guardado.SetInt(NoSlot(ChaveValida), valida ? 1 : 0);
        Guardado.SetString(NoSlot(ChaveData), System.DateTime.Now.ToString("yyyy-MM-dd", Invariante));
        int maxima = Mathf.Max(fase == Fases.IndiceSecreto ? 0 : fase, maisLonge); // a secreta não conta
        if (maxima > FaseMaxima) Guardado.SetInt(ChaveFaseMaxima, maxima);
        Guardado.Save();
    }

    // Só as moedas guardadas (o Kraken roubou ou devolveu no meio da fase: salva na hora).
    public static void SalvarMoedas(int moedas)
    {
        Guardado.SetInt(NoSlot(ChaveMoedas), moedas);
        Guardado.Save();
    }

    public static void Carregar(out int fase, out int maisLonge, out int mortes, out int moedas, out float tempo, out bool valida)
    {
        fase = Guardado.GetInt(NoSlot(ChaveFase), 0);
        maisLonge = Guardado.GetInt(NoSlot(ChaveMaisLonge), 0);
        mortes = Guardado.GetInt(NoSlot(ChaveMortes), 0);
        moedas = Guardado.GetInt(NoSlot(ChaveMoedas), 0);
        tempo = Guardado.GetFloat(NoSlot(ChaveTempo), 0f);
        valida = Guardado.GetInt(NoSlot(ChaveValida), 0) == 1;
    }

    // O que foi comprado na Loja e ainda não foi usado (escudos e elixires) e o duplo salto. Fazem parte da
    // partida salva (um Novo Jogo zera).
    public static int EscudosSalvos => Guardado.GetInt(NoSlot(ChaveEscudos), 0);
    public static int ElixiresSalvos => Guardado.GetInt(NoSlot(ChaveElixires), 0);
    public static bool PuloDuploSalvo => Guardado.GetInt(NoSlot(ChavePuloDuplo), 0) == 1;
    public static int PocoesDeEspinhosSalvas => Guardado.GetInt(NoSlot(ChavePocoesDeEspinhos), 0);
    public static int PocoesDoChaoSalvas => Guardado.GetInt(NoSlot(ChavePocoesDoChao), 0);

    // As fichas dos jogos de habilidade da Loja (um slot que nunca salvou fichas começa com "padrao").
    public static int FichasSalvas(int padrao) => Guardado.GetInt(NoSlot(ChaveFichas), padrao);
    public static void SalvarFichas(int fichas) => Guardado.SetInt(NoSlot(ChaveFichas), fichas); // (o Save vem logo depois)

    // Coop: os itens do jogador 2 em diante (jogador: 1 = P2...). Os do P1 são os de cima.
    static string DoJogador(string chave, int jogador) => chave + "_p" + (jogador + 1);
    public static int EscudosSalvosDe(int jogador) => Guardado.GetInt(NoSlot(DoJogador(ChaveEscudos, jogador)), 0);
    public static int ElixiresSalvosDe(int jogador) => Guardado.GetInt(NoSlot(DoJogador(ChaveElixires, jogador)), 0);
    public static bool PuloDuploSalvoDe(int jogador) => Guardado.GetInt(NoSlot(DoJogador(ChavePuloDuplo, jogador)), 0) == 1;

    // (não chama o Save: quem chama é o SalvarItensDaLoja, logo antes ou logo depois)
    public static void SalvarItensDoJogador(int jogador, int escudos, int elixires, bool puloDuplo)
    {
        Guardado.SetInt(NoSlot(DoJogador(ChaveEscudos, jogador)), escudos);
        Guardado.SetInt(NoSlot(DoJogador(ChaveElixires, jogador)), elixires);
        Guardado.SetInt(NoSlot(DoJogador(ChavePuloDuplo, jogador)), puloDuplo ? 1 : 0);
    }

    public static void SalvarItensDaLoja(int escudos, int elixires, bool puloDuplo, int pocoesDeEspinhos, int pocoesDoChao)
    {
        Guardado.SetInt(NoSlot(ChaveEscudos), escudos);
        Guardado.SetInt(NoSlot(ChaveElixires), elixires);
        Guardado.SetInt(NoSlot(ChavePuloDuplo), puloDuplo ? 1 : 0);
        Guardado.SetInt(NoSlot(ChavePocoesDeEspinhos), pocoesDeEspinhos);
        Guardado.SetInt(NoSlot(ChavePocoesDoChao), pocoesDoChao);
        Guardado.Save();
    }

    // O Louro já apresentou a Loja (a cutscene depois da 2ª fase)? Vale para sempre, como as conquistas:
    // só acontece uma vez (um Novo Jogo NÃO mostra de novo). Para rever: menu "Cilada (teste)" no Editor.
    public static bool TutorialDaLojaVisto => Guardado.GetInt(ChaveTutorialDaLoja, 0) == 1;

    public static void MarcarTutorialDaLoja()
    {
        Guardado.SetInt(ChaveTutorialDaLoja, 1);
        Guardado.Save();
    }

    public static void ApagarSecreta()
    {
        Guardado.SetInt(NoSlot(ChaveSecreta), 0);
        Guardado.Save();
    }

    // Zerou: o slot continua vivo, com o mapa todo aberto (dá para buscar as medalhas e vencer o Kraken de novo).
    public static void Zerou(int totalDeFases)
    {
        Guardado.SetInt(ChaveFaseMaxima, totalDeFases - 1);
        Guardado.Save();
    }

    // O modo difícil (o Grumete roubou o tesouro): é do slot.
    public static bool ModoDificil => Guardado.GetInt(NoSlot(ChaveDificil), 0) == 1;

    public static void SalvarModoDificil(bool ligado)
    {
        Guardado.SetInt(NoSlot(ChaveDificil), ligado ? 1 : 0);
        Guardado.Save();
    }

    // Chegou no baú e ainda não escolheu? (fechar o jogo no meio da escolha não pode custar a luta com o Kraken)
    public static bool EscolhaPendente => Guardado.GetInt(NoSlot(ChaveEscolha), 0) == 1;

    public static void SalvarEscolhaPendente(bool pendente)
    {
        Guardado.SetInt(NoSlot(ChaveEscolha), pendente ? 1 : 0);
        Guardado.Save();
    }

    // -1 = ainda não tem recorde
    public static int Recorde => Guardado.GetInt(ChaveRecorde, -1);

    public static void TentarSalvarRecorde(int mortes)
    {
        if (Recorde >= 0 && mortes >= Recorde) return;
        Guardado.SetInt(ChaveRecorde, mortes);
        Guardado.Save();
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
    // (no modo difícil, os recordes e as medalhas são outros: correr de um tsunami não vale nem atrapalha os da campanha)
    const string SufixoDificil = "dificil_";

    static string ChaveDaFase(int fase)
    {
        if (fase != Fases.IndiceSecreto && (fase < 0 || fase >= Fases.Todas.Length)) return "cilada_recorde_fase" + fase + "_";
        return ChaveNaColuna(SlotAtual, fase == Fases.IndiceSecreto ? Fases.Todas.Length : fase) + (ModoDificil ? SufixoDificil : "");
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
        if (Guardado.GetInt(ChaveRecordesMigrados, 0) == 1) return;
        for (int coluna = 0; coluna <= Fases.Todas.Length; coluna++)
        {
            string antiga = "cilada_" + RaizDaFase(coluna), nova = ChaveNaColuna(0, coluna);
            foreach ((string nome, char tipo) in ChavesDoRecorde)
            {
                if (!Guardado.HasKey(antiga + nome) || Guardado.HasKey(nova + nome)) continue;
                if (tipo == 'i') Guardado.SetInt(nova + nome, Guardado.GetInt(antiga + nome));
                else if (tipo == 'f') Guardado.SetFloat(nova + nome, Guardado.GetFloat(antiga + nome));
                else Guardado.SetString(nova + nome, Guardado.GetString(antiga + nome));
            }
        }
        Guardado.SetInt(ChaveRecordesMigrados, 1);
        Guardado.Save();
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
        if (Guardado.GetInt(chave + "existe", 0) != 1) return default;
        return new RecordeDaFase
        {
            existe = true,
            tempo = Guardado.GetFloat(chave + "tempo", 0f),
            mortes = Guardado.GetInt(chave + "mortes", 0),
            moedas = Guardado.GetInt(chave + "moedas", 0),
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
            Guardado.SetFloat(chave + "tempo", tempo);
            if (antes.existe) novidade |= Novidade.Tempo;
        }
        if (!antes.existe || mortes < antes.mortes)
        {
            Guardado.SetInt(chave + "mortes", mortes);
            if (antes.existe) novidade |= Novidade.Mortes;
        }
        if (!antes.existe || moedas > antes.moedas)
        {
            Guardado.SetInt(chave + "moedas", moedas);
            if (antes.existe) novidade |= Novidade.Moedas;
        }
        Guardado.SetInt(chave + "existe", 1);
        Guardado.Save();
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
        if (!Guardado.HasKey(chave + "ranking"))
        {
            // fase que você já tinha passado antes do painel existir: o recorde antigo vira o 1º lugar
            RecordeDaFase recorde = RecordeDe(fase);
            if (recorde.existe) lista.Add(new TempoDoRanking { tempo = recorde.tempo, mortes = -1, data = "" });
            return lista;
        }
        foreach (string item in Guardado.GetString(chave + "ranking", "").Split(';'))
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
        Guardado.GetInt(ChaveDaFase(fase) + "vezes", RecordeDe(fase).existe ? 1 : 0);

    // Coloca o tempo que você acabou de fazer no ranking da fase e conta mais uma vez concluída.
    // Devolve a posição (0 = o mais rápido) ou -1 se não entrou entre os 5.
    // Chamar ANTES de RegistrarFase (assim o recorde antigo ainda conta como o 1º lugar, se for o caso).
    public static int RegistrarNoRanking(int fase, float tempo, int mortes)
    {
        string chave = ChaveDaFase(fase);
        Guardado.SetInt(chave + "vezes", VezesConcluida(fase) + 1);
        List<TempoDoRanking> lista = MelhoresTempos(fase);
        int posicao = 0;
        while (posicao < lista.Count && lista[posicao].tempo <= tempo) posicao++; // empate: quem fez antes fica na frente
        if (posicao >= TamanhoDoRanking)
        {
            Guardado.Save();
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
        Guardado.SetString(chave + "ranking", string.Join(";", partes));
        Guardado.Save();
        return posicao;
    }
}
