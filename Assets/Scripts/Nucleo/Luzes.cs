using UnityEngine;
using UnityEngine.Rendering.Universal;

// Luzes 2D de verdade (URP): a luz global de cada fase e luzinhas pontuais em moedas, blocos,
// cristais, portas, lampiões... O jogo usa sprites "iluminados", então a luz global um pouco abaixo de 1
// deixa o cenário levemente mais escuro e as luzinhas aparecem brilhando.
public static class Luzes
{
    static Light2D global;

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
    }

    // Luz pontual presa a um objeto. raio em blocos.
    public static Light2D Ponto(Transform dono, Color cor, float raio, float intensidade, Vector3 deslocamento = default)
    {
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
        return luz;
    }

    // Cores usadas no jogo inteiro
    public static readonly Color Dourada = new Color(1f, 0.85f, 0.4f);
    public static readonly Color Lilas = new Color(0.8f, 0.55f, 1f);
    public static readonly Color Quente = new Color(1f, 0.75f, 0.45f);
    public static readonly Color Verde = new Color(0.6f, 1f, 0.6f);
}
