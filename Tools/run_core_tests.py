#!/usr/bin/env python3
"""Run the real ContentStore C# checks using .NET 8, without NuGet packages."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

parser = argparse.ArgumentParser()
parser.add_argument('--dotnet-root', type=Path, help='Directory containing the .NET 8 SDK')
args = parser.parse_args()
repository = Path(__file__).resolve().parents[1]
executable = shutil.which('dotnet')
root = args.dotnet_root or (Path(executable).resolve().parent if executable else None)
if root is None:
    parser.error('Install .NET 8 SDK or provide --dotnet-root.')
dotnet = root / ('dotnet.exe' if os.name == 'nt' else 'dotnet')
compilers = sorted((root / 'sdk').glob('8.*/Roslyn/bincore/csc.dll'))
references = sorted((root / 'packs/Microsoft.NETCore.App.Ref').glob('8.*/ref/net8.0'))
if not compilers or not references:
    parser.error('A .NET 8 SDK and its net8.0 reference pack are required.')
with tempfile.TemporaryDirectory(prefix='museum-core-tests-') as temporary:
    output = Path(temporary)
    (output / 'Usings.cs').write_text('global using System;\nglobal using System.Linq;\nglobal using System.Collections.Generic;\n')
    flags = ['-nologo', '-target:exe', '-out:"' + str(output / 'Tests.dll') + '"']
    flags += ['-r:"' + str(p) + '"' for p in references[-1].glob('*.dll')]
    flags += ['"' + str(p) + '"' for p in [repository / 'Assets/Museum/Scripts/ContentStore.cs', repository / 'Tests/ContentStore.Tests/Program.cs', output / 'Usings.cs']]
    response = output / 'compile.rsp'
    response.write_text('\n'.join(flags))
    subprocess.run([str(dotnet), str(compilers[-1]), '@' + str(response)], check=True)
    (output / 'Tests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '8.0.0'}}}))
    subprocess.run([str(dotnet), str(output / 'Tests.dll'), str(repository)], check=True)
