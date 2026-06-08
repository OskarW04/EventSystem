import os
from pathlib import Path


def get_output_filename(target_directory):
    """
    Maps directory paths to specific output filenames.
    """
    path_lower = target_directory.lower().replace("\\", "/")

    mappings = {
        "infrastructure": "infra.txt",
        "backend/functions": "python_functions.txt",
        "backend/layers": "python_shared.txt",
        "frontend/app": "app.txt",
        "frontend/components": "components.txt",
        "frontend/lib": "lib.txt",
    }

    for key, filename in mappings.items():
        if key in path_lower:
            return filename

    return "parsed_output.txt"  # Default fallback


def parse_files_to_txt(
    target_directory, target_extensions, output_dir="output", max_lines=5000
):
    """
    Parses files with specific extensions into .txt files with directory-specific names.
    """
    # Ensure the output directory exists
    Path(output_dir).mkdir(parents=True, exist_ok=True)

    # Normalize extensions to ensure they start with a dot (e.g., 'py' -> '.py')
    target_extensions = [
        ext if ext.startswith(".") else f".{ext}" for ext in target_extensions
    ]

    base_path = Path(target_directory)

    # Determine the output filename based on the directory being scanned
    base_output_filename = get_output_filename(target_directory)
    base_output_name = base_output_filename.replace(".txt", "")

    current_file_index = 1
    current_line_count = 0
    out_file = None

    def open_next_file():
        """Handles closing the current file and opening the next batch file."""
        nonlocal out_file, current_file_index, current_line_count
        if out_file:
            out_file.close()

        # Use indexed filenames for batching (e.g., infra_1.txt, infra_2.txt)
        if current_file_index == 1:
            output_path = Path(output_dir) / base_output_filename
        else:
            output_path = (
                Path(output_dir) / f"{base_output_name}_{current_file_index}.txt"
            )

        out_file = open(output_path, "w", encoding="utf-8")
        current_file_index += 1
        current_line_count = 0

    # Open the first output file
    open_next_file()

    # Iterate through all files in the directory recursively
    for file_path in base_path.rglob("*"):
        if file_path.is_file() and file_path.suffix in target_extensions:
            try:
                with open(file_path, "r", encoding="utf-8") as f:
                    lines = f.readlines()

                # Get the relative directory path for the header
                try:
                    dir_path = str(file_path.parent.relative_to(base_path))
                    if dir_path == ".":
                        dir_path = str(base_path)
                except ValueError:
                    dir_path = str(file_path.parent)

                # Prepare the header block to your exact specifications
                header_lines = [
                    "// ============\n",
                    f"// {dir_path}\n",
                    f"// {file_path.name}\n",
                    "\n",
                ]

                # Add a leading newline if this isn't the very first item in the txt file
                if current_line_count > 0:
                    header_lines[0] = "\n" + header_lines[0]

                # 1. Write the header
                for h_line in header_lines:
                    if current_line_count >= max_lines:
                        open_next_file()
                    out_file.write(h_line)
                    current_line_count += 1

                # 2. Write the file content
                for line in lines:
                    if current_line_count >= max_lines:
                        open_next_file()

                    # Ensure the line actually ends with a newline character to maintain accurate counting
                    if not line.endswith("\n"):
                        line += "\n"

                    out_file.write(line)
                    current_line_count += 1

            except UnicodeDecodeError:
                print(
                    f"Skipping {file_path}: Not a standard text file or contains invalid UTF-8 characters."
                )
            except Exception as e:
                print(f"Error processing {file_path}: {e}")

    # Close the final file once the loop finishes
    if out_file:
        out_file.close()

    # Check if the last file was completely empty (happens if no files were found)
    if current_file_index == 1:
        last_file_path = Path(output_dir) / base_output_filename
    else:
        last_file_path = (
            Path(output_dir) / f"{base_output_name}_{current_file_index - 1}.txt"
        )

    if last_file_path.exists() and last_file_path.stat().st_size == 0:
        last_file_path.unlink()
        current_file_index -= 1

    print(
        f"Parsing complete. Created {current_file_index - 1} file(s) in '{output_dir}' with base name '{base_output_filename}'."
    )


if __name__ == "__main__":
    # ==========================================
    #               CONFIGURATION
    # ==========================================

    DIRECTORY_TO_SCAN = ["./"]
    # EXTENSIONS_TO_PARSE = [
    #     ".py",
    #     ".sh",
    #     ".tf",
    #     ".tfvars",
    #     ".json",
    # ]  # The extensions you want to target
    EXTENSIONS_TO_PARSE = [
        # ".json",
        ".cs",
    ]
    OUTPUT_DIRECTORY = "./"
    MAX_LINES_PER_FILE = 5000

    # Support both single directory (string) and multiple directories (list/tuple)
    directories = (
        DIRECTORY_TO_SCAN
        if isinstance(DIRECTORY_TO_SCAN, (list, tuple))
        else [DIRECTORY_TO_SCAN]
    )

    for directory in directories:
        parse_files_to_txt(
            target_directory=directory,
            target_extensions=EXTENSIONS_TO_PARSE,
            output_dir=OUTPUT_DIRECTORY,
            max_lines=MAX_LINES_PER_FILE,
        )
