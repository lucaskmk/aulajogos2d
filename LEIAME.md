# CILADA! — um jogo de plataforma nada confiável

Jogo 2D de plataforma "troll" (no estilo Cat Mario / Level Devil): tudo parece normal,
mas o chão cai, espinhos brotam do nada, a bandeira foge e as nuvens têm dentes.

## Como jogar
1. Abra o projeto no Unity 6 (6000.6.2f1).
2. Abra `Assets/Scenes/SampleScene` e aperte **Play**. O jogo se monta sozinho.

| Tecla | Ação |
|---|---|
| A / D ou setas | andar |
| Espaço / W / ↑ | pular (segure para pular mais alto) |
| R | reiniciar a fase (conta como morte 😈) |
| Esc | voltar ao título |

Também funciona com controle (gamepad).

## Organização do código (`Assets/Scripts`)
- **Nucleo/**
  - `GerenciadorDoJogo` — máquina de estados (Título → Jogando → Morreu → Fase concluída → Vitória), contagem de mortes/moedas/tempo, recorde (PlayerPrefs) e HUD.
  - `Fases` — **as fases são desenhadas em texto**; cada caractere é um bloco (legenda no topo do arquivo).
  - `ConstrutorDeFase` — lê o texto e cria os objetos. O chão vira um único `CompositeCollider2D`.
  - `FabricaDeSprites` — toda a arte é gerada por código (pixel art escrita em texto + desenhos por fórmula).
  - `FabricaDeSons` — efeitos sonoros 8-bit gerados por código (ondas quadradas e ruído).
  - `CameraSeguir`, `Controles` (Input System), `Efeitos`.
- **Jogador/** `Jogador` — física com Rigidbody2D, pulo variável, *coyote time* e *buffer* de pulo.
- **Armadilhas/** uma classe por pegadinha: `ChaoQueCai`, `EspinhoEscondido`, `EspinhoQueCai`,
  `BlocoInvisivel`, `BlocoSurpresa`, `Mola`, `Serra`, `Esmagador`, `NuvemAssassina`, `Espinho`, e `Perigo` (base: "encostou, morreu").
- **Objetos/** `Inimigo`, `Moeda`, `Placa`, `Bandeira`, `BandeiraFujona`, `BandeiraFalsa`.

## Fases que mudam
Da fase 5 em diante, a fase **muda depois que você morre** (estilo Level Devil): armadilhas trocam de lugar,
moedas viram moedas assassinas, a bandeira certa vira a errada... Cada fase tem uma lista `mudancas`
em `Fases.cs` com as trocas usadas em cada tentativa.

## Criando uma fase nova
Copie um bloco `new Fase { ... }` em `Fases.cs` e desenhe com os caracteres da legenda
(`#` chão, `P` jogador, `G` bandeira, `C` chão que cai, `h` espinho escondido...).
Para testar uma fase direto: durante o Play, selecione o objeto `GerenciadorDoJogo`,
mude `Fase Inicial` no Inspector (0 = primeira) e aperte **Esc**.
