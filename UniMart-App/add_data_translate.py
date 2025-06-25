import os
import re

VIEWS_DIR = "Views"  # Adjust if your Views folder is elsewhere

# Elements to target
TAGS = ["h1", "h2", "h3", "h4", "h5", "h6", "label", "button", "a", "span", "small", "p", "li", "th", "td"]

def slugify(text):
    text = re.sub(r"[^\w\s-]", '', text)
    text = re.sub(r"\s+", '_', text)
    return text.strip('_').lower()

def add_data_translate_to_line(line):
    for tag in TAGS:
        # Regex to match opening tag with static text (not containing @, {, or < inside)
        pattern = rf"<{tag}([^>]*?)>([^<@{{}}]+)</{tag}>"
        def replacer(match):
            attrs, text = match.groups()
            if 'data-translate' in attrs:
                return match.group(0)
            key = slugify(text)
            return f"<{tag}{attrs} data-translate=\"{key}\">{text}</{tag}>"
        line = re.sub(pattern, replacer, line, flags=re.IGNORECASE)
    return line

def process_file(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()
    changed = False
    new_lines = []
    for line in lines:
        new_line = add_data_translate_to_line(line)
        if new_line != line:
            changed = True
        new_lines.append(new_line)
    if changed:
        with open(filepath, "w", encoding="utf-8") as f:
            f.writelines(new_lines)
        print(f"Updated: {filepath}")

def main():
    for root, _, files in os.walk(VIEWS_DIR):
        for file in files:
            if file.endswith(".cshtml"):
                process_file(os.path.join(root, file))

if __name__ == "__main__":
    main()