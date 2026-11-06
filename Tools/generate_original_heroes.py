#!/usr/bin/env python3
"""Draw the four playable souls as original pixel art.

Writes CharacterSheets/{pilot,medium,reaper,alchemist}.png using the frame rectangles,
pivots and clip layout of CharacterSheets/catalog.json, so the game code is unchanged.
Every frame is drawn from scratch: chibi proportions (big head, short body), a dark
outline, light from the upper left and four distinct original costumes.

Usage: python3 Tools/generate_original_heroes.py [--preview out.png]
"""
import argparse
import json
import math
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SHEETS = ROOT / 'Assets/_Game/Resources/CharacterSheets'
OUTLINE = (24, 14, 22, 255)


def hexc(value, alpha=255):
    return ((value >> 16) & 255, (value >> 8) & 255, value & 255, alpha)


# Each costume: colours plus headwear style. Keys: skin, skin_dark, hair, hair_dark, hair_light,
# coat, coat_dark, coat_light, pants, boots, accent, accent_dark, eye, headwear.
COSTUMES = {
    'pilot': dict(  # O Caçador: wide-brim hat, teal greatcoat, red scarf
        skin=hexc(0xf2c08a), skin_dark=hexc(0xc98a5c), hair=hexc(0xd8a24a), hair_dark=hexc(0xa06a2a), hair_light=hexc(0xf0cc78),
        coat=hexc(0x2e8a86), coat_dark=hexc(0x1c5a5c), coat_light=hexc(0x4cb8a8), pants=hexc(0x4a3a30), boots=hexc(0x6a4024),
        accent=hexc(0xd8443a), accent_dark=hexc(0x8e2228), eye=hexc(0x1e1420), hat=hexc(0x7a4a2a), hat_dark=hexc(0x4e2c18),
        hat_light=hexc(0xa06a3c), band=hexc(0xd8443a), headwear='hat'),
    'medium': dict(  # A Médium: violet hood and veil, silver hair, gold amulet
        skin=hexc(0xf0d0b8), skin_dark=hexc(0xc89a88), hair=hexc(0xd8d4e8), hair_dark=hexc(0x9a94b8), hair_light=hexc(0xf4f2fc),
        coat=hexc(0x6a3c9a), coat_dark=hexc(0x40205e), coat_light=hexc(0x9264c4), pants=hexc(0x3a2a52), boots=hexc(0x2c203c),
        accent=hexc(0xf0c25a), accent_dark=hexc(0xb07e2a), eye=hexc(0x4a2a6a), hat=hexc(0x553080), hat_dark=hexc(0x351a56),
        hat_light=hexc(0x7a4cae), band=hexc(0xf0c25a), headwear='hood'),
    'reaper': dict(  # O Ceifador: charcoal hood, bone mask, red sash
        skin=hexc(0xe8e0cc), skin_dark=hexc(0xb0a690), hair=hexc(0x2e2a36), hair_dark=hexc(0x1a1720), hair_light=hexc(0x46404e),
        coat=hexc(0x34303c), coat_dark=hexc(0x1e1b24), coat_light=hexc(0x4c4656), pants=hexc(0x24202a), boots=hexc(0x18151c),
        accent=hexc(0xb8283a), accent_dark=hexc(0x721826), eye=hexc(0xd83a4a), hat=hexc(0x2a2632), hat_dark=hexc(0x17141c),
        hat_light=hexc(0x423c4a), band=hexc(0xb8283a), headwear='mask'),
    'alchemist': dict(  # A Alquimista: brass goggles, wild orange hair, white coat, blue apron
        skin=hexc(0xf4c8a0), skin_dark=hexc(0xcc9670), hair=hexc(0xe8702a), hair_dark=hexc(0xb04a18), hair_light=hexc(0xffa458),
        coat=hexc(0xe8e4dc), coat_dark=hexc(0xb0aaa0), coat_light=hexc(0xffffff), pants=hexc(0x3a4a6a), boots=hexc(0x4a3424),
        accent=hexc(0x3a7ad8), accent_dark=hexc(0x24509a), eye=hexc(0x1e2430), hat=hexc(0xc89a3a), hat_dark=hexc(0x8a6420),
        hat_light=hexc(0xf0cc6a), band=hexc(0x5ad89a), headwear='goggles'),
}


