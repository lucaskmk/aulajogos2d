using System;
using System.Collections.Generic;

// TODAS as músicas do jogo, compostas e sintetizadas por código, no clima de navio pirata.
// Composição ORIGINAL (melodias, acordes e ritmos escritos para este jogo), e o sintetizador também foi
// escrito do zero aqui: acordeão, apito, violão (corda de Karplus-Strong), violino, as ondas do mar,
// tambor de mão, pisadas no convés, palmas e o sino do navio, além das ondas "8-bit" de videogame.
//
//  - Fases:             "Jiga do Grumete": ré menor, 6/8, dança de taverna (forma AABB), acordeão e apito.
//                       Cada fase toca num tom um pouco diferente (veja FabricaDeMusica.DaFase).
//  - Mapa:              "Balada do Arquipélago": lá menor, 3/4, violão dedilhado, ondas e o sino.
//  - Capitã (fase 6):   mi menor, 160 bpm, 4/4. Gancho no ritmo 3-3-2 no acordeão, violino e violão "girando".
//  - Cozinheira (13):   dó menor, 6/8 (uma valsa sombria e rápida), acordeão + violino + caixinha de música.
//  - Baleia (fase 16):  ré menor, 172 bpm. Estalos de orquestra, coro, baixo "galopando", o canto da baleia
//                       (notas que deslizam) e, na segunda vez do gancho, a música sobe um tom.
//
// Este arquivo NÃO usa a Unity (só C# puro): assim dá para gerar as músicas num programa de teste fora da
// Unity e ouvir os WAVs. Quem transforma o resultado num AudioClip é a FabricaDeMusica.
//
// COMO AS MÚSICAS SÃO ESCRITAS (igual às fases: em texto!)
//  Cada "passo" é uma semicolcheia (nas jigas e na balada, uma colcheia). Num texto de notas, cada palavra
//  ocupa um passo:
//    e5   toca a nota (letra, # ou b, oitava: "c#5", "bb4"; c4 é o dó central)
//    -    segura a nota anterior por mais um passo
//    .    silêncio
//    |    só separa os compassos (não ocupa passo)
//    d4>a4  a nota DESLIZA de ré 4 até lá 4 (o canto da baleia)
//  Nos padrões tirados dos ACORDES (baixo, arpejos), em vez da nota vai o "grau" do acorde atual:
//    1 = fundamental, 3 = terça, 5 = quinta, 7 = sétima, 8 = fundamental uma oitava acima,
//    10 = terça acima, 12 = quinta acima, 15 = duas oitavas, x = o acorde inteiro.
//  Na bateria, cada passo junta letras: k bumbo, s caixa (S forte), h chimbal, o chimbal aberto,
//    c prato, t tom agudo, m tom grave, p tímpano, x palmas, b tambor de mão, f pisada no convés,
//    g sino do navio.
//  Padrões mais curtos que a parte (baixo, arpejo, bateria) se repetem até o fim dela.
//  Os acordes são um por compasso ("Em C D B7"); com vírgula, dois no mesmo compasso ("Am,D").
public static class Musicas
{
    public enum Tema { Capita, Cozinheira, Baleia, Fase, Mapa }

    public const int Taxa = 22050;

    // Gera a música inteira (mono, de -1 a 1). Ela emenda sozinha: o fim continua no começo (loop).
    public static float[] Compor(Tema tema, int transpor = 0)
    {
        Partitura partitura = tema == Tema.Capita ? Capita() : tema == Tema.Cozinheira ? Cozinheira() : tema == Tema.Baleia ? Baleia()
            : tema == Tema.Fase ? Fase() : Mapa();
        partitura.transporTudo = transpor;
        return partitura.Sintetizar();
    }

    // Duração de cada passo (semicolcheia), em segundos.
    public static float SegundosPorPasso(float bpm) => 60f / bpm / 4f;

#if TESTE_MUSICA
    public static string RelatorioDoTeste => Sintetizador.Relatorio;
#endif

    // ==================================================================================================
    //  AS MÚSICAS
    // ==================================================================================================

    static readonly int[] Menor = { 0, 2, 3, 5, 7, 8, 10 }; // escala menor natural

    // Repete um padrão de um compasso "vezes" vezes (para pôr uma virada de bateria só no último).
    static string Vezes(string padrao, int vezes)
    {
        var partes = new string[vezes];
        for (int i = 0; i < vezes; i++) partes[i] = padrao;
        return string.Join(" | ", partes);
    }

    // ---------------------------------------------------------------- CAPITÃ (fase 6): "Âncora Giratória"
    static Partitura Capita()
    {
        var m = new Partitura(bpm: 160f, passosPorCompasso: 16, tonica: 4, escala: Menor) // mi menor
        {
            eco = 0.22f, sala = 0.1f, bombear = 0.18f,
        };
        const string rock = "kfh . h . shx . h . kh . kfh . shx . h h"; // com pisadas no convés e palmas
        const string corrida = "kfh h h h kshx h h h kfh h h h kshx h h h"; // bumbo em todo tempo (parte B)
        const string virada = "kh . h . sh . h . t t t t m m S S";

        // O gancho: nota repetida no ritmo 3-3-2 e um salto para cima.
        const string gancho =
            "e5 - . e5 - . g5 - b5 - . a5 - . g5 - | e5 - . e5 - . g5 - c6 - - - b5 - g5 - | " +
            "d5 - . d5 - . f#5 - a5 - . g5 - . f#5 - | d#5 - . d#5 - . f#5 - b5 - - - a5 - f#5 - | " +
            "e5 - . e5 - . g5 - b5 - . a5 - . g5 - | e5 - . e5 - . g5 - c6 - - - d6 - e6 - | " +
            "e6 - . d6 - . c6 - b5 - . a5 - . f#5 - | g5 - f#5 - e5 - - - - - . . b4 - d5 -";
        const string acordesDoGancho = "Em C D B7 Em C Am,D Em";

        // INTRO: o riff do baixo sozinho, com a bateria.
        m.Parte(2, "Em Em")
            .Notas(Timbre.Baixo, Bus.Seco, 1.1f,
                "e2 - e3 . e2 - d3 . e2 - c3 . e2 - b2 . | e2 - e3 . e2 - d3 . e2 - c3 . b2 - d#3 -")
            .Notas(Timbre.Baixo, Bus.Seco, 0.45f,
                "e3 - e4 . e3 - d4 . e3 - c4 . e3 - b3 . | e3 - e4 . e3 - d4 . e3 - c4 . b3 - d#4 -")
            .Bateria("kcg . . . s . . . k . k . s . . . | k . . . s . . . k . k . s s s S"); // o sino do navio abre a luta

        // A: o gancho.
        m.Parte(8, acordesDoGancho)
            .Notas(Timbre.Acordeao, Bus.Eco, 1f, gancho)
            .Notas(Timbre.Quadrada, Bus.Eco, 0.4f, gancho)
            .DosAcordes(Timbre.Baixo, Bus.Seco, 1f, 2, "1 . 8 . 1 . 8 . 1 . 8 . 1 . 8 .")
            .DosAcordes(Timbre.Violao, Bus.Seco, 0.85f, 4, "1 5 8 5 10 5 8 5")
            .Bateria(Vezes(rock, 7) + " | " + virada, prato: true);

        // B: a Capitã fica brava. Riff "girando" em semicolcheias embaixo e uma melodia longa por cima.
        m.Parte(8, "C D Em Em C D B B")
            .Notas(Timbre.Violino, Bus.Eco, 1.7f,
                "e5 - - - - - - - g5 - - - - - c6 - | b5 - - - - - - - a5 - - - f#5 - d5 - | " +
                "e5 - - - - - - - g5 - - - b5 - - - | e6 - - - - - - - d6 - - - b5 - - - | " +
                "c6 - - - - - - - b5 - - - g5 - e5 - | f#5 - - - - - - - a5 - - - d6 - - - | " +
                "d#6 - - - - - - - b5 - - - f#5 - - - | a5 - - - - - - - f#5 - - - d#5 - - -")
            .Notas(Timbre.Violao, Bus.Seco, 0.9f,
                "g4 e4 c4 e4 g4 e4 c4 e4 a4 e4 c4 e4 g4 e4 c4 e4 | f#4 d4 a3 d4 f#4 d4 a3 d4 a4 f#4 d4 f#4 b4 a4 f#4 d4 | " +
                "g4 e4 b3 e4 g4 e4 b3 e4 b4 g4 e4 g4 b4 g4 e4 g4 | e5 - - - d5 - - - b4 - - - g4 - a4 - | " +
                "g4 e4 c4 e4 g4 e4 c4 e4 a4 e4 c4 e4 g4 e4 c4 e4 | f#4 d4 a3 d4 f#4 d4 a3 d4 a4 f#4 d4 f#4 b4 a4 f#4 d4 | " +
                "f#4 d#4 b3 d#4 f#4 d#4 b3 d#4 a4 f#4 d#4 f#4 b4 a4 f#4 d#4 | b4 - - - - - - - a4 - - - f#4 - d#4 -")
            .DosAcordes(Timbre.Baixo, Bus.Seco, 1f, 2, "1 - 1 - 8 - 1 - 1 - 8 - 1 - 8 -")
            .Acordes(Timbre.Pad, Bus.Sala, 0.8f, 3)
            .Bateria(Vezes(corrida, 7) + " | ksh . ksh . ksh . ksh . t t m m S S S S", prato: true);

        // A': o gancho de novo, com uma segunda voz uma terça abaixo e um brilho uma oitava acima.
        m.Parte(8, acordesDoGancho)
            .Notas(Timbre.Acordeao, Bus.Eco, 1f, gancho)
            .Notas(Timbre.Quadrada, Bus.Eco, 0.4f, gancho)
            .Harmonia(Timbre.Violino, Bus.Sala, 0.8f, gancho)
            .Notas(Timbre.Apito, Bus.Eco, 0.35f, gancho, transpor: 12)
            .DosAcordes(Timbre.Baixo, Bus.Seco, 1f, 2, "1 . 8 . 1 . 8 . 1 . 8 . 1 . 8 .")
            .DosAcordes(Timbre.Violao, Bus.Seco, 0.75f, 4, "1 5 8 5 10 5 8 5")
            .Bateria(Vezes(rock, 7) + " | " + virada, prato: true);

        // C: respiro. Só os acordes, o arpejo e a bateria em meio-tempo... crescendo para voltar ao riff.
        m.Parte(4, "C D Em Em")
            .Acordes(Timbre.Pad, Bus.Sala, 1f, 3)
            .DosAcordes(Timbre.Violao, Bus.Seco, 0.85f, 4, "1 5 8 12 10 8 5 3")
            .DosAcordes(Timbre.Baixo, Bus.Seco, 0.9f, 2, "1 - - - - - - - 1 - - - 8 - - -")
            .Bateria("kc . . . . . . . s . . . . . . . | k . . . . . . . s . . . . . . . | " +
                     "k . s . k . s . k s k s k s s s | s s s s s s s s S S S S S S S S");
        return m;
    }

