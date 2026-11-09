# A história: os textos e os prompts das imagens das cutscenes

São **20 imagens** (19 diferentes: a `final_2` aparece em dois finais):

| Cena | Quando aparece | Imagens | Situação |
| --- | --- | --- | --- |
| Abertura | Novo jogo, antes do mapa | `abertura_1` a `abertura_7` | **prontas** (pintadas) |
| Escolha | depois de vencer o Kraken | `escolha_1` a `escolha_3` | **prontas** (pintadas) |
| Final: devolveu | escolheu DEVOLVER | `final_1`, `final_2` | **prontas** (pintadas) |
| Roubou | escolheu ROUBAR (o jogo recomeça no modo difícil) | `roubou_1` a `roubou_3` | **prontas** (pintadas) |
| Escolha na tempestade | zerou o modo difícil | `tempestade_1`, `tempestade_2` | **prontas** (pintadas) |
| Final verdadeiro | devolveu no modo difícil | `verdadeiro_1`, `final_2` | **prontas** (pintadas) |
| Final do rei | ficou com o ouro no modo difícil | `rei_1`, `rei_2` | **prontas** (pintadas) |

Todas já estão pintadas. Os prompts ficam aqui para refazer alguma (ou criar uma cena nova) no mesmo estilo; as
versões de código (`desenhar_historia.py`, 256 x 144) só voltam se rodar o script com `--forcar`.

## Como usar

1. **Para as que faltam, o melhor jeito de manter o estilo da abertura:** abra uma conversa nova com a IA de imagem e
   anexe as **7 imagens da abertura** (`Assets/Resources/Arte/historia/abertura_1.png` a `abertura_7.png`) dizendo
   *"use exatamente este estilo de arte e estes personagens"*. Depois mande os quadros que faltam, **um por vez, na
   mesma conversa**. (Se a IA não aceitar imagens, mande primeiro o bloco "Folha dos personagens".)
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
| `escolha_1` | O Kraken fugiu para o fundo do mar. E ali, no convés, estava o baú: o maior tesouro de todos os tempos. | The Kraken fled to the bottom of the sea. And there, on the deck, was the chest: the greatest treasure of all time. |
| `escolha_2` | O Deus da Riqueza desceu das nuvens: "Muito bem, pirata. Agora devolva o que é MEU, e eu tiro a sua maldição." | The God of Wealth came down from the clouds: "Well done, pirate. Now give back what is MINE, and I will lift your curse." |
| `escolha_3` | Marina: "Devolve, Grumete. Por favor." Louro: "Rouba. Confia." (+ os botões DEVOLVER / ROUBAR) | Marina: "Give it back, Deckhand. Please." Polly: "Steal it. Trust me." (+ GIVE BACK / STEAL buttons) |
| `roubou_1` | O Grumete fechou o baú, olhou bem nos olhos do Deus da Riqueza... e saiu correndo. | The Deckhand closed the chest, looked the God of Wealth right in the eye... and ran. |
| `roubou_2` | "LADRÃO!" O céu fechou. A maldição voltou pior: todo mundo virou maligno de novo, e o Kraken levou a Marina outra vez. | "THIEF!" The sky went dark. The curse came back worse: everyone turned evil again, and the Kraken took Marina once more. |
| `roubou_3` | Agora o mar inteiro quer o baú de volta. Em cada ilha, um tsunami. Corre, Grumete. E não olha para trás. | Now the whole sea wants the chest back. On every island, a tsunami. Run, Deckhand. And don't look back. |
| `tempestade_1` | O Kraken caiu de novo e a onda quebrou. No meio da tempestade, o Grumete abriu o baú. O ouro nem brilhava mais. | The Kraken fell again and the wave broke. In the middle of the storm, the Deckhand opened the chest. The gold didn't even shine anymore. |
| `tempestade_2` | O Deus da Riqueza apareceu uma última vez: "Última chance, pirata. Devolva... ou fique com ele PARA SEMPRE." (+ os botões DEVOLVER / FICAR COM O OURO) | The God of Wealth appeared one last time: "Last chance, pirate. Give it back... or keep it FOREVER." (+ the two buttons) |
| `verdadeiro_1` | O Grumete devolveu o ouro. Moeda por moeda. A tempestade parou, a maldição se desfez e o mar, enfim, acalmou. | The Deckhand gave the gold back. Coin by coin. The storm stopped, the curse was broken and the sea, at last, grew calm. |
| `final_2` (no final verdadeiro) | Deus da Riqueza: "Foi a volta mais longa que um pirata já deu. Mas você voltou." E o Louro contou essa história para todo mundo. Com mentiras, claro. | God of Wealth: "That was the longest way back a pirate ever took. But you came back." And Polly told the story to everyone. With lies, of course. |
| `rei_1` | O Grumete ficou com o ouro. Todo ele. Era, enfim, o pirata mais rico de todos os tempos. | The Deckhand kept the gold. All of it. At last, the richest pirate of all time. |
| `rei_2` | E reina até hoje, num mar que nunca mais acalmou, sem ninguém para gastar com ele. Nem o Louro ficou. Fim. (juro) | And reigns to this day over a sea that never calmed, with no one to spend it on. Not even Polly stayed. The End. (I swear) |

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