class Canvas:
    def __init__(self, width, height):
        self.w, self.h = width, height
        self.px = [[None] * width for _ in range(height)]

    def set(self, x, y, color):
        if color and 0 <= x < self.w and 0 <= y < self.h:
            self.px[y][x] = color

    def rect(self, x0, y0, x1, y1, color):
        for y in range(min(y0, y1), max(y0, y1) + 1):
            for x in range(min(x0, x1), max(x0, x1) + 1):
                self.set(x, y, color)

    def ellipse(self, cx, cy, rx, ry, color):
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                if ((x + .5 - cx) / max(rx, .5)) ** 2 + ((y + .5 - cy) / max(ry, .5)) ** 2 <= 1:
                    self.set(x, y, color)

    def outline(self, color=OUTLINE):
        solid = [[c is not None for c in row] for row in self.px]
        for y in range(self.h):
            for x in range(self.w):
                if solid[y][x]:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < self.w and 0 <= ny < self.h and solid[ny][nx]:
                        self.px[y][x] = color
                        break

    def image(self, alpha_scale=1.0):
        img = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        data = img.load()
        for y in range(self.h):
            for x in range(self.w):
                c = self.px[y][x]
                if c:
                    data[x, y] = (c[0], c[1], c[2], int(c[3] * alpha_scale))
        return img


# --------------------------------------------------------------------------- body parts

def draw_legs(c, cx, ground, facing, step, p):
    """step in [-1, 1]: walk phase; feet are two-pixel boots, legs two pixels of trousers."""
    if facing in ('Right', 'FrontRight', 'BackRight'):
        front, back = int(round(step * 2)), int(round(-step * 2))
        lift_f = 1 if step > .5 else 0
        lift_b = 1 if step < -.5 else 0
        for offset, lift, shade in ((back, lift_b, p['pants']), (front, lift_f, p['pants'])):
            c.rect(cx - 1 + offset, ground - 5 - lift, cx + 1 + offset, ground - 3 - lift, shade)
            c.rect(cx - 1 + offset, ground - 2 - lift, cx + 2 + offset, ground - 1 - lift, p['boots'])
    else:
        lift_l = 1 if step > .5 else 0
        lift_r = 1 if step < -.5 else 0
        c.rect(cx - 3, ground - 5 - lift_l, cx - 1, ground - 3 - lift_l, p['pants'])
        c.rect(cx + 1, ground - 5 - lift_r, cx + 3, ground - 3 - lift_r, p['pants'])
        c.rect(cx - 3, ground - 2 - lift_l, cx - 1, ground - 1 - lift_l, p['boots'])
        c.rect(cx + 1, ground - 2 - lift_r, cx + 3, ground - 1 - lift_r, p['boots'])


