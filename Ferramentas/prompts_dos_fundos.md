# Fundos pintados: os prompts de todas as imagens

São **13 lugares x 5 camadas = 65 imagens**. Cada bloco abaixo é um prompt **completo**: copie o bloco inteiro e mande para a IA de imagem, um por vez. Gere as 5 camadas de um lugar seguidas (na mesma conversa), para as cores combinarem.

**Como salvar:** `<pasta>_<camada>.png` (o nome está em cima de cada bloco, ex.: `selva_ceu.png`) e ponha todas em `Ferramentas/fundos_gerados/`. Não precisa acertar o tamanho, recortar nem apagar o fundo: eu passo um script que redimensiona, tira o magenta, emenda as bordas e coloca cada uma no lugar certo do jogo.

Se a IA souber fazer PNG com **fundo transparente de verdade**, pode pedir isso no lugar do magenta (as 4 camadas de baixo). O céu é sempre opaco.

| Camada | Distância | O que é |
|---|---|---|
| `ceu` | infinito | o céu inteiro, opaco (com sol/lua no alto à esquerda) |
| `ilhas` | bem longe | a faixa baixa do horizonte |
| `galeao` | longe | um marco grande lá no fundo |
| `palmeiras` | meio | coisas altas a meia distância |
| `mato` | perto | a faixa baixa e escura logo atrás da fase |

## Porto (fase 1, de dia) — `porto`

**`porto_ceu.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: bright sky blue, turquoise sea, white houses with terracotta roofs, red flags, warm sunlight.
Image size: 1577x672 (about 21:9, wide).
This is the SKY layer, the farthest one: it fills the WHOLE image edge to edge, fully opaque, nothing transparent. No ground and no foreground objects. The bottom edge is the horizon haze. Seamless horizontally: the left and right edges must match perfectly so the image can repeat side by side.
Show: a bright clear morning sky, light blue, a few big fluffy white clouds, a warm sun with a soft glow in the upper left.
```

**`porto_ilhas.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: bright sky blue, turquoise sea, white houses with terracotta roofs, red flags, warm sunlight.
Image size: 1363x266 (about 5:1, a very wide low strip; if the tool can't do 5:1, use 21:9 and keep the scenery in the bottom quarter).
This is the FARTHEST scenery layer: a long low silhouette on the horizon. Pale, hazy and low contrast, blending into the sky. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: a turquoise sea horizon with small green islands and a red-and-white lighthouse on a far rocky point; calm sea water fills the bottom of the strip.
```

**`porto_galeao.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: bright sky blue, turquoise sea, white houses with terracotta roofs, red flags, warm sunlight.
Image size: 1367x348 (about 4:1, wide strip; if the tool can't do 4:1, use 21:9 and keep the scenery in the bottom third).
This is a FAR-MIDDLE layer: one big landmark group in the distance, a bit hazy and desaturated. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: a cheerful harbor town on a hill: white houses with terracotta roofs, a small stone castle with red flags on top, a big pirate galleon moored at the docks.
```

**`porto_palmeiras.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: bright sky blue, turquoise sea, white houses with terracotta roofs, red flags, warm sunlight.
Image size: 1577x653 (about 21:9, wide).
This is the MIDDLE layer: tall elements at medium distance, darker and more saturated than the far layers, but still softer than the gameplay foreground. The bottom of the image is filled along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: wooden harbor docks with tall ship masts and furled sails, palm trees and a wooden harbor crane.
```

**`porto_mato.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: bright sky blue, turquoise sea, white houses with terracotta roofs, red flags, warm sunlight.
Image size: 1577x344 (about 4.6:1, wide low strip; if the tool can't do it, use 21:9 and keep the scenery in the bottom third).
This is the NEAREST background layer, right behind the gameplay: low and continuous along the bottom, the DARKEST layer, almost a silhouette with a little rim light. Nothing that looks like a platform, a ledge or spikes. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: the edge of a dock close up: crates, barrels, coiled ropes, fishing nets and small bushes.
```

## Selva (fase 2, tarde dourada) — `selva`

