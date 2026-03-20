"""
3 bot token'ını komut satırından alıp .env'ye yazar.
Kullanım: python env_guncelle.py "MAIN_TOKEN" "OPERATOR_TOKEN" "RUBLE_TOKEN"
Boş bırakmak için "" kullanın.
"""
from pathlib import Path
import sys

ENV_FILE = Path(__file__).resolve().parent / ".env"


def read_env():
    if not ENV_FILE.exists():
        return []
    with open(ENV_FILE, "r", encoding="utf-8") as f:
        return f.readlines()


def write_env(lines):
    with open(ENV_FILE, "w", encoding="utf-8") as f:
        f.writelines(lines)


def set_env_value(key: str, value: str, lines: list) -> list:
    out = []
    found = False
    for line in lines:
        if line.strip().startswith(f"{key}="):
            out.append(f"{key}={value}\n" if value else line)
            found = True
        else:
            out.append(line)
    if not found and value:
        out.append(f"{key}={value}\n")
    return out


def main():
    # Komut satırı: main operator ruble (isteğe bağlı)
    args = sys.argv[1:4]
    main_tok = args[0].strip() if len(args) > 0 else ""
    op_tok = args[1].strip() if len(args) > 1 else ""
    ruble_tok = args[2].strip() if len(args) > 2 else ""

    lines = read_env()
    if main_tok:
        lines = set_env_value("MAIN_BOT_TOKEN", main_tok, lines)
    if op_tok:
        lines = set_env_value("OPERATOR_BOT_TOKEN", op_tok, lines)
    if ruble_tok:
        lines = set_env_value("RUBLE_BOT_TOKEN", ruble_tok, lines)
    write_env(lines)
    print(".env kaydedildi. Eksik token varsa token_ekle.py ile ekleyin.")


if __name__ == "__main__":
    main()