def draw_torso(c, cx, top, facing, p, swing, pose, name):
    """Coat from row top to top+6 with lit left edge, belt/apron/scarf details and arms."""
    wide = 4 if facing in ('Front', 'Back') else 3
    c.rect(cx - wide, top, cx + wide, top + 6, p['coat'])
    c.rect(cx - wide, top, cx - wide, top + 6, p['coat_light'])
    c.rect(cx + wide, top + 1, cx + wide, top + 6, p['coat_dark'])
    c.rect(cx - wide, top + 6, cx + wide, top + 6, p['coat_dark'])
    if name == 'alchemist' and facing in ('Front', 'FrontRight'):
        c.rect(cx - 2, top + 2, cx + 2, top + 6, p['accent'])
        c.rect(cx + 2, top + 2, cx + 2, top + 6, p['accent_dark'])
        c.set(cx - 1, top + 3, p['band'])
    elif name == 'medium' and facing in ('Front', 'FrontRight'):
        c.set(cx, top + 1, p['accent']); c.set(cx, top + 2, p['accent_dark'])
        c.rect(cx - 1, top + 4, cx + 1, top + 4, p['coat_dark'])
    elif name == 'reaper':
        c.rect(cx - wide, top + 4, cx + wide, top + 4, p['accent'])
        if facing in ('Front', 'FrontRight'):
            c.rect(cx - 1, top + 1, cx + 1, top + 3, p['coat_dark'])
    else:
        c.rect(cx - wide, top + 4, cx + wide, top + 4, p['boots'])
        if facing in ('Front', 'FrontRight'):
            c.set(cx, top + 4, p['accent'])
    # Collar or scarf ring.
    if name == 'pilot' and facing != 'Back':
        c.rect(cx - wide + 1, top, cx + wide - 1, top + 1, p['accent'])
        c.rect(cx - wide + 1, top + 1, cx + wide - 1, top + 1, p['accent_dark'])
        if facing in ('Front', 'FrontRight'):
            c.rect(cx + 1, top + 2, cx + 2, top + 4, p['accent'])
    # Arms: a sleeve and a hand on each side, swinging opposite to the legs.
    arm_l = int(round(-swing)); arm_r = int(round(swing))
    if facing in ('Front', 'Back'):
        if pose == 'TwoHanded':
            c.rect(cx - wide - 1, top + 1, cx - wide - 1, top + 3, p['coat_dark'])
            c.rect(cx + wide + 1, top + 1, cx + wide + 1, top + 3, p['coat_dark'])
            c.set(cx - 2, top + 4, p['skin']); c.set(cx + 2, top + 4, p['skin'])
        else:
            c.rect(cx - wide - 1, top + 1 + arm_l, cx - wide - 1, top + 4 + arm_l, p['coat_dark'])
            c.set(cx - wide - 1, top + 5 + arm_l, p['skin'])
            if pose == 'OneHanded':
                c.rect(cx + wide + 1, top + 1, cx + wide + 1, top + 3, p['coat_dark'])
                c.set(cx + wide + 1, top + 4, p['skin'])
            else:
                c.rect(cx + wide + 1, top + 1 + arm_r, cx + wide + 1, top + 4 + arm_r, p['coat_dark'])
                c.set(cx + wide + 1, top + 5 + arm_r, p['skin'])
    else:
        reach = 2 if pose in ('OneHanded', 'TwoHanded') else 0
        c.rect(cx - 1 + arm_r, top + 1, cx + arm_r, top + 4, p['coat_dark'])
        c.set(cx + arm_r + reach, top + 4 - (1 if reach else 0), p['skin'])
        if reach:
            c.rect(cx + 1, top + 3, cx + reach, top + 3, p['coat_dark'])


