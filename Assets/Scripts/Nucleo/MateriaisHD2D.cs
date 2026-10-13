using UnityEngine;

// Os materiais com shaders feitos por nós (em Assets/Resources/Shaders):
//  - LuzAditiva: a cor do sprite SOMA com a tela (raios de sol, brilho do sol);
//  - SombraMultiplicada: a tela é MULTIPLICADA pela cor do sprite (sombras das nuvens).
//
// SEGURANÇA: um shader com erro (ou que a placa de vídeo não aceita) vira um sprite ROSA na tela.
// Por isso aqui sempre conferimos se o shader carregou e é suportado (shader.isSupported). Se não for,
// devolvemos null e quem pediu continua com o material normal de sprite: o efeito fica mais simples,
// mas o jogo nunca depende de um shader para funcionar.
// Cada material é criado uma vez só e reaproveitado pelo jogo inteiro (todas as fases, todas as mortes).
public static class MateriaisHD2D
{
    static Material luzAditiva, sombraMultiplicada;
    static bool tentouLuz, tentouSombra;

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar()
    {
        luzAditiva = sombraMultiplicada = null;
        tentouLuz = tentouSombra = false;
    }

    public static Material LuzAditiva => Carregar("Shaders/LuzAditiva", ref luzAditiva, ref tentouLuz);
    public static Material SombraMultiplicada => Carregar("Shaders/SombraMultiplicada", ref sombraMultiplicada, ref tentouSombra);

    static Material Carregar(string caminho, ref Material material, ref bool tentou)
    {
        if (material != null || tentou) return material;
        tentou = true; // se falhar, não fica tentando de novo a cada quadro
        Shader shader = Resources.Load<Shader>(caminho);
        if (shader == null || !shader.isSupported)
        {
            Debug.LogWarning($"Shader \"{caminho}\" não carregou ou não é suportado aqui: usando o material normal de sprite.");
            return null;
        }
        material = new Material(shader) { name = caminho, hideFlags = HideFlags.DontSave };
        return material;
    }

    // Põe a luz aditiva no sprite. Devolve false (e não mexe em nada) se o shader não estiver disponível.
    public static bool UsarLuzAditiva(SpriteRenderer desenho)
    {
        Material material = LuzAditiva;
        if (material == null || desenho == null) return false;
        desenho.sharedMaterial = material;
        return true;
    }
}
