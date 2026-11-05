# A história: os textos e os prompts das imagens das cutscenes

São **9 imagens**: 7 da abertura (passam num Novo jogo, antes do mapa) e 2 do final (depois de vencer o Kraken).
As imagens de hoje foram desenhadas por código (`desenhar_historia.py`, 256 x 144). Com os prompts abaixo dá para
gerar versões pintadas, no mesmo estilo do fundo do título (`Assets/Resources/Arte/titulo_fundo.png`).

## Como usar

1. Mande para a IA de imagem **primeiro o bloco "Folha dos personagens"** e depois os quadros, **um por vez, na mesma
   conversa** (assim os personagens ficam com a mesma cara em todas as imagens). Se a IA aceitar imagens de referência,
   mande junto `titulo_fundo.png` e os retratos `retrato_grumete.png`, `retrato_louro.png`, `retrato_sereia.png`
   e `retrato_fantasma.png` (todos em `Assets/Resources/Arte/`).
2. Cada bloco é um prompt **completo**: copie o bloco inteiro.
3. **Como salvar:** com o nome que está em cima do bloco (ex.: `abertura_1.png`), por cima do arquivo que já existe em
   `Assets/Resources/Arte/historia/`. Qualquer tamanho 16:9 serve (o jogo estica para a tela inteira); o ideal é
   **1672 x 941** ou **1920 x 1080**. Não precisa mexer em código.
4. **O texto NÃO vai na imagem**: o jogo escreve a legenda numa caixa de madeira em cima da **quarta parte de baixo**
   da imagem. Por isso todo prompt pede para deixar a parte de baixo mais simples (sem rostos nem coisas importantes ali).
5. Depois de trocar as imagens, abra o Unity: ele importa sozinho. O jogo percebe que a imagem é grande (pintada) e
   usa o filtro suave; as pequenas (de código) continuam com o pixel nítido. Cuidado: rodar `desenhar_historia.py`
   de novo SOBRESCREVE as imagens pintadas com as de código.

## Os textos de cada quadro

| Imagem | Português | Inglês |
| --- | --- | --- |
| `abertura_1` | Contam os marinheiros que, no fim do mar, existe o maior tesouro de todos os tempos. | Sailors say that, at the end of the sea, lies the greatest treasure of all time. |
| `abertura_2` | Um jovem pirata, o Grumete, foi o primeiro a encontrá-lo. Ouro que não acabava mais! | A young pirate, the Deckhand, was the first to find it. Gold without end! |
| `abertura_3` | Mas o tesouro tinha um guardião: o KRAKEN VERMELHO. E o Grumete falhou. | But the treasure had a guardian: the RED KRAKEN. And the Deckhand failed. |
| `abertura_4` | Os deuses do mar o amaldiçoaram: ele NUNCA poderá morrer. Toda vez que cair, a maré o traz de volta. | The sea gods cursed him: he can NEVER die. Every time he falls, the tide brings him back. |
| `abertura_5` | A maldição quebrou o equilíbrio do mundo. Criaturas e pessoas viraram malignas... até a Capitã e a Cozinheira. | The curse broke the balance of the world. Creatures and people turned evil... even the Captain and the Cook. |
| `abertura_6` | Então o Deus da Riqueza apareceu: "Aquele tesouro é MEU. Traga-o de volta e eu tiro a sua maldição." | Then the God of Wealth appeared: "That treasure is MINE. Bring it back and I will lift your curse." |
| `abertura_7` | E assim começa uma nova jornada: atravessar o mar, derrotar o Kraken e recuperar o tesouro. | And so a new journey begins: cross the sea, defeat the Kraken and recover the treasure. |
| `final_1` | O Kraken foi derrotado. O Grumete devolveu o tesouro ao Deus da Riqueza... e a maldição se desfez. | The Kraken was defeated. The Deckhand gave the treasure back to the God of Wealth... and the curse was broken. |
| `final_2` | O mundo voltou ao equilíbrio e todo mundo voltou a ser como era. E o Grumete, enfim, pôde descansar. Por enquanto. | The world found its balance again and everyone went back to how they were. And the Deckhand could finally rest. For now. |

(Os textos ficam em `Assets/Scripts/Nucleo/Historia.cs`; as traduções, em `Traducoes.cs`.)

## Folha dos personagens (mande primeiro)

```
16-bit pixel art character reference sheet for a cute pirate adventure 2D platformer game, crisp clean pixels, limited palette, soft painted lighting from the upper left, chibi proportions (big head, small body), dark navy background. No text, no letters, no logos, no UI. Show each character standing side by side, full body, facing the viewer:
1) THE DECKHAND (the hero): a young pirate boy, big round head, red bandana with a white dot tied at the back with two flying ends, short messy brown hair, big dark friendly eyes, rosy cheeks, blue-and-white horizontal striped sailor shirt, tan canvas pants, dark brown boots.
2) POLLY: a small green parrot with a light green forehead, curved yellow beak, red chest and blue wing tips.
3) MARINA: a cute mermaid with very long lilac hair, pink seashell top, teal green scaly tail with a lighter fin.
4) DORIS: a little friendly ghost made of a white bedsheet with a light blue shadow at the bottom, big blue eyes, wearing a red pirate bandana.
5) CAPTAIN ANCHOR: a female pirate captain, black tricorn hat with a white skull and gold trim, brown hair, green eyes, long red coat with gold buttons and collar, black pants, holding the chain of a huge iron anchor.
6) THE COOK: a spooky ghost-ship cook, tall white chef hat, pale face with glowing lilac eyes and a smile that is a little too wide, black dress with a white apron, holding a big kitchen cleaver.
7) THE RED KRAKEN: a giant red squid, pointed mantle with two fins on top, huge round yellow eyes with black slit pupils, many thick red tentacles with pale pink suckers.
8) THE GOD OF WEALTH: a big kind but imposing old god, golden crown with a blue gem and a red gem, long white beard, purple royal robe with gold trim, a halo of floating gold coins around his head.
9) THE TREASURE: a big open wooden pirate chest with gold bands, overflowing with gold coins and colorful gems, glowing.
```

