# 只修正朝向与落地位，不减面，保留骨骼与动画
import bpy, os, math, mathutils

SRC = r"D:\project\WildernessMobile\Tools\tripo\final_rigged.fbx"
OUT = r"D:\project\WildernessMobile\Tools\tripo\final_upright.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC, automatic_bone_orientation=True)

meshes = [o for o in bpy.data.objects if o.type == 'MESH']
arms = [o for o in bpy.data.objects if o.type == 'ARMATURE']
roots = [o for o in bpy.data.objects if o.parent is None]
print("MESH=%d ARM=%d ACTIONS=%d ROOTS=%d" % (len(meshes), len(arms), len(bpy.data.actions), len(roots)))

def mesh_bounds():
    pts = []
    for o in meshes:
        pts += [o.matrix_world @ mathutils.Vector(c) for c in o.bound_box]
    xs = [p.x for p in pts]; ys = [p.y for p in pts]; zs = [p.z for p in pts]
    return (max(xs)-min(xs), max(ys)-min(ys), max(zs)-min(zs)), (min(zs), max(zs))

# 让骨骼回到绑定姿态再测尺寸
for a in arms:
    if a.animation_data:
        a.animation_data.action = None
for a in arms:
    for pb in a.pose.bones:
        pb.matrix_basis = mathutils.Matrix.Identity(4)
bpy.context.view_layer.update()

d, _ = mesh_bounds()
print("DIMS before x=%.2f y=%.2f z=%.2f" % d)

# 最长轴立到 Z
if d[0] >= d[1] and d[0] >= d[2]:
    rot = mathutils.Euler((0, -math.pi/2, 0)).to_matrix().to_4x4()
    print("ROT X->Z")
elif d[1] >= d[0] and d[1] >= d[2]:
    rot = mathutils.Euler((math.pi/2, 0, 0)).to_matrix().to_4x4()
    print("ROT Y->Z")
else:
    rot = mathutils.Matrix.Identity(4)
    print("ALREADY Z-up")

if rot != mathutils.Matrix.Identity(4):
    for o in roots:
        o.matrix_world = rot @ o.matrix_world
bpy.context.view_layer.update()

d, zr = mesh_bounds()
print("DIMS after x=%.2f y=%.2f z=%.2f" % d)

# 脚底落到 z=0
for o in roots:
    o.location.z -= zr[0]
bpy.context.view_layer.update()
d, zr = mesh_bounds()
print("FINAL height=%.2f  minz=%.3f" % (d[2], zr[0]))

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
    add_leaf_bones=False,
)
print("EXPORTED %.1fMB" % (os.path.getsize(OUT) / 1048576.0))
