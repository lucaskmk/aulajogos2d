using UnityEngine;

// As fases de FORA da campanha das ilhas: as BÔNUS (das saídas secretas) e o MUNDO 2, o Mar de Baixo.
// A legenda das letras é a do Fases.cs; as do mar estão em MarDeBaixo.cs e as novas das ilhas em ArmadilhasNovas.cs.
// (este arquivo foi montado pelo gerador de fases: dá para editar à mão; confira com Ferramentas/checar_fase.py)
//
// No Mar de Baixo o chão costuma ficar ALTO (topo na linha 8): assim os tanques de água ('~') ficam fundos,
// da linha 8 à 13, com o fundo na 14. Um tanque de 30 blocos se atravessa em uns 6 s (o ar dura 8).
public static partial class Fases
{
    public static readonly Fase[] Bonus =
    {
        // sai da fase 6 ("Abordagem!"): moedas, o canhão, o baú que morde e as pranchas
        new Fase
        {
            nome = "O Porão do Tesouro",
            bioma = Bioma.Navios,
            corDoFundo = new Color32(120, 160, 210, 255),
            placas = new[]
            {
                "Fase bônus! Aqui é só moeda. Juro.\nO baú? É de enfeite.",
                "Os canhões levam você. Pra onde? Surpresa.",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "                                                                                          T",
                "",
                "",
                "                        $$$$$                                                           $$$$$",
                "          ?K?                                                     m$m                   BBBBB",
                "                                           $   $ $ $ $ $",
                "                                                                                             BBBBB",
                "  P i   $$$     !     )                s                    i  E    )           !   $E$   /  + ; B h  G",
                "###############################      ##########__##__##__##############     ############################",
                "###############################      ##########  ##  ##  ##############     ############################",
            },
        },

        // sai da fase 9 ("Vento em Popa!"): o tsunami, a maré e o clone espelhado
        new Fase
        {
            nome = "Corrida da Maré",
            bioma = Bioma.Praia,
            corDoFundo = new Color32(150, 200, 230, 255),
            velocidadeDaOnda = 4.5f,
            placas = new[]
            {
                "Corre! A maré não espera. (o tsunami também não)",
                "Aquele ali é você? Não pare pra conversar.",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "                                                                                                    T",
                "",
                "                              ?$?            ----------------                                               $$$       ---------------",
                "              $$$                            -------$$-------           $$                                            ------$--------",
                "                                             ----------------                                                         ---------------         m",
                "  P i N         ^     ^             h        ----------------   s               |               i       E         ^^  ---------------     h       G",
                "##########################CCC############   ###########################   ############################################################################",
                "##########################CCC############   ###########################   ############################################################################",
            },
        },

        // sai da fase 12 ("O Verdadeiro Final (juro)"): todas as ciladas "ao vivo" juntas
        new Fase
        {
            nome = "O Louro Mentiroso",
            bioma = Bioma.Rochedo,
            corDoFundo = new Color32(90, 110, 170, 255),
            noite = true,
            placas = new[]
            {
                "Bem-vindo à MINHA fase. Aqui tudo é verdade. :)",
                "Última bandeira. Agora é sério. (é mesmo?)",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "                                                                                                        v  v",
                "",
                "",
                "                                      $$$",
                "",
                "",
                "  P i                         p   E         V           s   |               !           i   h   )             R   E B Z     *",
                "####################ddd   ###################ddd   ##########################ddd   ###############################################",
                "####################ddd   ###################ddd   ##########################ddd   ###############################################",
            },
        },
    };

