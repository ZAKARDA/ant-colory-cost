#!/usr/bin/env python3
"""Antopia level generator + solver.

Original work: pictures are drawn from math (or from your own PNG pixel art in tools/art/),
queues are generated, and every level is proven winnable by the solver before it is written.

Usage:
  python3 tools/levelgen.py --out AntopiaUnity/Assets/Resources --count 150 [--art tools/art] [--preview preview.png]
"""
import argparse, json, math, os, random, sys, time
from collections import deque

NESTS = 3  # must match LevelManager._defaultSlotCount (and the MainGame scene value)

PALETTE_HEX = ["FFD23F","FF9F1C","FF6B35","EF476F","B5174B","9BDE5B","2EC4B6","14825F","A8E1F0","3FB8F5",
               "F2C9A6","F25FC0","A63CFF","4C4FC7","9A5A3C","E9EEF0","3B3F5C","FFB8D9","1F78D1","D98A3D",
               "E63946","F7E8A4","8F8FE0","7AD3A2"]
# NOTE: order above is only for previews; real indices must match ColorPalette.cs:
PALETTE_HEX = ["FFD23F","FF9F1C","FF6B35","EF476F","B5174B","9BDE5B","2EC4B6","14825F","A8E1F0","3FB8F5",
               "FFB8D9","F25FC0","A63CFF","4C4FC7","9A5A3C","E9EEF0","3B3F5C","F2C9A6","1F78D1","D98A3D",
               "E63946","F7E8A4","8F8FE0","7AD3A2"]

# themes: palette indices used for role 1..k of a picture
THEMES = {
    "sunny":  [0, 1, 2, 15, 14],
    "berry":  [3, 4, 10, 15, 7],
    "ocean":  [9, 8, 18, 15, 13],
    "garden": [5, 7, 0, 3, 14],
    "candy":  [10, 11, 12, 15, 9],
    "autumn": [19, 1, 14, 21, 4],
    "night":  [13, 12, 22, 0, 16],
    "mint":   [23, 6, 8, 15, 17],
}

# ---------- picture shapes: f(u,v)->role (0 empty). u,v in [-1,1], v up ----------
def r_heart(u, v):
    x, y = u * 1.15, v * 1.15 + 0.15
    if (x * x + y * y - 1) ** 3 - x * x * y ** 3 > 0: return 0
    if math.hypot(u + .42, v - .38) < .17: return 3
    if (x * x + y * y - 1) ** 3 - x * x * y ** 3 > -0.12: return 2
    return 1
def r_star(u, v):
    a = math.atan2(v, u); r = math.hypot(u, v)
    k = (a + math.pi / 2) % (2 * math.pi / 5) - math.pi / 5
    edge = 0.42 / max(0.05, math.cos(abs(k)) * 0.42 / 0.95 + math.sin(abs(k)) * 0.0) if False else None
    outer, inner = 0.98, 0.42
    t = abs(k) / (math.pi / 5)
    lim = outer * (1 - t) + inner * t
    if r > lim: return 0
    if r < 0.2: return 3
    return 2 if r > lim - .16 else 1
def r_smile(u, v):
    r = math.hypot(u, v)
    if r > .97: return 0
    if r > .85: return 2
    if math.hypot(u + .35, v - .3) < .13 or math.hypot(u - .35, v - .3) < .13: return 3
    if .42 < math.hypot(u, v + .05) < .58 and v < -.1: return 3
    return 1
def r_flower(u, v):
    r = math.hypot(u, v); a = math.atan2(v, u)
    if r < .24: return 3
    petal = .62 + .30 * abs(math.cos(3 * a))
    if r < petal and r > .24: return 1 if r > .45 else 2
    if v < -.55 and abs(u) < .08: return 4
    return 0
def r_mushroom(u, v):
    if v > -.05:
        if math.hypot(u, (v + .05) * 1.25) > .95: return 0
        for cx, cy in ((-.42, .35), (.1, .62), (.5, .2)):
            if math.hypot(u - cx, v - cy) < .15: return 3
        return 1
    if abs(u) < .34 and v > -.95: return 2
    return 0
def r_house(u, v):
    if v > .15:
        return 1 if abs(u) < (1.0 - v) * 1.15 else 0
    if v > -.9 and abs(u) < .75:
        if abs(u + .0) < .17 and v < -.25: return 3
        if abs(u - .42) < .16 and abs(v - -.1) < .16: return 4
        return 2
    return 0
def r_fish(u, v):
    body = ((u + .1) / .78) ** 2 + (v / .5) ** 2 <= 1
    tail = u > .55 and abs(v) < (u - .5) * 1.05 and u < .98
    if not (body or tail): return 0
    if math.hypot(u + .5, v - .1) < .09: return 3
    if body and v < -.15: return 2
    return 1
