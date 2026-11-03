"""筑梦地产：精细玩具风 3D 资产。运行：/usr/local/bin/3dpy Tools/Blender/build_cartoon_assets.py"""
import bpy
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
HERE = ROOT / "Tools" / "Blender"
FBX_DIR = ROOT / "Assets" / "Resources" / "Models"
OUT_DIR = HERE / "Output"
IMG_DIR = ROOT / "Docs" / "Images"
for folder in (FBX_DIR, OUT_DIR, IMG_DIR):
    folder.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
ASSETS, MATS = {}, {}


def mat(name, color, roughness=0.68, metallic=0.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    MATS[name] = m
    return m


# 颜色经过 Unity 线性空间校验：保留浓郁色相，避免高明度粉彩泛白。
for name, color, rough, metal in [
    ("Cream", (.98, .84, .58), .74, 0), ("Paper", (.93, .97, .89), .72, 0),
    ("Coral", (1.0, .25, .18), .62, 0), ("Orange", (1.0, .48, .045), .62, 0),
    ("Yellow", (1.0, .74, .08), .60, 0), ("Teal", (.02, .58, .48), .58, 0),
    ("Blue", (.05, .39, .88), .58, 0), ("Glass", (.02, .52, .66), .24, .18),
    ("GlassLight", (.10, .74, .82), .22, .12), ("Navy", (.035, .075, .13), .68, 0),
    ("Steel", (.28, .38, .44), .42, .38), ("Concrete", (.62, .72, .67), .78, 0),
    ("Leaf", (.12, .53, .12), .78, 0), ("LeafLight", (.38, .74, .12), .76, 0),
    ("LeafDark", (.035, .30, .12), .80, 0), ("Grass", (.33, .68, .16), .82, 0),
    ("Wood", (.36, .16, .055), .78, 0), ("Sand", (.83, .57, .25), .80, 0),
    ("Rock", (.30, .39, .42), .84, 0), ("Red", (.82, .035, .055), .62, 0),
    ("Purple", (.38, .16, .61), .64, 0), ("White", (.96, .93, .79), .70, 0),
    ("Soil", (.28, .13, .055), .90, 0), ("Pink", (.94, .20, .37), .66, 0),
]:
    mat(name, color, rough, metal)


def empty(name):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    return obj


def finish(obj, name, material, parent, bevel=0.0, smooth=False):
    obj.name = name
    if material:
        obj.data.materials.append(MATS[material] if isinstance(material, str) else material)
    obj.parent = parent
    if bevel:
        mod = obj.modifiers.new("Crafted soft edges", "BEVEL")
        mod.width, mod.segments = bevel, 3
    if smooth and obj.type == "MESH":
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    return obj


def cube(name, loc, scale, material, parent, bevel=.045, rotation=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if rotation:
        obj.rotation_euler = rotation
    return finish(obj, name, material, parent, bevel)


def cylinder(name, loc, radius, depth, material, parent, vertices=20, rotation=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    obj = bpy.context.object
    if rotation:
        obj.rotation_euler = rotation
    return finish(obj, name, material, parent, .018)


def cone(name, loc, radius1, radius2, depth, material, parent, vertices=20):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1, radius2=radius2, depth=depth, location=loc)
    return finish(bpy.context.object, name, material, parent, .018)


def ico(name, loc, scale, material, parent, subdivisions=2, smooth=True):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=loc)
    obj = bpy.context.object
    obj.scale = scale
    return finish(obj, name, material, parent, 0, smooth)


def beam(name, start, end, radius, material, parent, vertices=12):
    a, b = Vector(start), Vector(end)
    obj = cylinder(name, (a + b) / 2, radius, (b - a).length, material, parent, vertices)
    obj.rotation_euler = (b - a).to_track_quat("Z", "Y").to_euler()
    return obj


def torus(name, loc, major, minor, material, parent, rotation=None):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=24, minor_segments=8, location=loc)
    obj = bpy.context.object
    if rotation:
        obj.rotation_euler = rotation
    return finish(obj, name, material, parent, 0, True)


def register(root):
    ASSETS[root.name] = root
    return root


def site_base(root, color="Sand", width=12, depth=9):
    cube("Beveled site foundation", (0, 0, .12), (width, depth, .24), color, root, .14)
    cube("Foundation shadow course", (0, 0, .035), (width + .12, depth + .12, .10), "Soil", root, .06)


def front_window(root, x, y, z, w=1.25, h=.86, frame="White"):
    cube("Window outer frame", (x, y, z), (w + .16, .13, h + .16), frame, root, .035)
    cube("Recessed turquoise glass", (x, y - .075, z), (w, .045, h), "Glass", root, .025)
    cube("Window center mullion", (x, y - .11, z), (.055, .04, h), frame, root, .012)
    cube("Window sill", (x, y - .12, z - h / 2 - .08), (w + .28, .28, .10), frame, root, .025)


def side_window(root, x, y, z, w=1.05, h=.82):
    cube("Side window frame", (x, y, z), (.13, w + .16, h + .16), "White", root, .03)
    cube("Side glass", (x + .075, y, z), (.045, w, h), "Glass", root, .02)
    cube("Side mullion", (x + .11, y, z), (.04, .055, h), "White", root, .01)


