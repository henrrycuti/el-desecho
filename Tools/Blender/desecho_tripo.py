# Prepara el El Deshecho de Tripo para el juego (sin abrir Blender):
#   F:\Blender\blender.exe --background --python Tools/Blender/desecho_tripo.py [-- --preview carpeta]
# - Importa Importar/Tripo/*.fbx (malla + esqueleto estilo Mixamo).
# - Reduce la malla a ~20.000 triangulos (telefono) y guarda la textura a 2048.
# - Crea las 9 animaciones que usa la IA, todas agachadas y encorvadas:
#   Idle, Patrol, Investigate, Alert, Chase, Attack, Cast (rugido), Stunned (antidoto), Recover.
# - Exporta Assets/ElDesecho/Creature/Tripo/ElDeshechoTripo.fbx.
import bpy, glob, math, os, sys
from mathutils import Euler

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT_DIR = os.path.join(ROOT, "Assets", "ElDesecho", "Creature", "Tripo")
FPS = 30
TARGET_TRIS = 20000
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
PREVIEW = args[args.index("--preview") + 1] if "--preview" in args else None

bpy.ops.wm.read_factory_settings(use_empty=True)
source = sorted(glob.glob(os.path.join(ROOT, "Importar", "Tripo", "*.fbx")))[0]
bpy.ops.import_scene.fbx(filepath=source)
arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
mesh = next(o for o in bpy.context.scene.objects if o.type == "MESH")
scene = bpy.context.scene
scene.render.fps = FPS

# ---------- Malla y textura ----------
tris = sum(len(p.vertices) - 2 for p in mesh.data.polygons)
if tris > TARGET_TRIS:
    bpy.context.view_layer.objects.active = mesh
    dec = mesh.modifiers.new("Reducir", "DECIMATE")
    dec.ratio = TARGET_TRIS / tris
    bpy.ops.object.modifier_move_to_index(modifier=dec.name, index=0)
    bpy.ops.object.modifier_apply(modifier=dec.name)
print("TRIANGULOS", tris, "->", sum(len(p.vertices) - 2 for p in mesh.data.polygons))
os.makedirs(OUT_DIR, exist_ok=True)
skin = next((i for i in bpy.data.images if i.size[0] > 0), None)
if skin:
    skin.scale(2048, 2048)
    skin.filepath_raw = os.path.join(OUT_DIR, "Piel.png")
    skin.file_format = "PNG"
    skin.save()
    print("TEXTURA", skin.filepath_raw)

# ---------- Poses ----------
# Rotaciones en ejes del esqueleto (grados): X = cabeceo (+ dobla el tronco hacia delante; en piernas
# y brazos + lleva el miembro hacia atras), Y = alabeo lateral, Z = giro. El personaje mira hacia -Y.
P = lambda n: arm.pose.bones["mixamorig:" + n]
REST = {b.name: b.bone.matrix_local.to_3x3() for b in arm.pose.bones}


def rot(name, x=0, y=0, z=0):
    pb = P(name)
    m = REST[pb.name]
    world = Euler((math.radians(x), math.radians(y), math.radians(z)), "XYZ").to_matrix()
    pb.rotation_quaternion = (m.inverted() @ world @ m).to_quaternion()


# Postura base encorvada y agachada (comun a todas las animaciones).
BASE = {
    "Spine": (16, 0, 0), "Spine1": (14, 0, 0), "Spine2": (14, 0, 0), "Neck": (-14, 0, 0), "Head": (-22, 0, 0),
    "LeftShoulder": (0, 0, -6), "RightShoulder": (0, 0, 6),
    "LeftArm": (-12, -6, 0), "RightArm": (-12, 6, 0), "LeftForeArm": (-18, 0, 0), "RightForeArm": (-18, 0, 0),
    "LeftUpLeg": (-28, 0, 4), "RightUpLeg": (-28, 0, -4), "LeftLeg": (48, 0, 0), "RightLeg": (48, 0, 0),
    "LeftFoot": (-18, 0, 0), "RightFoot": (-18, 0, 0),
}
HIP_DROP = 9.0  # unidades del esqueleto (cm)


