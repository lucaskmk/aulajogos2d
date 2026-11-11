using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ONDE O JOGO GUARDA TUDO (partidas, recordes, conquistas, opções, idioma, teclas): num ARQUIVO, e não mais no
// PlayerPrefs (que no Windows é o registro). Com arquivo, a Steam consegue sincronizar o save na nuvem (Steam
// Auto-Cloud: basta apontar para a pasta abaixo), e dá para copiar o save de um computador para outro.
//
// Tem os mesmos nomes do PlayerPrefs (GetInt, SetInt, HasKey, DeleteKey, Save...), então o resto do jogo usa igual.
//
//  - O arquivo: <Application.persistentDataPath>/save.json
//    (no Windows: C:\Users\<você>\AppData\LocalLow\<empresa>\<produto>\save.json). No editor é "save_editor.json",
//    para os testes do editor não mexerem no save de quem joga o build no mesmo computador.
//  - O save ANTIGO (do registro) não se perde: uma chave que ainda não está no arquivo é procurada no PlayerPrefs
//    e copiada na hora. Apagar uma chave apaga dos dois lugares.
//  - No navegador (WebGL) continua tudo no PlayerPrefs: lá o "arquivo" precisaria de cuidados a mais para não sumir.
//  - Save() grava o arquivo inteiro (só se algo mudou), primeiro num temporário e depois trocando de nome:
//    se o jogo fechar no meio, o save anterior continua inteiro.
public static class Guardado
{
    [System.Serializable]
    class Item { public string chave; public string tipo; public string valor; } // tipo: "i", "f" ou "s"

    [System.Serializable]
    class Conteudo { public List<Item> itens = new List<Item>(); }

    static Dictionary<string, Item> itens;
    static bool mudou;

    static bool NoPlayerPrefs => Application.platform == RuntimePlatform.WebGLPlayer;
    public static string Caminho => Path.Combine(Application.persistentDataPath, Application.isEditor ? "save_editor.json" : "save.json");

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar()
    {
        itens = null;
        mudou = false;
    }

    static void Carregar()
    {
        if (itens != null) return;
        itens = new Dictionary<string, Item>();
        try
        {
            if (!File.Exists(Caminho)) return;
            Conteudo lido = JsonUtility.FromJson<Conteudo>(File.ReadAllText(Caminho));
            if (lido == null || lido.itens == null) return;
            foreach (Item item in lido.itens)
                if (item != null && !string.IsNullOrEmpty(item.chave)) itens[item.chave] = item;
        }
        catch (System.Exception erro)
        {
            // arquivo estragado ou sem permissão: o jogo continua (com o que tiver no registro), sem travar
            Debug.LogWarning("[Guardado] não deu para ler o save (" + Caminho + "): " + erro.Message);
        }
    }

    // Procura a chave: no arquivo e, se não tiver, no save antigo do registro (e traz para o arquivo).
    static bool Achar(string chave, string tipo, out Item item)
    {
        Carregar();
        if (itens.TryGetValue(chave, out item)) return true;
        if (!PlayerPrefs.HasKey(chave)) return false;
        string valor = tipo == "i" ? PlayerPrefs.GetInt(chave).ToString(Cultura)
            : tipo == "f" ? PlayerPrefs.GetFloat(chave).ToString("R", Cultura) : PlayerPrefs.GetString(chave);
        item = new Item { chave = chave, tipo = tipo, valor = valor };
        itens[chave] = item;
        mudou = true;
        return true;
    }

    static readonly System.Globalization.CultureInfo Cultura = System.Globalization.CultureInfo.InvariantCulture;

    static void Por(string chave, string tipo, string valor)
    {
        Carregar();
        if (itens.TryGetValue(chave, out Item item) && item.tipo == tipo && item.valor == valor) return;
        itens[chave] = new Item { chave = chave, tipo = tipo, valor = valor };
        mudou = true;
    }

    public static bool HasKey(string chave)
    {
        if (NoPlayerPrefs) return PlayerPrefs.HasKey(chave);
        Carregar();
        return itens.ContainsKey(chave) || PlayerPrefs.HasKey(chave);
    }

    public static int GetInt(string chave, int padrao = 0)
    {
        if (NoPlayerPrefs) return PlayerPrefs.GetInt(chave, padrao);
        return Achar(chave, "i", out Item item) && int.TryParse(item.valor, System.Globalization.NumberStyles.Integer, Cultura, out int valor) ? valor : padrao;
    }

    public static float GetFloat(string chave, float padrao = 0f)
    {
        if (NoPlayerPrefs) return PlayerPrefs.GetFloat(chave, padrao);
        return Achar(chave, "f", out Item item) && float.TryParse(item.valor, System.Globalization.NumberStyles.Float, Cultura, out float valor) ? valor : padrao;
    }

    public static string GetString(string chave, string padrao = "")
    {
        if (NoPlayerPrefs) return PlayerPrefs.GetString(chave, padrao);
        return Achar(chave, "s", out Item item) ? item.valor : padrao;
    }

    public static void SetInt(string chave, int valor)
    {
        if (NoPlayerPrefs) PlayerPrefs.SetInt(chave, valor);
        else Por(chave, "i", valor.ToString(Cultura));
    }

    public static void SetFloat(string chave, float valor)
    {
        if (NoPlayerPrefs) PlayerPrefs.SetFloat(chave, valor);
        else Por(chave, "f", valor.ToString("R", Cultura));
    }

    public static void SetString(string chave, string valor)
    {
        if (NoPlayerPrefs) PlayerPrefs.SetString(chave, valor);
        else Por(chave, "s", valor ?? "");
    }

    public static void DeleteKey(string chave)
    {
        PlayerPrefs.DeleteKey(chave); // (também o save antigo: senão ele "voltaria" na próxima leitura)
        if (NoPlayerPrefs) return;
        Carregar();
        if (itens.Remove(chave)) mudou = true;
    }

    public static void Save()
    {
        if (NoPlayerPrefs)
        {
            PlayerPrefs.Save();
            return;
        }
        if (itens == null || !mudou) return;
        try
        {
            var conteudo = new Conteudo();
            conteudo.itens.AddRange(itens.Values);
            string temporario = Caminho + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(Caminho));
            File.WriteAllText(temporario, JsonUtility.ToJson(conteudo, true));
            if (File.Exists(Caminho)) File.Delete(Caminho);
            File.Move(temporario, Caminho);
            mudou = false;
        }
        catch (System.Exception erro)
        {
            Debug.LogWarning("[Guardado] não deu para gravar o save (" + Caminho + "): " + erro.Message);
        }
    }
}