**`selva_ceu.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep jungle greens, warm golden-orange light, teal shadows.
Image size: 1577x672 (about 21:9, wide).
This is the SKY layer, the farthest one: it fills the WHOLE image edge to edge, fully opaque, nothing transparent. No ground and no foreground objects. The bottom edge is the horizon haze. Seamless horizontally: the left and right edges must match perfectly so the image can repeat side by side.
Show: a warm golden afternoon sky, orange near the horizon fading to soft blue at the top, big soft clouds lit in gold, the sun in the upper left.
```

**`selva_ilhas.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep jungle greens, warm golden-orange light, teal shadows.
Image size: 1363x266 (about 5:1, a very wide low strip; if the tool can't do 5:1, use 21:9 and keep the scenery in the bottom quarter).
This is the FARTHEST scenery layer: a long low silhouette on the horizon. Pale, hazy and low contrast, blending into the sky. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: distant jungle mountains with a tall waterfall, blue-green silhouettes fading into golden haze.
```

**`selva_galeao.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep jungle greens, warm golden-orange light, teal shadows.
Image size: 1367x348 (about 4:1, wide strip; if the tool can't do 4:1, use 21:9 and keep the scenery in the bottom third).
This is a FAR-MIDDLE layer: one big landmark group in the distance, a bit hazy and desaturated. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: ancient stone temple ruins overgrown with vines, half hidden among giant jungle trees, far away.
```

**`selva_palmeiras.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep jungle greens, warm golden-orange light, teal shadows.
Image size: 1577x653 (about 21:9, wide).
This is the MIDDLE layer: tall elements at medium distance, darker and more saturated than the far layers, but still softer than the gameplay foreground. The bottom of the image is filled along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: a dense tropical jungle: tall tree trunks, hanging vines, huge leaves, golden light beams between the trees.
```

**`selva_mato.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep jungle greens, warm golden-orange light, teal shadows.
Image size: 1577x344 (about 4.6:1, wide low strip; if the tool can't do it, use 21:9 and keep the scenery in the bottom third).
This is the NEAREST background layer, right behind the gameplay: low and continuous along the bottom, the DARKEST layer, almost a silhouette with a little rim light. Nothing that looks like a platform, a ledge or spikes. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: close jungle undergrowth: ferns, broad leaves, twisted roots and a few tropical flowers.
```

## Lagoa (fase 3, fim de tarde rosa) — `lagoa`

**`lagoa_ceu.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: soft pink, lavender, turquoise water, fresh greens.
Image size: 1577x672 (about 21:9, wide).
This is the SKY layer, the farthest one: it fills the WHOLE image edge to edge, fully opaque, nothing transparent. No ground and no foreground objects. The bottom edge is the horizon haze. Seamless horizontally: the left and right edges must match perfectly so the image can repeat side by side.
Show: a soft pink and lavender sky with cotton-candy clouds, a low sun in the upper left.
```

**`lagoa_ilhas.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: soft pink, lavender, turquoise water, fresh greens.
Image size: 1363x266 (about 5:1, a very wide low strip; if the tool can't do 5:1, use 21:9 and keep the scenery in the bottom quarter).
This is the FARTHEST scenery layer: a long low silhouette on the horizon. Pale, hazy and low contrast, blending into the sky. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: a calm turquoise lagoon horizon with tiny rocky islets and one small island with a palm tree, pink haze; calm water fills the bottom of the strip.
```

**`lagoa_galeao.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: soft pink, lavender, turquoise water, fresh greens.
Image size: 1367x348 (about 4:1, wide strip; if the tool can't do 4:1, use 21:9 and keep the scenery in the bottom third).
This is a FAR-MIDDLE layer: one big landmark group in the distance, a bit hazy and desaturated. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: the far shore of the lagoon: a wooden pier with a moored rowboat, small fishing huts on stilts over the water, pink blossom trees.
```

**`lagoa_palmeiras.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: soft pink, lavender, turquoise water, fresh greens.
Image size: 1577x653 (about 21:9, wide).
This is the MIDDLE layer: tall elements at medium distance, darker and more saturated than the far layers, but still softer than the gameplay foreground. The bottom of the image is filled along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: pink blossom trees and leaning palm trees at the water's edge, petals floating in the air.
```