def r_butterfly(u, v):
    if abs(u) < .06 and abs(v) < .7: return 3
    x = abs(u)
    up = ((x - .5) / .45) ** 2 + ((v - .3) / .5) ** 2 <= 1
    lo = ((x - .42) / .34) ** 2 + ((v + .42) / .38) ** 2 <= 1
    if up or lo:
        if math.hypot(x - .5, v - .3) < .16: return 2
        return 1
    return 0
def r_icecream(u, v):
    if v < .05:
        return 2 if abs(u) < (v + 1.0) * .52 and v > -1.0 else 0
    if math.hypot(u, (v - .3) * 1.1) < .6:
        return 3 if math.hypot(u + .2, v - .45) < .1 else 1
    return 0
def r_balloon(u, v):
    if ((u) / .62) ** 2 + ((v - .25) / .72) ** 2 <= 1:
        return 2 if math.hypot(u + .25, v - .55) < .14 else 1
    if abs(u) < .06 and -.95 < v < -.45: return 3
    return 0
def r_cat(u, v):
    head = math.hypot(u, v * 1.05) < .78
    ear = (u < -.25 and u > -.78 and v > .45 and v < (u + .78) * 2.4 + .0) or (u > .25 and u < .78 and v > .45 and v < (.78 - u) * 2.4)
    if not (head or ear): return 0
    if math.hypot(abs(u) - .3, v - .1) < .12: return 3
    if abs(u) < .1 and -.2 < v < -.08: return 2
    if v < -.28 and abs(u) < .22 and v > -.5: return 2
    return 1
def r_sun(u, v):
    r = math.hypot(u, v); a = math.atan2(v, u)
    if r < .5: return 1 if r > .22 else 2
    if r < .95 and abs(math.sin(4 * a)) > .8 and r > .62: return 3
    return 0
def r_moon(u, v):
    if math.hypot(u, v) > .9: return 0
    if math.hypot(u - .38, v - .12) < .72: return 0
    return 1 if math.hypot(u, v) < .76 else 2
def r_apple(u, v):
    if math.hypot(u, (v + .12) * .95) < .8 and not (abs(u) < .12 and v > .5):
        return 2 if math.hypot(u + .3, v - .1) < .12 else 1
    if abs(u - .06) < .07 and .6 < v < .95: return 3
    if (u - .3) ** 2 / .06 + (v - .8) ** 2 / .02 < 1: return 4
    return 0
def r_ant(u, v):  # the mascot silhouette: three round segments + antennae
    segs = [(-.55, -.05, .3), (0.0, 0.0, .24), (.5, .05, .22)]
    for cx, cy, rad in segs:
        if math.hypot(u - cx, v - cy) < rad: return 1 if cx != .5 else 2
    for sx in (-.2, .05, .3):
        if abs(u - sx) < .05 and -.7 < v < -.2: return 3
    if (abs(u - .7) < .05 and v > .2) or (abs(u - .45) < .05 and v > .15 and v < .6): return 3
    if math.hypot(u - .55, v - .12) < .06: return 4
    return 0
def r_diamond(u, v):
    d = abs(u) + abs(v)
    if d > .95: return 0
    return 3 if d < .25 else (2 if d < .55 else 1)
def r_ring(u, v):
    r = math.hypot(u, v)
    if r > .95: return 0
    if r > .72: return 1
    if r > .5: return 2
    if r > .28: return 3
    return 4

THEME_EXTRA = [10, 17, 21, 23, 8, 22, 19, 5]
SHAPES = [r_smile, r_heart, r_star, r_flower, r_mushroom, r_house, r_fish, r_butterfly, r_icecream,
          r_balloon, r_cat, r_sun, r_moon, r_apple, r_ant, r_diamond, r_ring]

def render_shape(fn, n, roles_cap):
    h = n
    w = n
    grid = [[0] * w for _ in range(h)]
    for iy in range(h):
        for ix in range(w):
            u = (ix + .5) / w * 2 - 1
            v = 1 - (iy + .5) / h * 2
            r = fn(u, v)
            if not r: continue
            r = min(r, roles_cap)
            # extra shades: split the main body into bands so larger pictures use more colors
            if roles_cap >= 4 and r == 1 and v < -.25: r = roles_cap + 1
            if roles_cap >= 5 and r == 1 and u > .35: r = roles_cap + 2
            grid[iy][ix] = r
    return grid  # grid[iy][ix], iy=0 is top

