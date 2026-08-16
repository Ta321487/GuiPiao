from PIL import Image
from pathlib import Path

src = Path(r"d:\TicketAssist\ttm\GuiPiao\GuiPiao.Mobile\Resources\AppIcon\guipiao_mark.png")
mark = Image.open(src).convert("RGBA")

# If the mark still has a baked checkerboard (opaque gray corners), strip it.
c0 = mark.getpixel((0, 0))
if c0[3] == 255 and max(c0[0], c0[1], c0[2]) < 220:
    px = mark.load()
    w, h = mark.size
    for y in range(h):
        for x in range(w):
            r, g, b, _ = px[x, y]
            mx, mn = max(r, g, b), min(r, g, b)
            if mx - mn <= 25 and mx < 220:
                px[x, y] = (255, 255, 255, 0)
                continue
            lum = int(0.299 * r + 0.587 * g + 0.114 * b)
            if lum >= 245:
                alpha = 255
            elif lum >= 200:
                alpha = int((lum - 200) * 255 / 45)
            else:
                alpha = 0
            px[x, y] = (255, 255, 255, alpha)
    mark.save(src)


def make_plate(size: int) -> Image.Image:
    # Brand blue plate — readable on light/dark taskbars & tray
    bg = Image.new("RGBA", (size, size), (0, 120, 212, 255))  # #0078D4
    pad = int(size * 0.15)
    inner = size - pad * 2
    m = mark.copy()
    m.thumbnail((inner, inner), Image.Resampling.LANCZOS)
    x = (size - m.width) // 2
    y = (size - m.height) // 2
    bg.alpha_composite(m, (x, y))
    return bg


out_dir_pc = Path(r"d:\TicketAssist\ttm\GuiPiao\Resources\AppIcon")
out_dir_mobile = Path(r"d:\TicketAssist\ttm\GuiPiao\GuiPiao.Mobile\Resources\AppIcon")
out_dir_pc.mkdir(parents=True, exist_ok=True)

# MAUI resource names: lowercase, letters/digits/underscore only (no hyphens).
for size, pc_name, mob_name in [
    (512, "guipiao-app.png", "guipiao_app.png"),
    (256, "guipiao-app-256.png", "guipiao_app_256.png"),
]:
    img = make_plate(size)
    img.save(out_dir_pc / pc_name)
    img.save(out_dir_mobile / mob_name)

base = make_plate(256)
ico_path = out_dir_pc / "guipiao.ico"
sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
base.save(ico_path, format="ICO", sizes=sizes)
print("wrote", ico_path, ico_path.stat().st_size)