**`lagoa_mato.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: soft pink, lavender, turquoise water, fresh greens.
Image size: 1577x344 (about 4.6:1, wide low strip; if the tool can't do it, use 21:9 and keep the scenery in the bottom third).
This is the NEAREST background layer, right behind the gameplay: low and continuous along the bottom, the DARKEST layer, almost a silhouette with a little rim light. Nothing that looks like a platform, a ledge or spikes. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: reeds, cattails, water lilies and pink flowers at the water's edge.
```

## Forte da Capitã (fase 4, tarde) — `forte`

**`forte_ceu.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: lavender-blue sky, grey and sandy stone, black flags, warm torch light.
Image size: 1577x672 (about 21:9, wide).
This is the SKY layer, the farthest one: it fills the WHOLE image edge to edge, fully opaque, nothing transparent. No ground and no foreground objects. The bottom edge is the horizon haze. Seamless horizontally: the left and right edges must match perfectly so the image can repeat side by side.
Show: a clear lavender-blue afternoon sky with long thin clouds and a little cannon smoke drifting, the sun in the upper left.
```

**`forte_ilhas.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: lavender-blue sky, grey and sandy stone, black flags, warm torch light.
Image size: 1363x266 (about 5:1, a very wide low strip; if the tool can't do 5:1, use 21:9 and keep the scenery in the bottom quarter).
This is the FARTHEST scenery layer: a long low silhouette on the horizon. Pale, hazy and low contrast, blending into the sky. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: a hazy blue sea horizon with distant fortress towers on far islands; sea water fills the bottom of the strip.
```

**`forte_galeao.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: lavender-blue sky, grey and sandy stone, black flags, warm torch light.
Image size: 1367x348 (about 4:1, wide strip; if the tool can't do 4:1, use 21:9 and keep the scenery in the bottom third).
This is a FAR-MIDDLE layer: one big landmark group in the distance, a bit hazy and desaturated. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: a huge stone pirate fortress on a cliff: thick walls, round towers, black flags with an anchor emblem, cannons on the battlements.
```

**`forte_palmeiras.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: lavender-blue sky, grey and sandy stone, black flags, warm torch light.
Image size: 1577x653 (about 21:9, wide).
This is the MIDDLE layer: tall elements at medium distance, darker and more saturated than the far layers, but still softer than the gameplay foreground. The bottom of the image is filled along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: stone battlements and watchtowers, wooden scaffolding, burning torches, piles of cannonballs, a few palm trees.
```

**`forte_mato.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: lavender-blue sky, grey and sandy stone, black flags, warm torch light.
Image size: 1577x344 (about 4.6:1, wide low strip; if the tool can't do it, use 21:9 and keep the scenery in the bottom third).
This is the NEAREST background layer, right behind the gameplay: low and continuous along the bottom, the DARKEST layer, almost a silhouette with a little rim light. Nothing that looks like a platform, a ledge or spikes. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: a low stone wall with sandbags, barrels, crates and coiled ropes.
```

## Vila (fase 5, entardecer com poeira) — `vila`

**`vila_ceu.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: purple and gold dusk, warm lantern orange, weathered wood.
Image size: 1577x672 (about 21:9, wide).
This is the SKY layer, the farthest one: it fills the WHOLE image edge to edge, fully opaque, nothing transparent. No ground and no foreground objects. The bottom edge is the horizon haze. Seamless horizontally: the left and right edges must match perfectly so the image can repeat side by side.
Show: a purple and gold dusk sky, warm dusty light, wispy clouds, a low sun in the upper left.
```

**`vila_ilhas.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: purple and gold dusk, warm lantern orange, weathered wood.
Image size: 1363x266 (about 5:1, a very wide low strip; if the tool can't do 5:1, use 21:9 and keep the scenery in the bottom quarter).
This is the FARTHEST scenery layer: a long low silhouette on the horizon. Pale, hazy and low contrast, blending into the sky. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: rolling hills with a windmill and a few tiny far houses in a dusty purple haze.
```

**`vila_galeao.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: purple and gold dusk, warm lantern orange, weathered wood.
Image size: 1367x348 (about 4:1, wide strip; if the tool can't do 4:1, use 21:9 and keep the scenery in the bottom third).
This is a FAR-MIDDLE layer: one big landmark group in the distance, a bit hazy and desaturated. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: a crooked pirate village on wooden stilts: leaning wooden houses, rope bridges between them, glowing lanterns.
```

