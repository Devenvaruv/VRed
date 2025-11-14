"""
batch_ai_mesh_lod_generator_dedup.py
Generates LOD versions of GLBs with UV transfer and deduplicated names.
"""

import bpy, bmesh, os, datetime
from collections import defaultdict

# ───── USER SETTINGS ──────────────────────────────────────────────────────────
SOURCE_DIR = r"C:\Users\varud\Documents\My Web Sites\My project (2)\Assets\Models\AI Medival Floor - Copy"
TRI_LEVELS = [3000, 1500, 500]  # LOD0 → LOD2
OUT_SUBFOLDER = "Processed_LOD"
# ──────────────────────────────────────────────────────────────────────────────

name_counts = defaultdict(int)  # For deduplicating base names

def log(msg): print(f"[{datetime.datetime.now():%H:%M:%S}] {msg}")
def tri_equiv(me): return sum(len(p.vertices)-2 for p in me.polygons)
def stats(obj, tag=""): return f"{tag} ↳ V:{len(obj.data.vertices):>6,} | T:{tri_equiv(obj.data):>6,}"

def normalize_scale_to_1_meter(obj):
    dims = obj.dimensions
    max_dim = max(dims)
    if max_dim == 0:
        return  # Avoid division by zero
    scale_factor = 1.0 / max_dim
    obj.scale = [s * scale_factor for s in obj.scale]
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(scale=True)

def clean_object(obj):
    bm = bmesh.new(); bm.from_mesh(obj.data)
    iso = [v for v in bm.verts if not v.link_edges]
    loose = [e for e in bm.edges if not e.link_faces]
    if iso: bmesh.ops.delete(bm, geom=iso, context='VERTS')
    if loose: bmesh.ops.delete(bm, geom=loose, context='EDGES')
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data); bm.free()

def decimate(obj, target_tris):
    cur = tri_equiv(obj.data)
    ratio = max(min(target_tris / cur if cur else 1.0, 1.0), .01)
    m = obj.modifiers.new("dec", 'DECIMATE')
    m.ratio = ratio
    m.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=m.name)

def transfer_uvs(low, high):
    m = low.modifiers.new("DataTransfer", "DATA_TRANSFER")
    m.object = high
    m.use_loop_data = True
    m.data_types_loops = {'UV'}
    m.loop_mapping = 'POLYINTERP_NEAREST'
    bpy.context.view_layer.objects.active = low
    bpy.ops.object.modifier_apply(modifier=m.name)

def get_clean_name(original_filename):
    name_only = os.path.splitext(original_filename)[0]
    parts = name_only.split('_', 1)
    base_name = parts[1] if len(parts) > 1 else name_only
    name_counts[base_name] += 1
    if name_counts[base_name] > 1:
        base_name = f"{base_name}_{name_counts[base_name]}"
    return base_name

def process_file(fpath, out_dir):
    log(f"➜ Import {os.path.basename(fpath)}")
    bpy.ops.import_scene.gltf(filepath=fpath)
    high = next((o for o in bpy.context.scene.objects if o.type == 'MESH'), None)
    if high is None:
        log("   ⚠️ No mesh found, skipped"); return

    normalize_scale_to_1_meter(high)

    print("Materials on high:")
    for mat in high.data.materials:
        print(" ", mat.name)

    clean_base = get_clean_name(os.path.basename(fpath))  # Apply strip + dedup

    for i, tri_target in enumerate(TRI_LEVELS):
        lod_name = f"{clean_base}_LOD{i}"
        low = high.copy(); low.data = high.data.copy()
        high.users_collection[0].objects.link(low)
        low.name = lod_name

        clean_object(low)
        decimate(low, tri_target)
        transfer_uvs(low, high)

        log(stats(low, f"   {lod_name}"))

        export_path = os.path.join(out_dir, f"{lod_name}.glb")
        bpy.ops.object.select_all(action='DESELECT'); low.select_set(True)

        export_kwargs = dict(
            filepath=export_path,
            export_format='GLB',
            export_apply=True,
        )
        if 'export_selected' in bpy.ops.export_scene.gltf.get_rna_type().properties:
            export_kwargs['export_selected'] = True
        else:
            export_kwargs['use_selection'] = True

        try:
            bpy.ops.export_scene.gltf(**export_kwargs)
        except Exception as e:
            log(f"   ❌ Export failed for {lod_name}")
            import traceback; traceback.print_exc()

def clear_scene():
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete()
    for coll in bpy.data.collections:
        if not coll.objects: bpy.data.collections.remove(coll)
    for datablock in (bpy.data.meshes, bpy.data.images):
        for it in datablock:
            if it.users == 0: datablock.remove(it)

def main():
    if not os.path.isdir(SOURCE_DIR):
        log("SOURCE_DIR not found"); return
    files = [f for f in os.listdir(SOURCE_DIR) if f.lower().endswith(".glb")]
    if not files:
        log("No GLBs in SOURCE_DIR"); return

    out_dir = os.path.join(SOURCE_DIR, OUT_SUBFOLDER)
    os.makedirs(out_dir, exist_ok=True)

    log(f"Found {len(files)} GLB(s). Starting …\n")
    for f in files:
        clear_scene()
        process_file(os.path.join(SOURCE_DIR, f), out_dir)
    log("🎉 LOD generation complete!")

if __name__ == "__main__":
    main()