def door(root, x, y, z=1.0, color="Teal"):
    cube("Door stone surround", (x, y, z), (1.55, .18, 2.0), "White", root, .055)
    cube("Door leaf", (x, y - .11, z), (1.25, .08, 1.78), color, root, .04)
    cube("Door upper glass", (x, y - .16, z + .38), (.82, .035, .62), "GlassLight", root, .025)
    cylinder("Door brass knob", (x + .43, y - .21, z - .18), .065, .08, "Yellow", root, 12, (math.pi / 2, 0, 0))


def railing(root, x, y, z, width, color="Teal"):
    cube("Balcony handrail", (x, y, z + .43), (width, .10, .10), color, root, .025)
    cube("Balcony bottom rail", (x, y, z + .06), (width, .08, .08), color, root, .02)
    count = max(2, int(width / .36))
    for i in range(count + 1):
        px = x - width / 2 + width * i / count
        cube("Balcony vertical baluster", (px, y, z + .24), (.055, .065, .40), color, root, .012)


def planter(root, x, y, z):
    cube("Terrace planter", (x, y, z), (.72, .42, .30), "Wood", root, .045)
    for dx in (-.22, .20):
        ico("Planter leaves", (x + dx, y, z + .28), (.28, .24, .28), "LeafLight", root, 2)
        ico("Planter flower", (x + dx, y - .05, z + .50), (.09, .09, .09), "Pink", root, 1)


def make_stage(index):
    root = register(empty(f"SM_Building_Stage{index}"))
    if index == 0:
        site_base(root)
        cube("Site office deck", (-3.7, 2.6, .28), (3.0, 2.1, .22), "Concrete", root)
        for x in (-5, 0, 5):
            for y in (-3.6, 3.6):
                cylinder("Survey stake", (x, y, .68), .085, 1.10, "Orange", root, 10)
                cube("Survey flag", (x + .25, y, 1.02), (.48, .055, .30), "Coral", root, .02)
        for x in (-4.8, 4.8):
            for y in (-2.8, 0, 2.8):
                cone("Safety cone", (x, y, .48), .24, .055, .72, "Orange", root, 16)
                cube("Cone rubber foot", (x, y, .16), (.55, .55, .10), "Navy", root, .035)
                cylinder("Cone reflective band", (x, y, .53), .14, .14, "White", root, 16)
        for x, y in ((-2, -1), (2.8, 2), (0, 3)):
            ico("Site rock", (x, y, .36), (.48, .38, .31), "Rock", root, 1, False)
        return root
    if index == 1:
        site_base(root, "Concrete")
        cube("Deep foundation mat", (0, 0, .42), (10.4, 7.4, .62), "Navy", root, .10)
        cube("Foundation top", (0, 0, .76), (10.0, 7.0, .14), "Concrete", root)
        for x in (-3.7, 0, 3.7):
            for y in (-2.4, 0, 2.4):
                cylinder("Reinforced pile", (x, y, 1.35), .28, 1.45, "Concrete", root)
                cylinder("Pile safety collar", (x, y, 1.98), .35, .18, "Orange", root, 16)
                for dx in (-.11, .11):
                    beam("Exposed rebar", (x + dx, y, 2.02), (x + dx, y, 2.50), .025, "Steel", root, 8)
        for y in (-3.45, 3.45):
            railing(root, 0, y, .84, 9.7, "Orange")
        return root
    if index == 2:
        site_base(root, "Concrete")
        for floor in range(3):
            z = .55 + floor * 1.72
            cube("Structural floor slab", (0, 0, z), (9.8, 6.8, .24), "Concrete", root, .05)
            for x in (-4.3, 0, 4.3):
                for y in (-2.8, 2.8):
                    cube("Structural column", (x, y, z + .81), (.34, .34, 1.52), "Paper", root, .035)
                    cube("Column foot", (x, y, z + .18), (.52, .52, .18), "Steel", root, .025)
            railing(root, 0, -3.25, z + .15, 9.5, "Orange")
            railing(root, 0, 3.25, z + .15, 9.5, "Orange")
        cube("Construction elevator core", (1.8, .8, 3.0), (2.1, 2.1, 5.1), "Coral", root, .09)
        for z in (1.3, 3.0, 4.7):
            cube("Core opening", (1.8, -.29, z), (1.15, .06, .85), "Navy", root, .025)
        for i in range(6):
            cube("External stair tread", (-4.7 + i * .28, 2.7, .5 + i * .25), (.65, 1.0, .12), "Steel", root, .02)
        return root
    if index == 3:
        site_base(root, "Concrete")
        for floor in range(6):
            z = .58 + floor * 1.53
            cube("Topped floor slab", (0, 0, z), (9.5, 6.6, .22), "Coral" if floor == 5 else "Concrete", root, .05)
            cube("Partial masonry wall", (0, .40, z + .72), (8.8, 5.3, 1.28), "Cream", root, .07)
            for x in (-2.7, 0, 2.7):
                if floor < 4:
                    front_window(root, x, -2.30, z + .74, 1.5, .82)
                else:
                    cube("Open window void", (x, -2.30, z + .74), (1.62, .10, .94), "Navy", root, .025)
            railing(root, 0, -3.15, z + .12, 9.1, "Teal" if floor < 3 else "Orange")
        cube("Roof parapet", (0, 0, 9.78), (9.8, 6.9, .44), "Orange", root, .11)
        for x in (-3.8, 3.8):
            cube("Roof safety post", (x, 2.8, 10.35), (.10, .10, 1.1), "Steel", root, .02)
        return root

    # 成品住宅：六层完整体量、三面窗、挑出阳台、入口、屋顶设备和花园。
    site_base(root, "Grass")
    cube("Entrance stone plaza", (0, -4.0, .24), (8.4, 2.0, .20), "Cream", root, .08)
    for i in range(3):
        cube("Entrance step", (0, -4.65 + i * .28, .26 + i * .12), (2.8 - i * .25, .70, .16), "White", root, .04)
    for floor in range(6):
        z = .52 + floor * 1.50
        cube("Residential floor volume", (0, 0, z + .67), (8.5, 5.8, 1.32), "Cream" if floor % 2 == 0 else "Paper", root, .10)
        cube("Floor shadow band", (0, 0, z + .06), (8.75, 6.05, .14), "Coral", root, .035)
        cube("Projecting balcony slab", (0, -3.10, z + .10), (9.05, .82, .20), "Coral", root, .055)
        for x in (-2.65, 0, 2.65):
            front_window(root, x, -2.94, z + .74, 1.55, .86)
            railing(root, x, -3.48, z + .18, 1.85)
        for y in (-1.55, 1.15):
            side_window(root, 4.29, y, z + .74, 1.18, .82)
        for y in (-1.55, 1.15):
            cube("Rear window frame", (-4.29, y, z + .74), (.13, 1.28, .98), "White", root, .03)
            cube("Rear glass", (-4.37, y, z + .74), (.04, 1.10, .80), "Glass", root, .02)
        if floor % 2 == 0:
            for x in (-3.65, 3.65):
                planter(root, x, -3.40, z + .38)
        if floor in (1, 3, 5):
            cube("Air conditioner", (4.48, 2.10, z + .58), (.48, .76, .50), "Concrete", root, .05)
            for yy in (1.88, 2.08, 2.28):
                cube("AC grille", (4.74, yy, z + .58), (.025, .07, .34), "Navy", root, .008)
    cube("Thick overhanging roof", (0, 0, 9.60), (9.35, 6.75, .44), "Coral", root, .14)
    cube("Roof inner terrace", (0, 0, 9.87), (8.65, 6.05, .16), "Sand", root, .07)
    cube("Roof garden room", (-1.55, .55, 10.52), (3.55, 3.15, 1.28), "Yellow", root, .12)
    front_window(root, -1.55, -1.06, 10.55, 1.50, .66)
    cube("Roof room cap", (-1.55, .55, 11.24), (3.90, 3.48, .20), "Orange", root, .07)
    for x in (1.3, 2.5, 3.7):
        cube("Solar panel frame", (x, .7, 10.08), (.95, 1.95, .12), "White", root, .025, (.18, 0, 0))
        cube("Solar panel cells", (x, .7, 10.16), (.82, 1.80, .045), "Navy", root, .015, (.18, 0, 0))
        for yy in (.25, .70, 1.15):
            cube("Solar cell divider", (x, yy, 10.23), (.82, .025, .025), "GlassLight", root, .005, (.18, 0, 0))
    door(root, 0, -3.01, 1.16)
    cube("Entrance canopy", (0, -3.70, 2.32), (3.15, 1.35, .22), "Yellow", root, .08)
    for x in (-1.25, 1.25):
        cylinder("Canopy column", (x, -4.12, 1.25), .10, 2.05, "White", root, 16)
    for x in (-4.85, 4.85):
        cylinder("Garden trunk", (x, 2.7, .75), .16, 1.25, "Wood", root, 12)
        ico("Garden crown", (x, 2.7, 1.85), (.85, .80, 1.05), "Leaf", root, 2)
        ico("Garden crown light", (x - .35, 2.45, 1.75), (.55, .52, .64), "LeafLight", root, 2)
    return root


