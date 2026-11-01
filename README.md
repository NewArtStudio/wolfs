# VampWolfs — protótipo 1x1 (Vampiro vs Lobisomem)

Protótipo de **combate estilo Battlerite** + **loop estilo Brumas de Avalon (Albion)**, em Unity 6 / URP.

Você é o **Vampiro**, farmando ovelhas numa fazenda à noite. Depois de 45 s um **Lobisomem (IA)** invade o mapa.
Quem morre perde **só a bag** (o que foi farmado na partida) — o **equipamento nunca cai**.
Para garantir o loot, fique 4 s no **portal roxo de extração** (levar dano interrompe).

> ⚠️ **Código ainda não executado na Unity.** Foi escrito sem o editor e verificado por compilação contra uma réplica da API da Unity e por revisão. Espere pequenos ajustes no primeiro Play — me mande o erro do Console que eu corrijo.

---

## Como abrir (10 minutos)

Requisitos: **Unity 6** (6000.x) pelo Unity Hub, e git (ou GitHub Desktop).

1. **Unity Hub → New project → template "Universal 3D"** → nome `wolfs` → Create. Quando abrir, **feche a Unity**.
2. Na pasta do projeto criado, rode:
   ```bash
   git init -b main
   git remote add origin https://github.com/NewArtStudio/wolfs.git
   git pull origin main
   ```
   Isso traz a pasta `Assets/VampWolfs` para dentro do projeto URP já configurado.
3. Abra o projeto na Unity. No menu do topo: **VampWolfs → Open And Play Farm Prototype**.
   (Ou **VampWolfs → Create Farm Prototype Scene** e depois Play.)
4. Opcional: faça commit da pasta inteira do projeto (`Packages/`, `ProjectSettings/`) para o repositório virar o projeto completo:
   ```bash
   git add . && git commit -m "Add Unity project settings" && git push -u origin main
   ```

Sem GitHub? Baixe o ZIP do repositório e copie `Assets/VampWolfs` para dentro do `Assets/` do projeto URP.

**Por que não abrir o repositório direto?** A configuração do URP (asset do pipeline, renderer) é gerada pelo template da Unity. Criar o projeto pelo template e trazer o código é o caminho mais seguro; depois do passo 4 o repositório já abre direto.

---

## Controles (Battlerite)

| Tecla | Ação |
|---|---|
| WASD | mover |
| Mouse | mirar (quase tudo é skillshot) |
| LMB (segurar) | ataque básico — sem cooldown, gera energia |
| RMB | Lança Carmesim · **Shift+RMB** (ou 1) = versão EX |
| Space | Passo da Névoa (dash) · **Shift+Space** (ou 2) = versão EX |
| Q | Olhar Hipnótico (stun) |
| E | Espelho de Sangue (counter) |
| R | Nova Sanguínea (custa 25 de energia) |
| F | Ultimate — Banquete Carmesim (custa 100) |
| C | cancelar conjuração (devolve cooldown e energia) |
| H | mostrar/esconder ajuda |
| Enter | jogar de novo (na tela final) |

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
    World/       FarmBuilder (mapa), Sheep, ExtractionPoint, Obstacle
    Loot/        Inventory (a BAG, em risco), Equipment (seguro), LootBag (bag no chão)
    UI/          GameHUD (IMGUI), FloatingText
```

Design e decisões: [`docs/DESIGN.md`](docs/DESIGN.md).
