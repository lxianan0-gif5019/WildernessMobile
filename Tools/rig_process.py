# 处理 Tripo 绑骨动画 FBX：减面到 4 万、统一朝向、导出给 Unity
import bpy, os, math, mathutils

SRC = r"D:\project\WildernessMobile\Tools\tripo\animated.fbx"
OUT = r"D:\project\WildernessMobile\Tools\tripo\WhaleGirl_rigged.fbx"
TARGET_TRIS = 40000

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC, automatic_bone_orientation=True)

meshes = [o for o in bpy.data.objects if o.type == 'MESH']
arms = [o for o in bpy.data.objects if o.type == 'ARMATURE']
print("MESHES=%d ARMATURES=%d ACTIONS=%d" % (len(meshes), len(arms), len(bpy.data.actions)))
for a in bpy.data.actions:
    print("  ACTION %s frames=%.0f" % (a.name, a.frame_range[1] - a.frame_range[0]))

total = 0
for o in meshes:
    total += len(o.data.polygons)
print("TRIS before=%d" % total)

# --- decimate (keeps vertex groups, so skinning survives)
if total > TARGET_TRIS * 1.2:
    ratio = TARGET_TRIS / float(total)
    for o in meshes:
        bpy.context.view_layer.objects.active = o
        m = o.modifiers.new("Dec", 'DECIMATE')
        m.ratio = ratio
        bpy.ops.object.modifier_apply(modifier=m.name)
    total = sum(len(o.data.polygons) for o in meshes)
    print("TRIS after=%d" % total)

# --- make the tallest axis vertical (Blender Z-up)
def bounds(objs):
    pts = []
    for o in objs:
        pts += [o.matrix_world @ mathutils.Vector(c) for c in o.bound_box]
    xs = [p.x for p in pts]; ys = [p.y for p in pts]; zs = [p.z for p in pts]
    return (max(xs)-min(xs), max(ys)-min(ys), max(zs)-min(zs))

roots = meshes + arms
d = bounds(roots)
print("DIMS x=%.2f y=%.2f z=%.2f" % d)

if max(d) > 0:
    if d[0] == max(d):
        rot = mathutils.Euler((0, -math.pi/2, 0)).to_matrix().to_4x4()
    elif d[1] == max(d):
        rot = mathutils.Euler((math.pi/2, 0, 0)).to_matrix().to_4x4()
    else:
        rot = mathutils.Matrix.Identity(4)

    if rot != mathutils.Matrix.Identity(4):
        for o in roots:
            o.matrix_world = rot @ o.matrix_world
        print("ROTATED to Z-up")
    print("DIMS after x=%.2f y=%.2f z=%.2f" % bounds(roots))

# --- drop feet to origin, centre on XZ
d = bounds(roots)
pts = []
for o in roots:
    pts += [o.matrix_world @ mathutils.Vector(c) for c in o.bound_box]
minz = min(p.z for p in pts)
for o in roots:
    o.location.z -= minz
print("DROPPED to ground, height=%.2f" % d[2])

bpy.context.view_layer.update()

os.makedirs(os.path.dirname(OUT), exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=OUT,
    use_selection=False,
    path_mode='COPY',
    embed_textures=True,
    axis_forward='-Z',
    axis_up='Y',
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0.0,
    add_leaf_bones=False,
)
print("EXPORTED %.1fMB" % (os.path.getsize(OUT) / 1048576.0))
