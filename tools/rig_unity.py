"""Rig a generated, A-posed humanoid for Unity: Mixamo bone names, skinned, exported as FBX.

    blender -b --factory-startup --python-exit-code 1 -P tools/rig_unity.py -- \
        in.glb out.fbx [--height 1.65] [--facing +x] [--report report.json] [--marker]

Steps 1 to 3 are Lamplighter's tools/rig_hero.py (github.com/imDev2023/lamplighter), which
rigged its Tripo hero for Three.js; here the clips are left to Unity, whose Humanoid importer
maps the Mixamo names by itself and retargets any humanoid clip onto the rig.

1. Import the model (one mesh, a simplified Tripo output), turn it to face -Y, stand its feet
   on Z = 0 at the origin and scale it to --height metres. --facing names the axis the raw
   model's face points along.
2. Measure joints from the mesh itself and print every one, so a wrong guess shows.
3. Build a humanoid armature with Mixamo bone names (mixamorig:Hips and so on), skin the mesh
   on a watertight voxel copy and carry the weights over, and blend the thighs' weights into
   the hips around the tunic's hem.
4. Save the textures beside the FBX as <out>_basecolor.png, <out>_normal.png (OpenGL, as
   Unity reads it) and <out>_metallicsmoothness.png (metal in red, smoothness in alpha, as
   URP's Lit shader reads it), since the Unity material is built from them, not from FBX.
5. Export the FBX the way Unity reads Blender files: Y up, -Z forward with the axis change
   baked in (so the root carries no rotation and the character faces Unity's +Z), scale 1,
   no leaf bones, no animation.

--marker adds a box skinned to mixamorig:LeftHand alone, outside the left hand, for the
one-time check that Unity keeps left as left (CLAUDE.md); never ship that export.

Blender 4.3 or newer. Never touches the input file.
"""

import json
import math
import os
import sys

import bpy
import numpy as np
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
if len(argv) < 2:
    raise SystemExit("usage: rig_unity.py -- in.glb out.fbx [--height M] [--facing +x|-x|+y|-y] [--report FILE] [--marker]")
src, dst = os.path.abspath(argv[0]), os.path.abspath(argv[1])
opts = {"--height": "1.65", "--facing": "+x", "--report": ""}
marker = "--marker" in argv
rest = [x for x in argv[2:] if x != "--marker"]
for i in range(0, len(rest) - 1, 2):
    if rest[i] not in opts:
        raise SystemExit(f"unknown option {rest[i]}")
    opts[rest[i]] = rest[i + 1]
if len(rest) % 2:
    raise SystemExit(f"option {rest[-1]} has no value")
height = float(opts["--height"])
turn = {"+x": -90, "-x": 90, "+y": 180, "-y": 0}[opts["--facing"]]

# 1. Import, turn to face -Y, feet on the ground at the origin, real height.
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
if len(meshes) != 1:
    raise SystemExit(f"expected one mesh, found {len(meshes)}")
body = meshes[0]
for o in list(bpy.context.scene.objects):
    if o is not body:
        bpy.data.objects.remove(o)
body.parent = None
body.name = "Hero"
body.data.name = "Hero"
body.matrix_world = Matrix.Rotation(math.radians(turn), 4, "Z") @ body.matrix_world
bpy.context.view_layer.objects.active = body
body.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# A simplified mesh keeps each surviving vertex's normal from the dense original, and spread
# over larger triangles those local bumps showed as patches on the face (2026-11-11), so
# shade from this mesh's own geometry instead.
bpy.ops.mesh.customdata_custom_splitnormals_clear()
body.data.shade_smooth()
# glTF splits vertices along every UV seam; the heat solve needs one connected surface.
# Merging keeps the UVs, which Blender stores per face corner.
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.remove_doubles(threshold=1e-5)
bpy.ops.object.mode_set(mode="OBJECT")

v = np.array([tuple(p.co) for p in body.data.vertices])
lo, hi = v.min(0), v.max(0)
s = height / (hi[2] - lo[2])
shift = np.array([-(lo[0] + hi[0]) / 2, -(lo[1] + hi[1]) / 2, -lo[2]])
for p in body.data.vertices:
    p.co = Vector(((np.array(p.co) + shift) * s).tolist())
v = (v + shift) * s
H = height


# 2. Measure joints.
def band(z0, z1, extra=None):
    m = (v[:, 2] >= z0 * H) & (v[:, 2] <= z1 * H)
    if extra is not None:
        m &= extra
    return v[m]


def side_centre(z0, z1, side):
    b = band(z0, z1, (v[:, 0] * side) > 0.005 * H)
    if len(b) == 0:
        raise SystemExit(f"no vertices on side {side} between {z0} and {z1} of the height")
    return b.mean(0)


