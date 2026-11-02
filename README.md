# VampWolfs — protótipo 1x1 (Vampiro vs Lobisomem)

Protótipo de **combate estilo Battlerite** + **loop estilo Brumas de Avalon (Albion)**, em Unity 6 / URP.

O jogo abre no **lobby** (cópia da Base do projeto Space em estilo vitoriano: cards dos personagens, Arsenal com baú, equipamento, refino, cristais e gemas — veja [`docs/LOBBY.md`](docs/LOBBY.md) e [`docs/EQUIPAMENTO.md`](docs/EQUIPAMENTO.md)). Crie vampiros, lobisomens e caçadores de recompensas, cada um com nível 1–50 e 20 habilidades de classe, e as habilidades vindas das armas e armaduras no estilo Albion — veja [`docs/HABILIDADES.md`](docs/HABILIDADES.md). Em **EXPLORAR**, você entra direto na Fazenda com o personagem escolhido, farmando ovelhas numa fazenda à noite que é **diferente a cada partida** (mapa procedural de 6×6 tiles — veja [`docs/MAPA.md`](docs/MAPA.md)). Depois de 45 s um **invasor (IA) de outra classe** invade o mapa.
Quem morre perde **só a bag** (o que foi farmado na partida) — o **equipamento nunca cai**.
Para garantir o loot, fique 4 s no **portal roxo de extração** (levar dano interrompe).

---

## Como abrir

Requisitos: **Unity 6.3** (6000.3.x) pelo Unity Hub e **GitHub Desktop**. O repositório é o projeto Unity completo (URP já configurado).

1. GitHub Desktop: **File → Clone repository → NewArtStudio/wolfs** (escolha uma pasta vazia).
2. Unity Hub: **Add → Add project from disk** → a pasta clonada.
3. Na Unity: **VampWolfs → Open And Play Farm Prototype**.

Para receber atualizações: **feche a Unity**, no GitHub Desktop **Fetch origin → Pull origin**, e abra a Unity de novo.

### Modelos 3D dos personagens

Coloque o FBX e as texturas em `Assets/VampWolfs/Art/Characters/<Personagem>/` (veja o README dessa pasta).
O menu **VampWolfs → Open And Play** (ou **Refresh Character Art**) encontra o modelo e as texturas (`*basecolor*`, `*normal*`) sozinho.
Altura e rotação de cada modelo ficam no Inspector do objeto **GameBootstrap** (`Height`, `YawOffset`).
Sem modelo na pasta, o personagem usa o boneco de primitivas.

---

## Controles (Battlerite)

| Tecla | Ação |
|---|---|
| WASD | mover |
| Mouse | mirar (quase tudo é skillshot) |
| LMB (segurar) | ataque básico da **arma** — sem cooldown, gera energia |
| RMB / Q / E | habilidades da **arma** (cada tecla com 3 opções, escolha no lobby) |
| F | ultimate da **arma** (custa 100 de energia) |
| R / Espaço / Shift | habilidade do **capacete** / **peito** / **botas** |
| 1 / 2 | habilidades da **classe** (as duas ativas escolhidas no lobby) |
| C | cancelar conjuração (devolve cooldown e energia) |
| H | mostrar/esconder ajuda |
| Rodinha do mouse | aproximar/afastar a câmera |
| I | ver equipamento e atributos (na Base abre o Arsenal) |
| Esc | fechar o Arsenal / a janela de melhoria (Base) |
| Enter | EXPLORAR (Base) / voltar à Base (tela final) |

Todos os números (tempo de invasão, % da bag que cai, dificuldade da IA…) ficam no **Inspector do objeto GameBootstrap**. Os números das habilidades ficam em `VampireKit.cs` e `WerewolfKit.cs`.

---

## Estrutura

Tudo é montado por código (sem prefabs), para o protótipo funcionar sem configurar cena.

```
Assets/VampWolfs/
  Editor/VampWolfsMenu.cs        menu "VampWolfs" (cria a cena e dá Play)
  Scripts/
    Core/        GameBootstrap (ponto de entrada), MatchManager (regras), MatchSettings (números),
                 InputReader (teclas), TopDownCamera, MaterialLibrary, Shapes (arte provisória)
    Combat/      CombatUnit (personagem: movimento + conjuração), Health, Energy, StatusEffects,
                 DamageSystem (dano/energia/lifesteal), Projectile, TelegraphIndicator, Effects
    Skills/      Ability (base) + tipos genéricos: Projectile, Melee, Area, Dash, Counter, Buff
    Characters/  VampireKit, WerewolfKit (números das skills), CharacterFactory, UnitVisual, PlayerController
    AI/          WerewolfAI (usa a mesma API do jogador: mesmos cooldowns e telegraphs)
    World/       FarmBuilder (luz/seed), FarmGenerator + FarmPlan + FarmTileCatalog + FarmPieces (mapa procedural), WalkGrid (navegação), Sheep, ExtractionPoint, Obstacle
    Loot/        Inventory (a BAG, em risco), Equipment (seguro), LootBag (bag no chão)
    UI/          GameHUD (IMGUI), FloatingText, BaseScreen (Base antiga em IMGUI, reserva se o lobby faltar)
    Vfx/         efeitos visuais (motor do Space + receitas das habilidades) — veja docs/VFX.md
    Lobby/       lobby copiado do Space (UI Toolkit): LobbyUI, BaseScreen, ArsenalScreen, BaseStage, PortraitStudio…
  Resources/Lobby/   Lobby.uss (estilos), fontes vitorianas, botão EXPLORAR
  Resources/Base/    fundo pintado do palco (ainda o hangar do Space) + shaders
  Resources/ItemIcons/  ícones pintados dos equipamentos (ainda os do Space)
```

Design e decisões: [`docs/DESIGN.md`](docs/DESIGN.md).
