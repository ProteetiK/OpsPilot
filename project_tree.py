
from pathlib import Path

# Project root: current directory
ROOT = Path.cwd()

# Output file
OUTPUT_FILE = ROOT / "project_tree.txt"

# Directories to exclude
EXCLUDED_DIRS = {
    ".git",
    ".venv",
    "venv",
    "__pycache__",
    "node_modules",
    "bin",
    "obj",
    ".idea",
    ".vscode"
}

# Files to exclude
EXCLUDED_FILES = {
    ".DS_Store"
}


def generate_tree(directory: Path, prefix: str = ""):
    entries = sorted(
        [
            item for item in directory.iterdir()
            if item.name not in EXCLUDED_DIRS
            and item.name not in EXCLUDED_FILES
            and item.name != OUTPUT_FILE.name
            and not item.is_symlink()
        ],
        key=lambda item: (item.is_file(), item.name.lower())
    )

    lines = []

    for index, item in enumerate(entries):
        is_last = index == len(entries) - 1

        connector = "└── " if is_last else "├── "
        lines.append(prefix + connector + item.name)

        if item.is_dir():
            extension = "    " if is_last else "│   "
            lines.extend(
                generate_tree(item, prefix + extension)
            )

    return lines


def main():
    tree = [ROOT.name + "/"]
    tree.extend(generate_tree(ROOT))

    OUTPUT_FILE.write_text(
        "\n".join(tree),
        encoding="utf-8"
    )

    print(f"Project tree saved to: {OUTPUT_FILE}")
    print(f"Total entries: {len(tree) - 1}")


if __name__ == "__main__":
    main()