**`vila_palmeiras.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: purple and gold dusk, warm lantern orange, weathered wood.
Image size: 1577x653 (about 21:9, wide).
This is the MIDDLE layer: tall elements at medium distance, darker and more saturated than the far layers, but still softer than the gameplay foreground. The bottom of the image is filled along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: closer village houses with colorful laundry lines, wooden signposts, market stalls with striped awnings, palm trees.
```

**`vila_mato.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: purple and gold dusk, warm lantern orange, weathered wood.
Image size: 1577x344 (about 4.6:1, wide low strip; if the tool can't do it, use 21:9 and keep the scenery in the bottom third).
This is the NEAREST background layer, right behind the gameplay: low and continuous along the bottom, the DARKEST layer, almost a silhouette with a little rim light. Nothing that looks like a platform, a ledge or spikes. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: wooden fences, crates, flower pots, a wheelbarrow and dusty bushes.
```

## Abordagem (navios) (fase 6, pôr do sol) — `navios`

**`navios_ceu.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: fiery orange and crimson sunset, deep blue sea, dark wood silhouettes.
Image size: 1577x672 (about 21:9, wide).
This is the SKY layer, the farthest one: it fills the WHOLE image edge to edge, fully opaque, nothing transparent. No ground and no foreground objects. The bottom edge is the horizon haze. Seamless horizontally: the left and right edges must match perfectly so the image can repeat side by side.
Show: a dramatic sunset over the open sea: orange and crimson sky, a big golden sun low in the upper left, clouds lit red and gold.
```

**`navios_ilhas.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: fiery orange and crimson sunset, deep blue sea, dark wood silhouettes.
Image size: 1363x266 (about 5:1, a very wide low strip; if the tool can't do 5:1, use 21:9 and keep the scenery in the bottom quarter).
This is the FARTHEST scenery layer: a long low silhouette on the horizon. Pale, hazy and low contrast, blending into the sky. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: the open sea horizon at sunset with silhouettes of distant pirate ships; the sea fills the bottom of the strip and reflects the orange light.
```

**`navios_galeao.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: fiery orange and crimson sunset, deep blue sea, dark wood silhouettes.
Image size: 1367x348 (about 4:1, wide strip; if the tool can't do 4:1, use 21:9 and keep the scenery in the bottom third).
This is a FAR-MIDDLE layer: one big landmark group in the distance, a bit hazy and desaturated. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: two big pirate galleons side by side in battle, cannon smoke, sails lit by the sunset, waves at their base.
```

**`navios_palmeiras.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: fiery orange and crimson sunset, deep blue sea, dark wood silhouettes.
Image size: 1577x653 (about 21:9, wide).
This is the MIDDLE layer: tall elements at medium distance, darker and more saturated than the far layers, but still softer than the gameplay foreground. The bottom of the image is filled along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: close ship masts with big sails and rigging, rope ladders and a crow's nest, backlit by the sunset.
```

**`navios_mato.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: fiery orange and crimson sunset, deep blue sea, dark wood silhouettes.
Image size: 1577x344 (about 4.6:1, wide low strip; if the tool can't do it, use 21:9 and keep the scenery in the bottom third).
This is the NEAREST background layer, right behind the gameplay: low and continuous along the bottom, the DARKEST layer, almost a silhouette with a little rim light. Nothing that looks like a platform, a ledge or spikes. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: ship railings, coiled ropes, barrels and a cannon on the edge of a deck, dark with orange rim light.
```

## Gruta da Cozinheira (fase 7, caverna escura) — `gruta`

**`gruta_ceu.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep purple and blue darkness, warm orange fire glow, teal glowing crystals.
Image size: 1577x672 (about 21:9, wide).
This is the SKY layer, the farthest one: it fills the WHOLE image edge to edge, fully opaque, nothing transparent. No ground and no foreground objects. The bottom edge is the horizon haze. Seamless horizontally: the left and right edges must match perfectly so the image can repeat side by side.
Show: the inside of a huge cave (no sky at all): a dark rock ceiling with stalactites, faint purple light, a few glowing teal crystals.
```