def grid_from_png(path, k):
    from PIL import Image
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    pal = [tuple(int(hx[i:i + 2], 16) for i in (0, 2, 4)) for hx in PALETTE_HEX]
    out = [[-1] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            r, g, b, a = im.getpixel((x, y))
            if a < 128: continue
            best = min(range(len(pal)), key=lambda i: (pal[i][0] - r) ** 2 + (pal[i][1] - g) ** 2 + (pal[i][2] - b) ** 2)
            out[y][x] = best
    return out

# ---------- level model ----------
class Level:
    def __init__(self, w, h, tiles):
        self.w, self.h = w, h
        self.tiles = tiles  # dict id -> color ; id = x*h + y (y=0 bottom)

def depth_map(g):
    """distance (in tiles) from each filled cell to the outside of the silhouette."""
    h = len(g); w = len(g[0]); INF = 10 ** 6
    d = [[INF] * w for _ in range(h)]
    dq = deque()
    for y in range(h):
        for x in range(w):
            if g[y][x]:
                edge = any(not (0 <= x + dx < w and 0 <= y + dy < h) or not g[y + dy][x + dx]
                           for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
                if edge: d[y][x] = 1; dq.append((x, y))
    while dq:
        x, y = dq.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and g[ny][nx] and d[ny][nx] > d[y][x] + 1:
                d[ny][nx] = d[y][x] + 1; dq.append((nx, ny))
    return d

def level_from_grid(grid_top_down):
    h = len(grid_top_down); w = len(grid_top_down[0])
    tiles = {}
    for iy in range(h):
        for x in range(w):
            c = grid_top_down[iy][x]
            if c is not None and c >= 0:
                y = h - 1 - iy
                tiles[x * h + y] = c
    return Level(w, h, tiles)

# ---------- solver ----------
def exposed_set(w, h, cells):
    """cells: list len w*h of color or -1. Returns set of exposed tile ids (id = x*h+y)."""
    stride = w + 2; ph = h + 2
    seen = bytearray(stride * ph)
    dq = deque([0]); seen[0] = 1
    exp = set()
    while dq:
        p = dq.popleft(); x = p % stride; y = p // stride
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if nx < 0 or ny < 0 or nx >= stride or ny >= ph: continue
            q = nx + ny * stride
            if seen[q]: continue
            if 0 < nx <= w and 0 < ny <= h:
                cid = (nx - 1) * h + ny - 1
                if cells[cid] >= 0:
                    exp.add(cid); continue
            seen[q] = 1; dq.append(q)
    return exp

def settle(w, h, cells, nests):
    """Nests eat exposed tiles of their color until fixpoint. cells mutated. nests: list [color, remaining]."""
    changed = True
    while changed:
        changed = False
        exp = exposed_set(w, h, cells)
        for n in nests:
            if n[1] <= 0: continue
            for cid in sorted(exp):
                if n[1] <= 0: break
                if cells[cid] == n[0]:
                    cells[cid] = -1; n[1] -= 1; changed = True
        if changed: pass
    nests[:] = [n for n in nests if n[1] > 0]

def solve(level, lanes, node_cap=3000):
    """lanes: list of lists of (color, ants). Returns (solved, nodes, peak_nests)."""
    w, h = level.w, level.h
    base = [-1] * (w * h)
    for cid, c in level.tiles.items(): base[cid] = c
    remaining_total = len(level.tiles)
    memo = set()
    nodes = [0]
    peak = [0]

    def key(cells, ptr, nests):
        return (bytes((c + 1) & 255 for c in cells), tuple(ptr), tuple(sorted((a, b) for a, b in nests)))

    def dfs(cells, ptr, nests):
        if nodes[0] > node_cap: return False
        nodes[0] += 1
        if all(c < 0 for c in cells): return True
        k = key(cells, ptr, nests)
        if k in memo: return False
        memo.add(k)
        if len(nests) >= NESTS: return False
        exp = exposed_set(w, h, cells)
        exp_colors = {cells[i] for i in exp}
        options = []
        for li, lane in enumerate(lanes):
            if ptr[li] < len(lane):
                col, ants = lane[ptr[li]]
                options.append((0 if col in exp_colors else 1, li))
        options.sort()
        for _, li in options:
            col, ants = lanes[li][ptr[li]]
            c2 = list(cells); n2 = [list(n) for n in nests] + [[col, ants]]
            p2 = list(ptr); p2[li] += 1
            peak[0] = max(peak[0], len(n2))
            settle(w, h, c2, n2)
            if dfs(c2, p2, n2): return True
        return False

    ok = dfs(base, [0] * len(lanes), [])
    return ok, nodes[0], peak[0]

def rollout(level, lanes, rng):
    w, h = level.w, level.h
    cells = [-1] * (w * h)
    for cid, c in level.tiles.items(): cells[cid] = c
    ptr = [0] * len(lanes); nests = []
    while True:
        if all(c < 0 for c in cells): return True
        if len(nests) >= NESTS: return False
        avail = [i for i in range(len(lanes)) if ptr[i] < len(lanes[i])]
        if not avail: return False
        li = rng.choice(avail); col, ants = lanes[li][ptr[li]]; ptr[li] += 1
        nests.append([col, ants]); settle(w, h, cells, nests)

def win_rate(level, lanes, rng, n=30):
    return sum(rollout(level, lanes, rng) for _ in range(n)) / n

# ---------- queue generation ----------
def peel_sequence(level, rng, lo, hi):
    """A guaranteed-valid solving order: each colony eats only tiles that are exposed when it is played."""
    w, h = level.w, level.h
    cells = [-1] * (w * h)
    for cid, c in level.tiles.items(): cells[cid] = c
    seq = []
    left = len(level.tiles)
    while left > 0:
        exp = sorted(exposed_set(w, h, cells))
        by = {}
        for cid in exp: by.setdefault(cells[cid], []).append(cid)
        col = rng.choice(sorted(by))
        take = min(len(by[col]), rng.randint(lo, hi))
        for cid in by[col][:take]: cells[cid] = -1
        left -= take; seq.append((col, take))
    return seq

def build_lanes(level, rng, lane_count, lo, hi, window):
    seq = peel_sequence(level, rng, lo, hi)
    lanes = [[] for _ in range(lane_count)]
    for i, col in enumerate(seq): lanes[i % lane_count].append(col)
    # controlled disorder: swap neighbours (within a lane, and across lanes) so a careless player gets stuck
    for lane in lanes:
        for _ in range(window * 2):
            if len(lane) > 2:
                i = rng.randrange(len(lane) - 1); lane[i], lane[i + 1] = lane[i + 1], lane[i]
    for _ in range(window * 2):
        a, b = rng.sample(range(lane_count), 2)
        if lanes[a] and lanes[b]:
            i = rng.randrange(len(lanes[a])); j = rng.randrange(len(lanes[b]))
            lanes[a][i], lanes[b][j] = lanes[b][j], lanes[a][i]
    return lanes

def make_level_json(level, lanes, difficulty):
    groups = {}
    for cid, c in level.tiles.items(): groups.setdefault(c, []).append(cid)
    lane_data = []
    for lane in lanes:
        lane_data.append({"ColonyDatas": [{"ColorIndex": c, "AntCount": a, "ObstacleType": 0, "ObstaclePairId": 0,
                                           "IceHp": 0, "IsPriority": False, "IsHidden": False} for c, a in lane],
                          "ColonySpawnerDatas": [], "Burrows": []})
    return {"LevelDifficulty": difficulty, "SizeX": level.w, "SizeY": level.h,
            "ColorData": [{"ColorIndex": c, "CubeDataIds": sorted(ids)} for c, ids in sorted(groups.items())],
            "LaneData": lane_data, "HiddenData": {"HiddenCubeDataIds": []}, "LinkedColonies": [], "LockKeys": [],
            "SealedTiles": [], "Gates": [], "ConveyorData": []}

def plan(n, total):
    """difficulty ramp: returns (size, roles, lanes, ant_lo, ant_hi, theme_name_index)."""
    t = n / max(1, total - 1)
    size = int(round(10 + 14 * t ** 0.85))
    roles = 2 + int(t * 3.2)
    lanes = 3 + (1 if t > .25 else 0) + (1 if t > .6 else 0)
    ant_lo = 2 + int(t * 2); ant_hi = 4 + int(t * 5)
    return size, min(roles, 5), lanes, ant_lo, ant_hi

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True); ap.add_argument("--count", type=int, default=150)
    ap.add_argument("--art", default=None); ap.add_argument("--preview", default=None)
    ap.add_argument("--seed", type=int, default=7)
    a = ap.parse_args()
    outdir = os.path.join(a.out, "Levels"); os.makedirs(outdir, exist_ok=True)
    rng_master = random.Random(a.seed)
    themes = list(THEMES.values())
    art_files = sorted(f for f in os.listdir(a.art) if f.lower().endswith(".png")) if a.art and os.path.isdir(a.art) else []
    stats = []; previews = []
    t0 = time.time()
    for n in range(a.count):
        size, roles, lane_count, lo, hi = plan(n, a.count)
        if n < 6: size, roles, lane_count = 10 + n, 2 if n < 3 else 3, 3
        rng = random.Random(rng_master.randint(0, 10 ** 9))
        ok = False; best = None
        for attempt in range(12):
            if art_files and n < len(art_files):
                grid = grid_from_png(os.path.join(a.art, art_files[n]), roles)
                level = level_from_grid(grid)
            else:
                fn = SHAPES[(n + (attempt // 20)) % len(SHAPES)]
                theme = themes[(n // len(SHAPES) + n) % len(themes)]
                g = render_shape(fn, size, roles)
                ext = [THEME_EXTRA[(n + 1 + attempt) % len(THEME_EXTRA)], THEME_EXTRA[(n + 4 + attempt) % len(THEME_EXTRA)], THEME_EXTRA[(n + 6) % len(THEME_EXTRA)]]
                full = theme + ext
                dm = depth_map(g)
                t_ = n / max(1, a.count - 1)
                width = max(1, int(round(size / (7 - 3 * t_))))      # onion-layer thickness
                layers = [theme[1], theme[0], ext[0], ext[1], ext[2]][: 3 + int(t_ * 2)]
                grid = []
                for iy, row in enumerate(g):
                    out = []
                    for ix, r in enumerate(row):
                        if not r: out.append(-1)
                        elif r == 1: out.append(layers[min((dm[iy][ix] - 1) // width, len(layers) - 1)])
                        else: out.append(full[min(r, len(full)) - 1])
                    grid.append(out)
                level = level_from_grid(grid)
            if len(level.tiles) < 20: continue
            lanes = build_lanes(level, rng, lane_count, lo, hi, 1 + int(n / max(1, a.count - 1) * 7 + attempt * .15))
            solved, nodes, peak = solve(level, lanes)
            if not solved: continue
            wr = win_rate(level, lanes, rng)
            if os.environ.get("DBG"): print("lvl",n+1,"try",attempt,"tiles",len(level.tiles),"nodes",nodes,"wr",wr,flush=True)
            t = n / max(1, a.count - 1)
            lo_t, hi_t = max(0.02, .55 - .5 * t), .95 - .65 * t   # accepted random-play win-rate band
            if n < 6: lo_t, hi_t = .85, 1.01   # tutorial levels: near-effortless
            score = 0 if lo_t <= wr <= hi_t else min(abs(wr - lo_t), abs(wr - hi_t))
            if best is None or score < best[0]: best = (score, level, lanes, nodes, wr, attempt)
            if score == 0: break
        if best is not None:
            _, level, lanes, nodes, wr, attempt = best; ok = True
        if not ok:
            print(f"level {n+1}: no solvable queue found", file=sys.stderr); continue
        js = make_level_json(level, lanes, n)
        with open(os.path.join(outdir, f"Level_{n+1}.json"), "w") as f: json.dump(js, f, separators=(",", ":"))
        stats.append((n + 1, level.w, level.h, len(level.tiles), len({c for c in level.tiles.values()}),
                      sum(len(l) for l in lanes), nodes, round(wr * 100)))
        previews.append((n + 1, level))
    with open(os.path.join(a.out, "LevelCatalog.json"), "w") as f: json.dump({"count": len(stats)}, f)
    print(f"{len(stats)}/{a.count} levels written in {time.time()-t0:.1f}s")
    print("lvl  size   tiles colors colonies solver_nodes randomWin%")
    for s in stats[:: max(1, len(stats) // 15)]:
        print(f"{s[0]:>3}  {s[1]}x{s[2]:<3} {s[3]:>5} {s[4]:>6} {s[5]:>8} {s[6]:>12} {s[7]:>5}")
    if a.preview:
        from PIL import Image
        pal = [tuple(int(hx[i:i + 2], 16) for i in (0, 2, 4)) for hx in PALETTE_HEX]
        pick = previews[:: max(1, len(previews) // 24)][:24]
        cell = 6; pad = 8; cols = 6; rows = (len(pick) + cols - 1) // cols
        W = cols * (24 * cell + pad) + pad; H = rows * (24 * cell + pad) + pad
        im = Image.new("RGB", (W, H), (250, 242, 228))
        for i, (num, lv) in enumerate(pick):
            ox = pad + (i % cols) * (24 * cell + pad); oy = pad + (i // cols) * (24 * cell + pad)
            for cid, c in lv.tiles.items():
                x, y = divmod(cid, lv.h)
                for dx in range(cell - 1):
                    for dy in range(cell - 1):
                        im.putpixel((ox + x * cell + dx, oy + (lv.h - 1 - y) * cell + dy), pal[c])
        im.save(a.preview)

if __name__ == "__main__":
    main()
