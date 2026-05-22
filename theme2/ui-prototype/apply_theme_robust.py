import os
import re
import shutil

# Directories
docs_dir = r"a:\Ps\POS\docs\ui-prototype"
design_dir = r"a:\Ps\POS\design1\ui-prototype"
backup_dir = r"a:\Ps\POS\design1_backup\ui-prototype"

# 1. Back up design1\ui-prototype if not already backed up
if not os.path.exists(backup_dir):
    print(f"Creating backup of design1/ui-prototype at: {backup_dir}")
    shutil.copytree(design_dir, backup_dir)
else:
    print(f"Backup already exists at: {backup_dir}")

# 2. Copy all files from docs/ui-prototype to design1/ui-prototype
print("Synchronising docs/ui-prototype files to design1/ui-prototype...")
for root, dirs, files in os.walk(docs_dir):
    rel_path = os.path.relpath(root, docs_dir)
    dest_path = os.path.join(design_dir, rel_path) if rel_path != "." else design_dir
    
    if not os.path.exists(dest_path):
        os.makedirs(dest_path)
        
    for file in files:
        if file.endswith(".py"):
            continue
        src_file = os.path.join(root, file)
        dest_file = os.path.join(dest_path, file)
        shutil.copy2(src_file, dest_file)
print("Synchronisation complete.\n")

# 3. Rename logo.png to logo_old.png in both directories to trigger text fallback
logo_paths = [
    os.path.join(docs_dir, "screens", "logo.png"),
    os.path.join(design_dir, "screens", "logo.png"),
]
for logo_path in logo_paths:
    if os.path.exists(logo_path):
        old_logo_path = logo_path.replace("logo.png", "logo_old.png")
        if not os.path.exists(old_logo_path):
            os.rename(logo_path, old_logo_path)
            print(f"Renamed {logo_path} -> logo_old.png")
        else:
            print(f"Already renamed or logo_old.png exists: {old_logo_path}")

target_dirs = [docs_dir, design_dir]

def process_html_file(filepath):
    print(f"Processing: {filepath}")
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()

    # 4. Clean duplicate style tags
    content = content.replace("<style>\n  <style>", "<style>")
    content = content.replace("<style>\n  \n  <style>", "<style>")
    content = content.replace("<style>\n<style>", "<style>")

    # 5. Inject Cache-Control Meta Tags to prevent local caching
    meta_tags = """<head>
  <meta http-equiv="Cache-Control" content="no-cache, no-store, must-revalidate" />
  <meta http-equiv="Pragma" content="no-cache" />
  <meta http-equiv="Expires" content="0" />"""
    
    if "no-cache, no-store, must-revalidate" not in content:
        content = content.replace("<head>", meta_tags, 1)

    # 6. Inject Cache-Buster to iframe src loader in index.html
    if os.path.basename(filepath) == "index.html":
        content = re.sub(r'frame\.src\s*=\s*url(?:\s*\+\s*"\?t="\s*\+\s*new\s*Date\(\)\.getTime\(\))*;?', 'frame.src = url;', content)
        content = content.replace("frame.src = url;", 'frame.src = url + "?t=" + new Date().getTime();')

    # 7. Rebrand IMAGYN -> MartPOS
    content = content.replace("IMAGYN POS", "MartPOS")
    content = content.replace("IMAGYN-POS", "MartPOS-POS")
    content = content.replace("IMAGYN", "MartPOS")
    content = content.replace("Imagyn Technologies", "MartPOS Technologies")
    content = content.replace("Imagyn Technologies Pvt. Ltd.", "MartPOS Pvt. Ltd.")
    content = content.replace("api.imagyntechnologies.com", "api.martpos.com")

    # 8. Font Import Updates via robust regex matching Google Fonts links (except Material Symbols)
    font_regex = re.compile(
        r'<link\s+[^>]*?href=["\']https://fonts\.googleapis\.com/css2\?[^"\']*?["\'][^>]*?>',
        re.DOTALL | re.IGNORECASE
    )
    new_font = '<link href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&family=IBM+Plex+Mono:wght@400;500;600&display=swap" rel="stylesheet" />'
    
    matches = font_regex.findall(content)
    for m in matches:
        if "Material" not in m:
            print(f"Replacing Google Fonts tag in {os.path.basename(filepath)}")
            content = content.replace(m, new_font)

    # 9. Font Variables / CSS Declarations
    content = content.replace("var(--heading)", "var(--sans)")
    content = content.replace("var(--sans-serif)", "var(--sans)")
    
    # Generic font stack replacements in inline styles or style tags
    content = re.sub(r"['\"]Plus Jakarta Sans['\"],\s*['\"]Inter['\"],\s*ui-sans-serif,\s*sans-serif", "'Manrope', sans-serif", content)
    content = re.sub(r"['\"]Inter Tight['\"],\s*['\"]Inter['\"],\s*ui-sans-serif,\s*sans-serif", "'Manrope', sans-serif", content)
    content = re.sub(r"['\"]Space Grotesk['\"],\s*sans-serif", "'Manrope', sans-serif", content)
    content = re.sub(r"['\"]Inter['\"],\s*sans-serif", "'Manrope', sans-serif", content)
    content = content.replace("font-family:Inter,sans-serif", "font-family:'Manrope',sans-serif")
    content = content.replace("font-family: Inter,sans-serif", "font-family: 'Manrope',sans-serif")
    content = content.replace("font-family: Inter, sans-serif", "font-family: 'Manrope', sans-serif")

    # 10. Rebrand Colors to match Shopify POS mint accent & soft pink warning
    # Clean up exact variable colors
    content = content.replace("--green:      #10B981;", "--green:      #35C5A6;")
    content = content.replace("--green-dim:  #ECFDF5;", "--green-dim:  #E6F9F5;")
    content = content.replace("--green-dark: #065F46;", "--green-dark: #2BB093;")
    content = content.replace("--green-l:    #ECFDF5;", "--green-l:    #E6F9F5;")
    
    content = content.replace("--red:        #EF4444;", "--red:        #E1477A;")
    content = content.replace("--red-dim:    #FEF2F2;", "--red-dim:    #FDF1F5;")
    content = content.replace("--red-l:      #FEF2F2;", "--red-l:      #FDF1F5;")
    
    content = content.replace("--bg:         #F8FAFC;", "--bg:         #F5F7F7;")

    # Global Hex Code overrides for exact emerald and red occurrences in styles/inline styles
    content = content.replace("#10B981", "#35C5A6")
    content = content.replace("#EF4444", "#E1477A")
    content = content.replace("#ef4444", "#E1477A")
    content = content.replace("#10b981", "#35C5A6")

    # 11. Rebrand Compliance/Fiscal terms
    content = content.replace("submit the Z-Report to FBR", "submit the Z-Report to the Fiscal Authority")
    content = content.replace("FBR FISCAL Z-REPORT", "FISCAL COMPLIANCE Z-REPORT")
    content = content.replace("FBR-ZREP-KHI", "COMP-ZREP-KHI")
    content = content.replace("FBR Integration Ready", "Compliance Integration Ready")
    content = content.replace("FBR #", "COMP #")
    content = content.replace("FBR Invoice #", "Compliance Invoice #")
    
    with open(filepath, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Finished: {filepath}\n")

# Process all html files in target directories
for target in target_dirs:
    for root, _, files in os.walk(target):
        for file in files:
            if file.endswith(".html"):
                process_html_file(os.path.join(root, file))

print("All theme applications completed successfully!")