**`gruta_ilhas.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep purple and blue darkness, warm orange fire glow, teal glowing crystals.
Image size: 1363x266 (about 5:1, a very wide low strip; if the tool can't do 5:1, use 21:9 and keep the scenery in the bottom quarter).
This is the FARTHEST scenery layer: a long low silhouette on the horizon. Pale, hazy and low contrast, blending into the sky. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: far cave walls with a faint underground waterfall and a glowing pool, deep blue darkness.
```

**`gruta_galeao.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep purple and blue darkness, warm orange fire glow, teal glowing crystals.
Image size: 1367x348 (about 4:1, wide strip; if the tool can't do 4:1, use 21:9 and keep the scenery in the bottom third).
This is a FAR-MIDDLE layer: one big landmark group in the distance, a bit hazy and desaturated. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: an underground pirate kitchen far away: huge hanging cooking pots, a chimney carved in the rock with smoke, warm orange fire glow.
```

**`gruta_palmeiras.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep purple and blue darkness, warm orange fire glow, teal glowing crystals.
Image size: 1577x653 (about 21:9, wide).
This is the MIDDLE layer: tall elements at medium distance, darker and more saturated than the far layers, but still softer than the gameplay foreground. The bottom of the image is filled along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: stalagmites and rock columns with glowing mushrooms and crystals.
```

**`gruta_mato.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep purple and blue darkness, warm orange fire glow, teal glowing crystals.
Image size: 1577x344 (about 4.6:1, wide low strip; if the tool can't do it, use 21:9 and keep the scenery in the bottom third).
This is the NEAREST background layer, right behind the gameplay: low and continuous along the bottom, the DARKEST layer, almost a silhouette with a little rim light. Nothing that looks like a platform, a ledge or spikes. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: rocks, fish bones, broken barrels, old pots and small glowing mushrooms on the cave floor, very dark.
```

## Navio Fantasma (fase 8, noite) — `navio_fantasma`

**`navio_fantasma_ceu.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: ghostly pale green, teal fog, dark blue night.
Image size: 1577x672 (about 21:9, wide).
This is the SKY layer, the farthest one: it fills the WHOLE image edge to edge, fully opaque, nothing transparent. No ground and no foreground objects. The bottom edge is the horizon haze. Seamless horizontally: the left and right edges must match perfectly so the image can repeat side by side.
Show: an eerie night sky with green-tinted fog, a pale full moon with a green halo in the upper left, wisps of ghost light.
```

**`navio_fantasma_ilhas.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: ghostly pale green, teal fog, dark blue night.
Image size: 1363x266 (about 5:1, a very wide low strip; if the tool can't do 5:1, use 21:9 and keep the scenery in the bottom quarter).
This is the FARTHEST scenery layer: a long low silhouette on the horizon. Pale, hazy and low contrast, blending into the sky. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: a foggy sea with ghostly shipwreck silhouettes far away; dark water fills the bottom of the strip.
```

**`navio_fantasma_galeao.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: ghostly pale green, teal fog, dark blue night.
Image size: 1367x348 (about 4:1, wide strip; if the tool can't do 4:1, use 21:9 and keep the scenery in the bottom third).
This is a FAR-MIDDLE layer: one big landmark group in the distance, a bit hazy and desaturated. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: a huge ghost ship with torn sails glowing pale green, floating above the fog.
```

**`navio_fantasma_palmeiras.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: ghostly pale green, teal fog, dark blue night.
Image size: 1577x653 (about 21:9, wide).
This is the MIDDLE layer: tall elements at medium distance, darker and more saturated than the far layers, but still softer than the gameplay foreground. The bottom of the image is filled along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: broken masts, tattered glowing sails and hanging chains, translucent green glow.
```

**`navio_fantasma_mato.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: ghostly pale green, teal fog, dark blue night.
Image size: 1577x344 (about 4.6:1, wide low strip; if the tool can't do it, use 21:9 and keep the scenery in the bottom third).
This is the NEAREST background layer, right behind the gameplay: low and continuous along the bottom, the DARKEST layer, almost a silhouette with a little rim light. Nothing that looks like a platform, a ledge or spikes. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: an old broken ship railing, ghostly lanterns, cobwebs and floating candles.
```