def wheels(root, xs, y=1.05, z=.62, radius=.58):
    for x in xs:
        for side in (-1, 1):
            cylinder("Chunky tire", (x, side * y, z), radius, .40, "Navy", root, 24, (math.pi / 2, 0, 0))
            cylinder("Wheel rim", (x, side * (y + .22), z), radius * .55, .065, "Steel", root, 20, (math.pi / 2, 0, 0))
            cylinder("Bright wheel hub", (x, side * (y + .27), z), radius * .28, .08, "Yellow", root, 16, (math.pi / 2, 0, 0))
            for a in range(6):
                angle = a * math.tau / 6
                cylinder("Wheel lug", (x + math.cos(angle) * radius * .36, side * (y + .27), z + math.sin(angle) * radius * .36), .035, .04, "White", root, 8, (math.pi / 2, 0, 0))


def truck_cab(root, x, color):
    cube("Cab lower body", (x, 0, 1.30), (1.90, 2.10, .85), color, root, .16)
    cube("Cab upper body", (x - .10, 0, 2.00), (1.65, 2.02, .80), color, root, .15)
    cube("Front windshield", (x + .75, 0, 2.04), (.065, 1.62, .60), "Glass", root, .035)
    for side in (-1, 1):
        cube("Cab side window", (x - .08, side * 1.035, 2.04), (1.12, .055, .58), "Glass", root, .035)
        cube("Cab door handle", (x - .42, side * 1.08, 1.52), (.30, .06, .07), "White", root, .018)
        beam("Mirror arm", (x + .52, side * 1.03, 2.10), (x + .72, side * 1.32, 2.10), .035, "Steel", root)
        cube("Wing mirror", (x + .72, side * 1.35, 2.10), (.20, .10, .30), "Navy", root, .035)
        cube("Headlamp", (x + .98, side * .68, 1.30), (.08, .40, .28), "Yellow", root, .04)
    cube("Front bumper", (x + 1.00, 0, .98), (.18, 2.14, .22), "Steel", root, .05)
    for yy in (-.48, 0, .48):
        cube("Front grille", (x + .99, yy, 1.52), (.07, .32, .055), "Navy", root, .012)
    cube("Cab roof", (x - .10, 0, 2.48), (1.83, 2.16, .16), color, root, .07)


