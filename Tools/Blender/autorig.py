"""
Auto-rig a static, front-facing humanoid mesh (Tripo export: Z up, facing -Y, arms down).
Usage: python3 autorig.py -- <in.fbx> <out.fbx> <preview_prefix> [--test-pose]
Bone names follow Unity's Humanoid naming so the avatar maps automatically.
"""
import bpy, sys, math, bmesh
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
src, out, preview = args[0], args[1], args[2]

for o in list(bpy.data.objects):
    bpy.data.objects.remove(o, do_unlink=True)
bpy.ops.import_scene.fbx(filepath=src)
mesh = [o for o in bpy.context.scene.objects if o.type == 'MESH'][0]

# Apply any object transform so vertex coordinates are world coordinates.
bpy.context.view_layer.objects.active = mesh
mesh.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

verts = [v.co.copy() for v in mesh.data.vertices]
H = max(v.z for v in verts)
minz = min(v.z for v in verts)
H = H - minz


def band(z0, z1):
    return [v for v in verts if z0 * H <= v.z - minz <= z1 * H]


def centroid(pts):
    if not pts:
        return Vector((0, 0, 0))
    s = Vector((0, 0, 0))
    for p in pts:
        s += p
    return s / len(pts)


def extreme_side(pts, sign, fraction=0.03):
    """Average of the most extreme points along +X (sign=1) or -X (sign=-1)."""
    side = sorted([p for p in pts if p.x * sign > 0], key=lambda p: -p.x * sign)
    if not side:
        return None
    n = max(5, int(len(side) * fraction))
    return centroid(side[:n])


def z(f):
    return minz + f * H


# ---- torso center line (x≈0, y from the mesh at that height)
def torso_center(f, half_width=0.08):
    pts = [v for v in band(f - 0.02, f + 0.02) if abs(v.x) < half_width * H]
    c = centroid(pts)
    return Vector((0.0, c.y, z(f)))


hips = torso_center(0.50)
spine = torso_center(0.58)
chest = torso_center(0.68)
neck = torso_center(0.82, 0.05)
head = torso_center(0.87, 0.06)
head_top = Vector((head.x, head.y, z(1.0)))

# ---- feet: boots near the ground, split left (+X) / right (-X)
ground = band(0.0, 0.05)
feet = {}
for sign in (1, -1):
    pts = [p for p in ground if p.x * sign > 0.01 * H]
    c = centroid(pts)
    front = min(pts, key=lambda p: p.y) if pts else c  # most -Y = toe tip
    feet[sign] = (Vector((c.x, c.y + 0.02 * H, z(0.05))), Vector((c.x, front.y + 0.03 * H, z(0.01))))

# ---- arms: hands are the most lateral points in the waist/thigh band
bones = {}


def add(name, headp, tailp, parent):
    bones[name] = (headp.copy(), tailp.copy(), parent)


add("Hips", hips, spine, None)
add("Spine", spine, chest, "Hips")
add("Chest", chest, neck - Vector((0, 0, 0.05 * H)), "Spine")
add("Neck", neck - Vector((0, 0, 0.05 * H)), head, "Chest")
add("Head", head, head_top, "Neck")

for sign, side in ((1, "Left"), (-1, "Right")):
    shoulder_z = 0.78
    torso_half = 0.09 * H
    clav_start = Vector((sign * 0.02 * H, chest.y, z(shoulder_z)))
    shoulder = Vector((sign * torso_half, chest.y, z(shoulder_z)))
    side_flag = "--%s-hand-front" % side.lower()
    if side_flag in args:
        # Hand raised in front of the chest: frontmost (most -Y) points on that side.
        pts = [p for p in band(0.48, 0.74) if p.x * sign > 0.03 * H]
        pts.sort(key=lambda p: p.y)
        hand = centroid(pts[:max(5, len(pts) // 40)])
    else:
        hand = extreme_side(band(0.36, 0.62), sign)
    if hand is None or abs(hand.x) < abs(shoulder.x):
        hand = Vector((sign * (torso_half + 0.05 * H), chest.y, z(0.45)))
    wrist = shoulder + (hand - shoulder) * 0.85
    elbow = shoulder + (wrist - shoulder) * 0.5 + Vector((sign * 0.015 * H, 0.02 * H, 0))
    hand_tip = wrist + (wrist - elbow).normalized() * 0.07 * H

    add(side + "Shoulder", clav_start, shoulder, "Chest")
    add(side + "UpperArm", shoulder, elbow, side + "Shoulder")
    add(side + "LowerArm", elbow, wrist, side + "UpperArm")
    add(side + "Hand", wrist, hand_tip, side + "LowerArm")

    ankle, toe = feet[sign]
    hip_joint = Vector((ankle.x * 0.9, hips.y, z(0.48)))
    knee = Vector((ankle.x * 0.95, (hip_joint.y + ankle.y) / 2 - 0.015 * H, z(0.27)))
    add(side + "UpperLeg", hip_joint, knee, "Hips")
    add(side + "LowerLeg", knee, ankle, side + "UpperLeg")
    add(side + "Foot", ankle, toe + Vector((0, 0.03 * H, 0.01 * H)), side + "LowerLeg")
    add(side + "Toes", toe + Vector((0, 0.03 * H, 0.01 * H)), toe - Vector((0, 0.02 * H, 0)), side + "Foot")

# ---- build armature
arm_data = bpy.data.armatures.new("Armature")
arm = bpy.data.objects.new("Armature", arm_data)
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='EDIT')
for name in bones:  # insertion order = parents first
    h, t, parent = bones[name]
    eb = arm_data.edit_bones.new(name)
    eb.head, eb.tail = h, t
    if parent:
        eb.parent = arm_data.edit_bones[parent]
        eb.use_connect = False
bpy.ops.object.mode_set(mode='OBJECT')

# ---- skinning: Blender heat weights, then distance weights for anything it missed
bpy.ops.object.select_all(action='DESELECT')
mesh.select_set(True)
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
heat_ok = True
try:
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
except Exception as e:
    heat_ok = False
    print("HEAT FAILED", e)
    bpy.ops.object.parent_set(type='ARMATURE_NAME')

groups = {vg.index: vg.name for vg in mesh.vertex_groups}
for name in bones:
    if name not in [vg.name for vg in mesh.vertex_groups]:
        mesh.vertex_groups.new(name=name)
vg_by_name = {vg.name: vg for vg in mesh.vertex_groups}


def seg_dist(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-9)))
    return (a + ab * t - p).length


