# 导入 GLB，轻减面，渲染一张预览图，并导出 FBX
import bpy, os, math, mathutils

GLB = r"D:\project\WildernessMobile\Tools\tripo\whale_girl.glb"
FBX = r"D:\project\WildernessMobile\Tools\tripo\PlayerModel.fbx"
PREVIEW = r"D:\project\WildernessMobile\Tools\tripo\blender_preview.png"
TARGET = 60000

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=GLB)

meshes = [o for o in bpy.data.objects if o.type == 'MESH']
obj = meshes[0]
tris = len(obj.data.loop_triangles) if obj.data.loop_triangles else len(obj.data.polygons)
if tris == 0:
    tris = len(obj.data.polygons)
print("TRIS before=%d" % tris)

if tris > TARGET * 1.2:
    bpy.context.view_layer.objects.active = obj
    m = obj.modifiers.new("Dec", 'DECIMATE')
    m.ratio = TARGET / float(tris)
    bpy.ops.object.modifier_apply(modifier=m.name)
    print("TRIS after=%d" % len(obj.data.polygons))

# --- normalise orientation: put the tallest axis on Z
bpy.context.view_layer.objects.active = obj
obj.select_set(True)

def dims(o):
    pts = [o.matrix_world @ mathutils.Vector(c) for c in o.bound_box]
    xs = [p.x for p in pts]; ys = [p.y for p in pts]; zs = [p.z for p in pts]
    return (max(xs)-min(xs), max(ys)-min(ys), max(zs)-min(zs))

d = dims(obj)
print("DIMS x=%.2f y=%.2f z=%.2f" % d)
if d[0] == max(d):
    obj.rotation_euler[1] -= math.pi/2       # X -> Z
elif d[1] == max(d):
    obj.rotation_euler[0] += math.pi/2       # Y -> Z
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
print("DIMS after x=%.2f y=%.2f z=%.2f" % dims(obj))

# --- preview render
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
bpy.context.view_layer.objects.active = obj

d = dims(obj)
centre = obj.matrix_world @ (0.125 * sum((mathutils.Vector(c) for c in obj.bound_box), mathutils.Vector()))
cam_data = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_data)
bpy.context.scene.collection.objects.link(cam)
cam.location = centre + mathutils.Vector((0, -max(d) * 2.2, 0))
cam.rotation_euler = (math.pi/2, 0, 0)
bpy.context.scene.camera = cam

light_data = bpy.data.lights.new("L", type='SUN')
light_data.energy = 4
light = bpy.data.objects.new("L", light_data)
bpy.context.scene.collection.objects.link(light)
light.rotation_euler = (math.radians(60), 0, math.radians(30))

scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x = 500
scene.render.resolution_y = 700
scene.render.filepath = PREVIEW
bpy.ops.render.render(write_still=True)
print("PREVIEW " + PREVIEW)

os.makedirs(os.path.dirname(FBX), exist_ok=True)
bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, path_mode='COPY',
                         embed_textures=True, axis_forward='-Z', axis_up='Y', add_leaf_bones=False)
print("EXPORTED %.1fMB polys=%d" % (os.path.getsize(FBX)/1048576.0, len(obj.data.polygons)))
