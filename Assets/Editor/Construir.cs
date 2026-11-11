// Gera o build de WINDOWS (o que vai para a Steam; o WebGL do itch continua sendo feito pelo Build Profiles).
//
//  - no editor: menu "Cilada > Build Windows (Steam)" ou "Cilada > Build Windows de teste (com os atalhos F7 a F10)";
//  - pela linha de comando: Unity.exe -batchmode -quit -projectPath <pasta> -executeMethod Construir.WindowsPelaLinhaDeComando -logFile build.log
//    (sai com código 0 se deu certo).
//
// O jogo sai em Builds/Windows/CiladaAVista.exe (a pasta Builds não entra no git). O nome do arquivo não tem acento
// de propósito: o SteamPipe e alguns antivírus se atrapalham com acento em nome de executável.
// O build normal NÃO tem os atalhos de teste (F7, F8, F9 e o painel F10): eles só existem no editor e no build de teste
// (veja Controles.AtalhosDeTeste).
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Construir
{
    const string Pasta = "Builds/Windows", Executavel = "CiladaAVista.exe";

    [MenuItem("Cilada/Build Windows (Steam)")]
    public static void Windows() => Fazer(deTeste: false);

    [MenuItem("Cilada/Build Windows de teste (com os atalhos F7 a F10)")]
    public static void WindowsDeTeste() => Fazer(deTeste: true);

    public static void WindowsPelaLinhaDeComando() => EditorApplication.Exit(Fazer(deTeste: false) ? 0 : 1);

    const string Icone = "Assets/Icone/icone.png"; // (gerado por Ferramentas/gerar_icone.py, a partir do desenho do Grumete)

    // O ícone do executável: o Grumete em pixel art, sem borrar (filtro "Point", sem compressão).
    static void PorOIcone()
    {
        var importador = AssetImporter.GetAtPath(Icone) as TextureImporter;
        if (importador == null)
        {
            Debug.LogWarning("[Construir] não achei " + Icone + ": o build sai com o ícone padrão do Unity");
            return;
        }
        if (importador.filterMode != FilterMode.Point || importador.mipmapEnabled || importador.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importador.filterMode = FilterMode.Point;
            importador.mipmapEnabled = false;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.SaveAndReimport();
        }
        var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(Icone);
        PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { textura }, IconKind.Any);
    }

    static bool Fazer(bool deTeste)
    {
        PorOIcone();
        // as cenas marcadas no Build Profiles; se não tiver nenhuma, a cena do projeto (o jogo se cria sozinho em qualquer cena)
        var cenas = new List<string>();
        foreach (EditorBuildSettingsScene cena in EditorBuildSettings.scenes)
            if (cena.enabled) cenas.Add(cena.path);
        if (cenas.Count == 0) cenas.Add("Assets/Scenes/SampleScene.unity");

        var opcoes = new BuildPlayerOptions
        {
            scenes = cenas.ToArray(),
            locationPathName = Pasta + (deTeste ? "DeTeste/" : "/") + Executavel,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = deTeste ? BuildOptions.Development : BuildOptions.None,
        };
        BuildSummary resumo = BuildPipeline.BuildPlayer(opcoes).summary;
        bool deuCerto = resumo.result == BuildResult.Succeeded;
        string texto = $"[Construir] {(deuCerto ? "PRONTO" : "FALHOU")}: {opcoes.locationPathName} "
            + $"({resumo.totalSize / (1024 * 1024)} MB, {resumo.totalTime.TotalSeconds:0} s, {resumo.totalErrors} erro(s), {resumo.totalWarnings} aviso(s))";
        if (deuCerto) Debug.Log(texto);
        else Debug.LogError(texto);
        return deuCerto;
    }
}