    // ---------------------------------------------------------------- COZINHEIRA (fase 13): "Valsa das Lâminas"
    static Partitura Cozinheira()
    {
        var m = new Partitura(bpm: 132f, passosPorCompasso: 12, tonica: 0, escala: Menor) // dó menor, 6/8
        {
            eco = 0.25f, sala = 0.16f, bombear = 0f,
        };
        const string valsa = "kh . h . h . sh . h . h .";
        const string valsaForte = "kh . h . kh . sh . h . kh .";

        // O gancho: uma nota longa, duas semicolcheias "cortando" e três colcheias descendo.
        const string gancho =
            "c5 - - - b4 c5 eb5 - d5 - c5 - | b4 - c5 - d5 - g4 - - - . . | " +
            "c5 - - - b4 c5 eb5 - f5 - g5 - | ab5 - - - g5 - f5 - eb5 - d5 - | " +
            "c5 - - - b4 c5 eb5 - d5 - c5 - | f5 - - - e5 f5 ab5 - g5 - f5 - | " +
            "eb5 - d5 - c5 - b4 - d5 - f5 - | c5 - - - - - . . g4 - b4 -";
        const string acordesDoGancho = "Cm Cm Ab G Cm Fm G7 Cm";
        const string pizz = "1 - - . 5 . 8 - - . 5 .";

        // INTRO: a caixinha de música sozinha, e o chimbal "tic, tic".
        m.Parte(2, "Cm Cm")
            .Notas(Timbre.Sino, Bus.Sala, 1f, "c6 . g5 . eb6 . g5 . d6 . g5 . | c6 . g5 . eb6 . g5 . b5 . g5 .")
            .Notas(Timbre.Pizzicato, Bus.Seco, 1f, "c3 - - . . . g2 - - . . . | c3 - - . . . g2 - - . b2 .")
            .Bateria("h . . . h . . . h . . . | h . . . h . . . s . S .");

        // A: o gancho no cravo, com o violino uma oitava abaixo.
        m.Parte(8, acordesDoGancho)
            .Notas(Timbre.Acordeao, Bus.Eco, 1f, gancho)
            .Notas(Timbre.Violino, Bus.Sala, 0.55f, gancho, transpor: -12)
            .DosAcordes(Timbre.Pizzicato, Bus.Seco, 1f, 2, pizz)
            .Acordes(Timbre.Pad, Bus.Sala, 0.55f, 3)
            .Bateria(Vezes(valsa, 7) + " | kh . h . sh . s . s s S S", prato: true);

        // B: o violino canta lá no alto; o cravo vira arpejo.
        m.Parte(8, "Ab Bb Gm Cm Ab Bb G G")
            .Notas(Timbre.Violino, Bus.Eco, 1f,
                "c6 - - - - - bb5 - ab5 - g5 - | f5 - - - - - d5 - eb5 - f5 - | g5 - - - - - bb5 - - - d6 - | c6 - - - - - - - - - . . | " +
                "eb6 - - - - - d6 - c6 - bb5 - | ab5 - - - - - g5 - f5 - d5 - | b4 - d5 - f5 - ab5 - - - g5 - | f5 - eb5 - d5 - b4 - - - g4 -")
            .DosAcordes(Timbre.Violao, Bus.Seco, 0.7f, 4, "1 5 8 5 10 8 12 8 10 8 5 3")
            .DosAcordes(Timbre.Pizzicato, Bus.Seco, 1f, 2, pizz)
            .Acordes(Timbre.Pad, Bus.Sala, 0.6f, 3)
            .Bateria(Vezes(valsaForte, 7) + " | ksh . ksh . ksh . t . t m m S", prato: true);

        // A': o gancho de novo, com o violino uma terça abaixo e a caixinha de música brilhando em cima.
        m.Parte(8, acordesDoGancho)
            .Notas(Timbre.Acordeao, Bus.Eco, 1f, gancho)
            .Harmonia(Timbre.Violino, Bus.Sala, 0.7f, gancho)
            .Notas(Timbre.Sino, Bus.Sala, 0.4f, gancho, transpor: 12)
            .DosAcordes(Timbre.Pizzicato, Bus.Seco, 1f, 2, pizz)
            .Acordes(Timbre.Pad, Bus.Sala, 0.55f, 3)
            .Bateria(Vezes(valsaForte, 7) + " | kh . h . sh . s . s s S S", prato: true);

        // C: a sala escurece. Caixinha de música, um "coração" no bumbo e o violino descendo devagar.
        m.Parte(4, "Cm Ab Fm G")
            .DosAcordes(Timbre.Sino, Bus.Sala, 0.8f, 5, "1 . 5 . 3 . 8 . 5 . 3 .")
            .Notas(Timbre.Violino, Bus.Eco, 0.8f,
                "g5 - - - - - - - - - f#5 - | f5 - - - - - - - - - e5 - | eb5 - - - - - - - - - d5 - | b4 - - - - - - - - - . .")
            .DosAcordes(Timbre.Pizzicato, Bus.Seco, 0.9f, 2, "1 - - . . . 5 - - . . .")
            .Acordes(Timbre.Pad, Bus.Sala, 0.7f, 3)
            .Bateria("k . . . . . k . . . . . | k . . . . . k . . . . . | k . . . . . k . . . . . | k . . . h . k . h . h .");
        return m;
    }

    // ---------------------------------------------------------------- BALEIA (fase 16, chefe final): "Leviatã"
    static Partitura Baleia()
    {
        var m = new Partitura(bpm: 172f, passosPorCompasso: 16, tonica: 2, escala: Menor) // ré menor
        {
            eco = 0.2f, sala = 0.14f, bombear = 0.42f,
        };
        const string pista = "kh h h h ksh h h h kh h h h ksh h h h"; // bumbo em todo tempo
        const string rock = "kh . h . sh . h . kh . kh . sh . h h";
        const string galope = "1 . 1 1 8 . 8 8 1 . 1 1 8 . 8 8";

        // O gancho: salto de oitava no ritmo 3-3-3-3-2-2 (e a sequência um semitom acima, no Mi bemol).
        const string gancho =
            "d5 - . d6 - . a5 - . g5 - . f5 - e5 - | eb5 - . eb6 - . bb5 - . g5 - . f5 - eb5 - | " +
            "d5 - . d6 - . a5 - . g5 - . f5 - a5 - | g5 - - - f5 - eb5 - d5 - - - - - . . | " +
            "f5 - . bb5 - . d6 - . c6 - . bb5 - a5 - | g5 - . bb5 - . d6 - . c6 - . bb5 - g5 - | " +
            "a5 - - - e6 - - - c#6 - - - a5 - - - | c#5 - d5 - e5 - f5 - g5 - a5 - bb5 - c#6 -";
        const string acordesDoGancho = "Dm Eb Dm Eb Bb Gm A A";
        const string viradaDoGancho = "kh h h h ksh h h h t t t t m m S S";

        // INTRO: BRAM! Estalos de orquestra, tímpanos, o coro... e o canto da baleia deslizando.
        m.Parte(4, "Dm Eb Dm A")
            .DosAcordes(Timbre.Estalo, Bus.Sala, 1f, 4, "x - - - - - - - . . . . . . . .")
            .Acordes(Timbre.Coro, Bus.Sala, 0.9f, 3)
            .Notas(Timbre.Canto, Bus.Sala, 1f,
                "d4>a4 - - - - - - - - - - - - - - - | a4>f4 - - - - - - - - - - - - - - - | " +
                "f4>d5 - - - - - - - - - - - - - - - | . . . . . . . . . . . . . . . .")
            .DosAcordes(Timbre.Baixo, Bus.Seco, 0.9f, 2, "1 - - - - - - - - - - - - - - -")
            .Bateria("pcg . . . . . . . . . . . . . . . | p . . . . . . . p . . . . . . . | " +
                     "pc . . . . . . . . . . . . . . . | m . m . t . t . m m t t S S S S");

        // A: o gancho, com o baixo galopando e o "bombeado" do bumbo nos acordes.
        m.Parte(8, acordesDoGancho)
            .Notas(Timbre.Acordeao, Bus.Eco, 1f, gancho)
            .Notas(Timbre.Quadrada, Bus.Eco, 0.4f, gancho)
            .Notas(Timbre.Apito, Bus.Eco, 0.45f, gancho, transpor: 12)
            .DosAcordes(Timbre.Baixo, Bus.Seco, 1f, 2, galope)
            .DosAcordes(Timbre.Violao, Bus.Seco, 0.8f, 4, "1 5 8 10 12 10 8 5")
            .Acordes(Timbre.Pad, Bus.Sala, 0.8f, 3)
            .Bateria(Vezes(pista, 7) + " | " + viradaDoGancho, prato: true);

        // B: a melodia "voa" lá no alto (violino + quadrada uma oitava abaixo), com o coro atrás.
        const string voo =
            "d6 - - - - - - - f6 - - - - - d6 - | e6 - - - - - - - c6 - - - - - g5 - | a5 - - - - - - - - - - - - - - - | " +
            ". . . . d6 - c6 - a5 - - - f5 - g5 - | f5 - - - - - - - bb5 - - - d6 - f6 - | g6 - - - - - - - e6 - - - c6 - - - | " +
            "c#6 - - - - - - - e6 - - - - - - - | a5 - - - - - - - . . . . . . . .";
        m.Parte(8, "Bb C Dm Dm Bb C A A")
            .Notas(Timbre.Violino, Bus.Eco, 1f, voo)
            .Notas(Timbre.Acordeao, Bus.Sala, 0.6f, voo, transpor: -12)
            .Acordes(Timbre.Coro, Bus.Sala, 1f, 3)
            .DosAcordes(Timbre.Baixo, Bus.Seco, 1f, 2, "1 - 8 - 1 - 8 - 1 - 8 - 1 - 8 -")
            .DosAcordes(Timbre.Violao, Bus.Seco, 0.75f, 4, "1 5 8 10 12 10 8 5")
            .Bateria(Vezes(rock, 7) + " | ksh . ksh . ksh . ksh . t t t t S S S S", prato: true);

        // A2: o gancho UM TOM ACIMA (mi menor), com uma segunda voz uma terça abaixo. É o clímax.
        m.Parte(8, acordesDoGancho, transpor: 2)
            .Notas(Timbre.Acordeao, Bus.Eco, 1f, gancho)
            .Notas(Timbre.Quadrada, Bus.Eco, 0.4f, gancho)
            .Harmonia(Timbre.Violino, Bus.Sala, 0.8f, gancho)
            .Notas(Timbre.Apito, Bus.Eco, 0.4f, gancho, transpor: 12)
            .DosAcordes(Timbre.Baixo, Bus.Seco, 1f, 2, galope)
            .DosAcordes(Timbre.Violao, Bus.Seco, 0.75f, 4, "1 5 8 10 12 10 8 5")
            .Acordes(Timbre.Pad, Bus.Sala, 0.8f, 3)
            .Bateria(Vezes(pista, 7) + " | " + viradaDoGancho, prato: true);

        // PONTE: de volta para ré menor, em meio-tempo: o coro, os tímpanos e a baleia cantando de novo.
        m.Parte(4, "Bb C Eb A")
            .Acordes(Timbre.Coro, Bus.Sala, 1f, 3)
            .Notas(Timbre.Canto, Bus.Sala, 0.9f,
                "f4>bb4 - - - - - - - - - - - - - - - | g4>c5 - - - - - - - - - - - - - - - | " +
                "g4>eb5 - - - - - - - - - - - - - - - | e4>a4 - - - - - - - . . . . . . . .")
            .DosAcordes(Timbre.Baixo, Bus.Seco, 1f, 2, "1 - - - - - - - - - - - 8 - - -")
            .Bateria("pc . . . . . . . s . . . . . . . | p . . . . . . . s . . . . . . . | " +
                     "p . . . . . . . s . . . . . . . | k . k . s . k . k k s s S S S S");
        return m;
    }