    public static readonly Fase[] MarDeBaixo =
    {
        // aprender a nadar e a respirar; a bolha falsa em cima e a de verdade no fundo
        new Fase
        {
            nome = "Mergulho de Cabeça",
            bioma = Bioma.Recife,
            corDoFundo = new Color32(40, 90, 150, 255),
            clima = Ambiente.Tipo.Bolhas,
            placas = new[]
            {
                "Na água, PULAR é braçada. E olhe as bolhinhas: é o seu AR.",
                "Bolha de ar enche o ar. Toda bolha? Toda bolha.",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "                                                                                      $$",
                "  P i                                 s  i                                      E       h   x   ^  G",
                "###############~~~~~~~~~~~~~~~~~~~~##########~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~#############################",
                "###############~~~~~~~~~~~~~~~~~~~~##########~~~~~~~~%~~~~~~~~~~~~~~~~~~~~~#############################",
                "###############~~~~~~~~~~~~~~$$~~~~##########~~~~~~~~~~~~~~~~~~~~~$$$~~~~~~#############################",
                "###############~~~~~$$~~~~~~~~~~~~~##########~~~~~$$$~~~~~~~~~~~~~~~~~z~~~~#############################",
                "###############~~~~~~~~~~@~~~~~~~~~##########~~~~~~~~~~~~~~~@~~~~~~~~~##~~~#############################",
                "###############~~~~~~~~~~~~~~~~~~~~##########~~~~~~~~~~~~~~~~~~~~~~~~~##~~~#############################",
                "########################################################################################################",
            },
        },

        // tanques longos (o ar conta); as águas-vivas iguais que quicam ou queimam
        new Fase
        {
            nome = "Bolha que Mente",
            bioma = Bioma.Recife,
            corDoFundo = new Color32(30, 70, 130, 255),
            clima = Ambiente.Tipo.Bolhas,
            placas = new[]
            {
                "As águas-vivas quicam! (quase todas)",
                "Prenda a respiração. E conte as bolhas.",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "  P i                                                     s               i                                                       x     G",
                "###########~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~##########     ###########~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~###############",
                "###########~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~##########     ###########~~~~~~~~~~~~~%~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~###############",
                "###########~~~~~~~~~~~~~&~~~~~~~~~~~~~~~~~$$$~~~~~~~~~~##########     ###########~~~~~~~~~~~~~~~~~~~~~~~~~&~~~~~~~~~~~$$~~~~~###############",
                "###########~~~~~~~~~~~~~~~~~~~~~~~~~@~~~~~~~~~~~~~~~~~~##########  z  ###########~~~~~~~@~~~~~~~~~~~~~~~~~~~~~~~@~~~~~~~~~~~~###############",
                "###########~~~~~~~@~~~~~~~~~~~%~~~~~~~~~~~~~~~~~&~~~~~~##########     ###########~~~~~~~~~~~~~~~~~~~@~~~~~~~~~~~~~~~~~~~~~~~~###############",
                "###########~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~##########     ###########~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~###############",
                "##################################################################   #######################################################################",
            },
        },

        // escura: as iscas dos peixes-lanterna parecem moedas
        new Fase
        {
            nome = "O Peixe da Luz",
            bioma = Bioma.Gruta,
            corDoFundo = new Color32(10, 20, 45, 255),
            clima = Ambiente.Tipo.Bolhas,
            escura = true,
            placas = new[]
            {
                "Tá escuro. Mas olha quanta moeda brilhando!",
                "Moeda que pisca vermelho não é moeda.",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "                                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "                                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "                                        $ f $      ~~~~~~~~~~~~~~~~~$~~~~~~~~~~~~",
                "          $   f     $                              ~~~~~f~~~~~~~~~~~~~~~~~~~~~~~~         f                   $f$        f",
                "  P i                             E           s    ~~~~~~~~~~~@~~~~~~~~~f~~~~~~~~     i         h                     E      G",
                "#########################    ######################~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~####################   ##########################",
                "#########################    ########################################################################   ##########################",
            },
        },

        // o chefe: o Tubarão-Martelo (ChefesDoMar.cs), arena de 22 blocos
        new Fase
        {
            nome = "A Forja do Martelo",
            bioma = Bioma.Gruta,
            corDoFundo = new Color32(60, 40, 50, 255),
            noite = true,
            saveFiel = true,
            tempoAlvo = 150f,
            placas = new[]
            {
                "Ouviu? Tem alguém martelando lá dentro.",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "  P i     s     {                           $$$           G",
                "################################################################",
                "################################################################",
            },
        },

        // as correntezas (=) debaixo d'água empurram para a esquerda
        new Fase
        {
            nome = "Correnteza",
            bioma = Bioma.AltoMar,
            corDoFundo = new Color32(35, 80, 140, 255),
            clima = Ambiente.Tipo.Bolhas,
            tempoAlvo = 110f,
            placas = new[]
            {
                "A correnteza puxa pra trás. Os risquinhos mostram onde.",
                "Por cima ou por baixo? Uma das duas bolhas mente.",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "  P i                                                           s   i                                                         x     E    h  x    G",
                "#############~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~=============~~~~############~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~#############################",
                "#############~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~=============~~~~############~~~~~~~@~~~~~~~~~~~~~~~~~%~~~~~@~~~~~~~~~~~~~~~~#############################",
                "#############~~~~~~~~~===============~~~~~~~~~~~~~~~~~~~~~~~~############~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~#############################",
                "#############~~~~~~~~~===============~~~~~~~~~~~~~~~~~~~~~~~~############~~~===========================================~~#############################",
                "#############~~~~~~~@~~~~~~~~~~~~~~~~~~~@~~~~~~~~~$$$~~~~~~~~############~~~===========================================~~#############################",
                "#############~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~############~~~~~~~~~~~~~~~~~~~@~~~~~~~~~~~~~~~~~$$~~~~~~~~~#############################",
                "######################################################################################################################################################",
            },
        },

        // as ostras sobre os buracos, a maré, o clone, a prancha comprida e o baú que morde
        new Fase
        {
            nome = "O Jardim das Ostras",
            bioma = Bioma.Recife,
            corDoFundo = new Color32(60, 110, 160, 255),
            clima = Ambiente.Tipo.Bolhas,
            tempoAlvo = 110f,
            placas = new[]
            {
                "Ostra é chão. Por meio segundo.",
                "Essa prancha é comprida demais. Pula!",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "                                                                                                                              T",
                "                                                                            ---------------                                     $$$",
                "                                                                            ------$$-------",
                "                                                                            ---------------",
                "  P i                x   x        !        x   x        s   |               ---------------        E  i  h  x   E       !             h    x     G",
                "###################  ##  ##   ###########  ##  ##   #######################################_____######################################################",
                "###################           ###########           #######################################     ######################################################",
            },
        },

        // a chefe final do Mar: a Mãe do Mar (ChefesDoMar.cs)
        new Fase
        {
            nome = "O Trono da Mãe do Mar",
            bioma = Bioma.AltoMar,
            corDoFundo = new Color32(30, 30, 80, 255),
            noite = true,
            saveFiel = true,
            tempoAlvo = 170f,
            placas = new[]
            {
                "A Mãe do Mar. A mãe do Kraken. Boa sorte. De verdade.",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "  P i     s     }                           L             G",
                "################################################################",
                "################################################################",
            },
        },
    };
}