# The crotch: the lowest point of the body between the legs.
mid = v[(np.abs(v[:, 0]) < 0.012 * H) & (v[:, 2] > 0.2 * H) & (v[:, 2] < 0.7 * H)]
crotch = mid[:, 2].min()
hip_z = crotch + 0.045 * H

# The arms: a line fitted through each arm's outer half, from the fingertip up to the
# elbow, meets shoulder height at the shoulder joint. (A gap between arm and torso is no
# use: a simplified mesh is sparse enough to show 2 cm gaps anywhere, and a painted A-pose
# can hang its arms against the body.)
chest = band(0.62, 0.66)
SHOULDER_Z = 0.795 * H
arm_lines = {}
for side in (1, -1):
    reach = v[(v[:, 2] > 0.35 * H) & (v[:, 2] < 0.85 * H)]
    tip = reach[np.argmax(reach[:, 0] * side)]
    outer = v[(v[:, 0] * side > 0.55 * tip[0] * side) & (v[:, 2] > tip[2]) & (v[:, 2] < 0.8 * H)]
    b, a = np.polyfit(outer[:, 2], outer[:, 0], 1)  # x as a function of z
    sx = side * float(np.clip(abs(a + b * SHOULDER_Z), 0.05 * H, 0.11 * H))
    arm_lines[side] = (tip, sx)
torso_half = (abs(arm_lines[1][1]) + abs(arm_lines[-1][1])) / 2 / 0.8

joints = {}
for side, name in ((1, "Left"), (-1, "Right")):
    # Character's left is +X once it faces -Y.
    joints[f"{name}Ankle"] = side_centre(0.045, 0.075, side) * [1, 1, 0] + [0, 0, 0.06 * H]
    leg_top = side_centre(max(crotch / H - 0.06, 0.3), crotch / H - 0.01, side)
    joints[f"{name}Hip"] = np.array([leg_top[0], leg_top[1], hip_z])
    # The knee sits a little below halfway from hip to ankle.
    kz = joints[f"{name}Ankle"][2] + 0.5 * (hip_z - joints[f"{name}Ankle"][2])
    joints[f"{name}Knee"] = side_centre(kz / H - 0.01, kz / H + 0.01, side) * [1, 1, 0] + [0, 0, kz]
    foot = band(0.0, 0.05, (v[:, 0] * side) > 0.005 * H)
    toe = foot[np.argmin(foot[:, 1])]
    ankle = joints[f"{name}Ankle"]
    joints[f"{name}ToeBase"] = np.array([ankle[0], ankle[1] + 0.7 * (toe[1] - ankle[1]), 0.025 * H])
    joints[f"{name}ToeEnd"] = np.array([ankle[0], toe[1], 0.02 * H])

    tip, sx = arm_lines[side]
    shoulder = np.array([sx, chest[:, 1].mean(), SHOULDER_Z])
    d = (tip - shoulder) / np.linalg.norm(tip - shoulder)
    wrist = tip - d * 0.085 * H
    joints[f"{name}Shoulder"] = np.array([side * 0.035 * H, shoulder[1], 0.79 * H])
    joints[f"{name}Arm"] = shoulder
    joints[f"{name}ForeArm"] = shoulder + (wrist - shoulder) * 0.5 + np.array([0, 0.012 * H, 0])
    joints[f"{name}Hand"] = wrist
    joints[f"{name}HandEnd"] = tip

cy = band(0.5, 0.75)[:, 1].mean()
# The neck: the narrowest slice between the shoulders and the head, arms left out.
widths = []
for z in np.arange(0.70, 0.93, 0.005):
    sl = band(z, z + 0.005, np.abs(v[:, 0]) < torso_half * 1.2)
    widths.append((np.abs(sl[:, 0]).max() if len(sl) else 1e9, z))
neck_z = min(widths)[1] * H
print(f"crotch {crotch:.3f} m, neck {neck_z:.3f} m, torso half-width {torso_half:.3f} m")
sp = np.linspace(hip_z, neck_z - 0.03 * H, 5)[1:4]
for k, z in (("Hips", hip_z), ("Spine", sp[0]), ("Spine1", sp[1]), ("Spine2", sp[2]), ("Neck", neck_z - 0.03 * H), ("Head", neck_z + 0.015 * H)):
    joints[k] = np.array([0.0, cy, z])
joints["HeadTop"] = np.array([0.0, cy, H])
for k in sorted(joints):
    print(f"joint {k:16s}", np.round(joints[k], 3).tolist())