    // ---------------------------------------------------------------- FASES: "Jiga do Grumete"
    // Uma jiga de taverna em 6/8, em ré menor (com o si natural do modo dórico, que dá o gosto de "mar").
    // Aqui cada passo é uma COLCHEIA (6 por compasso), e não uma semicolcheia: a jiga anda em colcheias.
    // Forma AABB, igual às músicas de dança de marinheiro: A no acordeão, A' com o apito por cima,
    // B lá no alto com o apito, B' com o acordeão e o violino fazendo a segunda voz.
    static Partitura Fase()
    {
        var m = new Partitura(bpm: 84f, passosPorCompasso: 6, tonica: 2, escala: Menor) // ré menor, 6/8
        {
            eco = 0.12f, sala = 0.16f, bombear = 0f,
        };
        const string violao = "x . x x . x"; // o violão "batendo" os acordes
        const string baixo = "1 . . 5 . .";
        const string bodhran = "bf . . b . .";
        const string bodhranFraco = ". b b . b b";

        const string a =
            "a4 d5 d5 f5 d5 d5 | a5 d5 d5 f5 e5 d5 | g4 c5 c5 e5 c5 c5 | g5 c5 c5 e5 d5 c5 | " +
            "a4 d5 d5 f5 d5 f5 | a5 g5 f5 g5 a5 b5 | c6 a5 f5 g5 e5 c5 | d5 e5 c5 d5 - -";
        const string acordesDoA = "Dm Dm C C Dm G F,C Dm";
        const string b =
            "c6 - a5 f5 a5 c6 | e6 - c6 g5 c6 e6 | d6 - a5 f5 a5 d6 | e6 d6 c6 a5 - - | " +
            "f6 - e6 d6 c6 a5 | g6 - e6 c6 d6 e6 | d6 b5 g5 c#6 e6 a5 | d6 - - a5 - -";
        const string acordesDoB = "F C Dm Am F C G,A Dm";

        // INTRO: o sino do navio, uma onda e o violão chamando a dança.
        m.Parte(2, "Dm Dm")
            .Notas(Timbre.Mar, Bus.Sala, 1f, "d4 - - - - - | - - - - - -")
            .DosAcordes(Timbre.Violao, Bus.Seco, 0.9f, 3, violao)
            .DosAcordes(Timbre.Baixo, Bus.Seco, 0.8f, 2, baixo)
            .Bateria("gb . . b . . | b . . b b b");

        // A: o acordeão puxa a melodia, o violino vai junto baixinho.
        m.Parte(8, acordesDoA)
            .Notas(Timbre.Acordeao, Bus.Eco, 1f, a)
            .Notas(Timbre.Violino, Bus.Sala, 0.4f, a)
            .DosAcordes(Timbre.Violao, Bus.Seco, 1f, 3, violao)
            .DosAcordes(Timbre.Baixo, Bus.Seco, 1f, 2, baixo)
            .Bateria(bodhran)
            .Bateria(bodhranFraco, volume: 0.4f);

        // A': de novo, com o apito uma oitava acima e as palmas da tripulação.
        m.Parte(8, acordesDoA)
            .Notas(Timbre.Acordeao, Bus.Eco, 1f, a)
            .Notas(Timbre.Apito, Bus.Eco, 0.45f, a, transpor: 12)
            .Harmonia(Timbre.Violino, Bus.Sala, 0.45f, a)
            .DosAcordes(Timbre.Violao, Bus.Seco, 1f, 3, violao)
            .DosAcordes(Timbre.Baixo, Bus.Seco, 1f, 2, baixo)
            .Bateria(bodhran)
            .Bateria(bodhranFraco, volume: 0.4f)
            .Bateria(". . . x . .", volume: 0.8f);

        // B: a melodia sobe; o apito canta e o acordeão segura os acordes.
        m.Parte(8, acordesDoB)
            .Notas(Timbre.Apito, Bus.Eco, 1f, b)
            .Notas(Timbre.Violino, Bus.Sala, 0.45f, b, transpor: -12)
            .Acordes(Timbre.Acordeao, Bus.Sala, 0.3f, 3)
            .DosAcordes(Timbre.Violao, Bus.Seco, 1f, 3, violao)
            .DosAcordes(Timbre.Baixo, Bus.Seco, 1f, 2, baixo)
            .Bateria(bodhran)
            .Bateria(bodhranFraco, volume: 0.45f)
            .Bateria(". . . x . .", volume: 0.8f);

        // B': o acordeão pega a melodia e o violino faz a segunda voz. A virada final volta para o A.
        m.Parte(8, acordesDoB)
            .Notas(Timbre.Acordeao, Bus.Eco, 1f, b)
            .Harmonia(Timbre.Violino, Bus.Sala, 0.55f, b)
            .DosAcordes(Timbre.Violao, Bus.Seco, 1f, 3, violao)
            .DosAcordes(Timbre.Baixo, Bus.Seco, 1f, 2, baixo)
            .Bateria(Vezes(bodhran, 7) + " | bf b b bf b bf")
            .Bateria(Vezes(bodhranFraco, 7) + " | . . . . . .", volume: 0.45f)
            .Bateria(". . . x . .", volume: 0.8f);
        return m;
    }

    // ---------------------------------------------------------------- MAPA: "Balada do Arquipélago"
    // Uma balada calma em 3/4, em lá menor: o violão dedilhando, as ondas indo e vindo e o sino do navio de
    // vez em quando. Também em colcheias (6 por compasso), bem devagar. Primeiro o acordeão canta a
    // melodia; depois o apito, com o violino fazendo a segunda voz.
    static Partitura Mapa()
    {
        var m = new Partitura(bpm: 50f, passosPorCompasso: 6, tonica: 9, escala: Menor) // lá menor, 3/4
        {
            eco = 0.2f, sala = 0.22f, bombear = 0f,
        };
        const string melodia =
            "e5 - - a5 - b5 | c6 - - b5 a5 - | g5 - - e5 - g5 | d5 - - - - - | " +
            "e5 - - a5 - b5 | c6 - - d6 - e6 | b5 - - g#5 - e5 | b5 - - - - - | " +
            "a5 - c6 f6 - e6 | d6 - - b5 - g5 | e6 - - d6 - c6 | a5 - - - - - | " +
            "f5 - a5 d6 - c6 | b5 - - g#5 - e5 | a5 - - c6 - b5 | a5 - - - - -";
        const string acordes = "Am F C G Am F E E F G C Am Dm E Am Am";
        string ondas = Vezes("a4 - - - - - - - - - - -", 8); // uma onda a cada dois compassos
        const string sino = "g . . . . . | . . . . . . | . . . . . . | . . . . . .";

        m.Parte(16, acordes)
            .Notas(Timbre.Acordeao, Bus.Eco, 0.75f, melodia, transpor: -12)
            .Notas(Timbre.Mar, Bus.Sala, 1f, ondas)
            .DosAcordes(Timbre.Violao, Bus.Seco, 0.8f, 3, "1 5 8 10 8 5")
            .DosAcordes(Timbre.Baixo, Bus.Seco, 0.7f, 2, "1 - - 5 - -")
            .Bateria(sino, volume: 0.5f)
            .Bateria("b . . . . .", volume: 0.3f);

        m.Parte(16, acordes)
            .Notas(Timbre.Apito, Bus.Eco, 0.85f, melodia)
            .Harmonia(Timbre.Violino, Bus.Sala, 0.5f, melodia)
            .Acordes(Timbre.Acordeao, Bus.Sala, 0.22f, 3)
            .Notas(Timbre.Mar, Bus.Sala, 1f, ondas)
            .DosAcordes(Timbre.Violao, Bus.Seco, 0.8f, 3, "1 5 8 10 8 5")
            .DosAcordes(Timbre.Baixo, Bus.Seco, 0.7f, 2, "1 - - 5 - -")
            .Bateria(sino, volume: 0.5f)
            .Bateria("b . . . . .", volume: 0.3f);
        return m;
    }

