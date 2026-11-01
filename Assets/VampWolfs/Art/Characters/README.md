# Skins dos personagens

Uma pasta por personagem. Coloque aqui o **FBX** e as **texturas** de cada um:

| Pasta | Personagem |
|---|---|
| `Werewolf/` | Lobisomem |
| `Vampire/` | Vampiro |
| `MonsterHunter/` | Caçador de monstros |

## O que o FBX precisa ter

- **Rig humanoide** (esqueleto com quadril, coluna, braços, pernas e cabeça). É isso que permite os 3 personagens usarem as mesmas animações.
  - Se o modelo veio **sem esqueleto** (por exemplo, gerado por IA ou comprado "static"), passe pelo **auto-rigger do Mixamo** antes: faça upload, posicione os marcadores e baixe com skin.
- Pose de **T** ou **A**.
- Escala real: o personagem deve ter ~1,8 m (o lobisomem pode ter ~2,2 m).
- Texturas na mesma pasta (Albedo/BaseColor, Normal e, se houver, Metallic/Roughness e Emission).

## Nomes sugeridos

```
Werewolf/Werewolf.fbx
Werewolf/Werewolf_BaseColor.png
Werewolf/Werewolf_Normal.png
```

## Como subir pelo site do GitHub

Abra a pasta do personagem no GitHub → **Add file → Upload files** → arraste o FBX e as texturas → **Commit changes**.

Limites do GitHub: até **25 MB por arquivo** pelo site e **100 MB** pelo git. Se o FBX for maior, me avise que eu configuro o Git LFS.
