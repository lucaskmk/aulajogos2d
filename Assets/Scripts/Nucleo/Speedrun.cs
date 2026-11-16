using System.Collections.Generic;
using System.IO;
using UnityEngine;

// MODO SPEEDRUN (Opções > Speedrun). Ligado, ele mostra:
//  - o tempo da fase com centésimos no HUD, embaixo do tempo total;
//  - o FANTASMA DO RECORDE: o Grumete transparente que refaz a sua tentativa mais rápida da fase
//    (no mesmo relógio do recorde, o TempoNaFase: as mortes daquela vez também aparecem, ele volta no checkpoint);
//  - as PARCIAIS da partida inteira: no fim de cada fase, quanto você está na frente (verde) ou atrás (vermelho)
//    da sua melhor partida completa, no mesmo ponto.
// Nada disso muda o jogo: é só relógio e comparação. Desligado, o fantasma e as parciais somem,
// mas continuam sendo gravados (ligar depois já mostra o recorde).
//
// O fantasma fica num arquivo por fase (fantasmas/<nome da fase>.bin, ao lado do save), e o Steam Cloud
// leva junto se o Auto-Cloud apontar para a pasta do save (veja PROXIMOS_PASSOS.md).
public static class Speedrun
{
    // ------------------------------------------------------------------ o fantasma

    public const float Intervalo = 1f / 15f; // 15 quadros por segundo (desliza entre eles)
    const int MaximoDeQuadros = 15 * 60 * 15; // 15 minutos

    public struct Quadro
    {
        public Vector2 lugar;
        public bool olhaEsquerda, andando, morto;
    }

    static readonly List<Quadro> gravando = new List<Quadro>();
    static float acumulado;
    static int faseGravando = -1;
    static readonly Dictionary<string, List<Quadro>> carregados = new Dictionary<string, List<Quadro>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar()
    {
        gravando.Clear();
        carregados.Clear();
        faseGravando = -1;
        mostrarParcialAte = 0f;
    }

    // Entrou na fase pelo mapa.
    public static void ComecarFase(int fase)
    {
        gravando.Clear();
        acumulado = Intervalo;
        faseGravando = fase;
    }

    // A cada quadro (jogando ou morrendo: o relógio da fase corre nos dois).
    public static void Gravar(Jogador lider, float dt)
    {
        if (lider == null || faseGravando < 0 || gravando.Count >= MaximoDeQuadros) return;
        acumulado += dt;
        while (acumulado >= Intervalo && gravando.Count < MaximoDeQuadros)
        {
            acumulado -= Intervalo;
            SpriteRenderer desenho = lider.Desenho;
            gravando.Add(new Quadro
            {
                lugar = lider.transform.position,
                olhaEsquerda = desenho != null && desenho.flipX,
                andando = Mathf.Abs(lider.Corpo != null ? lider.Corpo.linearVelocity.x : 0f) > 0.5f,
                morto = lider.Morto,
            });
        }
    }

    // Passou da fase com recorde de tempo: guarda o fantasma desta vez.
    public static void SalvarSeForRecorde(int fase, bool bateuOTempo)
    {
        if (fase != faseGravando || gravando.Count < 2) return;
        string chave = Chave(fase);
        if (!bateuOTempo && Carregar(fase) != null) return; // (a primeira vez que passa também vira fantasma)
        carregados[chave] = new List<Quadro>(gravando);
        try
        {
            Directory.CreateDirectory(Pasta);
            using var escrita = new BinaryWriter(File.Create(Arquivo(chave)));
            escrita.Write(1); // versão
            escrita.Write(gravando.Count);
            foreach (Quadro q in gravando)
            {
                escrita.Write(q.lugar.x);
                escrita.Write(q.lugar.y);
                escrita.Write((byte)((q.olhaEsquerda ? 1 : 0) | (q.andando ? 2 : 0) | (q.morto ? 4 : 0)));
            }
        }
        catch (System.Exception erro)
        {
            Debug.LogWarning("[Speedrun] não deu para salvar o fantasma: " + erro.Message);
        }
    }

