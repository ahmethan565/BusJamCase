"""
Reference simulator for the game rules defined in ARCHITECTURE.md §2.

Not part of the Unity project. Use it to answer "what should happen here?"
questions and to verify level designs. If this file and §2 disagree, §2 wins.

Usage:
    python3 -c "from bus_jam_sim import *; from levels import LEVELS; print(solve(LEVELS['L4']))"
"""
import random
import sys
from collections import Counter, deque

COLOR_CHARS = set("RGBYPO")


# ---------------------------------------------------------------- parsing

def parse(rows):
    """Returns (width, height, cells, passengers).
    cells: {(x, y): 'floor' | 'wall' | 'void'}, passengers: {(x, y): color_char}"""
    height, width = len(rows), len(rows[0])
    if any(len(r) != width for r in rows):
        raise ValueError("ragged rows")
    cells, passengers = {}, {}
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch == '-':
                cells[(x, y)] = 'void'
            elif ch == '#':
                cells[(x, y)] = 'wall'
            elif ch == '.':
                cells[(x, y)] = 'floor'
            elif ch in COLOR_CHARS:
                cells[(x, y)] = 'floor'
                passengers[(x, y)] = ch
            else:
                raise ValueError(f"unknown char {ch!r}")
    return width, height, cells, passengers


# ---------------------------------------------------------------- board rules (§2.3–2.5)

def exists(cells, p):
    return cells.get(p, 'void') != 'void'


def is_exit(cells, p):
    """A floor cell is an exit if every cell above it in its column is void."""
    if cells.get(p) != 'floor':
        return False
    x, y = p
    return all(not exists(cells, (x, above)) for above in range(y))


DIRECTIONS = ((0, -1), (-1, 0), (1, 0), (0, 1))   # up, left, right, down


def has_path(cells, occupied, start):
    if is_exit(cells, start):
        return True
    queue, seen = deque([start]), {start}
    while queue:
        cx, cy = queue.popleft()
        for dx, dy in DIRECTIONS:
            n = (cx + dx, cy + dy)
            if n in seen or cells.get(n) != 'floor' or n in occupied:
                continue
            if is_exit(cells, n):
                return True
            seen.add(n)
            queue.append(n)
    return False


# ---------------------------------------------------------------- validation (§7.3)

def validate(level):
    _, _, cells, passengers = parse(level['grid'])
    errors = []
    pax_count, bus_count = Counter(passengers.values()), Counter(level['buses'].replace(' ', ''))
    for color in sorted(set(pax_count) | set(bus_count)):
        if pax_count[color] != bus_count[color] * level['cap']:
            errors.append(f"color {color}: {pax_count[color]} passengers vs {bus_count[color]} buses")
    if not any(is_exit(cells, p) for p in cells):
        errors.append("no exit cell")
    for p in passengers:
        if not has_path(cells, set(), p):
            errors.append(f"passenger at {p} can never reach an exit")
    return errors


# ---------------------------------------------------------------- session (§2.6–2.8)

class Session:
    def __init__(self, level):
        _, _, self.cells, passengers = parse(level['grid'])
        self.passengers = dict(passengers)
        self.capacity = level['cap']
        self.buses = level['buses'].replace(' ', '')   # spaces are ignored (§7.1)
        self.slots = [None] * level['slots']
        self.bus_index = 0
        self.bus_fill = 0
        self.result = None            # None | 'won' | 'lost'

    def clone(self):
        c = object.__new__(Session)
        c.__dict__ = dict(self.__dict__)
        c.passengers = dict(self.passengers)
        c.slots = list(self.slots)
        return c

    def state_key(self):
        return (frozenset(self.passengers.items()), tuple(self.slots), self.bus_index, self.bus_fill)

    def current_bus(self):
        return self.buses[self.bus_index] if self.bus_index < len(self.buses) else None

    def movable(self):
        occupied = set(self.passengers)
        return [p for p in self.passengers if has_path(self.cells, occupied, p)]

    def tap(self, p):
        if self.result or p not in self.passengers:
            return 'noop'
        if not has_path(self.cells, set(self.passengers), p):
            return 'blocked'
        color = self.passengers.pop(p)
        if self.current_bus() == color:
            self._board()
        else:
            # The area is never left full while playing (§2.8), so a free slot always exists here.
            free = next(i for i, v in enumerate(self.slots) if v is None)
            self.slots[free] = color
            if all(v is not None for v in self.slots):
                self.result = 'lost'      # the last free slot was taken (§2.8)
                return 'lost'
        if not self.passengers and all(v is None for v in self.slots):
            self.result = 'won'
        return 'ok'

    def _board(self):
        self.bus_fill += 1
        if self.bus_fill == self.capacity:
            self.bus_index += 1
            self.bus_fill = 0
            self._board_waiting()

    def _board_waiting(self):
        while self.current_bus() is not None:
            slot = next((i for i, v in enumerate(self.slots) if v == self.current_bus()), None)
            if slot is None:
                return
            self.slots[slot] = None      # others do not shift
            self._board()


# ---------------------------------------------------------------- solver & difficulty (§7.4)

def solve(level, max_states=200_000):
    """Returns a winning tap sequence or None."""
    sys.setrecursionlimit(10_000)
    seen = set()

    def dfs(session):
        if session.result == 'won':
            return []
        if session.result == 'lost' or len(seen) >= max_states:
            return None
        key = session.state_key()
        if key in seen:
            return None
        seen.add(key)
        for move in session.movable():
            nxt = session.clone()
            nxt.tap(move)
            rest = dfs(nxt)
            if rest is not None:
                return [move] + rest
        return None

    return dfs(Session(level))


def random_win_rate(level, games=3000, seed=None):
    rng = random.Random(seed)
    wins = 0
    for _ in range(games):
        s = Session(level)
        while not s.result:
            moves = s.movable()
            if not moves:
                raise AssertionError("softlock: no movable passenger")   # must never happen (§2.9)
            s.tap(rng.choice(moves))
        wins += s.result == 'won'
    return wins / games