    // ==================================================================================================
    //  A PARTITURA: guarda as notas de todas as partes, e depois sintetiza tudo
    // ==================================================================================================

    enum Timbre { Quadrada, QuadradaFina, Baixo, Pizzicato, Cravo, Violino, Pad, Coro, Sino, Canto, Estalo, Acordeao, Apito, Violao, Mar }
    enum Bus { Seco, Sala, Eco } // Seco: sem efeito. Sala: com reverb. Eco: com eco E reverb.
    enum Tambor { Bumbo, Caixa, CaixaForte, Chimbal, ChimbalAberto, Prato, TomAgudo, TomGrave, Timpano, Palmas, Tambor, Pisada, SinoDoNavio }

    class Nota
    {
        public Timbre timbre;
        public Bus bus;
        public int passo, passos;   // começo e duração, em passos
        public int midi, midiFinal; // midiFinal >= 0: a nota desliza até ela
        public float volume;
    }

    class Batida
    {
        public Tambor tambor;
        public int passo;
        public float volume;
    }

    class Acorde
    {
        public int passo, passos;
        public int raiz;         // classe da nota (0 = dó ... 11 = si)
        public int[] intervalos; // em semitons, a partir da raiz
    }

    class Partitura
    {
        public readonly float bpm;
        public readonly int passosPorCompasso;
        public readonly int tonica;   // classe da nota da tônica (para a segunda voz)
        public readonly int[] escala;
        public float eco = 0.2f;      // quanto do eco entra na mistura
        public float sala = 0.25f;    // quanto do reverb entra na mistura
        public float bombear;         // 0 a 1: o quanto os acordes "abaixam" a cada bumbo (o "pump")
        public int transporTudo;      // semitons: a música inteira (menos a bateria) mais aguda ou mais grave

        public readonly List<Nota> notas = new List<Nota>();
        public readonly List<Batida> batidas = new List<Batida>();
        int compassos; // quantos compassos já foram escritos

        public Partitura(float bpm, int passosPorCompasso, int tonica, int[] escala)
        {
            this.bpm = bpm;
            this.passosPorCompasso = passosPorCompasso;
            this.tonica = tonica;
            this.escala = escala;
        }

        public int TotalDePassos => compassos * passosPorCompasso;

        // Começa uma parte nova (de "quantos" compassos), logo depois da anterior.
        public Parte Parte(int quantos, string acordes, int transpor = 0)
        {
            var parte = new Parte(this, compassos * passosPorCompasso, quantos * passosPorCompasso, acordes, transpor);
            compassos += quantos;
            return parte;
        }

        public float[] Sintetizar() => Sintetizador.Tocar(this);
    }

    class Parte
    {
        readonly Partitura partitura;
        readonly int inicio, passos, transpor;
        readonly List<Acorde> acordes;

        public Parte(Partitura partitura, int inicio, int passos, string textoDosAcordes, int transpor)
        {
            this.partitura = partitura;
            this.inicio = inicio;
            this.passos = passos;
            this.transpor = transpor;
            acordes = LerAcordes(textoDosAcordes, partitura.passosPorCompasso, transpor);
            int cobertos = 0;
            foreach (Acorde a in acordes) cobertos += a.passos;
            if (cobertos != passos) throw new ArgumentException($"Os acordes \"{textoDosAcordes}\" cobrem {cobertos} passos, mas a parte tem {passos}.");
        }

        // Notas escritas (melodia, riff...). O texto precisa ter EXATAMENTE o tamanho da parte.
        public Parte Notas(Timbre timbre, Bus bus, float volume, string texto, int transpor = 0)
        {
            foreach (Nota n in LerNotas(texto, passos, transpor + this.transpor))
            {
                n.timbre = timbre;
                n.bus = bus;
                n.volume = volume;
                n.passo += inicio;
                partitura.notas.Add(n);
            }
            return this;
        }

        // A mesma melodia uma TERÇA abaixo (andando na escala da música): a "segunda voz".
        public Parte Harmonia(Timbre timbre, Bus bus, float volume, string texto)
        {
            foreach (Nota n in LerNotas(texto, passos, 0))
            {
                n.midi = TercaAbaixo(n.midi, partitura.tonica, partitura.escala) + transpor;
                if (n.midiFinal >= 0) n.midiFinal = TercaAbaixo(n.midiFinal, partitura.tonica, partitura.escala) + transpor;
                n.timbre = timbre;
                n.bus = bus;
                n.volume = volume;
                n.passo += inicio;
                partitura.notas.Add(n);
            }
            return this;
        }

        // Notas tiradas dos acordes: o padrão (de graus: 1, 3, 5, 8...) se repete até o fim da parte.
        // "oitava" é a oitava da fundamental (2 = o mi do baixo, por exemplo, é o mi 2).
        public Parte DosAcordes(Timbre timbre, Bus bus, float volume, int oitava, string padrao)
        {
            string[] passosDoPadrao = Palavras(padrao);
            var tocando = new List<Nota>(); // as notas do passo anterior (para o "-" segurar)
            for (int p = 0; p < passos; p++)
            {
                string palavra = passosDoPadrao[p % passosDoPadrao.Length];
                Acorde acorde = AcordeEm(p);
                bool mudouDeAcorde = p > 0 && AcordeEm(p - 1) != acorde;
                if (palavra == "-" && !mudouDeAcorde)
                {
                    foreach (Nota n in tocando) n.passos++;
                    continue;
                }
                tocando.Clear();
                if (palavra == ".") continue;
                if (palavra == "-") palavra = "1"; // o acorde mudou no meio de um "segura": toca a fundamental do novo
                int raiz = (oitava + 1) * 12 + acorde.raiz;
                foreach (int midi in NotasDoGrau(palavra, raiz, acorde.intervalos))
                {
                    var n = new Nota { timbre = timbre, bus = bus, volume = volume, passo = inicio + p, passos = 1, midi = midi, midiFinal = -1 };
                    tocando.Add(n);
                    partitura.notas.Add(n);
                }
            }
            return this;
        }

        // Os acordes inteiros, segurados do começo ao fim (cordas, coro...).
        public Parte Acordes(Timbre timbre, Bus bus, float volume, int oitava)
        {
            foreach (Acorde a in acordes)
            {
                int raiz = (oitava + 1) * 12 + a.raiz;
                foreach (int intervalo in a.intervalos)
                    partitura.notas.Add(new Nota { timbre = timbre, bus = bus, volume = volume, passo = inicio + a.passo, passos = a.passos, midi = raiz + intervalo, midiFinal = -1 });
            }
            return this;
        }

        // Bateria: o padrão se repete até o fim da parte. prato: um prato no primeiro tempo da parte.
        public Parte Bateria(string padrao, bool prato = false, float volume = 1f)
        {
            string[] passosDoPadrao = Palavras(padrao);
            for (int p = 0; p < passos; p++)
            {
                string palavra = passosDoPadrao[p % passosDoPadrao.Length];
                if (palavra == ".") continue;
                foreach (char letra in palavra)
                {
                    Tambor? tambor = letra switch
                    {
                        'k' => Tambor.Bumbo, 's' => Tambor.Caixa, 'S' => Tambor.CaixaForte, 'h' => Tambor.Chimbal,
                        'o' => Tambor.ChimbalAberto, 'c' => Tambor.Prato, 't' => Tambor.TomAgudo, 'm' => Tambor.TomGrave,
                        'p' => Tambor.Timpano, 'x' => Tambor.Palmas, 'b' => Tambor.Tambor, 'f' => Tambor.Pisada,
                        'g' => Tambor.SinoDoNavio, _ => (Tambor?)null,
                    };
                    if (tambor == null) throw new ArgumentException($"Letra de bateria desconhecida: '{letra}' em \"{palavra}\".");
                    partitura.batidas.Add(new Batida { tambor = tambor.Value, passo = inicio + p, volume = volume });
                }
            }
            if (prato) partitura.batidas.Add(new Batida { tambor = Tambor.Prato, passo = inicio, volume = volume });
            return this;
        }

        Acorde AcordeEm(int passo)
        {
            foreach (Acorde a in acordes)
                if (passo >= a.passo && passo < a.passo + a.passos) return a;
            return acordes[acordes.Count - 1];
        }
    }

    // ---------------------------------------------------------------- leitura dos textos

