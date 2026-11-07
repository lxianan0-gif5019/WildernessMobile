# GLB -> FBX 转换 + 减面 + 贴图内嵌，供 Unity 使用
import bpy, os, sys

GLB = r"D:\project\WildernessMobile\Tools\tripo\whale_girl.glb"
OUT = r"D:\project\WildernessMobile\Tools\tripo\PlayerModel.fbx"
TARGET_TRIS = 20000

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=GLB)

meshes = [o for o in bpy.data.objects if o.type == 'MESH']
tris = sum(len(o.data.loop_triangles) if o.data.loop_triangles else len(o.data.polygons) for o in meshes)
if tris == 0:
    tris = sum(len(o.data.polygons) for o in meshes)
print("IMPORTED objects=%d tris=%d" % (len(meshes), tris))

# Decimate if far beyond the mobile budget
if tris > TARGET_TRIS * 1.2:
    ratio = TARGET_TRIS / float(tris)
    for o in meshes:
        bpy.context.view_layer.objects.active = o
        m = o.modifiers.new(name="Decimate", type='DECIMATE')
        m.ratio = ratio
        bpy.ops.object.modifier_apply(modifier=m.name)
    print("DECIMATED to ~%d tris (ratio %.3f)" % (TARGET_TRIS, ratio))

# Resize textures to 1024 to keep the APK small
for img in bpy.data.images:
    if img.size[0] > 1024 or img.size[1] > 1024:
        w, h = img.size
        img.scale(1024, int(1024 * h / max(1, w)))
        print("RESIZED texture %s from %dx%d" % (img.name, w, h))

for o in bpy.data.objects:
    if o.type == 'MESH':
        o.select_set(True)

# Make the tallest axis vertical in Blender (Z-up). The glTF importer sometimes leaves
# the mesh lying on its back, and exporting that gives a character lying down in Unity.
import mathutils
obj = meshes[0] if meshes else None
if obj:
    def world_dims(o):
        pts = [o.matrix_world @ mathutils.Vector(c) for c in o.bound_box]
        xs = [p.x for p in pts]; ys = [p.y for p in pts]; zs = [p.z for p in pts]
        return (max(xs)-min(xs), max(ys)-min(ys), max(zs)-min(zs))

    d = world_dims(obj)
    print("DIMS before x=%.2f y=%.2f z=%.2f" % d)

    if d[1] > d[2] * 1.2:          # taller along Y (glTF up) -> rotate to Z-up
        obj.rotation_euler[0] += -1.5707963
    elif d[0] > d[2] * 1.2:        # taller along X -> rotate to Z-up
        obj.rotation_euler[1] += 1.5707963

    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    print("DIMS after  x=%.2f y=%.2f z=%.2f" % world_dims(obj))

os.makedirs(os.path.dirname(OUT), exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=OUT,
    use_selection=False,
    path_mode='COPY',
    embed_textures=True,
    axis_forward='-Z',
    axis_up='Y',
    add_leaf_bones=False,
)

final = sum(len(o.data.polygons) for o in bpy.data.objects if o.type == 'MESH')
print("EXPORTED %s size=%.1fMB final_polys=%d" % (OUT, os.path.getsize(OUT) / 1048576.0, final))