## Penhasco dos Ventos (deserto) (fase 9, dia com vento) — `penhasco`

**`penhasco_ceu.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: pink and orange sky, red and ochre sandstone, dusty haze.
Image size: 1577x672 (about 21:9, wide).
This is the SKY layer, the farthest one: it fills the WHOLE image edge to edge, fully opaque, nothing transparent. No ground and no foreground objects. The bottom edge is the horizon haze. Seamless horizontally: the left and right edges must match perfectly so the image can repeat side by side.
Show: a bright windy desert sky, pink fading to orange at the horizon, long fast streaked clouds, the sun in the upper left.
```

**`penhasco_ilhas.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: pink and orange sky, red and ochre sandstone, dusty haze.
Image size: 1363x266 (about 5:1, a very wide low strip; if the tool can't do 5:1, use 21:9 and keep the scenery in the bottom quarter).
This is the FARTHEST scenery layer: a long low silhouette on the horizon. Pale, hazy and low contrast, blending into the sky. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: distant desert mesas and buttes on the horizon in a dusty haze.
```

**`penhasco_galeao.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: pink and orange sky, red and ochre sandstone, dusty haze.
Image size: 1367x348 (about 4:1, wide strip; if the tool can't do 4:1, use 21:9 and keep the scenery in the bottom third).
This is a FAR-MIDDLE layer: one big landmark group in the distance, a bit hazy and desaturated. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: huge sandstone rock arches and canyon walls, a swirling dust devil, a wooden windmill on a cliff top.
```

**`penhasco_palmeiras.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: pink and orange sky, red and ochre sandstone, dusty haze.
Image size: 1577x653 (about 21:9, wide).
This is the MIDDLE layer: tall elements at medium distance, darker and more saturated than the far layers, but still softer than the gameplay foreground. The bottom of the image is filled along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: a canyon cliff edge with wind-bent dead trees, tall cacti and hanging ropes, sand blowing in the wind.
```

**`penhasco_mato.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: pink and orange sky, red and ochre sandstone, dusty haze.
Image size: 1577x344 (about 4.6:1, wide low strip; if the tool can't do it, use 21:9 and keep the scenery in the bottom third).
This is the NEAREST background layer, right behind the gameplay: low and continuous along the bottom, the DARKEST layer, almost a silhouette with a little rim light. Nothing that looks like a platform, a ledge or spikes. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: dry desert ground: rocks, cacti, tumbleweeds, dry grass and an animal skull.
```

## Mar da Baleia (recife) (fase 10, noite de lua cheia) — `recife`

**`recife_ceu.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep navy blue, silver moonlight, turquoise glow, white foam.
Image size: 1577x672 (about 21:9, wide).
This is the SKY layer, the farthest one: it fills the WHOLE image edge to edge, fully opaque, nothing transparent. No ground and no foreground objects. The bottom edge is the horizon haze. Seamless horizontally: the left and right edges must match perfectly so the image can repeat side by side.
Show: a deep blue night over the open ocean, many stars, a big bright full moon in the upper left lighting the clouds silver.
```

**`recife_ilhas.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep navy blue, silver moonlight, turquoise glow, white foam.
Image size: 1363x266 (about 5:1, a very wide low strip; if the tool can't do 5:1, use 21:9 and keep the scenery in the bottom quarter).
This is the FARTHEST scenery layer: a long low silhouette on the horizon. Pale, hazy and low contrast, blending into the sky. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: the open ocean horizon at night with moonlight glittering on the water and a giant white whale tail splashing far away; the sea fills the bottom of the strip.
```

**`recife_galeao.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep navy blue, silver moonlight, turquoise glow, white foam.
Image size: 1367x348 (about 4:1, wide strip; if the tool can't do 4:1, use 21:9 and keep the scenery in the bottom third).
This is a FAR-MIDDLE layer: one big landmark group in the distance, a bit hazy and desaturated. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: jagged reef rocks sticking out of the sea, waves breaking with white foam, a wrecked ship stuck on the rocks.
```

**`recife_palmeiras.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep navy blue, silver moonlight, turquoise glow, white foam.
Image size: 1577x653 (about 21:9, wide).
This is the MIDDLE layer: tall elements at medium distance, darker and more saturated than the far layers, but still softer than the gameplay foreground. The bottom of the image is filled along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: tall wet reef rocks covered with seaweed and barnacles, sea spray, silver moonlight on the edges.
```

