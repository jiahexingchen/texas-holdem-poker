#!/usr/bin/env python3
"""
Generate SVG playing card assets for Texas Hold'em game.
Run: python generate_card_svgs.py
Output: ../client/Assets/Resources/Cards/
"""

import os
from pathlib import Path

# Card dimensions
CARD_WIDTH = 140
CARD_HEIGHT = 190
CORNER_RADIUS = 10

# Colors
RED = "#CC1111"
BLACK = "#111111"
WHITE = "#FFFFFF"
CARD_BACK_COLOR = "#2E4A7D"
CARD_BACK_PATTERN = "#3D5A8D"

# Suit symbols
SUITS = {
    'h': ('Hearts', RED, '♥'),
    'd': ('Diamonds', RED, '♦'),
    'c': ('Clubs', BLACK, '♣'),
    's': ('Spades', BLACK, '♠')
}

RANKS = ['2', '3', '4', '5', '6', '7', '8', '9', 'T', 'J', 'Q', 'K', 'A']
RANK_DISPLAY = {
    'T': '10', 'J': 'J', 'Q': 'Q', 'K': 'K', 'A': 'A',
    '2': '2', '3': '3', '4': '4', '5': '5', '6': '6', 
    '7': '7', '8': '8', '9': '9'
}


def generate_card_svg(rank: str, suit: str) -> str:
    """Generate SVG for a single card."""
    suit_name, color, symbol = SUITS[suit]
    rank_display = RANK_DISPLAY[rank]
    
    svg = f'''<?xml version="1.0" encoding="UTF-8"?>
<svg width="{CARD_WIDTH}" height="{CARD_HEIGHT}" xmlns="http://www.w3.org/2000/svg">
  <!-- Card background -->
  <rect x="1" y="1" width="{CARD_WIDTH-2}" height="{CARD_HEIGHT-2}" 
        rx="{CORNER_RADIUS}" ry="{CORNER_RADIUS}" 
        fill="{WHITE}" stroke="#CCCCCC" stroke-width="1"/>
  
  <!-- Top-left rank -->
  <text x="8" y="28" font-family="Arial, sans-serif" font-size="22" 
        font-weight="bold" fill="{color}">{rank_display}</text>
  
  <!-- Top-left suit -->
  <text x="10" y="50" font-family="Arial, sans-serif" font-size="20" 
        fill="{color}">{symbol}</text>
  
  <!-- Center suit (large) -->
  <text x="{CARD_WIDTH//2}" y="{CARD_HEIGHT//2 + 20}" 
        font-family="Arial, sans-serif" font-size="60" 
        text-anchor="middle" fill="{color}">{symbol}</text>
  
  <!-- Bottom-right rank (rotated) -->
  <g transform="rotate(180 {CARD_WIDTH//2} {CARD_HEIGHT//2})">
    <text x="8" y="28" font-family="Arial, sans-serif" font-size="22" 
          font-weight="bold" fill="{color}">{rank_display}</text>
    <text x="10" y="50" font-family="Arial, sans-serif" font-size="20" 
          fill="{color}">{symbol}</text>
  </g>
</svg>'''
    return svg


def generate_card_back_svg() -> str:
    """Generate SVG for card back."""
    pattern_size = 10
    
    svg = f'''<?xml version="1.0" encoding="UTF-8"?>
<svg width="{CARD_WIDTH}" height="{CARD_HEIGHT}" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <pattern id="checker" width="{pattern_size*2}" height="{pattern_size*2}" patternUnits="userSpaceOnUse">
      <rect width="{pattern_size}" height="{pattern_size}" fill="{CARD_BACK_COLOR}"/>
      <rect x="{pattern_size}" width="{pattern_size}" height="{pattern_size}" fill="{CARD_BACK_PATTERN}"/>
      <rect y="{pattern_size}" width="{pattern_size}" height="{pattern_size}" fill="{CARD_BACK_PATTERN}"/>
      <rect x="{pattern_size}" y="{pattern_size}" width="{pattern_size}" height="{pattern_size}" fill="{CARD_BACK_COLOR}"/>
    </pattern>
  </defs>
  
  <!-- Card background with pattern -->
  <rect x="1" y="1" width="{CARD_WIDTH-2}" height="{CARD_HEIGHT-2}" 
        rx="{CORNER_RADIUS}" ry="{CORNER_RADIUS}" 
        fill="url(#checker)"/>
  
  <!-- Border -->
  <rect x="1" y="1" width="{CARD_WIDTH-2}" height="{CARD_HEIGHT-2}" 
        rx="{CORNER_RADIUS}" ry="{CORNER_RADIUS}" 
        fill="none" stroke="{WHITE}" stroke-width="3"/>
  
  <!-- Inner border -->
  <rect x="8" y="8" width="{CARD_WIDTH-16}" height="{CARD_HEIGHT-16}" 
        rx="5" ry="5" 
        fill="none" stroke="{WHITE}" stroke-width="1" opacity="0.5"/>
  
  <!-- Center decoration -->
  <circle cx="{CARD_WIDTH//2}" cy="{CARD_HEIGHT//2}" r="25" 
          fill="{WHITE}"/>
  <circle cx="{CARD_WIDTH//2}" cy="{CARD_HEIGHT//2}" r="20" 
          fill="{CARD_BACK_COLOR}"/>
  <text x="{CARD_WIDTH//2}" y="{CARD_HEIGHT//2 + 8}" 
        font-family="Arial, sans-serif" font-size="24" font-weight="bold"
        text-anchor="middle" fill="{WHITE}">♠</text>
</svg>'''
    return svg


