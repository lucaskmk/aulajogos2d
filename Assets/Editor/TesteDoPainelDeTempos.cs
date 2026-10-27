// TEMPORÁRIO: testes do painel de melhores tempos (pode apagar a pasta Assets/Editor depois).
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TesteDoPainelDeTempos
{
    const int FaseFalsa = 9999, FaseFalsaComRecorde = 9998;

    static void Limpar(int fase)
    {
        string chave = "cilada_recorde_fase" + fase + "_";
        foreach (string k in new[] { "ranking", "vezes", "existe", "tempo", "mortes", "moedas" }) PlayerPrefs.DeleteKey(chave + k);
    }

    static int falhas;
    static void Conferir(bool ok, string oque)
    {
        if (ok) Debug.Log("[TestePainel] ok: " + oque);
        else { falhas++; Debug.LogError("[TestePainel] FALHOU: " + oque); }
    }

    static string Tempos(List<Progresso.TempoDoRanking> l)
    {
        var s = new List<string>();
        foreach (var t in l) s.Add(t.tempo.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        return string.Join(",", s);
    }

    [MenuItem("Cilada (teste)/Testar ranking de tempos")]
    public static void TestarRanking()
    {
        falhas = 0;
        var culturaAntes = System.Threading.Thread.CurrentThread.CurrentCulture;
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("pt-BR"); // vírgula decimal, como no Windows em português
        try
        {
            Limpar(FaseFalsa);
            Conferir(Progresso.MelhoresTempos(FaseFalsa).Count == 0, "fase nova começa sem tempos");
            Conferir(Progresso.VezesConcluida(FaseFalsa) == 0, "fase nova: 0 vezes");

            float[] tempos = { 50.5f, 40.25f, 60f, 45f, 70f, 80f, 35f };
            int[] esperado = { 0, 0, 2, 1, 4, -1, 0 };
            for (int i = 0; i < tempos.Length; i++)
            {
                int pos = Progresso.RegistrarNoRanking(FaseFalsa, tempos[i], i);
                Progresso.RegistrarFase(FaseFalsa, tempos[i], i, 0);
                Conferir(pos == esperado[i], $"tempo {tempos[i]} entrou na posição {pos} (esperado {esperado[i]})");
            }
            var lista = Progresso.MelhoresTempos(FaseFalsa);
            Conferir(Tempos(lista) == "35,40.25,45,50.5,60", "top 5 em ordem: " + Tempos(lista));
            Conferir(lista[1].mortes == 1 && !string.IsNullOrEmpty(lista[1].data), "guarda mortes e data: " + lista[1].mortes + " / " + lista[1].data);
            Conferir(Progresso.VezesConcluida(FaseFalsa) == 7, "contou 7 vezes: " + Progresso.VezesConcluida(FaseFalsa));
            Conferir(Mathf.Approximately(Progresso.RecordeDe(FaseFalsa).tempo, 35f), "recorde antigo continua igual ao 1º lugar");
            Conferir(Progresso.RegistrarNoRanking(FaseFalsa, 35f, 0) == 1, "empate fica atrás de quem fez antes");

            // fase que já tinha recorde antes do painel existir
            Limpar(FaseFalsaComRecorde);
            Progresso.RegistrarFase(FaseFalsaComRecorde, 30f, 3, 0);
            var antiga = Progresso.MelhoresTempos(FaseFalsaComRecorde);
            Conferir(antiga.Count == 1 && antiga[0].mortes == -1, "recorde antigo aparece como 1º lugar");
            Conferir(Progresso.RegistrarNoRanking(FaseFalsaComRecorde, 31f, 1) == 1, "tempo pior que o recorde antigo fica em 2º");
            Conferir(Progresso.VezesConcluida(FaseFalsaComRecorde) == 2, "recorde antigo conta como 1 vez + esta");
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = culturaAntes;
            Limpar(FaseFalsa);
            Limpar(FaseFalsaComRecorde);
            PlayerPrefs.Save();
        }
        if (falhas == 0) Debug.Log("[TestePainel] TODOS OS TESTES PASSARAM");
        else Debug.LogError($"[TestePainel] {falhas} teste(s) falharam");
    }

    // No Play: dá 20 moedas para a partida (para testar a Loja sem coletar moedas).
    [MenuItem("Cilada (teste)/Dar 20 moedas")]
    public static void DarMoedas()
    {
        if (!Application.isPlaying || GerenciadorDoJogo.Instancia == null) { Debug.LogWarning("[TestePainel] aperte Play primeiro"); return; }
        var campo = typeof(GerenciadorDoJogo).GetField("moedas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        campo.SetValue(GerenciadorDoJogo.Instancia, (int)campo.GetValue(GerenciadorDoJogo.Instancia) + 20);
        Debug.Log("[TestePainel] +20 moedas. Agora: " + GerenciadorDoJogo.Instancia.Moedas);
    }

    // Apaga a marca de "já viu o tutorial da Loja" (o Louro aparece de novo depois da 2ª fase).
    [MenuItem("Cilada (teste)/Rever o tutorial da Loja")]
    public static void ReverTutorialDaLoja()
    {
        PlayerPrefs.DeleteKey("cilada_tutorial_loja");
        PlayerPrefs.Save();
        Debug.Log("[TestePainel] tutorial da Loja vai aparecer de novo (passe da 2ª fase ou volte ao mapa depois dela)");
    }

    // No Play: termina a fase na hora (como se tivesse encostado na bandeira), para ver o painel.
    [MenuItem("Cilada (teste)/Concluir fase agora %#k")]
    public static void ConcluirFase()
    {
        if (!Application.isPlaying || GerenciadorDoJogo.Instancia == null) { Debug.LogWarning("[TestePainel] aperte Play e entre numa fase"); return; }
        GerenciadorDoJogo.Instancia.FaseConcluida();
    }
}