# 3. The armature, with Mixamo names.
P = "mixamorig:"
bones = [
    # name, head joint, tail joint, parent
    ("Hips", "Hips", "Spine", None),
    ("Spine", "Spine", "Spine1", "Hips"),
    ("Spine1", "Spine1", "Spine2", "Spine"),
    ("Spine2", "Spine2", "Neck", "Spine1"),
    ("Neck", "Neck", "Head", "Spine2"),
    ("Head", "Head", "HeadTop", "Neck"),
]
for n in ("Left", "Right"):
    bones += [
        (f"{n}Shoulder", f"{n}Shoulder", f"{n}Arm", "Spine2"),
        (f"{n}Arm", f"{n}Arm", f"{n}ForeArm", f"{n}Shoulder"),
        (f"{n}ForeArm", f"{n}ForeArm", f"{n}Hand", f"{n}Arm"),
        (f"{n}Hand", f"{n}Hand", f"{n}HandEnd", f"{n}ForeArm"),
        (f"{n}UpLeg", f"{n}Hip", f"{n}Knee", "Hips"),
        (f"{n}Leg", f"{n}Knee", f"{n}Ankle", f"{n}UpLeg"),
        (f"{n}Foot", f"{n}Ankle", f"{n}ToeBase", f"{n}Leg"),
        (f"{n}ToeBase", f"{n}ToeBase", f"{n}ToeEnd", f"{n}Foot"),
    ]

arm_data = bpy.data.armatures.new("HeroRig")
rig = bpy.data.objects.new("HeroRig", arm_data)
bpy.context.scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode="EDIT")
for name, h, t, parent in bones:
    eb = arm_data.edit_bones.new(P + name)
    eb.head = Vector(joints[h].tolist())
    eb.tail = Vector(joints[t].tolist())
    # Knees and elbows hinge about X: keep each bone's X axis lateral.
    eb.align_roll(Vector((0, -1, 0)) if abs(eb.vector.normalized().y) < 0.9 else Vector((0, 0, 1)))
    if parent:
        eb.parent = arm_data.edit_bones[P + parent]
        eb.use_connect = (eb.head - eb.parent.tail).length < 1e-4
bpy.ops.object.mode_set(mode="OBJECT")

# Bone heat fails on a generated mesh's thin, overlapping cloth layers (on the 24,000-
# triangle hero it weighted no vertex at all). Solve it on a watertight voxel copy instead,
# then carry the weights over to the real mesh from the nearest point on that copy.
proxy = bpy.data.objects.new("HeatProxy", body.data.copy())
bpy.context.scene.collection.objects.link(proxy)
remesh = proxy.modifiers.new("Remesh", "REMESH")
remesh.mode = "VOXEL"
remesh.voxel_size = 0.008 * H
bpy.ops.object.select_all(action="DESELECT")
bpy.context.view_layer.objects.active = proxy
proxy.select_set(True)
bpy.ops.object.modifier_apply(modifier="Remesh")
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.parent_set(type="ARMATURE_AUTO")
proxy_unweighted = sum(1 for p in proxy.data.vertices if not p.groups)
print(f"heat proxy: {len(proxy.data.vertices)} vertices, {proxy_unweighted} without a weight")

for b in arm_data.bones:
    body.vertex_groups.new(name=b.name)
transfer = body.modifiers.new("Weights", "DATA_TRANSFER")
transfer.object = proxy
transfer.use_vert_data = True
transfer.data_types_verts = {"VGROUP_WEIGHTS"}
transfer.vert_mapping = "POLYINTERP_NEAREST"
transfer.layers_vgroup_select_src = "ALL"
transfer.layers_vgroup_select_dst = "NAME"
bpy.ops.object.select_all(action="DESELECT")
bpy.context.view_layer.objects.active = body
body.select_set(True)
bpy.ops.object.modifier_apply(modifier="Weights")
bpy.data.objects.remove(proxy)

body.parent = rig
body.matrix_parent_inverse = rig.matrix_world.inverted()
skin = body.modifiers.new("Armature", "ARMATURE")
skin.object = rig

# Any vertex without a weight goes to the nearest bone segment.
groups = {g.index: g.name for g in body.vertex_groups}
segs = []
for b in arm_data.bones:
    segs.append((b.name, np.array(b.head_local), np.array(b.tail_local)))
unweighted = 0
for p in body.data.vertices:
    if sum(g.weight for g in p.groups) > 1e-4:
        continue
    unweighted += 1
    x = np.array(p.co)
    best, bd = None, 1e9
    for name, a, b in segs:
        ab = b - a
        t = np.clip(np.dot(x - a, ab) / np.dot(ab, ab), 0, 1)
        d = np.linalg.norm(x - (a + t * ab))
        if d < bd:
            best, bd = name, d
    body.vertex_groups[best].add([p.index], 1.0, "REPLACE")