    // O fantasma do recorde desta fase (null = ainda não tem).
    public static List<Quadro> Carregar(int fase)
    {
        string chave = Chave(fase);
        if (carregados.TryGetValue(chave, out List<Quadro> pronto)) return pronto;
        List<Quadro> lidos = null;
        try
        {
            string caminho = Arquivo(chave);
            if (File.Exists(caminho))
            {
                using var leitura = new BinaryReader(File.OpenRead(caminho));
                if (leitura.ReadInt32() == 1)
                {
                    int n = Mathf.Clamp(leitura.ReadInt32(), 0, MaximoDeQuadros);
                    lidos = new List<Quadro>(n);
                    for (int i = 0; i < n; i++)
                    {
                        var q = new Quadro { lugar = new Vector2(leitura.ReadSingle(), leitura.ReadSingle()) };
                        byte b = leitura.ReadByte();
                        q.olhaEsquerda = (b & 1) != 0;
                        q.andando = (b & 2) != 0;
                        q.morto = (b & 4) != 0;
                        lidos.Add(q);
                    }
                }
            }
        }
        catch (System.Exception erro)
        {
            Debug.LogWarning("[Speedrun] fantasma estragado, ignorando: " + erro.Message);
            lidos = null;
        }
        carregados[chave] = lidos;
        return lidos;
    }

    static string Pasta => Path.Combine(Application.persistentDataPath, Application.isEditor ? "fantasmas_editor" : "fantasmas");
    static string Arquivo(string chave) => Path.Combine(Pasta, chave + ".bin");

    // Pelo nome da fase (e não pelo número: fases novas no meio não trocam os fantasmas de lugar).
    // O modo tempestade tem os seus (a onda muda o caminho).
    static string Chave(int fase)
    {
        var texto = new System.Text.StringBuilder();
        foreach (char c in Fases.Dados(fase).nome)
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) texto.Append(char.ToLowerInvariant(c));
        if (GerenciadorDoJogo.Tempestade) texto.Append("_tempestade");
        return texto.ToString();
    }

    // ------------------------------------------------------------------ as parciais da partida

    // Cada slot guarda o tempo total no fim de cada fase da partida atual; a melhor partida completa guarda as dela.
    // Texto com os segundos separados por ';' (o índice é a fase; vazio = não passou por ela).
    static readonly System.Globalization.CultureInfo Invariante = System.Globalization.CultureInfo.InvariantCulture;
    static string ChaveDaPartida => $"cilada_s{Progresso.SlotAtual}_parciais" + (GerenciadorDoJogo.Tempestade ? "_t" : "");
    static string ChaveDaMelhor => "cilada_parciais_melhor" + (GerenciadorDoJogo.Tempestade ? "_t" : "");

    // A última diferença (para o HUD): segundos (negativo = na frente) e até quando aparece.
    public static float UltimaDiferenca { get; private set; }
    public static bool TemDiferenca => Time.unscaledTime < mostrarParcialAte;
    static float mostrarParcialAte;

    static float[] Ler(string chave)
    {
        string texto = Guardado.GetString(chave, "");
        var tempos = new float[Fases.Todas.Length];
        for (int i = 0; i < tempos.Length; i++) tempos[i] = -1f;
        string[] partes = texto.Split(';');
        for (int i = 0; i < partes.Length && i < tempos.Length; i++)
            if (float.TryParse(partes[i], System.Globalization.NumberStyles.Float, Invariante, out float t)) tempos[i] = t;
        return tempos;
    }

    static void Escrever(string chave, float[] tempos)
    {
        var partes = new string[tempos.Length];
        for (int i = 0; i < tempos.Length; i++) partes[i] = tempos[i] >= 0f ? tempos[i].ToString("0.###", Invariante) : "";
        Guardado.SetString(chave, string.Join(";", partes));
    }

    // Novo jogo: a partida começa do zero.
    public static void NovaPartida()
    {
        Guardado.DeleteKey(ChaveDaPartida);
    }

    // Fim de uma fase da campanha (em ordem): guarda a parcial e compara com a melhor partida.
    public static void Parcial(int fase, float tempoTotal, bool partidaValida)
    {
        if (fase < 0 || fase >= Fases.Todas.Length || !partidaValida) return;
        float[] agora = Ler(ChaveDaPartida);
        agora[fase] = tempoTotal;
        Escrever(ChaveDaPartida, agora);
        float melhor = Ler(ChaveDaMelhor)[fase];
        if (melhor < 0f) return;
        UltimaDiferenca = tempoTotal - melhor;
        mostrarParcialAte = Time.unscaledTime + 5f;
    }

    // Zerou: se foi a partida mais rápida, as parciais dela viram as novas "melhores".
    public static void Zerou(float tempoTotal, bool partidaValida)
    {
        if (!partidaValida) return;
        float[] agora = Ler(ChaveDaPartida);
        float[] melhor = Ler(ChaveDaMelhor);
        float ultimaMelhor = melhor[melhor.Length - 1];
        agora[agora.Length - 1] = tempoTotal;
        if (ultimaMelhor < 0f || tempoTotal < ultimaMelhor) Escrever(ChaveDaMelhor, agora);
        Placares.Enviar(GerenciadorDoJogo.Tempestade ? "speedrun_tempestade" : "speedrun", Mathf.RoundToInt(tempoTotal * 1000f));
    }
}

