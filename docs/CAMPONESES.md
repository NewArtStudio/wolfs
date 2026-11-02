# Camponeses e baús

Decidido com o criador (nov/2026). Todos os números são **[PROTÓTIPO]** e ficam em
`GameBootstrap → Settings → Peasants & chests` (Inspector) ou em `Scripts/Characters/PeasantKit.cs`.

## Regra principal

**Neste mapa da Fazenda tudo dropa T1:** baús e camponeses. O tier mora numa configuração (`Map Loot Tier = 1`),
para mapas futuros (outras zonas) darem tiers maiores sem mexer em código.

## Baús (o loot principal)

- **6 baús por mapa**, cada um com um grupo de **2 a 4 camponeses** de guarda (baú raro: +1 guarda).
- **¼ dos baús é raro** (dourado, luz amarela forte).
- **Para abrir:** fique **parado** ao lado do baú (até ~1,9 m). Leva **3 s** (comum) ou **5 s** (raro).
  Andar, conjurar ou **levar dano** reinicia. Uma barra "Abrindo baú…" aparece no meio da tela.
- **O que dá** (vai para a **bag**: em risco até você extrair):

| | Equipamentos T1 | Materiais | Extra |
|---|---|---|---|
| Comum | 2–3 | 1–2 | — |
| Raro | 4–5 (60% com refino +1 a +3) | 2–3 | 2 fragmentos de lua |

- Lugares sorteados com o mapa: em chão aberto e alcançável, fora das estradas e do lago, a ≥ 20 m de onde
  você nasce, ≥ 12 m do portal e espalhados (≥ 22 m entre si).
- O HUD mostra **o baú fechado mais perto** e quantos ainda estão fechados.

## Camponeses

- Camponeses e camponesas, com uma de 3 armas:

| Arma | Vida | Velocidade | Ataque |
|---|---|---|---|
| **Adaga** | 60 | 5,2 | facada rápida, 1,9 m, recarga 0,5 s |
| **Arado** | 75 | 4,6 | golpe largo de 2,8 m com **aviso no chão** (0,75 s), **empurra** 2,5 m, recarga 2,4 s |
| **Espingarda** | 50 | 5,2 | leque de **5 bagos** até 10 m (0,6 s para mirar), recarga 3,2 s; foge se você chegar perto |

  O dano das armas é cortado pela metade porque camponês não veste equipamento (regra do `DamageSystem`).
- **Comportamento (estilo Albion):**
  - os guardas ficam em volta do baú; atacam quem chega a **9 m** e **chamam o grupo inteiro**;
  - perseguem até **18 m** da casa, depois desistem, voltam e **curam tudo**;
  - **4 camponeses andam sozinhos** pelo mapa (alcance de visão um pouco menor);
  - atacam **você e o invasor**. O invasor revida quando um camponês bate nele de perto e você está longe:
    dá para **atrair o invasor para os guardas**.
- **Drop:** só **equipamento T1**, **25%** de chance por camponês. Nada mais.
- **XP:** camponês 15 · baú comum 25 · baú raro 60.
- Sobre eles aparece só uma **barra de vida pequena** (sem nome), e só quando estão a menos de 22 m.

## Onde está no código

- `Characters/PeasantKit.cs`: as 3 armas (vida, velocidade, ataques).
- `World/Peasant.cs`: cria o camponês (visual, grupo) e trata morte/drop.
- `AI/PeasantAI.cs`: o cérebro (guardar, avisar o grupo, perseguir, voltar e curar, passear).
- `AI/NavAgent.cs`: o "GPS" compartilhado pela IA do invasor e dos camponeses.
- `World/LootChest.cs`: o baú (canalização, loot, luz, tampa que abre).
- `World/FarmGenerator.cs → PickChestSpots`: onde os baús ficam.
- `Core/MatchManager.cs → SpawnChestsAndPeasants`: põe baús, guardas e andarilhos na partida.
- `Combat/Team.cs`: novo lado `Peasants`, inimigo de todos.