## A abertura (já pronta)

Guardados aqui como referência: estas são as imagens que já estão no jogo.

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



## Os quadros que faltam (no estilo da abertura)

Todos começam com o mesmo estilo, já pedindo para seguir as imagens da abertura. Mande na ordem abaixo.
Nos quadros com **botões de escolha** (`escolha_3` e `tempestade_2`) a parte de baixo que fica coberta é maior
(a legenda + os dois botões em cima dela): por isso esses dois pedem a **terça parte de baixo** simples.

### Escolha (depois de vencer o Kraken)

**`escolha_1.png`** — o baú no convés

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, rich limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). Match exactly the art style, colors and character designs of the attached opening cutscene images. No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just deck/sea, no faces or important details): a caption box will cover it.
Scene: right after the final battle, at sunset on the wooden deck of a big ship on the high seas. The sea is calming down; on the right, the last red tip of a Kraken tentacle sinks into the water with bubbles and foam. In the center of the deck sits the big treasure chest, open, overflowing with gold coins and gems, glowing with warm golden light and god rays. On the left, the Deckhand walks toward it, amazed, with Polly the parrot on his shoulder. A few gold coins are scattered on the deck. Mood: victory, wonder, temptation.
```

**`escolha_2.png`** — o Deus da Riqueza pede o tesouro

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, rich limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). Match exactly the art style, colors and character designs of the attached opening cutscene images. No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just deck/sea, no faces or important details): a caption box will cover it.
Scene: the same ship deck at sunset. The God of Wealth descends from parting golden clouds in the upper right, surrounded by a halo of floating gold coins and beams of light, calm and solemn, one open hand reaching down toward the treasure chest. On the left, the Deckhand stands next to the glowing open chest, looking up at the god, unsure, one hand resting on the chest; Polly on his shoulder looks at the gold. Mood: solemn, a big decision is coming.
```

**`escolha_3.png`** — devolver ou roubar?

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, rich limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). Match exactly the art style, colors and character designs of the attached opening cutscene images. No text, no letters, no logos, no UI, no border, no frame. Keep the bottom THIRD of the image simple and calm (just deck/sea, no faces or important details): a caption box and two choice buttons will cover it.
Scene: the dilemma, a symmetric composition on the ship deck at dusk. In the center, the Deckhand stands behind the glowing chest, torn, looking from one side to the other. On the LEFT half, in cool blue moonlight, Marina the mermaid leans out of the sea at the side of the ship, hands together, pleading with big sad eyes. On the RIGHT half, in warm golden light, Polly the parrot perches on the edge of the chest with a mischievous grin and a coin in his beak, whispering. High in the sky, the faint golden silhouette of the God of Wealth watches. Mood: a funny but serious moral choice, good on one side, greed on the other.
```

### Final: devolveu o tesouro

**`final_1.png`** — o tesouro devolvido

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, rich limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). Match exactly the art style, colors and character designs of the attached opening cutscene images. No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sea/ground, no faces or important details): a caption box will cover it.
Scene: a golden sky full of light beams. The God of Wealth, smiling kindly, floats on a cloud on the right and receives the glowing treasure chest, which floats up toward him trailing sparkles. On the left, the Deckhand stands on the ship deck, arms open, and the purple curse aura around him shatters into small purple shards flying away into the light; Polly flaps happily. Mood: relief, victory, warmth.
```

**`final_2.png`** — o mundo em equilíbrio de novo (aparece no final normal e no verdadeiro)

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, rich limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). Match exactly the art style, colors and character designs of the attached opening cutscene images. No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sea/sand, no faces or important details): a caption box will cover it.
Scene: a bright sunny day on a beautiful tropical beach, clear blue sky, white fluffy clouds, a green island with palm trees, turquoise sea, the pirate ship anchored in the background. Everyone is friends again and happy, together in the middle of the image: Marina waving from the shallow water, Captain Anchor smiling with normal green eyes, the Cook smiling sheepishly with normal lilac eyes, the Deckhand in the center waving at the viewer, Polly flying above him, Doris the little ghost floating happily, and a few friendly crabs. Far away in the sky, the God of Wealth smiles faintly in the clouds. Mood: peaceful, happy ending.
```

### Roubou o tesouro (vai para o modo difícil)

**`roubou_1.png`** — e saiu correndo

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, rich limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). Match exactly the art style, colors and character designs of the attached opening cutscene images. No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just deck/sea, no faces or important details): a caption box will cover it.
Scene: comic action at sunset turning red. The Deckhand runs fast to the right across the ship deck carrying the heavy closed treasure chest over his head, with a sly cheeky grin, gold coins spilling behind him, speed lines and dust. Polly flies next to him cackling. On the left, the God of Wealth floats in the clouds frozen in shock, eyes wide, mouth open, crown tilted, reaching out a hand too late. Mood: funny heist, "uh-oh".
```

