import pdfplumber, os
base = os.path.join(os.path.expanduser('~'), 'Desktop', 'HACIGOKMEN_TEMP', 'PDF')
out_dir = os.path.join(os.path.expanduser('~'), 'Desktop', 'HACIGOKMEN_TEMP', 'METIN_CIKTI')
log_file = os.path.join(os.path.expanduser('~'), 'Desktop', 'HACIGOKMEN_TEMP', 'extract_log.txt')
os.makedirs(out_dir, exist_ok=True)
log = []
count = 0
for f in sorted(os.listdir(base)):
    if not f.lower().endswith('.pdf'): continue
    fp = os.path.join(base, f)
    try:
        with pdfplumber.open(fp) as pdf:
            txt = ''
            for p in pdf.pages:
                t = p.extract_text()
                if t: txt += t + '\n---SAYFA---\n'
        if txt.strip():
            outf = os.path.join(out_dir, f.replace('.pdf','.txt'))
            with open(outf, 'w', encoding='utf-8') as fh:
                fh.write(txt)
            count += 1
            log.append(f'OK: {f} ({len(txt)} kar)')
        else:
            log.append(f'GORUNTU: {f}')
    except Exception as e:
        log.append(f'HATA: {f} -> {e}')
log.append(f'TOPLAM: {count}')
with open(log_file, 'w', encoding='utf-8') as lf:
    lf.write('\n'.join(log))