**`recife_mato.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: deep navy blue, silver moonlight, turquoise glow, white foam.
Image size: 1577x344 (about 4.6:1, wide low strip; if the tool can't do it, use 21:9 and keep the scenery in the bottom third).
This is the NEAREST background layer, right behind the gameplay: low and continuous along the bottom, the DARKEST layer, almost a silhouette with a little rim light. Nothing that looks like a platform, a ledge or spikes. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: coral, seaweed, starfish, shells and wet rocks at the water line.
```

## Praia do Tsunami (fase 11, tempestade) — `praia`

**`praia_ceu.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: stormy grey-green, beige sand, white foam, dark teal sea.
Image size: 1577x672 (about 21:9, wide).
This is the SKY layer, the farthest one: it fills the WHOLE image edge to edge, fully opaque, nothing transparent. No ground and no foreground objects. The bottom edge is the horizon haze. Seamless horizontally: the left and right edges must match perfectly so the image can repeat side by side.
Show: a stormy sky over a tropical beach: heavy dark grey-green clouds, rain streaks, lightning far away, a pale break of light in the upper left.
```

**`praia_ilhas.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: stormy grey-green, beige sand, white foam, dark teal sea.
Image size: 1363x266 (about 5:1, a very wide low strip; if the tool can't do 5:1, use 21:9 and keep the scenery in the bottom quarter).
This is the FARTHEST scenery layer: a long low silhouette on the horizon. Pale, hazy and low contrast, blending into the sky. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: a giant tsunami wave rising on the sea horizon under the storm; the sea fills the bottom of the strip.
```

**`praia_galeao.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: stormy grey-green, beige sand, white foam, dark teal sea.
Image size: 1367x348 (about 4:1, wide strip; if the tool can't do 4:1, use 21:9 and keep the scenery in the bottom third).
This is a FAR-MIDDLE layer: one big landmark group in the distance, a bit hazy and desaturated. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: a beach with straw huts on stilts and a broken wooden pier, palm trees bending in the storm.
```

**`praia_palmeiras.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: stormy grey-green, beige sand, white foam, dark teal sea.
Image size: 1577x653 (about 21:9, wide).
This is the MIDDLE layer: tall elements at medium distance, darker and more saturated than the far layers, but still softer than the gameplay foreground. The bottom of the image is filled along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: palm trees bending hard in the storm wind, leaves flying, rain.
```

**`praia_mato.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: stormy grey-green, beige sand, white foam, dark teal sea.
Image size: 1577x344 (about 4.6:1, wide low strip; if the tool can't do it, use 21:9 and keep the scenery in the bottom third).
This is the NEAREST background layer, right behind the gameplay: low and continuous along the bottom, the DARKEST layer, almost a silhouette with a little rim light. Nothing that looks like a platform, a ledge or spikes. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: sand dunes with beach grass, shells, a broken rowboat and a wooden warning sign.
```

## Rochedo (fase 12, pôr do sol) — `rochedo`

**`rochedo_ceu.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: huge orange and red sunset, purple clouds, red rock.
Image size: 1577x672 (about 21:9, wide).
This is the SKY layer, the farthest one: it fills the WHOLE image edge to edge, fully opaque, nothing transparent. No ground and no foreground objects. The bottom edge is the horizon haze. Seamless horizontally: the left and right edges must match perfectly so the image can repeat side by side.
Show: a huge orange and red sunset sky with purple clouds, a big low sun in the upper left.
```

**`rochedo_ilhas.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: huge orange and red sunset, purple clouds, red rock.
Image size: 1363x266 (about 5:1, a very wide low strip; if the tool can't do 5:1, use 21:9 and keep the scenery in the bottom quarter).
This is the FARTHEST scenery layer: a long low silhouette on the horizon. Pale, hazy and low contrast, blending into the sky. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: the sea horizon at sunset with a tiny lighthouse and sea stacks far away; the sea fills the bottom of the strip, reflecting orange.
```