def make_pump():
    root = register(empty("SM_PumpTruck"))
    cube("Heavy chassis", (0, 0, .82), (6.7, 2.25, .44), "Navy", root, .10)
    truck_cab(root, 2.20, "Cream")
    cube("Pump equipment bed", (-1.20, 0, 1.28), (3.9, 2.12, .68), "Coral", root, .12)
    for x in (-2.5, -1.7, -.9):
        cube("Equipment access panel", (x, -1.09, 1.30), (.62, .055, .40), "Orange", root, .035)
    wheels(root, (-2.1, -.8, 2.1))
    cylinder("Rotating turret", (-.8, 0, 1.78), .78, .48, "Coral", root, 24)
    joints = [(-.8, 0, 1.95), (-2.5, 0, 5.15), (1.8, 0, 6.95), (3.7, 0, 5.2)]
    for i in range(3):
        beam(f"Hydraulic boom {i}", joints[i], joints[i + 1], .23 - i * .035, "Yellow" if i == 1 else "Orange", root, 16)
        cylinder("Boom pivot", joints[i + 1], .34 - i * .04, .60, "Steel", root, 20, (math.pi / 2, 0, 0))
    beam("Hydraulic piston", (-.65, -.28, 2.25), (-1.9, -.28, 4.45), .075, "Steel", root)
    beam("Boom hydraulic piston", (-2.15, -.25, 5.05), (.8, -.25, 6.45), .065, "Steel", root)
    beam("Flexible concrete hose", joints[-1], (3.7, 0, 2.0), .085, "Navy", root)
    cone("Hose nozzle", (3.7, 0, 1.85), .16, .10, .32, "Steel", root)
    for x in (-2.3, 1.0):
        for side in (-1, 1):
            beam("Outrigger", (x, side * .8, 1.0), (x, side * 2.0, .45), .09, "Steel", root)
            cube("Outrigger foot", (x, side * 2.0, .20), (.65, .65, .16), "Navy", root, .04)


def make_truck():
    root = register(empty("SM_TransportTruck"))
    cube("Truck ladder chassis", (0, 0, .76), (6.0, 2.14, .38), "Navy", root, .08)
    truck_cab(root, 1.98, "Blue")
    cube("Cargo bed floor", (-1.20, 0, 1.12), (3.85, 2.18, .28), "Orange", root, .07)
    for side in (-1, 1):
        cube("Cargo side board", (-1.20, side * 1.04, 1.58), (3.85, .16, .85), "Orange", root, .055)
        for x in (-2.85, -1.25, .45):
            cube("Cargo side rib", (x, side * 1.14, 1.58), (.12, .10, .88), "Yellow", root, .025)
    cube("Cargo tailgate", (-3.08, 0, 1.58), (.16, 2.18, .85), "Orange", root, .05)
    for x in (-2.35, -1.45, -.55):
        for y in (-.48, .48):
            cube("Bagged building material", (x, y, 1.82), (.72, .78, .52), "Cream", root, .12)
            cube("Material strap", (x, y, 2.09), (.12, .80, .045), "Coral", root, .015)
    wheels(root, (-1.95, -.75, 1.9), 1.04, .60)
    for x in (-2.6, .3):
        beam("Cargo tie down", (x, -1.05, 2.18), (x, 1.05, 2.18), .035, "Navy", root)


def make_excavator():
    root = register(empty("SM_Excavator"))
    for y in (-.92, .92):
        cube("Track frame", (-.4, y, .62), (4.15, .78, .88), "Navy", root, .24)
        for x in (-1.75, -1.1, -.4, .3, .95):
            cylinder("Track roller", (x, y * 1.06, .62), .34, .15, "Steel", root, 18, (math.pi / 2, 0, 0))
        for i in range(13):
            x = -2.15 + i * .29
            for z in (.20, 1.04):
                cube("Individual track pad", (x, y, z), (.23, .88, .10), "Steel", root, .025)
        for x in (-2.28, 1.48):
            for z in (.40, .62, .84):
                cube("Track end pad", (x, y, z), (.10, .88, .18), "Steel", root, .025)
    cylinder("Slew ring", (-.3, 0, 1.18), 1.12, .34, "Steel", root, 28)
    cube("Rotating upper base", (-.3, 0, 1.48), (3.4, 2.25, .52), "Yellow", root, .13)
    cube("Rounded engine cowling", (-1.15, .15, 2.14), (2.05, 1.95, 1.25), "Orange", root, .18)
    for x in (-1.85, -1.60, -1.35):
        cube("Engine ventilation slot", (x, -.86, 2.20), (.10, .045, .56), "Navy", root, .015)
    cube("Cab frame", (.58, -.40, 2.42), (1.36, 1.40, 1.78), "Yellow", root, .14)
    cube("Cab front glass", (1.29, -.40, 2.52), (.055, 1.12, 1.24), "Glass", root, .04)
    cube("Cab side glass", (.58, -1.13, 2.52), (1.02, .045, 1.24), "Glass", root, .04)
    cube("Cab roof", (.58, -.40, 3.36), (1.55, 1.57, .16), "Navy", root, .06)
    cylinder("Exhaust", (-1.55, .55, 3.02), .10, .85, "Steel", root, 14)
    a, b, c = (.3, .55, 2.45), (2.65, .55, 5.50), (4.45, .55, 2.80)
    beam("Main boom", a, b, .26, "Yellow", root, 16)
    beam("Dipper stick", b, c, .21, "Orange", root, 16)
    for p in (a, b, c):
        cylinder("Arm pivot pin", p, .31, .52, "Steel", root, 20, (math.pi / 2, 0, 0))
    beam("Boom hydraulic barrel", (.15, .25, 2.30), (1.55, .25, 4.20), .11, "Steel", root)
    beam("Boom hydraulic rod", (1.55, .25, 4.20), (2.25, .25, 5.05), .065, "White", root)
    cube("Bucket shell", (4.70, .55, 2.20), (1.28, 1.50, .92), "Steel", root, .16, (0, .35, 0))
    cube("Bucket cutting edge", (5.23, .55, 1.84), (.20, 1.58, .18), "Navy", root, .035, (0, .35, 0))
    for y in (-.05, .35, .75, 1.15):
        cube("Bucket tooth", (5.38, y, 1.72), (.42, .16, .15), "Steel", root, .025, (0, .35, 0))


