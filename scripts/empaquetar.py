"""Empaqueta archivos versionados. Ejecutar después de guardar cambios en Git."""
import subprocess, zipfile
from pathlib import Path

root=Path(__file__).resolve().parents[1]
out=root/'output/BibliotecaMultimedia-entrega.zip'
out.parent.mkdir(exist_ok=True)
files=subprocess.check_output(['git','ls-files','-z'],cwd=root).decode('utf8').split('\0')
required=['README.md','docs/Evidencias.pdf','src/BibliotecaMultimedia.Api/Program.cs']
assert all(f in files and (root/f).is_file() for f in required),'Faltan archivos de entrega versionados.'
with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED) as archive:
    for f in files:
        if not f:continue
        p=Path(f)
        if any(part.lower() in {'bin','obj','.vs','.git','tmp','output','artifacts'} for part in p.parts):continue
        if p.name in {'appsettings.Local.json','.env'}:continue
        archive.write(root/p,arcname='BibliotecaMultimedia/'+p.as_posix())
with zipfile.ZipFile(out) as archive:
    assert archive.testzip() is None
    assert any('/Data/Migrations/' in n for n in archive.namelist())
    assert not any(set(Path(n).parts)&{'bin','obj','.vs','.git'} for n in archive.namelist())
    print(f'{out}\n{len(archive.namelist())} archivos; ZIP verificado sin bin, obj, .vs ni .git.')
