# Genera los modelos 3D de utileria de EL DESECHO con Blender (sin abrir la interfaz):
#   F:\Blender\blender.exe --background --python Tools/Blender/modelos_props.py
# Cada modelo se exporta a Assets/ElDesecho/Modelos/<Nombre>.fbx.
# Los materiales llevan el mismo nombre que los del juego (Oxido, Metal, Mesa...): el builder de Unity
# los sustituye por los materiales PBR reales. Las UV se proyectan en cubo a escala del mundo
# (1 unidad de UV = 1 m) para que las texturas no se estiren.
import bpy, bmesh, math, os

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "ElDesecho", "Modelos")
os.makedirs(OUT, exist_ok=True)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def material(obj, name):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    obj.data.materials.append(mat)
    return mat


def finish(obj, mat, bevel=0.0, smooth=True):
    """Aplica transformaciones, bisela un poco, suaviza y proyecta UV en cubo."""
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if bevel > 0:
        mod = obj.modifiers.new("Bisel", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        mod.limit_method = "ANGLE"
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if smooth:
        bpy.ops.object.shade_auto_smooth(angle=math.radians(40))
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=1.0, scale_to_bounds=False, correct_aspect=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)
    material(obj, mat)
    return obj


def cube(name, size, loc, mat, rot=(0, 0, 0), bevel=0.01):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    o.scale = size
    return finish(o, mat, bevel, smooth=False)


def cyl(name, r, depth, loc, mat, rot=(0, 0, 0), verts=16, bevel=0.0):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    return finish(o, mat, bevel)


def torus(name, major, minor, loc, mat, rot=(0, 0, 0), seg=24, minor_seg=8, arc=1.0):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=seg, minor_segments=minor_seg, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    if arc < 1.0:
        # Corta el toro para dejar solo un arco (codo de tuberia). Se corta antes de rotar: en espacio local.
        bm = bmesh.new()
        bm.from_mesh(o.data)
        limit = arc * 2 * math.pi
        doomed = [v for v in bm.verts if (math.atan2(v.co.y, v.co.x) % (2 * math.pi)) > limit + 1e-3]
        bmesh.ops.delete(bm, geom=doomed, context="VERTS")
        bm.to_mesh(o.data)
        bm.free()
    return finish(o, mat)


def export(name):
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    for o in bpy.context.scene.objects:
        o.select_set(o.type == "MESH")
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = name
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             mesh_smooth_type="FACE", add_leaf_bones=False, embed_textures=False, path_mode="STRIP")
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print(f"MODELO {name}: {tris} triangulos")


# ---------------- Alcantarilla ----------------
def barril():
    reset()
    cyl("Cuerpo", .3, .88, (0, 0, .44), "Oxido", verts=20)
    for z in (.12, .44, .76):
        torus("Aro", .305, .018, (0, 0, z), "Metal", seg=20, minor_seg=6)
    cyl("Tapa", .27, .02, (0, 0, .885), "Metal", verts=20)
    export("Barril")


def tuberia_codo():
    # Tramo recto de 2 m con bridas, un codo de 90 grados hacia arriba y una valvula de volante.
    reset()
    r = .16
    cyl("Tramo", r, 2.0, (0, 0, 0), "Oxido", rot=(0, math.pi / 2, 0), verts=16)
    for x in (-1.0, 1.0):
        cyl("Brida", r + .05, .07, (x, 0, 0), "Metal", rot=(0, math.pi / 2, 0), verts=16, bevel=.01)
    # Arco de 90 grados en el plano XZ con centro en (1, 0, .45): va de (1,0,0) a (1.45,0,.45).
    torus("Codo", .45, r, (1.0, 0, .45), "Oxido", rot=(-math.pi / 2, 0, 0), seg=24, minor_seg=12, arc=.25)
    cyl("Subida", r, 1.4, (1.45, 0, 1.15), "Oxido", verts=16)
    cyl("Brida", r + .05, .07, (1.45, 0, .47), "Metal", verts=16, bevel=.01)
    # Valvula
    cyl("Cuello", .05, .3, (-.3, 0, .3), "Metal", verts=10)
    torus("Volante", .16, .022, (-.3, 0, .46), "Oxido", seg=20, minor_seg=6)
    for a in range(4):
        cube("Radio", (.32, .02, .02), (-.3, 0, .46), "Oxido", rot=(0, 0, a * math.pi / 4), bevel=0)
    export("Tuberia_Codo")


def rejilla():
    reset()
    s = 1.2
    for x in (-s / 2, s / 2):
        cube("Marco", (.06, s, .06), (x, 0, .03), "Metal")
    for y in (-s / 2, s / 2):
        cube("Marco", (s, .06, .06), (0, y, .03), "Metal")
    for i in range(9):
        cube("Barra", (.025, s, .05), (-s / 2 + (i + 1) * s / 10, 0, .03), "Oxido", bevel=0)
    export("Rejilla")