def make_crane():
    root = register(empty("SM_TowerCrane"))
    cube("Crane concrete plinth", (0, 0, .28), (3.3, 3.3, .56), "Concrete", root, .10)
    cube("Crane base frame", (0, 0, .70), (1.65, 1.65, .28), "Steel", root, .05)
    for z in range(1, 13):
        for x, y in ((-.46, -.46), (.46, -.46), (-.46, .46), (.46, .46)):
            beam("Mast upright", (x, y, z), (x, y, z + 1), .072, "Orange", root)
        for y in (-.46, .46):
            beam("Mast X brace A", (-.46, y, z), (.46, y, z + 1), .042, "Yellow", root, 8)
            beam("Mast X brace B", (.46, y, z), (-.46, y, z + 1), .042, "Yellow", root, 8)
        for x in (-.46, .46):
            beam("Mast side brace", (x, -.46, z), (x, .46, z + 1), .042, "Orange", root, 8)
        cube("Mast horizontal collar", (0, 0, z), (1.08, 1.08, .08), "Orange", root, .015)
    cylinder("Slewing platform", (0, 0, 13.12), .92, .32, "Steel", root, 24)
    cube("Operator cab", (.86, -.62, 13.10), (1.32, 1.15, 1.10), "Cream", root, .12)
    cube("Operator front glass", (1.54, -.62, 13.17), (.055, .90, .65), "Glass", root, .03)
    cube("Operator side glass", (.86, -1.22, 13.17), (.96, .045, .65), "Glass", root, .03)
    for y in (-.34, .34):
        beam("Main jib lower chord", (-5.8, y, 13.70), (8.3, y, 13.70), .10, "Orange", root)
        beam("Main jib upper chord", (-5.8, y, 14.35), (8.3, y, 14.35), .075, "Yellow", root)
        for i in range(14):
            x = -5.8 + i
            beam("Jib triangular web", (x, y, 13.70), (x + .5, y, 14.35), .042, "Yellow", root, 8)
            beam("Jib vertical web", (x, y, 13.70), (x, y, 14.35), .04, "Orange", root, 8)
    for i in range(4):
        cube("Concrete counterweight", (-5.25 + i * .38, 0, 13.12), (.30, 1.65, 1.05), "Navy", root, .055)
    beam("A frame left", (0, -.40, 13.5), (0, -.40, 16.0), .09, "Orange", root)
    beam("A frame right", (0, .40, 13.5), (0, .40, 16.0), .09, "Orange", root)
    beam("A frame tie", (0, -.40, 16), (0, .40, 16), .08, "Yellow", root)
    for y in (-.28, .28):
        beam("Jib suspension cable", (0, y, 16), (7.2, y, 14.35), .025, "Steel", root, 8)
        beam("Counter jib cable", (0, y, 16), (-5.2, y, 14.35), .025, "Steel", root, 8)
    cube("Trolley", (5.8, 0, 13.55), (.75, .95, .30), "Coral", root, .05)
    for y in (-.16, .16):
        beam("Hoist cable", (5.8, y, 13.45), (5.8, y, 6.10), .026, "Navy", root, 8)
    cube("Hook block", (5.8, 0, 5.92), (.46, .46, .48), "Coral", root, .07)
    torus("Lifting hook", (5.8, 0, 5.47), .24, .065, "Steel", root, (math.pi / 2, 0, 0))


