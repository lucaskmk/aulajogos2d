// A DEMO da Steam (para o Steam Next Fest e a página "Em breve"): as fases 1 a 4, até a Capitã Âncora.
// Ela é o MESMO jogo, compilado com o símbolo CILADA_DEMO (menu "Cilada > Build Demo (fases 1 a 4)" em Construir.cs).
//  - o mapa só vai até a Capitã; vencer ela mostra o "fim da demo" (com o convite para a lista de desejos);
//  - o Mar de Baixo, as fases bônus e o modo tempestade não existem na demo;
//  - o save fica num arquivo próprio (save_demo.json): a demo não mistura com o jogo completo.
// Para testar a demo no editor: Project Settings > Player > Scripting Define Symbols, acrescente CILADA_DEMO.
public static class Demo
{
#if CILADA_DEMO
    public const bool Ativa = true;
#else
    public const bool Ativa = false;
#endif

    // A última fase da demo (a Capitã Âncora, índice 3).
    public const int UltimaFase = 3;

    // Esta fase existe na demo?
    public static bool TemAFase(int fase) => !Ativa || (fase >= 0 && fase <= UltimaFase) || fase == Fases.IndiceSecreto;
}
