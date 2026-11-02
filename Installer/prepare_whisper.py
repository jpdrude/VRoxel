"""Prepares Whisper for a setup that installs push to talk without internet (ISCC /DOffline); the regular setup
downloads it while installing instead:
  Installer/packages/whisper-cuda   whisper.cpp's server for NVIDIA graphics cards (CUDA 12)
  Installer/packages/whisper-cpu    the same for the processor, on any computer
  Installer/packages/whisper-model  the speech model, ggml-small.bin, checked against its published hash
  Installer/packages/vc_redist.x64.exe  Microsoft's Visual C++ runtime, which whisper.cpp needs (installed where missing)
Only the server and its libraries are kept, not whisper.cpp's other tools.

    python Installer/prepare_whisper.py                        downloads all three
    python Installer/prepare_whisper.py --cuda-from Whisper    takes the CUDA build (and the model, if there) from a
                                                               folder unpacked before, e.g. the project's Whisper
"""
import argparse
import hashlib
import shutil
import sys
import tempfile
import urllib.request
import zipfile
from pathlib import Path

RELEASE = "https://github.com/ggml-org/whisper.cpp/releases/download/b5454/"
CPU_ZIP = RELEASE + "whisper-bin-x64.zip"
CUDA_ZIP = RELEASE + "whisper-bin-win-cuda-12.4.0-x64.zip"
MODEL = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin"
MODEL_SHA256 = "1be3a9b2063867b937e64e2ec7483364a79917e157fa98c5d94b5c1fffea987b"
VC_REDIST = "https://aka.ms/vs/17/release/vc_redist.x64.exe"

packages = Path(__file__).resolve().parent / "packages"


def download(url, path):
    print("Downloading", url)
    with urllib.request.urlopen(url) as response, open(path, "wb") as out:
        total = int(response.headers.get("Content-Length", 0))
        done = 0
        while chunk := response.read(1 << 20):
            out.write(chunk)
            done += len(chunk)
            if total:
                print(f"\r  {done >> 20} of {total >> 20} MB", end="", flush=True)
    print()


def keep(name):
    """The server and the libraries it loads; not the other example programs or models"""
    lower = name.lower()
    return lower == "whisper-server.exe" or lower.endswith(".dll")


def from_zip(url, target):
    with tempfile.TemporaryDirectory() as temp:
        archive = Path(temp) / "whisper.zip"
        download(url, archive)
        reset(target)
        with zipfile.ZipFile(archive) as z:
            for entry in z.infolist():
                name = Path(entry.filename).name
                if not entry.is_dir() and keep(name):
                    with z.open(entry) as source, open(target / name, "wb") as out:
                        shutil.copyfileobj(source, out)


def from_folder(folder, target):
    reset(target)
    for path in Path(folder).iterdir():
        if path.is_file() and keep(path.name):
            shutil.copy2(path, target / path.name)


def reset(target):
    if target.exists():
        shutil.rmtree(target)
    target.mkdir(parents=True)


def check_server(target):
    if not (target / "whisper-server.exe").exists():
        sys.exit("No whisper-server.exe in " + str(target))
    print(target.name, "ready:", len(list(target.iterdir())), "files")


def sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as f:
        while chunk := f.read(1 << 20):
            digest.update(chunk)
    return digest.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--cuda-from", help="folder with an unpacked CUDA build (and maybe ggml-small.bin)")
    parser.add_argument("--skip-cpu", action="store_true")
    parser.add_argument("--skip-cuda", action="store_true")
    args = parser.parse_args()

    if not args.skip_cuda:
        target = packages / "whisper-cuda"
        if args.cuda_from:
            from_folder(args.cuda_from, target)
        else:
            from_zip(CUDA_ZIP, target)
        check_server(target)

    if not args.skip_cpu:
        target = packages / "whisper-cpu"
        from_zip(CPU_ZIP, target)
        check_server(target)

    model = packages / "whisper-model" / "ggml-small.bin"
    local = Path(args.cuda_from) / "ggml-small.bin" if args.cuda_from else None
    if not (model.exists() and sha256(model) == MODEL_SHA256):
        model.parent.mkdir(parents=True, exist_ok=True)
        if local is not None and local.exists() and sha256(local) == MODEL_SHA256:
            shutil.copy2(local, model)
        else:
            download(MODEL, model)
    if sha256(model) != MODEL_SHA256:
        model.unlink()
        sys.exit("The model's hash doesn't match, it was removed")
    print("Model ready:", model)

    download(VC_REDIST, packages / "vc_redist.x64.exe")
    print("Visual C++ runtime ready:", packages / "vc_redist.x64.exe")


if __name__ == "__main__":
    main()