def make_supplier():
    root = register(empty("SM_Supplier"))
    site_base(root, "Concrete", 11.5, 10.5)
    cube("Warehouse main volume", (0, -.8, 1.75), (8.4, 6.0, 3.5), "Cream", root, .15)
    cube("Warehouse plinth", (0, -.8, .40), (8.65, 6.25, .50), "Concrete", root, .08)
    pitched_roof(root, 8.4, 6.0, 3.95, "Blue", center_y=-.8)
    cube("Loading door frame", (0, -3.86, 1.43), (3.25, .18, 2.65), "White", root, .06)
    cube("Roller loading door", (0, -3.97, 1.43), (2.90, .08, 2.36), "Navy", root, .035)
    for z in (.55, .90, 1.25, 1.60, 1.95, 2.30):
        cube("Roller shutter slat", (0, -4.03, z), (2.86, .035, .045), "Steel", root, .008)
    for x in (-2.85, 2.85):
        front_window(root, x, -3.84, 2.12, 1.15, .90)
    for i in range(7):
        cube("Awning stripe", (-1.95 + i * .65, -4.37, 2.96), (.64, 1.12, .18), "Yellow" if i % 2 == 0 else "White", root, .025, (.10, 0, 0))
    for x in (-2.7, 0, 2.7):
        cylinder("Storage silo", (x, 3.55, 2.55), .96, 4.55, "Paper", root, 28)
        cone("Silo conical cap", (x, 3.55, 5.05), 1.05, .16, .55, "Coral", root, 28)
        for z in (1.0, 2.6, 4.3):
            torus("Silo reinforcing ring", (x, 3.55, z), .98, .045, "Steel", root)
        for dx in (-.60, .60):
            beam("Silo support leg", (x + dx, 3.55, .25), (x + dx, 3.55, 1.2), .08, "Steel", root)
        beam("Silo feed pipe", (x, 3.55, 4.9), (x, 2.4, 4.4), .10, "Steel", root)
    for x in (-4.4, 4.4):
        for y in (-3.5, 2.2):
            cube("Safety bollard", (x, y, .58), (.18, .18, .90), "Yellow", root, .04)


