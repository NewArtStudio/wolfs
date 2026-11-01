# Equipamento e atributos

Sistema copiado do projeto **Space** (`NewArtStudio/Space`) e adaptado ao VampWolfs. Os nomes foram trocados para a Europa vitoriana. Tudo marcado [PROTÓTIPO] pode ser ajustado.

## Fluxo (jogo de extração)

1. O jogo abre na **Base**. Lá estão o equipamento vestido, o baú, as melhorias e os atributos calculados.
2. **Entrar na Fazenda** inicia a partida.
3. O que você **veste nunca cai**. O que encontra vai para a **bag**, que está em risco.
4. Se **extrair**, a bag vai para o **baú**. Se **morrer**, perde a bag (60% cai no chão para quem te matou).
5. O progresso fica salvo em `Application.persistentDataPath/vampwolfs_profile.json`. O botão "Recomeçar perfil" apaga esse arquivo.

## Igual ao Space

- **10 slots:** chapéu/elmo, casaco/couraça, luvas, botas, mão direita, mão esquerda, relíquia e 3 joias.
- **307 equipamentos**, divididos assim:
  - 8 faixas (I a VIII);
  - 4 estilos: vanguarda (armadura), batedor (evasão), arcano (égide) e sagrado.
- **Atributos FOR/DES/INT/VIT:** cada peça tem uma base fixa, um **teto por atributo** e um **total de pontos**. O jogador adiciona pontos com **tônicos**.
- **Refino +1 a +10.** Armas usam o *Lingote de Aço de Sheffield* e armaduras os *Rebites de Latão Temperado*. Joias não refinam.
- **Encaixes de carta (0 a 4)**, abertos pela *Chave de Relojoeiro*, e **encaixe de gema**, aberto pela *Pinça de Joalheiro*.
- **Fórmulas no estilo Ragnarok:**
  - ATK = A + ⌊A/10⌋² (FOR para corpo a corpo, DES para distância);
  - MATK pela INT;
  - defesa ⌊VIT/2⌋;
  - defesa mágica INT + ⌊VIT/2⌋.

## Diferente do Space (decisões do criador)

| Space | VampWolfs | Por quê |
|---|---|---|
| HIT/FLEE: golpes podem errar por sorte | **Removido.** DES deixa a conjuração mais rápida (até 50% do tempo) e o ataque básico também | Combate de mira: se acertou, acertou |
| Evasion = % de esquiva | Evasion = **+velocidade** (0,3% por ponto) e **+redução física** (0,5% por ponto) | Mesmo motivo |
| Energy Shield | **Égide**: absorve dano antes da vida (0,1% da vida por ponto) e se recarrega 3 s depois do último dano | Tema vitoriano |
| Refino linear, sem falha | **Retorno decrescente** (+5% nos primeiros níveis, +2% nos últimos). Do +5 em diante, a falha **quebra a peça** (20%, 30%, 40%, 55%, 65%, 75%) | Ralo da economia, estilo Ragnarok |
| Gema dá aura de ameaça (tank) | Gema dá **+3 num atributo**, acima do teto (Rubi FOR, Esmeralda DES, Safira INT, Âmbar VIT) | Não existe "aggro" no PvP |
| Restrição de classe nas armas pesadas | Sem restrição por enquanto | As classes do VampWolfs ainda não estão fechadas |

## Escala de poder

- O dano é multiplicado por `0,5 + 0,5 × (dano da arma + ATK do atributo) / poder de referência da faixa I`.
- A vida é multiplicada pela faixa média do equipamento vestido e por (1 + VIT%).
- Com isso, **lutas entre jogadores da mesma faixa têm duração parecida da faixa I à VIII**: são cerca de 30 golpes do ataque básico em qualquer faixa, conforme os testes fora da Unity.
- O lobisomem invasor é equipado na **faixa de poder do jogador**.
- Monstros e ovelhas não escalam: equipamento alto mata ovelha rápido.

## Drops

- **Ovelha:** 4% de chance de um equipamento da faixa I e 12% de um material.
- **Lobisomem:** carrega 2 equipamentos e 2 materiais da faixa do jogador.

## Arquivos

- `Scripts/Gear/` contém:
  - `GearCatalog`: o catálogo de itens;
  - `CharacterStats`: as fórmulas e os números de balanceamento (`GearBalance`);
  - `GearUpgrades`: refino, tônicos, chaves e gemas;
  - `PlayerProfile`: o perfil salvo;
  - `GearLoadouts`: kit inicial, invasor e drops.
- `Scripts/UI/BaseScreen.cs` é a tela da Base.
- `Resources/VampWolfs/gear.json` é o catálogo, gerado por `Tools/Data/build_gear_data.py` a partir do Space. Para mudar nomes ou regras de refino, edite o script e rode de novo:
  `python3 Tools/Data/build_gear_data.py <pasta do Space> Assets/VampWolfs/Resources/VampWolfs/gear.json`

## Pendente

- Efeito das **cartas** (o encaixe já abre; as cartas em si ainda não existem, igual ao Space).
- Classes jogáveis além do vampiro (lobisomem e caçador) e restrições de arma por classe.
- Reset de pontos de atributo, mercado e reparo.
