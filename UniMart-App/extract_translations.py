import os
import re
import json

VIEWS_DIR = "Views"
TAGS = ["h1", "h2", "h3", "h4", "h5", "h6", "label", "button", "a", "span", "small", "p", "li", "th", "td"]

translations = {}

def slugify(text):
    text = re.sub(r"[^\w\s-]", '', text)
    text = re.sub(r"\s+", '_', text)
    return text.strip('_').lower()

def add_data_translate_to_line(line):
    for tag in TAGS:
        # Match tags with or without data-translate
        pattern = rf"<{tag}([^>]*?)>([^<@{{}}]+)</{tag}>"
        def replacer(match):
            attrs, text = match.groups()
            # Always collect the key and value
            if 'data-translate' in attrs:
                # Extract the key from the attribute
                key_match = re.search(r'data-translate="([^"]+)"', attrs)
                key = key_match.group(1) if key_match else slugify(text)
            else:
                key = slugify(text)
            translations[key] = text.strip()
            # Add data-translate if not present
            if 'data-translate' in attrs:
                return match.group(0)
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
    # Write translations to JSON
    with open("translations_en.json", "w", encoding="utf-8") as f:
        json.dump(translations, f, ensure_ascii=False, indent=2)
    print("Extracted translations written to translations_en.json")

if __name__ == "__main__":
    main()