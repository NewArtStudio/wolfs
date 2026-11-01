# VampWolfs — Design do protótipo 1

## Visão

Tema: **vampiros × lobisomens × humanos**. Gameplay = **combate de arena do Battlerite** dentro de um **loop de risco/extração das Brumas de Avalon (Albion)**.

Este protótipo valida só duas perguntas:

1. **O combate é bom?** Mirar, esquivar de telegraphs, gerenciar energia e cooldowns num 1x1.
2. **O loop de risco funciona?** Farmar → ser invadido → lutar ou fugir → perder/ganhar só a bag → extrair.

Fora de escopo agora: online, grupos, humanos, progressão, crafting, arte final.

---

## O que vem do Battlerite

Pesquisa (resumo):

- Câmera top-down; **WASD move e o mouse mira**. Praticamente toda habilidade que não é em si mesmo/aliado é **skillshot** — não existe clique-no-alvo.
- Kit: **ataque básico (M1)** + 5 habilidades com cooldown + **Ultimate**.
- **Energia** em vez de mana: começa em 0 e enche **causando dano**. É dividida em 4 barras de 25.
  - Gasta em **versões EX** (Shift + habilidade): variantes mais fortes ou diferentes que **dividem o cooldown** com a versão normal (25–50 de energia).
  - Gasta numa **habilidade de energia** (tecla R).
  - Ou guardada inteira para o **Ultimate** (consome 100). O dilema "gastar em EX agora ou guardar o ultimate" é central.
- M1 é a principal fonte de dano e de energia.
- Habilidades têm **tempo de conjuração** com indicador; dá para **cancelar** (tecla C).
- **Counters** (janela curta em que o próximo golpe é anulado e o atacante punido) e **invulnerabilidades** são o núcleo do jogo de leitura.
- Arenas pequenas com **paredes que bloqueiam linha de visão** e projéteis.

Como foi aplicado: `CombatUnit` (conjuração com cast time, movimento reduzido durante o cast, cancelamento), `Energy` (4×25), `Ability.Ex` (compartilha cooldown), `CounterAbility`, `TelegraphIndicator` (todo cast inimigo desenha a área no chão), `Obstacle` (cercas, feno, celeiro e árvores bloqueiam projéteis).

## O que vem das Brumas de Avalon (Albion)

- Instância pequena para **solo/dupla**, com mobs, recursos e **outros jogadores que podem aparecer** (aqui: o lobisomem invade).
- Você sai por **pontos de saída** do mapa (aqui: portal de extração).
- **Diferença intencional:** no Albion as Brumas letais são **full loot** (cai tudo, inclusive equipamento). Aqui **cai só a bag** (o que foi farmado na partida) e **o equipamento fica**. Isso diminui a punição para atrair jogadores casuais, mas mantém o risco real de cada partida. A regra parece com a das Abyssal Depths do Albion (perde o inventário, equipamento seguro).

### Regras do loot neste protótipo

- Matar ovelha → itens entram direto na **bag** (Sangue, Lã, raramente Fragmento Lunar).
- Morreu → a bag inteira sai do personagem; **60%** dela cai no chão como uma bolsa (`MatchSettings.BagDropShare`), o resto é destruído. Quem passar por cima pega.
- O invasor chega com loot próprio (farmado "em outro lugar") — é o motivo para lutar em vez de só fugir.
- **Extração**: 4 s parado no portal; levar dano reinicia a contagem. Só o que foi extraído "conta".

Pergunta de design em aberto: dropar 60% e destruir 40% cria um "imposto" que esfria o mercado de loot (bom para a economia) mas frustra quem vence. Testar 100% vs 60%.

---

## Kits (primeiro balanceamento)

### Vampiro — mago de sangue à distância (160 HP, vel. 6,2)