## Os quadros

Todos começam com o mesmo estilo. Mantenha os personagens iguais aos da folha.

**`abertura_1.png`** — o maior tesouro de todos os tempos

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sea/ground, no faces or important details): a caption box will cover it. Keep the characters exactly like in the character reference sheet.
Scene: a calm moonlit night at sea. A deep navy starry sky with a big pale full moon on the upper right and a few dark purple clouds. Far away on the horizon, a dark rocky island with a cave; a warm golden light beams out of the cave and up into the sky, reflected on the water as a golden path. On the left, a small pirate ship with a red sail with a white skull and a glowing lantern sails toward the island. Mysterious and inviting mood.
```

**`abertura_2.png`** — o Grumete encontra o tesouro

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sea/ground, no faces or important details): a caption box will cover it. Keep the characters exactly like in the character reference sheet.
Scene: inside a dark purple-brown sea cave with stalactites. In the center, a huge open treasure chest overflowing with gold coins and gems, surrounded by piles of gold, shining golden god rays upward and lighting the whole cave. On the left, the Deckhand stands amazed with his mouth open, golden light on his face, and Polly the parrot flies excitedly next to his shoulder. Wonder and excitement mood.
```

**`abertura_3.png`** — o guardião: o Kraken Vermelho

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sea/ground, no faces or important details): a caption box will cover it. Keep the characters exactly like in the character reference sheet.
Scene: a violent storm at night, the sky and the sea tinted dark red, rain and a bolt of lightning. The giant Red Kraken rises from the sea in the center-right, angry yellow eyes, tentacles everywhere; one tentacle lifts the glowing treasure chest high into the air. On the left, the little pirate ship is tilted and breaking, and the Deckhand is falling through the air, knocked away by a tentacle. Dramatic, dangerous but still cartoony (not gory) mood.
```

**`abertura_4.png`** — a maldição dos deuses do mar

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sea/ground, no faces or important details): a caption box will cover it. Keep the characters exactly like in the character reference sheet.
Scene: a dark purple stormy night. In the heavy clouds at the top, two huge glowing white eyes of the angry sea gods look down, with lightning on both sides. In the center, the Deckhand stands on a small rock in the sea, wrapped in a glowing purple curse aura, with small purple skulls circling around him. Above him, a translucent ghostly copy of the Deckhand rises, and a swirling whirlpool of sea water spins around the rock (the tide always brings him back). Ominous, magical mood.
```

**`abertura_5.png`** — o mundo perde o equilíbrio

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sea/ground, no faces or important details): a caption box will cover it. Keep the characters exactly like in the character reference sheet.
Scene: a cursed world under a blood-red sky with a black eclipse sun and an orange corona. The ground is cracked with glowing purple cracks. Corrupted villains stand facing the viewer with glowing red eyes and a red evil aura: Captain Anchor (furious, red eyes, the skull on her hat glowing red) on the left, the Cook (red eyes, wide creepy smile, cleaver) in the center, angry red-eyed crabs at their feet, a huge White Whale with a horn rising from a dark red sea on the right, and red Kraken tentacles coming out of the ground at both edges. Dark but kid-friendly mood.
```

**`abertura_6.png`** — o Deus da Riqueza

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sea/ground, no faces or important details): a caption box will cover it. Keep the characters exactly like in the character reference sheet.
Scene: a warm golden sunset sky with radiant beams of light coming down from above. The God of Wealth, big and majestic, floats on soft golden clouds in the upper center, with a halo of gold coins circling his head, one hand pointing down at the hero; gold coins rain softly through the air. Far below, on a small sandy beach, the tiny Deckhand and Polly look up at him in awe. Holy, epic, hopeful mood.
```

**`abertura_7.png`** — a nova jornada começa

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sea/ground, no faces or important details): a caption box will cover it. Keep the characters exactly like in the character reference sheet.
Scene: dawn at sea, a sky going from blue at the top to pink and orange at the horizon, the sun rising with a golden reflection on the water. On the left, the pirate ship with the red skull sail sails to the right; the Deckhand stands proudly on the deck with Polly on his shoulder. Far away on the right, on the horizon, a dark island under a dark cloud with two small glowing red eyes: the lair of the Red Kraken. Adventurous, determined mood.
```

**`final_1.png`** — o tesouro devolvido

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sea/ground, no faces or important details): a caption box will cover it. Keep the characters exactly like in the character reference sheet.
Scene: a golden sky full of light beams. The God of Wealth, smiling, floats on a cloud on the right and receives the glowing treasure chest that floats up between him and the hero. On the left, the Deckhand stands on the sand, and the purple curse aura around him shatters into small purple shards flying away. Relief, victory and warmth mood.
```

**`final_2.png`** — o mundo em equilíbrio de novo

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sea/ground, no faces or important details): a caption box will cover it. Keep the characters exactly like in the character reference sheet.
Scene: a bright sunny day on a beautiful beach, clear blue sky, white clouds, a green island with a palm tree, turquoise sea. Everyone is friends again and happy, lined up on the sand: Marina waving from the water, Captain Anchor (normal green eyes, smiling), the Cook (normal lilac eyes, smiling sheepishly), the Deckhand in the center waving at the viewer, Polly flying above him, and Doris floating happily on the right. A few gold coins shine on the sand. Peaceful, happy ending mood.
```