    static string[] Palavras(string texto)
    {
        var palavras = new List<string>();
        foreach (string p in texto.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            if (p != "|") palavras.Add(p);
        return palavras.ToArray();
    }

    static List<Nota> LerNotas(string texto, int passosEsperados, int transpor)
    {
        var notas = new List<Nota>();
        Nota atual = null;
        string[] palavras = Palavras(texto);
        if (palavras.Length != passosEsperados)
            throw new ArgumentException($"O texto tem {palavras.Length} passos, mas a parte tem {passosEsperados}: \"{texto}\"");
        for (int passo = 0; passo < palavras.Length; passo++)
        {
            string p = palavras[passo];
            if (p == "-")
            {
                if (atual != null) atual.passos++;
                continue;
            }
            atual = null;
            if (p == ".") continue;
            string[] deslize = p.Split('>');
            atual = new Nota
            {
                passo = passo,
                passos = 1,
                midi = Midi(deslize[0]) + transpor,
                midiFinal = deslize.Length > 1 ? Midi(deslize[1]) + transpor : -1,
            };
            notas.Add(atual);
        }
        return notas;
    }

    // "c#5" -> 73 (dó 4 = 60).
    static int Midi(string nome)
    {
        int[] naturais = { 9, 11, 0, 2, 4, 5, 7 }; // a b c d e f g
        char letra = char.ToLowerInvariant(nome[0]);
        if (letra < 'a' || letra > 'g') throw new ArgumentException("Nota desconhecida: " + nome);
        int classe = naturais[letra - 'a'];
        int i = 1;
        if (i < nome.Length && nome[i] == '#') { classe++; i++; }
        else if (i < nome.Length && nome[i] == 'b' && i + 1 < nome.Length && char.IsDigit(nome[i + 1])) { classe--; i++; }
        if (!int.TryParse(nome.Substring(i), out int oitava)) throw new ArgumentException("Nota sem oitava: " + nome);
        return (oitava + 1) * 12 + classe;
    }

    // "Em C Am,D B7" -> a lista de acordes, com início e duração em passos.
    static List<Acorde> LerAcordes(string texto, int passosPorCompasso, int transpor)
    {
        var lista = new List<Acorde>();
        int passo = 0;
        foreach (string compasso in Palavras(texto))
        {
            string[] nomes = compasso.Split(',');
            int cada = passosPorCompasso / nomes.Length;
            for (int i = 0; i < nomes.Length; i++)
            {
                int dur = i == nomes.Length - 1 ? passosPorCompasso - cada * i : cada;
                var acorde = LerAcorde(nomes[i]);
                acorde.raiz = Mod(acorde.raiz + transpor, 12);
                acorde.passo = passo;
                acorde.passos = dur;
                lista.Add(acorde);
                passo += dur;
            }
        }
        return lista;
    }

    static Acorde LerAcorde(string nome)
    {
        int raiz = Midi(nome.Substring(0, 1).ToLowerInvariant() + "4") % 12;
        int i = 1;
        if (i < nome.Length && nome[i] == '#') { raiz++; i++; }
        else if (i < nome.Length && nome[i] == 'b') { raiz--; i++; }
        string tipo = nome.Substring(i);
        int[] intervalos = tipo switch
        {
            "" => new[] { 0, 4, 7 },
            "m" => new[] { 0, 3, 7 },
            "7" => new[] { 0, 4, 7, 10 },
            "m7" => new[] { 0, 3, 7, 10 },
            "maj7" => new[] { 0, 4, 7, 11 },
            "dim" => new[] { 0, 3, 6 },
            "sus4" => new[] { 0, 5, 7 },
            "5" => new[] { 0, 7 },
            _ => throw new ArgumentException("Acorde desconhecido: " + nome),
        };
        return new Acorde { raiz = Mod(raiz, 12), intervalos = intervalos };
    }

    // As notas de um "grau" do acorde ("1", "5", "10", "x" = o acorde inteiro).
    static IEnumerable<int> NotasDoGrau(string grau, int raiz, int[] intervalos)
    {
        int terca = intervalos.Length > 1 ? intervalos[1] : 0;
        int quinta = intervalos.Length > 2 ? intervalos[2] : 7;
        int setima = intervalos.Length > 3 ? intervalos[3] : 12;
        switch (grau)
        {
            case "1": yield return raiz; break;
            case "3": yield return raiz + terca; break;
            case "5": yield return raiz + quinta; break;
            case "7": yield return raiz + setima; break;
            case "8": yield return raiz + 12; break;
            case "10": yield return raiz + 12 + terca; break;
            case "12": yield return raiz + 12 + quinta; break;
            case "15": yield return raiz + 24; break;
            case "x":
                foreach (int intervalo in intervalos) yield return raiz + intervalo;
                yield return raiz + 12;
                break;
            default: throw new ArgumentException("Grau desconhecido: " + grau);
        }
    }

    // A nota uma terça abaixo, andando na escala. Uma nota de fora da escala (a sensível, "si" em dó menor)
    // desce uma terça maior: a sensível vira a quinta do acorde de dominante, que é o que soa certo.
    static int TercaAbaixo(int midi, int tonica, int[] escala)
    {
        int relativa = midi - tonica;
        int oitava = (int)Math.Floor(relativa / 12.0);
        int classe = relativa - oitava * 12;
        int grau = Array.IndexOf(escala, classe);
        if (grau < 0) return midi - 4;
        int novo = grau - 2;
        int novaOitava = oitava;
        if (novo < 0) { novo += escala.Length; novaOitava--; }
        return tonica + novaOitava * 12 + escala[novo];
    }

    static int Mod(int x, int m) => ((x % m) + m) % m;

    // ==================================================================================================
    //  O SINTETIZADOR
    // ==================================================================================================

    static class Sintetizador
    {
#if TESTE_MUSICA
        public static string Relatorio;
#endif

        public static float[] Tocar(Partitura partitura)
        {
            int porPasso = (int)Math.Round(SegundosPorPasso(partitura.bpm) * Taxa);
            int total = partitura.TotalDePassos * porPasso; // um número EXATO de compassos: o loop emenda certinho
            var seco = new float[total];
            var sala = new float[total];
            var eco = new float[total];

            foreach (Nota n in partitura.notas)
            {
                float[] destino = n.bus == Bus.Seco ? seco : n.bus == Bus.Sala ? sala : eco;
                TocarNota(destino, n, porPasso, partitura.transporTudo);
            }
            var bumbos = new List<int>();
            foreach (Batida b in partitura.batidas)
            {
                int inicio = b.passo * porPasso;
                TocarTambor(seco, b.tambor, inicio, b.volume);
                if (b.tambor == Tambor.Bumbo) bumbos.Add(inicio);
            }

            // eco de colcheia pontuada (3 passos), que também vai para o reverb
            float[] repeticoes = Eco(eco, 3 * porPasso, 0.33f);
            for (int i = 0; i < total; i++) sala[i] += eco[i] + repeticoes[i] * partitura.eco * 2f;
            if (partitura.bombear > 0f) Bombear(sala, bumbos, partitura.bombear);
            float[] reverb = Reverb(sala, 0.78f);

            var musica = new float[total];
            float maximo = 0.0001f;
            for (int i = 0; i < total; i++)
            {
                float amostra = seco[i] + sala[i] + reverb[i] * partitura.sala;
                musica[i] = (float)Math.Tanh(amostra * 0.8f); // "cola" a mistura e segura os picos sem estourar
                maximo = Math.Max(maximo, Math.Abs(musica[i]));
            }
#if TESTE_MUSICA
            // (só no programa de teste, fora da Unity: os níveis de cada parte da mistura)
            double Rms(float[] x) { double s = 0; foreach (float v in x) s += v * v; return Math.Sqrt(s / x.Length); }
            float picoAntes = 0f;
            for (int i = 0; i < total; i++) picoAntes = Math.Max(picoAntes, Math.Abs(seco[i] + sala[i] + reverb[i] * partitura.sala));
            Relatorio = $"seco {Rms(seco):0.000}  sala {Rms(sala):0.000}  reverb {Rms(reverb) * partitura.sala:0.000}  pico antes do tanh {picoAntes * 0.8f:0.00}";
#endif
            for (int i = 0; i < total; i++) musica[i] *= 0.9f / maximo;
            return musica;
        }

        // ---------------------------------------------------------------- notas

        static float Frequencia(int midi) => 440f * MathF.Pow(2f, (midi - 69) / 12f);

        // Toca uma nota somando no destino. Tudo "dá a volta" no fim (o rabo de uma nota do fim cai no começo).
        static void TocarNota(float[] destino, Nota n, int porPasso, int transpor)
        {
            int inicio = n.passo * porPasso;
            int duracao = n.passos * porPasso;
            float freq = Frequencia(n.midi + transpor);
            float freqFinal = n.midiFinal >= 0 ? Frequencia(n.midiFinal + transpor) : freq;
            switch (n.timbre)
            {
                case Timbre.Quadrada: Quadrada(destino, inicio, duracao, freq, 0.2f * n.volume, 0.25f, true); break;
                case Timbre.QuadradaFina: Quadrada(destino, inicio, duracao, freq, 0.11f * n.volume, 0.125f, false); break;
                case Timbre.Baixo: Baixo(destino, inicio, duracao, freq, 0.3f * n.volume); break;
                case Timbre.Pizzicato: Pizzicato(destino, inicio, duracao, freq, 0.34f * n.volume); break;
                case Timbre.Cravo: Cravo(destino, inicio, duracao, freq, 0.17f * n.volume); break;
                case Timbre.Violino: Violino(destino, inicio, duracao, freq, 0.16f * n.volume); break;
                case Timbre.Pad: Pad(destino, inicio, duracao, freq, 0.045f * n.volume); break;
                case Timbre.Coro: Coro(destino, inicio, duracao, freq, 0.09f * n.volume); break;
                case Timbre.Sino: Sino(destino, inicio, duracao, freq, 0.16f * n.volume); break;
                case Timbre.Canto: Canto(destino, inicio, duracao, freq, freqFinal, 0.2f * n.volume); break;
                case Timbre.Estalo: Estalo(destino, inicio, duracao, freq, 0.08f * n.volume); break;
                case Timbre.Acordeao: Acordeao(destino, inicio, duracao, freq, 0.13f * n.volume); break;
                case Timbre.Apito: Apito(destino, inicio, duracao, freq, 0.16f * n.volume); break;
                case Timbre.Violao: Violao(destino, inicio, duracao, freq, 0.45f * n.volume); break;
                case Timbre.Mar: Mar(destino, inicio, duracao, 0.1f * n.volume); break;
            }
        }

        // ADSR: sobe em "ataque", cai até "sustentar" (curva exponencial com constante "queda"), segura até
        // o fim da nota e some em "soltar". t e duracao em segundos.
        static float Envelope(float t, float duracao, float ataque, float queda, float sustentar, float soltar)
        {
            float tt = Math.Min(t, duracao);
            float nivel = tt < ataque ? tt / ataque : sustentar + (1f - sustentar) * MathF.Exp(-(tt - ataque) / queda);
            if (t > duracao) nivel *= Math.Max(0f, 1f - (t - duracao) / soltar);
            return nivel;
        }

        // Correção "polyBLEP": arredonda o degrau das ondas quadrada e serra, que senão chiam (aliasing).
        static float Blep(float t, float dt)
        {
            if (t < dt) { t /= dt; return t + t - t * t - 1f; }
            if (t > 1f - dt) { t = (t - 1f) / dt; return t * t + t + t + 1f; }
            return 0f;
        }

        static float Pulso(float fase, float dt, float largura)
        {
            float v = fase < largura ? 1f : -1f;
            v += Blep(fase, dt);
            float depois = fase - largura;
            if (depois < 0f) depois += 1f;
            return v - Blep(depois, dt);
        }

        static float Serra(float fase, float dt) => 2f * fase - 1f - Blep(fase, dt);

        static float Triangulo(float fase) => 1f - 4f * Math.Abs(fase - 0.5f);

        static float Avancar(float fase, float dt)
        {
            fase += dt;
            return fase >= 1f ? fase - 1f : fase;
        }

        static float Coeficiente(float corte) => 1f - MathF.Exp(-2f * MathF.PI * corte / Taxa); // filtro de 1 polo

        static void Somar(float[] destino, int indice, float valor) => destino[Mod(indice, destino.Length)] += valor;

        // A melodia: onda quadrada (largura 25% ou 12,5%), com vibrato que entra depois do ataque.
        static void Quadrada(float[] destino, int inicio, int duracao, float freq, float volume, float largura, bool vibrato)
        {
            float soltar = 0.05f, dur = duracao / (float)Taxa;
            int fim = duracao + (int)(soltar * Taxa);
            float fase = 0f, filtro = 0f, a = Coeficiente(5200f);
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                float f = freq;
                if (vibrato) f *= 1f + 0.0045f * MathF.Sin(2f * MathF.PI * 5.6f * t) * Math.Clamp((t - 0.14f) * 5f, 0f, 1f);
                float dt = f / Taxa;
                fase = Avancar(fase, dt);
                filtro += (Pulso(fase, dt, largura) - filtro) * a;
                float env = vibrato ? Envelope(t, dur, 0.004f, 0.15f, 0.72f, soltar) : Envelope(t, dur, 0.002f, 0.09f, 0.35f, soltar);
                Somar(destino, inicio + i, filtro * env * volume);
            }
        }

        // Baixo: triângulo com um pouco de quadrada, abafado (grave e com "punch").
        static void Baixo(float[] destino, int inicio, int duracao, float freq, float volume)
        {
            float soltar = 0.03f, dur = duracao / (float)Taxa;
            int fim = duracao + (int)(soltar * Taxa);
            float fase = 0f, f1 = 0f, f2 = 0f, a = Coeficiente(1100f);
            float dt = freq / Taxa;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                fase = Avancar(fase, dt);
                float onda = Triangulo(fase) + 0.4f * Pulso(fase, dt, 0.5f);
                f1 += (onda - f1) * a;
                f2 += (f1 - f2) * a;
                Somar(destino, inicio + i, f2 * Envelope(t, dur, 0.003f, 0.2f, 0.8f, soltar) * volume);
            }
        }

