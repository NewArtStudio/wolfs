# Efeitos visuais (VFX)

Desde nov/2026 o VampWolfs usa o **motor de efeitos do projeto Space** (partículas, anéis e cones no chão, manchas,
feixes, clarões de luz e tremor de câmera), copiado para `Scripts/Vfx/` e `Resources/VFX/`, com receitas novas
para as habilidades do vampiro e do lobisomem. Sem os arquivos de `Resources/VFX`, o jogo volta sozinho para os
efeitos simples antigos (`Effects`).

## Como está organizado

| Arquivo | O que faz |
|---|---|
| `Vfx/VfxSystem.cs`, `VfxAssets.cs`, `VfxMeshes.cs` | motor copiado do Space (12 emissores de partícula compartilhados + morcegos, objetos reaproveitáveis, luzes, tremor) |
| `Vfx/VfxHub.cs` | cria os efeitos de cada partida e o pós-processamento (bloom, vinheta, tonemapping neutro) |
| `Vfx/SkillFx.cs` | **as receitas**: o que cada habilidade mostra ao conjurar, disparar, acertar, cair no chão… |
| `Vfx/UnitFx.cs` | o que acompanha o personagem: carga na mão, rastro do dash, escudos, estrelas do atordoamento, anel do time, poeira dos passos, sangue, cura e morte |
| `Vfx/ProjectileFx.cs` | projétil brilhante com rastro (no lugar da esfera lisa) |
| `Vfx/InkOutline.cs` | contorno de "tinta" nos personagens (visual de desenho animado; inimigo com tinta vermelho-escura) |
| `Vfx/AmbientFx.cs` | vaga-lumes, névoa roxa rente ao chão e faíscas do portal de extração |
| `Combat/TelegraphIndicator.cs` | telegrafias com o shader de chão do Space (borda brilhante, faixas e o preenchimento crescendo com o cast) |

## Receitas (campo `Fx` de cada habilidade nos kits)

| Vampiro | Fx | Visual |
|---|---|---|
| Seta de Sangue | `blood_bolt` | orbe vermelho com gotas caindo; respingo e mancha de sangue no acerto |
| Lança Carmesim | `lance` | risco de luz + brasas e fumaça escura; estrela, golpe de luz e tremor no acerto |
| EX Lança Perfurante | `lance_ex` | magenta com estilhaços de cristal |
| Passo da Névoa | `mist_step` | nuvem de névoa violeta na saída, no caminho e na chegada |
| EX Revoada de Morcegos | `bats` | revoada de morcegos (textura nova) |
| Olhar Hipnótico | `gaze` | orbe com espiral hipnótica; anel e estrela no acerto |
| Espelho de Sangue | `mirror` | escudo de vitral vermelho enquanto dura; estilhaça ao contra-atacar |
| Nova Sanguínea | `nova` | dois anéis, faíscas em círculo, fumaça e poça de sangue |
| Banquete Carmesim (ult.) | `feast` | projétil de fogo-sangue com morcegos; explosão, tremor forte |

| Lobisomem | Fx | Visual |
|---|---|---|
| Garras | `claws` | varredura no chão + 3 riscos de garra |
| Bote (e EX) | `pounce` | poeira na saída; anel, rachadura no chão, pedrinhas e tremor ao cair |
| Investida (e EX) | `rush` | rastro de poeira |
| Uivo | `howl` | 3 ondas pálidas, bafo e poeira |
| Couro Grosso | `hide` | escudo de aço enquanto dura |
| Dilacerar | `rend` | varredura vermelha + 4 riscos, tremor |
| Lua de Sangue (ult.) | `blood_moon` | círculo de runa no chão, ondas, brasas e tremor forte |

Para uma habilidade nova: dê um nome em `Fx` e escreva o `case` em `SkillFx`. Sem `Fx`, ela usa um efeito genérico na cor dela.

## Texturas vitorianas

`Rune` (círculo ocultista com pentagrama), `Hex` (treliça de vitral) e `Bat` (morcego batendo as asas, folha 2×2)
foram refeitas por `Tools/TexGen/victorian_vfx_textures.py`. As outras (faísca, fumaça, fogo, sangue, golpe…) são as do Space.

## Câmera

`MatchSettings.CameraZoom` (padrão 1,35) afasta a câmera mantendo o ângulo; a **rodinha do mouse** aproxima/afasta
dentro de `CameraZoomMin`–`CameraZoomMax`. A névoa acompanha o zoom.