| Tecla | Habilidade | Resumo |
|---|---|---|
| LMB | Seta de Sangue | projétil, 9 dano, 20% lifesteal, cast 0,3 s |
| RMB | Lança Carmesim | projétil longo, 18 dano, cast 0,75 s, cd 4 s |
| Shift+RMB | EX Lança Perfurante | 25 energia: perfura, 22 dano, lentidão 50% |
| Space | Passo da Névoa | dash 6 m, cd 8 s |
| Shift+Space | EX Revoada de Morcegos | 25 energia: dash 8 m + 0,8 s invulnerável |
| Q | Olhar Hipnótico | projétil, stun 1 s, cd 10 s |
| E | Espelho de Sangue | counter 1,2 s: anula o golpe, atordoa o atacante, cura 12 |
| R | Nova Sanguínea | 25 energia: área ao redor, 14 dano + lentidão, cura 10 |
| F | Banquete Carmesim | 100 energia: projétil perfurante grande, 32 dano, 60% lifesteal |

### Lobisomem — brigão corpo a corpo (200 HP, vel. 6,0) — IA

| Tecla | Habilidade | Resumo |
|---|---|---|
| LMB | Garras | arco de 110°, 11 dano |
| RMB | Bote | salto até 7 m, aterrissa: 14 dano + lentidão |
| Shift+RMB | EX Bote Selvagem | 25 energia: aterrissa com stun 0,8 s |
| Space | Investida | dash 5 m |
| Q | Uivo | área 4 m, lentidão 50% 2 s |
| E | Couro Grosso | -60% dano por 2,5 s |
| R | Dilacerar | 25 energia: golpe 140°, 22 dano |
| F | Lua de Sangue | 100 energia: área 20 dano, cura 30, +40% velocidade |

A matchup é intencionalmente assimétrica: o vampiro precisa manter distância e usar o counter quando o lobisomem pula; o lobisomem precisa acertar o bote.

### IA do lobisomem

Usa exatamente a mesma API do jogador (mesmos cooldowns, cast times e telegraphs). Decide a cada ~0,25 s: bote quando está a média distância, garras/dilacerar de perto, couro grosso quando está com pouca vida e o jogador está conjurando, ultimate com energia cheia e de perto. Tem chance de 45% de dar dash para o lado quando um projétil vai acertá-lo. Dificuldade ajustável em `MatchSettings`.

---

## Arquitetura (para outro dev)

- **Personagem não lê input.** `CombatUnit` recebe `MoveInput`, `AimPoint` e `TryCast()`. `PlayerController` (teclado/mouse) e `WerewolfAI` escrevem nesses campos. Isso permite trocar por rede depois sem reescrever o combate.
- **Habilidades são dados.** 6 tipos genéricos (`ProjectileAbility`, `MeleeAbility`, `AreaAbility`, `DashAbility`, `CounterAbility`, `BuffAbility`) configurados com números nos kits. Personagem novo = kit novo, quase sem código novo.
- **Um lugar para dano:** `DamageSystem.Deal` resolve dano, energia ganha e lifesteal. `HitEffect` padroniza o que acontece ao acertar (dano, lentidão, stun).
- **Arte provisória isolada:** `Shapes`, `UnitVisual`, `FarmBuilder`. Trocar por modelos reais não mexe na lógica.
- **HUD em IMGUI** só para o protótipo (zero configuração). Substituir por UI Toolkit/uGUI depois.

## Próximos passos sugeridos

1. Jogar e ajustar números (cast times, dano, raio do bote) até o 1x1 ficar justo.
2. Arte: modelos e animações (personagem + Animator ligado a `UnitVisual`).
3. Rede: Netcode for GameObjects ou Fish-Net, servidor autoritativo — o desenho com `MoveInput/AimPoint/TryCast` já ajuda.
4. Humanos como terceira facção (caçadores: armadilhas, prata contra lobisomem, luz contra vampiro).
5. Mapa maior com várias "salas" e saídas, grupos 2x2 / 3x3, invasão por outros jogadores.
6. Equipamento com stats reais e itens de Fragmento Lunar.