# ---------------- Laboratorio ----------------
def camilla():
    reset()
    w, l, h = .7, 1.95, .8
    cube("Colchon", (w, l, .12), (0, 0, h), "Bata", bevel=.03)
    cube("Bastidor", (w + .04, l + .04, .05), (0, 0, h - .08), "Mesa")
    for sx in (-1, 1):
        for sy in (-1, 1):
            cyl("Pata", .022, h - .12, (sx * (w / 2 - .04), sy * (l / 2 - .06), (h - .12) / 2 + .06), "Mesa", verts=10)
            cyl("Rueda", .055, .035, (sx * (w / 2 - .04), sy * (l / 2 - .06), .055), "Metal", rot=(0, math.pi / 2, 0), verts=12)
        cyl("Barandilla", .015, l * .7, (sx * (w / 2 + .02), 0, h + .2), "Mesa", rot=(math.pi / 2, 0, 0), verts=8)
        for sy in (-.3, .3):
            cyl("Soporte", .012, .2, (sx * (w / 2 + .02), sy * l, h + .1), "Mesa", verts=8)
    cube("Sabana", (w * .95, l * .45, .02), (.02, -l * .2, h + .07), "Bata", rot=(0, .03, .05), bevel=0)
    export("Camilla")


def estante_frascos():
    reset()
    w, d, h = 1.4, .45, 1.9
    for x in (-w / 2, w / 2):
        for y in (-d / 2, d / 2):
            cube("Poste", (.035, .035, h), (x, y, h / 2), "Casillero", bevel=.004)
    for z in (.1, .55, 1.0, 1.45, 1.88):
        cube("Balda", (w, d, .025), (0, 0, z), "Casillero", bevel=.004)
    k = 0
    for z in (.1, .55, 1.0, 1.45):
        for i in range(5):
            if (i + k) % 4 == 3:
                continue
            x = -w / 2 + .17 + i * .27
            r, hh = .07 + (i % 2) * .02, .22 + ((i + k) % 3) * .06
            cyl("Frasco", r, hh, (x, 0, z + .0125 + hh / 2), "Cristal", verts=14)
            cyl("Liquido", r * .85, hh * .6, (x, 0, z + .0125 + hh * .3), "Liquido", verts=12)
            cyl("Tapa", r * .9, .03, (x, 0, z + .0125 + hh + .015), "Metal", verts=12)
        k += 1
    export("Estante_Frascos")


def carro_instrumental():
    reset()
    w, d = .6, .4
    for z in (.25, .85):
        cube("Bandeja", (w, d, .02), (0, 0, z), "Mesa", bevel=.005)
        for x in (-w / 2, w / 2):
            cube("Borde", (.015, d, .04), (x, 0, z + .02), "Mesa", bevel=0)
    for sx in (-1, 1):
        for sy in (-1, 1):
            cyl("Pata", .012, .85, (sx * (w / 2 - .02), sy * (d / 2 - .02), .45), "Mesa", verts=8)
            cyl("Rueda", .035, .025, (sx * (w / 2 - .02), sy * (d / 2 - .02), .035), "Metal", rot=(0, math.pi / 2, 0), verts=10)
    cyl("Asa", .012, d, (w / 2 + .05, 0, .9), "Mesa", rot=(math.pi / 2, 0, 0), verts=8)
    for i in range(4):
        cube("Instrumento", (.12, .015, .006), (-.15 + i * .1, -.05 + (i % 2) * .08, .87), "Metal", rot=(0, 0, i * .4), bevel=0)
    cyl("Frasco", .04, .12, (.18, .1, .92), "Cristal", verts=12)
    export("Carro_Instrumental")


def tanque_contencion():
    reset()
    cyl("Base", .95, .35, (0, 0, .175), "Metal", verts=28, bevel=.02)
    cyl("Aro bajo", 1.0, .08, (0, 0, .37), "Oxido", verts=28)
    cyl("Cristal", .88, 2.3, (0, 0, 1.55), "Cristal", verts=28)
    cyl("Aro alto", 1.0, .08, (0, 0, 2.72), "Oxido", verts=28)
    cyl("Tapa", .95, .3, (0, 0, 2.9), "Metal", verts=28, bevel=.02)
    for a in range(6):
        ang = a * math.pi / 3
        cyl("Columna", .04, 2.3, (math.cos(ang) * .93, math.sin(ang) * .93, 1.55), "Metal", verts=8)
    cyl("Tubo", .07, 1.2, (0, 0, 3.6), "Oxido", verts=12)
    export("Tanque_Contencion")


for build in (barril, tuberia_codo, rejilla, camilla, estante_frascos, carro_instrumental, tanque_contencion):
    build()
print("MODELOS_OK")