        // Pizzicato (corda beliscada): curto, some rápido.
        static void Pizzicato(float[] destino, int inicio, int duracao, float freq, float volume)
        {
            int fim = Math.Min(duracao, (int)(0.6f * Taxa)) + (int)(0.02f * Taxa);
            float fase = 0f, f1 = 0f, a = Coeficiente(1500f);
            float dt = freq / Taxa;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                fase = Avancar(fase, dt);
                f1 += (Triangulo(fase) * 0.8f + Serra(fase, dt) * 0.3f - f1) * a;
                float env = Math.Min(1f, t / 0.002f) * MathF.Exp(-t / 0.14f) * (i < duracao ? 1f : 1f - (i - duracao) / (0.02f * Taxa));
                Somar(destino, inicio + i, f1 * env * volume);
            }
        }

        // Cravo: duas quadradas levemente desafinadas + uma serra uma oitava acima, com o "tlic" da pena.
        static void Cravo(float[] destino, int inicio, int duracao, float freq, float volume)
        {
            float queda = 0.45f * MathF.Pow(440f / freq, 0.35f); // as agudas somem antes
            int fim = Math.Min(duracao + (int)(0.08f * Taxa), (int)(2f * Taxa));
            float fa = 0f, fb = 0f, fc = 0f, filtro = 0f, a = Coeficiente(5600f);
            uint ruido = (uint)inicio * 2654435761u + 7u;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                float da = freq * 1.0015f / Taxa, db = freq * 0.9985f / Taxa, dc = 2f * freq / Taxa;
                fa = Avancar(fa, da);
                fb = Avancar(fb, db);
                fc = Avancar(fc, dc);
                float onda = 0.5f * Pulso(fa, da, 0.25f) + 0.5f * Pulso(fb, db, 0.3f) + 0.3f * Serra(fc, dc);
                if (i < 90) onda += Ruido(ref ruido) * (1f - i / 90f) * 0.8f; // o "tlic"
                filtro += (onda - filtro) * a;
                float env = Math.Min(1f, t / 0.001f) * MathF.Exp(-t / queda);
                if (i > duracao) env *= Math.Max(0f, 1f - (i - duracao) / (0.08f * Taxa));
                Somar(destino, inicio + i, filtro * env * volume);
            }
        }

        // Violino: serra com vibrato, bem filtrada, com o arco "entrando" devagar.
        static void Violino(float[] destino, int inicio, int duracao, float freq, float volume)
        {
            float soltar = 0.12f, dur = duracao / (float)Taxa;
            int fim = duracao + (int)(soltar * Taxa);
            float fase = 0f, f1 = 0f, f2 = 0f, a = Coeficiente(3200f);
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                float f = freq * (1f + 0.006f * MathF.Sin(2f * MathF.PI * 5.8f * t) * Math.Clamp((t - 0.1f) * 4f, 0f, 1f));
                float dt = f / Taxa;
                fase = Avancar(fase, dt);
                f1 += (Serra(fase, dt) - f1) * a;
                f2 += (f1 - f2) * a;
                Somar(destino, inicio + i, f2 * Envelope(t, dur, 0.045f, 0.3f, 0.85f, soltar) * volume);
            }
        }

        // Cordas / sintetizador de fundo: 5 serras desafinadas entre si, abafadas, entrando devagar.
        static readonly float[] Desafino = { 1f, 1.006f, 0.994f, 1.0115f, 0.9885f };

        static void Pad(float[] destino, int inicio, int duracao, float freq, float volume)
        {
            float soltar = 0.3f, dur = duracao / (float)Taxa;
            int fim = duracao + (int)(soltar * Taxa);
            var fases = new float[Desafino.Length];
            for (int v = 0; v < fases.Length; v++) fases[v] = v * 0.23f;
            float f1 = 0f, f2 = 0f, a = Coeficiente(1500f);
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa, soma = 0f;
                for (int v = 0; v < fases.Length; v++)
                {
                    float dt = freq * Desafino[v] / Taxa;
                    fases[v] = Avancar(fases[v], dt);
                    soma += Serra(fases[v], dt);
                }
                f1 += (soma - f1) * a;
                f2 += (f1 - f2) * a;
                Somar(destino, inicio + i, f2 * Envelope(t, dur, 0.12f, 1f, 1f, soltar) * volume);
            }
        }

        // Coro ("aaah"): serras desafinadas passando por dois filtros de formante (as "vogais" da voz).
        static void Coro(float[] destino, int inicio, int duracao, float freq, float volume)
        {
            float soltar = 0.45f, dur = duracao / (float)Taxa;
            int fim = duracao + (int)(soltar * Taxa);
            var fases = new float[4];
            for (int v = 0; v < fases.Length; v++) fases[v] = v * 0.31f;
            var vogal1 = new Formante(760f, 6f);
            var vogal2 = new Formante(1180f, 7f);
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa, soma = 0f;
                float vibrato = 1f + 0.004f * MathF.Sin(2f * MathF.PI * 5f * t + freq);
                for (int v = 0; v < fases.Length; v++)
                {
                    float dt = freq * Desafino[v] * vibrato / Taxa;
                    fases[v] = Avancar(fases[v], dt);
                    soma += Serra(fases[v], dt);
                }
                float voz = vogal1.Filtrar(soma) + 0.7f * vogal2.Filtrar(soma);
                Somar(destino, inicio + i, voz * Envelope(t, dur, 0.25f, 1f, 1f, soltar) * volume);
            }
        }

        // Filtro passa-banda (de variáveis de estado): deixa passar só em volta de "centro".
        class Formante
        {
            readonly float f, q;
            float baixo, banda;
            public Formante(float centro, float ressonancia)
            {
                f = 2f * MathF.Sin(MathF.PI * centro / Taxa);
                q = 1f / ressonancia;
            }
            public float Filtrar(float x)
            {
                baixo += f * banda;
                float alto = x - baixo - q * banda;
                banda += f * alto;
                return banda;
            }
        }

        // Caixinha de música: senoide com dois harmônicos "de sino" (desafinados) que somem devagar.
        static void Sino(float[] destino, int inicio, int duracao, float freq, float volume)
        {
            int fim = Math.Min(duracao + (int)(0.9f * Taxa), (int)(2.5f * Taxa));
            double w1 = 2.0 * Math.PI * freq / Taxa, w2 = w1 * 3.01, w3 = w1 * 5.43;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                float onda = (float)(Math.Sin(w1 * i) + 0.3 * Math.Sin(w2 * i) * Math.Exp(-t / 0.2) + 0.12 * Math.Sin(w3 * i) * Math.Exp(-t / 0.08));
                float env = Math.Min(1f, t / 0.0015f) * MathF.Exp(-t / 0.7f);
                Somar(destino, inicio + i, onda * env * volume);
            }
        }

        // O canto da baleia: uma senoide que DESLIZA de uma nota até a outra, com vibrato lento.
        static void Canto(float[] destino, int inicio, int duracao, float freq, float freqFinal, float volume)
        {
            float soltar = 0.5f, dur = duracao / (float)Taxa;
            int fim = duracao + (int)(soltar * Taxa);
            double fase = 0.0;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                float p = Math.Clamp(t / dur, 0f, 1f);
                float deslize = p * p * (3f - 2f * p); // devagar no começo e no fim
                float f = freq * MathF.Pow(freqFinal / freq, deslize);
                f *= 1f + 0.008f * MathF.Sin(2f * MathF.PI * 4.2f * t) * Math.Clamp((t - 0.3f) * 2f, 0f, 1f);
                fase += f / Taxa;
                float onda = (float)(Math.Sin(2.0 * Math.PI * fase) + 0.18 * Math.Sin(4.0 * Math.PI * fase));
                Somar(destino, inicio + i, onda * Envelope(t, dur, 0.25f, 1f, 1f, soltar) * volume);
            }
        }

        // Estalo de orquestra: serras abertas que fecham rápido (o filtro desce), com um sopro de ruído.
        static void Estalo(float[] destino, int inicio, int duracao, float freq, float volume)
        {
            int fim = Math.Min(duracao, (int)(1.2f * Taxa)) + (int)(0.05f * Taxa);
            var fases = new float[3];
            float f1 = 0f;
            uint ruido = (uint)inicio * 747796405u + (uint)freq;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa, soma = 0f;
                for (int v = 0; v < fases.Length; v++)
                {
                    float dt = freq * Desafino[v] / Taxa;
                    fases[v] = Avancar(fases[v], dt);
                    soma += Serra(fases[v], dt);
                }
                soma += Ruido(ref ruido) * 0.6f * MathF.Exp(-t / 0.04f);
                float corte = 700f + 5000f * MathF.Exp(-t / 0.09f);
                f1 += (soma - f1) * Coeficiente(corte);
                float env = Math.Min(1f, t / 0.003f) * MathF.Exp(-t / 0.35f);
                if (i > duracao) env *= Math.Max(0f, 1f - (i - duracao) / (0.05f * Taxa));
                Somar(destino, inicio + i, f1 * env * volume);
            }
        }


        // Acordeão: três palhetas por nota, duas levemente desafinadas para cima e para baixo (o "tremido"
        // de acordeão de taverna), com o fole entrando rápido e segurando.
        static void Acordeao(float[] destino, int inicio, int duracao, float freq, float volume)
        {
            float soltar = 0.06f, dur = duracao / (float)Taxa;
            int fim = duracao + (int)(soltar * Taxa);
            float fa = 0f, fb = 0.37f, fc = 0.71f, f1 = 0f, f2 = 0f, a = Coeficiente(3400f);
            float da = freq / Taxa, db = freq * 1.0068f / Taxa, dc = freq * 0.9935f / Taxa;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                fa = Avancar(fa, da);
                fb = Avancar(fb, db);
                fc = Avancar(fc, dc);
                float onda = Pulso(fa, da, 0.4f) + 0.7f * Pulso(fb, db, 0.42f) + 0.7f * Pulso(fc, dc, 0.38f) + 0.35f * Serra(fa, da);
                f1 += (onda - f1) * a;
                f2 += (f1 - f2) * a;
                float fole = 1f + 0.06f * MathF.Sin(2f * MathF.PI * 0.9f * t); // o fole respirando
                Somar(destino, inicio + i, (0.6f * f1 + 0.4f * f2) * Envelope(t, dur, 0.018f, 0.2f, 0.82f, soltar) * fole * volume);
            }
        }

        // Apito (flautinha de lata): quase uma senoide, com um sopro de ar no começo e vibrato que entra depois.
        static void Apito(float[] destino, int inicio, int duracao, float freq, float volume)
        {
            float soltar = 0.05f, dur = duracao / (float)Taxa;
            int fim = duracao + (int)(soltar * Taxa);
            double fase = 0.0;
            var sopro = new Formante(Math.Min(freq * 2f, 4000f), 5f);
            uint ruido = (uint)inicio * 1664525u + 1013904223u;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                float f = freq * (1f - 0.02f * MathF.Exp(-t / 0.015f)); // a nota "encaixa" vindo de baixo
                f *= 1f + 0.005f * MathF.Sin(2f * MathF.PI * 5.4f * t) * Math.Clamp((t - 0.18f) * 4f, 0f, 1f);
                fase += f / Taxa;
                float onda = (float)(Math.Sin(2.0 * Math.PI * fase) + 0.12 * Math.Sin(4.0 * Math.PI * fase) + 0.05 * Math.Sin(6.0 * Math.PI * fase));
                float ar = sopro.Filtrar(Ruido(ref ruido)) * (0.12f + 0.5f * MathF.Exp(-t / 0.03f));
                Somar(destino, inicio + i, (onda + ar) * Envelope(t, dur, 0.012f, 0.25f, 0.88f, soltar) * volume);
            }
        }

        // Violão: corda beliscada pelo método de Karplus-Strong. Um ruído curtinho entra numa "linha" do
        // tamanho de uma volta da onda, e cada volta passa por uma média de duas amostras (que vai abafando
        // os agudos, como numa corda de verdade). O que sai é a própria corda vibrando.
        static void Violao(float[] destino, int inicio, int duracao, float freq, float volume)
        {
            int fim = Math.Min(duracao + (int)(0.05f * Taxa), (int)(2.5f * Taxa));
            float periodo = Taxa / freq - 0.5f; // a média de duas amostras atrasa meia amostra
            float perda = MathF.Exp(-1f / (freq * 0.55f * MathF.Pow(220f / freq, 0.3f))); // as agudas param antes
            int excita = (int)(Taxa / freq);
            var corda = new float[fim];
            uint ruido = (uint)inicio * 22695477u + (uint)freq * 31u + 5u;
            float palheta = 0f, a = Coeficiente(Math.Min(5000f, freq * 9f));
            for (int i = 0; i < fim; i++)
            {
                float entrada = 0f;
                if (i < excita)
                {
                    palheta += (Ruido(ref ruido) - palheta) * a;
                    entrada = palheta;
                }
                float atras = i - periodo, volta = 0f;
                if (atras >= 1f)
                {
                    int k = (int)atras;
                    float fr = atras - k;
                    float agora = corda[k] + (corda[k + 1] - corda[k]) * fr;
                    float antes = corda[k - 1] + (corda[k] - corda[k - 1]) * fr;
                    volta = 0.5f * (agora + antes) * perda;
                }
                corda[i] = entrada + volta;
                float abafa = i > duracao ? Math.Max(0f, 1f - (i - duracao) / (0.05f * Taxa)) : 1f; // a mão abafa a corda
                Somar(destino, inicio + i, corda[i] * abafa * volume);
            }
        }

        // O mar: ruído filtrado que cresce e some (uma onda chegando na areia). A nota escrita não importa,
        // só a duração: cada "nota" é uma onda.
        static void Mar(float[] destino, int inicio, int duracao, float volume)
        {
            float dur = duracao / (float)Taxa;
            uint ruido = (uint)inicio * 2891336453u + 11u;
            float f1 = 0f, f2 = 0f;
            for (int i = 0; i < duracao; i++)
            {
                float p = i / (float)Taxa / dur;
                float onda = p < 0.55f ? MathF.Sin(p / 0.55f * MathF.PI * 0.5f) : MathF.Cos((p - 0.55f) / 0.45f * MathF.PI * 0.5f);
                onda *= onda;
                float a = Coeficiente(250f + 1800f * onda);
                f1 += (Ruido(ref ruido) - f1) * a;
                f2 += (f1 - f2) * a;
                Somar(destino, inicio + i, f2 * onda * volume * 4f);
            }
        }
        // ---------------------------------------------------------------- bateria

        static float Ruido(ref uint estado)
        {
            estado ^= estado << 13;
            estado ^= estado >> 17;
            estado ^= estado << 5;
            return estado / (float)uint.MaxValue * 2f - 1f;
        }

        static void TocarTambor(float[] destino, Tambor tambor, int inicio, float volume)
        {
            uint ruido = (uint)inicio * 2246822519u + (uint)tambor * 3266489917u + 1u;
            switch (tambor)
            {
                case Tambor.Bumbo: Bumbo(destino, inicio, 0.62f * volume, ref ruido); break;
                case Tambor.Caixa: Caixa(destino, inicio, 0.3f * volume, ref ruido); break;
                case Tambor.CaixaForte: Caixa(destino, inicio, 0.42f * volume, ref ruido); break;
                case Tambor.Chimbal: Chimbal(destino, inicio, 0.075f * volume, 0.035f, ref ruido); break;
                case Tambor.ChimbalAberto: Chimbal(destino, inicio, 0.07f * volume, 0.22f, ref ruido); break;
                case Tambor.Prato: Chimbal(destino, inicio, 0.16f * volume, 1.4f, ref ruido); break;
                case Tambor.TomAgudo: Tom(destino, inicio, 0.38f * volume, 240f, 130f); break;
                case Tambor.TomGrave: Tom(destino, inicio, 0.42f * volume, 150f, 82f); break;
                case Tambor.Timpano: Timpano(destino, inicio, 0.55f * volume, ref ruido); break;
                case Tambor.Palmas: Palmas(destino, inicio, 0.28f * volume, ref ruido); break;
                case Tambor.Tambor: TamborDeMao(destino, inicio, 0.42f * volume, ref ruido); break;
                case Tambor.Pisada: Pisada(destino, inicio, 0.3f * volume, ref ruido); break;
                case Tambor.SinoDoNavio: SinoDoNavio(destino, inicio, 0.17f * volume); break;
            }
        }

        // Bumbo: senoide que despenca de 160 Hz para 46 Hz, com um "toc" de ruído no começo.
        static void Bumbo(float[] destino, int inicio, float volume, ref uint ruido)
        {
            int fim = (int)(0.38f * Taxa);
            double fase = 0.0;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                fase += (46f + 114f * MathF.Exp(-t / 0.035f)) / Taxa;
                float corpo = MathF.Sin(2f * MathF.PI * (float)fase) * MathF.Exp(-t / 0.14f);
                float toc = i < 60 ? Ruido(ref ruido) * (1f - i / 60f) * 0.5f : 0f;
                Somar(destino, inicio + i, (corpo + toc) * volume);
            }
        }

        // Caixa: ruído (a esteira) + um tom de 185 Hz (o corpo).
        static void Caixa(float[] destino, int inicio, float volume, ref uint ruido)
        {
            int fim = (int)(0.22f * Taxa);
            float anterior = 0f;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                float r = Ruido(ref ruido);
                float esteira = r - anterior * 0.6f; // tira um pouco do grave do ruído
                anterior = r;
                float corpo = MathF.Sin(2f * MathF.PI * 185f * t) * MathF.Exp(-t / 0.05f);
                Somar(destino, inicio + i, (esteira * 0.7f * MathF.Exp(-t / 0.065f) + corpo * 0.6f) * volume);
            }
        }

        // Chimbal (e prato, mais longo): só o agudo do ruído.
        static void Chimbal(float[] destino, int inicio, float volume, float queda, ref uint ruido)
        {
            int fim = (int)(Math.Min(queda * 5f, 3f) * Taxa);
            float anterior = 0f;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                float r = Ruido(ref ruido);
                float agudo = r - anterior;
                anterior = r;
                Somar(destino, inicio + i, agudo * MathF.Exp(-t / queda) * volume);
            }
        }

        static void Tom(float[] destino, int inicio, float volume, float de, float ate)
        {
            int fim = (int)(0.3f * Taxa);
            double fase = 0.0;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                fase += (ate + (de - ate) * MathF.Exp(-t / 0.05f)) / Taxa;
                Somar(destino, inicio + i, MathF.Sin(2f * MathF.PI * (float)fase) * MathF.Exp(-t / 0.12f) * volume);
            }
        }

        // Tímpano: grave, longo e com a "pele" vibrando (um pouco de ruído grave no começo).
        static void Timpano(float[] destino, int inicio, float volume, ref uint ruido)
        {
            int fim = (int)(1.4f * Taxa);
            double fase = 0.0;
            float pele = 0f;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                fase += (73f + 20f * MathF.Exp(-t / 0.06f)) / Taxa;
                pele += (Ruido(ref ruido) - pele) * 0.05f;
                float onda = MathF.Sin(2f * MathF.PI * (float)fase) + 0.4f * MathF.Sin(2f * MathF.PI * (float)fase * 1.5f) * MathF.Exp(-t / 0.2f);
                Somar(destino, inicio + i, (onda * MathF.Exp(-t / 0.45f) + pele * 3f * MathF.Exp(-t / 0.08f)) * volume);
            }
        }

        // Palmas: três estalos de ruído bem juntinhos e um rabinho.
        static void Palmas(float[] destino, int inicio, float volume, ref uint ruido)
        {
            int fim = (int)(0.2f * Taxa);
            var banda = new Formante(1300f, 2f);
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                float env = 0f;
                for (int k = 0; k < 3; k++)
                {
                    float tk = t - k * 0.011f;
                    if (tk >= 0f) env = Math.Max(env, MathF.Exp(-tk / (k == 2 ? 0.06f : 0.008f)));
                }
                Somar(destino, inicio + i, banda.Filtrar(Ruido(ref ruido)) * env * volume * 3f);
            }
        }


        // Tambor de mão irlandês (bodhrán): pele grande, um "bum" curto que desce de tom e o tapa da baqueta.
        static void TamborDeMao(float[] destino, int inicio, float volume, ref uint ruido)
        {
            int fim = (int)(0.3f * Taxa);
            double fase = 0.0;
            float tapa = 0f;
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                fase += (92f + 75f * MathF.Exp(-t / 0.02f)) / Taxa;
                float corpo = MathF.Sin(2f * MathF.PI * (float)fase) * MathF.Exp(-t / 0.1f)
                            + 0.35f * MathF.Sin(2f * MathF.PI * (float)fase * 2.3f) * MathF.Exp(-t / 0.035f);
                tapa += (Ruido(ref ruido) - tapa) * 0.3f;
                Somar(destino, inicio + i, (corpo + tapa * 1.6f * MathF.Exp(-t / 0.012f)) * volume);
            }
        }

        // Pisada no convés: um "tum" grave e o estalo da madeira.
        static void Pisada(float[] destino, int inicio, float volume, ref uint ruido)
        {
            int fim = (int)(0.25f * Taxa);
            double fase = 0.0;
            var madeira = new Formante(430f, 4f);
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa;
                fase += (60f + 70f * MathF.Exp(-t / 0.015f)) / Taxa;
                float corpo = MathF.Sin(2f * MathF.PI * (float)fase) * MathF.Exp(-t / 0.08f);
                float estalo = madeira.Filtrar(Ruido(ref ruido)) * 2.2f * MathF.Exp(-t / 0.03f);
                Somar(destino, inicio + i, (corpo + estalo) * volume);
            }
        }

        // Sino do navio: "dim!", com harmônicos de sino (fora da série harmônica) que somem em tempos diferentes.
        static readonly float[] ParciaisDoSino = { 1f, 2.32f, 3.92f, 5.61f };
        static readonly float[] ForcaDoSino = { 1f, 0.55f, 0.32f, 0.18f };
        static readonly float[] QuedaDoSino = { 1.1f, 0.55f, 0.25f, 0.12f };

        static void SinoDoNavio(float[] destino, int inicio, float volume)
        {
            int fim = (int)(2.2f * Taxa);
            for (int i = 0; i < fim; i++)
            {
                float t = i / (float)Taxa, soma = 0f;
                for (int p = 0; p < ParciaisDoSino.Length; p++)
                    soma += ForcaDoSino[p] * MathF.Sin(2f * MathF.PI * 880f * ParciaisDoSino[p] * t) * MathF.Exp(-t / QuedaDoSino[p]);
                Somar(destino, inicio + i, soma * Math.Min(1f, t / 0.001f) * volume);
            }
        }
        // ---------------------------------------------------------------- efeitos

        // Eco com realimentação. Passa DUAS vezes pela música e guarda a segunda: as repetições do fim
        // caem no começo, e o loop emenda sem "pulo". Devolve só as repetições.
        static float[] Eco(float[] entrada, int atraso, float retorno)
        {
            int n = entrada.Length;
            var linha = new float[atraso];
            var saida = new float[n];
            float abafa = 0f;
            for (int i = 0; i < 2 * n; i++)
            {
                int k = i % atraso;
                float repeticao = linha[k];
                abafa += (repeticao - abafa) * 0.5f; // cada repetição mais escura
                linha[k] = entrada[i % n] + abafa * retorno;
                if (i >= n) saida[i - n] = repeticao;
            }
            return saida;
        }

        // Reverb de sala: 4 ecos curtinhos em paralelo (cada um abafando os agudos) e 2 "espalhadores" em série.
        // Também passa duas vezes pela música, para o rabo do fim cair no começo.
        static float[] Reverb(float[] entrada, float realimenta)
        {
            int n = entrada.Length;
            var saida = new float[n];
            foreach (int atraso in new[] { 1201, 1307, 1423, 1531 })
            {
                var linha = new float[atraso];
                float abafa = 0f;
                for (int i = 0; i < 2 * n; i++)
                {
                    int k = i % atraso;
                    float atrasado = linha[k];
                    abafa += (atrasado - abafa) * 0.55f;
                    linha[k] = entrada[i % n] + abafa * realimenta;
                    if (i >= n) saida[i - n] += atrasado * 0.25f;
                }
            }
            foreach (int atraso in new[] { 347, 113 })
            {
                var linha = new float[atraso];
                var espalhado = new float[n];
                for (int i = 0; i < 2 * n; i++)
                {
                    int k = i % atraso;
                    float atrasado = linha[k];
                    float x = saida[i % n];
                    float y = atrasado - 0.6f * x;
                    linha[k] = x + 0.6f * y;
                    if (i >= n) espalhado[i - n] = y;
                }
                saida = espalhado;
            }
            return saida;
        }

        // "Pump": a cada bumbo, o que está na sala abaixa e volta (deixa o bumbo respirar e dá o balanço).
        static void Bombear(float[] bus, List<int> bumbos, float quanto)
        {
            int n = bus.Length;
            var ganho = new float[n];
            for (int i = 0; i < n; i++) ganho[i] = 1f;
            int alcance = (int)(0.3f * Taxa);
            foreach (int b in bumbos)
                for (int j = 0; j < alcance; j++)
                {
                    int k = Mod(b + j, n);
                    float g = 1f - quanto * MathF.Exp(-j / (0.09f * Taxa));
                    if (g < ganho[k]) ganho[k] = g;
                }
            for (int i = 0; i < n; i++) bus[i] *= ganho[i];
        }
    }
}