def pose(offsets, hip=0.0):
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    for n in set(BASE) | set(offsets):
        bx, by, bz = BASE.get(n, (0, 0, 0))
        ox, oy, oz = offsets.get(n, (0, 0, 0))
        rot(n, bx + ox, by + oy, bz + oz)
    # Cadera: el eje Y local apunta hacia arriba.
    P("Hips").location = (0, -HIP_DROP + hip, 0)


def key_all(frame):
    for pb in arm.pose.bones:
        pb.keyframe_insert("rotation_quaternion", frame=frame)
    P("Hips").keyframe_insert("location", frame=frame)


s = math.sin
TAU = 2 * math.pi


def walk(t, period, swing, knee, arms, lean, bob):
    f = TAU * t / period
    return {
        "Spine": (lean, 3 * s(f), 4 * s(f)), "Spine1": (lean * .6, 0, 0),
        "Head": (4 * s(2 * f + 1), 0, 6 * s(f)),
        "LeftUpLeg": (-swing * s(f), 0, 0), "RightUpLeg": (swing * s(f), 0, 0),
        "LeftLeg": (knee * max(0, s(f + 1.4)), 0, 0), "RightLeg": (knee * max(0, s(f + 1.4 + math.pi)), 0, 0),
        "LeftArm": (arms * s(f), 0, 0), "RightArm": (-arms * s(f), 0, 0),
        "LeftForeArm": (-arms * .5 * max(0, s(f)), 0, 0), "RightForeArm": (-arms * .5 * max(0, -s(f)), 0, 0),
    }, -bob * abs(s(f))


def idle(t):
    f = TAU * t / 4
    return {
        "Spine": (3 * s(f), 0, 0), "Spine2": (2 * s(f), 0, 0),
        "Head": (3 * s(2 * f), 0, 10 * s(f)),
        "LeftArm": (4 * s(f + 1), 0, 0), "RightArm": (4 * s(f + 2), 0, 0),
    }, 1.5 * s(f)


def investigate(t):
    f = TAU * t / 4
    return {
        "Spine": (-4, 0, 0), "Neck": (-8, 0, 0),
        "Head": (-10 + 6 * s(2 * f), 14 * s(f + .6), 38 * s(f)),
        "RightArm": (-25, 10, 0), "RightForeArm": (-30, 0, 0),
    }, 2


def alert(t):
    k = min(1, t / .25)
    return {
        "Spine": (8 * k, 0, 0), "Neck": (-18 * k, 0, 0), "Head": (-20 * k, 0, 25 * k),
        "LeftUpLeg": (-12 * k, 0, 0), "RightUpLeg": (-12 * k, 0, 0), "LeftLeg": (20 * k, 0, 0), "RightLeg": (20 * k, 0, 0),
        "LeftArm": (-30 * k, -20 * k, 0), "RightArm": (-30 * k, 20 * k, 0),
    }, -6 * k


def attack(t):
    k = math.sin(min(1, t / .5) * math.pi / 2)
    back = max(0, min(1, (t - .75) / .45))
    a = k * (1 - back)
    grab = max(0, min(1, (t - .35) / .2)) * (1 - back)
    return {
        "Spine": (18 * a, 0, 0), "Spine1": (8 * a, 0, 0), "Neck": (-12 * a, 0, 0), "Head": (-14 * a, 0, 0),
        "LeftArm": (-75 * a, -25 * a + 30 * grab, 0), "RightArm": (-75 * a, 25 * a - 30 * grab, 0),
        "LeftForeArm": (-10 * a, 0, 0), "RightForeArm": (-10 * a, 0, 0),
        "LeftUpLeg": (-20 * a, 0, 0), "RightLeg": (25 * a, 0, 0),
    }, -5 * a