**`roubou_2.png`** — "LADRÃO!"

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, rich limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). Match exactly the art style, colors and character designs of the attached opening cutscene images. No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sea/ground, no faces or important details): a caption box will cover it.
Scene: the sky turns black and blood red with lightning. In the center top, the God of Wealth has become furious and enormous, his eyes glowing red, his robe darkened, coins swirling like a storm around him. Below, the curse returns: Captain Anchor and the Cook stand with glowing red eyes and a red evil aura again; on the right, a huge red Kraken tentacle bursts from the sea, wraps around Marina and pulls her down into the dark water while she reaches out. On the left, the small Deckhand hugs the stolen chest, scared, with Polly hiding behind him. Mood: dramatic punishment, still cartoony (not gory).
```

**`roubou_3.png`** — em cada ilha, um tsunami

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, rich limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). Match exactly the art style, colors and character designs of the attached opening cutscene images. No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sand/sea, no faces or important details): a caption box will cover it.
Scene: a stormy gray-blue sky with rain and lightning over a tropical island beach. A gigantic dark blue tsunami wave with white foam curls over the whole left side of the image, about to crash. The Deckhand runs desperately to the right along the beach with the treasure chest strapped on his back, Polly flying ahead of him screaming. Bent palm trees, flying leaves and spray. Mood: chase, panic, "RUN!".
```

### Escolha na tempestade (zerou o modo difícil)

**`tempestade_1.png`** — o ouro nem brilhava mais

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, rich limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). Match exactly the art style, colors and character designs of the attached opening cutscene images, but with a cold desaturated storm palette (grays, dark teal, navy). No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just deck/sea, no faces or important details): a caption box will cover it.
Scene: heavy rain on a soaked wooden ship deck at night in an endless storm. In the background, a giant wave breaks and collapses, and the last Kraken tentacle sinks. In the center, the Deckhand kneels in front of the open treasure chest; the gold inside is dull and gray, with no shine at all. He looks tired and sad, rain dripping from his bandana; Polly sits hunched and wet on the chest. Mood: melancholic, "was it worth it?".
```

**`tempestade_2.png`** — última chance

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, rich limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). Match exactly the art style, colors and character designs of the attached opening cutscene images, but with a cold desaturated storm palette with one warm golden light. No text, no letters, no logos, no UI, no border, no frame. Keep the bottom THIRD of the image simple and calm (just deck/sea, no faces or important details): a caption box and two choice buttons will cover it.
Scene: the same stormy deck. The God of Wealth appears one last time in the upper right, half transparent, made of dim golden light breaking through the dark storm clouds, stern but sad, one hand held out, waiting. The Deckhand stands next to the dull chest, looking up at him, rain falling between them. Mood: final, quiet, heavy decision.
```

### Final verdadeiro (devolveu no modo difícil)

**`verdadeiro_1.png`** — moeda por moeda

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, rich limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). Match exactly the art style, colors and character designs of the attached opening cutscene images. No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just deck/sea, no faces or important details): a caption box will cover it.
Scene: the storm is ending. Dark clouds part and warm sunbeams break through, with a rainbow appearing over a calming sea. On the left, the Deckhand pours the gold out of the chest, and a long glowing stream of coins rises one by one into the sky toward the God of Wealth on the right, who gently gathers them with a proud smile. Polly watches, surprised. Mood: redemption, the sea finally at peace.
```

### Final do rei (ficou com o ouro no modo difícil)

**`rei_1.png`** — o pirata mais rico de todos os tempos

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, rich limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). Match exactly the art style, colors and character designs of the attached opening cutscene images, with a dark stormy palette and bright gold. No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just gold pile/sea, no faces or important details): a caption box will cover it.
Scene: an enormous mountain of gold coins and gems rising out of a dark stormy sea, rain and lightning all around. On the very top, the Deckhand sits on the treasure chest like a throne, wearing a small golden crown, holding up a coin, grinning proudly. He is all alone. Mood: triumphant but a little lonely.
```

**`rei_2.png`** — reina até hoje, sozinho

```
16-bit pixel art cutscene illustration for a cute pirate adventure 2D platformer game, crisp clean pixels, rich limited palette, soft painted lighting, cinematic wide shot, 16:9 (1672x941). Match exactly the art style, colors and character designs of the attached opening cutscene images, with a dark, cold, desaturated storm palette. No text, no letters, no logos, no UI, no border, no frame. Keep the bottom quarter of the image simple and calm (just sea, no important details): a caption box will cover it.
Scene: a very wide, lonely shot. A tiny golden island of treasure in the middle of an endless dark stormy sea that never calms, rain everywhere. On top of it, the tiny crowned Deckhand sits alone. Far away on the upper right, small Polly flies away toward a thin line of light on the horizon, leaving him. Mood: bittersweet, quiet, "The End".
```