unweighted = 0
deform_names = [n for n in bones if not n.endswith("Shoulder")] + [n for n in bones if n.endswith("Shoulder")]
for v in mesh.data.vertices:
    total = sum(g.weight for g in v.groups)
    if total > 1e-4:
        continue
    unweighted += 1
    scores = []
    for name in bones:
        h, t, _ = bones[name]
        d = seg_dist(v.co, h, t) + 1e-4
        scores.append((1.0 / d ** 4, name))
    scores.sort(reverse=True)
    top = scores[:3]
    s = sum(w for w, _ in top)
    for w, name in top:
        vg_by_name[name].add([v.index], w / s, 'REPLACE')

print(f"RIG H={H:.3f} heat_ok={heat_ok} unweighted_fixed={unweighted}/{len(mesh.data.vertices)}")
for name in ("LeftHand", "RightHand", "LeftFoot", "RightFoot", "Head", "Hips"):
    h, t, _ = bones[name]
    print(f"  {name}: head=({h.x:.3f},{h.y:.3f},{h.z:.3f})")


# ---- preview renders (rest pose and a test pose)
def render(prefix):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'TEXTURE'
    scene.render.resolution_x = 400
    scene.render.resolution_y = 400
    if scene.world is None:
        scene.world = bpy.data.worlds.new("w")
    cam = scene.camera
    if cam is None:
        cd = bpy.data.cameras.new("cam")
        cd.type = 'ORTHO'
        cd.ortho_scale = H * 1.25
        cam = bpy.data.objects.new("cam", cd)
        scene.collection.objects.link(cam)
        scene.camera = cam
    for view, loc, rot in (("front", (0, -3, minz + H / 2), (90, 0, 0)), ("side", (3, 0, minz + H / 2), (90, 0, 90))):
        cam.location = loc
        cam.rotation_euler = [math.radians(a) for a in rot]
        scene.render.filepath = f"{prefix}_{view}.png"
        bpy.ops.render.render(write_still=True)


arm.show_in_front = True
render(preview + "_rest")

if "--test-pose" in args:
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')
    pb = arm.pose.bones
    for b in pb:
        b.rotation_mode = 'XYZ'

    def rot_world(name, axis, deg):
        b = pb[name]
        # rotate around a world axis, expressed in the bone's rest frame
        rest = b.bone.matrix_local.to_3x3()
        local_axis = rest.inverted() @ Vector(axis)
        from mathutils import Quaternion
        b.rotation_mode = 'QUATERNION'
        b.rotation_quaternion = Quaternion(local_axis, math.radians(deg)) @ b.rotation_quaternion

    rot_world("LeftUpperArm", (0, 1, 0), -60)    # raise left arm sideways
    rot_world("LeftLowerArm", (0, 1, 0), -30)
    rot_world("RightUpperArm", (1, 0, 0), -70)   # right arm forward
    rot_world("RightLowerArm", (1, 0, 0), -40)
    rot_world("LeftUpperLeg", (1, 0, 0), -35)    # left leg forward
    rot_world("LeftLowerLeg", (1, 0, 0), 40)
    rot_world("RightUpperLeg", (1, 0, 0), 25)    # right leg back
    rot_world("Head", (0, 0, 1), 30)
    bpy.ops.object.mode_set(mode='OBJECT')
    render(preview + "_pose")
    # back to rest pose before export
    bpy.ops.object.mode_set(mode='POSE')
    bpy.ops.pose.select_all(action='SELECT')
    bpy.ops.pose.transforms_clear()
    bpy.ops.object.mode_set(mode='OBJECT')

# ---- export for Unity
bpy.ops.object.select_all(action='DESELECT')
mesh.select_set(True)
arm.select_set(True)
bpy.ops.export_scene.fbx(
    filepath=out,
    use_selection=True,
    object_types={'ARMATURE', 'MESH'},
    add_leaf_bones=False,
    bake_anim=False,
    apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z',
    axis_up='Y',
    primary_bone_axis='Y',
    secondary_bone_axis='X',
    armature_nodetype='NULL',
    path_mode='STRIP',
    embed_textures=False,
)
print("EXPORTED", out)