def pitched_roof(root, sx, sy, z, material, center_x=0, center_y=0):
    # 封闭五面三棱柱：两个坡面、两个山墙和底面均有真实厚度。
    hx, hy, rise, thick = (sx + .75) / 2, (sy + .70) / 2, sy * .20, .24
    verts = []
    section = [(-hy, z), (0, z + rise), (hy, z), (hy, z - thick), (-hy, z - thick)]
    for x in (-hx, hx):
        verts.extend([(x, y, zz) for y, zz in section])
    faces = [(0, 2, 1), (0, 3, 2), (0, 4, 3), (5, 6, 7), (5, 7, 8), (5, 8, 9),
             (0, 1, 6, 5), (1, 2, 7, 6), (2, 3, 8, 7), (3, 4, 9, 8), (4, 0, 5, 9)]
    mesh = bpy.data.meshes.new("Solid gable roof mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Solid pitched roof", mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = (center_x, center_y, 0)
    finish(obj, "Solid pitched roof", material, root, .055)
    cube("Roof ridge cap", (center_x, center_y, z + rise + .04), (sx + .90, .24, .18), material, root, .05)


def make_office(name, wall, roof, size, kind):
    root = register(empty(name))
    sx, sy, sz = size
    site_base(root, "Grass", sx + 2.0, sy + 2.4)
    cube("Stone building plinth", (0, 0, .32), (sx + .30, sy + .30, .46), "Concrete", root, .08)
    cube("Main building volume", (0, 0, sz / 2 + .28), size, wall, root, .14)
    cube("Wall base course", (0, 0, .62), (sx + .14, sy + .14, .28), "White", root, .045)
    pitched_roof(root, sx, sy, sz + .55, roof)
    door(root, 0, -sy / 2 - .08, 1.30)
    for x in (-sx * .30, sx * .30):
        front_window(root, x, -sy / 2 - .06, sz * .58, 1.18 if sx < 7 else 1.42, 1.02)
    for y in (-sy * .23, sy * .23):
        side_window(root, sx / 2 + .04, y, sz * .58, 1.05, .94)
    cube("Entrance canopy", (0, -sy / 2 - .65, 2.62), (2.85, 1.25, .22), "Yellow", root, .07)
    for x in (-1.10, 1.10):
        cylinder("Porch column", (x, -sy / 2 - 1.02, 1.40), .095, 2.20, "White", root, 16)
    for i in range(2):
        cube("Porch step", (0, -sy / 2 - 1.15 + i * .28, .28 + i * .13), (2.65 - i * .18, .75, .16), "White", root, .04)
    if kind == "hospital":
        cube("Hospital sign board", (0, -sy / 2 - .17, sz - .35), (2.25, .18, 1.90), "White", root, .10)
        cube("Red cross horizontal", (0, -sy / 2 - .29, sz - .35), (1.45, .10, .43), "Red", root, .035)
        cube("Red cross vertical", (0, -sy / 2 - .30, sz - .35), (.43, .10, 1.45), "Red", root, .035)
        cube("Hospital upper floor", (0, .4, sz + 1.15), (sx * .56, sy * .60, 1.45), "Paper", root, .12)
        cube("Hospital upper cap", (0, .4, sz + 1.98), (sx * .62, sy * .66, .24), "Coral", root, .08)
        for x in (-1.6, 0, 1.6):
            front_window(root, x, .4 - sy * .30 - .04, sz + 1.18, 1.0, .74)
    elif kind == "school":
        cube("Clock tower", (0, .25, sz + 1.35), (2.05, 1.85, 2.05), "Cream", root, .10)
        pitched_roof(root, 2.05, 1.85, sz + 2.55, "Blue", center_y=.25)
        cylinder("Clock rim", (0, -.72, sz + 1.47), .62, .15, "Yellow", root, 32, (math.pi / 2, 0, 0))
        cylinder("Clock face", (0, -.81, sz + 1.47), .52, .055, "White", root, 32, (math.pi / 2, 0, 0))
        beam("Clock hour hand", (0, -.86, sz + 1.47), (-.23, -.86, sz + 1.62), .025, "Navy", root, 8)
        beam("Clock minute hand", (0, -.87, sz + 1.47), (.30, -.87, sz + 1.63), .020, "Navy", root, 8)
        for x in (-sx / 2 - .6, sx / 2 + .6):
            ico("School shrub", (x, -sy / 2 + .2, .70), (.70, .65, .62), "LeafLight", root)
    else:
        cube("Company sign board", (0, -sy / 2 - .17, sz - .25), (2.85, .16, .66), "Yellow", root, .06)
        for x in (-.75, 0, .75):
            cube("Sign graphic", (x, -sy / 2 - .27, sz - .25), (.40, .05, .30), "Teal", root, .025)
        if kind == "hq":
            cube("HQ upper storey", (-.45, .3, sz + 1.15), (sx * .65, sy * .68, 1.45), "Teal", root, .12)
            cube("HQ upper roof", (-.45, .3, sz + 1.98), (sx * .72, sy * .75, .24), "Orange", root, .08)
            for x in (-1.7, -.45, .8):
                front_window(root, x, .3 - sy * .34 - .04, sz + 1.17, .85, .72)
            cylinder("Rooftop flagpole", (sx * .32, 0, sz + 1.55), .055, 2.5, "Steel", root, 12)
            cube("Company flag", (sx * .32 + .45, 0, sz + 2.40), (.85, .055, .52), "Coral", root, .025)
    return root


def make_tree():
    root = register(empty("SM_Tree"))
    cylinder("Tapered trunk", (0, 0, 1.08), .24, 2.16, "Wood", root, 14)
    cone("Trunk base flare", (0, 0, .26), .38, .24, .52, "Wood", root, 14)
    for angle in (0, 2.1, 4.2):
        beam("Tree branch", (0, 0, 1.45), (math.cos(angle) * .68, math.sin(angle) * .68, 2.30), .11, "Wood", root)
    ico("Crown center", (0, 0, 3.00), (1.38, 1.28, 1.48), "Leaf", root, 3)
    ico("Crown light", (-.72, -.25, 2.73), (.98, .91, 1.03), "LeafLight", root, 2)
    ico("Crown dark", (.77, .28, 2.72), (.94, .88, 1.06), "LeafDark", root, 2)
    ico("Crown top", (.10, .10, 3.83), (.78, .72, .78), "LeafLight", root, 2)


def make_rock():
    root = register(empty("SM_Rock"))
    ico("Main faceted rock", (0, 0, .72), (1.20, .94, .82), "Rock", root, 2, False)
    ico("Small rock", (.95, -.34, .38), (.58, .50, .40), "Concrete", root, 1, False)
    ico("Rock foot", (-.72, -.42, .24), (.42, .36, .25), "Rock", root, 1, False)


def make_streetlight():
    root = register(empty("SM_Streetlight"))
    cube("Lamp base", (0, 0, .14), (.62, .62, .28), "Concrete", root, .07)
    cylinder("Lamp post", (0, 0, 2.10), .095, 4.05, "Navy", root, 16)
    cylinder("Post collar", (0, 0, .58), .16, .42, "Steel", root, 16)
    beam("Curved lamp arm", (0, 0, 4.02), (.85, 0, 4.45), .075, "Navy", root)
    cube("Lamp housing", (1.02, 0, 4.40), (.72, .44, .24), "Navy", root, .09)
    cube("Warm lamp diffuser", (1.02, 0, 4.25), (.56, .34, .08), "Yellow", root, .04)


def make_flowerbed():
    root = register(empty("SM_FlowerBed"))
    cube("Raised planter stone", (0, 0, .24), (2.45, 1.20, .48), "Cream", root, .12)
    cube("Planter soil", (0, 0, .50), (2.16, .92, .10), "Soil", root, .05)
    for i in range(7):
        x = - .90 + i * .30
        y = .18 if i % 2 else -.18
        beam("Flower stem", (x, y, .53), (x, y, .92 + (i % 3) * .06), .025, "Leaf", root, 8)
        ico("Flower leaves", (x, y, .66), (.20, .17, .12), "LeafLight", root, 1)
        ico("Flower blossom", (x, y, .98 + (i % 3) * .06), (.14, .14, .12), "Pink" if i % 3 else "Yellow", root, 2)


def make_bench():
    root = register(empty("SM_Bench"))
    for x in (-.90, .90):
        for y in (-.30, .30):
            cube("Bench steel leg", (x, y, .40), (.12, .12, .75), "Navy", root, .03)
        beam("Bench back support", (x, .28, .35), (x, .43, 1.35), .055, "Navy", root)
    for y in (-.30, -.10, .10, .30):
        cube("Wooden seat slat", (0, y, .80), (2.35, .16, .12), "Wood", root, .035)
    for z in (1.05, 1.28):
        cube("Wooden back slat", (0, .42, z), (2.35, .12, .18), "Wood", root, .035)


def make_villa():
    root = register(empty("SM_Villa"))
    site_base(root, "Grass", 9.6, 8.6)
    cube("Villa main body", (0, .35, 1.90), (6.6, 5.1, 3.45), "Cream", root, .15)
    pitched_roof(root, 6.6, 5.1, 3.92, "Coral")
    cube("Villa side wing", (-3.55, .9, 1.40), (2.25, 3.9, 2.45), "Paper", root, .12)
    pitched_roof(root, 2.25, 3.9, 2.85, "Blue", center_x=-3.55, center_y=.9)
    door(root, .6, -2.27, 1.25)
    for x in (-1.65, 2.25):
        front_window(root, x, -2.24, 2.02, 1.18, 1.10)
    for y in (-.85, 1.35):
        side_window(root, 3.35, y, 2.02, 1.10, 1.05)
    cube("Entry porch", (.6, -2.85, 2.55), (2.6, 1.15, .20), "Yellow", root, .07)
    for x in (-.35, 1.55):
        cylinder("Porch post", (x, -3.18, 1.36), .09, 2.1, "White", root, 14)
    cube("Chimney", (-1.8, 1.0, 4.45), (.70, .75, 1.65), "Coral", root, .07)
    cube("Chimney cap", (-1.8, 1.0, 5.32), (.88, .92, .18), "White", root, .04)
    for x in (-3.8, 3.8):
        ico("Villa garden shrub", (x, -2.7, .65), (.68, .58, .60), "LeafLight", root, 2)
    return root


for i in range(5):
    make_stage(i)
make_pump()
make_truck()
make_excavator()
make_crane()
make_supplier()
make_office("SM_TeamOffice", "Orange", "Navy", (5.2, 4.0, 3.0), "office")
make_office("SM_School", "Cream", "Blue", (9.0, 5.4, 4.1), "school")
make_office("SM_Hospital", "Paper", "Coral", (8.4, 6.0, 5.0), "hospital")
make_office("SM_HQ", "Teal", "Orange", (6.4, 5.0, 4.2), "hq")
make_tree()
make_rock()
make_streetlight()
make_flowerbed()
make_bench()
make_villa()


def export_fbx(root):
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for child in root.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_DIR / f"{root.name}.fbx"), use_selection=True,
        axis_forward="-Z", axis_up="Y", apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL", use_mesh_modifiers=True,
        add_leaf_bones=False, bake_anim=False,
    )
    print(f"EXPORTED {root.name}")


