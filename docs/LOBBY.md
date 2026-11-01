# Lobby (Base) — copiado do Space

O lobby é a tela inicial do jogo: a **Base Espacial do projeto Space** copiada para o VampWolfs (out/2026),
com tudo o que dava trocado para o estilo **vitoriano cartunizado**. O que não dava para trocar sozinho
(as imagens PNG) ficou **igual ao Space**, de propósito, para ser substituído depois.

## O que tem

- **Tela inicial** (`Scripts/Lobby/BaseScreen.cs`): painel com o nome do jogo e os atalhos
  **INVENTÁRIO** (abre o Arsenal) e **FAMILIAR** (morcego/corvo que vai recolher o saque — *em breve*);
  à direita o card do **Vampiro** (retrato 3D, classe, patente/XP *em breve*, Habilidades *em breve*,
  Equipamento), o **REGISTRO** (sangue, lã, fragmentos, incursões, extrações, itens no baú, resetar perfil)
  e o botão **EXPLORAR**, que entra **direto na Fazenda** (por enquanto é o único destino).
- **Arsenal** (`ArsenalScreen.cs`): o vampiro no meio do cenário (gira arrastando), os 10 slots em volta
  ligados por linhas, status do personagem e o **Baú** (abas Tudo/Armas/Vestes/Joias/Materiais, busca, ordenar).
  Arraste do baú para o slot (ou duplo clique) para vestir; do slot para a grade para tirar;
  arraste um lingote/cristal/chave/gema até uma peça para melhorar (janela com a prévia e a chance de quebrar).
- **Palco** (`BaseStage.cs`): o fundo é uma pintura; só o vampiro é 3D, com sombra no chão pintado e luz própria.
- **Retratos** (`PortraitStudio.cs`): estúdio escondido que fotografa o vampiro (card e Arsenal).

Atalhos na Base: **I** abre/fecha o Arsenal, **Esc** fecha janelas, **Enter** = EXPLORAR.
Arraste o mouse numa área vazia da tela para **girar o vampiro** no palco. O tamanho e a posição dele no palco ficam no
Inspector do GameBootstrap (`LobbyCharacterScale`, `LobbyCharacterForward`).

## O que já está no estilo vitoriano

- Cores: painéis vinho/nogueira, texto pergaminho, acento latão (antes azul-aço e âmbar) — feito por
  `Tools/Lobby/victorian_restyle.py` a partir do `Space.uss`; molduras (FrameBox) com fundo vinho.
- Fontes: **IM Fell English SC** (títulos, tipografia de época) e **Crimson Text** (texto) — licença OFL, em
  `Resources/Lobby/Fonts/`.
- Textos: "Londres · Inverno de 1888", Mansão/Baú, nomes vitorianos dos equipamentos e estilos
  (Vanguarda, Batedor, Arcano, Sagrado), ícones novos de lua, morcego e da classe Vampiro.

## O que ficou igual ao Space (trocar depois)

| Arquivo | O que é | Trocar por |
|---|---|---|
| `Resources/Base/BaseBackdrop.png` | fundo do palco (hangar espacial) | pintura vitoriana (salão da mansão, cripta…) na mesma resolução; ajustar `BaseBackdrop_calib.txt` |
| `Resources/Lobby/Textures/explore_button.png` | botão EXPLORAR (laranja industrial) | botão/placa de latão |
| `Resources/ItemIcons/*.png` | ícones pintados dos equipamentos | ícones vitorianos com o mesmo nome (id do equipamento) |
| Ícones vetoriais de peças (`GearIcons.cs`) | desenhos sci-fi das peças | desenhos vitorianos (ou só os PNGs acima) |
| Cantos chanfrados das molduras (`FrameBox.cs`) | visual "painel de nave" | cantos ornamentados, se quiser |

## Como funciona por dentro

- `LobbyUI` (MonoBehaviour criado pelo `GameBootstrap`) monta o painel do UI Toolkit por código, guarda o perfil,
  mostra avisos e liga/desliga o lobby ao entrar e sair da Fazenda. Sem `Resources/Lobby/Lobby.uss` o jogo usa a
  Base antiga em IMGUI (`Scripts/UI/BaseScreen.cs`).
- As telas só chamam o `PlayerProfile` (vestir, tirar, materiais) e o `GameBootstrap` (EXPLORAR); as regras não mudaram.