def generate_chip_svg(value: int, color: str, text_color: str = WHITE) -> str:
    """Generate SVG for a poker chip."""
    size = 80
    cx, cy = size // 2, size // 2
    radius = size // 2 - 2
    
    svg = f'''<?xml version="1.0" encoding="UTF-8"?>
<svg width="{size}" height="{size}" xmlns="http://www.w3.org/2000/svg">
  <!-- Main chip -->
  <circle cx="{cx}" cy="{cy}" r="{radius}" fill="{color}"/>
  
  <!-- Edge ring -->
  <circle cx="{cx}" cy="{cy}" r="{radius}" fill="none" 
          stroke="#000000" stroke-width="5" opacity="0.3"/>
  <circle cx="{cx}" cy="{cy}" r="{radius-5}" fill="none" 
          stroke="{WHITE}" stroke-width="2" opacity="0.5"/>
  
  <!-- Edge stripes -->
  <g stroke="{WHITE}" stroke-width="3" opacity="0.7">
    <line x1="{cx}" y1="2" x2="{cx}" y2="12"/>
    <line x1="{cx}" y1="{size-2}" x2="{cx}" y2="{size-12}"/>
    <line x1="2" y1="{cy}" x2="12" y2="{cy}"/>
    <line x1="{size-2}" y1="{cy}" x2="{size-12}" y2="{cy}"/>
  </g>
  
  <!-- Center circle -->
  <circle cx="{cx}" cy="{cy}" r="18" fill="{WHITE}"/>
  <circle cx="{cx}" cy="{cy}" r="15" fill="{color}"/>
  
  <!-- Value text -->
  <text x="{cx}" y="{cy + 6}" font-family="Arial, sans-serif" 
        font-size="14" font-weight="bold" text-anchor="middle" 
        fill="{text_color}">{value}</text>
</svg>'''
    return svg


def main():
    # Output directory
    script_dir = Path(__file__).parent
    cards_dir = script_dir / "../client/Assets/Resources/Cards"
    chips_dir = script_dir / "../client/Assets/Resources/Chips"
    
    cards_dir.mkdir(parents=True, exist_ok=True)
    chips_dir.mkdir(parents=True, exist_ok=True)
    
    # Generate all cards
    print("Generating card SVGs...")
    for suit in SUITS.keys():
        for rank in RANKS:
            filename = f"{rank}{suit}.svg"
            svg_content = generate_card_svg(rank, suit)
            
            filepath = cards_dir / filename
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(svg_content)
            print(f"  Created {filename}")
    
    # Generate card back
    print("Generating card back...")
    with open(cards_dir / "card_back.svg", 'w', encoding='utf-8') as f:
        f.write(generate_card_back_svg())
    print("  Created card_back.svg")
    
    # Generate chips
    print("Generating chip SVGs...")
    chips = [
        (1, "#FFFFFF", BLACK),      # White
        (5, "#CC1111", WHITE),      # Red
        (25, "#118811", WHITE),     # Green
        (100, "#111111", WHITE),    # Black
        (500, "#880088", WHITE),    # Purple
        (1000, "#FFD700", BLACK),   # Gold
    ]
    
    for value, color, text_color in chips:
        filename = f"chip_{value}.svg"
        svg_content = generate_chip_svg(value, color, text_color)
        
        filepath = chips_dir / filename
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(svg_content)
        print(f"  Created {filename}")
    
    total = len(SUITS) * len(RANKS) + 1 + len(chips)
    print(f"\nGenerated {total} SVG files:")
    print(f"  - {len(SUITS) * len(RANKS)} card faces")
    print(f"  - 1 card back")
    print(f"  - {len(chips)} chips")
    print(f"\nOutput directories:")
    print(f"  Cards: {cards_dir.absolute()}")
    print(f"  Chips: {chips_dir.absolute()}")


if __name__ == "__main__":
    main()
