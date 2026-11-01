# Auto-rig de personagens (Blender)

`autorig.py` coloca um esqueleto humanoide (nomes no padrão Humanoid da Unity) num modelo estático
vindo do Tripo ou similar: em pé (Z para cima no Blender), de frente para -Y, braços para baixo.

```bash
pip install bpy            # Blender como módulo Python (Python 3.13)
python3 autorig.py -- entrada.fbx saida_Rigged.fbx previa --test-pose
# mão levantada na frente do peito? adicione --left-hand-front ou --right-hand-front
```

Gera `previa_rest_front.png` / `previa_pose_front.png` para conferir o resultado.
Os originais sem esqueleto ficam em `ArtSource/`, fora da pasta Assets (a Unity não importa).
