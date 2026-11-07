# 程序化生成低多边形"鲸鱼娘"，导出 FBX 给 Unity 使用
# 运行：blender.exe --background --python build_whale_girl.py
import bpy, math, os

OUT = r"D:\project\WildernessMobile\Assets\Resources\PlayerModel.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# ---------------------------------------------------------------- materials
def mat(name, rgb, rough=0.55, metal=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    if b:
        b.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
        b.inputs["Roughness"].default_value = rough
        b.inputs["Metallic"].default_value = metal
    return m

M_SKIN   = mat("Skin",   (0.96, 0.86, 0.80))
M_HAIR   = mat("Hair",   (0.14, 0.38, 0.72))
M_HAIR2  = mat("HairTip",(0.35, 0.72, 0.92))
M_NAVY   = mat("Dress",  (0.10, 0.13, 0.31))
M_BLUE   = mat("DressLight", (0.22, 0.38, 0.72))
M_WHITE  = mat("Cloth",  (0.95, 0.96, 0.98))
M_TAIL   = mat("Tail",   (0.16, 0.30, 0.62))
M_SHOE   = mat("Shoe",   (0.09, 0.11, 0.28), rough=0.35)
M_EYE    = mat("Eye",    (0.13, 0.42, 0.82), rough=0.2)
M_GOLD   = mat("Trim",   (0.80, 0.66, 0.35), rough=0.4, metal=0.6)

parts = []

def finish(o, m, scale=None, rot=None):
    if scale: o.scale = scale
    if rot: o.rotation_euler = rot
    if o.data.materials: o.data.materials.clear()
    o.data.materials.append(m)
    for p in o.data.polygons:
        p.use_smooth = True
    parts.append(o)
    return o

def sphere(name, loc, scale, m):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, radius=1.0, location=loc)
    o = bpy.context.object; o.name = name
    return finish(o, m, scale)

def cyl(name, loc, r, depth, m, rot=(0,0,0), verts=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth, location=loc, rotation=rot)
    o = bpy.context.object; o.name = name
    return finish(o, m)

def cone(name, loc, r1, r2, depth, m, rot=(0,0,0), verts=16):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2, depth=depth, location=loc, rotation=rot)
    o = bpy.context.object; o.name = name
    return finish(o, m)