def draw_head(c, cx, top, facing, p, name, blink=False, dead=False):
    """Big chibi head, 11 wide and 10 tall, from row top. The face looks toward +x when sideways."""
    shift = {'Front': 0, 'FrontRight': 1, 'Right': 2, 'BackRight': 1, 'Back': 0}[facing]
    # Skull shape and skin.
    c.ellipse(cx + .5, top + 5, 5.5, 5, p['skin'])
    c.rect(cx - 4, top + 7, cx + 4, top + 9, p['skin'])
    c.rect(cx - 4, top + 8, cx - 4, top + 9, p['skin_dark'] if facing != 'Right' else p['skin'])
    c.rect(cx + 5, top + 6, cx + 5, top + 8, p['skin_dark'])
    show_face = facing in ('Front', 'FrontRight', 'Right')
    hw = p['headwear']
    # Hair mass: covers the top and, from behind, the whole head.
    if facing in ('Back', 'BackRight') or not show_face:
        c.ellipse(cx + .5, top + 5, 5.5, 5, p['hair'])
        c.rect(cx - 4, top + 7, cx + 5, top + 9, p['hair'])
        c.rect(cx - 4, top + 2, cx - 3, top + 8, p['hair_light'])
        c.rect(cx + 4, top + 4, cx + 5, top + 9, p['hair_dark'])
        if facing == 'BackRight':
            c.rect(cx + 4, top + 6, cx + 5, top + 8, p['skin'])
            c.set(cx + 5, top + 7, p['skin_dark'])
    else:
        c.ellipse(cx + .5 - shift * .5, top + 3, 5.5, 3.2, p['hair'])
        c.rect(cx - 5, top + 2, cx - 5 + max(0, 2 - shift), top + 7, p['hair'])
        if facing == 'Front':
            c.rect(cx + 5, top + 2, cx + 6, top + 6, p['hair'])
        c.rect(cx - 4, top + 1, cx - 2, top + 2, p['hair_light'])
        c.set(cx - 4 + shift, top + 5, p['hair']); c.set(cx - 1 + shift, top + 5, p['hair']); c.set(cx + 2 + shift, top + 5, p['hair'])
        c.rect(cx + 3, top + 3, cx + 5, top + 4, p['hair_dark'])
        # Eyes, two pixels tall; a dead soul gets crosses.
        ey = top + 6
        eyes = [cx - 2 + shift, cx + 2 + shift] if facing != 'Right' else [cx + 3]
        for ex in eyes:
            if dead:
                c.set(ex - 1, ey, p['eye']); c.set(ex + 1, ey, p['eye']); c.set(ex, ey + 1, p['eye'])
                c.set(ex - 1, ey + 2, p['eye']); c.set(ex + 1, ey + 2, p['eye'])
            elif blink:
                c.set(ex, ey + 1, p['eye'])
            else:
                c.rect(ex, ey, ex, ey + 1, p['eye'])
                c.set(ex, ey, (255, 255, 255, 255) if name != 'reaper' else p['eye'])
        if facing == 'Right':
            c.set(cx + 6, top + 7, p['skin'])
        if name != 'reaper' and facing != 'Right':
            c.set(cx - 3 + shift, top + 8, (240, 130, 120, 255))
            c.set(cx + 3 + shift, top + 8, (240, 130, 120, 255))
    # Headwear per costume.
    if hw == 'hat':
        c.rect(cx - 7, top + 2, cx + 7, top + 3, p['hat'])
        c.rect(cx - 7, top + 3, cx + 7, top + 3, p['hat_dark'])
        c.rect(cx - 4, top - 2, cx + 4, top + 2, p['hat'])
        c.rect(cx - 4, top - 2, cx - 3, top + 1, p['hat_light'])
        c.rect(cx - 4, top + 1, cx + 4, top + 1, p['band'])
        c.rect(cx + 3, top - 1, cx + 4, top + 1, p['hat_dark'])
    elif hw == 'hood':
        c.ellipse(cx + .5, top + 4, 6.5, 5.5, p['hat'])
        c.rect(cx - 6, top + 4, cx - 5, top + 10, p['hat'])
        c.rect(cx + 6, top + 4, cx + 7, top + 10, p['hat_dark'])
        c.rect(cx - 5, top, cx - 3, top + 4, p['hat_light'])
        if show_face:
            c.ellipse(cx + .5 + shift * .5, top + 6.5, 4.2, 3.6, p['skin'])
            for i, hx in enumerate(range(cx - 3 + shift, cx + 4 + shift)):
                c.set(hx, top + 4 - (i % 2), p['hair'])
            ey = top + 6
            eyes = [cx - 2 + shift, cx + 2 + shift] if facing != 'Right' else [cx + 3]
            for ex in eyes:
                if dead:
                    c.set(ex, ey, p['eye']); c.set(ex - 1, ey + 1, p['eye']); c.set(ex + 1, ey + 1, p['eye'])
                else:
                    c.rect(ex, ey, ex, ey + (0 if blink else 1), p['eye'])
            c.set(cx + shift, top + 9, p['band'])
        else:
            # A silver braid falls from under the hood.
            c.rect(cx, top + 7, cx + 1, top + 11, p['hair'])
            c.rect(cx + 1, top + 8, cx + 1, top + 11, p['hair_dark'])
    elif hw == 'mask':
        c.ellipse(cx + .5, top + 4, 6.5, 5.5, p['hat'])
        c.rect(cx - 6, top + 4, cx + 7, top + 10, p['hat'])
        c.rect(cx - 5, top, cx - 3, top + 5, p['hat_light'])
        c.rect(cx + 6, top + 4, cx + 7, top + 10, p['hat_dark'])
        if show_face:
            c.ellipse(cx + .5 + shift * .5, top + 6.5, 4, 3.4, p['skin'])
            c.rect(cx - 3 + shift, top + 9, cx + 4 + shift, top + 9, p['skin_dark'])
            for ex in ([cx - 2 + shift, cx + 2 + shift] if facing != 'Right' else [cx + 3]):
                c.rect(ex, top + 5, ex + 1, top + 6, OUTLINE)
                if not dead:
                    c.set(ex, top + 6, p['eye'])
            c.rect(cx - 1 + shift, top + 8, cx + 1 + shift, top + 8, OUTLINE)
    elif hw == 'goggles':
        for hx in range(cx - 6, cx + 7, 2):
            c.set(hx, top - 1 - (abs(hx - cx) < 4), p['hair'])
            c.set(hx + 1, top, p['hair_light'])
        if show_face:
            gy = top + 2
            for gx in ([cx - 3 + shift, cx + 2 + shift] if facing != 'Right' else [cx + 2]):
                c.rect(gx, gy, gx + 2, gy + 1, p['hat'])
                c.set(gx + 1, gy, p['band']); c.set(gx + 2, gy + 1, p['hat_dark'])
            c.rect(cx - 5, gy + 1, cx + 6, gy + 1, p['hat_dark']) if facing == 'Front' else None
        else:
            c.rect(cx - 5, top + 3, cx + 6, top + 3, p['hat_dark'])


