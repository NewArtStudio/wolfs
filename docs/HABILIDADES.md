# Personagens e habilidades

Decidido com o criador (nov/2026), inspirado no **Albion Online**. Todos os números são **[PROTÓTIPO]**.

## Personagens

- Até **8 personagens** por conta, de 3 classes: **Vampiro**, **Lobisomem** e **Caçador de Recompensas**.
- Cada personagem tem a própria classe, **nível (1 a 50)**, XP, habilidades e **equipamento vestido**.
  O **baú e os recursos são da conta** (todos usam), como no Albion.
- No lobby: clique numa vaga livre da grade **MANSÃO** para criar (classe + nome); clique num personagem para escolhê-lo.
  O escolhido é o que vai para a Fazenda no EXPLORAR.
- XP na Fazenda: ovelha **8**, invasor derrotado **120**, extração **60**. Do nível 1 ao 50 ≈ **58 mil XP**
  (`50 + 15·(N−1) + (N−1)²` por nível). Na tela de Habilidades há um botão **+1 nível (teste)** (só no Editor/Development Build).
- O **invasor é sempre de outra classe** (sorteada), com equipamento da sua faixa de poder e o seu nível.

| Classe | Vida | Velocidade | Dano das habilidades de classe |
|---|---|---|---|
| Vampiro | 160 | 6,2 | mágico (INT) |
| Lobisomem | 200 | 6,0 | físico perto (FOR) |
| Caçador de Recompensas | 170 | 6,1 | físico longe (DES) |

## A barra de habilidades (10 teclas)

| Origem | Teclas |
|---|---|
| **Arma** | Clique esq. (ataque básico, fixo) · Clique dir., **Q**, **E** (cada uma com 3 opções) · **F** ultimate (fixo, custa 100 de energia) |
| **Capacete** | **R** (3 opções) |
| **Peito** | **Espaço** (3 opções) |
| **Botas** ("perna") | **Shift** (3 opções) |
| **Classe** | **1** e **2** (as duas ativas que você escolher) + as passivas aprendidas |

As versões EX (Shift + tecla) saíram: o Shift agora é das botas.

## Habilidades de equipamento (estilo Albion)

- Vêm da **família** da peça: todo *Sabre de Cavalaria* (de qualquer faixa) tem o mesmo kit; outra arma tem outro kit e outro ultimate.
- A escolha de cada tecla fica guardada por família e por personagem (tela Habilidades → aba **EQUIPAMENTO**).
- Não têm nível: crescem com o equipamento (atributos, refino, faixa).
- **10 armas:** Varinha, Crucifixo, Revólver, Pistola de Pederneira, Bengala-Espada, Sabre de Cavalaria, Foice de Ceifador,
  Machado de Carrasco, Cajado, Mosquete (+ "mãos nuas" sem arma). Cada uma: 1 básico + 3×3 opções + 1 ultimate = 11.
- **12 armaduras:** 4 estilos (vanguarda, batedor, arcano, sagrado) × capacete/peito/botas, 3 opções cada.
- Total: **111 habilidades de arma + 36 de armadura**.

## Habilidades de classe

- **20 por classe** (60 no total): 10 ativas + 10 passivas, cada uma do **nível 1 ao 10**.
- Cada uma libera num nível do personagem (ativas: 1, 1, 4, 7, 10, 14, 18, 23, 29, 35; passivas: 2, 3, 5, 8, 12, 16, 20, 26, 32, 40).
- **Pontos:** 2 no nível 1 e +1 por nível (51 no nível 50). Com 200 níveis possíveis por classe, dá para maximizar
  ~5 habilidades: cada jogador monta a sua build. "−" devolve o ponto; **Devolver pontos** zera tudo.
- Ativas: +11% de dano/cura/escudo/sangramento por nível e −3% de recarga por nível. Passivas: o valor por nível × nível.
- Personagem novo já sai com as duas primeiras ativas aprendidas e nas teclas 1 e 2.

## Efeitos novos no combate

Sangramento/queimadura (dano por segundo), prender (não anda, mas conjura), marca (recebe mais dano), empurrar e puxar,
escudo temporário (faixa dourada na barra de vida), bônus de dano, remover lentidão/atordoamento, zonas no chão
e armadilhas (o dano das zonas é **por segundo** dentro; ticks de zona/sangramento não disparam contra-ataque). Passivas: dano, vida, velocidade, roubo de vida, recarga, energia, redução de dano, cura recebida,
dano com pouca vida, cura ao matar, sangramento no ataque básico, dano em alvo lento/preso e regeneração.

## Onde mexer

- Números e habilidades: `Tools/Data/build_skill_data.py` → gera `Resources/VampWolfs/skills.json`
  (`python3 Tools/Data/build_skill_data.py Assets/VampWolfs/Resources/VampWolfs/skills.json`).
- As descrições são montadas dos números (`Scripts/Skills/SkillText.cs`): mude o número, não o texto.
- Código: `Scripts/Skills/` (SkillCatalog, SkillFactory, SkillText, ZoneAbility + Zone, TrapAbility + Trap), `Scripts/Gear/CharacterSave.cs`,
  `Scripts/Combat/PassiveStats.cs`, tela `Scripts/Lobby/SkillsScreen.cs`.
