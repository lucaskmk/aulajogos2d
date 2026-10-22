# Onde paramos (e o que falta)

Arquivo de passagem de bastão: dá para continuar daqui (no Claude Code ou na mão).
O README explica o jogo e o código; o GDD explica o design. Aqui fica só o estado do trabalho.

## Pronto
- **16 fases + a secreta**, todas integradas (`Fases.cs`), com o mapa do mundo (`MapaDoMundo.cs`) e as falas (`Textos.cs`).
- **Os 3 chefes no molde Helltaker** (3 etapas de 2 golpes, aviso antes de todo ataque, padrões fixos, checkpoint por etapa):
  `ChefeCapita.cs` (fase 6), `ChefeCozinheira.cs` (fase 13) e `BaleiaBranca.cs` (fase 16), com a base em `Chefe.cs`.
  O dano é **pular na cabeça no momento de vulnerabilidade** de cada um (âncora presa, clarão do lampião, atordoada depois do mergulho).
- **Recordes por fase e medalhas** (`Progresso.cs`, `Medalhas.cs`, `Interface.cs`, `MapaDoMundo.cs`) e a conquista "Perfeccionista".
- **Atalhos de teste:** F9 no mapa (libera tudo só na partida atual) e F10 (painel de gráficos, FPS e pior quadro).
- **Visual v3 (HD-2D):** blocos (`ArteDosBlocos.cs`), fundo nítido com a luz pintada, raios de sol, sombras de nuvens, vento nas plantas e a tela pixelada
  (o tilt-shift ficou desligado de fábrica; dá para ligar no F10).

## Leva do tema pirata (pronta, falta testar na Unity)
O tema de Re:Zero saiu inteiro (por causa dos direitos autorais). O jogo agora é **CILADA À VISTA!**, uma aventura de pirata:
- **Nomes e textos:** o Grumete, a Marina (sereia), o Louro (papagaio), a Dóris (fantasminha), a Capitã Âncora, a Cozinheira,
  o Kraken e a Baleia Branca; fases, falas, conquistas, mensagens de morte, créditos, README e GDD.
- **Desenhos novos:** os personagens, a âncora, os cutelos, os caranguejos e ratos, o tentáculo e os tentáculos da Maré Negra (no lugar das mãos).
- **Áudio 100% próprio:** os dois MP3 do anime foram apagados. A Volta da Maré agora é gerada por código (`SomDaMare.cs`: a onda, o sino do navio e um lamento de acordeão).
- **Trilha pirata** (`Musicas.cs`, sintetizador próprio com acordeão, apito, violão, tambor de mão, pisadas e sino):
  - "Jiga do Grumete" nas fases (6/8, ré menor, AABB), num tom por fase (`FabricaDeMusica.DaFase`);
  - "Balada do Arquipélago" no mapa;
  - as músicas dos 3 chefes reorquestradas com os instrumentos de pirata.
  - A `FabricaDeEpico.cs` (a música antiga, "abertura de anime") foi apagada.
- **Mapa do arquipélago:** as ilhas separadas por estreitos (as pontes de madeira atravessam), o mar no norte e no sul com navios,
  palmeiras, o farol, o forte da Capitã, o navio fantasma das Cabines, naufrágios, baús e a Rocha da Caveira.
  Os caminhos são os mesmos de antes (só ganharam pontes onde passam por cima do mar), então a ordem das fases e os atalhos não mudaram.
- **Fundos das fases:** ilhas com um vulcão lá longe, um galeão no horizonte, palmeiras e o mato da praia (as camadas de `Paralaxe.cs`).
  No chão das fases, barris e estrelas-do-mar no lugar das cercas e dos cogumelos.
- **Capitã Âncora:** mais difícil (avisos mais curtos, tudo mais rápido, mais ataques por rodada) e com a **tempestade de âncoras**
  (âncoras rápidas para todos os lados, que voltam pelo mesmo caminho). A janela da âncora presa continua mais longa que a original
  (3 / 2,7 / 2,4 s). As outras dificuldades dos chefes voltaram a ser as de antes.
- **Gráficos:** fundos nítidos e com detalhe (sem o desfoque), caixas de fala novas (fonte pixel, retrato, moldura, no alto da tela),
  os personagens redesenhados com três tons por material (`ArteDosPersonagens.cs`), a Capitã com rosto novo e as chefes com sombreamento,
  e a névoa do mapa mais macia.

### Para conferir na Unity
- O Console ao dar Play: `[Cilada à Vista] visual v3 (HD-2D escuro) rodando...` (sem erro vermelho).
- **Capitã:** a tempestade de âncoras (as linhas piscam antes; as âncoras cravam e voltam), se a luta ficou difícil na medida,
  a janela da âncora presa e se o pisão fora da janela não dá dano.
- **Cozinheira e Baleia:** se a luta ficou igual à de antes (dificuldade e janela de dano).
- **Música:** a jiga em cada fase (o tom muda de fase para fase), a balada no mapa, a de cada chefe na luta (continua depois de morrer e volta para a da fase depois da vitória).
  E o engasgo ao gerar a música (meio segundo), principalmente no navegador.
- **Morte:** a onda e o sino da Volta da Maré caem bem no renascimento.
- **Mapa:** os navios no mar, a luz do farol, as palmeiras ao vento, o horizonte (mar com galeões e ilhas) e a névoa abrindo pelas pontes novas.
- **Fundos das fases:** o galeão e as palmeiras no HD-2D e no Simples; se nada pontudo no fundo parece espinho.

## Ainda falta
- **Página do itch.io:** como jogar e os segredos (fase secreta: todas as moedas da fase 5 e depois seta para baixo no ponto 5).
- **Tempos-alvo das medalhas:** são estimativas. Ajustar o campo `tempoAlvo` em `Fases.cs` depois de jogar,
  e conferir se TODAS as moedas de cada fase dá para pegar (senão a medalha de moeda fica impossível).
- **Narrativa (opcional):** um prólogo curto (o Grumete cai do navio, e a maré sempre o traz de volta), um cartão de capítulo ao entrar em cada fase e um epílogo com a Marina.
- **Modo Justo (opcional, desligado por padrão):** o save nunca "muda de lugar" e a bandeira `W` não manda para o começo.
- **Sons das mecânicas:** efeitos para a âncora, o vento, as facas, a Maré Negra, o tic do ritmo e as plataformas.
- **Equipe:** pôr os nomes em `Textos.Equipe` (créditos), no README e no GDD.

## Como integrar uma mecânica ou fase nova
Cada mecânica nova é **um arquivo novo** em `Assets/Scripts/Armadilhas/` (com `.meta`). Os sprites são registrados com
`FabricaDeSprites.Registrar` num método `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`. Integrar uma fase nova é:
1. Pôr um `case` no `switch` de `ConstrutorDeFase.Construir`.
2. Acrescentar a fase no lugar certo de `Fases.Todas`, com `corDoFundo` e, se for o caso, `noite`/`escura`. Pôr também a linha da legenda no topo de `Fases.cs`.
3. Pôr o ponto dela no `MapaDoMundo` e a fala do mapa em `Textos.DepoisDaFase`, pelo nome da fase.
4. Se tiver conquista nova, pôr em `Conquistas.Todas`.
5. Testar o terreno:
   ```
   python3 Ferramentas/extrair_fases.py Assets/Scripts/Nucleo/Fases.cs /tmp/fases
   python3 Ferramentas/checar_fase.py /tmp/fases/fase_XX.json --de s
   ```
6. Para mexer numa música: editar `Musicas.cs` e ouvir com `dotnet run --project Ferramentas/ouvir_musicas -- Builds/musicas`.