def hero(name, clip, facing, pose, frame, count, height):
    """One frame of one soul, on a 32 x height canvas with the feet 4 px above the bottom."""
    p = COSTUMES[name]
    c = Canvas(32, height)
    cx, ground = 15, height - 4
    t = frame / max(1, count)
    step = swing = bob = 0
    blink = dead = False
    lift = 0
    if clip in ('Walk', 'Kick', 'Stairs', 'Entrance', 'Dance', 'TableFlip'):
        step = math.sin(t * 2 * math.pi)
        swing = -step * 1.2
        bob = 1 if frame % 3 == 1 else 0
        if clip == 'Dance':
            lift = 1 if frame % 2 else 0
    elif clip in ('Idle', 'SelectIdle', 'Pet', 'Draw', 'ThumbsUp', 'SelectIntro', 'OpenChest', 'OpenMimic', 'Revive', 'SpitOut',
                  'GhostIdle', 'MiniBlank', 'ObtainItem', 'TableSlide', 'PitRespawn'):
        bob = 1 if (frame % max(2, count)) >= max(1, count // 2) else 0
        blink = clip == 'Idle' and frame == count - 1 and count >= 5
    elif clip == 'Fly':
        lift = 2 + frame % 2
    if clip == 'Dodge':
        return roll(name, facing, frame, count, height)
    if clip in ('Death', 'Tumble'):
        return death(name, frame, count, height, clip == 'Tumble')
    if clip == 'PitFall':
        base = hero(name, 'Idle', facing, 'Empty', 0, 6, height)
        scale = max(.15, 1 - frame / max(1, count - 1) * .85)
        small = base.resize((max(1, int(32 * scale)), max(1, int(height * scale))), Image.NEAREST)
        out = Image.new('RGBA', (32, height), (0, 0, 0, 0))
        out.paste(small, (16 - small.width // 2, ground - int((height - 4) * scale)), small)
        return out
    if clip == 'Hand':
        h = Canvas(32, height)
        h.rect(cx - 1, ground - 3, cx + 2, ground - 1, p['skin'])
        h.rect(cx + 2, ground - 3, cx + 2, ground - 1, p['skin_dark'])
        h.outline()
        return h.image()
    if clip == 'Shadow':
        h = Canvas(32, height)
        h.ellipse(cx + .5, ground - 1, 7, 2.2, (16, 10, 18, 110))
        return h.image()
    g = ground - lift
    draw_legs(c, cx, g, facing, step, p)
    torso_top = g - 11 + bob
    draw_torso(c, cx, torso_top, facing, p, swing, pose, name)
    if clip in ('ObtainItem', 'ThumbsUp', 'MiniBlank', 'Revive'):
        c.rect(cx - 6, torso_top - 3, cx - 6, torso_top + 1, p['coat_dark'])
        c.rect(cx + 6, torso_top - 3, cx + 6, torso_top + 1, p['coat_dark'])
        c.set(cx - 6, torso_top - 4, p['skin']); c.set(cx + 6, torso_top - 4, p['skin'])
    draw_head(c, cx, torso_top - 9, facing, p, name, blink=blink)
    c.outline()
    return c.image(.62 if clip == 'GhostIdle' else 1)


def roll(name, facing, frame, count, height):
    """Dodge roll: crouch, tuck into a spinning ball, then spring back up."""
    p = COSTUMES[name]
    c = Canvas(32, height)
    cx, ground = 15, height - 4
    if frame <= 1 or frame >= count - 2:
        crouch = 2 if frame in (1, count - 2) else 1
        draw_legs(c, cx, ground, facing, 0, p)
        draw_torso(c, cx, ground - 11 + crouch, facing, p, 0, 'Empty', name)
        draw_head(c, cx, ground - 20 + crouch * 2, facing, p, name)
        c.outline()
        return c.image()
    # Ball: coat body with a rotating band of hair/headwear and a hand.
    radius = 5.6
    cy = ground - 6.2 - (1 if frame in (3, 4) else 0)
    c.ellipse(cx + .5, cy, radius, radius, p['coat'])
    c.ellipse(cx - 1, cy - 1.5, 2.5, 2.5, p['coat_light'])
    angle = (frame - 2) / max(1, count - 4) * 2 * math.pi
    head_color = p['hat'] if p['headwear'] in ('hat', 'hood', 'mask') else p['hair']
    for i in range(-3, 4):
        a = angle + i * .28
        hx, hy = cx + .5 + math.cos(a) * 3.8, cy + math.sin(a) * 3.8
        c.ellipse(hx, hy, 1.6, 1.6, head_color)
    a = angle + math.pi
    c.set(int(cx + .5 + math.cos(a) * 4), int(cy + math.sin(a) * 4), p['skin'])
    b = angle + math.pi * .55
    c.ellipse(cx + .5 + math.cos(b) * 4, cy + math.sin(b) * 4, 1.2, 1.2, p['boots'])
    c.outline()
    return c.image()


def death(name, frame, count, height, tumble):
    """Stagger back, topple and lie still with crossed eyes."""
    p = COSTUMES[name]
    stand = Canvas(32, height)
    cx, ground = 15, height - 4
    dead = frame >= count * .4
    draw_legs(stand, cx, ground, 'Front', 0, p)
    draw_torso(stand, cx, ground - 11, 'Front', p, 0, 'Empty', name)
    draw_head(stand, cx, ground - 20, 'Front', p, name, dead=dead)
    stand.outline()
    image = stand.image()
    fall = min(1, frame / max(1, count * .45))
    if tumble:
        fall = min(1, frame / max(1, count - 1))
    angle = 90 * (fall ** 1.6)
    rotated = image.rotate(angle, resample=Image.NEAREST, center=(cx, ground), expand=False)
    box = rotated.getbbox()
    # Keep the body on the floor: lying flat it rests on the ground line, and while it
    # topples nothing may dip below it.
    if box and (angle > 60 or box[3] > ground):
        dy = ground - box[3]
        out = Image.new('RGBA', (32, height), (0, 0, 0, 0))
        out.paste(rotated, (0, dy), rotated)
        rotated = out
    return rotated


def generate(preview=None):
    catalog = json.loads((SHEETS / 'catalog.json').read_text())
    frames = catalog['frames']
    width, height = catalog['textureWidth'], catalog['textureHeight']
    sheets = {}
    for name in catalog['characters']:
        sheet = Image.new('RGBA', (width, height), (0, 0, 0, 0))
        for clip in catalog['clips']:
            indices = clip['frames']
            for k, index in enumerate(indices):
                f = frames[index]
                art = hero(name, clip['name'], clip['facing'], clip['pose'], k, len(indices), f['height'])
                # Catalog rectangles use Unity coordinates (origin at the bottom left).
                top = height - (f['y'] + f['height'])
                sheet.paste(Image.new('RGBA', (f['width'], f['height']), (0, 0, 0, 0)), (f['x'], top))
                sheet.paste(art, (f['x'], top), art)
        sheet.save(SHEETS / (name + '.png'))
        sheets[name] = sheet
    catalog['sourceSha256'] = 'original-heroes'
    for f in frames:
        f.pop('sourceRgbaSha256', None)
    (SHEETS / 'catalog.json').write_text(json.dumps(catalog, indent=2) + '\n')
    if preview:
        rows = []
        for name in catalog['characters']:
            row = Image.new('RGBA', (32 * 14, 44), (52, 46, 60, 255))
            picks = [('Idle', 'Front', 'Empty', 0), ('Idle', 'FrontRight', 'OneHanded', 0), ('Idle', 'Right', 'Empty', 0),
                     ('Idle', 'BackRight', 'Empty', 0), ('Idle', 'Back', 'Empty', 0), ('Walk', 'Front', 'Empty', 1),
                     ('Walk', 'Right', 'Empty', 1), ('Walk', 'Right', 'Empty', 4), ('Dodge', 'Right', 'Empty', 0),
                     ('Dodge', 'Right', 'Empty', 3), ('Dodge', 'Right', 'Empty', 5), ('Death', 'Front', 'Empty', 4),
                     ('Death', 'Front', 'Empty', 18), ('ObtainItem', 'Front', 'Empty', 0)]
            for i, (clip, facing, pose, k) in enumerate(picks):
                count = {'Idle': 6, 'Walk': 6, 'Dodge': 9, 'Death': 19, 'ObtainItem': 6}[clip]
                art = hero(name, clip, facing, pose, k, count, 44)
                row.paste(art, (i * 32, 0), art)
            rows.append(row)
        board = Image.new('RGBA', (32 * 14, 44 * len(rows)), (52, 46, 60, 255))
        for i, row in enumerate(rows):
            board.paste(row, (0, i * 44))
        board.resize((board.width * 4, board.height * 4), Image.NEAREST).save(preview)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--preview')
    parser.add_argument('--preview-only', action='store_true')
    args = parser.parse_args()
    if args.preview_only:
        # Draw the preview board without touching the sheets.
        catalog_names = ['pilot', 'medium', 'reaper', 'alchemist']
        rows = []
        picks = [('Idle', 'Front', 'Empty', 0), ('Idle', 'FrontRight', 'OneHanded', 0), ('Idle', 'Right', 'Empty', 0),
                 ('Idle', 'BackRight', 'Empty', 0), ('Idle', 'Back', 'Empty', 0), ('Walk', 'Front', 'Empty', 1),
                 ('Walk', 'Right', 'Empty', 1), ('Walk', 'Right', 'Empty', 4), ('Dodge', 'Right', 'Empty', 0),
                 ('Dodge', 'Right', 'Empty', 3), ('Dodge', 'Right', 'Empty', 5), ('Death', 'Front', 'Empty', 4),
                 ('Death', 'Front', 'Empty', 18), ('ObtainItem', 'Front', 'Empty', 0)]
        counts = {'Idle': 6, 'Walk': 6, 'Dodge': 9, 'Death': 19, 'ObtainItem': 6}
        board = Image.new('RGBA', (32 * len(picks), 44 * 4), (52, 46, 60, 255))
        for r, name in enumerate(catalog_names):
            for i, (clip, facing, pose, k) in enumerate(picks):
                art = hero(name, clip, facing, pose, k, counts[clip], 44)
                board.paste(art, (i * 32, r * 44), art)
        board.resize((board.width * 4, board.height * 4), Image.NEAREST).save(args.preview)
    else:
        generate(args.preview)