**`rochedo_galeao.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: huge orange and red sunset, purple clouds, red rock.
Image size: 1367x348 (about 4:1, wide strip; if the tool can't do 4:1, use 21:9 and keep the scenery in the bottom third).
This is a FAR-MIDDLE layer: one big landmark group in the distance, a bit hazy and desaturated. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: tall red rock formations by the sea glowing in the sunset light, one of them shaped like a skull.
```

**`rochedo_palmeiras.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: huge orange and red sunset, purple clouds, red rock.
Image size: 1577x653 (about 21:9, wide).
This is the MIDDLE layer: tall elements at medium distance, darker and more saturated than the far layers, but still softer than the gameplay foreground. The bottom of the image is filled along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: red rock pillars with a few dry twisted trees and long shadows.
```

**`rochedo_mato.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: huge orange and red sunset, purple clouds, red rock.
Image size: 1577x344 (about 4.6:1, wide low strip; if the tool can't do it, use 21:9 and keep the scenery in the bottom third).
This is the NEAREST background layer, right behind the gameplay: low and continuous along the bottom, the DARKEST layer, almost a silhouette with a little rim light. Nothing that looks like a platform, a ledge or spikes. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: red rocks, dry bushes and old bones.
```

## Alto-mar do Kraken (fase 13, tempestade de noite, sem terra nenhuma) — `alto_mar`

**`alto_mar_ceu.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: dark crimson and black storm, red lightning, dark teal sea, white foam.
Image size: 1577x672 (about 21:9, wide).
This is the SKY layer, the farthest one: it fills the WHOLE image edge to edge, fully opaque, nothing transparent. No ground and no foreground objects. The bottom edge is the horizon haze. Seamless horizontally: the left and right edges must match perfectly so the image can repeat side by side.
Show: a violent storm at sea at night: swirling dark red and black clouds, red-tinted lightning, heavy rain, no moon.
```

**`alto_mar_ilhas.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: dark crimson and black storm, red lightning, dark teal sea, white foam.
Image size: 1363x266 (about 5:1, a very wide low strip; if the tool can't do 5:1, use 21:9 and keep the scenery in the bottom quarter).
This is the FARTHEST scenery layer: a long low silhouette on the horizon. Pale, hazy and low contrast, blending into the sky. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: huge stormy waves on the horizon and a giant red tentacle rising from the sea far away; only sea, no land; the sea fills the bottom of the strip.
```

**`alto_mar_galeao.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: dark crimson and black storm, red lightning, dark teal sea, white foam.
Image size: 1367x348 (about 4:1, wide strip; if the tool can't do 4:1, use 21:9 and keep the scenery in the bottom third).
This is a FAR-MIDDLE layer: one big landmark group in the distance, a bit hazy and desaturated. The scenery touches the bottom edge along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient, no shadow on it. Seamless horizontally: left and right edges must match.
Show: a sinking pirate ship tilted in the storm with giant red tentacles wrapped around it, big waves at its base.
```

**`alto_mar_palmeiras.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: dark crimson and black storm, red lightning, dark teal sea, white foam.
Image size: 1577x653 (about 21:9, wide).
This is the MIDDLE layer: tall elements at medium distance, darker and more saturated than the far layers, but still softer than the gameplay foreground. The bottom of the image is filled along the whole width. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: towering dark storm waves with white foam crests, a broken mast and torn sails floating, ropes flying in the wind.
```

**`alto_mar_mato.png`**

```
16-bit pixel art for a side-scrolling 2D platformer game, pirate adventure theme, crisp clean pixels, limited palette, soft painted lighting, light comes from the upper left. No text, no letters, no logos, no people, no UI, no border, no frame. Color palette for this place: dark crimson and black storm, red lightning, dark teal sea, white foam.
Image size: 1577x344 (about 4.6:1, wide low strip; if the tool can't do it, use 21:9 and keep the scenery in the bottom third).
This is the NEAREST background layer, right behind the gameplay: low and continuous along the bottom, the DARKEST layer, almost a silhouette with a little rim light. Nothing that looks like a platform, a ledge or spikes. Everything that is not scenery must be ONE flat solid pure magenta #FF00FF background (it will be cut out), no gradient. Seamless horizontally: left and right edges must match.
Show: choppy dark water close up with floating debris: barrels, planks, a lifebuoy, white foam.
```