for asset in ASSETS.values():
    export_fbx(asset)

layout = {
    "SM_Building_Stage0": (-27, 12, 0), "SM_Building_Stage1": (-10, 12, 0),
    "SM_Building_Stage2": (7, 12, 0), "SM_Building_Stage3": (25, 12, 0),
    "SM_Building_Stage4": (45, 12, 0), "SM_TowerCrane": (-31, 34, 0),
    "SM_Excavator": (-15, 32, 0), "SM_PumpTruck": (1, 33, 0),
    "SM_TransportTruck": (17, 32, 0), "SM_Supplier": (35, 34, 0),
    "SM_TeamOffice": (-28, -11, 0), "SM_School": (-13, -11, 0),
    "SM_Hospital": (5, -11, 0), "SM_HQ": (25, -11, 0),
    "SM_Tree": (39, -10, 0), "SM_Rock": (45, -10, 0),
    "SM_Streetlight": (-28, -25, 0), "SM_FlowerBed": (-21, -25, 0),
    "SM_Bench": (-15, -25, 0), "SM_Villa": (0, -26, 0),
}
for name, loc in layout.items():
    ASSETS[name].location = loc

cube("Presentation grass", (7, 8, -.38), (1000, 1000, .55), "Grass", None, .10)
world = bpy.data.worlds.new("Bright toy sky")
bpy.context.scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (.45, .76, .90, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = .58
bpy.ops.object.light_add(type="SUN", location=(10, -20, 45))
sun = bpy.context.object
sun.name = "Warm sunlight"
sun.data.energy = 1.7
sun.rotation_euler = (math.radians(26), math.radians(-28), math.radians(-32))
bpy.ops.object.light_add(type="AREA", location=(0, -25, 35))
area = bpy.context.object
area.data.energy = 1500
area.data.shape = "DISK"
area.data.size = 38
area.rotation_euler = (Vector((4, 10, 0)) - area.location).to_track_quat("-Z", "Y").to_euler()
bpy.ops.object.camera_add(location=(78, -99, 86))
cam = bpy.context.object
cam.name = "CartoonHeroCamera"
target = Vector((7, 7, 4))
cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
cam.data.type = "ORTHO"
cam.data.ortho_scale = 106
cam.data.clip_start = .1
cam.data.clip_end = 500
scene = bpy.context.scene
scene.camera = cam
scene.render.engine = "CYCLES"
scene.cycles.samples = 48
scene.cycles.use_denoising = True
scene.render.resolution_x = 1800
scene.render.resolution_y = 1350
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.render.filepath = str(IMG_DIR / "卡通资产总览.png")
scene.view_settings.view_transform = "Standard"
scene.view_settings.look = "None"
scene.view_settings.exposure = .12
scene.view_settings.gamma = 1.0
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT_DIR / "CartoonRealEstateAssets.blend"))
bpy.ops.render.render(write_still=True)
# 第二张近景专门检查立体细节，不改变 blend 中保存的总览相机。
cam.location = (74, -64, 59)
target = Vector((29, 6, 4))
cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
cam.data.ortho_scale = 64
scene.render.resolution_x = 1800
scene.render.resolution_y = 1100
scene.render.filepath = str(IMG_DIR / "精细模型近景.png")
bpy.ops.render.render(write_still=True)
print(f"DONE: {len(ASSETS)} detailed cartoon assets, FBX={FBX_DIR}")
