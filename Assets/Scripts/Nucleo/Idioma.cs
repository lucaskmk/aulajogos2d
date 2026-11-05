using UnityEngine;

// O idioma do jogo: Português ou Inglês. Fica salvo no PlayerPrefs (troca pelo botão "Idioma" do título).
// Na primeira vez que o jogo abre, ele escolhe sozinho: Português se o computador estiver em português,
// Inglês em qualquer outra língua.
//
// COMO OS TEXTOS SÃO TRADUZIDOS (duas formas):
//  1) Idioma.T("texto em português", "text in English"): direto no código, para textos montados na hora
//     (com números, nomes...). Ex.: Idioma.T($"Fase {n}", $"Level {n}").
//  2) Idioma.Traduzir("texto em português"): procura o texto na lista de Traducoes.cs. A Interface já faz isso
//     sozinha com TODO texto que ela desenha (e com as falas, as placas e os avisos). Então, para traduzir
//     um texto fixo (uma placa, uma fala, uma opção de menu), basta colocar o par em Traducoes.cs.
//     ATENÇÃO: mudou um texto em português? Mude a chave em Traducoes.cs também (tem que ser IGUALZINHO,
//     com os mesmos acentos, pontos e "\n"), senão no modo inglês ele aparece em português.
public static class Idioma
{
    public enum Lingua { Portugues, Ingles }

    const string Chave = "cilada_idioma";
    static int atual = -1; // -1 = ainda não leu do PlayerPrefs

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => atual = -1;

    public static Lingua Atual
    {
        get
        {
            if (atual < 0)
            {
                int padrao = Application.systemLanguage == SystemLanguage.Portuguese ? 0 : 1;
                atual = PlayerPrefs.GetInt(Chave, padrao);
            }
            return (Lingua)atual;
        }
        set
        {
            atual = (int)value;
            PlayerPrefs.SetInt(Chave, atual);
            PlayerPrefs.Save();
        }
    }

    public static bool Ingles => Atual == Lingua.Ingles;

    // O nome do idioma atual, como aparece no botão do título.
    public static string Nome => Ingles ? "English" : "Português";

    // Troca Português <-> Inglês (e salva).
    public static void Trocar() => Atual = Ingles ? Lingua.Portugues : Lingua.Ingles;

    // Escolhe entre os dois textos, conforme o idioma.
    public static string T(string portugues, string ingles) => Ingles ? ingles : portugues;

    // Procura a tradução em Traducoes.cs. Sem tradução (ou em português), devolve o próprio texto.
    public static string Traduzir(string portugues)
    {
        if (!Ingles || string.IsNullOrEmpty(portugues)) return portugues;
        return Traducoes.Ingles.TryGetValue(portugues, out string ingles) ? ingles : portugues;
    }
}
