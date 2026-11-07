# 从四个方向渲染 FBX，用于判断真实朝向/形态
import bpy, os, math, mathutils, sys

SRC = r"D:\project\WildernessMobile\Tools\tripo\final_upright.fbx"
OUTDIR = r"D:\project\WildernessMobile\Tools\tripo\views"
os.makedirs(OUTDIR, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC, automatic_bone_orientation=True)

# 让骨骼停在第 0 帧
for o in bpy.data.objects:
    if o.type == 'ARMATURE' and o.animation_data:
        o.animation_data.action = None

meshes = [o for o in bpy.data.objects if o.type == 'MESH']
pts = []
for o in meshes:
    pts += [o.matrix_world @ mathutils.Vector(c) for c in o.bound_box]
xs = [p.x for p in pts]; ys = [p.y for p in pts]; zs = [p.z for p in pts]
cx = (max(xs)+min(xs))/2; cy = (max(ys)+min(ys))/2; cz = (max(zs)+min(zs))/2
size = max(max(xs)-min(xs), max(ys)-min(ys), max(zs)-min(zs), 0.2)
print("MESH dims x=%.2f y=%.2f z=%.2f" % (max(xs)-min(xs), max(ys)-min(ys), max(zs)-min(zs)))

centre = mathutils.Vector((cx, cy, cz))
dist = size * 2.6

light_data = bpy.data.lights.new("L", type='SUN'); light_data.energy = 5
light = bpy.data.objects.new("L", light_data)
bpy.context.scene.collection.objects.link(light)
light.rotation_euler = (math.radians(55), 0, math.radians(35))

cam_data = bpy.data.cameras.new("C"); cam_data.lens = 50
cam = bpy.data.objects.new("C", cam_data)
bpy.context.scene.collection.objects.link(cam)
bpy.context.scene.camera = cam

scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x = 420
scene.render.resolution_y = 560
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'TEXTURE'

views = {
    "minusY": (centre + mathutils.Vector((0, -dist, 0)), (math.pi/2, 0, 0)),
    "plusY":  (centre + mathutils.Vector((0,  dist, 0)), (math.pi/2, 0, math.pi)),
    "plusX":  (centre + mathutils.Vector(( dist, 0, 0)), (math.pi/2, 0, math.pi/2)),
    "plusZ":  (centre + mathutils.Vector((0, 0,  dist)), (0, 0, 0)),
}

for name, (loc, rot) in views.items():
    cam.location = loc
    cam.rotation_euler = rot
    scene.render.filepath = os.path.join(OUTDIR, name + ".png")
    bpy.ops.render.render(write_still=True)
    print("RENDERED " + name)