def cast(t):
    f = TAU * t / 2.5
    k = .5 - .5 * math.cos(f)
    shake = 2.5 * s(t * 70) * k
    return {
        "Spine": (-14 * k, shake * .4, 0), "Spine1": (-8 * k, 0, 0), "Neck": (-14 * k, 0, 0), "Head": (-28 * k + shake, 0, shake),
        "LeftArm": (-20 * k, -55 * k, 0), "RightArm": (-20 * k, 55 * k, 0),
        "LeftForeArm": (-20 * k, 0, 0), "RightForeArm": (-20 * k, 0, 0),
    }, 3 * k


def stunned(t):
    shake = 3 * s(t * 45) + 2 * s(t * 23)
    return {
        "Spine": (28, shake * .5, 0), "Spine1": (16, 0, 0), "Spine2": (10, 0, 0), "Neck": (6, 0, shake), "Head": (12 + shake, 0, 0),
        "LeftUpLeg": (-70, 0, 6), "RightUpLeg": (-70, 0, -6), "LeftLeg": (95, 0, 0), "RightLeg": (95, 0, 0),
        "LeftFoot": (20, 0, 0), "RightFoot": (20, 0, 0),
        "LeftArm": (-95, -30, 0), "RightArm": (-95, 30, 0), "LeftForeArm": (-110, 0, 0), "RightForeArm": (-110, 0, 0),
    }, -38


def recover(t):
    k = 1 - min(1, t / 1.5)
    k = k * k * (3 - 2 * k)
    st, hip = stunned(0)
    return {n: tuple(v * k for v in vals) for n, vals in st.items()}, hip * k


CLIPS = [
    ("Idle", 4, idle, True),
    ("Patrol", 2, lambda t: walk(t, 2, 22, 30, 14, 4, 2.5), True),
    ("Investigate", 4, investigate, True),
    ("Alert", 1, alert, False),
    ("Chase", 1, lambda t: walk(t, 1, 38, 55, 30, 14, 5), True),
    ("Attack", 1.3, attack, False),
    ("Cast", 2.5, cast, True),
    ("Stunned", 2, stunned, True),
    ("Recover", 1.5, recover, False),
]

arm.animation_data_create()
bpy.context.view_layer.objects.active = arm
for name, length, fn, loop in CLIPS:
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    arm.animation_data.action = action
    frames = int(round(length * FPS))
    for frame in list(range(0, frames, 2)) + [frames]:
        t = 0 if (loop and frame == frames) else frame / FPS
        offsets, hip = fn(t)
        pose(offsets, hip)
        key_all(frame + 1)
    print("ANIMACION", name, frames, "fotogramas")
arm.animation_data.action = None

# ---------- Vista previa (opcional) ----------
if PREVIEW:
    os.makedirs(PREVIEW, exist_ok=True)
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "TEXTURE"
    scene.render.resolution_x, scene.render.resolution_y = 300, 400
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    scene.collection.objects.link(cam)
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 1.25
    cam.location = (1.6, -1.6, .55)
    cam.rotation_euler = (math.radians(88), 0, math.radians(45))
    scene.camera = cam
    for name, length, fn, loop in CLIPS:
        arm.animation_data.action = bpy.data.actions[name]
        for i, frac in enumerate((0, .4)):
            scene.frame_set(int(1 + frac * length * FPS))
            scene.render.filepath = os.path.join(PREVIEW, f"{name}_{i}.png")
            bpy.ops.render.render(write_still=True)
    arm.animation_data.action = None

# ---------- Exportar: una toma por accion ----------
for o in bpy.context.scene.objects:
    o.select_set(o in (arm, mesh))
bpy.context.view_layer.objects.active = arm
path = os.path.join(OUT_DIR, "ElDeshechoTripo.fbx")
bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"},
                         add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
                         bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
                         bake_anim_simplify_factor=0.5, path_mode="STRIP", embed_textures=False,
                         apply_scale_options="FBX_SCALE_ALL")
print("DESECHO_TRIPO_OK", path)
