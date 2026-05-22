import os
import re

# Base directory
base_dir = r"a:\Ps\POS\docs\ui-prototype"

# Files to update
files = [
    os.path.join(base_dir, "index.html"),
    os.path.join(base_dir, "screens", "terminal_login.html"),
    os.path.join(base_dir, "screens", "provision_terminal.html"),
    os.path.join(base_dir, "screens", "shift_open.html"),
    os.path.join(base_dir, "screens", "main_checkout.html"),
    os.path.join(base_dir, "screens", "payment_screen.html"),
    os.path.join(base_dir, "screens", "cash_control.html"),
    os.path.join(base_dir, "screens", "shift_close.html"),
]

def apply_replacements(content, filepath):
    # 1. Fonts replacement
    font_regex = r'<link href="https://fonts\.googleapis\.com/css2\?family=[^"]+&display=swap"\s*/?>'
    new_font = '<link href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&family=IBM+Plex+Mono:wght@400;500;600&display=swap" rel="stylesheet" />'
    content = re.sub(font_regex, new_font, content)

    # 2. Font variable replacement
    content = content.replace("var(--heading)", "var(--sans)")
    content = content.replace("var(--sans-serif)", "var(--sans)")
    content = content.replace("'Inter Tight', 'Inter', ui-sans-serif, sans-serif", "'Manrope', sans-serif")
    content = content.replace("'Space Grotesk', sans-serif", "'Manrope', sans-serif")
    content = content.replace("'Inter', sans-serif", "'Manrope', sans-serif")

    # 3. Branding replacement
    content = content.replace("IMAGYN POS", "MartPOS")
    content = content.replace("IMAGYN-POS", "MartPOS-POS")
    content = content.replace("IMAGYN", "MartPOS")
    content = content.replace("Imagyn Technologies", "MartPOS Technologies")
    content = content.replace("Imagyn Technologies Pvt. Ltd.", "MartPOS Pvt. Ltd.")
    content = content.replace("api.imagyntechnologies.com", "api.martpos.com")

    # 4. Color theme (Mint/Aqua and Soft Pink)
    # Lime green replaced by Mint (#35C5A6)
    content = content.replace("#A8E63D", "#35C5A6")
    content = content.replace("#F4FBE9", "#E6F9F5")
    content = content.replace("#96D32A", "#2BB093")
    
    # Red replaced by Soft Pink (#E1477A)
    content = content.replace("#EF4444", "#E1477A")
    content = content.replace("#FEF2F2", "#FDF1F5")
    
    # rgba values of Lime green replaced by Mint
    content = content.replace("rgba(168, 230, 61", "rgba(53, 197, 166")
    content = content.replace("rgba(168,230,61", "rgba(53,197,166")
    content = content.replace("rgba(239, 68, 68", "rgba(225, 71, 122")
    content = content.replace("rgba(239,68,68", "rgba(225,71,122")

    # index.html custom colors (change #10B981 emerald to #35C5A6 mint)
    content = content.replace("#10B981", "#35C5A6")
    content = content.replace("rgba(16,185,129", "rgba(53,197,166")
    content = content.replace("#6EE7B7", "#A5F3E5")
    content = content.replace("#A7F3D0", "#A5F3E5")

    # 5. Background and layout updates
    # Change body background to #F5F7F7 where appropriate
    content = content.replace("--bg:             #FFFFFF;", "--bg:             #F5F7F7;")
    content = content.replace("--bg: #FFFFFF;", "--bg: #F5F7F7;")
    content = content.replace("--bg:         #FFFFFF;", "--bg:         #F5F7F7;")
    content = content.replace("--surface:        #F7F7F7;", "--surface:        #F5F7F7;")
    content = content.replace("--surface-solid:  #F7F7F7;", "--surface-solid:  #F5F7F7;")
    content = content.replace("--surface: #F7F7F7;", "--surface: #F5F7F7;")
    content = content.replace("--surface-solid: #F7F7F7;", "--surface-solid: #F5F7F7;")

    # 6. Light Left Sidebar Style (not dark/charcoal)
    # The active nav pill should be #111111 (Active Black) with #FFFFFF text/icon
    # Update sidebar and nav-btn classes in the CSS
    old_sidebar_css = """  .sidebar {
    width: 76px; min-width: 76px; height: 100vh;
    background: var(--navy); display: flex; flex-direction: column;
    align-items: center; padding: 20px 0; position: fixed; left: 0; top: 0; z-index: 100;
  }"""
    
    new_sidebar_css = """  .sidebar {
    width: 76px; min-width: 76px; height: 100vh;
    background: #FFFFFF; border-right: 1px solid var(--border); display: flex; flex-direction: column;
    align-items: center; padding: 20px 0; position: fixed; left: 0; top: 0; z-index: 100;
  }"""
    content = content.replace(old_sidebar_css, new_sidebar_css)

    old_nav_btn_css = """  .nav-btn {
    width: 44px; height: 44px; border-radius: 50%;
    display: flex; align-items: center; justify-content: center;
    text-decoration: none; transition: all 0.1s;
    background: transparent; color: rgba(255,255,255,0.55);
    cursor: pointer; border: none; outline: none;
  }
  .nav-btn:hover {
    background: rgba(255,255,255,0.05);
    color: #FFFFFF;
  }
  .nav-btn.active {
    background: var(--green);
    color: #202020;
  }"""

    new_nav_btn_css = """  .nav-btn {
    width: 44px; height: 44px; border-radius: 50%;
    display: flex; align-items: center; justify-content: center;
    text-decoration: none; transition: all 0.1s;
    background: transparent; color: #7A7F85;
    cursor: pointer; border: none; outline: none;
  }
  .nav-btn:hover {
    background: #F5F7F7;
    color: #111111;
  }
  .nav-btn.active {
    background: #111111;
    color: #FFFFFF;
  }"""
    content = content.replace(old_nav_btn_css, new_nav_btn_css)

    old_sidebar_footer_css = """  .sidebar-footer {
    width: 100%; display: flex; flex-direction: column; align-items: center; gap: 12px;
    padding-top: 12px; border-top: 1px solid rgba(255,255,255,0.06); flex-shrink: 0;
  }"""

    new_sidebar_footer_css = """  .sidebar-footer {
    width: 100%; display: flex; flex-direction: column; align-items: center; gap: 12px;
    padding-top: 12px; border-top: 1px solid var(--border); flex-shrink: 0;
  }"""
    content = content.replace(old_sidebar_footer_css, new_sidebar_footer_css)

    old_logout_btn_css = """  .logout-btn {
    width: 44px; height: 44px; border-radius: 50%;
    display: flex; align-items: center; justify-content: center;
    text-decoration: none; color: rgba(255,255,255,0.55);
    transition: all 0.1s;
  }
  .logout-btn:hover {
    background: rgba(239,68,68,0.1);
    color: var(--red);
  }"""

    new_logout_btn_css = """  .logout-btn {
    width: 44px; height: 44px; border-radius: 50%;
    display: flex; align-items: center; justify-content: center;
    text-decoration: none; color: #7A7F85;
    transition: all 0.1s;
  }
  .logout-btn:hover {
    background: rgba(225,71,122,0.1);
    color: var(--red);
  }"""
    content = content.replace(old_logout_btn_css, new_logout_btn_css)

    # 7. Remove Pakistan/FBR-specific terminology
    content = content.replace("submit the Z-Report to FBR", "submit the Z-Report to the Fiscal Authority")
    content = content.replace("FBR FISCAL Z-REPORT", "FISCAL COMPLIANCE Z-REPORT")
    content = content.replace("FBR-ZREP-KHI", "COMP-ZREP-KHI")
    content = content.replace("FBR Integration Ready", "Compliance Integration Ready")
    content = content.replace("FBR #", "COMP #")
    
    # 8. Cash Drawer Triggering Rule updates in payment screen notes if any
    # e.g., print receipt template update if any
    content = content.replace("FBR Invoice #", "Compliance Invoice #")

    return content

for filepath in files:
    if os.path.exists(filepath):
        print(f"Processing {filepath}...")
        with open(filepath, "r", encoding="utf-8") as f:
            content = f.read()
        
        new_content = apply_replacements(content, filepath)
        
        with open(filepath, "w", encoding="utf-8") as f:
            f.write(new_content)
        print(f"Updated {filepath} successfully.")
    else:
        print(f"File NOT found: {filepath}")
