"""The 5 levels from ARCHITECTURE.md §21.1. Rows: y = 0 is the top row (nearest the road)."""

LEVELS = {
    "L1": dict(grid=["RBR",
                     "B.B",
                     "R.."], buses="RB", cap=3, slots=5),
    "L2": dict(grid=["GRRB",
                     "R#.G",
                     "BG#B",
                     "...."], buses="RGB", cap=3, slots=5),
    "L3": dict(grid=["RGBGR",
                     "YBRBY",
                     "-YG.-",
                     "--.--"], buses="RBGY", cap=3, slots=5),
    "L4": dict(grid=["RRBBGG",
                     "Y-RG-B",
                     "GYB..R",
                     "-BR#G-",
                     "-GYRB-"], buses="GRBYGRB", cap=3, slots=5),
    "L5": dict(grid=["--RG--",
                     "-BYPG-",
                     "RP#.#B",
                     "GRBRBY",
                     ".R-GBG",
                     "YB.RPG"], buses="GRPBYRGB", cap=3, slots=6),
}

# Known solutions (x, y), verified by bus_jam_sim.solve and replay.
KNOWN_SOLUTIONS = {
    "L1": [(0,0),(1,0),(2,0),(0,1),(2,1),(0,2)],
    "L2": [(0,0),(1,0),(2,0),(3,0),(0,1),(3,1),(0,2),(1,2),(3,2)],
    "L3": [(0,0),(1,0),(2,0),(3,0),(4,0),(0,1),(2,1),(1,1),(3,1),(4,1),(1,2),(2,2)],
    "L4": [(0,0),(1,0),(2,0),(3,0),(4,0),(5,0),(3,1),(0,1),(2,1),(5,1),(0,2),(1,2),(2,2),(5,2),
           (2,3),(2,4),(1,3),(4,3),(1,4),(3,4),(4,4)],
    "L5": [(2,0),(3,0),(1,1),(2,1),(3,1),(4,1),(0,2),(0,3),(1,2),(5,2),(3,3),(4,3),(4,4),(4,5),
           (1,3),(2,3),(5,3),(1,4),(3,4),(0,5),(5,4),(1,5),(3,5),(5,5)],
}
