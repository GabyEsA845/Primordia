"""Genera RootWall_OpenedFront.png (en cada tramo): el muro abierto con el interior del hueco transparente.
Es la capa que va POR ENCIMA del jugador mientras cruza el hueco (RootWallCrossing), como el marco de las puertas normales."""
from PIL import Image
from collections import deque
import os
root = os.path.dirname(os.path.abspath(__file__))
for band in ("Swamp", "Smoke", "Burnt"):
    path = os.path.join(root, band, "pieces")
    im = Image.open(os.path.join(path, "RootWall_Opened.png")).convert("RGBA")
    w, h = im.size
    px = im.load()
    inside = lambda c: c[3] > 0 and ((c[2] >= c[1] + 3 and c[2] >= c[0]) or c[:3] == (14, 18, 16))
    seen = set()
    todo = deque([(64, 40)])
    while todo:
        x, y = todo.popleft()
        if (x, y) in seen or not (0 <= x < w and 0 <= y < h) or not inside(px[x, y]):
            continue
        seen.add((x, y))
        todo.extend([(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)])
    out = im.copy()
    for x, y in seen:
        out.putpixel((x, y), (0, 0, 0, 0))
    out.save(os.path.join(path, "RootWall_OpenedFront.png"))
    print(band, len(seen), "px de interior")
    prev = Image.new("RGBA", (w, h), (255, 0, 255, 255)); prev.alpha_composite(out)
    if band == "Swamp":
        prev.resize((w * 6, h * 6), Image.NEAREST).save(os.environ.get("PREVIEW", "preview.png"))
