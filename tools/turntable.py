"""Render a model from every side, and its head up close, to look at it before trusting it.

    blender -b --factory-startup --python-exit-code 1 -P tools/turntable.py -- \
        in.glb|in.fbx out_dir [--views 8] [--size 640] [--head 0.17] [--no-normal] [--smooth]

Writes view_000.png, view_045.png ... (the azimuth in degrees around the vertical axis,
0 looking from Blender's -Y towards +Y), head_000.png and head_090.png ... (the top
--head share of the model's height, up close) and sheet.png, all views in one row above
the head close-ups. Soft sun from the camera's upper right and a grey world, so colour and
form read as they will in a neutral scene. Free: Blender runs locally.
"""

import math
import os
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
if len(argv) < 2:
    raise SystemExit("usage: turntable.py -- in.glb out_dir [--views N] [--size PX] [--head SHARE]")
src, out = os.path.abspath(argv[0]), os.path.abspath(argv[1])
opts = {"--views": "8", "--size": "640", "--head": "0.17"}
no_normal = "--no-normal" in argv
smooth = "--smooth" in argv
rest = [x for x in argv[2:] if x not in ("--no-normal", "--smooth")]
for i in range(0, len(rest) - 1, 2):
    if rest[i] not in opts:
        raise SystemExit(f"unknown option {rest[i]}")
    opts[rest[i]] = rest[i + 1]
n_views, size, head_share = int(opts["--views"]), int(opts["--size"]), float(opts["--head"])
os.makedirs(out, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
if src.lower().endswith(".fbx"):
    bpy.ops.import_scene.fbx(filepath=src)
else:
    bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
if not meshes:
    raise SystemExit("no mesh in the file")
bpy.context.view_layer.update()
pts = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
centre, dims = (lo + hi) / 2, hi - lo
tris = sum(len(p.vertices) - 2 for o in meshes for p in o.data.polygons)
if no_normal:
    # Unhook every normal map, to see what the geometry alone shades like.
    for o in meshes:
        for slot in o.material_slots:
            nt = slot.material.node_tree if slot.material and slot.material.use_nodes else None
            for link in list(nt.links) if nt else []:
                if link.to_socket.name == "Normal" and link.to_node.type == "BSDF_PRINCIPLED":
                    nt.links.remove(link)
    print("normal maps unhooked")
if smooth:
    # Drop the imported vertex normals and shade from the mesh's own geometry, as
    # tools/rig_unity.py does before export.
    for o in meshes:
        with bpy.context.temp_override(object=o, active_object=o, selected_objects=[o], selected_editable_objects=[o]):
            bpy.ops.mesh.customdata_custom_splitnormals_clear()
        o.data.shade_smooth()
    print("normals recomputed from the geometry")
print(f"bounds {[round(v, 4) for v in lo]} to {[round(v, 4) for v in hi]}, size {[round(v, 4) for v in dims]}, {tris} triangles")

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE_NEXT"
scene.render.resolution_x = scene.render.resolution_y = size
scene.render.film_transparent = False
scene.view_settings.view_transform = "Standard"
world = bpy.data.worlds.new("Grey")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.42, 0.42, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
scene.world = world

cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
scene.collection.objects.link(cam)
scene.camera = cam
cam.data.type = "ORTHO"
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
scene.collection.objects.link(sun)
sun.data.energy = 2.5
sun.data.angle = math.radians(20)


def shoot(azimuth, target, half, path):
    a = math.radians(azimuth)
    d = Vector((math.sin(a), -math.cos(a), 0))  # from the target towards the camera
    cam.location = target + d * (dims.length * 2)
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = half * 2
    cam.data.clip_end = dims.length * 5
    # The sun comes from above, over the camera's right shoulder.
    right = Vector((math.cos(a), math.sin(a), 0))
    sun.rotation_euler = (-(d + right * 0.6 + Vector((0, 0, 1.2)))).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


tall = max(dims.z, dims.x, dims.y) * 0.55
names = []
for k in range(n_views):
    az = round(360 * k / n_views)
    p = os.path.join(out, f"view_{az:03d}.png")
    shoot(az, centre, tall, p)
    names.append(p)
head_c = Vector((centre.x, centre.y, hi.z - dims.z * head_share / 2))
heads = []
for az in (0, 90, 180, 270):
    p = os.path.join(out, f"head_{az:03d}.png")
    shoot(az, head_c, dims.z * head_share * 0.58, p)
    heads.append(p)

# One sheet: the views in a row, the head close-ups beneath.
cols = max(len(names), len(heads))
sheet = bpy.data.images.new("sheet", cols * size, 2 * size)
px = [0.42, 0.42, 0.42, 1.0] * (cols * size * 2 * size)  # empty cells stay the world grey
for row, files in ((1, names), (0, heads)):
    for i, f in enumerate(files):
        im = bpy.data.images.load(f)
        src_px = list(im.pixels)
        for y in range(size):
            s0 = y * size * 4
            d0 = ((row * size + y) * cols * size + i * size) * 4
            px[d0 : d0 + size * 4] = src_px[s0 : s0 + size * 4]
sheet.pixels = px
sheet.filepath_raw = os.path.join(out, "sheet.png")
sheet.file_format = "PNG"
sheet.save()
print(f"wrote {out}")