print(f"vertices without a weight after the heat solve: {unweighted} of {len(body.data.vertices)}")

# Around the tunic's hem, cloth and trousers lie in thin layers that the heat solve splits
# between the thighs and the hips, so a swinging thigh pushes the trousers through the hem.
# From a little below the crotch up to the hip joints, hand a growing share of each thigh's
# weight to the hips, so the layers move together.
hips_group = body.vertex_groups[P + "Hips"]
leg_groups = {body.vertex_groups[P + n].index for n in ("LeftUpLeg", "RightUpLeg")}
z_lo, z_hi = crotch - 0.06 * H, hip_z + 0.02 * H
moved = 0
for p in body.data.vertices:
    z = p.co.z
    if not z_lo < z < z_hi:
        continue
    share = 0.6 * (z - z_lo) / (z_hi - z_lo)
    for g in p.groups:
        if g.group in leg_groups and g.weight > 0:
            give = g.weight * share
            g.weight -= give
            hips_group.add([p.index], give, "ADD")
            moved += 1
print(f"thigh weights blended into the hips near the hem: {moved}")

# The one-time left-right check: a box outside the left hand, moved by that hand alone.
if marker:
    hand = arm_data.bones[P + "LeftHand"]
    centre = Vector(hand.tail_local) + Vector((0.04 * H, 0, 0))
    bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.mesh.primitive_cube_add(size=0.05 * H, location=centre)
    box = bpy.context.active_object
    box.name = "LeftHandMarker"
    box.data.name = "LeftHandMarker"
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    box.vertex_groups.new(name=P + "LeftHand").add(list(range(len(box.data.vertices))), 1.0, "REPLACE")
    box.parent = rig
    box.modifiers.new("Armature", "ARMATURE").object = rig

# 4. Textures, by what each image feeds in the material.
stem = os.path.splitext(dst)[0]
textures = {}
mat = body.active_material
if mat and mat.use_nodes:
    for node in mat.node_tree.nodes:
        if node.type != "TEX_IMAGE" or node.image is None:
            continue
        role = None
        for link in node.outputs["Color"].links:
            to = link.to_node
            if to.type == "BSDF_PRINCIPLED" and link.to_socket.name == "Base Color":
                role = "basecolor"
            elif to.type == "NORMAL_MAP":
                role = "normal"
            elif to.type in ("SEPARATE_COLOR", "SEPRGB"):
                role = "metallicroughness"
        if role is None:
            print(f"texture {node.image.name}: feeds nothing known, skipped")
            continue
        img = node.image
        if role == "metallicroughness":
            # glTF keeps roughness in green and metal in blue; URP's Lit shader reads metal
            # from red and smoothness from alpha.
            role = "metallicsmoothness"
            w, h = img.size
            px = np.empty(w * h * 4, dtype=np.float32)
            img.pixels.foreach_get(px)
            px = px.reshape(-1, 4)
            out = np.empty_like(px)
            out[:, 0] = out[:, 1] = out[:, 2] = px[:, 2]
            out[:, 3] = 1.0 - px[:, 1]
            img = bpy.data.images.new("MetallicSmoothness", w, h, alpha=True, float_buffer=False)
            img.colorspace_settings.name = "Non-Color"
            img.pixels.foreach_set(out.ravel())
        path = f"{stem}_{role}.png"
        img.filepath_raw = path
        img.file_format = "PNG"
        img.save()
        textures[role] = {"file": os.path.basename(path), "size": list(img.size)}
        print(f"texture {role}: {img.size[0]} x {img.size[1]} -> {path}")

# 5. Export.
body.name = "Hero"
body.data.name = "Hero"
bpy.ops.object.select_all(action="DESELECT")
for o in bpy.context.scene.objects:
    if o.type in ("MESH", "ARMATURE"):
        o.select_set(True)
bpy.ops.export_scene.fbx(
    filepath=dst,
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
    bake_space_transform=True,
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    add_leaf_bones=False,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    armature_nodetype="NULL",
    bake_anim=False,
    path_mode="STRIP",
)
report = {
    "out": dst,
    "triangles": sum(len(p.vertices) - 2 for p in body.data.polygons),
    "vertices": len(body.data.vertices),
    "bones": len(arm_data.bones),
    "height": height,
    "unweighted_before_fill": unweighted,
    "textures": textures,
    "marker": marker,
    "joints": {k: np.round(j, 4).tolist() for k, j in joints.items()},
}
print(json.dumps(report))
if opts["--report"]:
    with open(opts["--report"], "w") as fh:
        json.dump(report, fh, indent=1)