def box(name, loc, scale, m, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    o = bpy.context.object; o.name = name
    return finish(o, m, scale)

# ---------------------------------------------------------------- character
# 面向 -Y（Blender 惯例），导出后 Unity 中是 +Z 朝前
# 总高约 1.62 米，脚底在 z=0

# legs (white stockings)
cyl("LegL", (-0.075, 0.0, 0.30), 0.055, 0.55, M_WHITE)
cyl("LegR", ( 0.075, 0.0, 0.30), 0.055, 0.55, M_WHITE)

# shoes
box("ShoeL", (-0.075, -0.02, 0.035), (0.11, 0.20, 0.07), M_SHOE)
box("ShoeR", ( 0.075, -0.02, 0.035), (0.11, 0.20, 0.07), M_SHOE)

# petticoat (white ruffle) then skirt (navy bell)
cone("Petticoat", (0, 0, 0.50), 0.34, 0.16, 0.30, M_WHITE, verts=24)
cone("Skirt",     (0, 0, 0.54), 0.31, 0.15, 0.34, M_NAVY, verts=24)

# light blue front panel
box("SkirtPanel", (0, -0.22, 0.52), (0.16, 0.06, 0.30), M_BLUE)

# apron
box("Apron", (0, -0.235, 0.66), (0.20, 0.045, 0.24), M_WHITE)

# bodice
cyl("Waist", (0, 0, 0.86), 0.115, 0.22, M_NAVY)
cyl("Chest", (0, 0, 1.00), 0.125, 0.16, M_NAVY)
box("ChestWhite", (0, -0.10, 1.02), (0.13, 0.05, 0.13), M_WHITE)
# gold buttons
for i in range(3):
    sphere("Btn%d" % i, (0, -0.115, 1.06 - i*0.05), (0.012, 0.012, 0.012), M_GOLD)

# collar + bow
cyl("Collar", (0, 0, 1.10), 0.085, 0.04, M_WHITE)
box("BowL", (-0.045, -0.10, 1.09), (0.06, 0.05, 0.05), M_NAVY, rot=(0, 0, 0.5))
box("BowR", ( 0.045, -0.10, 1.09), (0.06, 0.05, 0.05), M_NAVY, rot=(0, 0, -0.5))
sphere("Gem", (0, -0.115, 1.10), (0.022, 0.018, 0.022), M_BLUE)

# arms: navy sleeves + skin hands
for s, tag in ((-1, "L"), (1, "R")):
    cyl("Sleeve" + tag, (s*0.175, 0, 0.98), 0.05, 0.30, M_NAVY, rot=(0, s*0.18, 0))
    cyl("Cuff"   + tag, (s*0.205, 0, 0.83), 0.055, 0.04, M_WHITE, rot=(0, s*0.18, 0))
    sphere("Hand" + tag, (s*0.215, 0, 0.78), (0.045, 0.045, 0.055), M_SKIN)

# head
sphere("Head", (0, 0, 1.30), (0.135, 0.130, 0.145), M_SKIN)

# eyes
for s in (-1, 1):
    sphere("Eye%+d" % s, (s*0.055, -0.118, 1.29), (0.030, 0.020, 0.040), M_EYE)

# hair: cap, back mass, two long side tails
sphere("HairCap", (0, 0.012, 1.33), (0.150, 0.150, 0.150), M_HAIR)
cone("HairBack", (0, 0.075, 1.06), 0.16, 0.10, 0.55, M_HAIR, verts=20)
cone("HairTailL", (-0.135, 0.05, 1.00), 0.075, 0.030, 0.75, M_HAIR, rot=(0.05, -0.10, 0))
cone("HairTailR", ( 0.135, 0.05, 1.00), 0.075, 0.030, 0.75, M_HAIR, rot=(0.05,  0.10, 0))
# lighter tips
cone("HairTipL", (-0.145, 0.05, 0.62), 0.045, 0.015, 0.28, M_HAIR2, rot=(0.05, -0.10, 0))
cone("HairTipR", ( 0.145, 0.05, 0.62), 0.045, 0.015, 0.28, M_HAIR2, rot=(0.05,  0.10, 0))

# headband (white frill)
cyl("Headband", (0, 0.01, 1.40), 0.150, 0.035, M_WHITE, rot=(0.25, 0, 0), verts=24)
# side bow
box("HairBow", (0.135, -0.02, 1.36), (0.07, 0.05, 0.05), M_BLUE, rot=(0, 0, -0.4))
# ahoge
cyl("Ahoge", (0, 0.0, 1.48), 0.010, 0.10, M_HAIR, rot=(0.5, 0, 0), verts=8)

# fin ears (whale flippers, pointing back and out)
for s, tag in ((-1, "L"), (1, "R")):
    cone("Fin" + tag, (s*0.155, 0.05, 1.31), 0.075, 0.015, 0.18, M_HAIR2,
         rot=(1.2, 0, s*0.9), verts=10)

# ---------------------------------------------------------------- tail
# grows from the lower back, sweeps back and down, then the fluke
cyl("Tail1", (0, 0.20, 0.80), 0.075, 0.26, M_TAIL, rot=(1.05, 0, 0))
cyl("Tail2", (0, 0.36, 0.66), 0.060, 0.26, M_TAIL, rot=(1.25, 0, 0))
cyl("Tail3", (0, 0.50, 0.55), 0.045, 0.24, M_TAIL, rot=(1.45, 0, 0))
# fluke
box("FlukeL", (-0.10, 0.60, 0.50), (0.20, 0.13, 0.020), M_TAIL, rot=(0.15, 0.35, 0.2))
box("FlukeR", ( 0.10, 0.60, 0.50), (0.20, 0.13, 0.020), M_TAIL, rot=(0.15, -0.35, -0.2))

# ---------------------------------------------------------------- join + export
bpy.ops.object.select_all(action='DESELECT')
for o in parts:
    o.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()

obj = bpy.context.object
obj.name = "PlayerModel"
obj.location = (0, 0, 0)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# normals + shading
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.object.mode_set(mode='OBJECT')

os.makedirs(os.path.dirname(OUT), exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=OUT,
    use_selection=True,
    axis_forward='-Z',
    axis_up='Y',
)

dims = obj.dimensions
print("WHALE_OK verts=%d polys=%d height=%.2f width=%.2f depth=%.2f" % (
    len(obj.data.vertices), len(obj.data.polygons), dims.z, dims.x, dims.y))