// O fantasma do recorde na fase: lê o relógio da fase (TempoNaFase) e fica onde você estava naquele segundo.
public class FantasmaDoRecorde : MonoBehaviour
{
    List<Speedrun.Quadro> quadros;
    SpriteRenderer desenho;
    Sprite parado, andando;

    public static void Criar(Transform raiz, int fase)
    {
        if (!Opcoes.Speedrun || Tripulacao.Coop) return; // (no coop, 4 Grumetes e mais um fantasma confundem)
        List<Speedrun.Quadro> quadros = Speedrun.Carregar(fase);
        if (quadros == null || quadros.Count < 2) return;
        var objeto = new GameObject("FantasmaDoRecorde");
        objeto.transform.SetParent(raiz, false);
        var fantasma = objeto.AddComponent<FantasmaDoRecorde>();
        fantasma.quadros = quadros;
        fantasma.desenho = objeto.AddComponent<SpriteRenderer>();
        fantasma.desenho.sortingOrder = 9;
        fantasma.desenho.color = new Color(0.55f, 0.95f, 1f, 0.38f);
        fantasma.parado = FabricaDeSprites.Pegar("jogador");
        fantasma.andando = FabricaDeSprites.Pegar("jogador_andando");
        fantasma.LateUpdate();
    }

    void LateUpdate()
    {
        var jogo = GerenciadorDoJogo.Instancia;
        if (jogo == null) return;
        float posicao = jogo.TempoNaFase / Speedrun.Intervalo;
        int i = Mathf.FloorToInt(posicao);
        if (i >= quadros.Count - 1)
        {
            desenho.enabled = false; // o recorde já tinha chegado: você está atrás
            return;
        }
        Speedrun.Quadro a = quadros[i], b = quadros[i + 1];
        desenho.enabled = !a.morto;
        // (morreu e voltou no checkpoint: pula direto, sem deslizar pela fase inteira)
        float t = (b.lugar - a.lugar).sqrMagnitude > 9f ? 0f : posicao - i;
        transform.position = Vector2.Lerp(a.lugar, b.lugar, t);
        desenho.flipX = a.olhaEsquerda;
        desenho.sprite = a.andando && (int)(Time.time * 10f) % 2 == 0 ? andando : parado;
    }
}

// Placares on-line. Hoje só chama o gancho: com o Steamworks.NET instalado, ligue assim (uma vez, no começo do jogo):
//     Placares.AoEnviar += (nome, valor) => { /* SteamUserStats.FindOrCreateLeaderboard(nome, Ascending, NumericTime)
//                                                e UploadLeaderboardScore(KeepBest, valor) */ };
// Os nomes: "tempo_<fase>" e "mortes_<fase>" (fase = o número, 1 a 13; a secreta é "secreta"), "speedrun" (ms)
// e as versões "_tempestade" do modo difícil. Menor é melhor em todos.
public static class Placares
{
    public static event System.Action<string, int> AoEnviar;

    public static void Enviar(string nome, int valor)
    {
        AoEnviar?.Invoke(nome, valor);
    }

    public static void FaseConcluida(int fase, float tempo, int mortes, bool coop)
    {
        if (coop) return; // (os placares são de quem joga sozinho: 4 pessoas revezando a frente é outro esporte)
        string nome = fase == Fases.IndiceSecreto ? "secreta"
            : Fases.EhBonus(fase) ? "bonus" + (fase - Fases.PrimeiroBonus + 1)
            : Fases.EhDoMar(fase) ? "mar" + (fase - Fases.PrimeiroDoMar + 1)
            : (fase + 1).ToString();
        string sufixo = GerenciadorDoJogo.Tempestade ? "_tempestade" : "";
        Enviar("tempo_" + nome + sufixo, Mathf.RoundToInt(tempo * 1000f));
        Enviar("mortes_" + nome + sufixo, mortes);
    }
}
