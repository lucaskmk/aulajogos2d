using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Luzes 2D de verdade (URP): a luz global de cada fase e luzinhas pontuais em moedas, blocos,
// cristais, portas, lampiões... O jogo usa sprites "iluminados", então a luz global um pouco abaixo de 1
// deixa o cenário levemente mais escuro e as luzinhas aparecem brilhando.
// As luzinhas só existem nas fases ESCURAS (noite, biblioteca): de dia elas "estouravam" em branco.
// (De dia elas nem são criadas: Ponto() devolve null. Por isso quem chama usa "luz?." / testa null.)
public static class Luzes
{
    const float LimiteDoEscuro = 0.8f; // luz global abaixo disso = fase escura, acende as luzinhas
    const float Forca = 0.55f;         // multiplica a intensidade de todas as luzinhas

    static Light2D global;
    static float intensidadeAtual = 1f;
    static readonly List<(Light2D luz, float intensidade)> pontos = new List<(Light2D, float)>();

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar()
    {
        pontos.Clear();
        intensidadeAtual = 1f;
    }

    public static void PrepararGlobal()
    {
        global = Object.FindAnyObjectByType<Light2D>();
        if (global != null && global.lightType == Light2D.LightType.Global) return;
        global = new GameObject("Global Light 2D").AddComponent<Light2D>();
        global.lightType = Light2D.LightType.Global;
        global.intensity = 1f;
    }

    // Quão clara é a fase (1 = normal; a fase noturna fica bem mais escura).
    public static void Ambiente(float intensidade, Color cor)
    {
        if (global == null) PrepararGlobal();
        global.intensity = intensidade;
        global.color = cor;
        intensidadeAtual = intensidade;
        pontos.RemoveAll(p => p.luz == null);
        foreach (var p in pontos) Aplicar(p.luz, p.intensidade);
    }

    static bool Escuro => intensidadeAtual < LimiteDoEscuro;

    static void Aplicar(Light2D luz, float intensidade)
    {
        if (!Escuro) Object.Destroy(luz.gameObject); // ficou claro: some de vez
        else luz.intensity = intensidade * Forca;
    }

    // Luz pontual presa a um objeto. raio em blocos.
    public static Light2D Ponto(Transform dono, Color cor, float raio, float intensidade, Vector3 deslocamento = default)
    {
        if (!Escuro) return null; // de dia não precisa de luzinha
        var objeto = new GameObject("Luz");
        objeto.transform.SetParent(dono, false);
        objeto.transform.localPosition = deslocamento;
        var luz = objeto.AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Point;
        luz.color = cor;
        luz.intensity = intensidade;
        luz.pointLightOuterRadius = raio;
        luz.pointLightInnerRadius = raio * 0.15f;
        luz.falloffIntensity = 0.65f;
        pontos.Add((luz, intensidade));
        Aplicar(luz, intensidade);
        return luz;
    }

    // Cores usadas no jogo inteiro
    public static readonly Color Dourada = new Color(1f, 0.85f, 0.4f);
    public static readonly Color Lilas = new Color(0.8f, 0.55f, 1f);
    public static readonly Color Quente = new Color(1f, 0.75f, 0.45f);
    public static readonly Color Verde = new Color(0.6f, 1f, 0.6f);
}
